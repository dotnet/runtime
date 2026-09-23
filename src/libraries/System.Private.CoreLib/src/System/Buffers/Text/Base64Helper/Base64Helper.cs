// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET
using System.Runtime.Intrinsics;
#endif

namespace System.Buffers.Text
{
    internal static partial class Base64Helper
    {
        [DoesNotReturn]
        internal static void ThrowUnreachableException()
        {
#if NET
            throw new UnreachableException();
#else
            throw new Exception("Unreachable");
#endif
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
            void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<T> dest, ReadOnlySpan<byte> encodingMap);
            void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<T> dest, ReadOnlySpan<byte> encodingMap);
            void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<T> destination, ReadOnlySpan<byte> encodingMap);
            int IncrementPadTwo { get; }
            int IncrementPadOne { get; }
#if NET
            // The callers guarantee that dest has room for the whole (widened) vector.
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
            // The callers guarantee that src has enough elements for the vector loads.
            bool TryLoadVector512(ReadOnlySpan<T> src, out Vector512<sbyte> str);
            bool TryLoadAvxVector256(ReadOnlySpan<T> src, out Vector256<sbyte> str);
            bool TryLoadVector128(ReadOnlySpan<T> src, out Vector128<byte> str);
            bool TryLoadArmVector128x4(ReadOnlySpan<T> src,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4);
#endif // NET
            int DecodeFourElements(ReadOnlySpan<T> source, ReadOnlySpan<sbyte> decodingMap);
            int DecodeRemaining(ReadOnlySpan<T> remaining, ReadOnlySpan<sbyte> decodingMap, out uint t2, out uint t3);
            int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<T> span);
            OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TTBase64Decoder>(TTBase64Decoder decoder, ReadOnlySpan<T> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TTBase64Decoder : IBase64Decoder<T>;
        }
    }
}
