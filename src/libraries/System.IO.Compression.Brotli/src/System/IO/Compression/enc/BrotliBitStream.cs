// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/brotli_bit_stream.c, brotli_bit_stream.h and block_encoder_inc.h.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>
/// Scratch storage of the meta-block storing functions of <see cref="BrotliBitStream"/> (allocated per call in C).
/// An encoder instance owns one: its parts are allocated on first use and reused by the following meta-blocks.
/// </summary>
internal sealed class MetablockArena
{
    private HuffmanTree[]? _tree;
    private SimpleCodes? _simple;
    private BrotliBitStream.StoreMetaBlockArena? _storeMetaBlock;

    public HuffmanTree[] Tree => _tree ??= new HuffmanTree[BrotliBitStream.MaxHuffmanTreeSize];

    /// <summary>Scratch of <see cref="BrotliBitStream.StoreMetaBlockTrivial"/> and <see cref="BrotliBitStream.StoreMetaBlockFast"/>.</summary>
    public SimpleCodes Simple => _simple ??= new SimpleCodes();

    /// <summary>Scratch of <see cref="BrotliBitStream.StoreMetaBlock"/>.</summary>
    public BrotliBitStream.StoreMetaBlockArena StoreMetaBlock => _storeMetaBlock ??= new BrotliBitStream.StoreMetaBlockArena();

    /// <summary>The histograms and prefix codes of a meta-block stored with one prefix code per category.</summary>
    internal sealed class SimpleCodes
    {
        public HistogramLiteral LitHisto;
        public HistogramCommand CmdHisto;
        public HistogramDistance DistHisto;
        public readonly byte[] LitDepth = new byte[BrotliConstants.NumLiteralSymbols];
        public readonly ushort[] LitBits = new ushort[BrotliConstants.NumLiteralSymbols];
        public readonly byte[] CmdDepth = new byte[BrotliConstants.NumCommandSymbols];
        public readonly ushort[] CmdBits = new ushort[BrotliConstants.NumCommandSymbols];
        public readonly byte[] DistDepth = new byte[BrotliBitStream.MaxSimpleDistanceAlphabetSize];
        public readonly ushort[] DistBits = new ushort[BrotliBitStream.MaxSimpleDistanceAlphabetSize];
    }
}

/// <summary>
/// Brotli bit stream functions to support the low level format. There are no
/// compression algorithms here, just the right ordering of bits to match the
/// specs.
/// All Store functions here will use a storage_ix, which is always the bit
/// position for the current storage.
/// </summary>
internal static class BrotliBitStream
{
    public const int MaxHuffmanTreeSize = 2 * BrotliConstants.NumCommandSymbols + 1;

    /// <summary>
    /// The maximum size of Huffman dictionary for distances assuming that
    /// NPOSTFIX = 0 and NDIRECT = 0 (== 140).
    /// </summary>
    public const int MaxSimpleDistanceAlphabetSize =
        BrotliConstants.NumDistanceShortCodes + 0 + ((int)BrotliConstants.LargeMaxDistanceBits << (0 + 1));

    private const int SymbolBits = 9;

    internal struct BlockTypeCodeCalculator
    {
        public nuint LastType;
        public nuint SecondLastType;

        public BlockTypeCodeCalculator()
        {
            LastType = 1;
            SecondLastType = 0;
        }
    }

    /// <summary>Data structure that stores almost everything that is needed to encode each block switch command.</summary>
    internal sealed class BlockSplitCode
    {
        public BlockTypeCodeCalculator TypeCodeCalculator = new();
        public readonly byte[] TypeDepths = new byte[BrotliConstants.MaxBlockTypeSymbols];
        public readonly ushort[] TypeBits = new ushort[BrotliConstants.MaxBlockTypeSymbols];
        public readonly byte[] LengthDepths = new byte[BrotliConstants.NumBlockLenSymbols];
        public readonly ushort[] LengthBits = new ushort[BrotliConstants.NumBlockLenSymbols];
    }

    /// <summary>Manages the encoding of one block category (literal, command or distance).</summary>
    internal sealed class BlockEncoder
    {
        public nuint HistogramLength;
        public nuint NumBlockTypes;
        public byte[] BlockTypes = [];  // Not owned.
        public uint[] BlockLengths = [];  // Not owned.
        public nuint NumBlocks;
        public readonly BlockSplitCode BlockSplitCode = new();
        public nuint BlockIx;
        public nuint BlockLen;
        public nuint EntropyIx;
        // The code tables are reused by later meta-blocks; entries that are not written for the current meta-block
        // are never read (as with the uninitialized allocations of the reference implementation).
        public byte[] Depths = [];
        public ushort[] Bits = [];

        /// <summary>Port of InitBlockEncoder.</summary>
        public void Init(nuint histogramLength, nuint numBlockTypes, byte[]? blockTypes, uint[]? blockLengths, nuint numBlocks)
        {
            HistogramLength = histogramLength;
            NumBlockTypes = numBlockTypes;
            BlockTypes = blockTypes ?? [];
            BlockLengths = blockLengths ?? [];
            NumBlocks = numBlocks;
            BlockSplitCode.TypeCodeCalculator = new BlockTypeCodeCalculator();
            BlockIx = 0;
            BlockLen = numBlocks == 0 ? 0 : BlockLengths[0];
            EntropyIx = 0;
        }
    }

    /// <summary>Scratch storage of <see cref="StoreMetaBlock"/> (allocated per call in C).</summary>
    internal sealed class StoreMetaBlockArena
    {
        public readonly EncodeContextMapArena ContextMapArena = new();
        public readonly BlockEncoder LiteralEnc = new();
        public readonly BlockEncoder CommandEnc = new();
        public readonly BlockEncoder DistanceEnc = new();
    }

    /// <summary>Scratch storage for context map encoding.</summary>
    internal sealed class EncodeContextMapArena
    {
        public readonly uint[] Histogram = new uint[BrotliConstants.MaxContextMapSymbols];
        public readonly byte[] Depths = new byte[BrotliConstants.MaxContextMapSymbols];
        public readonly ushort[] Bits = new ushort[BrotliConstants.MaxContextMapSymbols];
        // Scratch for the run-length coded context map (allocated per call in C).
        public uint[] RleSymbols = [];
    }

    // _kBrotliPrefixCodeRanges (common/constants.c): offset and number of extra bits of block length codes.
    private static ReadOnlySpan<uint> BlockLengthPrefixCodeOffset =>
    [
        1, 5, 9, 13, 17, 25, 33, 41, 49, 65, 81, 97, 113, 145, 177, 209,
        241, 305, 369, 497, 753, 1265, 2289, 4337, 8433, 16625,
    ];

