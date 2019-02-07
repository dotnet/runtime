// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the property name and <see cref="Guid"/> value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="Guid"/> using the default <see cref="StandardFormat"/> (i.e. 'D'), as the form: nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn.
        /// </remarks>
        public void WriteString(string propertyName, Guid value, bool escape = true)
            => WriteString(propertyName.AsSpan(), value, escape);

        /// <summary>
        /// Writes the property name and <see cref="Guid"/> value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="Guid"/> using the default <see cref="StandardFormat"/> (i.e. 'D'), as the form: nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn.
        /// </remarks>
        public void WriteString(ReadOnlySpan<char> propertyName, Guid value, bool escape = true)
        {
            JsonWriterHelper.ValidateProperty(propertyName);

            if (escape)
            {
                WriteStringEscape(propertyName, value);
            }
            else
            {
                WriteStringByOptions(propertyName, value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        /// <summary>
        /// Writes the property name and <see cref="Guid"/> value (as a JSON string) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a JSON string as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="Guid"/> using the default <see cref="StandardFormat"/> (i.e. 'D'), as the form: nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn.
        /// </remarks>
        public void WriteString(ReadOnlySpan<byte> utf8PropertyName, Guid value, bool escape = true)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            if (escape)
            {
                WriteStringEscape(utf8PropertyName, value);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringEscape(ReadOnlySpan<char> propertyName, Guid value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteStringEscapeProperty(propertyName, value, propertyIdx);
            }
            else
            {
                WriteStringByOptions(propertyName, value);
            }
        }

        private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, Guid value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteStringEscapeProperty(utf8PropertyName, value, propertyIdx);
            }
            else
            {
                WriteStringByOptions(utf8PropertyName, value);
            }
        }

        private void WriteStringEscapeProperty(ReadOnlySpan<char> propertyName, Guid value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < propertyName.Length);

            char[] propertyArray = null;

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

            WriteStringByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, Guid value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < utf8PropertyName.Length);

            byte[] propertyArray = null;

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

            WriteStringByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteStringByOptions(ReadOnlySpan<char> propertyName, Guid value)
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

        private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, Guid value)
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

        private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, Guid value)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteStringValue(value, ref idx);

            Advance(idx);
        }

        private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, Guid value)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteStringValue(value, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, Guid value)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteStringValue(value, ref idx);

            Advance(idx);
        }

        private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, Guid value)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteStringValue(value, ref idx);

            Advance(idx);
        }

        private void WriteStringValue(Guid value, ref int idx)
        {
            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            FormatLoop(value, ref idx);

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;
        }

        private void FormatLoop(Guid value, ref int idx)
        {
            if (!Utf8Formatter.TryFormat(value, _buffer.Slice(idx), out int bytesWritten))
            {
                AdvanceAndGrow(ref idx, JsonConstants.MaximumFormatGuidLength);
                bool result = Utf8Formatter.TryFormat(value, _buffer, out bytesWritten);
                Debug.Assert(result);
            }
            idx += bytesWritten;
        }
    }
}
