// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the <see cref="TimeSpan"/> value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="TimeSpan"/> using the constant ('c') <see cref="StandardFormat"/> , for example: 10:02:35.
        /// </remarks>
        public void WriteStringValue(TimeSpan value)
        {
            if (!_options.SkipValidation)
            {
                ValidateWritingValue();
            }

            if (_options.Indented)
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

        private void WriteStringValueMinimized(TimeSpan value)
        {
            int maxRequired = JsonConstants.MaximumTimeSpanParseLength + 3; // 2 quotes, and optionally, 1 list separator

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

            bool result = Utf8Formatter.TryFormat(value, output.Slice(BytesPending), out int bytesWritten, 'c');
            Debug.Assert(result);
            BytesPending += bytesWritten;

            output[BytesPending++] = JsonConstants.Quote;
        }

        private void WriteStringValueIndented(TimeSpan value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            // 2 quotes, and optionally, 1 list separator and 1-2 bytes for new line
            int maxRequired = indent + JsonConstants.MaximumTimeSpanParseLength + 3 + s_newLineLength;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.PropertyName)
            {
                if (_tokenType != JsonTokenType.None)
                {
                    WriteNewLine(output);
                }
                JsonWriterHelper.WriteIndentation(output.Slice(BytesPending), indent);
                BytesPending += indent;
            }

            output[BytesPending++] = JsonConstants.Quote;

            bool result = Utf8Formatter.TryFormat(value, output.Slice(BytesPending), out int bytesWritten, 'c');
            Debug.Assert(result);
            BytesPending += bytesWritten;

            output[BytesPending++] = JsonConstants.Quote;
        }
    }
}
