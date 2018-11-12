// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a Boolean as a UTF8 string.
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
        ///     G (default)   True/False
        ///     l             true/false
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(bool value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            char symbol = FormattingHelpers.GetSymbolOrDefault(format, 'G');

            if (value)
            {
                if (symbol == 'G')
                {
                    // By having each branch perform its own call to TryWriteUInt32BigEndian, we ensure that a
                    // constant value is passed to this routine, which means the compiler can reverse endianness
                    // at compile time instead of runtime if necessary.
                    const uint TrueValueUppercase = ('T' << 24) + ('r' << 16) + ('u' << 8) + ('e' << 0);
                    if (!BinaryPrimitives.TryWriteUInt32BigEndian(destination, TrueValueUppercase))
                    {
                        goto BufferTooSmall;
                    }
                }
                else if (symbol == 'l')
                {
                    const uint TrueValueLowercase = ('t' << 24) + ('r' << 16) + ('u' << 8) + ('e' << 0);
                    if (!BinaryPrimitives.TryWriteUInt32BigEndian(destination, TrueValueLowercase))
                    {
                        goto BufferTooSmall;
                    }
                }
                else
                {
                    goto BadFormat;
                }

                bytesWritten = 4;
                return true;
            }
            else
            {
                if (symbol == 'G')
                {
                    // This check can't be performed earlier because we need to throw if an invalid symbol is
                    // provided, even if the buffer is too small.
                    if ((uint)4 >= (uint)destination.Length)
                    {
                        goto BufferTooSmall;
                    }

                    const uint FalsValueUppercase = ('F' << 24) + ('a' << 16) + ('l' << 8) + ('s' << 0);
                    BinaryPrimitives.WriteUInt32BigEndian(destination, FalsValueUppercase);
                }
                else if (symbol == 'l')
                {
                    if ((uint)4 >= (uint)destination.Length)
                    {
                        goto BufferTooSmall;
                    }

                    const uint FalsValueLowercase = ('f' << 24) + ('a' << 16) + ('l' << 8) + ('s' << 0);
                    BinaryPrimitives.WriteUInt32BigEndian(destination, FalsValueLowercase);
                }
                else
                {
                    goto BadFormat;
                }

                destination[4] = (byte)'e';
                bytesWritten = 5;
                return true;
            }

        BufferTooSmall:
            bytesWritten = 0;
            return false;

        BadFormat:
            return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
        }
    }
}
