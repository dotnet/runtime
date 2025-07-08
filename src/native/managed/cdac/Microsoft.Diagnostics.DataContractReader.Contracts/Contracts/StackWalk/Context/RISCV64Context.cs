// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.RISCV64;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// RISC-V 64-bit specific thread context.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct RISCV64Context : IPlatformContext
{
    [Flags]
    public enum ContextFlagsValues : uint
    {
        CONTEXT_RISCV64 = 0x00800000,
        CONTEXT_CONTROL = CONTEXT_RISCV64 | 0x1,
        CONTEXT_INTEGER = CONTEXT_RISCV64 | 0x2,
        CONTEXT_FLOATING_POINT = CONTEXT_RISCV64 | 0x4,
        CONTEXT_DEBUG_REGISTERS = CONTEXT_RISCV64 | 0x8,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS,

        //
        // This flag is set by the unwinder if it has unwound to a call
        // site, and cleared whenever it unwinds through a trap frame.
        // It is used by language-specific exception handlers to help
        // differentiate exception scopes during dispatching.
        //
        CONTEXT_UNWOUND_TO_CALL = 0x20000000,
        CONTEXT_AREA_MASK = 0xFFFF,
    }

    public readonly uint Size => 0x300; // Approximate size, may need adjustment

    public readonly uint DefaultContextFlags => (uint)(ContextFlagsValues.CONTEXT_CONTROL |
                                                       ContextFlagsValues.CONTEXT_INTEGER |
                                                       ContextFlagsValues.CONTEXT_FLOATING_POINT |
                                                       ContextFlagsValues.CONTEXT_DEBUG_REGISTERS);

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
        RISCV64Unwinder unwinder = new(target);
        unwinder.Unwind(ref this);
    }

    // Control flags

    [FieldOffset(0x0)]
    public uint ContextFlags;

    #region General registers

    [Register(RegisterType.General)]
    [FieldOffset(0x8)]
    public ulong Ra;

    [Register(RegisterType.General | RegisterType.StackPointer)]
    [FieldOffset(0x10)]
    public ulong Sp;

    [Register(RegisterType.General)]
    [FieldOffset(0x18)]
    public ulong Gp;

    [Register(RegisterType.General)]
    [FieldOffset(0x20)]
    public ulong Tp;

    [Register(RegisterType.General)]
    [FieldOffset(0x28)]
    public ulong T0;

    [Register(RegisterType.General)]
    [FieldOffset(0x30)]
    public ulong T1;

    [Register(RegisterType.General)]
    [FieldOffset(0x38)]
    public ulong T2;

    [Register(RegisterType.General | RegisterType.FramePointer)]
    [FieldOffset(0x40)]
    public ulong Fp;

    [Register(RegisterType.General)]
    [FieldOffset(0x48)]
    public ulong S1;

    [Register(RegisterType.General)]
    [FieldOffset(0x50)]
    public ulong A0;

    [Register(RegisterType.General)]
    [FieldOffset(0x58)]
    public ulong A1;

    [Register(RegisterType.General)]
    [FieldOffset(0x60)]
    public ulong A2;

    [Register(RegisterType.General)]
    [FieldOffset(0x68)]
    public ulong A3;

    [Register(RegisterType.General)]
    [FieldOffset(0x70)]
    public ulong A4;

    [Register(RegisterType.General)]
    [FieldOffset(0x78)]
    public ulong A5;

    [Register(RegisterType.General)]
    [FieldOffset(0x80)]
    public ulong A6;

    [Register(RegisterType.General)]
    [FieldOffset(0x88)]
    public ulong A7;

    [Register(RegisterType.General)]
    [FieldOffset(0x90)]
    public ulong S2;

    [Register(RegisterType.General)]
    [FieldOffset(0x98)]
    public ulong S3;

    [Register(RegisterType.General)]
    [FieldOffset(0xa0)]
    public ulong S4;

    [Register(RegisterType.General)]
    [FieldOffset(0xa8)]
    public ulong S5;

    [Register(RegisterType.General)]
    [FieldOffset(0xb0)]
    public ulong S6;

    [Register(RegisterType.General)]
    [FieldOffset(0xb8)]
    public ulong S7;

    [Register(RegisterType.General)]
    [FieldOffset(0xc0)]
    public ulong S8;

    [Register(RegisterType.General)]
    [FieldOffset(0xc8)]
    public ulong S9;

    [Register(RegisterType.General)]
    [FieldOffset(0xd0)]
    public ulong S10;

    [Register(RegisterType.General)]
    [FieldOffset(0xd8)]
    public ulong S11;

    [Register(RegisterType.General)]
    [FieldOffset(0xe0)]
    public ulong T3;

    [Register(RegisterType.General)]
    [FieldOffset(0xe8)]
    public ulong T4;

    [Register(RegisterType.General)]
    [FieldOffset(0xf0)]
    public ulong T5;

    [Register(RegisterType.General)]
    [FieldOffset(0xf8)]
    public ulong T6;

    #endregion

    #region Control Registers

    [Register(RegisterType.Control | RegisterType.ProgramCounter)]
    [FieldOffset(0x100)]
    public ulong Pc;

    #endregion

    #region Floating Point Registers

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x110)]
    public unsafe fixed ulong F[32];

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x210)]
    public uint Fcsr;

    #endregion
}
