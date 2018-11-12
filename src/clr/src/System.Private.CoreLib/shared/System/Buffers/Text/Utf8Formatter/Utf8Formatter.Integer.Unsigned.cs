// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        //
        // Common worker for all unsigned integer TryFormat overloads
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatUInt64(ulong value, Span<byte> destination, out int bytesWritten, StandardFormat format)
        {
            if (format.IsDefault)
            {
                return TryFormatUInt64Default(value, destination, out bytesWritten);
            }

            switch (format.Symbol)
            {
                case 'G':
                case 'g':
                    if (format.HasPrecision)
                        throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported); // With a precision, 'G' can produce exponential format, even for integers.
                    return TryFormatUInt64D(value, format.Precision, destination, insertNegationSign: false, out bytesWritten);

                case 'd':
                case 'D':
                    return TryFormatUInt64D(value, format.Precision, destination, insertNegationSign: false, out bytesWritten);

                case 'n':
                case 'N':
                    return TryFormatUInt64N(value, format.Precision, destination, insertNegationSign: false, out bytesWritten);

                case 'x':
                    return TryFormatUInt64X(value, format.Precision, true /* useLower */, destination, out bytesWritten);

                case 'X':
                    return TryFormatUInt64X(value, format.Precision, false /* useLower */, destination, out bytesWritten);

                default:
                    return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
            }
        }
    }
}
