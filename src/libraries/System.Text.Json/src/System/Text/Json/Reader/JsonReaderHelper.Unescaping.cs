// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        public static bool TryGetUnescapedBase64Bytes(ReadOnlySpan<byte> utf8Source, [NotNullWhen(true)] out byte[]? bytes)
        {
            byte[]? unescapedArray = null;

            Span<byte> utf8Unescaped = utf8Source.Length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (unescapedArray = ArrayPool<byte>.Shared.Rent(utf8Source.Length));

            Unescape(utf8Source, utf8Unescaped, out int written);
            Debug.Assert(written > 0);

            utf8Unescaped = utf8Unescaped.Slice(0, written);
            Debug.Assert(!utf8Unescaped.IsEmpty);

            bool result = TryDecodeBase64InPlace(utf8Unescaped, out bytes!);

            if (unescapedArray != null)
            {
                utf8Unescaped.Clear();
                ArrayPool<byte>.Shared.Return(unescapedArray);
            }
            return result;
        }

        // Reject any invalid UTF-8 data rather than silently replacing.
        public static readonly UTF8Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // TODO: Similar to escaping, replace the unescaping logic with publicly shipping APIs from https://github.com/dotnet/runtime/issues/27919
        public static string GetUnescapedString(ReadOnlySpan<byte> utf8Source)
        {
            // The escaped name is always >= than the unescaped, so it is safe to use escaped name for the buffer length.
            int length = utf8Source.Length;
            byte[]? pooledName = null;

            Span<byte> utf8Unescaped = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (pooledName = ArrayPool<byte>.Shared.Rent(length));

            Unescape(utf8Source, utf8Unescaped, out int written);
            Debug.Assert(written > 0);

            utf8Unescaped = utf8Unescaped.Slice(0, written);
            Debug.Assert(!utf8Unescaped.IsEmpty);

            string utf8String = TranscodeHelper(utf8Unescaped);

            if (pooledName != null)
            {
                utf8Unescaped.Clear();
                ArrayPool<byte>.Shared.Return(pooledName);
            }

            return utf8String;
        }

        public static ReadOnlySpan<byte> GetUnescapedSpan(ReadOnlySpan<byte> utf8Source)
        {
            // The escaped name is always >= than the unescaped, so it is safe to use escaped name for the buffer length.
            int length = utf8Source.Length;
            byte[]? pooledName = null;

            Span<byte> utf8Unescaped = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (pooledName = ArrayPool<byte>.Shared.Rent(length));

            Unescape(utf8Source, utf8Unescaped, out int written);
            Debug.Assert(written > 0);

            ReadOnlySpan<byte> propertyName = utf8Unescaped.Slice(0, written).ToArray();
            Debug.Assert(!propertyName.IsEmpty);

            if (pooledName != null)
            {
                new Span<byte>(pooledName, 0, written).Clear();
                ArrayPool<byte>.Shared.Return(pooledName);
            }

            return propertyName;
        }

        public static bool UnescapeAndCompare(ReadOnlySpan<byte> utf8Source, ReadOnlySpan<byte> other)
        {
            Debug.Assert(utf8Source.Length >= other.Length && utf8Source.Length / JsonConstants.MaxExpansionFactorWhileEscaping <= other.Length);

            byte[]? unescapedArray = null;

            Span<byte> utf8Unescaped = utf8Source.Length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (unescapedArray = ArrayPool<byte>.Shared.Rent(utf8Source.Length));

            Unescape(utf8Source, utf8Unescaped, 0, out int written);
            Debug.Assert(written > 0);

            utf8Unescaped = utf8Unescaped.Slice(0, written);
            Debug.Assert(!utf8Unescaped.IsEmpty);

            bool result = other.SequenceEqual(utf8Unescaped);

            if (unescapedArray != null)
            {
                utf8Unescaped.Clear();
                ArrayPool<byte>.Shared.Return(unescapedArray);
            }

            return result;
        }

        public static bool UnescapeAndCompare(ReadOnlySequence<byte> utf8Source, ReadOnlySpan<byte> other)
        {
            Debug.Assert(!utf8Source.IsSingleSegment);
            Debug.Assert(utf8Source.Length >= other.Length && utf8Source.Length / JsonConstants.MaxExpansionFactorWhileEscaping <= other.Length);

            byte[]? escapedArray = null;
            byte[]? unescapedArray = null;

            int length = checked((int)utf8Source.Length);

            Span<byte> utf8Unescaped = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            Span<byte> utf8Escaped = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (escapedArray = ArrayPool<byte>.Shared.Rent(length));

            utf8Source.CopyTo(utf8Escaped);
            utf8Escaped = utf8Escaped.Slice(0, length);

            Unescape(utf8Escaped, utf8Unescaped, 0, out int written);
            Debug.Assert(written > 0);

            utf8Unescaped = utf8Unescaped.Slice(0, written);
            Debug.Assert(!utf8Unescaped.IsEmpty);

            bool result = other.SequenceEqual(utf8Unescaped);

            if (unescapedArray != null)
            {
                Debug.Assert(escapedArray != null);
                utf8Unescaped.Clear();
                ArrayPool<byte>.Shared.Return(unescapedArray);
                utf8Escaped.Clear();
                ArrayPool<byte>.Shared.Return(escapedArray);
            }

            return result;
        }

        public static bool TryDecodeBase64InPlace(Span<byte> utf8Unescaped, [NotNullWhen(true)] out byte[]? bytes)
        {
            OperationStatus status = Base64.DecodeFromUtf8InPlace(utf8Unescaped, out int bytesWritten);
            if (status != OperationStatus.Done)
            {
                bytes = null;
                return false;
            }
            bytes = utf8Unescaped.Slice(0, bytesWritten).ToArray();
            return true;
        }

        public static bool TryDecodeBase64(ReadOnlySpan<byte> utf8Unescaped, [NotNullWhen(true)] out byte[]? bytes)
        {
            byte[]? pooledArray = null;

            Span<byte> byteSpan = utf8Unescaped.Length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (pooledArray = ArrayPool<byte>.Shared.Rent(utf8Unescaped.Length));

            OperationStatus status = Base64.DecodeFromUtf8(utf8Unescaped, byteSpan, out int bytesConsumed, out int bytesWritten);

            if (status != OperationStatus.Done)
            {
                bytes = null;

                if (pooledArray != null)
                {
                    byteSpan.Clear();
                    ArrayPool<byte>.Shared.Return(pooledArray);
                }

                return false;
            }
            Debug.Assert(bytesConsumed == utf8Unescaped.Length);

            bytes = byteSpan.Slice(0, bytesWritten).ToArray();

            if (pooledArray != null)
            {
                byteSpan.Clear();
                ArrayPool<byte>.Shared.Return(pooledArray);
            }

            return true;
        }

        public static string TranscodeHelper(ReadOnlySpan<byte> utf8Unescaped)
        {
            try
            {
#if BUILDING_INBOX_LIBRARY
                return s_utf8Encoding.GetString(utf8Unescaped);
#else
                if (utf8Unescaped.IsEmpty)
                {
                    return string.Empty;
                }
                unsafe
                {
                    fixed (byte* bytePtr = utf8Unescaped)
                    {
                        return s_utf8Encoding.GetString(bytePtr, utf8Unescaped.Length);
                    }
                }
#endif
            }
            catch (DecoderFallbackException ex)
            {
                // We want to be consistent with the exception being thrown
                // so the user only has to catch a single exception.
                // Since we already throw InvalidOperationException for mismatch token type,
                // and while unescaping, using that exception for failure to decode invalid UTF-8 bytes as well.
                // Therefore, wrapping the DecoderFallbackException around an InvalidOperationException.
                throw ThrowHelper.GetInvalidOperationException_ReadInvalidUTF8(ex);
            }
        }

        public static int TranscodeHelper(ReadOnlySpan<byte> utf8Unescaped, Span<char> destination)
        {
            try
            {
#if BUILDING_INBOX_LIBRARY
                return s_utf8Encoding.GetChars(utf8Unescaped, destination);
#else
                if (utf8Unescaped.IsEmpty)
                {
                    return 0;
                }
                unsafe
                {
                    fixed (byte* srcPtr = utf8Unescaped)
                    fixed (char* destPtr = destination)
                    {
                        return s_utf8Encoding.GetChars(srcPtr, utf8Unescaped.Length, destPtr, destination.Length);
                    }
                }
#endif
            }
            catch (DecoderFallbackException dfe)
            {
                // We want to be consistent with the exception being thrown
                // so the user only has to catch a single exception.
                // Since we already throw InvalidOperationException for mismatch token type,
                // and while unescaping, using that exception for failure to decode invalid UTF-8 bytes as well.
                // Therefore, wrapping the DecoderFallbackException around an InvalidOperationException.
                throw ThrowHelper.GetInvalidOperationException_ReadInvalidUTF8(dfe);
            }
            catch (ArgumentException)
            {
                // Destination buffer was too small; clear it up since the encoder might have not.
                destination.Clear();
                throw;
            }
        }

        public static void ValidateUtf8(ReadOnlySpan<byte> utf8Buffer)
        {
            try
            {
#if BUILDING_INBOX_LIBRARY
                s_utf8Encoding.GetCharCount(utf8Buffer);
#else
                if (utf8Buffer.IsEmpty)
                {
                    return;
                }
                unsafe
                {
                    fixed (byte* srcPtr = utf8Buffer)
                    {
                        s_utf8Encoding.GetCharCount(srcPtr, utf8Buffer.Length);
                    }
                }
#endif
            }
            catch (DecoderFallbackException ex)
            {
                // We want to be consistent with the exception being thrown
                // so the user only has to catch a single exception.
                // Since we already throw InvalidOperationException for mismatch token type,
                // and while unescaping, using that exception for failure to decode invalid UTF-8 bytes as well.
                // Therefore, wrapping the DecoderFallbackException around an InvalidOperationException.
                throw ThrowHelper.GetInvalidOperationException_ReadInvalidUTF8(ex);
            }
        }

        internal static int GetUtf8ByteCount(ReadOnlySpan<char> text)
        {
            try
            {
#if BUILDING_INBOX_LIBRARY
                return s_utf8Encoding.GetByteCount(text);
#else
                if (text.IsEmpty)
                {
                    return 0;
                }
                unsafe
                {
                    fixed (char* charPtr = text)
                    {
                        return s_utf8Encoding.GetByteCount(charPtr, text.Length);
                    }
                }
#endif
            }
            catch (EncoderFallbackException ex)
            {
                // We want to be consistent with the exception being thrown
                // so the user only has to catch a single exception.
                // Since we already throw ArgumentException when validating other arguments,
                // using that exception for failure to encode invalid UTF-16 chars as well.
                // Therefore, wrapping the EncoderFallbackException around an ArgumentException.
                throw ThrowHelper.GetArgumentException_ReadInvalidUTF16(ex);
            }
        }

        internal static int GetUtf8FromText(ReadOnlySpan<char> text, Span<byte> dest)
        {
            try
            {
#if BUILDING_INBOX_LIBRARY
                return s_utf8Encoding.GetBytes(text, dest);
#else
                if (text.IsEmpty)
                {
                    return 0;
                }

                unsafe
                {
                    fixed (char* charPtr = text)
                    fixed (byte* destPtr = dest)
                    {
                        return s_utf8Encoding.GetBytes(charPtr, text.Length, destPtr, dest.Length);
                    }
                }
#endif
            }
            catch (EncoderFallbackException ex)
            {
                // We want to be consistent with the exception being thrown
                // so the user only has to catch a single exception.
                // Since we already throw ArgumentException when validating other arguments,
                // using that exception for failure to encode invalid UTF-16 chars as well.
                // Therefore, wrapping the EncoderFallbackException around an ArgumentException.
                throw ThrowHelper.GetArgumentException_ReadInvalidUTF16(ex);
            }
        }

        internal static string GetTextFromUtf8(ReadOnlySpan<byte> utf8Text)
        {
#if BUILDING_INBOX_LIBRARY
            return s_utf8Encoding.GetString(utf8Text);
#else
            if (utf8Text.IsEmpty)
            {
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* bytePtr = utf8Text)
                {
                    return s_utf8Encoding.GetString(bytePtr, utf8Text.Length);
                }
            }
