// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        /// <summary>
        /// Writes the string text value (as a JSON comment).
        /// </summary>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON comment within /*..*/.</param>
        /// <param name="escape">If this is set to false, the writer assumes the value is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        public void WriteCommentValue(string value, bool escape = true)
            => WriteCommentValue(value.AsSpan(), escape);

        /// <summary>
        /// Writes the UTF-16 text value (as a JSON comment).
        /// </summary>
        /// <param name="value">The UTF-16 encoded value to be written as a UTF-8 transcoded JSON comment within /*..*/.</param>
        /// <param name="escape">If this is set to false, the writer assumes the value is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        public void WriteCommentValue(ReadOnlySpan<char> value, bool escape = true)
        {
            JsonWriterHelper.ValidateValue(value);

            if (escape)
            {
                WriteCommentEscape(value);
            }
            else
            {
                WriteCommentByOptions(value);
            }
        }

        private void WriteCommentEscape(ReadOnlySpan<char> value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(value);

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);

            if (valueIdx != -1)
            {
                WriteCommentEscapeValue(value, valueIdx);
            }
            else
            {
                WriteCommentByOptions(value);
            }
        }

        private void WriteCommentByOptions(ReadOnlySpan<char> value)
        {
            if (_writerOptions.Indented)
            {
                WriteCommentIndented(value);
            }
            else
            {
                WriteCommentMinimized(value);
            }
        }

        private void WriteCommentMinimized(ReadOnlySpan<char> escapedValue)
        {
            int idx = 0;

            WriteCommentValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteCommentIndented(ReadOnlySpan<char> escapedValue)
        {
            int idx = 0;

            if (_tokenType != JsonTokenType.None)
                WriteNewLine(ref idx);

            int indent = Indentation;
            while (true)
            {
                bool result = JsonWriterHelper.TryWriteIndentation(_buffer.Slice(idx), indent, out int bytesWritten);
                idx += bytesWritten;
                if (result)
                {
                    break;
                }
                indent -= bytesWritten;
                AdvanceAndGrow(ref idx);
            }

            WriteCommentValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteCommentEscapeValue(ReadOnlySpan<char> value, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < value.Length);

            char[] valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

            Span<char> escapedValue;
            if (length > StackallocThreshold)
            {
                valueArray = ArrayPool<char>.Shared.Rent(length);
                escapedValue = valueArray;
            }
            else
            {
                // Cannot create a span directly since it gets passed to instance methods on a ref struct.
                unsafe
                {
                    char* ptr = stackalloc char[length];
                    escapedValue = new Span<char>(ptr, length);
                }
            }
            JsonWriterHelper.EscapeString(value, escapedValue, firstEscapeIndexVal, out int written);

            WriteCommentByOptions(escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<char>.Shared.Return(valueArray);
            }
        }

        /// <summary>
        /// Writes the UTF-8 text value (as a JSON comment).
        /// </summary>
        /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON comment within /*..*/.</param>
        /// <param name="escape">If this is set to false, the writer assumes the value is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large.
        /// </exception>
        public void WriteCommentValue(ReadOnlySpan<byte> utf8Value, bool escape = true)
        {
            JsonWriterHelper.ValidateValue(utf8Value);

            if (escape)
            {
                WriteCommentEscape(utf8Value);
            }
            else
            {
                WriteCommentByOptions(utf8Value);
            }
        }

        private void WriteCommentEscape(ReadOnlySpan<byte> utf8Value)
        {
            int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Value);

            Debug.Assert(valueIdx >= -1 && valueIdx < int.MaxValue / 2);

            if (valueIdx != -1)
            {
                WriteCommentEscapeValue(utf8Value, valueIdx);
            }
            else
            {
                WriteCommentByOptions(utf8Value);
            }
        }

        private void WriteCommentByOptions(ReadOnlySpan<byte> utf8Value)
        {
            if (_writerOptions.Indented)
            {
                WriteCommentIndented(utf8Value);
            }
            else
            {
                WriteCommentMinimized(utf8Value);
            }
        }

        private void WriteCommentMinimized(ReadOnlySpan<byte> escapedValue)
        {
            int idx = 0;

            WriteCommentValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteCommentIndented(ReadOnlySpan<byte> escapedValue)
        {
            int idx = 0;
            WriteFormattingPreamble(ref idx);

            WriteCommentValue(escapedValue, ref idx);

            Advance(idx);
        }

        private void WriteCommentEscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

            byte[] valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

            Span<byte> escapedValue;
            if (length > StackallocThreshold)
            {
                valueArray = ArrayPool<byte>.Shared.Rent(length);
                escapedValue = valueArray;
            }
            else
            {
                // Cannot create a span directly since it gets passed to instance methods on a ref struct.
                unsafe
                {
                    byte* ptr = stackalloc byte[length];
                    escapedValue = new Span<byte>(ptr, length);
                }
            }
            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, out int written);

            WriteCommentByOptions(escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }
        }

        private void WriteCommentValue(ReadOnlySpan<char> escapedValue, ref int idx)
        {
            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Slash;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Asterisk;

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(escapedValue);
            int partialConsumed = 0;
            while (true)
            {
                OperationStatus status = JsonWriterHelper.ToUtf8(byteSpan.Slice(partialConsumed), _buffer.Slice(idx), out int consumed, out int written);
                idx += written;
                if (status == OperationStatus.Done)
                {
                    break;
                }
                partialConsumed += consumed;
                AdvanceAndGrow(ref idx);
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Asterisk;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Slash;
        }

        private void WriteCommentValue(ReadOnlySpan<byte> escapedValue, ref int idx)
        {
            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Slash;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Asterisk;

            CopyLoop(escapedValue, ref idx);

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Asterisk;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Slash;
        }
    }
}
