// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection.Metadata;

#if !HOST_MODEL
using ILCompiler.DependencyAnalysis;
#endif

#if HOST_MODEL
namespace Microsoft.NET.HostModel.Win32Resources
#else
namespace ILCompiler.Win32Resources
#endif
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

            public static void Write(ref ObjectDataBuilder builder, ushort namedEntries, ushort idEntries)
            {
                builder.EmitUInt(0); // Characteristics
                builder.EmitUInt(0); // TimeDateStamp
                builder.EmitUShort(4); // MajorVersion
                builder.EmitUShort(0); // MinorVersion
                builder.EmitUShort(namedEntries);
                builder.EmitUShort(idEntries);
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

            public static ObjectDataBuilder.Reservation Write(ref ObjectDataBuilder dataBuilder, string name, SortedDictionary<string, List<ObjectDataBuilder.Reservation>> nameTable)
            {
                List<ObjectDataBuilder.Reservation> relatedNameReferences;
                if (!nameTable.TryGetValue(name, out relatedNameReferences))
                {
                    relatedNameReferences = new List<ObjectDataBuilder.Reservation>();
                    nameTable[name] = relatedNameReferences;
                }
                relatedNameReferences.Add(dataBuilder.ReserveInt());
                return dataBuilder.ReserveInt();
            }

            public static ObjectDataBuilder.Reservation Write(ref ObjectDataBuilder dataBuilder, ushort id)
            {
                dataBuilder.EmitInt(id);
                return dataBuilder.ReserveInt();
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
#if HOST_MODEL
            public static void Write(ref ObjectDataBuilder dataBuilder, int sectionBase, int offsetFromSymbol, int sizeOfData)
#else
            public static void Write(ref ObjectDataBuilder dataBuilder, ISymbolNode node, int offsetFromSymbol, int sizeOfData)
#endif
            {
#if HOST_MODEL
                dataBuilder.EmitInt(sectionBase + offsetFromSymbol);
#else
                dataBuilder.EmitReloc(node,
#if READYTORUN
                    RelocType.IMAGE_REL_BASED_ADDR32NB,
#else
                    RelocType.IMAGE_REL_BASED_ABSOLUTE,
#endif
                    offsetFromSymbol);
#endif
                dataBuilder.EmitInt(sizeOfData);
                dataBuilder.EmitInt(1252);  // CODEPAGE = DEFAULT_CODEPAGE
                dataBuilder.EmitInt(0); // RESERVED
            }

            public uint OffsetToData;
            public uint Size;
            private uint CodePage;
            private uint Reserved;
        }
    }
}
