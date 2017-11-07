// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a Double as a UTF8 string.
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
        ///     G/g  (default)  
        ///     F/f             12.45       Fixed point
        ///     E/e             1.245000e1  Exponential
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(double value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            return TryFormatFloatingPoint<double>(value, buffer, out bytesWritten, format);
        }

        /// <summary>
        /// Formats a Single as a UTF8 string.
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
        ///     G/g  (default)  
        ///     F/f             12.45       Fixed point
        ///     E/e             1.245000e1  Exponential
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(float value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            return TryFormatFloatingPoint<float>(value, buffer, out bytesWritten, format);
        }

        //
        // Common handler for TryFormat(Double) and TryFormat(Single). You may notice that this particular routine isn't getting into the "no allocation" spirit
        // of things. The DoubleToNumber() code is incredibly complex and is one of the few pieces of Number formatting never C#-ized. It would be really 
        // be preferable not to have another version of that lying around. Until we really hit a scenario where floating point formatting needs the perf, we'll
        // make do with this.
        //
        private static bool TryFormatFloatingPoint<T>(T value, Span<byte> buffer, out int bytesWritten, StandardFormat format) where T : IFormattable
        {
            if (format.IsDefault)
            {
                format = 'G';
            }

            switch (format.Symbol)
            {
                case 'g':
                case 'G':
                    if (format.Precision != StandardFormat.NoPrecision)
                        throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported);
                    break;

                case 'f':
                case 'F':
                case 'e':
                case 'E':
                    break;

                default:
                    return ThrowHelper.TryFormatThrowFormatException(out bytesWritten);
            }

            string formatString = format.ToString();
            string utf16Text = value.ToString(formatString, CultureInfo.InvariantCulture);
            int length = utf16Text.Length;
            if (length > buffer.Length)
            {
                bytesWritten = 0;
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                Debug.Assert(utf16Text[i] < 128, "A culture-invariant ToString() of a floating point expected to produce ASCII characters only.");
                buffer[i] = (byte)(utf16Text[i]);
            }

            bytesWritten = length;
            return true;
        }
    }
}
