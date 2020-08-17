// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the raw bytes value as a Base64 encoded JSON string as an element of a JSON array.
        /// </summary>
        /// <param name="bytes">The binary data to write as Base64 encoded text.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// The bytes are encoded before writing.
        /// </remarks>
        public void WriteBase64StringValue(ReadOnlySpan<byte> bytes)
        {
            JsonWriterHelper.ValidateBytes(bytes);

            WriteBase64ByOptions(bytes);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteBase64ByOptions(ReadOnlySpan<byte> bytes)
        {
            if (!_options.SkipValidation)
            {
                ValidateWritingValue();
            }

            if (_options.Indented)
            {
                WriteBase64Indented(bytes);
            }
            else
            {
                WriteBase64Minimized(bytes);
            }
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteBase64Minimized(ReadOnlySpan<byte> bytes)
        {
            int encodingLength = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

            Debug.Assert(encodingLength < int.MaxValue - 3);

            // 2 quotes to surround the base-64 encoded string value.
            // Optionally, 1 list separator
            int maxRequired = encodingLength + 3;

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

            Base64EncodeAndWrite(bytes, output, encodingLength);

            output[BytesPending++] = JsonConstants.Quote;
        }

        // TODO: https://github.com/dotnet/runtime/issues/29293
        private void WriteBase64Indented(ReadOnlySpan<byte> bytes)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            int encodingLength = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

            Debug.Assert(encodingLength < int.MaxValue - indent - 3 - s_newLineLength);

            // indentation + 2 quotes to surround the base-64 encoded string value.
            // Optionally, 1 list separator, and 1-2 bytes for new line
            int maxRequired = indent + encodingLength + 3 + s_newLineLength;

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

            Base64EncodeAndWrite(bytes, output, encodingLength);

            output[BytesPending++] = JsonConstants.Quote;
        }
    }
}
