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
        var bytes = await File.ReadAllBytesAsync(inputPath, token).ConfigureAwait(false);
        if (!ScraperState.CreateScraperState(bytes, out var state))
        {
            return false;
        }
        if (Verbose)
        {
            Console.WriteLine($"Magic starts at 0x{state.MagicStart:x8} in {inputPath}");
        }
        var header = ReadHeader(state);
        if (Verbose)
        {
            DumpHeaderDirectory(header);
        }
        var content = ReadContent(state, header);
        content.AddToModel(_builder);
        if (Verbose)
        {
            Console.WriteLine($"\nFinished scraping content from {inputPath}");
        }
        return true;
    }

    private sealed class ScraperState
    {
        public ReadOnlyMemory<byte> Data { get; }
        public bool LittleEndian { get; }
        private long _position;

        // expect MagicLE and MagicBE to have the same length
        public long MagicStart => HeaderStart - MagicLE.Length;
        public long HeaderStart { get; }

        private ScraperState(ReadOnlyMemory<byte> data, bool isLittleEndian, long headerStart)
        {
            Data = data;
            LittleEndian = isLittleEndian;
            HeaderStart = headerStart;
            _position = headerStart;
        }

        public static bool CreateScraperState(ReadOnlyMemory<byte> bytes, [NotNullWhen(true)] out ScraperState? scraperState)
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

        public ReadOnlySpan<byte> GetBytes(long offset, int length) => Data.Span.Slice((int)offset, length);

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
            GetBytes(_position, buffer.Length).CopyTo(buffer);
            _position += buffer.Length;
        }

        public void Skip(int count)
        {
            _position += count;
        }
    }

    // see typedef Directory in data-descriptor-blob.md
    private struct HeaderDirectory
    {
        public uint FlagsAndBaselineStart;
        public uint TypesStart;

        public uint FieldsPoolStart;
        public uint GlobalLiteralValuesStart;

        public uint GlobalPointersStart;
        public uint NamesStart;

        public uint TypesCount;
        public uint FieldsPoolCount;

        public uint GlobalLiteralValuesCount;
        public uint GlobalPointerValuesCount;

        public uint NamesPoolCount;

        public byte TypeSpecSize;
        public byte FieldSpecSize;
        public byte GlobalLiteralSpecSize;
        public byte GlobalPointerSpecSize;
    };

    private static void DumpHeaderDirectory(HeaderDirectory headerDirectory)
    {
        Console.WriteLine($"""
        Scaped Header Directory:

        Baseline Start        = 0x{headerDirectory.FlagsAndBaselineStart:x8}
        Types Start           = 0x{headerDirectory.TypesStart:x8}
        Fields Pool Start     = 0x{headerDirectory.FieldsPoolStart:x8}
        Global Literals Start = 0x{headerDirectory.GlobalLiteralValuesStart:x8}
        Global Pointers Start = 0x{headerDirectory.GlobalPointersStart:x8}
        Names Pool Start      = 0x{headerDirectory.NamesStart:x8}

        Types Count                 = {headerDirectory.TypesCount}
        Fields Pool Count           = {headerDirectory.FieldsPoolCount}
        Global Literal Values Count = {headerDirectory.GlobalLiteralValuesCount}
        Global Pointer Values Count = {headerDirectory.GlobalPointerValuesCount}
        Names Pool Count            = {headerDirectory.NamesPoolCount}

        """);
    }

    private static HeaderDirectory ReadHeader(ScraperState state)
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
            FlagsAndBaselineStart = baselineStart,
            TypesStart = typesStart,
            FieldsPoolStart = fieldPoolStart,
            GlobalLiteralValuesStart = globalLiteralValuesStart,
            GlobalPointersStart = globalPointersStart,
            NamesStart = namesStart,

            TypesCount = typeCount,
            FieldsPoolCount = fieldPoolCount,

            GlobalLiteralValuesCount = globalLiteralValuesCount,
            GlobalPointerValuesCount = globalPointerValuesCount,

            NamesPoolCount = namesPoolCount,

            TypeSpecSize = typeSpecSize,
            FieldSpecSize = fieldSpecSize,
            GlobalLiteralSpecSize = globalLiteralSpecSize,
            GlobalPointerSpecSize = globalPointerSpecSize,
        };
    }

    private struct TypeSpec
    {
        public uint NameIdx;
        public uint FieldsIdx;
        public ushort? Size;
    }

    private struct FieldSpec
    {
        public uint NameIdx;
        public uint TypeNameIdx;
        public ushort FieldOffset;
    }

    // Like a FieldSpec but with names resolved
    private struct FieldEntry
    {
        public string Name;
        public string Type;
        public ushort Offset;
    }

    private struct GlobalLiteralSpec
    {
        public uint NameIdx;
        public uint TypeNameIdx;
        public ulong Value;
    }

    private struct GlobalPointerSpec
    {
        public uint NameIdx;
        public uint AuxDataIdx;
    }

    private sealed class Content
    {
        public required bool Verbose {get; init; }
        public required uint PlatformFlags { get; init; }
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
            WriteVerbose("\nAdding scraped content to model");
            builder.PlatformFlags = PlatformFlags;
            string baseline = GetPoolString(Baseline);
            WriteVerbose($"Baseline Name = {baseline}");
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
                WriteVerbose($"Type {typeName} has fields starting at index {j}");
                while (j < fields.Length && !string.IsNullOrEmpty(fields[j].Name))
                {
                    typeBuilder.AddOrUpdateField(fields[j].Name, fields[j].Type, fields[j].Offset);
                    WriteVerbose($"Type {typeName} has field {fields[j].Name} with offset {fields[j].Offset}");
                    j++;
                }
                if (typeSpec.Size is not null)
                {
                    WriteVerbose($"Type {typeName} has size {typeSpec.Size}");
                }
                else
                {
                    WriteVerbose($"Type {typeName} has indeterminate size");
                }
            }

            foreach (var globalSpec in GlobaLiteralSpecs)
            {
                var globalName = GetPoolString(globalSpec.NameIdx);
                var globalType = GetPoolString(globalSpec.TypeNameIdx);
                var globalValue = DataDescriptorModel.GlobalValue.MakeDirect(globalSpec.Value);
                builder.AddOrUpdateGlobal(globalName, globalType, globalValue);
                WriteVerbose($"Global {globalName} has type {globalType} with value {globalValue}");
            }

            foreach (var globalPointer in GlobalPointerSpecs)
            {
                var globalName = GetPoolString(globalPointer.NameIdx);
                var auxDataIdx = globalPointer.AuxDataIdx;
                var globalValue = DataDescriptorModel.GlobalValue.MakeIndirect(auxDataIdx);
                builder.AddOrUpdateGlobal(globalName, DataDescriptorModel.PointerTypeName, globalValue);
                WriteVerbose($"Global pointer {globalName} has index {globalValue}");
            }
        }

        private void WriteVerbose(string msg)
        {
            if (Verbose)
                Console.WriteLine(msg);
        }
    }

    private Content ReadContent(ScraperState state, HeaderDirectory header)
    {
        WriteVerbose("\nReading scraped content");
        state.ResetPosition(state.HeaderStart + header.FlagsAndBaselineStart);
        var platformFlags = state.ReadUInt32();
        var baselineNameIdx = state.ReadUInt32();
        WriteVerbose($"flags = 0x{platformFlags:x8}, baseline Name Idx = {baselineNameIdx}");

        TypeSpec[] typeSpecs = ReadTypeSpecs(state, header);
        FieldSpec[] fieldSpecs = ReadFieldSpecs(state, header);
        GlobalLiteralSpec[] globalLiteralSpecs = ReadGlobalLiteralSpecs(state, header);
        GlobalPointerSpec[] globalPointerSpecs = ReadGlobalPointerSpecs(state, header);
        byte[] namesPool = ReadNamesPool(state, header);

        byte[] endMagic = new byte[4];
        state.ReadBytes(endMagic.AsSpan());
        if (!CheckEndMagic(endMagic))
        {
            if (endMagic.All(b => b == 0))
            {
                throw new InvalidOperationException("expected endMagic, got all zeros. Did you add something to the data descriptor that can't be initialized at compile time?");
            }
            throw new InvalidOperationException($"expected endMagic, got 0x{endMagic[0]:x} 0x{endMagic[1]:x} 0x{endMagic[2]:x} 0x{endMagic[3]:x}");
        }
        else
        {
            WriteVerbose("\nFound correct endMagic at end of content");
        }
        return new Content
        {
            Verbose = Verbose,
            PlatformFlags = platformFlags,
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
        TypeSpec[] typeSpecs = new TypeSpec[header.TypesCount];

        state.ResetPosition(state.HeaderStart + (long)header.TypesStart);
        for (int i = 0; i < header.TypesCount; i++)
        {
            int bytesRead = 0;
            typeSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            typeSpecs[i].FieldsIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            ushort size = state.ReadUInt16();
            bytesRead += sizeof(ushort);
            if (size != 0)
            {
                typeSpecs[i].Size = size;
            }
            WriteVerbose($"TypeSpec[{i}]: NameIdx = {typeSpecs[i].NameIdx}, FieldsIdx = {typeSpecs[i].FieldsIdx}, Size = {typeSpecs[i].Size}");
            // skip padding
            if (bytesRead < header.TypeSpecSize)
            {
                state.Skip(header.TypeSpecSize - bytesRead);
            }
        }
        return typeSpecs;
    }

    private static FieldSpec[] ReadFieldSpecs(ScraperState state, HeaderDirectory header)
    {
        state.ResetPosition(state.HeaderStart + (long)header.FieldsPoolStart);
        FieldSpec[] fieldSpecs = new FieldSpec[header.FieldsPoolCount];
        for (int i = 0; i < header.FieldsPoolCount; i++)
        {
            int bytesRead = 0;
            fieldSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            fieldSpecs[i].TypeNameIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            fieldSpecs[i].FieldOffset = state.ReadUInt16();
            bytesRead += sizeof(ushort);
            // skip padding
            if (bytesRead < header.FieldSpecSize)
            {
                state.Skip(header.FieldSpecSize - bytesRead);
            }
        }
        return fieldSpecs;
    }

    private static GlobalLiteralSpec[] ReadGlobalLiteralSpecs(ScraperState state, HeaderDirectory header)
    {
        GlobalLiteralSpec[] globalSpecs = new GlobalLiteralSpec[header.GlobalLiteralValuesCount];
        state.ResetPosition(state.HeaderStart + (long)header.GlobalLiteralValuesStart);
        for (int i = 0; i < header.GlobalLiteralValuesCount; i++)
        {
            int bytesRead = 0;
            globalSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            globalSpecs[i].TypeNameIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            globalSpecs[i].Value = state.ReadUInt64();
            bytesRead += sizeof(ulong);
            // skip padding
            if (bytesRead < header.GlobalLiteralSpecSize)
            {
                state.Skip(header.GlobalLiteralSpecSize - bytesRead);
            }
        }
        return globalSpecs;
    }

    private static GlobalPointerSpec[] ReadGlobalPointerSpecs(ScraperState state, HeaderDirectory header)
    {
        GlobalPointerSpec[] globalSpecs = new GlobalPointerSpec[header.GlobalPointerValuesCount];
        state.ResetPosition(state.HeaderStart + (long)header.GlobalPointersStart);
        for (int i = 0; i < header.GlobalPointerValuesCount; i++)
        {
            int bytesRead = 0;
            globalSpecs[i].NameIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            globalSpecs[i].AuxDataIdx = state.ReadUInt32();
            bytesRead += sizeof(uint);
            // skip padding
            if (bytesRead < header.GlobalPointerSpecSize)
            {
                state.Skip(header.GlobalPointerSpecSize - bytesRead);
            }
        }
        return globalSpecs;
    }

    private static byte[] ReadNamesPool(ScraperState state, HeaderDirectory header)
    {
        byte[] namesPool = new byte[header.NamesPoolCount];
        state.ResetPosition(state.HeaderStart + (long)header.NamesStart);
        state.ReadBytes(namesPool.AsSpan());
        return namesPool;
    }

    private static bool CheckEndMagic(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] == 0x01 && bytes[1] == 0x02 && bytes[2] == 0x03 && bytes[3] == 0x04);
    }

    private void WriteVerbose(string msg)
    {
        if (Verbose)
            Console.WriteLine(msg);
    }
}
