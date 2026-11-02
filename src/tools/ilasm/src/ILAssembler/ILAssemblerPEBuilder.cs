// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILAssembler;

/// <summary>
/// The <see cref="ManagedPEBuilder"/> for every image the assembler writes, including VTable fixups,
/// unmanaged exports, TLS data, and data label reference fixups. The image has a debug directory
/// only when the assembler supplies one.
/// </summary>
/// <remarks>
/// Given no <see cref="DebugDirectoryBuilder"/>, a deterministic <see cref="ManagedPEBuilder"/> adds a debug
/// directory with a Reproducible entry of its own. The assembler adds a debug directory only with a PDB, so
/// this builder passes an empty <see cref="DebugDirectoryBuilder"/> instead, and it clears the PE header's debug
/// directory entry whenever the directory has no entries, which an empty builder would otherwise leave with a
/// non-zero address and size 0: an image built without a PDB has no debug directory, deterministic or not.
/// </remarks>
internal sealed class ILAssemblerPEBuilder : ManagedPEBuilder
{
    private const string RelocationSectionName = ".reloc";
    private const string SDataSectionName = ".sdata";
    private const string TextSectionName = ".text";
    private const string TlsSectionName = ".tls";
    private const int DataSectionCount = 3;
    private const int ExportDirectoryHeaderSize = 40;
    private const int RelocationPageSize = 0x1000;
    private const int TlsDirectoryAddressCount = 4;
    private const ushort ImageRelBasedAbsolute = 0;
    private const ushort ImageRelBasedHighLow = 3;
    private const ushort ImageRelBasedDir64 = 10;

    private readonly ImmutableArray<VTableFixupInfo> _vtableFixups;
    private readonly ImmutableArray<ExportInfo> _exports;
    private readonly BlobBuilder? _mappedFieldData;
    private readonly BlobBuilder? _tlsData;
    private readonly BlobBuilder? _cilData;
    private readonly ImmutableArray<DataLocation> _mappedFields;
    private readonly ImmutableArray<DataLabelFixup> _dataLabelFixups;
    private readonly string _dllName;
    private readonly bool _hasSDataSection;
    private readonly List<ExportStubInfo> _exportStubs = new();
    private readonly DataLayout[] _dataLayouts = new DataLayout[DataSectionCount];

    private BlobBuilder? _textSectionBuilder;
    private int _textSectionRva;
    private int _vtableFixupsRva;
    private int _vtableFixupsSize;
    private Blob _vtableDirectoryFixup;
    private int _exportDirectoryRva;
    private int _exportDirectorySize;
    private int _relocationDirectoryRva;
    private int _relocationDirectorySize;
    private Blob _tlsDirectoryFixup;
    private int _tlsDirectoryRva;
    private int _tlsDirectorySize;
    private int _tlsIndexRva;
    private int _tlsCallbacksRva;

    public readonly record struct VTableFixupInfo(
        DataLocation DataLocation,
        int SlotCount,
        ushort Flags,
        ImmutableArray<int> MethodTokens);

    public readonly record struct ExportInfo(
        int Ordinal,
        string Name,
        int VTableEntryIndex,
        int VTableSlotIndex);

    public readonly record struct DataLabelFixup(
        DataLocation Source,
        DataLocation Target,
        int PointerSize);

    private readonly record struct DataLayout(Blob Data, int Rva);

    private readonly record struct ExportStubInfo(int StubRva, int AddressRva, Blob AddressFixup);

    private readonly record struct BaseRelocation(int Rva, ushort Type);

