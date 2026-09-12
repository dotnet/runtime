// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/70182
//
// LSRA can coalesce identical floating-point, SIMD, and mask constants that are
// simultaneously live into a single interval and reuse the already-materialized
// register instead of re-emitting the constant.
//
// The shapes below are the ones that actually drive that path: they come from
// System.Numerics vector math (Vector3/Vector4.Normalize, Quaternion.Lerp,
// Matrix4x4.Decompose), where the SIMD zero/one constants used by the reciprocal,
// square-root, and sign/compare sequences stay live across several distinct
// consumers. On hardware with AVX-512 masking those constants are register
// resident (rather than contained memory operands), so the coalescing/redefinition
// logic runs and, prior to the fix, could mis-assign registers or trip an LSRA
// assert. Each test drives the shape at full opts and validates the result against
// an independent scalar reference, so a bad register reuse (which produces garbage
// rather than a small rounding difference) is caught.

namespace Runtime_70182;

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_70182
{
    private const float Tolerance = 1e-4f;

    // Kept in non-readonly statics so the JIT cannot constant-fold the operations
    // away and must actually generate the vector-math code under test.
    private static Vector3 s_v3 = new Vector3(1.5f, -2.5f, 3.5f);
    private static Vector4 s_v4 = new Vector4(1.5f, -2.5f, 3.5f, -4.5f);
    private static Quaternion s_qa = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
    private static Quaternion s_qb = new Quaternion(-4.0f, 3.0f, -2.0f, 1.0f);
    private static float s_amount = 0.25f;

    [Fact]
    public static void TestEntryPoint()
    {
        TestVector3Normalize();
        TestVector4Normalize();
        TestQuaternionLerp();
        TestMatrix4x4Decompose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 NormalizeVector3(Vector3 v) => Vector3.Normalize(v);

    private static void TestVector3Normalize()
    {
        Vector3 actual = NormalizeVector3(s_v3);

        double len = System.Math.Sqrt((double)s_v3.X * s_v3.X + (double)s_v3.Y * s_v3.Y + (double)s_v3.Z * s_v3.Z);
        Assert.Equal((float)(s_v3.X / len), actual.X, Tolerance);
        Assert.Equal((float)(s_v3.Y / len), actual.Y, Tolerance);
        Assert.Equal((float)(s_v3.Z / len), actual.Z, Tolerance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 NormalizeVector4(Vector4 v) => Vector4.Normalize(v);

    private static void TestVector4Normalize()
    {
        Vector4 actual = NormalizeVector4(s_v4);

        double len = System.Math.Sqrt((double)s_v4.X * s_v4.X + (double)s_v4.Y * s_v4.Y +
                                      (double)s_v4.Z * s_v4.Z + (double)s_v4.W * s_v4.W);
        Assert.Equal((float)(s_v4.X / len), actual.X, Tolerance);
        Assert.Equal((float)(s_v4.Y / len), actual.Y, Tolerance);
        Assert.Equal((float)(s_v4.Z / len), actual.Z, Tolerance);
        Assert.Equal((float)(s_v4.W / len), actual.W, Tolerance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Quaternion LerpQuaternion(Quaternion a, Quaternion b, float t) => Quaternion.Lerp(a, b, t);

    private static void TestQuaternionLerp()
    {
        Quaternion a = Quaternion.Normalize(s_qa);
        Quaternion b = Quaternion.Normalize(s_qb);
        Quaternion actual = LerpQuaternion(a, b, s_amount);

        // Reference lerp: blend towards the closer orientation, then normalize.
        float dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        float t = s_amount;
        float s = 1.0f - t;
        float sign = dot >= 0.0f ? t : -t;

        float rx = s * a.X + sign * b.X;
        float ry = s * a.Y + sign * b.Y;
        float rz = s * a.Z + sign * b.Z;
        float rw = s * a.W + sign * b.W;
        double rlen = System.Math.Sqrt((double)rx * rx + (double)ry * ry + (double)rz * rz + (double)rw * rw);

        Assert.Equal((float)(rx / rlen), actual.X, Tolerance);
        Assert.Equal((float)(ry / rlen), actual.Y, Tolerance);
        Assert.Equal((float)(rz / rlen), actual.Z, Tolerance);
        Assert.Equal((float)(rw / rlen), actual.W, Tolerance);

        // The lerp result is a unit quaternion regardless of the register the
        // constants land in; a bad reuse would break this invariant.
        double actualLen = System.Math.Sqrt((double)actual.X * actual.X + (double)actual.Y * actual.Y +
                                            (double)actual.Z * actual.Z + (double)actual.W * actual.W);
        Assert.Equal(1.0f, (float)actualLen, Tolerance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Decompose(Matrix4x4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
    {
        return Matrix4x4.Decompose(matrix, out scale, out rotation, out translation);
    }

    private static void TestMatrix4x4Decompose()
    {
        // Compose a matrix from known scale/rotation/translation, then decompose it.
        Vector3 scale = new Vector3(2.0f, 3.0f, 4.0f);
        Quaternion rotation = Quaternion.Normalize(new Quaternion(0.3f, 0.5f, 0.1f, 0.8f));
        Vector3 translation = new Vector3(5.0f, -6.0f, 7.0f);

        Matrix4x4 matrix = Matrix4x4.CreateScale(scale) *
                           Matrix4x4.CreateFromQuaternion(rotation) *
                           Matrix4x4.CreateTranslation(translation);

        bool success = Decompose(matrix, out Vector3 outScale, out Quaternion outRotation, out Vector3 outTranslation);

        Assert.True(success);
        Assert.Equal(scale.X, outScale.X, Tolerance);
        Assert.Equal(scale.Y, outScale.Y, Tolerance);
        Assert.Equal(scale.Z, outScale.Z, Tolerance);
        Assert.Equal(translation.X, outTranslation.X, Tolerance);
        Assert.Equal(translation.Y, outTranslation.Y, Tolerance);
        Assert.Equal(translation.Z, outTranslation.Z, Tolerance);

        // Rotation may come back negated (q and -q are the same orientation); compare
        // via the absolute dot product, which is 1 for equivalent unit quaternions.
        float dot = rotation.X * outRotation.X + rotation.Y * outRotation.Y +
                    rotation.Z * outRotation.Z + rotation.W * outRotation.W;
        Assert.Equal(1.0f, System.Math.Abs(dot), Tolerance);
    }
}
