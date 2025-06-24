// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> integer type using platform-specific behavior on overflow.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.ConvertToInteger(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertToIntegerNative<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : IFloatingPoint<TFrom>
            where TTo : IBinaryInteger<TTo> =>
            InvokeSpanIntoSpan<TFrom, TTo, ConvertToIntegerNativeOperator<TFrom, TTo>>(source, destination);

        /// <summary>TFrom.ConvertToInteger&lt;TTo&gt;(x)</summary>
        internal readonly struct ConvertToIntegerNativeOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo>
            where TFrom : IFloatingPoint<TFrom>
            where TTo : IBinaryInteger<TTo>
        {
            public static TTo Invoke(TFrom x) => TFrom.ConvertToIntegerNative<TTo>(x);

            public static unsafe bool Vectorizable =>
                sizeof(TFrom) == sizeof(TTo);

            public static Vector128<TTo> Invoke(Vector128<TFrom> x)
            {
                if (typeof(TFrom) == typeof(float))
                {
                    return Vector128.IsNegative(Vector128<TTo>.AllBitsSet) != Vector128<TTo>.Zero ?
                        Vector128.ConvertToInt32Native(x.AsSingle()).As<int, TTo>() :
                        Vector128.ConvertToUInt32Native(x.AsSingle()).As<uint, TTo>();
                }

                if (typeof(TFrom) == typeof(double))
                {
                    return Vector128.IsNegative(Vector128<TTo>.AllBitsSet) != Vector128<TTo>.Zero ?
                        Vector128.ConvertToInt64Native(x.AsDouble()).As<long, TTo>() :
                        Vector128.ConvertToUInt64Native(x.AsDouble()).As<ulong, TTo>();
                }

                throw new NotSupportedException();
            }

            public static Vector256<TTo> Invoke(Vector256<TFrom> x)
            {
                if (typeof(TFrom) == typeof(float))
                {
                    return Vector256.IsNegative(Vector256<TTo>.AllBitsSet) != Vector256<TTo>.Zero ?
                        Vector256.ConvertToInt32Native(x.AsSingle()).As<int, TTo>() :
                        Vector256.ConvertToUInt32Native(x.AsSingle()).As<uint, TTo>();
                }

                if (typeof(TFrom) == typeof(double))
                {
                    return Vector256.IsNegative(Vector256<TTo>.AllBitsSet) != Vector256<TTo>.Zero ?
                        Vector256.ConvertToInt64Native(x.AsDouble()).As<long, TTo>() :
                        Vector256.ConvertToUInt64Native(x.AsDouble()).As<ulong, TTo>();
                }

                throw new NotSupportedException();
            }

            public static Vector512<TTo> Invoke(Vector512<TFrom> x)
            {
                if (typeof(TFrom) == typeof(float))
                {
                    return Vector512.IsNegative(Vector512<TTo>.AllBitsSet) != Vector512<TTo>.Zero ?
                        Vector512.ConvertToInt32Native(x.AsSingle()).As<int, TTo>() :
                        Vector512.ConvertToUInt32Native(x.AsSingle()).As<uint, TTo>();
                }

                if (typeof(TFrom) == typeof(double))
                {
                    return Vector512.IsNegative(Vector512<TTo>.AllBitsSet) != Vector512<TTo>.Zero ?
                        Vector512.ConvertToInt64Native(x.AsDouble()).As<long, TTo>() :
                        Vector512.ConvertToUInt64Native(x.AsDouble()).As<ulong, TTo>();
                }

                throw new NotSupportedException();
            }
        }
    }
}
