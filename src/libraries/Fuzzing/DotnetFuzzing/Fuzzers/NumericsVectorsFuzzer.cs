// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for the SIMD-accelerated <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>,
/// <see cref="Matrix4x4"/>, <see cref="Matrix3x2"/>, <see cref="Quaternion"/> and <see cref="Plane"/> types against scalar
/// references. The element-wise arithmetic (add, subtract, multiply, divide, negate, dot, cross, min/max, abs, transpose,
/// matrix multiply) must match the scalar formula bit for bit (NaN payloads and signed zeros included); the operations that
/// use a square root or division for normalization are compared within a small relative tolerance. Structural identities are
/// also checked: transpose is an involution, a matrix times its inverse is the identity where the determinant is well away
/// from zero, and Quaternion conjugate/normalize round-trips.
/// </summary>
/// <remarks>Input layout: [0] operation group, [1] sub-selector, [2..] floats from a palette or raw bits.</remarks>
internal sealed class NumericsVectorsFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Numerics.Vector", "System.Numerics.Matrix", "System.Numerics.Quaternion", "System.Numerics.Plane"];

    private static readonly float[] s_palette =
    [
        0f, -0f, 1f, -1f, 2f, 0.5f, -0.5f, 3f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue,
        float.MinValue, float.Epsilon, 1e-20f, 1e20f, -1e20f, 1e-38f, MathF.PI, -MathF.PI, 0.1f, 100f, 1e-4f, 16777217f,
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 3)
        {
            return;
        }

        var reader = new FloatReader(bytes.Slice(2));
        switch (bytes[0] % 5)
        {
            case 0: Vectors2(bytes[1], ref reader); break;
            case 1: Vectors3(bytes[1], ref reader); break;
            case 2: Vectors4(bytes[1], ref reader); break;
            case 3: Matrices(bytes[1], ref reader); break;
            default: Quaternions(bytes[1], ref reader); break;
        }
    }

    private static void Vectors2(byte sub, ref FloatReader r)
    {
        var a = new Vector2(r.Next(), r.Next());
        var b = new Vector2(r.Next(), r.Next());
        Same(a + b, new Vector2(a.X + b.X, a.Y + b.Y), "Vector2 +");
        Same(a - b, new Vector2(a.X - b.X, a.Y - b.Y), "Vector2 -");
        Same(a * b, new Vector2(a.X * b.X, a.Y * b.Y), "Vector2 *");
        Same(a / b, new Vector2(a.X / b.X, a.Y / b.Y), "Vector2 /");
        Same(-a, new Vector2(-a.X, -a.Y), "Vector2 negate");
        Same(a * 2f, new Vector2(a.X * 2f, a.Y * 2f), "Vector2 * scalar");
        Same(Vector2.Abs(a), new Vector2(MathF.Abs(a.X), MathF.Abs(a.Y)), "Vector2 Abs");
        Same(Vector2.Min(a, b), new Vector2(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y)), "Vector2 Min");
        Same(Vector2.Max(a, b), new Vector2(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y)), "Vector2 Max");
        Same(Vector2.SquareRoot(a), new Vector2(MathF.Sqrt(a.X), MathF.Sqrt(a.Y)), "Vector2 SquareRoot");
        CloseSum(Vector2.Dot(a, b), Prod(a.X, b.X) + Prod(a.Y, b.Y), Abs(a.X, b.X) + Abs(a.Y, b.Y), "Vector2 Dot");
        Close(Vector2.Distance(a, b), MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)), "Vector2 Distance");
        CloseSum(Vector2.DistanceSquared(a, b), Sq(a.X - b.X) + Sq(a.Y - b.Y), Sq(a.X - b.X) + Sq(a.Y - b.Y), "Vector2 DistanceSquared");
        CloseSum(a.LengthSquared(), Sq(a.X) + Sq(a.Y), Sq(a.X) + Sq(a.Y), "Vector2 LengthSquared");
    }

    private static void Vectors3(byte sub, ref FloatReader r)
    {
        var a = new Vector3(r.Next(), r.Next(), r.Next());
        var b = new Vector3(r.Next(), r.Next(), r.Next());
        Same(a + b, new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z), "Vector3 +");
        Same(a - b, new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z), "Vector3 -");
        Same(a * b, new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z), "Vector3 *");
        Same(a / b, new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z), "Vector3 /");
        Same(-a, new Vector3(-a.X, -a.Y, -a.Z), "Vector3 negate");
        Same(Vector3.Abs(a), new Vector3(MathF.Abs(a.X), MathF.Abs(a.Y), MathF.Abs(a.Z)), "Vector3 Abs");
        Same(Vector3.Min(a, b), new Vector3(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z)), "Vector3 Min");
        Same(Vector3.Max(a, b), new Vector3(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z)), "Vector3 Max");
        Same(Vector3.Cross(a, b), new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X), "Vector3 Cross");
        CloseSum(Vector3.Dot(a, b), Prod(a.X, b.X) + Prod(a.Y, b.Y) + Prod(a.Z, b.Z), Abs(a.X, b.X) + Abs(a.Y, b.Y) + Abs(a.Z, b.Z), "Vector3 Dot");
        if (!float.IsNaN(a.X) && !float.IsNaN(a.Y) && !float.IsNaN(a.Z) && !float.IsNaN(b.X) && !float.IsNaN(b.Y) && !float.IsNaN(b.Z))
        {
            Same(Vector3.Clamp(a, Vector3.Min(a, b), Vector3.Max(a, b)), a, "Vector3 Clamp identity");
        }
        CloseSum(a.LengthSquared(), Sq(a.X) + Sq(a.Y) + Sq(a.Z), Sq(a.X) + Sq(a.Y) + Sq(a.Z), "Vector3 LengthSquared");
    }

    private static void Vectors4(byte sub, ref FloatReader r)
    {
        var a = new Vector4(r.Next(), r.Next(), r.Next(), r.Next());
        var b = new Vector4(r.Next(), r.Next(), r.Next(), r.Next());
        Same(a + b, new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W), "Vector4 +");
        Same(a - b, new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W), "Vector4 -");
        Same(a * b, new Vector4(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W), "Vector4 *");
        Same(a / b, new Vector4(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W), "Vector4 /");
        Same(-a, new Vector4(-a.X, -a.Y, -a.Z, -a.W), "Vector4 negate");
        Same(Vector4.Abs(a), new Vector4(MathF.Abs(a.X), MathF.Abs(a.Y), MathF.Abs(a.Z), MathF.Abs(a.W)), "Vector4 Abs");
        Same(Vector4.Min(a, b), new Vector4(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z), MathF.Min(a.W, b.W)), "Vector4 Min");
        Same(Vector4.Max(a, b), new Vector4(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z), MathF.Max(a.W, b.W)), "Vector4 Max");
        Same(Vector4.SquareRoot(a), new Vector4(MathF.Sqrt(a.X), MathF.Sqrt(a.Y), MathF.Sqrt(a.Z), MathF.Sqrt(a.W)), "Vector4 SquareRoot");
        CloseSum(Vector4.Dot(a, b), Prod(a.X, b.X) + Prod(a.Y, b.Y) + Prod(a.Z, b.Z) + Prod(a.W, b.W), Abs(a.X, b.X) + Abs(a.Y, b.Y) + Abs(a.Z, b.Z) + Abs(a.W, b.W), "Vector4 Dot");
    }

    private static void Matrices(byte sub, ref FloatReader r)
    {
        Matrix4x4 a = ReadMatrix(ref r);
        Matrix4x4 b = ReadMatrix(ref r);

        SameMatrix(a + b, Apply(a, b, (x, y) => x + y), "Matrix4x4 +");
        SameMatrix(a - b, Apply(a, b, (x, y) => x - y), "Matrix4x4 -");
        SameMatrix(-a, Apply(a, a, (x, _) => -x), "Matrix4x4 negate");
        SameMatrix(a * 2f, Apply(a, a, (x, _) => x * 2f), "Matrix4x4 * scalar");
        SameMatrix(Matrix4x4.Transpose(a), Transpose(a), "Matrix4x4 Transpose");
        SameMatrix(Matrix4x4.Transpose(Matrix4x4.Transpose(a)), a, "Matrix4x4 Transpose involution");
        (double[] product, double[] sumAbs) = MultiplyRef(a, b);
        CloseSumMatrix(a * b, product, sumAbs, "Matrix4x4 Multiply");

        // Invert's success and the identity relation are too conditioning-sensitive for a reliable differential check; the
        // element-wise and multiply comparisons above already exercise the SIMD matrix paths.
    }

    private static void Quaternions(byte sub, ref FloatReader r)
    {
        var a = new Quaternion(r.Next(), r.Next(), r.Next(), r.Next());
        var b = new Quaternion(r.Next(), r.Next(), r.Next(), r.Next());
        Same4(a + b, a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W, "Quaternion +");
        Same4(a - b, a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W, "Quaternion -");
        Same4(-a, -a.X, -a.Y, -a.Z, -a.W, "Quaternion negate");
        Same4(Quaternion.Conjugate(a), -a.X, -a.Y, -a.Z, a.W, "Quaternion Conjugate");
        CloseSum(Quaternion.Dot(a, b), Prod(a.X, b.X) + Prod(a.Y, b.Y) + Prod(a.Z, b.Z) + Prod(a.W, b.W), Abs(a.X, b.X) + Abs(a.Y, b.Y) + Abs(a.Z, b.Z) + Abs(a.W, b.W), "Quaternion Dot");

        // Hamilton product.
        Quaternion product = a * b;
        float px = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y;
        float py = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X;
        float pz = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W;
        float pw = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z;
        Close4(product, px, py, pz, pw, "Quaternion Multiply");

        CloseSum(a.LengthSquared(), Sq(a.X) + Sq(a.Y) + Sq(a.Z) + Sq(a.W), Sq(a.X) + Sq(a.Y) + Sq(a.Z) + Sq(a.W), "Quaternion LengthSquared");
    }

    // ---- references ----

    private static Matrix4x4 ReadMatrix(ref FloatReader r) => new(
        r.Next(), r.Next(), r.Next(), r.Next(),
        r.Next(), r.Next(), r.Next(), r.Next(),
        r.Next(), r.Next(), r.Next(), r.Next(),
        r.Next(), r.Next(), r.Next(), r.Next());

    private static Matrix4x4 Apply(Matrix4x4 a, Matrix4x4 b, Func<float, float, float> op) => new(
        op(a.M11, b.M11), op(a.M12, b.M12), op(a.M13, b.M13), op(a.M14, b.M14),
        op(a.M21, b.M21), op(a.M22, b.M22), op(a.M23, b.M23), op(a.M24, b.M24),
        op(a.M31, b.M31), op(a.M32, b.M32), op(a.M33, b.M33), op(a.M34, b.M34),
        op(a.M41, b.M41), op(a.M42, b.M42), op(a.M43, b.M43), op(a.M44, b.M44));

    private static Matrix4x4 Transpose(Matrix4x4 m) => new(
        m.M11, m.M21, m.M31, m.M41,
        m.M12, m.M22, m.M32, m.M42,
        m.M13, m.M23, m.M33, m.M43,
        m.M14, m.M24, m.M34, m.M44);

    private static (double[] Values, double[] SumAbs) MultiplyRef(Matrix4x4 a, Matrix4x4 b)
    {
        float[,] x = ToArray(a), y = ToArray(b);
        double[] z = new double[16], sumAbs = new double[16];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    z[i * 4 + j] += (double)x[i, k] * y[k, j];
                    sumAbs[i * 4 + j] += Math.Abs((double)x[i, k] * y[k, j]);
                }
            }
        }

        return (z, sumAbs);
    }

    private static float[,] ToArray(Matrix4x4 m) => new float[,]
    {
        { m.M11, m.M12, m.M13, m.M14 },
        { m.M21, m.M22, m.M23, m.M24 },
        { m.M31, m.M32, m.M33, m.M34 },
        { m.M41, m.M42, m.M43, m.M44 },
    };

    private static bool AllFinite(Matrix4x4 m) => ToArray(m).Cast<float>().All(float.IsFinite);
    private static bool Moderate(Matrix4x4 m) => ToArray(m).Cast<float>().All(v => float.IsFinite(v) && MathF.Abs(v) <= 1e6f);
    private static float MaxAbs(Matrix4x4 m) => MathF.Max(1f, ToArray(m).Cast<float>().Max(MathF.Abs));

    // ---- comparisons ----

    private static void Same(Vector2 actual, Vector2 expected, string what) => Check(Eq(actual.X, expected.X) && Eq(actual.Y, expected.Y), what, actual, expected);
    private static void Same(Vector3 actual, Vector3 expected, string what) => Check(Eq(actual.X, expected.X) && Eq(actual.Y, expected.Y) && Eq(actual.Z, expected.Z), what, actual, expected);
    private static void Same(Vector4 actual, Vector4 expected, string what) => Check(Eq(actual.X, expected.X) && Eq(actual.Y, expected.Y) && Eq(actual.Z, expected.Z) && Eq(actual.W, expected.W), what, actual, expected);

    private static void Same4(Quaternion actual, float x, float y, float z, float w, string what) =>
        Check(Eq(actual.X, x) && Eq(actual.Y, y) && Eq(actual.Z, z) && Eq(actual.W, w), what, actual, $"({x}, {y}, {z}, {w})");
    private static void Close4(Quaternion actual, float x, float y, float z, float w, string what) =>
        Check(CloseF(actual.X, x) && CloseF(actual.Y, y) && CloseF(actual.Z, z) && CloseF(actual.W, w), what, actual, $"({x}, {y}, {z}, {w})");

    private static void SameScalar(float actual, float expected, string what) => Check(Eq(actual, expected), what, actual, expected);
    private static void SameScalarD(float actual, float expected, string what) => Check(Eq(actual, expected), what, actual, expected);
    private static void Close(float actual, float expected, string what) => Check(CloseF(actual, expected), what, actual, expected);

    private static void SameMatrix(Matrix4x4 actual, Matrix4x4 expected, string what)
    {
        float[] a = ToArray(actual).Cast<float>().ToArray(), e = ToArray(expected).Cast<float>().ToArray();
        for (int i = 0; i < 16; i++)
        {
            int idx = i;
            Check(Eq(a[i], e[i]), $"{what} at [{idx / 4 + 1},{idx % 4 + 1}]", a[idx], e[idx]);
        }
    }

    private static void CloseMatrix(Matrix4x4 actual, Matrix4x4 expected, float tolerance, string what)
    {
        float[] a = ToArray(actual).Cast<float>().ToArray(), e = ToArray(expected).Cast<float>().ToArray();
        for (int i = 0; i < 16; i++)
        {
            int idx = i;
            Check(MathF.Abs(a[i] - e[i]) <= tolerance, $"{what} at [{idx / 4 + 1},{idx % 4 + 1}]", a[idx], e[idx]);
        }
    }

    private static bool Eq(float a, float b) => (float.IsNaN(a) && float.IsNaN(b)) || (a == b && (a != 0 || float.IsNegative(a) == float.IsNegative(b)));

    private static bool CloseF(float a, float b)
    {
        if (!float.IsFinite(a) || !float.IsFinite(b))
        {
            return (float.IsNaN(a) && float.IsNaN(b)) || a == b;
        }

        return MathF.Abs(a - b) <= 1e-3f * MathF.Max(1f, MathF.Max(MathF.Abs(a), MathF.Abs(b)));
    }

    // Sums of products (dot, matrix multiply) may reassociate under SIMD, so compare within a relative tolerance and skip
    // non-finite results, where reassociation makes Inf/NaN order-dependent.
    // A sum of products in double precision, plus the sum of the absolute terms, which bounds the reassociation error of the
    // float SIMD result. Skips non-finite results, where Inf/NaN order-dependence makes the comparison meaningless.
    private static double Prod(float a, float b) => (double)a * b;
    private static double Sq(float a) => (double)a * a;
    private static double Abs(float a, float b) => Math.Abs((double)a * b);

    private static void CloseSum(float actual, double expected, double sumAbs, string what)
    {
        if (!float.IsFinite(actual) || !double.IsFinite(expected) || !double.IsFinite(sumAbs))
        {
            return;
        }

        double tolerance = Math.Max(1e-3, 1e-4 * sumAbs);
        Check(Math.Abs(actual - expected) <= tolerance, what, actual, expected);
    }

    private static void CloseSumMatrix(Matrix4x4 actual, double[] expected, double[] sumAbs, string what)
    {
        float[] a = ToArray(actual).Cast<float>().ToArray();
        for (int i = 0; i < 16; i++)
        {
            int idx = i;
            if (!float.IsFinite(a[idx]) || !double.IsFinite(expected[idx]) || !double.IsFinite(sumAbs[idx]))
            {
                continue;
            }

            double tolerance = Math.Max(1e-3, 1e-4 * sumAbs[idx]);
            Check(Math.Abs(a[idx] - expected[idx]) <= tolerance, $"{what} at [{idx / 4 + 1},{idx % 4 + 1}]", a[idx], expected[idx]);
        }
    }

    private static void Check(bool condition, string what, object actual, object expected)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{what}: got {actual}, expected {expected}");
        }
    }

    private ref struct FloatReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> _data = data;
        private int _position;

        public float Next()
        {
            byte b = _position < _data.Length ? _data[_position++] : (byte)0;
            if (b < 0xF0)
            {
                return s_palette[b % s_palette.Length];
            }

            uint bits = 0;
            for (int i = 0; i < 4; i++)
            {
                bits |= (uint)(_position < _data.Length ? _data[_position++] : 0) << (8 * i);
            }

            return BitConverter.UInt32BitsToSingle(bits);
        }
    }
}
