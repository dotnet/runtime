// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Explicit)]
internal struct FloatNonAlignedFieldWithSmallOffset
{
    [FieldOffset(1)]
    public float field;

    public FloatNonAlignedFieldWithSmallOffset(float a)
    {
        field = a;
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct FloatNonAlignedFieldWithLargeOffset
{
    [FieldOffset(0x10001)]
    public float field;

    public FloatNonAlignedFieldWithLargeOffset(float a)
    {
        field = a;
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct DoubleNonAlignedFieldWithSmallOffset
{
    [FieldOffset(1)]
    public double field;

    public DoubleNonAlignedFieldWithSmallOffset(float a)
    {
        field = a;
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct DoubleNonAlignedFieldWithLargeOffset
{
    [FieldOffset(0x10001)]
    public double field;

    public DoubleNonAlignedFieldWithLargeOffset(float a)
    {
        field = a;
    }
}

struct SimpleStruct
{
    // the field is aligned inside SimpleStruct.
    public float field;
}

[StructLayout(LayoutKind.Explicit)]
internal struct StructNonAlignedField
{
    // SimpleStruct is unaligned, so the result offset to its float field is unaligned.
    [FieldOffset(1)]
    public SimpleStruct field;

    public StructNonAlignedField(float a)
    {
        field.field = a;
    }
}

public class Test_Runtime_34170
{
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        
        var a = new FloatNonAlignedFieldWithSmallOffset(1);
        Debug.Assert(a.field == 1);
        Console.WriteLine(a.field);
        var b = new FloatNonAlignedFieldWithLargeOffset(1);
        Debug.Assert(b.field == 1);
        Console.WriteLine(b.field);

        var c = new DoubleNonAlignedFieldWithSmallOffset(1);
        Debug.Assert(c.field == 1);
        Console.WriteLine(c.field);
        var d = new DoubleNonAlignedFieldWithLargeOffset(1);
        Debug.Assert(d.field == 1);
        Console.WriteLine(d.field);
        
        var e = new StructNonAlignedField(1);
        Debug.Assert(e.field.field == 1);
        Console.WriteLine(e.field.field);

        return 100;
    }
}
