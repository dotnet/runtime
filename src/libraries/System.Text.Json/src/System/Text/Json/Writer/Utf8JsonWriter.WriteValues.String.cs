// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the string text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string element of a JSON array.</param>
        /// <param name="escape">If this is set to false, the writer assumes the value is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(string value, bool escape = true)
           => WriteStringValue(value.AsSpan(), escape);

        /// <summary>
        /// Writes the UTF-16 text value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON string element of a JSON array.</param>
        /// <param name="escape">If this is set to false, the writer assumes the value is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(ReadOnlySpan<char> value, bool escape = true)
        {
            JsonWriterHelper.ValidateValue(value);

            if (escape)
            {
                WriteStringEscape(value);
            }
            else
            {
                WriteStringByOptions(value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringEscape(ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);

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
            if (_writerOptions.Indented)
            {
                WriteStringIndented(value);
            }
            else
            {
                WriteStringMinimized(value);
            }
        }

        private void WriteStringMinimized(ReadOnlySpan<char> escapedValue)
        {
            int idx = 0;
            WriteListSeparator(ref idx);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<char> escapedValue)
        {
            int idx = WriteCommaAndFormattingPreamble();

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringEscapeValue(ReadOnlySpan<char> value, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < value.Length);

            char[] valueArray = null;

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
        /// <param name="escape">If this is set to false, the writer assumes the value is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStringValue(ReadOnlySpan<byte> utf8Value, bool escape = true)
        {
            JsonWriterHelper.ValidateValue(utf8Value);

            if (escape)
            {
                WriteStringEscape(utf8Value);
            }
            else
            {
                WriteStringByOptions(utf8Value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringEscape(ReadOnlySpan<byte> utf8Value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);

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
            if (_writerOptions.Indented)
            {
                WriteStringIndented(utf8Value);
            }
            else
            {
                WriteStringMinimized(utf8Value);
            }
        }

        private void WriteStringMinimized(ReadOnlySpan<byte> escapedValue)
        {
            int idx = 0;
            WriteListSeparator(ref idx);

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<byte> escapedValue)
        {
            int idx = WriteCommaAndFormattingPreamble();

            WriteStringValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteStringEscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

            byte[] valueArray = null;

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

            WriteStringByOptions(escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }
        }
    }
}
