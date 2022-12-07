// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using WebAssemblyInfo;

public class AppendMemorySnapshotToWasmFile : Task
{
    [Required, NotNull]
    public string Source { get; set; } = default!;

    [Required, NotNull]
    public string Destination { get; set; } = default!;

    [Required, NotNull]
    public string DataSectionFile { get; set; } = default!;

    public bool Verbose { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, $"Memory snapshot at '{DataSectionFile}' size '{new FileInfo(DataSectionFile).Length}'");

        using var reader = new WasmRewriter(Source, Destination, DataSectionFile, 0, DataMode.Active, Verbose);
        reader.Parse();

        return true;
    }
}

#region wa-edit

namespace WebAssemblyInfo
{
    internal sealed class WasmRewriter : WasmReaderBase, IDisposable
    {
        private readonly string DestinationPath;
        private readonly string _dataSectionFile;
        private readonly int _dataOffset;
        private readonly DataMode _dataSectionMode;
        private readonly bool _verbose;
        private BinaryWriter Writer;

        public WasmRewriter(string source, string destination, string dataSectionFile, int dataOffset, DataMode dataSectionMode, bool verbose) : base(source, verbose)
        {
            if (_verbose)
                Console.WriteLine($"Writing wasm file: {destination}");

            DestinationPath = destination;
            _dataSectionFile = dataSectionFile;
            _dataOffset = dataOffset;
            _dataSectionMode = dataSectionMode;
            _verbose = verbose;
            var stream = File.Open(DestinationPath, FileMode.Create);
            Writer = new BinaryWriter(stream);
        }

        public void Dispose()
        {
            Writer.Dispose();
            Reader.Dispose();
        }

        protected override void ReadModule()
        {
            Writer.Write(MagicWasm);
            Writer.Write(1); // Version

            base.ReadModule();

            Writer.BaseStream.Position = MagicWasm.Length;
            Writer.Write(Version);
        }

        protected override void ReadSection(SectionInfo section)
        {
            if (File.Exists(_dataSectionFile))
            {
                if (section.id == SectionId.Data)
                {
                    RewriteDataSection();
                    return;
                }

                if (section.id == SectionId.DataCount)
                {
                    // omit DataCount section for now, it is not needed
                    return;
                }
            }

            WriteSection(section);
        }

        private void WriteSection(SectionInfo section)
        {
            Reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
            Writer.Write(Reader.ReadBytes((int)section.size + (int)(section.begin - section.offset)));
        }

        private struct Chunk
        {
            public int index, size;
        }

        private List<Chunk> Split(byte[] data)
        {
            int zeroesLen = 9;
            var list = new List<Chunk>();
            var span = new ReadOnlySpan<byte>(data);
            var zeroes = new ReadOnlySpan<byte>(new byte[zeroesLen]);
            int offset = 0;
            int stripped = 0;

            do
            {
                int index = span.IndexOf(zeroes);
                if (index == -1)
                {
                    if (_verbose)
                        Console.WriteLine($"  add last idx: {offset} size: {data.Length - offset} span remaining len: {span.Length}");

                    list.Add(new Chunk { index = offset, size = data.Length - offset });
                    return list;
                }
                if (index != 0)
                {
                    if (_verbose)
                        Console.WriteLine($"  add idx: {offset} size: {index} span remaining len: {span.Length} span index: {index}");

                    list.Add(new Chunk { index = offset, size = index });
                    span = span.Slice(index + zeroesLen);
                    offset += index + zeroesLen;
                    stripped += zeroesLen;
                }

                index = -1;
                for (int i = 0; i < span.Length; i++)
                {
                    if (span[i] != (byte)0)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    stripped += data.Length - offset;
                    break;
                }

                //Console.WriteLine($"skip: {index}");
                if (index != 0)
                {
                    span = span.Slice(index);
                    offset += index;
                    stripped += index;
                }
            } while (true);

            if (_verbose)
                Console.Write($"    segments detected: {list.Count:N0} zero bytes stripped: {stripped:N0}");

            return list;
        }

        private void RewriteDataSection()
        {
            //var oo = Writer.BaseStream.Position;
            var bytes = File.ReadAllBytes(_dataSectionFile);
            var segments = Split(bytes);

            var mode = _dataSectionMode;
            var sectionLen = U32Len((uint)segments.Count);
            foreach (var segment in segments)
                sectionLen += GetDataSegmentLength(mode, segment, _dataOffset + segment.index);

            // section beginning
            Writer.Write((byte)SectionId.Data);
            WriteU32(sectionLen);

            // section content
            WriteU32((uint)segments.Count);
            foreach (var segment in segments)
                WriteDataSegment(mode, bytes, segment, _dataOffset + segment.index);

            //var pos = Writer.BaseStream.Position;
            //Writer.BaseStream.Position = oo;
            //DumpBytes(64);
            //Writer.BaseStream.Position = pos;
        }

