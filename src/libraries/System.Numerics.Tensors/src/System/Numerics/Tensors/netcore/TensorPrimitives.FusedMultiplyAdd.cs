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

            public static TVector Invoke<TVector>(TVector x, TVector y, TVector z) where TVector : struct, ISimdVector<TVector, T>
            {
                if (sizeof(TVector) == sizeof(Vector128<T>))
                {
                    if (Fma.IsSupported)
                    {
                        if (typeof(T) == typeof(float)) return (TVector)(object)Fma.MultiplyAdd((Vector128<float>)(object)x, (Vector128<float>)(object)y, (Vector128<float>)(object)z);
                        if (typeof(T) == typeof(double)) return (TVector)(object)Fma.MultiplyAdd((Vector128<double>)(object)x, (Vector128<double>)(object)y, (Vector128<double>)(object)z);
                    }

                    if (AdvSimd.IsSupported)
                    {
                        if (typeof(T) == typeof(float)) return (TVector)(object)AdvSimd.FusedMultiplyAdd((Vector128<float>)(object)z, (Vector128<float>)(object)x, (Vector128<float>)(object)y);
                    }

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        if (typeof(T) == typeof(double)) return (TVector)(object)AdvSimd.Arm64.FusedMultiplyAdd((Vector128<double>)(object)z, (Vector128<double>)(object)x, (Vector128<double>)(object)y);
                    }
                }

                if (sizeof(TVector) == sizeof(Vector256<T>))
                {
                    if (Fma.IsSupported)
                    {
                        if (typeof(T) == typeof(float)) return (TVector)(object)Fma.MultiplyAdd((Vector256<float>)(object)x, (Vector256<float>)(object)y, (Vector256<float>)(object)z);
                        if (typeof(T) == typeof(double)) return (TVector)(object)Fma.MultiplyAdd((Vector256<double>)(object)x, (Vector256<double>)(object)y, (Vector256<double>)(object)z);
                    }
                }

                if (sizeof(TVector) == sizeof(Vector512<T>))
                {
                    if (Avx512F.IsSupported)
                    {
                        if (typeof(T) == typeof(float)) return (TVector)(object)Avx512F.FusedMultiplyAdd(((Vector512<T>)(object)x).AsSingle(), ((Vector512<T>)(object)y).AsSingle(), ((Vector512<T>)(object)z).AsSingle());
                        if (typeof(T) == typeof(double)) return (TVector)(object)Avx512F.FusedMultiplyAdd(((Vector512<T>)(object)x).AsDouble(), ((Vector512<T>)(object)y).AsDouble(), ((Vector512<T>)(object)z).AsDouble());
                    }
                }

                if (sizeof(T) == sizeof(Vector128<T>))
                {
                    if (typeof(T) == typeof(float))
                    {
                        Vector128<float> xFloats = (Vector128<float>)(object)x;
                        Vector128<float> yFloats = (Vector128<float>)(object)y;
                        Vector128<float> zFloats = (Vector128<float>)(object)z;
                        return Vector128.Create(
                            float.FusedMultiplyAdd(xFloats[0], yFloats[0], zFloats[0]),
                            float.FusedMultiplyAdd(xFloats[1], yFloats[1], zFloats[1]),
                            float.FusedMultiplyAdd(xFloats[2], yFloats[2], zFloats[2]),
                            float.FusedMultiplyAdd(xFloats[3], yFloats[3], zFloats[3])).As<float, T>();
                    }
                    else
                    {
                        Debug.Assert(typeof(T) == typeof(double));
                        Vector128<double> xDoubles = (Vector128<double>)(object)x;
                        Vector128<double> yDoubles = (Vector128<double>)(object)y;
                        Vector128<double> zDoubles = (Vector128<double>)(object)z;
                        return Vector128.Create(
                            double.FusedMultiplyAdd(xDoubles[0], yDoubles[0], zDoubles[0]),
                            double.FusedMultiplyAdd(xDoubles[1], yDoubles[1], zDoubles[1])).As<double, T>();
                    }
                }
                else
                {
                    return TernaryOperatorAutoImpl<T, FusedMultiplyAddOperator<T>, TVector>(x, y, z);
                }
            }
        }
    }
}
