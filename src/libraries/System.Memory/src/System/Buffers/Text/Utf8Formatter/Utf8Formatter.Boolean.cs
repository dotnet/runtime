// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a Boolean as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="buffer">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     G (default)   True/False
        ///     l             true/false
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(bool value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            ReadOnlySpan<byte> result;
            if (format.IsDefault || format.Symbol == 'G')
            {
                result = value ? Utf8Constants.s_True : Utf8Constants.s_False;
            }
            else if (format.Symbol == 'l')
            {
                result = value ? Utf8Constants.s_true : Utf8Constants.s_false;
            }
            else
            {
                return ThrowHelper.TryFormatThrowFormatException(out bytesWritten);
            }

            if (!result.TryCopyTo(buffer))
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = result.Length;
            return true;
        }
    }
}
