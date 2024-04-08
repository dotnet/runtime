// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class ObjectFileScraper
{
    public static readonly ReadOnlyMemory<byte> MagicLE = new byte[8] { 0x44, 0x41, 0x43, 0x42, 0x4C, 0x4F, 0x42, 0x00 }; // "DACBLOB\0"
    public static readonly ReadOnlyMemory<byte> MagicBE = new byte[8] { 0x00, 0x42, 0x4F, 0x4C, 0x42, 0x43, 0x41, 0x44 };

    private readonly DataDescriptorModel.Builder _builder;

    public bool Verbose {get;}
    public ObjectFileScraper(bool verbose, DataDescriptorModel.Builder builder)
    {
        Verbose = verbose;
        _builder = builder;
    }

    public async Task<bool> ScrapeInput(string inputPath, CancellationToken token)
    {
        var bytes = await File.ReadAllBytesAsync(inputPath, token);
        if (!ScraperState.FindMagic(bytes, out var state))
        {
            return false;
        }
        if (Verbose) {
            Console.WriteLine($"{inputPath}: magic at {state.MagicStart}");
        }
        var header = ReadHeader(state);
        if (Verbose) {
            DumpHeaderDirectory(header);
        }
        var content = ReadContent(state, header);
        content.AddToModel(_builder);
        return true;
    }

    class ScraperState
    {
        public ReadOnlyMemory<byte> Data { get; }
        public bool LittleEndian { get; }
        private long _position;

        public long MagicStart => HeaderStart - MagicLE.Length;
        public long HeaderStart { get; }

        private ScraperState(ReadOnlyMemory<byte> data, bool isLittleEndian, long headerStart)
        {
            Data = data;
            LittleEndian = isLittleEndian;
            HeaderStart = headerStart;
            _position = headerStart;
        }

        public static bool FindMagic(ReadOnlyMemory<byte> bytes, [NotNullWhen(true)] out ScraperState? scraperState)
        {
            if (FindMagic(bytes.Span, out int offset, out bool isLittleEndian))
            {
                scraperState = new ScraperState(bytes, isLittleEndian, offset + MagicLE.Length);
                return true;
            }
            scraperState = null;
            return false;
        }

        private static bool FindMagic(ReadOnlySpan<byte> buffer, out int offset, out bool isLittleEndian)
        {
            int start = buffer.IndexOf(MagicLE.Span);
            if (start != -1)
            {
                offset = start;
                isLittleEndian = true;
                return true;
            }
            start = buffer.IndexOf(MagicBE.Span);
            if (start != -1)
            {
                offset = start;
                isLittleEndian = false;
                return true;
            }
            offset = 0;
            isLittleEndian = false;
            return false;
        }

        public ulong GetUInt64(long offset) => LittleEndian ? BinaryPrimitives.ReadUInt64LittleEndian(Data.Span.Slice((int)offset)) : BinaryPrimitives.ReadUInt64BigEndian(Data.Span.Slice((int)offset));
        public uint GetUInt32(long offset) => LittleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(Data.Span.Slice((int)offset)) : BinaryPrimitives.ReadUInt32BigEndian(Data.Span.Slice((int)offset));
        public ushort GetUInt16(long offset) => LittleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(Data.Span.Slice((int)offset)) : BinaryPrimitives.ReadUInt16BigEndian(Data.Span.Slice((int)offset));
        public byte GetByte(long offset) => Data.Span[(int)offset];


        public void GetBytes(long offset, Span<byte> buffer) => Data.Span.Slice((int)offset, buffer.Length).CopyTo(buffer);

        public void ResetPosition(long position)
        {
            _position = position;
        }

        public ulong ReadUInt64()
        {
            var value = GetUInt64(_position);
            _position += sizeof(ulong);
            return value;
        }
        public uint ReadUInt32()
        {
            var value = GetUInt32(_position);
            _position += sizeof(uint);
            return value;
        }
        public ushort ReadUInt16()
        {
            var value = GetUInt16(_position);
            _position += sizeof(ushort);
            return value;
        }

        public byte ReadByte()
        {
            var value = GetByte(_position);
            _position += sizeof(byte);
            return value;
        }
        public void ReadBytes(Span<byte> buffer)
        {
            GetBytes(_position, buffer);
            _position += buffer.Length;
        }

        public void Skip(int count)
        {
            _position += count;
        }
    }


    struct HeaderDirectory
    {
        public uint BaselineStart;
        public uint TypesStart;

        public uint FieldPoolStart;
        public uint GlobalLiteralValuesStart;

        public uint GlobalPointersStart;
        public uint NamesStart;

        public uint TypeCount;
        public uint FieldPoolCount;

        public uint GlobalLiteralValuesCount;
        public uint GlobalPointerValuesCount;

        public uint NamesPoolCount;

        public byte TypeSpecSize;
        public byte FieldSpecSize;
        public byte GlobalLiteralSpecSize;
        public byte GlobalPointerSpecSize;
    };

    private void DumpHeaderDirectory(HeaderDirectory headerDirectory)
    {
        Console.WriteLine($"Baseline Start = {headerDirectory.BaselineStart}");
        Console.WriteLine($"Types Start = {headerDirectory.TypesStart}");
        Console.WriteLine($"Field Pool  Start = {headerDirectory.FieldPoolStart}");
        Console.WriteLine($"Global Literals Start = {headerDirectory.GlobalLiteralValuesStart}");
        Console.WriteLine($"Global Pointers Start = {headerDirectory.GlobalPointersStart}");
        Console.WriteLine($"Names Start = {headerDirectory.NamesStart}");
        Console.WriteLine($"TypeCount = {headerDirectory.TypeCount}");
        Console.WriteLine($"FieldPoolCount = {headerDirectory.FieldPoolCount}");
        Console.WriteLine($"GlobalLiteralValuesCount = {headerDirectory.GlobalLiteralValuesCount}");
        Console.WriteLine($"GlobalPointerValuesCount = {headerDirectory.GlobalPointerValuesCount}");
    }

    private uint Swap(bool isLittleEndian, uint value) => (isLittleEndian == BitConverter.IsLittleEndian) ? value : BinaryPrimitives.ReverseEndianness(value);
    private ushort Swap(bool isLittleEndian, ushort value) => (isLittleEndian == BitConverter.IsLittleEndian) ? value : BinaryPrimitives.ReverseEndianness(value);
    private ulong Swap(bool isLittleEndian, ulong value) => (isLittleEndian == BitConverter.IsLittleEndian) ? value : BinaryPrimitives.ReverseEndianness(value);


    private HeaderDirectory ReadHeader(ScraperState state)
    {
        state.ResetPosition(state.HeaderStart);
        var baselineStart = state.ReadUInt32();
        var typesStart = state.ReadUInt32();

        var fieldPoolStart = state.ReadUInt32();
        var globalLiteralValuesStart = state.ReadUInt32();

        var globalPointersStart = state.ReadUInt32();
        var namesStart = state.ReadUInt32();

        var typeCount = state.ReadUInt32();
        var fieldPoolCount = state.ReadUInt32();

        var globalLiteralValuesCount = state.ReadUInt32();
        var globalPointerValuesCount = state.ReadUInt32();

        var namesPoolCount = state.ReadUInt32();

        var typeSpecSize = state.ReadByte();
        var fieldSpecSize = state.ReadByte();
        var globalLiteralSpecSize = state.ReadByte();
        var globalPointerSpecSize = state.ReadByte();

        return new HeaderDirectory {
            BaselineStart = baselineStart,
            TypesStart = typesStart,
            FieldPoolStart = fieldPoolStart,
            GlobalLiteralValuesStart = globalLiteralValuesStart,
            GlobalPointersStart = globalPointersStart,
            NamesStart = namesStart,

            TypeCount = typeCount,
            FieldPoolCount = fieldPoolCount,

            GlobalLiteralValuesCount = globalLiteralValuesCount,
            GlobalPointerValuesCount = globalPointerValuesCount,

            NamesPoolCount = namesPoolCount,

            TypeSpecSize = typeSpecSize,
            FieldSpecSize = fieldSpecSize,
            GlobalLiteralSpecSize = globalLiteralSpecSize,
            GlobalPointerSpecSize = globalPointerSpecSize,
        };
    }

    struct TypeSpec
    {
        public uint NameIdx;
        public uint FieldsIdx;
        public ushort Size;
    }

    struct FieldSpec
    {
        public uint NameIdx;
        public uint TypeNameIdx;
        public ushort FieldOffset;
    }

    // Like a FieldSpec but with names resolved
    struct FieldEntry
    {
        public string Name;
        public string Type;
        public ushort Offset;
    }

    struct GlobalLiteralSpec
    {
        public uint NameIdx;
        public uint TypeNameIdx;
        public ulong Value;
    }

    struct GlobalPointerSpec
    {
        public uint NameIdx;
        public uint AuxDataIdx;
    }

    class Content
    {
        public required uint Baseline { get; init; }
        public required IReadOnlyList<TypeSpec> TypeSpecs { get; init; }
        public required IReadOnlyList<FieldSpec> FieldSpecs { get; init; }
        public required IReadOnlyList<GlobalLiteralSpec> GlobaLiteralSpecs { get; init; }
        public required IReadOnlyList<GlobalPointerSpec> GlobalPointerSpecs { get; init; }
        public required ReadOnlyMemory<byte> NamesPool { get; init; }

        internal string GetPoolString(uint stringIdx)
        {
            var nameStart = NamesPool.Span.Slice((int)stringIdx);
            var end = nameStart.IndexOf((byte)0); // find the first nul after index
            if (end == -1)
                throw new InvalidOperationException("expected a nul-terminated name");
            var nameBytes = nameStart.Slice(0, end);
            return System.Text.Encoding.UTF8.GetString(nameBytes);
        }

        public void AddToModel(DataDescriptorModel.Builder builder)
        {
            string baseline = GetPoolString(Baseline);
            Console.WriteLine($"baseline Name = {baseline}");
            builder.SetBaseline(baseline);

            FieldEntry[] fields = FieldSpecs.Select((fieldSpec) =>
                (fieldSpec.NameIdx != 0) ?
                new FieldEntry
                {
                    Name = GetPoolString(fieldSpec.NameIdx),
                    Type = GetPoolString(fieldSpec.TypeNameIdx),
                    Offset = fieldSpec.FieldOffset
                } :
                default
            ).ToArray();

            foreach (var typeSpec in TypeSpecs)
            {
                string typeName = GetPoolString(typeSpec.NameIdx);
                var typeBuilder = builder.AddOrUpdateType(typeName, typeSpec.Size);
                uint j = typeSpec.FieldsIdx; // convert byte offset to index;
                Console.WriteLine($"Type {typeName} has fields starting at index {j}");
                while (j < fields.Length && !String.IsNullOrEmpty(fields[j].Name))
                {
                    typeBuilder.AddOrUpdateField(fields[j].Name, fields[j].Type, fields[j].Offset);
                    Console.WriteLine($"Type {typeName} has field {fields[j].Name} with offset {fields[j].Offset}");
                    j++;
                }
                if (typeSpec.Size != 0)
                {
                    Console.WriteLine($"Type {typeName} has size {typeSpec.Size}");
                }
                else
                {
                    Console.WriteLine($"Type {typeName} has indeterminate size");
                }
            }

            foreach (var globalSpec in GlobaLiteralSpecs)
            {
                var globalName = GetPoolString(globalSpec.NameIdx);
                var globalType = GetPoolString(globalSpec.TypeNameIdx);
                var globalValue = DataDescriptorModel.GlobalValue.MakeDirect(globalSpec.Value);
                builder.AddOrUpdateGlobal(globalName, globalType, globalValue);
                Console.WriteLine($"Global {globalName} has type {globalType} with value {globalValue}");
            }

            foreach (var globalPointer in GlobalPointerSpecs)
            {
                var globalName = GetPoolString(globalPointer.NameIdx);
                var auxDataIdx = globalPointer.AuxDataIdx;
                var globalValue = DataDescriptorModel.GlobalValue.MakeIndirect(auxDataIdx);
                builder.AddOrUpdateGlobal(globalName, DataDescriptorModel.PointerTypeName, globalValue);
                Console.WriteLine($"Global pointer {globalName} has index {globalValue}");
            }
        }
    }

    private Content ReadContent(ScraperState state, HeaderDirectory header)
    {
        state.ResetPosition(state.HeaderStart + header.BaselineStart);
        var baselineNameIdx = state.ReadUInt32();
        Console.WriteLine($"baseline Name Idx = {baselineNameIdx}");

        TypeSpec[] typeSpecs = ReadTypeSpecs(state, header);
        FieldSpec[] fieldSpecs = ReadFieldSpecs(state, header);
        GlobalLiteralSpec[] globalLiteralSpecs = ReadGlobalLiteralSpecs(state, header);
        GlobalPointerSpec[] globalPointerSpecs = ReadGlobalPointerSpecs(state, header);
        byte[] namesPool = ReadNamesPool(state, header);

        byte[] endMagic = new byte[4];
        state.ReadBytes(endMagic.AsSpan());
        if (!CheckEndMagic(endMagic))
        {
            throw new InvalidOperationException($"expected endMagic, got 0x{endMagic[0]:x} 0x{endMagic[1]:x} 0x{endMagic[2]:x} 0x{endMagic[3]:x}");
        }

        return new Content
        {
            Baseline = baselineNameIdx,
            TypeSpecs = typeSpecs,
            FieldSpecs = fieldSpecs,
            GlobaLiteralSpecs = globalLiteralSpecs,
            GlobalPointerSpecs = globalPointerSpecs,
            NamesPool = namesPool
        };
    }

    private TypeSpec[] ReadTypeSpecs(ScraperState state, HeaderDirectory header)
    {
        TypeSpec[] typeSpecs = new TypeSpec[header.TypeCount];

        state.ResetPosition(state.HeaderStart + (long)header.TypesStart);
        for (int i = 0; i < header.TypeCount; i++)
        {
            int bytesRead = 0;
            typeSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += 4;
            typeSpecs[i].FieldsIdx = state.ReadUInt32();
            bytesRead += 4;
            typeSpecs[i].Size = state.ReadUInt16();
            bytesRead += 2;
            Console.WriteLine($"TypeSpec[{i}]: NameIdx = {typeSpecs[i].NameIdx}, FieldsIdx = {typeSpecs[i].FieldsIdx}, Size = {typeSpecs[i].Size}");
            // skip padding
            if (bytesRead < header.TypeSpecSize)
            {
                state.Skip(header.TypeSpecSize - bytesRead);
            }
        }
        return typeSpecs;
    }

    private FieldSpec[] ReadFieldSpecs(ScraperState state, HeaderDirectory header)
    {
        state.ResetPosition(state.HeaderStart + (long)header.FieldPoolStart);
        FieldSpec[] fieldSpecs = new FieldSpec[header.FieldPoolCount];
        for (int i = 0; i < header.FieldPoolCount; i++)
        {
            int bytesRead = 0;
            fieldSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += 4;
            fieldSpecs[i].TypeNameIdx = state.ReadUInt32();
            bytesRead += 4;
            fieldSpecs[i].FieldOffset = state.ReadUInt16();
            bytesRead += 2;
            // skip padding
            if (bytesRead < header.FieldSpecSize)
            {
                state.Skip(header.FieldSpecSize - bytesRead);
            }
        }
        return fieldSpecs;
    }

    private GlobalLiteralSpec[] ReadGlobalLiteralSpecs(ScraperState state, HeaderDirectory header)
    {
        GlobalLiteralSpec[] globalSpecs = new GlobalLiteralSpec[header.GlobalLiteralValuesCount];
        state.ResetPosition(state.HeaderStart + (long)header.GlobalLiteralValuesStart);
        for (int i = 0; i < header.GlobalLiteralValuesCount; i++)
        {
            int bytesRead = 0;
            globalSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += 4;
            globalSpecs[i].TypeNameIdx = state.ReadUInt32();
            bytesRead += 4;
            globalSpecs[i].Value = state.ReadUInt64();
            bytesRead += 8;
            // skip padding
            if (bytesRead < header.GlobalLiteralSpecSize)
            {
                state.Skip(header.GlobalLiteralSpecSize - bytesRead);
            }
        }
        return globalSpecs;
    }

    private GlobalPointerSpec[] ReadGlobalPointerSpecs(ScraperState state, HeaderDirectory header)
    {
        GlobalPointerSpec[] globalSpecs = new GlobalPointerSpec[header.GlobalPointerValuesCount];
        state.ResetPosition(state.HeaderStart + (long)header.GlobalPointersStart);
        for (int i = 0; i < header.GlobalPointerValuesCount; i++)
        {
            int bytesRead = 0;
            globalSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += 4;
            globalSpecs[i].AuxDataIdx = state.ReadUInt32();
            bytesRead += 4;
            // skip padding
            if (bytesRead < header.GlobalPointerSpecSize)
            {
                state.Skip(header.GlobalPointerSpecSize - bytesRead);
            }
        }
        return globalSpecs;
    }

    private byte[] ReadNamesPool(ScraperState state, HeaderDirectory header)
    {
        byte[] namesPool = new byte[header.NamesPoolCount];
        state.ResetPosition(state.HeaderStart + (long)header.NamesStart);
        state.ReadBytes(namesPool.AsSpan());
        return namesPool;
    }

    private bool CheckEndMagic(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] == 0x01 && bytes[1] == 0x02 && bytes[2] == 0x03 && bytes[3] == 0x04);
    }

}
