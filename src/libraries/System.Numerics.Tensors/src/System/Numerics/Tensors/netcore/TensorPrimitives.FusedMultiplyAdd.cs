// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="addend">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" /> and length of <paramref name="addend" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="addend"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />[i]) + <paramref name="addend" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// <para>
        /// This computes (<paramref name="x"/> * <paramref name="y"/>) as if to infinite precision, adds <paramref name="addend"/> to that result as if to
        /// infinite precision, and finally rounds to the nearest representable value. This differs from the non-fused sequence which would compute
        /// (<paramref name="x"/> * <paramref name="y"/>) as if to infinite precision, round the result to the nearest representable value, add <paramref name="addend"/> to the
        /// rounded result as if to infinite precision, and finally round to the nearest representable value.
        /// </para>
        /// </remarks>
        public static void FusedMultiplyAdd<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanSpanIntoSpan<T, FusedMultiplyAddOperator<T>>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="addend">The third tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />[i]) + <paramref name="addend" /></c>.
        /// It corresponds to the <c>axpy</c> method defined by <c>BLAS1</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// <para>
        /// This computes (<paramref name="x"/> * <paramref name="y"/>) as if to infinite precision, adds <paramref name="addend"/> to that result as if to
        /// infinite precision, and finally rounds to the nearest representable value. This differs from the non-fused sequence which would compute
        /// (<paramref name="x"/> * <paramref name="y"/>) as if to infinite precision, round the result to the nearest representable value, add <paramref name="addend"/> to the
        /// rounded result as if to infinite precision, and finally round to the nearest representable value.
        /// </para>
        /// </remarks>
        public static void FusedMultiplyAdd<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T addend, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanSpanScalarIntoSpan<T, FusedMultiplyAddOperator<T>>(x, y, addend, destination);

        /// <summary>Computes the element-wise result of <c>(<paramref name="x" /> * <paramref name="y" />) * <paramref name="addend" /></c> for the specified tensors of numbers.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="addend">The third tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="addend" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="addend"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (<paramref name="x" />[i] * <paramref name="y" />) + <paramref name="addend" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// <para>
        /// This computes (<paramref name="x"/> * <paramref name="y"/>) as if to infinite precision, adds <paramref name="addend"/> to that result as if to
        /// infinite precision, and finally rounds to the nearest representable value. This differs from the non-fused sequence which would compute
        /// (<paramref name="x"/> * <paramref name="y"/>) as if to infinite precision, round the result to the nearest representable value, add <paramref name="addend"/> to the
        /// rounded result as if to infinite precision, and finally round to the nearest representable value.
        /// </para>
        /// </remarks>
        public static void FusedMultiplyAdd<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanScalarSpanIntoSpan<T, FusedMultiplyAddOperator<T>>(x, y, addend, destination);

        /// <summary>(x * y) + z</summary>
        private readonly struct FusedMultiplyAddOperator<T> : ITernaryOperator<T> where T : IFloatingPointIeee754<T>
        {
            public static T Invoke(T x, T y, T z) => T.FusedMultiplyAdd(x, y, z);

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z)
            {
                if (Fma.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return AdvSimd.FusedMultiplyAdd(z.AsSingle(), x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.FusedMultiplyAdd(z.AsDouble(), x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float))
                {
                    Vector128<float> xFloats = x.AsSingle();
                    Vector128<float> yFloats = y.AsSingle();
                    Vector128<float> zFloats = z.AsSingle();
                    return Vector128.Create(
                        float.FusedMultiplyAdd(xFloats[0], yFloats[0], zFloats[0]),
                        float.FusedMultiplyAdd(xFloats[1], yFloats[1], zFloats[1]),
                        float.FusedMultiplyAdd(xFloats[2], yFloats[2], zFloats[2]),
                        float.FusedMultiplyAdd(xFloats[3], yFloats[3], zFloats[3])).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector128<double> xDoubles = x.AsDouble();
                    Vector128<double> yDoubles = y.AsDouble();
                    Vector128<double> zDoubles = z.AsDouble();
                    return Vector128.Create(
                        double.FusedMultiplyAdd(xDoubles[0], yDoubles[0], zDoubles[0]),
                        double.FusedMultiplyAdd(xDoubles[1], yDoubles[1], zDoubles[1])).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z)
            {
                if (Fma.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return Vector256.Create(
                    Invoke(x.GetLower(), y.GetLower(), z.GetLower()),
                    Invoke(x.GetUpper(), y.GetUpper(), z.GetUpper()));
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.FusedMultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.FusedMultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return Vector512.Create(
                    Invoke(x.GetLower(), y.GetLower(), z.GetLower()),
                    Invoke(x.GetUpper(), y.GetUpper(), z.GetUpper()));
            }
        }
    }
}
