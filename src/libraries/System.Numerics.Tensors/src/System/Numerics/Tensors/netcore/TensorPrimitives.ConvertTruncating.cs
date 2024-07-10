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
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.CreateTruncating(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertTruncating<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (TryConvertUniversal(source, destination))
            {
                return;
            }

            if (((typeof(TFrom) == typeof(byte) || typeof(TFrom) == typeof(sbyte)) && (typeof(TTo) == typeof(byte) || typeof(TTo) == typeof(sbyte))) ||
                ((typeof(TFrom) == typeof(ushort) || typeof(TFrom) == typeof(short)) && (typeof(TTo) == typeof(ushort) || typeof(TTo) == typeof(short))) ||
                ((IsUInt32Like<TFrom>() || IsInt32Like<TFrom>()) && (IsUInt32Like<TTo>() || IsInt32Like<TTo>())) ||
                ((IsUInt64Like<TFrom>() || IsInt64Like<TFrom>()) && (IsUInt64Like<TTo>() || IsInt64Like<TTo>())))
            {
                source.CopyTo(Rename<TTo, TFrom>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(float) && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan<float, uint, ConvertSingleToUInt32>(Rename<TFrom, float>(source), Rename<TTo, uint>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(float) && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan<float, int, ConvertSingleToInt32>(Rename<TFrom, float>(source), Rename<TTo, int>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(double) && IsUInt64Like<TTo>())
            {
                InvokeSpanIntoSpan<double, ulong, ConvertDoubleToUInt64>(Rename<TFrom, double>(source), Rename<TTo, ulong>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(double) && IsInt64Like<TTo>())
            {
                InvokeSpanIntoSpan<double, long, ConvertDoubleToInt64>(Rename<TFrom, double>(source), Rename<TTo, long>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(ushort) && typeof(TTo) == typeof(byte))
            {
                InvokeSpanIntoSpan_2to1<ushort, byte, NarrowUInt16ToByteOperator>(Rename<TFrom, ushort>(source), Rename<TTo, byte>(destination));
                return;
            }

            if (typeof(TFrom) == typeof(short) && typeof(TTo) == typeof(sbyte))
            {
                InvokeSpanIntoSpan_2to1<short, sbyte, NarrowInt16ToSByteOperator>(Rename<TFrom, short>(source), Rename<TTo, sbyte>(destination));
                return;
            }

            if (IsUInt32Like<TFrom>() && typeof(TTo) == typeof(ushort))
            {
                InvokeSpanIntoSpan_2to1<uint, ushort, NarrowUInt32ToUInt16Operator>(Rename<TFrom, uint>(source), Rename<TTo, ushort>(destination));
                return;
            }

            if (IsInt32Like<TFrom>() && typeof(TTo) == typeof(short))
            {
                InvokeSpanIntoSpan_2to1<int, short, NarrowInt32ToInt16Operator>(Rename<TFrom, int>(source), Rename<TTo, short>(destination));
                return;
            }

            if (IsUInt64Like<TFrom>() && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_2to1<ulong, uint, NarrowUInt64ToUInt32Operator>(Rename<TFrom, ulong>(source), Rename<TTo, uint>(destination));
                return;
            }

            if (IsInt64Like<TFrom>() && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_2to1<long, int, NarrowInt64ToInt32Operator>(Rename<TFrom, long>(source), Rename<TTo, int>(destination));
                return;
            }

            InvokeSpanIntoSpan<TFrom, TTo, ConvertTruncatingFallbackOperator<TFrom, TTo>>(source, destination);
        }

        /// <summary>(float)int</summary>
        private readonly struct ConvertSingleToInt32 : IUnaryOperator<float, int>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static int Invoke(float x) => int.CreateTruncating(x);
            public static Vector128<int> Invoke(Vector128<float> x) => Vector128.ConvertToInt32(x);
            public static Vector256<int> Invoke(Vector256<float> x) => Vector256.ConvertToInt32(x);
            public static Vector512<int> Invoke(Vector512<float> x) => Vector512.ConvertToInt32(x);
        }

        /// <summary>(float)uint</summary>
        private readonly struct ConvertSingleToUInt32 : IUnaryOperator<float, uint>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static uint Invoke(float x) => uint.CreateTruncating(x);
            public static Vector128<uint> Invoke(Vector128<float> x) => Vector128.ConvertToUInt32(x);
            public static Vector256<uint> Invoke(Vector256<float> x) => Vector256.ConvertToUInt32(x);
            public static Vector512<uint> Invoke(Vector512<float> x) => Vector512.ConvertToUInt32(x);
        }

        /// <summary>(ulong)double</summary>
        private readonly struct ConvertDoubleToUInt64 : IUnaryOperator<double, ulong>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static ulong Invoke(double x) => ulong.CreateTruncating(x);
            public static Vector128<ulong> Invoke(Vector128<double> x) => Vector128.ConvertToUInt64(x);
            public static Vector256<ulong> Invoke(Vector256<double> x) => Vector256.ConvertToUInt64(x);
            public static Vector512<ulong> Invoke(Vector512<double> x) => Vector512.ConvertToUInt64(x);
        }

        /// <summary>(long)double</summary>
        private readonly struct ConvertDoubleToInt64 : IUnaryOperator<double, long>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static long Invoke(double x) => long.CreateTruncating(x);
            public static Vector128<long> Invoke(Vector128<double> x) => Vector128.ConvertToInt64(x);
            public static Vector256<long> Invoke(Vector256<double> x) => Vector256.ConvertToInt64(x);
            public static Vector512<long> Invoke(Vector512<double> x) => Vector512.ConvertToInt64(x);
        }

        /// <summary>(byte)ushort</summary>
        private readonly struct NarrowUInt16ToByteOperator : IUnaryTwoToOneOperator<ushort, byte>
        {
            public static bool Vectorizable => true;

            public static byte Invoke(ushort x) => (byte)x;
            public static Vector128<byte> Invoke(Vector128<ushort> lower, Vector128<ushort> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<byte> Invoke(Vector256<ushort> lower, Vector256<ushort> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<byte> Invoke(Vector512<ushort> lower, Vector512<ushort> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(sbyte)short</summary>
        private readonly struct NarrowInt16ToSByteOperator : IUnaryTwoToOneOperator<short, sbyte>
        {
            public static bool Vectorizable => true;

            public static sbyte Invoke(short x) => (sbyte)x;
            public static Vector128<sbyte> Invoke(Vector128<short> lower, Vector128<short> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<sbyte> Invoke(Vector256<short> lower, Vector256<short> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<sbyte> Invoke(Vector512<short> lower, Vector512<short> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(ushort)uint</summary>
        private readonly struct NarrowUInt32ToUInt16Operator : IUnaryTwoToOneOperator<uint, ushort>
        {
            public static bool Vectorizable => true;

            public static ushort Invoke(uint x) => (ushort)x;
            public static Vector128<ushort> Invoke(Vector128<uint> lower, Vector128<uint> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<ushort> Invoke(Vector256<uint> lower, Vector256<uint> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<ushort> Invoke(Vector512<uint> lower, Vector512<uint> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(short)int</summary>
        private readonly struct NarrowInt32ToInt16Operator : IUnaryTwoToOneOperator<int, short>
        {
            public static bool Vectorizable => true;

            public static short Invoke(int x) => (short)x;
            public static Vector128<short> Invoke(Vector128<int> lower, Vector128<int> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<short> Invoke(Vector256<int> lower, Vector256<int> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<short> Invoke(Vector512<int> lower, Vector512<int> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(uint)ulong</summary>
        private readonly struct NarrowUInt64ToUInt32Operator : IUnaryTwoToOneOperator<ulong, uint>
        {
            public static bool Vectorizable => true;

            public static uint Invoke(ulong x) => (uint)x;
            public static Vector128<uint> Invoke(Vector128<ulong> lower, Vector128<ulong> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<uint> Invoke(Vector256<ulong> lower, Vector256<ulong> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<uint> Invoke(Vector512<ulong> lower, Vector512<ulong> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(int)long</summary>
        private readonly struct NarrowInt64ToInt32Operator : IUnaryTwoToOneOperator<long, int>
        {
            public static bool Vectorizable => true;

            public static int Invoke(long x) => (int)x;
            public static Vector128<int> Invoke(Vector128<long> lower, Vector128<long> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<int> Invoke(Vector256<long> lower, Vector256<long> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<int> Invoke(Vector512<long> lower, Vector512<long> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>T.CreateTruncating(x)</summary>
        private readonly struct ConvertTruncatingFallbackOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo> where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        {
            public static bool Vectorizable => false;

            public static TTo Invoke(TFrom x) => TTo.CreateTruncating(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => throw new NotSupportedException();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => throw new NotSupportedException();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => throw new NotSupportedException();
        }
    }
}
