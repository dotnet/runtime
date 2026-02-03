// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace ILAssembler;

/// <summary>
/// Support for VTable fixups and native exports in IL assembly.
/// </summary>
/// <remarks>
/// VTable fixups allow managed methods to be exported as unmanaged entry points
/// that can be called from native code. This is used for:
/// - DllExport functionality (exporting managed methods from a DLL)
/// - COM interop with custom vtables
/// - Reverse P/Invoke scenarios
///
/// The implementation requires:
/// 1. VTableFixups directory in the CLR header - points to an array of VTableFixup entries
/// 2. Each VTableFixup entry contains: RVA to slot data, slot count, and flags
/// 3. The slot data contains method tokens that the runtime patches with method addresses
/// 4. For exports, jump stubs in the text section that indirect through the vtable slots
/// 5. PE Export directory pointing to the jump stubs
/// </remarks>
internal static class VTableFixupSupport
{
    // COR_VTABLE_* flags from corhdr.h
    public const ushort COR_VTABLE_32BIT = 0x01;
    public const ushort COR_VTABLE_64BIT = 0x02;
    public const ushort COR_VTABLE_FROM_UNMANAGED = 0x04;
    public const ushort COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN = 0x08;
    public const ushort COR_VTABLE_CALL_MOST_DERIVED = 0x10;

    /// <summary>
    /// Represents a VTable fixup entry parsed from .vtfixup directive.
    /// </summary>
    /// <param name="SlotCount">Number of slots in this VTable.</param>
    /// <param name="Flags">COR_VTABLE_* flags.</param>
    /// <param name="DataLabel">Label name in the data section where method tokens are stored.</param>
    public readonly record struct VTableFixupEntry(int SlotCount, ushort Flags, string DataLabel);

    /// <summary>
    /// Represents a method export from .export directive.
    /// </summary>
    /// <param name="Ordinal">Export ordinal number.</param>
    /// <param name="Name">Export name (method name or alias).</param>
    /// <param name="MethodToken">Method definition token.</param>
    /// <param name="VTableEntryIndex">1-based index of the VTable fixup entry.</param>
    /// <param name="VTableSlotIndex">1-based slot index within the VTable entry.</param>
    public readonly record struct MethodExport(int Ordinal, string Name, int MethodToken, int VTableEntryIndex, int VTableSlotIndex);

    /// <summary>
    /// Checks if any VTable fixups or exports are defined.
    /// </summary>
    public static bool HasVTableFixupsOrExports(
        ImmutableArray<VTableFixupEntry> vtableFixups,
        ImmutableArray<MethodExport> exports)
    {
        return !vtableFixups.IsDefaultOrEmpty || !exports.IsDefaultOrEmpty;
    }

    /// <summary>
    /// Validates that exports have corresponding vtable entries.
    /// </summary>
    public static void ValidateExports(
        ImmutableArray<VTableFixupEntry> vtableFixups,
        ImmutableArray<MethodExport> exports,
        Action<string> reportError)
    {
        if (exports.IsDefaultOrEmpty)
            return;

        foreach (var export in exports)
        {
            if (export.VTableEntryIndex <= 0 || export.VTableEntryIndex > vtableFixups.Length)
            {
                reportError($"Export '{export.Name}' references invalid VTable entry index {export.VTableEntryIndex}");
                continue;
            }

            var vtfEntry = vtableFixups[export.VTableEntryIndex - 1];
            if (export.VTableSlotIndex <= 0 || export.VTableSlotIndex > vtfEntry.SlotCount)
            {
                reportError($"Export '{export.Name}' references invalid VTable slot {export.VTableSlotIndex} (entry has {vtfEntry.SlotCount} slots)");
            }
        }
    }

    /// <summary>
    /// Calculates the size of the VTableFixups directory data.
    /// </summary>
    /// <remarks>
    /// The VTableFixups directory is an array of IMAGE_COR_VTABLEFIXUP structures:
    /// struct IMAGE_COR_VTABLEFIXUP {
    ///     DWORD RVA;      // RVA of the vtable slot data
    ///     WORD  Count;    // Number of entries
    ///     WORD  Type;     // COR_VTABLE_* flags
    /// };
    /// Total: 8 bytes per entry
    /// </remarks>
    public static int CalculateVTableFixupsDirectorySize(ImmutableArray<VTableFixupEntry> vtableFixups)
    {
        if (vtableFixups.IsDefaultOrEmpty)
            return 0;

        return vtableFixups.Length * 8; // 8 bytes per entry
    }

