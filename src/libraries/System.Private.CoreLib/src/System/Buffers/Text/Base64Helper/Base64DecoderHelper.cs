// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics.CodeAnalysis;

#if NET
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
#endif

namespace System.Buffers.Text
{
    // AVX2 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/avx2
    // Vector128 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/ssse3
    internal static partial class Base64Helper
    {
#if NET
        private const int Avx512DecodeInputLength = 64;
        private const int Avx512DecodeOutputLength = 48;
        private const int Avx512DecodeStoreLength = 64;
        private const int Avx512DecodeMinInputLength = 88; // 64 input elements + 24 to cover the store overshoot and padding.
        private const int Avx512DecodePairMinInputLength = Avx512DecodeInputLength + Avx512DecodeMinInputLength;
        private const int Avx512DecodePairMinDestinationLength = Avx512DecodeOutputLength + Avx512DecodeStoreLength;
        private const int Avx2DecodeInputLength = 32;
        private const int Avx2DecodeOutputLength = 24;
        private const int Avx2DecodeStoreLength = 32;
        private const int Avx2DecodeMinInputLength = 45; // 32 input elements + 13 to cover the store overshoot and padding.
        private const int Vector128DecodeInputLength = 16;
        private const int Vector128DecodeOutputLength = 12;
        private const int Vector128DecodeStoreLength = 16;
        private const int Vector128DecodeMinInputLength = 24; // 16 input elements + 8 to cover the store overshoot and padding.
        private const int AdvSimdDecodeInputLength = 64;
        private const int AdvSimdDecodeOutputLength = 48;
        private const int AdvSimdDecodeMinInputLength = AdvSimdDecodeInputLength + 2; // Leave possible padding for scalar decoding.
        private const int Avx512DecodeTailInputLength = 12;
        private const int Avx512DecodeTailOutputLength = 9;
#endif

