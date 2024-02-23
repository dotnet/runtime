// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise leading zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.LeadingZeroCount(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void LeadingZeroCount<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan<T, LeadingZeroCountOperator<T>>(x, destination);

        /// <summary>T.LeadingZeroCount(x)</summary>
        internal readonly unsafe struct LeadingZeroCountOperator<T> : IUnaryOperator<T, T> where T : IBinaryInteger<T>
        {
            public static bool Vectorizable =>
                (Avx512CD.VL.IsSupported && (sizeof(T) == 4 || sizeof(T) == 8)) ||
                (AdvSimd.IsSupported && (sizeof(T) == 1 || sizeof(T) == 2 || sizeof(T) == 4));

            public static T Invoke(T x) => T.LeadingZeroCount(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (Avx512CD.VL.IsSupported)
                {
                    if (sizeof(T) == 4) return Avx512CD.VL.LeadingZeroCount(x.AsUInt32()).As<uint, T>();
                    if (sizeof(T) == 8) return Avx512CD.VL.LeadingZeroCount(x.AsUInt64()).As<ulong, T>();
                }

                Debug.Assert(AdvSimd.IsSupported);
                {
                    if (sizeof(T) == 1) return AdvSimd.LeadingZeroCount(x.AsByte()).As<byte, T>();
                    if (sizeof(T) == 2) return AdvSimd.LeadingZeroCount(x.AsUInt16()).As<ushort, T>();
                    if (sizeof(T) == 4) return AdvSimd.LeadingZeroCount(x.AsUInt32()).As<uint, T>();

                    throw new NotSupportedException();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (Avx512CD.VL.IsSupported)
                {
                    if (sizeof(T) == 4) return Avx512CD.VL.LeadingZeroCount(x.AsUInt32()).As<uint, T>();
                    if (sizeof(T) == 8) return Avx512CD.VL.LeadingZeroCount(x.AsUInt64()).As<ulong, T>();
                }

                return Vector256.Create(Invoke(x.GetLower()), Invoke(x.GetUpper()));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (Avx512CD.IsSupported)
                {
                    if (sizeof(T) == 4) return Avx512CD.LeadingZeroCount(x.AsUInt32()).As<uint, T>();
                    if (sizeof(T) == 8) return Avx512CD.LeadingZeroCount(x.AsUInt64()).As<ulong, T>();
                }

                return Vector512.Create(Invoke(x.GetLower()), Invoke(x.GetUpper()));
            }
        }
    }
}
