// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the <see cref="int"/> value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to be written as a JSON number as an element of a JSON array.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="int"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumberValue(int value)
            => WriteNumberValue((long)value);

        /// <summary>
        /// Writes the <see cref="long"/> value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to be written as a JSON number as an element of a JSON array.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="long"/> using the default <see cref="StandardFormat"/> (i.e. 'G'), for example: 32767.
        /// </remarks>
        public void WriteNumberValue(long value)
        {
            ValidateWritingValue();
            if (_writerOptions.Indented)
            {
                WriteNumberValueIndented(value);
            }
            else
            {
                WriteNumberValueMinimized(value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        private void WriteNumberValueMinimized(long value)
        {
            int idx = 0;
            WriteListSeparator(ref idx);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberValueIndented(long value)
        {
            int idx = WriteCommaAndFormattingPreamble();

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }
    }
}