    private static ReadOnlySpan<byte> BlockLengthPrefixCodeNBits =>
    [
        2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5,
        6, 6, 7, 8, 9, 10, 11, 12, 13, 24,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint BlockLengthPrefixCode(uint len)
    {
        ReadOnlySpan<uint> offsets = BlockLengthPrefixCodeOffset;
        uint code = (len >= 177) ? (len >= 753 ? 20u : 14u) : (len >= 41 ? 7u : 0u);
        while (code < (BrotliConstants.NumBlockLenSymbols - 1) &&
            len >= offsets[(int)code + 1])
        {
            ++code;
        }
        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetBlockLengthPrefixCode(uint len, out nuint code, out uint nExtra, out uint extra)
    {
        uint c = BlockLengthPrefixCode(len);
        code = c;
        nExtra = BlockLengthPrefixCodeNBits[(int)c];
        extra = len - BlockLengthPrefixCodeOffset[(int)c];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint NextBlockTypeCode(ref BlockTypeCodeCalculator calculator, byte type)
    {
        nuint typeCode = (type == calculator.LastType + 1) ? 1u :
            (type == calculator.SecondLastType) ? 0u : type + 2u;
        calculator.SecondLastType = calculator.LastType;
        calculator.LastType = type;
        return typeCode;
    }

    /// <summary>
    /// <paramref name="nibblesbits"/> represents the 2 bits to encode MNIBBLES (0-3)
    /// REQUIRES: length > 0
    /// REQUIRES: length &lt;= (1 &lt;&lt; 24)
    /// </summary>
    private static void EncodeMlen(nuint length, out ulong bits, out nuint numbits, out ulong nibblesbits)
    {
        nuint lg = (length == 1) ? 1 : FastLog.Log2FloorNonZero((uint)(length - 1)) + 1u;
        nuint mnibbles = (lg < 16 ? 16 : (lg + 3)) / 4;
        Debug.Assert(length > 0);
        Debug.Assert(length <= (1 << 24));
        Debug.Assert(lg <= 24);
        nibblesbits = mnibbles - 4;
        numbits = mnibbles * 4;
        bits = length - 1;
    }

    /// <summary>
    /// For each insert-and-copy command code: base and number of extra bits of its insert length code (bits 0-15 and
    /// 16-23) and of its copy length code (bits 24-39 and 40-47). The insert and copy length codes are decoded from
    /// the command code as in RFC 7932, section 5; the command code of a command is always derived from its insert
    /// length and copy length code (see <see cref="Command.Init"/>), so these are the same codes that StoreCommandExtra
    /// of the reference implementation recomputes from the lengths.
    /// </summary>
    private static ReadOnlySpan<ulong> CommandExtraLut =>
    [
        0x00002000000, 0x00003000000, 0x00004000000, 0x00005000000, 0x00006000000, 0x00007000000,
        0x00008000000, 0x00009000000, 0x00002000001, 0x00003000001, 0x00004000001, 0x00005000001,
        0x00006000001, 0x00007000001, 0x00008000001, 0x00009000001, 0x00002000002, 0x00003000002,
        0x00004000002, 0x00005000002, 0x00006000002, 0x00007000002, 0x00008000002, 0x00009000002,
        0x00002000003, 0x00003000003, 0x00004000003, 0x00005000003, 0x00006000003, 0x00007000003,
        0x00008000003, 0x00009000003, 0x00002000004, 0x00003000004, 0x00004000004, 0x00005000004,
        0x00006000004, 0x00007000004, 0x00008000004, 0x00009000004, 0x00002000005, 0x00003000005,
        0x00004000005, 0x00005000005, 0x00006000005, 0x00007000005, 0x00008000005, 0x00009000005,
        0x00002010006, 0x00003010006, 0x00004010006, 0x00005010006, 0x00006010006, 0x00007010006,
        0x00008010006, 0x00009010006, 0x00002010008, 0x00003010008, 0x00004010008, 0x00005010008,
        0x00006010008, 0x00007010008, 0x00008010008, 0x00009010008, 0x1000A000000, 0x1000C000000,
        0x2000E000000, 0x20012000000, 0x30016000000, 0x3001E000000, 0x40026000000, 0x40036000000,
        0x1000A000001, 0x1000C000001, 0x2000E000001, 0x20012000001, 0x30016000001, 0x3001E000001,
        0x40026000001, 0x40036000001, 0x1000A000002, 0x1000C000002, 0x2000E000002, 0x20012000002,
        0x30016000002, 0x3001E000002, 0x40026000002, 0x40036000002, 0x1000A000003, 0x1000C000003,
        0x2000E000003, 0x20012000003, 0x30016000003, 0x3001E000003, 0x40026000003, 0x40036000003,
        0x1000A000004, 0x1000C000004, 0x2000E000004, 0x20012000004, 0x30016000004, 0x3001E000004,
        0x40026000004, 0x40036000004, 0x1000A000005, 0x1000C000005, 0x2000E000005, 0x20012000005,
        0x30016000005, 0x3001E000005, 0x40026000005, 0x40036000005, 0x1000A010006, 0x1000C010006,
        0x2000E010006, 0x20012010006, 0x30016010006, 0x3001E010006, 0x40026010006, 0x40036010006,
        0x1000A010008, 0x1000C010008, 0x2000E010008, 0x20012010008, 0x30016010008, 0x3001E010008,
        0x40026010008, 0x40036010008, 0x00002000000, 0x00003000000, 0x00004000000, 0x00005000000,
        0x00006000000, 0x00007000000, 0x00008000000, 0x00009000000, 0x00002000001, 0x00003000001,
        0x00004000001, 0x00005000001, 0x00006000001, 0x00007000001, 0x00008000001, 0x00009000001,
        0x00002000002, 0x00003000002, 0x00004000002, 0x00005000002, 0x00006000002, 0x00007000002,
        0x00008000002, 0x00009000002, 0x00002000003, 0x00003000003, 0x00004000003, 0x00005000003,
        0x00006000003, 0x00007000003, 0x00008000003, 0x00009000003, 0x00002000004, 0x00003000004,
        0x00004000004, 0x00005000004, 0x00006000004, 0x00007000004, 0x00008000004, 0x00009000004,
        0x00002000005, 0x00003000005, 0x00004000005, 0x00005000005, 0x00006000005, 0x00007000005,
        0x00008000005, 0x00009000005, 0x00002010006, 0x00003010006, 0x00004010006, 0x00005010006,
        0x00006010006, 0x00007010006, 0x00008010006, 0x00009010006, 0x00002010008, 0x00003010008,
        0x00004010008, 0x00005010008, 0x00006010008, 0x00007010008, 0x00008010008, 0x00009010008,
        0x1000A000000, 0x1000C000000, 0x2000E000000, 0x20012000000, 0x30016000000, 0x3001E000000,
        0x40026000000, 0x40036000000, 0x1000A000001, 0x1000C000001, 0x2000E000001, 0x20012000001,
        0x30016000001, 0x3001E000001, 0x40026000001, 0x40036000001, 0x1000A000002, 0x1000C000002,
        0x2000E000002, 0x20012000002, 0x30016000002, 0x3001E000002, 0x40026000002, 0x40036000002,
        0x1000A000003, 0x1000C000003, 0x2000E000003, 0x20012000003, 0x30016000003, 0x3001E000003,
        0x40026000003, 0x40036000003, 0x1000A000004, 0x1000C000004, 0x2000E000004, 0x20012000004,
        0x30016000004, 0x3001E000004, 0x40026000004, 0x40036000004, 0x1000A000005, 0x1000C000005,
        0x2000E000005, 0x20012000005, 0x30016000005, 0x3001E000005, 0x40026000005, 0x40036000005,
        0x1000A010006, 0x1000C010006, 0x2000E010006, 0x20012010006, 0x30016010006, 0x3001E010006,
        0x40026010006, 0x40036010006, 0x1000A010008, 0x1000C010008, 0x2000E010008, 0x20012010008,
        0x30016010008, 0x3001E010008, 0x40026010008, 0x40036010008, 0x0000202000A, 0x0000302000A,
        0x0000402000A, 0x0000502000A, 0x0000602000A, 0x0000702000A, 0x0000802000A, 0x0000902000A,
        0x0000202000E, 0x0000302000E, 0x0000402000E, 0x0000502000E, 0x0000602000E, 0x0000702000E,
        0x0000802000E, 0x0000902000E, 0x00002030012, 0x00003030012, 0x00004030012, 0x00005030012,
        0x00006030012, 0x00007030012, 0x00008030012, 0x00009030012, 0x0000203001A, 0x0000303001A,
        0x0000403001A, 0x0000503001A, 0x0000603001A, 0x0000703001A, 0x0000803001A, 0x0000903001A,
        0x00002040022, 0x00003040022, 0x00004040022, 0x00005040022, 0x00006040022, 0x00007040022,
        0x00008040022, 0x00009040022, 0x00002040032, 0x00003040032, 0x00004040032, 0x00005040032,
        0x00006040032, 0x00007040032, 0x00008040032, 0x00009040032, 0x00002050042, 0x00003050042,
        0x00004050042, 0x00005050042, 0x00006050042, 0x00007050042, 0x00008050042, 0x00009050042,
        0x00002050062, 0x00003050062, 0x00004050062, 0x00005050062, 0x00006050062, 0x00007050062,
        0x00008050062, 0x00009050062, 0x1000A02000A, 0x1000C02000A, 0x2000E02000A, 0x2001202000A,
        0x3001602000A, 0x3001E02000A, 0x4002602000A, 0x4003602000A, 0x1000A02000E, 0x1000C02000E,
        0x2000E02000E, 0x2001202000E, 0x3001602000E, 0x3001E02000E, 0x4002602000E, 0x4003602000E,
        0x1000A030012, 0x1000C030012, 0x2000E030012, 0x20012030012, 0x30016030012, 0x3001E030012,
        0x40026030012, 0x40036030012, 0x1000A03001A, 0x1000C03001A, 0x2000E03001A, 0x2001203001A,
        0x3001603001A, 0x3001E03001A, 0x4002603001A, 0x4003603001A, 0x1000A040022, 0x1000C040022,
        0x2000E040022, 0x20012040022, 0x30016040022, 0x3001E040022, 0x40026040022, 0x40036040022,
        0x1000A040032, 0x1000C040032, 0x2000E040032, 0x20012040032, 0x30016040032, 0x3001E040032,
        0x40026040032, 0x40036040032, 0x1000A050042, 0x1000C050042, 0x2000E050042, 0x20012050042,
        0x30016050042, 0x3001E050042, 0x40026050042, 0x40036050042, 0x1000A050062, 0x1000C050062,
        0x2000E050062, 0x20012050062, 0x30016050062, 0x3001E050062, 0x40026050062, 0x40036050062,
        0x50046000000, 0x50066000000, 0x60086000000, 0x700C6000000, 0x80146000000, 0x90246000000,
        0xA0446000000, 0x180846000000, 0x50046000001, 0x50066000001, 0x60086000001, 0x700C6000001,
        0x80146000001, 0x90246000001, 0xA0446000001, 0x180846000001, 0x50046000002, 0x50066000002,
        0x60086000002, 0x700C6000002, 0x80146000002, 0x90246000002, 0xA0446000002, 0x180846000002,
        0x50046000003, 0x50066000003, 0x60086000003, 0x700C6000003, 0x80146000003, 0x90246000003,
        0xA0446000003, 0x180846000003, 0x50046000004, 0x50066000004, 0x60086000004, 0x700C6000004,
        0x80146000004, 0x90246000004, 0xA0446000004, 0x180846000004, 0x50046000005, 0x50066000005,
        0x60086000005, 0x700C6000005, 0x80146000005, 0x90246000005, 0xA0446000005, 0x180846000005,
        0x50046010006, 0x50066010006, 0x60086010006, 0x700C6010006, 0x80146010006, 0x90246010006,
        0xA0446010006, 0x180846010006, 0x50046010008, 0x50066010008, 0x60086010008, 0x700C6010008,
        0x80146010008, 0x90246010008, 0xA0446010008, 0x180846010008, 0x00002060082, 0x00003060082,
        0x00004060082, 0x00005060082, 0x00006060082, 0x00007060082, 0x00008060082, 0x00009060082,
        0x000020700C2, 0x000030700C2, 0x000040700C2, 0x000050700C2, 0x000060700C2, 0x000070700C2,
        0x000080700C2, 0x000090700C2, 0x00002080142, 0x00003080142, 0x00004080142, 0x00005080142,
        0x00006080142, 0x00007080142, 0x00008080142, 0x00009080142, 0x00002090242, 0x00003090242,
        0x00004090242, 0x00005090242, 0x00006090242, 0x00007090242, 0x00008090242, 0x00009090242,
        0x000020A0442, 0x000030A0442, 0x000040A0442, 0x000050A0442, 0x000060A0442, 0x000070A0442,
        0x000080A0442, 0x000090A0442, 0x000020C0842, 0x000030C0842, 0x000040C0842, 0x000050C0842,
        0x000060C0842, 0x000070C0842, 0x000080C0842, 0x000090C0842, 0x000020E1842, 0x000030E1842,
        0x000040E1842, 0x000050E1842, 0x000060E1842, 0x000070E1842, 0x000080E1842, 0x000090E1842,
        0x00002185842, 0x00003185842, 0x00004185842, 0x00005185842, 0x00006185842, 0x00007185842,
        0x00008185842, 0x00009185842, 0x5004602000A, 0x5006602000A, 0x6008602000A, 0x700C602000A,
        0x8014602000A, 0x9024602000A, 0xA044602000A, 0x18084602000A, 0x5004602000E, 0x5006602000E,
        0x6008602000E, 0x700C602000E, 0x8014602000E, 0x9024602000E, 0xA044602000E, 0x18084602000E,
        0x50046030012, 0x50066030012, 0x60086030012, 0x700C6030012, 0x80146030012, 0x90246030012,
        0xA0446030012, 0x180846030012, 0x5004603001A, 0x5006603001A, 0x6008603001A, 0x700C603001A,
        0x8014603001A, 0x9024603001A, 0xA044603001A, 0x18084603001A, 0x50046040022, 0x50066040022,
        0x60086040022, 0x700C6040022, 0x80146040022, 0x90246040022, 0xA0446040022, 0x180846040022,
        0x50046040032, 0x50066040032, 0x60086040032, 0x700C6040032, 0x80146040032, 0x90246040032,
        0xA0446040032, 0x180846040032, 0x50046050042, 0x50066050042, 0x60086050042, 0x700C6050042,
        0x80146050042, 0x90246050042, 0xA0446050042, 0x180846050042, 0x50046050062, 0x50066050062,
        0x60086050062, 0x700C6050062, 0x80146050062, 0x90246050062, 0xA0446050062, 0x180846050062,
        0x1000A060082, 0x1000C060082, 0x2000E060082, 0x20012060082, 0x30016060082, 0x3001E060082,
        0x40026060082, 0x40036060082, 0x1000A0700C2, 0x1000C0700C2, 0x2000E0700C2, 0x200120700C2,
        0x300160700C2, 0x3001E0700C2, 0x400260700C2, 0x400360700C2, 0x1000A080142, 0x1000C080142,
        0x2000E080142, 0x20012080142, 0x30016080142, 0x3001E080142, 0x40026080142, 0x40036080142,
        0x1000A090242, 0x1000C090242, 0x2000E090242, 0x20012090242, 0x30016090242, 0x3001E090242,
        0x40026090242, 0x40036090242, 0x1000A0A0442, 0x1000C0A0442, 0x2000E0A0442, 0x200120A0442,
        0x300160A0442, 0x3001E0A0442, 0x400260A0442, 0x400360A0442, 0x1000A0C0842, 0x1000C0C0842,
        0x2000E0C0842, 0x200120C0842, 0x300160C0842, 0x3001E0C0842, 0x400260C0842, 0x400360C0842,
        0x1000A0E1842, 0x1000C0E1842, 0x2000E0E1842, 0x200120E1842, 0x300160E1842, 0x3001E0E1842,
        0x400260E1842, 0x400360E1842, 0x1000A185842, 0x1000C185842, 0x2000E185842, 0x20012185842,
        0x30016185842, 0x3001E185842, 0x40026185842, 0x40036185842, 0x50046060082, 0x50066060082,
        0x60086060082, 0x700C6060082, 0x80146060082, 0x90246060082, 0xA0446060082, 0x180846060082,
        0x500460700C2, 0x500660700C2, 0x600860700C2, 0x700C60700C2, 0x801460700C2, 0x902460700C2,
        0xA04460700C2, 0x1808460700C2, 0x50046080142, 0x50066080142, 0x60086080142, 0x700C6080142,
        0x80146080142, 0x90246080142, 0xA0446080142, 0x180846080142, 0x50046090242, 0x50066090242,
        0x60086090242, 0x700C6090242, 0x80146090242, 0x90246090242, 0xA0446090242, 0x180846090242,
        0x500460A0442, 0x500660A0442, 0x600860A0442, 0x700C60A0442, 0x801460A0442, 0x902460A0442,
        0xA04460A0442, 0x1808460A0442, 0x500460C0842, 0x500660C0842, 0x600860C0842, 0x700C60C0842,
        0x801460C0842, 0x902460C0842, 0xA04460C0842, 0x1808460C0842, 0x500460E1842, 0x500660E1842,
        0x600860E1842, 0x700C60E1842, 0x801460E1842, 0x902460E1842, 0xA04460E1842, 0x1808460E1842,
        0x50046185842, 0x50066185842, 0x60086185842, 0x700C6185842, 0x80146185842, 0x90246185842,
        0xA0446185842, 0x180846185842,
    ];

    /// <summary>Returns the extra bits of the insert and copy lengths of <paramref name="cmd"/> (StoreCommandExtra).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetCommandExtra(in Command cmd, out nuint numBits)
    {
        ulong entry = CommandExtraLut[cmd.CmdPrefix];
        int insnumextra = (int)(entry >> 16) & 0xFF;
        ulong insextraval = cmd.InsertLen - (uint)(entry & 0xFFFF);
        ulong copyextraval = cmd.CopyLenCode() - (uint)((entry >> 24) & 0xFFFF);
        numBits = (nuint)insnumextra + (nuint)((entry >> 40) & 0xFF);
        Debug.Assert(insnumextra == (int)Command.GetInsertExtra(Command.GetInsertLengthCode(cmd.InsertLen)));
        Debug.Assert((entry >> 40) == Command.GetCopyExtra(Command.GetCopyLengthCode(cmd.CopyLenCode())));
        Debug.Assert((uint)(entry & 0xFFFF) == Command.GetInsertBase(Command.GetInsertLengthCode(cmd.InsertLen)));
        Debug.Assert((uint)((entry >> 24) & 0xFFFF) == Command.GetCopyBase(Command.GetCopyLengthCode(cmd.CopyLenCode())));
        return (copyextraval << insnumextra) | insextraval;
    }

    /// <summary>
    /// Same as <see cref="BitWriter.WriteBits"/>, but the partially written byte at <c>storage[ix &gt;&gt; 3]</c> is kept in
    /// <paramref name="acc"/> instead of being reloaded from <paramref name="storage"/>, which avoids a store-to-load
    /// dependency between consecutive writes. The stored bytes are identical. <paramref name="acc"/> must be
    /// (re)initialized to <c>storage[ix &gt;&gt; 3]</c> whenever <paramref name="ix"/> was advanced by other means.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PutBits(nuint nBits, ulong bits, ref ulong acc, ref nuint ix, Span<byte> storage)
    {
        Debug.Assert((bits >> (int)nBits) == 0);
        Debug.Assert(nBits <= 56);
        nuint pos = ix;
        ulong v = acc | (bits << (int)(pos & 7));
        BinaryPrimitives.WriteUInt64LittleEndian(storage.Slice((int)(pos >> 3), 8), v);
        nuint newPos = pos + nBits;
        // Drop the bytes that are complete now; the shift is at most 56 bits.
        acc = v >> (int)((newPos & ~(nuint)7) - (pos & ~(nuint)7));
        ix = newPos;
    }

    /// <summary>
    /// Returns the ring buffer bytes data[(pos + i) &amp; mask] for 0 &lt;= i &lt; length as at most two contiguous segments.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetRingBufferSegments(ReadOnlySpan<byte> data, nuint pos, nuint mask, nuint length,
        out ReadOnlySpan<byte> first, out ReadOnlySpan<byte> second)
    {
        nuint start = pos & mask;
        nuint firstLength = Math.Min(length, mask + 1 - start);
        first = data.Slice((int)start, (int)firstLength);
        second = data.Slice(0, (int)(length - firstLength));
    }

    /// <summary>Stores a number between 0 and 255.</summary>
    private static void StoreVarLenUint8(nuint n, ref nuint storageIx, Span<byte> storage)
    {
        if (n == 0)
        {
            BitWriter.WriteBits(1, 0, ref storageIx, storage);
        }
        else
        {
            nuint nbits = FastLog.Log2FloorNonZero(n);
            BitWriter.WriteBits(1, 1, ref storageIx, storage);
            BitWriter.WriteBits(3, nbits, ref storageIx, storage);
            BitWriter.WriteBits(nbits, n - ((nuint)1 << (int)nbits), ref storageIx, storage);
        }
    }

    /// <summary>
    /// Stores the compressed meta-block header.
    /// REQUIRES: length > 0
    /// REQUIRES: length &lt;= (1 &lt;&lt; 24)
    /// </summary>
    private static void StoreCompressedMetaBlockHeader(bool isFinalBlock, nuint length, ref nuint storageIx, Span<byte> storage)
    {
        // Write ISLAST bit.
        BitWriter.WriteBits(1, isFinalBlock ? 1UL : 0UL, ref storageIx, storage);
        // Write ISEMPTY bit.
        if (isFinalBlock)
        {
            BitWriter.WriteBits(1, 0, ref storageIx, storage);
        }

        EncodeMlen(length, out ulong lenbits, out nuint nlenbits, out ulong nibblesbits);
        BitWriter.WriteBits(2, nibblesbits, ref storageIx, storage);
        BitWriter.WriteBits(nlenbits, lenbits, ref storageIx, storage);

        if (!isFinalBlock)
        {
            // Write ISUNCOMPRESSED bit.
            BitWriter.WriteBits(1, 0, ref storageIx, storage);
        }
    }

    /// <summary>
    /// Stores the uncompressed meta-block header.
    /// REQUIRES: length > 0
    /// REQUIRES: length &lt;= (1 &lt;&lt; 24)
    /// </summary>
    private static void StoreUncompressedMetaBlockHeader(nuint length, ref nuint storageIx, Span<byte> storage)
    {
        // Write ISLAST bit.
        // Uncompressed block cannot be the last one, so set to 0.
        BitWriter.WriteBits(1, 0, ref storageIx, storage);
        EncodeMlen(length, out ulong lenbits, out nuint nlenbits, out ulong nibblesbits);
        BitWriter.WriteBits(2, nibblesbits, ref storageIx, storage);
        BitWriter.WriteBits(nlenbits, lenbits, ref storageIx, storage);
        // Write ISUNCOMPRESSED bit.
        BitWriter.WriteBits(1, 1, ref storageIx, storage);
    }

    private static void StoreHuffmanTreeOfHuffmanTreeToBitMask(int numCodes, ReadOnlySpan<byte> codeLengthBitdepth,
        ref nuint storageIx, Span<byte> storage)
    {
        ReadOnlySpan<byte> storageOrder = [1, 2, 3, 4, 0, 5, 17, 6, 16, 7, 8, 9, 10, 11, 12, 13, 14, 15];
        // The bit lengths of the Huffman code over the code length alphabet
        // are compressed with the following static Huffman code:
        //   Symbol   Code
        //   ------   ----
        //   0          00
        //   1        1110
        //   2         110
        //   3          01
        //   4          10
        //   5        1111
        ReadOnlySpan<byte> huffmanBitLengthHuffmanCodeSymbols = [0, 7, 3, 2, 1, 15];
        ReadOnlySpan<byte> huffmanBitLengthHuffmanCodeBitLengths = [2, 4, 3, 2, 2, 4];

        nuint skipSome = 0;  // skips none.

        // Throw away trailing zeros:
        nuint codesToStore = BrotliConstants.CodeLengthCodes;
        if (numCodes > 1)
        {
            for (; codesToStore > 0; --codesToStore)
            {
                if (codeLengthBitdepth[storageOrder[(int)codesToStore - 1]] != 0)
                {
                    break;
                }
            }
        }
        if (codeLengthBitdepth[storageOrder[0]] == 0 &&
            codeLengthBitdepth[storageOrder[1]] == 0)
        {
            skipSome = 2;  // skips two.
            if (codeLengthBitdepth[storageOrder[2]] == 0)
            {
                skipSome = 3;  // skips three.
            }
        }
        BitWriter.WriteBits(2, skipSome, ref storageIx, storage);
        for (nuint i = skipSome; i < codesToStore; ++i)
        {
            int l = codeLengthBitdepth[storageOrder[(int)i]];
            BitWriter.WriteBits(huffmanBitLengthHuffmanCodeBitLengths[l],
                huffmanBitLengthHuffmanCodeSymbols[l], ref storageIx, storage);
        }
    }

    private static void StoreHuffmanTreeToBitMask(
        nuint huffmanTreeSize, ReadOnlySpan<byte> huffmanTree,
        ReadOnlySpan<byte> huffmanTreeExtraBits, ReadOnlySpan<byte> codeLengthBitdepth,
        ReadOnlySpan<ushort> codeLengthBitdepthSymbols,
        ref nuint storageIx, Span<byte> storage)
    {
        for (nuint i = 0; i < huffmanTreeSize; ++i)
        {
            int ix = huffmanTree[(int)i];
            BitWriter.WriteBits(codeLengthBitdepth[ix], codeLengthBitdepthSymbols[ix], ref storageIx, storage);
            // Extra bits
            switch (ix)
            {
                case BrotliConstants.RepeatPreviousCodeLength:
                    BitWriter.WriteBits(2, huffmanTreeExtraBits[(int)i], ref storageIx, storage);
                    break;
                case BrotliConstants.RepeatZeroCodeLength:
                    BitWriter.WriteBits(3, huffmanTreeExtraBits[(int)i], ref storageIx, storage);
                    break;
            }
        }
    }

    private static void StoreSimpleHuffmanTree(ReadOnlySpan<byte> depths, Span<nuint> symbols,
        nuint numSymbols, nuint maxBits, ref nuint storageIx, Span<byte> storage)
    {
        // value of 1 indicates a simple Huffman code
        BitWriter.WriteBits(2, 1, ref storageIx, storage);
        BitWriter.WriteBits(2, numSymbols - 1, ref storageIx, storage);  // NSYM - 1

        {
            // Sort
            for (int i = 0; i < (int)numSymbols; i++)
            {
                for (int j = i + 1; j < (int)numSymbols; j++)
                {
                    if (depths[(int)symbols[j]] < depths[(int)symbols[i]])
                    {
                        (symbols[j], symbols[i]) = (symbols[i], symbols[j]);
                    }
                }
            }
        }

        if (numSymbols == 2)
        {
            BitWriter.WriteBits(maxBits, symbols[0], ref storageIx, storage);
            BitWriter.WriteBits(maxBits, symbols[1], ref storageIx, storage);
        }
        else if (numSymbols == 3)
        {
            BitWriter.WriteBits(maxBits, symbols[0], ref storageIx, storage);
            BitWriter.WriteBits(maxBits, symbols[1], ref storageIx, storage);
            BitWriter.WriteBits(maxBits, symbols[2], ref storageIx, storage);
        }
        else
        {
            BitWriter.WriteBits(maxBits, symbols[0], ref storageIx, storage);
            BitWriter.WriteBits(maxBits, symbols[1], ref storageIx, storage);
            BitWriter.WriteBits(maxBits, symbols[2], ref storageIx, storage);
            BitWriter.WriteBits(maxBits, symbols[3], ref storageIx, storage);
            // tree-select
            BitWriter.WriteBits(1, depths[(int)symbols[0]] == 1 ? 1UL : 0UL, ref storageIx, storage);
        }
    }

    /// <summary>
    /// Write the Huffman tree into the brotli-representation.
    /// </summary>
    /// <remarks>
    /// <paramref name="depths"/>: Symbol depths.<br/>
    /// <paramref name="num"/>: Alphabet size.<br/>
    /// </remarks>
    public static void StoreHuffmanTree(ReadOnlySpan<byte> depths, nuint num, Span<HuffmanTree> tree,
        ref nuint storageIx, Span<byte> storage)
    {
        // Write the Huffman tree into the brotli-representation.
        // The command alphabet is the largest, so this allocation will fit all
        // alphabets.
        Span<byte> huffmanTree = stackalloc byte[BrotliConstants.NumCommandSymbols];
        Span<byte> huffmanTreeExtraBits = stackalloc byte[BrotliConstants.NumCommandSymbols];
        nuint huffmanTreeSize = 0;
        Span<byte> codeLengthBitdepth = stackalloc byte[BrotliConstants.CodeLengthCodes];
        Span<ushort> codeLengthBitdepthSymbols = stackalloc ushort[BrotliConstants.CodeLengthCodes];
        Span<uint> huffmanTreeHistogram = stackalloc uint[BrotliConstants.CodeLengthCodes];
        codeLengthBitdepth.Clear();
        codeLengthBitdepthSymbols.Clear();
        huffmanTreeHistogram.Clear();
        nuint i;
        int numCodes = 0;
        nuint code = 0;

        Debug.Assert(num <= BrotliConstants.NumCommandSymbols);

        EntropyEncode.WriteHuffmanTree(depths, num, ref huffmanTreeSize, huffmanTree, huffmanTreeExtraBits);

        // Calculate the statistics of the Huffman tree in brotli-representation.
        for (i = 0; i < huffmanTreeSize; ++i)
        {
            ++huffmanTreeHistogram[huffmanTree[(int)i]];
        }

        for (i = 0; i < BrotliConstants.CodeLengthCodes; ++i)
        {
            if (huffmanTreeHistogram[(int)i] != 0)
            {
                if (numCodes == 0)
                {
                    code = i;
                    numCodes = 1;
                }
                else if (numCodes == 1)
                {
                    numCodes = 2;
                    break;
                }
            }
        }

        // Calculate another Huffman tree to use for compressing both the
        // earlier Huffman tree with.
        EntropyEncode.CreateHuffmanTree(huffmanTreeHistogram, BrotliConstants.CodeLengthCodes,
                                        5, tree, codeLengthBitdepth);
        EntropyEncode.ConvertBitDepthsToSymbols(codeLengthBitdepth,
                                                BrotliConstants.CodeLengthCodes,
                                                codeLengthBitdepthSymbols);

        // Now, we have all the data, let's start storing it
        StoreHuffmanTreeOfHuffmanTreeToBitMask(numCodes, codeLengthBitdepth, ref storageIx, storage);

        if (numCodes == 1)
        {
            codeLengthBitdepth[(int)code] = 0;
        }

        // Store the real Huffman tree now.
        StoreHuffmanTreeToBitMask(huffmanTreeSize,
                                  huffmanTree,
                                  huffmanTreeExtraBits,
                                  codeLengthBitdepth,
                                  codeLengthBitdepthSymbols,
                                  ref storageIx, storage);
    }

    /// <summary>
    /// Builds a Huffman tree from histogram[0:length] into depth[0:length] and
    /// bits[0:length] and stores the encoded tree to the bit stream.
    /// </summary>
    private static void BuildAndStoreHuffmanTree(ReadOnlySpan<uint> histogram, nuint histogramLength,
        nuint alphabetSize, Span<HuffmanTree> tree, Span<byte> depth, Span<ushort> bits,
        ref nuint storageIx, Span<byte> storage)
    {
        nuint count = 0;
        Span<nuint> s4 = stackalloc nuint[4];
        s4.Clear();
        nuint i;
        nuint maxBits = 0;
        for (i = 0; i < histogramLength; i++)
        {
            if (histogram[(int)i] != 0)
            {
                if (count < 4)
                {
                    s4[(int)count] = i;
                }
                else if (count > 4)
                {
                    break;
                }
                count++;
            }
        }

        maxBits += (nuint)(64 - ulong.LeadingZeroCount((ulong)(alphabetSize - 1)));

        if (count <= 1)
        {
            BitWriter.WriteBits(4, 1, ref storageIx, storage);
            BitWriter.WriteBits(maxBits, s4[0], ref storageIx, storage);
            depth[(int)s4[0]] = 0;
            bits[(int)s4[0]] = 0;
            return;
        }

        depth.Slice(0, (int)histogramLength).Clear();
        EntropyEncode.CreateHuffmanTree(histogram, histogramLength, 15, tree, depth);
        EntropyEncode.ConvertBitDepthsToSymbols(depth, histogramLength, bits);

        if (count <= 4)
        {
            StoreSimpleHuffmanTree(depth, s4, count, maxBits, ref storageIx, storage);
        }
        else
        {
            StoreHuffmanTree(depth, histogramLength, tree, ref storageIx, storage);
        }
    }

    private struct SortHuffmanTreeComparator : IHuffmanTreeComparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Less(in HuffmanTree v0, in HuffmanTree v1) => v0.TotalCount < v1.TotalCount;
    }

    public static void BuildAndStoreHuffmanTreeFast(Span<HuffmanTree> tree, ReadOnlySpan<uint> histogram,
        nuint histogramTotal, nuint maxBits, Span<byte> depth, Span<ushort> bits,
        ref nuint storageIx, Span<byte> storage)
    {
        nuint count = 0;
        Span<nuint> symbols = stackalloc nuint[4];
        symbols.Clear();
        nuint length = 0;
        nuint total = histogramTotal;
        while (total != 0)
        {
            uint h = histogram[(int)length];
            if (h != 0)
            {
                if (count < 4)
                {
                    symbols[(int)count] = length;
                }
                ++count;
                total -= h;
            }
            ++length;
        }

        if (count <= 1)
        {
            BitWriter.WriteBits(4, 1, ref storageIx, storage);
            BitWriter.WriteBits(maxBits, symbols[0], ref storageIx, storage);
            depth[(int)symbols[0]] = 0;
            bits[(int)symbols[0]] = 0;
            return;
        }

        depth.Slice(0, (int)length).Clear();
        {
            for (uint countLimit = 1; ; countLimit *= 2)
            {
                int node = 0;
                for (nuint l = length; l != 0;)
                {
                    --l;
                    uint h = histogram[(int)l];
                    if (h != 0)
                    {
                        if (h >= countLimit)
                        {
                            tree[node] = new HuffmanTree(h, -1, (short)l);
                        }
                        else
                        {
                            tree[node] = new HuffmanTree(countLimit, -1, (short)l);
                        }
                        ++node;
                    }
                }
                {
                    int n = node;
                    HuffmanTree sentinel;
                    int i = 0;      // Points to the next leaf node.
                    int j = n + 1;  // Points to the next non-leaf node.
                    int k;

                    EntropyEncode.SortHuffmanTreeItems<SortHuffmanTreeComparator>(tree, (nuint)n);
                    // The nodes are:
                    // [0, n): the sorted leaf nodes that we start with.
                    // [n]: we add a sentinel here.
                    // [n + 1, 2n): new parent nodes are added here, starting from
                    //              (n+1). These are naturally in ascending order.
                    // [2n]: we add a sentinel at the end as well.
                    // There will be (2n+1) elements at the end.
                    sentinel = new HuffmanTree(uint.MaxValue, -1, -1);
                    tree[node++] = sentinel;
                    tree[node++] = sentinel;

                    for (k = n - 1; k > 0; --k)
                    {
                        int left, right;
                        if (tree[i].TotalCount <= tree[j].TotalCount)
                        {
                            left = i;
                            ++i;
                        }
                        else
                        {
                            left = j;
                            ++j;
                        }
                        if (tree[i].TotalCount <= tree[j].TotalCount)
                        {
                            right = i;
                            ++i;
                        }
                        else
                        {
                            right = j;
                            ++j;
                        }
                        // The sentinel node becomes the parent node.
                        ref HuffmanTree parent = ref tree[node - 1];
                        parent.TotalCount = tree[left].TotalCount + tree[right].TotalCount;
                        parent.IndexLeft = (short)left;
                        parent.IndexRightOrValue = (short)right;
                        // Add back the last sentinel node.
                        tree[node++] = sentinel;
                    }
                    if (EntropyEncode.SetDepth(2 * n - 1, tree, depth, 14))
                    {
                        // We need to pack the Huffman tree in 14 bits. If this was not
                        // successful, add fake entities to the lowest values and retry.
                        break;
                    }
                }
            }
        }
        EntropyEncode.ConvertBitDepthsToSymbols(depth, length, bits);
        if (count <= 4)
        {
            // value of 1 indicates a simple Huffman code
            BitWriter.WriteBits(2, 1, ref storageIx, storage);
            BitWriter.WriteBits(2, count - 1, ref storageIx, storage);  // NSYM - 1

            // Sort
            for (int i = 0; i < (int)count; i++)
            {
                for (int j = i + 1; j < (int)count; j++)
                {
                    if (depth[(int)symbols[j]] < depth[(int)symbols[i]])
                    {
                        (symbols[j], symbols[i]) = (symbols[i], symbols[j]);
                    }
                }
            }

            if (count == 2)
            {
                BitWriter.WriteBits(maxBits, symbols[0], ref storageIx, storage);
                BitWriter.WriteBits(maxBits, symbols[1], ref storageIx, storage);
            }
            else if (count == 3)
            {
                BitWriter.WriteBits(maxBits, symbols[0], ref storageIx, storage);
                BitWriter.WriteBits(maxBits, symbols[1], ref storageIx, storage);
                BitWriter.WriteBits(maxBits, symbols[2], ref storageIx, storage);
            }
            else
            {
                BitWriter.WriteBits(maxBits, symbols[0], ref storageIx, storage);
                BitWriter.WriteBits(maxBits, symbols[1], ref storageIx, storage);
                BitWriter.WriteBits(maxBits, symbols[2], ref storageIx, storage);
                BitWriter.WriteBits(maxBits, symbols[3], ref storageIx, storage);
                // tree-select
                BitWriter.WriteBits(1, depth[(int)symbols[0]] == 1 ? 1UL : 0UL, ref storageIx, storage);
            }
        }
        else
        {
            byte previousValue = 8;
            ReadOnlySpan<byte> codeLengthDepth = EntropyEncodeStatic.CodeLengthDepth;
            ReadOnlySpan<uint> codeLengthBits = EntropyEncodeStatic.CodeLengthBits;
            // Complex Huffman Tree
            EntropyEncodeStatic.StoreStaticCodeLengthCode(ref storageIx, storage);

            // Actual RLE coding.
            for (nuint i = 0; i < length;)
            {
                byte value = depth[(int)i];
                nuint reps = 1;
                for (nuint k = i + 1; k < length && depth[(int)k] == value; ++k)
                {
                    ++reps;
                }
                i += reps;
                if (value == 0)
                {
                    BitWriter.WriteBits(EntropyEncodeStatic.ZeroRepsDepth[(int)reps], EntropyEncodeStatic.ZeroRepsBits[(int)reps],
                                        ref storageIx, storage);
                }
                else
                {
                    if (previousValue != value)
                    {
                        BitWriter.WriteBits(codeLengthDepth[value], codeLengthBits[value],
                                            ref storageIx, storage);
                        --reps;
                    }
                    if (reps < 3)
                    {
                        while (reps != 0)
                        {
                            reps--;
                            BitWriter.WriteBits(codeLengthDepth[value], codeLengthBits[value],
                                                ref storageIx, storage);
                        }
                    }
                    else
                    {
                        reps -= 3;
                        BitWriter.WriteBits(EntropyEncodeStatic.NonZeroRepsDepth[(int)reps], EntropyEncodeStatic.NonZeroRepsBits[(int)reps],
                                            ref storageIx, storage);
                    }
                    previousValue = value;
                }
            }
        }
    }

    private static nuint IndexOf(ReadOnlySpan<byte> v, nuint vSize, byte value)
    {
        nuint i = 0;
        for (; i < vSize; ++i)
        {
            if (v[(int)i] == value)
            {
                return i;
            }
        }
        return i;
    }

    private static void MoveToFront(Span<byte> v, nuint index)
    {
        byte value = v[(int)index];
        for (nuint i = index; i != 0; --i)
        {
            v[(int)i] = v[(int)i - 1];
        }
        v[0] = value;
    }

    private static void MoveToFrontTransform(ReadOnlySpan<uint> vIn, nuint vSize, Span<uint> vOut)
    {
        nuint i;
        uint maxValue;
        if (vSize == 0)
        {
            return;
        }
        Span<byte> mtf = stackalloc byte[256];
        maxValue = vIn[0];
        for (i = 1; i < vSize; ++i)
        {
            if (vIn[(int)i] > maxValue)
            {
                maxValue = vIn[(int)i];
            }
        }
        Debug.Assert(maxValue < 256u);
        for (i = 0; i <= maxValue; ++i)
        {
            mtf[(int)i] = (byte)i;
        }
        {
            nuint mtfSize = (nuint)maxValue + 1;
            for (i = 0; i < vSize; ++i)
            {
                nuint index = IndexOf(mtf, mtfSize, (byte)vIn[(int)i]);
                Debug.Assert(index < mtfSize);
                vOut[(int)i] = (uint)index;
                MoveToFront(mtf, index);
            }
        }
    }

    /// <summary>
    /// Finds runs of zeros in v[0..in_size) and replaces them with a prefix code of
    /// the run length plus extra bits (lower 9 bits is the prefix code and the rest
    /// are the extra bits). Non-zero values in v[] are shifted by
    /// *max_length_prefix. Will not create prefix codes bigger than the initial
    /// value of *max_run_length_prefix. The prefix code of run length L is simply
    /// Log2Floor(L) and the number of extra bits is the same as the prefix code.
    /// </summary>
    private static void RunLengthCodeZeros(nuint inSize, Span<uint> v, out nuint outSize, ref uint maxRunLengthPrefix)
    {
        uint maxReps = 0;
        nuint i;
        uint maxPrefix;
        for (i = 0; i < inSize;)
        {
            uint reps = 0;
            for (; i < inSize && v[(int)i] != 0; ++i)
            {
            }
            for (; i < inSize && v[(int)i] == 0; ++i)
            {
                ++reps;
            }
            maxReps = Math.Max(reps, maxReps);
        }
        maxPrefix = maxReps > 0 ? FastLog.Log2FloorNonZero(maxReps) : 0;
        maxPrefix = Math.Min(maxPrefix, maxRunLengthPrefix);
        maxRunLengthPrefix = maxPrefix;
        nuint o = 0;
        for (i = 0; i < inSize;)
        {
            Debug.Assert(o <= i);
            if (v[(int)i] != 0)
            {
                v[(int)o] = v[(int)i] + maxRunLengthPrefix;
                ++i;
                ++o;
            }
            else
            {
                uint reps = 1;
                for (nuint k = i + 1; k < inSize && v[(int)k] == 0; ++k)
                {
                    ++reps;
                }
                i += reps;
                while (reps != 0)
                {
                    if (reps < (2u << (int)maxPrefix))
                    {
                        uint runLengthPrefix = FastLog.Log2FloorNonZero(reps);
                        uint extraBits = reps - (1u << (int)runLengthPrefix);
                        v[(int)o] = runLengthPrefix + (extraBits << 9);
                        ++o;
                        break;
                    }
                    else
                    {
                        uint extraBits = (1u << (int)maxPrefix) - 1u;
                        v[(int)o] = maxPrefix + (extraBits << 9);
                        reps -= (2u << (int)maxPrefix) - 1u;
                        ++o;
                    }
                }
            }
        }
        outSize = o;
    }

    private static void EncodeContextMap(EncodeContextMapArena arena, ReadOnlySpan<uint> contextMap,
        nuint contextMapSize, nuint numClusters, Span<HuffmanTree> tree,
        ref nuint storageIx, Span<byte> storage)
    {
        nuint i;
        uint maxRunLengthPrefix = 6;
        const uint SymbolMask = (1u << SymbolBits) - 1u;
        uint[] histogram = arena.Histogram;
        byte[] depths = arena.Depths;
        ushort[] bits = arena.Bits;

        StoreVarLenUint8(numClusters - 1, ref storageIx, storage);

        if (numClusters == 1)
        {
            return;
        }

        if ((nuint)arena.RleSymbols.Length < contextMapSize)
        {
            arena.RleSymbols = new uint[contextMapSize];
        }
        uint[] rleSymbols = arena.RleSymbols;
        MoveToFrontTransform(contextMap, contextMapSize, rleSymbols);
        RunLengthCodeZeros(contextMapSize, rleSymbols, out nuint numRleSymbols, ref maxRunLengthPrefix);
        Array.Clear(histogram);
        for (i = 0; i < numRleSymbols; ++i)
        {
            ++histogram[rleSymbols[i] & SymbolMask];
        }
        {
            bool useRle = maxRunLengthPrefix > 0;
            BitWriter.WriteBits(1, useRle ? 1UL : 0UL, ref storageIx, storage);
            if (useRle)
            {
                BitWriter.WriteBits(4, maxRunLengthPrefix - 1, ref storageIx, storage);
            }
        }
        BuildAndStoreHuffmanTree(histogram, numClusters + maxRunLengthPrefix,
                                 numClusters + maxRunLengthPrefix,
                                 tree, depths, bits, ref storageIx, storage);
        for (i = 0; i < numRleSymbols; ++i)
        {
            uint rleSymbol = rleSymbols[i] & SymbolMask;
            uint extraBitsVal = rleSymbols[i] >> SymbolBits;
            BitWriter.WriteBits(depths[rleSymbol], bits[rleSymbol], ref storageIx, storage);
            if (rleSymbol > 0 && rleSymbol <= maxRunLengthPrefix)
            {
                BitWriter.WriteBits(rleSymbol, extraBitsVal, ref storageIx, storage);
            }
        }
        BitWriter.WriteBits(1, 1, ref storageIx, storage);  // use move-to-front
    }

    /// <summary>Stores the block switch command with index block_ix to the bit stream.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreBlockSwitch(BlockSplitCode code, uint blockLen, byte blockType, bool isFirstBlock,
        ref nuint storageIx, Span<byte> storage)
    {
        nuint typecode = NextBlockTypeCode(ref code.TypeCodeCalculator, blockType);
        if (!isFirstBlock)
        {
            BitWriter.WriteBits(code.TypeDepths[typecode], code.TypeBits[typecode], ref storageIx, storage);
        }
        GetBlockLengthPrefixCode(blockLen, out nuint lencode, out uint lenNextra, out uint lenExtra);

        BitWriter.WriteBits(code.LengthDepths[lencode], code.LengthBits[lencode], ref storageIx, storage);
        BitWriter.WriteBits(lenNextra, lenExtra, ref storageIx, storage);
    }

