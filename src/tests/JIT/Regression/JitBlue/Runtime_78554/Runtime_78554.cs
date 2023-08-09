// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_78554
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(uint op)
    {
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void ArrayIndexConsume(uint[] a, uint i)
    {
        if (i < a.Length)
        {
           i = a[i];
        }
        Consume(i);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var arr = new uint[] { 1, 42, 3000 };
        ArrayIndexConsume(arr, 0xffffffff);
        return 100;
    }
}
