// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_134487
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(Vector128.Create(-1, 0, -1, 0),
                     MaskChain(Vector128.Create(1, 2, 3, 4), Vector128.Create(1, 0, 3, 0)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> MaskChain(Vector128<int> a, Vector128<int> b)
    {
        Vector128<int> m = Vector128.Equals(a, b);
        Vector128<int> k = Vector128.GreaterThan(a, b);

        // Each step shares the previous mask VN along two paths. Keep the chain unrolled so
        // the mask query encounters a DAG rather than a loop phi.
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;
        m ^= m & k;

        return Vector128.Equals(m, Vector128<int>.AllBitsSet);
    }
}
