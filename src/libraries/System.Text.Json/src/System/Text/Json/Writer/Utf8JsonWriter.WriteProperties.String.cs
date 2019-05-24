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
        /// Writes the pre-encoded property name and pre-encoded value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The JSON encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value should already be escaped when the instance of <see cref="JsonEncodedText"/> was created.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(JsonEncodedText propertyName, JsonEncodedText value)
            => WriteStringHelper(propertyName.EncodedUtf8Bytes, value.EncodedUtf8Bytes);

        private void WriteStringHelper(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxTokenSize && utf8Value.Length <= JsonConstants.MaxTokenSize);

            WriteStringByOptions(utf8PropertyName, utf8Value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and pre-encoded value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The JSON encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The value should already be escaped when the instance of <see cref="JsonEncodedText"/> was created. The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(string propertyName, JsonEncodedText value)
            => WriteString(propertyName.AsSpan(), value);

        /// <summary>
        /// Writes the property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(string propertyName, string value)
            => WriteString(propertyName.AsSpan(), value.AsSpan());

        /// <summary>
        /// Writes the property name and text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
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
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
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
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created. The value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(JsonEncodedText propertyName, string value)
            => WriteString(propertyName, value.AsSpan());

        /// <summary>
        /// Writes the pre-encoded property name and text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created. The value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<char> value)
            => WriteStringHelperEscapeValue(propertyName.EncodedUtf8Bytes, value);

        private void WriteStringHelperEscapeValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxTokenSize);

            JsonWriterHelper.ValidateValue(value);

            int valueIdx = JsonWriterHelper.NeedsEscaping(value);

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
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(string propertyName, ReadOnlySpan<char> value)
            => WriteString(propertyName.AsSpan(), value);

        /// <summary>
        /// Writes the UTF-8 property name and text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
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
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created. The value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<byte> utf8Value)
            => WriteStringHelperEscapeValue(propertyName.EncodedUtf8Bytes, utf8Value);

        private void WriteStringHelperEscapeValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxTokenSize);

            JsonWriterHelper.ValidateValue(utf8Value);

            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);

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
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(string propertyName, ReadOnlySpan<byte> utf8Value)
            => WriteString(propertyName.AsSpan(), utf8Value);

        /// <summary>
        /// Writes the property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
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
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The JSON encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The value should already be escaped when the instance of <see cref="JsonEncodedText"/> was created. The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<char> propertyName, JsonEncodedText value)
            => WriteStringHelperEscapeProperty(propertyName, value.EncodedUtf8Bytes);

        private void WriteStringHelperEscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8Value.Length <= JsonConstants.MaxTokenSize);

            JsonWriterHelper.ValidateProperty(propertyName);

            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

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
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<char> propertyName, string value)
            => WriteString(propertyName, value.AsSpan());

        /// <summary>
        /// Writes the UTF-8 property name and pre-encoded value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The JSON encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The value should already be escaped when the instance of <see cref="JsonEncodedText"/> was created. The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, JsonEncodedText value)
            => WriteStringHelperEscapeProperty(utf8PropertyName, value.EncodedUtf8Bytes);

        private void WriteStringHelperEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8Value.Length <= JsonConstants.MaxTokenSize);

            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

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
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <remarks>
        /// The property name and value is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, string value)
            => WriteString(utf8PropertyName, value.AsSpan());

        private void WriteStringEscapeValueOnly(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < utf8Value.Length);

            byte[] valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndex);

            Span<byte> escapedValue = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndex, out int written);

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

            char[] valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndex);

            Span<char> escapedValue = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (valueArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndex, out int written);

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

            char[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndex);

            Span<char> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (propertyArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndex, out int written);

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

            byte[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndex);

            Span<byte> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (propertyArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndex, out int written);

            WriteStringByOptions(escapedPropertyName.Slice(0, written), escapedValue);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

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
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

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
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

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
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

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

        private void WriteStringEscapePropertyOrValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

            char[] valueArray = null;
            char[] propertyArray = null;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

                Span<char> escapedValue;
                if (length > JsonConstants.StackallocThreshold)
                {
                    valueArray = ArrayPool<char>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        char* ptr = stackalloc char[length];
                        escapedValue = new Span<char>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, out int written);
                value = escapedValue.Slice(0, written);
            }

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

                Span<char> escapedPropertyName;
                if (length > JsonConstants.StackallocThreshold)
                {
                    propertyArray = ArrayPool<char>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        char* ptr = stackalloc char[length];
                        escapedPropertyName = new Span<char>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, out int written);
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

        private void WriteStringEscapePropertyOrValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);

            byte[] valueArray = null;
            byte[] propertyArray = null;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

                Span<byte> escapedValue;
                if (length > JsonConstants.StackallocThreshold)
                {
                    valueArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        byte* ptr = stackalloc byte[length];
                        escapedValue = new Span<byte>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, out int written);
                utf8Value = escapedValue.Slice(0, written);
            }

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

                Span<byte> escapedPropertyName;
                if (length > JsonConstants.StackallocThreshold)
                {
                    propertyArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        byte* ptr = stackalloc byte[length];
                        escapedPropertyName = new Span<byte>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, out int written);
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

        private void WriteStringEscapePropertyOrValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

            byte[] valueArray = null;
            char[] propertyArray = null;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

                Span<byte> escapedValue;
                if (length > JsonConstants.StackallocThreshold)
                {
                    valueArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        byte* ptr = stackalloc byte[length];
                        escapedValue = new Span<byte>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, out int written);
                utf8Value = escapedValue.Slice(0, written);
            }

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

                Span<char> escapedPropertyName;
                if (length > JsonConstants.StackallocThreshold)
                {
                    propertyArray = ArrayPool<char>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        char* ptr = stackalloc char[length];
                        escapedPropertyName = new Span<char>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, out int written);
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

        private void WriteStringEscapePropertyOrValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value, int firstEscapeIndexProp, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);

            char[] valueArray = null;
            byte[] propertyArray = null;

            if (firstEscapeIndexVal != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

                Span<char> escapedValue;
                if (length > JsonConstants.StackallocThreshold)
                {
                    valueArray = ArrayPool<char>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        char* ptr = stackalloc char[length];
                        escapedValue = new Span<char>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, out int written);
                value = escapedValue.Slice(0, written);
            }

            if (firstEscapeIndexProp != -1)
            {
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

                Span<byte> escapedPropertyName;
                if (length > JsonConstants.StackallocThreshold)
                {
                    propertyArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets assigned to parameter and passed down.
                    unsafe
                    {
                        byte* ptr = stackalloc byte[length];
                        escapedPropertyName = new Span<byte>(ptr, length);
                    }
                }

                JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, out int written);
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
            if (Options.Indented)
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
            if (Options.Indented)
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
            if (Options.Indented)
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
            if (Options.Indented)
            {
                WriteStringIndented(utf8PropertyName, value);
            }
            else
            {
                WriteStringMinimized(utf8PropertyName, value);
            }
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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
            output[BytesPending++] = JsonConstants.KeyValueSeperator;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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
            output[BytesPending++] = JsonConstants.KeyValueSeperator;

            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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
            output[BytesPending++] = JsonConstants.KeyValueSeperator;

            output[BytesPending++] = JsonConstants.Quote;

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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
            output[BytesPending++] = JsonConstants.KeyValueSeperator;

            output[BytesPending++] = JsonConstants.Quote;

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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

            escapedValue.CopyTo(output.Slice(BytesPending));
            BytesPending += escapedValue.Length;

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/corefx/issues/36958
        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedValue.Length <= JsonConstants.MaxTokenSize);
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

            TranscodeAndWrite(escapedValue, output);

            output[BytesPending++] = JsonConstants.Quote;
        }
    }
}
