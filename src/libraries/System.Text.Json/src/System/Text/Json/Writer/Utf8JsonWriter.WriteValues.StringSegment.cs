// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the text value segment as a partial JSON string.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="isFinalSegment">Indicates that this is the final segment of the string.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled) or
        /// if the previously written segment (if any) was not written with this same overload.
        /// </exception>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        public void WriteStringValueSegment(ReadOnlySpan<char> value, bool isFinalSegment)
        {
            JsonWriterHelper.ValidateValue(value);

            if (!_options.SkipValidation)
            {
                ValidateWritingSegment(EnclosingContainerType.Utf16StringSequence);
            }

            if (_enclosingContainer != EnclosingContainerType.Utf16StringSequence)
            {
                WriteStringSegmentPrologue();
                _enclosingContainer = EnclosingContainerType.Utf16StringSequence;
            }

            // The steps to write a string segment are to complete the previous partial code point
            // and escape either of which might not be required so there is a fast path for each of these steps.
            if (HasPartialStringData)
            {
                WriteStringSegmentWithLeftover(value, isFinalSegment);
            }
            else
            {
                WriteStringSegmentEscape(value, isFinalSegment);
            }

            if (isFinalSegment)
            {
                WriteStringSegmentEpilogue();

                SetFlagToAddListSeparatorBeforeNextItem();
                EnclosingContainerType container = _bitStack.Peek() ? EnclosingContainerType.Object : EnclosingContainerType.Array;
                _enclosingContainer = _bitStack.CurrentDepth == 0 ? EnclosingContainerType.None : container;
                _tokenType = JsonTokenType.String;
            }
        }

        private void WriteStringSegmentWithLeftover(scoped ReadOnlySpan<char> value, bool isFinalSegment)
        {
            Debug.Assert(HasPartialStringData);
            Debug.Assert(_enclosingContainer == EnclosingContainerType.Utf16StringSequence);

            scoped ReadOnlySpan<char> partialStringDataBuffer = PartialUtf16StringData;

            Span<char> combinedBuffer = stackalloc char[2];
            combinedBuffer = combinedBuffer.Slice(0, ConcatInto(partialStringDataBuffer, value, combinedBuffer));

            switch (Rune.DecodeFromUtf16(combinedBuffer, out _, out int charsConsumed))
            {
                case OperationStatus.NeedMoreData:
                    Debug.Assert(value.Length + partialStringDataBuffer.Length < 2);
                    Debug.Assert(charsConsumed == value.Length + partialStringDataBuffer.Length);
                    // Let the encoder deal with the error if this is a final buffer.
                    value = combinedBuffer.Slice(0, charsConsumed);
                    partialStringDataBuffer = [];
                    break;
                case OperationStatus.Done:
                    Debug.Assert(charsConsumed > partialStringDataBuffer.Length);
                    Debug.Assert(charsConsumed <= 2);
                    // Divide up the code point chars into its own buffer and the remainder of the input buffer.
                    value = value.Slice(charsConsumed - partialStringDataBuffer.Length);
                    partialStringDataBuffer = combinedBuffer.Slice(0, charsConsumed);
                    break;
                case OperationStatus.InvalidData:
                    Debug.Assert(charsConsumed >= partialStringDataBuffer.Length);
                    Debug.Assert(charsConsumed <= 2);
                    value = value.Slice(charsConsumed - partialStringDataBuffer.Length);
                    partialStringDataBuffer = combinedBuffer.Slice(0, charsConsumed);
                    break;
                case OperationStatus.DestinationTooSmall:
                default:
                    Debug.Fail("Unexpected OperationStatus return value.");
                    break;
            }

            // The "isFinalSegment" argument indicates whether input that NeedsMoreData should be consumed as an error or not.
            // Because we have validated above that partialStringDataBuffer will be the next consumed chars during Rune decoding
            // (even if this is because it is invalid), we should pass isFinalSegment = true to indicate to the decoder to
            // parse the code units without extra data.
            //
            // This is relevant in the case of having ['\uD800', 'C'], where the validation above would have needed both code units
            // to determine that only the first unit should be consumed (as invalid). So this method will get only ['\uD800'].
            // Because we know more data will not be able to complete this code point, we need to pass isFinalSegment = true
            // to ensure that the encoder consumes this data eagerly instead of leaving it and returning NeedsMoreData.
            WriteStringSegmentEscape(partialStringDataBuffer, true);

            ClearPartialStringData();

            WriteStringSegmentEscape(value, isFinalSegment);
        }

        private void WriteStringSegmentEscape(ReadOnlySpan<char> value, bool isFinalSegment)
        {
            if (value.IsEmpty) return;

            int escapeIdx = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);
            if (escapeIdx != -1)
            {
                WriteStringSegmentEscapeValue(value, escapeIdx, isFinalSegment);
            }
            else
            {
                WriteStringSegmentData(value);
            }
        }

        private void WriteStringSegmentEscapeValue(ReadOnlySpan<char> value, int firstEscapeIndexVal, bool isFinalSegment)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < value.Length);

            char[]? valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

            Span<char> escapedValue = length <= JsonConstants.StackallocCharThreshold ?
                stackalloc char[JsonConstants.StackallocCharThreshold] :
                (valueArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int consumed, out int written, isFinalSegment);

            WriteStringSegmentData(escapedValue.Slice(0, written));

            Debug.Assert(consumed == value.Length || !isFinalSegment);
            if (value.Length != consumed)
            {
                Debug.Assert(!isFinalSegment);
                Debug.Assert(value.Length - consumed < 2);
                PartialUtf16StringData = value.Slice(consumed);
            }

            if (valueArray != null)
            {
                ArrayPool<char>.Shared.Return(valueArray);
            }
        }

        private void WriteStringSegmentData(ReadOnlySpan<char> escapedValue)
        {
            Debug.Assert(escapedValue.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding));

            int requiredBytes = escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding;

            if (_memory.Length - BytesPending < requiredBytes)
            {
                Grow(requiredBytes);
            }

            Span<byte> output = _memory.Span;

            TranscodeAndWrite(escapedValue, output);
        }

        /// <summary>
        /// Writes the UTF-8 text value segment as a partial JSON string.
        /// </summary>
        /// <param name="value">The UTF-8 encoded value to be written as a JSON string element of a JSON array.</param>
        /// <param name="isFinalSegment">Indicates that this is the final segment of the string.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled) or
        /// if the previously written segment (if any) was not written with this same overload.
        /// </exception>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        public void WriteStringValueSegment(ReadOnlySpan<byte> value, bool isFinalSegment)
        {
            JsonWriterHelper.ValidateValue(value);

            if (!_options.SkipValidation)
            {
                ValidateWritingSegment(EnclosingContainerType.Utf8StringSequence);
            }

            if (_enclosingContainer != EnclosingContainerType.Utf8StringSequence)
            {
                WriteStringSegmentPrologue();
                _enclosingContainer = EnclosingContainerType.Utf8StringSequence;
            }

            // The steps to write a string segment are to complete the previous partial code point
            // and escape either of which might not be required so there is a fast path for each of these steps.
            if (HasPartialStringData)
            {
                WriteStringSegmentWithLeftover(value, isFinalSegment);
            }
            else
            {
                WriteStringSegmentEscape(value, isFinalSegment);
            }

            if (isFinalSegment)
            {
                WriteStringSegmentEpilogue();

                SetFlagToAddListSeparatorBeforeNextItem();
                EnclosingContainerType container = _bitStack.Peek() ? EnclosingContainerType.Object : EnclosingContainerType.Array;
                _enclosingContainer = _bitStack.CurrentDepth == 0 ? EnclosingContainerType.None : container;
                _tokenType = JsonTokenType.String;
            }
        }

        private void WriteStringSegmentWithLeftover(scoped ReadOnlySpan<byte> utf8Value, bool isFinalSegment)
        {
            Debug.Assert(HasPartialStringData);
            Debug.Assert(_enclosingContainer == EnclosingContainerType.Utf8StringSequence);

            scoped ReadOnlySpan<byte> partialStringDataBuffer = PartialUtf8StringData;

            Span<byte> combinedBuffer = stackalloc byte[4];
            combinedBuffer = combinedBuffer.Slice(0, ConcatInto(partialStringDataBuffer, utf8Value, combinedBuffer));

            switch (Rune.DecodeFromUtf8(combinedBuffer, out _, out int bytesConsumed))
            {
                case OperationStatus.NeedMoreData:
                    Debug.Assert(utf8Value.Length + partialStringDataBuffer.Length < 4);
                    Debug.Assert(bytesConsumed == utf8Value.Length + partialStringDataBuffer.Length);
                    // Let the encoder deal with the error if this is a final buffer.
                    utf8Value = combinedBuffer.Slice(0, bytesConsumed);
                    partialStringDataBuffer = [];
                    break;
                case OperationStatus.Done:
                    Debug.Assert(bytesConsumed > partialStringDataBuffer.Length);
                    Debug.Assert(bytesConsumed <= 4);
                    // Divide up the code point bytes into its own buffer and the remainder of the input buffer.
                    utf8Value = utf8Value.Slice(bytesConsumed - partialStringDataBuffer.Length);
                    partialStringDataBuffer = combinedBuffer.Slice(0, bytesConsumed);
                    break;
                case OperationStatus.InvalidData:
                    Debug.Assert(bytesConsumed >= partialStringDataBuffer.Length);
                    Debug.Assert(bytesConsumed <= 4);
                    utf8Value = utf8Value.Slice(bytesConsumed - partialStringDataBuffer.Length);
                    partialStringDataBuffer = combinedBuffer.Slice(0, bytesConsumed);
                    break;
                case OperationStatus.DestinationTooSmall:
                default:
                    Debug.Fail("Unexpected OperationStatus return value.");
                    break;
            }

            // The "isFinalSegment" argument indicates whether input that NeedsMoreData should be consumed as an error or not.
            // Because we have validated above that partialStringDataBuffer will be the next consumed bytes during Rune decoding
            // (even if this is because it is invalid), we should pass isFinalSegment = true to indicate to the decoder to
            // parse the code units without extra data.
            //
            // This is relevant in the case of having [<3-length prefix code unit>, <continuation>, <ascii>], where the validation
            // above would have needed all 3 code units to determine that only the first 2 units should be consumed (as invalid).
            // So this method will get only <3-size prefix code unit><continuation>. Because we know more data will not be able
            // to complete this code point, we need to pass isFinalSegment = true to ensure that the encoder consumes this data eagerly
            // instead of leaving it and returning NeedsMoreData.
            WriteStringSegmentEscape(partialStringDataBuffer, true);

            ClearPartialStringData();

            WriteStringSegmentEscape(utf8Value, isFinalSegment);
        }

        private void WriteStringSegmentEscape(ReadOnlySpan<byte> utf8Value, bool isFinalSegment)
        {
            if (utf8Value.IsEmpty) return;

            int escapeIdx = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);
            if (escapeIdx != -1)
            {
                WriteStringSegmentEscapeValue(utf8Value, escapeIdx, isFinalSegment);
            }
            else
            {
                WriteStringSegmentData(utf8Value);
            }
        }

        private void WriteStringSegmentEscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal, bool isFinalSegment)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);
            byte[]? valueArray = null;
            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);
            Span<byte> escapedValue = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int consumed, out int written, isFinalSegment);

            WriteStringSegmentData(escapedValue.Slice(0, written));

            Debug.Assert(consumed == utf8Value.Length || !isFinalSegment);
            if (utf8Value.Length != consumed)
            {
                Debug.Assert(!isFinalSegment);
                Debug.Assert(utf8Value.Length - consumed < 4);
                PartialUtf8StringData = utf8Value.Slice(consumed);
            }

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }
        }

        private void WriteStringSegmentData(ReadOnlySpan<byte> escapedValue)
        {
            Debug.Assert(escapedValue.Length < int.MaxValue - 3);

            int requiredBytes = escapedValue.Length;

            if (_memory.Length - BytesPending < requiredBytes)
            {
                Grow(requiredBytes);
            }

            Span<byte> output = _memory.Span;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;
        }

        /// <summary>
        /// Writes the input bytes as a partial JSON string.
        /// </summary>
        /// <param name="value">The bytes to be written as a JSON string element of a JSON array.</param>
        /// <param name="isFinalSegment">Indicates that this is the final segment of the string.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled) or
        /// if the previously written segment (if any) was not written with this same overload.
        /// </exception>
        public void WriteBase64StringSegment(ReadOnlySpan<byte> value, bool isFinalSegment)
        {
            if (value.Length > Base64.GetMaxDecodedFromUtf8Length(int.MaxValue))
            {
                ThrowHelper.ThrowArgumentException_ValueTooLarge(value.Length);
            }

            if (!_options.SkipValidation)
            {
                ValidateWritingSegment(EnclosingContainerType.Base64StringSequence);
            }

            if (_enclosingContainer != EnclosingContainerType.Base64StringSequence)
            {
                WriteStringSegmentPrologue();
                _enclosingContainer = EnclosingContainerType.Base64StringSequence;
            }

            // The steps to write a string segment are to complete the previous partial string data
            // and escape either of which might not be required so there is a fast path for each of these steps.
            if (HasPartialStringData)
            {
                WriteBase64StringSegmentWithLeftover(value, isFinalSegment);
            }
            else
            {
                WriteBase64StringSegmentData(value, isFinalSegment);
            }

            if (isFinalSegment)
            {
                WriteStringSegmentEpilogue();

                SetFlagToAddListSeparatorBeforeNextItem();
                EnclosingContainerType container = _bitStack.Peek() ? EnclosingContainerType.Object : EnclosingContainerType.Array;
                _enclosingContainer = _bitStack.CurrentDepth == 0 ? EnclosingContainerType.None : container;
                _tokenType = JsonTokenType.String;
            }
        }

        private void WriteBase64StringSegmentWithLeftover(scoped ReadOnlySpan<byte> bytes, bool isFinalSegment)
        {
            Debug.Assert(HasPartialStringData);
            Debug.Assert(_enclosingContainer == EnclosingContainerType.Base64StringSequence);

            scoped ReadOnlySpan<byte> partialStringDataBuffer = PartialBase64StringData;

            Span<byte> combinedBuffer = stackalloc byte[3];
            combinedBuffer = combinedBuffer.Slice(0, ConcatInto(partialStringDataBuffer, bytes, combinedBuffer));
            if (combinedBuffer.Length is 3)
            {
                // Divide up the partial bytes into its own buffer and the remainder of the input buffer.
                bytes = bytes.Slice(3 - partialStringDataBuffer.Length);
                partialStringDataBuffer = combinedBuffer.Slice(0, 3);
            }
            else
            {
                Debug.Assert(combinedBuffer.Length is 1 or 2);
                // Need more data. If this is a final segment, then the encoder will append '=' as needed.
                Debug.Assert(bytes.Length + partialStringDataBuffer.Length < 3);
                bytes = combinedBuffer;
                partialStringDataBuffer = [];
            }

            // It doesn't matter if we pass true or false for isFinalSegment since we are guaranteed to not have partial data
            // here (it is either empty or completed using the combined buffer above).
            WriteBase64StringSegmentData(partialStringDataBuffer, false);

            ClearPartialStringData();

            WriteBase64StringSegmentData(bytes, isFinalSegment);
        }

        private void WriteBase64StringSegmentData(ReadOnlySpan<byte> bytes, bool isFinalSegment)
        {
            int leftoverSize;
            if (!isFinalSegment && (leftoverSize = bytes.Length % 3) != 0)
            {
                // If this is not the final segment, we need to wait for more data to come in.
                PartialBase64StringData = bytes.Slice(bytes.Length - leftoverSize);
                bytes = bytes.Slice(0, bytes.Length - leftoverSize);
            }

            if (bytes.Length == 0)
            {
                return;
            }

            int requiredBytes = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

            if (_memory.Length - BytesPending < requiredBytes)
            {
                Grow(requiredBytes);
            }

            Span<byte> output = _memory.Span;

            // For non-final segments, the input is sliced to be a multiple of 3 bytes above which guarantees
            // that the base64 encoding will never end with padding since 3x input bytes turn into exactly 4x base64 bytes.
            Base64EncodeAndWrite(bytes, output);
        }

        private void WriteStringSegmentPrologue()
        {
            if (_options.Indented)
            {
                WriteStringSegmentIndentedPrologue();
            }
            else
            {
                WriteStringSegmentMinimizedPrologue();
            }
        }

        private void WriteStringSegmentIndentedPrologue()
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            // One quote and optionally 1 indent, 1 list separator and 1-2 bytes for new line
            int bytesRequired = 1 + indent + 1 + _newLineLength;
            if (_memory.Length - BytesPending < bytesRequired)
            {
                Grow(bytesRequired);
            }

            Span<byte> output = _memory.Span;

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

        private void WriteStringSegmentMinimizedPrologue()
        {
            // One quote and optionally 1 list separator
            int bytesRequired = 2;
            if (_memory.Length - BytesPending < bytesRequired)
            {
                Grow(bytesRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteStringSegmentEpilogue()
        {
            if (_memory.Length == BytesPending)
            {
                Grow(1);
            }

            _memory.Span[BytesPending++] = JsonConstants.Quote;
        }

        /// <summary>
        /// Given a byte buffer <paramref name="dest"/>, concatenates as much of <paramref name="srcLeft"/> followed
        /// by <paramref name="srcRight"/> into it as will fit, then returns the total number of bytes copied.
        /// </summary>
        private static int ConcatInto<T>(ReadOnlySpan<T> srcLeft, ReadOnlySpan<T> srcRight, Span<T> dest)
        {
            int total = 0;
            for (int i = 0; i < srcLeft.Length; i++)
            {
                if ((uint)total >= (uint)dest.Length)
                {
                    goto Finish;
                }
                else
                {
                    dest[total++] = srcLeft[i];
                }
            }
            for (int i = 0; i < srcRight.Length; i++)
            {
                if ((uint)total >= (uint)dest.Length)
                {
                    goto Finish;
                }
                else
                {
                    dest[total++] = srcRight[i];
                }
            }
        Finish:
            return total;
        }
    }
}