        internal static byte[] DecodeToArray<TBase64Decoder, T>(TBase64Decoder decoder, ReadOnlySpan<T> source)
            where TBase64Decoder : IBase64Decoder<T>
            where T : unmanaged
        {
            if (source.IsEmpty)
            {
                return Array.Empty<byte>();
            }

            int upperBound = decoder.GetMaxDecodedLength(source.Length);
            byte[]? rented = null;
            scoped Span<byte> destination;
#if NET
            SmallDecodingBuffer smallBuffer;
            DecodingBuffer buffer;
            if ((uint)upperBound <= SmallDecodingBufferLength)
            {
                smallBuffer = default;
                destination = smallBuffer;
                destination = destination.Slice(SmallDecodingBufferLength - upperBound);
            }
            else if ((uint)upperBound <= MaxStackallocThreshold)
            {
                buffer = default;
                destination = buffer;
            }
#else
            Span<byte> buffer = stackalloc byte[MaxStackallocThreshold];
            if ((uint)upperBound <= MaxStackallocThreshold)
            {
                destination = buffer;
            }
#endif
            else
            {
                destination = rented = ArrayPool<byte>.Shared.Rent(upperBound);
            }

            OperationStatus status = DecodeFrom(decoder, source, destination, out _, out int bytesWritten, isFinalBlock: true, ignoreWhiteSpace: true);
            Debug.Assert(status is OperationStatus.Done or OperationStatus.InvalidData);
            byte[] result = destination.Slice(0, bytesWritten).ToArray();
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            if (status != OperationStatus.Done)
            {
                throw new FormatException(SR.Format_BadBase64Char);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static OperationStatus DecodeFrom<TBase64Decoder, T>(TBase64Decoder decoder, ReadOnlySpan<T> source, Span<byte> bytes,
            out int bytesConsumed, out int bytesWritten, bool isFinalBlock, bool ignoreWhiteSpace)
            where TBase64Decoder : IBase64Decoder<T>
            where T : unmanaged
        {
            if (source.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            return DecodeFromCore(decoder, source, bytes, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace);
        }

        private static OperationStatus DecodeFromCore<TBase64Decoder, T>(TBase64Decoder decoder, ReadOnlySpan<T> source, Span<byte> bytes,
            out int bytesConsumed, out int bytesWritten, bool isFinalBlock, bool ignoreWhiteSpace)
            where TBase64Decoder : IBase64Decoder<T>
            where T : unmanaged
        {
            int srcLength = decoder.SrcLength(isFinalBlock, source.Length);
            int destLength = bytes.Length;
            int maxSrcLength = srcLength;
            int decodedLength = decoder.GetMaxDecodedLength(srcLength);

            // max. 2 padding chars
            if (destLength < decodedLength - 2)
            {
                // For overflow see comment below
                maxSrcLength = destLength / 3 * 4;
            }

            ReadOnlySpan<T> src = source;
            Span<byte> dest = bytes;
            int srcEnd = source.Length - srcLength;
            int srcMax;

#if NET
            if (maxSrcLength >= Vector128DecodeInputLength)
            {
                if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
                {
                    if (src.Length >= Vector128DecodeInputLength && dest.Length >= Avx512DecodeTailOutputLength)
                    {
                        Avx512Decode(decoder, ref src, ref dest);
                    }

                    goto Scalar;
                }

                if (Avx2.IsSupported && src.Length >= Avx2DecodeMinInputLength && dest.Length >= Avx2DecodeStoreLength)
                {
                    Avx2Decode(decoder, ref src, ref dest);
                }

                if (AdvSimd.Arm64.IsSupported && src.Length >= AdvSimdDecodeMinInputLength && dest.Length >= AdvSimdDecodeOutputLength)
                {
                    AdvSimdDecode(decoder, ref src, ref dest);
                }

                if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported) &&
                    BitConverter.IsLittleEndian &&
                    src.Length >= Vector128DecodeMinInputLength &&
                    dest.Length >= Vector128DecodeStoreLength)
                {
                    Vector128Decode(decoder, ref src, ref dest);
                }
            }
        Scalar:
#endif

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
#if NET
                (maxSrcLength, int remainder) = int.DivRem(destLength, 3);
                maxSrcLength *= 4;
#else
                maxSrcLength = (destLength / 3) * 4;
                int remainder = (int)((uint)destLength % 3);
#endif
                if (isFinalBlock && remainder > 0)
                {
                    srcLength &= ~0x3; // In case of Base64UrlDecoder source can be not a multiple of 4, round down to multiple of 4
                }
            }

            ReadOnlySpan<sbyte> decodingMap = decoder.DecodingMap;
            srcMax = Math.Max(3, source.Length - maxSrcLength);

            while (src.Length > srcMax && dest.Length >= 3)
            {
                int result = decoder.DecodeFourElements(src, decodingMap);

                if (result < 0)
                {
                    goto InvalidDataExit;
                }

                WriteThreeLowOrderBytes(dest, result);
                src = src.Slice(4);
                dest = dest.Slice(3);
            }

            if (maxSrcLength != srcLength - skipLastChunk)
            {
                goto DestinationTooSmallExit;
            }

            if (src.Length == srcEnd)
            {
                if (isFinalBlock)
                {
                    goto InvalidDataExit;
                }

                if (src.IsEmpty)
                {
                    goto DoneExit;
                }

                goto NeedMoreDataExit;
            }

            // if isFinalBlock is false, we will never reach this point
            // Handle remaining bytes, for Base64 its always 4 bytes, for Base64Url up to 8 bytes left.
            // If more than 4 bytes remained it will end up in DestinationTooSmallExit or InvalidDataExit (might succeed after whitespace removed)
            int remaining = src.Length - srcEnd;
            Debug.Assert(typeof(TBase64Decoder) == typeof(Base64DecoderByte) ? remaining == 4 : remaining < 8);
            int decodeLength = remaining;
            if (typeof(TBase64Decoder) == typeof(Base64DecoderByte) || typeof(TBase64Decoder) == typeof(Base64DecoderChar))
            {
                decodeLength = 4;
            }
            int i0 = decoder.DecodeRemaining(src, decodingMap, decodeLength, out uint t2, out uint t3);

            if (i0 < 0)
            {
                goto InvalidDataExit;
            }

            if (!decoder.IsValidPadding(t3))
            {
                int i2 = decodingMap[(byte)t2];
                int i3 = decodingMap[(byte)t3];

                i2 <<= 6;

                i0 |= i3;
                i0 |= i2;

                if (i0 < 0)
                {
                    goto InvalidDataExit;
                }

                if (dest.Length < 3)
                {
                    goto DestinationTooSmallExit;
                }

                WriteThreeLowOrderBytes(dest, i0);
                dest = dest.Slice(3);
                src = src.Slice(4);
            }
            else if (!decoder.IsValidPadding(t2))
            {
                int i2 = decodingMap[(byte)t2];

                i2 <<= 6;

                i0 |= i2;

                if ((i0 & 0x800000c0) != 0) // if negative or 2 unused bits are not 0.
                {
                    goto InvalidDataExit;
                }

                if (dest.Length < 2)
                {
                    goto DestinationTooSmallExit;
                }

                dest[0] = (byte)(i0 >> 16);
                dest[1] = (byte)(i0 >> 8);
                dest = dest.Slice(2);
                src = src.Slice(remaining);
            }
            else
            {
                if ((i0 & 0x8000F000) != 0) // if negative or 4 unused bits are not 0.
                {
                    goto InvalidDataExit;
                }

                if (dest.Length < 1)
                {
                    goto DestinationTooSmallExit;
                }

                dest[0] = (byte)(i0 >> 16);
                dest = dest.Slice(1);
                src = src.Slice(remaining);
            }

            if (srcLength != source.Length)
            {
                goto InvalidDataExit;
            }

        DoneExit:
            bytesConsumed = source.Length - src.Length;
            bytesWritten = destLength - dest.Length;
            return OperationStatus.Done;

        DestinationTooSmallExit:
            if (srcLength != source.Length && isFinalBlock)
            {
                goto InvalidDataExit; // if input is not a multiple of 4, and there is no more data, return invalid data instead
            }

            if (ignoreWhiteSpace)
            {
                // Fall through to InvalidDataFallback which strips whitespace and re-evaluates destination size requirement
                goto InvalidDataExit;
            }

            bytesConsumed = source.Length - src.Length;
            bytesWritten = destLength - dest.Length;
            return OperationStatus.DestinationTooSmall;

        NeedMoreDataExit:
            bytesConsumed = source.Length - src.Length;
            bytesWritten = destLength - dest.Length;
            return OperationStatus.NeedMoreData;

        InvalidDataExit:
            bytesConsumed = source.Length - src.Length;
            bytesWritten = destLength - dest.Length;
            return ignoreWhiteSpace ?
                InvalidDataFallback(decoder, source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock) :
                OperationStatus.InvalidData;

            static OperationStatus InvalidDataFallback(TBase64Decoder decoder, ReadOnlySpan<T> source, Span<byte> bytes,
                ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock)
            {
                source = source.Slice(bytesConsumed);
                bytes = bytes.Slice(bytesWritten);

                OperationStatus status;
                do
                {
                    int localConsumed = decoder.IndexOfAnyExceptWhiteSpace(source);
                    if (localConsumed < 0)
                    {
                        // The remainder of the input is all whitespace. Mark it all as having been consumed,
                        // and mark the operation as being done.
                        bytesConsumed += source.Length;
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
                        return decoder.DecodeWithWhiteSpaceBlockwiseWrapper(decoder, source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
                    }

                    // Skip over the starting whitespace and continue.
                    bytesConsumed += localConsumed;
                    source = source.Slice(localConsumed);

                    // Try again after consumed whitespace
                    status = DecodeFrom(decoder, source, bytes, out localConsumed, out int localWritten, isFinalBlock, ignoreWhiteSpace: false);
                    bytesConsumed += localConsumed;
                    bytesWritten += localWritten;

                    if (status is OperationStatus.Done or OperationStatus.NeedMoreData)
                    {
                        break;
                    }

                    // The DecodeFrom helper will return DestinationTooSmall if the destination is too small,
                    // regardless of whether it's actually too small once you skip whitespace characters.
                    // In that case we loop again and fall back to block-wise decoding if we can't make progress.

                    source = source.Slice(localConsumed);
                    bytes = bytes.Slice(localWritten);
                }
                while (!source.IsEmpty);

                return status;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static OperationStatus DecodeFromUtf8InPlace<TBase64Decoder>(TBase64Decoder decoder, Span<byte> buffer,
            out int bytesWritten, bool ignoreWhiteSpace)
            where TBase64Decoder : IBase64Decoder<byte>
        {
            if (!decoder.IsInvalidLength(buffer.Length))
            {
                if (buffer.IsEmpty)
                {
                    bytesWritten = 0;
                    return OperationStatus.Done;
                }

                if (buffer.Length <= 4)
                {
                    OperationStatus status = DecodeInPlaceTail(decoder, buffer, buffer, decoder.DecodingMap, out bytesWritten);
                    if (status == OperationStatus.Done || !ignoreWhiteSpace)
                    {
                        return status;
                    }
                    goto Invalid;
                }

                return DecodeFromUtf8InPlaceCore(decoder, buffer, out bytesWritten, ignoreWhiteSpace);
            }

        Invalid:
            bytesWritten = 0;
            return ignoreWhiteSpace ?
                DecodeWithWhiteSpaceFromUtf8InPlace(decoder, buffer, ref bytesWritten, sourceIndex: 0) :
                OperationStatus.InvalidData;
        }

        private static OperationStatus DecodeFromUtf8InPlaceCore<TBase64Decoder>(TBase64Decoder decoder, Span<byte> buffer,
            out int bytesWritten, bool ignoreWhiteSpace)
            where TBase64Decoder : IBase64Decoder<byte>
        {
            ReadOnlySpan<byte> src = buffer;
            Span<byte> dest = buffer;
#if NET
            if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && typeof(TBase64Decoder) != typeof(Base64DecoderByte))
            {
                if (src.Length >= Vector128DecodeInputLength)
                {
                    Avx512Decode(decoder, ref src, ref dest);
                }
            }
            else
            {
                if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && src.Length >= Avx512DecodeMinInputLength)
                {
                    Avx512Decode(decoder, ref src, ref dest, decodeNarrow: false);
                }

                if (Avx2.IsSupported && src.Length >= Avx2DecodeMinInputLength)
                {
                    Avx2Decode(decoder, ref src, ref dest);
                }

                if (AdvSimd.Arm64.IsSupported && src.Length >= AdvSimdDecodeMinInputLength)
                {
                    AdvSimdDecode(decoder, ref src, ref dest);
                }

                if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported) &&
                    BitConverter.IsLittleEndian &&
                    src.Length >= Vector128DecodeMinInputLength)
                {
                    Vector128Decode(decoder, ref src, ref dest);
                }
            }
#endif
            ReadOnlySpan<sbyte> decodingMap = decoder.DecodingMap;
            while (src.Length > 4 && dest.Length >= 3)
            {
                int result = decoder.DecodeFourElements(src, decodingMap);
                if (result < 0)
                {
                    goto Invalid;
                }

                WriteThreeLowOrderBytes(dest, result);
                src = src.Slice(4);
                dest = dest.Slice(3);
            }

            if (DecodeInPlaceTail(decoder, src, dest, decodingMap, out int finalWritten) != OperationStatus.Done)
            {
                goto Invalid;
            }

            bytesWritten = buffer.Length - dest.Length + finalWritten;
            return OperationStatus.Done;

