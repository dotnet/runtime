// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime
{
    internal sealed class TokenInfo
    {
        public char startChar;      // First element of numbering sequence for format token
        public int startIdx;       // Start index of separator token
        public string formatString;   // Format string for separator token
        public int length;         // Length of separator token, or minimum length of decimal numbers for format token

        // Instances of this internal class must be created via CreateFormat and CreateSeparator
        private TokenInfo()
        {
        }

        [Conditional("DEBUG")]
        public void AssertSeparator(bool isSeparator)
        {
            Debug.Assert(isSeparator == (formatString != null), "AssertSeparator");
        }

        // Creates a TokenInfo for a separator token.
        public static TokenInfo CreateSeparator(string formatString, int startIdx, int tokLen)
        {
            Debug.Assert(startIdx >= 0 && tokLen > 0);
            TokenInfo token = new TokenInfo();
            {
                token.startIdx = startIdx;
                token.formatString = formatString;
                token.length = tokLen;
            }
            return token;
        }

        // Maps a token of alphanumeric characters to a numbering format ID and a
        // minimum length bound.  Tokens specify the character(s) that begins a Unicode
        // numbering sequence.  For example, "i" specifies lower case roman numeral
        // numbering.  Leading "zeros" specify a minimum length to be maintained by
        // padding, if necessary.
        public static TokenInfo CreateFormat(string formatString, int startIdx, int tokLen)
        {
            Debug.Assert(startIdx >= 0 && tokLen > 0);
            TokenInfo token = new TokenInfo();
            token.formatString = null;
            token.length = 1;

            bool useDefault = false;
            char ch = formatString[startIdx];

            switch (ch)
            {
                case '1':
                case 'A':
                case 'I':
                case 'a':
                case 'i':
                    break;
                default:
                    // NOTE: We do not support Tamil and Ethiopic numbering systems having no zeros
                    if (CharUtil.IsDecimalDigitOne(ch))
                    {
                        break;
                    }
                    if (CharUtil.IsDecimalDigitOne((char)(ch + 1)))
                    {
                        // Leading zeros request padding.  Track how much.
                        int idx = startIdx;
                        do
                        {
                            token.length++;
                        } while (--tokLen > 0 && ch == formatString[++idx]);

                        // Recognize the token only if the next character is "one"
                        if (formatString[idx] == ++ch)
                        {
                            break;
                        }
                    }
                    useDefault = true;
                    break;
            }

            if (tokLen != 1)
            {
                // If remaining token length is not 1, do not recognize the token
                useDefault = true;
            }

            if (useDefault)
            {
                // Default to Arabic numbering with no zero padding
                token.startChar = NumberFormatter.DefaultStartChar;
                token.length = 1;
            }
            else
            {
                token.startChar = ch;
            }
            return token;
        }
    }

    internal sealed class NumberFormatter : NumberFormatterBase
    {
        private readonly string _formatString;
        private readonly int _lang;
        private readonly string _letterValue;
        private readonly string _groupingSeparator;
        private readonly int _groupingSize;

        private readonly List<TokenInfo> _tokens;

        public const char DefaultStartChar = '1';
        private static readonly TokenInfo s_defaultFormat = TokenInfo.CreateFormat("0", 0, 1);
        private static readonly TokenInfo s_defaultSeparator = TokenInfo.CreateSeparator(".", 0, 1);

        // Creates a Format object parsing format string into format tokens (alphanumeric) and separators (non-alphanumeric).
        public NumberFormatter(string formatString, int lang, string letterValue, string groupingSeparator, int groupingSize)
        {
            Debug.Assert(groupingSeparator.Length <= 1);
            _formatString = formatString;
            _lang = lang;
            _letterValue = letterValue;
            _groupingSeparator = groupingSeparator;
            _groupingSize = groupingSeparator.Length > 0 ? groupingSize : 0;

            if (formatString == "1" || formatString.Length == 0)
            {
                // Special case of the default format
                return;
            }

            _tokens = new List<TokenInfo>();
            int idxStart = 0;
            bool isAlphaNumeric = CharUtil.IsAlphaNumeric(formatString[idxStart]);

            if (isAlphaNumeric)
            {
                // If the first one is alpha num add empty separator as a prefix
                _tokens.Add(null);
            }

            for (int idx = 0; idx <= formatString.Length; idx++)
            {
                // Loop until a switch from formatString token to separator is detected (or vice-versa)
                if (idx == formatString.Length || isAlphaNumeric != CharUtil.IsAlphaNumeric(formatString[idx]))
                {
                    if (isAlphaNumeric)
                    {
                        // Just finished a format token
                        _tokens.Add(TokenInfo.CreateFormat(formatString, idxStart, idx - idxStart));
                    }
                    else
                    {
                        // Just finished a separator token
                        _tokens.Add(TokenInfo.CreateSeparator(formatString, idxStart, idx - idxStart));
                    }

                    // Begin parsing the next format token or separator
                    idxStart = idx;

                    // Flip flag from format token to separator or vice-versa
                    isAlphaNumeric = !isAlphaNumeric;
                }
            }
        }

        /// <summary>
        /// Format the given xsl:number place marker
        /// </summary>
        /// <param name="val">Place marker - either a sequence of ints, or a double singleton</param>
        /// <returns>Formatted string</returns>
        public string FormatSequence(IList<XPathItem> val)
        {
            StringBuilder sb = new StringBuilder();

            // If the value was supplied directly, in the 'value' attribute, check its validity
            if (val.Count == 1 && val[0].ValueType == typeof(double))
            {
                double dblVal = val[0].ValueAsDouble;
                if (!(0.5 <= dblVal && dblVal < double.PositiveInfinity))
                {
                    // Errata E24: It is an error if the number is NaN, infinite or less than 0.5; an XSLT processor may signal
                    // the error; if it does not signal the error, it must recover by converting the number to a string as if
                    // by a call to the 'string' function and inserting the resulting string into the result tree.
                    return XPathConvert.DoubleToString(dblVal);
                }
            }

            if (_tokens == null)
            {
                // Special case of the default format
                for (int idx = 0; idx < val.Count; idx++)
                {
                    if (idx > 0)
                    {
                        sb.Append('.');
                    }
                    FormatItem(sb, val[idx], DefaultStartChar, 1);
                }
            }
            else
            {
                int cFormats = _tokens.Count;
                TokenInfo prefix = _tokens[0], suffix;

                if (cFormats % 2 == 0)
                {
                    suffix = null;
                }
                else
                {
                    suffix = _tokens[--cFormats];
                }

                TokenInfo periodicSeparator = 2 < cFormats ? _tokens[cFormats - 2] : s_defaultSeparator;
                TokenInfo periodicFormat = 0 < cFormats ? _tokens[cFormats - 1] : s_defaultFormat;

                if (prefix != null)
                {
                    prefix.AssertSeparator(true);
                    sb.Append(prefix.formatString, prefix.startIdx, prefix.length);
                }

                int valCount = val.Count;
                for (int i = 0; i < valCount; i++)
                {
                    int formatIndex = i * 2;
                    bool haveFormat = formatIndex < cFormats;

                    if (i > 0)
                    {
                        TokenInfo thisSeparator = haveFormat ? _tokens[formatIndex + 0] : periodicSeparator;
                        thisSeparator.AssertSeparator(true);
                        sb.Append(thisSeparator.formatString, thisSeparator.startIdx, thisSeparator.length);
                    }

                    TokenInfo thisFormat = haveFormat ? _tokens[formatIndex + 1] : periodicFormat;
                    thisFormat.AssertSeparator(false);
                    FormatItem(sb, val[i], thisFormat.startChar, thisFormat.length);
                }

                if (suffix != null)
                {
                    suffix.AssertSeparator(true);
                    sb.Append(suffix.formatString, suffix.startIdx, suffix.length);
                }
            }
            return sb.ToString();
        }

        private void FormatItem(StringBuilder sb, XPathItem item, char startChar, int length)
        {
            double dblVal;

            if (item.ValueType == typeof(int))
            {
                dblVal = (double)item.ValueAsInt;
            }
            else
            {
                Debug.Assert(item.ValueType == typeof(double), "Item must be either of type int, or double");
                dblVal = XsltFunctions.Round(item.ValueAsDouble);
            }

            Debug.Assert(1 <= dblVal && dblVal < double.PositiveInfinity);
            char zero = '0';

            switch (startChar)
            {
                case '1':
                    break;
                case 'A':
                case 'a':
                    if (dblVal <= MaxAlphabeticValue)
                    {
                        ConvertToAlphabetic(sb, dblVal, startChar, 26);
                        return;
                    }
                    break;
                case 'I':
                case 'i':
                    if (dblVal <= MaxRomanValue)
                    {
                        ConvertToRoman(sb, dblVal, /*upperCase:*/ startChar == 'I');
                        return;
                    }
                    break;
                default:
                    Debug.Assert(CharUtil.IsDecimalDigitOne(startChar), $"Unexpected startChar: {startChar}");
                    zero = (char)(startChar - 1);
                    break;
            }

            sb.Append(ConvertToDecimal(dblVal, length, zero, _groupingSeparator, _groupingSize));
        }

        private static string ConvertToDecimal(double val, int minLen, char zero, string groupSeparator, int groupSize)
        {
            Debug.Assert(val >= 0 && val == Math.Round(val), "ConvertToArabic operates on non-negative integer numbers only");
            string str = XPathConvert.DoubleToString(val);
            int shift = zero - '0';

            // Figure out new string length without separators
            int oldLen = str.Length;
            int newLen = Math.Max(oldLen, minLen);

            // Calculate length of string with separators
            if (groupSize != 0)
            {
                Debug.Assert(groupSeparator.Length == 1);
                checked { newLen += (newLen - 1) / groupSize; }
            }

            // If the new number of characters equals the old one, no changes need to be made
            if (newLen == oldLen && shift == 0)
            {
                return str;
            }

            // If grouping is not needed, add zero padding only
            if (groupSize == 0 && shift == 0)
            {
                return str.PadLeft(newLen, zero);
            }

            // Add both grouping separators and zero padding to the string representation of a number
#if true
            unsafe
            {
                char* result = stackalloc char[newLen];
                char separator = (groupSeparator.Length > 0) ? groupSeparator[0] : ' ';

                fixed (char* pin = str)
                {
                    char* pOldEnd = pin + oldLen - 1;
                    char* pNewEnd = result + newLen - 1;
                    int cnt = groupSize;

                    while (true)
                    {
                        // Move digit to its new location (zero if we've run out of digits)
                        *pNewEnd-- = (pOldEnd >= pin) ? (char)(*pOldEnd-- + shift) : zero;
                        if (pNewEnd < result)
                        {
                            break;
                        }
                        if (/*groupSize > 0 && */--cnt == 0)
                        {
                            // Every groupSize digits insert the separator
                            *pNewEnd-- = separator;
                            cnt = groupSize;
                            Debug.Assert(pNewEnd >= result, "Separator cannot be the first character");
                        }
                    }
                }
                return new string(result, 0, newLen);
            }
#else
            // Safe version is about 20% slower after NGEN
            char[] result = new char[newLen];
            char separator = (groupSeparator.Length > 0) ? groupSeparator[0] : ' ';

            int oldEnd = oldLen - 1;
            int newEnd = newLen - 1;
            int cnt = groupSize;

            while (true) {
                // Move digit to its new location (zero if we've run out of digits)
                result[newEnd--] = (oldEnd >= 0) ? (char)(str[oldEnd--] + shift) : zero;
                if (newEnd < 0) {
                    break;
                }
                if (/*groupSize > 0 && */--cnt == 0) {
                    // Every groupSize digits insert the separator
                    result[newEnd--] = separator;
                    cnt = groupSize;
                    Debug.Assert(newEnd >= 0, "Separator cannot be the first character");
                }
            }
            return new string(result, 0, newLen);
#endif
        }
    }
}
