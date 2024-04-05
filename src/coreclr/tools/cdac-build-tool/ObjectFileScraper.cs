// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class ObjectFileScraper
{
    public readonly ReadOnlyMemory<byte> MagicLE = new byte[8]{0x44, 0x41, 0x43, 0x42, 0x4C, 0x4F, 0x42, 0x00}; // "DACBLOB\0"
    public readonly ReadOnlyMemory<byte> MagicBE = new byte[8]{0x00, 0x42, 0x4F, 0x4C, 0x42, 0x43, 0x41, 0x44};

    private readonly DataDescriptorModel.Builder _builder;

    public bool Verbose {get;}
    public ObjectFileScraper(bool verbose, DataDescriptorModel.Builder builder)
    {
        Verbose = verbose;
        _builder = builder;
    }

    public async Task<bool> ScrapeInput(string inputPath, CancellationToken token)
    {
        using var file = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var r = await FindMagic(file, token);

        if (!r.Found) {
            return false;
        }
        if (Verbose) {
            Console.WriteLine ($"{inputPath}: magic at {r.Position}");
        }
        file.Seek(r.Position + (long)MagicLE.Length, SeekOrigin.Begin); // skip magic
        var headerStart = file.Position;
        var header = ReadHeader(file, r.LittleEndian);
        if (Verbose) {
            DumpHeaderDirectory(header);
        }
        var content = ReadContent(file, headerStart, header, r.LittleEndian);
        content.AddToModel(_builder);
        return true;
    }

    struct MagicResult {
        public bool Found {get; init;}
        public long Position {get; init;}
        public bool LittleEndian {get; init;}
    }

    private async Task<MagicResult> FindMagic(Stream stream, CancellationToken token)
    {
        var buf = new byte[4096];
        long pos = stream.Position;
        while (true) {
            token.ThrowIfCancellationRequested();
            int bytesRead = await stream.ReadAsync(buf, 0, buf.Length, token);
            if (bytesRead == 0)
                return new (){Found = false, Position = 0, LittleEndian = true};
            // FIXME: what if magic spans a buffer boundary
            if (FindMagic(buf, out int offset, out bool isLittleEndian)) {
                pos += (long)offset;
                return new (){Found = true, Position = pos, LittleEndian = isLittleEndian};
            }
            pos += bytesRead;
        }
    }

    private bool FindMagic(ReadOnlySpan<byte> buffer, out int offset, out bool isLittleEndian)
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


    private HeaderDirectory ReadHeader(Stream stream, bool isLE)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var baselineStart = Swap(isLE, reader.ReadUInt32());
        var typesStart = Swap(isLE, reader.ReadUInt32());

        var fieldPoolStart = Swap(isLE, reader.ReadUInt32());
        var globalLiteralValuesStart = Swap(isLE, reader.ReadUInt32());

        var globalPointersStart = Swap(isLE, reader.ReadUInt32());
        var namesStart = Swap(isLE, reader.ReadUInt32());

        var typeCount = Swap(isLE, reader.ReadUInt32());
        var fieldPoolCount = Swap(isLE, reader.ReadUInt32());

        var globalLiteralValuesCount = Swap(isLE, reader.ReadUInt32());
        var globalPointerValuesCount = Swap(isLE, reader.ReadUInt32());

        var namesPoolCount = Swap(isLE, reader.ReadUInt32());

        var typeSpecSize = reader.ReadByte();
        var fieldSpecSize = reader.ReadByte();
        var globalLiteralSpecSize = reader.ReadByte();
        var globalPointerSpecSize = reader.ReadByte();

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

    private Content ReadContent(Stream stream, long startPos, HeaderDirectory header, bool isLE)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        reader.BaseStream.Seek(startPos + header.BaselineStart, SeekOrigin.Begin);
        var baselineNameIdx = Swap(isLE, reader.ReadUInt32());
        Console.WriteLine($"baseline Name Idx = {baselineNameIdx}");

        TypeSpec[] typeSpecs = ReadTypeSpecs(reader, startPos, header, isLE);
        FieldSpec[] fieldSpecs = ReadFieldSpecs(reader, startPos, header, isLE);
        GlobalLiteralSpec[] globalLiteralSpecs = ReadGlobalLiteralSpecs(reader, startPos, header, isLE);
        GlobalPointerSpec[] globalPointerSpecs = ReadGlobalPointerSpecs(reader, startPos, header, isLE);
        byte[] namesPool = ReadNamesPool(reader, startPos, header);

        byte[] endMagic = new byte[4];
        reader.Read(endMagic.AsSpan());
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

    private TypeSpec[] ReadTypeSpecs(BinaryReader reader, long startPos, HeaderDirectory header, bool isLE)
    {
        TypeSpec[] typeSpecs = new TypeSpec[header.TypeCount];

        reader.BaseStream.Seek(startPos + (long)header.TypesStart, SeekOrigin.Begin);
        for (int i = 0; i < header.TypeCount; i++)
        {
            int bytesRead = 0;
            typeSpecs[i].NameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            typeSpecs[i].FieldsIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            typeSpecs[i].Size = Swap(isLE, reader.ReadUInt16());
            bytesRead += 2;
            Console.WriteLine($"TypeSpec[{i}]: NameIdx = {typeSpecs[i].NameIdx}, FieldsIdx = {typeSpecs[i].FieldsIdx}, Size = {typeSpecs[i].Size}");
            // skip padding
            if (bytesRead < header.TypeSpecSize)
            {
                reader.BaseStream.Seek(header.TypeSpecSize - bytesRead, SeekOrigin.Current);
            }
        }
        return typeSpecs;
    }

    private FieldSpec[] ReadFieldSpecs(BinaryReader reader, long startPos, HeaderDirectory header, bool isLE)
    {
        reader.BaseStream.Seek(startPos + (long)header.FieldPoolStart, SeekOrigin.Begin);
        FieldSpec[] fieldSpecs = new FieldSpec[header.FieldPoolCount];
        for (int i = 0; i < header.FieldPoolCount; i++)
        {
            int bytesRead = 0;
            fieldSpecs[i].NameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            fieldSpecs[i].TypeNameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            fieldSpecs[i].FieldOffset = Swap(isLE, reader.ReadUInt16());
            bytesRead += 2;
            // skip padding
            if (bytesRead < header.FieldSpecSize)
            {
                reader.BaseStream.Seek(header.FieldSpecSize - bytesRead, SeekOrigin.Current);
            }
        }
        return fieldSpecs;
    }

    private GlobalLiteralSpec[] ReadGlobalLiteralSpecs(BinaryReader reader, long startPos, HeaderDirectory header, bool isLE)
    {
        GlobalLiteralSpec[] globalSpecs = new GlobalLiteralSpec[header.GlobalLiteralValuesCount];
        reader.BaseStream.Seek(startPos + (long)header.GlobalLiteralValuesStart, SeekOrigin.Begin);
        for (int i = 0; i < header.GlobalLiteralValuesCount; i++)
        {
            int bytesRead = 0;
            globalSpecs[i].NameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            globalSpecs[i].TypeNameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            globalSpecs[i].Value = Swap(isLE, reader.ReadUInt64());
            bytesRead += 8;
            // skip padding
            if (bytesRead < header.GlobalLiteralSpecSize)
            {
                reader.BaseStream.Seek(header.GlobalLiteralSpecSize - bytesRead, SeekOrigin.Current);
            }
        }
        return globalSpecs;
    }

    private GlobalPointerSpec[] ReadGlobalPointerSpecs(BinaryReader reader, long startPos, HeaderDirectory header, bool isLE)
    {
        GlobalPointerSpec[] globalSpecs = new GlobalPointerSpec[header.GlobalPointerValuesCount];
        reader.BaseStream.Seek(startPos + (long)header.GlobalPointersStart, SeekOrigin.Begin);
        for (int i = 0; i < header.GlobalPointerValuesCount; i++)
        {
            int bytesRead = 0;
            globalSpecs[i].NameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            globalSpecs[i].AuxDataIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            // skip padding
            if (bytesRead < header.GlobalPointerSpecSize)
            {
                reader.BaseStream.Seek(header.GlobalPointerSpecSize - bytesRead, SeekOrigin.Current);
            }
        }
        return globalSpecs;
    }

    private byte[] ReadNamesPool(BinaryReader reader, long startPos, HeaderDirectory header)
    {
        byte[] namesPool = new byte[header.NamesPoolCount];
        reader.BaseStream.Seek(startPos + (long)header.NamesStart, SeekOrigin.Begin);
        int namesPoolCountRead = reader.Read(namesPool.AsSpan());
        if (namesPoolCountRead != header.NamesPoolCount) {
            throw new InvalidOperationException ($"expected to read {header.NamesPoolCount} bytes of strings");
        }
        return namesPool;
    }

    private bool CheckEndMagic(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] == 0x01 && bytes[1] == 0x02 && bytes[2] == 0x03 && bytes[3] == 0x04);
    }

}
