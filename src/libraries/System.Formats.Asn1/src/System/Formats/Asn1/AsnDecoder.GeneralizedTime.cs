// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads a GeneralizedTime value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 24).
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
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
        public static DateTimeOffset ReadGeneralizedTime(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            byte[]? rented = null;

            // An X.509 time is 15 characters (yyyyMMddHHmmssZ), beyond that is fractions (no limit) or
            // BER specified offset.
            const int StackBufSize = 64;
            Span<byte> tmpSpace = stackalloc byte[StackBufSize];

            ReadOnlySpan<byte> contents = GetOctetStringContents(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.GeneralizedTime,
                UniversalTagNumber.GeneralizedTime,
                out int bytesRead,
                ref rented,
                tmpSpace);

            DateTimeOffset value = ParseGeneralizedTime(ruleSet, contents);

            if (rented != null)
            {
                CryptoPool.Return(rented, contents.Length);
            }

            bytesConsumed = bytesRead;
            return value;
        }

        private static DateTimeOffset ParseGeneralizedTime(
            AsnEncodingRules ruleSet,
            ReadOnlySpan<byte> contentOctets)
        {
            // T-REC-X.680-201510 sec 46 defines a lot of formats for GeneralizedTime.
            //
            // All formats start with yyyyMMdd.
            //
            // "Local time" formats are
            //   [date]HH.fractionOfAnHourToAnArbitraryPrecision
            //   [date]HHmm.fractionOfAMinuteToAnArbitraryPrecision
            //   [date]HHmmss.fractionOfASecondToAnArbitraryPrecision
            //
            // "UTC time" formats are the local formats suffixed with 'Z'
            //
            // "UTC offset time" formats are the local formats suffixed with
            //  +HH
            //  +HHmm
            //  -HH
            //  -HHmm
            //
            // Since T-REC-X.680-201510 46.3(a)(1) and 46.3(a)(2) both specify the ISO 8601:2004
            // Basic format, we shall presume that 46.3(a)(3) also meant only the Basic format,
            // and therefore [+/-]HH:mm (with the colon) are prohibited. (based on ISO 8601:201x-DIS)

            // Since DateTimeOffset doesn't have a notion of
            // "I'm a local time, but with an unknown offset", the computer's current offset will
            // be used.

            // T-REC-X.690-201510 sec 11.7 binds CER and DER to a much smaller set of inputs:
            //  * Only the UTC/Z format can be used.
            //  * HHmmss must always be used
            //  * If fractions are present they will be separated by period, never comma.
            //  * If fractions are present the last digit mustn't be 0.

            bool strict = ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER;
            if (strict && contentOctets.Length < 15)
            {
                // yyyyMMddHHmmssZ
                throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
            }
            else if (contentOctets.Length < 10)
            {
                // yyyyMMddHH
                throw new AsnContentException();
            }

            ReadOnlySpan<byte> contents = contentOctets;

            int year = ParseNonNegativeIntAndSlice(ref contents, 4);
            int month = ParseNonNegativeIntAndSlice(ref contents, 2);
            int day = ParseNonNegativeIntAndSlice(ref contents, 2);
            int hour = ParseNonNegativeIntAndSlice(ref contents, 2);
            int? minute = null;
            int? second = null;
            ulong fraction = 0;
            ulong fractionScale = 1;
            byte lastFracDigit = 0xFF;
            TimeSpan? timeOffset = null;
            bool isZulu = false;

            const byte HmsState = 0;
            const byte FracState = 1;
            const byte SuffixState = 2;
            byte state = HmsState;

            static byte? GetNextState(byte octet)
            {
                if (octet == 'Z' || octet == '-' || octet == '+')
                {
                    return SuffixState;
                }

                if (octet == '.' || octet == ',')
                {
                    return FracState;
                }

                return null;
            }

            // This while loop could be rewritten to include the FracState and Suffix
            // processing steps.  But since there's a forward flow to the state machine
            // the loop body then needs to account for that.
            while (state == HmsState && contents.Length != 0)
            {
                byte? nextState = GetNextState(contents[0]);

                if (nextState == null)
                {
                    if (minute == null)
                    {
                        minute = ParseNonNegativeIntAndSlice(ref contents, 2);
                    }
                    else if (second == null)
                    {
                        second = ParseNonNegativeIntAndSlice(ref contents, 2);
                    }
                    else
                    {
                        throw new AsnContentException();
                    }
                }
                else
                {
                    state = nextState.Value;
                }
            }

            if (state == FracState)
            {
                Debug.Assert(!contents.IsEmpty);
                byte octet = contents[0];
                Debug.Assert(state == GetNextState(octet));

                if (octet == '.')
                {
                    // Always valid
                }
                else if (octet == ',')
                {
                    // Valid for BER, but not CER or DER.
                    // T-REC-X.690-201510 sec 11.7.4
                    if (strict)
                    {
                        throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                    }
                }
                else
                {
                    Debug.Fail($"Unhandled value '{octet:X2}' in {nameof(FracState)}");
                    throw new AsnContentException();
                }

                contents = contents.Slice(1);

                if (contents.IsEmpty)
                {
                    throw new AsnContentException();
                }

                // There are 36,000,000,000 ticks per hour, and hour is our largest scale.
                // In case the double -> Ticks conversion allows for rounding up we can allow
                // for a 12th digit.

                if (!Utf8Parser.TryParse(SliceAtMost(contents, 12), out fraction, out int fracLength) ||
                    fracLength == 0)
                {
                    throw new AsnContentException();
                }

                lastFracDigit = (byte)(fraction % 10);

                for (int i = 0; i < fracLength; i++)
                {
                    fractionScale *= 10;
                }

                contents = contents.Slice(fracLength);

                // Drain off any remaining digits.
                // The unsigned parsers will not accept + or - as a leading character, so
                // they won't eat timezone suffix.
                // But Utf8Parser.TryParse reports false on overflow, so limit it to 9 digits at a time.
                while (Utf8Parser.TryParse(SliceAtMost(contents, 9), out uint nonSemantic, out fracLength))
                {
                    contents = contents.Slice(fracLength);
                    lastFracDigit = (byte)(nonSemantic % 10);
                }

                if (contents.Length != 0)
                {
                    byte? nextState = GetNextState(contents[0]);

                    if (nextState == null)
                    {
                        throw new AsnContentException();
                    }

                    // If this produces FracState we'll finish with a non-empty contents, and still throw.
                    state = nextState.Value;
                }
            }

            if (state == SuffixState)
            {
                Debug.Assert(!contents.IsEmpty);
                byte octet = contents[0];
                Debug.Assert(state == GetNextState(octet));
                contents = contents.Slice(1);

                if (octet == 'Z')
                {
                    timeOffset = TimeSpan.Zero;
                    isZulu = true;
                }
                else
                {
                    bool isMinus;

                    if (octet == '+')
                    {
                        isMinus = false;
                    }
                    else if (octet == '-')
                    {
                        isMinus = true;
                    }
                    else
                    {
                        Debug.Fail($"Unhandled value '{octet:X2}' in {nameof(SuffixState)}");
                        throw new AsnContentException();
                    }

                    if (contents.IsEmpty)
                    {
                        throw new AsnContentException();
                    }

                    int offsetHour = ParseNonNegativeIntAndSlice(ref contents, 2);
                    int offsetMinute = 0;

                    if (contents.Length != 0)
                    {
                        offsetMinute = ParseNonNegativeIntAndSlice(ref contents, 2);
                    }

                    // ISO 8601:2004 4.2.1 restricts a "minute" value to [00,59].
                    // The "hour" value is effectively bound to [00,23] by the same section, but
                    // is bound to [00,14] by DateTimeOffset, so no additional check is required here.
                    if (offsetMinute > 59)
                    {
                        throw new AsnContentException();
                    }

                    TimeSpan tmp = new TimeSpan(offsetHour, offsetMinute, 0);

                    if (isMinus)
                    {
                        tmp = -tmp;
                    }

                    timeOffset = tmp;
                }
            }

            // Was there data after a suffix, or fracstate went re-entrant?
            if (!contents.IsEmpty)
            {
                throw new AsnContentException();
            }

            // T-REC-X.690-201510 sec 11.7
            if (strict)
            {
                if (!isZulu || !second.HasValue)
                {
                    throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                }

                if (lastFracDigit == 0)
                {
                    throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                }
            }

            double frac = (double)fraction / fractionScale;
            TimeSpan fractionSpan = TimeSpan.Zero;

            if (!minute.HasValue)
            {
                minute = 0;
                second = 0;

                if (fraction != 0)
                {
                    // No minutes means this is fractions of an hour
                    fractionSpan = new TimeSpan((long)(frac * TimeSpan.TicksPerHour));
                }
            }
            else if (!second.HasValue)
            {
                second = 0;

                if (fraction != 0)
                {
                    // No seconds means this is fractions of a minute
                    fractionSpan = new TimeSpan((long)(frac * TimeSpan.TicksPerMinute));
                }
            }
            else if (fraction != 0)
            {
                // Both minutes and seconds means fractions of a second.
                fractionSpan = new TimeSpan((long)(frac * TimeSpan.TicksPerSecond));
            }

            DateTimeOffset value;

            try
            {
                if (timeOffset == null)
                {
                    // Use the local timezone offset since there's no information in the contents.
                    // T-REC-X.680-201510 sec 46.2(a).
                    value = new DateTimeOffset(new DateTime(year, month, day, hour, minute.Value, second.Value));
                }
                else
                {
                    // T-REC-X.680-201510 sec 46.2(b) or 46.2(c).
                    value = new DateTimeOffset(year, month, day, hour, minute.Value, second.Value, timeOffset.Value);
                }

                value += fractionSpan;
                return value;
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
        ///   Reads the next value as a GeneralizedTime with a specified tag.
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 24).
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
        public DateTimeOffset ReadGeneralizedTime(Asn1Tag? expectedTag = null)
        {
            DateTimeOffset ret = AsnDecoder.ReadGeneralizedTime(_data.Span, RuleSet, out int consumed, expectedTag);
            _data = _data.Slice(consumed);
            return ret;
        }
    }
}
