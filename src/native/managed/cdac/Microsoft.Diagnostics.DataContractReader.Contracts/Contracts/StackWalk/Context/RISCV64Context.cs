// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        CONTEXT_RISCV64 = 0x01000000,
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

    public readonly uint Size => 0x220;

    public readonly uint DefaultContextFlags => (uint)ContextFlagsValues.CONTEXT_ALL;

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

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        if (name.Equals("zero", StringComparison.OrdinalIgnoreCase)) { return false; }
        if (name.Equals("ra", StringComparison.OrdinalIgnoreCase)) { Ra = value.Value; return true; }
        if (name.Equals("sp", StringComparison.OrdinalIgnoreCase)) { Sp = value.Value; return true; }
        if (name.Equals("gp", StringComparison.OrdinalIgnoreCase)) { Gp = value.Value; return true; }
        if (name.Equals("tp", StringComparison.OrdinalIgnoreCase)) { Tp = value.Value; return true; }
        if (name.Equals("t0", StringComparison.OrdinalIgnoreCase)) { T0 = value.Value; return true; }
        if (name.Equals("t1", StringComparison.OrdinalIgnoreCase)) { T1 = value.Value; return true; }
        if (name.Equals("t2", StringComparison.OrdinalIgnoreCase)) { T2 = value.Value; return true; }
        if (name.Equals("fp", StringComparison.OrdinalIgnoreCase)) { Fp = value.Value; return true; }
        if (name.Equals("s1", StringComparison.OrdinalIgnoreCase)) { S1 = value.Value; return true; }
        if (name.Equals("a0", StringComparison.OrdinalIgnoreCase)) { A0 = value.Value; return true; }
        if (name.Equals("a1", StringComparison.OrdinalIgnoreCase)) { A1 = value.Value; return true; }
        if (name.Equals("a2", StringComparison.OrdinalIgnoreCase)) { A2 = value.Value; return true; }
        if (name.Equals("a3", StringComparison.OrdinalIgnoreCase)) { A3 = value.Value; return true; }
        if (name.Equals("a4", StringComparison.OrdinalIgnoreCase)) { A4 = value.Value; return true; }
        if (name.Equals("a5", StringComparison.OrdinalIgnoreCase)) { A5 = value.Value; return true; }
        if (name.Equals("a6", StringComparison.OrdinalIgnoreCase)) { A6 = value.Value; return true; }
        if (name.Equals("a7", StringComparison.OrdinalIgnoreCase)) { A7 = value.Value; return true; }
        if (name.Equals("s2", StringComparison.OrdinalIgnoreCase)) { S2 = value.Value; return true; }
        if (name.Equals("s3", StringComparison.OrdinalIgnoreCase)) { S3 = value.Value; return true; }
        if (name.Equals("s4", StringComparison.OrdinalIgnoreCase)) { S4 = value.Value; return true; }
        if (name.Equals("s5", StringComparison.OrdinalIgnoreCase)) { S5 = value.Value; return true; }
        if (name.Equals("s6", StringComparison.OrdinalIgnoreCase)) { S6 = value.Value; return true; }
        if (name.Equals("s7", StringComparison.OrdinalIgnoreCase)) { S7 = value.Value; return true; }
        if (name.Equals("s8", StringComparison.OrdinalIgnoreCase)) { S8 = value.Value; return true; }
        if (name.Equals("s9", StringComparison.OrdinalIgnoreCase)) { S9 = value.Value; return true; }
        if (name.Equals("s10", StringComparison.OrdinalIgnoreCase)) { S10 = value.Value; return true; }
        if (name.Equals("s11", StringComparison.OrdinalIgnoreCase)) { S11 = value.Value; return true; }
        if (name.Equals("t3", StringComparison.OrdinalIgnoreCase)) { T3 = value.Value; return true; }
        if (name.Equals("t4", StringComparison.OrdinalIgnoreCase)) { T4 = value.Value; return true; }
        if (name.Equals("t5", StringComparison.OrdinalIgnoreCase)) { T5 = value.Value; return true; }
        if (name.Equals("t6", StringComparison.OrdinalIgnoreCase)) { T6 = value.Value; return true; }
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase)) { Pc = value.Value; return true; }
        if (name.Equals("fcsr", StringComparison.OrdinalIgnoreCase)) { Fcsr = (uint)value.Value; return true; }
        return false;
    }

    public bool TryReadRegister(string name, out TargetNUInt value)
    {
        value = default;
        if (name.Equals("zero", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(0); return true; }
        if (name.Equals("ra", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ra); return true; }
        if (name.Equals("sp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Sp); return true; }
        if (name.Equals("gp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Gp); return true; }
        if (name.Equals("tp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Tp); return true; }
        if (name.Equals("t0", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(T0); return true; }
        if (name.Equals("t1", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(T1); return true; }
        if (name.Equals("t2", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(T2); return true; }
        if (name.Equals("fp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fp); return true; }
        if (name.Equals("s1", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S1); return true; }
        if (name.Equals("a0", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A0); return true; }
        if (name.Equals("a1", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A1); return true; }
        if (name.Equals("a2", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A2); return true; }
        if (name.Equals("a3", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A3); return true; }
        if (name.Equals("a4", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A4); return true; }
        if (name.Equals("a5", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A5); return true; }
        if (name.Equals("a6", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A6); return true; }
        if (name.Equals("a7", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(A7); return true; }
        if (name.Equals("s2", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S2); return true; }
        if (name.Equals("s3", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S3); return true; }
        if (name.Equals("s4", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S4); return true; }
        if (name.Equals("s5", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S5); return true; }
        if (name.Equals("s6", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S6); return true; }
        if (name.Equals("s7", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S7); return true; }
        if (name.Equals("s8", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S8); return true; }
        if (name.Equals("s9", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S9); return true; }
        if (name.Equals("s10", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S10); return true; }
        if (name.Equals("s11", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(S11); return true; }
        if (name.Equals("t3", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(T3); return true; }
        if (name.Equals("t4", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(T4); return true; }
        if (name.Equals("t5", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(T5); return true; }
        if (name.Equals("t6", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(T6); return true; }
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Pc); return true; }
        if (name.Equals("fcsr", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fcsr); return true; }
        return false;
    }


    // Maps numbered registers (0–31) to their canonical names for by-number dispatch.
    // Register 0 (zero) is hardwired to 0 and has no backing field; reads return 0, writes are ignored.
    private static readonly FrozenDictionary<int, string> s_registersByNumber = new Dictionary<int, string>
    {
        [0] = "zero",
        [1] = "Ra", [2] = "Sp", [3] = "Gp", [4] = "Tp",
        [5] = "T0", [6] = "T1", [7] = "T2", [8] = "Fp", [9] = "S1",
        [10] = "A0", [11] = "A1", [12] = "A2", [13] = "A3", [14] = "A4",
        [15] = "A5", [16] = "A6", [17] = "A7",
        [18] = "S2", [19] = "S3", [20] = "S4", [21] = "S5", [22] = "S6",
        [23] = "S7", [24] = "S8", [25] = "S9", [26] = "S10", [27] = "S11",
        [28] = "T3", [29] = "T4", [30] = "T5", [31] = "T6",
    }.ToFrozenDictionary();

    public bool TryGetRegisterName(int number, [NotNullWhen(true)] out string? name)
        => s_registersByNumber.TryGetValue(number, out name);

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
