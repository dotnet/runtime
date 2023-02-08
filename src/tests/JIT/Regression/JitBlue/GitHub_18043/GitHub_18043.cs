// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

// GitHub18043: ensure dead box optimizations treat Vector<float> as a struct type.

public class X
{
    public static int VT()
    {
        Vector<float> f = new Vector<float>(4);
        Vector<float>[] a = new Vector<float>[10];
        a[5] = f;
        return Array.IndexOf(a, f);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int r1 = VT();
        return (r1 == 5 ? 100 : 0);
    }
}
