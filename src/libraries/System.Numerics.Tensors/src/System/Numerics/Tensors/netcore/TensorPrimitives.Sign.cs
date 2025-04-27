// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise sign of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Sign(<paramref name="x" />[i])</c>.
        /// If the value is less than 0, the result is -1; if the value is 0, the result is 0; if the value is greater than 0, the result is 1.
        /// </para>
        /// </remarks>
        public static void Sign<T>(ReadOnlySpan<T> x, Span<int> destination)
            where T : INumber<T> =>
            InvokeSpanIntoSpan<T, int, SignOperator<T>>(x, destination);

        /// <summary>T.Sign(x)</summary>
        private readonly struct SignOperator<T> : IUnaryOperator<T, int>
            where T : INumber<T>
        {
            public static unsafe bool Vectorizable =>
                // TODO: Extend vectorization to handle primitives whose size is not the same as int
                typeof(T) == typeof(uint) || typeof(T) == typeof(int) || typeof(T) == typeof(float);

            public static int Invoke(T x) => T.Sign(x);

            public static Vector128<int> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(uint))
                {
                    return Vector128.ConditionalSelect(Vector128.Equals(x, Vector128<T>.Zero).AsInt32(),
                        Vector128<int>.Zero,
                        Vector128<int>.One);
                }

                if (typeof(T) == typeof(int))
                {
                    Vector128<int> value = x.AsInt32();
                    return (value >> 31) | ((-value).AsUInt32() >> 31).AsInt32();
                }

                if (Vector128.EqualsAny(IsNaN(x).AsInt32(), Vector128<int>.AllBitsSet))
                {
                    ThrowHelper.ThrowArithmetic_NaN();
                }

                return Vector128.ConditionalSelect(Vector128.LessThan(x, Vector128<T>.Zero).AsInt32(),
                    Vector128.Create(-1),
                    Vector128.ConditionalSelect(Vector128.GreaterThan(x, Vector128<T>.Zero).AsInt32(),
                        Vector128<int>.One,
                        Vector128<int>.Zero));
            }

            public static Vector256<int> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(uint))
                {
                    return Vector256.ConditionalSelect(Vector256.Equals(x, Vector256<T>.Zero).AsInt32(),
                        Vector256<int>.Zero,
                        Vector256<int>.One);
                }

                if (typeof(T) == typeof(int))
                {
                    Vector256<int> value = x.AsInt32();
                    return (value >> 31) | ((-value).AsUInt32() >> 31).AsInt32();
                }

                if (Vector256.EqualsAny(IsNaN(x).AsInt32(), Vector256<int>.AllBitsSet))
                {
                    ThrowHelper.ThrowArithmetic_NaN();
                }

                return Vector256.ConditionalSelect(Vector256.LessThan(x, Vector256<T>.Zero).AsInt32(),
                    Vector256.Create(-1),
                    Vector256.ConditionalSelect(Vector256.GreaterThan(x, Vector256<T>.Zero).AsInt32(),
                        Vector256<int>.One,
                        Vector256<int>.Zero));
            }

            public static Vector512<int> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(uint))
                {
                    return Vector512.ConditionalSelect(Vector512.Equals(x, Vector512<T>.Zero).AsInt32(),
                        Vector512<int>.Zero,
                        Vector512<int>.One);
                }
                else if (typeof(T) == typeof(int))
                {
                    Vector512<int> value = x.AsInt32();
                    return (value >> 31) | ((-value).AsUInt32() >> 31).AsInt32();
                }
                else
                {
                    if (Vector512.EqualsAny(IsNaN(x).AsInt32(), Vector512<int>.AllBitsSet))
                    {
                        ThrowHelper.ThrowArithmetic_NaN();
                    }

                    return Vector512.ConditionalSelect(Vector512.LessThan(x, Vector512<T>.Zero).AsInt32(),
                        Vector512.Create(-1),
                        Vector512.ConditionalSelect(Vector512.GreaterThan(x, Vector512<T>.Zero).AsInt32(),
                            Vector512<int>.One,
                            Vector512<int>.Zero));
                }
            }
        }
    }
}