#endif
        }

        internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
        {
            Debug.Assert(destination.Length >= source.Length);

            int idx = source.IndexOf(JsonConstants.BackSlash);
            Debug.Assert(idx >= 0);

            bool result = TryUnescape(source, destination, idx, out written);
            Debug.Assert(result);
        }

        internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, int idx, out int written)
        {
            Debug.Assert(idx >= 0 && idx < source.Length);
            Debug.Assert(source[idx] == JsonConstants.BackSlash);
            Debug.Assert(destination.Length >= source.Length);

            bool result = TryUnescape(source, destination, idx, out written);
            Debug.Assert(result);
        }

        /// <summary>
        /// Used when writing to buffers not guaranteed to fit the unescaped result.
        /// </summary>
        internal static bool TryUnescape(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
        {
            int idx = source.IndexOf(JsonConstants.BackSlash);
            Debug.Assert(idx >= 0);

            return TryUnescape(source, destination, idx, out written);
        }

        /// <summary>
        /// Used when writing to buffers not guaranteed to fit the unescaped result.
        /// </summary>
        private static bool TryUnescape(ReadOnlySpan<byte> source, Span<byte> destination, int idx, out int written)
        {
            Debug.Assert(idx >= 0 && idx < source.Length);
            Debug.Assert(source[idx] == JsonConstants.BackSlash);

            if (!source.Slice(0, idx).TryCopyTo(destination))
            {
                written = 0;
                goto DestinationTooShort;
            }

            written = idx;

            while (true)
            {
                Debug.Assert(source[idx] == JsonConstants.BackSlash);

                if (written == destination.Length)
                {
                    goto DestinationTooShort;
                }

                switch (source[++idx])
                {
                    case JsonConstants.Quote:
                        destination[written++] = JsonConstants.Quote;
                        break;
                    case (byte)'n':
                        destination[written++] = JsonConstants.LineFeed;
                        break;
                    case (byte)'r':
                        destination[written++] = JsonConstants.CarriageReturn;
                        break;
                    case JsonConstants.BackSlash:
                        destination[written++] = JsonConstants.BackSlash;
                        break;
                    case JsonConstants.Slash:
                        destination[written++] = JsonConstants.Slash;
                        break;
                    case (byte)'t':
                        destination[written++] = JsonConstants.Tab;
                        break;
                    case (byte)'b':
                        destination[written++] = JsonConstants.BackSpace;
                        break;
                    case (byte)'f':
                        destination[written++] = JsonConstants.FormFeed;
                        break;
                    default:
                        Debug.Assert(source[idx] == 'u', "invalid escape sequences must have already been caught by Utf8JsonReader.Read()");

                        // The source is known to be valid JSON, and hence if we see a \u, it is guaranteed to have 4 hex digits following it
                        // Otherwise, the Utf8JsonReader would have already thrown an exception.
                        Debug.Assert(source.Length >= idx + 5);

                        bool result = Utf8Parser.TryParse(source.Slice(idx + 1, 4), out int scalar, out int bytesConsumed, 'x');
                        Debug.Assert(result);
                        Debug.Assert(bytesConsumed == 4);
                        idx += 4;

                        if (JsonHelpers.IsInRangeInclusive((uint)scalar, JsonConstants.HighSurrogateStartValue, JsonConstants.LowSurrogateEndValue))
                        {
                            // The first hex value cannot be a low surrogate.
                            if (scalar >= JsonConstants.LowSurrogateStartValue)
                            {
                                ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(scalar);
                            }

                            Debug.Assert(JsonHelpers.IsInRangeInclusive((uint)scalar, JsonConstants.HighSurrogateStartValue, JsonConstants.HighSurrogateEndValue));

                            // We must have a low surrogate following a high surrogate.
                            if (source.Length < idx + 7 || source[idx + 1] != '\\' || source[idx + 2] != 'u')
                            {
                                ThrowHelper.ThrowInvalidOperationException_ReadIncompleteUTF16();
                            }

                            // The source is known to be valid JSON, and hence if we see a \u, it is guaranteed to have 4 hex digits following it
                            // Otherwise, the Utf8JsonReader would have already thrown an exception.
                            result = Utf8Parser.TryParse(source.Slice(idx + 3, 4), out int lowSurrogate, out bytesConsumed, 'x');
                            Debug.Assert(result);
                            Debug.Assert(bytesConsumed == 4);
                            idx += 6;

                            // If the first hex value is a high surrogate, the next one must be a low surrogate.
                            if (!JsonHelpers.IsInRangeInclusive((uint)lowSurrogate, JsonConstants.LowSurrogateStartValue, JsonConstants.LowSurrogateEndValue))
                            {
                                ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(lowSurrogate);
                            }

                            // To find the unicode scalar:
                            // (0x400 * (High surrogate - 0xD800)) + Low surrogate - 0xDC00 + 0x10000
                            scalar = (JsonConstants.BitShiftBy10 * (scalar - JsonConstants.HighSurrogateStartValue))
                                + (lowSurrogate - JsonConstants.LowSurrogateStartValue)
                                + JsonConstants.UnicodePlane01StartValue;
                        }

#if BUILDING_INBOX_LIBRARY
                        var rune = new Rune(scalar);
                        bool success = rune.TryEncodeToUtf8(destination.Slice(written), out int bytesWritten);
#else
                        bool success = TryEncodeToUtf8Bytes((uint)scalar, destination.Slice(written), out int bytesWritten);
#endif
                        if (!success)
                        {
                            goto DestinationTooShort;
                        }

                        Debug.Assert(bytesWritten <= 4);
                        written += bytesWritten;
                        break;
                }

                if (++idx == source.Length)
                {
                    goto Success;
                }

                if (source[idx] != JsonConstants.BackSlash)
                {
                    ReadOnlySpan<byte> remaining = source.Slice(idx);
                    int nextUnescapedSegmentLength = remaining.IndexOf(JsonConstants.BackSlash);
                    if (nextUnescapedSegmentLength < 0)
                    {
                        nextUnescapedSegmentLength = remaining.Length;
                    }

                    if ((uint)(written + nextUnescapedSegmentLength) >= (uint)destination.Length)
                    {
                        goto DestinationTooShort;
                    }

                    Debug.Assert(nextUnescapedSegmentLength > 0);
                    switch (nextUnescapedSegmentLength)
                    {
                        case 1:
                            destination[written++] = source[idx++];
                            break;
                        case 2:
                            destination[written++] = source[idx++];
                            destination[written++] = source[idx++];
                            break;
                        case 3:
                            destination[written++] = source[idx++];
                            destination[written++] = source[idx++];
                            destination[written++] = source[idx++];
                            break;
                        default:
                            remaining.Slice(0, nextUnescapedSegmentLength).CopyTo(destination.Slice(written));
                            written += nextUnescapedSegmentLength;
                            idx += nextUnescapedSegmentLength;
                            break;
                    }

                    Debug.Assert(idx == source.Length || source[idx] == JsonConstants.BackSlash);

                    if (idx == source.Length)
                    {
                        goto Success;
                    }
                }
            }

        Success:
            return true;

        DestinationTooShort:
            return false;
        }

#if !BUILDING_INBOX_LIBRARY
        /// <summary>
        /// Copies the UTF-8 code unit representation of this scalar to an output buffer.
        /// The buffer must be large enough to hold the required number of <see cref="byte"/>s.
        /// </summary>
        private static bool TryEncodeToUtf8Bytes(uint scalar, Span<byte> utf8Destination, out int bytesWritten)
        {
            Debug.Assert(JsonHelpers.IsValidUnicodeScalar(scalar));

            if (scalar < 0x80U)
            {
                // Single UTF-8 code unit
                if ((uint)utf8Destination.Length < 1u)
                {
                    bytesWritten = 0;
                    return false;
                }

                utf8Destination[0] = (byte)scalar;
                bytesWritten = 1;
            }
            else if (scalar < 0x800U)
            {
                // Two UTF-8 code units
                if ((uint)utf8Destination.Length < 2u)
                {
                    bytesWritten = 0;
                    return false;
                }

                utf8Destination[0] = (byte)(0xC0U | (scalar >> 6));
                utf8Destination[1] = (byte)(0x80U | (scalar & 0x3FU));
                bytesWritten = 2;
            }
            else if (scalar < 0x10000U)
            {
                // Three UTF-8 code units
                if ((uint)utf8Destination.Length < 3u)
                {
                    bytesWritten = 0;
                    return false;
                }

                utf8Destination[0] = (byte)(0xE0U | (scalar >> 12));
                utf8Destination[1] = (byte)(0x80U | ((scalar >> 6) & 0x3FU));
                utf8Destination[2] = (byte)(0x80U | (scalar & 0x3FU));
                bytesWritten = 3;
            }
            else
            {
                // Four UTF-8 code units
                if ((uint)utf8Destination.Length < 4u)
                {
                    bytesWritten = 0;
                    return false;
                }

                utf8Destination[0] = (byte)(0xF0U | (scalar >> 18));
                utf8Destination[1] = (byte)(0x80U | ((scalar >> 12) & 0x3FU));
                utf8Destination[2] = (byte)(0x80U | ((scalar >> 6) & 0x3FU));
                utf8Destination[3] = (byte)(0x80U | (scalar & 0x3FU));
                bytesWritten = 4;
            }

            return true;
        }
#endif
    }
}
