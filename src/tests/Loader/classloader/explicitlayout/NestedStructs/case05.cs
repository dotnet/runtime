// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct FirstLevel
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(0)]
    public SecondLevel SecondLevel;
}

[StructLayout(LayoutKind.Explicit)]
public struct SecondLevel
{
    [FieldOffset(0)]
    public long Value;
}

public class Test_NestedStructsWithExplicitLayout_Case05 {
    public static int Main ()
    {
        try
        {
            Console.WriteLine("ABC");
            var x = new FirstLevel();
            x.Object = new object();

            Console.WriteLine("FAIL: object and non-object overlap was not detected");
            return 101;
        }
        catch (TypeLoadException e)
        {
            Console.WriteLine("PASS: object and non-object overlap was detected");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL: unexpected exception type");
            return 102;
        }
    }
}
