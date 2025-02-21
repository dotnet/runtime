// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// AMD64-specific thread context.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AMD64Context : IPlatformContext
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
    }

    public readonly uint Size => 0x4d0;
    public readonly uint DefaultContextFlags => (uint)ContextFlagsValues.CONTEXT_FULL;

    public TargetPointer StackPointer
    {
        readonly get => new(Rsp);
        set => Rsp = value.Value;
    }
    public TargetPointer InstructionPointer
    {
        readonly get => new(Rip);
        set => Rip = value.Value;
    }
    public TargetPointer FramePointer
    {
        readonly get => new(Rbp);
        set => Rbp = value.Value;
    }

    public void Unwind(Target target)
    {
        Unwinder.AMD64Unwind(ref this, target);
    }

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

    [FieldOffset(0x34)]
    public uint MxCsr;

    #region Segment registers

    [Register(RegisterType.Segments)]
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

    [Register(RegisterType.Segments)]
    [FieldOffset(0x42)]
    public ushort Ss;

    #endregion

    [Register(RegisterType.General)]
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

    [Register(RegisterType.Control | RegisterType.FramePointer)]
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

    // [Register(RegisterType.FloatPoint)]
    // [FieldOffset(0x100)]
    // public XmmSaveArea FltSave;

    // [Register(RegisterType.FloatPoint)]
    // [FieldOffset(0x300)]
    // public VectorRegisterArea VectorRegisters;

    #endregion

    [Register(RegisterType.Debug)]
    [FieldOffset(0x4a8)]
    public ulong DebugControl;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x4b0)]
    public ulong LastBranchToRip;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x4b8)]
    public ulong LastBranchFromRip;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x4c0)]
    public ulong LastExceptionToRip;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x4c8)]
    public ulong LastExceptionFromRip;

    public override readonly string ToString()
    {
        return $"P1Home: {P1Home}, P2Home: {P2Home}, P3Home: {P3Home}, P4Home: {P4Home}, P5Home: {P5Home}, P6Home: {P6Home}, " +
               $"ContextFlags: {ContextFlags}, MxCsr: {MxCsr}, Cs: {Cs}, Ds: {Ds}, Es: {Es}, Fs: {Fs}, Gs: {Gs}, Ss: {Ss}, " +
               $"EFlags: {EFlags}, Dr0: {Dr0}, Dr1: {Dr1}, Dr2: {Dr2}, Dr3: {Dr3}, Dr6: {Dr6}, Dr7: {Dr7}, " +
               $"Rax: {Rax}, Rcx: {Rcx}, Rdx: {Rdx}, Rbx: {Rbx}, Rsp: {Rsp}, Rbp: {Rbp}, Rsi: {Rsi}, Rdi: {Rdi}, " +
               $"R8: {R8}, R9: {R9}, R10: {R10}, R11: {R11}, R12: {R12}, R13: {R13}, R14: {R14}, R15: {R15}, Rip: {Rip}, " +
               $"DebugControl: {DebugControl}, LastBranchToRip: {LastBranchToRip}, LastBranchFromRip: {LastBranchFromRip}, " +
               $"LastExceptionToRip: {LastExceptionToRip}, LastExceptionFromRip: {LastExceptionFromRip}";
    }
}
