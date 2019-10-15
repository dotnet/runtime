// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write the provided <see cref="DateTimeOffset"/> as a UTCTime with tag
        ///   UNIVERSAL 23, and accepting the two-digit year as valid in context.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteUtcTime(Asn1Tag,DateTimeOffset)"/>
        /// <seealso cref="WriteUtcTime(DateTimeOffset,int)"/>
        public void WriteUtcTime(DateTimeOffset value)
        {
            WriteUtcTimeCore(Asn1Tag.UtcTime, value);
        }

        /// <summary>
        ///   Write the provided <see cref="DateTimeOffset"/> as a UTCTime with a specified tag,
        ///   accepting the two-digit year as valid in context.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteUtcTime(Asn1Tag,DateTimeOffset,int)"/>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        public void WriteUtcTime(Asn1Tag tag, DateTimeOffset value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.UtcTime);

            // Clear the constructed flag, if present.
            WriteUtcTimeCore(tag.AsPrimitive(), value);
        }

        /// <summary>
        ///   Write the provided <see cref="DateTimeOffset"/> as a UTCTime with tag
        ///   UNIVERSAL 23, provided the year is in the allowed range.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="twoDigitYearMax">
        ///   The maximum valid year for <paramref name="value"/>, after conversion to UTC.
        ///   For the X.509 Time.utcTime range of 1950-2049, pass <c>2049</c>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/>.<see cref="DateTimeOffset.Year"/> (after conversion to UTC)
        ///   is not in the range
        ///   (<paramref name="twoDigitYearMax"/> - 100, <paramref name="twoDigitYearMax"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteUtcTime(Asn1Tag,DateTimeOffset,int)"/>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        public void WriteUtcTime(DateTimeOffset value, int twoDigitYearMax)
        {
            // Defer to the longer override for twoDigitYearMax validity.
            WriteUtcTime(Asn1Tag.UtcTime, value, twoDigitYearMax);
        }

        /// <summary>
        ///   Write the provided <see cref="DateTimeOffset"/> as a UTCTime with a specified tag,
        ///   provided the year is in the allowed range.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="twoDigitYearMax">
        ///   The maximum valid year for <paramref name="value"/>, after conversion to UTC.
        ///   For the X.509 Time.utcTime range of 1950-2049, pass <c>2049</c>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/>.<see cref="DateTimeOffset.Year"/> (after conversion to UTC)
        ///   is not in the range
        ///   (<paramref name="twoDigitYearMax"/> - 100, <paramref name="twoDigitYearMax"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteUtcTime(System.Security.Cryptography.Asn1.Asn1Tag,System.DateTimeOffset,int)"/>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        public void WriteUtcTime(Asn1Tag tag, DateTimeOffset value, int twoDigitYearMax)
        {
            CheckUniversalTag(tag, UniversalTagNumber.UtcTime);

            value = value.ToUniversalTime();

            if (value.Year > twoDigitYearMax || value.Year <= twoDigitYearMax - 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            WriteUtcTimeCore(tag.AsPrimitive(), value);
        }

        // T-REC-X.680-201508 sec 47
        // T-REC-X.690-201508 sec 11.8
        private void WriteUtcTimeCore(Asn1Tag tag, DateTimeOffset value)
        {
            // Because UtcTime is IMPLICIT VisibleString it technically can have
            // a constructed form.
            // DER says character strings must be primitive.
            // CER says character strings <= 1000 encoded bytes must be primitive.
            // So we'll just make BER be primitive, too.
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);

            // BER allows for omitting the seconds, but that's not an option we need to expose.
            // BER allows for non-UTC values, but that's also not an option we need to expose.
            // So the format is always yyMMddHHmmssZ (13)
            const int UtcTimeValueLength = 13;
            WriteLength(UtcTimeValueLength);

            DateTimeOffset normalized = value.ToUniversalTime();

            int year = normalized.Year;
            int month = normalized.Month;
            int day = normalized.Day;
            int hour = normalized.Hour;
            int minute = normalized.Minute;
            int second = normalized.Second;

            Span<byte> baseSpan = _buffer.AsSpan(_offset);
            StandardFormat format = new StandardFormat('D', 2);

            if (!Utf8Formatter.TryFormat(year % 100, baseSpan.Slice(0, 2), out _, format) ||
                !Utf8Formatter.TryFormat(month, baseSpan.Slice(2, 2), out _, format) ||
                !Utf8Formatter.TryFormat(day, baseSpan.Slice(4, 2), out _, format) ||
                !Utf8Formatter.TryFormat(hour, baseSpan.Slice(6, 2), out _, format) ||
                !Utf8Formatter.TryFormat(minute, baseSpan.Slice(8, 2), out _, format) ||
                !Utf8Formatter.TryFormat(second, baseSpan.Slice(10, 2), out _, format))
            {
                Debug.Fail($"Utf8Formatter.TryFormat failed to build components of {normalized:O}");
                throw new CryptographicException();
            }

            _buffer[_offset + 12] = (byte)'Z';

            _offset += UtcTimeValueLength;
        }
    }
}
