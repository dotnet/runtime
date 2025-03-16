// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// ARM64-specific thread context.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct ARM64Context : IPlatformContext
{
    [Flags]
    public enum ContextFlagsValues : uint
    {
        CONTEXT_ARM64 = 0x00400000,
        CONTEXT_CONTROL = CONTEXT_ARM64 | 0x1,
        CONTEXT_INTEGER = CONTEXT_ARM64 | 0x2,
        CONTEXT_FLOATING_POINT = CONTEXT_ARM64 | 0x4,
        CONTEXT_DEBUG_REGISTERS = CONTEXT_ARM64 | 0x8,
        CONTEXT_X18 = CONTEXT_ARM64 | 0x10,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_X18,
    }

    public readonly uint Size => 0x390;

    public readonly uint DefaultContextFlags => (uint)ContextFlagsValues.CONTEXT_FULL;

    public TargetPointer StackPointer
    {
        readonly get => new(Sp);
        set => Sp = value.Value;
    }
    public TargetPointer InstructionPointer
    {
        readonly get => new(Pc);
        set => Pc = value.Value;
    }
    public TargetPointer FramePointer
    {
        readonly get => new(Fp);
        set => Fp = value.Value;
    }

    public void Unwind(Target target)
    {
        Unwinder.ARM64Unwind(ref this, target);
    }

    // Control flags

    [FieldOffset(0x0)]
    public uint ContextFlags;

    #region General registers

    [Register(RegisterType.General)]
    [FieldOffset(0x4)]
    public uint Cpsr;

    [Register(RegisterType.General)]
    [FieldOffset(0x8)]
    public ulong X0;

    [Register(RegisterType.General)]
    [FieldOffset(0x10)]
    public ulong X1;

    [Register(RegisterType.General)]
    [FieldOffset(0x18)]
    public ulong X2;

    [Register(RegisterType.General)]
    [FieldOffset(0x20)]
    public ulong X3;

    [Register(RegisterType.General)]
    [FieldOffset(0x28)]
    public ulong X4;

    [Register(RegisterType.General)]
    [FieldOffset(0x30)]
    public ulong X5;

    [Register(RegisterType.General)]
    [FieldOffset(0x38)]
    public ulong X6;

    [Register(RegisterType.General)]
    [FieldOffset(0x40)]
    public ulong X7;

    [Register(RegisterType.General)]
    [FieldOffset(0x48)]
    public ulong X8;

    [Register(RegisterType.General)]
    [FieldOffset(0x50)]
    public ulong X9;

    [Register(RegisterType.General)]
    [FieldOffset(0x58)]
    public ulong X10;

    [Register(RegisterType.General)]
    [FieldOffset(0x60)]
    public ulong X11;

    [Register(RegisterType.General)]
    [FieldOffset(0x68)]
    public ulong X12;

    [Register(RegisterType.General)]
    [FieldOffset(0x70)]
    public ulong X13;

    [Register(RegisterType.General)]
    [FieldOffset(0x78)]
    public ulong X14;

    [Register(RegisterType.General)]
    [FieldOffset(0x80)]
    public ulong X15;

    [Register(RegisterType.General)]
    [FieldOffset(0x88)]
    public ulong X16;

    [Register(RegisterType.General)]
    [FieldOffset(0x90)]
    public ulong X17;

    [Register(RegisterType.General)]
    [FieldOffset(0x98)]
    public ulong X18;

    [Register(RegisterType.General)]
    [FieldOffset(0xa0)]
    public ulong X19;

    [Register(RegisterType.General)]
    [FieldOffset(0xa8)]
    public ulong X20;

    [Register(RegisterType.General)]
    [FieldOffset(0xb0)]
    public ulong X21;

    [Register(RegisterType.General)]
    [FieldOffset(0xb8)]
    public ulong X22;

    [Register(RegisterType.General)]
    [FieldOffset(0xc0)]
    public ulong X23;

    [Register(RegisterType.General)]
    [FieldOffset(0xc8)]
    public ulong X24;

    [Register(RegisterType.General)]
    [FieldOffset(0xd0)]
    public ulong X25;

    [Register(RegisterType.General)]
    [FieldOffset(0xd8)]
    public ulong X26;

    [Register(RegisterType.General)]
    [FieldOffset(0xe0)]
    public ulong X27;

    [Register(RegisterType.General)]
    [FieldOffset(0xe8)]
    public ulong X28;

    #endregion

    #region Control Registers

    [Register(RegisterType.Control | RegisterType.FramePointer)]
    [FieldOffset(0xf0)]
    public ulong Fp;

    [Register(RegisterType.Control)]
    [FieldOffset(0xf8)]
    public ulong Lr;

    [Register(RegisterType.Control | RegisterType.StackPointer)]
    [FieldOffset(0x100)]
    public ulong Sp;

    [Register(RegisterType.Control | RegisterType.ProgramCounter)]
    [FieldOffset(0x108)]
    public ulong Pc;

    #endregion

    #region Floating Point/NEON Registers

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x110)]
    public unsafe fixed ulong V[32 * 2];

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x310)]
    public uint Fpcr;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x314)]
    public uint Fpsr;

    #endregion

    #region Debug Registers

#pragma warning disable CA1823 // Avoid unused private fields. See https://github.com/dotnet/roslyn/issues/29224
    private const int ARM64_MAX_BREAKPOINTS = 8;
    private const int ARM64_MAX_WATCHPOINTS = 2;
#pragma warning restore CA1823 // Avoid unused private fields

    [Register(RegisterType.Debug)]
    [FieldOffset(0x318)]
    public unsafe fixed uint Bcr[ARM64_MAX_BREAKPOINTS];

    [Register(RegisterType.Debug)]
    [FieldOffset(0x338)]
    public unsafe fixed ulong Bvr[ARM64_MAX_BREAKPOINTS];

    [Register(RegisterType.Debug)]
    [FieldOffset(0x378)]
    public unsafe fixed uint Wcr[ARM64_MAX_WATCHPOINTS];

    [Register(RegisterType.Debug)]
    [FieldOffset(0x380)]
    public unsafe fixed ulong Wvr[ARM64_MAX_WATCHPOINTS];

    #endregion
}
