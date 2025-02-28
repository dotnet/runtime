// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> integer type using saturation on overflow.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.ConvertToInteger(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertToInteger<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : IFloatingPoint<TFrom>
            where TTo : IBinaryInteger<TTo> =>
            InvokeSpanIntoSpan<TFrom, TTo, ConvertToIntegerOperator<TFrom, TTo>>(source, destination);

        /// <summary>TFrom.ConvertToInteger&lt;TTo&gt;(x)</summary>
        internal readonly struct ConvertToIntegerOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo>
            where TFrom : IFloatingPoint<TFrom>
            where TTo : IBinaryInteger<TTo>
        {
            public static TTo Invoke(TFrom x) => TFrom.ConvertToInteger<TTo>(x);

            public static bool Vectorizable =>
                (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(int)) ||
                (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(uint)) ||
                (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(long)) ||
                (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(ulong));

            public static Vector128<TTo> Invoke(Vector128<TFrom> x)
            {
                if (typeof(TFrom) == typeof(float))
                {
                    if (typeof(TTo) == typeof(int)) return Vector128.ConvertToInt32(x.AsSingle()).As<int, TTo>();
                    if (typeof(TTo) == typeof(uint)) return Vector128.ConvertToUInt32(x.AsSingle()).As<uint, TTo>();
                }

                if (typeof(TFrom) == typeof(double))
                {
                    if (typeof(TTo) == typeof(long)) return Vector128.ConvertToInt64(x.AsDouble()).As<long, TTo>();
                    if (typeof(TTo) == typeof(ulong)) return Vector128.ConvertToUInt64(x.AsDouble()).As<ulong, TTo>();
                }

                throw new NotSupportedException();
            }

            public static Vector256<TTo> Invoke(Vector256<TFrom> x)
            {
                if (typeof(TFrom) == typeof(float))
                {
                    if (typeof(TTo) == typeof(int)) return Vector256.ConvertToInt32(x.AsSingle()).As<int, TTo>();
                    if (typeof(TTo) == typeof(uint)) return Vector256.ConvertToUInt32(x.AsSingle()).As<uint, TTo>();
                }

                if (typeof(TFrom) == typeof(double))
                {
                    if (typeof(TTo) == typeof(long)) return Vector256.ConvertToInt64(x.AsDouble()).As<long, TTo>();
                    if (typeof(TTo) == typeof(ulong)) return Vector256.ConvertToUInt64(x.AsDouble()).As<ulong, TTo>();
                }

                throw new NotSupportedException();
            }

            public static Vector512<TTo> Invoke(Vector512<TFrom> x)
            {
                if (typeof(TFrom) == typeof(float))
                {
                    if (typeof(TTo) == typeof(int)) return Vector512.ConvertToInt32(x.AsSingle()).As<int, TTo>();
                    if (typeof(TTo) == typeof(uint)) return Vector512.ConvertToUInt32(x.AsSingle()).As<uint, TTo>();
                }

                if (typeof(TFrom) == typeof(double))
                {
                    if (typeof(TTo) == typeof(long)) return Vector512.ConvertToInt64(x.AsDouble()).As<long, TTo>();
                    if (typeof(TTo) == typeof(ulong)) return Vector512.ConvertToUInt64(x.AsDouble()).As<ulong, TTo>();
                }

                throw new NotSupportedException();
            }
        }
    }
}