    /// <summary>
    /// Builds a BlockSplitCode data structure from the block split given by the
    /// vector of block types and block lengths and stores it to the bit stream.
    /// </summary>
    private static void BuildAndStoreBlockSplitCode(ReadOnlySpan<byte> types, ReadOnlySpan<uint> lengths,
        nuint numBlocks, nuint numTypes, Span<HuffmanTree> tree, BlockSplitCode code,
        ref nuint storageIx, Span<byte> storage)
    {
        Span<uint> typeHisto = stackalloc uint[BrotliConstants.MaxBlockTypeSymbols];
        Span<uint> lengthHisto = stackalloc uint[BrotliConstants.NumBlockLenSymbols];
        BlockTypeCodeCalculator typeCodeCalculator = new();
        typeHisto.Slice(0, (int)(numTypes + 2)).Clear();
        lengthHisto.Clear();
        for (nuint i = 0; i < numBlocks; ++i)
        {
            nuint typeCode = NextBlockTypeCode(ref typeCodeCalculator, types[(int)i]);
            if (i != 0)
            {
                ++typeHisto[(int)typeCode];
            }
            ++lengthHisto[(int)BlockLengthPrefixCode(lengths[(int)i])];
        }
        StoreVarLenUint8(numTypes - 1, ref storageIx, storage);
        if (numTypes > 1)
        {
            // TODO(eustas): else? could StoreBlockSwitch occur?
            BuildAndStoreHuffmanTree(typeHisto, numTypes + 2, numTypes + 2, tree,
                                     code.TypeDepths, code.TypeBits,
                                     ref storageIx, storage);
            BuildAndStoreHuffmanTree(lengthHisto, BrotliConstants.NumBlockLenSymbols,
                                     BrotliConstants.NumBlockLenSymbols,
                                     tree, code.LengthDepths,
                                     code.LengthBits, ref storageIx, storage);
            StoreBlockSwitch(code, lengths[0], types[0], true, ref storageIx, storage);
        }
    }

