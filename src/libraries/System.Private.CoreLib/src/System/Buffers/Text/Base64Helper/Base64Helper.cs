// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Wasm;
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

#if NET8_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool VectorContainsNonAsciiChar(Vector128<ushort> utf16Vector)
        {
            // prefer architecture specific intrinsic as they offer better perf
            if (Sse2.IsSupported)
            {
                if (Sse41.IsSupported)
                {
                    Vector128<ushort> asciiMaskForTestZ = Vector128.Create((ushort)0xFF80);
                    // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                    return !Sse41.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
                }
                else
                {
                    Vector128<ushort> asciiMaskForAddSaturate = Vector128.Create((ushort)0x7F80);
                    // The operation below forces the 0x8000 bit of each WORD to be set iff the WORD element
                    // has value >= 0x0800 (non-ASCII). Then we'll treat the vector as a BYTE vector in order
                    // to extract the mask. Reminder: the 0x0080 bit of each WORD should be ignored.
                    return (Sse2.MoveMask(Sse2.AddSaturate(utf16Vector, asciiMaskForAddSaturate).AsByte()) & 0b_1010_1010_1010_1010) != 0;
                }
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                // First we pick four chars, a larger one from all four pairs of adjecent chars in the vector.
                // If any of those four chars has a non-ASCII bit set, we have seen non-ASCII data.
                Vector128<ushort> maxChars = AdvSimd.Arm64.MaxPairwise(utf16Vector, utf16Vector);
                return (maxChars.AsUInt64().ToScalar() & 0xFF80FF80FF80FF80) != 0;
            }
            else
            {
                const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
                Vector128<ushort> zeroIsAscii = utf16Vector & Vector128.Create(asciiMask);
                // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                return zeroIsAscii != Vector128<ushort>.Zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<byte> ExtractAsciiVector(Vector128<ushort> vectorFirst, Vector128<ushort> vectorSecond)
        {
            // Narrows two vectors of words [ w7 w6 w5 w4 w3 w2 w1 w0 ] and [ w7' w6' w5' w4' w3' w2' w1' w0' ]
            // to a vector of bytes [ b7 ... b0 b7' ... b0'].

            // prefer architecture specific intrinsic as they don't perform additional AND like Vector128.Narrow does
            if (Sse2.IsSupported)
            {
                return Sse2.PackUnsignedSaturate(vectorFirst.AsInt16(), vectorSecond.AsInt16());
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.UnzipEven(vectorFirst.AsByte(), vectorSecond.AsByte());
            }
            else if (PackedSimd.IsSupported)
            {
                return PackedSimd.ConvertNarrowingSaturateUnsigned(vectorFirst.AsInt16(), vectorSecond.AsInt16());
            }
            else
            {
                return Vector128.Narrow(vectorFirst, vectorSecond);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool VectorContainsNonAsciiChar(Vector256<ushort> utf16Vector)
        {
            if (Avx.IsSupported)
            {
                Vector256<ushort> asciiMaskForTestZ = Vector256.Create((ushort)0xFF80);
                return !Avx.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
            }
            else
            {
                const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
                Vector256<ushort> zeroIsAscii = utf16Vector & Vector256.Create(asciiMask);
                // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                return zeroIsAscii != Vector256<ushort>.Zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool VectorContainsNonAsciiChar(Vector512<ushort> utf16Vector)
        {
            const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
            Vector512<ushort> zeroIsAscii = utf16Vector & Vector512.Create(asciiMask);
            // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
            return zeroIsAscii != Vector512<ushort>.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<byte> ShuffleUnsafe(Vector128<byte> vector, Vector128<byte> indices)
        {
            if (Ssse3.IsSupported)
            {
                return Ssse3.Shuffle(vector, indices);
            }

            if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.VectorTableLookup(vector, indices);
            }

            if (PackedSimd.IsSupported)
            {
                return PackedSimd.Swizzle(vector, indices);
            }

            return Vector128.Shuffle(vector, indices);
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
            unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, T* dest, ref byte encodingMap);
            unsafe void EncodeTwoOptionallyPadOne(byte* oneByte, T* dest, ref byte encodingMap);
            unsafe void EncodeThreeAndWrite(byte* threeBytes, T* destination, ref byte encodingMap);
            int IncrementPadTwo { get; }
            int IncrementPadOne { get; }
#if NET
            unsafe void StoreVector512ToDestination(T* dest, T* destStart, int destLength, Vector512<byte> str);
            unsafe void StoreVector256ToDestination(T* dest, T* destStart, int destLength, Vector256<byte> str);
            unsafe void StoreVector128ToDestination(T* dest, T* destStart, int destLength, Vector128<byte> str);
#if NET9_0_OR_GREATER
            unsafe void StoreArmVector128x4ToDestination(T* dest, T* destStart, int destLength, Vector128<byte> res1,
                Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4);
#endif // NET9_0_OR_GREATER
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
            unsafe bool TryLoadVector512(T* src, T* srcStart, int sourceLength, out Vector512<sbyte> str);
            unsafe bool TryLoadAvxVector256(T* src, T* srcStart, int sourceLength, out Vector256<sbyte> str);
            unsafe bool TryLoadVector128(T* src, T* srcStart, int sourceLength, out Vector128<byte> str);
#if NET9_0_OR_GREATER
            unsafe bool TryLoadArmVector128x4(T* src, T* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4);
#endif // NET9_0_OR_GREATER
#endif // NET
            unsafe int DecodeFourElements(T* source, ref sbyte decodingMap);
            unsafe int DecodeRemaining(T* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3);
            int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<T> span);
            OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TTBase64Decoder>(TTBase64Decoder decoder, ReadOnlySpan<T> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TTBase64Decoder : IBase64Decoder<T>;
        }
    }
}
