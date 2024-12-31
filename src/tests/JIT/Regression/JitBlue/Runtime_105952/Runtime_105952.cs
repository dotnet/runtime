// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_105952
{
    [Fact]
    public static void TestEntryPoint()
    {
    	new Runtime_105952().Foo();
    }

    private readonly Vector128<ulong> _field;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Foo()
    {
        M2();
        M(Vector128<ulong>.Zero & _field, M2());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M(Vector128<ulong> v, int x)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int M2() => 0;
}
