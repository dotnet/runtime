// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;

namespace ILAssembler;

/// <summary>
/// Support for VTable fixups and native exports in IL assembly.
/// </summary>
internal static class VTableFixupSupport
{
    // COR_VTABLE_* flags from corhdr.h
    public const ushort COR_VTABLE_32BIT = 0x01;
    public const ushort COR_VTABLE_64BIT = 0x02;
    public const ushort COR_VTABLE_FROM_UNMANAGED = 0x04;
    public const ushort COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN = 0x08;
    public const ushort COR_VTABLE_CALL_MOST_DERIVED = 0x10;

    /// <summary>
    /// Represents a VTable fixup entry parsed from a .vtfixup directive.
    /// </summary>
    /// <param name="SlotCount">Number of slots in this VTable.</param>
    /// <param name="Flags">COR_VTABLE_* flags.</param>
    /// <param name="DataLabel">Label in mapped field data where method tokens are stored.</param>
    public readonly record struct VTableFixupEntry(int SlotCount, ushort Flags, string DataLabel);

    public static int GetSlotSize(ushort flags) =>
        (flags & COR_VTABLE_64BIT) != 0 ? sizeof(long) : sizeof(int);

    public static Machine GetEffectiveMachine(Machine machine) =>
        machine switch
        {
            Machine.Unknown => Machine.I386,
            Machine.Arm => Machine.ArmThumb2,
            _ => machine,
        };

    public static int GetPointerSize(Machine machine) =>
        GetEffectiveMachine(machine) switch
        {
            Machine.Amd64 or
            Machine.IA64 or
            Machine.Arm64 or
            Machine.LoongArch64 or
            Machine.RiscV64 => sizeof(long),
            _ => sizeof(int),
        };
}
