// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET
using System.Runtime.Intrinsics;
#endif

namespace System.Buffers.Text
{
    internal static partial class Base64Helper
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
            ReadOnlySpan<byte> EncodingMap { get; }
            sbyte Avx2LutChar62 { get; }
            sbyte Avx2LutChar63 { get; }
            ReadOnlySpan<byte> AdvSimdLut4 { get; }
            uint Ssse3AdvSimdLutE3 { get; }
            int GetMaxSrcLength(int srcLength, int destLength);
            int GetMaxEncodedLength(int srcLength);
            uint GetInPlaceDestinationLength(int encodedLength, int leftOver);
            unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, T* dest, ref byte encodingMap);
            unsafe void EncodeTwoOptionallyPadOne(byte* oneByte, T* dest, ref byte encodingMap);
            unsafe void EncodeThreeAndWrite(byte* threeBytes, T* destination, ref byte encodingMap);
            int IncrementPadTwo { get; }
            int IncrementPadOne { get; }
#if NET
            unsafe void StoreVector512ToDestination(T* dest, T* destStart, int destLength, Vector512<byte> str);
            unsafe void StoreVector256ToDestination(T* dest, T* destStart, int destLength, Vector256<byte> str);
            unsafe void StoreVector128ToDestination(T* dest, T* destStart, int destLength, Vector128<byte> str);
            unsafe void StoreArmVector128x4ToDestination(T* dest, T* destStart, int destLength, Vector128<byte> res1,
                Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4);
#endif
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
            unsafe bool TryLoadVector512(T* src, T* srcStart, int sourceLength, out Vector512<sbyte> str);
            unsafe bool TryLoadAvxVector256(T* src, T* srcStart, int sourceLength, out Vector256<sbyte> str);
            unsafe bool TryLoadVector128(T* src, T* srcStart, int sourceLength, out Vector128<byte> str);
            unsafe bool TryLoadArmVector128x4(T* src, T* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4);
#endif
            unsafe int DecodeFourElements(T* source, ref sbyte decodingMap);
            unsafe int DecodeRemaining(T* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3);
            int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<T> span);
            OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TTBase64Decoder>(TTBase64Decoder decoder, ReadOnlySpan<T> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TTBase64Decoder : IBase64Decoder<T>;
        }
    }
}
