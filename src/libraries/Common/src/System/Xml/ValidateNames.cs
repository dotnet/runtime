// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    /// <summary>
    /// Contains various static functions and methods for parsing and validating:
    ///     NCName (not namespace-aware, no colons allowed)
    ///     QName (prefix:local-name)
    /// </summary>
    internal static class ValidateNames
    {
        internal enum Flags
        {
            NCNames = 0x1,              // Validate that each non-empty prefix and localName is a valid NCName
            CheckLocalName = 0x2,       // Validate the local-name
            CheckPrefixMapping = 0x4,   // Validate the prefix --> namespace mapping
            All = 0x7,
            AllExceptNCNames = 0x6,
            AllExceptPrefixMapping = 0x3,
        };

        static XmlCharType xmlCharType = XmlCharType.Instance;

        //-----------------------------------------------
        // Nmtoken parsing
        //-----------------------------------------------
        /// <summary>
        /// Attempts to parse the input string as an Nmtoken (see the XML spec production [7] && XML Namespaces spec).
        /// Quits parsing when an invalid Nmtoken char is reached or the end of string is reached.
        /// Returns the number of valid Nmtoken chars that were parsed.
        /// </summary>
        internal static unsafe int ParseNmtoken(string s, int offset)
        {
            Debug.Assert(s != null && offset <= s.Length);

            // Keep parsing until the end of string or an invalid NCName character is reached
            int i = offset;
            while (i < s.Length)
            {
                if ((xmlCharType.charProperties[s[i]] & XmlCharType.fNCNameSC) != 0)
                {
                    i++;
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(s, i))
                {
                    i += 2;
                }
#endif
                else
                {
                    break;
                }
            }

            return i - offset;
        }

        //-----------------------------------------------
        // Nmtoken parsing (no XML namespaces support)
        //-----------------------------------------------
        /// <summary>
        /// Attempts to parse the input string as an Nmtoken (see the XML spec production [7]) without taking 
        /// into account the XML Namespaces spec. What it means is that the ':' character is allowed at any 
        /// position and any number of times in the token.
        /// Quits parsing when an invalid Nmtoken char is reached or the end of string is reached.
        /// Returns the number of valid Nmtoken chars that were parsed.
        /// </summary>
        internal static unsafe int ParseNmtokenNoNamespaces(string s, int offset)
        {
            Debug.Assert(s != null && offset <= s.Length);

            // Keep parsing until the end of string or an invalid Name character is reached
            int i = offset;
            while (i < s.Length)
            {
                if ((xmlCharType.charProperties[s[i]] & XmlCharType.fNCNameSC) != 0 || s[i] == ':')
                { // if (xmlCharType.IsNameSingleChar(s[i])) {
                    i++;
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(s, i))
                {
                    i += 2;
                }
#endif
                else
                {
                    break;
                }
            }

            return i - offset;
        }

        // helper methods
        internal static bool IsNmtokenNoNamespaces(string s)
        {
            int endPos = ParseNmtokenNoNamespaces(s, 0);
            return endPos > 0 && endPos == s.Length;
        }

        //-----------------------------------------------
        // Name parsing (no XML namespaces support)
        //-----------------------------------------------
        /// <summary>
        /// Attempts to parse the input string as a Name without taking into account the XML Namespaces spec.
        /// What it means is that the ':' character does not delimiter prefix and local name, but it is a regular
        /// name character, which is allowed to appear at any position and any number of times in the name.
        /// Quits parsing when an invalid Name char is reached or the end of string is reached.
        /// Returns the number of valid Name chars that were parsed.
        /// </summary>
        internal static unsafe int ParseNameNoNamespaces(string s, int offset)
        {
            Debug.Assert(s != null && offset <= s.Length);

            // Quit if the first character is not a valid NCName starting character
            int i = offset;
            if (i < s.Length)
            {
                if ((xmlCharType.charProperties[s[i]] & XmlCharType.fNCStartNameSC) != 0 || s[i] == ':')
                {
                    i++;
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(s, i))
                {
                    i += 2;
                }
#endif
                else
                {
                    return 0; // no valid StartNCName char
                }

                // Keep parsing until the end of string or an invalid NCName character is reached
                while (i < s.Length)
                {
                    if ((xmlCharType.charProperties[s[i]] & XmlCharType.fNCNameSC) != 0 || s[i] == ':')
                    {
                        i++;
                    }
#if XML10_FIFTH_EDITION
                    else if (xmlCharType.IsNCNameSurrogateChar(s, i))
                    {
                        i += 2;
                    }
#endif
                    else
                    {
                        break;
                    }
                }
            }

            return i - offset;
        }

        // helper methods
        internal static bool IsNameNoNamespaces(string s)
        {
            int endPos = ParseNameNoNamespaces(s, 0);
            return endPos > 0 && endPos == s.Length;
        }

        //-----------------------------------------------
        // NCName parsing
        //-----------------------------------------------

        /// <summary>
        /// Attempts to parse the input string as an NCName (see the XML Namespace spec).
        /// Quits parsing when an invalid NCName char is reached or the end of string is reached.
        /// Returns the number of valid NCName chars that were parsed.
        /// </summary>
        internal static unsafe int ParseNCName(string s, int offset)
        {
            Debug.Assert(s != null && offset <= s.Length);

            // Quit if the first character is not a valid NCName starting character
            int i = offset;
            if (i < s.Length)
            {
                if ((xmlCharType.charProperties[s[i]] & XmlCharType.fNCStartNameSC) != 0)
                {
                    i++;
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(s, i))
                {
                    i += 2;
                }
#endif
                else
                {
                    return 0; // no valid StartNCName char
                }

                // Keep parsing until the end of string or an invalid NCName character is reached
                while (i < s.Length)
                {
                    if ((xmlCharType.charProperties[s[i]] & XmlCharType.fNCNameSC) != 0)
                    {
                        i++;
                    }
#if XML10_FIFTH_EDITION
                    else if (xmlCharType.IsNCNameSurrogateChar(s, i))
                    {
                        i += 2;
                    }
#endif
                    else
                    {
                        break;
                    }
                }
            }

            return i - offset;
        }

        internal static int ParseNCName(string s)
        {
            return ParseNCName(s, 0);
        }

        //-----------------------------------------------
        // QName parsing
        //-----------------------------------------------

        /// <summary>
        /// Attempts to parse the input string as a QName (see the XML Namespace spec).
        /// Quits parsing when an invalid QName char is reached or the end of string is reached.
        /// Returns the number of valid QName chars that were parsed.
        /// Sets colonOffset to the offset of a colon character if it exists, or 0 otherwise.
        /// </summary>
        internal static int ParseQName(string s, int offset, out int colonOffset)
        {
            // Assume no colon
            colonOffset = 0;

            // Parse NCName (may be prefix, may be local name)
            int len = ParseNCName(s, offset);
            if (len != 0)
            {
                // Non-empty NCName, so look for colon if there are any characters left
                offset += len;
                if (offset < s.Length && s[offset] == ':')
                {
                    // First NCName was prefix, so look for local name part
                    int lenLocal = ParseNCName(s, offset + 1);
                    if (lenLocal != 0)
                    {
                        // Local name part found, so increase total QName length (add 1 for colon)
                        colonOffset = offset;
                        len += lenLocal + 1;
                    }
                }
            }

            return len;
        }

        /// <summary>
        /// Calls parseQName and throws exception if the resulting name is not a valid QName.
        /// Returns the prefix and local name parts.
        /// </summary>
        internal static void ParseQNameThrow(string s, out string prefix, out string localName)
        {
            int colonOffset;
            int len = ParseQName(s, 0, out colonOffset);

            if (len == 0 || len != s.Length)
            {
                // If the string is not a valid QName, then throw
                ThrowInvalidName(s, 0, len);
            }

            if (colonOffset != 0)
            {
                prefix = s.Substring(0, colonOffset);
                localName = s.Substring(colonOffset + 1);
            }
            else
            {
                prefix = "";
                localName = s;
            }
        }

        /// <summary>
        /// Throws an invalid name exception.
        /// </summary>
        /// <param name="s">String that was parsed.</param>
        /// <param name="offsetStartChar">Offset in string where parsing began.</param>
        /// <param name="offsetBadChar">Offset in string where parsing failed.</param>
        internal static void ThrowInvalidName(string s, int offsetStartChar, int offsetBadChar)
        {
            // If the name is empty, throw an exception
            if (offsetStartChar >= s.Length)
                throw new XmlException(SR.Xml_EmptyName);

            Debug.Assert(offsetBadChar < s.Length);

            if (xmlCharType.IsNCNameSingleChar(s[offsetBadChar]) && !XmlCharType.Instance.IsStartNCNameSingleChar(s[offsetBadChar]))
            {
                // The error character is a valid name character, but is not a valid start name character
                throw new XmlException(SR.Format(SR.Xml_BadStartNameChar, XmlExceptionHelper.BuildCharExceptionArgs(s, offsetBadChar)));
            }
            else
            {
                // The error character is an invalid name character
                throw new XmlException(SR.Format(SR.Xml_BadNameChar, XmlExceptionHelper.BuildCharExceptionArgs(s, offsetBadChar)));
            }
        }

        /// <summary>
        /// Split a QualifiedName into prefix and localname, w/o any checking.
        /// (Used for XmlReader/XPathNavigator MoveTo(name) methods)
        /// </summary>
        internal static void SplitQName(string name, out string prefix, out string lname)
        {
            int colonPos = name.IndexOf(':');
            if (-1 == colonPos)
            {
                prefix = string.Empty;
                lname = name;
            }
            else if (0 == colonPos || (name.Length - 1) == colonPos)
            {
                throw new ArgumentException(SR.Format(SR.Xml_BadNameChar, XmlExceptionHelper.BuildCharExceptionArgs(':', '\0')), nameof(name));
            }
            else
            {
                prefix = name.Substring(0, colonPos);
                colonPos++; // move after colon
                lname = name.Substring(colonPos, name.Length - colonPos);
            }
        }
    }

    internal class XmlExceptionHelper
    {
        internal static string[] BuildCharExceptionArgs(string data, int invCharIndex)
        {
            return BuildCharExceptionArgs(data[invCharIndex], invCharIndex + 1 < data.Length ? data[invCharIndex + 1] : '\0');
        }

        internal static string[] BuildCharExceptionArgs(char[] data, int invCharIndex)
        {
            return BuildCharExceptionArgs(data, data.Length, invCharIndex);
        }

        internal static string[] BuildCharExceptionArgs(char[] data, int length, int invCharIndex)
        {
            Debug.Assert(invCharIndex < data.Length);
            Debug.Assert(invCharIndex < length);
            Debug.Assert(length <= data.Length);

            return BuildCharExceptionArgs(data[invCharIndex], invCharIndex + 1 < length ? data[invCharIndex + 1] : '\0');
        }

        internal static string[] BuildCharExceptionArgs(char invChar, char nextChar)
        {
            string[] aStringList = new string[2];

            // for surrogate characters include both high and low char in the message so that a full character is displayed
            if (XmlCharType.IsHighSurrogate(invChar) && nextChar != 0)
            {
                int combinedChar = XmlCharType.CombineSurrogateChar(nextChar, invChar);
                aStringList[0] = new string(new char[] { invChar, nextChar });
                aStringList[1] = string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", combinedChar);
            }
            else
            {
                // don't include 0 character in the string - in means eof-of-string in native code, where this may bubble up to
                if ((int)invChar == 0)
                {
                    aStringList[0] = ".";
                }
                else
                {
                    aStringList[0] = Convert.ToString(invChar, CultureInfo.InvariantCulture);
                }
                aStringList[1] = string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", (int)invChar);
            }
            return aStringList;
        }
    }
}
