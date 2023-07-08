// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_80616
{
    [Fact]
    public static int TestEntryPoint()
    {
        Vector<uint> foo = default;
        FooBar(ref foo, default);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void FooBar(ref Vector<uint> foo, Vector<uint> bar)
    {
        foo = bar;
        Expose(ref bar);
        Unsafe.InitBlock(&bar, 0xcd, (uint)sizeof(Vector<uint>));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Expose(ref Vector<uint> f)
    {
    }
}