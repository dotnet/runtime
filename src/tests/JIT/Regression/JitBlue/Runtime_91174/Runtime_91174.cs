// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_91174
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Foo(ref Vector256<uint> v1, ref Vector256<uint> v2)
    {
        if (Vector256.ToScalar(v1) < Vector256.ToScalar(v2))
        {
            Console.WriteLine("FAIL");
            return 101;
        }

        return 100;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Vector256<uint> v1 = Vector256.Create<uint>(20);
        Vector256<uint> v2 = Vector256.Create<uint>(10);
        return Foo(ref v1, ref v2);
    }
}
