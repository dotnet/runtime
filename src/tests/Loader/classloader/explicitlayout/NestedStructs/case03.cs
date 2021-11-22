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

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct SecondLevel
{
    public object? Object;
    public int High;
    public int Low;
}

public class Test_NestedStructsWithExplicitLayout_Case03 {
    public static int Main ()
    {
        try
        {
            var x = new FirstLevel();
            x.Low = 3;
            var result = 97 + x.SecondLevel.Low;

            if (result == 100)
            {
                Console.WriteLine("PASS: union of Explict + Sequential works correctly");
            }

            return result;
        }
        catch (TypeLoadException e)
        {
            Console.WriteLine("FAIL: type loading failed");
            return 101;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL: unexpected exception type");
            return 102;
        }
    }
}
