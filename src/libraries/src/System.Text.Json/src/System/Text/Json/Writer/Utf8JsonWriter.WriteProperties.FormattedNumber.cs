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
        /// Writes the property name and value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="utf8FormattedNumber">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="utf8FormattedNumber"/> does not represent a valid JSON number.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="long"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        internal void WriteNumber(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8FormattedNumber)
        {
            JsonWriterHelper.ValidateProperty(propertyName);
            JsonWriterHelper.ValidateNumber(utf8FormattedNumber);

            WriteNumberEscape(propertyName, utf8FormattedNumber);
            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        /// <summary>
        /// Writes the property name and value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="utf8FormattedNumber">The value to be written as a JSON number as part of the name/value pair.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="utf8FormattedNumber"/> does not represent a valid JSON number.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="long"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        internal void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8FormattedNumber)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);
            JsonWriterHelper.ValidateNumber(utf8FormattedNumber);

            WriteNumberEscape(utf8PropertyName, utf8FormattedNumber);
            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        private void WriteNumberEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
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

        private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
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

        private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value, int firstEscapeIndexProp)
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

        private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value, int firstEscapeIndexProp)
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

        private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
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

        private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
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

        private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            int idx = WritePropertyNameMinimized(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> value)
        {
            int idx = WritePropertyNameIndented(escapedPropertyName);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberValueFormatLoop(ReadOnlySpan<byte> value, ref int idx)
        {
            if (_buffer.Length - idx - value.Length < 0)
            {
                AdvanceAndGrow(ref idx, value.Length);
            }

            value.CopyTo(_buffer.Slice(idx));
            idx += value.Length;
        }
    }
}
