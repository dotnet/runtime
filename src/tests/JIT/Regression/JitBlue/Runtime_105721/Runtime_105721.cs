// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_105721
{
    [Fact]
    public static void TestEntryPoint()
    {
        new Runtime_105721().Foo();
    }

    private short s_21 = -1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Foo()
    {
        Vector128<short> v1 = Vector128.CreateScalar<short>(s_21);
        Vector128<int> v2 = Vector128.CreateScalar<int>(s_21);
        Check(v1, v2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Check(Vector128<short> v1, Vector128<int> v2)
    {
        Assert.Equal(-1, v1[0]);
        Assert.Equal(-1, v2[0]);
    }
}
