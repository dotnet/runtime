// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise division of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IDivisionOperators<T, T, T> =>
            InvokeSpanSpanIntoSpan<T, DivideOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise division of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" />[i] / <paramref name="y" /></c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IDivisionOperators<T, T, T> =>
            InvokeSpanScalarIntoSpan<T, DivideOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise division of numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a scalar.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="DivideByZeroException"><typeparamref name="T"/> is an integer type and an element in <paramref name="y"/> is equal to zero.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <paramref name="x" /> / <paramref name="y" />[i]</c>.
        /// </para>
        /// <para>
        /// If either of the element-wise input values is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the resulting element-wise value is also NaN.
        /// </para>
        /// </remarks>
        public static void Divide<T>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IDivisionOperators<T, T, T> =>
            InvokeScalarSpanIntoSpan<T, DivideOperator<T>>(x, y, destination);

        /// <summary>x / y</summary>
        internal readonly struct DivideOperator<T> : IBinaryOperator<T> where T : IDivisionOperators<T, T, T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float)
                                            || typeof(T) == typeof(double)
                                            || (Vector256.IsHardwareAccelerated && typeof(T) == typeof(int));
            public static T Invoke(T x, T y) => x / y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (typeof(T) == typeof(int))
                {
                    if (Vector128.EqualsAny(y.AsInt32(), Vector128<int>.Zero))
                    {
                        throw new DivideByZeroException();
                    }

                    Vector256<double> num_pd;
                    Vector256<double> den_pd;

                    if (Avx.IsSupported)
                    {
                        num_pd = Avx.ConvertToVector256Double(x.AsInt32());
                        den_pd = Avx.ConvertToVector256Double(y.AsInt32());
                    }
                    else
                    {
                        num_pd = Vector256.ConvertToDouble(Vector256.WidenLower(x.AsInt32().ToVector256Unsafe()));
                        den_pd = Vector256.ConvertToDouble(Vector256.WidenLower(y.AsInt32().ToVector256Unsafe()));
                    }

                    Vector256<double> div_pd = num_pd / den_pd;

                    Vector128<int> div_epi32;

                    if (Avx.IsSupported)
                    {
                        div_epi32 = Avx.ConvertToVector128Int32WithTruncation(div_pd);
                    }
                    else
                    {
                        Vector256<long> div_epi64 = Vector256.ConvertToInt64(div_pd);
                        div_epi32 = Vector128.Narrow(div_epi64.GetLower(), div_epi64.GetUpper());
                    }

                    return div_epi32.As<int, T>();
                }
                return x / y;
            }
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(int))
                {
                    if (!Vector512.IsHardwareAccelerated)
                    {
                        return Invoke(x.GetLower(), y.GetLower()).ToVector256Unsafe().WithUpper(Invoke(x.GetUpper(), y.GetUpper()));
                    }

                    if (Vector256.EqualsAny(y.AsInt32(), Vector256<int>.Zero))
                    {
                        throw new DivideByZeroException();
                    }

                    Vector512<double> num_pd;
                    Vector512<double> den_pd;

                    if (Avx512F.IsSupported)
                    {
                        num_pd = Avx512F.ConvertToVector512Double(x.AsInt32());
                        den_pd = Avx512F.ConvertToVector512Double(y.AsInt32());
                    }
                    else
                    {
                        num_pd = Vector512.ConvertToDouble(Vector512.WidenLower(x.AsInt32().ToVector512Unsafe()));
                        den_pd = Vector512.ConvertToDouble(Vector512.WidenLower(y.AsInt32().ToVector512Unsafe()));
                    }

                    Vector512<double> div_pd = num_pd / den_pd;

                    Vector256<int> div_epi32;

                    if (Avx512F.IsSupported)
                    {
                        div_epi32 = Avx512F.ConvertToVector256Int32WithTruncation(div_pd);
                    }
                    else
                    {
                        Vector512<long> div_epi64 = Vector512.ConvertToInt64(div_pd);
                        div_epi32 = Vector256.Narrow(div_epi64.GetLower(), div_epi64.GetUpper());
                    }
                    return div_epi32.As<int, T>();
                }
                return x / y;
            }
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(int))
                {
                    return Invoke(x.GetLower(), y.GetLower()).ToVector512Unsafe().WithUpper(Invoke(x.GetUpper(), y.GetUpper()));
                }
                return x / y;
            }
        }
    }
}
