// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text.Unicode;
using static System.Buffers.Text.Base64;

namespace System.Buffers.Text
{
    // AVX2 and Vector128 version based on https://github.com/gfoidl/Base64/blob/master/source/gfoidl.Base64/Internal/Encodings/Base64UrlEncoding.cs

    public static partial class Base64Url
    {
        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text within a byte span of size "length".
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="base64Length"/> is less than 0.
        /// </exception>
        public static int GetMaxDecodedLength(int base64Length)
        {
            if (base64Length < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }

            int remainder = base64Length % 4;

            return (base64Length >> 2) * 3 + (remainder > 0 ? remainder - 1 : 0);
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// If the input is not a multiple of 4, it will decode as much as it can, to the closest multiple of 4.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to encode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to encode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the decoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is false and the input is not a multiple of 4
        /// - InvalidData - if the input contains bytes outside of the expected Base64Url range,
        /// or if it contains invalid/more than two padding characters and <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// </returns>
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFromUtf8<Base64UrlDecoder>(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        /// <summary>
        /// Decode the span of UTF-8 encoded text in Base64Url (in-place) into binary data.
        /// The decoded binary output is smaller than the text data contained in the input (the operation deflates the data).
        /// TODO: If the input is not a multiple of 4, it will not decode any.
        /// </summary>
        /// <param name="buffer">The input span which contains the base 64 text data that needs to be decoded.</param>
        /// <returns>TODO: It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - InvalidData - if the input contains bytes outside of the expected base 64 range, or if it contains invalid/more than two padding characters.
        /// It does not return DestinationTooSmall since that is not possible for base 64 decoding.
        /// It does not return NeedMoreData since this method tramples the data in the buffer and
        /// hence can only be called once with all the data in the buffer.
        /// </returns>
        public static int DecodeFromUtf8InPlace(Span<byte> buffer)
        {
            DecodeFromUtf8InPlace<Base64UrlDecoder>(buffer, out int bytesWritten, ignoreWhiteSpace: true);
            return bytesWritten;
        }

        public static int DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);

            if (OperationStatus.Done == status)
            {
                return bytesWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException("DestinationTooSmall", nameof(destination));
            }

            throw new InvalidOperationException("InvalidData");
        }

        public static bool TryDecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out bytesWritten);

