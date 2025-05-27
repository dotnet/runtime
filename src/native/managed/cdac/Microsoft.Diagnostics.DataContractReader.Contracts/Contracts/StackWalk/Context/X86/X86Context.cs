// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// X86-specific windows thread context.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct X86Context : IPlatformContext
{
    [Flags]
    public enum ContextFlagsValues : uint
    {
        CONTEXT_i386 = 0x00100000,
        CONTEXT_CONTROL = CONTEXT_i386 | 0x1,
        CONTEXT_INTEGER = CONTEXT_i386 | 0x2,
        CONTEXT_SEGMENTS = CONTEXT_i386 | 0x4,
        CONTEXT_FLOATING_POINT = CONTEXT_i386 | 0x8,
        CONTEXT_DEBUG_REGISTERS = CONTEXT_i386 | 0x10,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS,
        CONTEXT_XSTATE = CONTEXT_i386 | 0x40,

        //
        // This flag is set by the unwinder if it has unwound to a call
        // site, and cleared whenever it unwinds through a trap frame.
        // It is used by language-specific exception handlers to help
        // differentiate exception scopes during dispatching.
        //
        CONTEXT_UNWOUND_TO_CALL = 0x20000000,
        CONTEXT_AREA_MASK = 0xFFFF,
    }

    public readonly uint Size => 0x2cc;
    public readonly uint DefaultContextFlags => (uint)ContextFlagsValues.CONTEXT_FULL;

    public TargetPointer StackPointer
    {
        readonly get => new(Esp);
        set => Esp = (uint)value.Value;
    }
    public TargetPointer InstructionPointer
    {
        readonly get => new(Eip);
        set => Eip = (uint)value.Value;
    }
    public TargetPointer FramePointer
    {
        readonly get => new(Ebp);
        set => Ebp = (uint)value.Value;
    }

    public void Unwind(Target target)
    {
        X86Unwinder unwinder = new(target);
        unwinder.Unwind(ref this);
    }

    // Control flags

    [FieldOffset(0x0)]
    public uint ContextFlags;

    #region Debug registers

    [Register(RegisterType.Debug)]
    [FieldOffset(0x4)]
    public uint Dr0;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x8)]
    public uint Dr1;

    [Register(RegisterType.Debug)]
    [FieldOffset(0xc)]
    public uint Dr2;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x10)]
    public uint Dr3;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x14)]
    public uint Dr6;

    [Register(RegisterType.Debug)]
    [FieldOffset(0x18)]
    public uint Dr7;

    #endregion

    #region Floating point registers

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x1c)]
    public uint ControlWord;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x20)]
    public uint StatusWord;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x24)]
    public uint TagWord;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x28)]
    public uint ErrorOffset;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x2c)]
    public uint ErrorSelector;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x30)]
    public uint DataOffset;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x34)]
    public uint DataSelector;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x38)]
    public Float80 ST0;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x42)]
    public Float80 ST1;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x4c)]
    public Float80 ST2;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x56)]
    public Float80 ST3;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x60)]
    public Float80 ST4;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x6a)]
    public Float80 ST5;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x74)]
    public Float80 ST6;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x7e)]
    public Float80 ST7;

    [Register(RegisterType.FloatingPoint)]
    [FieldOffset(0x88)]
    public uint Cr0NpxState;

    #endregion

    #region Segment Registers

    [Register(RegisterType.Segments)]
    [FieldOffset(0x8c)]
    public uint Gs;

    [Register(RegisterType.Segments)]
    [FieldOffset(0x90)]
    public uint Fs;

    [Register(RegisterType.Segments)]
    [FieldOffset(0x94)]
    public uint Es;

    [Register(RegisterType.Segments)]
    [FieldOffset(0x98)]
    public uint Ds;

    #endregion

    #region Integer registers

    [Register(RegisterType.General)]
    [FieldOffset(0x9c)]
    public uint Edi;

    [Register(RegisterType.General)]
    [FieldOffset(0xa0)]
    public uint Esi;

    [Register(RegisterType.General)]
    [FieldOffset(0xa4)]
    public uint Ebx;

    [Register(RegisterType.General)]
    [FieldOffset(0xa8)]
    public uint Edx;

    [Register(RegisterType.General)]
    [FieldOffset(0xac)]
    public uint Ecx;

    [Register(RegisterType.General)]
    [FieldOffset(0xb0)]
    public uint Eax;

    #endregion

    #region Control registers

    [Register(RegisterType.Control | RegisterType.FramePointer)]
    [FieldOffset(0xb4)]
    public uint Ebp;

    [Register(RegisterType.Control | RegisterType.ProgramCounter)]
    [FieldOffset(0xb8)]
    public uint Eip;

    [Register(RegisterType.Segments)]
    [FieldOffset(0xbc)]
    public uint Cs;

    [Register(RegisterType.General)]
    [FieldOffset(0xc0)]
    public uint EFlags;

    [Register(RegisterType.Control | RegisterType.StackPointer)]
    [FieldOffset(0xc4)]
    public uint Esp;

    [Register(RegisterType.Segments)]
    [FieldOffset(0xc8)]
    public uint Ss;

    #endregion

    [FieldOffset(0xcc)]
    public unsafe fixed byte ExtendedRegisters[512];    /// <summary>
    /// Returns a string representation of the X86Context with all register values except ExtendedRegisters.
    /// </summary>
    /// <returns>A formatted string with all register values</returns>
    public override readonly string ToString()
    {
        return $"X86Context: " +
               $"ContextFlags=0x{ContextFlags:X8} " +
               // Debug registers
               $"Dr0=0x{Dr0:X8} Dr1=0x{Dr1:X8} Dr2=0x{Dr2:X8} Dr3=0x{Dr3:X8} Dr6=0x{Dr6:X8} Dr7=0x{Dr7:X8} " +
               // Floating point control registers
               $"ControlWord=0x{ControlWord:X8} StatusWord=0x{StatusWord:X8} TagWord=0x{TagWord:X8} " +
               $"ErrorOffset=0x{ErrorOffset:X8} ErrorSelector=0x{ErrorSelector:X4} " +
               $"DataOffset=0x{DataOffset:X8} DataSelector=0x{DataSelector:X4} " +
               // Floating point registers (displaying only Mantissa and Exponent for each)
            //    $"ST0.M=0x{ST0.Mantissa:X16} ST0.E=0x{ST0.Exponent:X4} " +
            //    $"ST1.M=0x{ST1.Mantissa:X16} ST1.E=0x{ST1.Exponent:X4} " +
            //    $"ST2.M=0x{ST2.Mantissa:X16} ST2.E=0x{ST2.Exponent:X4} " +
            //    $"ST3.M=0x{ST3.Mantissa:X16} ST3.E=0x{ST3.Exponent:X4} " +
            //    $"ST4.M=0x{ST4.Mantissa:X16} ST4.E=0x{ST4.Exponent:X4} " +
            //    $"ST5.M=0x{ST5.Mantissa:X16} ST5.E=0x{ST5.Exponent:X4} " +
            //    $"ST6.M=0x{ST6.Mantissa:X16} ST6.E=0x{ST6.Exponent:X4} " +
            //    $"ST7.M=0x{ST7.Mantissa:X16} ST7.E=0x{ST7.Exponent:X4} " +
            //    $"Cr0NpxState=0x{Cr0NpxState:X8} " +
               // Segment registers
               $"Gs=0x{Gs:X4} Fs=0x{Fs:X4} Es=0x{Es:X4} Ds=0x{Ds:X4} Cs=0x{Cs:X4} Ss=0x{Ss:X4} " +
               // Integer registers
               $"Edi=0x{Edi:X8} Esi=0x{Esi:X8} Ebx=0x{Ebx:X8} Edx=0x{Edx:X8} Ecx=0x{Ecx:X8} Eax=0x{Eax:X8} " +
               // Control registers
               $"Ebp=0x{Ebp:X8} Eip=0x{Eip:X8} EFlags=0x{EFlags:X8} Esp=0x{Esp:X8}";
    }

}

/// <summary>
/// Float in X86-specific windows thread context.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct Float80
{
    [FieldOffset(0x0)]
    public readonly ulong Mantissa;

    [FieldOffset(0x8)]
    public readonly ushort Exponent;
}
