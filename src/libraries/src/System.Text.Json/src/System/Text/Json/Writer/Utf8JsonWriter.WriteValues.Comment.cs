// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        private static char[] s_singleLineCommentDelimiter = new char[2] { '*', '/' };
        private static ReadOnlySpan<byte> SingleLineCommentDelimiterUtf8 => new byte[2] { (byte)'*', (byte)'/' };

        /// <summary>
        /// Writes the string text value (as a JSON comment).
        /// </summary>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON comment within /*..*/.</param>
        /// <remarks>
        /// The comment value is not escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large OR if the given string text value contains a comment delimiter (i.e. */).
        /// </exception>
        public void WriteCommentValue(string value)
            => WriteCommentValue(value.AsSpan());

        /// <summary>
        /// Writes the UTF-16 text value (as a JSON comment).
        /// </summary>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON comment within /*..*/.</param>
        /// <remarks>
        /// The comment value is not escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large OR if the given UTF-16 text value contains a comment delimiter (i.e. */).
        /// </exception>
        public void WriteCommentValue(ReadOnlySpan<char> value)
        {
            JsonWriterHelper.ValidateValue(value);

            if (value.IndexOf(s_singleLineCommentDelimiter) != -1)
            {
                ThrowHelper.ThrowArgumentException_InvalidCommentValue();
            }

            WriteCommentByOptions(value);
        }

        private void WriteCommentByOptions(ReadOnlySpan<char> value)
        {
            if (Options.Indented)
            {
                WriteCommentIndented(value);
            }
            else
            {
                WriteCommentMinimized(value);
            }
        }

        private void WriteCommentMinimized(ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - 4);

            // All ASCII, /*...*/ => escapedValue.Length + 4
            // Optionally, up to 3x growth when transcoding
            int maxRequired = (value.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 4;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            output[BytesPending++] = JsonConstants.Slash;
            output[BytesPending++] = JsonConstants.Asterisk;

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(value);
            OperationStatus status = JsonWriterHelper.ToUtf8(byteSpan, output.Slice(BytesPending), out int _, out int written);
            Debug.Assert(status != OperationStatus.DestinationTooSmall);
            BytesPending += written;

            output[BytesPending++] = JsonConstants.Asterisk;
            output[BytesPending++] = JsonConstants.Slash;
        }

        private void WriteCommentIndented(ReadOnlySpan<char> value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(value.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - indent - 4 - s_newLineLength);

            // All ASCII, /*...*/ => escapedValue.Length + 4
            // Optionally, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (value.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 4 + s_newLineLength;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output);
            }

            JsonWriterHelper.WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Slash;
            output[BytesPending++] = JsonConstants.Asterisk;

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(value);
            OperationStatus status = JsonWriterHelper.ToUtf8(byteSpan, output.Slice(BytesPending), out int _, out int written);
            Debug.Assert(status != OperationStatus.DestinationTooSmall);
            BytesPending += written;

            output[BytesPending++] = JsonConstants.Asterisk;
            output[BytesPending++] = JsonConstants.Slash;
        }

        /// <summary>
        /// Writes the UTF-8 text value (as a JSON comment).
        /// </summary>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON comment within /*..*/.</param>
        /// <remarks>
        /// The comment value is not escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large OR if the given UTF-8 text value contains a comment delimiter (i.e. */).
        /// </exception>
        public void WriteCommentValue(ReadOnlySpan<byte> utf8Value)
        {
            JsonWriterHelper.ValidateValue(utf8Value);

            if (utf8Value.IndexOf(SingleLineCommentDelimiterUtf8) != -1)
            {
                ThrowHelper.ThrowArgumentException_InvalidCommentValue();
            }

            WriteCommentByOptions(utf8Value);
        }

        private void WriteCommentByOptions(ReadOnlySpan<byte> utf8Value)
        {
            if (Options.Indented)
            {
                WriteCommentIndented(utf8Value);
            }
            else
            {
                WriteCommentMinimized(utf8Value);
            }
        }

        private void WriteCommentMinimized(ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8Value.Length < int.MaxValue - 4);

            int maxRequired = utf8Value.Length + 4; // /*...*/

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            output[BytesPending++] = JsonConstants.Slash;
            output[BytesPending++] = JsonConstants.Asterisk;

            utf8Value.CopyTo(output.Slice(BytesPending));
            BytesPending += utf8Value.Length;

            output[BytesPending++] = JsonConstants.Asterisk;
            output[BytesPending++] = JsonConstants.Slash;
        }

        private void WriteCommentIndented(ReadOnlySpan<byte> utf8Value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(utf8Value.Length < int.MaxValue - indent - 4 - s_newLineLength);

            int minRequired = indent + utf8Value.Length + 4; // /*...*/
            int maxRequired = minRequired + s_newLineLength; // Optionally, 1-2 bytes for new line

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output);
            }

            JsonWriterHelper.WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = JsonConstants.Slash;
            output[BytesPending++] = JsonConstants.Asterisk;

            utf8Value.CopyTo(output.Slice(BytesPending));
            BytesPending += utf8Value.Length;

            output[BytesPending++] = JsonConstants.Asterisk;
            output[BytesPending++] = JsonConstants.Slash;
        }
    }
}
