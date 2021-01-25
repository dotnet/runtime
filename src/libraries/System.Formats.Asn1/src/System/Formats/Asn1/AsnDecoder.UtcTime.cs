// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads a UtcTime value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="twoDigitYearMax">
        ///   The largest year to represent with this value.
        ///   The default value, 2049, represents the 1950-2049 range for X.509 certificates.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 24).
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        ///
        ///   -or-
        ///
        ///   <paramref name="twoDigitYearMax"/> is not in the range [99, 9999].
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        public static DateTimeOffset ReadUtcTime(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            int twoDigitYearMax = 2049,
            Asn1Tag? expectedTag = null)
        {
            if (twoDigitYearMax < 1 || twoDigitYearMax > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(twoDigitYearMax));
            }

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

            // The longest format is 17 bytes.
            Span<byte> tmpSpace = stackalloc byte[17];
            byte[]? rented = null;

            ReadOnlySpan<byte> contents = GetOctetStringContents(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.UtcTime,
                UniversalTagNumber.UtcTime,
                out int bytesRead,
                ref rented,
                tmpSpace);

            DateTimeOffset value = ParseUtcTime(contents, ruleSet, twoDigitYearMax);

            if (rented != null)
            {
                Debug.Fail($"UtcTime did not fit in tmpSpace ({contents.Length} total)");
                CryptoPool.Return(rented, contents.Length);
            }

            bytesConsumed = bytesRead;
            return value;
        }

        private static DateTimeOffset ParseUtcTime(
            ReadOnlySpan<byte> contentOctets,
            AsnEncodingRules ruleSet,
            int twoDigitYearMax)
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
            if (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER)
            {
                if (contentOctets.Length != HasSecondsZulu)
                {
                    throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                }
            }

            // 11, 13, 15, 17 are legal.
            // Range check + odd.
            if (contentOctets.Length < NoSecondsZulu ||
                contentOctets.Length > HasSecondsOffset ||
                (contentOctets.Length & 1) != 1)
            {
                throw new AsnContentException();
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
                    throw new AsnContentException();
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
                    throw new AsnContentException();
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
                throw new AsnContentException();
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
                throw new AsnContentException(SR.ContentException_DefaultMessage, e);
            }
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a UTCTime with a specified tag using the
        ///   <see cref="AsnReaderOptions.UtcTimeTwoDigitYearMax"/> value from options passed to
        ///   the constructor (with a default of 2049).
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 23).
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="ReadUtcTime(int,Nullable{Asn1Tag})"/>
        public DateTimeOffset ReadUtcTime(Asn1Tag? expectedTag = null)
        {
            DateTimeOffset ret = AsnDecoder.ReadUtcTime(
                _data.Span,
                RuleSet,
                out int consumed,
                _options.UtcTimeTwoDigitYearMax,
                expectedTag);

            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Reads the next value as a UTCTime with a specified tag.
        /// </summary>
        /// <param name="twoDigitYearMax">
        ///   The largest year to represent with this value.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 23).
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="ReadUtcTime(Nullable{Asn1Tag})"/>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        public DateTimeOffset ReadUtcTime(int twoDigitYearMax, Asn1Tag? expectedTag = null)
        {
            DateTimeOffset ret =
                AsnDecoder.ReadUtcTime(_data.Span, RuleSet, out int consumed, twoDigitYearMax, expectedTag);

            _data = _data.Slice(consumed);
            return ret;
        }
    }
}
