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

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        if (name.Equals("r0", StringComparison.OrdinalIgnoreCase)) { R0 = (uint)value.Value; return true; }
        if (name.Equals("r1", StringComparison.OrdinalIgnoreCase)) { R1 = (uint)value.Value; return true; }
        if (name.Equals("r2", StringComparison.OrdinalIgnoreCase)) { R2 = (uint)value.Value; return true; }
        if (name.Equals("r3", StringComparison.OrdinalIgnoreCase)) { R3 = (uint)value.Value; return true; }
        if (name.Equals("r4", StringComparison.OrdinalIgnoreCase)) { R4 = (uint)value.Value; return true; }
        if (name.Equals("r5", StringComparison.OrdinalIgnoreCase)) { R5 = (uint)value.Value; return true; }
        if (name.Equals("r6", StringComparison.OrdinalIgnoreCase)) { R6 = (uint)value.Value; return true; }
        if (name.Equals("r7", StringComparison.OrdinalIgnoreCase)) { R7 = (uint)value.Value; return true; }
        if (name.Equals("r8", StringComparison.OrdinalIgnoreCase)) { R8 = (uint)value.Value; return true; }
        if (name.Equals("r9", StringComparison.OrdinalIgnoreCase)) { R9 = (uint)value.Value; return true; }
        if (name.Equals("r10", StringComparison.OrdinalIgnoreCase)) { R10 = (uint)value.Value; return true; }
        if (name.Equals("r11", StringComparison.OrdinalIgnoreCase)) { R11 = (uint)value.Value; return true; }
        if (name.Equals("r12", StringComparison.OrdinalIgnoreCase)) { R12 = (uint)value.Value; return true; }
        if (name.Equals("sp", StringComparison.OrdinalIgnoreCase)) { Sp = (uint)value.Value; return true; }
        if (name.Equals("lr", StringComparison.OrdinalIgnoreCase)) { Lr = (uint)value.Value; return true; }
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase)) { Pc = (uint)value.Value; return true; }
        if (name.Equals("cpsr", StringComparison.OrdinalIgnoreCase)) { Cpsr = (uint)value.Value; return true; }
        if (name.Equals("fpscr", StringComparison.OrdinalIgnoreCase)) { Fpscr = (uint)value.Value; return true; }
        return false;
    }

    public bool TryReadRegister(string name, out TargetNUInt value)
    {
        value = default;
        if (name.Equals("r0", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R0); return true; }
        if (name.Equals("r1", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R1); return true; }
        if (name.Equals("r2", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R2); return true; }
        if (name.Equals("r3", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R3); return true; }
        if (name.Equals("r4", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R4); return true; }
        if (name.Equals("r5", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R5); return true; }
        if (name.Equals("r6", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R6); return true; }
        if (name.Equals("r7", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R7); return true; }
        if (name.Equals("r8", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R8); return true; }
        if (name.Equals("r9", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R9); return true; }
        if (name.Equals("r10", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R10); return true; }
        if (name.Equals("r11", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R11); return true; }
        if (name.Equals("r12", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R12); return true; }
        if (name.Equals("sp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Sp); return true; }
        if (name.Equals("lr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Lr); return true; }
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Pc); return true; }
        if (name.Equals("cpsr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Cpsr); return true; }
        if (name.Equals("fpscr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fpscr); return true; }
        return false;
    }

    public bool TrySetRegister(int number, TargetNUInt value)
    {
        switch (number)
        {
            case 0: R0 = (uint)value.Value; return true;
            case 1: R1 = (uint)value.Value; return true;
            case 2: R2 = (uint)value.Value; return true;
            case 3: R3 = (uint)value.Value; return true;
            case 4: R4 = (uint)value.Value; return true;
            case 5: R5 = (uint)value.Value; return true;
            case 6: R6 = (uint)value.Value; return true;
            case 7: R7 = (uint)value.Value; return true;
            case 8: R8 = (uint)value.Value; return true;
            case 9: R9 = (uint)value.Value; return true;
            case 10: R10 = (uint)value.Value; return true;
            case 11: R11 = (uint)value.Value; return true;
            case 12: R12 = (uint)value.Value; return true;
            case 13: Sp = (uint)value.Value; return true;
            case 14: Lr = (uint)value.Value; return true;
            case 15: Pc = (uint)value.Value; return true;
            case 16: Cpsr = (uint)value.Value; return true;
            default: return false;
        }
    }

    public bool TryReadRegister(int number, out TargetNUInt value)
    {
        switch (number)
        {
            case 0: value = new TargetNUInt(R0); return true;
            case 1: value = new TargetNUInt(R1); return true;
            case 2: value = new TargetNUInt(R2); return true;
            case 3: value = new TargetNUInt(R3); return true;
            case 4: value = new TargetNUInt(R4); return true;
            case 5: value = new TargetNUInt(R5); return true;
            case 6: value = new TargetNUInt(R6); return true;
            case 7: value = new TargetNUInt(R7); return true;
            case 8: value = new TargetNUInt(R8); return true;
            case 9: value = new TargetNUInt(R9); return true;
            case 10: value = new TargetNUInt(R10); return true;
            case 11: value = new TargetNUInt(R11); return true;
            case 12: value = new TargetNUInt(R12); return true;
            case 13: value = new TargetNUInt(Sp); return true;
            case 14: value = new TargetNUInt(Lr); return true;
            case 15: value = new TargetNUInt(Pc); return true;
            case 16: value = new TargetNUInt(Cpsr); return true;
            default: value = default; return false;
        }
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
