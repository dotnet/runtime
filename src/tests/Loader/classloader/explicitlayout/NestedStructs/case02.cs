// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

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

public class Test_NestedStructsWithExplicitLayout_Case02
{
    private int Run(int value)
    {
        var x = new FirstLevel();
        x.Low = value;
        return x.SecondLevel.ThirdLevel.Low;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            var expectedResult = 13;

            var testInstance = new Test_NestedStructsWithExplicitLayout_Case02();
            var result = testInstance.Run(expectedResult);

            if (result == expectedResult)
            {
                Console.WriteLine("PASS: types were loaded correctly");
                return 100;
            }

            Console.WriteLine("FAIL: invalid value");
            return 103;
        }
        catch (TypeLoadException e)
        {
            Console.WriteLine("FAIL: type was not loaded correctly");
            return 101;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL: unknown error");
            return 102;
        }
    }
}
