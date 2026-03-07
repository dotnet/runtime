// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System;
using Internal.Text;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// The header of a WebCIL file.
    /// </summary>
    /// <remarks>
    /// The header is a subset of the PE, COFF and CLI headers that are needed by the runtime to load managed assemblies.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct WebcilHeader
    {
        public uint Id;               // 'W' 'b' 'I' 'L'
        public ushort VersionMajor;    // 0
        public ushort VersionMinor;    // 0
        public ushort CoffSections;
        public ushort Reserved0;       // 0
        public uint PeCliHeaderRva;
        public uint PeCliHeaderSize;
        public uint PeDebugRva;
        public uint PeDebugSize;

        public static int EncodeSize()
        {
            int size = sizeof(uint) +  // 4
                    sizeof(ushort) + // 6
                    sizeof(ushort) + // 8
                    sizeof(ushort) + // 10
                    sizeof(ushort) + // 12
                    sizeof(uint) +   // 16
                    sizeof(uint) +   // 20
                    sizeof(uint) +   // 24
                    sizeof(uint);    // 28
            Debug.Assert(size == 28);
            return size;
        }

        public int Emit(Stream outputStream)
        {
            int encodeSize = EncodeSize();
            Span<byte> headerBuffer = stackalloc byte[encodeSize];
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(0, 4), Id);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(4, 2), VersionMajor);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(6, 2), VersionMinor);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(8, 2), CoffSections);
            BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.Slice(10, 2), Reserved0);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(12, 4), PeCliHeaderRva);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(16, 4), PeCliHeaderSize);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(20, 4), PeDebugRva);
            BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.Slice(24, 4), PeDebugSize);

            outputStream.Write(headerBuffer);
            return encodeSize;
        }
    }

    /// <summary>
    /// Represents a section header in a WebCIL file.
    /// </summary>
    /// <remarks>
    /// This is the WebCIL analog of <see cref="System.Reflection.PortableExecutable.SectionHeader"/>, but with fewer fields.
    /// </remarks>

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct WebcilSectionHeader
    {
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;

        public WebcilSectionHeader(uint virtualSize, uint virtualAddress, uint sizeOfRawData, uint pointerToRawData)
        {
            VirtualSize = virtualSize;
            VirtualAddress = virtualAddress;
            SizeOfRawData = sizeOfRawData;
            PointerToRawData = pointerToRawData;
        }

        public static int EncodeSize()
        {
            return sizeof(uint) +  // 4
                    sizeof(uint) + // 8
                    sizeof(uint) + // 12
                    sizeof(uint);  // 16
        }

        public int Encode(Stream outputStream)
        {
            int encodeSize = EncodeSize();
            Span<byte> header = stackalloc byte[encodeSize];

            // The Webcil spec requires little-endian encoding
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(0, 4), VirtualSize);
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4, 4), VirtualAddress);
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(8, 4), SizeOfRawData);
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(12, 4), PointerToRawData);

            outputStream.Write(header);

            return encodeSize;
        }
    }
}
