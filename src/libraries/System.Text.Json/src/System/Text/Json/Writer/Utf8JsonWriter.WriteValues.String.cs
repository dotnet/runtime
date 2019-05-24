// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the pre-encoded text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The JSON encoded value to be written as a UTF-8 transcoded JSON string element of a JSON array.</param>
        /// <remarks>
        /// The value should already be escaped when the instance of <see cref="JsonEncodedText"/> was created.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(JsonEncodedText value)
            => WriteStringValueHelper(value.EncodedUtf8Bytes);

        private void WriteStringValueHelper(ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8Value.Length <= JsonConstants.MaxTokenSize);

            WriteStringByOptions(utf8Value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the string text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string element of a JSON array.</param>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(string value)
           => WriteStringValue(value.AsSpan());

        /// <summary>
        /// Writes the text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string element of a JSON array.</param>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(ReadOnlySpan<char> value)
        {
            JsonWriterHelper.ValidateValue(value);

            WriteStringEscape(value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringEscape(ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);

            Debug.Assert(valueIdx >= -1 && valueIdx < value.Length);

            if (valueIdx != -1)
            {
                WriteStringEscapeValue(value, valueIdx);
            }
            else
            {
                WriteStringByOptions(value);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<char> value)
        {
            ValidateWritingValue();
            if (Options.Indented)
            {
                WriteStringIndented(value);
            }
            else
            {
                WriteStringMinimized(value);
            }
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringMinimized(ReadOnlySpan<char> escapedValue)
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

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }
            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringIndented(ReadOnlySpan<char> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedValue.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - indent - 3 - s_newLineLength);

            // All ASCII, 2 quotes => indent + escapedValue.Length + 2
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 3 + s_newLineLength;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output);
            }

            JsonWriterHelper.WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteStringEscapeValue(ReadOnlySpan<char> value, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < value.Length);

            char[] valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

            Span<char> escapedValue = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (valueArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, out int written);

            WriteStringByOptions(escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<char>.Shared.Return(valueArray);
            }
        }

        /// <summary>
        /// Writes the UTF-8 text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string element of a JSON array.</param>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(ReadOnlySpan<byte> utf8Value)
        {
            JsonWriterHelper.ValidateValue(utf8Value);

            WriteStringEscape(utf8Value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringEscape(ReadOnlySpan<byte> utf8Value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);

            Debug.Assert(valueIdx >= -1 && valueIdx < utf8Value.Length);

            if (valueIdx != -1)
            {
                WriteStringEscapeValue(utf8Value, valueIdx);
            }
            else
            {
                WriteStringByOptions(utf8Value);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<byte> utf8Value)
        {
            ValidateWritingValue();
            if (Options.Indented)
            {
                WriteStringIndented(utf8Value);
            }
            else
            {
                WriteStringMinimized(utf8Value);
            }
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringMinimized(ReadOnlySpan<byte> escapedValue)
        {
            Debug.Assert(escapedValue.Length < int.MaxValue - 3);

            int minRequired = escapedValue.Length + 2; // 2 quotes
            int maxRequired = minRequired + 1; // Optionally, 1 list separator

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }
            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringIndented(ReadOnlySpan<byte> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedValue.Length < int.MaxValue - indent - 3 - s_newLineLength);

            int minRequired = indent + escapedValue.Length + 2; // 2 quotes
            int maxRequired = minRequired + 1 + s_newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output);
            }

            JsonWriterHelper.WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteStringEscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

            byte[] valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

            Span<byte> escapedValue = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, out int written);

            WriteStringByOptions(escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }
        }
    }
}
