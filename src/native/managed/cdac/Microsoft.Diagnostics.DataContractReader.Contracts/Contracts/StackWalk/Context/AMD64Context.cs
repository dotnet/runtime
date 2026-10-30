// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.AMD64;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// AMD64-specific thread context.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct AMD64Context : IPlatformContext
{
    [Flags]
    public enum ContextFlagsValues : uint
    {
        CONTEXT_AMD = 0x00100000,
        CONTEXT_CONTROL = CONTEXT_AMD | 0x1,
        CONTEXT_INTEGER = CONTEXT_AMD | 0x2,
        CONTEXT_SEGMENTS = CONTEXT_AMD | 0x4,
        CONTEXT_FLOATING_POINT = CONTEXT_AMD | 0x8,
        CONTEXT_DEBUG_REGISTERS = CONTEXT_AMD | 0x10,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS,
        CONTEXT_XSTATE = CONTEXT_AMD | 0x40,
        CONTEXT_KERNEL_CET = CONTEXT_AMD | 0x80,

        CONTEXT_AREA_MASK = 0xFFFF,
    }

    public readonly uint Size => 0x4d0;

    public readonly uint ContextControlFlags => (uint)ContextFlagsValues.CONTEXT_CONTROL;

    public readonly uint FullContextFlags => (uint)ContextFlagsValues.CONTEXT_FULL;

    public readonly uint AllContextFlags => (uint)ContextFlagsValues.CONTEXT_ALL;

    public readonly int StackPointerRegister => 4;

    public TargetPointer StackPointer
    {
        readonly get => new(Rsp);
        set => Rsp = value.Value;
    }
    public TargetCodePointer InstructionPointer
    {
        readonly get => new(Rip);
        set => Rip = value.Value;
    }
    public TargetPointer FramePointer
    {
        readonly get => new(Rbp);
        set => Rbp = value.Value;
    }

    public uint RawContextFlags { readonly get => ContextFlags; set => ContextFlags = value; }

    public void Unwind(Target target)
    {
        AMD64Unwinder unwinder = new(target);
        unwinder.Unwind(ref this);
    }

    // Clears the x64 hardware trace flag (EFLAGS.TF, bit 0x100).
    public void UnsetSingleStepFlag() => EFlags &= ~0x100;

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        if (name.Equals("cs", StringComparison.OrdinalIgnoreCase)) { Cs = (ushort)value.Value; return true; }
        if (name.Equals("ds", StringComparison.OrdinalIgnoreCase)) { Ds = (ushort)value.Value; return true; }
        if (name.Equals("es", StringComparison.OrdinalIgnoreCase)) { Es = (ushort)value.Value; return true; }
        if (name.Equals("fs", StringComparison.OrdinalIgnoreCase)) { Fs = (ushort)value.Value; return true; }
        if (name.Equals("gs", StringComparison.OrdinalIgnoreCase)) { Gs = (ushort)value.Value; return true; }
        if (name.Equals("ss", StringComparison.OrdinalIgnoreCase)) { Ss = (ushort)value.Value; return true; }
        if (name.Equals("eflags", StringComparison.OrdinalIgnoreCase)) { EFlags = (int)value.Value; return true; }
        if (name.Equals("dr0", StringComparison.OrdinalIgnoreCase)) { Dr0 = value.Value; return true; }
        if (name.Equals("dr1", StringComparison.OrdinalIgnoreCase)) { Dr1 = value.Value; return true; }
        if (name.Equals("dr2", StringComparison.OrdinalIgnoreCase)) { Dr2 = value.Value; return true; }
        if (name.Equals("dr3", StringComparison.OrdinalIgnoreCase)) { Dr3 = value.Value; return true; }
        if (name.Equals("dr6", StringComparison.OrdinalIgnoreCase)) { Dr6 = value.Value; return true; }
        if (name.Equals("dr7", StringComparison.OrdinalIgnoreCase)) { Dr7 = value.Value; return true; }
        if (name.Equals("rax", StringComparison.OrdinalIgnoreCase)) { Rax = value.Value; return true; }
        if (name.Equals("rcx", StringComparison.OrdinalIgnoreCase)) { Rcx = value.Value; return true; }
        if (name.Equals("rdx", StringComparison.OrdinalIgnoreCase)) { Rdx = value.Value; return true; }
        if (name.Equals("rbx", StringComparison.OrdinalIgnoreCase)) { Rbx = value.Value; return true; }
        if (name.Equals("rsp", StringComparison.OrdinalIgnoreCase)) { Rsp = value.Value; return true; }
        if (name.Equals("rbp", StringComparison.OrdinalIgnoreCase)) { Rbp = value.Value; return true; }
        if (name.Equals("rsi", StringComparison.OrdinalIgnoreCase)) { Rsi = value.Value; return true; }
        if (name.Equals("rdi", StringComparison.OrdinalIgnoreCase)) { Rdi = value.Value; return true; }
        if (name.Equals("r8", StringComparison.OrdinalIgnoreCase)) { R8 = value.Value; return true; }
        if (name.Equals("r9", StringComparison.OrdinalIgnoreCase)) { R9 = value.Value; return true; }
        if (name.Equals("r10", StringComparison.OrdinalIgnoreCase)) { R10 = value.Value; return true; }
        if (name.Equals("r11", StringComparison.OrdinalIgnoreCase)) { R11 = value.Value; return true; }
        if (name.Equals("r12", StringComparison.OrdinalIgnoreCase)) { R12 = value.Value; return true; }
        if (name.Equals("r13", StringComparison.OrdinalIgnoreCase)) { R13 = value.Value; return true; }
        if (name.Equals("r14", StringComparison.OrdinalIgnoreCase)) { R14 = value.Value; return true; }
        if (name.Equals("r15", StringComparison.OrdinalIgnoreCase)) { R15 = value.Value; return true; }
        if (name.Equals("rip", StringComparison.OrdinalIgnoreCase)) { Rip = value.Value; return true; }
        if (name.Equals("debugcontrol", StringComparison.OrdinalIgnoreCase)) { DebugControl = value.Value; return true; }
        if (name.Equals("lastbranchtorip", StringComparison.OrdinalIgnoreCase)) { LastBranchToRip = value.Value; return true; }
        if (name.Equals("lastbranchfromrip", StringComparison.OrdinalIgnoreCase)) { LastBranchFromRip = value.Value; return true; }
        if (name.Equals("lastexceptiontorip", StringComparison.OrdinalIgnoreCase)) { LastExceptionToRip = value.Value; return true; }
        if (name.Equals("lastexceptionfromrip", StringComparison.OrdinalIgnoreCase)) { LastExceptionFromRip = value.Value; return true; }
        return false;
    }

    public bool TryReadRegister(string name, out TargetNUInt value)
    {
        value = default;
        if (name.Equals("cs", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Cs); return true; }
        if (name.Equals("ds", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ds); return true; }
        if (name.Equals("es", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Es); return true; }
        if (name.Equals("fs", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fs); return true; }
        if (name.Equals("gs", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Gs); return true; }
        if (name.Equals("ss", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ss); return true; }
        if (name.Equals("eflags", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(unchecked((uint)EFlags)); return true; }
        if (name.Equals("dr0", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr0); return true; }
        if (name.Equals("dr1", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr1); return true; }
        if (name.Equals("dr2", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr2); return true; }
        if (name.Equals("dr3", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr3); return true; }
        if (name.Equals("dr6", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr6); return true; }
        if (name.Equals("dr7", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr7); return true; }
        if (name.Equals("rax", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rax); return true; }
        if (name.Equals("rcx", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rcx); return true; }
        if (name.Equals("rdx", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rdx); return true; }
        if (name.Equals("rbx", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rbx); return true; }
        if (name.Equals("rsp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rsp); return true; }
        if (name.Equals("rbp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rbp); return true; }
        if (name.Equals("rsi", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rsi); return true; }
        if (name.Equals("rdi", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rdi); return true; }
        if (name.Equals("r8", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R8); return true; }
        if (name.Equals("r9", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R9); return true; }
        if (name.Equals("r10", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R10); return true; }
        if (name.Equals("r11", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R11); return true; }
        if (name.Equals("r12", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R12); return true; }
        if (name.Equals("r13", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R13); return true; }
        if (name.Equals("r14", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R14); return true; }
        if (name.Equals("r15", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(R15); return true; }
        if (name.Equals("rip", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Rip); return true; }
        if (name.Equals("debugcontrol", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(DebugControl); return true; }
        if (name.Equals("lastbranchtorip", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(LastBranchToRip); return true; }
        if (name.Equals("lastbranchfromrip", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(LastBranchFromRip); return true; }
        if (name.Equals("lastexceptiontorip", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(LastExceptionToRip); return true; }
        if (name.Equals("lastexceptionfromrip", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(LastExceptionFromRip); return true; }
        return false;
    }

    public bool TrySetRegister(int number, TargetNUInt value)
    {
        switch (number)
        {
            case 0: Rax = value.Value; return true;
            case 1: Rcx = value.Value; return true;
            case 2: Rdx = value.Value; return true;
            case 3: Rbx = value.Value; return true;
            case 4: Rsp = value.Value; return true;
            case 5: Rbp = value.Value; return true;
            case 6: Rsi = value.Value; return true;
            case 7: Rdi = value.Value; return true;
            case 8: R8 = value.Value; return true;
            case 9: R9 = value.Value; return true;
            case 10: R10 = value.Value; return true;
            case 11: R11 = value.Value; return true;
            case 12: R12 = value.Value; return true;
            case 13: R13 = value.Value; return true;
            case 14: R14 = value.Value; return true;
            case 15: R15 = value.Value; return true;
            default: return false;
        }
    }

    public bool TryReadRegister(int number, out TargetNUInt value)
    {
        switch (number)
        {
            case 0: value = new TargetNUInt(Rax); return true;
            case 1: value = new TargetNUInt(Rcx); return true;
            case 2: value = new TargetNUInt(Rdx); return true;
            case 3: value = new TargetNUInt(Rbx); return true;
            case 4: value = new TargetNUInt(Rsp); return true;
            case 5: value = new TargetNUInt(Rbp); return true;
            case 6: value = new TargetNUInt(Rsi); return true;
            case 7: value = new TargetNUInt(Rdi); return true;
            case 8: value = new TargetNUInt(R8); return true;
            case 9: value = new TargetNUInt(R9); return true;
            case 10: value = new TargetNUInt(R10); return true;
            case 11: value = new TargetNUInt(R11); return true;
            case 12: value = new TargetNUInt(R12); return true;
            case 13: value = new TargetNUInt(R13); return true;
            case 14: value = new TargetNUInt(R14); return true;
            case 15: value = new TargetNUInt(R15); return true;
            default: value = default; return false;
        }
    }

    // XMM0-XMM15 occupy the FltSave area with a 16-byte (128-bit) stride.
    private const int XmmRegisterCount = 16;
    private const int XmmRegisterStride = 16;

    public readonly bool TryReadFloatingPointRegister(ReadOnlySpan<byte> context, int index, out double value)
        => SimdRegisterAccess.TryReadRegister(context, (int)Marshal.OffsetOf<AMD64Context>(nameof(Xmm)), XmmRegisterStride, XmmRegisterCount, index, out value);

    public readonly bool TryWriteFloatingPointRegister(Span<byte> context, int index, ReadOnlySpan<byte> value)
        => SimdRegisterAccess.TryWriteRegister(context, (int)Marshal.OffsetOf<AMD64Context>(nameof(Xmm)), XmmRegisterStride, XmmRegisterCount, index, value);


    public readonly (uint Flag, string Name)[] GetScalarRegisters() => s_scalarRegisters;
    public readonly (uint Flag, int Start, int End)[] GetWideSpans() => s_wideSpans;

    private static readonly (uint Flag, string Name)[] s_scalarRegisters =
    [
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "cs"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "ss"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "eflags"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "rsp"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "rax"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "rcx"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "rdx"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "rbx"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "rbp"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "rsi"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "rdi"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r8"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r9"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r10"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r11"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r12"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r13"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r14"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "r15"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "ds"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "es"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "fs"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "gs"),
    ];

    private static readonly (uint Flag, int Start, int End)[] s_wideSpans =
    [
        // Matches native CORDbgCopyThreadContext's CONTROL chunk [Rip, Xmm0), which
        // copies Rip together with the legacy x87 save area that has no named field here.
        ((uint)ContextFlagsValues.CONTEXT_CONTROL,
            (int)Marshal.OffsetOf<AMD64Context>(nameof(Rip)), (int)Marshal.OffsetOf<AMD64Context>(nameof(Xmm))),
        ((uint)ContextFlagsValues.CONTEXT_DEBUG_REGISTERS,
            (int)Marshal.OffsetOf<AMD64Context>(nameof(Dr0)), (int)Marshal.OffsetOf<AMD64Context>(nameof(Rax))),
        ((uint)ContextFlagsValues.CONTEXT_FLOATING_POINT,
            (int)Marshal.OffsetOf<AMD64Context>(nameof(MxCsr)), (int)Marshal.OffsetOf<AMD64Context>(nameof(Cs))),
        ((uint)ContextFlagsValues.CONTEXT_FLOATING_POINT,
            (int)Marshal.OffsetOf<AMD64Context>(nameof(Xmm)), (int)Marshal.OffsetOf<AMD64Context>(nameof(Xmm)) + (16 * 2 * sizeof(ulong))),
    ];

    [FieldOffset(0x0)]
    public ulong P1Home;

    [FieldOffset(0x8)]
    public ulong P2Home;

    [FieldOffset(0x10)]
    public ulong P3Home;

    [FieldOffset(0x18)]
    public ulong P4Home;

    [FieldOffset(0x20)]
    public ulong P5Home;

    [FieldOffset(0x28)]
    public ulong P6Home;

    [FieldOffset(0x30)]
    public uint ContextFlags;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x34)]
    public uint MxCsr;

    #region Segment registers

    [Register(RegisterType.Control)]
    [FieldOffset(0x38)]
    public ushort Cs;

    [Register(RegisterType.Segments)]
    [FieldOffset(0x3a)]
    public ushort Ds;

    [Register(RegisterType.Segments)]
    [FieldOffset(0x3c)]
    public ushort Es;

    [Register(RegisterType.Segments)]
    [FieldOffset(0x3e)]
    public ushort Fs;

    [Register(RegisterType.Segments)]
    [FieldOffset(0x40)]
    public ushort Gs;

    [Register(RegisterType.Control)]
    [FieldOffset(0x42)]
    public ushort Ss;

    #endregion

    [Register(RegisterType.Control)]
    [FieldOffset(0x44)]
    public int EFlags;

    #region Debug registers

    [Register(RegisterType.Debug)]
    [FieldOffset(0x48)]
    public ulong Dr0;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x50)]
    public ulong Dr1;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x58)]
    public ulong Dr2;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x60)]
    public ulong Dr3;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x68)]
    public ulong Dr6;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x70)]
    public ulong Dr7;

    #endregion

    #region General and control registers

    [Register(RegisterType.General)]
    [FieldOffset(0x78)]
    public ulong Rax;

    [Register(RegisterType.General)]
    [FieldOffset(0x80)]
    public ulong Rcx;

    [Register(RegisterType.General)]
    [FieldOffset(0x88)]
    public ulong Rdx;

    [Register(RegisterType.General)]
    [FieldOffset(0x90)]
    public ulong Rbx;

    [Register(RegisterType.Control | RegisterType.StackPointer)]
    [FieldOffset(0x98)]
    public ulong Rsp;

    [Register(RegisterType.General | RegisterType.FramePointer)]
    [FieldOffset(0xa0)]
    public ulong Rbp;

    [Register(RegisterType.General)]
    [FieldOffset(0xa8)]
    public ulong Rsi;

    [Register(RegisterType.General)]
    [FieldOffset(0xb0)]
    public ulong Rdi;

    [Register(RegisterType.General)]
    [FieldOffset(0xb8)]
    public ulong R8;

    [Register(RegisterType.General)]
    [FieldOffset(0xc0)]
    public ulong R9;

    [Register(RegisterType.General)]
    [FieldOffset(0xc8)]
    public ulong R10;

    [Register(RegisterType.General)]
    [FieldOffset(0xd0)]
    public ulong R11;

    [Register(RegisterType.General)]
    [FieldOffset(0xd8)]
    public ulong R12;

    [Register(RegisterType.General)]
    [FieldOffset(0xe0)]
    public ulong R13;

    [Register(RegisterType.General)]
    [FieldOffset(0xe8)]
    public ulong R14;

    [Register(RegisterType.General)]
    [FieldOffset(0xf0)]
    public ulong R15;

    [Register(RegisterType.Control | RegisterType.ProgramCounter)]
    [FieldOffset(0xf8)]
    public ulong Rip;

    #endregion

    #region Floating point registers

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x1a0)]
    public unsafe fixed ulong Xmm[16 * 2];

    #endregion

    [FieldOffset(0x4a8)]
    public ulong DebugControl;

    [FieldOffset(0x4b0)]
    public ulong LastBranchToRip;

    [FieldOffset(0x4b8)]
    public ulong LastBranchFromRip;

    [FieldOffset(0x4c0)]
    public ulong LastExceptionToRip;

    [FieldOffset(0x4c8)]
    public ulong LastExceptionFromRip;
}