            return status == OperationStatus.Done;
        }

        public static byte[] DecodeFromUtf8(ReadOnlySpan<byte> source)
        {
            Span<byte> destination = stackalloc byte[GetMaxDecodedLength(source.Length)];
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);

            return OperationStatus.Done == status ? destination.Slice(0, bytesWritten).ToArray() :
                throw new InvalidOperationException("InvalidData");
        }

        public static OperationStatus DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination,
            out int charsConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFromChars<Base64UrlDecoder>(source, destination, out charsConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        internal static unsafe OperationStatus DecodeFromChars<TBase64Decoder>(ReadOnlySpan<char> source, Span<byte> destination,
            out int bytesConsumed, out int bytesWritten, bool isFinalBlock, bool ignoreWhiteSpace)
            where TBase64Decoder : IBase64Decoder
        {
            if (source.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (char* srcBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* destBytes = &MemoryMarshal.GetReference(destination))
            {
                int srcLength = TBase64Decoder.SrcLength(isFinalBlock, source.Length);
                int destLength = destination.Length;
                int maxSrcLength = srcLength;
                int decodedLength = TBase64Decoder.GetMaxDecodedLength(srcLength);

                // max. 2 padding chars
                if (destLength < decodedLength - 2)
                {
                    // For overflow see comment below
                    maxSrcLength = destLength / 3 * 4;
                }

                char* src = srcBytes;
                byte* dest = destBytes;
                char* srcEnd = srcBytes + (uint)srcLength;
                char* srcMax = srcBytes + (uint)maxSrcLength;

                if (maxSrcLength >= 24)
                {
                    char* end = srcMax - 88;
                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && (end >= src))
                    {
                        Avx512Decode<TBase64Decoder>(ref src, ref dest, end, maxSrcLength, destLength, (ushort*)srcBytes, destBytes);

                        if (src == srcEnd)
                        {
                            goto DoneExit;
                        }
                    }

                    end = srcMax - 45;
                    if (Avx2.IsSupported && (end >= src))
                    {
                        Avx2Decode<TBase64Decoder>(ref src, ref dest, end, maxSrcLength, destLength, (ushort*)srcBytes, destBytes);

                        if (src == srcEnd)
                        {
                            goto DoneExit;
                        }
                    }

                    end = srcMax - 24;
                    if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && BitConverter.IsLittleEndian && (end >= src))
                    {
                        Vector128Decode<TBase64Decoder>(ref src, ref dest, end, maxSrcLength, destLength, (ushort*)srcBytes, destBytes);

                        if (src == srcEnd)
                        {
                            goto DoneExit;
                        }
                    }
                }

                // Last bytes could have padding characters, so process them separately and treat them as valid only if isFinalBlock is true
                // if isFinalBlock is false, padding characters are considered invalid
                int skipLastChunk = isFinalBlock ? 4 : 0;

                if (destLength >= decodedLength)
                {
                    maxSrcLength = srcLength - skipLastChunk;
                }
                else
                {
                    // This should never overflow since destLength here is less than int.MaxValue / 4 * 3 (i.e. 1610612733)
                    // Therefore, (destLength / 3) * 4 will always be less than 2147483641
                    Debug.Assert(destLength < (int.MaxValue / 4 * 3));
                    maxSrcLength = (destLength / 3) * 4;
                    srcLength &= ~0x3; // Round down to multiple of 4, this only affect Base64UrlDecoder path
                }

                ref sbyte decodingMap = ref MemoryMarshal.GetReference(TBase64Decoder.DecodingMap);
                srcMax = srcBytes + maxSrcLength;

                while (src < srcMax)
                {
                    int result = Decode(src, ref decodingMap);

                    if (result < 0)
                    {
                        goto InvalidDataExit;
                    }

                    WriteThreeLowOrderBytes(dest, result);
                    src += 4;
                    dest += 3;
                }

                if (maxSrcLength != srcLength - skipLastChunk)
                {
                    goto DestinationTooSmallExit;
                }

                // If input is less than 4 bytes, srcLength == sourceIndex == 0
                // If input is not a multiple of 4, sourceIndex == srcLength != 0
                if (src == srcEnd)
                {
                    if (isFinalBlock)
                    {
                        goto InvalidDataExit;
                    }

                    if (src == srcBytes + source.Length)
                    {
                        goto DoneExit;
                    }

                    goto NeedMoreDataExit;
                }

                // if isFinalBlock is false, we will never reach this point

                uint t0;
                uint t1;
                uint t2;
                uint t3;
                // Handle remaining, for Base64 its always 4 bytes, for Base64Url it could be 2, 3, or 4 bytes left.
                long remaining = srcEnd - src;
                switch (remaining)
                {
                    case 2:
                        t0 = srcEnd[-2];
                        t1 = srcEnd[-1];
                        t2 = EncodingPad;
                        t3 = EncodingPad;
                        break;
                    case 3:
                        t0 = srcEnd[-3];
                        t1 = srcEnd[-2];
                        t2 = srcEnd[-1];
                        t3 = EncodingPad;
                        break;
                    case 4:
                        t0 = srcEnd[-4];
                        t1 = srcEnd[-3];
                        t2 = srcEnd[-2];
                        t3 = srcEnd[-1];
                        break;
                    default:
                        goto InvalidDataExit;
                }

                if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
                {
                    goto InvalidDataExit;
                }

                int i0 = Unsafe.Add(ref decodingMap, (IntPtr)t0);
                int i1 = Unsafe.Add(ref decodingMap, (IntPtr)t1);

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;

                byte* destMax = destBytes + (uint)destLength;

                if (t3 != EncodingPad)
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);
                    int i3 = Unsafe.Add(ref decodingMap, (IntPtr)t3);

                    i2 <<= 6;

                    i0 |= i3;
                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 3 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    WriteThreeLowOrderBytes(dest, i0);
                    dest += 3;
                    src += 4;
                }
                else if (t2 != EncodingPad)
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);

                    i2 <<= 6;

                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 2 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    dest[0] = (byte)(i0 >> 16);
                    dest[1] = (byte)(i0 >> 8);
                    dest += 2;
                    src += remaining;
                }
                else
                {
                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 1 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    dest[0] = (byte)(i0 >> 16);
                    dest += 1;
                    src += remaining;
                }

                if (srcLength != source.Length)
                {
                    goto InvalidDataExit;
                }

            DoneExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.Done;

            DestinationTooSmallExit:
                if (srcLength != source.Length && isFinalBlock)
                {
                    goto InvalidDataExit; // if input is not a multiple of 4, and there is no more data, return invalid data instead
                }

                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.DestinationTooSmall;

            NeedMoreDataExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.NeedMoreData;

            InvalidDataExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return ignoreWhiteSpace ?
                    InvalidDataFallback(source, destination, ref bytesConsumed, ref bytesWritten, isFinalBlock) :
                    OperationStatus.InvalidData;
            }

            static OperationStatus InvalidDataFallback(ReadOnlySpan<char> utf8, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock)
            {
                utf8 = utf8.Slice(bytesConsumed);
                bytes = bytes.Slice(bytesWritten);

                OperationStatus status;
                do
                {
                    int localConsumed = IndexOfAnyExceptWhiteSpace(utf8);
                    if (localConsumed < 0)
                    {
                        // The remainder of the input is all whitespace. Mark it all as having been consumed,
                        // and mark the operation as being done.
                        bytesConsumed += utf8.Length;
                        status = OperationStatus.Done;
                        break;
                    }

                    if (localConsumed == 0)
                    {
                        // Non-whitespace was found at the beginning of the input. Since it wasn't consumed
                        // by the previous call to DecodeFromUtf8, it must be part of a Base64 sequence
                        // that was interrupted by whitespace or something else considered invalid.
                        // Fall back to block-wise decoding. This is very slow, but it's also very non-standard
                        // formatting of the input; whitespace is typically only found between blocks, such as
                        // when Convert.ToBase64String inserts a line break every 76 output characters.
                        return DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(utf8, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
                    }

                    // Skip over the starting whitespace and continue.
                    bytesConsumed += localConsumed;
                    utf8 = utf8.Slice(localConsumed);

                    // Try again after consumed whitespace
                    status = DecodeFromChars<TBase64Decoder>(utf8, bytes, out localConsumed, out int localWritten, isFinalBlock, ignoreWhiteSpace: false);
                    bytesConsumed += localConsumed;
                    bytesWritten += localWritten;
                    if (status is not OperationStatus.InvalidData)
                    {
                        break;
                    }

                    utf8 = utf8.Slice(localConsumed);
                    bytes = bytes.Slice(localWritten);
                }
                while (!utf8.IsEmpty);

                return status;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static unsafe void Avx512Decode<TBase64Decoder>(ref char* srcBytes, ref byte* destBytes, char* srcEnd, int sourceLength, int destLength, ushort* srcStart, byte* destStart)
            where TBase64Decoder : IBase64Decoder
        {
            // Reference for VBMI implementation : https://github.com/WojciechMula/base64simd/tree/master/decode
            // If we have AVX512 support, pick off 64 bytes at a time for as long as we can,
            // but make sure that we quit before seeing any == markers at the end of the
            // string. Also, because we write 16 zeroes at the end of the output, ensure
            // that there are at least 22 valid bytes of input data remaining to close the
            // gap. 64 + 2 + 22 = 88 bytes.
            ushort* src = (ushort*)srcBytes;
            byte* dest = destBytes;

            // The JIT won't hoist these "constants", so help it
            Vector512<sbyte> vbmiLookup0 = Vector512.Create(TBase64Decoder.VbmiLookup0).AsSByte();
            Vector512<sbyte> vbmiLookup1 = Vector512.Create(TBase64Decoder.VbmiLookup1).AsSByte();
            Vector512<byte> vbmiPackedLanesControl = Vector512.Create(
                0x06000102, 0x090a0405, 0x0c0d0e08, 0x16101112,
                0x191a1415, 0x1c1d1e18, 0x26202122, 0x292a2425,
                0x2c2d2e28, 0x36303132, 0x393a3435, 0x3c3d3e38,
                0x00000000, 0x00000000, 0x00000000, 0x00000000).AsByte();

            Vector512<sbyte> mergeConstant0 = Vector512.Create(0x01400140).AsSByte();
            Vector512<short> mergeConstant1 = Vector512.Create(0x00011000).AsInt16();

            // This algorithm requires AVX512VBMI support.
            // Vbmi was first introduced in CannonLake and is available from IceLake on.
            do
            {
                AssertRead<Vector512<ushort>>(src, srcStart, sourceLength);
                Vector512<ushort> utf16VectorLower = Vector512.Load(src);
                Vector512<ushort> utf16VectorUpper = Vector512.Load(src + 32);
                Vector512<sbyte> str = Vector512.Narrow(utf16VectorLower, utf16VectorUpper).AsSByte();

                // Step 1: Translate encoded Base64 input to their original indices
                // This step also checks for invalid inputs and exits.
                // After this, we have indices which are verified to have upper 2 bits set to 0 in each byte.
                // origIndex      = [...|00dddddd|00cccccc|00bbbbbb|00aaaaaa]
                Vector512<sbyte> origIndex = Avx512Vbmi.PermuteVar64x8x2(vbmiLookup0, str, vbmiLookup1);
                Vector512<sbyte> errorVec = (origIndex.AsInt32() | str.AsInt32()).AsSByte();
                if (errorVec.ExtractMostSignificantBits() != 0)
                {
                    break;
                }

                // Step 2: Now we need to reshuffle bits to remove the 0 bits.
                // multiAdd1: [...|0000cccc|ccdddddd|0000aaaa|aabbbbbb]
                Vector512<short> multiAdd1 = Avx512BW.MultiplyAddAdjacent(origIndex.AsByte(), mergeConstant0);
                // multiAdd1: [...|00000000|aaaaaabb|bbbbcccc|ccdddddd]
                Vector512<int> multiAdd2 = Avx512BW.MultiplyAddAdjacent(multiAdd1, mergeConstant1);

                // Step 3: Pack 48 bytes
                str = Avx512Vbmi.PermuteVar64x8(multiAdd2.AsByte(), vbmiPackedLanesControl).AsSByte();

                Base64.AssertWrite<Vector512<sbyte>>(dest, destStart, destLength);
                str.Store((sbyte*)dest);
                src += 64;
                dest += 48;
            }
            while (src <= srcEnd);

            srcBytes = (char*)src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static unsafe void Avx2Decode<TBase64Decoder>(ref char* srcBytes, ref byte* destBytes,
            char* srcEnd, int sourceLength, int destLength, ushort* srcStart, byte* destStart)
            where TBase64Decoder : IBase64Decoder
        {
            // If we have AVX2 support, pick off 32 bytes at a time for as long as we can,
            // but make sure that we quit before seeing any == markers at the end of the
            // string. Also, because we write 8 zeroes at the end of the output, ensure
            // that there are at least 11 valid bytes of input data remaining to close the
            // gap. 32 + 2 + 11 = 45 bytes.

            // See SSSE3-version below for an explanation of how the code works.

            // The JIT won't hoist these "constants", so help it
            Vector256<sbyte> lutHi = Vector256.Create(TBase64Decoder.Avx2LutHigh);

            Vector256<sbyte> lutLo = Vector256.Create(TBase64Decoder.Avx2LutLow);

            Vector256<sbyte> lutShift = Vector256.Create(TBase64Decoder.Avx2LutShift);

            Vector256<sbyte> packBytesInLaneMask = Vector256.Create(
                2, 1, 0, 6,
                5, 4, 10, 9,
                8, 14, 13, 12,
                -1, -1, -1, -1,
                2, 1, 0, 6,
                5, 4, 10, 9,
                8, 14, 13, 12,
                -1, -1, -1, -1);

            Vector256<int> packLanesControl = Vector256.Create(
                 0, 0, 0, 0,
                1, 0, 0, 0,
                2, 0, 0, 0,
                4, 0, 0, 0,
                5, 0, 0, 0,
                6, 0, 0, 0,
                -1, -1, -1, -1,
                -1, -1, -1, -1).AsInt32();

            Vector256<sbyte> maskSlashOrUnderscore = Vector256.Create((sbyte)TBase64Decoder.MaskSlashOrUnderscore);
            Vector256<sbyte> shiftForUnderscore = Vector256.Create((sbyte)33);
            Vector256<sbyte> mergeConstant0 = Vector256.Create(0x01400140).AsSByte();
            Vector256<short> mergeConstant1 = Vector256.Create(0x00011000).AsInt16();

            ushort* src = (ushort*)srcBytes;
            byte* dest = destBytes;

            //while (remaining >= 45)
            do
            {
                AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
                Vector256<ushort> utf16VectorLower = Avx.LoadVector256(src);
                Vector256<ushort> utf16VectorUpper = Avx.LoadVector256(src + 16);
                Vector256<sbyte> str = Vector256.Narrow(utf16VectorLower, utf16VectorUpper).AsSByte();

                Vector256<sbyte> hiNibbles = Avx2.And(Avx2.ShiftRightLogical(str.AsInt32(), 4).AsSByte(), maskSlashOrUnderscore);

                if (!TBase64Decoder.TryDecode256Core(str, hiNibbles, maskSlashOrUnderscore, lutLo, lutHi, lutShift, shiftForUnderscore, out str))
                {
                    break;
                }

                // in, lower lane, bits, upper case are most significant bits, lower case are least significant bits:
                // 00llllll 00kkkkLL 00jjKKKK 00JJJJJJ
                // 00iiiiii 00hhhhII 00ggHHHH 00GGGGGG
                // 00ffffff 00eeeeFF 00ddEEEE 00DDDDDD
                // 00cccccc 00bbbbCC 00aaBBBB 00AAAAAA

                Vector256<short> merge_ab_and_bc = Avx2.MultiplyAddAdjacent(str.AsByte(), mergeConstant0);
                // 0000kkkk LLllllll 0000JJJJ JJjjKKKK
                // 0000hhhh IIiiiiii 0000GGGG GGggHHHH
                // 0000eeee FFffffff 0000DDDD DDddEEEE
                // 0000bbbb CCcccccc 0000AAAA AAaaBBBB

                Vector256<int> output = Avx2.MultiplyAddAdjacent(merge_ab_and_bc, mergeConstant1);
                // 00000000 JJJJJJjj KKKKkkkk LLllllll
                // 00000000 GGGGGGgg HHHHhhhh IIiiiiii
                // 00000000 DDDDDDdd EEEEeeee FFffffff
                // 00000000 AAAAAAaa BBBBbbbb CCcccccc

                // Pack bytes together in each lane:
                output = Avx2.Shuffle(output.AsSByte(), packBytesInLaneMask).AsInt32();
                // 00000000 00000000 00000000 00000000
                // LLllllll KKKKkkkk JJJJJJjj IIiiiiii
                // HHHHhhhh GGGGGGgg FFffffff EEEEeeee
                // DDDDDDdd CCcccccc BBBBbbbb AAAAAAaa

                // Pack lanes
                str = Avx2.PermuteVar8x32(output, packLanesControl).AsSByte();

                Base64.AssertWrite<Vector256<sbyte>>(dest, destStart, destLength);
                Avx.Store(dest, str.AsByte());

                src += 32;
                dest += 24;
            }
            while (src <= srcEnd);

            srcBytes = (char*)src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(Ssse3))]
        private static unsafe void Vector128Decode<TBase64Decoder>(ref char* srcBytes, ref byte* destBytes, char* srcEnd, int sourceLength, int destLength, ushort* srcStart, byte* destStart)
            where TBase64Decoder : IBase64Decoder
        {
            Debug.Assert((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && BitConverter.IsLittleEndian);

            // If we have Vector128 support, pick off 16 bytes at a time for as long as we can,
            // but make sure that we quit before seeing any == markers at the end of the
            // string. Also, because we write four zeroes at the end of the output, ensure
            // that there are at least 6 valid bytes of input data remaining to close the
            // gap. 16 + 2 + 6 = 24 bytes.

            // The input consists of six character sets in the Base64 alphabet,
            // which we need to map back to the 6-bit values they represent.
            // There are three ranges, two singles, and then there's the rest.
            //
            //  #  From       To        Add  Characters
            //  1  [43]       [62]      +19  +
            //  2  [47]       [63]      +16  /
            //  3  [48..57]   [52..61]   +4  0..9
            //  4  [65..90]   [0..25]   -65  A..Z
            //  5  [97..122]  [26..51]  -71  a..z
            // (6) Everything else => invalid input

            // We will use LUTS for character validation & offset computation
            // Remember that 0x2X and 0x0X are the same index for _mm_shuffle_epi8,
            // this allows to mask with 0x2F instead of 0x0F and thus save one constant declaration (register and/or memory access)

            // For offsets:
            // Perfect hash for lut = ((src>>4)&0x2F)+((src==0x2F)?0xFF:0x00)
            // 0000 = garbage
            // 0001 = /
            // 0010 = +
            // 0011 = 0-9
            // 0100 = A-Z
            // 0101 = A-Z
            // 0110 = a-z
            // 0111 = a-z
            // 1000 >= garbage

            // For validation, here's the table.
            // A character is valid if and only if the AND of the 2 lookups equals 0:

            // hi \ lo              0000 0001 0010 0011 0100 0101 0110 0111 1000 1001 1010 1011 1100 1101 1110 1111
            //      LUT             0x15 0x11 0x11 0x11 0x11 0x11 0x11 0x11 0x11 0x11 0x13 0x1A 0x1B 0x1B 0x1B 0x1A

            // 0000 0X10 char        NUL  SOH  STX  ETX  EOT  ENQ  ACK  BEL   BS   HT   LF   VT   FF   CR   SO   SI
            //           andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10

            // 0001 0x10 char        DLE  DC1  DC2  DC3  DC4  NAK  SYN  ETB  CAN   EM  SUB  ESC   FS   GS   RS   US
            //           andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10

            // 0010 0x01 char               !    "    #    $    %    &    '    (    )    *    +    ,    -    .    /
            //           andlut     0x01 0x01 0x01 0x01 0x01 0x01 0x01 0x01 0x01 0x01 0x01 0x00 0x01 0x01 0x01 0x00

            // 0011 0x02 char          0    1    2    3    4    5    6    7    8    9    :    ;    <    =    >    ?
            //           andlut     0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x02 0x02 0x02 0x02 0x02 0x02

            // 0100 0x04 char          @    A    B    C    D    E    F    G    H    I    J    K    L    M    N    0
            //           andlut     0x04 0x00 0x00 0x00 0X00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00

            // 0101 0x08 char          P    Q    R    S    T    U    V    W    X    Y    Z    [    \    ]    ^    _
            //           andlut     0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x08 0x08 0x08 0x08 0x08

            // 0110 0x04 char          `    a    b    c    d    e    f    g    h    i    j    k    l    m    n    o
            //           andlut     0x04 0x00 0x00 0x00 0X00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00
            // 0111 0X08 char          p    q    r    s    t    u    v    w    x    y    z    {    |    }    ~
            //           andlut     0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x08 0x08 0x08 0x08 0x08

            // 1000 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10
            // 1001 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10
            // 1010 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10
            // 1011 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10
            // 1100 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10
            // 1101 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10
            // 1110 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10
            // 1111 0x10 andlut     0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10 0x10

            // The JIT won't hoist these "constants", so help it
            Vector128<byte> lutHi = Vector128.Create(TBase64Decoder.Vector128LutHigh).AsByte();
            Vector128<byte> lutLo = Vector128.Create(TBase64Decoder.Vector128LutLow).AsByte();
            Vector128<sbyte> lutShift = Vector128.Create(TBase64Decoder.Vector128LutShift).AsSByte();
            Vector128<sbyte> packBytesMask = Vector128.Create(0x06000102, 0x090A0405, 0x0C0D0E08, 0xffffffff).AsSByte();
            Vector128<byte> mergeConstant0 = Vector128.Create(0x01400140).AsByte();
            Vector128<short> mergeConstant1 = Vector128.Create(0x00011000).AsInt16();
            Vector128<byte> one = Vector128.Create((byte)1);
            Vector128<byte> mask2F = Vector128.Create(TBase64Decoder.MaskSlashOrUnderscore);
            Vector128<byte> mask8F = Vector128.Create((byte)0x8F);
            Vector128<byte> shiftForUnderscore = Vector128.Create((byte)33);
            ushort* src = (ushort*)srcBytes;
            byte* dest = destBytes;

            //while (remaining >= 24)
            do
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                Vector128<ushort> utf16VectorLower = Vector128.LoadUnsafe(ref *src);
                Vector128<ushort> utf16VectorUpper = Vector128.LoadUnsafe(ref *src, 8);
                Vector128<byte> str = Vector128.Narrow(utf16VectorLower, utf16VectorUpper);

                // lookup
                Vector128<byte> hiNibbles = Vector128.ShiftRightLogical(str.AsInt32(), 4).AsByte() & mask2F;

                if (!TBase64Decoder.TryDecode128Core(str, hiNibbles, mask2F, mask8F, lutLo, lutHi, lutShift, shiftForUnderscore, out str))
                {
                    break;
                }

                // in, bits, upper case are most significant bits, lower case are least significant bits
                // 00llllll 00kkkkLL 00jjKKKK 00JJJJJJ
                // 00iiiiii 00hhhhII 00ggHHHH 00GGGGGG
                // 00ffffff 00eeeeFF 00ddEEEE 00DDDDDD
                // 00cccccc 00bbbbCC 00aaBBBB 00AAAAAA

                Vector128<short> merge_ab_and_bc;
                if (Ssse3.IsSupported)
                {
                    merge_ab_and_bc = Ssse3.MultiplyAddAdjacent(str.AsByte(), mergeConstant0.AsSByte());
                }
                else
                {
                    Vector128<ushort> evens = AdvSimd.ShiftLeftLogicalWideningLower(AdvSimd.Arm64.UnzipEven(str, one).GetLower(), 6);
                    Vector128<ushort> odds = AdvSimd.Arm64.TransposeOdd(str, Vector128<byte>.Zero).AsUInt16();
                    merge_ab_and_bc = Vector128.Add(evens, odds).AsInt16();
                }
                // 0000kkkk LLllllll 0000JJJJ JJjjKKKK
                // 0000hhhh IIiiiiii 0000GGGG GGggHHHH
                // 0000eeee FFffffff 0000DDDD DDddEEEE
                // 0000bbbb CCcccccc 0000AAAA AAaaBBBB

                Vector128<int> output;
                if (Ssse3.IsSupported)
                {
                    output = Sse2.MultiplyAddAdjacent(merge_ab_and_bc, mergeConstant1);
                }
                else
                {
                    Vector128<int> ievens = AdvSimd.ShiftLeftLogicalWideningLower(AdvSimd.Arm64.UnzipEven(merge_ab_and_bc, one.AsInt16()).GetLower(), 12);
                    Vector128<int> iodds = AdvSimd.Arm64.TransposeOdd(merge_ab_and_bc, Vector128<short>.Zero).AsInt32();
                    output = Vector128.Add(ievens, iodds).AsInt32();
                }
                // 00000000 JJJJJJjj KKKKkkkk LLllllll
                // 00000000 GGGGGGgg HHHHhhhh IIiiiiii
                // 00000000 DDDDDDdd EEEEeeee FFffffff
                // 00000000 AAAAAAaa BBBBbbbb CCcccccc

                // Pack bytes together:
                str = SimdShuffle(output.AsByte(), packBytesMask.AsByte(), mask8F);
                // 00000000 00000000 00000000 00000000
                // LLllllll KKKKkkkk JJJJJJjj IIiiiiii
                // HHHHhhhh GGGGGGgg FFffffff EEEEeeee
                // DDDDDDdd CCcccccc BBBBbbbb AAAAAAaa

                Base64.AssertWrite<Vector128<sbyte>>(dest, destStart, destLength);
                str.Store(dest);

                src += 16;
                dest += 12;
            }
            while (src <= srcEnd);

            srcBytes = (char*)src;
            destBytes = dest;
        }

        [Conditional("DEBUG")]
        private static unsafe void AssertRead<TVector>(ushort* src, ushort* srcStart, int srcLength)
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

        internal static OperationStatus DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(ReadOnlySpan<char> utf8, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
            where TBase64Decoder : IBase64Decoder
        {
            const int BlockSize = 4;
            Span<char> buffer = stackalloc char[BlockSize];
            OperationStatus status = OperationStatus.Done;

            while (!utf8.IsEmpty)
            {
                int encodedIdx = 0;
                int bufferIdx = 0;
                int skipped = 0;

                for (; encodedIdx < utf8.Length && (uint)bufferIdx < (uint)buffer.Length; ++encodedIdx)
                {
                    if (IsWhiteSpace(utf8[encodedIdx]))
                    {
                        skipped++;
                    }
                    else
                    {
                        buffer[bufferIdx] = utf8[encodedIdx];
                        bufferIdx++;
                    }
                }

                utf8 = utf8.Slice(encodedIdx);
                bytesConsumed += skipped;

                if (bufferIdx == 0)
                {
                    continue;
                }

                bool hasAnotherBlock = utf8.Length >= BlockSize && bufferIdx == BlockSize;
                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount(ref buffer[^1]);
                    if (paddingCount > 0)
                    {
                        hasAnotherBlock = false;
                        localIsFinalBlock = true;
                    }
                }

                if (localIsFinalBlock && !isFinalBlock)
                {
                    localIsFinalBlock = false;
                }

                status = DecodeFromChars<TBase64Decoder>(buffer.Slice(0, bufferIdx), bytes, out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);
                bytesConsumed += localConsumed;
                bytesWritten += localWritten;

                if (status != OperationStatus.Done)
                {
                    return status;
                }

                // The remaining data must all be whitespace in order to be valid.
                if (!hasAnotherBlock)
                {
                    for (int i = 0; i < utf8.Length; ++i)
                    {
                        if (!IsWhiteSpace(utf8[i]))
                        {
                            // Revert previous dest increment, since an invalid state followed.
                            bytesConsumed -= localConsumed;
                            bytesWritten -= localWritten;

                            return OperationStatus.InvalidData;
                        }

                        bytesConsumed++;
                    }

                    break;
                }

                bytes = bytes.Slice(localWritten);
            }

            return status;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPaddingCount(ref char ptrToLastElement)
        {
            int padding = 0;

            if (ptrToLastElement == EncodingPad) padding++;
            if (Unsafe.Subtract(ref ptrToLastElement, 1) == EncodingPad) padding++;

            return padding;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<char> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (!IsWhiteSpace(span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int Decode(char* encodedBytes, ref sbyte decodingMap)
        {
            uint t0 = encodedBytes[0];
            uint t1 = encodedBytes[1];
            uint t2 = encodedBytes[2];
            uint t3 = encodedBytes[3];

            if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
            {
                return -1; // One or more chars falls outside the 00..ff range, invalid Base64Url character.
            }

            int i0 = Unsafe.Add(ref decodingMap, t0);
            int i1 = Unsafe.Add(ref decodingMap, t1);
            int i2 = Unsafe.Add(ref decodingMap, t2);
            int i3 = Unsafe.Add(ref decodingMap, t3);

            i0 <<= 18;
            i1 <<= 12;
            i2 <<= 6;

            i0 |= i3;
            i1 |= i2;

            i0 |= i1;
            return i0;
        }

        public static int DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);

            if (OperationStatus.Done == status)
            {
                return bytesWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException("DestinationTooSmall", nameof(destination));
            }

            throw new InvalidOperationException("InvalidData");
        }

        public static bool TryDecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out bytesWritten);

            return OperationStatus.Done == status;
        }

        public static byte[] DecodeFromChars(ReadOnlySpan<char> source)
        {
            Span<byte> destination = stackalloc byte[GetMaxDecodedLength(source.Length)];
            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);

            return OperationStatus.Done == status ? destination.Slice(0, bytesWritten).ToArray() :
                throw new InvalidOperationException("InvalidData");
        }

        private readonly struct Base64UrlDecoder : IBase64Decoder
        {
            public static ReadOnlySpan<sbyte> DecodingMap =>
                [
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1,         //62 is placed at index 45 (for -), 63 at index 95 (for _)
                    52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         //52-61 are placed at index 48-57 (for 0-9)
                    -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
                    15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, 63,         //0-25 are placed at index 65-90 (for A-Z)
                    -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
                    41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,         //26-51 are placed at index 97-122 (for a-z)
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Bytes over 122 ('z') are invalid and cannot be decoded
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Hence, padding the map with 255, which indicates invalid input
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                ];

            public static ReadOnlySpan<uint> VbmiLookup0 =>
                [
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80803e80,
                    0x37363534, 0x3b3a3938, 0x80803d3c, 0x80808080
                ];

            public static ReadOnlySpan<uint> VbmiLookup1 =>
                [
                    0x02010080, 0x06050403, 0x0a090807, 0x0e0d0c0b,
                    0x1211100f, 0x16151413, 0x80191817, 0x3f808080,
                    0x1c1b1a80, 0x201f1e1d, 0x24232221, 0x28272625,
                    0x2c2b2a29, 0x302f2e2d, 0x80333231, 0x80808080
                ];

            public static ReadOnlySpan<sbyte> Avx2LutHigh =>
                [
                    0x00, 0x00, 0x2d, 0x39,
                    0x4f, 0x5a, 0x6f, 0x7a,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x2d, 0x39,
                    0x4f, 0x5a, 0x6f, 0x7a,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00
                ];

            public static ReadOnlySpan<sbyte> Avx2LutLow =>
                [
                    0x01, 0x01, 0x2d, 0x30,
                    0x41, 0x50, 0x61, 0x70,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x2d, 0x30,
                    0x41, 0x50, 0x61, 0x70,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01
                ];

            public static ReadOnlySpan<sbyte> Avx2LutShift =>
                [
                    0,   0,  17,   4,
                  -65, -65, -71, -71,
                    0,   0,   0,   0,
                    0,   0,   0,   0,
                    0,   0,  17,   4,
                  -65, -65, -71, -71,
                    0,   0,   0,   0,
                    0,   0,   0,   0
                ];

            public static byte MaskSlashOrUnderscore => (byte)'_'; // underscore

            public static ReadOnlySpan<int> Vector128LutHigh => [ 0x392d0000, 0x7a6f5a4f, 0x00000000, 0x00000000 ];

            public static ReadOnlySpan<int> Vector128LutLow => [0x302d0101, 0x70615041, 0x01010101, 0x01010101];

            public static ReadOnlySpan<uint> Vector128LutShift => [0x04110000, 0xb9b9bfbf, 0x00000000, 0x00000000];

            public static int GetMaxDecodedLength(int utf8Length) => Base64Url.GetMaxDecodedLength(utf8Length);

            public static bool IsInValidLength(int bufferLength) => bufferLength % 4 == 1; // Should we fail here? One byte cannot be decoded completely

            public static int SrcLength(bool isFinalBlock, int utf8Length) => isFinalBlock ? utf8Length : utf8Length & ~0x3;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            public static bool TryDecode128Core(
                Vector128<byte> str,
                Vector128<byte> hiNibbles,
                Vector128<byte> maskSlashOrUnderscore,
                Vector128<byte> mask8F,
                Vector128<byte> lutLow,
                Vector128<byte> lutHigh,
                Vector128<sbyte> lutShift,
                Vector128<byte> shiftForUnderscore,
                out Vector128<byte> result)
            {
                Vector128<byte> lowerBound = SimdShuffle(lutLow, hiNibbles, mask8F);
                Vector128<byte> upperBound = SimdShuffle(lutHigh, hiNibbles, mask8F);

                Vector128<byte> below = Vector128.LessThan(str, lowerBound);
                Vector128<byte> above = Vector128.GreaterThan(str, upperBound);
                Vector128<byte> eq5F = Vector128.Equals(str, maskSlashOrUnderscore);

                // Take care as arguments are flipped in order!
                Vector128<byte> outside = Vector128.AndNot(below | above, eq5F);

                if (outside != Vector128<byte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector128<byte> shift = SimdShuffle(lutShift.AsByte(), hiNibbles, mask8F);
                str += shift;

                result = str + (eq5F & shiftForUnderscore);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public static bool TryDecode256Core(
                Vector256<sbyte> str,
                Vector256<sbyte> hiNibbles,
                Vector256<sbyte> maskSlashOrUnderscore,
                Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh,
                Vector256<sbyte> lutShift,
                Vector256<sbyte> shiftForUnderscore,
                out Vector256<sbyte> result)
            {
                Vector256<sbyte> lowerBound = Avx2.Shuffle(lutLow, hiNibbles);
                Vector256<sbyte> upperBound = Avx2.Shuffle(lutHigh, hiNibbles);

                Vector256<sbyte> below = Vector256.LessThan(str, lowerBound);
                Vector256<sbyte> above = Vector256.GreaterThan(str, upperBound);
                Vector256<sbyte> eq5F = Vector256.Equals(str, maskSlashOrUnderscore);

                // Take care as arguments are flipped in order!
                Vector256<sbyte> outside = Vector256.AndNot(below | above, eq5F);

                if (outside != Vector256<sbyte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector256<sbyte> shift = Avx2.Shuffle(lutShift, hiNibbles);
                str += shift;

                result = str + (eq5F & shiftForUnderscore);
                return true;
            }
        }
    }
}
