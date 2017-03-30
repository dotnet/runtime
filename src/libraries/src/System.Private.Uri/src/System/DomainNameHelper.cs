// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System
{
    // The class designed as to keep working set of Uri class as minimal.
    // The idea is to stay with static helper methods and strings
    internal class DomainNameHelper
    {
        private DomainNameHelper()
        {
        }

        internal const string Localhost = "localhost";
        internal const string Loopback = "loopback";

        internal static string ParseCanonicalName(string str, int start, int end, ref bool loopback)
        {
            string res = null;

            for (int i = end - 1; i >= start; --i)
            {
                if (str[i] >= 'A' && str[i] <= 'Z')
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

        internal static unsafe bool IsValid(char* name, ushort pos, ref int returnedEnd, ref bool notCanonical, bool notImplicitFile)
        {
            char* curPos = name + pos;
            char* newPos = curPos;
            char* end = name + returnedEnd;
            for (; newPos < end; ++newPos)
            {
                char ch = *newPos;
                if (ch > 0x7f) return false;    // not ascii
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

            returnedEnd = (ushort)(end - name);
            return true;
        }

        //
        // Checks if the domain name is valid according to iri
        // There are pretty much no restrictions and we effectively return the end of the
        // domain name.
        //
        internal static unsafe bool IsValidByIri(char* name, ushort pos, ref int returnedEnd, ref bool notCanonical, bool notImplicitFile)
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

            returnedEnd = (ushort)(end - name);
            return true;
        }

        internal static string IdnEquivalent(string hostname)
        {
            bool allAscii = true;
            bool atLeastOneValidIdn = false;
            unsafe
            {
                fixed (char* host = hostname)
                {
                    return IdnEquivalent(host, 0, hostname.Length, ref allAscii, ref atLeastOneValidIdn);
                }
            }
        }

        //
        // Will convert a host name into its idn equivalent + tell you if it had a valid idn label
        //
        internal static unsafe string IdnEquivalent(char* hostname, int start, int end, ref bool allAscii, ref bool atLeastOneValidIdn)
        {
            string bidiStrippedHost = null;
            string idnEquivalent = IdnEquivalent(hostname, start, end, ref allAscii, ref bidiStrippedHost);

            if (idnEquivalent != null)
            {
                string strippedHost = (allAscii ? idnEquivalent : bidiStrippedHost);

                fixed (char* strippedHostPtr = strippedHost)
                {
                    int length = strippedHost.Length;
                    int newPos = 0;
                    int curPos = 0;
                    bool foundAce = false;
                    bool checkedAce = false;
                    bool foundDot = false;

                    do
                    {
                        foundAce = false;
                        checkedAce = false;
                        foundDot = false;

                        //find the dot or hit the end
                        newPos = curPos;
                        while (newPos < length)
                        {
                            char c = strippedHostPtr[newPos];
                            if (!checkedAce)
                            {
                                checkedAce = true;
                                if ((newPos + 3 < length) && IsIdnAce(strippedHostPtr, newPos))
                                {
                                    newPos += 4;
                                    foundAce = true;
                                    continue;
                                }
                            }

                            if ((c == '.') || (c == '\u3002') ||    //IDEOGRAPHIC FULL STOP 
                                (c == '\uFF0E') ||                  //FULLWIDTH FULL STOP
                                (c == '\uFF61'))                    //HALFWIDTH IDEOGRAPHIC FULL STOP
                            {
                                foundDot = true;
                                break;
                            }
                            ++newPos;
                        }

                        if (foundAce)
                        {
                            // check ace validity
                            try
                            {
                                IdnMapping map = new IdnMapping();
                                map.GetUnicode(new string(strippedHostPtr, curPos, newPos - curPos));
                                atLeastOneValidIdn = true;
                                break;
                            }
                            catch (ArgumentException)
                            {
                                // not valid ace so treat it as a normal ascii label
                            }
                        }

                        curPos = newPos + (foundDot ? 1 : 0);
                    } while (curPos < length);
                }
            }
            else
            {
                atLeastOneValidIdn = false;
            }
            return idnEquivalent;
        }

        //
        // Will convert a host name into its idn equivalent
        //
        internal static unsafe string IdnEquivalent(char* hostname, int start, int end, ref bool allAscii, ref string bidiStrippedHost)
        {
            string idn = null;
            if (end <= start)
                return idn;

            // indexes are validated

            int newPos = start;
            allAscii = true;

            while (newPos < end)
            {
                // check if only ascii chars
                // special case since idnmapping will not lowercase if only ascii present
                if (hostname[newPos] > '\x7F')
                {
                    allAscii = false;
                    break;
                }
                ++newPos;
            }

            if (allAscii)
            {
                // just lowercase for ascii
                string unescapedHostname = new string(hostname, start, end - start);
                return unescapedHostname.ToLowerInvariant();
            }
            else
            {
                IdnMapping map = new IdnMapping();
                string asciiForm;
                bidiStrippedHost = UriHelper.StripBidiControlCharacter(hostname, start, end - start);
                try
                {
                    asciiForm = map.GetAscii(bidiStrippedHost);
                }
                catch (ArgumentException)
                {
                    throw new UriFormatException(SR.net_uri_BadUnicodeHostForIdn);
                }
                return asciiForm;
            }
        }

        private static unsafe bool IsIdnAce(string input, int index)
        {
            if ((input[index] == 'x') &&
                (input[index + 1] == 'n') &&
                (input[index + 2] == '-') &&
                (input[index + 3] == '-'))
                return true;
            else
                return false;
        }

        private static unsafe bool IsIdnAce(char* input, int index)
        {
            if ((input[index] == 'x') &&
                (input[index + 1] == 'n') &&
                (input[index + 2] == '-') &&
                (input[index + 3] == '-'))
                return true;
            else
                return false;
        }

        //
        // Will convert a host name into its unicode equivalent expanding any existing idn names present
        //
        internal static unsafe string UnicodeEquivalent(string idnHost, char* hostname, int start, int end)
        {
            IdnMapping map = new IdnMapping();

            // Test common scenario first for perf
            // try to get unicode equivalent 
            try
            {
                return map.GetUnicode(idnHost);
            }
            catch (ArgumentException)
            {
            }
            // Here because something threw in GetUnicode above
            // Need to now check individual labels of they had an ace label that was not valid Idn name
            // or if there is a label with invalid Idn char.
            bool dummy = true;
            return UnicodeEquivalent(hostname, start, end, ref dummy, ref dummy);
        }

        internal static unsafe string UnicodeEquivalent(char* hostname, int start, int end, ref bool allAscii, ref bool atLeastOneValidIdn)
        {
            IdnMapping map = new IdnMapping();

            // hostname already validated
            allAscii = true;
            atLeastOneValidIdn = false;
            string idn = null;
            if (end <= start)
                return idn;

            string unescapedHostname = UriHelper.StripBidiControlCharacter(hostname, start, (end - start));

            string unicodeEqvlHost = null;
            int curPos = 0;
            int newPos = 0;
            int length = unescapedHostname.Length;
            bool asciiLabel = true;
            bool foundAce = false;
            bool checkedAce = false;
            bool foundDot = false;


            // We run a loop where for every label
            // a) if label is ascii and no ace then we lowercase it
            // b) if label is ascii and ace and not valid idn then just lowercase it
            // c) if label is ascii and ace and is valid idn then get its unicode eqvl
            // d) if label is unicode then clean it by running it through idnmapping
            do
            {
                asciiLabel = true;
                foundAce = false;
                checkedAce = false;
                foundDot = false;

                //find the dot or hit the end
                newPos = curPos;
                while (newPos < length)
                {
                    char c = unescapedHostname[newPos];
                    if (!checkedAce)
                    {
                        checkedAce = true;
                        if ((newPos + 3 < length) && (c == 'x') && IsIdnAce(unescapedHostname, newPos))
                            foundAce = true;
                    }
                    if (asciiLabel && (c > '\x7F'))
                    {
                        asciiLabel = false;
                        allAscii = false;
                    }
                    if ((c == '.') || (c == '\u3002') ||    //IDEOGRAPHIC FULL STOP 
                        (c == '\uFF0E') ||                  //FULLWIDTH FULL STOP
                        (c == '\uFF61'))                    //HALFWIDTH IDEOGRAPHIC FULL STOP
                    {
                        foundDot = true;
                        break;
                    }
                    ++newPos;
                }

                if (!asciiLabel)
                {
                    string asciiForm = unescapedHostname.Substring(curPos, newPos - curPos);
                    try
                    {
                        asciiForm = map.GetAscii(asciiForm);
                    }
                    catch (ArgumentException)
                    {
                        throw new UriFormatException(SR.net_uri_BadUnicodeHostForIdn);
                    }

                    unicodeEqvlHost += map.GetUnicode(asciiForm);
                    if (foundDot)
                        unicodeEqvlHost += ".";
                }
                else
                {
                    bool aceValid = false;
                    if (foundAce)
                    {
                        // check ace validity
                        try
                        {
                            unicodeEqvlHost += map.GetUnicode(unescapedHostname.Substring(curPos, newPos - curPos));
                            if (foundDot)
                                unicodeEqvlHost += ".";
                            aceValid = true;
                            atLeastOneValidIdn = true;
                        }
                        catch (ArgumentException)
                        {
                            // not valid ace so treat it as a normal ascii label
                        }
                    }

                    if (!aceValid)
                    {
                        // for invalid aces we just lowercase the label
                        unicodeEqvlHost += unescapedHostname.Substring(curPos, newPos - curPos).ToLowerInvariant();
                        if (foundDot)
                            unicodeEqvlHost += ".";
                    }
                }

                curPos = newPos + (foundDot ? 1 : 0);
            } while (curPos < length);

            return unicodeEqvlHost;
        }

        //
        //  Determines whether a character is a letter or digit according to the
        //  DNS specification [RFC 1035]. We use our own variant of IsLetterOrDigit
        //  because the base version returns false positives for non-ANSI characters
        //
        private static bool IsASCIILetterOrDigit(char character, ref bool notCanonical)
        {
            if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
                return true;

            if (character >= 'A' && character <= 'Z')
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
        private static bool IsValidDomainLabelCharacter(char character, ref bool notCanonical)
        {
            if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || (character == '-') || (character == '_'))
                return true;

            if (character >= 'A' && character <= 'Z')
            {
                notCanonical = true;
                return true;
            }
            return false;
        }
    }
}
