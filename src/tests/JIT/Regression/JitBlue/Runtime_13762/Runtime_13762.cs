// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_13762
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Problem(null);
        }
        catch (NullReferenceException)
        {
            return 100;
        }

        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Problem(ClassWithField a)
    {
        a.Field = a.Call(ref a.Field);
    }
}

public class ClassWithField
{
    public int Field;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Call(ref int a)
    {
        throw new Exception("This should have been an NRE!");
    }
}
