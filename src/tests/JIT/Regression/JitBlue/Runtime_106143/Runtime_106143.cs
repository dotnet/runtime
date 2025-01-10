// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_106431
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T M<T>(Vector<T> x) => (x + x).ToScalar();
    
    [Fact]
    public static int Test()
    {
        ulong x = M(new Vector<ulong>(1));
        return (int)x + 98;
    }
}
