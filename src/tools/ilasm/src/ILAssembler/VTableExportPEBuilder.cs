// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILAssembler;

/// <summary>
/// A PE builder that extends ManagedPEBuilder to support VTable fixups, unmanaged exports,
/// and data label reference fixups.
/// </summary>
/// <remarks>
/// This builder extends ManagedPEBuilder with additional features:
/// 1. VTable fixups - adds an .sdata section containing VTableFixups directory and slot data
/// 2. Export stubs - jump thunks that indirect through vtable slots
/// 3. PE Export Directory - for native callers to find exported methods
/// 4. Data label fixups - patches references from one .data label to another with correct RVAs
///
/// For VTable fixups:
/// - The runtime patches the token slots with actual method addresses at load time
///
/// For exports:
/// - Export stubs are small pieces of machine code that jump through vtable slots
/// - The PE Export Directory lists the exports by name and ordinal
/// - Native code calls the export stubs, which redirect through the vtable
///
/// For data label fixups:
/// - When .data contains a reference like `&amp;Label`, the reference is patched with the
///   correct RVA of the target label in the mapped field data section.
/// </remarks>
internal sealed class VTableExportPEBuilder : ManagedPEBuilder
{
    private const string TextSectionName = ".text";
    private const string SDataSectionName = ".sdata";

    private readonly ImmutableArray<VTableFixupInfo> _vtableFixups;
    private readonly ImmutableArray<ExportInfo> _exports;
    private readonly Dictionary<string, int> _mappedFieldDataOffsets;
    private readonly BlobBuilder? _mappedFieldData;
    private readonly IReadOnlyDictionary<string, List<Blob>>? _dataLabelFixups;
    private readonly string _dllName;

    // Sizes needed to calculate mapped field data RVA
    private readonly int _ilStreamSize;
    private readonly int _metadataSize;
    private readonly int _managedResourcesSize;
    private readonly int _strongNameSignatureSize;
    private readonly int _debugDataSize;

    // Calculated during serialization
    private int _sdataRva;
    private int _sdataSize;
    private BlobBuilder? _textSectionBuilder;
    private int _textSectionRva;

    // Export-related state
    private int _exportDirectoryRva;
    private int _exportDirectorySize;

    /// <summary>
    /// Information about a VTable fixup entry.
    /// </summary>
    public readonly record struct VTableFixupInfo(
        string DataLabel,
        int SlotCount,
        ushort Flags,
        ImmutableArray<int> MethodTokens);

