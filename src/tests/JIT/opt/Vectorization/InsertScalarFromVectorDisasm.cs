// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Validates that Insert(vector, vector2.GetElement(idx1), idx2) folds into a single
// insertps that selects the source element via count_s, rather than first moving
// the element to lane 0 with a separate shuffle (movshdup/unpckhps/shufps).

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class InsertScalarFromVectorDisasm
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Lane1ToLane2(Vector128<float> dst, Vector128<float> a, Vector128<float> b)
    {
        // X64-NOT: movshdup
        // X64: insertps {{.*}}, 96
        Vector128<float> src = a + b;
        return dst.WithElement(2, src.GetElement(1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Lane3ToLane1(Vector128<float> dst, Vector128<float> a, Vector128<float> b)
    {
        // X64-NOT: shufps
        // X64: insertps {{.*}}, -48
        Vector128<float> src = a + b;
        return dst.WithElement(1, src.GetElement(3));
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Vector128<float> dst = Vector128.Create(10f, 11f, 12f, 13f);
        Vector128<float> src = Vector128.Create(20f, 21f, 22f, 23f);

        Assert.Equal(Vector128.Create(10f, 11f, 21f, 13f), Lane1ToLane2(dst, src, Vector128<float>.Zero));
        Assert.Equal(Vector128.Create(10f, 23f, 12f, 13f), Lane3ToLane1(dst, src, Vector128<float>.Zero));
    }
}
