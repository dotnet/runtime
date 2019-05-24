// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the pre-encoded property name and <see cref="DateTimeOffset"/> value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="DateTimeOffset"/> using the round-trippable ('O') <see cref="StandardFormat"/> , for example: 2017-06-12T05:30:45.7680000-07:00.
        /// </remarks>
        public void WriteString(JsonEncodedText propertyName, DateTimeOffset value)
            => WriteStringHelper(propertyName.EncodedUtf8Bytes, value);

        private void WriteStringHelper(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxTokenSize);

            WriteStringByOptions(utf8PropertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and <see cref="DateTimeOffset"/> value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="DateTimeOffset"/> using the round-trippable ('O') <see cref="StandardFormat"/> , for example: 2017-06-12T05:30:45.7680000-07:00.
        /// </remarks>
        public void WriteString(string propertyName, DateTimeOffset value)
            => WriteString(propertyName.AsSpan(), value);

        /// <summary>
        /// Writes the property name and <see cref="DateTimeOffset"/> value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="DateTimeOffset"/> using the round-trippable ('O') <see cref="StandardFormat"/> , for example: 2017-06-12T05:30:45.7680000-07:00.
        /// </remarks>
        public void WriteString(ReadOnlySpan<char> propertyName, DateTimeOffset value)
        {
            JsonWriterHelper.ValidateProperty(propertyName);

            WriteStringEscape(propertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and <see cref="DateTimeOffset"/> value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="DateTimeOffset"/> using the round-trippable ('O') <see cref="StandardFormat"/> , for example: 2017-06-12T05:30:45.7680000-07:00.
        /// </remarks>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            WriteStringEscape(utf8PropertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringEscape(ReadOnlySpan<char> propertyName, DateTimeOffset value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length);

            if (propertyIdx != -1)
            {
                WriteStringEscapeProperty(propertyName, value, propertyIdx);
            }
            else
            {
                WriteStringByOptions(propertyName, value);
            }
        }

        private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length);

            if (propertyIdx != -1)
            {
                WriteStringEscapeProperty(utf8PropertyName, value, propertyIdx);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, value);
            }
        }

        private void WriteStringEscapeProperty(ReadOnlySpan<char> propertyName, DateTimeOffset value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < propertyName.Length);

            char[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

            Span<char> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (propertyArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, out int written);

            WriteStringByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < utf8PropertyName.Length);

            byte[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

            Span<byte> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (propertyArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, out int written);

            WriteStringByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<char> propertyName, DateTimeOffset value)
        {
            ValidateWritingProperty();
            if (Options.Indented)
            {
                WriteStringIndented(propertyName, value);
            }
            else
            {
                WriteStringMinimized(propertyName, value);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
        {
            ValidateWritingProperty();
            if (Options.Indented)
            {
                WriteStringIndented(utf8PropertyName, value);
            }
            else
            {
                WriteStringMinimized(utf8PropertyName, value);
            }
        }

        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, DateTimeOffset value)
        {
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - JsonConstants.MaximumFormatDateTimeOffsetLength - 6);

            // All ASCII, 2 quotes for property name, 2 quotes for date, and 1 colon => escapedPropertyName.Length + JsonConstants.MaximumFormatDateTimeOffsetLength + 5
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + JsonConstants.MaximumFormatDateTimeOffsetLength + 6;

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

            TranscodeAndWrite(escapedPropertyName, output);

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeperator;

            output[BytesPending++] = JsonConstants.Quote;

            Span<byte> tempSpan = stackalloc byte[JsonConstants.MaximumFormatDateTimeOffsetLength];
            bool result = Utf8Formatter.TryFormat(value, tempSpan, out int bytesWritten, s_dateTimeStandardFormat);
            Debug.Assert(result);
            JsonWriterHelper.TrimDateTimeOffset(tempSpan.Slice(0, bytesWritten), out bytesWritten);
            tempSpan.Slice(0, bytesWritten).CopyTo(output.Slice(BytesPending));
            BytesPending += bytesWritten;

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, DateTimeOffset value)
        {
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - JsonConstants.MaximumFormatDateTimeOffsetLength - 6);

            int minRequired = escapedPropertyName.Length + JsonConstants.MaximumFormatDateTimeOffsetLength + 5; // 2 quotes for property name, 2 quotes for date, and 1 colon
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

            escapedPropertyName.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedPropertyName.Length;

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeperator;

            output[BytesPending++] = JsonConstants.Quote;

            Span<byte> tempSpan = stackalloc byte[JsonConstants.MaximumFormatDateTimeOffsetLength];
            bool result = Utf8Formatter.TryFormat(value, tempSpan, out int bytesWritten, s_dateTimeStandardFormat);
            Debug.Assert(result);
            JsonWriterHelper.TrimDateTimeOffset(tempSpan.Slice(0, bytesWritten), out bytesWritten);
            tempSpan.Slice(0, bytesWritten).CopyTo(output.Slice(BytesPending));
            BytesPending += bytesWritten;

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, DateTimeOffset value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - indent - JsonConstants.MaximumFormatDateTimeOffsetLength - 7 - s_newLineLength);

            // All ASCII, 2 quotes for property name, 2 quotes for date, 1 colon, and 1 space => escapedPropertyName.Length + JsonConstants.MaximumFormatDateTimeOffsetLength + 6
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + JsonConstants.MaximumFormatDateTimeOffsetLength + 7 + s_newLineLength;

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

            TranscodeAndWrite(escapedPropertyName, output);

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeperator;
            output[BytesPending++] = JsonConstants.Space;

            output[BytesPending++] = JsonConstants.Quote;

            Span<byte> tempSpan = stackalloc byte[JsonConstants.MaximumFormatDateTimeOffsetLength];
            bool result = Utf8Formatter.TryFormat(value, tempSpan, out int bytesWritten, s_dateTimeStandardFormat);
            Debug.Assert(result);
            JsonWriterHelper.TrimDateTimeOffset(tempSpan.Slice(0, bytesWritten), out bytesWritten);
            tempSpan.Slice(0, bytesWritten).CopyTo(output.Slice(BytesPending));
            BytesPending += bytesWritten;

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, DateTimeOffset value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedPropertyName.Length < int.MaxValue - indent - JsonConstants.MaximumFormatDateTimeOffsetLength - 7 - s_newLineLength);

            int minRequired = indent + escapedPropertyName.Length + JsonConstants.MaximumFormatDateTimeOffsetLength + 6; // 2 quotes for property name, 2 quotes for date, 1 colon, and 1 space
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

            escapedPropertyName.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedPropertyName.Length;

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeperator;
            output[BytesPending++] = JsonConstants.Space;

            output[BytesPending++] = JsonConstants.Quote;

            Span<byte> tempSpan = stackalloc byte[JsonConstants.MaximumFormatDateTimeOffsetLength];
            bool result = Utf8Formatter.TryFormat(value, tempSpan, out int bytesWritten, s_dateTimeStandardFormat);
            Debug.Assert(result);
            JsonWriterHelper.TrimDateTimeOffset(tempSpan.Slice(0, bytesWritten), out bytesWritten);
            tempSpan.Slice(0, bytesWritten).CopyTo(output.Slice(BytesPending));
            BytesPending += bytesWritten;

            output[BytesPending++] = JsonConstants.Quote;
        }
    }
}
