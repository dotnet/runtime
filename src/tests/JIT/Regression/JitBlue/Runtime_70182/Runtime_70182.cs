// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/70182
//
// LSRA can coalesce identical floating-point, SIMD, and mask constants that are
// simultaneously live into a single interval and reuse the already-materialized
// register instead of re-emitting the constant. These tests exercise patterns
// where several identical constants overlap (so they are not folded away by CSE)
// and are consumed in distinct registers/operations, then validate that the
// numeric results are still correct.

namespace Runtime_70182;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_70182
{
    [Fact]
    public static void TestEntryPoint()
    {
        TestDoubleConstantReuse();
        TestVectorConstantReuse();
        TestMatrix4x4CreateShadow();
        TestMatrix4x4CreateReflection();
        TestQuaternionNormalizeLerp();
    }

    // Several uses of the same double constant that stay live at once.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double DoubleWork(double x)
    {
        // 3.5 is used four times; keeping multiple copies live at the same time
        // forces the allocator through the coalesce/reuse path rather than a
        // trivial single-def/single-use interval.
        double a = x * 3.5;
        double b = x + 3.5;
        double c = x - 3.5;
        double d = x / 3.5;
        return (a + b) * (c + d) + 3.5;
    }

    private static void TestDoubleConstantReuse()
    {
        double result = DoubleWork(7.0);

        double a = 7.0 * 3.5;
        double b = 7.0 + 3.5;
        double c = 7.0 - 3.5;
        double d = 7.0 / 3.5;
        double expected = (a + b) * (c + d) + 3.5;

        Assert.Equal(expected, result);
    }

    // Several uses of the same vector constant that stay live at once.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> VectorWork(Vector128<float> x)
    {
        Vector128<float> k = Vector128.Create(2.0f, 3.0f, 4.0f, 5.0f);

        // Each expression re-references the identical constant while earlier
        // references are still live.
        Vector128<float> a = x * k;
        Vector128<float> b = x + k;
        Vector128<float> c = a - k;
        Vector128<float> d = b + k;
        return (a + b) + (c + d);
    }

    private static void TestVectorConstantReuse()
    {
        Vector128<float> x = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        Vector128<float> result = VectorWork(x);

        Vector128<float> k = Vector128.Create(2.0f, 3.0f, 4.0f, 5.0f);
        Vector128<float> a = x * k;
        Vector128<float> b = x + k;
        Vector128<float> c = a - k;
        Vector128<float> d = b + k;
        Vector128<float> expected = (a + b) + (c + d);

        Assert.Equal(expected, result);
    }

    // Matrix4x4.CreateShadow materializes several identical zero vectors; this is
    // one of the shapes that regressed without register-tracking fixes.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Matrix4x4 CreateShadow(Vector3 lightDirection, Plane plane)
    {
        return Matrix4x4.CreateShadow(lightDirection, plane);
    }

    private static void TestMatrix4x4CreateShadow()
    {
        var light = new Vector3(1.0f, -2.0f, 3.0f);
        var plane = new Plane(0.0f, 1.0f, 0.0f, 0.0f);

        Assert.Equal(Matrix4x4.CreateShadow(light, plane), CreateShadow(light, plane));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Matrix4x4 CreateReflection(Plane plane)
    {
        return Matrix4x4.CreateReflection(plane);
    }

    private static void TestMatrix4x4CreateReflection()
    {
        var plane = Plane.Normalize(new Plane(1.0f, 2.0f, 3.0f, 4.0f));

        Assert.Equal(Matrix4x4.CreateReflection(plane), CreateReflection(plane));
    }

    // Quaternion.Lerp with normalization produces overlapping zero/mask constants.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Quaternion Lerp(Quaternion a, Quaternion b, float t)
    {
        return Quaternion.Lerp(a, b, t);
    }

    private static void TestQuaternionNormalizeLerp()
    {
        var a = Quaternion.Normalize(new Quaternion(1.0f, 2.0f, 3.0f, 4.0f));
        var b = Quaternion.Normalize(new Quaternion(-4.0f, 3.0f, -2.0f, 1.0f));

        Assert.Equal(Quaternion.Lerp(a, b, 0.25f), Lerp(a, b, 0.25f));
    }
}
