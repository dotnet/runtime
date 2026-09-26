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
/// A PE builder that extends <see cref="ManagedPEBuilder"/> with VTable fixups,
/// unmanaged exports, and data label reference fixups.
/// </summary>
internal sealed class VTableExportPEBuilder : ManagedPEBuilder
{
    private const string RelocationSectionName = ".reloc";
    private const string SDataSectionName = ".sdata";
    private const string TextSectionName = ".text";
    private const int ExportDirectoryHeaderSize = 40;
    private const int RelocationPageSize = 0x1000;
    private const ushort ImageRelBasedAbsolute = 0;
    private const ushort ImageRelBasedHighLow = 3;
    private const ushort ImageRelBasedDir64 = 10;

    private readonly ImmutableArray<VTableFixupInfo> _vtableFixups;
    private readonly ImmutableArray<ExportInfo> _exports;
    private readonly BlobBuilder? _mappedFieldData;
    private readonly ImmutableArray<DataLabelFixup> _dataLabelFixups;
    private readonly string _dllName;
    private readonly bool _moveMappedFieldDataToSData;
    private readonly List<ExportStubInfo> _exportStubs = new();

    private BlobBuilder? _textSectionBuilder;
    private int _textSectionRva;
    private int _managedTextSize;
    private int _mappedFieldDataRva;
    private int _vtableFixupsRva;
    private int _vtableFixupsSize;
    private int _exportDirectoryRva;
    private int _exportDirectorySize;
    private int _relocationDirectoryRva;
    private int _relocationDirectorySize;

    public readonly record struct VTableFixupInfo(
        int DataOffset,
        int SlotCount,
        ushort Flags,
        ImmutableArray<int> MethodTokens);

    public readonly record struct ExportInfo(
        int Ordinal,
        string Name,
        int VTableEntryIndex,
        int VTableSlotIndex);

    public readonly record struct DataLabelFixup(
        int DataOffset,
        int TargetOffset,
        int PointerSize);

    private readonly record struct ExportStubInfo(int StubRva, int AddressRva, Blob AddressFixup);

    private readonly record struct BaseRelocation(int Rva, ushort Type);

    public VTableExportPEBuilder(
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
        string? dllName = null)
        : base(
            header,
            metadataRootBuilder,
            ilStream,
            mappedFieldData: vtableFixups.IsDefaultOrEmpty ? mappedFieldData : null,
            managedResources,
            nativeResources,
            debugDirectoryBuilder,
            strongNameSignatureSize,
            entryPoint,
            vtableFixups.IsDefaultOrEmpty && dataLabelFixups.IsDefaultOrEmpty
                ? flags
                : (flags & ~CorFlags.ILOnly),
            deterministicIdProvider)
    {
        _vtableFixups = vtableFixups.IsDefault ? ImmutableArray<VTableFixupInfo>.Empty : vtableFixups;
        _exports = exports.IsDefault ? ImmutableArray<ExportInfo>.Empty : exports;
        _mappedFieldData = mappedFieldData;
        _dataLabelFixups = dataLabelFixups.IsDefault
            ? ImmutableArray<DataLabelFixup>.Empty
            : dataLabelFixups;
        _dllName = dllName ?? "output.dll";
        _moveMappedFieldDataToSData = !_vtableFixups.IsEmpty;
    }

    internal static bool IsExportMachineSupported(Machine machine) =>
        VTableFixupSupport.GetEffectiveMachine(machine) is
            Machine.I386 or
            Machine.Amd64 or
            Machine.Arm64;

