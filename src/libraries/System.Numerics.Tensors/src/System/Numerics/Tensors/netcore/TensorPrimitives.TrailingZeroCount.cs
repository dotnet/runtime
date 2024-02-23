// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.TrailingZeroCount(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void TrailingZeroCount<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan<T, TrailingZeroCountOperator<T>>(x, destination);

        /// <summary>T.TrailingZeroCount(x)</summary>
        private readonly unsafe struct TrailingZeroCountOperator<T> : IUnaryOperator<T, T> where T : IBinaryInteger<T>
        {
            public static bool Vectorizable =>
                (AdvSimd.IsSupported && AdvSimd.Arm64.IsSupported && sizeof(T) == 1) ||
                PopCountOperator<T>.Vectorizable; // http://0x80.pl/notesen/2023-01-31-avx512-bsf.html#trailing-zeros-simplified

            public static T Invoke(T x) => T.TrailingZeroCount(x);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (AdvSimd.IsSupported && sizeof(T) == 1)
                {
                    return AdvSimd.LeadingZeroCount(AdvSimd.Arm64.ReverseElementBits(x.AsByte())).As<byte, T>();
                }

                if (PopCountOperator<T>.Vectorizable)
                {
                    return PopCountOperator<T>.Invoke(~x & (x - Vector128<T>.One));
                }

                throw new NotSupportedException();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (PopCountOperator<T>.Vectorizable)
                {
                    PopCountOperator<T>.Invoke(~x & (x - Vector256<T>.One));
                }

                return Vector256.Create(Invoke(x.GetLower()), Invoke(x.GetUpper()));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (PopCountOperator<T>.Vectorizable)
                {
                    PopCountOperator<T>.Invoke(~x & (x - Vector512<T>.One));
                }

                return Vector512.Create(Invoke(x.GetLower()), Invoke(x.GetUpper()));
            }
        }
    }
}
