// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;

namespace System.Xml
{
    /// <summary>
    ///  The XmlCharType class is used for quick character type recognition
    ///  which is optimized for the first 127 ascii characters.
    /// </summary>
    internal struct XmlCharType
    {
        // Surrogate constants
        internal const int SurHighStart = 0xd800;    // 1101 10xx
        internal const int SurHighEnd = 0xdbff;
        internal const int SurLowStart = 0xdc00;    // 1101 11xx
        internal const int SurLowEnd = 0xdfff;
        internal const int SurMask = 0xfc00;    // 1111 11xx

        // Characters defined in the XML 1.0 Fourth Edition
        // Whitespace chars -- Section 2.3 [3]
        // Letters -- Appendix B [84]
        // Starting NCName characters -- Section 2.3 [5] (Starting Name characters without ':')
        // NCName characters -- Section 2.3 [4]          (Name characters without ':')
        // Character data characters -- Section 2.2 [2]
        // PubidChar ::=  #x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%] Section 2.3 of spec
        internal const int fWhitespace = 1;
        internal const int fLetter = 2;
        internal const int fNCStartNameSC = 4;
        internal const int fNCNameSC = 8;
        internal const int fCharData = 16;
        internal const int fNCNameXml4e = 32;
        internal const int fText = 64;
        internal const int fAttrValue = 128;

        // bitmap for public ID characters - 1 bit per character 0x0 - 0x80; no character > 0x80 is a PUBLIC ID char
        private const string s_PublicIdBitmap = "\u2400\u0000\uffbb\uafff\uffff\u87ff\ufffe\u07ff";


        public static XmlCharType Instance => default;

        public bool IsWhiteSpace(char ch)
        {
            return (s_charProperties[ch] & fWhitespace) != 0;
        }

        public bool IsNCNameSingleChar(char ch)
        {
            return (s_charProperties[ch] & fNCNameSC) != 0;
        }

        public bool IsStartNCNameSingleChar(char ch)
        {
            return (s_charProperties[ch] & fNCStartNameSC) != 0;
        }

        public bool IsNameSingleChar(char ch)
        {
            return IsNCNameSingleChar(ch) || ch == ':';
        }

        public bool IsCharData(char ch)
        {
            return (s_charProperties[ch] & fCharData) != 0;
        }

        // [13] PubidChar ::=  #x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%] Section 2.3 of spec
        public bool IsPubidChar(char ch)
        {
            if (ch < (char)0x80)
            {
                return (s_PublicIdBitmap[ch >> 4] & (1 << (ch & 0xF))) != 0;
            }
            return false;
        }

        // TextChar = CharData - { 0xA, 0xD, '<', '&', ']' }
        internal bool IsTextChar(char ch)
        {
            return (s_charProperties[ch] & fText) != 0;
        }

        // AttrValueChar = CharData - { 0xA, 0xD, 0x9, '<', '>', '&', '\'', '"' }
        internal bool IsAttributeValueChar(char ch)
        {
            return (s_charProperties[ch] & fAttrValue) != 0;
        }

        // XML 1.0 Fourth Edition definitions
        public bool IsLetter(char ch)
        {
            return (s_charProperties[ch] & fLetter) != 0;
        }

        // This method uses the XML 4th edition name character ranges
        public bool IsNCNameCharXml4e(char ch)
        {
            return (s_charProperties[ch] & fNCNameXml4e) != 0;
        }

        // This method uses the XML 4th edition name character ranges
        public bool IsStartNCNameCharXml4e(char ch)
        {
            return IsLetter(ch) || ch == '_';
        }

        // This method uses the XML 4th edition name character ranges
        public bool IsNameCharXml4e(char ch)
        {
            return IsNCNameCharXml4e(ch) || ch == ':';
        }

        // Digit methods
        public static bool IsDigit(char ch)
        {
            return InRange(ch, 0x30, 0x39);
        }

        // Surrogate methods
        internal static bool IsHighSurrogate(int ch)
        {
            return InRange(ch, SurHighStart, SurHighEnd);
        }

        internal static bool IsLowSurrogate(int ch)
        {
            return InRange(ch, SurLowStart, SurLowEnd);
        }

        internal static bool IsSurrogate(int ch)
        {
            return InRange(ch, SurHighStart, SurLowEnd);
        }

        internal static int CombineSurrogateChar(int lowChar, int highChar)
        {
            return (lowChar - SurLowStart) | ((highChar - SurHighStart) << 10) + 0x10000;
        }

        internal static void SplitSurrogateChar(int combinedChar, out char lowChar, out char highChar)
        {
            int v = combinedChar - 0x10000;
            lowChar = (char)(SurLowStart + v % 1024);
            highChar = (char)(SurHighStart + v / 1024);
        }

        internal bool IsOnlyWhitespace(string? str)
        {
            return IsOnlyWhitespaceWithPos(str) == -1;
        }

        // Character checking on strings
        internal int IsOnlyWhitespaceWithPos(string? str)
        {
            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if ((s_charProperties[str[i]] & fWhitespace) == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        internal int IsOnlyCharData(string str)
        {
            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if ((s_charProperties[str[i]] & fCharData) == 0)
                    {
                        if (i + 1 >= str.Length || !(XmlCharType.IsHighSurrogate(str[i]) && XmlCharType.IsLowSurrogate(str[i + 1])))
                        {
                            return i;
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }
            return -1;
        }

        internal static bool IsOnlyDigits(string str, int startPos, int len)
        {
            Debug.Assert(str != null);
            Debug.Assert(startPos + len <= str.Length);
            Debug.Assert(startPos <= str.Length);

            for (int i = startPos; i < startPos + len; i++)
            {
                if (!IsDigit(str[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal int IsPublicId(string str)
        {
            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (!IsPubidChar(str[i]))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        // This method tests whether a value is in a given range with just one test; start and end should be constants
        private static bool InRange(int value, int start, int end)
        {
            Debug.Assert(start <= end);
            return (uint)(value - start) <= (uint)(end - start);
        }

    }
}
