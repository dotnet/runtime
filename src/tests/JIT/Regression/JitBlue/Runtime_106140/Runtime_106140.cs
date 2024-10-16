// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_106140
{
    public struct S1
    {
        public double F0;
        public Vector256<int> F1;
        public S1(bool f3) : this()
        {
            F1 = Vector256.Create(1,2,100,4,5,6,7,8);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int t(S1 s)
    {
        return s.F1.GetElement(2);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(100, t(new S1(false)));
    }
}
