// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct ComplexStruct
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(0)]
    public InnerStruct Inner;

    [FieldOffset(8)]
    public double Double;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct InnerStruct
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(8)]
    public int High;

    [FieldOffset(12)]
    public int Low;
}

public class Test_NestedStructsWithExplicitLayout {
    private ComplexStruct currentCount = default;

    private void IncrementCount()
    {
        var x = new ComplexStruct();
        x.Inner.High = currentCount.Inner.High + 1;
        currentCount = x;
    }

    public static int Main ()
    {
        try
        {
            var instance = new Test_NestedStructsWithExplicitLayout();
            instance.IncrementCount();
        }
        catch (TypeLoadException e)
        {
            return 101;
        }
        catch (Exception e)
        {
            return 102;
        }

        return 100;
    }
}
