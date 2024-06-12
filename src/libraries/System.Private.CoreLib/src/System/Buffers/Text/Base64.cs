// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Buffers.Text
{
    public static partial class Base64
    {
        [Conditional("DEBUG")]
        internal static unsafe void AssertRead<TVector>(byte* src, byte* srcStart, int srcLength)
        {
            int vectorElements = Unsafe.SizeOf<TVector>();
            byte* readEnd = src + vectorElements;
            byte* srcEnd = srcStart + srcLength;

            if (readEnd > srcEnd)
            {
                int srcIndex = (int)(src - srcStart);
                Debug.Fail($"Read for {typeof(TVector)} is not within safe bounds. srcIndex: {srcIndex}, srcLength: {srcLength}");
            }
        }

        [Conditional("DEBUG")]
        internal static unsafe void AssertWrite<TVector>(byte* dest, byte* destStart, int destLength)
        {
            int vectorElements = Unsafe.SizeOf<TVector>();
            byte* writeEnd = dest + vectorElements;
            byte* destEnd = destStart + destLength;

            if (writeEnd > destEnd)
            {
                int destIndex = (int)(dest - destStart);
                Debug.Fail($"Write for {typeof(TVector)} is not within safe bounds. destIndex: {destIndex}, destLength: {destLength}");
            }
        }

        [Conditional("DEBUG")]
        internal static unsafe void AssertRead<TVector>(ushort* src, ushort* srcStart, int srcLength)
        {
            int vectorElements = Unsafe.SizeOf<TVector>();
            ushort* readEnd = src + vectorElements;
            ushort* srcEnd = srcStart + srcLength;

            if (readEnd > srcEnd)
            {
                int srcIndex = (int)(src - srcStart);
                Debug.Fail($"Read for {typeof(TVector)} is not within safe bounds. srcIndex: {srcIndex}, srcLength: {srcLength}");
            }
        }

        [Conditional("DEBUG")]
        internal static unsafe void AssertWrite<TVector>(ushort* dest, ushort* destStart, int destLength)
        {
            int vectorElements = Unsafe.SizeOf<TVector>();
            ushort* writeEnd = dest + vectorElements;
            ushort* destEnd = destStart + destLength;

            if (writeEnd > destEnd)
            {
                int destIndex = (int)(dest - destStart);
                Debug.Fail($"Write for {typeof(TVector)} is not within safe bounds. destIndex: {destIndex}, destLength: {destLength}");
            }
        }

        internal interface IBase64Encoder<T> where T : unmanaged
        {
            static abstract ReadOnlySpan<byte> EncodingMap { get; }
            static abstract sbyte Avx2LutChar62 { get; }
            static abstract sbyte Avx2LutChar63 { get; }
            static abstract ReadOnlySpan<byte> AdvSimdLut4 { get; }
            static abstract uint Ssse3AdvSimdLutE3 { get; }
            static abstract int GetMaxSrcLength(int srcLength, int destLength);
            static abstract int GetMaxEncodedLength(int srcLength);
            static abstract uint GetInPlaceDestinationLength(int encodedLength, int leftOver);
            static abstract unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, T* dest, ref byte encodingMap);
            static abstract unsafe void EncodeTwoOptionallyPadOne(byte* oneByte, T* dest, ref byte encodingMap);
            static abstract unsafe void EncodeThreeAndWrite(byte* threeBytes, T* destination, ref byte encodingMap);
            static abstract int IncrementPadTwo { get; }
            static abstract int IncrementPadOne { get; }
            static abstract unsafe void StoreVector512ToDestination(T* dest, T* destStart, int destLength, Vector512<byte> str);
            static abstract unsafe void StoreVector256ToDestination(T* dest, T* destStart, int destLength, Vector256<byte> str);
            static abstract unsafe void StoreVector128ToDestination(T* dest, T* destStart, int destLength, Vector128<byte> str);
            static abstract unsafe void StoreArmVector128x4ToDestination(T* dest, T* destStart, int destLength, Vector128<byte> res1,
                Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4);
        }

        internal interface IBase64Decoder<T> where T : unmanaged
        {
            static abstract ReadOnlySpan<sbyte> DecodingMap { get; }
            static abstract ReadOnlySpan<uint> VbmiLookup0 { get; }
            static abstract ReadOnlySpan<uint> VbmiLookup1 { get; }
            static abstract ReadOnlySpan<sbyte> Avx2LutHigh { get; }
            static abstract ReadOnlySpan<sbyte> Avx2LutLow { get; }
            static abstract ReadOnlySpan<sbyte> Avx2LutShift { get; }
            static abstract byte MaskSlashOrUnderscore { get; }
            static abstract ReadOnlySpan<int> Vector128LutHigh { get; }
            static abstract ReadOnlySpan<int> Vector128LutLow { get; }
            static abstract ReadOnlySpan<uint> Vector128LutShift { get; }
            static abstract ReadOnlySpan<uint> AdvSimdLutOne3 { get; }
            static abstract uint AdvSimdLutTwo3Uint1 { get; }
            static abstract int SrcLength(bool isFinalBlock, int sourceLength);
            static abstract int GetMaxDecodedLength(int sourceLength);
            static abstract bool IsInvalidLength(int bufferLength);
            static abstract bool IsValidPadding(uint padChar);
            static abstract bool TryDecode128Core(
                Vector128<byte> str,
                Vector128<byte> hiNibbles,
                Vector128<byte> maskSlashOrUnderscore,
                Vector128<byte> mask8F,
                Vector128<byte> lutLow,
                Vector128<byte> lutHigh,
                Vector128<sbyte> lutShift,
                Vector128<byte> shiftForUnderscore,
                out Vector128<byte> result);
            static abstract bool TryDecode256Core(
                Vector256<sbyte> str,
                Vector256<sbyte> hiNibbles,
                Vector256<sbyte> maskSlashOrUnderscore,
                Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh,
                Vector256<sbyte> lutShift,
                Vector256<sbyte> shiftForUnderscore,
                out Vector256<sbyte> result);
            static abstract unsafe int DecodeFourElements(T* source, ref sbyte decodingMap);
            static abstract unsafe int DecodeRemaining(T* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3);
            static abstract int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<T> span);
            static abstract OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TTBase64Decoder>(ReadOnlySpan<T> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TTBase64Decoder : IBase64Decoder<T>;
            static abstract unsafe bool TryLoadVector512(T* src, T* srcStart, int sourceLength, out Vector512<sbyte> str);
            static abstract unsafe bool TryLoadAvxVector256(T* src, T* srcStart, int sourceLength, out Vector256<sbyte> str);
            static abstract unsafe bool TryLoadVector128(T* src, T* srcStart, int sourceLength, out Vector128<byte> str);
            static abstract unsafe bool TryLoadArmVector128x4(T* src, T* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4);
        }
    }
}
