// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
#endif

namespace System.Buffers.Text
{
    internal static partial class Base64Helper
    {
        internal const int MaxStackallocThreshold = 256;

        [DoesNotReturn]
        internal static void ThrowUnreachableException()
        {
            throw new UnreachableException();
        }

#if NET
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector128<byte>, Vector128<byte>, Vector128<byte>) LoadArmVector128x3(ReadOnlySpan<byte> source)
        {
            var table = (Vector128.Create(source), Vector128.Create(source.Slice(16)), Vector128.Create(source.Slice(32)));
            return (
                AdvSimd.Arm64.VectorTableLookup(table, Vector128.Create((byte)0, 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45)),
                AdvSimd.Arm64.VectorTableLookup(table, Vector128.Create((byte)1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34, 37, 40, 43, 46)),
                AdvSimd.Arm64.VectorTableLookup(table, Vector128.Create((byte)2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35, 38, 41, 44, 47)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void StoreArmVector128x3(Span<byte> destination, Vector128<byte> first, Vector128<byte> second, Vector128<byte> third)
        {
            var table = (first, second, third);
            Vector128<byte> firstIndices = Vector128.Create((byte)0, 16, 32, 1, 17, 33, 2, 18, 34, 3, 19, 35, 4, 20, 36, 5);
            Vector128<byte> secondIndices = Vector128.Create((byte)21, 37, 6, 22, 38, 7, 23, 39, 8, 24, 40, 9, 25, 41, 10, 26);
            Vector128<byte> thirdIndices = Vector128.Create((byte)42, 11, 27, 43, 12, 28, 44, 13, 29, 45, 14, 30, 46, 15, 31, 47);
            AdvSimd.Arm64.VectorTableLookup(table, firstIndices).CopyTo(destination);
            AdvSimd.Arm64.VectorTableLookup(table, secondIndices).CopyTo(destination.Slice(16));
            AdvSimd.Arm64.VectorTableLookup(table, thirdIndices).CopyTo(destination.Slice(32));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector128<T>, Vector128<T>, Vector128<T>, Vector128<T>) LoadArmVector128x4<T>(ReadOnlySpan<T> source)
        {
            int count = Vector128<T>.Count;
            Vector128<T> first = Vector128.Create(source);
            Vector128<T> second = Vector128.Create(source.Slice(count));
            Vector128<T> third = Vector128.Create(source.Slice(2 * count));
            Vector128<T> fourth = Vector128.Create(source.Slice(3 * count));
            Vector128<T> evenLow = Vector128.UnzipEven(first, second);
            Vector128<T> oddLow = Vector128.UnzipOdd(first, second);
            Vector128<T> evenHigh = Vector128.UnzipEven(third, fourth);
            Vector128<T> oddHigh = Vector128.UnzipOdd(third, fourth);
            return (Vector128.UnzipEven(evenLow, evenHigh), Vector128.UnzipEven(oddLow, oddHigh),
                Vector128.UnzipOdd(evenLow, evenHigh), Vector128.UnzipOdd(oddLow, oddHigh));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void StoreArmVector128x4<T>(Span<T> destination, Vector128<T> first, Vector128<T> second, Vector128<T> third, Vector128<T> fourth)
        {
            int count = Vector128<T>.Count;
            Vector128<T> evenLow = Vector128.ZipLower(first, third);
            Vector128<T> oddLow = Vector128.ZipLower(second, fourth);
            Vector128<T> evenHigh = Vector128.ZipUpper(first, third);
            Vector128<T> oddHigh = Vector128.ZipUpper(second, fourth);
            Vector128.ZipLower(evenLow, oddLow).CopyTo(destination);
            Vector128.ZipUpper(evenLow, oddLow).CopyTo(destination.Slice(count));
            Vector128.ZipLower(evenHigh, oddHigh).CopyTo(destination.Slice(2 * count));
            Vector128.ZipUpper(evenHigh, oddHigh).CopyTo(destination.Slice(3 * count));
        }
#endif

        internal interface IBase64Encoder<T> where T : unmanaged
        {
            ReadOnlySpan<byte> EncodingMap { get; }
            sbyte Avx2LutChar62 { get; }
            sbyte Avx2LutChar63 { get; }
            ReadOnlySpan<byte> AdvSimdLut4 { get; }
            uint Ssse3AdvSimdLutE3 { get; }
            int GetMaxSrcLength(int srcLength, int destLength);
            int GetMaxEncodedLength(int srcLength);
            uint GetInPlaceDestinationLength(int encodedLength, int leftOver);
            void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<T> dest, ReadOnlySpan<byte> encodingMap);
            void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> oneByte, Span<T> dest, ReadOnlySpan<byte> encodingMap);
            void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<T> destination, ReadOnlySpan<byte> encodingMap);
            int IncrementPadTwo { get; }
            int IncrementPadOne { get; }
#if NET
            void StoreVector512ToDestination(Span<T> dest, Vector512<byte> str);
            void StoreVector256ToDestination(Span<T> dest, Vector256<byte> str);
            void StoreVector128ToDestination(Span<T> dest, Vector128<byte> str);
            void StoreArmVector128x4ToDestination(Span<T> dest, Vector128<byte> res1,
                Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4);
#endif // NET
        }

        internal interface IBase64Decoder<T> where T : unmanaged
        {
            ReadOnlySpan<sbyte> DecodingMap { get; }
            ReadOnlySpan<uint> VbmiLookup0 { get; }
            ReadOnlySpan<uint> VbmiLookup1 { get; }
            ReadOnlySpan<sbyte> Avx2LutHigh { get; }
            ReadOnlySpan<sbyte> Avx2LutLow { get; }
            ReadOnlySpan<sbyte> Avx2LutShift { get; }
            byte MaskSlashOrUnderscore { get; }
            ReadOnlySpan<int> Vector128LutHigh { get; }
            ReadOnlySpan<int> Vector128LutLow { get; }
            ReadOnlySpan<uint> Vector128LutShift { get; }
            ReadOnlySpan<uint> AdvSimdLutOne3 { get; }
            uint AdvSimdLutTwo3Uint1 { get; }
            int SrcLength(bool isFinalBlock, int sourceLength);
            int GetMaxDecodedLength(int sourceLength);
            bool IsInvalidLength(int bufferLength);
            bool IsValidPadding(uint padChar);
#if NET
            bool TryDecode128Core(
                Vector128<byte> str,
                Vector128<byte> hiNibbles,
                Vector128<byte> maskSlashOrUnderscore,
                Vector128<byte> mask8F,
                Vector128<byte> lutLow,
                Vector128<byte> lutHigh,
                Vector128<sbyte> lutShift,
                Vector128<byte> shiftForUnderscore,
                out Vector128<byte> result);
            bool TryDecode256Core(
                Vector256<sbyte> str,
                Vector256<sbyte> hiNibbles,
                Vector256<sbyte> maskSlashOrUnderscore,
                Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh,
                Vector256<sbyte> lutShift,
                Vector256<sbyte> shiftForUnderscore,
                out Vector256<sbyte> result);
            bool TryLoadVector512(ReadOnlySpan<T> src, out Vector512<sbyte> str);
            bool TryLoadAvxVector256(ReadOnlySpan<T> src, out Vector256<sbyte> str);
            bool TryLoadVector128(ReadOnlySpan<T> src, out Vector128<byte> str);
            bool TryLoadArmVector128x4(ReadOnlySpan<T> src,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4);
#endif // NET
            int DecodeFourElements(ReadOnlySpan<T> source, ReadOnlySpan<sbyte> decodingMap);
            int DecodeRemaining(ReadOnlySpan<T> srcEnd, ReadOnlySpan<sbyte> decodingMap, int remaining, out uint t2, out uint t3);
            int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<T> span);
            OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TTBase64Decoder>(TTBase64Decoder decoder, ReadOnlySpan<T> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TTBase64Decoder : IBase64Decoder<T>;
        }
    }
}
