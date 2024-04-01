// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class ObjectFileScraper
{
    public readonly ReadOnlyMemory<byte> MagicLE = new byte[8]{0x44, 0x41, 0x43, 0x42, 0x4C, 0x4F, 0x42, 0x00}; // "DACBLOB\0"
    public readonly ReadOnlyMemory<byte> MagicBE = new byte[8]{0x00, 0x42, 0x4F, 0x4C, 0x42, 0x43, 0x41, 0x44};

    public bool Verbose {get;}
    public ObjectFileScraper(bool verbose) {
        Verbose = verbose;
    }

    public async Task<bool> ScrapeInput(string inputPath, CancellationToken token)
    {
        using var file = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var r = await FindMagic(file, token);

        if (!r.Found){
            return false;
        }
        if (Verbose) {
            Console.WriteLine ($"{inputPath}: magic at {r.Position}");
        }
        file.Seek(r.Position + (long)MagicLE.Length, SeekOrigin.Begin); // skip magic
        var header = ReadHeader(file, r.LittleEndian);
        if (Verbose) {
            Console.WriteLine ($"{inputPath}: namesStart = {header.NamesStart}");
            Console.WriteLine ($"{inputPath}: typeCount = {header.TypeCount}");
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
        

    private HeaderDirectory ReadHeader(Stream stream, bool isLE)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var typesStart = Swap(isLE, reader.ReadUInt32());
        var fieldPoolStart = Swap(isLE, reader.ReadUInt32());
        var globalValuesStart = Swap(isLE, reader.ReadUInt32());
        var namesStart = Swap(isLE, reader.ReadUInt32());

        var typeCount = Swap(isLE, reader.ReadUInt32());
        var fieldPoolCount = Swap(isLE, reader.ReadUInt32());

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

            NamesPoolCount = namesPoolCount,

            TypeSpecSize = typeSpecSize,
            FieldSpecSize = fieldSpecSize,
            GlobalSpecSize = globalSpecSize,
            Reserved0 = reserved0
        };
    }

}
