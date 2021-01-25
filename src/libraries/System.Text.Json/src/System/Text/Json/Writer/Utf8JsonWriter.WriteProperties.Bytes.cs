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
        /// Writes the pre-encoded property name and raw bytes value (as a Base64 encoded JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write.</param>
        /// <param name="bytes">The binary data to write as Base64 encoded text.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        public void WriteBase64String(JsonEncodedText propertyName, ReadOnlySpan<byte> bytes)
        {
            ReadOnlySpan<byte> utf8PropertyName = propertyName.EncodedUtf8Bytes;
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxUnescapedTokenSize);

            JsonWriterHelper.ValidateBytes(bytes);

            WriteBase64ByOptions(utf8PropertyName, bytes);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and raw bytes value (as a Base64 encoded JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="bytes">The binary data to write as Base64 encoded text.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="propertyName"/> parameter is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteBase64String(string propertyName, ReadOnlySpan<byte> bytes)
            => WriteBase64String((propertyName ?? throw new ArgumentNullException(nameof(propertyName))).AsSpan(), bytes);

        /// <summary>
        /// Writes the property name and raw bytes value (as a Base64 encoded JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="bytes">The binary data to write as Base64 encoded text.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteBase64String(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
        {
            JsonWriterHelper.ValidatePropertyAndBytes(propertyName, bytes);

            WriteBase64Escape(propertyName, bytes);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and raw bytes value (as a Base64 encoded JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write.</param>
        /// <param name="bytes">The binary data to write as Base64 encoded text.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteBase64String(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes)
        {
            JsonWriterHelper.ValidatePropertyAndBytes(utf8PropertyName, bytes);

            WriteBase64Escape(utf8PropertyName, bytes);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteBase64Escape(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length);

            if (propertyIdx != -1)
            {
                WriteBase64EscapeProperty(propertyName, bytes, propertyIdx);
            }
            else
            {
                WriteBase64ByOptions(propertyName, bytes);
            }
        }

        private void WriteBase64Escape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length);

            if (propertyIdx != -1)
            {
                WriteBase64EscapeProperty(utf8PropertyName, bytes, propertyIdx);
            }
            else
            {
                WriteBase64ByOptions(utf8PropertyName, bytes);
            }
        }

        private void WriteBase64EscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < propertyName.Length);

            char[]? propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

            Span<char> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (propertyArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);

            WriteBase64ByOptions(escapedPropertyName.Slice(0, written), bytes);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteBase64EscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < utf8PropertyName.Length);

            byte[]? propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

            Span<byte> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (propertyArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);

            WriteBase64ByOptions(escapedPropertyName.Slice(0, written), bytes);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteBase64ByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteBase64Indented(propertyName, bytes);
            }
            else
            {
                WriteBase64Minimized(propertyName, bytes);
            }
        }

        private void WriteBase64ByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteBase64Indented(utf8PropertyName, bytes);
            }
            else
            {
                WriteBase64Minimized(utf8PropertyName, bytes);
            }
        }

        private void WriteBase64Minimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> bytes)
        {
            int encodedLength = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

            Debug.Assert(escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding < int.MaxValue - encodedLength - 6);

            // All ASCII, 2 quotes for property name, 2 quotes to surround the base-64 encoded string value, and 1 colon => escapedPropertyName.Length + encodedLength + 5
            // Optionally, 1 list separator, and up to 3x growth when transcoding.
            int maxRequired = (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + encodedLength + 6;

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

            Base64EncodeAndWrite(bytes, output, encodedLength);

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteBase64Minimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> bytes)
        {
            int encodedLength = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

            Debug.Assert(escapedPropertyName.Length < int.MaxValue - encodedLength - 6);

            // 2 quotes for property name, 2 quotes to surround the base-64 encoded string value, and 1 colon => escapedPropertyName.Length + encodedLength + 5
            // Optionally, 1 list separator.
            int maxRequired = escapedPropertyName.Length + encodedLength + 6;

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

            Base64EncodeAndWrite(bytes, output, encodedLength);

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteBase64Indented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> bytes)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            int encodedLength = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

            Debug.Assert(escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding < int.MaxValue - indent - encodedLength - 7 - s_newLineLength);

            // All ASCII, 2 quotes for property name, 2 quotes to surround the base-64 encoded string value, 1 colon, and 1 space => indent + escapedPropertyName.Length + encodedLength + 6
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding.
            int maxRequired = indent + (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + encodedLength + 7 + s_newLineLength;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            Debug.Assert(_options.SkipValidation || _tokenType != JsonTokenType.PropertyName);

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

            Base64EncodeAndWrite(bytes, output, encodedLength);

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteBase64Indented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> bytes)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            int encodedLength = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

            Debug.Assert(escapedPropertyName.Length < int.MaxValue - indent - encodedLength - 7 - s_newLineLength);

            // 2 quotes for property name, 2 quotes to surround the base-64 encoded string value, 1 colon, and 1 space => indent + escapedPropertyName.Length + encodedLength + 6
            // Optionally, 1 list separator, and 1-2 bytes for new line.
            int maxRequired = indent + escapedPropertyName.Length + encodedLength + 7 + s_newLineLength;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            Debug.Assert(_options.SkipValidation || _tokenType != JsonTokenType.PropertyName);

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

            Base64EncodeAndWrite(bytes, output, encodedLength);

            output[BytesPending++] = JsonConstants.Quote;
        }
    }
}
