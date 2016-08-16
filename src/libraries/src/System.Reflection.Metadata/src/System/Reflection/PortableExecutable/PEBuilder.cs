// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable
{
    public abstract class PEBuilder
    {
        public PEHeaderBuilder Header { get; }
        public Func<IEnumerable<Blob>, BlobContentId> IdProvider { get; }
        public bool IsDeterministic { get; }

        private readonly Lazy<ImmutableArray<Section>> _lazySections;
        
        protected struct Section
        {
            public readonly string Name;
            public readonly SectionCharacteristics Characteristics;

            public Section(string name, SectionCharacteristics characteristics)
            {
                if (name == null)
                {
                    Throw.ArgumentNull(nameof(name));
                }

                Name = name;
                Characteristics = characteristics;
            }
        }

        private struct SerializedSection
        {
            public readonly BlobBuilder Builder;

            public readonly string Name;
            public readonly SectionCharacteristics Characteristics;
            public readonly int RelativeVirtualAddress;
            public readonly int SizeOfRawData;
            public readonly int PointerToRawData;

            public SerializedSection(BlobBuilder builder, string name, SectionCharacteristics characteristics, int relativeVirtualAddress, int sizeOfRawData, int pointerToRawData)
            {
                Name = name;
                Characteristics = characteristics;
                Builder = builder;
                RelativeVirtualAddress = relativeVirtualAddress;
                SizeOfRawData = sizeOfRawData;
                PointerToRawData = pointerToRawData;
            }

            public int VirtualSize => Builder.Count;
        }

        protected PEBuilder(PEHeaderBuilder header, Func<IEnumerable<Blob>, BlobContentId> deterministicIdProvider)
        {
            if (header == null)
            {
                Throw.ArgumentNull(nameof(header));
            }

            IdProvider = deterministicIdProvider ?? BlobContentId.GetTimeBasedProvider();
            IsDeterministic = deterministicIdProvider != null;
            Header = header;
            _lazySections = new Lazy<ImmutableArray<Section>>(CreateSections);
        }

        protected ImmutableArray<Section> GetSections()
        {
            var sections = _lazySections.Value;
            if (sections.IsDefault)
            {
                throw new InvalidOperationException(SR.Format(SR.MustNotReturnNull, nameof(CreateSections)));
            }

            return sections;
        }

        protected abstract ImmutableArray<Section> CreateSections();

        protected abstract BlobBuilder SerializeSection(string name, SectionLocation location);

        internal protected abstract PEDirectoriesBuilder GetDirectories();

        public BlobContentId Serialize(BlobBuilder builder)
        {
            // Define and serialize sections in two steps.
            // We need to know about all sections before serializing them.
            var serializedSections = SerializeSections();

            // The positions and sizes of directories are calculated during section serialization.
            var directories = GetDirectories();

            Blob stampFixup;
            WritePESignature(builder);
            WriteCoffHeader(builder, serializedSections, out stampFixup);
            WritePEHeader(builder, directories, serializedSections);
            WriteSectionHeaders(builder, serializedSections);
            builder.Align(Header.FileAlignment);

            foreach (var section in serializedSections)
            {
                builder.LinkSuffix(section.Builder);
                builder.Align(Header.FileAlignment);
            }

            var contentId = IdProvider(builder.GetBlobs());

            // patch timestamp in COFF header:
            var stampWriter = new BlobWriter(stampFixup);
            stampWriter.WriteUInt32(contentId.Stamp);
            Debug.Assert(stampWriter.RemainingBytes == 0);

            return contentId;
        }

        private ImmutableArray<SerializedSection> SerializeSections()
        {
            var sections = GetSections();
            var result = ImmutableArray.CreateBuilder<SerializedSection>(sections.Length);
            int sizeOfPeHeaders = Header.ComputeSizeOfPeHeaders(sections.Length);

            var nextRva = BitArithmetic.Align(sizeOfPeHeaders, Header.SectionAlignment);
            var nextPointer = BitArithmetic.Align(sizeOfPeHeaders, Header.FileAlignment);

            foreach (var section in sections)
            {
                var builder = SerializeSection(section.Name, new SectionLocation(nextRva, nextPointer));

                var serialized = new SerializedSection(
                    builder,
                    section.Name,
                    section.Characteristics,
                    relativeVirtualAddress: nextRva,
                    sizeOfRawData: BitArithmetic.Align(builder.Count, Header.FileAlignment),
                    pointerToRawData: nextPointer);

                result.Add(serialized);

                nextRva = BitArithmetic.Align(serialized.RelativeVirtualAddress + serialized.VirtualSize, Header.SectionAlignment);
                nextPointer = serialized.PointerToRawData + serialized.SizeOfRawData;
            }

            return result.MoveToImmutable();
        }

        private void WritePESignature(BlobBuilder builder)
        {
            // MS-DOS stub (128 bytes)
            builder.WriteBytes(s_dosHeader);

            // PE Signature "PE\0\0" 
            builder.WriteUInt32(0x00004550);
        }

        private static readonly byte[] s_dosHeader = new byte[]
        {
            0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
            0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
            0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,
            0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
            0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,
            0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
            0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,
            0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
            0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,
            0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private void WriteCoffHeader(BlobBuilder builder, ImmutableArray<SerializedSection> sections, out Blob stampFixup)
        {
            // Machine
            builder.WriteUInt16((ushort)(Header.Machine == 0 ? Machine.I386 : Header.Machine));

            // NumberOfSections
            builder.WriteUInt16((ushort)sections.Length);

            // TimeDateStamp:
            stampFixup = builder.ReserveBytes(sizeof(uint));

            // PointerToSymbolTable (TODO: not supported):
            // The file pointer to the COFF symbol table, or zero if no COFF symbol table is present. 
            // This value should be zero for a PE image.
            builder.WriteUInt32(0);

            // NumberOfSymbols (TODO: not supported):
            // The number of entries in the symbol table. This data can be used to locate the string table, 
            // which immediately follows the symbol table. This value should be zero for a PE image.
            builder.WriteUInt32(0);

            // SizeOfOptionalHeader:
            // The size of the optional header, which is required for executable files but not for object files. 
            // This value should be zero for an object file (TODO).
            builder.WriteUInt16((ushort)(Header.Is32Bit ? 224 : 240));

            // Characteristics
            builder.WriteUInt16((ushort)Header.ImageCharacteristics);
        }

        private void WritePEHeader(BlobBuilder builder, PEDirectoriesBuilder directories, ImmutableArray<SerializedSection> sections)
        {
            builder.WriteUInt16((ushort)(Header.Is32Bit ? PEMagic.PE32 : PEMagic.PE32Plus));
            builder.WriteByte(Header.MajorLinkerVersion);
            builder.WriteByte(Header.MinorLinkerVersion);

            // SizeOfCode:
            builder.WriteUInt32((uint)SumRawDataSizes(sections, SectionCharacteristics.ContainsCode));

            // SizeOfInitializedData:
            builder.WriteUInt32((uint)SumRawDataSizes(sections, SectionCharacteristics.ContainsInitializedData));

            // SizeOfUninitializedData:
            builder.WriteUInt32((uint)SumRawDataSizes(sections, SectionCharacteristics.ContainsUninitializedData));

            // AddressOfEntryPoint:
            builder.WriteUInt32((uint)directories.AddressOfEntryPoint);

            // BaseOfCode:
            int codeSectionIndex = IndexOfSection(sections, SectionCharacteristics.ContainsCode);
            builder.WriteUInt32((uint)(codeSectionIndex != -1 ? sections[codeSectionIndex].RelativeVirtualAddress : 0));

            if (Header.Is32Bit)
            {
                // BaseOfData:
                int dataSectionIndex = IndexOfSection(sections, SectionCharacteristics.ContainsInitializedData);
                builder.WriteUInt32((uint)(dataSectionIndex != -1 ? sections[dataSectionIndex].RelativeVirtualAddress : 0));

                builder.WriteUInt32((uint)Header.ImageBase);
            }
            else
            {
                builder.WriteUInt64(Header.ImageBase);
            }

            // NT additional fields:
            builder.WriteUInt32((uint)Header.SectionAlignment);
            builder.WriteUInt32((uint)Header.FileAlignment);
            builder.WriteUInt16(Header.MajorOperatingSystemVersion);
            builder.WriteUInt16(Header.MinorOperatingSystemVersion);
            builder.WriteUInt16(Header.MajorImageVersion);
            builder.WriteUInt16(Header.MinorImageVersion);
            builder.WriteUInt16(Header.MajorSubsystemVersion);
            builder.WriteUInt16(Header.MinorSubsystemVersion);
                                
            // Win32VersionValue (reserved, should be 0)
            builder.WriteUInt32(0);

            // SizeOfImage:
            var lastSection = sections[sections.Length - 1];
            builder.WriteUInt32((uint)BitArithmetic.Align(lastSection.RelativeVirtualAddress + lastSection.VirtualSize, Header.SectionAlignment));

            // SizeOfHeaders:
            builder.WriteUInt32((uint)BitArithmetic.Align(Header.ComputeSizeOfPeHeaders(sections.Length), Header.FileAlignment));

            // Checksum:
            // Shall be zero for strong name signing. 
            builder.WriteUInt32(0);

            builder.WriteUInt16((ushort)Header.Subsystem);
            builder.WriteUInt16((ushort)Header.DllCharacteristics);

            if (Header.Is32Bit)
            {
                builder.WriteUInt32((uint)Header.SizeOfStackReserve);
                builder.WriteUInt32((uint)Header.SizeOfStackCommit);
                builder.WriteUInt32((uint)Header.SizeOfHeapReserve);
                builder.WriteUInt32((uint)Header.SizeOfHeapCommit);
            }
            else
            {
                builder.WriteUInt64(Header.SizeOfStackReserve);
                builder.WriteUInt64(Header.SizeOfStackCommit);
                builder.WriteUInt64(Header.SizeOfHeapReserve);
                builder.WriteUInt64(Header.SizeOfHeapCommit);
            }

            // LoaderFlags
            builder.WriteUInt32(0);

            // The number of data-directory entries in the remainder of the header.
            builder.WriteUInt32(16);

            // directory entries:
            builder.WriteUInt32((uint)directories.ExportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.ExportTable.Size);
            builder.WriteUInt32((uint)directories.ImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.ImportTable.Size);
            builder.WriteUInt32((uint)directories.ResourceTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.ResourceTable.Size);
            builder.WriteUInt32((uint)directories.ExceptionTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.ExceptionTable.Size);

            // Authenticode CertificateTable directory. Shall be zero before the PE is signed.
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            builder.WriteUInt32((uint)directories.BaseRelocationTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.BaseRelocationTable.Size);
            builder.WriteUInt32((uint)directories.DebugTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.DebugTable.Size);
            builder.WriteUInt32((uint)directories.CopyrightTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.CopyrightTable.Size);
            builder.WriteUInt32((uint)directories.GlobalPointerTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.GlobalPointerTable.Size);
            builder.WriteUInt32((uint)directories.ThreadLocalStorageTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.ThreadLocalStorageTable.Size);
            builder.WriteUInt32((uint)directories.LoadConfigTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.LoadConfigTable.Size);
            builder.WriteUInt32((uint)directories.BoundImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.BoundImportTable.Size);
            builder.WriteUInt32((uint)directories.ImportAddressTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.ImportAddressTable.Size);
            builder.WriteUInt32((uint)directories.DelayImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.DelayImportTable.Size);
            builder.WriteUInt32((uint)directories.CorHeaderTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)directories.CorHeaderTable.Size);

            // Reserved, should be 0
            builder.WriteUInt64(0);
        }

        private void WriteSectionHeaders(BlobBuilder builder, ImmutableArray<SerializedSection> serializedSections)
        {
            foreach (var serializedSection in serializedSections)
            {
                WriteSectionHeader(builder, serializedSection);
            }
        }

        private static void WriteSectionHeader(BlobBuilder builder, SerializedSection serializedSection)
        {
            if (serializedSection.VirtualSize == 0)
            {
                return;
            }

            for (int j = 0, m = serializedSection.Name.Length; j < 8; j++)
            {
                if (j < m)
                {
                    builder.WriteByte((byte)serializedSection.Name[j]);
                }
                else
                {
                    builder.WriteByte(0);
                }
            }

            builder.WriteUInt32((uint)serializedSection.VirtualSize);
            builder.WriteUInt32((uint)serializedSection.RelativeVirtualAddress);
            builder.WriteUInt32((uint)serializedSection.SizeOfRawData);
            builder.WriteUInt32((uint)serializedSection.PointerToRawData);

            // PointerToRelocations (TODO: not supported):
            builder.WriteUInt32(0);

            // PointerToLinenumbers (TODO: not supported):
            builder.WriteUInt32(0);

            // NumberOfRelocations (TODO: not supported):
            builder.WriteUInt16(0);

            // NumberOfLinenumbers (TODO: not supported):
            builder.WriteUInt16(0);

            builder.WriteUInt32((uint)serializedSection.Characteristics);
        }

        private static int IndexOfSection(ImmutableArray<SerializedSection> sections, SectionCharacteristics characteristics)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                if ((sections[i].Characteristics & characteristics) == characteristics)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int SumRawDataSizes(ImmutableArray<SerializedSection> sections,SectionCharacteristics characteristics)
        {
            int result = 0;
            for (int i = 0; i < sections.Length; i++)
            {
                if ((sections[i].Characteristics & characteristics) == characteristics)
                {
                    result += sections[i].SizeOfRawData;
                }
            }

            return result;
        }
    }
}
