// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers.Text
{
    // AVX2 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/avx2
    // Vector128 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/ssse3

    public static partial class Base64
    {
        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as base64 into binary data.
        /// If the input is not a multiple of 4, it will decode as much as it can, to the closest multiple of 4.
        /// </summary>
        /// <param name="utf8">The input span which contains UTF-8 encoded text in base64 that needs to be decoded.</param>
        /// <param name="bytes">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to encode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to encode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the decoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is false and the input is not a multiple of 4, otherwise the partial input would be considered as InvalidData
        /// - InvalidData - if the input contains bytes outside of the expected base64 range, or if it contains invalid/more than two padding characters,
        ///   or if the input is incomplete (i.e. not a multiple of 4) and <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// </returns>
        public static unsafe OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            if (utf8.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* srcBytes = &MemoryMarshal.GetReference(utf8))
            fixed (byte* destBytes = &MemoryMarshal.GetReference(bytes))
            {
                int srcLength = utf8.Length & ~0x3;  // only decode input up to the closest multiple of 4.
                int destLength = bytes.Length;
                int maxSrcLength = srcLength;
                int decodedLength = GetMaxDecodedFromUtf8Length(srcLength);

                // max. 2 padding chars
                if (destLength < decodedLength - 2)
                {
                    // For overflow see comment below
                    maxSrcLength = destLength / 3 * 4;
                }

                byte* src = srcBytes;
                byte* dest = destBytes;
                byte* srcEnd = srcBytes + (uint)srcLength;
                byte* srcMax = srcBytes + (uint)maxSrcLength;

                if (maxSrcLength >= 24)
                {
                    byte* end = srcMax - 45;
                    if (Avx2.IsSupported && (end >= src))
                    {
                        Avx2Decode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
                    }

                    end = srcMax - 24;
                    if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && BitConverter.IsLittleEndian && (end >= src))
                    {
                        Vector128Decode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
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
                }

                ref sbyte decodingMap = ref MemoryMarshal.GetReference(DecodingMap);
                srcMax = srcBytes + maxSrcLength;

                while (src < srcMax)
                {
                    int result = Decode(src, ref decodingMap);

                    if (result < 0)
                        goto InvalidDataExit;

                    WriteThreeLowOrderBytes(dest, result);
                    src += 4;
                    dest += 3;
                }

                if (maxSrcLength != srcLength - skipLastChunk)
                    goto DestinationTooSmallExit;

                // If input is less than 4 bytes, srcLength == sourceIndex == 0
                // If input is not a multiple of 4, sourceIndex == srcLength != 0
                if (src == srcEnd)
                {
                    if (isFinalBlock)
                        goto InvalidDataExit;

                    if (src == srcBytes + utf8.Length)
                        goto DoneExit;

                    goto NeedMoreDataExit;
                }

                // if isFinalBlock is false, we will never reach this point

                // Handle last four bytes. There are 0, 1, 2 padding chars.
                uint t0 = srcEnd[-4];
                uint t1 = srcEnd[-3];
                uint t2 = srcEnd[-2];
                uint t3 = srcEnd[-1];

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
                        goto InvalidDataExit;
                    if (dest + 3 > destMax)
                        goto DestinationTooSmallExit;

                    WriteThreeLowOrderBytes(dest, i0);
                    dest += 3;
                }
                else if (t2 != EncodingPad)
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);

                    i2 <<= 6;

                    i0 |= i2;

                    if (i0 < 0)
                        goto InvalidDataExit;
                    if (dest + 2 > destMax)
                        goto DestinationTooSmallExit;

                    dest[0] = (byte)(i0 >> 16);
                    dest[1] = (byte)(i0 >> 8);
                    dest += 2;
                }
                else
                {
                    if (i0 < 0)
                        goto InvalidDataExit;
                    if (dest + 1 > destMax)
                        goto DestinationTooSmallExit;

                    dest[0] = (byte)(i0 >> 16);
                    dest += 1;
                }

                src += 4;

                if (srcLength != utf8.Length)
                    goto InvalidDataExit;

            DoneExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.Done;

            DestinationTooSmallExit:
                if (srcLength != utf8.Length && isFinalBlock)
                    goto InvalidDataExit; // if input is not a multiple of 4, and there is no more data, return invalid data instead

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
                return OperationStatus.InvalidData;
            }
        }

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to deocde base 64 encoded text within a byte span of size "length".
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is less than 0.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxDecodedFromUtf8Length(int length)
        {
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            return (length >> 2) * 3;
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text in base 64 (in-place) into binary data.
        /// The decoded binary output is smaller than the text data contained in the input (the operation deflates the data).
        /// If the input is not a multiple of 4, it will not decode any.
        /// </summary>
        /// <param name="buffer">The input span which contains the base 64 text data that needs to be decoded.</param>
        /// <param name="bytesWritten">The number of bytes written into the buffer.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - InvalidData - if the input contains bytes outside of the expected base 64 range, or if it contains invalid/more than two padding characters,
        ///   or if the input is incomplete (i.e. not a multiple of 4).
        /// It does not return DestinationTooSmall since that is not possible for base 64 decoding.
        /// It does not return NeedMoreData since this method tramples the data in the buffer and
        /// hence can only be called once with all the data in the buffer.
        /// </returns>
        public static unsafe OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten)
        {
            if (buffer.IsEmpty)
            {
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* bufferBytes = &MemoryMarshal.GetReference(buffer))
            {
                int bufferLength = buffer.Length;
                uint sourceIndex = 0;
                uint destIndex = 0;

                // only decode input if it is a multiple of 4
                if (bufferLength != ((bufferLength >> 2) * 4))
                    goto InvalidExit;
                if (bufferLength == 0)
                    goto DoneExit;

                ref sbyte decodingMap = ref MemoryMarshal.GetReference(DecodingMap);

                while (sourceIndex < bufferLength - 4)
                {
                    int result = Decode(bufferBytes + sourceIndex, ref decodingMap);
                    if (result < 0)
                        goto InvalidExit;
                    WriteThreeLowOrderBytes(bufferBytes + destIndex, result);
                    destIndex += 3;
                    sourceIndex += 4;
                }

                uint t0 = bufferBytes[bufferLength - 4];
                uint t1 = bufferBytes[bufferLength - 3];
                uint t2 = bufferBytes[bufferLength - 2];
                uint t3 = bufferBytes[bufferLength - 1];

                int i0 = Unsafe.Add(ref decodingMap, (IntPtr)t0);
                int i1 = Unsafe.Add(ref decodingMap, (IntPtr)t1);

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;

                if (t3 != EncodingPad)
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);
                    int i3 = Unsafe.Add(ref decodingMap, (IntPtr)t3);

                    i2 <<= 6;

                    i0 |= i3;
                    i0 |= i2;

                    if (i0 < 0)
                        goto InvalidExit;

                    WriteThreeLowOrderBytes(bufferBytes + destIndex, i0);
                    destIndex += 3;
                }
                else if (t2 != EncodingPad)
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);

                    i2 <<= 6;

                    i0 |= i2;

                    if (i0 < 0)
                        goto InvalidExit;

                    bufferBytes[destIndex] = (byte)(i0 >> 16);
                    bufferBytes[destIndex + 1] = (byte)(i0 >> 8);
                    destIndex += 2;
                }
                else
                {
                    if (i0 < 0)
                        goto InvalidExit;

                    bufferBytes[destIndex] = (byte)(i0 >> 16);
                    destIndex += 1;
                }

            DoneExit:
                bytesWritten = (int)destIndex;
                return OperationStatus.Done;

            InvalidExit:
                bytesWritten = (int)destIndex;
                return OperationStatus.InvalidData;
            }
        }

        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Avx2Decode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
        {
            // If we have AVX2 support, pick off 32 bytes at a time for as long as we can,
            // but make sure that we quit before seeing any == markers at the end of the
            // string. Also, because we write 8 zeroes at the end of the output, ensure
            // that there are at least 11 valid bytes of input data remaining to close the
            // gap. 32 + 2 + 11 = 45 bytes.

            // See SSSE3-version below for an explanation of how the code works.

            // The JIT won't hoist these "constants", so help it
            Vector256<sbyte> lutHi = Vector256.Create(
                0x10, 0x10, 0x01, 0x02,
                0x04, 0x08, 0x04, 0x08,
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x01, 0x02,
                0x04, 0x08, 0x04, 0x08,
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10);

            Vector256<sbyte> lutLo = Vector256.Create(
                0x15, 0x11, 0x11, 0x11,
                0x11, 0x11, 0x11, 0x11,
                0x11, 0x11, 0x13, 0x1A,
                0x1B, 0x1B, 0x1B, 0x1A,
                0x15, 0x11, 0x11, 0x11,
                0x11, 0x11, 0x11, 0x11,
                0x11, 0x11, 0x13, 0x1A,
                0x1B, 0x1B, 0x1B, 0x1A);

            Vector256<sbyte> lutShift = Vector256.Create(
                 0, 16, 19, 4,
                -65, -65, -71, -71,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 16, 19, 4,
                -65, -65, -71, -71,
                0, 0, 0, 0,
                0, 0, 0, 0);

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

            Vector256<sbyte> mask2F = Vector256.Create((sbyte)'/');
            Vector256<sbyte> mergeConstant0 = Vector256.Create(0x01400140).AsSByte();
            Vector256<short> mergeConstant1 = Vector256.Create(0x00011000).AsInt16();

            byte* src = srcBytes;
            byte* dest = destBytes;

            //while (remaining >= 45)
            do
            {
                AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
                Vector256<sbyte> str = Avx.LoadVector256(src).AsSByte();

                Vector256<sbyte> hiNibbles = Avx2.And(Avx2.ShiftRightLogical(str.AsInt32(), 4).AsSByte(), mask2F);
                Vector256<sbyte> loNibbles = Avx2.And(str, mask2F);
                Vector256<sbyte> hi = Avx2.Shuffle(lutHi, hiNibbles);
                Vector256<sbyte> lo = Avx2.Shuffle(lutLo, loNibbles);

                if (!Avx.TestZ(lo, hi))
                    break;

                Vector256<sbyte> eq2F = Avx2.CompareEqual(str, mask2F);
                Vector256<sbyte> shift = Avx2.Shuffle(lutShift, Avx2.Add(eq2F, hiNibbles));
                str = Avx2.Add(str, shift);

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

                AssertWrite<Vector256<sbyte>>(dest, destStart, destLength);
                Avx.Store(dest, str.AsByte());

                src += 32;
                dest += 24;
            }
            while (src <= srcEnd);

            srcBytes = src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> SimdShuffle(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask8F)
        {
            Debug.Assert((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && BitConverter.IsLittleEndian);

            if (AdvSimd.Arm64.IsSupported)
            {
                right &= mask8F;
            }

            return Vector128.ShuffleUnsafe(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Vector128Decode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
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
            Vector128<byte>  lutHi = Vector128.Create(0x02011010, 0x08040804, 0x10101010, 0x10101010).AsByte();
            Vector128<byte>  lutLo = Vector128.Create(0x11111115, 0x11111111, 0x1A131111, 0x1A1B1B1B).AsByte();
            Vector128<sbyte> lutShift = Vector128.Create(0x04131000, 0xb9b9bfbf, 0x00000000, 0x00000000).AsSByte();
            Vector128<sbyte> packBytesMask = Vector128.Create(0x06000102, 0x090A0405, 0x0C0D0E08, 0xffffffff).AsSByte();
            Vector128<byte>  mergeConstant0 = Vector128.Create(0x01400140).AsByte();
            Vector128<short> mergeConstant1 = Vector128.Create(0x00011000).AsInt16();
            Vector128<byte>  one = Vector128.Create((byte)1);
            Vector128<byte>  mask2F = Vector128.Create((byte)'/');
            Vector128<byte>  mask8F = Vector128.Create((byte)0x8F);

            byte* src = srcBytes;
            byte* dest = destBytes;

            //while (remaining >= 24)
            do
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                Vector128<byte> str = Vector128.LoadUnsafe(ref *src);

                // lookup
                Vector128<byte> hiNibbles = Vector128.ShiftRightLogical(str.AsInt32(), 4).AsByte() & mask2F;
                Vector128<byte> loNibbles = str & mask2F;
                Vector128<byte> hi = SimdShuffle(lutHi, hiNibbles, mask8F);
                Vector128<byte> lo = SimdShuffle(lutLo, loNibbles, mask8F);

                // Check for invalid input: if any "and" values from lo and hi are not zero,
                // fall back on bytewise code to do error checking and reporting:
                if ((lo & hi) != Vector128<byte>.Zero)
                    break;

                Vector128<byte> eq2F = Vector128.Equals(str, mask2F);
                Vector128<byte> shift = SimdShuffle(lutShift.AsByte(), (eq2F + hiNibbles), mask8F);

                // Now simply add the delta values to the input:
                str += shift;

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

                AssertWrite<Vector128<sbyte>>(dest, destStart, destLength);
                str.Store(dest);

                src += 16;
                dest += 12;
            }
            while (src <= srcEnd);

            srcBytes = src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int Decode(byte* encodedBytes, ref sbyte decodingMap)
        {
            uint t0 = encodedBytes[0];
            uint t1 = encodedBytes[1];
            uint t2 = encodedBytes[2];
            uint t3 = encodedBytes[3];

            int i0 = Unsafe.Add(ref decodingMap, (IntPtr)t0);
            int i1 = Unsafe.Add(ref decodingMap, (IntPtr)t1);
            int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);
            int i3 = Unsafe.Add(ref decodingMap, (IntPtr)t3);

            i0 <<= 18;
            i1 <<= 12;
            i2 <<= 6;

            i0 |= i3;
            i1 |= i2;

            i0 |= i1;
            return i0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteThreeLowOrderBytes(byte* destination, int value)
        {
            destination[0] = (byte)(value >> 16);
            destination[1] = (byte)(value >> 8);
            destination[2] = (byte)(value);
        }

        // Pre-computing this table using a custom string(s_characters) and GenerateDecodingMapAndVerify (found in tests)
        private static ReadOnlySpan<sbyte> DecodingMap => new sbyte[] {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,         //62 is placed at index 43 (for +), 63 at index 47 (for /)
            52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         //52-61 are placed at index 48-57 (for 0-9), 64 at index 61 (for =)
            -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,         //0-25 are placed at index 65-90 (for A-Z)
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
        };
    }
}
