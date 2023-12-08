// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable
{
    /// <summary>
    /// Managed .text PE section.
    /// </summary>
    /// <remarks>
    /// Contains in the following order:
    /// - Import Address Table
    /// - COR Header
    /// - IL
    /// - Metadata
    /// - Managed Resource Data
    /// - Strong Name Signature
    /// - Debug Data (directory and extra info)
    /// - Import Table
    /// - Name Table
    /// - Runtime Startup Stub
    /// - Mapped Field Data
    /// </remarks>
    internal sealed class ManagedTextSection
    {
        public Characteristics ImageCharacteristics { get; }
        public Machine Machine { get; }

        /// <summary>
        /// The size of IL stream (unaligned).
        /// </summary>
        public int ILStreamSize { get; }

        /// <summary>
        /// Total size of metadata (header and all streams).
        /// </summary>
        public int MetadataSize { get; }

        /// <summary>
        /// The size of managed resource data stream.
        /// Aligned to <see cref="ManagedResourcesDataAlignment"/>.
        /// </summary>
        public int ResourceDataSize { get; }

        /// <summary>
        /// Size of strong name hash.
        /// </summary>
        public int StrongNameSignatureSize { get; }

        /// <summary>
        /// Size of Debug data.
        /// </summary>
        public int DebugDataSize { get; }

        /// <summary>
        /// The size of mapped field data stream.
        /// Aligned to <see cref="MappedFieldDataAlignment"/>.
        /// </summary>
        public int MappedFieldDataSize { get; }

        public ManagedTextSection(
            Characteristics imageCharacteristics,
            Machine machine,
            int ilStreamSize,
            int metadataSize,
            int resourceDataSize,
            int strongNameSignatureSize,
            int debugDataSize,
            int mappedFieldDataSize)
        {
            MetadataSize = metadataSize;
            ResourceDataSize = resourceDataSize;
            ILStreamSize = ilStreamSize;
            MappedFieldDataSize = mappedFieldDataSize;
            StrongNameSignatureSize = strongNameSignatureSize;
            ImageCharacteristics = imageCharacteristics;
            Machine = machine;
            DebugDataSize = debugDataSize;
        }

        /// <summary>
        /// If set, the module must include a machine code stub that transfers control to the virtual execution system.
        /// </summary>
        internal bool RequiresStartupStub => Machine == Machine.I386 || Machine == 0;

        /// <summary>
        /// If set, the module contains instructions that assume a 64 bit instruction set. For example it may depend on an address being 64 bits.
        /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
        /// </summary>
        internal bool Requires64bits => Machine == Machine.Amd64 || Machine == Machine.IA64 || Machine == Machine.Arm64;

        public bool Is32Bit => !Requires64bits;

        public const int ManagedResourcesDataAlignment = 8;

        private static ReadOnlySpan<byte> CorEntryPointDll => "mscoree.dll"u8;
        private ReadOnlySpan<byte> CorEntryPointName => (ImageCharacteristics & Characteristics.Dll) != 0 ? "_CorDllMain"u8 : "_CorExeMain"u8;

        private int SizeOfImportAddressTable => RequiresStartupStub ? (Is32Bit ? 2 * sizeof(uint) : 2 * sizeof(ulong)) : 0;

        // (_is32bit ? 66 : 70);
        private int SizeOfImportTable =>
            sizeof(uint) + // RVA
            sizeof(uint) + // 0
            sizeof(uint) + // 0
            sizeof(uint) + // name RVA
            sizeof(uint) + // import address table RVA
            20 +           // ?
            (Is32Bit ? 3 * sizeof(uint) : 2 * sizeof(ulong)) + // import lookup table
            sizeof(ushort) + // hint
            CorEntryPointName.Length +
            1;    // NUL

        private static int SizeOfNameTable =>
            CorEntryPointDll.Length + 1 + sizeof(ushort);

        private int SizeOfRuntimeStartupStub => Is32Bit ? 8 : 16;

        public const int MappedFieldDataAlignment = 8;

        internal int CalculateOffsetToMappedFieldDataStreamUnaligned()
        {
            int result = ComputeOffsetToImportTable();

            if (RequiresStartupStub)
            {
                result += SizeOfImportTable + SizeOfNameTable;
                result = BitArithmetic.Align(result, Is32Bit ? 4 : 8); //optional padding to make startup stub's target address align on word or double word boundary
                result += SizeOfRuntimeStartupStub;
            }

            return result;
        }

        public int CalculateOffsetToMappedFieldDataStream()
        {
            int result = CalculateOffsetToMappedFieldDataStreamUnaligned();
            if (MappedFieldDataSize != 0)
            {
                result = BitArithmetic.Align(result, MappedFieldDataAlignment);
            }
            return result;
        }

        internal int ComputeOffsetToDebugDirectory()
        {
            Debug.Assert(MetadataSize % 4 == 0);
            Debug.Assert(ResourceDataSize % 4 == 0);

            return
                ComputeOffsetToMetadata() +
                MetadataSize +
                ResourceDataSize +
                StrongNameSignatureSize;
        }

        private int ComputeOffsetToImportTable()
        {
            return
                ComputeOffsetToDebugDirectory() +
                DebugDataSize;
        }

        private const int CorHeaderSize =
            sizeof(int) +    // header size
            sizeof(short) +  // major runtime version
            sizeof(short) +  // minor runtime version
            sizeof(long) +   // metadata directory
            sizeof(int) +    // COR flags
            sizeof(int) +    // entry point
            sizeof(long) +   // resources directory
            sizeof(long) +   // strong name signature directory
            sizeof(long) +   // code manager table directory
            sizeof(long) +   // vtable fixups directory
            sizeof(long) +   // export address table jumps directory
            sizeof(long);   // managed-native header directory

        public int OffsetToILStream => SizeOfImportAddressTable + CorHeaderSize;

        private int ComputeOffsetToMetadata()
        {
            return OffsetToILStream + BitArithmetic.Align(ILStreamSize, 4);
        }

        public int ComputeSizeOfTextSection()
        {
            Debug.Assert(MappedFieldDataSize % MappedFieldDataAlignment == 0);
            return CalculateOffsetToMappedFieldDataStream() + MappedFieldDataSize;
        }

        public int GetEntryPointAddress(int rva)
        {
            // TODO: constants
            return RequiresStartupStub ?
                rva + CalculateOffsetToMappedFieldDataStreamUnaligned() - (Is32Bit ? 6 : 10) :
                0;
        }

        public DirectoryEntry GetImportAddressTableDirectoryEntry(int rva)
        {
            return RequiresStartupStub ?
                new DirectoryEntry(rva, SizeOfImportAddressTable) :
                default(DirectoryEntry);
        }

        public DirectoryEntry GetImportTableDirectoryEntry(int rva)
        {
            // TODO: constants
            return RequiresStartupStub ?
                new DirectoryEntry(rva + ComputeOffsetToImportTable(), (Is32Bit ? 66 : 70) + 13) :
                default(DirectoryEntry);
        }

        public DirectoryEntry GetCorHeaderDirectoryEntry(int rva)
        {
            return new DirectoryEntry(rva + SizeOfImportAddressTable, CorHeaderSize);
        }

        #region Serialization

        /// <summary>
        /// Serializes .text section data into a specified <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">An empty builder to serialize section data to.</param>
        /// <param name="relativeVirtualAddess">Relative virtual address of the section within the containing PE file.</param>
        /// <param name="entryPointTokenOrRelativeVirtualAddress">Entry point token or RVA (<see cref="CorHeader.EntryPointTokenOrRelativeVirtualAddress"/>)</param>
        /// <param name="corFlags">COR Flags (<see cref="CorHeader.Flags"/>).</param>
        /// <param name="baseAddress">Base address of the PE image.</param>
        /// <param name="metadataBuilder"><see cref="BlobBuilder"/> containing metadata. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="ilBuilder"><see cref="BlobBuilder"/> containing IL stream. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="mappedFieldDataBuilderOpt"><see cref="BlobBuilder"/> containing mapped field data. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="resourceBuilderOpt"><see cref="BlobBuilder"/> containing managed resource data. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="debugDataBuilderOpt"><see cref="BlobBuilder"/> containing PE debug table and data. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="strongNameSignature">Blob reserved in the <paramref name="builder"/> for strong name signature.</param>
        public void Serialize(
            BlobBuilder builder,
            int relativeVirtualAddess,
            int entryPointTokenOrRelativeVirtualAddress,
            CorFlags corFlags,
            ulong baseAddress,
            BlobBuilder metadataBuilder,
            BlobBuilder ilBuilder,
            BlobBuilder? mappedFieldDataBuilderOpt,
            BlobBuilder? resourceBuilderOpt,
            BlobBuilder? debugDataBuilderOpt,
            out Blob strongNameSignature)
        {
            Debug.Assert(builder.Count == 0);
            Debug.Assert(metadataBuilder.Count == MetadataSize);
            Debug.Assert(metadataBuilder.Count % 4 == 0);
            Debug.Assert(ilBuilder.Count == ILStreamSize);
            Debug.Assert((mappedFieldDataBuilderOpt?.Count ?? 0) == MappedFieldDataSize);
            Debug.Assert((resourceBuilderOpt?.Count ?? 0) == ResourceDataSize);
            Debug.Assert((resourceBuilderOpt?.Count ?? 0) % 4 == 0);

            // TODO: avoid recalculation
            int importTableRva = GetImportTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;
            int importAddressTableRva = GetImportAddressTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;

            if (RequiresStartupStub)
            {
                WriteImportAddressTable(builder, importTableRva);
            }

            WriteCorHeader(builder, relativeVirtualAddess, entryPointTokenOrRelativeVirtualAddress, corFlags);

            // IL:
            ilBuilder.Align(4);
            builder.LinkSuffix(ilBuilder);

            // metadata:
            builder.LinkSuffix(metadataBuilder);

            // managed resources:
            if (resourceBuilderOpt != null)
            {
                builder.LinkSuffix(resourceBuilderOpt);
            }

            // strong name signature:
            strongNameSignature = builder.ReserveBytes(StrongNameSignatureSize);

            // The bytes are required to be 0 for the purpose of calculating hash of the PE content
            // when strong name signing.
            new BlobWriter(strongNameSignature).WriteBytes(0, StrongNameSignatureSize);

            // debug directory and data:
            if (debugDataBuilderOpt != null)
            {
                builder.LinkSuffix(debugDataBuilderOpt);
            }

            if (RequiresStartupStub)
            {
                WriteImportTable(builder, importTableRva, importAddressTableRva);
                WriteNameTable(builder);
                WriteRuntimeStartupStub(builder, importAddressTableRva, baseAddress);
            }

            // mapped field data:
            if (mappedFieldDataBuilderOpt != null)
            {
                if (mappedFieldDataBuilderOpt.Count != 0)
                    builder.Align(MappedFieldDataAlignment);
                builder.LinkSuffix(mappedFieldDataBuilderOpt);
            }

            Debug.Assert(builder.Count == ComputeSizeOfTextSection());
        }

        private void WriteImportAddressTable(BlobBuilder builder, int importTableRva)
        {
            int start = builder.Count;

            int ilRva = importTableRva + 40;
            int hintRva = ilRva + (Is32Bit ? 12 : 16);

            // Import Address Table
            if (Is32Bit)
            {
                builder.WriteUInt32((uint)hintRva); // 4
                builder.WriteUInt32(0); // 8
            }
            else
            {
                builder.WriteUInt64((uint)hintRva); // 8
                builder.WriteUInt64(0); // 16
            }

            Debug.Assert(builder.Count - start == SizeOfImportAddressTable);
        }

        private void WriteImportTable(BlobBuilder builder, int importTableRva, int importAddressTableRva)
        {
            int start = builder.Count;

            int ilRVA = importTableRva + 40;
            int hintRva = ilRVA + (Is32Bit ? 12 : 16);
            int nameRva = hintRva + 12 + 2;

            // Import table
            builder.WriteUInt32((uint)ilRVA); // 4
            builder.WriteUInt32(0); // 8
            builder.WriteUInt32(0); // 12
            builder.WriteUInt32((uint)nameRva); // 16
            builder.WriteUInt32((uint)importAddressTableRva); // 20
            builder.WriteBytes(0, 20); // 40

            // Import Lookup table
            if (Is32Bit)
            {
                builder.WriteUInt32((uint)hintRva); // 44
                builder.WriteUInt32(0); // 48
                builder.WriteUInt32(0); // 52
            }
            else
            {
                builder.WriteUInt64((uint)hintRva); // 48
                builder.WriteUInt64(0); // 56
            }

            // Hint table
            builder.WriteUInt16(0); // Hint 54|58

            builder.WriteBytes(CorEntryPointName); // 65|69
            builder.WriteByte(0); // 66|70
            Debug.Assert(builder.Count - start == SizeOfImportTable);
        }

        private static void WriteNameTable(BlobBuilder builder)
        {
            int start = builder.Count;

            builder.WriteBytes(CorEntryPointDll);
            builder.WriteByte(0);
            builder.WriteUInt16(0);
            Debug.Assert(builder.Count - start == SizeOfNameTable);
        }

        private void WriteCorHeader(BlobBuilder builder, int textSectionRva, int entryPointTokenOrRva, CorFlags corFlags)
        {
            const ushort majorRuntimeVersion = 2;
            const ushort minorRuntimeVersion = 5;

            int metadataRva = textSectionRva + ComputeOffsetToMetadata();
            int resourcesRva = metadataRva + MetadataSize;
            int signatureRva = resourcesRva + ResourceDataSize;

            int start = builder.Count;

            // Size:
            builder.WriteUInt32(CorHeaderSize);

            // Version:
            builder.WriteUInt16(majorRuntimeVersion);
            builder.WriteUInt16(minorRuntimeVersion);

            // MetadataDirectory:
            builder.WriteUInt32((uint)metadataRva);
            builder.WriteUInt32((uint)MetadataSize);

            // COR Flags:
            builder.WriteUInt32((uint)corFlags);

            // EntryPoint:
            builder.WriteUInt32((uint)entryPointTokenOrRva);

            // ResourcesDirectory:
            builder.WriteUInt32((uint)(ResourceDataSize == 0 ? 0 : resourcesRva)); // 28
            builder.WriteUInt32((uint)ResourceDataSize);

            // StrongNameSignatureDirectory:
            builder.WriteUInt32((uint)(StrongNameSignatureSize == 0 ? 0 : signatureRva)); // 36
            builder.WriteUInt32((uint)StrongNameSignatureSize);

            // CodeManagerTableDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            // VtableFixupsDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            // ExportAddressTableJumpsDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            // ManagedNativeHeaderDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            Debug.Assert(builder.Count - start == CorHeaderSize);
            Debug.Assert(builder.Count % 4 == 0);
        }

        private void WriteRuntimeStartupStub(BlobBuilder sectionBuilder, int importAddressTableRva, ulong baseAddress)
        {
            // entry point code, consisting of a jump indirect to _CorXXXMain
            if (Is32Bit)
            {
                // Write zeros (nops) to pad the entry point code so that the target address is aligned on a 4 byte boundary.
                // Note that the section is aligned to FileAlignment, which is at least 512, so we can align relatively to the start of the section.
                sectionBuilder.Align(4);

                sectionBuilder.WriteUInt16(0);
                sectionBuilder.WriteByte(0xff);
                sectionBuilder.WriteByte(0x25); //4
                sectionBuilder.WriteUInt32((uint)importAddressTableRva + (uint)baseAddress); //8
            }
            else
            {
                // Write zeros (nops) to pad the entry point code so that the target address is aligned on a 8 byte boundary.
                // Note that the section is aligned to FileAlignment, which is at least 512, so we can align relatively to the start of the section.
                sectionBuilder.Align(8);

                sectionBuilder.WriteUInt32(0);
                sectionBuilder.WriteUInt16(0);
                sectionBuilder.WriteByte(0xff);
                sectionBuilder.WriteByte(0x25); //8
                sectionBuilder.WriteUInt64((ulong)importAddressTableRva + baseAddress); //16
            }
        }

        #endregion
    }
}
