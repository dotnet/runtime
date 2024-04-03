// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
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
            Console.WriteLine ($"{inputPath}: namesStart = {header.NamesStart}");
            Console.WriteLine ($"{inputPath}: typeCount = {header.TypeCount}");
            Console.WriteLine ($"{inputPath}: fieldPoolCount = {header.FieldPoolCount}");
        }
        var content = ReadContent(file, headerStart, header, r.LittleEndian);
        if (Verbose) {
            Console.WriteLine ($"{inputPath}: baseline = \"{content.Baseline}\"");
        }
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

    struct HeaderDirectory {
        public uint TypesStart;
        public uint FieldPoolStart;

        public uint GlobalValuesStart;
        public uint NamesStart;

        public uint TypeCount;
        public uint FieldPoolCount;

        public uint GlobalValuesCount;
        public uint NamesPoolCount;

        public byte TypeSpecSize;
        public byte FieldSpecSize;
        public byte GlobalSpecSize;
        public byte Reserved0;
    };

    //struct BinaryBlobDataDescriptor
    //{
    //HeaderDirectory Directory;
    //uint32_t BaselineName;
    //struct TypeSpec Types[CDacBlobTypesCount];
    //struct FieldSpec FieldPool[CDacBlobFieldPoolCount];
    //struct GlobalSpec GlobalValues[CDacBlobGlobalsCount];
    //uint8_t NamesPool[sizeof(struct CDacStringPoolSizes)];
    //};


    private uint Swap(bool isLittleEndian, uint value) => (isLittleEndian == BitConverter.IsLittleEndian) ? value : BinaryPrimitives.ReverseEndianness(value);
    private ushort Swap(bool isLittleEndian, ushort value) => (isLittleEndian == BitConverter.IsLittleEndian) ? value : BinaryPrimitives.ReverseEndianness(value);
    private ulong Swap(bool isLittleEndian, ulong value) => (isLittleEndian == BitConverter.IsLittleEndian) ? value : BinaryPrimitives.ReverseEndianness(value);


    private HeaderDirectory ReadHeader(Stream stream, bool isLE)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var typesStart = Swap(isLE, reader.ReadUInt32());
        var fieldPoolStart = Swap(isLE, reader.ReadUInt32());
        var globalValuesStart = Swap(isLE, reader.ReadUInt32());
        var namesStart = Swap(isLE, reader.ReadUInt32());

        var typeCount = Swap(isLE, reader.ReadUInt32());
        var fieldPoolCount = Swap(isLE, reader.ReadUInt32());

        var globalValuesCount = Swap(isLE, reader.ReadUInt32());
        var namesPoolCount = Swap(isLE, reader.ReadUInt32());

        var typeSpecSize = reader.ReadByte();
        var fieldSpecSize = reader.ReadByte();
        var globalSpecSize = reader.ReadByte();
        var reserved0 = reader.ReadByte();

        return new HeaderDirectory {
            TypesStart = typesStart,
            FieldPoolStart = fieldPoolStart,
            GlobalValuesStart = globalValuesStart,
            NamesStart = namesStart,

            TypeCount = typeCount,
            FieldPoolCount = fieldPoolCount,

            GlobalValuesCount = globalValuesCount,
            NamesPoolCount = namesPoolCount,

            TypeSpecSize = typeSpecSize,
            FieldSpecSize = fieldSpecSize,
            GlobalSpecSize = globalSpecSize,
            Reserved0 = reserved0
        };
    }

    struct Content
    {
        public string Baseline;
        public GlobalEntry[] Globals;
    }


    struct GlobalEntry
    {
        public string Name;
        public string Type;
        public ulong Value;
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

    struct FieldEntry
    {
        public string Name;
        public string Type;
        public ushort Offset;
    }

    struct GlobalSpec
    {
        public uint NameIdx;
        public uint TypeNameIdx;
        public ulong Value;
    }

    private Content ReadContent(Stream stream, long startPos, HeaderDirectory header, bool isLE)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        // fixme: badspec - we should have the offset of baseline name too
        var baselineNameIdx = Swap(isLE, reader.ReadUInt32());
        Console.WriteLine ($"baseline Name Idx = {baselineNameIdx}");
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
            Console.WriteLine ($"TypeSpec[{i}]: NameIdx = {typeSpecs[i].NameIdx}, FieldsIdx = {typeSpecs[i].FieldsIdx}, Size = {typeSpecs[i].Size}");
            // skip padding
            if (bytesRead < header.TypeSpecSize) {
                reader.BaseStream.Seek(header.TypeSpecSize - bytesRead, SeekOrigin.Current);
            }
        }

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
            if (bytesRead < header.FieldSpecSize) {
                reader.BaseStream.Seek(header.FieldSpecSize - bytesRead, SeekOrigin.Current);
            }
        }

        GlobalSpec[] globalSpecs = new GlobalSpec[header.GlobalValuesCount];
        reader.BaseStream.Seek(startPos + (long)header.GlobalValuesStart, SeekOrigin.Begin);
        for (int i = 0; i < header.GlobalValuesCount; i++)
        {
            int bytesRead = 0;
            globalSpecs[i].NameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            globalSpecs[i].TypeNameIdx = Swap(isLE, reader.ReadUInt32());
            bytesRead += 4;
            globalSpecs[i].Value = Swap(isLE, reader.ReadUInt64());
            bytesRead += 8;
            // skip padding
            if (bytesRead < header.GlobalSpecSize) {
                reader.BaseStream.Seek(header.GlobalSpecSize - bytesRead, SeekOrigin.Current);
            }
        }

        // FIXME seek to names pool start
        byte[] namesPool = new byte[header.NamesPoolCount];
        reader.BaseStream.Seek(startPos + (long)header.NamesStart, SeekOrigin.Begin);
        int namesPoolCountRead = reader.Read(namesPool.AsSpan());
        if (namesPoolCountRead != header.NamesPoolCount) {
            throw new InvalidOperationException ($"expected to read {header.NamesPoolCount} bytes of strings");
        }

        var baseline = GetPoolString(namesPool, baselineNameIdx);
        Console.WriteLine ($"baseline Name = {baseline}");


        byte[] endMagic = new byte[4];
        reader.Read(endMagic.AsSpan());
        if (!CheckEndMagic(endMagic)) {
            throw new InvalidOperationException($"expected endMagic, got 0x{endMagic[0]:x} 0x{endMagic[1]:x} 0x{endMagic[2]:x} 0x{endMagic[3]:x}");
        }

        FieldEntry[] fields = new FieldEntry[fieldSpecs.Length];
        for (int i = 0; i < fieldSpecs.Length; i++)
        {
            if (fieldSpecs[i].NameIdx == 0)
                continue;
            fields[i].Name = GetPoolString(namesPool, fieldSpecs[i].NameIdx);
            fields[i].Type = GetPoolString(namesPool, fieldSpecs[i].TypeNameIdx);
            fields[i].Offset = fieldSpecs[i].FieldOffset;
            Console.WriteLine ($"field entry {i} Name = {fields[i].Name}, Type = {fields[i].Type}, Offset = {fields[i].Offset}");
        }

        for (int i = 0; i < typeSpecs.Length; i++)
        {
            string typeName = GetPoolString(namesPool, typeSpecs[i].NameIdx);
            var typeBuilder = _builder.AddOrUpdateType(typeName, typeSpecs[i].Size);
            uint j = typeSpecs[i].FieldsIdx; // convert byte offset to index;
            Console.WriteLine($"Type {typeName} has fields starting at index {j}");
            while (j < fields.Length && !String.IsNullOrEmpty(fields[j].Name)) {
                typeBuilder.AddOrUpdateField(fields[j].Name, fields[j].Type, fields[j].Offset);
                Console.WriteLine($"Type {typeName} has field {fields[j].Name} with offset {fields[j].Offset}");
                j++;
            }
            if (typeSpecs[i].Size != 0)
            {
                Console.WriteLine($"Type {typeName} has size {typeSpecs[i].Size}");
            }
            else
            {
                Console.WriteLine($"Type {typeName} has indeterminate size");
            }
        }

        GlobalEntry[] globals = new GlobalEntry[globalSpecs.Length];
        for (int i = 0; i < globalSpecs.Length; i++)
        {
            globals[i].Name = GetPoolString(namesPool, globalSpecs[i].NameIdx);
            globals[i].Type = GetPoolString(namesPool, globalSpecs[i].TypeNameIdx);
            globals[i].Value = globalSpecs[i].Value;
            Console.WriteLine($"Global[{i}] {globals[i].Name} has type {globals[i].Type} with value {globals[i].Value}");
        }

        return new Content
        {
            Baseline = baseline,
            Globals = globals,
        };
    }

    public string GetPoolString(ReadOnlySpan<byte> names, uint index)
    {
        var nameStart = names.Slice((int)index);
        var end = nameStart.IndexOf((byte)0); // find the first nul after index
        if (end == -1)
            throw new InvalidOperationException ("expected a nul-terminated name");
        var nameBytes = nameStart.Slice(0, end);
        return System.Text.Encoding.UTF8.GetString(nameBytes);
    }

    private bool CheckEndMagic(ReadOnlySpan<byte> bytes)
    {
        // FIXME: also endianness
        return (bytes[0] == 0x01 && bytes[1] == 0x02 && bytes[2] == 0x03 && bytes[3] == 0x04);
    }

}
