// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_91862
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Foo(default);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(Vector128<float> v)
    {
        int result = 101;
        // This tree results in a BOUNDS_CHECK for Bar(...) & 3
        float x = Vector128.GetElement(v, Bar(ref result) & 3);

        if (result != 100)
        {
            Console.WriteLine("FAIL");
        }

        // After inlining x is DCE'd, which will extract side effects of its assignment above.
        // That results IR amenable to forward sub, and we end up with a BOUNDS_CHECK
        // with a complex index expression that we can still prove is within bounds.
        Baz(x);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Bar(ref int result)
    {
        result = 100;
        return 0;
    }

    private static void Baz(float x)
    {
    }
}