    public ILAssemblerPEBuilder(
        PEHeaderBuilder header,
        MetadataRootBuilder metadataRootBuilder,
        BlobBuilder ilStream,
        BlobBuilder? mappedFieldData = null,
        BlobBuilder? managedResources = null,
        ResourceSectionBuilder? nativeResources = null,
        DebugDirectoryBuilder? debugDirectoryBuilder = null,
        int strongNameSignatureSize = 128,
        MethodDefinitionHandle entryPoint = default,
        CorFlags flags = CorFlags.ILOnly,
        Func<IEnumerable<Blob>, BlobContentId>? deterministicIdProvider = null,
        ImmutableArray<VTableFixupInfo> vtableFixups = default,
        ImmutableArray<ExportInfo> exports = default,
        ImmutableArray<DataLabelFixup> dataLabelFixups = default,
        string? dllName = null,
        BlobBuilder? tlsData = null,
        BlobBuilder? cilData = null,
        ImmutableArray<DataLocation> mappedFields = default)
        : base(
            header,
            metadataRootBuilder,
            ilStream,
            mappedFieldData: null,
            managedResources,
            nativeResources,
            debugDirectoryBuilder ?? new DebugDirectoryBuilder(),
            strongNameSignatureSize,
            entryPoint,
            GetCorFlags(header.Machine, flags, exports, dataLabelFixups, tlsData),
            deterministicIdProvider)
    {
        _vtableFixups = vtableFixups.IsDefault ? ImmutableArray<VTableFixupInfo>.Empty : vtableFixups;
        _exports = exports.IsDefault ? ImmutableArray<ExportInfo>.Empty : exports;
        _mappedFieldData = mappedFieldData;
        _tlsData = tlsData;
        _cilData = cilData;
        _mappedFields = mappedFields.IsDefault ? ImmutableArray<DataLocation>.Empty : mappedFields;
        _dataLabelFixups = dataLabelFixups.IsDefault
            ? ImmutableArray<DataLabelFixup>.Empty
            : dataLabelFixups;
        _dllName = dllName ?? "output.dll";
        _hasSDataSection =
            mappedFieldData is { Count: > 0 } ||
            _mappedFields.Any(location => location.Section == DataSection.Data) ||
            !_vtableFixups.IsEmpty ||
            HasTlsData;
    }

    private bool HasTlsData => (_tlsData?.Count ?? 0) > 0;

    private static CorFlags GetCorFlags(
        Machine machine,
        CorFlags flags,
        ImmutableArray<ExportInfo> exports,
        ImmutableArray<DataLabelFixup> dataLabelFixups,
        BlobBuilder? tlsData)
    {
        if (!exports.IsDefaultOrEmpty || !dataLabelFixups.IsDefaultOrEmpty ||
            (tlsData?.Count ?? 0) > 0)
        {
            flags &= ~CorFlags.ILOnly;

            if (machine == Machine.I386)
            {
                // Match native ilasm's COR_SET_32BIT_REQUIRED for TLS, data addresses, and exports.
                flags = (flags & ~CorFlags.Prefers32Bit) | CorFlags.Requires32Bit;
            }
        }

        return flags;
    }

    internal static bool IsExportMachineSupported(Machine machine) =>
        machine is
            Machine.I386 or
            Machine.Amd64 or
            Machine.Arm64;

    protected override ImmutableArray<Section> CreateSections()
    {
        ImmutableArray<Section> baseSections = base.CreateSections();
        bool hasRelocationSection = false;
        var builder = ImmutableArray.CreateBuilder<Section>(baseSections.Length + 3);

        for (int i = 0; i < baseSections.Length; i++)
        {
            Section section = baseSections[i];
            builder.Add(section);

            if (i == 0 && _hasSDataSection)
            {
                builder.Add(new Section(
                    SDataSectionName,
                    SectionCharacteristics.MemRead |
                    SectionCharacteristics.MemWrite |
                    SectionCharacteristics.ContainsInitializedData));
            }

            if (i == 0 && HasTlsData)
            {
                builder.Add(new Section(
                    TlsSectionName,
                    SectionCharacteristics.MemRead |
                    SectionCharacteristics.MemWrite |
                    SectionCharacteristics.ContainsInitializedData));
            }

            hasRelocationSection |= section.Name == RelocationSectionName;
        }

        if ((_exports.Length > 0 || !_dataLabelFixups.IsEmpty || HasTlsData) && !hasRelocationSection)
        {
            builder.Add(new Section(
                RelocationSectionName,
                SectionCharacteristics.MemRead |
                SectionCharacteristics.MemDiscardable |
                SectionCharacteristics.ContainsInitializedData));
        }

        return builder.ToImmutable();
    }