        private static uint GetDataSegmentLength(DataMode mode, Chunk chunk, int memoryOffset)
        {
            var len = U32Len((uint)mode) + U32Len((uint)chunk.size) + (uint)chunk.size;
            if (mode == DataMode.Active)
                len += WasmRewriter.ConstI32ExprLen(memoryOffset);

            return len;
        }

        private void WriteDataSegment(DataMode mode, byte[] data, Chunk chunk, int memoryOffset)
        {
            // data segment
            WriteU32((uint)mode);

            if (mode == DataMode.Active)
                WriteConstI32Expr(memoryOffset);

            WriteU32((uint)chunk.size);
            Writer.Write(data, chunk.index, chunk.size);
        }

        public void DumpBytes(int count)
        {
            var pos = Writer.BaseStream.Position;
            Console.WriteLine("bytes");

            for (int i = 0; i < count; i++)
            {
                Console.Write($" {Writer.BaseStream.ReadByte():x}");
            }

            Console.WriteLine();
            Writer.BaseStream.Position = pos;
        }

        private static uint ConstI32ExprLen(int cn)
        {
            return 2 + I32Len(cn);
        }

        // i32.const <cn>
        private void WriteConstI32Expr(int cn)
        {
            Writer.Write((byte)Opcode.I32_Const);
            WriteI32(cn);
            Writer.Write((byte)Opcode.End);
        }

        public void WriteU32(uint n)
        {
            do
            {
                byte b = (byte)(n & 0x7f);
                n >>= 7;
                if (n != 0)
                    b |= 0x80;
                Writer.Write(b);
            } while (n != 0);
        }

        public static uint U32Len(uint n)
        {
            uint len = 0u;
            do
            {
                n >>= 7;
                len++;
            } while (n != 0);

            return len;
        }

        public void WriteI32(int n)
        {
            var final = false;
            do
            {
                byte b = (byte)(n & 0x7f);
                n >>= 7;

                if ((n == 0 && ((n & 0x80000000) == 0)) || (n == -1 && ((n & 0x80000000) == 0x80)))
                    final = true;
                else
                    b |= 0x80;

                Writer.Write(b);
            } while (!final);
        }

        public static uint I32Len(int n)
        {
            var final = false;
            var len = 0u;
            do
            {
                n >>= 7;

                if ((n == 0 && ((n & 0x80000000) == 0)) || (n == -1 && ((n & 0x80000000) == 0x80)))
                    final = true;

                len++;
            } while (!final);

            return len;
        }
    }

    public abstract class WasmReaderBase
    {
        public readonly BinaryReader Reader;
        private readonly bool _verbose;

        public uint Version { get; private set; }
        public string Path { get; private set; }

        public WasmReaderBase(string path, bool verbose)
        {
            if (_verbose)
                Console.WriteLine($"Reading wasm file: {path}");

            Path = path;
            _verbose = verbose;
            var stream = File.Open(Path, FileMode.Open);
            Reader = new BinaryReader(stream);
        }

        public void Parse()
        {
            ReadModule();
        }

        protected byte[] MagicWasm = { 0x0, 0x61, 0x73, 0x6d };

        protected virtual void ReadModule()
        {
            var magicBytes = Reader.ReadBytes(4);

            for (int i = 0; i < MagicWasm.Length; i++)
            {
                if (MagicWasm[i] != magicBytes[i])
                    throw new FileLoadException("not wasm file, module magic is wrong");
            }

            Version = Reader.ReadUInt32();
            if (_verbose)
                Console.WriteLine($"WebAssembly binary format version: {Version}");

            while (Reader.BaseStream.Position < Reader.BaseStream.Length)
                ReadSection();
        }

        protected enum SectionId
        {
            Custom = 0,
            Type,
            Import,
            Function,
            Table,
            Memory,
            Global,
            Export,
            Start,
            Element,
            Code,
            Data,
            DataCount,
            Tag,
        }

        protected struct SectionInfo
        {
            public SectionId id;
            public uint size;
            public long offset;
            public long begin;
        }
        protected List<SectionInfo> sections = new();
        protected Dictionary<SectionId, List<SectionInfo>> sectionsById = new();

        protected abstract void ReadSection(SectionInfo section);

        private void ReadSection()
        {
            var section = new SectionInfo() { offset = Reader.BaseStream.Position, id = (SectionId)Reader.ReadByte(), size = ReadU32(), begin = Reader.BaseStream.Position };
            sections.Add(section);
            if (!sectionsById.ContainsKey(section.id))
                sectionsById[section.id] = new List<SectionInfo>();

            sectionsById[section.id].Add(section);

            if (_verbose)
                Console.Write($"Reading section: {section.id,9} size: {section.size,12}");

            ReadSection(section);

            if (_verbose)
                Console.WriteLine();

            Reader.BaseStream.Seek(section.begin + section.size, SeekOrigin.Begin);
        }

