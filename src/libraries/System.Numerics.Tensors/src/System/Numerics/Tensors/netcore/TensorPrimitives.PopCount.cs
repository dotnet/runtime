// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.PopCount(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void PopCount<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan<T, PopCountOperator<T>>(x, destination);

        /// <summary>T.PopCount(x)</summary>
        private readonly unsafe struct PopCountOperator<T> : IUnaryOperator<T, T> where T : IBinaryInteger<T>
        {
            // TODO https://github.com/dotnet/runtime/issues/96162: Use AVX512 popcount operations when available

            public static bool Vectorizable =>
                // The implementation uses a vectorized version of the BitOperations.PopCount software fallback:
                // https://github.com/dotnet/runtime/blob/aff061bab1b6d9ccd5731bd16fa8e89ad82ab75a/src/libraries/System.Private.CoreLib/src/System/Numerics/BitOperations.cs#L496-L508
                // This relies on 64-bit shifts for sizeof(T) == 8, and such shifts aren't accelerated on today's hardware.
                // Alternative approaches, such as doing two 32-bit operations and combining them were observed to not
                // provide any meaningfuls speedup over scalar. So for now, we don't vectorize when sizeof(T) == 8.
                sizeof(T) == 1 || sizeof(T) == 2 || sizeof(T) == 4;

            public static T Invoke(T x) => T.PopCount(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (sizeof(T) == 1)
                {
                    if (AdvSimd.IsSupported)
                    {
                        return AdvSimd.PopCount(x.AsByte()).As<byte, T>();
                    }

                    if (PackedSimd.IsSupported)
                    {
                        return PackedSimd.PopCount(x.AsByte()).As<byte, T>();
                    }

                    Vector128<byte> c1 = Vector128.Create((byte)0x55);
                    Vector128<byte> c2 = Vector128.Create((byte)0x33);
                    Vector128<byte> c3 = Vector128.Create((byte)0x0F);

                    // We don't have a per element shuffle for byte on some platforms.
                    // However, we do currently always have a 16-bit shift available and
                    // due to how the algorithm works, we don't need to worry about
                    // any bits that shift into the lower 8-bits from the upper 8-bits.
                    Vector128<byte> tmp = x.AsByte();
                    tmp -= (x.AsUInt16() >> 1).AsByte() & c1;
                    tmp = (tmp & c2) + ((tmp.AsUInt16() >> 2).AsByte() & c2);
                    return ((tmp + (tmp.AsUInt16() >> 4).AsByte()) & c3).As<byte, T>();
                }

                if (sizeof(T) == 2)
                {
                    Vector128<ushort> c1 = Vector128.Create((ushort)0x5555);
                    Vector128<ushort> c2 = Vector128.Create((ushort)0x3333);
                    Vector128<ushort> c3 = Vector128.Create((ushort)0x0F0F);
                    Vector128<ushort> c4 = Vector128.Create((ushort)0x0101);

                    Vector128<ushort> tmp = x.AsUInt16();
                    tmp -= (tmp >> 1) & c1;
                    tmp = (tmp & c2) + ((tmp >> 2) & c2);
                    tmp = (((tmp + (tmp >> 4)) & c3) * c4) >> 8;
                    return tmp.As<ushort, T>();
                }

                Debug.Assert(sizeof(T) == 4);
                {
                    Vector128<uint> c1 = Vector128.Create(0x55555555u);
                    Vector128<uint> c2 = Vector128.Create(0x33333333u);
                    Vector128<uint> c3 = Vector128.Create(0x0F0F0F0Fu);
                    Vector128<uint> c4 = Vector128.Create(0x01010101u);

                    Vector128<uint> tmp = x.AsUInt32();
                    tmp -= (tmp >> 1) & c1;
                    tmp = (tmp & c2) + ((tmp >> 2) & c2);
                    tmp = (((tmp + (tmp >> 4)) & c3) * c4) >> 24;
                    return tmp.As<uint, T>();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (sizeof(T) == 1)
                {
                    Vector256<byte> c1 = Vector256.Create((byte)0x55);
                    Vector256<byte> c2 = Vector256.Create((byte)0x33);
                    Vector256<byte> c3 = Vector256.Create((byte)0x0F);

                    // We don't have a per element shuffle for byte on some platforms.
                    // However, we do currently always have a 16-bit shift available and
                    // due to how the algorithm works, we don't need to worry about
                    // any bits that shift into the lower 8-bits from the upper 8-bits.
                    Vector256<byte> tmp = x.AsByte();
                    tmp -= (x.AsUInt16() >> 1).AsByte() & c1;
                    tmp = (tmp & c2) + ((tmp.AsUInt16() >> 2).AsByte() & c2);
                    return ((tmp + (tmp.AsUInt16() >> 4).AsByte()) & c3).As<byte, T>();
                }

                if (sizeof(T) == 2)
                {
                    Vector256<ushort> c1 = Vector256.Create((ushort)0x5555);
                    Vector256<ushort> c2 = Vector256.Create((ushort)0x3333);
                    Vector256<ushort> c3 = Vector256.Create((ushort)0x0F0F);
                    Vector256<ushort> c4 = Vector256.Create((ushort)0x0101);

                    Vector256<ushort> tmp = x.AsUInt16();
                    tmp -= (tmp >> 1) & c1;
                    tmp = (tmp & c2) + ((tmp >> 2) & c2);
                    tmp = (((tmp + (tmp >> 4)) & c3) * c4) >> 8;
                    return tmp.As<ushort, T>();
                }

                Debug.Assert(sizeof(T) == 4);
                {
                    Vector256<uint> c1 = Vector256.Create(0x55555555u);
                    Vector256<uint> c2 = Vector256.Create(0x33333333u);
                    Vector256<uint> c3 = Vector256.Create(0x0F0F0F0Fu);
                    Vector256<uint> c4 = Vector256.Create(0x01010101u);

                    Vector256<uint> tmp = x.AsUInt32();
                    tmp -= (tmp >> 1) & c1;
                    tmp = (tmp & c2) + ((tmp >> 2) & c2);
                    tmp = (((tmp + (tmp >> 4)) & c3) * c4) >> 24;
                    return tmp.As<uint, T>();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (sizeof(T) == 1)
                {
                    Vector512<byte> c1 = Vector512.Create((byte)0x55);
                    Vector512<byte> c2 = Vector512.Create((byte)0x33);
                    Vector512<byte> c3 = Vector512.Create((byte)0x0F);

                    // We don't have a per element shuffle for byte on some platforms.
                    // However, we do currently always have a 16-bit shift available and
                    // due to how the algorithm works, we don't need to worry about
                    // any bits that shift into the lower 8-bits from the upper 8-bits.
                    Vector512<byte> tmp = x.AsByte();
                    tmp -= (x.AsUInt16() >> 1).AsByte() & c1;
                    tmp = (tmp & c2) + ((tmp.AsUInt16() >> 2).AsByte() & c2);
                    return ((tmp + (tmp.AsUInt16() >> 4).AsByte()) & c3).As<byte, T>();
                }

                if (sizeof(T) == 2)
                {
                    Vector512<ushort> c1 = Vector512.Create((ushort)0x5555);
                    Vector512<ushort> c2 = Vector512.Create((ushort)0x3333);
                    Vector512<ushort> c3 = Vector512.Create((ushort)0x0F0F);
                    Vector512<ushort> c4 = Vector512.Create((ushort)0x0101);

                    Vector512<ushort> tmp = x.AsUInt16();
                    tmp -= (tmp >> 1) & c1;
                    tmp = (tmp & c2) + ((tmp >> 2) & c2);
                    tmp = (((tmp + (tmp >> 4)) & c3) * c4) >> 8;
                    return tmp.As<ushort, T>();
                }

                Debug.Assert(sizeof(T) == 4);
                {
                    Vector512<uint> c1 = Vector512.Create(0x55555555u);
                    Vector512<uint> c2 = Vector512.Create(0x33333333u);
                    Vector512<uint> c3 = Vector512.Create(0x0F0F0F0Fu);
                    Vector512<uint> c4 = Vector512.Create(0x01010101u);

                    Vector512<uint> tmp = x.AsUInt32();
                    tmp -= (tmp >> 1) & c1;
                    tmp = (tmp & c2) + ((tmp >> 2) & c2);
                    tmp = (((tmp + (tmp >> 4)) & c3) * c4) >> 24;
                    return tmp.As<uint, T>();
                }
            }
        }
    }
}