    protected override BlobBuilder SerializeSection(string name, SectionLocation location)
    {
        switch (name)
        {
            case TextSectionName:
                BlobBuilder textBuilder = base.SerializeSection(name, location);
                _textSectionBuilder = textBuilder;
                _textSectionRva = location.RelativeVirtualAddress;
                WriteData(textBuilder, DataSection.Cil, location.RelativeVirtualAddress);
                SerializeExportStubs(textBuilder, location.RelativeVirtualAddress);
                if (!_hasSDataSection)
                {
                    PatchDataLayout();
                }

                return textBuilder;

            case SDataSectionName:
                return SerializeSDataSection(location);

            case TlsSectionName:
                var tlsBuilder = new BlobBuilder();
                WriteData(tlsBuilder, DataSection.Tls, location.RelativeVirtualAddress);
                PatchDataLayout();
                return tlsBuilder;

            case RelocationSectionName:
                return SerializeRelocationSection(location);

            default:
                return base.SerializeSection(name, location);
        }
    }

    protected override PEDirectoriesBuilder GetDirectories()
    {
        PEDirectoriesBuilder directories = base.GetDirectories();

        // An empty debug directory must not leave an RVA with a zero size in the PE header.
        if (directories.DebugTable.Size == 0)
        {
            directories.DebugTable = default;
        }

        if (_exportDirectoryRva != 0 && _exportDirectorySize != 0)
        {
            directories.ExportTable = new DirectoryEntry(_exportDirectoryRva, _exportDirectorySize);
        }

        if (_relocationDirectoryRva != 0 && _relocationDirectorySize != 0)
        {
            directories.BaseRelocationTable = new DirectoryEntry(
                _relocationDirectoryRva,
                _relocationDirectorySize);
        }

        if (HasTlsData)
        {
            directories.ThreadLocalStorageTable = new DirectoryEntry(_tlsDirectoryRva, _tlsDirectorySize);
        }

        return directories;
    }

    private void SerializeExportStubs(BlobBuilder builder, int textSectionRva)
    {
        Machine machine = Header.Machine;

        foreach (ExportInfo _ in _exports)
        {
            (int addressOffset, int addressSize, int addressAlignment) = machine switch
            {
                Machine.I386 => (2, sizeof(uint), 16),
                Machine.Amd64 => (2, sizeof(ulong), 4),
                Machine.Arm64 => (16, sizeof(ulong), 8),
                _ => throw new UnreachableException(),
            };

            int addressMisalignment = (builder.Count + addressOffset) & (addressAlignment - 1);
            if (addressMisalignment != 0)
            {
                builder.WriteBytes(0, addressAlignment - addressMisalignment);
            }

            int stubRva = textSectionRva + builder.Count;
            Blob addressFixup;
            switch (machine)
            {
                case Machine.I386:
                    builder.WriteByte(0xFF);
                    builder.WriteByte(0x25);
                    addressFixup = builder.ReserveBytes(addressSize);
                    break;

                case Machine.Amd64:
                    builder.WriteByte(0x48);
                    builder.WriteByte(0xA1);
                    addressFixup = builder.ReserveBytes(addressSize);
                    builder.WriteByte(0xFF);
                    builder.WriteByte(0xE0);
                    break;

                case Machine.Arm64:
                    // ldr x16, #16; ldr x16, [x16]; br x16; nop; .quad vtableSlotAddress
                    builder.WriteUInt32(0x58000090);
                    builder.WriteUInt32(0xF9400210);
                    builder.WriteUInt32(0xD61F0200);
                    builder.WriteUInt32(0xD503201F);
                    addressFixup = builder.ReserveBytes(addressSize);
                    break;

                default:
                    throw new UnreachableException();
            }

            _exportStubs.Add(new ExportStubInfo(
                stubRva,
                stubRva + addressOffset,
                addressFixup));
        }
    }

