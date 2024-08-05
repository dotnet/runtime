// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Explicit)]
public struct FirstLevel
{
    [FieldOffset(0)]
    public object? ConflictingObjectField;

    [FieldOffset(0)]
    public SecondLevel SecondLevel;
}

[StructLayout(LayoutKind.Explicit)]
public struct SecondLevel
{
    [FieldOffset(0)]
    public long ConflictingValueTypeField;
}

public class Test_NestedStructsWithExplicitLayout_Case05
{
    private void Run()
    {
        var x = new FirstLevel();
        x.ConflictingObjectField = new object();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            var test = new Test_NestedStructsWithExplicitLayout_Case05();
            test.Run();
        }
        catch (TypeLoadException e)
        {
            Console.WriteLine("PASS: object and non-object field overlap was detected");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL: unexpected exception type");
            return 102;
        }

        Console.WriteLine("FAIL: object and non-object field overlap was not detected");
        return 101;
    }
}
