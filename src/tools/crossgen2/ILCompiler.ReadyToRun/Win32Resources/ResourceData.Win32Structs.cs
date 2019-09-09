// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Reflection.Metadata;

namespace ILCompiler.Win32Resources
{
    public unsafe partial class ResourceData
    {
        private struct IMAGE_RESOURCE_DIRECTORY
        {
            public IMAGE_RESOURCE_DIRECTORY(ref BlobReader blobReader)
            {
                Characteristics = blobReader.ReadUInt32();
                TimeDateStamp = blobReader.ReadUInt32();
                MajorVersion = blobReader.ReadUInt16();
                MinorVersion = blobReader.ReadUInt16();
                NumberOfNamedEntries = blobReader.ReadUInt16();
                NumberOfIdEntries = blobReader.ReadUInt16();
            }

            public readonly uint Characteristics;
            public readonly uint TimeDateStamp;
            public readonly ushort MajorVersion;
            public readonly ushort MinorVersion;
            public readonly ushort NumberOfNamedEntries;
            public readonly ushort NumberOfIdEntries;
        }

        private struct IMAGE_RESOURCE_DIRECTORY_ENTRY
        {
            public IMAGE_RESOURCE_DIRECTORY_ENTRY(ref BlobReader blobReader)
            {
                Name = blobReader.ReadUInt32();
                OffsetToData = blobReader.ReadUInt32();
            }

            public readonly uint Name;
            public readonly uint OffsetToData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_RESOURCE_DATA_ENTRY
        {
            public IMAGE_RESOURCE_DATA_ENTRY(ref BlobReader blobReader)
            {
                OffsetToData = blobReader.ReadUInt32();
                Size = blobReader.ReadUInt32();
                CodePage = blobReader.ReadUInt32();
                Reserved = blobReader.ReadUInt32();
            }

            public uint OffsetToData;
            public uint Size;
            private uint CodePage;
            private uint Reserved;
        }
    }
}
