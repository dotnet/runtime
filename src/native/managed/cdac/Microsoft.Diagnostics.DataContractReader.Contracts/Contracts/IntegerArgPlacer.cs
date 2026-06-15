// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public static class IntegerArgPlacer
{
    // Places integer arguments into the appropriate registers and stack slots for the native ABI.
    public static void PlaceArgs(Target target, IPlatformAgnosticContext ctx, ref TargetPointer sp, ReadOnlySpan<TargetNUInt> args)
    {
        RuntimeInfoArchitecture arch = target.Contracts.RuntimeInfo.GetTargetArchitecture();
        RuntimeInfoOperatingSystem os = target.Contracts.RuntimeInfo.GetTargetOperatingSystem();
        CallingConvention cc = GetCallingConvention(arch, os);
        int regCount = Math.Min(args.Length, cc.IntegerArgRegisters.Length);

        // Place register args.
        for (int i = 0; i < regCount; i++)
        {
            SetRegisterChecked(ctx, cc.IntegerArgRegisters[i], args[i], arch);
        }

        // Push stack-passed args (those beyond the register slots) right-to-left
        for (int i = args.Length - 1; i >= regCount; i--)
        {
            StackPusher.PushSlot(target, ref sp, args[i], align: false);
        }

        // Reserve home / shadow space (Windows x64 only).
        if (cc.HomeSpaceSlotCount > 0)
        {
            sp = new TargetPointer(sp.Value - (ulong)(cc.HomeSpaceSlotCount * target.PointerSize));
        }
    }

    private readonly record struct CallingConvention(
        string[] IntegerArgRegisters,
        int HomeSpaceSlotCount);

    private static CallingConvention GetCallingConvention(RuntimeInfoArchitecture arch, RuntimeInfoOperatingSystem os)
    {
        switch (arch)
        {
            case RuntimeInfoArchitecture.X86:
                // cdecl / stdcall: all args on the stack, no register args, no home space.
                return new CallingConvention([], HomeSpaceSlotCount: 0);

            case RuntimeInfoArchitecture.X64 when os == RuntimeInfoOperatingSystem.Windows:
                // Microsoft x64 calling convention: 4 integer arg regs + always 32 bytes
                // of caller-allocated home / shadow space.
                return new CallingConvention(["rcx", "rdx", "r8", "r9"], HomeSpaceSlotCount: 4);

            case RuntimeInfoArchitecture.X64:
                // System V AMD64 ABI (Linux, macOS, *BSD, ...): 6 integer arg regs, no home space.
                return new CallingConvention(["rdi", "rsi", "rdx", "rcx", "r8", "r9"], HomeSpaceSlotCount: 0);

            case RuntimeInfoArchitecture.Arm:
                // AAPCS32: r0..r3.
                return new CallingConvention(["r0", "r1", "r2", "r3"], HomeSpaceSlotCount: 0);

            case RuntimeInfoArchitecture.Arm64:
                // AAPCS64: x0..x7.
                return new CallingConvention(["x0", "x1", "x2", "x3", "x4", "x5", "x6", "x7"], HomeSpaceSlotCount: 0);

            case RuntimeInfoArchitecture.LoongArch64:
            case RuntimeInfoArchitecture.RiscV64:
                // LoongArch and RISC-V calling conventions: a0..a7.
                return new CallingConvention(["a0", "a1", "a2", "a3", "a4", "a5", "a6", "a7"], HomeSpaceSlotCount: 0);

            default:
                throw new NotSupportedException($"IntegerArgPlacer.PlaceArgs does not support architecture '{arch}'.");
        }
    }

    private static void SetRegisterChecked(IPlatformAgnosticContext ctx, string register, TargetNUInt value, RuntimeInfoArchitecture arch)
    {
        if ((arch is RuntimeInfoArchitecture.X86 or RuntimeInfoArchitecture.Arm) && value.Value > uint.MaxValue)
        {
            throw new InvalidOperationException($"Cannot set register '{register}' to value {value.Value} on x86 context: value exceeds 32-bit range.");
        }
        if (!ctx.TrySetRegister(register, value))
        {
            throw new InvalidOperationException($"Failed to set register '{register}' on context.");
        }
    }
}