    /// <summary>Stores a context map where the histogram type is always the block type.</summary>
    private static void StoreTrivialContextMap(EncodeContextMapArena arena, nuint numTypes, nuint contextBits,
        Span<HuffmanTree> tree, ref nuint storageIx, Span<byte> storage)
    {
        StoreVarLenUint8(numTypes - 1, ref storageIx, storage);
        if (numTypes > 1)
        {
            nuint repeatCode = contextBits - 1u;
            nuint repeatBits = (1u << (int)repeatCode) - 1u;
            nuint alphabetSize = numTypes + repeatCode;
            uint[] histogram = arena.Histogram;
            byte[] depths = arena.Depths;
            ushort[] bits = arena.Bits;
            histogram.AsSpan(0, (int)alphabetSize).Clear();
            // Write RLEMAX.
            BitWriter.WriteBits(1, 1, ref storageIx, storage);
            BitWriter.WriteBits(4, repeatCode - 1, ref storageIx, storage);
            histogram[repeatCode] = (uint)numTypes;
            histogram[0] = 1;
            for (nuint i = contextBits; i < alphabetSize; ++i)
            {
                histogram[i] = 1;
            }
            BuildAndStoreHuffmanTree(histogram, alphabetSize, alphabetSize,
                                     tree, depths, bits, ref storageIx, storage);
            for (nuint i = 0; i < numTypes; ++i)
            {
                nuint code = (i == 0 ? 0 : i + contextBits - 1);
                BitWriter.WriteBits(depths[code], bits[code], ref storageIx, storage);
                BitWriter.WriteBits(depths[repeatCode], bits[repeatCode], ref storageIx, storage);
                BitWriter.WriteBits(repeatCode, repeatBits, ref storageIx, storage);
            }
            // Write IMTF (inverse-move-to-front) bit.
            BitWriter.WriteBits(1, 1, ref storageIx, storage);
        }
    }

