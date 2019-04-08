// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a Double as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="destination">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     G/g  (default)  
        ///     F/f             12.45       Fixed point
        ///     E/e             1.245000e1  Exponential
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(double value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            return TryFormatFloatingPoint<double>(value, destination, out bytesWritten, format);
        }

        /// <summary>
        /// Formats a Single as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="destination">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     G/g  (default)  
        ///     F/f             12.45       Fixed point
        ///     E/e             1.245000e1  Exponential
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(float value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            return TryFormatFloatingPoint<float>(value, destination, out bytesWritten, format);
        }

        private static bool TryFormatFloatingPoint<T>(T value, Span<byte> destination, out int bytesWritten, StandardFormat format) where T : IFormattable, ISpanFormattable
        {
            Span<char> formatText = stackalloc char[0];

            if (!format.IsDefault)
            {
                formatText = stackalloc char[StandardFormat.FormatStringLength];
                int formatTextLength = format.Format(formatText);
                formatText = formatText.Slice(0, formatTextLength);
            }

            // We first try to format into a stack-allocated buffer, and if it succeeds, we can avoid
            // all allocation.  If that fails, we fall back to allocating strings.  If it proves impactful,
            // that allocation (as well as roundtripping from byte to char and back to byte) could be avoided by
            // calling into a refactored Number.FormatSingle/Double directly.

            const int StackBufferLength = 128; // large enough to handle the majority cases
            Span<char> stackBuffer = stackalloc char[StackBufferLength];
            ReadOnlySpan<char> utf16Text = stackalloc char[0];

            // Try to format into the stack buffer.  If we're successful, we can avoid all allocations.
            if (value.TryFormat(stackBuffer, out int formattedLength, formatText, CultureInfo.InvariantCulture))
            {
                utf16Text = stackBuffer.Slice(0, formattedLength);
            }
            else
            {
                // The stack buffer wasn't large enough.  If the destination buffer isn't at least as
                // big as the stack buffer, we know the whole operation will eventually fail, so we
                // can just fail now.
                if (destination.Length <= StackBufferLength)
                {
                    bytesWritten = 0;
                    return false;
                }

                // Fall back to using a string format and allocating a string for the resulting formatted value.
                utf16Text = value.ToString(new string(formatText), CultureInfo.InvariantCulture);
            }

            // Copy the value to the destination, if it's large enough.

            if (utf16Text.Length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            try
            {
                bytesWritten = Encoding.UTF8.GetBytes(utf16Text, destination);
                return true;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
        }
    }
}
