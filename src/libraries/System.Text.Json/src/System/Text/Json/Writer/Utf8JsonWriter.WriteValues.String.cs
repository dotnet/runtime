// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        private const byte HighSurrogateByteSentinel = 0xFF;
        private const char HighSurrogateCharSentinel = (char)(HighSurrogateByteSentinel<<8 | HighSurrogateByteSentinel);

        private int _partialStringSegmentChar;

        /// <summary>
        /// Writes the pre-encoded text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The JSON-encoded value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(JsonEncodedText value)
        {
            ReadOnlySpan<byte> utf8Value = value.EncodedUtf8Bytes;
            Debug.Assert(utf8Value.Length <= JsonConstants.MaxUnescapedTokenSize);

            WriteStringByOptions(utf8Value, JsonTokenType.String);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the string text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// <para>
        /// The value is escaped before writing.</para>
        /// <para>
        /// If <paramref name="value"/> is <see langword="null"/> the JSON null value is written,
        /// as if <see cref="WriteNullValue"/> was called.
        /// </para>
        /// </remarks>
        public void WriteStringValue(string? value)
        {
            if (value == null)
            {
                WriteNullValue();
            }
            else
            {
                WriteStringValue(value.AsSpan());
            }
        }

        /// <summary>
        /// Writes the text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        public void WriteStringValue(ReadOnlySpan<char> value)
        {
            JsonWriterHelper.ValidateValue(value);

            WriteStringEscape(value, JsonTokenType.String);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the text value segment as a partial JSON string.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="isFinalSegment">Indicates that this is the final segment of the string.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        public void WriteStringValueSegment(ReadOnlySpan<char> value, bool isFinalSegment)
        {
            JsonWriterHelper.ValidateValue(value);

            JsonTokenType nextTokenType = isFinalSegment ? JsonTokenType.String : StringSegmentSentinel;

            // If we have a high surrogate left over from the last segment we need to make sure it's written out. When
            // the first character of the current segment is a low surrogate we'll write as a complete pair, otherwise
            // we'll write it on its own.
            if (_partialStringSegmentChar != 0)
            {
                // Unfortunately we cannot use MemoryMarshal.CreateSpan here because it is not available in netstandard2.0.
                unsafe
                {
                    fixed (int* partialStringSegmentCharPtr = &_partialStringSegmentChar)
                    {
                        Span<char> partialStringSegmentChar = new Span<char>(partialStringSegmentCharPtr, 2);
                        if (partialStringSegmentChar[1] == HighSurrogateCharSentinel)
                        {
                            if (value.Length > 0 && char.IsLowSurrogate(value[0]))
                            {
                                partialStringSegmentChar[1] = value[0];
                                WriteStringEscape(partialStringSegmentChar, StringSegmentSentinel);
                                value = value.Slice(1);
                            }
                            else
                            {
                                // The caller sent a high surrogate on the previous call to this method, but did not provide a
                                // low surrogate on the this call. We should handle it gracefully.
                                WriteStringEscape(partialStringSegmentChar.Slice(0, 1), StringSegmentSentinel);
                            }
                        }
                        else
                        {
                            // The caller sent a partial UTF-8 sequence on a previous call to WriteStringValueSegment(byte) but
                            // switched to calling WriteStringValueSegment(char) on this call. We should handle this gracefully.
                            Span<byte> partialStringSegmentUtf8Bytes = MemoryMarshal.Cast<char, byte>(partialStringSegmentChar);
                            WriteStringEscape(partialStringSegmentUtf8Bytes.Slice(0, partialStringSegmentUtf8Bytes[3]), StringSegmentSentinel);
                        }
                    }
                }

                _partialStringSegmentChar = 0;
            }

            // If the last character of the segment is a high surrogate we need to cache it and write the rest of the
            // string. The cached value will be written when the next segment is written.
            if (!isFinalSegment && value.Length > 0)
            {
                char finalChar = value[value.Length - 1];
                if (char.IsHighSurrogate(finalChar))
                {
                    // Unfortunately we cannot use MemoryMarshal.CreateSpan here because it is not available in netstandard2.0.
                    unsafe
                    {
                        fixed (int* partialStringSegmentCharPtr = &_partialStringSegmentChar)
                        {
                            Span<char> partialStringSegmentChar = new Span<char>(partialStringSegmentCharPtr, 2);
                            partialStringSegmentChar[0] = finalChar;
                            partialStringSegmentChar[1] = HighSurrogateCharSentinel;
                        }
                    }

                    value = value.Slice(0, value.Length - 1);
                }
            }

            WriteStringEscape(value, nextTokenType);

            if (isFinalSegment)
            {
                SetFlagToAddListSeparatorBeforeNextItem();
            }

            _tokenType = nextTokenType;
        }

        private void WriteStringEscape(ReadOnlySpan<char> value, JsonTokenType stringTokenType)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < value.Length);

            if (valueIdx != -1)
            {
                WriteStringEscapeValue(value, valueIdx, stringTokenType);
            }
            else
            {
                WriteStringByOptions(value, stringTokenType);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<char> value, JsonTokenType stringTokenType)
        {
            if (!_options.SkipValidation && _tokenType != StringSegmentSentinel)
            {
                ValidateWritingValue();
            }

            if (_options.Indented)
            {
                WriteStringIndented(value, stringTokenType);
            }
            else
            {
                WriteStringMinimized(value, stringTokenType);
            }
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringMinimized(ReadOnlySpan<char> escapedValue, JsonTokenType stringTokenType)
        {
            Debug.Assert(escapedValue.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - 3);

            // All ASCII, 2 quotes => escapedValue.Length + 2
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = (escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 3;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_tokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                if (_currentDepth < 0)
                {
                    output[BytesPending++] = JsonConstants.ListSeparator;
                }

                output[BytesPending++] = JsonConstants.Quote;
            }

            TranscodeAndWrite(escapedValue, output);

            if (stringTokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                output[BytesPending++] = JsonConstants.Quote;
            }
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringIndented(ReadOnlySpan<char> escapedValue, JsonTokenType stringTokenType)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedValue.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - indent - 3 - _newLineLength);

            // All ASCII, 2 quotes => indent + escapedValue.Length + 2
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 3 + _newLineLength;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_tokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                if (_currentDepth < 0)
                {
                    output[BytesPending++] = JsonConstants.ListSeparator;
                }

                if (_tokenType != JsonTokenType.PropertyName && _tokenType != Utf8JsonWriter.StringSegmentSentinel)
                {
                    if (_tokenType != JsonTokenType.None)
                    {
                        WriteNewLine(output);
                    }
                    WriteIndentation(output.Slice(BytesPending), indent);
                    BytesPending += indent;
                }

                output[BytesPending++] = JsonConstants.Quote;
            }

            TranscodeAndWrite(escapedValue, output);

            if (stringTokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                output[BytesPending++] = JsonConstants.Quote;
            }
        }

        private void WriteStringEscapeValue(
            ReadOnlySpan<char> value,
            int firstEscapeIndexVal,
            JsonTokenType stringTokenType)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < value.Length);

            char[]? valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

            Span<char> escapedValue = length <= JsonConstants.StackallocCharThreshold ?
                stackalloc char[JsonConstants.StackallocCharThreshold] :
                (valueArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int written);

            WriteStringByOptions(escapedValue.Slice(0, written), stringTokenType);

            if (valueArray != null)
            {
                ArrayPool<char>.Shared.Return(valueArray);
            }
        }

        /// <summary>
        /// Writes the UTF-8 text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string element of a JSON array.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        public void WriteStringValue(ReadOnlySpan<byte> utf8Value)
        {
            JsonWriterHelper.ValidateValue(utf8Value);

            WriteStringEscape(utf8Value, JsonTokenType.String);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the UTF-8 text value segment as a partial JSON string.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string element of a JSON array.</param>
        /// <param name="isFinalSegment">Indicates that this is the final segment of the string.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        public void WriteStringValueSegment(ReadOnlySpan<byte> utf8Value, bool isFinalSegment)
        {
            JsonWriterHelper.ValidateValue(utf8Value);

            JsonTokenType nextTokenType = isFinalSegment ? JsonTokenType.String : Utf8JsonWriter.StringSegmentSentinel;

            if (_partialStringSegmentChar != 0)
            {
                // Unfortunately we cannot use MemoryMarshal.CreateSpan here because it is not available in netstandard2.0.
                unsafe
                {
                    fixed (int* partialStringSegmentCharPtr = &_partialStringSegmentChar)
                    {
                        Span<byte> partialStringSegmentUtf8Bytes = new Span<byte>(partialStringSegmentCharPtr, 4);
                        if (partialStringSegmentUtf8Bytes[3] == HighSurrogateByteSentinel)
                        {
                            // The caller sent a high surrogate on a previous call to WriteStringValueSegment(char) but switched
                            // to calling WriteStringValueSegment(byte) on this call. We'll handle this gracefully by writing the
                            // high surrogate on its own.
                            Span<char> surrogatePair = MemoryMarshal.Cast<byte, char>(partialStringSegmentUtf8Bytes);
                            WriteStringEscape(surrogatePair.Slice(0, 1), StringSegmentSentinel);
                        }
                        else
                        {
                            // Attempt to complete the UTF-8 sequence from the previous segment.
                            int requiredByteCount = JsonWriterHelper.GetUtf8CharByteCount(partialStringSegmentUtf8Bytes[0]);
                            int remainingByteCount = requiredByteCount - partialStringSegmentUtf8Bytes[3];
                            int availableByteCount = Math.Min(remainingByteCount, utf8Value.Length);

                            for (int i = 0; i < availableByteCount; i++)
                            {
                                int nextByteIndex = partialStringSegmentUtf8Bytes[3] + i;

                                byte remainingByte = utf8Value[0];
                                if (JsonWriterHelper.GetUtf8CharByteCount(remainingByte) != 0)
                                {
                                    // Invalid UTF-8 sequence! Write what we cached without trying to complete the sequence.
                                    requiredByteCount = nextByteIndex;
                                    remainingByteCount = 0;
                                    break;
                                }

                                partialStringSegmentUtf8Bytes[nextByteIndex] = remainingByte;
                                remainingByteCount--;
                                utf8Value = utf8Value.Slice(1);
                            }

                            if (isFinalSegment || remainingByteCount == 0)
                            {
                                WriteStringEscape(partialStringSegmentUtf8Bytes.Slice(0, requiredByteCount), StringSegmentSentinel);
                            }
                            else
                            {
                                // We didn't have enough to complete the sequence, so update the count of bytes we do have so that
                                // the next iteration will pick up where we left off.
                                partialStringSegmentUtf8Bytes[3] = (byte)(requiredByteCount - remainingByteCount);
                            }
                        }
                    }
                }
            }

            if (!isFinalSegment && utf8Value.Length > 0)
            {
                int expectedUtf8ByteCount = 0;
                int startOfPartialUtf8Sequence = -1;
                for (int i = utf8Value.Length - 1; i >= utf8Value.Length - 3; i--)
                {
                    expectedUtf8ByteCount = JsonWriterHelper.GetUtf8CharByteCount(utf8Value[i]);
                    if (expectedUtf8ByteCount == 0)
                    {
                        continue;
                    }

                    if (expectedUtf8ByteCount > 1)
                    {
                        startOfPartialUtf8Sequence = i;
                    }

                    break;
                }

                if (startOfPartialUtf8Sequence >= 0)
                {
                    // Unfortunately we cannot use MemoryMarshal.CreateSpan here because it is not available in netstandard2.0.
                    unsafe
                    {
                        fixed (int* partialStringSegmentCharPtr = &_partialStringSegmentChar)
                        {
                            Span<byte> partialStringSegmentUtf8Bytes = new Span<byte>(partialStringSegmentCharPtr, 4);
                            ReadOnlySpan<byte> bytesToWrite = utf8Value.Slice(startOfPartialUtf8Sequence);
                            bytesToWrite.CopyTo(partialStringSegmentUtf8Bytes);
                            partialStringSegmentUtf8Bytes[3] = (byte)bytesToWrite.Length;
                        }
                    }

                    utf8Value = utf8Value.Slice(0, startOfPartialUtf8Sequence);
                }
            }

            WriteStringEscape(utf8Value, nextTokenType);

            if (isFinalSegment)
            {
                SetFlagToAddListSeparatorBeforeNextItem();
            }

            _tokenType = nextTokenType;
        }

        private void WriteStringEscape(ReadOnlySpan<byte> utf8Value, JsonTokenType stringTokenType)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < utf8Value.Length);

            if (valueIdx != -1)
            {
                WriteStringEscapeValue(utf8Value, valueIdx, stringTokenType);
            }
            else
            {
                WriteStringByOptions(utf8Value, stringTokenType);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<byte> utf8Value, JsonTokenType stringTokenType)
        {
            if (!_options.SkipValidation && _tokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                ValidateWritingValue();
            }

            if (_options.Indented)
            {
                WriteStringIndented(utf8Value, stringTokenType);
            }
            else
            {
                WriteStringMinimized(utf8Value, stringTokenType);
            }
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringMinimized(ReadOnlySpan<byte> escapedValue, JsonTokenType stringTokenType)
        {
            Debug.Assert(escapedValue.Length < int.MaxValue - 3);

            int minRequired = escapedValue.Length + 2; // 2 quotes
            int maxRequired = minRequired + 1; // Optionally, 1 list separator

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_tokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                if (_currentDepth < 0)
                {
                    output[BytesPending++] = JsonConstants.ListSeparator;
                }
                output[BytesPending++] = JsonConstants.Quote;
            }

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            if (stringTokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                output[BytesPending++] = JsonConstants.Quote;
            }
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringIndented(ReadOnlySpan<byte> escapedValue, JsonTokenType stringTokenType)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedValue.Length < int.MaxValue - indent - 3 - _newLineLength);

            int minRequired = indent + escapedValue.Length + 2; // 2 quotes
            int maxRequired = minRequired + 1 + _newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_tokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                if (_currentDepth < 0)
                {
                    output[BytesPending++] = JsonConstants.ListSeparator;
                }

                if (_tokenType != JsonTokenType.PropertyName)
                {
                    if (_tokenType != JsonTokenType.None)
                    {
                        WriteNewLine(output);
                    }
                    WriteIndentation(output.Slice(BytesPending), indent);
                    BytesPending += indent;
                }

                output[BytesPending++] = JsonConstants.Quote;
            }

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            if (stringTokenType != Utf8JsonWriter.StringSegmentSentinel)
            {
                output[BytesPending++] = JsonConstants.Quote;
            }
        }

        private void WriteStringEscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal, JsonTokenType stringTokenType)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

            byte[]? valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

            Span<byte> escapedValue = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int written);

            WriteStringByOptions(escapedValue.Slice(0, written), stringTokenType);

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }
        }

        /// <summary>
        /// Writes a number as a JSON string. The string value is not escaped.
        /// </summary>
        /// <param name="utf8Value"></param>
        internal void WriteNumberValueAsStringUnescaped(ReadOnlySpan<byte> utf8Value)
        {
            // The value has been validated prior to calling this method.

            WriteStringByOptions(utf8Value, JsonTokenType.String);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }
    }
}
