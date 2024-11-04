// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

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
                (sizeof(T) == 1 && (Avx512BW.IsSupported && Avx512Vbmi.VL.IsSupported)) ||
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
                if (Avx512Vbmi.VL.IsSupported && sizeof(T) == 1)
                {
                    Vector128<byte> nibbleMask = Vector128.Create<byte>(0xF);
                    Vector128<byte> permuteMask = Vector128.Create<byte>(0x80);
                    Vector128<byte> lookupVectorLow =
                        Vector128.Create((byte)8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4);
                    Vector128<byte> lookupVectorHigh =
                        Vector128.Create((byte)3, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    Vector128<byte> lowNibble = x.AsByte() & nibbleMask;
                    Vector128<byte> highNibble = Sse2.ShiftRightLogical(x.AsInt32(), 4).AsByte() & nibbleMask;
                    Vector128<byte> byteSelectMask = Sse2.CompareEqual(highNibble, Vector128<byte>.Zero);
                    Vector128<byte> indexVector = Sse41.BlendVariable(highNibble, lowNibble, byteSelectMask);
                    indexVector += (~byteSelectMask & nibbleMask);
                    indexVector |= (~byteSelectMask & permuteMask);
                    return Avx512Vbmi.VL.PermuteVar16x8x2(lookupVectorLow, indexVector, lookupVectorHigh).As<byte, T>();
                }

                Debug.Assert(AdvSimd.IsSupported);
                {
                    if (sizeof(T) == 1) return AdvSimd.LeadingZeroCount(x.AsByte()).As<byte, T>();
                    if (sizeof(T) == 2) return AdvSimd.LeadingZeroCount(x.AsUInt16()).As<ushort, T>();

                    Debug.Assert(sizeof(T) == 4);
                    return AdvSimd.LeadingZeroCount(x.AsUInt32()).As<uint, T>();
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
                if (Avx512Vbmi.VL.IsSupported && sizeof(T) == 1)
                {
                    Vector256<byte> nibbleMask = Vector256.Create<byte>(0xF);
                    Vector256<byte> lookupVector =
                        Vector256.Create((byte)8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4,
                                               3, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    Vector256<byte> lowNibble = x.AsByte() & nibbleMask;
                    Vector256<byte> highNibble = Avx2.ShiftRightLogical(x.AsInt32(), 4).AsByte() & nibbleMask;
                    Vector256<byte> byteSelectMask = Avx2.CompareEqual(highNibble, Vector256<byte>.Zero);
                    Vector256<byte> indexVector = Avx2.BlendVariable(highNibble, lowNibble, byteSelectMask);
                    indexVector += (~byteSelectMask & nibbleMask);
                    return Avx512Vbmi.VL.PermuteVar32x8(lookupVector, indexVector).As<byte, T>();
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
                if (Avx512BW.IsSupported && Avx512Vbmi.IsSupported && sizeof(T) == 1)
                {
                    Vector512<byte> nibbleMask = Vector512.Create<byte>(0xF);
                    Vector512<byte> lookupVector =
                        Vector512.Create((byte)8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4,
                                               3, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                               0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                               0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    Vector512<byte> lowNibble = x.AsByte() & nibbleMask;
                    Vector512<byte> highNibble = Avx512F.ShiftRightLogical(x.AsInt32(), 4).AsByte() & nibbleMask;
                    Vector512<byte> byteSelect = Avx512BW.CompareEqual(highNibble, Vector512<byte>.Zero);
                    Vector512<byte> indexVector = Avx512BW.BlendVariable(highNibble, lowNibble, byteSelect);
                    indexVector += (~byteSelect & nibbleMask);
                    return Avx512Vbmi.PermuteVar64x8(lookupVector, indexVector).As<byte, T>();
                }

                return Vector512.Create(Invoke(x.GetLower()), Invoke(x.GetUpper()));
            }
        }
    }
}
