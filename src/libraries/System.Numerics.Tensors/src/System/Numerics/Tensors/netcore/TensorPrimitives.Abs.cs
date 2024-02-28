// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise absolute value of each number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="OverflowException"><typeparamref name="T"/> is a signed integer type and <paramref name="x"/> contained a value equal to <typeparamref name="T"/>'s minimum value.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Abs(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The absolute value of a <typeparamref name="T"/> is its numeric value without its sign. For example, the absolute value of both 1.2e-03 and -1.2e03 is 1.2e03.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is the original NaN value with the sign bit removed.
        /// </para>
        /// </remarks>
        public static void Abs<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : INumberBase<T> =>
            InvokeSpanIntoSpan<T, AbsoluteOperator<T>>(x, destination);

        /// <summary>T.Abs(x)</summary>
        internal readonly struct AbsoluteOperator<T> : IUnaryOperator<T, T> where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.Abs(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector128<T> abs = Vector128.ConditionalSelect(Vector128.LessThan(x, Vector128<T>.Zero), -x, x);
                    if (Vector128.LessThan(abs, Vector128<T>.Zero) != Vector128<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }
                }

                return Vector128.Abs(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector256<T> abs = Vector256.ConditionalSelect(Vector256.LessThan(x, Vector256<T>.Zero), -x, x);
                    if (Vector256.LessThan(abs, Vector256<T>.Zero) != Vector256<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }
                }

                return Vector256.Abs(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector512<T> abs = Vector512.ConditionalSelect(Vector512.LessThan(x, Vector512<T>.Zero), -x, x);
                    if (Vector512.LessThan(abs, Vector512<T>.Zero) != Vector512<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }
                }

                return Vector512.Abs(x);
            }
        }
    }
}
