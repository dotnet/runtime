// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of copying the sign from one number to another number in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="sign">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="sign" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="sign"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.CopySign(<paramref name="x" />[i], <paramref name="sign" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void CopySign<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> sign, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanIntoSpan<T, CopySignOperator<T>>(x, sign, destination);

        /// <summary>Computes the element-wise result of copying the sign from one number to another number in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="sign">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.CopySign(<paramref name="x" />[i], <paramref name="sign" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void CopySign<T>(ReadOnlySpan<T> x, T sign, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarIntoSpan<T, CopySignOperator<T>>(x, sign, destination);

        private readonly struct CopySignOperator<T> : IBinaryOperator<T> where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => T.CopySign(x, y);

            public static TVector Invoke<TVector>(TVector x, TVector y) where TVector : struct, ISimdVector<TVector, T>
            {
                if (typeof(T) == typeof(float))
                {
                    return TVector.ConditionalSelect(TVector.Create((T)(object)(-0.0f)), y, x);
                }

                if (typeof(T) == typeof(double))
                {
                    return TVector.ConditionalSelect(TVector.Create((T)(object)(-0.0d)), y, x);
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    TVector absValue = TVector.Abs(x);
                    TVector sign = TVector.GreaterThanOrEqual(y, TVector.Zero);
                    TVector error = sign & TVector.LessThan(absValue, TVector.Zero);
                    if (error != TVector.Zero)
                    {
                        Math.Abs(int.MinValue); // throw OverflowException
                    }

                    return TVector.ConditionalSelect(sign, absValue, -absValue);
                }

                return x;
            }
        }
    }
}
