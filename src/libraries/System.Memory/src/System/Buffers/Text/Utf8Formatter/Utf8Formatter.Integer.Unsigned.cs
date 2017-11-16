// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
        private static bool TryFormatUInt64(ulong value, Span<byte> buffer, out int bytesWritten, StandardFormat format)
        {
            if (format.IsDefault)
            {
                // Officially, the default is "G" but "G without a precision is equivalent to "D" and so that's why we're using "D" (eliminates an unnecessary HasPrecision check)
                format = 'D';
            }

            switch (format.Symbol)
            {
                case 'G':
                case 'g':
                    if (format.HasPrecision)
                        throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported); // With a precision, 'G' can produce exponential format, even for integers.
                    return TryFormatUInt64D(value, format.Precision, buffer, out bytesWritten);

                case 'd':
                case 'D':
                    return TryFormatUInt64D(value, format.Precision, buffer, out bytesWritten);

                case 'n':
                case 'N':
                    return TryFormatUInt64N(value, format.Precision, buffer, out bytesWritten);

                case 'x':
                    return TryFormatUInt64X(value, format.Precision, true, buffer, out bytesWritten);

                case 'X':
                    return TryFormatUInt64X(value, format.Precision, false, buffer, out bytesWritten);

                default:
                    return ThrowHelper.TryFormatThrowFormatException(out bytesWritten);
            }
        }
    }
}
