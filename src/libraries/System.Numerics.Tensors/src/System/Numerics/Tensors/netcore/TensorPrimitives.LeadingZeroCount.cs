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
                (Avx512CD.VL.IsSupported && (sizeof(T) == 2 || sizeof(T) == 4 || sizeof(T) == 8)) ||
                (Avx512Vbmi.VL.IsSupported && sizeof(T) == 1) ||
                (AdvSimd.IsSupported && (sizeof(T) == 1 || sizeof(T) == 2 || sizeof(T) == 4));

            public static T Invoke(T x) => T.LeadingZeroCount(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (Avx512Vbmi.VL.IsSupported && sizeof(T) == 1)
                {
                    Vector128<byte> lookupVectorLow = Vector128.Create((byte)8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4);
                    Vector128<byte> lookupVectorHigh = Vector128.Create((byte)3, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    Vector128<byte> nibbleMask = Vector128.Create<byte>(0xF);
                    Vector128<byte> permuteMask = Vector128.Create<byte>(0x80);
                    Vector128<byte> lowNibble = x.AsByte() & nibbleMask;
                    Vector128<byte> highNibble = Sse2.ShiftRightLogical(x.AsInt32(), 4).AsByte() & nibbleMask;
                    Vector128<byte> nibbleSelectMask = Sse2.CompareEqual(highNibble, Vector128<byte>.Zero);
                    Vector128<byte> indexVector = Sse41.BlendVariable(highNibble, lowNibble, nibbleSelectMask) +
                        (~nibbleSelectMask & nibbleMask);
                    indexVector |= ~nibbleSelectMask & permuteMask;
                    return Avx512Vbmi.VL.PermuteVar16x8x2(lookupVectorLow, indexVector, lookupVectorHigh).As<byte, T>();
                }

                if (Avx512CD.VL.IsSupported)
                {
                    if (sizeof(T) == 2)
                    {
                        Vector128<uint> lowHalf = Vector128.Create((uint)0x0000FFFF);
                        Vector128<uint> x_bot16 = Sse2.Or(Sse2.ShiftLeftLogical(x.AsUInt32(), 16), lowHalf);
                        Vector128<uint> x_top16 = Sse2.Or(x.AsUInt32(), lowHalf);
                        Vector128<uint> lz_bot16 = Avx512CD.VL.LeadingZeroCount(x_bot16);
                        Vector128<uint> lz_top16 = Avx512CD.VL.LeadingZeroCount(x_top16);
                        Vector128<uint> lz_top16_shift = Sse2.ShiftLeftLogical(lz_top16, 16);
                        return Sse2.Or(lz_bot16, lz_top16_shift).AsUInt16().As<ushort, T>();
                    }

                    if (sizeof(T) == 4)
                    {
                        return Avx512CD.VL.LeadingZeroCount(x.AsUInt32()).As<uint, T>();
                    }

                    if (sizeof(T) == 8)
                    {
                        return Avx512CD.VL.LeadingZeroCount(x.AsUInt64()).As<ulong, T>();
                    }
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
                if (Avx512Vbmi.VL.IsSupported && sizeof(T) == 1)
                {
                    Vector256<byte> lookupVector =
                        Vector256.Create((byte)8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4,
                                               3, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    Vector256<byte> nibbleMask = Vector256.Create<byte>(0xF);
                    Vector256<byte> lowNibble = x.AsByte() & nibbleMask;
                    Vector256<byte> highNibble = Avx2.ShiftRightLogical(x.AsInt32(), 4).AsByte() & nibbleMask;
                    Vector256<byte> nibbleSelectMask = Avx2.CompareEqual(highNibble, Vector256<byte>.Zero);
                    Vector256<byte> indexVector = Avx2.BlendVariable(highNibble, lowNibble, nibbleSelectMask) +
                        (~nibbleSelectMask & nibbleMask);
                    return Avx512Vbmi.VL.PermuteVar32x8(lookupVector, indexVector).As<byte, T>();
                }

                if (Avx512CD.VL.IsSupported)
                {
                    if (sizeof(T) == 2)
                    {
                        Vector256<uint> lowHalf = Vector256.Create((uint)0x0000FFFF);
                        Vector256<uint> x_bot16 = Avx2.Or(Avx2.ShiftLeftLogical(x.AsUInt32(), 16), lowHalf);
                        Vector256<uint> x_top16 = Avx2.Or(x.AsUInt32(), lowHalf);
                        Vector256<uint> lz_bot16 = Avx512CD.VL.LeadingZeroCount(x_bot16);
                        Vector256<uint> lz_top16 = Avx512CD.VL.LeadingZeroCount(x_top16);
                        Vector256<uint> lz_top16_shift = Avx2.ShiftLeftLogical(lz_top16, 16);
                        return Avx2.Or(lz_bot16, lz_top16_shift).AsUInt16().As<ushort, T>();
                    }

                    if (sizeof(T) == 4)
                    {
                        return Avx512CD.VL.LeadingZeroCount(x.AsUInt32()).As<uint, T>();
                    }

                    if (sizeof(T) == 8)
                    {
                        return Avx512CD.VL.LeadingZeroCount(x.AsUInt64()).As<ulong, T>();
                    }
                }

                return Vector256.Create(Invoke(x.GetLower()), Invoke(x.GetUpper()));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (Avx512BW.IsSupported && Avx512Vbmi.IsSupported && sizeof(T) == 1)
                {
                    // Use each element of x as an index into a lookup table.
                    // Lookup can be broken down into the following:
                    //      Bit 7 is set -- Result is 0, else result is from lookup table
                    //      Bit 6 is set -- Use lookupVectorB, else use lookupVectorA
                    //      Bit 5:0      -- Index to use for lookup table
                    Vector512<byte> lookupVectorA =
                        Vector512.Create((byte)8, 7, 6, 6, 5, 5, 5, 5,
                                               4, 4, 4, 4, 4, 4, 4, 4,
                                               3, 3, 3, 3, 3, 3, 3, 3,
                                               3, 3, 3, 3, 3, 3, 3, 3,
                                               2, 2, 2, 2, 2, 2, 2, 2,
                                               2, 2, 2, 2, 2, 2, 2, 2,
                                               2, 2, 2, 2, 2, 2, 2, 2,
                                               2, 2, 2, 2, 2, 2, 2, 2);
                    Vector512<byte> lookupVectorB = Vector512.Create((byte)1);
                    Vector512<byte> bit7ZeroMask = Avx512BW.CompareLessThan(x.AsByte(), Vector512.Create((byte)128));
                    return Avx512F.And(bit7ZeroMask, Avx512Vbmi.PermuteVar64x8x2(lookupVectorA, x.AsByte(), lookupVectorB)).As<byte, T>();
                }

                if (Avx512CD.IsSupported)
                {
                    if (sizeof(T) == 2)
                    {
                        Vector512<uint> lowHalf = Vector512.Create((uint)0x0000FFFF);
                        Vector512<uint> x_bot16 = Avx512F.Or(Avx512F.ShiftLeftLogical(x.AsUInt32(), 16), lowHalf);
                        Vector512<uint> x_top16 = Avx512F.Or(x.AsUInt32(), lowHalf);
                        Vector512<uint> lz_bot16 = Avx512CD.LeadingZeroCount(x_bot16);
                        Vector512<uint> lz_top16 = Avx512CD.LeadingZeroCount(x_top16);
                        Vector512<uint> lz_top16_shift = Avx512F.ShiftLeftLogical(lz_top16, 16);
                        return Avx512F.Or(lz_bot16, lz_top16_shift).AsUInt16().As<ushort, T>();
                    }

                    if (sizeof(T) == 4)
                    {
                        return Avx512CD.LeadingZeroCount(x.AsUInt32()).As<uint, T>();
                    }

                    if (sizeof(T) == 8)
                    {
                        return Avx512CD.LeadingZeroCount(x.AsUInt64()).As<ulong, T>();
                    }
                }

                return Vector512.Create(Invoke(x.GetLower()), Invoke(x.GetUpper()));
            }
        }
    }
}
