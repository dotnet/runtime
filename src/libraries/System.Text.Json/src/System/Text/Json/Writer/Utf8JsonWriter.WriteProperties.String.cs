// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(string propertyName, string value, bool escape = true)
            => WriteString(propertyName.AsSpan(), value.AsSpan(), escape);

        /// <summary>
        /// Writes the UTF-16 property name and UTF-16 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value, bool escape = true)
        {
            JsonWriterHelper.ValidatePropertyAndValue(propertyName, value);

            if (escape)
            {
                WriteStringEscape(propertyName, value);
            }
            else
            {
                WriteStringDontEscape(propertyName, value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the UTF-8 property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value, bool escape = true)
        {
            JsonWriterHelper.ValidatePropertyAndValue(utf8PropertyName, utf8Value);

            if (escape)
            {
                WriteStringEscape(utf8PropertyName, utf8Value);
            }
            else
            {
                WriteStringDontEscape(utf8PropertyName, utf8Value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and UTF-16 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(string propertyName, ReadOnlySpan<char> value, bool escape = true)
            => WriteString(propertyName.AsSpan(), value, escape);

        /// <summary>
        /// Writes the UTF-8 property name and UTF-16 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value, bool escape = true)
        {
            JsonWriterHelper.ValidatePropertyAndValue(utf8PropertyName, value);

            if (escape)
            {
                WriteStringEscape(utf8PropertyName, value);
            }
            else
            {
                WriteStringDontEscape(utf8PropertyName, value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(string propertyName, ReadOnlySpan<byte> utf8Value, bool escape = true)
            => WriteString(propertyName.AsSpan(), utf8Value, escape);

        /// <summary>
        /// Writes the UTF-16 property name and UTF-8 text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value, bool escape = true)
        {
            JsonWriterHelper.ValidatePropertyAndValue(propertyName, utf8Value);

            if (escape)
            {
                WriteStringEscape(propertyName, utf8Value);
            }
            else
            {
                WriteStringDontEscape(propertyName, utf8Value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the UTF-16 property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<char> propertyName, string value, bool escape = true)
            => WriteString(propertyName, value.AsSpan(), escape);

        /// <summary>
        /// Writes the UTF-8 property name and string text value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.
        /// The value is always escaped</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name or value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, string value, bool escape = true)
            => WriteString(utf8PropertyName, value.AsSpan(), escape);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringDontEscape(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);
            if (valueIdx != -1)
            {
                WriteStringEscapeValueOnly(escapedPropertyName, value, valueIdx);
            }
            else
            {
                WriteStringByOptions(escapedPropertyName, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringDontEscape(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> utf8Value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);
            if (valueIdx != -1)
            {
                WriteStringEscapeValueOnly(escapedPropertyName, utf8Value, valueIdx);
            }
            else
            {
                WriteStringByOptions(escapedPropertyName, utf8Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringDontEscape(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> utf8Value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);
            if (valueIdx != -1)
            {
                WriteStringEscapeValueOnly(escapedPropertyName, utf8Value, valueIdx);
            }
            else
            {
                WriteStringByOptions(escapedPropertyName, utf8Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringDontEscape(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);
            if (valueIdx != -1)
            {
                WriteStringEscapeValueOnly(escapedPropertyName, value, valueIdx);
            }
            else
            {
                WriteStringByOptions(escapedPropertyName, value);
            }
        }

        private void WriteStringEscapeValueOnly(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> value, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < value.Length);

            char[] valueArray = ArrayPool<char>.Shared.Rent(JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndex));
            Span<char> escapedValue = valueArray;
            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndex, out int written);

            WriteStringByOptions(escapedPropertyName, escapedValue.Slice(0, written));

            ArrayPool<char>.Shared.Return(valueArray);
        }

        private void WriteStringEscapeValueOnly(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < utf8Value.Length);

            byte[] valueArray = ArrayPool<byte>.Shared.Rent(JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndex));
            Span<byte> escapedValue = valueArray;
            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndex, out int written);

            WriteStringByOptions(escapedPropertyName, escapedValue.Slice(0, written));

            ArrayPool<byte>.Shared.Return(valueArray);
        }

        private void WriteStringEscapeValueOnly(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < utf8Value.Length);

            byte[] valueArray = ArrayPool<byte>.Shared.Rent(JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndex));
            Span<byte> escapedValue = valueArray;
            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndex, out int written);

            WriteStringByOptions(escapedPropertyName, escapedValue.Slice(0, written));

            ArrayPool<byte>.Shared.Return(valueArray);
        }

        private void WriteStringEscapeValueOnly(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> value, int firstEscapeIndex)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndex >= 0 && firstEscapeIndex < value.Length);

            char[] valueArray = ArrayPool<char>.Shared.Rent(JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndex));
            Span<char> escapedValue = valueArray;
            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndex, out int written);

            WriteStringByOptions(escapedPropertyName, escapedValue.Slice(0, written));

            ArrayPool<char>.Shared.Return(valueArray);
        }

        private void WriteStringEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

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

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

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

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

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

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);
            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

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
                if (length > StackallocThreshold)
                {
                    valueArray = ArrayPool<char>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
                if (length > StackallocThreshold)
                {
                    propertyArray = ArrayPool<char>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
                if (length > StackallocThreshold)
                {
                    valueArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
                if (length > StackallocThreshold)
                {
                    propertyArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
                if (length > StackallocThreshold)
                {
                    valueArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
                if (length > StackallocThreshold)
                {
                    propertyArray = ArrayPool<char>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
                if (length > StackallocThreshold)
                {
                    valueArray = ArrayPool<char>.Shared.Rent(length);
                    escapedValue = valueArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
                if (length > StackallocThreshold)
                {
                    propertyArray = ArrayPool<byte>.Shared.Rent(length);
                    escapedPropertyName = propertyArray;
                }
                else
                {
                    // Cannot create a span directly since it gets passed to instance methods on a ref struct.
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
            if (_writerOptions.Indented)
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
            if (_writerOptions.Indented)
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
            if (_writerOptions.Indented)
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
            if (_writerOptions.Indented)
            {
                WriteStringIndented(utf8PropertyName, value);
            }
            else
            {
                WriteStringMinimized(utf8PropertyName, value);
            }
        }

        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringValue(ReadOnlySpan<char> escapedValue, ref int idx)
        {
            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(escapedValue);
            int partialConsumed = 0;
            while (true)
            {
                OperationStatus status = JsonWriterHelper.ToUtf8(byteSpan.Slice(partialConsumed), _buffer.Slice(idx), out int consumed, out int written);
                idx += written;
                if (status == OperationStatus.Done)
                {
                    break;
                }
                partialConsumed += consumed;
                AdvanceAndGrow(ref idx);
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;
        }

        private void WriteStringValue(ReadOnlySpan<byte> escapedValue, ref int idx)
        {
            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            CopyLoop(escapedValue, ref idx);

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;
        }
    }
}
