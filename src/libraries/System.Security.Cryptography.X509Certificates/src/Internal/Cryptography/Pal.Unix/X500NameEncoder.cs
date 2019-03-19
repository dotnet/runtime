// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static partial class X500NameEncoder
    {
        private const string OidTagPrefix = "OID.";

        private static readonly char[] s_quoteNeedingChars =
        {
            ',',
            '+',
            '=',
            '\"',
            '\n',
            // \r is NOT in this list, because it isn't in Windows.
            '<',
            '>',
            '#',
            ';',
        };

        private static readonly List<char> s_useSemicolonSeparators = new List<char>(1) { ';' };
        private static readonly List<char> s_useCommaSeparators = new List<char>(1) { ',' };
        private static readonly List<char> s_useNewlineSeparators = new List<char>(2) { '\r', '\n' };
        private static readonly List<char> s_defaultSeparators = new List<char>(2) { ',', ';' };

        internal static string X500DistinguishedNameDecode(
            byte[] encodedName,
            bool printOid,
            X500DistinguishedNameFlags flags,
            bool addTrailingDelimiter = false)
        {
            bool reverse = (flags & X500DistinguishedNameFlags.Reversed) == X500DistinguishedNameFlags.Reversed;
            bool quoteIfNeeded = (flags & X500DistinguishedNameFlags.DoNotUseQuotes) != X500DistinguishedNameFlags.DoNotUseQuotes;
            bool useMultiSeparator = (flags & X500DistinguishedNameFlags.DoNotUsePlusSign) != X500DistinguishedNameFlags.DoNotUsePlusSign;
            string dnSeparator;

            if ((flags & X500DistinguishedNameFlags.UseSemicolons) == X500DistinguishedNameFlags.UseSemicolons)
            {
                dnSeparator = "; ";
            }
            // Explicit UseCommas has preference over explicit UseNewLines.
            else if ((flags & (X500DistinguishedNameFlags.UseNewLines | X500DistinguishedNameFlags.UseCommas)) == X500DistinguishedNameFlags.UseNewLines)
            {
                dnSeparator = Environment.NewLine;
            }
            else
            {
                // This is matching Windows (native) behavior, UseCommas does not need to be asserted,
                // it is just what happens if neither UseSemicolons nor UseNewLines is specified.
                dnSeparator = ", ";
            }

            string multiValueSparator = useMultiSeparator ? " + " : " ";

            try
            {
                return X500DistinguishedNameDecode(
                    encodedName,
                    printOid,
                    reverse,
                    quoteIfNeeded,
                    dnSeparator,
                    multiValueSparator,
                    addTrailingDelimiter);
            }
            catch (CryptographicException)
            {
                // Windows compat:
                return "";
            }
        }

        internal static byte[] X500DistinguishedNameEncode(
            string stringForm,
            X500DistinguishedNameFlags flags)
        {
            bool reverse = (flags & X500DistinguishedNameFlags.Reversed) == X500DistinguishedNameFlags.Reversed;
            bool noQuotes = (flags & X500DistinguishedNameFlags.DoNotUseQuotes) == X500DistinguishedNameFlags.DoNotUseQuotes;

            List<char> dnSeparators;

            // This rank ordering is based off of testing against the Windows implementation.
            if ((flags & X500DistinguishedNameFlags.UseSemicolons) == X500DistinguishedNameFlags.UseSemicolons)
            {
                // Just semicolon.
                dnSeparators = s_useSemicolonSeparators;
            }
            else if ((flags & X500DistinguishedNameFlags.UseCommas) == X500DistinguishedNameFlags.UseCommas)
            {
                // Just comma
                dnSeparators = s_useCommaSeparators;
            }
            else if ((flags & X500DistinguishedNameFlags.UseNewLines) == X500DistinguishedNameFlags.UseNewLines)
            {
                // CR or LF.  Not "and".  Whichever is first was the separator, the later one is trimmed as whitespace.
                dnSeparators = s_useNewlineSeparators;
            }
            else
            {
                // Comma or semicolon, but not CR or LF.
                dnSeparators = s_defaultSeparators;
            }

            Debug.Assert(dnSeparators.Count != 0);

            List<byte[]> encodedSets = ParseDistinguishedName(stringForm, dnSeparators, noQuotes);

            if (reverse)
            {
                encodedSets.Reverse();
            }

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();
                foreach (byte[] encodedSet in encodedSets)
                {
                    writer.WriteEncodedValue(encodedSet);
                }
                writer.PopSequence();
                return writer.Encode();
            }
        }

        private static bool NeedsQuoting(string rdnValue)
        {
            if (string.IsNullOrEmpty(rdnValue))
            {
                return true;
            }

            if (IsQuotableWhitespace(rdnValue[0]) ||
                IsQuotableWhitespace(rdnValue[rdnValue.Length - 1]))
            {
                return true;
            }

            int index = rdnValue.IndexOfAny(s_quoteNeedingChars);

            return index != -1;
        }

        private static bool IsQuotableWhitespace(char c)
        {
            // There's a whole lot of Unicode whitespace that isn't covered here; but this
            // matches what Windows deems quote-worthy.
            //
            // 0x20: Space
            // 0x09: Character Tabulation (tab)
            // 0x0A: Line Feed
            // 0x0B: Line Tabulation (vertical tab)
            // 0x0C: Form Feed
            // 0x0D: Carriage Return
            return (c == ' ' || (c >= 0x09 && c <= 0x0D));
        }

        private static void AppendOid(StringBuilder decodedName, string oidValue)
        {
            Oid oid = new Oid(oidValue);

            if (StringComparer.Ordinal.Equals(oid.FriendlyName, oidValue) ||
                string.IsNullOrEmpty(oid.FriendlyName))
            {
                decodedName.Append(OidTagPrefix);
                decodedName.Append(oid.Value);
            }
            else
            {
                decodedName.Append(oid.FriendlyName);
            }

            decodedName.Append('=');
        }

        private enum ParseState
        {
            Invalid,
            SeekTag,
            SeekTagEnd,
            SeekEquals,
            SeekValueStart,
            SeekValueEnd,
            SeekEndQuote,
            MaybeEndQuote,
            SeekComma,
        }

        private static List<byte[]> ParseDistinguishedName(
            string stringForm,
            List<char> dnSeparators,
            bool noQuotes)
        {
            // 16 is way more RDNs than we should ever need. A fairly standard set of values is
            // { E, CN, O, OU, L, S, C } = 7;
            // The EV values add in
            // {
            //   STREET, PostalCode, SERIALNUMBER, 2.5.4.15,
            //   1.3.6.1.4.1.311.60.2.1.2, 1.3.6.1.4.1.311.60.2.1.3
            // } = 6
            //
            // 7 + 6 = 13, round up to the nearest power-of-two.
            const int InitalRdnSize = 16;
            List<byte[]> encodedSets = new List<byte[]>(InitalRdnSize);
            char[] chars = stringForm.ToCharArray();

            int pos;
            int end = chars.Length;

            int tagStart = -1;
            int tagEnd = -1;
            Oid tagOid = null;
            int valueStart = -1;
            int valueEnd = -1;
            bool hadEscapedQuote = false;

            const char KeyValueSeparator = '=';
            const char QuotedValueChar = '"';

            ParseState state = ParseState.SeekTag;

            for (pos = 0; pos < end; pos++)
            {
                char c = chars[pos];

                switch (state)
                {
                    case ParseState.SeekTag:
                        if (char.IsWhiteSpace(c))
                        {
                            continue;
                        }

                        if (char.IsControl(c))
                        {
                            state = ParseState.Invalid;
                            break;
                        }

                        // The first character in the tag start.
                        // We know that there's at least one valid
                        // character, so make end be start+1.
                        //
                        // Single letter values with no whitespace padding them
                        // (e.g. E=) would otherwise be ambiguous with length.
                        // (SeekEquals can't set the tagEnd value because it
                        // doesn't know if it was preceded by whitespace)

                        // Note that we make no check here for the dnSeparator(s).
                        // Two separators in a row is invalid (except for UseNewlines,
                        // and they are only allowed because they are whitespace).
                        //
                        // But the throw for an invalid value will come from when the
                        // OID fails to encode.
                        tagStart = pos;
                        tagEnd = pos + 1;
                        state = ParseState.SeekTagEnd;
                        break;

                    case ParseState.SeekTagEnd:
                        if (c == KeyValueSeparator)
                        {
                            goto case ParseState.SeekEquals;
                        }

                        if (char.IsWhiteSpace(c))
                        {
                            // Tag values aren't permitted whitespace, but there
                            // can be whitespace between the tag and the separator.
                            state = ParseState.SeekEquals;
                            break;
                        }

                        if (char.IsControl(c))
                        {
                            state = ParseState.Invalid;
                            break;
                        }

                        // We found another character in the tag, so move the
                        // end (non-inclusive) to the next character.
                        tagEnd = pos + 1;
                        break;

                    case ParseState.SeekEquals:
                        if (c == KeyValueSeparator)
                        {
                            Debug.Assert(tagStart >= 0);
                            tagOid = ParseOid(stringForm, tagStart, tagEnd);
                            tagStart = -1;

                            state = ParseState.SeekValueStart;
                            break;
                        }

                        if (!char.IsWhiteSpace(c))
                        {
                            state = ParseState.Invalid;
                            break;
                        }

                        break;

                    case ParseState.SeekValueStart:
                        if (char.IsWhiteSpace(c))
                        {
                            continue;
                        }

                        // If the first non-whitespace character is a quote,
                        // this is a quoted string.  Unless the flags say to
                        // not interpret quoted strings.
                        if (c == QuotedValueChar && !noQuotes)
                        {
                            state = ParseState.SeekEndQuote;
                            valueStart = pos + 1;
                            break;
                        }

                        // It's possible to just write "CN=,O=". So we might
                        // run into the RDN separator here.
                        if (dnSeparators.Contains(c))
                        {
                            valueStart = pos;
                            valueEnd = pos;
                            goto case ParseState.SeekComma;
                        }

                        state = ParseState.SeekValueEnd;
                        valueStart = pos;
                        valueEnd = pos + 1;
                        break;

                    case ParseState.SeekEndQuote:
                        // The only escape sequence in DN parsing is that a quoted
                        // value can embed quotes via "", the same as a C# verbatim
                        // string.  So, if we see a quote while looking for a close
                        // quote we need to remember that this might have been the
                        // end, but be open to the possibility that there's another
                        // quote coming.
                        if (c == QuotedValueChar)
                        {
                            state = ParseState.MaybeEndQuote;
                            valueEnd = pos;
                            break;
                        }

                        // Everything else is okay.
                        break;

                    case ParseState.MaybeEndQuote:
                        if (c == QuotedValueChar)
                        {
                            state = ParseState.SeekEndQuote;
                            hadEscapedQuote = true;
                            valueEnd = -1;
                            break;
                        }

                        // If the character wasn't another quote:
                        //   dnSeparator: process value, state transition to SeekTag
                        //   whitespace: state transition to SeekComma
                        //   anything else: invalid.
                        // since that's the same table as SeekComma, just change state
                        // and go there.
                        state = ParseState.SeekComma;
                        goto case ParseState.SeekComma;

                    case ParseState.SeekValueEnd:
                        // Every time we see a non-whitespace character we need to mark it
                        if (dnSeparators.Contains(c))
                        {
                            goto case ParseState.SeekComma;
                        }

                        if (char.IsWhiteSpace(c))
                        {
                            continue;
                        }

                        // Including control characters.
                        valueEnd = pos + 1;

                        break;

                    case ParseState.SeekComma:
                        if (dnSeparators.Contains(c))
                        {
                            Debug.Assert(tagOid != null);
                            Debug.Assert(valueEnd != -1);
                            Debug.Assert(valueStart != -1);

                            encodedSets.Add(ParseRdn(tagOid, chars, valueStart, valueEnd, hadEscapedQuote));
                            tagOid = null;
                            valueStart = -1;
                            valueEnd = -1;
                            state = ParseState.SeekTag;
                            break;
                        }

                        if (!char.IsWhiteSpace(c))
                        {
                            state = ParseState.Invalid;
                            break;
                        }

                        break;

                    default:
                        Debug.Fail($"Invalid parser state. Position {pos}, State {state}, Character {c}, String \"{stringForm}\"");
                        throw new CryptographicException(SR.Cryptography_Invalid_X500Name);
                }

                if (state == ParseState.Invalid)
                {
                    break;
                }
            }

            // Okay, so we've run out of input.  There are a couple of valid states we can be in.
            // * 'CN='
            //   state: SeekValueStart.  Neither valueStart nor valueEnd has a value yet.
            // * 'CN=a'
            //   state: SeekValueEnd.  valueEnd was set to pos(a) + 1, close it off.
            // * 'CN=a '
            //   state: SeekValueEnd.  valueEnd is marking at the start of the whitespace.
            // * 'CN="a"'
            //   state: MaybeEndQuote.  valueEnd is marking at the end-quote.
            // * 'CN="a" '
            //   state: SeekComma.  This is the same as MaybeEndQuote.
            // * 'CN=a,'
            //   state: SeekTag.  There's nothing to do here.
            // * ''
            //   state: SeekTag.  There's nothing to do here.
            //
            // And, of course, invalid ones.
            // * 'CN="'
            //   state: SeekEndQuote.  Throw.
            // * 'CN':
            //   state: SeekEndTag.  Throw.
            switch (state)
            {
                // The last semantic character parsed was =.
                case ParseState.SeekValueStart:
                    valueStart = chars.Length;
                    valueEnd = valueStart;
                    goto case ParseState.SeekComma;

                // If we were in an unquoted value and just ran out of text
                case ParseState.SeekValueEnd:
                    Debug.Assert(!hadEscapedQuote);
                    goto case ParseState.SeekComma;

                // If the last character was a close quote, or it was a close quote
                // then some whitespace.
                case ParseState.MaybeEndQuote:
                case ParseState.SeekComma:
                    Debug.Assert(tagOid != null);
                    Debug.Assert(valueStart != -1);
                    Debug.Assert(valueEnd != -1);

                    encodedSets.Add(ParseRdn(tagOid, chars, valueStart, valueEnd, hadEscapedQuote));
                    break;

                // If the entire string was empty, or ended in a dnSeparator.
                case ParseState.SeekTag:
                    break;

                default:
                    // While this is an error, it should be due to bad input, so no Debug.Fail.
                    throw new CryptographicException(SR.Cryptography_Invalid_X500Name);
            }

            return encodedSets;
        }

        private static Oid ParseOid(string stringForm, int tagStart, int tagEnd)
        {
            int length = tagEnd - tagStart;

            if (length > OidTagPrefix.Length)
            {
                // Since we only care if the match starts exactly at tagStart, tell IndexOf
                // that we're only examining OidTagPrefix.Length characters.  So it won't do
                // more than one linear equality check.
                int prefixIndex = stringForm.IndexOf(
                    OidTagPrefix,
                    tagStart,
                    OidTagPrefix.Length,
                    StringComparison.OrdinalIgnoreCase);

                if (prefixIndex == tagStart)
                {
                    return new Oid(stringForm.Substring(tagStart + OidTagPrefix.Length, length - OidTagPrefix.Length));
                }
            }

            return new Oid(stringForm.Substring(tagStart, length));
        }

        private static byte[] ParseRdn(Oid tagOid, char[] chars, int valueStart, int valueEnd, bool hadEscapedQuote)
        {
            ReadOnlySpan<char> charValue;

            if (hadEscapedQuote)
            {
                charValue = ExtractValue(chars, valueStart, valueEnd);
            }
            else
            {
                charValue = new ReadOnlySpan<char>(chars, valueStart, valueEnd - valueStart);
            }

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSetOf();
                writer.PushSequence();

                try
                {
                    writer.WriteObjectIdentifier(tagOid);
                }
                catch (CryptographicException e)
                {
                    throw new CryptographicException(SR.Cryptography_Invalid_X500Name, e);
                }

                if (tagOid.Value == Oids.EmailAddress)
                {
                    try
                    {
                        // An email address with an invalid value will throw.
                        writer.WriteCharacterString(UniversalTagNumber.IA5String, charValue);
                    }
                    catch (EncoderFallbackException)
                    {
                        throw new CryptographicException(SR.Cryptography_Invalid_IA5String);
                    }
                }
                else if (IsValidPrintableString(charValue))
                {
                    writer.WriteCharacterString(UniversalTagNumber.PrintableString, charValue);
                }
                else
                {
                    writer.WriteCharacterString(UniversalTagNumber.UTF8String, charValue);
                }
                
                writer.PopSequence();
                writer.PopSetOf();
                return writer.Encode();
            }
        }

        private static bool IsValidPrintableString(ReadOnlySpan<char> value)
        {
            try
            {
                Encoding encoding = AsnCharacterStringEncodings.GetEncoding(UniversalTagNumber.PrintableString);
                // Throws on invalid characters.
                encoding.GetByteCount(value);
                return true;
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }

        private static char[] ExtractValue(char[] chars, int valueStart, int valueEnd)
        {
            // The string is guaranteed to be between ((valueEnd - valueStart) / 2) (all quotes) and
            // (valueEnd - valueStart - 1) (one escaped quote)
            List<char> builder = new List<char>(valueEnd - valueStart - 1);

            bool skippedQuote = false;

            for (int i = valueStart; i < valueEnd; i++)
            {
                char c = chars[i];

                if (c == '"' && !skippedQuote)
                {
                    skippedQuote = true;
                    continue;
                }

                // If we just skipped a quote, this will be one.
                // If this is a quote, we should have just skipped one.
                Debug.Assert(skippedQuote == (c == '"'));

                skippedQuote = false;
                builder.Add(c);
            }

            return builder.ToArray();
        }
    }
}
