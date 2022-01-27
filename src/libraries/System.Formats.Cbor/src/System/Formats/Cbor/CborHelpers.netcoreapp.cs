// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Formats.Cbor
{
    internal static partial class CborHelpers
    {
        public static readonly DateTimeOffset UnixEpoch = DateTimeOffset.UnixEpoch;

        public static int GetBytes(Encoding encoding, ReadOnlySpan<char> source, Span<byte> destination)
            => encoding.GetBytes(source, destination);

        public static int GetByteCount(Encoding encoding, ReadOnlySpan<char> chars)
            => encoding.GetByteCount(chars);

        public static int GetChars(Encoding encoding, ReadOnlySpan<byte> source, Span<char> destination)
            => encoding.GetChars(source, destination);

        public static int GetCharCount(Encoding encoding, ReadOnlySpan<byte> source)
            => encoding.GetCharCount(source);

        public static string GetString(Encoding encoding, ReadOnlySpan<byte> bytes)
            => encoding.GetString(bytes);

        public static BigInteger CreateBigIntegerFromUnsignedBigEndianBytes(byte[] bytes)
            => new BigInteger(bytes, isUnsigned: true, isBigEndian: true);

        public static byte[] CreateUnsignedBigEndianBytesFromBigInteger(BigInteger value)
            => value.ToByteArray(isUnsigned: true, isBigEndian: true);

        public static void GetBitsFromDecimal(decimal d, Span<int> destination)
            => decimal.GetBits(d, destination);

        public static string BuildStringFromIndefiniteLengthTextString<TState>(int length, TState state, SpanAction<char, TState> action)
            => string.Create(length, state, action);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half ReadHalfBigEndian(ReadOnlySpan<byte> source)
            => BinaryPrimitives.ReadHalfBigEndian(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float ReadSingleBigEndian(ReadOnlySpan<byte> source)
            => BinaryPrimitives.ReadSingleBigEndian(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDoubleBigEndian(ReadOnlySpan<byte> source)
            => BinaryPrimitives.ReadDoubleBigEndian(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteSingleBigEndian(Span<byte> destination, float value)
            => BinaryPrimitives.WriteSingleBigEndian(destination, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteDoubleBigEndian(Span<byte> destination, double value)
            => BinaryPrimitives.WriteDoubleBigEndian(destination, value);
    }
}