    /// <summary>
    /// Creates entropy codes of block lengths and block types and stores them
    /// to the bit stream.
    /// </summary>
    private static void BuildAndStoreBlockSwitchEntropyCodes(BlockEncoder self, Span<HuffmanTree> tree,
        ref nuint storageIx, Span<byte> storage)
    {
        BuildAndStoreBlockSplitCode(self.BlockTypes, self.BlockLengths,
            self.NumBlocks, self.NumBlockTypes, tree, self.BlockSplitCode,
            ref storageIx, storage);
    }

    /// <summary>
    /// Stores the next symbol with the entropy code of the current block type.
    /// Updates the block type and block length at block boundaries.
    /// </summary>
    /// <remarks>
    /// <paramref name="storageIx"/> and <paramref name="acc"/> (see <see cref="PutBits"/>) should be locals of the
    /// caller (they are never passed by reference to a non-inlined method), so that the JIT can keep them in registers.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreSymbol(BlockEncoder self, nuint symbol, ref ulong acc, ref nuint storageIx, Span<byte> storage)
    {
        if (self.BlockLen == 0)
        {
            storageIx = StartNextBlock(self, storageIx, storage);
            acc = storage[(int)(storageIx >> 3)];
            self.EntropyIx = self.BlockTypes[(int)self.BlockIx] * self.HistogramLength;
        }
        --self.BlockLen;
        {
            int ix = (int)(self.EntropyIx + symbol);
            PutBits(self.Depths[ix], self.Bits[ix], ref acc, ref storageIx, storage);
        }
    }

