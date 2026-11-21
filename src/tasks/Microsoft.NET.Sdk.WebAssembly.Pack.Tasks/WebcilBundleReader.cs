// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.NET.Sdk.WebAssembly;

internal sealed class WebcilBundleReader
{
    private const int HeaderSize = 20;
    private const int EntrySize = 16;
    private const uint BundleMagic = 0x42496257;
    private const uint WebcilMagic = 0x4c496257;
    private static readonly UTF8Encoding s_utf8 = new(false, true);

    private readonly Dictionary<string, Guid> _mvids = new(StringComparer.OrdinalIgnoreCase);

    public WebcilBundleReader(string path)
    {
        byte[] module = File.ReadAllBytes(path);
        (int payloadStart, int payloadSize) = FindPayload(module);
        ReadOnlySpan<byte> payload = module.AsSpan(payloadStart, payloadSize);
        if (payload.Length < HeaderSize || ReadUInt32(payload, 0) != BundleMagic ||
            ReadUInt16(payload, 4) != 1 || ReadUInt16(payload, 6) != 0)
        {
            throw new BadImageFormatException("Invalid Webcil bundle header.");
        }

        uint count = ReadUInt32(payload, 8);
        uint stringsStart = ReadUInt32(payload, 12);
        uint stringsSize = ReadUInt32(payload, 16);
        if (count > (uint)((payload.Length - HeaderSize) / EntrySize) ||
            stringsStart < HeaderSize + (long)count * EntrySize ||
            stringsStart > payload.Length || stringsSize > payload.Length - stringsStart)
        {
            throw new BadImageFormatException("Invalid Webcil bundle directory.");
        }

        uint stringsEnd = stringsStart + stringsSize;
        var imageRanges = new List<(uint Offset, uint Size)>();
        for (int i = 0; i < count; i++)
        {
            int entryOffset = HeaderSize + i * EntrySize;
            uint nameOffset = ReadUInt32(payload, entryOffset);
            uint nameSize = ReadUInt32(payload, entryOffset + 4);
            uint imageOffset = ReadUInt32(payload, entryOffset + 8);
            uint imageSize = ReadUInt32(payload, entryOffset + 12);
            if (nameSize == 0 || nameOffset > stringsSize || nameSize > stringsSize - nameOffset ||
                imageOffset % 16 != 0 || imageOffset < stringsEnd || imageOffset > payload.Length ||
                imageSize < 32 || imageSize > payload.Length - imageOffset ||
                ReadUInt32(payload, (int)imageOffset) != WebcilMagic ||
                ReadUInt16(payload, (int)imageOffset + 4) != 1 ||
                ReadUInt16(payload, (int)imageOffset + 6) != 0 ||
                ReadUInt32(payload, (int)imageOffset + 28) != 0)
            {
                throw new BadImageFormatException("Invalid Webcil bundle entry.");
            }

            string name = s_utf8.GetString(module, checked(payloadStart + (int)(stringsStart + nameOffset)), (int)nameSize);
            if (!name.EndsWith(".dll", StringComparison.Ordinal) || name.IndexOf('\\') >= 0 ||
                name.Any(character => character < ' '))
            {
                throw new BadImageFormatException($"Invalid Webcil bundle entry name '{name}'.");
            }

            foreach (string part in name.Split('/'))
            {
                if (part is "" or "." or "..")
                {
                    throw new BadImageFormatException($"Invalid Webcil bundle entry name '{name}'.");
                }
            }

            byte[] imageBytes = payload.Slice((int)imageOffset, (int)imageSize).ToArray();
            using var image = new MemoryStream(imageBytes, writable: false);
            using var reader = new WebcilReader(image);
            MetadataReader metadata = reader.GetMetadataReader();
            if (!metadata.IsAssembly)
            {
                throw new BadImageFormatException($"Webcil bundle entry '{name}' is not an assembly.");
            }

            Guid mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
            string simpleFileName = Path.GetFileName(name);
            if (_mvids.ContainsKey(simpleFileName))
            {
                throw new BadImageFormatException($"Duplicate Webcil bundle assembly name '{simpleFileName}'.");
            }
            _mvids.Add(simpleFileName, mvid);
            imageRanges.Add((imageOffset, imageSize));
        }

        imageRanges.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        for (int i = 1; i < imageRanges.Count; i++)
        {
            if (imageRanges[i - 1].Size > imageRanges[i].Offset - imageRanges[i - 1].Offset)
            {
                throw new BadImageFormatException("Overlapping Webcil bundle images.");
            }
        }
    }

