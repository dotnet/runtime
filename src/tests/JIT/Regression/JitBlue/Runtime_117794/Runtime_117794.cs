// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_117794
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(Vector128.Create(0u, 0, 1, 1), M0(Vector128<ulong>.AllBitsSet, Vector128.Create(0u, 0, 2, 2)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<uint> M0(Vector128<ulong> v, Vector128<uint> w) => Vector128.ConditionalSelect(
        Vector128.GreaterThan(w, Vector128<uint>.Zero),
        (v & Vector128<uint>.One.AsUInt64()).AsUInt32(),
        Vector128<uint>.Zero
    );
}
