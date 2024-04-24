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
            static abstract int IncrementPadTwo { get; }
            static abstract int IncrementPadOne { get; }
            static abstract ReadOnlySpan<byte> EncodingMap { get; }
            static abstract Vector256<sbyte> Avx2Lut { get; }
            static abstract Vector128<byte> AdvSimdLut4 { get; }
            static abstract Vector128<byte> Ssse3AdvSimdLut { get; }
            static abstract int GetMaxSrcLength(int srcLength, int destLength);
            static abstract unsafe uint EncodeOneOptionallyPadTwo(byte* oneByte, ref byte encodingMap);
            static abstract unsafe uint EncodeTwoOptionallyPadOne(byte* oneByte, ref byte encodingMap);
        }
    }
}
