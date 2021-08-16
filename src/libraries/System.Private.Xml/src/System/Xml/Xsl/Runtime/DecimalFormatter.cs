// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Xml.Xsl.Runtime
{
    internal sealed class DecimalFormat
    {
        public NumberFormatInfo info;
        public char digit;
        public char zeroDigit;
        public char patternSeparator;

        internal DecimalFormat(NumberFormatInfo info, char digit, char zeroDigit, char patternSeparator)
        {
            this.info = info;
            this.digit = digit;
            this.zeroDigit = zeroDigit;
            this.patternSeparator = patternSeparator;
        }
    }

    internal sealed class DecimalFormatter
    {
        private readonly NumberFormatInfo _posFormatInfo;
        private readonly NumberFormatInfo _negFormatInfo;
        private readonly string _posFormat;
        private readonly string _negFormat;
        private readonly char _zeroDigit;

        // These characters have special meaning for CLR and must be escaped
        // <spec>https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings</spec>
        private const string ClrSpecialChars = "0#.,%\u2030Ee\\'\";";

        // This character is used to escape literal (passive) digits '0'..'9'
        private const char EscChar = '\a';

        public DecimalFormatter(string formatPicture, DecimalFormat decimalFormat)
        {
            Debug.Assert(formatPicture != null && decimalFormat != null);
            if (formatPicture.Length == 0)
            {
                throw XsltException.Create(SR.Xslt_InvalidFormat);
            }

            _zeroDigit = decimalFormat.zeroDigit;
            _posFormatInfo = (NumberFormatInfo)decimalFormat.info.Clone();
            StringBuilder temp = new StringBuilder();

            bool integer = true;
            bool sawPattern = false, sawZeroDigit = false, sawDigit = false, sawDecimalSeparator = false;
            bool digitOrZeroDigit = false;
            char decimalSeparator = _posFormatInfo.NumberDecimalSeparator[0];
            char groupSeparator = _posFormatInfo.NumberGroupSeparator[0];
            char percentSymbol = _posFormatInfo.PercentSymbol[0];
            char perMilleSymbol = _posFormatInfo.PerMilleSymbol[0];

            int commaIndex = 0;
            int groupingSize = 0;
            int decimalIndex = -1;
            int lastDigitIndex = -1;

            for (int i = 0; i < formatPicture.Length; i++)
            {
                char ch = formatPicture[i];

                if (ch == decimalFormat.digit)
                {
                    if (sawZeroDigit && integer)
                    {
                        throw XsltException.Create(SR.Xslt_InvalidFormat1, formatPicture);
                    }
                    lastDigitIndex = temp.Length;
                    sawDigit = digitOrZeroDigit = true;
                    temp.Append('#');
                    continue;
                }
                if (ch == decimalFormat.zeroDigit)
                {
                    if (sawDigit && !integer)
                    {
                        throw XsltException.Create(SR.Xslt_InvalidFormat2, formatPicture);
                    }
                    lastDigitIndex = temp.Length;
                    sawZeroDigit = digitOrZeroDigit = true;
                    temp.Append('0');
                    continue;
                }
                if (ch == decimalFormat.patternSeparator)
                {
                    if (!digitOrZeroDigit)
                    {
                        throw XsltException.Create(SR.Xslt_InvalidFormat8);
                    }
                    if (sawPattern)
                    {
                        throw XsltException.Create(SR.Xslt_InvalidFormat3, formatPicture);
                    }
                    sawPattern = true;

                    if (decimalIndex < 0)
                    {
                        decimalIndex = lastDigitIndex + 1;
                    }
                    groupingSize = RemoveTrailingComma(temp, commaIndex, decimalIndex);

                    if (groupingSize > 9)
                    {
                        groupingSize = 0;
                    }
                    _posFormatInfo.NumberGroupSizes = new int[] { groupingSize };
                    if (!sawDecimalSeparator)
                    {
                        _posFormatInfo.NumberDecimalDigits = 0;
                    }

                    _posFormat = temp.ToString();

                    temp.Length = 0;
                    decimalIndex = -1;
                    lastDigitIndex = -1;
                    commaIndex = 0;
                    sawDigit = sawZeroDigit = digitOrZeroDigit = false;
                    sawDecimalSeparator = false;
                    integer = true;
                    _negFormatInfo = (NumberFormatInfo)decimalFormat.info.Clone();
                    _negFormatInfo.NegativeSign = string.Empty;
                    continue;
                }
                if (ch == decimalSeparator)
                {
                    if (sawDecimalSeparator)
                    {
                        throw XsltException.Create(SR.Xslt_InvalidFormat5, formatPicture);
                    }
                    decimalIndex = temp.Length;
                    sawDecimalSeparator = true;
                    sawDigit = sawZeroDigit = integer = false;
                    temp.Append('.');
                    continue;
                }
                if (ch == groupSeparator)
                {
                    commaIndex = temp.Length;
                    lastDigitIndex = commaIndex;
                    temp.Append(',');
                    continue;
                }
                if (ch == percentSymbol)
                {
                    temp.Append('%');
                    continue;
                }
                if (ch == perMilleSymbol)
                {
                    temp.Append('\u2030');
                    continue;
                }
                if (ch == '\'')
                {
                    int pos = formatPicture.IndexOf('\'', i + 1);
                    if (pos < 0)
                    {
                        pos = formatPicture.Length - 1;
                    }
                    temp.Append(formatPicture, i, pos - i + 1);
                    i = pos;
                    continue;
                }
                // Escape literal digits with EscChar, double literal EscChar
                if ('0' <= ch && ch <= '9' || ch == EscChar)
                {
                    if (decimalFormat.zeroDigit != '0')
                    {
                        temp.Append(EscChar);
                    }
                }
                // Escape characters having special meaning for CLR
                if (ClrSpecialChars.Contains(ch))
                {
                    temp.Append('\\');
                }
                temp.Append(ch);
            }

            if (!digitOrZeroDigit)
            {
                throw XsltException.Create(SR.Xslt_InvalidFormat8);
            }
            NumberFormatInfo formatInfo = sawPattern ? _negFormatInfo : _posFormatInfo;

            if (decimalIndex < 0)
            {
                decimalIndex = lastDigitIndex + 1;
            }
            groupingSize = RemoveTrailingComma(temp, commaIndex, decimalIndex);
            if (groupingSize > 9)
            {
                groupingSize = 0;
            }
            formatInfo.NumberGroupSizes = new int[] { groupingSize };
            if (!sawDecimalSeparator)
            {
                formatInfo.NumberDecimalDigits = 0;
            }

            if (sawPattern)
            {
                _negFormat = temp.ToString();
            }
            else
            {
                _posFormat = temp.ToString();
            }
        }

        private static int RemoveTrailingComma(StringBuilder builder, int commaIndex, int decimalIndex)
        {
            if (commaIndex > 0 && commaIndex == (decimalIndex - 1))
            {
                builder.Remove(decimalIndex - 1, 1);
            }
            else if (decimalIndex > commaIndex)
            {
                return decimalIndex - commaIndex - 1;
            }
            return 0;
        }

        public string Format(double value)
        {
            NumberFormatInfo formatInfo;
            string subPicture;

            if (value < 0 && _negFormatInfo != null)
            {
                formatInfo = _negFormatInfo;
                subPicture = _negFormat;
            }
            else
            {
                formatInfo = _posFormatInfo;
                subPicture = _posFormat;
            }

            string result = value.ToString(subPicture, formatInfo);

            if (_zeroDigit != '0')
            {
                StringBuilder builder = new StringBuilder(result.Length);
                int shift = _zeroDigit - '0';
                for (int i = 0; i < result.Length; i++)
                {
                    char ch = result[i];
                    if ((uint)(ch - '0') <= 9)
                    {
                        ch += (char)shift;
                    }
                    else if (ch == EscChar)
                    {
                        // This is an escaped literal digit or EscChar, thus unescape it. We make use
                        // of the fact that no extra EscChar could be inserted by value.ToString().
                        Debug.Assert(i + 1 < result.Length);
                        ch = result[++i];
                        Debug.Assert('0' <= ch && ch <= '9' || ch == EscChar);
                    }
                    builder.Append(ch);
                }
                result = builder.ToString();
            }
            return result;
        }

        public static string Format(double value, string formatPicture, DecimalFormat decimalFormat)
        {
            return new DecimalFormatter(formatPicture, decimalFormat).Format(value);
        }
    }
}
