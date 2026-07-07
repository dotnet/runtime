// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.ObjectWriter
{
    internal enum WebcilVersion
    {
        Version0 = 0,
        Version1 = 1
    }
    /// <summary>
    /// Provides encoding helpers for writing Webcil headers to a stream.
    /// </summary>
    internal static class WebcilEncoder
    {
        public static int HeaderEncodeSize(WebcilVersion version)
        {
            int size = sizeof(uint) +    // Id: 4
                    sizeof(ushort) +     // VersionMajor: 6
                    sizeof(ushort) +     // VersionMinor: 8
                    sizeof(ushort) +     // CoffSections: 10
                    sizeof(ushort) +     // Reserved0: 12
                    sizeof(uint) +       // PeCliHeaderRva: 16
                    sizeof(uint) +       // PeCliHeaderSize: 20
                    sizeof(uint) +       // PeDebugRva: 24
                    sizeof(uint);        // PeDebugSize: 28
            Debug.Assert(size == 28);
            if (version >= WebcilVersion.Version1)
            {
                size += sizeof(uint); // TableBase: 32
            }
            return size;
        }

        public static int TableBaseOffset => 28;

        public static int EmitHeader(in WebcilHeader header, Stream outputStream)
        {
            int encodeSize = HeaderEncodeSize((WebcilVersion)header.VersionMajor);
            Span<byte> headerBuffer = stackalloc byte[encodeSize];
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(0, 4), header.Id);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(4, 2), header.VersionMajor);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(6, 2), header.VersionMinor);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(8, 2), header.CoffSections);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(10, 2), header.Reserved0);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(12, 4), header.PeCliHeaderRva);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(16, 4), header.PeCliHeaderSize);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(20, 4), header.PeDebugRva);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(24, 4), header.PeDebugSize);
    
            if ((WebcilVersion)header.VersionMajor >= WebcilVersion.Version1)
            {
                // TableBase is always written as 0 in the file, as the spec requires it to be filled
                // in by the getWebcilPayload function at runtime.
                BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(28, 4), 0); 
            }

            outputStream.Write(headerBuffer);
            return encodeSize;
        }

        public static int SectionHeaderEncodeSize()
        {
            return sizeof(uint) +                // VirtualSize: 4
                    sizeof(uint) +                // VirtualAddress: 8
                    sizeof(uint) +                // SizeOfRawData: 12
                    sizeof(uint);                 // PointerToRawData: 16
        }

        public static int EncodeSectionHeader(in WebcilSectionHeader sectionHeader, Stream outputStream)
        {
            int encodeSize = SectionHeaderEncodeSize();
            Span<byte> header = stackalloc byte[encodeSize];

            // The Webcil spec requires little-endian encoding
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(0, 4), sectionHeader.VirtualSize);
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4, 4), sectionHeader.VirtualAddress);
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(8, 4), sectionHeader.SizeOfRawData);
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(12, 4), sectionHeader.PointerToRawData);

            outputStream.Write(header);

            return encodeSize;
        }
    }
}
