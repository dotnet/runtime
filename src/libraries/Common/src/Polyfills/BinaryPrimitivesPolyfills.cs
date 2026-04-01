// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Binary;

/// <summary>Provides downlevel polyfills for static methods on <see cref="BinaryPrimitives"/>.</summary>
internal static class BinaryPrimitivesPolyfills
{
    extension(BinaryPrimitives)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadHalfBigEndian(ReadOnlySpan<byte> source)
        {
            return BitConverter.IsLittleEndian
                ? BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<ushort>(source))
                : MemoryMarshal.Read<ushort>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalfBigEndian(Span<byte> destination, ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ushort tmp = BinaryPrimitives.ReverseEndianness(value);
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingleBigEndian(ReadOnlySpan<byte> source)
        {
            return BitConverter.IsLittleEndian
                ? BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(source)))
                : MemoryMarshal.Read<float>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleBigEndian(Span<byte> destination, float value)
        {
            if (BitConverter.IsLittleEndian)
            {
                int tmp = BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(value));
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDoubleBigEndian(ReadOnlySpan<byte> source)
        {
            return BitConverter.IsLittleEndian
                ? BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(source)))
                : MemoryMarshal.Read<double>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleBigEndian(Span<byte> destination, double value)
        {
            if (BitConverter.IsLittleEndian)
            {
                long tmp = BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToInt64Bits(value));
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }
    }
}