        Invalid:
            bytesWritten = buffer.Length - dest.Length;
            return ignoreWhiteSpace ?
                DecodeWithWhiteSpaceFromUtf8InPlace(decoder, buffer, ref bytesWritten, (uint)(buffer.Length - src.Length)) :
                OperationStatus.InvalidData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OperationStatus DecodeInPlaceTail<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<byte> src,
            Span<byte> dest, ReadOnlySpan<sbyte> decodingMap, out int bytesWritten)
            where TBase64Decoder : IBase64Decoder<byte>
        {
            bytesWritten = 0;
            int remaining = typeof(TBase64Decoder) == typeof(Base64DecoderByte) ? 4 : src.Length;
            int value = decoder.DecodeRemaining(src, decodingMap, remaining, out uint t2, out uint t3);
            if (value < 0)
            {
                return OperationStatus.InvalidData;
            }

            if (!decoder.IsValidPadding(t3))
            {
                value |= decodingMap[(byte)t2] << 6;
                int i3 = decodingMap[(byte)t3];
                value |= i3;
                if (value < 0)
                {
                    return OperationStatus.InvalidData;
                }

                WriteThreeLowOrderBytes(dest, value);
                bytesWritten = 3;
            }
            else if (!decoder.IsValidPadding(t2))
            {
                value |= decodingMap[(byte)t2] << 6;
                if ((value & 0x800000c0) != 0)
                {
                    return OperationStatus.InvalidData;
                }
                dest[0] = (byte)(value >> 16);
                dest[1] = (byte)(value >> 8);
                bytesWritten = 2;
            }
            else
            {
                if ((value & 0x8000F000) != 0)
                {
                    return OperationStatus.InvalidData;
                }
                dest[0] = (byte)(value >> 16);
                bytesWritten = 1;
            }

            return OperationStatus.Done;
        }

