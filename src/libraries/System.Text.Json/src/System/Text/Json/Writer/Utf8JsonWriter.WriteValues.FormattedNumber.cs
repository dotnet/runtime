// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="utf8FormattedNumber">The value to be written as a JSON number as an element of a JSON array.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="utf8FormattedNumber"/> does not represent a valid JSON number.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="int"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        internal void WriteNumberValue(ReadOnlySpan<byte> utf8FormattedNumber)
        {
            JsonWriterHelper.ValidateNumber(utf8FormattedNumber);

            ValidateWritingValue();
            if (_writerOptions.Indented)
            {
                WriteNumberValueIndented(utf8FormattedNumber);
            }
            else
            {
                WriteNumberValueMinimized(utf8FormattedNumber);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        private void WriteNumberValueMinimized(ReadOnlySpan<byte> value)
        {
            int idx = 0;
            WriteListSeparator(ref idx);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberValueIndented(ReadOnlySpan<byte> value)
        {
            int idx = WriteCommaAndFormattingPreamble();

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }
    }
}
