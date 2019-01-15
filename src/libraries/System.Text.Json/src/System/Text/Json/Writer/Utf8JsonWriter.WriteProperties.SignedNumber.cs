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
        /// Writes the property name and <see cref="long"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="long"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumber(string propertyName, long value, bool escape = true)
            => WriteNumber(propertyName.AsSpan(), value, escape);

        /// <summary>
        /// Writes the property name and <see cref="long"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="long"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumber(ReadOnlySpan<char> propertyName, long value, bool escape = true)
        {
            JsonWriterHelper.ValidateProperty(propertyName);

            if (escape)
            {
                WriteNumberEscape(propertyName, value);
            }
            else
            {
                WriteNumberByOptions(propertyName, value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        /// <summary>
        /// Writes the property name and <see cref="long"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="long"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, long value, bool escape = true)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);

            if (escape)
            {
                WriteNumberEscape(utf8PropertyName, value);
            }
            else
            {
                WriteNumberByOptions(utf8PropertyName, value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        /// <summary>
        /// Writes the property name and <see cref="int"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="int"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumber(string propertyName, int value, bool escape = true)
            => WriteNumber(propertyName.AsSpan(), (long)value, escape);

        /// <summary>
        /// Writes the property name and <see cref="int"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="value">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="int"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumber(ReadOnlySpan<char> propertyName, int value, bool escape = true)
            => WriteNumber(propertyName, (long)value, escape);

        /// <summary>
        /// Writes the property name and <see cref="int"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="value">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="int"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, int value, bool escape = true)
            => WriteNumber(utf8PropertyName, (long)value, escape);

        private void WriteNumberEscape(ReadOnlySpan<char> propertyName, long value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteNumberEscapeProperty(propertyName, value, propertyIdx);
            }
            else
            {
                WriteNumberByOptions(propertyName, value);
            }
        }

        private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, long value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

            if (propertyIdx != -1)
            {
                WriteNumberEscapeProperty(utf8PropertyName, value, propertyIdx);
            }
            else
            {
                WriteNumberByOptions(utf8PropertyName, value);
            }
        }

        private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, long value, int firstEscapeIndexProp)
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

            WriteNumberByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, long value, int firstEscapeIndexProp)
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

            WriteNumberByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, long value)
        {
            ValidateWritingProperty();
            if (_writerOptions.Indented)
            {
                WriteNumberIndented(propertyName, value);
            }
            else
            {
                WriteNumberMinimized(propertyName, value);
            }
        }

        private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, long value)
        {
            ValidateWritingProperty();
            if (_writerOptions.Indented)
            {
                WriteNumberIndented(utf8PropertyName, value);
            }
            else
            {
                WriteNumberMinimized(utf8PropertyName, value);
            }
        }

        private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, long value)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, long value)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, long value)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, long value)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberValueFormatLoop(long value, ref int idx)
        {
            if (!Utf8Formatter.TryFormat(value, _buffer.Slice(idx), out int bytesWritten))
            {
                AdvanceAndGrow(ref idx, JsonConstants.MaximumFormatInt64Length);
                bool result = Utf8Formatter.TryFormat(value, _buffer, out bytesWritten);
                Debug.Assert(result);
            }
            idx += bytesWritten;
        }
    }
}
