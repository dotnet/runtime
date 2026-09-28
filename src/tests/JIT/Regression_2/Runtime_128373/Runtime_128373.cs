// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

// Repro for https://github.com/dotnet/runtime/issues/128373.
//
// Under tiered compilation with tiered PGO on the SysV x64 ABI, copy assertion
// propagation and forward substitution could both introduce an illegal
// `LCL_VAR` of a promoted (non-DNER) struct local into a `FIELD_LIST` entry of
// a multi-reg return, hitting a Lowering::CheckNode assert in Checked builds
// and segfaulting LSRA in Release builds.

public class Runtime_128373
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Repeatedly invoke ProblematicBody until tiered compilation produces
        // the optimized version that used to hit the assert.
        for (int i = 0; i < 300; i++)
        {
            CreateWrappedInputs(i, out var forward, out var up);
            WrappedQuaternion q = ProblematicBody(forward, up);
            Assert.False(float.IsNaN(q.x));
            Thread.Sleep(16);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CreateWrappedInputs(int i, out WrappedVector3 forward, out WrappedVector3 up)
    {
        forward = new WrappedVector3(0.25f + (i * 0.00001f), 1.25f, -0.75f).normalized;
        up = new WrappedVector3(0.05f, 1.0f, 0.15f + (i * 0.00002f)).normalized;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WrappedQuaternion ProblematicBody(WrappedVector3 forward, WrappedVector3 upwards)
    {
        var epsilonSquared = WrappedVector3.kEpsilon * WrappedVector3.kEpsilon;
        var forwardVector = WrappedVector3.ToNumerics(forward);
        if (forwardVector.LengthSquared() <= epsilonSquared)
            return WrappedQuaternion.identity;

        var forwardNormalized = NumericsVector3.Normalize(forwardVector);
        var upVector = WrappedVector3.ToNumerics(upwards);
        var upNormalized = upVector.LengthSquared() <= epsilonSquared ? NumericsVector3.UnitY : NumericsVector3.Normalize(upVector);

        var right = NumericsVector3.Cross(upNormalized, forwardNormalized);
        if (right.LengthSquared() <= epsilonSquared)
        {
            right = NumericsVector3.Cross(NumericsVector3.UnitY, forwardNormalized);
            if (right.LengthSquared() <= epsilonSquared)
                right = NumericsVector3.UnitX;
        }

        right = NumericsVector3.Normalize(right);
        var up = NumericsVector3.Cross(forwardNormalized, right);
        var matrix = new NumericsMatrix4x4(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            forwardNormalized.X, forwardNormalized.Y, forwardNormalized.Z, 0f,
            0f, 0f, 0f, 1f);

        return WrappedQuaternion.FromNumerics(NumericsQuaternion.CreateFromRotationMatrix(matrix));
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct WrappedVector3
    {
        public const float kEpsilon = 1E-05f;

        [FieldOffset(0)] private NumericsVector3 _value;
        [FieldOffset(0)] public float x;
        [FieldOffset(4)] public float y;
        [FieldOffset(8)] public float z;

        public WrappedVector3(float x, float y, float z)
        {
            this = default;
            _value = new NumericsVector3(x, y, z);
        }

        public float magnitude => _value.Length();
        public WrappedVector3 normalized => magnitude > kEpsilon ? this / magnitude : zero;
        public static WrappedVector3 zero => new(0f, 0f, 0f);
        public static WrappedVector3 operator /(WrappedVector3 a, float d) => FromNumerics(a._value / d);

        internal static NumericsVector3 ToNumerics(WrappedVector3 value) => value._value;
        internal static WrappedVector3 FromNumerics(NumericsVector3 value) => new() { _value = value };
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct WrappedQuaternion
    {
        [FieldOffset(0)] private NumericsQuaternion _value;
        [FieldOffset(0)] public float x;
        [FieldOffset(4)] public float y;
        [FieldOffset(8)] public float z;
        [FieldOffset(12)] public float w;

        public static WrappedQuaternion identity => new(0f, 0f, 0f, 1f);

        public WrappedQuaternion(float x, float y, float z, float w)
        {
            this = default;
            _value = new NumericsQuaternion(x, y, z, w);
        }

        internal static WrappedQuaternion FromNumerics(NumericsQuaternion value) => new() { _value = value };
    }
}
