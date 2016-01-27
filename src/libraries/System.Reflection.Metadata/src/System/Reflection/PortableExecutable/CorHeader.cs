// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.PortableExecutable
{
    public sealed class CorHeader
    {
        public ushort MajorRuntimeVersion { get; private set; }
        public ushort MinorRuntimeVersion { get; private set; }
        public DirectoryEntry MetadataDirectory { get; private set; }
        public CorFlags Flags { get; private set; }
        public int EntryPointTokenOrRelativeVirtualAddress { get; private set; }
        public DirectoryEntry ResourcesDirectory { get; private set; }
        public DirectoryEntry StrongNameSignatureDirectory { get; private set; }
        public DirectoryEntry CodeManagerTableDirectory { get; private set; }
        public DirectoryEntry VtableFixupsDirectory { get; private set; }
        public DirectoryEntry ExportAddressTableJumpsDirectory { get; private set; }
        public DirectoryEntry ManagedNativeHeaderDirectory { get; private set; }

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
