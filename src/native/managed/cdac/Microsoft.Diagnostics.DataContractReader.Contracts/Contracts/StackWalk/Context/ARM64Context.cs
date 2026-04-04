// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM64;

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
        CONTEXT_XSTATE = CONTEXT_ARM64 | 0x20,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_X18,

        //
        // This flag is set by the unwinder if it has unwound to a call
        // site, and cleared whenever it unwinds through a trap frame.
        // It is used by language-specific exception handlers to help
        // differentiate exception scopes during dispatching.
        //
        CONTEXT_UNWOUND_TO_CALL = 0x20000000,
        CONTEXT_AREA_MASK = 0xFFFF,
    }

    public readonly uint Size => 0x390;

    public readonly uint DefaultContextFlags => (uint)(ContextFlagsValues.CONTEXT_CONTROL |
                                                       ContextFlagsValues.CONTEXT_INTEGER |
                                                       ContextFlagsValues.CONTEXT_FLOATING_POINT |
                                                       ContextFlagsValues.CONTEXT_DEBUG_REGISTERS);

    public readonly int StackPointerRegister => 31;

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
        ARM64Unwinder unwinder = new(target);
        unwinder.Unwind(ref this);
    }

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        if (name.Equals("cpsr", StringComparison.OrdinalIgnoreCase)) { Cpsr = (uint)value.Value; return true; }
        if (name.Equals("x0", StringComparison.OrdinalIgnoreCase)) { X0 = value.Value; return true; }
        if (name.Equals("x1", StringComparison.OrdinalIgnoreCase)) { X1 = value.Value; return true; }
        if (name.Equals("x2", StringComparison.OrdinalIgnoreCase)) { X2 = value.Value; return true; }
        if (name.Equals("x3", StringComparison.OrdinalIgnoreCase)) { X3 = value.Value; return true; }
        if (name.Equals("x4", StringComparison.OrdinalIgnoreCase)) { X4 = value.Value; return true; }
        if (name.Equals("x5", StringComparison.OrdinalIgnoreCase)) { X5 = value.Value; return true; }
        if (name.Equals("x6", StringComparison.OrdinalIgnoreCase)) { X6 = value.Value; return true; }
        if (name.Equals("x7", StringComparison.OrdinalIgnoreCase)) { X7 = value.Value; return true; }
        if (name.Equals("x8", StringComparison.OrdinalIgnoreCase)) { X8 = value.Value; return true; }
        if (name.Equals("x9", StringComparison.OrdinalIgnoreCase)) { X9 = value.Value; return true; }
        if (name.Equals("x10", StringComparison.OrdinalIgnoreCase)) { X10 = value.Value; return true; }
        if (name.Equals("x11", StringComparison.OrdinalIgnoreCase)) { X11 = value.Value; return true; }
        if (name.Equals("x12", StringComparison.OrdinalIgnoreCase)) { X12 = value.Value; return true; }
        if (name.Equals("x13", StringComparison.OrdinalIgnoreCase)) { X13 = value.Value; return true; }
        if (name.Equals("x14", StringComparison.OrdinalIgnoreCase)) { X14 = value.Value; return true; }
        if (name.Equals("x15", StringComparison.OrdinalIgnoreCase)) { X15 = value.Value; return true; }
        if (name.Equals("x16", StringComparison.OrdinalIgnoreCase)) { X16 = value.Value; return true; }
        if (name.Equals("x17", StringComparison.OrdinalIgnoreCase)) { X17 = value.Value; return true; }
        if (name.Equals("x18", StringComparison.OrdinalIgnoreCase)) { X18 = value.Value; return true; }
        if (name.Equals("x19", StringComparison.OrdinalIgnoreCase)) { X19 = value.Value; return true; }
        if (name.Equals("x20", StringComparison.OrdinalIgnoreCase)) { X20 = value.Value; return true; }
        if (name.Equals("x21", StringComparison.OrdinalIgnoreCase)) { X21 = value.Value; return true; }
        if (name.Equals("x22", StringComparison.OrdinalIgnoreCase)) { X22 = value.Value; return true; }
        if (name.Equals("x23", StringComparison.OrdinalIgnoreCase)) { X23 = value.Value; return true; }
        if (name.Equals("x24", StringComparison.OrdinalIgnoreCase)) { X24 = value.Value; return true; }
        if (name.Equals("x25", StringComparison.OrdinalIgnoreCase)) { X25 = value.Value; return true; }
        if (name.Equals("x26", StringComparison.OrdinalIgnoreCase)) { X26 = value.Value; return true; }
        if (name.Equals("x27", StringComparison.OrdinalIgnoreCase)) { X27 = value.Value; return true; }
        if (name.Equals("x28", StringComparison.OrdinalIgnoreCase)) { X28 = value.Value; return true; }
        if (name.Equals("fp", StringComparison.OrdinalIgnoreCase)) { Fp = value.Value; return true; }
        if (name.Equals("lr", StringComparison.OrdinalIgnoreCase)) { Lr = value.Value; return true; }
        if (name.Equals("sp", StringComparison.OrdinalIgnoreCase)) { Sp = value.Value; return true; }
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase)) { Pc = value.Value; return true; }
        if (name.Equals("fpcr", StringComparison.OrdinalIgnoreCase)) { Fpcr = (uint)value.Value; return true; }
        if (name.Equals("fpsr", StringComparison.OrdinalIgnoreCase)) { Fpsr = (uint)value.Value; return true; }
        return false;
    }

    public bool TryReadRegister(string name, out TargetNUInt value)
    {
        value = default;
        if (name.Equals("cpsr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Cpsr); return true; }
        if (name.Equals("x0", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X0); return true; }
        if (name.Equals("x1", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X1); return true; }
        if (name.Equals("x2", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X2); return true; }
        if (name.Equals("x3", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X3); return true; }
        if (name.Equals("x4", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X4); return true; }
        if (name.Equals("x5", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X5); return true; }
        if (name.Equals("x6", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X6); return true; }
        if (name.Equals("x7", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X7); return true; }
        if (name.Equals("x8", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X8); return true; }
        if (name.Equals("x9", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X9); return true; }
        if (name.Equals("x10", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X10); return true; }
        if (name.Equals("x11", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X11); return true; }
        if (name.Equals("x12", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X12); return true; }
        if (name.Equals("x13", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X13); return true; }
        if (name.Equals("x14", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X14); return true; }
        if (name.Equals("x15", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X15); return true; }
        if (name.Equals("x16", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X16); return true; }
        if (name.Equals("x17", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X17); return true; }
        if (name.Equals("x18", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X18); return true; }
        if (name.Equals("x19", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X19); return true; }
        if (name.Equals("x20", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X20); return true; }
        if (name.Equals("x21", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X21); return true; }
        if (name.Equals("x22", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X22); return true; }
        if (name.Equals("x23", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X23); return true; }
        if (name.Equals("x24", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X24); return true; }
        if (name.Equals("x25", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X25); return true; }
        if (name.Equals("x26", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X26); return true; }
        if (name.Equals("x27", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X27); return true; }
        if (name.Equals("x28", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(X28); return true; }
        if (name.Equals("fp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fp); return true; }
        if (name.Equals("lr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Lr); return true; }
        if (name.Equals("sp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Sp); return true; }
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Pc); return true; }
        if (name.Equals("fpcr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fpcr); return true; }
        if (name.Equals("fpsr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fpsr); return true; }
        return false;
    }

    public bool TrySetRegister(int number, TargetNUInt value)
    {
        switch (number)
        {
            case 0: X0 = value.Value; return true;
            case 1: X1 = value.Value; return true;
            case 2: X2 = value.Value; return true;
            case 3: X3 = value.Value; return true;
            case 4: X4 = value.Value; return true;
            case 5: X5 = value.Value; return true;
            case 6: X6 = value.Value; return true;
            case 7: X7 = value.Value; return true;
            case 8: X8 = value.Value; return true;
            case 9: X9 = value.Value; return true;
            case 10: X10 = value.Value; return true;
            case 11: X11 = value.Value; return true;
            case 12: X12 = value.Value; return true;
            case 13: X13 = value.Value; return true;
            case 14: X14 = value.Value; return true;
            case 15: X15 = value.Value; return true;
            case 16: X16 = value.Value; return true;
            case 17: X17 = value.Value; return true;
            case 18: X18 = value.Value; return true;
            case 19: X19 = value.Value; return true;
            case 20: X20 = value.Value; return true;
            case 21: X21 = value.Value; return true;
            case 22: X22 = value.Value; return true;
            case 23: X23 = value.Value; return true;
            case 24: X24 = value.Value; return true;
            case 25: X25 = value.Value; return true;
            case 26: X26 = value.Value; return true;
            case 27: X27 = value.Value; return true;
            case 28: X28 = value.Value; return true;
            case 29: Fp = value.Value; return true;
            case 30: Lr = value.Value; return true;
            case 31: Sp = value.Value; return true;
            case 32: Pc = value.Value; return true;
            default: return false;
        }
    }

    public bool TryReadRegister(int number, out TargetNUInt value)
    {
        switch (number)
        {
            case 0: value = new TargetNUInt(X0); return true;
            case 1: value = new TargetNUInt(X1); return true;
            case 2: value = new TargetNUInt(X2); return true;
            case 3: value = new TargetNUInt(X3); return true;
            case 4: value = new TargetNUInt(X4); return true;
            case 5: value = new TargetNUInt(X5); return true;
            case 6: value = new TargetNUInt(X6); return true;
            case 7: value = new TargetNUInt(X7); return true;
            case 8: value = new TargetNUInt(X8); return true;
            case 9: value = new TargetNUInt(X9); return true;
            case 10: value = new TargetNUInt(X10); return true;
            case 11: value = new TargetNUInt(X11); return true;
            case 12: value = new TargetNUInt(X12); return true;
            case 13: value = new TargetNUInt(X13); return true;
            case 14: value = new TargetNUInt(X14); return true;
            case 15: value = new TargetNUInt(X15); return true;
            case 16: value = new TargetNUInt(X16); return true;
            case 17: value = new TargetNUInt(X17); return true;
            case 18: value = new TargetNUInt(X18); return true;
            case 19: value = new TargetNUInt(X19); return true;
            case 20: value = new TargetNUInt(X20); return true;
            case 21: value = new TargetNUInt(X21); return true;
            case 22: value = new TargetNUInt(X22); return true;
            case 23: value = new TargetNUInt(X23); return true;
            case 24: value = new TargetNUInt(X24); return true;
            case 25: value = new TargetNUInt(X25); return true;
            case 26: value = new TargetNUInt(X26); return true;
            case 27: value = new TargetNUInt(X27); return true;
            case 28: value = new TargetNUInt(X28); return true;
            case 29: value = new TargetNUInt(Fp); return true;
            case 30: value = new TargetNUInt(Lr); return true;
            case 31: value = new TargetNUInt(Sp); return true;
            case 32: value = new TargetNUInt(Pc); return true;
            default: value = default; return false;
        }
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
