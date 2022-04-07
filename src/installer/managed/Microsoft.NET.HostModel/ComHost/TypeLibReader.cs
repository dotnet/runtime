// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;

namespace Microsoft.NET.HostModel.ComHost
{
    /// <summary>
    /// Reads data from a COM Type Library file based on the official implementation in the Win32 function LoadTypeLib.
    /// We do the reading ourselves instead of calling into the OS so we don't have to worry about the type library's
    /// dependencies being discoverable on disk.
    /// </summary>
    internal class TypeLibReader
    {
        private byte[] tlbBytes;

        public TypeLibReader(byte[] tlbBytes)
        {
            this.tlbBytes = tlbBytes;
        }

        private const int OffsetOfGuidOffset = sizeof(int) * 2;
        private const int SizeOfGuidOffset = sizeof(int);

        private const int OffsetOfMajorVersion = OffsetOfGuidOffset + SizeOfGuidOffset + sizeof(int) * 2 + sizeof(ushort) * 2;
        private const int SizeOfMajorVersion = sizeof(ushort);
        private const int OffsetOfMinorVersion = OffsetOfMajorVersion + SizeOfMajorVersion;
        private const int SizeOfMinorVersion = sizeof(ushort);

        private const int OffsetOfTypeInfosCount = OffsetOfMinorVersion + SizeOfMinorVersion + sizeof(int);
        private const int SizeOfTypeInfosCount = sizeof(int);

        private const int OffsetOfTablesStart = OffsetOfTypeInfosCount + SizeOfTypeInfosCount + sizeof(int) * 12;
        private const int NumTablesToSkip = 5;
        private const int SizeOfTableHeader = sizeof(int) * 4;

        private static Guid FindGuid(ReadOnlySpan<byte> fileContents)
        {
            checked
            {
                int typelibGuidEntryOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(fileContents.Slice(OffsetOfGuidOffset));
                int infoRefsOffsetCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(fileContents.Slice(OffsetOfTypeInfosCount));
                int infoBytes = infoRefsOffsetCount * SizeOfTypeInfosCount;
                int guidTableOffset = OffsetOfTablesStart + infoBytes + SizeOfTableHeader * NumTablesToSkip;
                int fileOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(fileContents.Slice(guidTableOffset));
                return new Guid(fileContents.Slice(fileOffset + typelibGuidEntryOffset, 16).ToArray());
            }
        }

        public bool TryReadTypeLibGuidAndVersion(out Guid typelibId, out Version version)
        {
            typelibId = default;
            version = default;
            try
            {
                var span = new ReadOnlySpan<byte>(tlbBytes);
                typelibId = FindGuid(span);
                ushort majorVer = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(OffsetOfMajorVersion));
                ushort minorVer = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(OffsetOfMinorVersion));
                version = new Version(majorVer, minorVer);
                return true;
            }
            catch (System.OverflowException)
            {
                return false;
            }
            catch (System.IndexOutOfRangeException)
            {
                return false;
            }
        }
    }
}
