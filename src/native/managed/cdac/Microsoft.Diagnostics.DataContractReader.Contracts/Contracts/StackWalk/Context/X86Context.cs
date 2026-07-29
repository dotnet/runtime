// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86;

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
        CONTEXT_i386 = 0x00010000,
        CONTEXT_CONTROL = CONTEXT_i386 | 0x1,
        CONTEXT_INTEGER = CONTEXT_i386 | 0x2,
        CONTEXT_SEGMENTS = CONTEXT_i386 | 0x4,
        CONTEXT_FLOATING_POINT = CONTEXT_i386 | 0x8,
        CONTEXT_DEBUG_REGISTERS = CONTEXT_i386 | 0x10,
        CONTEXT_EXTENDED_REGISTERS = CONTEXT_i386 | 0x20,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS,
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

    public readonly uint SizeWithoutExtendedRegisters => 0xcc;

    public readonly uint ExtendedRegistersFlag => (uint)ContextFlagsValues.CONTEXT_EXTENDED_REGISTERS;

    public readonly uint ContextControlFlags => (uint)ContextFlagsValues.CONTEXT_CONTROL;

    public readonly uint FullContextFlags => (uint)ContextFlagsValues.CONTEXT_FULL;

    public readonly uint AllContextFlags => (uint)ContextFlagsValues.CONTEXT_ALL;

    public readonly int StackPointerRegister => 4;

    public TargetPointer StackPointer
    {
        readonly get => new(Esp);
        set => Esp = (uint)value.Value;
    }
    public TargetCodePointer InstructionPointer
    {
        readonly get => new(Eip);
        set => Eip = (uint)value.Value;
    }
    public TargetPointer FramePointer
    {
        readonly get => new(Ebp);
        set => Ebp = (uint)value.Value;
    }

    public uint RawContextFlags { readonly get => ContextFlags; set => ContextFlags = value; }

    public void Unwind(Target target)
    {
        X86Unwinder unwinder = new(target);
        unwinder.Unwind(ref this);
    }

    // Clears the x86 hardware trace flag (EFLAGS.TF, bit 0x100).
    public void UnsetSingleStepFlag() => EFlags &= ~0x100u;

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        if (name.Equals("dr0", StringComparison.OrdinalIgnoreCase)) { Dr0 = (uint)value.Value; return true; }
        if (name.Equals("dr1", StringComparison.OrdinalIgnoreCase)) { Dr1 = (uint)value.Value; return true; }
        if (name.Equals("dr2", StringComparison.OrdinalIgnoreCase)) { Dr2 = (uint)value.Value; return true; }
        if (name.Equals("dr3", StringComparison.OrdinalIgnoreCase)) { Dr3 = (uint)value.Value; return true; }
        if (name.Equals("dr6", StringComparison.OrdinalIgnoreCase)) { Dr6 = (uint)value.Value; return true; }
        if (name.Equals("dr7", StringComparison.OrdinalIgnoreCase)) { Dr7 = (uint)value.Value; return true; }
        if (name.Equals("controlword", StringComparison.OrdinalIgnoreCase)) { ControlWord = (uint)value.Value; return true; }
        if (name.Equals("statusword", StringComparison.OrdinalIgnoreCase)) { StatusWord = (uint)value.Value; return true; }
        if (name.Equals("tagword", StringComparison.OrdinalIgnoreCase)) { TagWord = (uint)value.Value; return true; }
        if (name.Equals("erroroffset", StringComparison.OrdinalIgnoreCase)) { ErrorOffset = (uint)value.Value; return true; }
        if (name.Equals("errorselector", StringComparison.OrdinalIgnoreCase)) { ErrorSelector = (uint)value.Value; return true; }
        if (name.Equals("dataoffset", StringComparison.OrdinalIgnoreCase)) { DataOffset = (uint)value.Value; return true; }
        if (name.Equals("dataselector", StringComparison.OrdinalIgnoreCase)) { DataSelector = (uint)value.Value; return true; }
        if (name.Equals("cr0npxstate", StringComparison.OrdinalIgnoreCase)) { Cr0NpxState = (uint)value.Value; return true; }
        if (name.Equals("gs", StringComparison.OrdinalIgnoreCase)) { Gs = (uint)value.Value; return true; }
        if (name.Equals("fs", StringComparison.OrdinalIgnoreCase)) { Fs = (uint)value.Value; return true; }
        if (name.Equals("es", StringComparison.OrdinalIgnoreCase)) { Es = (uint)value.Value; return true; }
        if (name.Equals("ds", StringComparison.OrdinalIgnoreCase)) { Ds = (uint)value.Value; return true; }
        if (name.Equals("edi", StringComparison.OrdinalIgnoreCase)) { Edi = (uint)value.Value; return true; }
        if (name.Equals("esi", StringComparison.OrdinalIgnoreCase)) { Esi = (uint)value.Value; return true; }
        if (name.Equals("ebx", StringComparison.OrdinalIgnoreCase)) { Ebx = (uint)value.Value; return true; }
        if (name.Equals("edx", StringComparison.OrdinalIgnoreCase)) { Edx = (uint)value.Value; return true; }
        if (name.Equals("ecx", StringComparison.OrdinalIgnoreCase)) { Ecx = (uint)value.Value; return true; }
        if (name.Equals("eax", StringComparison.OrdinalIgnoreCase)) { Eax = (uint)value.Value; return true; }
        if (name.Equals("ebp", StringComparison.OrdinalIgnoreCase)) { Ebp = (uint)value.Value; return true; }
        if (name.Equals("eip", StringComparison.OrdinalIgnoreCase)) { Eip = (uint)value.Value; return true; }
        if (name.Equals("cs", StringComparison.OrdinalIgnoreCase)) { Cs = (uint)value.Value; return true; }
        if (name.Equals("eflags", StringComparison.OrdinalIgnoreCase)) { EFlags = (uint)value.Value; return true; }
        if (name.Equals("esp", StringComparison.OrdinalIgnoreCase)) { Esp = (uint)value.Value; return true; }
        if (name.Equals("ss", StringComparison.OrdinalIgnoreCase)) { Ss = (uint)value.Value; return true; }
        return false;
    }

    public bool TryReadRegister(string name, out TargetNUInt value)
    {
        value = default;
        if (name.Equals("dr0", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr0); return true; }
        if (name.Equals("dr1", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr1); return true; }
        if (name.Equals("dr2", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr2); return true; }
        if (name.Equals("dr3", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr3); return true; }
        if (name.Equals("dr6", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr6); return true; }
        if (name.Equals("dr7", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Dr7); return true; }
        if (name.Equals("controlword", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(ControlWord); return true; }
        if (name.Equals("statusword", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(StatusWord); return true; }
        if (name.Equals("tagword", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(TagWord); return true; }
        if (name.Equals("erroroffset", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(ErrorOffset); return true; }
        if (name.Equals("errorselector", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(ErrorSelector); return true; }
        if (name.Equals("dataoffset", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(DataOffset); return true; }
        if (name.Equals("dataselector", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(DataSelector); return true; }
        if (name.Equals("cr0npxstate", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Cr0NpxState); return true; }
        if (name.Equals("gs", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Gs); return true; }
        if (name.Equals("fs", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Fs); return true; }
        if (name.Equals("es", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Es); return true; }
        if (name.Equals("ds", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ds); return true; }
        if (name.Equals("edi", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Edi); return true; }
        if (name.Equals("esi", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Esi); return true; }
        if (name.Equals("ebx", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ebx); return true; }
        if (name.Equals("edx", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Edx); return true; }
        if (name.Equals("ecx", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ecx); return true; }
        if (name.Equals("eax", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Eax); return true; }
        if (name.Equals("ebp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ebp); return true; }
        if (name.Equals("eip", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Eip); return true; }
        if (name.Equals("cs", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Cs); return true; }
        if (name.Equals("eflags", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(EFlags); return true; }
        if (name.Equals("esp", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Esp); return true; }
        if (name.Equals("ss", StringComparison.OrdinalIgnoreCase)) { value = new TargetNUInt(Ss); return true; }
        return false;
    }

    public bool TrySetRegister(int number, TargetNUInt value)
    {
        switch (number)
        {
            case 0: Eax = (uint)value.Value; return true;
            case 1: Ecx = (uint)value.Value; return true;
            case 2: Edx = (uint)value.Value; return true;
            case 3: Ebx = (uint)value.Value; return true;
            case 4: Esp = (uint)value.Value; return true;
            case 5: Ebp = (uint)value.Value; return true;
            case 6: Esi = (uint)value.Value; return true;
            case 7: Edi = (uint)value.Value; return true;
            default: return false;
        }
    }

    public bool TryReadRegister(int number, out TargetNUInt value)
    {
        switch (number)
        {
            case 0: value = new TargetNUInt(Eax); return true;
            case 1: value = new TargetNUInt(Ecx); return true;
            case 2: value = new TargetNUInt(Edx); return true;
            case 3: value = new TargetNUInt(Ebx); return true;
            case 4: value = new TargetNUInt(Esp); return true;
            case 5: value = new TargetNUInt(Ebp); return true;
            case 6: value = new TargetNUInt(Esi); return true;
            case 7: value = new TargetNUInt(Edi); return true;
            default: value = default; return false;
        }
    }

    // The x87 FP stack exposes 8 logical registers ST(0)-ST(7); each occupies a 10-byte
    // 80-bit slot in the FloatSave register area.
    private const int X87RegisterCount = 8;

    private static int Float80Size
        => (int)Marshal.OffsetOf<X86Context>(nameof(ST1)) - (int)Marshal.OffsetOf<X86Context>(nameof(ST0));

    public readonly bool TryReadFloatingPointRegister(ReadOnlySpan<byte> context, int index, out double value)
    {
        value = 0.0;
        if ((uint)index >= X87RegisterCount)
            return false;

        // The availability mask exposes all 8 slots even when the live stack is shallower;
        // out-of-depth slots read as 0.
        if (!TryGetX87SlotOffset(context, index, out int offset))
            return true;

        if (offset + Float80Size <= context.Length)
            value = FloatConversion.X87ExtendedToDouble(context.Slice(offset, Float80Size));
        return true;
    }

    public readonly bool TryWriteFloatingPointRegister(Span<byte> context, int index, ReadOnlySpan<byte> value)
    {
        if ((uint)index >= X87RegisterCount)
            return false;

        // Writing an out-of-depth (or otherwise unresolvable) slot is not supported.
        if (!TryGetX87SlotOffset(context, index, out int offset) || offset + Float80Size > context.Length)
            return false;

        double d = value.Length == sizeof(float)
            ? BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(value))
            : BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(value));
        FloatConversion.X87DoubleToExtended(d, context.Slice(offset, Float80Size));
        return true;
    }

    // Resolves the byte offset of the 80-bit slot backing logical register ST(<paramref name="logicalIndex"/>).
    // REGISTER_X86_FPSTACK_0 names the bottom of the logical stack: ST(i) with i = top - logicalIndex.
    // Returns false when logicalIndex is beyond the live stack depth.
    private static bool TryGetX87SlotOffset(ReadOnlySpan<byte> context, int logicalIndex, out int offset)
    {
        offset = 0;
        uint statusWord = BinaryPrimitives.ReadUInt32LittleEndian(context.Slice((int)Marshal.OffsetOf<X86Context>(nameof(StatusWord))));
        uint rawTop = (statusWord >> 11) & 0x7;
        uint floatStackTop = 7 - rawTop;
        if ((uint)logicalIndex > floatStackTop)
            return false;
        uint physIdx = (rawTop + (floatStackTop - (uint)logicalIndex)) & 0x7;
        offset = (int)Marshal.OffsetOf<X86Context>(nameof(ST0)) + ((int)physIdx * Float80Size);
        return true;
    }

    public readonly (uint Flag, string Name)[] GetScalarRegisters() => s_scalarRegisters;
    public readonly (uint Flag, int Start, int End)[] GetWideSpans() => s_wideSpans;

    private static readonly (uint Flag, string Name)[] s_scalarRegisters =
    [
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "ebp"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "eip"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "cs"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "eflags"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "esp"),
        ((uint)ContextFlagsValues.CONTEXT_CONTROL, "ss"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "edi"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "esi"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "ebx"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "edx"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "ecx"),
        ((uint)ContextFlagsValues.CONTEXT_INTEGER, "eax"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "gs"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "fs"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "es"),
        ((uint)ContextFlagsValues.CONTEXT_SEGMENTS, "ds"),
    ];

    private static readonly (uint Flag, int Start, int End)[] s_wideSpans =
    [
        ((uint)ContextFlagsValues.CONTEXT_DEBUG_REGISTERS,
            (int)Marshal.OffsetOf<X86Context>(nameof(Dr0)), (int)Marshal.OffsetOf<X86Context>(nameof(ControlWord))),
        ((uint)ContextFlagsValues.CONTEXT_FLOATING_POINT,
            (int)Marshal.OffsetOf<X86Context>(nameof(ControlWord)), (int)Marshal.OffsetOf<X86Context>(nameof(Gs))),
        ((uint)ContextFlagsValues.CONTEXT_EXTENDED_REGISTERS,
            (int)Marshal.OffsetOf<X86Context>(nameof(ExtendedRegisters)), (int)Marshal.OffsetOf<X86Context>(nameof(ExtendedRegisters)) + 512),
    ];

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

    [Register(RegisterType.Control)]
    [FieldOffset(0xbc)]
    public uint Cs;

    [Register(RegisterType.Control)]
    [FieldOffset(0xc0)]
    public uint EFlags;

    [Register(RegisterType.Control | RegisterType.StackPointer)]
    [FieldOffset(0xc4)]
    public uint Esp;

    [Register(RegisterType.Control)]
    [FieldOffset(0xc8)]
    public uint Ss;

    #endregion

    [Register(RegisterType.Extended)]
    [FieldOffset(0xcc)]
    public unsafe fixed byte ExtendedRegisters[512];
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