    /// <summary>
    /// Information about an unmanaged export.
    /// </summary>
    public readonly record struct ExportInfo(
        int Ordinal,
        string Name,
        int MethodToken,
        int VTableEntryIndex,  // 1-based
        int VTableSlotIndex);  // 1-based

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
        Dictionary<string, int>? mappedFieldDataOffsets = null,
        IReadOnlyDictionary<string, List<Blob>>? dataLabelFixups = null,
        int metadataSize = 0,
        int debugDataSize = 0,
        string? dllName = null)
        : base(header, metadataRootBuilder, ilStream, mappedFieldData, managedResources,
               nativeResources, debugDirectoryBuilder, strongNameSignatureSize, entryPoint,
               // Clear ILOnly flag if we have vtable fixups - mixed mode assembly
               vtableFixups.IsDefaultOrEmpty ? flags : (flags & ~CorFlags.ILOnly),
               deterministicIdProvider)
    {
        _vtableFixups = vtableFixups.IsDefault ? ImmutableArray<VTableFixupInfo>.Empty : vtableFixups;
        _exports = exports.IsDefault ? ImmutableArray<ExportInfo>.Empty : exports;
        _mappedFieldDataOffsets = mappedFieldDataOffsets ?? new Dictionary<string, int>();
        _mappedFieldData = mappedFieldData;
        _dataLabelFixups = dataLabelFixups;
        _dllName = dllName ?? "output.dll";

        // Store sizes needed for RVA calculation
        _ilStreamSize = ilStream.Count;
        _metadataSize = metadataSize;
        _managedResourcesSize = managedResources?.Count ?? 0;
        _strongNameSignatureSize = strongNameSignatureSize;
        _debugDataSize = debugDataSize;
    }

    protected override ImmutableArray<Section> CreateSections()
    {
        var baseSections = base.CreateSections();

        // If we have vtable fixups, add .sdata section
        if (_vtableFixups.Length > 0)
        {
            var builder = ImmutableArray.CreateBuilder<Section>(baseSections.Length + 1);

            // Add .text section first
            builder.Add(baseSections[0]);

            // Add .sdata section for VTable fixup data (must be read/write for runtime patching)
            builder.Add(new Section(SDataSectionName,
                SectionCharacteristics.MemRead |
                SectionCharacteristics.MemWrite |
                SectionCharacteristics.ContainsInitializedData));

            // Add remaining sections
            for (int i = 1; i < baseSections.Length; i++)
            {
                builder.Add(baseSections[i]);
            }

            return builder.ToImmutable();
        }

        return baseSections;
    }

    protected override BlobBuilder SerializeSection(string name, SectionLocation location)
    {
        if (name == TextSectionName)
        {
            // Apply data label fixups before serializing the text section
            ApplyDataLabelFixups(location);

            // Serialize the text section
            var builder = base.SerializeSection(name, location);

            // Store for later patching
            _textSectionBuilder = builder;
            _textSectionRva = location.RelativeVirtualAddress;

            return builder;
        }

        if (name == SDataSectionName)
        {
            var builder = SerializeSDataSection(location);

            // Now that we have the .sdata RVA, patch the COR header's VTableFixups directory
            if (_textSectionBuilder is not null && _vtableFixups.Length > 0)
            {
                PatchCorHeaderVTableFixups(_textSectionBuilder, _textSectionRva);
            }

            return builder;
        }

        return base.SerializeSection(name, location);
    }

    /// <summary>
    /// Patches the COR header's VTableFixups directory entry in the already-serialized text section.
    /// </summary>
    private void PatchCorHeaderVTableFixups(BlobBuilder textSection, int _)
    {
        // The COR header is at offset SizeOfImportAddressTable in the text section
        // VTableFixups directory is at offset 52 within the COR header (after CodeManagerTable at 44)
        bool is32Bit = Header.Machine == Machine.I386 || Header.Machine == 0;
        int sizeOfImportAddressTable = (is32Bit || Header.Machine == 0) ? 8 : 0;

        // COR header offset in text section
        int corHeaderOffset = sizeOfImportAddressTable;

        // VTableFixups directory entry is at offset 52 within COR header
        const int vtableFixupsOffset = 52;
        int patchOffset = corHeaderOffset + vtableFixupsOffset;

        // Find the blob containing this offset and patch it
        int currentOffset = 0;
        foreach (var blob in textSection.GetBlobs())
        {
            int blobEnd = currentOffset + blob.Length;
            if (patchOffset >= currentOffset && patchOffset + 8 <= blobEnd)
            {
                // Patch within this blob
                var bytes = blob.GetBytes();
                int relativeOffset = patchOffset - currentOffset;

                // Write VTableFixups RVA (4 bytes)
                bytes.Array![bytes.Offset + relativeOffset + 0] = (byte)(_sdataRva & 0xFF);
                bytes.Array[bytes.Offset + relativeOffset + 1] = (byte)((_sdataRva >> 8) & 0xFF);
                bytes.Array[bytes.Offset + relativeOffset + 2] = (byte)((_sdataRva >> 16) & 0xFF);
                bytes.Array[bytes.Offset + relativeOffset + 3] = (byte)((_sdataRva >> 24) & 0xFF);

                // Write VTableFixups size (4 bytes)
                bytes.Array[bytes.Offset + relativeOffset + 4] = (byte)(_sdataSize & 0xFF);
                bytes.Array[bytes.Offset + relativeOffset + 5] = (byte)((_sdataSize >> 8) & 0xFF);
                bytes.Array[bytes.Offset + relativeOffset + 6] = (byte)((_sdataSize >> 16) & 0xFF);
                bytes.Array[bytes.Offset + relativeOffset + 7] = (byte)((_sdataSize >> 24) & 0xFF);

                return;
            }
            currentOffset = blobEnd;
        }
    }

    /// <summary>
    /// Override to add export directory entry if we have exports.
    /// </summary>
    protected override PEDirectoriesBuilder GetDirectories()
    {
        var directories = base.GetDirectories();

        // Add export directory if we have exports
        if (_exportDirectoryRva != 0 && _exportDirectorySize != 0)
        {
            directories.ExportTable = new DirectoryEntry(_exportDirectoryRva, _exportDirectorySize);
        }

        return directories;
    }

    /// <summary>
    /// Applies fixups for data label references (e.g., .data Ptr = &amp;Label).
    /// </summary>
    private void ApplyDataLabelFixups(SectionLocation textSectionLocation)
    {
        if (_dataLabelFixups is null || _dataLabelFixups.Count == 0 || _mappedFieldData is null)
        {
            return;
        }

        // Calculate the RVA of the mapped field data within the text section
        int mappedFieldDataOffset = CalculateMappedFieldDataOffset();
        int mappedFieldDataRva = textSectionLocation.RelativeVirtualAddress + mappedFieldDataOffset;

        // Apply each fixup
        foreach (var (labelName, fixupBlobs) in _dataLabelFixups)
        {
            if (!_mappedFieldDataOffsets.TryGetValue(labelName, out int labelOffset))
            {
                // Label not found - skip (should have been caught during parsing)
                continue;
            }

            int targetRva = mappedFieldDataRva + labelOffset;

            foreach (var fixupBlob in fixupBlobs)
            {
                // Write the target RVA to the reserved fixup location
                var writer = new BlobWriter(fixupBlob);
                writer.WriteInt32(targetRva);
            }
        }
    }

    /// <summary>
    /// Calculates the offset to mapped field data within the text section.
    /// </summary>
    /// <remarks>
    /// The text section layout is:
    /// - Import Address Table (8 bytes for 32-bit, 16 for 64-bit, or 0 if not needed)
    /// - COR Header (72 bytes)
    /// - IL Stream (aligned to 4)
    /// - Metadata
    /// - Managed Resources
    /// - Strong Name Signature
    /// - Debug Data
    /// - Import Table + Name Table + Runtime Startup Stub (if needed)
    /// - Mapped Field Data (aligned to 8)
    /// </remarks>
    private int CalculateMappedFieldDataOffset()
    {
        bool is32Bit = Header.Machine == Machine.I386 || Header.Machine == 0;
        bool requiresStartupStub = is32Bit || Header.Machine == 0;

        // Import Address Table size
        int sizeOfImportAddressTable = requiresStartupStub ? (is32Bit ? 8 : 16) : 0;

        // COR Header size (fixed at 72 bytes)
        const int corHeaderSize = 72;

        // Offset to IL stream
        int offset = sizeOfImportAddressTable + corHeaderSize;

        // IL stream (aligned to 4)
        offset += Align(_ilStreamSize, 4);

        // Metadata
        offset += _metadataSize;

        // Managed resources
        offset += _managedResourcesSize;

        // Strong name signature
        offset += _strongNameSignatureSize;

        // Debug data
        offset += _debugDataSize;

        // Import table, name table, and startup stub (if needed)
        if (requiresStartupStub)
        {
            // Import table size (matches ManagedTextSection.SizeOfImportTable)
            // 32-bit: 4+4+4+4+4+20+12+2+11+1 = 66
            // 64-bit: 4+4+4+4+4+20+16+2+11+1 = 70
            int sizeOfImportTable = is32Bit ? 66 : 70;

            // Name table size: "mscoree.dll" + NUL + hint = 11+1+2 = 14 bytes
            const int sizeOfNameTable = 14;

            offset += sizeOfImportTable + sizeOfNameTable;

            // Align for startup stub
            offset = Align(offset, is32Bit ? 4 : 8);

            // Startup stub size
            int startupStubSize = is32Bit ? 8 : 16;
            offset += startupStubSize;
        }

        // Align for mapped field data (if present)
        if (_mappedFieldData is not null && _mappedFieldData.Count > 0)
        {
            offset = Align(offset, 8);
        }

        return offset;
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private BlobBuilder SerializeSDataSection(SectionLocation location)
    {
        var builder = new BlobBuilder();

        if (_vtableFixups.IsEmpty)
        {
            return builder;
        }

        _sdataRva = location.RelativeVirtualAddress;

        // Calculate sizes for VTableFixups directory
        int vtfDirSize = _vtableFixups.Length * 8; // 8 bytes per IMAGE_COR_VTABLEFIXUP entry

        // Calculate slot data size and build slot offset map
        var slotOffsets = new Dictionary<(int EntryIndex, int SlotIndex), int>();
        int slotDataOffset = vtfDirSize;

        for (int entryIndex = 0; entryIndex < _vtableFixups.Length; entryIndex++)
        {
            var vtf = _vtableFixups[entryIndex];
            bool is64Bit = (vtf.Flags & VTableFixupSupport.COR_VTABLE_64BIT) != 0;
            int slotSize = is64Bit ? 8 : 4;

            for (int slotIndex = 0; slotIndex < vtf.SlotCount; slotIndex++)
            {
                slotOffsets[(entryIndex + 1, slotIndex + 1)] = slotDataOffset + slotIndex * slotSize;
            }
            slotDataOffset += vtf.SlotCount * slotSize;
        }

        int slotDataEndOffset = slotDataOffset;

        // Calculate export-related sizes
        int exportStubsOffset = slotDataEndOffset;
        int numExports = _exports.Length;
        int exportStubSize = GetExportStubSize();
        int exportStubsTotalSize = numExports * exportStubSize;

        // Export directory comes after export stubs
        int exportDirOffset = Align(exportStubsOffset + exportStubsTotalSize, 4);

        // Export directory structure:
        // - IMAGE_EXPORT_DIRECTORY (40 bytes)
        // - Export Address Table (4 bytes per export)
        // - Export Name Pointer Table (4 bytes per export)
        // - Export Ordinal Table (2 bytes per export)
        // - Export names (null-terminated strings)
        // - DLL name (null-terminated string)
        int exportAddrTableOffset = exportDirOffset + 40;
        int exportNamePtrTableOffset = exportAddrTableOffset + numExports * 4;
        int exportOrdinalTableOffset = exportNamePtrTableOffset + numExports * 4;

        // Calculate name table size
        int nameTableSize = 0;
        foreach (var export in _exports)
        {
            nameTableSize += Encoding.ASCII.GetByteCount(export.Name) + 1;
        }
        int dllNameSize = Encoding.ASCII.GetByteCount(_dllName) + 1;

        int exportNamesOffset = exportOrdinalTableOffset + numExports * 2;
        int dllNameOffset = exportNamesOffset + nameTableSize;
        int exportDirTotalSize = numExports > 0 ? (dllNameOffset + dllNameSize - exportDirOffset) : 0;

        // Store total size for COR header patching (only vtfixup directory, not stubs/exports)
        _sdataSize = vtfDirSize;

        // Write VTableFixups directory (array of IMAGE_COR_VTABLEFIXUP structures)
        int currentSlotDataOffset = vtfDirSize;
        foreach (var vtf in _vtableFixups)
        {
            int slotDataRva = location.RelativeVirtualAddress + currentSlotDataOffset;
            builder.WriteInt32(slotDataRva);              // RVA to slot data
            builder.WriteUInt16((ushort)vtf.SlotCount);   // Count
            builder.WriteUInt16(vtf.Flags);               // Type/Flags

            bool is64Bit = (vtf.Flags & VTableFixupSupport.COR_VTABLE_64BIT) != 0;
            int slotSize = is64Bit ? 8 : 4;
            currentSlotDataOffset += vtf.SlotCount * slotSize;
        }

        // Write slot data (method tokens that get patched by the runtime)
        foreach (var vtf in _vtableFixups)
        {
            bool is64Bit = (vtf.Flags & VTableFixupSupport.COR_VTABLE_64BIT) != 0;

            for (int i = 0; i < vtf.SlotCount; i++)
            {
                int token = i < vtf.MethodTokens.Length ? vtf.MethodTokens[i] : 0;
                if (is64Bit)
                {
                    builder.WriteInt64(token);
                }
                else
                {
                    builder.WriteInt32(token);
                }
            }
        }

        // Write export stubs if we have exports
        if (numExports > 0)
        {
            var exportStubRvas = new int[numExports];

            for (int i = 0; i < numExports; i++)
            {
                var export = _exports[i];

                // Find the vtable slot address for this export
                if (!slotOffsets.TryGetValue((export.VTableEntryIndex, export.VTableSlotIndex), out int slotOffset))
                {
                    // Export doesn't have a valid vtable reference, skip
                    continue;
                }

                int slotRva = location.RelativeVirtualAddress + slotOffset;
                exportStubRvas[i] = location.RelativeVirtualAddress + exportStubsOffset + i * exportStubSize;

                // Write the export stub
                WriteExportStub(builder, slotRva);
            }

            // Align for export directory
            builder.Align(4);

            // Record export directory location
            _exportDirectoryRva = location.RelativeVirtualAddress + builder.Count;

            // Write IMAGE_EXPORT_DIRECTORY
            int baseOrdinal = int.MaxValue;
            int maxOrdinal = 0;
            foreach (var export in _exports)
            {
                if (export.Ordinal < baseOrdinal) baseOrdinal = export.Ordinal;
                if (export.Ordinal > maxOrdinal) maxOrdinal = export.Ordinal;
            }
            if (baseOrdinal == int.MaxValue) baseOrdinal = 1;
            int numFunctions = maxOrdinal - baseOrdinal + 1;

            int exportDirStart = builder.Count;

            builder.WriteUInt32(0);                    // Characteristics
            builder.WriteUInt32(0);                    // TimeDateStamp (filled later or 0)
            builder.WriteUInt16(0);                    // MajorVersion
            builder.WriteUInt16(0);                    // MinorVersion
            builder.WriteInt32(location.RelativeVirtualAddress + exportDirStart + 40 +
                numExports * 4 + numExports * 4 + numExports * 2 + nameTableSize); // Name RVA (DLL name)
            builder.WriteInt32(baseOrdinal);          // Base
            builder.WriteInt32(numExports);           // NumberOfFunctions
            builder.WriteInt32(numExports);           // NumberOfNames
            builder.WriteInt32(location.RelativeVirtualAddress + exportDirStart + 40); // AddressOfFunctions
            builder.WriteInt32(location.RelativeVirtualAddress + exportDirStart + 40 + numExports * 4); // AddressOfNames
            builder.WriteInt32(location.RelativeVirtualAddress + exportDirStart + 40 + numExports * 4 * 2); // AddressOfNameOrdinals

            // Sort exports by name for binary search
            var sortedExports = _exports.AsSpan().ToArray();
            Array.Sort(sortedExports, (a, b) => string.CompareOrdinal(a.Name, b.Name));

            // Write Export Address Table (RVAs to stubs)
            var exportsArray = _exports.AsSpan().ToArray();
            foreach (var export in sortedExports)
            {
                int stubIndex = Array.FindIndex(exportsArray, e => e.Ordinal == export.Ordinal);
                builder.WriteInt32(exportStubRvas[stubIndex]);
            }

            // Write Export Name Pointer Table (RVAs to names)
            int nameOffset = location.RelativeVirtualAddress + exportDirStart + 40 +
                numExports * 4 + numExports * 4 + numExports * 2;
            foreach (var export in sortedExports)
            {
                builder.WriteInt32(nameOffset);
                nameOffset += Encoding.ASCII.GetByteCount(export.Name) + 1;
            }

            // Write Export Ordinal Table
            for (int i = 0; i < numExports; i++)
            {
                builder.WriteUInt16((ushort)(sortedExports[i].Ordinal - baseOrdinal));
            }

            // Write export names
            foreach (var export in sortedExports)
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(export.Name);
                builder.WriteBytes(nameBytes);
                builder.WriteByte(0); // null terminator
            }

            // Write DLL name
            byte[] dllNameBytes = Encoding.ASCII.GetBytes(_dllName);
            builder.WriteBytes(dllNameBytes);
            builder.WriteByte(0); // null terminator

            _exportDirectorySize = builder.Count - exportDirStart;
        }

        return builder;
    }

    /// <summary>
    /// Gets the size of an export stub for the current machine type.
    /// </summary>
    private int GetExportStubSize()
    {
        var machine = Header.Machine == 0 ? Machine.I386 : Header.Machine;
        int size = VTableFixupSupport.GetExportStubSize(machine);
        // VTableFixupSupport returns 12 for ARM64 but we need 16 for our implementation
        return machine == Machine.Arm64 ? 16 : (size == 0 ? 6 : size);
    }

    /// <summary>
    /// Writes an export stub that jumps through a vtable slot.
    /// </summary>
    private void WriteExportStub(BlobBuilder builder, int vtableSlotRva)
    {
        // Calculate absolute address (RVA + ImageBase)
        long absoluteAddress = (long)Header.ImageBase + vtableSlotRva;

        switch (Header.Machine)
        {
            case Machine.Amd64:
                VTableFixupSupport.WriteExportStubAmd64(builder, absoluteAddress);
                break;

            case Machine.I386:
            case 0: // Default to x86
                VTableFixupSupport.WriteExportStubX86(builder, (int)absoluteAddress);
                break;

            case Machine.Arm:
                VTableFixupSupport.WriteExportStubArm(builder, (int)absoluteAddress);
                break;

            case Machine.Arm64:
                // ARM64 is more complex - use inline implementation
                // ldr x16, [literal]; br x16
                builder.WriteUInt32(0x58000050); // ldr x16, #8
                builder.WriteUInt32(0xD61F0200); // br x16
                builder.WriteInt64(absoluteAddress);
                break;
        }
    }

    /// <summary>
    /// Gets the RVA of the VTableFixups directory after serialization.
    /// </summary>
    public int VTableFixupsRva => _sdataRva;

    /// <summary>
    /// Gets the size of the VTableFixups directory.
    /// </summary>
    public int VTableFixupsSize => _sdataSize;

    /// <summary>
    /// Gets the RVA of the export directory after serialization.
    /// </summary>
    public int ExportDirectoryRva => _exportDirectoryRva;

    /// <summary>
    /// Gets the size of the export directory.
    /// </summary>
    public int ExportDirectorySize => _exportDirectorySize;
}
