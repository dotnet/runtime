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
        /// Writes the <see cref="uint"/> value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="uint"/> using the default <see cref="StandardFormat"/> (that is, 'G'), for example: 32767.
        /// </remarks>
        [CLSCompliant(false)]
        public void WriteNumberValue(uint value)
            => WriteNumberValue((ulong)value);

        /// <summary>
        /// Writes the <see cref="ulong"/> value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="ulong"/> using the default <see cref="StandardFormat"/> (that is, 'G'), for example: 32767.
        /// </remarks>
        [CLSCompliant(false)]
        public void WriteNumberValue(ulong value)
        {
            if (!_options.SkipValidation)
            {
                ValidateWritingValue();
            }

            if (_options.Indented)
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

        private void WriteNumberValueMinimized(ulong value)
        {
            int maxRequired = JsonConstants.MaximumFormatUInt64Length + 1; // Optionally, 1 list separator

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            bool result = Utf8Formatter.TryFormat(value, output.Slice(BytesPending), out int bytesWritten);
            Debug.Assert(result);
            BytesPending += bytesWritten;
        }

        private void WriteNumberValueIndented(ulong value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * _options.MaxDepth);

            int maxRequired = indent + JsonConstants.MaximumFormatUInt64Length + 1 + s_newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line

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

            bool result = Utf8Formatter.TryFormat(value, output.Slice(BytesPending), out int bytesWritten);
            Debug.Assert(result);
            BytesPending += bytesWritten;
        }

        internal void WriteNumberValueAsString(ulong value)
        {
            Span<byte> utf8Number = stackalloc byte[JsonConstants.MaximumFormatUInt64Length];
            bool result = Utf8Formatter.TryFormat(value, utf8Number, out int bytesWritten);
            Debug.Assert(result);
            WriteNumberValueAsStringUnescaped(utf8Number.Slice(0, bytesWritten));
        }

#if NET7_0_OR_GREATER
        /// <summary>
        /// Writes the <see cref="UInt128"/> value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="UInt128"/> using the default <see cref="StandardFormat"/> (that is, 'G'), for example: 32767.
        /// </remarks>
        [CLSCompliant(false)]
        public void WriteNumberValue(UInt128 value)
        {
            // TODO: [ActiveIssue("https://github.com/dotnet/runtime/issues/73842")]
            // TODO: Once Utf8Formatter has UInt128 overload this implementation should be replaced with similar to UInt64

            if (_options.Indented)
            {
                int indent = Indentation;
                int maxRequired = indent + 1 + s_newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line
                if (_memory.Length - BytesPending < maxRequired)
                {
                    Grow(maxRequired);
                }

                Span<byte> output = _memory.Span;

                if (_currentDepth < 0)
                {
                    output[BytesPending++] = JsonConstants.ListSeparator;
                    // clear 'Add list separator' flag so that we don't end up having two separators
                    // due to using WriteRawValue
                    _currentDepth = CurrentDepth;
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
            }

            WriteRawValue(value.ToString("G"), skipInputValidation: true);
            _tokenType = JsonTokenType.Number;
        }

        internal void WriteNumberValueAsString(UInt128 value)
        {
            // TODO: [ActiveIssue("https://github.com/dotnet/runtime/issues/73842")]
            // TODO: Once Utf8Formatter has UInt128 overload this implementation should be replaced with similar to UInt64

            string valueAsString = value.ToString("G");

            byte[]? tempArray = null;

            // For performance, avoid obtaining actual byte count unless memory usage is higher than the threshold.
            Span<byte> utf8 = valueAsString.Length <= (JsonConstants.ArrayPoolMaxSizeBeforeUsingNormalAlloc / JsonConstants.MaxExpansionFactorWhileTranscoding) ?
                // Use a pooled alloc.
                tempArray = ArrayPool<byte>.Shared.Rent(valueAsString.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) :
                // Use a normal alloc since the pool would create a normal alloc anyway based on the threshold (per current implementation)
                // and by using a normal alloc we can avoid the Clear().
                new byte[JsonReaderHelper.GetUtf8ByteCount(valueAsString)];

            try
            {
                int actualByteCount = JsonReaderHelper.GetUtf8FromText(valueAsString, utf8);
                utf8 = utf8.Slice(0, actualByteCount);
                WriteNumberValueAsStringUnescaped(utf8);
            }
            finally
            {
                if (tempArray != null)
                {
                    utf8.Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }
            }
        }
#endif
    }
}
