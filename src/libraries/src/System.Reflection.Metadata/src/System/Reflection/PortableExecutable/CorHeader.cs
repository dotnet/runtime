// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.PortableExecutable
{
    public sealed class CorHeader
    {
        public ushort MajorRuntimeVersion { get; }
        public ushort MinorRuntimeVersion { get; }
        public DirectoryEntry MetadataDirectory { get; }
        public CorFlags Flags { get; }
        public int EntryPointTokenOrRelativeVirtualAddress { get; }
        public DirectoryEntry ResourcesDirectory { get; }
        public DirectoryEntry StrongNameSignatureDirectory { get; }
        public DirectoryEntry CodeManagerTableDirectory { get; }
        public DirectoryEntry VtableFixupsDirectory { get; }
        public DirectoryEntry ExportAddressTableJumpsDirectory { get; }
        public DirectoryEntry ManagedNativeHeaderDirectory { get; }

        internal CorHeader(ref PEBinaryReader reader)
        {
            // byte count
            reader.ReadInt32();

            MajorRuntimeVersion = reader.ReadUInt16();
            MinorRuntimeVersion = reader.ReadUInt16();
            MetadataDirectory = new DirectoryEntry(ref reader);
            Flags = (CorFlags)reader.ReadUInt32();
            EntryPointTokenOrRelativeVirtualAddress = reader.ReadInt32();
            ResourcesDirectory = new DirectoryEntry(ref reader);
            StrongNameSignatureDirectory = new DirectoryEntry(ref reader);
            CodeManagerTableDirectory = new DirectoryEntry(ref reader);
            VtableFixupsDirectory = new DirectoryEntry(ref reader);
            ExportAddressTableJumpsDirectory = new DirectoryEntry(ref reader);
            ManagedNativeHeaderDirectory = new DirectoryEntry(ref reader);
        }
    }
}
