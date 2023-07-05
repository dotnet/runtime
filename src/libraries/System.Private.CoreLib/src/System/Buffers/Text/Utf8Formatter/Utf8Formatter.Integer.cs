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
        /// <summary>
        /// Formats a Byte as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(byte value, Span<byte> destination, out int bytesWritten, StandardFormat format = default) =>
            TryFormat((uint)value, destination, out bytesWritten, format);

        /// <summary>
        /// Formats an SByte as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [CLSCompliant(false)]
        public static bool TryFormat(sbyte value, Span<byte> destination, out int bytesWritten, StandardFormat format = default) =>
            TryFormat(value, 0xFF, destination, out bytesWritten, format);

        /// <summary>
        /// Formats a Unt16 as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [CLSCompliant(false)]
        public static bool TryFormat(ushort value, Span<byte> destination, out int bytesWritten, StandardFormat format = default) =>
            TryFormat((uint)value, destination, out bytesWritten, format);

        /// <summary>
        /// Formats an Int16 as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(short value, Span<byte> destination, out int bytesWritten, StandardFormat format = default) =>
            TryFormat(value, 0xFFFF, destination, out bytesWritten, format);

        /// <summary>
        /// Formats a UInt32 as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static bool TryFormat(uint value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            if (format.IsDefault)
            {
                return Number.TryUInt32ToDecStr(value, destination, out bytesWritten);
            }

            switch (format.Symbol | 0x20)
            {
                case 'd':
                    return Number.TryUInt32ToDecStr(value, format.PrecisionOrZero, destination, out bytesWritten);

                case 'x':
                    return Number.TryInt32ToHexStr((int)value, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);

                case 'n':
                    return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);

                case 'g' or 'r':
                    if (format.HasPrecision)
                    {
                        ThrowGWithPrecisionNotSupported();
                    }
                    goto case 'd';

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'd';
            }
        }

        /// <summary>
        /// Formats an Int32 as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(int value, Span<byte> destination, out int bytesWritten, StandardFormat format = default) =>
            TryFormat(value, ~0, destination, out bytesWritten, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormat(int value, int hexMask, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            if (format.IsDefault)
            {
                return value >= 0 ?
                    Number.TryUInt32ToDecStr((uint)value, destination, out bytesWritten) :
                    Number.TryNegativeInt32ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);
            }

            switch (format.Symbol | 0x20)
            {
                case 'd':
                    return value >= 0 ?
                        Number.TryUInt32ToDecStr((uint)value, format.PrecisionOrZero, destination, out bytesWritten) :
                        Number.TryNegativeInt32ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);

                case 'x':
                    return Number.TryInt32ToHexStr(value & hexMask, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);

                case 'n':
                    return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);

                case 'g' or 'r':
                    if (format.HasPrecision)
                    {
                        ThrowGWithPrecisionNotSupported();
                    }
                    goto case 'd';

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'd';
            }
        }

        /// <summary>
        /// Formats a UInt64 as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static bool TryFormat(ulong value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            if (format.IsDefault)
            {
                return Number.TryUInt64ToDecStr(value, destination, out bytesWritten);
            }

            switch (format.Symbol | 0x20)
            {
                case 'd':
                    return Number.TryUInt64ToDecStr(value, format.PrecisionOrZero, destination, out bytesWritten);

                case 'x':
                    return Number.TryInt64ToHexStr((long)value, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);

                case 'n':
                    return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);

                case 'g' or 'r':
                    if (format.HasPrecision)
                    {
                        ThrowGWithPrecisionNotSupported();
                    }
                    goto case 'd';

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'd';
            }
        }

        /// <summary>
        /// Formats an Int64 as a UTF8 string.
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
        ///     G/g (default)
        ///     D/d             32767
        ///     N/n             32,767
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFormat(long value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            if (format.IsDefault)
            {
                return value >= 0 ?
                    Number.TryUInt64ToDecStr((ulong)value, destination, out bytesWritten) :
                    Number.TryNegativeInt64ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);
            }

            switch (format.Symbol | 0x20)
            {
                case 'd':
                    return value >= 0 ?
                        Number.TryUInt64ToDecStr((ulong)value, format.PrecisionOrZero, destination, out bytesWritten) :
                        Number.TryNegativeInt64ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);

                case 'x':
                    return Number.TryInt64ToHexStr(value, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);

                case 'n':
                    return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);

                case 'g' or 'r':
                    if (format.HasPrecision)
                    {
                        ThrowGWithPrecisionNotSupported();
                    }
                    goto case 'd';

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'd';
            }
        }

        private static void ThrowGWithPrecisionNotSupported() =>
            // With a precision, 'G' can produce exponential format, even for integers.
            throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported);
    }
}
