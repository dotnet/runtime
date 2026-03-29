// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Formats.Cbor
{
    internal static class CborHelpers
    {
#if NET
        public static readonly DateTimeOffset UnixEpoch = DateTimeOffset.UnixEpoch;
#else
        private const long UnixEpochTicks = 719162L /*Number of days from 1/1/0001 to 12/31/1969*/ * 10000 * 1000 * 60 * 60 * 24; /* Ticks per day.*/

        public static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(UnixEpochTicks, TimeSpan.Zero);
#endif

#if NET
        public static BigInteger CreateBigIntegerFromUnsignedBigEndianBytes(byte[] bytes)
            => new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
#else
        public static BigInteger CreateBigIntegerFromUnsignedBigEndianBytes(byte[] bigEndianBytes)
        {
            if (bigEndianBytes.Length == 0)
            {
                return new BigInteger(bigEndianBytes);
            }

            byte[] temp;
            if ((bigEndianBytes[0] & 0x80) != 0) // Is negative?
            {
                // To prevent positive values from being misinterpreted as negative values,
                // you can add a zero-byte value to the most significant side of the array.
                // Right in this case as it is Big-endian.
                var bytesPlusOne = new byte[bigEndianBytes.Length + 1];
                bigEndianBytes.CopyTo(bytesPlusOne.AsSpan(1));
                temp = bytesPlusOne;
            }
            else
            {
                temp = bigEndianBytes;
            }

            // Reverse endianness
            temp.AsSpan().Reverse();

            return new BigInteger(temp);
        }
#endif

#if NET
        public static byte[] CreateUnsignedBigEndianBytesFromBigInteger(BigInteger value)
            => value.ToByteArray(isUnsigned: true, isBigEndian: true);
#else
        public static byte[] CreateUnsignedBigEndianBytesFromBigInteger(BigInteger value)
        {
            byte[] littleEndianBytes = value.ToByteArray();

            if (littleEndianBytes.Length == 1)
            {
                return littleEndianBytes;
            }

            Span<byte> bytesAsSpan = littleEndianBytes;
            bytesAsSpan.Reverse();

            int start = 0;
            for (int i = 0; i < bytesAsSpan.Length; i++)
            {
                if (bytesAsSpan[i] == 0x00)
                {
                    start++;
                }
                else
                {
                    break;
                }
            }

            Debug.Assert(start <= 1); // If there is a case where we trim more than one byte, we want to add it to our tests.

            return start == 0 ? littleEndianBytes : bytesAsSpan.Slice(start).ToArray();
        }
#endif

#if NET
        public static void GetBitsFromDecimal(decimal d, Span<int> destination)
            => decimal.GetBits(d, destination);
#else
        public static void GetBitsFromDecimal(decimal d, Span<int> destination)
        {
            decimal.GetBits(d).CopyTo(destination);
        }
#endif

#if NET
        public static string BuildStringFromIndefiniteLengthTextString<TState>(int length, TState state, SpanAction<char, TState> action)
            => string.Create(length, state, action);
#else
        public delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

        public static string BuildStringFromIndefiniteLengthTextString<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            char[] arr = new char[length];
            action(arr, state);
            return new string(arr);
        }
#endif

#if NET
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half ReadHalfBigEndian(ReadOnlySpan<byte> source)
            => BinaryPrimitives.ReadHalfBigEndian(source);
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadHalfBigEndian(ReadOnlySpan<byte> source)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(source);
        }
#endif

        internal static uint SingleToUInt32Bits(float value)
            => unchecked((uint)SingleToInt32Bits(value));

        internal static unsafe int SingleToInt32Bits(float value)
            => *((int*)&value);

        internal static float UInt32BitsToSingle(uint value)
            => Int32BitsToSingle((int)value);

        internal static unsafe float Int32BitsToSingle(int value)
            => *((float*)&value);
    }

    internal static class BinaryPrimitivesPolyfills
    {
        extension(BinaryPrimitives)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double ReadDoubleBigEndian(ReadOnlySpan<byte> source)
            {
                return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(source));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void WriteDoubleBigEndian(Span<byte> destination, double value)
            {
                BinaryPrimitives.WriteInt64BigEndian(destination, BitConverter.DoubleToInt64Bits(value));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe float ReadSingleBigEndian(ReadOnlySpan<byte> source)
            {
                int intValue = BinaryPrimitives.ReadInt32BigEndian(source);
                return *((float*)&intValue);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void WriteSingleBigEndian(Span<byte> destination, float value)
            {
                BinaryPrimitives.WriteInt32BigEndian(destination, *((int*)&value));
            }
        }
    }

    internal static class StackExtensions
    {
        public static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T result)
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
