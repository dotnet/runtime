// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct ComplexStruct
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(0)]
    public InnerStruct Inner;

    [FieldOffset(8)]
    public double Double;

    [FieldOffset(8)]
    public ulong High;

    [FieldOffset(16)]
    public ulong Low;
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

public class Test_NestedStructsWithExplicitLayout_Case01 {
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
            var instance = new Test_NestedStructsWithExplicitLayout_Case01();
            instance.IncrementCount();
            var result = 99 + instance.currentCount.Inner.High;

            if (result == 100)
            {
                Console.WriteLine("PASS: union of Explicit + Explicit works correctly");
            }

            return result;
        }
        catch (TypeLoadException e)
        {
            Console.WriteLine("FAIL: type was not loaded");
            return 101;
        }
        catch (Exception e)
        {
            return 102;
        }
    }
}
