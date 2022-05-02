// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        //
        // Common worker for all signed integer TryFormat overloads
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatInt64(long value, ulong mask, Span<byte> destination, out int bytesWritten, StandardFormat format)
        {
            if (format.IsDefault)
            {
                return TryFormatInt64Default(value, destination, out bytesWritten);
            }

            switch (format.Symbol)
            {
                case 'G':
                case 'g':
                case 'R':
                case 'r': // treat 'r' (which historically was only for floating-point) as being equivalent to 'g', rather than throwing
                    if (format.HasPrecision)
                    {
                        throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported); // With a precision, 'G' can produce exponential format, even for integers.
                    }
                    return TryFormatInt64D(value, format.Precision, destination, out bytesWritten);

                case 'd':
                case 'D':
                    return TryFormatInt64D(value, format.Precision, destination, out bytesWritten);

                case 'n':
                case 'N':
                    return TryFormatInt64N(value, format.Precision, destination, out bytesWritten);

                case 'x':
                    return TryFormatUInt64X((ulong)value & mask, format.Precision, true, destination, out bytesWritten);

                case 'X':
                    return TryFormatUInt64X((ulong)value & mask, format.Precision, false, destination, out bytesWritten);

                default:
                    return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
            }
        }
    }
}
