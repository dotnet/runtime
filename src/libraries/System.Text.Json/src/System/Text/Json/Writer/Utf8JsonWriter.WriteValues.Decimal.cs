// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the <see cref="decimal"/> value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to be written as a JSON number as an element of a JSON array.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="decimal"/> using the default <see cref="StandardFormat"/> (i.e. 'G').
        /// </remarks>
        public void WriteNumberValue(decimal value)
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

        private void WriteNumberValueMinimized(decimal value)
        {
            int idx = 0;
            WriteListSeparator(ref idx);

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }

        private void WriteNumberValueIndented(decimal value)
        {
            int idx = WriteCommaAndFormattingPreamble();

            WriteNumberValueFormatLoop(value, ref idx);

            Advance(idx);
        }
    }
}
