// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// ARM-specific thread context.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct ARMContext : IPlatformContext
{

    [Flags]
    public enum ContextFlagsValues : uint
    {
        CONTEXT_ARM = 0x00200000,
        CONTEXT_CONTROL = CONTEXT_ARM | 0x1,
        CONTEXT_INTEGER = CONTEXT_ARM | 0x2,
        CONTEXT_FLOATING_POINT = CONTEXT_ARM | 0x4,
        CONTEXT_DEBUG_REGISTERS = CONTEXT_ARM | 0x8,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS,

        CONTEXT_UNWOUND_TO_CALL = 0x20000000,
    }

    public readonly uint Size => 0x1a0;
    public readonly uint DefaultContextFlags => (uint)ContextFlagsValues.CONTEXT_ALL;

    public TargetPointer StackPointer
    {
        readonly get => new(Sp);
        set => Sp = (uint)value.Value;
    }

    public TargetPointer InstructionPointer
    {
        readonly get => new(Pc);
        set => Pc = (uint)value.Value;
    }

    public TargetPointer FramePointer
    {
        readonly get => new(R11);
        set => R11 = (uint)value.Value;
    }

    public void Unwind(Target target)
    {
        ARMUnwinder unwinder = new(target);
        unwinder.Unwind(ref this);
    }

    // Control flags

    [FieldOffset(0x0)]
    public uint ContextFlags;

    #region General registers

    [Register(RegisterType.General)]
    [FieldOffset(0x4)]
    public uint R0;

    [Register(RegisterType.General)]
    [FieldOffset(0x8)]
    public uint R1;

    [Register(RegisterType.General)]
    [FieldOffset(0xc)]
    public uint R2;

    [Register(RegisterType.General)]
    [FieldOffset(0x10)]
    public uint R3;

    [Register(RegisterType.General)]
    [FieldOffset(0x14)]
    public uint R4;

    [Register(RegisterType.General)]
    [FieldOffset(0x18)]
    public uint R5;

    [Register(RegisterType.General)]
    [FieldOffset(0x1c)]
    public uint R6;

    [Register(RegisterType.General)]
    [FieldOffset(0x20)]
    public uint R7;

    [Register(RegisterType.General)]
    [FieldOffset(0x24)]
    public uint R8;

    [Register(RegisterType.General)]
    [FieldOffset(0x28)]
    public uint R9;

    [Register(RegisterType.General)]
    [FieldOffset(0x2c)]
    public uint R10;

    [Register(RegisterType.General | RegisterType.FramePointer)]
    [FieldOffset(0x30)]
    public uint R11;

    [Register(RegisterType.General)]
    [FieldOffset(0x34)]
    public uint R12;

    #endregion

    #region Control Registers

    [Register(RegisterType.Control | RegisterType.StackPointer)]
    [FieldOffset(0x38)]
    public uint Sp;

    [Register(RegisterType.Control)]
    [FieldOffset(0x3c)]
    public uint Lr;

    [Register(RegisterType.Control | RegisterType.ProgramCounter)]
    [FieldOffset(0x40)]
    public uint Pc;

    [Register(RegisterType.General)]
    [FieldOffset(0x44)]
    public uint Cpsr;

    #endregion

    #region Floating Point/NEON Registers

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x48)]
    public uint Fpscr;

    [FieldOffset(0x50)]
    public unsafe fixed ulong D[32];

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x50)]
    public M128A Q0;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x60)]
    public M128A Q1;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x70)]
    public M128A Q2;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x80)]
    public M128A Q3;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x90)]
    public M128A Q4;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0xa0)]
    public M128A Q5;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0xb0)]
    public M128A Q6;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0xc0)]
    public M128A Q7;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0xd0)]
    public M128A Q8;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0xe0)]
    public M128A Q9;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0xf0)]
    public M128A Q10;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x100)]
    public M128A Q11;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x110)]
    public M128A Q12;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x120)]
    public M128A Q13;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x130)]
    public M128A Q14;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x140)]
    public M128A Q15;

    #endregion

    #region Debug registers
    [Register(RegisterType.Debug)]
    [FieldOffset(0x150)]
    public unsafe fixed uint Bvr[8];

    [Register(RegisterType.Debug)]
    [FieldOffset(0x170)]
    public unsafe fixed uint Bcr[8];

    [Register(RegisterType.Debug)]
    [FieldOffset(0x190)]
    public unsafe fixed uint Wvr[1];

    [Register(RegisterType.Debug)]
    [FieldOffset(0x194)]
    public unsafe fixed uint Wcr[1];

    #endregion

    [FieldOffset(0x198)]
    public ulong Padding;
}
