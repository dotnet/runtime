// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    // The class designed as to keep working set of Uri class as minimal.
    // The idea is to stay with static helper methods and strings
    internal static class DomainNameHelper
    {
        // Regular ascii dot '.'
        // IDEOGRAPHIC FULL STOP '\u3002'
        // FULLWIDTH FULL STOP '\uFF0E'
        // HALFWIDTH IDEOGRAPHIC FULL STOP '\uFF61'
        // Using SearchValues isn't beneficial here as it would defer to IndexOfAny(char, char, char, char) anyway
        private const string IriDotCharacters = ".\u3002\uFF0E\uFF61";

        // The Unicode specification allows certain code points to be normalized not to
        // punycode, but to ASCII representations that retain the same meaning. For example,
        // the codepoint U+00BC "Vulgar Fraction One Quarter" is normalized to '1/4' rather
        // than being punycoded.
        //
        // This means that a host containing Unicode characters can be normalized to contain
        // URI reserved characters, changing the meaning of a URI only when certain properties
        // such as IdnHost are accessed. To be safe, disallow control characters in normalized hosts.
        private static readonly SearchValues<char> s_unsafeForNormalizedHostChars =
            SearchValues.Create(@"\/?@#:[]");

        // Takes into account the additional legal domain name characters '-' and '_'
        // Note that '_' char is formally invalid but is historically in use, especially on corpnets
        private static readonly SearchValues<char> s_validChars =
            SearchValues.Create("-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz.");

        // For IRI, we're accepting anything non-ascii, so invert the condition to just check for invalid ascii characters
        private static readonly SearchValues<char> s_iriInvalidAsciiChars = SearchValues.Create(
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A\u000B\u000C\u000D\u000E\u000F" +
            "\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F" +
            " !\"#$%&'()*+,/:;<=>?@[\\]^`{|}~\u007F");

        private static readonly SearchValues<char> s_asciiLetterUpperOrColonChars =
            SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ:");

        private static readonly IdnMapping s_idnMapping = new IdnMapping();

        private const string Localhost = "localhost";
        private const string Loopback = "loopback";

        internal static string ParseCanonicalName(string str, int start, int end, ref bool loopback)
        {
            // Do a quick search for the colon or uppercase letters
            int index = str.AsSpan(start, end - start).LastIndexOfAny(s_asciiLetterUpperOrColonChars);
            if (index >= 0)
            {
                Debug.Assert(!str.AsSpan(start, index).Contains(':'),
                    "A colon should appear at most once, and must never be followed by letters.");

                if (str[start + index] == ':')
                {
                    // Shrink the slice to only include chars before the colon
                    end = start + index;

                    // Look for uppercase letters again.
                    // The index value doesn't matter anymore (nor does the search direction), just whether we've found anything
                    index = str.AsSpan(start, index).IndexOfAnyInRange('A', 'Z');
                }
            }

            Debug.Assert(index == -1 || char.IsAsciiLetterUpper(str[start + index]));

            if (index >= 0)
            {
                // We saw uppercase letters. Avoid allocating both the substring and the lower-cased variant.
                return string.Create(end - start, (str, start), static (buffer, state) =>
                {
                    int newLength = state.str.AsSpan(state.start, buffer.Length).ToLowerInvariant(buffer);
                    Debug.Assert(newLength == buffer.Length);
                });
            }

            string res = str.Substring(start, end - start);

            if (res is Localhost or Loopback)
            {
                loopback = true;
                return Localhost;
            }

            return res;
        }

        public static bool IsValid(ReadOnlySpan<char> hostname, bool iri, bool notImplicitFile, out int length)
        {
            int invalidCharOrDelimiterIndex = iri
                ? hostname.IndexOfAny(s_iriInvalidAsciiChars)
                : hostname.IndexOfAnyExcept(s_validChars);

            if (invalidCharOrDelimiterIndex >= 0)
            {
                char c = hostname[invalidCharOrDelimiterIndex];

                if (c is '/' or '\\' || (notImplicitFile && (c is ':' or '?' or '#')))
                {
                    hostname = hostname.Slice(0, invalidCharOrDelimiterIndex);
                }
                else
                {
                    length = 0;
                    return false;
                }
            }

            length = hostname.Length;

            if (length == 0)
            {
                return false;
            }

            //  Determines whether a string is a valid domain name label. In keeping
            //  with RFC 1123, section 2.1, the requirement that the first character
            //  of a label be alphabetic is dropped. Therefore, Domain names are
            //  formed as:
            //
            //      <label> -> <alphanum> [<alphanum> | <hyphen> | <underscore>] * 62

            // We already verified the content, now verify the lengths of individual labels
            while (true)
            {
                char firstChar = hostname[0];
                if ((!iri || firstChar < 0xA0) && !char.IsAsciiLetterOrDigit(firstChar))
                {
                    return false;
                }

                int dotIndex = iri
                    ? hostname.IndexOfAny(IriDotCharacters)
                    : hostname.IndexOf('.');

                int labelLength = dotIndex < 0 ? hostname.Length : dotIndex;

                if (iri)
                {
                    ReadOnlySpan<char> label = hostname.Slice(0, labelLength);
                    if (!Ascii.IsValid(label))
                    {
                        // s_iriInvalidAsciiChars confirmed everything in [0, 7F] range.
                        // Chars in [80, 9F] range are also invalid, check for them now.
                        if (hostname.IndexOfAnyInRange('\u0080', '\u009F') >= 0)
                        {
                            return false;
                        }

                        // Account for the ACE prefix ("xn--")
                        labelLength += 4;

                        foreach (char c in label)
                        {
                            if (c > 0xFF)
                            {
                                // counts for two octets
                                labelLength++;
                            }
                        }
                    }
                }

                if (!IriHelper.IsInInclusiveRange((uint)labelLength, 1, 63))
                {
                    return false;
                }

                if (dotIndex < 0)
                {
                    // We validated the last label
                    return true;
                }

                hostname = hostname.Slice(dotIndex + 1);

                if (hostname.IsEmpty)
                {
                    // Hostname ended with a dot
                    return true;
                }
            }
        }

        /// <summary>Converts a host name into its idn equivalent.</summary>
        public static string IdnEquivalent(string hostname)
        {
            // check if only ascii chars
            // special case since idnmapping will not lowercase if only ascii present
            if (Ascii.IsValid(hostname))
            {
                // just lowercase for ascii
                return hostname.ToLowerInvariant();
            }

            string bidiStrippedHost = UriHelper.StripBidiControlCharacters(hostname, hostname);

            try
            {
                string asciiForm = s_idnMapping.GetAscii(bidiStrippedHost);
                if (asciiForm.AsSpan().IndexOfAny(s_unsafeForNormalizedHostChars) >= 0)
                {
                    throw new UriFormatException(SR.net_uri_BadUnicodeHostForIdn);
                }
                return asciiForm;
            }
            catch (ArgumentException)
            {
                throw new UriFormatException(SR.net_uri_BadUnicodeHostForIdn);
            }
        }

        public static bool TryGetUnicodeEquivalent(string hostname, ref ValueStringBuilder dest)
        {
            Debug.Assert(ReferenceEquals(hostname, UriHelper.StripBidiControlCharacters(hostname, hostname)));

            // We run a loop where for every label
            // a) if label is ascii and no ace then we lowercase it
            // b) if label is ascii and ace and not valid idn then just lowercase it
            // c) if label is ascii and ace and is valid idn then get its unicode eqvl
            // d) if label is unicode then clean it by running it through idnmapping
            for (int i = 0; i < hostname.Length; i++)
            {
                if (i != 0)
                {
                    dest.Append('.');
                }

                ReadOnlySpan<char> label = hostname.AsSpan(i);

                int dotIndex = label.IndexOfAny(IriDotCharacters);
                if (dotIndex >= 0)
                {
                    label = label.Slice(0, dotIndex);
                }

                if (!Ascii.IsValid(label))
                {
                    try
                    {
                        string asciiForm = s_idnMapping.GetAscii(hostname, i, label.Length);

                        dest.Append(s_idnMapping.GetUnicode(asciiForm));
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
                else
                {
                    bool aceValid = false;

                    if (label.StartsWith("xn--", StringComparison.Ordinal))
                    {
                        // check ace validity
                        try
                        {
                            dest.Append(s_idnMapping.GetUnicode(hostname, i, label.Length));
                            aceValid = true;
                        }
                        catch (ArgumentException)
                        {
                            // not valid ace so treat it as a normal ascii label
                        }
                    }

                    if (!aceValid)
                    {
                        // for invalid aces we just lowercase the label
                        int charsWritten = label.ToLowerInvariant(dest.AppendSpan(label.Length));
                        Debug.Assert(charsWritten == label.Length);
                    }
                }

                i += label.Length;
            }

            return true;
        }
    }
}
