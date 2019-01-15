// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the <see cref="DateTime"/> value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to be written as a JSON string as an element of a JSON array.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="DateTime"/> using the round-trippable ('O') <see cref="StandardFormat"/> , for example: 2017-06-12T05:30:45.7680000.
        /// </remarks>
        public void WriteStringValue(DateTime value)
        {
            ValidateWritingValue();
            if (_writerOptions.Indented)
            {
                WriteStringValueIndented(value);
            }
            else
            {
                WriteStringValueMinimized(value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringValueMinimized(DateTime value)
        {
            int idx = 0;
            WriteListSeparator(ref idx);

            WriteStringValue(value, ref idx);

            Advance(idx);
        }

        private void WriteStringValueIndented(DateTime value)
        {
            int idx = WriteCommaAndFormattingPreamble();

            WriteStringValue(value, ref idx);

            Advance(idx);
        }
    }
}
