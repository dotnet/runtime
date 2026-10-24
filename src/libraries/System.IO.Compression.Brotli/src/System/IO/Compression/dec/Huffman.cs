// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/dec/huffman.h and huffman.c.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>An entry of a Huffman decoding table.</summary>
internal struct HuffmanCode
{
    /// <summary>Number of bits used for this symbol.</summary>
    public byte Bits;
    /// <summary>Symbol value or table offset.</summary>
    public ushort Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HuffmanCode(byte bits, ushort value)
    {
        Bits = bits;
        Value = value;
    }
}

/// <summary>Contains a collection of Huffman trees with the same alphabet size.</summary>
/// <remarks>
/// alphabet_size_limit is needed due to simple codes, since log2(alphabet_size_max) could be greater than
/// log2(alphabet_size_limit). <see cref="Htrees"/> holds offsets of the trees in <see cref="Codes"/>, which is shared
/// by all the tree groups of a decoder (the group's trees start at <see cref="Base"/>).
/// </remarks>
internal sealed class HuffmanTreeGroup
{
    public HtreeOffsets Htrees;
    public HuffmanCode[]? Codes;
    public int Base;
    public ushort AlphabetSizeMax;
    public ushort AlphabetSizeLimit;
    public ushort NumHtrees;
}

/// <summary>Offsets of the trees of a <see cref="HuffmanTreeGroup"/> (at most 256 trees per group).</summary>
[InlineArray(BrotliConstants.MaxNumberOfBlockTypes)]
internal struct HtreeOffsets
{
    private int _e0;
}

/// <summary>Utilities for building Huffman decoding tables.</summary>
internal static class Huffman
{
    public const int MaxCodeLength = 15;

    /// <summary>BROTLI_NUM_BLOCK_LEN_SYMBOLS == 26.</summary>
    public const int MaxSize26 = 396;
    /// <summary>BROTLI_MAX_BLOCK_TYPE_SYMBOLS == 258.</summary>
    public const int MaxSize258 = 632;
    /// <summary>BROTLI_MAX_CONTEXT_MAP_SYMBOLS == 272.</summary>
    public const int MaxSize272 = 646;

    public const int MaxCodeLengthCodeLength = 5;

    private const int ReverseBitsMax = 8;
    private const uint ReverseBitsLowest = 1u << (ReverseBitsMax - 1);

    private static ReadOnlySpan<byte> ReverseBitsTable =>
    [
        0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
        0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
        0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
        0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
        0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
        0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
        0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
        0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
        0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
        0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
        0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
        0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
        0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
        0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
        0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
        0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
        0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
        0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
        0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
        0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
        0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
        0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
        0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
        0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
        0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
        0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
        0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
        0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
        0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
        0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
        0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
        0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF,
    ];

    /// <summary>
    /// Returns reverse(num, ReverseBitsMax), where reverse(value, len) is the bit-wise reversal of the len least
    /// significant bits of value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReverseBits(uint num) => ReverseBitsTable[(int)(byte)num];

    /// <summary>
    /// Stores code in table[offset], table[offset+step], ..., table[offset+end]. Assumes that end is an integer
    /// multiple of step.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReplicateValue(Span<HuffmanCode> table, int offset, int step, int end, HuffmanCode code)
    {
        do
        {
            end -= step;
            table[offset + end] = code;
        }
        while (end > 0);
    }

    /// <summary>
    /// Returns the table width of the next 2nd level table. <paramref name="count"/> is the histogram of bit
    /// lengths for the remaining symbols, <paramref name="len"/> is the code length of the next processed symbol.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int NextTableBitSize(ReadOnlySpan<ushort> count, int len, int rootBits)
    {
        int left = 1 << (len - rootBits);
        while (len < MaxCodeLength)
        {
            left -= count[len];
            if (left <= 0)
            {
                break;
            }
            ++len;
            left <<= 1;
        }
        return len - rootBits;
    }

