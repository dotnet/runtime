// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {

        /// <summary>
        ///   Reads the next value as a UTCTime with tag UNIVERSAL 23.
        /// </summary>
        /// <param name="twoDigitYearMax">
        ///   The largest year to represent with this value.
        ///   The default value, 2049, represents the 1950-2049 range for X.509 certificates.
        /// </param>
        /// <returns>
        ///   a DateTimeOffset representing the value encoded in the UTCTime.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        /// <seealso cref="ReadUtcTime(System.Security.Cryptography.Asn1.Asn1Tag,int)"/>
        public DateTimeOffset ReadUtcTime(int twoDigitYearMax = 2049) =>
            ReadUtcTime(Asn1Tag.UtcTime, twoDigitYearMax);


        /// <summary>
        ///   Reads the next value as a UTCTime with a specified tag.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="twoDigitYearMax">
        ///   The largest year to represent with this value.
        ///   The default value, 2049, represents the 1950-2049 range for X.509 certificates.
        /// </param>
        /// <returns>
        ///   a DateTimeOffset representing the value encoded in the UTCTime.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        public DateTimeOffset ReadUtcTime(Asn1Tag expectedTag, int twoDigitYearMax = 2049)
        {
            // T-REC-X.680-201510 sec 47.3 says it is IMPLICIT VisibleString, which means
            // that BER is allowed to do complex constructed forms.

            // The full allowed formats (T-REC-X.680-201510 sec 47.3)
            // YYMMDDhhmmZ  (a, b1, c1)
            // YYMMDDhhmm+hhmm (a, b1, c2+)
            // YYMMDDhhmm-hhmm (a, b1, c2-)
            // YYMMDDhhmmssZ (a, b2, c1)
            // YYMMDDhhmmss+hhmm (a, b2, c2+)
            // YYMMDDhhmmss-hhmm (a, b2, c2-)

            // CER and DER are restricted to YYMMDDhhmmssZ
            // T-REC-X.690-201510 sec 11.8

            byte[] rented = null;
            // The longest format is 17 bytes.
            Span<byte> tmpSpace = stackalloc byte[17];

            ReadOnlySpan<byte> contents = GetOctetStringContents(
                expectedTag,
                UniversalTagNumber.UtcTime,
                out int bytesRead,
                ref rented,
                tmpSpace);

            DateTimeOffset value = ParseUtcTime(contents, twoDigitYearMax);

            if (rented != null)
            {
                Debug.Fail($"UtcTime did not fit in tmpSpace ({contents.Length} total)");
                Array.Clear(rented, 0, contents.Length);
                ArrayPool<byte>.Shared.Return(rented);
            }

            _data = _data.Slice(bytesRead);
            return value;
        }

        private DateTimeOffset ParseUtcTime(ReadOnlySpan<byte> contentOctets, int twoDigitYearMax)
        {
            // The full allowed formats (T-REC-X.680-201510 sec 47.3)
            // a) YYMMDD
            // b1) hhmm
            // b2) hhmmss
            // c1) Z
            // c2) {+|-}hhmm
            //
            // YYMMDDhhmmZ  (a, b1, c1)
            // YYMMDDhhmm+hhmm (a, b1, c2+)
            // YYMMDDhhmm-hhmm (a, b1, c2-)
            // YYMMDDhhmmssZ (a, b2, c1)
            // YYMMDDhhmmss+hhmm (a, b2, c2+)
            // YYMMDDhhmmss-hhmm (a, b2, c2-)

            const int NoSecondsZulu = 11;
            const int NoSecondsOffset = 15;
            const int HasSecondsZulu = 13;
            const int HasSecondsOffset = 17;

            // T-REC-X.690-201510 sec 11.8
            if (RuleSet == AsnEncodingRules.DER || RuleSet == AsnEncodingRules.CER)
            {
                if (contentOctets.Length != HasSecondsZulu)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            // 11, 13, 15, 17 are legal.
            // Range check + odd.
            if (contentOctets.Length < NoSecondsZulu ||
                contentOctets.Length > HasSecondsOffset ||
                (contentOctets.Length & 1) != 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlySpan<byte> contents = contentOctets;

            int year = ParseNonNegativeIntAndSlice(ref contents, 2);
            int month = ParseNonNegativeIntAndSlice(ref contents, 2);
            int day = ParseNonNegativeIntAndSlice(ref contents, 2);
            int hour = ParseNonNegativeIntAndSlice(ref contents, 2);
            int minute = ParseNonNegativeIntAndSlice(ref contents, 2);
            int second = 0;
            int offsetHour = 0;
            int offsetMinute = 0;
            bool minus = false;

            if (contentOctets.Length == HasSecondsOffset ||
                contentOctets.Length == HasSecondsZulu)
            {
                second = ParseNonNegativeIntAndSlice(ref contents, 2);
            }

            if (contentOctets.Length == NoSecondsZulu ||
                contentOctets.Length == HasSecondsZulu)
            {
                if (contents[0] != 'Z')
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }
            else
            {
                Debug.Assert(
                    contentOctets.Length == NoSecondsOffset ||
                    contentOctets.Length == HasSecondsOffset);

                if (contents[0] == '-')
                {
                    minus = true;
                }
                else if (contents[0] != '+')
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                contents = contents.Slice(1);
                offsetHour = ParseNonNegativeIntAndSlice(ref contents, 2);
                offsetMinute = ParseNonNegativeIntAndSlice(ref contents, 2);
                Debug.Assert(contents.IsEmpty);
            }

            // ISO 8601:2004 4.2.1 restricts a "minute" value to [00,59].
            // The "hour" value is effectively bound to [00,23] by the same section, but
            // is bound to [00,14] by DateTimeOffset, so no additional check is required here.
            if (offsetMinute > 59)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            TimeSpan offset = new TimeSpan(offsetHour, offsetMinute, 0);

            if (minus)
            {
                offset = -offset;
            }

            // Apply the twoDigitYearMax value.
            // Example: year=50, TDYM=2049
            //  century = 20
            //  year > 49 => century = 19
            //  scaledYear = 1900 + 50 = 1950
            //
            // Example: year=49, TDYM=2049
            //  century = 20
            //  year is not > 49 => century = 20
            //  scaledYear = 2000 + 49 = 2049
            int century = twoDigitYearMax / 100;

            if (year > twoDigitYearMax % 100)
            {
                century--;
            }

            int scaledYear = century * 100 + year;

            try
            {
                return new DateTimeOffset(scaledYear, month, day, hour, minute, second, offset);
            }
            catch (Exception e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }
    }
}
