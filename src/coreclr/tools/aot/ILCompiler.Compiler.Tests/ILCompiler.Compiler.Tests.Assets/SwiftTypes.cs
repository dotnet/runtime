// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
