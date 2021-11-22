// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct FirstLevel
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(0)]
    public SecondLevel SecondLevel;

    [FieldOffset(8)]
    public int High;

    [FieldOffset(12)]
    public int Low;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct SecondLevel
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(0)]
    public ThirdLevel ThirdLevel;

    [FieldOffset(8)]
    public int High;

    [FieldOffset(12)]
    public int Low;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct ThirdLevel
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(8)]
    public int High;

    [FieldOffset(12)]
    public int Low;
}

public class Test_NestedStructsWithExplicitLayout_Case02 {
    public static int Main ()
    {
        // Console.WriteLine($"FirstLevel: {sizeof(FirstLevel)}");
        // Console.WriteLine($"SecondLevel: {sizeof(SecondLevel)}");
        // Console.WriteLine($"ThirdLevel: {sizeof(ThirdLevel)}");

        try
        {
            var x = new FirstLevel();
            x.Low = 13;
            return 87 + x.SecondLevel.ThirdLevel.Low;
        }
        catch (TypeLoadException e)
        {
            return 101;
        }
        catch (Exception e)
        {
            return 102;
        }
    }
}
