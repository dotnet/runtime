// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the pre-encoded property name (as a JSON string) as the first part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        public void WritePropertyName(JsonEncodedText propertyName)
            => WritePropertyNameHelper(propertyName.EncodedUtf8Bytes);

        internal void WritePropertyNameSection(ReadOnlySpan<byte> escapedPropertyNameSection)
        {
            if (_options.Indented)
            {
                ReadOnlySpan<byte> escapedPropertyName =
                    escapedPropertyNameSection.Slice(1, escapedPropertyNameSection.Length - 3);

                WritePropertyNameHelper(escapedPropertyName);
            }
            else
            {
                Debug.Assert(escapedPropertyNameSection.Length <= JsonConstants.MaxUnescapedTokenSize - 3);

                WriteStringPropertyNameSection(escapedPropertyNameSection);

                _currentDepth &= JsonConstants.RemoveFlagsBitMask;
                _tokenType = JsonTokenType.PropertyName;
                _commentAfterNoneOrPropertyName = false;
            }
        }

        private void WritePropertyNameHelper(ReadOnlySpan<byte> utf8PropertyName)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxUnescapedTokenSize);

            WriteStringByOptionsPropertyName(utf8PropertyName);

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _tokenType = JsonTokenType.PropertyName;
            _commentAfterNoneOrPropertyName = false;
        }

        /// <summary>
        /// Writes the property name (as a JSON string) as the first part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
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
        public void WritePropertyName(string propertyName)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }
            WritePropertyName(propertyName.AsSpan());
        }

        /// <summary>
        /// Writes the property name (as a JSON string) as the first part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        public void WritePropertyName(ReadOnlySpan<char> propertyName)
        {
            JsonWriterHelper.ValidateProperty(propertyName);

            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteStringEscapeProperty(propertyName, propertyIdx);
            }
            else
            {
                WriteStringByOptionsPropertyName(propertyName);
            }
            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _tokenType = JsonTokenType.PropertyName;
            _commentAfterNoneOrPropertyName = false;
        }

        private void WriteStringEscapeProperty(scoped ReadOnlySpan<char> propertyName, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

            char[]? propertyArray = null;
            scoped Span<char> escapedPropertyName;

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

                if (length > JsonConstants.StackallocCharThreshold)
                {
                    propertyArray = ArrayPool<char>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    escapedPropertyName = stackalloc char[JsonConstants.StackallocCharThreshold];
                }

                JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);
                propertyName = escapedPropertyName.Slice(0, written);
            }

            WriteStringByOptionsPropertyName(propertyName);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringByOptionsPropertyName(ReadOnlySpan<char> propertyName)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteStringIndentedPropertyName(propertyName);
            }
            else
            {
                WriteStringMinimizedPropertyName(propertyName);
            }
        }

        private void WriteStringMinimizedPropertyName(ReadOnlySpan<char> escapedPropertyName)
        {
            Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue - 4) / JsonConstants.MaxExpansionFactorWhileTranscoding);

            // All ASCII, 2 quotes for property name, and 1 colon => escapedPropertyName.Length + 3
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 4;

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
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
        }

        private void WriteStringIndentedPropertyName(ReadOnlySpan<char> escapedPropertyName)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue - 5 - indent - s_newLineLength) / JsonConstants.MaxExpansionFactorWhileTranscoding);

            // All ASCII, 2 quotes for property name, 1 colon, and 1 space => escapedPropertyName.Length + 4
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 5 + s_newLineLength;

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

            WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedPropertyName, output);

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
            output[BytesPending++] = JsonConstants.Space;
        }

        /// <summary>
        /// Writes the UTF-8 property name (as a JSON string) as the first part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteStringEscapeProperty(utf8PropertyName, propertyIdx);
            }
            else
            {
                WriteStringByOptionsPropertyName(utf8PropertyName);
            }
            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _tokenType = JsonTokenType.PropertyName;
            _commentAfterNoneOrPropertyName = false;
        }

        private void WritePropertyNameUnescaped(ReadOnlySpan<byte> utf8PropertyName)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);
            WriteStringByOptionsPropertyName(utf8PropertyName);

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _tokenType = JsonTokenType.PropertyName;
            _commentAfterNoneOrPropertyName = false;
        }

        private void WriteStringEscapeProperty(scoped ReadOnlySpan<byte> utf8PropertyName, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);

            byte[]? propertyArray = null;
            scoped Span<byte> escapedPropertyName;

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

                if (length > JsonConstants.StackallocByteThreshold)
                {
                    propertyArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    escapedPropertyName = stackalloc byte[JsonConstants.StackallocByteThreshold];
                }

                JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);
                utf8PropertyName = escapedPropertyName.Slice(0, written);
            }

            WriteStringByOptionsPropertyName(utf8PropertyName);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringByOptionsPropertyName(ReadOnlySpan<byte> utf8PropertyName)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteStringIndentedPropertyName(utf8PropertyName);
            }
            else
            {
                WriteStringMinimizedPropertyName(utf8PropertyName);
            }
        }

        // AggressiveInlining used since this is only called from one location.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringMinimizedPropertyName(ReadOnlySpan<byte> escapedPropertyName)
        {
            Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - 4);

            int minRequired = escapedPropertyName.Length + 3; // 2 quotes for property name, and 1 colon
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
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
        }

        // AggressiveInlining used since this is only called from one location.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringPropertyNameSection(ReadOnlySpan<byte> escapedPropertyNameSection)
        {
            Debug.Assert(escapedPropertyNameSection.Length <= JsonConstants.MaxEscapedTokenSize - 3);
            Debug.Assert(escapedPropertyNameSection.Length < int.MaxValue - 4);

            int maxRequired = escapedPropertyNameSection.Length + 1; // Optionally, 1 list separator

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            escapedPropertyNameSection.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedPropertyNameSection.Length;
        }

        // AggressiveInlining used since this is only called from one location.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringIndentedPropertyName(ReadOnlySpan<byte> escapedPropertyName)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - indent - 5 - s_newLineLength);

            int minRequired = indent + escapedPropertyName.Length + 4; // 2 quotes for property name, 1 colon, and 1 space
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

            Debug.Assert(_options.SkipValidation || _tokenType != JsonTokenType.PropertyName);

            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output);
            }

            WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            escapedPropertyName.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedPropertyName.Length;

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
            output[BytesPending++] = JsonConstants.Space;
        }

        /// <summary>
        /// Writes the pre-encoded property name and pre-encoded value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write.</param>
        /// <param name="value">The JSON-encoded value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        public void WriteString(JsonEncodedText propertyName, JsonEncodedText value)
            => WriteStringHelper(propertyName.EncodedUtf8Bytes, value.EncodedUtf8Bytes);

        private void WriteStringHelper(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxUnescapedTokenSize && utf8Value.Length <= JsonConstants.MaxUnescapedTokenSize);

            WriteStringByOptions(utf8PropertyName, utf8Value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and pre-encoded value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write.</param>
        /// <param name="value">The JSON-encoded value to write.</param>
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
        public void WriteString(string propertyName, JsonEncodedText value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }
            WriteString(propertyName.AsSpan(), value);
        }

        /// <summary>
        /// Writes the property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="propertyName"/> parameter is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// <para>
        /// The property name and value is escaped before writing.
        /// </para>
        /// <para>
        /// If <paramref name="value"/> is <see langword="null"/> the JSON null value is written,
        /// as if <see cref="WriteNull(System.ReadOnlySpan{byte})"/> were called.
        /// </para>
        /// </remarks>
        public void WriteString(string propertyName, string? value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            if (value == null)
            {
                WriteNull(propertyName.AsSpan());
            }
            else
            {
                WriteString(propertyName.AsSpan(), value.AsSpan());
            }
        }

        /// <summary>
        /// Writes the property name and text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
        {
            JsonWriterHelper.ValidatePropertyAndValue(propertyName, value);

            WriteStringEscape(propertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the UTF-8 property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            JsonWriterHelper.ValidatePropertyAndValue(utf8PropertyName, utf8Value);

            WriteStringEscape(utf8PropertyName, utf8Value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the pre-encoded property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// <para>
        /// The value is escaped before writing.
        /// </para>
        /// <para>
        /// If <paramref name="value"/> is <see langword="null"/> the JSON null value is written,
        /// as if <see cref="WriteNull(System.Text.Json.JsonEncodedText)"/> was called.
        /// </para>
        /// </remarks>
        public void WriteString(JsonEncodedText propertyName, string? value)
        {
            if (value == null)
            {
                WriteNull(propertyName);
            }
            else
            {
                WriteString(propertyName, value.AsSpan());
            }
        }

        /// <summary>
        /// Writes the pre-encoded property name and text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write.</param>
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
        public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<char> value)
            => WriteStringHelperEscapeValue(propertyName.EncodedUtf8Bytes, value);

        private void WriteStringHelperEscapeValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxUnescapedTokenSize);

            JsonWriterHelper.ValidateValue(value);

            int valueIdx = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < value.Length && valueIdx < int.MaxValue / 2);

            if (valueIdx != -1)
            {
                WriteStringEscapeValueOnly(utf8PropertyName, value, valueIdx);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="propertyName"/> parameter is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        public void WriteString(string propertyName, ReadOnlySpan<char> value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }
            WriteString(propertyName.AsSpan(), value);
        }

        /// <summary>
        /// Writes the UTF-8 property name and text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
        {
            JsonWriterHelper.ValidatePropertyAndValue(utf8PropertyName, value);

            WriteStringEscape(utf8PropertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the pre-encoded property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The value is escaped before writing.
        /// </remarks>
        public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<byte> utf8Value)
            => WriteStringHelperEscapeValue(propertyName.EncodedUtf8Bytes, utf8Value);

        private void WriteStringHelperEscapeValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxUnescapedTokenSize);

            JsonWriterHelper.ValidateValue(utf8Value);

            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < utf8Value.Length && valueIdx < int.MaxValue / 2);

            if (valueIdx != -1)
            {
                WriteStringEscapeValueOnly(utf8PropertyName, utf8Value, valueIdx);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, utf8Value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="propertyName"/> parameter is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        public void WriteString(string propertyName, ReadOnlySpan<byte> utf8Value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }
            WriteString(propertyName.AsSpan(), utf8Value);
        }

        /// <summary>
        /// Writes the property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
        {
            JsonWriterHelper.ValidatePropertyAndValue(propertyName, utf8Value);

            WriteStringEscape(propertyName, utf8Value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and pre-encoded value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="value">The JSON-encoded value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteString(ReadOnlySpan<char> propertyName, JsonEncodedText value)
            => WriteStringHelperEscapeProperty(propertyName, value.EncodedUtf8Bytes);

        private void WriteStringHelperEscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8Value.Length <= JsonConstants.MaxUnescapedTokenSize);

            JsonWriterHelper.ValidateProperty(propertyName);

            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteStringEscapePropertyOnly(propertyName, utf8Value, propertyIdx);
            }
            else
            {
                WriteStringByOptions(propertyName, utf8Value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// <para>
        /// The property name and value are escaped before writing.
        /// </para>
        /// <para>
        /// If <paramref name="value"/> is <see langword="null"/> the JSON null value is written,
        /// as if <see cref="WriteNull(System.ReadOnlySpan{char})"/> was called.
        /// </para>
        /// </remarks>
        public void WriteString(ReadOnlySpan<char> propertyName, string? value)
        {
            if (value == null)
            {
                WriteNull(propertyName);
            }
            else
            {
                WriteString(propertyName, value.AsSpan());
            }
        }

        /// <summary>
        /// Writes the UTF-8 property name and pre-encoded value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write.</param>
        /// <param name="value">The JSON-encoded value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, JsonEncodedText value)
            => WriteStringHelperEscapeProperty(utf8PropertyName, value.EncodedUtf8Bytes);

        private void WriteStringHelperEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8Value.Length <= JsonConstants.MaxUnescapedTokenSize);

            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteStringEscapePropertyOnly(utf8PropertyName, utf8Value, propertyIdx);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, utf8Value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the UTF-8 property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// <para>
        /// The property name and value are escaped before writing.
        /// </para>
        /// <para>
        /// If <paramref name="value"/> is <see langword="null"/> the JSON null value is written,
        /// as if <see cref="WriteNull(System.ReadOnlySpan{byte})"/> was called.
        /// </para>
        /// </remarks>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, string? value)
        {
            if (value == null)
            {
                WriteNull(utf8PropertyName);
            }
            else
            {
                WriteString(utf8PropertyName, value.AsSpan());
            }
        }

        private void WriteStringEscapeValueOnly(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < utf8Value.Length);

            byte[]? valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndex);

            Span<byte> escapedValue = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndex, _options.Encoder, out int written);

            WriteStringByOptions(escapedPropertyName, escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }
        }

        private void WriteStringEscapeValueOnly(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> value, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < value.Length);

            char[]? valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndex);

            Span<char> escapedValue = length <= JsonConstants.StackallocCharThreshold ?
                stackalloc char[JsonConstants.StackallocCharThreshold] :
                (valueArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndex, _options.Encoder, out int written);

            WriteStringByOptions(escapedPropertyName, escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<char>.Shared.Return(valueArray);
            }
        }

        private void WriteStringEscapePropertyOnly(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> escapedValue, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < propertyName.Length);

            char[]? propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndex);

            Span<char> escapedPropertyName = length <= JsonConstants.StackallocCharThreshold ?
                stackalloc char[JsonConstants.StackallocCharThreshold] :
                (propertyArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndex, _options.Encoder, out int written);

            WriteStringByOptions(escapedPropertyName.Slice(0, written), escapedValue);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscapePropertyOnly(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> escapedValue, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < utf8PropertyName.Length);

            byte[]? propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndex);

            Span<byte> escapedPropertyName = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (propertyArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndex, _options.Encoder, out int written);

            WriteStringByOptions(escapedPropertyName.Slice(0, written), escapedValue);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < value.Length && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length && propertyIdx < int.MaxValue / 2);

            // Equivalent to: valueIdx != -1 || propertyIdx != -1
            if (valueIdx + propertyIdx != -2)
            {
                WriteStringEscapePropertyOrValue(propertyName, value, propertyIdx, valueIdx);
            }
            else
            {
                WriteStringByOptions(propertyName, value);
            }
        }

        private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < utf8Value.Length && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length && propertyIdx < int.MaxValue / 2);

            // Equivalent to: valueIdx != -1 || propertyIdx != -1
            if (valueIdx + propertyIdx != -2)
            {
                WriteStringEscapePropertyOrValue(utf8PropertyName, utf8Value, propertyIdx, valueIdx);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, utf8Value);
            }
        }

        private void WriteStringEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < utf8Value.Length && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length && propertyIdx < int.MaxValue / 2);

            // Equivalent to: valueIdx != -1 || propertyIdx != -1
            if (valueIdx + propertyIdx != -2)
            {
                WriteStringEscapePropertyOrValue(propertyName, utf8Value, propertyIdx, valueIdx);
            }
            else
            {
                WriteStringByOptions(propertyName, utf8Value);
            }
        }

        private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);

            Debug.Assert(valueIdx >= -1 && valueIdx < value.Length && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length && propertyIdx < int.MaxValue / 2);

            // Equivalent to: valueIdx != -1 || propertyIdx != -1
            if (valueIdx + propertyIdx != -2)
            {
                WriteStringEscapePropertyOrValue(utf8PropertyName, value, propertyIdx, valueIdx);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, value);
            }
        }

        private void WriteStringEscapePropertyOrValue(scoped ReadOnlySpan<char> propertyName, scoped ReadOnlySpan<char> value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

            char[]? valueArray = null;
            char[]? propertyArray = null;
            scoped Span<char> escapedValue;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

                if (length > JsonConstants.StackallocCharThreshold)
                {
                    valueArray = ArrayPool<char>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    escapedValue = stackalloc char[JsonConstants.StackallocCharThreshold];
                }

                JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int written);
                value = escapedValue.Slice(0, written);
            }

            scoped Span<char> escapedPropertyName;

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

                if (length > JsonConstants.StackallocCharThreshold)
                {
                    propertyArray = ArrayPool<char>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    escapedPropertyName = stackalloc char[JsonConstants.StackallocCharThreshold];
                }

                JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);
                propertyName = escapedPropertyName.Slice(0, written);
            }

            WriteStringByOptions(propertyName, value);

            if (valueArray != null)
            {
                ArrayPool<char>.Shared.Return(valueArray);
            }

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscapePropertyOrValue(scoped ReadOnlySpan<byte> utf8PropertyName, scoped ReadOnlySpan<byte> utf8Value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);

            byte[]? valueArray = null;
            byte[]? propertyArray = null;
            scoped Span<byte> escapedValue;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

                if (length > JsonConstants.StackallocByteThreshold)
                {
                    valueArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    escapedValue = stackalloc byte[JsonConstants.StackallocByteThreshold];
                }

                JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int written);
                utf8Value = escapedValue.Slice(0, written);
            }

            scoped Span<byte> escapedPropertyName;

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

                if (length > JsonConstants.StackallocByteThreshold)
                {
                    propertyArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    escapedPropertyName = stackalloc byte[JsonConstants.StackallocByteThreshold];
                }

                JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);
                utf8PropertyName = escapedPropertyName.Slice(0, written);
            }

            WriteStringByOptions(utf8PropertyName, utf8Value);

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscapePropertyOrValue(scoped ReadOnlySpan<char> propertyName, scoped ReadOnlySpan<byte> utf8Value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

            byte[]? valueArray = null;
            char[]? propertyArray = null;
            scoped Span<byte> escapedValue;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

                if (length > JsonConstants.StackallocByteThreshold)
                {
                    valueArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    escapedValue = stackalloc byte[JsonConstants.StackallocByteThreshold];
                }

                JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int written);
                utf8Value = escapedValue.Slice(0, written);
            }

            scoped Span<char> escapedPropertyName;

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

                if (length > JsonConstants.StackallocCharThreshold)
                {
                    propertyArray = ArrayPool<char>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    escapedPropertyName = stackalloc char[JsonConstants.StackallocCharThreshold];
                }

                JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);
                propertyName = escapedPropertyName.Slice(0, written);
            }

            WriteStringByOptions(propertyName, utf8Value);

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscapePropertyOrValue(scoped ReadOnlySpan<byte> utf8PropertyName, scoped ReadOnlySpan<char> value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);

            char[]? valueArray = null;
            byte[]? propertyArray = null;
            scoped Span<char> escapedValue;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

                if (length > JsonConstants.StackallocCharThreshold)
                {
                    valueArray = ArrayPool<char>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    escapedValue = stackalloc char[JsonConstants.StackallocCharThreshold];
                }

                JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, _options.Encoder, out int written);
                value = escapedValue.Slice(0, written);
            }

            scoped Span<byte> escapedPropertyName;

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

                if (length > JsonConstants.StackallocByteThreshold)
                {
                    propertyArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    escapedPropertyName = stackalloc byte[JsonConstants.StackallocByteThreshold];
                }

                JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);
                utf8PropertyName = escapedPropertyName.Slice(0, written);
            }

            WriteStringByOptions(utf8PropertyName, value);

            if (valueArray != null)
            {
                ArrayPool<char>.Shared.Return(valueArray);
            }

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteStringIndented(propertyName, value);
            }
            else
            {
                WriteStringMinimized(propertyName, value);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteStringIndented(utf8PropertyName, utf8Value);
            }
            else
            {
                WriteStringMinimized(utf8PropertyName, utf8Value);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteStringIndented(propertyName, utf8Value);
            }
            else
            {
                WriteStringMinimized(propertyName, utf8Value);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteStringIndented(utf8PropertyName, value);
            }
            else
            {
                WriteStringMinimized(utf8PropertyName, value);
            }
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxUnescapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < ((int.MaxValue - 6) / JsonConstants.MaxExpansionFactorWhileTranscoding) - escapedValue.Length);

            // All ASCII, 2 quotes for property name, 2 quotes for value, and 1 colon => escapedPropertyName.Length + escapedValue.Length + 5
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = ((escapedPropertyName.Length + escapedValue.Length) * JsonConstants.MaxExpansionFactorWhileTranscoding) + 6;

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
            output[BytesPending++] = JsonConstants.KeyValueSeparator;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - escapedValue.Length - 6);

            int minRequired = escapedPropertyName.Length + escapedValue.Length + 5; // 2 quotes for property name, 2 quotes for value, and 1 colon
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
            output[BytesPending++] = JsonConstants.KeyValueSeparator;

            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - escapedValue.Length - 6);

            // All ASCII, 2 quotes for property name, 2 quotes for value, and 1 colon => escapedPropertyName.Length + escapedValue.Length + 5
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + escapedValue.Length + 6;

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
            output[BytesPending++] = JsonConstants.KeyValueSeparator;

            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - escapedValue.Length - 6);

            // All ASCII, 2 quotes for property name, 2 quotes for value, and 1 colon => escapedPropertyName.Length + escapedValue.Length + 5
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = (escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + escapedPropertyName.Length + 6;

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
            output[BytesPending++] = JsonConstants.KeyValueSeparator;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < ((int.MaxValue - 7 - indent - s_newLineLength) / JsonConstants.MaxExpansionFactorWhileTranscoding) - escapedValue.Length);

            // All ASCII, 2 quotes for property name, 2 quotes for value, 1 colon, and 1 space => escapedPropertyName.Length + escapedValue.Length + 6
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + ((escapedPropertyName.Length + escapedValue.Length) * JsonConstants.MaxExpansionFactorWhileTranscoding) + 7 + s_newLineLength;

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

            WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedPropertyName, output);

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
            output[BytesPending++] = JsonConstants.Space;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - indent - escapedValue.Length - 7 - s_newLineLength);

            int minRequired = indent + escapedPropertyName.Length + escapedValue.Length + 6; // 2 quotes for property name, 2 quotes for value, 1 colon, and 1 space
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

            Debug.Assert(_options.SkipValidation || _tokenType != JsonTokenType.PropertyName);

            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output);
            }

            WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            escapedPropertyName.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedPropertyName.Length;

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
            output[BytesPending++] = JsonConstants.Space;

            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - escapedValue.Length - 7 - indent - s_newLineLength);

            // All ASCII, 2 quotes for property name, 2 quotes for value, 1 colon, and 1 space => escapedPropertyName.Length + escapedValue.Length + 6
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + escapedValue.Length + 7 + s_newLineLength;

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

            WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedPropertyName, output);

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
            output[BytesPending++] = JsonConstants.Space;

            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= _indentLength * _options.MaxDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxEscapedTokenSize);
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - escapedValue.Length - 7 - indent - s_newLineLength);

            // All ASCII, 2 quotes for property name, 2 quotes for value, 1 colon, and 1 space => escapedPropertyName.Length + escapedValue.Length + 6
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + escapedPropertyName.Length + 7 + s_newLineLength;

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

            WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Quote;

            escapedPropertyName.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedPropertyName.Length;

            output[BytesPending++] = JsonConstants.Quote;
            output[BytesPending++] = JsonConstants.KeyValueSeparator;
            output[BytesPending++] = JsonConstants.Space;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }
    }
}
