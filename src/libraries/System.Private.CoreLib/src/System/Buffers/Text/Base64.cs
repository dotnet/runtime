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

        internal interface IBase64Encoder
        {
            static abstract ReadOnlySpan<byte> EncodingMap { get; }
            static abstract sbyte Avx2LutChar62 { get; }
            static abstract sbyte Avx2LutChar63 { get; }
            static abstract ReadOnlySpan<byte> AdvSimdLut4 { get; }
            static abstract uint Ssse3AdvSimdLutE3 { get; }
            static abstract int GetMaxSrcLength(int srcLength, int destLength);
            static abstract unsafe uint EncodeOneOptionallyPadTwo(byte* oneByte, ref byte encodingMap);
            static abstract unsafe uint EncodeTwoOptionallyPadOne(byte* oneByte, ref byte encodingMap);
            static abstract int IncrementPadTwo { get; }
            static abstract int IncrementPadOne { get; }
        }

        internal interface IBase64Decoder
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
            static abstract int SrcLength(bool isFinalBlock, int utf8Length);
            static abstract int GetMaxDecodedLength(int utf8Length);
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

        }
    }
}