    public bool TryGetMvid(string fileName, out Guid mvid) => _mvids.TryGetValue(fileName, out mvid);

    public bool ContainsMatchingAssembly(string path)
    {
        if (!TryGetMvid(Path.GetFileName(path), out Guid bundleMvid))
        {
            return false;
        }

        Guid? candidateMvid = ConvertDllsToWebcil.TryReadMvid(path);
        if (candidateMvid is null || candidateMvid.Value != bundleMvid)
        {
            throw new BadImageFormatException($"The IL assembly '{path}' does not match the Webcil component bundle (bundle MVID {bundleMvid}, assembly MVID {candidateMvid}).");
        }
        return true;
    }

    private static (int Start, int Size) FindPayload(byte[] module)
    {
        ReadOnlySpan<byte> data = module;
        if (data.Length < 8 || ReadUInt32(data, 0) != 0x6d736100 || ReadUInt32(data, 4) != 1)
        {
            throw new BadImageFormatException("The Webcil bundle is not a WebAssembly module.");
        }

        int position = 8;
        while (position < data.Length)
        {
            byte section = data[position++];
            uint length = ReadULEB(data, ref position, data.Length);
            if (length > data.Length - position)
            {
                throw new BadImageFormatException("Invalid WebAssembly section length.");
            }
            int end = position + (int)length;
            if (section != 11)
            {
                position = end;
                continue;
            }

            if (ReadULEB(data, ref position, end) < 2 || ReadULEB(data, ref position, end) != 1)
            {
                throw new BadImageFormatException("Invalid Webcil bundle data segments.");
            }

            uint sizesLength = ReadULEB(data, ref position, end);
            if (sizesLength < 4 || sizesLength > end - position)
            {
                throw new BadImageFormatException("Invalid Webcil bundle size segment.");
            }
            uint payloadSize = ReadUInt32(data, position);
            for (int i = position + 4; i < position + sizesLength; i++)
            {
                if (data[i] != 0)
                {
                    throw new BadImageFormatException("Webcil bundle requires a zero-sized function table.");
                }
            }
            position += (int)sizesLength;
            if (ReadULEB(data, ref position, end) != 0 || position >= end || data[position++] != 0x23 ||
                ReadULEB(data, ref position, end) != 0 || position >= end || data[position++] != 0x0b)
            {
                throw new BadImageFormatException("Webcil bundle payload must be an active data segment.");
            }
            uint segmentSize = ReadULEB(data, ref position, end);
            if (segmentSize != payloadSize || segmentSize > end - position)
            {
                throw new BadImageFormatException("Invalid Webcil bundle payload size.");
            }
            return (position, (int)segmentSize);
        }

        throw new BadImageFormatException("Webcil bundle has no data section.");
    }

    private static uint ReadULEB(ReadOnlySpan<byte> data, ref int position, int end)
    {
        uint value = 0;
        for (int shift = 0; shift < 35; shift += 7)
        {
            if (position >= end)
            {
                throw new BadImageFormatException("Truncated WebAssembly integer.");
            }
            byte b = data[position++];
            if (shift == 28 && (b & 0xf0) != 0)
            {
                throw new BadImageFormatException("Invalid WebAssembly integer.");
            }
            value |= (uint)(b & 0x7f) << shift;
            if ((b & 0x80) == 0)
            {
                return value;
            }
        }

        throw new BadImageFormatException("Invalid WebAssembly integer.");
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, sizeof(uint)));

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, sizeof(ushort)));
}