        internal static OperationStatus DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<byte> source,
            Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
            where TBase64Decoder : IBase64Decoder<byte>
        {
            const int BlockSize = 4;
            Span<byte> buffer = stackalloc byte[4] { 0, 0, 0, 0 };
            OperationStatus status = OperationStatus.Done;

            while (!source.IsEmpty)
            {
                // Skip over any leading whitespace
                if (IsWhiteSpace(source[0]))
                {
                    source = source.Slice(1);
                    bytesConsumed++;
                    continue;
                }

                int encodedIdx = 0;
                int bufferIdx = 0;
                int skipped = 0;

                for (; encodedIdx < source.Length && (uint)bufferIdx < (uint)buffer.Length; ++encodedIdx)
                {
                    if (IsWhiteSpace(source[encodedIdx]))
                    {
                        skipped++;
                    }
                    else
                    {
                        buffer[bufferIdx] = source[encodedIdx];
                        bufferIdx++;
                    }
                }

                source = source.Slice(encodedIdx);
                Debug.Assert(bufferIdx > 0);

                bool hasAnotherBlock;

                if (typeof(TBase64Decoder) == typeof(Base64DecoderByte))
                {
                    hasAnotherBlock = source.Length >= BlockSize;
                }
                else
                {
                    hasAnotherBlock = source.Length > 1;
                }

                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount(decoder, buffer);
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

                status = DecodeFrom<TBase64Decoder, byte>(decoder, buffer.Slice(0, bufferIdx), bytes,
                    out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);

                if (status != OperationStatus.Done)
                {
                    Debug.Assert(localConsumed == 0 && localWritten == 0, "On failure, should not have consumed or written any bytes");
                    return status;
                }

                bytesConsumed += skipped;
                bytesConsumed += localConsumed;
                bytesWritten += localWritten;

                // The remaining data must all be whitespace in order to be valid.
                if (!hasAnotherBlock)
                {
                    for (int i = 0; i < source.Length; ++i)
                    {
                        if (!IsWhiteSpace(source[i]))
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
                Debug.Assert(!source.IsEmpty);
            }

            return status;
        }

        internal static OperationStatus DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<ushort> source,
            Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
            where TBase64Decoder : IBase64Decoder<ushort>
        {
            const int BlockSize = 4;
            Span<ushort> buffer = stackalloc ushort[4] { 0, 0, 0, 0 };
            OperationStatus status = OperationStatus.Done;

            while (!source.IsEmpty)
            {
                // Skip over any leading whitespace
                if (IsWhiteSpace(source[0]))
                {
                    source = source.Slice(1);
                    bytesConsumed++;
                    continue;
                }

                int encodedIdx = 0;
                int bufferIdx = 0;
                int skipped = 0;

                for (; encodedIdx < source.Length && (uint)bufferIdx < (uint)buffer.Length; ++encodedIdx)
                {
                    if (IsWhiteSpace(source[encodedIdx]))
                    {
                        skipped++;
                    }
                    else
                    {
                        buffer[bufferIdx] = source[encodedIdx];
                        bufferIdx++;
                    }
                }

                source = source.Slice(encodedIdx);
                Debug.Assert(bufferIdx > 0);

                bool hasAnotherBlock;

                if (decoder is Base64DecoderChar)
                {
                    hasAnotherBlock = source.Length >= BlockSize;
                }
                else
                {
                    hasAnotherBlock = source.Length > 1;
                }

                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount(decoder, buffer);
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

                status = DecodeFrom(decoder, buffer.Slice(0, bufferIdx), bytes,
                    out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);

                if (status != OperationStatus.Done)
                {
                    Debug.Assert(localConsumed == 0 && localWritten == 0, "On failure, should not have consumed or written any bytes");
                    return status;
                }

                bytesConsumed += skipped;
                bytesConsumed += localConsumed;
                bytesWritten += localWritten;

                // The remaining data must all be whitespace in order to be valid.
                if (!hasAnotherBlock)
                {
                    for (int i = 0; i < source.Length; ++i)
                    {
                        if (!IsWhiteSpace(source[i]))
                        {
                            // Revert previous dest increment, since an invalid state followed.
                            bytesConsumed -= localConsumed;
                            bytesWritten -= localWritten;

                            return OperationStatus.InvalidData;
                        }
                    }

                    bytesConsumed += source.Length;
                    break;
                }

                bytes = bytes.Slice(localWritten);
                Debug.Assert(!source.IsEmpty);
            }

            return status;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPaddingCount<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<byte> buffer)
            where TBase64Decoder : IBase64Decoder<byte>
        {
            int padding = 0;

            if (decoder.IsValidPadding(buffer[buffer.Length - 1]))
            {
                padding++;
            }

            if (decoder.IsValidPadding(buffer[buffer.Length - 2]))
            {
                padding++;
            }

            return padding;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPaddingCount<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<ushort> buffer)
            where TBase64Decoder : IBase64Decoder<ushort>
        {
            int padding = 0;

            if (decoder.IsValidPadding(buffer[buffer.Length - 1]))
            {
                padding++;
            }

            if (decoder.IsValidPadding(buffer[buffer.Length - 2]))
            {
                padding++;
            }

            return padding;
        }

        private static OperationStatus DecodeWithWhiteSpaceFromUtf8InPlace<TBase64Decoder>(TBase64Decoder decoder, Span<byte> source,
            ref int destIndex, uint sourceIndex)
            where TBase64Decoder : IBase64Decoder<byte>
        {
            int BlockSize = Math.Min(source.Length - (int)sourceIndex, 4);
            Span<byte> buffer = stackalloc byte[4] { 0, 0, 0, 0 };
            buffer = buffer.Slice(4 - BlockSize);

            OperationStatus status = OperationStatus.Done;
            int localDestIndex = destIndex;
            bool hasPaddingBeenProcessed = false;
            int localBytesWritten = 0;

            while (sourceIndex < (uint)source.Length)
            {
                int bufferIdx = 0;

                while (bufferIdx < BlockSize && sourceIndex < (uint)source.Length)
                {
                    if (!IsWhiteSpace(source[(int)sourceIndex]))
                    {
                        buffer[bufferIdx] = source[(int)sourceIndex];
                        bufferIdx++;
                    }

                    sourceIndex++;
                }

                if (bufferIdx == 0)
                {
                    continue;
                }

                if (bufferIdx != 4)
                {
                    // Base64 require 4 bytes, for Base64Url it can be less than 4 bytes but not 1 byte.
                    if (decoder is Base64DecoderByte || bufferIdx == 1)
                    {
                        status = OperationStatus.InvalidData;
                        break;
                    }
                    else // For Base64Url fill empty slots in last block with padding
                    {
                        while (bufferIdx < BlockSize)  // Can happen only for last block
                        {
                            Debug.Assert(source.Length == sourceIndex);
                            buffer[bufferIdx++] = (byte)EncodingPad;
                        }
                    }
                }

                if (hasPaddingBeenProcessed)
                {
                    // Padding has already been processed, a new valid block cannot be processed.
                    // Revert previous dest increment, since an invalid state followed.
                    localDestIndex -= localBytesWritten;
                    status = OperationStatus.InvalidData;
                    break;
                }

                status = DecodeFromUtf8InPlace<TBase64Decoder>(decoder, buffer, out localBytesWritten, ignoreWhiteSpace: false);
                localDestIndex += localBytesWritten;
                hasPaddingBeenProcessed = localBytesWritten < 3;

                if (status != OperationStatus.Done)
                {
                    break;
                }

                // Write result to source span in place.
                for (int i = 0; i < localBytesWritten; i++)
                {
                    source[localDestIndex - localBytesWritten + i] = buffer[i];
                }
            }

            destIndex = localDestIndex;
            return status;
        }

#if NET
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static void Avx512Decode<TBase64Decoder, T>(TBase64Decoder decoder, ref ReadOnlySpan<T> srcBytes,
            ref Span<byte> destBytes, bool decodeNarrow = true)
            where TBase64Decoder : IBase64Decoder<T>
            where T : unmanaged
        {
            // Reference for VBMI implementation : https://github.com/WojciechMula/base64simd/tree/master/decode
            // If we have AVX512 support, pick off 64 bytes at a time for as long as we can,
            // but make sure that we quit before seeing any == markers at the end of the
            // string. Also, because we write 16 zeroes at the end of the output, ensure
            // that there are at least 22 valid bytes of input data remaining to close the
            // gap. 64 + 2 + 22 = 88 bytes.
            ReadOnlySpan<T> src = srcBytes;
            Span<byte> dest = destBytes;

            // The JIT won't hoist these "constants", so help it
            Vector512<sbyte> vbmiLookup0 = Vector512.Create(decoder.VbmiLookup0).AsSByte();
            Vector512<sbyte> vbmiLookup1 = Vector512.Create(decoder.VbmiLookup1).AsSByte();
            Vector512<byte> vbmiPackedLanesControl = Vector512.Create(
                0x06000102, 0x090a0405, 0x0c0d0e08, 0x16101112,
                0x191a1415, 0x1c1d1e18, 0x26202122, 0x292a2425,
                0x2c2d2e28, 0x36303132, 0x393a3435, 0x3c3d3e38,
                0x00000000, 0x00000000, 0x00000000, 0x00000000).AsByte();

            Vector512<sbyte> mergeConstant0 = Vector512.Create(0x01400140).AsSByte();
            Vector512<short> mergeConstant1 = Vector512.Create(0x00011000).AsInt16();

            // This algorithm requires AVX512VBMI support.
            // Vbmi was first introduced in CannonLake and is available from IceLake on.
            while (src.Length >= Avx512DecodePairMinInputLength && dest.Length >= Avx512DecodePairMinDestinationLength)
            {
                if (!decoder.TryLoadVector512(src, out Vector512<sbyte> first) ||
                    !decoder.TryLoadVector512(src.Slice(Avx512DecodeInputLength), out Vector512<sbyte> second))
                {
                    break;
                }

                Vector512<sbyte> firstIndex = Avx512Vbmi.PermuteVar64x8x2(vbmiLookup0, first, vbmiLookup1);
                Vector512<sbyte> secondIndex = Avx512Vbmi.PermuteVar64x8x2(vbmiLookup0, second, vbmiLookup1);
                if (((firstIndex | first) | (secondIndex | second)).ExtractMostSignificantBits() != 0)
                {
                    break;
                }

                PackVector(firstIndex, vbmiPackedLanesControl, mergeConstant0, mergeConstant1).CopyTo(dest);
                PackVector(secondIndex, vbmiPackedLanesControl, mergeConstant0, mergeConstant1).CopyTo(dest.Slice(Avx512DecodeOutputLength));
                src = src.Slice(2 * Avx512DecodeInputLength);
                dest = dest.Slice(2 * Avx512DecodeOutputLength);
            }

            while (src.Length >= Avx512DecodeMinInputLength && dest.Length >= Avx512DecodeStoreLength)
            {
                if (!decoder.TryLoadVector512(src, out Vector512<sbyte> str) ||
                    !TryDecodeVector(str, vbmiLookup0, vbmiLookup1, vbmiPackedLanesControl, mergeConstant0, mergeConstant1, out Vector512<byte> result))
                {
                    break;
                }

                result.CopyTo(dest);
                src = src.Slice(Avx512DecodeInputLength);
                dest = dest.Slice(Avx512DecodeOutputLength);
            }

            if (!decodeNarrow)
            {
                goto Exit;
            }

            while (src.Length >= Avx2DecodeInputLength + 4 && dest.Length >= Avx2DecodeOutputLength)
            {
                if (!decoder.TryLoadAvxVector256(src, out Vector256<sbyte> input) ||
                    !TryDecodeVector(input.ToVector512(), vbmiLookup0, vbmiLookup1,
                        vbmiPackedLanesControl, mergeConstant0, mergeConstant1, out Vector512<byte> result, uint.MaxValue))
                {
                    break;
                }

                result.GetLower().GetLower().CopyTo(dest);
                if (!BitConverter.TryWriteBytes(dest.Slice(16), result.AsUInt64().GetElement(2)))
                {
                    ThrowUnreachableException();
                }
                src = src.Slice(Avx2DecodeInputLength);
                dest = dest.Slice(Avx2DecodeOutputLength);
            }

            while (src.Length >= Vector128DecodeInputLength + 4 && dest.Length >= Vector128DecodeOutputLength)
            {
                if (!decoder.TryLoadVector128(src, out Vector128<byte> input) ||
                    !TryDecodeVector(input.ToVector256().ToVector512().AsSByte(), vbmiLookup0, vbmiLookup1,
                        vbmiPackedLanesControl, mergeConstant0, mergeConstant1, out Vector512<byte> result, ushort.MaxValue))
                {
                    break;
                }

                if (!BitConverter.TryWriteBytes(dest, result.AsUInt64().GetElement(0)))
                {
                    ThrowUnreachableException();
                }

                if (!BitConverter.TryWriteBytes(dest.Slice(8), result.AsUInt32().GetElement(2)))
                {
                    ThrowUnreachableException();
                }
                src = src.Slice(Vector128DecodeInputLength);
                dest = dest.Slice(Vector128DecodeOutputLength);
            }

            while (src.Length >= Vector128DecodeInputLength && dest.Length >= Avx512DecodeTailOutputLength)
            {
                if (!decoder.TryLoadVector128(src, out Vector128<byte> input) ||
                    !TryDecodeVector(input.ToVector256().ToVector512().AsSByte(), vbmiLookup0, vbmiLookup1,
                        vbmiPackedLanesControl, mergeConstant0, mergeConstant1, out Vector512<byte> result, 0xFFF))
                {
                    break;
                }

                if (!BitConverter.TryWriteBytes(dest, result.AsUInt64().GetElement(0)))
                {
                    ThrowUnreachableException();
                }
                dest[8] = result.GetElement(8);
                src = src.Slice(Avx512DecodeTailInputLength);
                dest = dest.Slice(Avx512DecodeTailOutputLength);
            }

        Exit:
            srcBytes = src;
            destBytes = dest;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool TryDecodeVector(Vector512<sbyte> str, Vector512<sbyte> lookup0, Vector512<sbyte> lookup1,
                Vector512<byte> lanes, Vector512<sbyte> merge0, Vector512<short> merge1, out Vector512<byte> result, ulong errorMask = ulong.MaxValue)
            {
                Vector512<sbyte> origIndex = Avx512Vbmi.PermuteVar64x8x2(lookup0, str, lookup1);
                Vector512<sbyte> errorVec = (origIndex.AsInt32() | str.AsInt32()).AsSByte();
                ulong errors;
                if (errorMask == ulong.MaxValue)
                {
                    errors = errorVec.ExtractMostSignificantBits();
                }
                else if (errorMask == uint.MaxValue)
                {
                    errors = errorVec.GetLower().ExtractMostSignificantBits();
                }
                else
                {
                    errors = errorVec.GetLower().GetLower().ExtractMostSignificantBits();
                }

                if ((errors & errorMask) != 0)
                {
                    result = default;
                    return false;
                }

                result = PackVector(origIndex, lanes, merge0, merge1);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Vector512<byte> PackVector(Vector512<sbyte> indices, Vector512<byte> lanes, Vector512<sbyte> merge0, Vector512<short> merge1)
            {
                Vector512<short> multiAdd1 = Avx512BW.MultiplyAddAdjacent(indices.AsByte(), merge0);
                Vector512<int> multiAdd2 = Avx512BW.MultiplyAddAdjacent(multiAdd1, merge1);
                return Avx512Vbmi.PermuteVar64x8(multiAdd2.AsByte(), lanes);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static void Avx2Decode<TBase64Decoder, T>(TBase64Decoder decoder, ref ReadOnlySpan<T> srcBytes, ref Span<byte> destBytes)
            where TBase64Decoder : IBase64Decoder<T>
            where T : unmanaged
        {
            // If we have AVX2 support, pick off 32 bytes at a time for as long as we can,
            // but make sure that we quit before seeing any == markers at the end of the
            // string. Also, because we write 8 zeroes at the end of the output, ensure
            // that there are at least 11 valid bytes of input data remaining to close the
            // gap. 32 + 2 + 11 = 45 bytes.

            // See SSSE3-version below for an explanation of how the code works.

            // The JIT won't hoist these "constants", so help it
            Vector256<sbyte> lutHi = Vector256.Create(decoder.Avx2LutHigh);

            Vector256<sbyte> lutLo = Vector256.Create(decoder.Avx2LutLow);

            Vector256<sbyte> lutShift = Vector256.Create(decoder.Avx2LutShift);

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

            Vector256<sbyte> maskSlashOrUnderscore = Vector256.Create((sbyte)decoder.MaskSlashOrUnderscore);
            Vector256<sbyte> shiftForUnderscore = Vector256.Create((sbyte)33);
            Vector256<sbyte> mergeConstant0 = Vector256.Create(0x01400140).AsSByte();
            Vector256<short> mergeConstant1 = Vector256.Create(0x00011000).AsInt16();

            ReadOnlySpan<T> src = srcBytes;
            Span<byte> dest = destBytes;

            while (src.Length >= Avx2DecodeMinInputLength && dest.Length >= Avx2DecodeStoreLength)
            {
                if (!decoder.TryLoadAvxVector256(src, out Vector256<sbyte> str))
                {
                    break;
                }

                Vector256<sbyte> hiNibbles = ((str.AsInt32()) >>> 4).AsSByte() & maskSlashOrUnderscore;

                if (!decoder.TryDecode256Core(str, hiNibbles, maskSlashOrUnderscore, lutLo, lutHi, lutShift, shiftForUnderscore, out str))
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

                str.AsByte().CopyTo(dest);

                src = src.Slice(Avx2DecodeInputLength);
                dest = dest.Slice(Avx2DecodeOutputLength);
            }

            srcBytes = src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal static Vector128<byte> SimdShuffle(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask8F)
        {
            Debug.Assert((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported) && BitConverter.IsLittleEndian);

            if (Ssse3.IsSupported)
            {
                return Ssse3.Shuffle(left, right);
            }
            else if (PackedSimd.IsSupported)
            {
                return PackedSimd.Swizzle(left, right & mask8F);
            }
            else
            {
                return AdvSimd.Arm64.VectorTableLookup(left, right & mask8F);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static void AdvSimdDecode<TBase64Decoder, T>(TBase64Decoder decoder, ref ReadOnlySpan<T> srcBytes, ref Span<byte> destBytes)
            where TBase64Decoder : IBase64Decoder<T>
            where T : unmanaged
        {
            // C# implementation of https://github.com/aklomp/base64/blob/3a5add8652076612a8407627a42c768736a4263f/lib/arch/neon64/dec_loop.c
            // If we have AdvSimd support, pick off 64 bytes at a time for as long as we can,
            // but make sure that we quit before seeing any == markers at the end of the
            // string. 64 + 2 = 66 bytes.

            // In the decoding process, we want to map each byte, representing a Base64 value, to its 6-bit (0-63) representation.
            // It uses the following mapping. Values outside the following groups are invalid and, we abort decoding when encounter one.
            //
            // #    From       To         Char
            // 1    [43]       [62]       +
            // 2    [47]       [63]       /
            // 3    [48..57]   [52..61]   0..9
            // 4    [65..90]   [0..25]    A..Z
            // 5    [97..122]  [26..51]   a..z
            //
            // To map an input value to its Base64 representation, we use look-up tables 'decLutOne' and 'decLutTwo'.
            // 'decLutOne' helps to map groups 1, 2 and 3 while 'decLutTwo' maps groups 4 and 5 in the above list.
            // After mapping, each value falls between 0-63. Consequently, the last six bits of each byte now hold a valid value.
            // We then compress four such bytes (with valid 4 * 6 = 24 bits) to three UTF8 bytes (3 * 8 = 24 bits).
            // For faster decoding, we use SIMD operations that allow the processing of multiple bytes together.
            // However, the compress operation on adjacent values of a vector could be slower. Thus, we de-interleave while reading
            // the input bytes that store adjacent bytes in separate vectors. This later simplifies the compress step with the help
            // of logical operations. This requires interleaving while storing the decoded result.

            // Values in 'decLutOne' maps input values from 0 to 63.
            //   255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
            //   255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
            //   255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  62, 255, 255, 255,  63
            //    52,  53,  54,  55,  56,  57,  58,  59,  60,  61, 255, 255, 255, 255, 255, 255
            var decLutOne = (Vector128<byte>.AllBitsSet,
                             Vector128<byte>.AllBitsSet,
                             Vector128.Create(decoder.AdvSimdLutOne3).AsByte(),
                             Vector128.Create(0x37363534, 0x3B3A3938, 0xFFFF3D3C, 0xFFFFFFFF).AsByte());

            // Values in 'decLutTwo' maps input values from 63 to 127.
            //    0, 255,   0,   1,   2,   3,   4,   5,   6,   7,   8,   9,  10,  11,  12,  13
            //   14,  15,  16,  17,  18,  19,  20,  21,  22,  23,  24,  25, 255, 255, 255, 255
            //  255, 255,  26,  27,  28,  29,  30,  31,  32,  33,  34,  35,  36,  37,  38,  39
            //   40,  41,  42,  43,  44,  45,  46,  47,  48,  49,  50,  51, 255, 255, 255, 255
            var decLutTwo = (Vector128.Create(0x0100FF00, 0x05040302, 0x09080706, 0x0D0C0B0A).AsByte(),
                             Vector128.Create(0x11100F0E, 0x15141312, 0x19181716, 0xFFFFFFFF).AsByte(),
                             Vector128.Create(decoder.AdvSimdLutTwo3Uint1, 0x1F1E1D1C, 0x23222120, 0x27262524).AsByte(),
                             Vector128.Create(0x2B2A2928, 0x2F2E2D2C, 0x33323130, 0xFFFFFFFF).AsByte());

            ReadOnlySpan<T> src = srcBytes;
            Span<byte> dest = destBytes;
            Vector128<byte> offset = Vector128.Create<byte>(63);

            while (src.Length >= AdvSimdDecodeMinInputLength && dest.Length >= AdvSimdDecodeOutputLength)
            {
                // Step 1: Load 64 bytes and de-interleave.
                if (!decoder.TryLoadArmVector128x4(src,
                    out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4))
                {
                    break;
                }

                // Step 2: Map each valid input to its Base64 value.
                // We use two look-ups to compute partial results and combine them later.

                // Step 2.1: Detect valid Base64 values from the first three groups. Maps input as,
                //  0 to  63 (Invalid) => 255
                //  0 to  63 (Valid)   => Their Base64 equivalent
                // 64 to 255           => 0

                // Each input value acts as an index in the look-up table 'decLutOne'.
                // e.g., for group 1: index 43 maps to 62 (Base64 '+').
                // Group 4 and 5 values are out-of-range (>64), so they are mapped to zero.
                // Other valid indices but invalid values are mapped to 255.
                Vector128<byte> decOne1 = AdvSimd.Arm64.VectorTableLookup(decLutOne, str1);
                Vector128<byte> decOne2 = AdvSimd.Arm64.VectorTableLookup(decLutOne, str2);
                Vector128<byte> decOne3 = AdvSimd.Arm64.VectorTableLookup(decLutOne, str3);
                Vector128<byte> decOne4 = AdvSimd.Arm64.VectorTableLookup(decLutOne, str4);

                // Step 2.2: Detect valid Base64 values from groups 4 and 5. Maps input as,
                //   0 to  63           => 0
                //  64 to 122 (Valid)   => Their Base64 equivalent
                //  64 to 122 (Invalid) => 255
                // 123 to 255           => Remains unchanged

                // Subtract/offset each input value by 63 so that it can be used as a valid offset.
                // Subtract saturate makes values from the first three groups set to zero that are
                // then mapped to zero in the subsequent look-up.
                Vector128<byte> decTwo1 = AdvSimd.SubtractSaturate(str1, offset);
                Vector128<byte> decTwo2 = AdvSimd.SubtractSaturate(str2, offset);
                Vector128<byte> decTwo3 = AdvSimd.SubtractSaturate(str3, offset);
                Vector128<byte> decTwo4 = AdvSimd.SubtractSaturate(str4, offset);

                // We use VTBX to map values where out-of-range indices are unchanged.
                decTwo1 = AdvSimd.Arm64.VectorTableLookupExtension(decTwo1, decLutTwo, decTwo1);
                decTwo2 = AdvSimd.Arm64.VectorTableLookupExtension(decTwo2, decLutTwo, decTwo2);
                decTwo3 = AdvSimd.Arm64.VectorTableLookupExtension(decTwo3, decLutTwo, decTwo3);
                decTwo4 = AdvSimd.Arm64.VectorTableLookupExtension(decTwo4, decLutTwo, decTwo4);

                // Step 3: Combine the partial result.
                // Each look-up above maps valid values to their Base64 equivalent or zero.
                // Thus the intermediate results 'decOne' and 'decTwo' could be OR-ed to get final values.
                str1 = (decOne1 | decTwo1);
                str2 = (decOne2 | decTwo2);
                str3 = (decOne3 | decTwo3);
                str4 = (decOne4 | decTwo4);

                // Step 4: Detect an invalid input value.
                // Invalid values < 122 are set to 255 while the ones above 122 are unchanged.
                // Check for invalid input, any value larger than 63.
                Vector128<byte> classified = (Vector128.GreaterThan(str1, offset)
                                            | Vector128.GreaterThan(str2, offset)
                                            | Vector128.GreaterThan(str3, offset)
                                            | Vector128.GreaterThan(str4, offset));

                // Check that all bits are zero.
                if (classified != Vector128<byte>.Zero)
                {
                    break;
                }

                // Step 5: Compress four bytes into three.
                Vector128<byte> res1 = ((str1 << 2) | (str2 >> 4));
                Vector128<byte> res2 = ((str2 << 4) | (str3 >> 2));
                Vector128<byte> res3 = ((str3 << 6) | str4);

                // Step 6: Interleave and store decoded results.
                StoreArmVector128x3(dest, res1, res2, res3);

                src = src.Slice(AdvSimdDecodeInputLength);
                dest = dest.Slice(AdvSimdDecodeOutputLength);
            }

            srcBytes = src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        private static void Vector128Decode<TBase64Decoder, T>(TBase64Decoder decoder, ref ReadOnlySpan<T> srcBytes, ref Span<byte> destBytes)
            where TBase64Decoder : IBase64Decoder<T>
            where T : unmanaged
        {
            Debug.Assert((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported) && BitConverter.IsLittleEndian);

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
            Vector128<byte> lutHi = Vector128.Create(decoder.Vector128LutHigh).AsByte();
            Vector128<byte> lutLo = Vector128.Create(decoder.Vector128LutLow).AsByte();
            Vector128<sbyte> lutShift = Vector128.Create(decoder.Vector128LutShift).AsSByte();
            Vector128<sbyte> packBytesMask = Vector128.Create(0x06000102, 0x090A0405, 0x0C0D0E08, 0xffffffff).AsSByte();
            Vector128<byte> mergeConstant0 = Vector128.Create(0x01400140).AsByte();
            Vector128<short> mergeConstant1 = Vector128.Create(0x00011000).AsInt16();
            Vector128<byte> one = Vector128<byte>.One;
            Vector128<byte> mask2F = Vector128.Create(decoder.MaskSlashOrUnderscore);
            Vector128<byte> mask8F = Vector128.Create((byte)0x8F);
            Vector128<byte> shiftForUnderscore = Vector128.Create((byte)33);
            ReadOnlySpan<T> src = srcBytes;
            Span<byte> dest = destBytes;

            while (src.Length >= Vector128DecodeMinInputLength && dest.Length >= Vector128DecodeStoreLength)
            {
                if (!decoder.TryLoadVector128(src, out Vector128<byte> str))
                {
                    break;
                }

                // lookup
                Vector128<byte> hiNibbles = Vector128.ShiftRightLogical(str.AsInt32(), 4).AsByte() & mask2F;

                if (!decoder.TryDecode128Core(str, hiNibbles, mask2F, mask8F, lutLo, lutHi, lutShift, shiftForUnderscore, out str))
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
                else if (AdvSimd.Arm64.IsSupported)
                {
                    Vector128<ushort> evens = AdvSimd.ShiftLeftLogicalWideningLower(AdvSimd.Arm64.UnzipEven(str, one).GetLower(), 6);
                    Vector128<ushort> odds = AdvSimd.Arm64.TransposeOdd(str, Vector128<byte>.Zero).AsUInt16();
                    merge_ab_and_bc = Vector128.Add(evens, odds).AsInt16();
                }
                else if (PackedSimd.IsSupported)
                {
                    // MultiplyAddAdjacent by {64,1,...} is the even byte (low of each u16 lane) times 64 plus the odd byte.
                    Vector128<ushort> u = str.AsUInt16();
                    Vector128<ushort> evens = Vector128.ShiftLeft(u & Vector128.Create((ushort)0x00FF), 6);
                    Vector128<ushort> odds = Vector128.ShiftRightLogical(u, 8);
                    merge_ab_and_bc = (evens + odds).AsInt16();
                }
                else
                {
                    // We explicitly recheck each IsSupported query to ensure that the trimmer can see which paths are live/dead
                    ThrowUnreachableException();
                    merge_ab_and_bc = default;
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
                else if (AdvSimd.Arm64.IsSupported)
                {
                    Vector128<int> ievens = AdvSimd.ShiftLeftLogicalWideningLower(AdvSimd.Arm64.UnzipEven(merge_ab_and_bc, one.AsInt16()).GetLower(), 12);
                    Vector128<int> iodds = AdvSimd.Arm64.TransposeOdd(merge_ab_and_bc, Vector128<short>.Zero).AsInt32();
                    output = Vector128.Add(ievens, iodds).AsInt32();
                }
                else if (PackedSimd.IsSupported)
                {
                    // MultiplyAddAdjacent by {4096,1,...} is the even i16 (low of each i32 lane) times 4096 plus the odd i16.
                    Vector128<uint> m = merge_ab_and_bc.AsUInt32();
                    Vector128<uint> ievens = Vector128.ShiftLeft(m & Vector128.Create(0x0000FFFFu), 12);
                    Vector128<uint> iodds = Vector128.ShiftRightLogical(m, 16);
                    output = (ievens + iodds).AsInt32();
                }
                else
                {
                    // We explicitly recheck each IsSupported query to ensure that the trimmer can see which paths are live/dead
                    ThrowUnreachableException();
                    output = default;
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

                str.CopyTo(dest);

                src = src.Slice(Vector128DecodeInputLength);
                dest = dest.Slice(Vector128DecodeOutputLength);
            }

            srcBytes = src;
            destBytes = dest;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteThreeLowOrderBytes(Span<byte> destination, int value)
        {
            destination[0] = (byte)(value >> 16);
            destination[1] = (byte)(value >> 8);
            destination[2] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsWhiteSpace(int value)
        {
            Debug.Assert(value >= 0 && value <= ushort.MaxValue);
            uint charMinusLowUInt32;
            return (int)((0xC8000100U << (short)(charMinusLowUInt32 = (ushort)(value - '\t'))) & (charMinusLowUInt32 - 32)) < 0;
        }

        internal readonly struct Base64DecoderByte : IBase64Decoder<byte>
        {
            // Pre-computing this table using a custom string(s_characters) and GenerateDecodingMapAndVerify (found in tests)
            public ReadOnlySpan<sbyte> DecodingMap =>
                [
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,         //62 is placed at index 43 (for +), 63 at index 47 (for /)
                    52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         //52-61 are placed at index 48-57 (for 0-9)
                    -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
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
                ];

            public ReadOnlySpan<uint> VbmiLookup0 =>
                [
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x3e808080, 0x3f808080,
                    0x37363534, 0x3b3a3938, 0x80803d3c, 0x80808080
                ];

            public ReadOnlySpan<uint> VbmiLookup1 =>
                [
                    0x02010080, 0x06050403, 0x0a090807, 0x0e0d0c0b,
                    0x1211100f, 0x16151413, 0x80191817, 0x80808080,
                    0x1c1b1a80, 0x201f1e1d, 0x24232221, 0x28272625,
                    0x2c2b2a29, 0x302f2e2d, 0x80333231, 0x80808080
                ];

            public ReadOnlySpan<sbyte> Avx2LutHigh =>
                [
                    0x10, 0x10, 0x01, 0x02,
                    0x04, 0x08, 0x04, 0x08,
                    0x10, 0x10, 0x10, 0x10,
                    0x10, 0x10, 0x10, 0x10,
                    0x10, 0x10, 0x01, 0x02,
                    0x04, 0x08, 0x04, 0x08,
                    0x10, 0x10, 0x10, 0x10,
                    0x10, 0x10, 0x10, 0x10
                ];

            public ReadOnlySpan<sbyte> Avx2LutLow =>
                [
                    0x15, 0x11, 0x11, 0x11,
                    0x11, 0x11, 0x11, 0x11,
                    0x11, 0x11, 0x13, 0x1A,
                    0x1B, 0x1B, 0x1B, 0x1A,
                    0x15, 0x11, 0x11, 0x11,
                    0x11, 0x11, 0x11, 0x11,
                    0x11, 0x11, 0x13, 0x1A,
                    0x1B, 0x1B, 0x1B, 0x1A
                ];

            public ReadOnlySpan<sbyte> Avx2LutShift =>
                [
                    0, 16, 19, 4,
                    -65, -65, -71, -71,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 16, 19, 4,
                    -65, -65, -71, -71,
                    0, 0, 0, 0,
                    0, 0, 0, 0
                ];

            public byte MaskSlashOrUnderscore => (byte)'/';

            public ReadOnlySpan<int> Vector128LutHigh => [0x02011010, 0x08040804, 0x10101010, 0x10101010];

            public ReadOnlySpan<int> Vector128LutLow => [0x11111115, 0x11111111, 0x1A131111, 0x1A1B1B1B];

            public ReadOnlySpan<uint> Vector128LutShift => [0x04131000, 0xb9b9bfbf, 0x00000000, 0x00000000];

            public ReadOnlySpan<uint> AdvSimdLutOne3 => [0xFFFFFFFF, 0xFFFFFFFF, 0x3EFFFFFF, 0x3FFFFFFF];

            public uint AdvSimdLutTwo3Uint1 => 0x1B1AFFFF;

            public int GetMaxDecodedLength(int utf8Length) => Base64.GetMaxDecodedFromUtf8Length(utf8Length);

            public bool IsInvalidLength(int bufferLength) => bufferLength % 4 != 0; // only decode input if it is a multiple of 4

            public bool IsValidPadding(uint padChar) => padChar == EncodingPad;

            public int SrcLength(bool _, int utf8Length) => utf8Length & ~0x3;  // only decode input up to the closest multiple of 4.

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            [CompExactlyDependsOn(typeof(PackedSimd))]
            public bool TryDecode128Core(
                Vector128<byte> str,
                Vector128<byte> hiNibbles,
                Vector128<byte> maskSlashOrUnderscore,
                Vector128<byte> mask8F,
                Vector128<byte> lutLow,
                Vector128<byte> lutHigh,
                Vector128<sbyte> lutShift,
                Vector128<byte> _,
                out Vector128<byte> result)
            {
                Vector128<byte> loNibbles = str & maskSlashOrUnderscore;
                Vector128<byte> hi = SimdShuffle(lutHigh, hiNibbles, mask8F);
                Vector128<byte> lo = SimdShuffle(lutLow, loNibbles, mask8F);

                // Check for invalid input: if any "and" values from lo and hi are not zero,
                // fall back on bytewise code to do error checking and reporting:
                if ((lo & hi) != Vector128<byte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector128<byte> eq2F = Vector128.Equals(str, maskSlashOrUnderscore);
                Vector128<byte> shift = SimdShuffle(lutShift.AsByte(), (eq2F + hiNibbles), mask8F);

                // Now simply add the delta values to the input:
                result = str + shift;

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public bool TryDecode256Core(
                Vector256<sbyte> str,
                Vector256<sbyte> hiNibbles,
                Vector256<sbyte> maskSlashOrUnderscore,
                Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh,
                Vector256<sbyte> lutShift,
                Vector256<sbyte> _,
                out Vector256<sbyte> result)
            {
                Vector256<sbyte> loNibbles = str & maskSlashOrUnderscore;
                Vector256<sbyte> hi = Avx2.Shuffle(lutHigh, hiNibbles);
                Vector256<sbyte> lo = Avx2.Shuffle(lutLow, loNibbles);

                if ((lo & hi) != Vector256<sbyte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector256<sbyte> eq2F = Avx2.CompareEqual(str, maskSlashOrUnderscore);
                Vector256<sbyte> shift = Avx2.Shuffle(lutShift, eq2F + hiNibbles);

                result = str + shift;

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryLoadVector512(ReadOnlySpan<byte> src, out Vector512<sbyte> str)
            {
                str = Vector512.Create(src).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public bool TryLoadAvxVector256(ReadOnlySpan<byte> src, out Vector256<sbyte> str)
            {
                str = Vector256.Create(src).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryLoadVector128(ReadOnlySpan<byte> src, out Vector128<byte> str)
            {
                str = Vector128.Create(src);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public bool TryLoadArmVector128x4(ReadOnlySpan<byte> src,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4)
            {
                (str1, str2, str3, str4) = LoadArmVector128x4(src);

                return true;
            }
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int DecodeFourElements(ReadOnlySpan<byte> source, ReadOnlySpan<sbyte> decodingMap)
            {
                // The 'source' span expected to have at least 4 elements, and the 'decodingMap' consists 256 sbytes
                uint input = BinaryPrimitives.ReadUInt32LittleEndian(source);
                uint t0 = (byte)input;
                uint t1 = (byte)(input >> 8);
                uint t2 = (byte)(input >> 16);
                uint t3 = input >> 24;

                int i0 = decodingMap[(byte)t0];
                int i1 = decodingMap[(byte)t1];
                int i2 = decodingMap[(byte)t2];
                int i3 = decodingMap[(byte)t3];

                i0 <<= 18;
                i1 <<= 12;
                i2 <<= 6;

                i0 |= i3;
                i1 |= i2;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int DecodeRemaining(ReadOnlySpan<byte> srcEnd, ReadOnlySpan<sbyte> decodingMap, int remaining, out uint t2, out uint t3)
            {
                uint t0;
                uint t1;
                t2 = EncodingPad;
                t3 = EncodingPad;
                switch (remaining)
                {
                    case 2 when srcEnd.Length >= 2:
                        t0 = srcEnd[0];
                        t1 = srcEnd[1];
                        break;
                    case 3 when srcEnd.Length >= 3:
                        t0 = srcEnd[0];
                        t1 = srcEnd[1];
                        t2 = srcEnd[2];
                        break;
                    case 4 when srcEnd.Length >= 4:
                        uint input = BinaryPrimitives.ReadUInt32LittleEndian(srcEnd);
                        t0 = (byte)input;
                        t1 = (byte)(input >> 8);
                        t2 = (byte)(input >> 16);
                        t3 = input >> 24;
                        break;
                    default:
                        return -1;
                }

                int i0 = decodingMap[(byte)t0];
                int i1 = decodingMap[(byte)t1];

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<byte> span)
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
            public OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<byte> utf8,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TBase64Decoder : IBase64Decoder<byte> =>
                DecodeWithWhiteSpaceBlockwise(decoder, utf8, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
        }

        internal readonly struct Base64DecoderChar : IBase64Decoder<ushort>
        {
            public ReadOnlySpan<sbyte> DecodingMap => default(Base64DecoderByte).DecodingMap;

            public ReadOnlySpan<uint> VbmiLookup0 => default(Base64DecoderByte).VbmiLookup0;

            public ReadOnlySpan<uint> VbmiLookup1 => default(Base64DecoderByte).VbmiLookup1;

            public ReadOnlySpan<sbyte> Avx2LutHigh => default(Base64DecoderByte).Avx2LutHigh;

            public ReadOnlySpan<sbyte> Avx2LutLow => default(Base64DecoderByte).Avx2LutLow;

            public ReadOnlySpan<sbyte> Avx2LutShift => default(Base64DecoderByte).Avx2LutShift;

            public byte MaskSlashOrUnderscore => default(Base64DecoderByte).MaskSlashOrUnderscore;

            public ReadOnlySpan<int> Vector128LutHigh => default(Base64DecoderByte).Vector128LutHigh;

            public ReadOnlySpan<int> Vector128LutLow => default(Base64DecoderByte).Vector128LutLow;

            public ReadOnlySpan<uint> Vector128LutShift => default(Base64DecoderByte).Vector128LutShift;

            public ReadOnlySpan<uint> AdvSimdLutOne3 => default(Base64DecoderByte).AdvSimdLutOne3;

            public uint AdvSimdLutTwo3Uint1 => default(Base64DecoderByte).AdvSimdLutTwo3Uint1;

            public int GetMaxDecodedLength(int sourceLength) => Base64.GetMaxDecodedFromUtf8Length(sourceLength);

            public bool IsInvalidLength(int bufferLength) => bufferLength % 4 != 0;

            public bool IsValidPadding(uint padChar) => padChar == EncodingPad;

            public int SrcLength(bool _, int sourceLength) => sourceLength & ~0x3;

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            [CompExactlyDependsOn(typeof(PackedSimd))]
            public bool TryDecode128Core(Vector128<byte> str, Vector128<byte> hiNibbles, Vector128<byte> maskSlashOrUnderscore, Vector128<byte> mask8F,
                Vector128<byte> lutLow, Vector128<byte> lutHigh, Vector128<sbyte> lutShift, Vector128<byte> shiftForUnderscore, out Vector128<byte> result) =>
                default(Base64DecoderByte).TryDecode128Core(str, hiNibbles, maskSlashOrUnderscore, mask8F, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public bool TryDecode256Core(Vector256<sbyte> str, Vector256<sbyte> hiNibbles, Vector256<sbyte> maskSlashOrUnderscore, Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh, Vector256<sbyte> lutShift, Vector256<sbyte> shiftForUnderscore, out Vector256<sbyte> result) =>
                default(Base64DecoderByte).TryDecode256Core(str, hiNibbles, maskSlashOrUnderscore, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryLoadVector512(ReadOnlySpan<ushort> src, out Vector512<sbyte> str)
            {
                Vector512<ushort> utf16VectorLower = Vector512.Create(src);
                Vector512<ushort> utf16VectorUpper = Vector512.Create(src.Slice(32));
                if (Ascii.VectorContainsNonAsciiChar(utf16VectorLower | utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Ascii.ExtractAsciiVector(utf16VectorLower, utf16VectorUpper).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public bool TryLoadAvxVector256(ReadOnlySpan<ushort> src, out Vector256<sbyte> str)
            {
                Vector256<ushort> utf16VectorLower = Vector256.Create(src);
                Vector256<ushort> utf16VectorUpper = Vector256.Create(src.Slice(16));

                if (Ascii.VectorContainsNonAsciiChar(utf16VectorLower | utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Ascii.ExtractAsciiVector(utf16VectorLower, utf16VectorUpper).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryLoadVector128(ReadOnlySpan<ushort> src, out Vector128<byte> str)
            {
                Vector128<ushort> utf16VectorLower = Vector128.Create(src);
                Vector128<ushort> utf16VectorUpper = Vector128.Create(src.Slice(8));
                if (Ascii.VectorContainsNonAsciiChar(utf16VectorLower | utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Ascii.ExtractAsciiVector(utf16VectorLower, utf16VectorUpper);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public bool TryLoadArmVector128x4(ReadOnlySpan<ushort> src,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4)
            {
                var (s11, s12, s21, s22) = LoadArmVector128x4(src);
                var (s31, s32, s41, s42) = LoadArmVector128x4(src.Slice(32));

                if (Ascii.VectorContainsNonAsciiChar(s11 | s12 | s21 | s22 | s31 | s32 | s41 | s42))
                {
                    str1 = str2 = str3 = str4 = default;
                    return false;
                }

                str1 = Ascii.ExtractAsciiVector(s11, s31);
                str2 = Ascii.ExtractAsciiVector(s12, s32);
                str3 = Ascii.ExtractAsciiVector(s21, s41);
                str4 = Ascii.ExtractAsciiVector(s22, s42);

                return true;
            }
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int DecodeFourElements(ReadOnlySpan<ushort> source, ReadOnlySpan<sbyte> decodingMap)
            {
                // The 'source' span expected to have at least 4 elements, and the 'decodingMap' consists 256 sbytes
                uint t0 = source[0];
                uint t1 = source[1];
                uint t2 = source[2];
                uint t3 = source[3];

                if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
                {
                    return -1; // One or more chars falls outside the 00..ff range, invalid Base64 character.
                }

                int i0 = decodingMap[(byte)t0];
                int i1 = decodingMap[(byte)t1];
                int i2 = decodingMap[(byte)t2];
                int i3 = decodingMap[(byte)t3];

                i0 <<= 18;
                i1 <<= 12;
                i2 <<= 6;

                i0 |= i3;
                i1 |= i2;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int DecodeRemaining(ReadOnlySpan<ushort> srcEnd, ReadOnlySpan<sbyte> decodingMap, int remaining, out uint t2, out uint t3)
            {
                uint t0;
                uint t1;
                t2 = EncodingPad;
                t3 = EncodingPad;
                switch (remaining)
                {
                    case 2 when srcEnd.Length >= 2:
                        t0 = srcEnd[0];
                        t1 = srcEnd[1];
                        break;
                    case 3 when srcEnd.Length >= 3:
                        t0 = srcEnd[0];
                        t1 = srcEnd[1];
                        t2 = srcEnd[2];
                        break;
                    case 4 when srcEnd.Length >= 4:
                        t0 = srcEnd[0];
                        t1 = srcEnd[1];
                        t2 = srcEnd[2];
                        t3 = srcEnd[3];
                        break;
                    default:
                        return -1;
                }

                if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
                {
                    return -1;
                }

                int i0 = decodingMap[(byte)t0];
                int i1 = decodingMap[(byte)t1];

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<ushort> span)
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
            public OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<ushort> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true) where TBase64Decoder : IBase64Decoder<ushort> =>
                DecodeWithWhiteSpaceBlockwise(default(Base64DecoderChar), source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
        }
    }
}
