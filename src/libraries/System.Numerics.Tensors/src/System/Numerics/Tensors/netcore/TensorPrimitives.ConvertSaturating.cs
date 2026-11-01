// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.CreateSaturating(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertSaturating<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (TryConvertUniversal(source, destination) || TryConvertFloatingPointToInteger(source, destination))
            {
                return;
            }

            if (typeof(TFrom) == typeof(short) && typeof(TTo) == typeof(sbyte))
            {
                InvokeSpanIntoSpan_2to1<short, sbyte, NarrowInt16ToSByteSaturatingOperator>(Rename<TFrom, short>(source), Rename<TTo, sbyte>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(ushort) && typeof(TTo) == typeof(byte))
            {
                InvokeSpanIntoSpan_2to1<ushort, byte, NarrowUInt16ToByteSaturatingOperator>(Rename<TFrom, ushort>(source), Rename<TTo, byte>(destination));
                return;
            }

            if (IsInt32Like<TFrom>() && typeof(TTo) == typeof(short))
            {
                InvokeSpanIntoSpan_2to1<int, short, NarrowInt32ToInt16SaturatingOperator>(Rename<TFrom, int>(source), Rename<TTo, short>(destination));
                return;
            }

            if (IsUInt32Like<TFrom>() && typeof(TTo) == typeof(ushort))
            {
                InvokeSpanIntoSpan_2to1<uint, ushort, NarrowUInt32ToUInt16SaturatingOperator>(Rename<TFrom, uint>(source), Rename<TTo, ushort>(destination));
                return;
            }

            if (IsInt64Like<TFrom>() && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_2to1<long, int, NarrowInt64ToInt32SaturatingOperator>(Rename<TFrom, long>(source), Rename<TTo, int>(destination));
                return;
            }

            if (IsUInt64Like<TFrom>() && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_2to1<ulong, uint, NarrowUInt64ToUInt32SaturatingOperator>(Rename<TFrom, ulong>(source), Rename<TTo, uint>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(sbyte) && typeof(TTo) == typeof(byte))
            {
                InvokeSpanIntoSpan<sbyte, byte, ConvertSaturatingSignedToUnsignedOperator<sbyte, byte>>(Rename<TFrom, sbyte>(source), Rename<TTo, byte>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(byte) && typeof(TTo) == typeof(sbyte))
            {
                InvokeSpanIntoSpan<byte, sbyte, ConvertSaturatingUnsignedToSignedOperator<byte, sbyte>>(Rename<TFrom, byte>(source), Rename<TTo, sbyte>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(short) && typeof(TTo) == typeof(ushort))
            {
                InvokeSpanIntoSpan<short, ushort, ConvertSaturatingSignedToUnsignedOperator<short, ushort>>(Rename<TFrom, short>(source), Rename<TTo, ushort>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(ushort) && typeof(TTo) == typeof(short))
            {
                InvokeSpanIntoSpan<ushort, short, ConvertSaturatingUnsignedToSignedOperator<ushort, short>>(Rename<TFrom, ushort>(source), Rename<TTo, short>(destination));
                return;
            }

            if (IsInt32Like<TFrom>() && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan<int, uint, ConvertSaturatingSignedToUnsignedOperator<int, uint>>(Rename<TFrom, int>(source), Rename<TTo, uint>(destination));
                return;
            }

            if (IsUInt32Like<TFrom>() && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan<uint, int, ConvertSaturatingUnsignedToSignedOperator<uint, int>>(Rename<TFrom, uint>(source), Rename<TTo, int>(destination));
                return;
            }

            if (IsInt64Like<TFrom>() && IsUInt64Like<TTo>())
            {
                InvokeSpanIntoSpan<long, ulong, ConvertSaturatingSignedToUnsignedOperator<long, ulong>>(Rename<TFrom, long>(source), Rename<TTo, ulong>(destination));
                return;
            }

            if (IsUInt64Like<TFrom>() && IsInt64Like<TTo>())
            {
                InvokeSpanIntoSpan<ulong, long, ConvertSaturatingUnsignedToSignedOperator<ulong, long>>(Rename<TFrom, ulong>(source), Rename<TTo, long>(destination));
                return;
            }

            InvokeSpanIntoSpan<TFrom, TTo, ConvertSaturatingFallbackOperator<TFrom, TTo>>(source, destination);
        }

        /// <summary>Converts signed integers to unsigned integers of the same size, with saturation.</summary>
        private readonly struct ConvertSaturatingSignedToUnsignedOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo>
            where TFrom : INumber<TFrom>
            where TTo : INumberBase<TTo>
        {
            public static bool Vectorizable => true;

            public static TTo Invoke(TFrom x) => TTo.CreateSaturating(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => Vector128.Max(x, Vector128<TFrom>.Zero).As<TFrom, TTo>();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => Vector256.Max(x, Vector256<TFrom>.Zero).As<TFrom, TTo>();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => Vector512.Max(x, Vector512<TFrom>.Zero).As<TFrom, TTo>();
        }

        /// <summary>Converts unsigned integers to signed integers of the same size, with saturation.</summary>
        private readonly struct ConvertSaturatingUnsignedToSignedOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo>
            where TFrom : INumber<TFrom>
            where TTo : INumberBase<TTo>, IMinMaxValue<TTo>
        {
            public static bool Vectorizable => true;

            public static TTo Invoke(TFrom x) => TTo.CreateSaturating(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => Vector128.Min(x, Vector128.Create(TFrom.CreateChecked(TTo.MaxValue))).As<TFrom, TTo>();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => Vector256.Min(x, Vector256.Create(TFrom.CreateChecked(TTo.MaxValue))).As<TFrom, TTo>();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => Vector512.Min(x, Vector512.Create(TFrom.CreateChecked(TTo.MaxValue))).As<TFrom, TTo>();
        }

        /// <summary>short.CreateSaturating(x)</summary>
        private readonly struct NarrowInt32ToInt16SaturatingOperator : IUnaryTwoToOneOperator<int, short>
        {
            public static bool Vectorizable => true;

            public static short Invoke(int x) => short.CreateSaturating(x);
            public static Vector128<short> Invoke(Vector128<int> lower, Vector128<int> upper) => Vector128.NarrowWithSaturation(lower, upper);
            public static Vector256<short> Invoke(Vector256<int> lower, Vector256<int> upper) => Vector256.NarrowWithSaturation(lower, upper);
            public static Vector512<short> Invoke(Vector512<int> lower, Vector512<int> upper) => Vector512.NarrowWithSaturation(lower, upper);
        }

        /// <summary>sbyte.CreateSaturating(x)</summary>
        private readonly struct NarrowInt16ToSByteSaturatingOperator : IUnaryTwoToOneOperator<short, sbyte>
        {
            public static bool Vectorizable => true;

            public static sbyte Invoke(short x) => sbyte.CreateSaturating(x);
            public static Vector128<sbyte> Invoke(Vector128<short> lower, Vector128<short> upper) => Vector128.NarrowWithSaturation(lower, upper);
            public static Vector256<sbyte> Invoke(Vector256<short> lower, Vector256<short> upper) => Vector256.NarrowWithSaturation(lower, upper);
            public static Vector512<sbyte> Invoke(Vector512<short> lower, Vector512<short> upper) => Vector512.NarrowWithSaturation(lower, upper);
        }

        /// <summary>int.CreateSaturating(x)</summary>
        private readonly struct NarrowInt64ToInt32SaturatingOperator : IUnaryTwoToOneOperator<long, int>
        {
            public static bool Vectorizable => true;

            public static int Invoke(long x) => int.CreateSaturating(x);
            public static Vector128<int> Invoke(Vector128<long> lower, Vector128<long> upper) => Vector128.NarrowWithSaturation(lower, upper);
            public static Vector256<int> Invoke(Vector256<long> lower, Vector256<long> upper) => Vector256.NarrowWithSaturation(lower, upper);
            public static Vector512<int> Invoke(Vector512<long> lower, Vector512<long> upper) => Vector512.NarrowWithSaturation(lower, upper);
        }

        /// <summary>byte.CreateSaturating(x)</summary>
        private readonly struct NarrowUInt16ToByteSaturatingOperator : IUnaryTwoToOneOperator<ushort, byte>
        {
            public static bool Vectorizable => true;

            public static byte Invoke(ushort x) => byte.CreateSaturating(x);
            public static Vector128<byte> Invoke(Vector128<ushort> lower, Vector128<ushort> upper) => Vector128.NarrowWithSaturation(lower, upper);
            public static Vector256<byte> Invoke(Vector256<ushort> lower, Vector256<ushort> upper) => Vector256.NarrowWithSaturation(lower, upper);
            public static Vector512<byte> Invoke(Vector512<ushort> lower, Vector512<ushort> upper) => Vector512.NarrowWithSaturation(lower, upper);
        }

        /// <summary>ushort.CreateSaturating(x)</summary>
        private readonly struct NarrowUInt32ToUInt16SaturatingOperator : IUnaryTwoToOneOperator<uint, ushort>
        {
            public static bool Vectorizable => true;

            public static ushort Invoke(uint x) => ushort.CreateSaturating(x);
            public static Vector128<ushort> Invoke(Vector128<uint> lower, Vector128<uint> upper) => Vector128.NarrowWithSaturation(lower, upper);
            public static Vector256<ushort> Invoke(Vector256<uint> lower, Vector256<uint> upper) => Vector256.NarrowWithSaturation(lower, upper);
            public static Vector512<ushort> Invoke(Vector512<uint> lower, Vector512<uint> upper) => Vector512.NarrowWithSaturation(lower, upper);
        }

        /// <summary>uint.CreateSaturating(x)</summary>
        private readonly struct NarrowUInt64ToUInt32SaturatingOperator : IUnaryTwoToOneOperator<ulong, uint>
        {
            public static bool Vectorizable => true;

            public static uint Invoke(ulong x) => uint.CreateSaturating(x);
            public static Vector128<uint> Invoke(Vector128<ulong> lower, Vector128<ulong> upper) => Vector128.NarrowWithSaturation(lower, upper);
            public static Vector256<uint> Invoke(Vector256<ulong> lower, Vector256<ulong> upper) => Vector256.NarrowWithSaturation(lower, upper);
            public static Vector512<uint> Invoke(Vector512<ulong> lower, Vector512<ulong> upper) => Vector512.NarrowWithSaturation(lower, upper);
        }

        /// <summary>T.CreateSaturating(x)</summary>
        internal readonly struct ConvertSaturatingFallbackOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo> where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        {
            public static bool Vectorizable => false;

            public static TTo Invoke(TFrom x) => TTo.CreateSaturating(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => throw new NotSupportedException();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => throw new NotSupportedException();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => throw new NotSupportedException();
        }
    }
}