    private BlobBuilder SerializeSDataSection(SectionLocation location)
    {
        var builder = new BlobBuilder();
        int vtableDirectorySize = checked(_vtableFixups.Length * 8);

        if (_vtableFixups.Length > 0)
        {
            _vtableFixupsRva = location.RelativeVirtualAddress;
            _vtableFixupsSize = vtableDirectorySize;
            _vtableDirectoryFixup = builder.ReserveBytes(vtableDirectorySize);
        }

        WriteData(builder, DataSection.Data, location.RelativeVirtualAddress);

        if (_exports.Length > 0)
        {
            builder.Align(sizeof(int));
            SerializeExportDirectory(builder, location.RelativeVirtualAddress);
        }

        if (HasTlsData)
        {
            // IMAGE_TLS_DIRECTORY contains four VAs followed by two DWORDs (PE/COFF section 6.7).
            int pointerSize = VTableFixupSupport.GetPointerSize(Header.Machine);
            builder.Align(pointerSize);
            _tlsDirectoryRva = location.RelativeVirtualAddress + builder.Count;
            _tlsDirectorySize = (TlsDirectoryAddressCount * pointerSize) + (2 * sizeof(uint));
            _tlsDirectoryFixup = builder.ReserveBytes(_tlsDirectorySize);
            _tlsCallbacksRva = location.RelativeVirtualAddress + builder.Count;
            builder.WriteBytes(0, pointerSize);
            _tlsIndexRva = location.RelativeVirtualAddress + builder.Count;
            builder.WriteUInt32(0);
        }
        else
        {
            PatchDataLayout();
        }

        PatchCorHeaderVTableFixups();
        return builder;
    }

    private BlobBuilder? GetDataSource(DataSection section) => section switch
    {
        DataSection.Data => _mappedFieldData,
        DataSection.Tls => _tlsData,
        DataSection.Cil => _cilData,
        _ => throw new UnreachableException(),
    };

    private void WriteData(BlobBuilder builder, DataSection section, int sectionRva)
    {
        BlobBuilder? data = GetDataSource(section);
        if ((data?.Count ?? 0) > 0)
        {
            builder.Align(ManagedPEBuilder.MappedFieldDataAlignment);
        }

        int dataRva = checked(sectionRva + builder.Count);
        Blob dataBlob = builder.ReserveBytes(data?.Count ?? 0);
        _dataLayouts[(int)section] = new DataLayout(dataBlob, dataRva);
        if (data is not null && data.Count > 0)
        {
            var writer = new BlobWriter(dataBlob);
            foreach (Blob blob in data.GetBlobs())
            {
                ArraySegment<byte> bytes = blob.GetBytes();
                Debug.Assert(bytes.Array is not null);
                writer.WriteBytes(bytes.Array, bytes.Offset, bytes.Count);
            }
        }
    }

    private int GetDataRva(DataLocation location) =>
        checked(_dataLayouts[(int)location.Section].Rva + location.Offset);

