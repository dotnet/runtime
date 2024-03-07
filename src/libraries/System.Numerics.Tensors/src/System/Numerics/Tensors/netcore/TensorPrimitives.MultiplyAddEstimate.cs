// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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
        /// Behaves the same as either <see cref="MultiplyAdd{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, ReadOnlySpan{T}, Span{T})"/> or
        /// <see cref="FusedMultiplyAdd{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, ReadOnlySpan{T}, Span{T})"/> depending on the current machine's capabilities.
        /// </para>
        /// </remarks>
        public static void MultiplyAddEstimate<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanSpanSpanIntoSpan<T, MultiplyAddEstimateOperator<T>>(x, y, addend, destination);

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
        /// Behaves the same as either <see cref="MultiplyAdd{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, T, Span{T})"/> or
        /// <see cref="FusedMultiplyAdd{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, T, Span{T})"/> depending on the current machine's capabilities.
        /// </para>
        /// </remarks>
        public static void MultiplyAddEstimate<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T addend, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanSpanScalarIntoSpan<T, MultiplyAddEstimateOperator<T>>(x, y, addend, destination);

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
        /// Behaves the same as either <see cref="MultiplyAdd{T}(ReadOnlySpan{T}, T, ReadOnlySpan{T}, Span{T})"/> or
        /// <see cref="FusedMultiplyAdd{T}(ReadOnlySpan{T}, T, ReadOnlySpan{T}, Span{T})"/> depending on the current machine's capabilities.
        /// </para>
        /// </remarks>
        public static void MultiplyAddEstimate<T>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> addend, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanScalarSpanIntoSpan<T, MultiplyAddEstimateOperator<T>>(x, y, addend, destination);

        /// <summary>(x * y) + z</summary>
        private readonly struct MultiplyAddEstimateOperator<T> : ITernaryOperator<T> where T : INumberBase<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y, T z)
            {
                // TODO https://github.com/dotnet/runtime/issues/98053: Use T.MultiplyAddEstimate when it's available.

                if (Fma.IsSupported || AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(Half))
                    {
                        Half result = Half.FusedMultiplyAdd(Unsafe.As<T, Half>(ref x), Unsafe.As<T, Half>(ref y), Unsafe.As<T, Half>(ref z));
                        return Unsafe.As<Half, T>(ref result);
                    }

                    if (typeof(T) == typeof(float))
                    {
                        float result = float.FusedMultiplyAdd(Unsafe.As<T, float>(ref x), Unsafe.As<T, float>(ref y), Unsafe.As<T, float>(ref z));
                        return Unsafe.As<float, T>(ref result);
                    }

                    if (typeof(T) == typeof(double))
                    {
                        double result = double.FusedMultiplyAdd(Unsafe.As<T, double>(ref x), Unsafe.As<T, double>(ref y), Unsafe.As<T, double>(ref z));
                        return Unsafe.As<double, T>(ref result);
                    }
                }

                return (x * y) + z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                return (x * y) + z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z)
            {
                if (Fma.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return (x * y) + z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.FusedMultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.FusedMultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return (x * y) + z;
            }
        }
    }
}
