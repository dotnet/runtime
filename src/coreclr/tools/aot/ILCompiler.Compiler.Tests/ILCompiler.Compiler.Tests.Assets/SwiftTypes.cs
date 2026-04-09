// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ILCompiler.Compiler.Tests.Assets.SwiftTypes;

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Double, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int16)]
public struct I64_D_I8_I32_UI16
{
    public long i64;
    public double d;
    public sbyte i8;
    public int i32;
    public ushort ui16;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Double, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int8)]
public struct I64_D_I8_I32_UI8
{
    public long i64;
    public double d;
    public sbyte i8;
    public int i32;
    public byte u8;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int16)]
public struct F5_S1_S0
{
    public short F0;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int16, ExpectedLoweringAttribute.Lowered.Int64)]
public struct F5_S1
{
    public ulong F0;
    public long F1;
    public F5_S1_S0 F2;
    public long F3;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int32)]
[StructLayout(LayoutKind.Sequential, Size = 3)]
public struct F5_S2_S0
{
    public short F0;
    public byte F1;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int32, ExpectedLoweringAttribute.Lowered.Int64)]
public struct F5_S2
{
    public ulong F0;
    public long F1;
    public F5_S2_S0 F2;
    public long F3;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int64)]
[StructLayout(LayoutKind.Sequential, Size = 5)]
public struct ThreeByteStruct_SByte_Byte
{
    public F5_S2_S0 F0;
    public sbyte F1;
    public byte F2;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Double, ExpectedLoweringAttribute.Lowered.Float)]
[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct F2087_S0_S0
{
    public double F0;
    public float F1;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Double, ExpectedLoweringAttribute.Lowered.Float, ExpectedLoweringAttribute.Lowered.Int32, ExpectedLoweringAttribute.Lowered.Int8)]
[StructLayout(LayoutKind.Sequential, Size = 17)]
public struct F2087_S0
{
    public F2087_S0_S0 F0;
    public int F1;
    public sbyte F2;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Float, ExpectedLoweringAttribute.Lowered.Int32, ExpectedLoweringAttribute.Lowered.Int16)]
public struct F114_S0
{
    public float F0;
    public ushort F1;
    public short F2;
    public ushort F3;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Double, ExpectedLoweringAttribute.Lowered.Float, ExpectedLoweringAttribute.Lowered.Int32, ExpectedLoweringAttribute.Lowered.Int32)]
[StructLayout(LayoutKind.Sequential, Size = 20)]
struct F352_S0
{
    public double F0;
    public float F1;
    public uint F2;
    public int F3;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int64)]
[InlineArray(4)]
public struct InlineArray4Longs
{
    private long l;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Float, ExpectedLoweringAttribute.Lowered.Int32, ExpectedLoweringAttribute.Lowered.Int64)]
public struct UnalignedLargeOpaque
{
    public float F0;
    public short F1;
    public short F2;
    public int F3;
    public int F4;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int16, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int64)]
[StructLayout(LayoutKind.Sequential, Size = 21)]
public struct PointerSizeOpaqueBlocks
{
    public short F0;
    public nint F1;
    public int F2;
    public byte F3;
}

public struct PointerSizeOpaqueBlocksNonNaturalAlignment_S0
{
    public byte F0;
    public nint F1;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int16, ExpectedLoweringAttribute.Lowered.Int8, ExpectedLoweringAttribute.Lowered.Int64, ExpectedLoweringAttribute.Lowered.Int64, Offsets = [0x0, 0x8, 0x10, 0x18])]
[StructLayout(LayoutKind.Sequential, Size = 21)]
public struct PointerSizeOpaqueBlocksNonNaturalAlignment
{
    public short F0;
    public PointerSizeOpaqueBlocksNonNaturalAlignment_S0 F1;
    public int F2;
    public byte F3;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Int64)]
public struct F128_S_S0
{
    public sbyte F0;
    public short F1;
    public int F2;
}

[ExpectedLowering(ExpectedLoweringAttribute.Lowered.Float, ExpectedLoweringAttribute.Lowered.Int32, ExpectedLoweringAttribute.Lowered.Int64)]
public struct F128_S
{
    public float F0;
    public F128_S_S0 F1;
    public uint F2;
}
