// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Threading;
using Xunit;

public class Runtime_136306
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.False(TestAnd(Vector128.Create(1, 0, 0, 0), new int[] { 1, 0, 0, 0 }));
        Assert.False(TestAndNot(0, new int[] { 1, 0, 0, 0 }));
    }

    // The memory operand contained by the AND must not be read after the Interlocked.Exchange
    // once the AND is folded into vptest.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestAnd(Vector128<int> m, int[] a)
    {
        return (m & Vector128.Create(a)) == (Vector128.Create(Interlocked.Exchange(ref a[0], 0)) & Vector128<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestAndNot(int x, int[] a)
    {
        return Vector128.AndNot(Vector128.Create(a), Vector128.Create(x)) == (Vector128.Create(Interlocked.Exchange(ref a[0], 0)) & Vector128<int>.Zero);
    }
}
