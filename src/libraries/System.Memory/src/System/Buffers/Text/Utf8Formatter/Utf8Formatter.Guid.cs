// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !netstandard
using Internal.Runtime.CompilerServices;
#else
using System.Runtime.CompilerServices;
#endif

using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        #region Constants

        private const int GuidChars = 32;

        private const byte OpenBrace = (byte)'{';
        private const byte CloseBrace = (byte)'}';

        private const byte OpenParen = (byte)'(';
        private const byte CloseParen = (byte)')';

        private const byte Dash = (byte)'-';

        #endregion Constants

        /// <summary>
        /// Formats a Guid as a UTF8 string.
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
        ///     D (default)     nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
        ///     B               {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
        ///     P               (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
        ///     N               nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static unsafe bool TryFormat(Guid value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            char formatSymbol = format.IsDefault ? 'D' : format.Symbol;

            bool dash;
            bool bookEnds;
            switch (formatSymbol)
            {
                case 'D': // nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
                    dash = true;
                    bookEnds = false;
                    break;

                case 'B': // {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
                    dash = true;
                    bookEnds = true;
                    break;

                case 'P': // (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
                    dash = true;
                    bookEnds = true;
                    break;

                case 'N': // nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
                    dash = false;
                    bookEnds = false;
                    break;

                default:
                    return ThrowHelper.TryFormatThrowFormatException(out bytesWritten);
            }

            bytesWritten = GuidChars + (dash ? 4 : 0) + (bookEnds ? 2 : 0);
            if (buffer.Length < bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            ref byte utf8Bytes = ref MemoryMarshal.GetReference(buffer);
            byte* bytes = (byte*)&value;
            int idx = 0;

            if (bookEnds && format.Symbol == 'B')
                Unsafe.Add(ref utf8Bytes, idx++) = OpenBrace;
            else if (bookEnds && format.Symbol == (byte)'P')
                Unsafe.Add(ref utf8Bytes, idx++) = OpenParen;

            FormattingHelpers.WriteHexByte(bytes[3], ref utf8Bytes, idx);
            FormattingHelpers.WriteHexByte(bytes[2], ref utf8Bytes, idx + 2);
            FormattingHelpers.WriteHexByte(bytes[1], ref utf8Bytes, idx + 4);
            FormattingHelpers.WriteHexByte(bytes[0], ref utf8Bytes, idx + 6);
            idx += 8;

            if (dash)
                Unsafe.Add(ref utf8Bytes, idx++) = Dash;

            FormattingHelpers.WriteHexByte(bytes[5], ref utf8Bytes, idx);
            FormattingHelpers.WriteHexByte(bytes[4], ref utf8Bytes, idx + 2);
            idx += 4;

            if (dash)
                Unsafe.Add(ref utf8Bytes, idx++) = Dash;

            FormattingHelpers.WriteHexByte(bytes[7], ref utf8Bytes, idx);
            FormattingHelpers.WriteHexByte(bytes[6], ref utf8Bytes, idx + 2);
            idx += 4;

            if (dash)
                Unsafe.Add(ref utf8Bytes, idx++) = Dash;

            FormattingHelpers.WriteHexByte(bytes[8], ref utf8Bytes, idx);
            FormattingHelpers.WriteHexByte(bytes[9], ref utf8Bytes, idx + 2);
            idx += 4;

            if (dash)
                Unsafe.Add(ref utf8Bytes, idx++) = Dash;

            FormattingHelpers.WriteHexByte(bytes[10], ref utf8Bytes, idx);
            FormattingHelpers.WriteHexByte(bytes[11], ref utf8Bytes, idx + 2);
            FormattingHelpers.WriteHexByte(bytes[12], ref utf8Bytes, idx + 4);
            FormattingHelpers.WriteHexByte(bytes[13], ref utf8Bytes, idx + 6);
            FormattingHelpers.WriteHexByte(bytes[14], ref utf8Bytes, idx + 8);
            FormattingHelpers.WriteHexByte(bytes[15], ref utf8Bytes, idx + 10);
            idx += 12;

            if (bookEnds)
            {
                if (format.Symbol == 'B')
                {
                    Unsafe.Add(ref utf8Bytes, idx++) = CloseBrace;
                }
                else if (format.Symbol == 'P')
                {
                    Unsafe.Add(ref utf8Bytes, idx++) = CloseParen;
                }
            }

            return true;
        }
    }
}
