// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.PortableExecutable
{
    public sealed class CorHeader
    {
        public ushort MajorRuntimeVersion { get; private init; }
        public ushort MinorRuntimeVersion { get; private init; }
        public DirectoryEntry MetadataDirectory { get; private init; }
        public CorFlags Flags { get; private init; }
        public int EntryPointTokenOrRelativeVirtualAddress { get; private init; }
        public DirectoryEntry ResourcesDirectory { get; private init; }
        public DirectoryEntry StrongNameSignatureDirectory { get; private init; }
        public DirectoryEntry CodeManagerTableDirectory { get; private init; }
        public DirectoryEntry VtableFixupsDirectory { get; private init; }
        public DirectoryEntry ExportAddressTableJumpsDirectory { get; private init; }
        public DirectoryEntry ManagedNativeHeaderDirectory { get; private init; }

        private CorHeader() { }

        internal static CorHeader Create<TReader>(ref TReader reader) where TReader : IBinaryReader
        {
            // byte count
            reader.ReadInt32();

            return new CorHeader()
            {
                MajorRuntimeVersion = reader.ReadUInt16(),
                MinorRuntimeVersion = reader.ReadUInt16(),
                MetadataDirectory = DirectoryEntry.Create(ref reader),
                Flags = (CorFlags)reader.ReadUInt32(),
                EntryPointTokenOrRelativeVirtualAddress = reader.ReadInt32(),
                ResourcesDirectory = DirectoryEntry.Create(ref reader),
                StrongNameSignatureDirectory = DirectoryEntry.Create(ref reader),
                CodeManagerTableDirectory = DirectoryEntry.Create(ref reader),
                VtableFixupsDirectory = DirectoryEntry.Create(ref reader),
                ExportAddressTableJumpsDirectory = DirectoryEntry.Create(ref reader),
                ManagedNativeHeaderDirectory = DirectoryEntry.Create(ref reader),
            };
        }
    }
}