    /// <summary>Builds Huffman lookup table assuming code lengths are in symbol order.</summary>
    public static void BuildCodeLengthsHuffmanTable(Span<HuffmanCode> table, ReadOnlySpan<byte> codeLengths, ReadOnlySpan<ushort> count)
    {
        HuffmanCode code;   // current table entry
        int symbol;         // symbol index in original or sorted table
        uint key;           // prefix code
        uint keyStep;       // prefix code addend
        int step;           // step size to replicate values in current table
        int tableSize;      // size of current table
        Span<int> sorted = stackalloc int[BrotliConstants.CodeLengthCodes]; // symbols sorted by code length
        // offsets in sorted table for each length
        Span<int> offset = stackalloc int[MaxCodeLengthCodeLength + 1];
        int bits;
        int bitsCount;
        Debug.Assert(MaxCodeLengthCodeLength <= ReverseBitsMax);

        // Generate offsets into sorted symbol table by code length.
        symbol = -1;
        for (bits = 1; bits <= MaxCodeLengthCodeLength; bits++)
        {
            symbol += count[bits];
            offset[bits] = symbol;
        }
        // Symbols with code length 0 are placed after all other symbols.
        offset[0] = BrotliConstants.CodeLengthCodes - 1;

        // Sort symbols by length, by symbol order within each length.
        symbol = BrotliConstants.CodeLengthCodes;
        do
        {
            symbol--;
            sorted[offset[codeLengths[symbol]]--] = symbol;
        }
        while (symbol != 0);

        tableSize = 1 << MaxCodeLengthCodeLength;

        // Special case: all symbols but one have 0 code length.
        if (offset[0] == 0)
        {
            code = new HuffmanCode(0, (ushort)sorted[0]);
            for (key = 0; key < (uint)tableSize; ++key)
            {
                table[(int)key] = code;
            }
            return;
        }

        // Fill in table.
        key = 0;
        keyStep = ReverseBitsLowest;
        symbol = 0;
        bits = 1;
        step = 2;
        do
        {
            for (bitsCount = count[bits]; bitsCount != 0; --bitsCount)
            {
                code = new HuffmanCode((byte)bits, (ushort)sorted[symbol++]);
                ReplicateValue(table, ReverseBits(key), step, tableSize, code);
                key += keyStep;
            }
            step <<= 1;
            keyStep >>= 1;
        }
        while (++bits <= MaxCodeLengthCodeLength);
    }

    /// <summary>
    /// Builds Huffman lookup table assuming code lengths are in symbol order. Returns size of resulting table.
    /// </summary>
    /// <param name="rootTable">The table to fill.</param>
    /// <param name="rootBits">Width of the root table.</param>
    /// <param name="symbolListsArray">
    /// The whole symbol lists storage; C's <c>symbol_lists</c> pointer points to element
    /// <c>MaxCodeLength + 1</c> of it (negative indices are addressable).
    /// </param>
    /// <param name="count">Histogram of code lengths; consumed (modified) by this method.</param>
    public static uint BuildHuffmanTable(Span<HuffmanCode> rootTable, int rootBits, ReadOnlySpan<ushort> symbolListsArray, Span<ushort> count)
    {
        const int SymbolListsBase = MaxCodeLength + 1;
        HuffmanCode code;   // current table entry
        int table;          // next available space in table (offset in rootTable)
        int len;            // current code length
        int symbol;         // symbol index in original or sorted table
        uint key;           // prefix code
        uint keyStep;       // prefix code addend
        uint subKey;        // 2nd level table prefix code
        uint subKeyStep;    // 2nd level table prefix code addend
        int step;           // step size to replicate values in current table
        int tableBits;      // key length of current table
        int tableSize;      // size of current table
        int totalSize;      // sum of root table size and 2nd level table sizes
        int maxLength = -1;
        int bits;
        int bitsCount;

        Debug.Assert(rootBits <= ReverseBitsMax);
        Debug.Assert(MaxCodeLength - rootBits <= ReverseBitsMax);

        while (symbolListsArray[SymbolListsBase + maxLength] == 0xFFFF)
        {
            maxLength--;
        }
        maxLength += MaxCodeLength + 1;

        table = 0;
        tableBits = rootBits;
        tableSize = 1 << tableBits;
        totalSize = tableSize;

        // Fill in the root table. Reduce the table size to if possible, and create the repetitions by memcpy.
        if (tableBits > maxLength)
        {
            tableBits = maxLength;
            tableSize = 1 << tableBits;
        }
        key = 0;
        keyStep = ReverseBitsLowest;
        bits = 1;
        step = 2;
        do
        {
            symbol = bits - (MaxCodeLength + 1);
            for (bitsCount = count[bits]; bitsCount != 0; --bitsCount)
            {
                symbol = symbolListsArray[SymbolListsBase + symbol];
                code = new HuffmanCode((byte)bits, (ushort)symbol);
                ReplicateValue(rootTable, table + ReverseBits(key), step, tableSize, code);
                key += keyStep;
            }
            step <<= 1;
            keyStep >>= 1;
        }
        while (++bits <= tableBits);

        // If root_bits != table_bits then replicate to fill the remaining slots.
        while (totalSize != tableSize)
        {
            rootTable.Slice(table, tableSize).CopyTo(rootTable.Slice(table + tableSize));
            tableSize <<= 1;
        }

        // Fill in 2nd level tables and add pointers to root table.
        keyStep = ReverseBitsLowest >> (rootBits - 1);
        subKey = ReverseBitsLowest << 1;
        subKeyStep = ReverseBitsLowest;
        for (len = rootBits + 1, step = 2; len <= maxLength; ++len)
        {
            symbol = len - (MaxCodeLength + 1);
            for (; count[len] != 0; --count[len])
            {
                if (subKey == (ReverseBitsLowest << 1))
                {
                    table += tableSize;
                    tableBits = NextTableBitSize(count, len, rootBits);
                    tableSize = 1 << tableBits;
                    totalSize += tableSize;
                    subKey = (uint)ReverseBits(key);
                    key += keyStep;
                    rootTable[(int)subKey] = new HuffmanCode((byte)(tableBits + rootBits), (ushort)(table - (int)subKey));
                    subKey = 0;
                }
                symbol = symbolListsArray[SymbolListsBase + symbol];
                code = new HuffmanCode((byte)(len - rootBits), (ushort)symbol);
                ReplicateValue(rootTable, table + ReverseBits(subKey), step, tableSize, code);
                subKey += subKeyStep;
            }
            step <<= 1;
            subKeyStep >>= 1;
        }
        return (uint)totalSize;
    }