        public uint ReadU32()
        {
            uint value = 0;
            var offset = 0;
            do
            {
                var b = Reader.ReadByte();
                value |= (uint)(b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    break;

                offset += 7;
            } while (true);

            return value;
        }

        protected int ReadI32()
        {
            int value = 0;
            var offset = 0;
            byte b;

            do
            {
                b = Reader.ReadByte();
                value |= (int)(b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    break;

                offset += 7;
            } while (true);

            if (offset < 32 && (b & 0x40) == 0x40)
                value |= (~(int)0 << offset);

            return value;
        }

        protected long ReadI64()
        {
            long value = 0;
            var offset = 0;
            byte b;

            do
            {
                b = Reader.ReadByte();
                value |= (long)(b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    break;

                offset += 7;
            } while (true);

            if (offset < 64 && (b & 0x40) == 0x40)
                value |= (~(long)0 << offset);

            return value;
        }
    }

    public enum DataMode
    {
        Active = 0,
        Passive = 1,
        ActiveMemory = 2,
    }

    internal enum Opcode : byte
    {
        // control
        Unreachable = 0x00,
        Nop = 0x01,
        Block = 0x02,
        Loop = 0x03,
        If = 0x04,
        Else = 0x05,
        Try = 0x06,
        Catch = 0x07,
        Throw = 0x08,
        Rethrow = 0x09,
        End = 0x0b,
        Br = 0x0c,
        Br_If = 0x0d,
        Br_Table = 0x0e,
        Return = 0x0f,
        Call = 0x10,
        Call_Indirect = 0x11,
        Delegate = 0x18,
        Catch_All = 0x19,
        // reference
        Ref_Null = 0xd0,
        Ref_Is_Null = 0xd1,
        Ref_Func = 0xd2,
        // parametric
        Drop = 0x1a,
        Select = 0x1b,
        Select_Vec = 0x1c,
        // variable
        Local_Get = 0x20,
        Local_Set = 0x21,
        Local_Tee = 0x22,
        Global_Get = 0x23,
        Global_Set = 0x24,
        // table
        Table_Get = 0x25,
        Table_Set = 0x26,
        // memory
        I32_Load = 0x28,
        I64_Load = 0x29,
        F32_Load = 0x2a,
        F64_Load = 0x2b,
        I32_Load8_S = 0x2c,
        I32_Load8_U = 0x2d,
        I32_Load16_S = 0x2e,
        I32_Load16_U = 0x2f,
        I64_Load8_S = 0x30,
        I64_Load8_U = 0x31,
        I64_Load16_S = 0x32,
        I64_Load16_U = 0x33,
        I64_Load32_S = 0x34,
        I64_Load32_U = 0x35,
        I32_Store = 0x36,
        I64_Store = 0x37,
        F32_Store = 0x38,
        F64_Store = 0x39,
        I32_Store8 = 0x3a,
        I32_Store16 = 0x3b,
        I64_Store8 = 0x3c,
        I64_Store16 = 0x3d,
        I64_Store32 = 0x3e,
        Memory_Size = 0x3f,
        Memory_Grow = 0x40,
        // numeric
        I32_Const = 0x41,
        I64_Const = 0x42,
        F32_Const = 0x43,
        F64_Const = 0x44,
        I32_Eqz = 0x45,
        I32_Eq = 0x46,
        I32_Ne = 0x47,
        I32_Lt_S = 0x48,
        I32_Lt_U = 0x49,
        I32_Gt_S = 0x4a,
        I32_Gt_U = 0x4b,
        I32_Le_S = 0x4c,
        I32_Le_U = 0x4d,
        I32_Ge_S = 0x4e,
        I32_Ge_U = 0x4f,
        I64_Eqz = 0x50,
        I64_Eq = 0x51,
        I64_Ne = 0x52,
        I64_Lt_S = 0x53,
        I64_Lt_U = 0x54,
        I64_Gt_S = 0x55,
        I64_Gt_U = 0x56,
        I64_Le_S = 0x57,
        I64_Le_U = 0x58,
        I64_Ge_S = 0x59,
        I64_Ge_U = 0x5a,
        F32_Eq = 0x5b,
        F32_Ne = 0x5c,
        F32_Lt = 0x5d,
        F32_Gt = 0x5e,
        F32_Le = 0x5f,
        F32_Ge = 0x60,
        F64_Eq = 0x61,
        F64_Ne = 0x62,
        F64_Lt = 0x63,
        F64_Gt = 0x64,
        F64_Le = 0x65,
        F64_Ge = 0x66,
        I32_Clz = 0x67,
        I32_Ctz = 0x68,
        I32_Popcnt = 0x69,
        I32_Add = 0x6a,
        I32_Sub = 0x6b,
        I32_Mul = 0x6c,
        I32_Div_S = 0x6d,
        I32_Div_U = 0x6e,
        I32_Rem_S = 0x6f,
        I32_Rem_U = 0x70,
        I32_And = 0x71,
        I32_Or = 0x72,
        I32_Xor = 0x73,
        I32_Shl = 0x74,
        I32_Shr_S = 0x75,
        I32_Shr_U = 0x76,
        I32_Rotl = 0x77,
        I32_Rotr = 0x78,
        I64_Clz = 0x79,
        I64_Ctz = 0x7a,
        I64_Popcnt = 0x7b,
        I64_Add = 0x7c,
        I64_Sub = 0x7d,
        I64_Mul = 0x7e,
        I64_Div_S = 0x7f,
        I64_Div_U = 0x80,
        I64_Rem_S = 0x81,
        I64_Rem_U = 0x82,
        I64_And = 0x83,
        I64_Or = 0x84,
        I64_Xor = 0x85,
        I64_Shl = 0x86,
        I64_Shr_S = 0x87,
        I64_Shr_U = 0x88,
        I64_Rotl = 0x89,
        I64_Rotr = 0x8a,
        F32_Abs = 0x8b,
        F32_Neg = 0x8c,
        F32_Ceil = 0x8d,
        F32_Floor = 0x8e,
        F32_Trunc = 0x8f,
        F32_Nearest = 0x90,
        F32_Sqrt = 0x91,
        F32_Add = 0x92,
        F32_Sub = 0x93,
        F32_Mul = 0x94,
        F32_Div = 0x95,
        F32_Min = 0x96,
        F32_Max = 0x97,
        F32_Copysign = 0x98,
        F64_Abs = 0x99,
        F64_Neg = 0x9a,
        F64_Ceil = 0x9b,
        F64_Floor = 0x9c,
        F64_Trunc = 0x9d,
        F64_Nearest = 0x9e,
        F64_Sqrt = 0x9f,
        F64_Add = 0xa0,
        F64_Sub = 0xa1,
        F64_Mul = 0xa2,
        F64_Div = 0xa3,
        F64_Min = 0xa4,
        F64_Max = 0xa5,
        F64_Copysign = 0xa6,
        I32_Wrap_I64 = 0xa7,
        I32_Trunc_F32_S = 0xa8,
        I32_Trunc_F32_U = 0xa9,
        I32_Trunc_F64_S = 0xaa,
        I32_Trunc_F64_U = 0xab,
        I64_Extend_I32_S = 0xac,
        I64_Extend_I32_U = 0xad,
        I64_Trunc_F32_S = 0xae,
        I64_Trunc_F32_U = 0xaf,
        I64_Trunc_F64_S = 0xb0,
        I64_Trunc_F64_U = 0xb1,
        F32_Convert_I32_S = 0xb2,
        F32_Convert_I32_U = 0xb3,
        F32_Convert_I64_S = 0xb4,
        F32_Convert_I64_U = 0xb5,
        F32_Demote_F64 = 0xb6,
        F64_Convert_I32_S = 0xb7,
        F64_Convert_I32_U = 0xb8,
        F64_Convert_I64_S = 0xb9,
        F64_Convert_I64_U = 0xba,
        F64_Promote_F32 = 0xbb,
        I32_Reinterpret_F32 = 0xbc,
        I64_Reinterpret_F64 = 0xbd,
        F32_Reinterpret_I32 = 0xbe,
        F64_Reinterpret_I64 = 0xbf,
        I32_Extend8_S = 0xc0,
        I32_Extend16_S = 0xc1,
        I64_Extend8_S = 0xc2,
        I64_Extend16_S = 0xc3,
        I64_Extend32_S = 0xc4,
        // special
        Prefix = 0xfc,
        SIMDPrefix = 0xfd,
        MTPrefix = 0xfe,
    }

    internal enum PrefixOpcode : byte
    {
        // saturating
        I32_Trunc_Sat_F32_S = 0,
        I32_Trunc_Sat_F32_U = 1,
        I32_Trunc_Sat_F64_S = 2,
        I32_Trunc_Sat_F64_U = 3,
        I64_Trunc_Sat_F32_S = 4,
        I64_Trunc_Sat_F32_U = 5,
        I64_Trunc_Sat_F64_S = 6,
        I64_Trunc_Sat_F64_U = 7,
        // memory
        Memory_Init = 8,
        Data_Drop = 9,
        Memory_Copy = 10,
        Memory_Fill = 11,
        // table
        Table_Init = 12,
        Elem_Drop = 13,
        Table_Copy = 14,
        Table_Grow = 15,
        Table_Size = 16,
        Table_Fill = 17,
    }
}

#endregion
