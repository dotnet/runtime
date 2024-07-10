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

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector128.ConditionalSelect(Vector128.Create(-0.0f).As<float, T>(), y, x);
                }

                if (typeof(T) == typeof(double))
                {
                    return Vector128.ConditionalSelect(Vector128.Create(-0.0d).As<double, T>(), y, x);
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector128<T> absValue = Vector128.Abs(x);
                    Vector128<T> sign = Vector128.GreaterThanOrEqual(y, Vector128<T>.Zero);
                    Vector128<T> error = sign & Vector128.LessThan(absValue, Vector128<T>.Zero);
                    if (error != Vector128<T>.Zero)
                    {
                        Math.Abs(int.MinValue); // throw OverflowException
                    }

                    return Vector128.ConditionalSelect(sign, absValue, -absValue);
                }

                return x;
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector256.ConditionalSelect(Vector256.Create(-0.0f).As<float, T>(), y, x);
                }

                if (typeof(T) == typeof(double))
                {
                    return Vector256.ConditionalSelect(Vector256.Create(-0.0d).As<double, T>(), y, x);
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector256<T> absValue = Vector256.Abs(x);
                    Vector256<T> sign = Vector256.GreaterThanOrEqual(y, Vector256<T>.Zero);
                    Vector256<T> error = sign & Vector256.LessThan(absValue, Vector256<T>.Zero);
                    if (error != Vector256<T>.Zero)
                    {
                        Math.Abs(int.MinValue); // throw OverflowException
                    }

                    return Vector256.ConditionalSelect(sign, absValue, -absValue);
                }

                return x;
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector512.ConditionalSelect(Vector512.Create(-0.0f).As<float, T>(), y, x);
                }

                if (typeof(T) == typeof(double))
                {
                    return Vector512.ConditionalSelect(Vector512.Create(-0.0d).As<double, T>(), y, x);
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector512<T> absValue = Vector512.Abs(x);
                    Vector512<T> sign = Vector512.GreaterThanOrEqual(y, Vector512<T>.Zero);
                    Vector512<T> error = sign & Vector512.LessThan(absValue, Vector512<T>.Zero);
                    if (error != Vector512<T>.Zero)
                    {
                        Math.Abs(int.MinValue); // throw OverflowException
                    }

                    return Vector512.ConditionalSelect(sign, absValue, -absValue);
                }

                return x;
            }
        }
    }
}
