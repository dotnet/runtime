// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.PortableExecutable
{
    /// <summary>
    /// Identifies the location, size and format of a block of debug information.
    /// </summary>
    public struct DebugDirectoryEntry
    {
        /// <summary>
        /// The time and date that the debug data was created if the PE/COFF file is not deterministic,
        /// otherwise a value based on the hash of the content. 
        /// </summary>
        /// <remarks>
        /// The algorithm used to calculate this value is an implementation 
        /// detail of the tool that produced the file.
        /// </remarks>
        public uint Stamp { get; private set; }

        /// <summary>
        /// The major version number of the debug data format.
        /// </summary>
        public ushort MajorVersion { get; private set; }

        /// <summary>
        /// The minor version number of the debug data format.
        /// </summary>
        public ushort MinorVersion { get; private set; }

        /// <summary>
        /// The format of debugging information. 
        /// </summary>
        public DebugDirectoryEntryType Type { get; private set; }

        /// <summary>
        /// The size of the debug data (not including the debug directory itself).
        /// </summary>
        public int DataSize { get; private set; }

        /// <summary>
        /// The address of the debug data when loaded, relative to the image base.
        /// </summary>
        public int DataRelativeVirtualAddress { get; private set; }

        /// <summary>
        /// The file pointer to the debug data.
        /// </summary>
        public int DataPointer { get; private set; }

        internal DebugDirectoryEntry(
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
