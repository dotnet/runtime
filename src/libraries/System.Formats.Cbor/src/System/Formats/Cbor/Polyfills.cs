// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Formats.Cbor
{
    internal static class DateTimeOffsetPolyfills
    {
        private const long UnixEpochTicks = 719162L * 10000 * 1000 * 60 * 60 * 24;

        extension(DateTimeOffset)
        {
            public static DateTimeOffset UnixEpoch => new DateTimeOffset(UnixEpochTicks, TimeSpan.Zero);
        }
    }

    internal static class BigIntegerPolyfills
    {
        extension(BigInteger self)
        {
            public byte[] ToByteArray(bool isUnsigned, bool isBigEndian)
            {
                byte[] littleEndianBytes = self.ToByteArray();

                if (littleEndianBytes.Length == 1)
                    return littleEndianBytes;

                Span<byte> bytesAsSpan = littleEndianBytes;

                if (isBigEndian)
                    bytesAsSpan.Reverse();

                if (isUnsigned)
                {
                    int start = 0;
                    for (int i = 0; i < bytesAsSpan.Length; i++)
                    {
                        if (bytesAsSpan[i] == 0x00)
                            start++;
                        else
                            break;
                    }

                    if (start > 0)
                        return bytesAsSpan.Slice(start).ToArray();
                }

                return littleEndianBytes;
            }
        }
    }

    internal static class DecimalPolyfills
    {
        extension(decimal)
        {
            public static void GetBits(decimal d, Span<int> destination)
            {
                decimal.GetBits(d).CopyTo(destination);
            }
        }
    }

    internal static class StringPolyfills
    {
        internal delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

        extension(string)
        {
            public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
            {
                char[] arr = new char[length];
                action(arr, state);
                return new string(arr);
            }
        }
    }

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

    internal static class BitConverterPolyfills
    {
        extension(BitConverter)
        {
            public static unsafe int SingleToInt32Bits(float value)
                => *(int*)&value;

            public static unsafe float Int32BitsToSingle(int value)
                => *(float*)&value;

            public static uint SingleToUInt32Bits(float value)
                => (uint)BitConverter.SingleToInt32Bits(value);

            public static float UInt32BitsToSingle(uint value)
                => BitConverter.Int32BitsToSingle((int)value);
        }
    }

    internal static class StackPolyfills
    {
        extension<T>(Stack<T> stack)
        {
            public bool TryPop([MaybeNullWhen(false)] out T result)
            {
                if (stack.Count > 0)
                {
                    result = stack.Pop();
                    return true;
                }

                result = default;
                return false;
            }
        }
    }
}