    /// <summary>
    /// Builds a simple Huffman table. The <paramref name="numSymbols"/> parameter is to be interpreted as follows:
    /// 0 means 1 symbol, 1 means 2 symbols, 2 means 3 symbols, 3 means 4 symbols with lengths [2, 2, 2, 2],
    /// 4 means 4 symbols with lengths [1, 2, 3, 3].
    /// </summary>
    public static uint BuildSimpleHuffmanTable(Span<HuffmanCode> table, int rootBits, Span<ushort> val, uint numSymbols)
    {
        uint tableSize = 1;
        uint goalSize = 1u << rootBits;
        switch (numSymbols)
        {
            case 0:
                table[0] = new HuffmanCode(0, val[0]);
                break;
            case 1:
                if (val[1] > val[0])
                {
                    table[0] = new HuffmanCode(1, val[0]);
                    table[1] = new HuffmanCode(1, val[1]);
                }
                else
                {
                    table[0] = new HuffmanCode(1, val[1]);
                    table[1] = new HuffmanCode(1, val[0]);
                }
                tableSize = 2;
                break;
            case 2:
                table[0] = new HuffmanCode(1, val[0]);
                table[2] = new HuffmanCode(1, val[0]);
                if (val[2] > val[1])
                {
                    table[1] = new HuffmanCode(2, val[1]);
                    table[3] = new HuffmanCode(2, val[2]);
                }
                else
                {
                    table[1] = new HuffmanCode(2, val[2]);
                    table[3] = new HuffmanCode(2, val[1]);
                }
                tableSize = 4;
                break;
            case 3:
                {
                    for (int i = 0; i < 3; ++i)
                    {
                        for (int k = i + 1; k < 4; ++k)
                        {
                            if (val[k] < val[i])
                            {
                                (val[k], val[i]) = (val[i], val[k]);
                            }
                        }
                    }
                    table[0] = new HuffmanCode(2, val[0]);
                    table[2] = new HuffmanCode(2, val[1]);
                    table[1] = new HuffmanCode(2, val[2]);
                    table[3] = new HuffmanCode(2, val[3]);
                    tableSize = 4;
                    break;
                }
            case 4:
                {
                    if (val[3] < val[2])
                    {
                        (val[3], val[2]) = (val[2], val[3]);
                    }
                    table[0] = new HuffmanCode(1, val[0]);
                    table[1] = new HuffmanCode(2, val[1]);
                    table[2] = new HuffmanCode(1, val[0]);
                    table[3] = new HuffmanCode(3, val[2]);
                    table[4] = new HuffmanCode(1, val[0]);
                    table[5] = new HuffmanCode(2, val[1]);
                    table[6] = new HuffmanCode(1, val[0]);
                    table[7] = new HuffmanCode(3, val[3]);
                    tableSize = 8;
                    break;
                }
        }
        while (tableSize != goalSize)
        {
            table.Slice(0, (int)tableSize).CopyTo(table.Slice((int)tableSize));
            tableSize <<= 1;
        }
        return goalSize;
    }
}
