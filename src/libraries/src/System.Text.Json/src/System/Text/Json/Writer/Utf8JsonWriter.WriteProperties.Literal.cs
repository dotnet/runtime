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
        /// Writes the pre-encoded property name and the JSON literal "null" as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNull(JsonEncodedText propertyName)
        {
            WriteLiteralHelper(propertyName.EncodedUtf8Bytes, JsonConstants.NullValue);
            _tokenType = JsonTokenType.Null;
        }

        private void WriteLiteralHelper(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxTokenSize);

            WriteLiteralByOptions(utf8PropertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
        }

        /// <summary>
        /// Writes the property name and the JSON literal "null" as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNull(string propertyName)
            => WriteNull(propertyName.AsSpan());

        /// <summary>
        /// Writes the property name and the JSON literal "null" as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNull(ReadOnlySpan<char> propertyName)
        {
            JsonWriterHelper.ValidateProperty(propertyName);

            ReadOnlySpan<byte> span = JsonConstants.NullValue;

            WriteLiteralEscape(propertyName, span);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Null;
        }

        /// <summary>
        /// Writes the property name and the JSON literal "null" as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteNull(ReadOnlySpan<byte> utf8PropertyName)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            ReadOnlySpan<byte> span = JsonConstants.NullValue;

            WriteLiteralEscape(utf8PropertyName, span);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Null;
        }

        /// <summary>
        /// Writes the pre-encoded property name and <see cref="bool"/> value (as a JSON literal "true" or "false") as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as part of the name/value pair.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBoolean(JsonEncodedText propertyName, bool value)
        {
            if (value)
            {
                WriteLiteralHelper(propertyName.EncodedUtf8Bytes, JsonConstants.TrueValue);
                _tokenType = JsonTokenType.True;
            }
            else
            {
                WriteLiteralHelper(propertyName.EncodedUtf8Bytes, JsonConstants.FalseValue);
                _tokenType = JsonTokenType.False;
            }
        }

        /// <summary>
        /// Writes the property name and <see cref="bool"/> value (as a JSON literal "true" or "false") as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as part of the name/value pair.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBoolean(string propertyName, bool value)
            => WriteBoolean(propertyName.AsSpan(), value);

        /// <summary>
        /// Writes the property name and <see cref="bool"/> value (as a JSON literal "true" or "false") as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as part of the name/value pair.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBoolean(ReadOnlySpan<char> propertyName, bool value)
        {
            JsonWriterHelper.ValidateProperty(propertyName);

            ReadOnlySpan<byte> span = value ? JsonConstants.TrueValue : JsonConstants.FalseValue;

            WriteLiteralEscape(propertyName, span);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = value ? JsonTokenType.True : JsonTokenType.False;
        }

        /// <summary>
        /// Writes the property name and <see cref="bool"/> value (as a JSON literal "true" or "false") as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a JSON literal "true" or "false" as part of the name/value pair.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteBoolean(ReadOnlySpan<byte> utf8PropertyName, bool value)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            ReadOnlySpan<byte> span = value ? JsonConstants.TrueValue : JsonConstants.FalseValue;

            WriteLiteralEscape(utf8PropertyName, span);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = value ? JsonTokenType.True : JsonTokenType.False;
        }

        private void WriteLiteralEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length);

            if (propertyIdx != -1)
            {
                WriteLiteralEscapeProperty(propertyName, value, propertyIdx);
            }
            else
            {
                WriteLiteralByOptions(propertyName, value);
            }
        }

        private void WriteLiteralEscape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length);

            if (propertyIdx != -1)
            {
                WriteLiteralEscapeProperty(utf8PropertyName, value, propertyIdx);
            }
            else
            {
                WriteLiteralByOptions(utf8PropertyName, value);
            }
        }

        private void WriteLiteralEscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < propertyName.Length);

            char[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

            Span<char> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (propertyArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, out int written);

            WriteLiteralByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteLiteralEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < utf8PropertyName.Length);

            byte[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

            Span<byte> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (propertyArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, out int written);

            WriteLiteralByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteLiteralByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
        {
            ValidateWritingProperty();
            if (Options.Indented)
            {
                WriteLiteralIndented(propertyName, value);
            }
            else
            {
                WriteLiteralMinimized(propertyName, value);
            }
        }

        private void WriteLiteralByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
        {
            ValidateWritingProperty();
            if (Options.Indented)
            {
                WriteLiteralIndented(utf8PropertyName, value);
            }
            else
            {
                WriteLiteralMinimized(utf8PropertyName, value);
            }
        }

        private void WriteLiteralMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            Debug.Assert(value.Length <= JsonConstants.MaxTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - value.Length - 4);

            // All ASCII, 2 quotes for property name, and 1 colon => escapedPropertyName.Length + value.Length + 3
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + value.Length + 4;

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

            value.CopyTo(output.Slice(BytesPending));
            BytesPending += value.Length;
        }

        private void WriteLiteralMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            Debug.Assert(value.Length <= JsonConstants.MaxTokenSize);
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - value.Length - 4);

            int minRequired = escapedPropertyName.Length + value.Length + 3; // 2 quotes for property name, and 1 colon
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

            value.CopyTo(output.Slice(BytesPending));
            BytesPending += value.Length;
        }

        private void WriteLiteralIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(value.Length <= JsonConstants.MaxTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - indent - value.Length - 5 - s_newLineLength);

            // All ASCII, 2 quotes for property name, 1 colon, and 1 space => escapedPropertyName.Length + value.Length + 4
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + value.Length + 5 + s_newLineLength;

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

            value.CopyTo(output.Slice(BytesPending));
            BytesPending += value.Length;
        }

        private void WriteLiteralIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(value.Length <= JsonConstants.MaxTokenSize);
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - indent - value.Length - 5 - s_newLineLength);

            int minRequired = indent + escapedPropertyName.Length + value.Length + 4; // 2 quotes for property name, 1 colon, and 1 space
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

            value.CopyTo(output.Slice(BytesPending));
            BytesPending += value.Length;
        }
    }
}
