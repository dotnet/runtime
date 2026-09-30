// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_120792
{
    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 300; i++)
        {
            Problem(42);
            Thread.Sleep(16);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Problem(object value)
    {
        Type tt = value == null ? typeof(object) : value.GetType();
        if (!tt.Equals(typeof(int)) || value == null)
            throw new InvalidOperationException();
    }
}
