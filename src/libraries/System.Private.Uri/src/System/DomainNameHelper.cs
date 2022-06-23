// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static readonly IdnMapping s_idnMapping = new IdnMapping();

        internal const string Localhost = "localhost";
        internal const string Loopback = "loopback";

        internal static string ParseCanonicalName(string str, int start, int end, ref bool loopback)
        {
            string? res = null;

            for (int i = end - 1; i >= start; --i)
            {
                if (char.IsAsciiLetterUpper(str[i]))
                {
                    res = str.Substring(start, end - start).ToLowerInvariant();
                    break;
                }
                if (str[i] == ':')
                    end = i;
            }

            if (res == null)
            {
                res = str.Substring(start, end - start);
            }

            if (res == Localhost || res == Loopback)
            {
                loopback = true;
                return Localhost;
            }
            return res;
        }

        //
        // IsValid
        //
        //  Determines whether a string is a valid domain name
        //
        //      subdomain -> <label> | <label> "." <subdomain>
        //
        // Inputs:
        //  - name as Name to test
        //  - starting position
        //  - ending position
        //
        // Outputs:
        //  The end position of a valid domain name string, the canonical flag if found so
        //
        // Returns:
        //  bool
        //
        //  Remarks: Optimized for speed as a most common case,
        //           MUST NOT be used unless all input indexes are verified and trusted.
        //

        internal static unsafe bool IsValid(char* name, int pos, ref int returnedEnd, ref bool notCanonical, bool notImplicitFile)
        {
            char* curPos = name + pos;
            char* newPos = curPos;
            char* end = name + returnedEnd;
            for (; newPos < end; ++newPos)
            {
                char ch = *newPos;
                if (ch > 0x7f) return false;    // not ascii
                if (ch < 'a') // Optimize for lower-case letters, which make up the majority of most Uris, and which are all greater than symbols checked for below
                {
                    if (ch == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
                    {
                        end = newPos;
                        break;
                    }
                }
            }

            if (end == curPos)
            {
                return false;
            }

            do
            {
                //  Determines whether a string is a valid domain name label. In keeping
                //  with RFC 1123, section 2.1, the requirement that the first character
                //  of a label be alphabetic is dropped. Therefore, Domain names are
                //  formed as:
                //
                //      <label> -> <alphanum> [<alphanum> | <hyphen> | <underscore>] * 62

                //find the dot or hit the end
                newPos = curPos;
                while (newPos < end)
                {
                    if (*newPos == '.') break;
                    ++newPos;
                }

                //check the label start/range
                if (curPos == newPos || newPos - curPos > 63 || !IsASCIILetterOrDigit(*curPos++, ref notCanonical))
                {
                    return false;
                }
                //check the label content
                while (curPos < newPos)
                {
                    if (!IsValidDomainLabelCharacter(*curPos++, ref notCanonical))
                    {
                        return false;
                    }
                }
                ++curPos;
            } while (curPos < end);

            returnedEnd = (int)(end - name);
            return true;
        }

        //
        // Checks if the domain name is valid according to iri
        // There are pretty much no restrictions and we effectively return the end of the
        // domain name.
        //
        internal static unsafe bool IsValidByIri(char* name, int pos, ref int returnedEnd, ref bool notCanonical, bool notImplicitFile)
        {
            char* curPos = name + pos;
            char* newPos = curPos;
            char* end = name + returnedEnd;
            int count = 0; // count number of octets in a label;

            for (; newPos < end; ++newPos)
            {
                char ch = *newPos;
                if (ch == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
                {
                    end = newPos;
                    break;
                }
            }

            if (end == curPos)
            {
                return false;
            }

            do
            {
                //  Determines whether a string is a valid domain name label. In keeping
                //  with RFC 1123, section 2.1, the requirement that the first character
                //  of a label be alphabetic is dropped. Therefore, Domain names are
                //  formed as:
                //
                //      <label> -> <alphanum> [<alphanum> | <hyphen> | <underscore>] * 62

                //find the dot or hit the end
                newPos = curPos;
                count = 0;
                bool labelHasUnicode = false; // if label has unicode we need to add 4 to label count for xn--
                while (newPos < end)
                {
                    if ((*newPos == '.') ||
                        (*newPos == '\u3002') ||    //IDEOGRAPHIC FULL STOP
                        (*newPos == '\uFF0E') ||    //FULLWIDTH FULL STOP
                        (*newPos == '\uFF61'))      //HALFWIDTH IDEOGRAPHIC FULL STOP
                        break;
                    count++;
                    if (*newPos > 0xFF)
                        count++; // counts for two octets
                    if (*newPos >= 0xA0)
                        labelHasUnicode = true;

                    ++newPos;
                }

                //check the label start/range
                if (curPos == newPos || (labelHasUnicode ? count + 4 : count) > 63 || ((*curPos++ < 0xA0) && !IsASCIILetterOrDigit(*(curPos - 1), ref notCanonical)))
                {
                    return false;
                }
                //check the label content
                while (curPos < newPos)
                {
                    if ((*curPos++ < 0xA0) && !IsValidDomainLabelCharacter(*(curPos - 1), ref notCanonical))
                    {
                        return false;
                    }
                }
                ++curPos;
            } while (curPos < end);

            returnedEnd = (int)(end - name);
            return true;
        }

        /// <summary>Converts a host name into its idn equivalent.</summary>
        internal static string IdnEquivalent(string hostname)
        {
            if (hostname.Length == 0)
            {
                return hostname;
            }

            // check if only ascii chars
            // special case since idnmapping will not lowercase if only ascii present
            bool allAscii = true;
            foreach (char c in hostname)
            {
                if (c > 0x7F)
                {
                    allAscii = false;
                    break;
                }
            }

            if (allAscii)
            {
                // just lowercase for ascii
                return hostname.ToLowerInvariant();
            }

            string bidiStrippedHost = UriHelper.StripBidiControlCharacters(hostname, hostname);

            try
            {
                string asciiForm = s_idnMapping.GetAscii(bidiStrippedHost);
                if (ContainsCharactersUnsafeForNormalizedHost(asciiForm))
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

        internal static bool TryGetUnicodeEquivalent(string hostname, ref ValueStringBuilder dest)
        {
            Debug.Assert(ReferenceEquals(hostname, UriHelper.StripBidiControlCharacters(hostname, hostname)));

            int curPos = 0;

            // We run a loop where for every label
            // a) if label is ascii and no ace then we lowercase it
            // b) if label is ascii and ace and not valid idn then just lowercase it
            // c) if label is ascii and ace and is valid idn then get its unicode eqvl
            // d) if label is unicode then clean it by running it through idnmapping
            do
            {
                if (curPos != 0)
                {
                    dest.Append('.');
                }

                bool asciiLabel = true;

                //find the dot or hit the end
                int newPos;
                for (newPos = curPos; (uint)newPos < (uint)hostname.Length; newPos++)
                {
                    char c = hostname[newPos];

                    if (c == '.')
                    {
                        break;
                    }

                    if (c > '\x7F')
                    {
                        asciiLabel = false;

                        if ((c == '\u3002') || // IDEOGRAPHIC FULL STOP
                            (c == '\uFF0E') || // FULLWIDTH FULL STOP
                            (c == '\uFF61'))   // HALFWIDTH IDEOGRAPHIC FULL STOP
                        {
                            break;
                        }
                    }
                }

                if (!asciiLabel)
                {
                    try
                    {
                        string asciiForm = s_idnMapping.GetAscii(hostname, curPos, newPos - curPos);

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

                    if ((uint)(curPos + 3) < (uint)hostname.Length
                        && hostname[curPos] == 'x'
                        && hostname[curPos + 1] == 'n'
                        && hostname[curPos + 2] == '-'
                        && hostname[curPos + 3] == '-')
                    {
                        // check ace validity
                        try
                        {
                            dest.Append(s_idnMapping.GetUnicode(hostname, curPos, newPos - curPos));
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
                        ReadOnlySpan<char> slice = hostname.AsSpan(curPos, newPos - curPos);
                        int charsWritten = slice.ToLowerInvariant(dest.AppendSpan(slice.Length));
                        Debug.Assert(charsWritten == slice.Length);
                    }
                }

                curPos = newPos + 1;
            } while (curPos < hostname.Length);

            return true;
        }

        //
        //  Determines whether a character is a letter or digit according to the
        //  DNS specification [RFC 1035]. We use our own variant of IsLetterOrDigit
        //  because the base version returns false positives for non-ANSI characters
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsASCIILetterOrDigit(char character, ref bool notCanonical)
        {
            if (char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character))
            {
                return true;
            }

            if (char.IsAsciiLetterUpper(character))
            {
                notCanonical = true;
                return true;
            }

            return false;
        }

        //
        //  Takes into account the additional legal domain name characters '-' and '_'
        //  Note that '_' char is formally invalid but is historically in use, especially on corpnets
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidDomainLabelCharacter(char character, ref bool notCanonical)
        {
            if (char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-' || character == '_')
            {
                return true;
            }

            if (char.IsAsciiLetterUpper(character))
            {
                notCanonical = true;
                return true;
            }

            return false;
        }

        // The Unicode specification allows certain code points to be normalized not to
        // punycode, but to ASCII representations that retain the same meaning. For example,
        // the codepoint U+00BC "Vulgar Fraction One Quarter" is normalized to '1/4' rather
        // than being punycoded.
        //
        // This means that a host containing Unicode characters can be normalized to contain
        // URI reserved characters, changing the meaning of a URI only when certain properties
        // such as IdnHost are accessed. To be safe, disallow control characters in normalized hosts.
        private static readonly char[] s_UnsafeForNormalizedHost = { '\\', '/', '?', '@', '#', ':', '[', ']' };

        internal static bool ContainsCharactersUnsafeForNormalizedHost(string host)
        {
            return host.IndexOfAny(s_UnsafeForNormalizedHost) != -1;
        }
    }
}
