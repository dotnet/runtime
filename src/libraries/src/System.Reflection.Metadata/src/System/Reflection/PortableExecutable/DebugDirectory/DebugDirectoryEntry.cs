// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable
{
    /// <summary>
    /// Identifies the location, size and format of a block of debug information.
    /// </summary>
    public readonly struct DebugDirectoryEntry
    {
        internal const int Size =
            sizeof(uint) +   // Characteristics
            sizeof(uint) +   // TimeDataStamp
            sizeof(uint) +   // Version
            sizeof(uint) +   // Type
            sizeof(uint) +   // SizeOfData
            sizeof(uint) +   // AddressOfRawData
            sizeof(uint);    // PointerToRawData

        /// <summary>
        /// The time and date that the debug data was created if the PE/COFF file is not deterministic,
        /// otherwise a value based on the hash of the content. 
        /// </summary>
        /// <remarks>
        /// The algorithm used to calculate this value is an implementation 
        /// detail of the tool that produced the file.
        /// </remarks>
        public uint Stamp { get; }

        /// <summary>
        /// The major version number of the debug data format.
        /// </summary>
        public ushort MajorVersion { get; }

        /// <summary>
        /// The minor version number of the debug data format.
        /// </summary>
        public ushort MinorVersion { get; }

        /// <summary>
        /// The format of debugging information. 
        /// </summary>
        public DebugDirectoryEntryType Type { get; }

        /// <summary>
        /// The size of the debug data (not including the debug directory itself).
        /// </summary>
        public int DataSize { get; }

        /// <summary>
        /// The address of the debug data when loaded, relative to the image base.
        /// </summary>
        public int DataRelativeVirtualAddress { get; }

        /// <summary>
        /// The file pointer to the debug data.
        /// </summary>
        public int DataPointer { get; }

        /// <summary>
        /// True if the entry is a <see cref="DebugDirectoryEntryType.CodeView"/> entry pointing to a Portable PDB.
        /// </summary>
        public bool IsPortableCodeView => MinorVersion == PortablePdbVersions.PortableCodeViewVersionMagic;

        public DebugDirectoryEntry(
            uint stamp,
            ushort majorVersion,
            ushort minorVersion,
            DebugDirectoryEntryType type,
            int dataSize,
            int dataRelativeVirtualAddress,
            int dataPointer)
        {
            Stamp = stamp;
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            Type = type;
            DataSize = dataSize;
            DataRelativeVirtualAddress = dataRelativeVirtualAddress;
            DataPointer = dataPointer;
        }
    }
}
