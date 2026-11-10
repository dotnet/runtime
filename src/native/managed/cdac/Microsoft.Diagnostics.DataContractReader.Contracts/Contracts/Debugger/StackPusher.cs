// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public static class StackPusher
{
    public static TargetPointer Push(Target target, ref TargetPointer sp, Span<byte> bytes, bool align)
    {
        if (align)
        {
            AlignStackPointer(target, ref sp);
        }

        sp = new TargetPointer(sp.Value - (ulong)bytes.Length);

        if (align)
        {
            AlignStackPointer(target, ref sp);
        }

        target.WriteBuffer(sp.Value, bytes);
        return sp;
    }

    public static TargetPointer PushSlot(Target target, ref TargetPointer sp, TargetNUInt value, bool align)
    {
        if (align)
        {
            AlignStackPointer(target, ref sp);
        }

        sp = new TargetPointer(sp.Value - (ulong)target.PointerSize);

        if (align)
        {
            AlignStackPointer(target, ref sp);
        }

        target.WriteNUInt(sp.Value, value);
        return sp;
    }

    private static uint GetStackAlignment(Target target)
    {
        RuntimeInfoArchitecture arch = target.Contracts.RuntimeInfo.GetTargetArchitecture();
        RuntimeInfoOperatingSystem os = target.Contracts.RuntimeInfo.GetTargetOperatingSystem();

        return arch switch
        {
            // Windows x86 (cdecl/stdcall) only requires 4-byte SP alignment.
            // System V i386 ABI requires 16-byte alignment at call sites.
            RuntimeInfoArchitecture.X86 => os == RuntimeInfoOperatingSystem.Windows ? 4u : 16u,
            RuntimeInfoArchitecture.X64 => 16,
            RuntimeInfoArchitecture.Arm => 8, // AAPCS32: 8-byte aligned at public interfaces
            RuntimeInfoArchitecture.Arm64 => 16,
            RuntimeInfoArchitecture.LoongArch64 => 16,
            RuntimeInfoArchitecture.RiscV64 => 16,
            _ => throw new NotSupportedException($"StackPusher does not know the stack alignment for architecture '{arch}'."),
        };
    }

    private static void AlignStackPointer(Target target, ref TargetPointer sp)
    {
        uint alignment = GetStackAlignment(target);
        ulong mask = ~((ulong)alignment - 1UL);
        sp = new TargetPointer(sp.Value & mask);
    }

}
