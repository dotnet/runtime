// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Xml
{
    internal static class XmlConvertEx
    {
        private static XmlCharType xmlCharType = XmlCharType.Instance;
        private static readonly char[] WhitespaceChars = new char[] { ' ', '\t', '\n', '\r' };

        #region XPath
        public static double ToXPathDouble(object o)
        {
            string str = o as string;
            if (str != null)
            {
                str = TrimString(str);
                if (str.Length != 0 && str[0] != '+')
                {
                    double d;
                    if (Double.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out d))
                    {
                        return d;
                    }
                }
                return Double.NaN;
            }
            if (o is double)
            {
                return (double)o;
            }
            if (o is bool)
            {
                return ((bool)o) ? 1.0 : 0.0;
            }
            try
            {
                return Convert.ToDouble(o, NumberFormatInfo.InvariantInfo);
            }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (ArgumentNullException) { }
            return Double.NaN;
        }

        // Rounding to closest integer.
        // If there are few such integers then we choose the one closest to positive infinity
        public static double XPathRound(double value)
        {
            double temp = Math.Round(value);
            return (value - temp == 0.5) ? temp + 1 : temp;
        }
        #endregion

        public static string TrimString(string value)
        {
            return value.Trim(WhitespaceChars);
        }

        public static string[] SplitString(string value)
        {
            return value.Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries);
        }

        public static Uri ToUri(string s)
        {
            if (!string.IsNullOrEmpty(s))
            { //string.Empty is a valid uri but not "   "
                s = TrimString(s);
                if (s.Length == 0 || s.IndexOf("##", StringComparison.Ordinal) != -1)
                {
                    throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Uri"));
                }
            }
            Uri uri;
            if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out uri))
            {
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Uri"));
            }
            return uri;
        }

        public static string EscapeValueForDebuggerDisplay(string value)
        {
            System.Text.StringBuilder sb = null;
            int i = 0;
            int start = 0;
            while (i < value.Length)
            {
                char ch = value[i];
                if ((int)ch < 0x20 || ch == '"')
                {
                    if (sb == null)
                    {
                        sb = new System.Text.StringBuilder(value.Length + 4);
                    }
                    if (i - start > 0)
                    {
                        sb.Append(value, start, i - start);
                    }
                    start = i + 1;
                    switch (ch)
                    {
                        case '"':
                            sb.Append("\\\"");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
                i++;
            }
            if (sb == null)
            {
                return value;
            }
            if (i - start > 0)
            {
                sb.Append(value, start, i - start);
            }
            return sb.ToString();
        }

        public static Exception CreateInvalidSurrogatePairException(char low, char hi)
        {
            return CreateInvalidSurrogatePairException(low, hi, ExceptionType.ArgumentException);
        }

        private static Exception CreateInvalidSurrogatePairException(char low, char hi, ExceptionType exceptionType)
        {
            return CreateInvalidSurrogatePairException(low, hi, exceptionType, 0, 0);
        }

        private static Exception CreateInvalidSurrogatePairException(char low, char hi, ExceptionType exceptionType, int lineNo, int linePos)
        {
            string[] args = new string[] {
                ((uint)hi).ToString( "X", CultureInfo.InvariantCulture ),
                ((uint)low).ToString( "X", CultureInfo.InvariantCulture )
            };
            return CreateException(SR.Xml_InvalidSurrogatePairWithArgs, args, exceptionType, lineNo, linePos);
        }

        private static Exception CreateException(string res, string[] args, ExceptionType exceptionType, int lineNo, int linePos)
        {
            switch (exceptionType)
            {
                case ExceptionType.ArgumentException:
                    return new ArgumentException(SR.Format(res, args));
                case ExceptionType.XmlException:
                default:
                    return new System.Xml.XmlException(SR.Format(res, args), null, lineNo, linePos);
            }
        }

        private static Exception CreateException(string res, string arg, ExceptionType exceptionType, int lineNo, int linePos)
        {
            return CreateException(res, new string[] { arg }, exceptionType, lineNo, linePos);
        }

        private static Exception CreateException(string res, ExceptionType exceptionType, int lineNo, int linePos)
        {
            return CreateException(res, Array.Empty<string>(), exceptionType, lineNo, linePos);
        }

        public static Exception CreateInvalidCharException(string data, int invCharPos, ExceptionType exceptionType)
        {
            return CreateException(SR.Xml_InvalidCharacter, XmlExceptionHelper.BuildCharExceptionArgs(data, invCharPos), exceptionType, 0, invCharPos + 1);
        }

        public static Exception CreateInvalidHighSurrogateCharException(char hi)
        {
            return CreateInvalidHighSurrogateCharException(hi, ExceptionType.ArgumentException);
        }

        private static Exception CreateInvalidHighSurrogateCharException(char hi, ExceptionType exceptionType)
        {
            return CreateInvalidHighSurrogateCharException(hi, exceptionType, 0, 0);
        }

        private static Exception CreateInvalidHighSurrogateCharException(char hi, ExceptionType exceptionType, int lineNo, int linePos)
        {
            return CreateException(SR.Xml_InvalidSurrogateHighChar, ((uint)hi).ToString("X", CultureInfo.InvariantCulture), exceptionType, lineNo, linePos);
        }

        public static void VerifyCharData(string data, ExceptionType exceptionType)
        {
            VerifyCharData(data, exceptionType, exceptionType);
        }

        public static unsafe void VerifyCharData(string data, ExceptionType invCharExceptionType, ExceptionType invSurrogateExceptionType)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            int i = 0;
            int len = data.Length;
            for (; ;)
            {
                while (i < len && (xmlCharType.charProperties[data[i]] & XmlCharType.fCharData) != 0)
                {
                    i++;
                }
                if (i == len)
                {
                    return;
                }

                char ch = data[i];
                if (XmlCharType.IsHighSurrogate(ch))
                {
                    if (i + 1 == len)
                    {
                        throw CreateException(SR.Xml_InvalidSurrogateMissingLowChar, invSurrogateExceptionType, 0, i + 1);
                    }
                    ch = data[i + 1];
                    if (XmlCharType.IsLowSurrogate(ch))
                    {
                        i += 2;
                        continue;
                    }
                    else
                    {
                        throw CreateInvalidSurrogatePairException(data[i + 1], data[i], invSurrogateExceptionType, 0, i + 1);
                    }
                }
                throw CreateInvalidCharException(data, i, invCharExceptionType);
            }
        }

        public static string VerifyQName(string name, ExceptionType exceptionType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            int colonPosition = -1;

            int endPos = ValidateNames.ParseQName(name, 0, out colonPosition);
            if (endPos != name.Length)
            {
                throw CreateException(SR.Xml_BadNameChar, XmlExceptionHelper.BuildCharExceptionArgs(name, endPos), exceptionType, 0, endPos + 1);
            }
            return name;
        }

        // Trim beginning of a string using XML whitespace characters
        public static string TrimStringStart(string value)
        {
            return value.TrimStart(WhitespaceChars);
        }

        // Trim end of a string using XML whitespace characters
        public static string TrimStringEnd(string value)
        {
            return value.TrimEnd(WhitespaceChars);
        }
    }
}