    private void PatchDataLayout()
    {
        PatchMappedFieldRvas();
        ApplyDataLabelFixups();
        ApplyVTableTokens();
        PatchExportStubAddresses();

        if (!_vtableFixups.IsEmpty)
        {
            var writer = new BlobWriter(_vtableDirectoryFixup);
            foreach (VTableFixupInfo fixup in _vtableFixups)
            {
                writer.WriteInt32(GetDataRva(fixup.DataLocation));
                writer.WriteUInt16((ushort)fixup.SlotCount);
                writer.WriteUInt16(fixup.Flags);
            }
        }

        if (HasTlsData)
        {
            Debug.Assert(_tlsData is not null);
            var writer = new BlobWriter(_tlsDirectoryFixup);
            WriteAddress(_dataLayouts[(int)DataSection.Tls].Rva);
            WriteAddress(checked(_dataLayouts[(int)DataSection.Tls].Rva + _tlsData.Count));
            WriteAddress(_tlsIndexRva);
            WriteAddress(_tlsCallbacksRva);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);

            void WriteAddress(int rva)
            {
                ulong address = Header.ImageBase + (uint)rva;
                if (Header.Machine == Machine.I386)
                {
                    writer.WriteUInt32(unchecked((uint)address));
                }
                else
                {
                    writer.WriteUInt64(address);
                }
            }
        }
    }

    private void ApplyDataLabelFixups()
    {
        if (_dataLabelFixups.IsEmpty)
        {
            return;
        }

        foreach (DataLabelFixup fixup in _dataLabelFixups)
        {
            var writer = new BlobWriter(_dataLayouts[(int)fixup.Source.Section].Data)
            {
                Offset = fixup.Source.Offset,
            };
            int targetRva = GetDataRva(fixup.Target);
            ulong targetAddress = Header.ImageBase + (uint)targetRva;
            if (fixup.PointerSize == sizeof(long))
            {
                writer.WriteUInt64(targetAddress);
            }
            else
            {
                writer.WriteUInt32(unchecked((uint)targetAddress));
            }
        }
    }

    private void ApplyVTableTokens()
    {
        foreach (VTableFixupInfo fixup in _vtableFixups)
        {
            var writer = new BlobWriter(_dataLayouts[(int)fixup.DataLocation.Section].Data);
            int slotSize = VTableFixupSupport.GetSlotSize(fixup.Flags);
            for (int slotIndex = 0; slotIndex < fixup.SlotCount; slotIndex++)
            {
                int slotOffset = fixup.DataLocation.Offset + (slotIndex * slotSize);
                int token = fixup.MethodTokens[slotIndex];
                if (token == 0)
                {
                    continue;
                }

                writer.Offset = slotOffset;
                if (slotSize == sizeof(long))
                {
                    writer.WriteInt64(token);
                }
                else
                {
                    writer.WriteInt32(token);
                }
            }
        }
    }

    private void PatchMappedFieldRvas()
    {
        if (_mappedFields.IsEmpty)
        {
            return;
        }

        Debug.Assert(_textSectionBuilder is not null);

        int corHeaderOffset = GetCorHeaderOffset();
        int metadataRva = ReadInt32AtOffset(_textSectionBuilder, corHeaderOffset + 8);
        int metadataSize = ReadInt32AtOffset(_textSectionBuilder, corHeaderOffset + 12);
        int metadataOffset = metadataRva - _textSectionRva;
        byte[] metadata = ReadBytes(_textSectionBuilder, metadataOffset, metadataSize);

        using MetadataReaderProvider provider =
            MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(metadata));
        MetadataReader reader = provider.GetMetadataReader();
        int rowCount = reader.GetTableRowCount(TableIndex.FieldRva);
        Debug.Assert(rowCount == _mappedFields.Length);
        if (rowCount == 0)
        {
            return;
        }

        int tableOffset = reader.GetTableMetadataOffset(TableIndex.FieldRva);
        int rowSize = reader.GetTableRowSize(TableIndex.FieldRva);
        for (int row = 0; row < rowCount; row++)
        {
            int rvaOffset = metadataOffset + tableOffset + (row * rowSize);
            WriteInt32AtOffset(_textSectionBuilder, rvaOffset, GetDataRva(_mappedFields[row]));
        }
    }

    private void PatchExportStubAddresses()
    {
        Debug.Assert(_exportStubs.Count == _exports.Length);

        for (int i = 0; i < _exports.Length; i++)
        {
            ExportInfo export = _exports[i];
            VTableFixupInfo fixup = _vtableFixups[export.VTableEntryIndex - 1];
            int slotSize = VTableFixupSupport.GetSlotSize(fixup.Flags);
            int slotRva =
                GetDataRva(fixup.DataLocation) +
                ((export.VTableSlotIndex - 1) * slotSize);
            ulong absoluteAddress = Header.ImageBase + (uint)slotRva;
            var writer = new BlobWriter(_exportStubs[i].AddressFixup);

            if (_exportStubs[i].AddressFixup.Length == sizeof(ulong))
            {
                writer.WriteUInt64(absoluteAddress);
            }
            else
            {
                writer.WriteUInt32(unchecked((uint)absoluteAddress));
            }
        }
    }

    private void SerializeExportDirectory(BlobBuilder builder, int sectionRva)
    {
        int baseOrdinal = _exports.Min(export => export.Ordinal);
        int maxOrdinal = _exports.Max(export => export.Ordinal);
        int numberOfFunctions = checked((int)((long)maxOrdinal - baseOrdinal + 1));
        Debug.Assert(numberOfFunctions <= ushort.MaxValue + 1);

        ExportInfo[] sortedExports = _exports.ToArray();
        Array.Sort(sortedExports, static (left, right) => string.CompareOrdinal(left.Name, right.Name));

        int nameTableSize = 0;
        foreach (ExportInfo export in sortedExports)
        {
            nameTableSize = checked(nameTableSize + Encoding.ASCII.GetByteCount(export.Name) + 1);
        }

        int dllNameSize = checked(Encoding.ASCII.GetByteCount(_dllName) + 1);
        int exportDirectoryOffset = builder.Count;
        int addressTableOffset = checked(exportDirectoryOffset + ExportDirectoryHeaderSize);
        int namePointerTableOffset = checked(addressTableOffset + (numberOfFunctions * sizeof(int)));
        int ordinalTableOffset = checked(namePointerTableOffset + (_exports.Length * sizeof(int)));
        int exportNamesOffset = checked(ordinalTableOffset + (_exports.Length * sizeof(ushort)));
        int dllNameOffset = checked(exportNamesOffset + nameTableSize);

        _exportDirectoryRva = sectionRva + exportDirectoryOffset;

        builder.WriteUInt32(0);
        builder.WriteUInt32(0);
        builder.WriteUInt16(0);
        builder.WriteUInt16(0);
        builder.WriteInt32(sectionRva + dllNameOffset);
        builder.WriteInt32(baseOrdinal);
        builder.WriteInt32(numberOfFunctions);
        builder.WriteInt32(_exports.Length);
        builder.WriteInt32(sectionRva + addressTableOffset);
        builder.WriteInt32(sectionRva + namePointerTableOffset);
        builder.WriteInt32(sectionRva + ordinalTableOffset);

        var functionRvas = new int[numberOfFunctions];
        for (int i = 0; i < _exports.Length; i++)
        {
            int functionIndex = _exports[i].Ordinal - baseOrdinal;
            if (functionRvas[functionIndex] == 0)
            {
                functionRvas[functionIndex] = _exportStubs[i].StubRva;
            }
        }

        foreach (int functionRva in functionRvas)
        {
            builder.WriteInt32(functionRva);
        }

        int nameRva = sectionRva + exportNamesOffset;
        foreach (ExportInfo export in sortedExports)
        {
            builder.WriteInt32(nameRva);
            nameRva += Encoding.ASCII.GetByteCount(export.Name) + 1;
        }

        foreach (ExportInfo export in sortedExports)
        {
            builder.WriteUInt16(checked((ushort)(export.Ordinal - baseOrdinal)));
        }

        foreach (ExportInfo export in sortedExports)
        {
            builder.WriteBytes(Encoding.ASCII.GetBytes(export.Name));
            builder.WriteByte(0);
        }

        builder.WriteBytes(Encoding.ASCII.GetBytes(_dllName));
        builder.WriteByte(0);
        Debug.Assert(builder.Count == dllNameOffset + dllNameSize);

        _exportDirectorySize = builder.Count - exportDirectoryOffset;
    }

    private BlobBuilder SerializeRelocationSection(SectionLocation location)
    {
        Machine machine = Header.Machine;
        ushort exportRelocationType = machine is Machine.Amd64 or Machine.Arm64
            ? ImageRelBasedDir64
            : ImageRelBasedHighLow;
        var relocations = new List<BaseRelocation>(
            _exportStubs.Count + _dataLabelFixups.Length + 5);

        if (machine == Machine.I386)
        {
            int entryPointRva = base.GetDirectories().AddressOfEntryPoint;
            if (entryPointRva != 0)
            {
                relocations.Add(new BaseRelocation(entryPointRva + 2, ImageRelBasedHighLow));
            }
        }

        foreach (ExportStubInfo stub in _exportStubs)
        {
            relocations.Add(new BaseRelocation(stub.AddressRva, exportRelocationType));
        }

        foreach (DataLabelFixup fixup in _dataLabelFixups)
        {
            relocations.Add(new BaseRelocation(
                GetDataRva(fixup.Source),
                fixup.PointerSize == sizeof(long)
                    ? ImageRelBasedDir64
                    : ImageRelBasedHighLow));
        }

        if (HasTlsData)
        {
            int pointerSize = VTableFixupSupport.GetPointerSize(machine);
            for (int i = 0; i < TlsDirectoryAddressCount; i++)
            {
                relocations.Add(new BaseRelocation(
                    _tlsDirectoryRva + (i * pointerSize),
                    exportRelocationType));
            }
        }

        relocations.Sort(static (left, right) => left.Rva.CompareTo(right.Rva));

        var builder = new BlobBuilder();
        int firstRelocation = 0;
        while (firstRelocation < relocations.Count)
        {
            int pageRva = relocations[firstRelocation].Rva & ~(RelocationPageSize - 1);
            int lastRelocation = firstRelocation + 1;
            while (lastRelocation < relocations.Count &&
                   (relocations[lastRelocation].Rva & ~(RelocationPageSize - 1)) == pageRva)
            {
                lastRelocation++;
            }

            int relocationCount = lastRelocation - firstRelocation;
            int paddedRelocationCount = (relocationCount + 1) & ~1;
            int blockSize = 8 + (paddedRelocationCount * sizeof(ushort));
            builder.WriteInt32(pageRva);
            builder.WriteInt32(blockSize);

            for (int i = firstRelocation; i < lastRelocation; i++)
            {
                BaseRelocation relocation = relocations[i];
                int pageOffset = relocation.Rva - pageRva;
                builder.WriteUInt16((ushort)((relocation.Type << 12) | pageOffset));
            }

            if (paddedRelocationCount != relocationCount)
            {
                builder.WriteUInt16(ImageRelBasedAbsolute);
            }

            firstRelocation = lastRelocation;
        }

        _relocationDirectoryRva = location.RelativeVirtualAddress;
        _relocationDirectorySize = builder.Count;
        return builder;
    }

    private void PatchCorHeaderVTableFixups()
    {
        if (_vtableFixups.Length == 0)
        {
            return;
        }

        Debug.Assert(_textSectionBuilder is not null);

        const int VTableFixupsOffset = 48;
        int patchOffset = GetCorHeaderOffset() + VTableFixupsOffset;
        WriteInt32AtOffset(_textSectionBuilder, patchOffset, _vtableFixupsRva);
        WriteInt32AtOffset(_textSectionBuilder, patchOffset + sizeof(int), _vtableFixupsSize);
    }

    private int GetCorHeaderOffset()
    {
        // ManagedTextSection places SizeOfImportAddressTable bytes before the COR header.
        // Keep this machine check synchronized with ManagedTextSection.RequiresStartupStub
        // and ManagedTextSection.SizeOfImportAddressTable in System.Reflection.Metadata.
        return Header.Machine == Machine.I386
            ? 2 * sizeof(int)
            : 0;
    }

    private static int ReadInt32AtOffset(BlobBuilder builder, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(builder, offset, sizeof(int)));

    private static byte[] ReadBytes(BlobBuilder builder, int offset, int count)
    {
        var result = new byte[count];
        int builderOffset = 0;
        int resultOffset = 0;

        foreach (Blob blob in builder.GetBlobs())
        {
            int blobEnd = builderOffset + blob.Length;
            if (offset < blobEnd && offset + count > builderOffset)
            {
                int sourceOffset = Math.Max(offset - builderOffset, 0);
                int copyLength = Math.Min(blob.Length - sourceOffset, count - resultOffset);
                blob.GetBytes().AsSpan(sourceOffset, copyLength)
                    .CopyTo(result.AsSpan(resultOffset, copyLength));
                resultOffset += copyLength;
                if (resultOffset == count)
                {
                    return result;
                }
            }

            builderOffset = blobEnd;
        }

        throw new ArgumentOutOfRangeException(nameof(offset));
    }

    private static void WriteInt32AtOffset(BlobBuilder builder, int offset, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        WriteBytesAtOffset(builder, offset, bytes);
    }

    private static void WriteBytesAtOffset(BlobBuilder builder, int offset, ReadOnlySpan<byte> value)
    {
        int builderOffset = 0;
        int valueOffset = 0;

        foreach (Blob blob in builder.GetBlobs())
        {
            int blobEnd = builderOffset + blob.Length;
            if (offset < blobEnd && offset + value.Length > builderOffset)
            {
                int destinationOffset = Math.Max(offset - builderOffset, 0);
                int copyLength = Math.Min(blob.Length - destinationOffset, value.Length - valueOffset);
                value.Slice(valueOffset, copyLength)
                    .CopyTo(blob.GetBytes().AsSpan(destinationOffset, copyLength));
                valueOffset += copyLength;
                if (valueOffset == value.Length)
                {
                    return;
                }
            }

            builderOffset = blobEnd;
        }

        throw new ArgumentOutOfRangeException(nameof(offset));
    }

}