    /// <summary>
    /// Calculates the size of the vtable slot data (method tokens).
    /// </summary>
    public static int CalculateVTableSlotDataSize(ImmutableArray<VTableFixupEntry> vtableFixups)
    {
        if (vtableFixups.IsDefaultOrEmpty)
            return 0;

        int totalSize = 0;
        foreach (var entry in vtableFixups)
        {
            int slotSize = (entry.Flags & COR_VTABLE_64BIT) != 0 ? 8 : 4;
            totalSize += entry.SlotCount * slotSize;
        }

        return totalSize;
    }

    /// <summary>
    /// Gets the size of an export stub for the given machine type.
    /// </summary>
    public static int GetExportStubSize(Machine machine)
    {
        return machine switch
        {
            Machine.Amd64 => 12,  // mov rax, [addr]; jmp rax
            Machine.I386 => 6,    // jmp [addr]
            Machine.Arm => 8,     // ldr pc, [pc, #0]; addr
            Machine.Arm64 => 12,  // adrp x16, addr; ldr x16, [x16]; br x16
            _ => 0
        };
    }

    /// <summary>
    /// Writes the VTableFixups directory entries to a blob.
    /// </summary>
    /// <param name="builder">The blob builder to write to.</param>
    /// <param name="vtableFixups">The vtable fixup entries.</param>
    /// <param name="slotDataRvas">RVAs of the slot data for each entry.</param>
    public static void WriteVTableFixupsDirectory(
        BlobBuilder builder,
        ImmutableArray<VTableFixupEntry> vtableFixups,
        ReadOnlySpan<int> slotDataRvas)
    {
        if (vtableFixups.IsDefaultOrEmpty)
            return;

        for (int i = 0; i < vtableFixups.Length; i++)
        {
            var entry = vtableFixups[i];
            builder.WriteInt32(slotDataRvas[i]);        // RVA
            builder.WriteUInt16((ushort)entry.SlotCount); // Count
            builder.WriteUInt16(entry.Flags);             // Type
        }
    }

    /// <summary>
    /// Writes the vtable slot data (method tokens) to a blob.
    /// </summary>
    /// <param name="builder">The blob builder to write to.</param>
    /// <param name="vtableFixups">The vtable fixup entries.</param>
    /// <param name="getMethodToken">Function to get method token for a given vtable entry and slot.</param>
    public static void WriteVTableSlotData(
        BlobBuilder builder,
        ImmutableArray<VTableFixupEntry> vtableFixups,
        Func<int, int, int> getMethodToken)
    {
        if (vtableFixups.IsDefaultOrEmpty)
            return;

        for (int entryIndex = 0; entryIndex < vtableFixups.Length; entryIndex++)
        {
            var entry = vtableFixups[entryIndex];
            bool is64Bit = (entry.Flags & COR_VTABLE_64BIT) != 0;

            for (int slotIndex = 0; slotIndex < entry.SlotCount; slotIndex++)
            {
                int token = getMethodToken(entryIndex + 1, slotIndex + 1);
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
    }

    /// <summary>
    /// Writes an export stub for AMD64.
    /// </summary>
    public static void WriteExportStubAmd64(BlobBuilder builder, long vtableSlotAddress)
    {
        // mov rax, [vtableSlotAddress]
        builder.WriteByte(0x48); // REX.W
        builder.WriteByte(0xA1); // mov rax, moffs64
        builder.WriteInt64(vtableSlotAddress);
        // jmp rax
        builder.WriteByte(0xFF);
        builder.WriteByte(0xE0);
    }

    /// <summary>
    /// Writes an export stub for x86.
    /// </summary>
    public static void WriteExportStubX86(BlobBuilder builder, int vtableSlotAddress)
    {
        // jmp [vtableSlotAddress]
        builder.WriteByte(0xFF);
        builder.WriteByte(0x25);
        builder.WriteInt32(vtableSlotAddress);
    }

    /// <summary>
    /// Writes an export stub for ARM (Thumb-2).
    /// </summary>
    public static void WriteExportStubArm(BlobBuilder builder, int vtableSlotAddress)
    {
        // ldr pc, [pc, #0]
        builder.WriteUInt16(0xF8DF);
        builder.WriteUInt16(0xF000);
        // address
        builder.WriteInt32(vtableSlotAddress);
    }
}
