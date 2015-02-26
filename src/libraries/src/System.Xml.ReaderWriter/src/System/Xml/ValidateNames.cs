// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        private static XmlCharType s_xmlCharType = XmlCharType.Instance;


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
#if !SILVERLIGHT_DISABLE_SECURITY && XMLCHARTYPE_USE_RESOURCE
        [System.Security.SecuritySafeCritical]
#endif
        internal static unsafe int ParseNmtokenNoNamespaces(string s, int offset)
        {
            Debug.Assert(s != null && offset <= s.Length);

            // Keep parsing until the end of string or an invalid Name character is reached
            int i = offset;
            while (i < s.Length)
            {
                if (s_xmlCharType.IsNameSingleChar(s[i]) || s[i] == ':')
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
#if !SILVERLIGHT_DISABLE_SECURITY && XMLCHARTYPE_USE_RESOURCE
        [System.Security.SecuritySafeCritical]
#endif
        internal static unsafe int ParseNameNoNamespaces(string s, int offset)
        {
            Debug.Assert(s != null && offset <= s.Length);

            // Quit if the first character is not a valid NCName starting character
            int i = offset;
            if (i < s.Length)
            {
                if (s_xmlCharType.IsStartNCNameSingleChar(s[i]) || s[i] == ':')
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
                    if (s_xmlCharType.IsNCNameSingleChar(s[i]) || s[i] == ':')
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
#if !SILVERLIGHT_DISABLE_SECURITY && XMLCHARTYPE_USE_RESOURCE
        [System.Security.SecuritySafeCritical]
#endif
        internal static unsafe int ParseNCName(string s, int offset)
        {
            Debug.Assert(s != null && offset <= s.Length);

            // Quit if the first character is not a valid NCName starting character
            int i = offset;
            if (i < s.Length)
            {
                if (s_xmlCharType.IsStartNCNameSingleChar(s[i]))
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
                    if (s_xmlCharType.IsNCNameSingleChar(s[i]))
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

        /// <summary>
        /// Calls parseName and throws exception if the resulting name is not a valid NCName.
        /// Returns the input string if there is no error.
        /// </summary>
        internal static string ParseNCNameThrow(string s)
        {
            // throwOnError = true
            ParseNCNameInternal(s, true);
            return s;
        }

        /// <summary>
        /// Calls parseName and returns false or throws exception if the resulting name is not
        /// a valid NCName.  Returns the input string if there is no error.
        /// </summary>
        private static bool ParseNCNameInternal(string s, bool throwOnError)
        {
            int len = ParseNCName(s, 0);

            if (len == 0 || len != s.Length)
            {
                // If the string is not a valid NCName, then throw or return false
                if (throwOnError) ThrowInvalidName(s, 0, len);
                return false;
            }

            return true;
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
            int len, lenLocal;

            // Assume no colon
            colonOffset = 0;

            // Parse NCName (may be prefix, may be local name)
            len = ParseNCName(s, offset);
            if (len != 0)
            {
                // Non-empty NCName, so look for colon if there are any characters left
                offset += len;
                if (offset < s.Length && s[offset] == ':')
                {
                    // First NCName was prefix, so look for local name part
                    lenLocal = ParseNCName(s, offset + 1);
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
                throw new XmlException(SR.Xml_EmptyName, string.Empty);

            Debug.Assert(offsetBadChar < s.Length);

            if (s_xmlCharType.IsNCNameSingleChar(s[offsetBadChar]) && !XmlCharType.Instance.IsStartNCNameSingleChar(s[offsetBadChar]))
            {
                // The error character is a valid name character, but is not a valid start name character
                throw new XmlException(SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(s, offsetBadChar));
            }
            else
            {
                // The error character is an invalid name character
                throw new XmlException(SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(s, offsetBadChar));
            }
        }
    }
}
