// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class X500NameEncoder
    {
        private const string OidTagPrefix = "OID.";
        private const string UseSemicolonSeparators = ";";
        private const string UseCommaSeparators = ",";
        private const string UseNewlineSeparators = "\r\n";
        private const string DefaultSeparators = ",;";

        private static readonly SearchValues<char> s_needsQuotingChars =
            SearchValues.Create(",+=\"\n<>#;"); // \r is NOT in this list, because it isn't in Windows.

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
            bool forceUtf8Encoding = (flags & X500DistinguishedNameFlags.ForceUTF8Encoding) == X500DistinguishedNameFlags.ForceUTF8Encoding;

            string dnSeparators;

            // This rank ordering is based off of testing against the Windows implementation.
            if ((flags & X500DistinguishedNameFlags.UseSemicolons) == X500DistinguishedNameFlags.UseSemicolons)
            {
                // Just semicolon.
                dnSeparators = UseSemicolonSeparators;
            }
            else if ((flags & X500DistinguishedNameFlags.UseCommas) == X500DistinguishedNameFlags.UseCommas)
            {
                // Just comma
                dnSeparators = UseCommaSeparators;
            }
            else if ((flags & X500DistinguishedNameFlags.UseNewLines) == X500DistinguishedNameFlags.UseNewLines)
            {
                // CR or LF.  Not "and".  Whichever is first was the separator, the later one is trimmed as whitespace.
                dnSeparators = UseNewlineSeparators;
            }
            else
            {
                // Comma or semicolon, but not CR or LF.
                dnSeparators = DefaultSeparators;
            }

            Debug.Assert(dnSeparators.Length != 0);

            List<byte[]> encodedSets = ParseDistinguishedName(stringForm, dnSeparators, noQuotes, forceUtf8Encoding);

            if (reverse)
            {
                encodedSets.Reverse();
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                foreach (byte[] encodedSet in encodedSets)
                {
                    writer.WriteEncodedValue(encodedSet);
                }
            }

            return writer.Encode();
        }

        private static bool NeedsQuoting(ReadOnlySpan<char> rdnValue) =>
            rdnValue.IsEmpty ||
            IsQuotableWhitespace(rdnValue[0]) ||
            IsQuotableWhitespace(rdnValue[^1]) ||
            rdnValue.IndexOfAny(s_needsQuotingChars) >= 0;

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

        private static void AppendOid(ref ValueStringBuilder decodedName, string oidValue)
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
            string dnSeparators,
            bool noQuotes,
            bool forceUtf8Encoding)
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
            ReadOnlySpan<char> chars = stringForm;

            int pos;
            int end = chars.Length;

            int tagStart = -1;
            int tagEnd = -1;
            ReadOnlySpan<char> tagOid = default;
            bool hasTagOid = false;
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
                            tagOid = ParseOid(chars[tagStart..tagEnd]);
                            hasTagOid = true;
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
                            Debug.Assert(hasTagOid);
                            Debug.Assert(valueEnd != -1);
                            Debug.Assert(valueStart != -1);

                            encodedSets.Add(ParseRdn(tagOid, chars[valueStart..valueEnd], hadEscapedQuote, forceUtf8Encoding));
                            hasTagOid = false;
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

                    encodedSets.Add(ParseRdn(tagOid, chars[valueStart..valueEnd], hadEscapedQuote, forceUtf8Encoding));
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

        private static ReadOnlySpan<char> ParseOid(ReadOnlySpan<char> str)
        {
            if (str.Length > OidTagPrefix.Length)
            {
                bool prefixed = str.StartsWith(OidTagPrefix, StringComparison.OrdinalIgnoreCase);

                if (prefixed)
                {
                    return str.Slice(OidTagPrefix.Length);
                }
            }

            return new Oid(str.ToString()).Value; // Value can be null, but permit the null-to-empty conversion.
        }

        private static byte[] ParseRdn(ReadOnlySpan<char> tagOid, ReadOnlySpan<char> chars, bool hadEscapedQuote, bool forceUtf8Encoding)
        {
            scoped ReadOnlySpan<char> data;

            if (hadEscapedQuote)
            {
                const int MaxStackAllocSize = 256;
                Span<char> destination = chars.Length > MaxStackAllocSize ?
                    new char[chars.Length] :
                    stackalloc char[MaxStackAllocSize];

                int written = ExtractValue(chars, destination);
                data = destination.Slice(0, written);
            }
            else
            {
                data = chars;
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSetOf())
            using (writer.PushSequence())
            {
                try
                {
                    writer.WriteObjectIdentifier(tagOid);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Invalid_X500Name, e);
                }

                if (tagOid.SequenceEqual(Oids.EmailAddress))
                {
                    try
                    {
                        // An email address with an invalid value will throw.
                        writer.WriteCharacterString(UniversalTagNumber.IA5String, data);
                    }
                    catch (EncoderFallbackException)
                    {
                        throw new CryptographicException(SR.Cryptography_Invalid_IA5String);
                    }
                }
                else if (forceUtf8Encoding)
                {
                    writer.WriteCharacterString(UniversalTagNumber.UTF8String, data);
                }
                else
                {
                    try
                    {
                        writer.WriteCharacterString(UniversalTagNumber.PrintableString, data);
                    }
                    catch (EncoderFallbackException)
                    {
                        writer.WriteCharacterString(UniversalTagNumber.UTF8String, data);
                    }
                }
            }

            return writer.Encode();
        }

        private static int ExtractValue(ReadOnlySpan<char> chars, Span<char> destination)
        {
            Debug.Assert(destination.Length >= chars.Length);

            bool skippedQuote = false;
            int written = 0;

            foreach (char c in chars)
            {
                if (c == '"' && !skippedQuote)
                {
                    skippedQuote = true;
                    continue;
                }

                // If we just skipped a quote, this will be one.
                // If this is a quote, we should have just skipped one.
                Debug.Assert(skippedQuote == (c == '"'));

                skippedQuote = false;
                destination[written++] = c;
            }

            return written;
        }
    }
}