    protected override ImmutableArray<Section> CreateSections()
    {
        ImmutableArray<Section> baseSections = base.CreateSections();
        bool hasRelocationSection = false;
        var builder = ImmutableArray.CreateBuilder<Section>(baseSections.Length + 2);

        for (int i = 0; i < baseSections.Length; i++)
        {
            Section section = baseSections[i];
            builder.Add(section);

            if (i == 0 && _moveMappedFieldDataToSData)
            {
                builder.Add(new Section(
                    SDataSectionName,
                    SectionCharacteristics.MemRead |
                    SectionCharacteristics.MemWrite |
                    SectionCharacteristics.ContainsInitializedData));
            }

            hasRelocationSection |= section.Name == RelocationSectionName;
        }

        if ((_exports.Length > 0 || !_dataLabelFixups.IsEmpty) && !hasRelocationSection)
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
                _managedTextSize = textBuilder.Count;
                if (!_moveMappedFieldDataToSData && (_mappedFieldData?.Count ?? 0) > 0)
                {
                    _mappedFieldDataRva =
                        location.RelativeVirtualAddress +
                        textBuilder.Count -
                        _mappedFieldData!.Count;
                    ApplyDataLabelFixups(
                        textBuilder,
                        _mappedFieldDataRva - location.RelativeVirtualAddress);
                }

                SerializeExportStubs(textBuilder, location.RelativeVirtualAddress);
                return textBuilder;

            case SDataSectionName:
                return SerializeSDataSection(location);

            case RelocationSectionName:
                return SerializeRelocationSection(location);

            default:
                return base.SerializeSection(name, location);
        }
    }

    protected override PEDirectoriesBuilder GetDirectories()
    {
        PEDirectoriesBuilder directories = base.GetDirectories();

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

        return directories;
    }

    private void SerializeExportStubs(BlobBuilder builder, int textSectionRva)
    {
        Machine machine = VTableFixupSupport.GetEffectiveMachine(Header.Machine);

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
        int mappedFieldDataOffset = Align(vtableDirectorySize, ManagedPEBuilder.MappedFieldDataAlignment);
        _mappedFieldDataRva = location.RelativeVirtualAddress + mappedFieldDataOffset;

        if (_vtableFixups.Length > 0)
        {
            _vtableFixupsRva = location.RelativeVirtualAddress;
            _vtableFixupsSize = vtableDirectorySize;

            foreach (VTableFixupInfo fixup in _vtableFixups)
            {
                builder.WriteInt32(_mappedFieldDataRva + fixup.DataOffset);
                builder.WriteUInt16((ushort)fixup.SlotCount);
                builder.WriteUInt16(fixup.Flags);
            }
        }

        if (_mappedFieldData is not null)
        {
            if (_mappedFieldData.Count > 0)
            {
                builder.Align(ManagedPEBuilder.MappedFieldDataAlignment);
                Debug.Assert(builder.Count == mappedFieldDataOffset);

                ApplyDataLabelFixups(_mappedFieldData, dataOffset: 0);
                byte[] mappedFieldData = _mappedFieldData.ToArray();
                ApplyVTableTokens(mappedFieldData);
                builder.WriteBytes(mappedFieldData);
            }

            PatchMappedFieldRvas();
        }

        PatchExportStubAddresses();

        if (_exports.Length > 0)
        {
            builder.Align(sizeof(int));
            SerializeExportDirectory(builder, location.RelativeVirtualAddress);
        }

        PatchCorHeaderVTableFixups();
        return builder;
    }

    private void ApplyDataLabelFixups(BlobBuilder builder, int dataOffset)
    {
        if (_dataLabelFixups.IsEmpty)
        {
            return;
        }

        foreach (DataLabelFixup fixup in _dataLabelFixups)
        {
            int targetRva = _mappedFieldDataRva + fixup.TargetOffset;
            ulong targetAddress = Header.ImageBase + (uint)targetRva;
            if (fixup.PointerSize == sizeof(long))
            {
                WriteUInt64AtOffset(
                    builder,
                    dataOffset + fixup.DataOffset,
                    targetAddress);
            }
            else
            {
                WriteInt32AtOffset(
                    builder,
                    dataOffset + fixup.DataOffset,
                    unchecked((int)(uint)targetAddress));
            }
        }
    }

    private void ApplyVTableTokens(Span<byte> mappedFieldData)
    {
        foreach (VTableFixupInfo fixup in _vtableFixups)
        {
            int slotSize = VTableFixupSupport.GetSlotSize(fixup.Flags);
            for (int slotIndex = 0; slotIndex < fixup.SlotCount; slotIndex++)
            {
                int slotOffset = fixup.DataOffset + (slotIndex * slotSize);
                int token = fixup.MethodTokens[slotIndex];
                if (token == 0)
                {
                    continue;
                }

                if (slotSize == sizeof(long))
                {
                    BinaryPrimitives.WriteInt64LittleEndian(
                        mappedFieldData.Slice(slotOffset, sizeof(long)),
                        token);
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        mappedFieldData.Slice(slotOffset, sizeof(int)),
                        token);
                }
            }
        }
    }

    private void PatchMappedFieldRvas()
    {
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
        if (rowCount == 0)
        {
            return;
        }

        int tableOffset = reader.GetTableMetadataOffset(TableIndex.FieldRva);
        int rowSize = reader.GetTableRowSize(TableIndex.FieldRva);
        int assumedMappedFieldDataRva = _textSectionRva + _managedTextSize;
        int rvaDelta = _mappedFieldDataRva - assumedMappedFieldDataRva;

        for (int row = 0; row < rowCount; row++)
        {
            int rvaOffset = metadataOffset + tableOffset + (row * rowSize);
            int currentRva = ReadInt32AtOffset(_textSectionBuilder, rvaOffset);
            WriteInt32AtOffset(_textSectionBuilder, rvaOffset, checked(currentRva + rvaDelta));
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
                _mappedFieldDataRva +
                fixup.DataOffset +
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
        Machine machine = VTableFixupSupport.GetEffectiveMachine(Header.Machine);
        ushort exportRelocationType = machine is Machine.Amd64 or Machine.Arm64
            ? ImageRelBasedDir64
            : ImageRelBasedHighLow;
        var relocations = new List<BaseRelocation>(
            _exportStubs.Count + _dataLabelFixups.Length + 1);

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
                _mappedFieldDataRva + fixup.DataOffset,
                fixup.PointerSize == sizeof(long)
                    ? ImageRelBasedDir64
                    : ImageRelBasedHighLow));
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
        return VTableFixupSupport.GetEffectiveMachine(Header.Machine) == Machine.I386
            ? 2 * sizeof(int)
            : 0;
    }

    private static int Align(int value, int alignment) =>
        checked((value + alignment - 1) & ~(alignment - 1));

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

    private static void WriteUInt64AtOffset(BlobBuilder builder, int offset, ulong value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
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