    /// <summary>
    /// Stores the next symbol with the entropy code of the current block type and
    /// context value.
    /// Updates the block type and block length at block boundaries.
    /// </summary>
    /// <remarks>See <see cref="StoreSymbol"/> regarding <paramref name="storageIx"/> and <paramref name="acc"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreSymbolWithContext(BlockEncoder self, nuint symbol, nuint context, ReadOnlySpan<uint> contextMap,
        ref ulong acc, ref nuint storageIx, Span<byte> storage, int contextBits)
    {
        if (self.BlockLen == 0)
        {
            storageIx = StartNextBlock(self, storageIx, storage);
            acc = storage[(int)(storageIx >> 3)];
            self.EntropyIx = (nuint)self.BlockTypes[(int)self.BlockIx] << contextBits;
        }
        --self.BlockLen;
        {
            nuint histoIx = contextMap[(int)(self.EntropyIx + context)];
            int ix = (int)(histoIx * self.HistogramLength + symbol);
            PutBits(self.Depths[ix], self.Bits[ix], ref acc, ref storageIx, storage);
        }
    }

    /// <summary>
    /// Stores the literals of <paramref name="literals"/> with the literal block encoder (StoreSymbol for each literal).
    /// The literals up to the next block switch are written in a tight loop.
    /// Not inlined, so that the register allocation of the hot loop does not depend on the (large) callers.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreLiterals(BlockEncoder self, ReadOnlySpan<byte> literals, ref ulong acc, ref nuint storageIx,
        Span<byte> storage)
    {
        Debug.Assert(self.HistogramLength == BrotliConstants.NumLiteralSymbols);
        nuint ix = storageIx;
        ulong a = acc;
        while (!literals.IsEmpty)
        {
            if (self.BlockLen == 0)
            {
                ix = StartNextBlock(self, ix, storage);
                a = storage[(int)(ix >> 3)];
                self.EntropyIx = self.BlockTypes[(int)self.BlockIx] * self.HistogramLength;
            }
            int run = (int)Math.Min((nuint)literals.Length, self.BlockLen);
            self.BlockLen -= (nuint)run;
            ReadOnlySpan<byte> depths = self.Depths.AsSpan((int)self.EntropyIx, BrotliConstants.NumLiteralSymbols);
            ReadOnlySpan<ushort> bits = self.Bits.AsSpan((int)self.EntropyIx, BrotliConstants.NumLiteralSymbols);
            foreach (byte literal in literals.Slice(0, run))
            {
                PutBits(depths[literal], bits[literal], ref a, ref ix, storage);
            }
            literals = literals.Slice(run);
        }
        acc = a;
        storageIx = ix;
    }

    /// <summary>
    /// Stores the literals of <paramref name="literals"/> with the literal block encoder and the literal context map
    /// (StoreSymbolWithContext for each literal), updating the two previous bytes.
    /// The literals up to the next block switch are written in a tight loop.
    /// Not inlined, so that the register allocation of the hot loop does not depend on the (large) callers.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreLiteralsWithContext(BlockEncoder self, ReadOnlySpan<byte> literals, ref byte prevByte,
        ref byte prevByte2, ReadOnlySpan<byte> literalContextLut, ReadOnlySpan<uint> contextMap, ref ulong acc,
        ref nuint storageIx, Span<byte> storage)
    {
        Debug.Assert(self.HistogramLength == BrotliConstants.NumLiteralSymbols);
        const int NumContexts = 1 << BrotliConstants.LiteralContextBits;
        ReadOnlySpan<byte> lut1 = literalContextLut.Slice(0, 256);
        ReadOnlySpan<byte> lut2 = literalContextLut.Slice(256, 256);
        byte[] depths = self.Depths;
        ushort[] bits = self.Bits;
        nuint ix = storageIx;
        ulong a = acc;
        byte p1 = prevByte;
        byte p2 = prevByte2;
        while (!literals.IsEmpty)
        {
            if (self.BlockLen == 0)
            {
                ix = StartNextBlock(self, ix, storage);
                a = storage[(int)(ix >> 3)];
                self.EntropyIx = (nuint)self.BlockTypes[(int)self.BlockIx] << BrotliConstants.LiteralContextBits;
            }
            int run = (int)Math.Min((nuint)literals.Length, self.BlockLen);
            self.BlockLen -= (nuint)run;
            ReadOnlySpan<uint> blockContextMap = contextMap.Slice((int)self.EntropyIx, NumContexts);
            foreach (byte literal in literals.Slice(0, run))
            {
                int context = lut1[p1] | lut2[p2];
                int i = (int)(blockContextMap[context] * BrotliConstants.NumLiteralSymbols) + literal;
                PutBits(depths[i], bits[i], ref a, ref ix, storage);
                p2 = p1;
                p1 = literal;
            }
            literals = literals.Slice(run);
        }
        prevByte = p1;
        prevByte2 = p2;
        acc = a;
        storageIx = ix;
    }

    /// <summary>
    /// Common part of StoreSymbol and StoreSymbolWithContext at block boundaries; returns the new storage_ix.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint StartNextBlock(BlockEncoder self, nuint storageIx, Span<byte> storage)
    {
        nuint blockIx = ++self.BlockIx;
        uint blockLen = self.BlockLengths[(int)blockIx];
        byte blockType = self.BlockTypes[(int)blockIx];
        self.BlockLen = blockLen;
        StoreBlockSwitch(self.BlockSplitCode, blockLen, blockType, false, ref storageIx, storage);
        return storageIx;
    }

    /// <summary>
    /// Creates entropy codes for all block types and stores them to the bit stream
    /// (FN(BuildAndStoreEntropyCodes) of block_encoder_inc.h).
    /// </summary>
    private static void BuildAndStoreEntropyCodes<THistogram>(BlockEncoder self, Span<THistogram> histograms,
        nuint histogramsSize, nuint alphabetSize, Span<HuffmanTree> tree,
        ref nuint storageIx, Span<byte> storage)
        where THistogram : struct, IHistogram<THistogram>
    {
        nuint tableSize = histogramsSize * self.HistogramLength;
        if ((nuint)self.Depths.Length < tableSize)
        {
            self.Depths = new byte[tableSize];
            self.Bits = new ushort[tableSize];
        }

        for (nuint i = 0; i < histogramsSize; ++i)
        {
            int ix = (int)(i * self.HistogramLength);
            BuildAndStoreHuffmanTree(histograms[(int)i].Data, self.HistogramLength,
                alphabetSize, tree, self.Depths.AsSpan(ix), self.Bits.AsSpan(ix),
                ref storageIx, storage);
        }
    }

    private static void JumpToByteBoundary(ref nuint storageIx, Span<byte> storage)
    {
        storageIx = (storageIx + 7u) & ~(nuint)7u;
        storage[(int)(storageIx >> 3)] = 0;
    }

    /// <summary>
    /// REQUIRES: length > 0
    /// REQUIRES: length &lt;= (1 &lt;&lt; 24)
    /// </summary>
    public static void StoreMetaBlock(ReadOnlySpan<byte> input, nuint startPos, nuint length, nuint mask,
        byte prevByte, byte prevByte2, bool isLast, in EncoderParams parameters, BrotliContextType literalContextMode,
        ReadOnlySpan<Command> commands, nuint nCommands, MetaBlockSplit mb, ref nuint storageIx, Span<byte> storage,
        MetablockArena? arena = null)
    {
        nuint pos = startPos;
        nuint i;
        uint numDistanceSymbols = parameters.Dist.AlphabetSizeMax;
        uint numEffectiveDistanceSymbols = parameters.Dist.AlphabetSizeLimit;
        ReadOnlySpan<byte> literalContextLut = BrotliContext.GetLut(literalContextMode);
        Debug.Assert(numEffectiveDistanceSymbols <= Histogram.NumHistogramDistanceSymbols);

        StoreCompressedMetaBlockHeader(isLast, length, ref storageIx, storage);

        arena ??= new MetablockArena();
        HuffmanTree[] tree = arena.Tree;
        StoreMetaBlockArena scratch = arena.StoreMetaBlock;
        EncodeContextMapArena contextMapArena = scratch.ContextMapArena;
        BlockEncoder literalEnc = scratch.LiteralEnc;
        BlockEncoder commandEnc = scratch.CommandEnc;
        BlockEncoder distanceEnc = scratch.DistanceEnc;
        literalEnc.Init(BrotliConstants.NumLiteralSymbols,
            mb.LiteralSplit.NumTypes, mb.LiteralSplit.Types,
            mb.LiteralSplit.Lengths, mb.LiteralSplit.NumBlocks);
        commandEnc.Init(BrotliConstants.NumCommandSymbols,
            mb.CommandSplit.NumTypes, mb.CommandSplit.Types,
            mb.CommandSplit.Lengths, mb.CommandSplit.NumBlocks);
        distanceEnc.Init(numEffectiveDistanceSymbols,
            mb.DistanceSplit.NumTypes, mb.DistanceSplit.Types,
            mb.DistanceSplit.Lengths, mb.DistanceSplit.NumBlocks);

        BuildAndStoreBlockSwitchEntropyCodes(literalEnc, tree, ref storageIx, storage);
        BuildAndStoreBlockSwitchEntropyCodes(commandEnc, tree, ref storageIx, storage);
        BuildAndStoreBlockSwitchEntropyCodes(distanceEnc, tree, ref storageIx, storage);

        BitWriter.WriteBits(2, parameters.Dist.DistancePostfixBits, ref storageIx, storage);
        BitWriter.WriteBits(4, parameters.Dist.NumDirectDistanceCodes >> (int)parameters.Dist.DistancePostfixBits,
            ref storageIx, storage);
        for (i = 0; i < mb.LiteralSplit.NumTypes; ++i)
        {
            BitWriter.WriteBits(2, (ulong)literalContextMode, ref storageIx, storage);
        }

        if (mb.LiteralContextMapSize == 0)
        {
            StoreTrivialContextMap(contextMapArena, mb.LiteralHistogramsSize,
                BrotliConstants.LiteralContextBits, tree, ref storageIx, storage);
        }
        else
        {
            EncodeContextMap(contextMapArena, mb.LiteralContextMap, mb.LiteralContextMapSize,
                mb.LiteralHistogramsSize, tree, ref storageIx, storage);
        }

        if (mb.DistanceContextMapSize == 0)
        {
            StoreTrivialContextMap(contextMapArena, mb.DistanceHistogramsSize,
                BrotliConstants.DistanceContextBits, tree, ref storageIx, storage);
        }
        else
        {
            EncodeContextMap(contextMapArena, mb.DistanceContextMap, mb.DistanceContextMapSize,
                mb.DistanceHistogramsSize, tree, ref storageIx, storage);
        }

        BuildAndStoreEntropyCodes<HistogramLiteral>(literalEnc, mb.LiteralHistograms,
            mb.LiteralHistogramsSize, BrotliConstants.NumLiteralSymbols, tree,
            ref storageIx, storage);
        BuildAndStoreEntropyCodes<HistogramCommand>(commandEnc, mb.CommandHistograms,
            mb.CommandHistogramsSize, BrotliConstants.NumCommandSymbols, tree,
            ref storageIx, storage);
        BuildAndStoreEntropyCodes<HistogramDistance>(distanceEnc, mb.DistanceHistograms,
            mb.DistanceHistogramsSize, numDistanceSymbols, tree,
            ref storageIx, storage);

        ReadOnlySpan<uint> literalContextMap = mb.LiteralContextMap;
        ReadOnlySpan<uint> distanceContextMap = mb.DistanceContextMap;
        bool trivialLiteralContextMap = mb.LiteralContextMapSize == 0;
        bool trivialDistanceContextMap = mb.DistanceContextMapSize == 0;
        // Local copies of storage_ix and of the partial byte (see PutBits), so that they can be kept in registers.
        nuint ix = storageIx;
        ulong acc = storage[(int)(ix >> 3)];
        commands = commands.Slice(0, (int)nCommands);
        for (int c = 0; c < commands.Length; ++c)
        {
            ref readonly Command cmd = ref commands[c];
            nuint cmdCode = cmd.CmdPrefix;
            StoreSymbol(commandEnc, cmdCode, ref acc, ref ix, storage);
            ulong extra = GetCommandExtra(cmd, out nuint extraBits);
            PutBits(extraBits, extra, ref acc, ref ix, storage);
            nuint insertLen = cmd.InsertLen;
            if (insertLen != 0)
            {
                GetRingBufferSegments(input, pos, mask, insertLen, out ReadOnlySpan<byte> first, out ReadOnlySpan<byte> second);
                if (trivialLiteralContextMap)
                {
                    StoreLiterals(literalEnc, first, ref acc, ref ix, storage);
                    if (!second.IsEmpty)
                    {
                        StoreLiterals(literalEnc, second, ref acc, ref ix, storage);
                    }
                }
                else
                {
                    StoreLiteralsWithContext(literalEnc, first, ref prevByte, ref prevByte2, literalContextLut,
                        literalContextMap, ref acc, ref ix, storage);
                    if (!second.IsEmpty)
                    {
                        StoreLiteralsWithContext(literalEnc, second, ref prevByte, ref prevByte2, literalContextLut,
                            literalContextMap, ref acc, ref ix, storage);
                    }
                }
                pos += insertLen;
            }
            uint copyLen = cmd.CopyLength;
            pos += copyLen;
            if (copyLen != 0)
            {
                prevByte2 = input[(int)((pos - 2) & mask)];
                prevByte = input[(int)((pos - 1) & mask)];
                if (cmd.CmdPrefix >= 128)
                {
                    nuint distCode = (nuint)(cmd.DistPrefix & 0x3FF);
                    uint distnumextra = (uint)cmd.DistPrefix >> 10;
                    ulong distextra = cmd.DistExtra;
                    if (trivialDistanceContextMap)
                    {
                        StoreSymbol(distanceEnc, distCode, ref acc, ref ix, storage);
                    }
                    else
                    {
                        nuint context = cmd.DistanceContext();
                        StoreSymbolWithContext(distanceEnc, distCode, context,
                            distanceContextMap, ref acc, ref ix, storage,
                            BrotliConstants.DistanceContextBits);
                    }
                    PutBits(distnumextra, distextra, ref acc, ref ix, storage);
                }
            }
        }
        storageIx = ix;
        if (isLast)
        {
            JumpToByteBoundary(ref storageIx, storage);
        }
    }

    /// <summary>Adds the bytes of <paramref name="literals"/> to the 256-entry <paramref name="histogram"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CountLiterals(ReadOnlySpan<byte> literals, Span<uint> histogram)
    {
        histogram = histogram.Slice(0, BrotliConstants.NumLiteralSymbols);
        foreach (byte literal in literals)
        {
            ++histogram[literal];
        }
    }

    // Not inlined, so that the register allocation of the hot loops does not depend on the (large) callers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BuildHistograms(ReadOnlySpan<byte> input, nuint startPos, nuint mask,
        ReadOnlySpan<Command> commands, nuint nCommands,
        ref HistogramLiteral litHisto, ref HistogramCommand cmdHisto, ref HistogramDistance distHisto)
    {
        nuint pos = startPos;
        Span<uint> litData = litHisto.Data;
        commands = commands.Slice(0, (int)nCommands);
        for (int i = 0; i < commands.Length; ++i)
        {
            ref readonly Command cmd = ref commands[i];
            Histogram.HistogramAdd(ref cmdHisto, cmd.CmdPrefix);
            nuint insertLen = cmd.InsertLen;
            if (insertLen != 0)
            {
                // Same as HistogramAdd for each literal.
                GetRingBufferSegments(input, pos, mask, insertLen, out ReadOnlySpan<byte> first, out ReadOnlySpan<byte> second);
                CountLiterals(first, litData);
                CountLiterals(second, litData);
                litHisto.TotalCount += insertLen;
                pos += insertLen;
            }
            uint copyLen = cmd.CopyLength;
            pos += copyLen;
            if (copyLen != 0 && cmd.CmdPrefix >= 128)
            {
                Histogram.HistogramAdd(ref distHisto, (nuint)(cmd.DistPrefix & 0x3FF));
            }
        }
    }

    // Not inlined, so that the register allocation of the hot loops does not depend on the (large) callers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreDataWithHuffmanCodes(ReadOnlySpan<byte> input, nuint startPos, nuint mask,
        ReadOnlySpan<Command> commands, nuint nCommands,
        ReadOnlySpan<byte> litDepth, ReadOnlySpan<ushort> litBits,
        ReadOnlySpan<byte> cmdDepth, ReadOnlySpan<ushort> cmdBits,
        ReadOnlySpan<byte> distDepth, ReadOnlySpan<ushort> distBits,
        ref nuint storageIx, Span<byte> storage)
    {
        nuint pos = startPos;
        // Local copies of storage_ix and of the partial byte (see PutBits), so that they can be kept in registers.
        nuint ix = storageIx;
        ulong acc = storage[(int)(ix >> 3)];
        litDepth = litDepth.Slice(0, BrotliConstants.NumLiteralSymbols);
        litBits = litBits.Slice(0, BrotliConstants.NumLiteralSymbols);
        commands = commands.Slice(0, (int)nCommands);
        for (int i = 0; i < commands.Length; ++i)
        {
            ref readonly Command cmd = ref commands[i];
            int cmdCode = cmd.CmdPrefix;
            PutBits(cmdDepth[cmdCode], cmdBits[cmdCode], ref acc, ref ix, storage);
            ulong extra = GetCommandExtra(cmd, out nuint extraBits);
            PutBits(extraBits, extra, ref acc, ref ix, storage);
            nuint insertLen = cmd.InsertLen;
            if (insertLen != 0)
            {
                GetRingBufferSegments(input, pos, mask, insertLen, out ReadOnlySpan<byte> first, out ReadOnlySpan<byte> second);
                foreach (byte literal in first)
                {
                    PutBits(litDepth[literal], litBits[literal], ref acc, ref ix, storage);
                }
                foreach (byte literal in second)
                {
                    PutBits(litDepth[literal], litBits[literal], ref acc, ref ix, storage);
                }
                pos += insertLen;
            }
            uint copyLen = cmd.CopyLength;
            pos += copyLen;
            if (copyLen != 0 && cmd.CmdPrefix >= 128)
            {
                int distCode = cmd.DistPrefix & 0x3FF;
                uint distnumextra = (uint)cmd.DistPrefix >> 10;
                uint distextra = cmd.DistExtra;
                PutBits(distDepth[distCode], distBits[distCode], ref acc, ref ix, storage);
                PutBits(distnumextra, distextra, ref acc, ref ix, storage);
            }
        }
        storageIx = ix;
    }

    /// <summary>
    /// Stores the meta-block without doing any block splitting, just collects
    /// one histogram per block category and uses that for entropy coding.
    /// REQUIRES: length > 0
    /// REQUIRES: length &lt;= (1 &lt;&lt; 24)
    /// The optional <paramref name="arena"/> provides reusable scratch storage (allocated per call in C).
    /// </summary>
    public static void StoreMetaBlockTrivial(ReadOnlySpan<byte> input, nuint startPos, nuint length, nuint mask,
        bool isLast, in EncoderParams parameters, ReadOnlySpan<Command> commands, nuint nCommands,
        ref nuint storageIx, Span<byte> storage, MetablockArena? arena = null)
    {
        arena ??= new MetablockArena();
        HuffmanTree[] tree = arena.Tree;
        MetablockArena.SimpleCodes codes = arena.Simple;
        uint numDistanceSymbols = parameters.Dist.AlphabetSizeMax;

        StoreCompressedMetaBlockHeader(isLast, length, ref storageIx, storage);

        Histogram.HistogramClear(ref codes.LitHisto);
        Histogram.HistogramClear(ref codes.CmdHisto);
        Histogram.HistogramClear(ref codes.DistHisto);

        BuildHistograms(input, startPos, mask, commands, nCommands,
                        ref codes.LitHisto, ref codes.CmdHisto, ref codes.DistHisto);

        BitWriter.WriteBits(13, 0, ref storageIx, storage);

        BuildAndStoreHuffmanTree(codes.LitHisto.Data, BrotliConstants.NumLiteralSymbols,
                                 BrotliConstants.NumLiteralSymbols, tree,
                                 codes.LitDepth, codes.LitBits,
                                 ref storageIx, storage);
        BuildAndStoreHuffmanTree(codes.CmdHisto.Data, BrotliConstants.NumCommandSymbols,
                                 BrotliConstants.NumCommandSymbols, tree,
                                 codes.CmdDepth, codes.CmdBits,
                                 ref storageIx, storage);
        BuildAndStoreHuffmanTree(codes.DistHisto.Data,
                                 MaxSimpleDistanceAlphabetSize,
                                 numDistanceSymbols, tree,
                                 codes.DistDepth, codes.DistBits,
                                 ref storageIx, storage);
        StoreDataWithHuffmanCodes(input, startPos, mask, commands,
                                  nCommands, codes.LitDepth, codes.LitBits,
                                  codes.CmdDepth, codes.CmdBits,
                                  codes.DistDepth, codes.DistBits,
                                  ref storageIx, storage);
        if (isLast)
        {
            JumpToByteBoundary(ref storageIx, storage);
        }
    }

    /// <summary>
    /// Same as <see cref="StoreMetaBlockTrivial"/>, but uses static prefix codes for histograms with a only a few
    /// symbols, and uses static code length prefix codes for all other histograms.
    /// REQUIRES: length > 0
    /// REQUIRES: length &lt;= (1 &lt;&lt; 24)
    /// The optional <paramref name="arena"/> provides reusable scratch storage (allocated per call in C).
    /// </summary>
    public static void StoreMetaBlockFast(ReadOnlySpan<byte> input, nuint startPos, nuint length, nuint mask,
        bool isLast, in EncoderParams parameters, ReadOnlySpan<Command> commands, nuint nCommands,
        ref nuint storageIx, Span<byte> storage, MetablockArena? arena = null)
    {
        arena ??= new MetablockArena();
        HuffmanTree[] tree = arena.Tree;
        MetablockArena.SimpleCodes codes = arena.Simple;
        uint numDistanceSymbols = parameters.Dist.AlphabetSizeMax;
        uint distanceAlphabetBits = FastLog.Log2FloorNonZero(numDistanceSymbols - 1) + 1;

        StoreCompressedMetaBlockHeader(isLast, length, ref storageIx, storage);

        BitWriter.WriteBits(13, 0, ref storageIx, storage);

        if (nCommands <= 128)
        {
            Span<uint> histogram = codes.LitHisto.Data;
            histogram.Clear();
            nuint pos = startPos;
            nuint numLiterals = 0;
            ReadOnlySpan<Command> cmds = commands.Slice(0, (int)nCommands);
            for (int i = 0; i < cmds.Length; ++i)
            {
                ref readonly Command cmd = ref cmds[i];
                nuint insertLen = cmd.InsertLen;
                if (insertLen != 0)
                {
                    GetRingBufferSegments(input, pos, mask, insertLen, out ReadOnlySpan<byte> first, out ReadOnlySpan<byte> second);
                    CountLiterals(first, histogram);
                    CountLiterals(second, histogram);
                    pos += insertLen;
                }
                numLiterals += insertLen;
                pos += cmd.CopyLength;
            }
            BuildAndStoreHuffmanTreeFast(tree, histogram, numLiterals,
                                         /* maxBits = */ 8,
                                         codes.LitDepth, codes.LitBits,
                                         ref storageIx, storage);
            EntropyEncodeStatic.StoreStaticCommandHuffmanTree(ref storageIx, storage);
            EntropyEncodeStatic.StoreStaticDistanceHuffmanTree(ref storageIx, storage);
            StoreDataWithHuffmanCodes(input, startPos, mask, commands,
                                      nCommands, codes.LitDepth, codes.LitBits,
                                      EntropyEncodeStatic.StaticCommandCodeDepth,
                                      EntropyEncodeStatic.StaticCommandCodeBits,
                                      EntropyEncodeStatic.StaticDistanceCodeDepth,
                                      EntropyEncodeStatic.StaticDistanceCodeBits,
                                      ref storageIx, storage);
        }
        else
        {
            Histogram.HistogramClear(ref codes.LitHisto);
            Histogram.HistogramClear(ref codes.CmdHisto);
            Histogram.HistogramClear(ref codes.DistHisto);
            BuildHistograms(input, startPos, mask, commands, nCommands,
                            ref codes.LitHisto, ref codes.CmdHisto, ref codes.DistHisto);
            BuildAndStoreHuffmanTreeFast(tree, codes.LitHisto.Data,
                                         codes.LitHisto.TotalCount,
                                         /* maxBits = */ 8,
                                         codes.LitDepth, codes.LitBits,
                                         ref storageIx, storage);
            BuildAndStoreHuffmanTreeFast(tree, codes.CmdHisto.Data,
                                         codes.CmdHisto.TotalCount,
                                         /* maxBits = */ 10,
                                         codes.CmdDepth, codes.CmdBits,
                                         ref storageIx, storage);
            BuildAndStoreHuffmanTreeFast(tree, codes.DistHisto.Data,
                                         codes.DistHisto.TotalCount,
                                         /* maxBits = */ distanceAlphabetBits,
                                         codes.DistDepth, codes.DistBits,
                                         ref storageIx, storage);
            StoreDataWithHuffmanCodes(input, startPos, mask, commands,
                                      nCommands, codes.LitDepth, codes.LitBits,
                                      codes.CmdDepth, codes.CmdBits,
                                      codes.DistDepth, codes.DistBits,
                                      ref storageIx, storage);
        }

        if (isLast)
        {
            JumpToByteBoundary(ref storageIx, storage);
        }
    }

    /// <summary>
    /// This is for storing uncompressed blocks (simple raw storage of
    /// bytes-as-bytes).
    /// REQUIRES: length > 0
    /// REQUIRES: length &lt;= (1 &lt;&lt; 24)
    /// </summary>
    public static void StoreUncompressedMetaBlock(bool isFinalBlock, ReadOnlySpan<byte> input, nuint position,
        nuint mask, nuint len, ref nuint storageIx, Span<byte> storage)
    {
        nuint maskedPos = position & mask;
        StoreUncompressedMetaBlockHeader(len, ref storageIx, storage);
        JumpToByteBoundary(ref storageIx, storage);

        if (maskedPos + len > mask + 1)
        {
            nuint len1 = mask + 1 - maskedPos;
            input.Slice((int)maskedPos, (int)len1).CopyTo(storage.Slice((int)(storageIx >> 3)));
            storageIx += len1 << 3;
            len -= len1;
            maskedPos = 0;
        }
        input.Slice((int)maskedPos, (int)len).CopyTo(storage.Slice((int)(storageIx >> 3)));
        storageIx += len << 3;

        // We need to clear the next 4 bytes to continue to be
        // compatible with BrotliWriteBits.
        BitWriter.WriteBitsPrepareStorage(storageIx, storage);

        // Since the uncompressed block itself may not be the final block, add an
        // empty one after this.
        if (isFinalBlock)
        {
            BitWriter.WriteBits(1, 1, ref storageIx, storage);  // islast
            BitWriter.WriteBits(1, 1, ref storageIx, storage);  // isempty
            JumpToByteBoundary(ref storageIx, storage);
        }
    }
}
