// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Xslt;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Xml.Xsl.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class XsltFunctions
    {
        private static readonly CompareInfo s_compareInfo = CultureInfo.InvariantCulture.CompareInfo;


        //------------------------------------------------
        // Xslt/XPath functions
        //------------------------------------------------

        public static bool StartsWith(string s1, string s2)
        {
            //return collation.IsPrefix(s1, s2);
            return s1.Length >= s2.Length && string.CompareOrdinal(s1, 0, s2, 0, s2.Length) == 0;
        }

        public static bool Contains(string s1, string s2)
        {
            //return collation.IndexOf(s1, s2) >= 0;
            return s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal) >= 0;
        }

        public static string SubstringBefore(string s1, string s2)
        {
            if (s2.Length == 0) { return s2; }
            //int idx = collation.IndexOf(s1, s2);
            int idx = s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
            return (idx < 1) ? string.Empty : s1.Substring(0, idx);
        }

        public static string SubstringAfter(string s1, string s2)
        {
            if (s2.Length == 0) { return s1; }
            //int idx = collation.IndexOf(s1, s2);
            int idx = s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
            return (idx < 0) ? string.Empty : s1.Substring(idx + s2.Length);
        }

        public static string Substring(string value, double startIndex)
        {
            startIndex = Round(startIndex);
            if (startIndex <= 0)
            {
                return value;
            }
            else if (startIndex <= value.Length)
            {
                return value.Substring((int)startIndex - 1);
            }
            else
            {
                Debug.Assert(value.Length < startIndex || Double.IsNaN(startIndex));
                return string.Empty;
            }
        }

        public static string Substring(string value, double startIndex, double length)
        {
            startIndex = Round(startIndex) - 1;             // start index
            if (startIndex >= value.Length)
            {
                return string.Empty;
            }

            double endIndex = startIndex + Round(length);   // end index
            startIndex = (startIndex <= 0) ? 0 : startIndex;

            if (startIndex < endIndex)
            {
                if (endIndex > value.Length)
                {
                    endIndex = value.Length;
                }
                Debug.Assert(0 <= startIndex && startIndex <= endIndex && endIndex <= value.Length);
                return value.Substring((int)startIndex, (int)(endIndex - startIndex));
            }
            else
            {
                Debug.Assert(endIndex <= startIndex || Double.IsNaN(endIndex));
                return string.Empty;
            }
        }

        public static string NormalizeSpace(string value)
        {
            XmlCharType xmlCharType = XmlCharType.Instance;
            StringBuilder sb = null;
            int idx, idxStart = 0, idxSpace = 0;

            for (idx = 0; idx < value.Length; idx++)
            {
                if (xmlCharType.IsWhiteSpace(value[idx]))
                {
                    if (idx == idxStart)
                    {
                        // Previous character was a whitespace character, so discard this character
                        idxStart++;
                    }
                    else if (value[idx] != ' ' || idxSpace == idx)
                    {
                        // Space was previous character or this is a non-space character
                        if (sb == null)
                            sb = new StringBuilder(value.Length);
                        else
                            sb.Append(' ');

                        // Copy non-space characters into string builder
                        if (idxSpace == idx)
                            sb.Append(value, idxStart, idx - idxStart - 1);
                        else
                            sb.Append(value, idxStart, idx - idxStart);

                        idxStart = idx + 1;
                    }
                    else
                    {
                        // Single whitespace character doesn't cause normalization, but mark its position
                        idxSpace = idx + 1;
                    }
                }
            }

            if (sb == null)
            {
                // Check for string that is entirely composed of whitespace
                if (idxStart == idx) return string.Empty;

                // If string does not end with a space, then it must already be normalized
                if (idxStart == 0 && idxSpace != idx) return value;

                sb = new StringBuilder(value.Length);
            }
            else if (idx != idxStart)
            {
                sb.Append(' ');
            }

            // Copy non-space characters into string builder
            if (idxSpace == idx)
                sb.Append(value, idxStart, idx - idxStart - 1);
            else
                sb.Append(value, idxStart, idx - idxStart);

            return sb.ToString();
        }

        public static string Translate(string arg, string mapString, string transString)
        {
            if (mapString.Length == 0)
            {
                return arg;
            }

            StringBuilder sb = new StringBuilder(arg.Length);

            for (int i = 0; i < arg.Length; i++)
            {
                int index = mapString.IndexOf(arg[i]);
                if (index < 0)
                {
                    // Keep the character
                    sb.Append(arg[i]);
                }
                else if (index < transString.Length)
                {
                    // Replace the character
                    sb.Append(transString[index]);
                }
                else
                {
                    // Remove the character
                }
            }
            return sb.ToString();
        }

        public static bool Lang(string value, XPathNavigator context)
        {
            string lang = context.XmlLang;

            if (!lang.StartsWith(value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return (lang.Length == value.Length || lang[value.Length] == '-');
        }

        // Round value using XPath rounding rules (round towards positive infinity).
        // Values between -0.5 and -0.0 are rounded to -0.0 (negative zero).
        public static double Round(double value)
        {
            double temp = Math.Round(value);
            return (value - temp == 0.5) ? temp + 1 : temp;
        }

        // Spec: http://www.w3.org/TR/xslt.html#function-system-property
        public static XPathItem SystemProperty(XmlQualifiedName name)
        {
            if (name.Namespace == XmlReservedNs.NsXslt)
            {
                // "xsl:version" must return 1.0 as a number, see http://www.w3.org/TR/xslt20/#incompatility-without-schema
                switch (name.Name)
                {
                    case "version": return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Double), 1.0);
                    case "vendor": return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), "Microsoft");
                    case "vendor-url": return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), "http://www.microsoft.com");
                }
            }
            else if (name.Namespace == XmlReservedNs.NsMsxsl && name.Name == "version")
            {
                // msxsl:version
                return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), typeof(XsltLibrary).Assembly.ImageRuntimeVersion);
            }
            // If the property name is not recognized, return the empty string
            return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), string.Empty);
        }


        //------------------------------------------------
        // Navigator functions
        //------------------------------------------------

        public static string BaseUri(XPathNavigator navigator)
        {
            return navigator.BaseURI;
        }

        public static string OuterXml(XPathNavigator navigator)
        {
            RtfNavigator rtf = navigator as RtfNavigator;
            if (rtf == null)
            {
                return navigator.OuterXml;
            }
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.CheckCharacters = false;
            XmlWriter xw = XmlWriter.Create(sb, settings);
            rtf.CopyToWriter(xw);
            xw.Close();
            return sb.ToString();
        }


        //------------------------------------------------
        // EXslt Functions
        //------------------------------------------------

        public static string EXslObjectType(IList<XPathItem> value)
        {
            if (value.Count != 1)
            {
                XsltLibrary.CheckXsltValue(value);
                return "node-set";
            }

            XPathItem item = value[0];
            if (item is RtfNavigator)
            {
                return "RTF";
            }
            else if (item.IsNode)
            {
                Debug.Assert(item is XPathNavigator);
                return "node-set";
            }

            object o = item.TypedValue;
            if (o is string)
            {
                return "string";
            }
            else if (o is double)
            {
                return "number";
            }
            else if (o is bool)
            {
                return "boolean";
            }
            else
            {
                Debug.Fail("Unexpected type: " + o.GetType().ToString());
                return "external";
            }
        }


        //------------------------------------------------
        // Msxml Extension Functions
        //------------------------------------------------

        public static double MSNumber(IList<XPathItem> value)
        {
            XsltLibrary.CheckXsltValue(value);
            if (value.Count == 0)
            {
                return Double.NaN;
            }
            XPathItem item = value[0];

            string stringValue;

            if (item.IsNode)
            {
                stringValue = item.Value;
            }
            else
            {
                Type itemType = item.ValueType;
                if (itemType == XsltConvert.StringType)
                {
                    stringValue = item.Value;
                }
                else if (itemType == XsltConvert.DoubleType)
                {
                    return item.ValueAsDouble;
                }
                else
                {
                    Debug.Assert(itemType == XsltConvert.BooleanType, "Unexpected type of atomic value " + itemType.ToString());
                    return item.ValueAsBoolean ? 1d : 0d;
                }
            }

            Debug.Assert(stringValue != null);
            double d;
            if (XmlConvert.TryToDouble(stringValue, out d) != null)
            {
                d = double.NaN;
            }
            return d;
        }

        // string ms:format-date(string datetime[, string format[, string language]])
        // string ms:format-time(string datetime[, string format[, string language]])
        //
        // Format xsd:dateTime as a date/time string for a given language using a given format string.
        // * Datetime contains a lexical representation of xsd:dateTime. If datetime is not valid, the
        //   empty string is returned.
        // * Format specifies a format string in the same way as for GetDateFormat/GetTimeFormat system
        //   functions. If format is the empty string or not passed, the default date/time format for the
        //   given culture is used.
        // * Language specifies a culture used for formatting. If language is the empty string or not
        //   passed, the current culture is used. If language is not recognized, a runtime error happens.
        public static string MSFormatDateTime(string dateTime, string format, string lang, bool isDate)
        {
            try
            {
                string locale = GetCultureInfo(lang).Name;

                XsdDateTime xdt;
                if (!XsdDateTime.TryParse(dateTime, XsdDateTimeFlags.AllXsd | XsdDateTimeFlags.XdrDateTime | XsdDateTimeFlags.XdrTimeNoTz, out xdt))
                {
                    return string.Empty;
                }
                DateTime dt = xdt.ToZulu();

                // If format is the empty string or not specified, use the default format for the given locale
                if (format.Length == 0)
                {
                    format = null;
                }
                return dt.ToString(format, new CultureInfo(locale));
            }
            catch (ArgumentException)
            { // Operations with DateTime can throw this exception eventualy
                return string.Empty;
            }
        }

        public static double MSStringCompare(string s1, string s2, string lang, string options)
        {
            CultureInfo cultinfo = GetCultureInfo(lang);
            CompareOptions opts = CompareOptions.None;
            bool upperFirst = false;
            for (int idx = 0; idx < options.Length; idx++)
            {
                switch (options[idx])
                {
                    case 'i':
                        opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;
                        break;
                    case 'u':
                        upperFirst = true;
                        break;
                    default:
                        upperFirst = true;
                        opts = CompareOptions.IgnoreCase;
                        break;
                }
            }

            if (upperFirst)
            {
                if (opts != CompareOptions.None)
                {
                    throw new XslTransformException(SR.Xslt_InvalidCompareOption, options);
                }
                opts = CompareOptions.IgnoreCase;
            }

            int result = cultinfo.CompareInfo.Compare(s1, s2, opts);
            if (upperFirst && result == 0)
            {
                result = -cultinfo.CompareInfo.Compare(s1, s2, CompareOptions.None);
            }
            return result;
        }

        public static string MSUtc(string dateTime)
        {
            XsdDateTime xdt;
            DateTime dt;
            try
            {
                if (!XsdDateTime.TryParse(dateTime, XsdDateTimeFlags.AllXsd | XsdDateTimeFlags.XdrDateTime | XsdDateTimeFlags.XdrTimeNoTz, out xdt))
                {
                    return string.Empty;
                }
                dt = xdt.ToZulu();
            }
            catch (ArgumentException)
            { // Operations with DateTime can throw this exception eventualy
                return string.Empty;
            }
            char[] text = "----------T00:00:00.000".ToCharArray();
            //            "YYYY-MM-DDTHH:NN:SS.III"
            //             0         1         2
            //             01234567890123456789012
            switch (xdt.TypeCode)
            {
                case XmlTypeCode.DateTime:
                    PrintDate(text, dt);
                    PrintTime(text, dt);
                    break;
                case XmlTypeCode.Time:
                    PrintTime(text, dt);
                    break;
                case XmlTypeCode.Date:
                    PrintDate(text, dt);
                    break;
                case XmlTypeCode.GYearMonth:
                    PrintYear(text, dt.Year);
                    ShortToCharArray(text, 5, dt.Month);
                    break;
                case XmlTypeCode.GYear:
                    PrintYear(text, dt.Year);
                    break;
                case XmlTypeCode.GMonthDay:
                    ShortToCharArray(text, 5, dt.Month);
                    ShortToCharArray(text, 8, dt.Day);
                    break;
                case XmlTypeCode.GDay:
                    ShortToCharArray(text, 8, dt.Day);
                    break;
                case XmlTypeCode.GMonth:
                    ShortToCharArray(text, 5, dt.Month);
                    break;
            }
            return new String(text);
        }

        public static string MSLocalName(string name)
        {
            int colonOffset;
            int len = ValidateNames.ParseQName(name, 0, out colonOffset);

            if (len != name.Length)
            {
                return string.Empty;
            }
            if (colonOffset == 0)
            {
                return name;
            }
            else
            {
                return name.Substring(colonOffset + 1);
            }
        }

        public static string MSNamespaceUri(string name, XPathNavigator currentNode)
        {
            int colonOffset;
            int len = ValidateNames.ParseQName(name, 0, out colonOffset);

            if (len != name.Length)
            {
                return string.Empty;
            }
            string prefix = name.Substring(0, colonOffset);
            if (prefix == "xmlns")
            {
                return string.Empty;
            }
            string ns = currentNode.LookupNamespace(prefix);
            if (ns != null)
            {
                return ns;
            }
            if (prefix == "xml")
            {
                return XmlReservedNs.NsXml;
            }
            return string.Empty;
        }


        //------------------------------------------------
        // Helper Functions
        //------------------------------------------------

        private static CultureInfo GetCultureInfo(string lang)
        {
            Debug.Assert(lang != null);
            if (lang.Length == 0)
            {
                return CultureInfo.CurrentCulture;
            }
            else
            {
                try
                {
                    return new CultureInfo(lang);
                }
                catch (System.ArgumentException)
                {
                    throw new XslTransformException(SR.Xslt_InvalidLanguage, lang);
                }
            }
        }

        private static void PrintDate(char[] text, DateTime dt)
        {
            PrintYear(text, dt.Year);
            ShortToCharArray(text, 5, dt.Month);
            ShortToCharArray(text, 8, dt.Day);
        }

        private static void PrintTime(char[] text, DateTime dt)
        {
            ShortToCharArray(text, 11, dt.Hour);
            ShortToCharArray(text, 14, dt.Minute);
            ShortToCharArray(text, 17, dt.Second);
            PrintMsec(text, dt.Millisecond);
        }

        private static void PrintYear(char[] text, int value)
        {
            text[0] = (char)((value / 1000) % 10 + '0');
            text[1] = (char)((value / 100) % 10 + '0');
            text[2] = (char)((value / 10) % 10 + '0');
            text[3] = (char)((value / 1) % 10 + '0');
        }

        private static void PrintMsec(char[] text, int value)
        {
            if (value == 0)
            {
                return;
            }
            text[20] = (char)((value / 100) % 10 + '0');
            text[21] = (char)((value / 10) % 10 + '0');
            text[22] = (char)((value / 1) % 10 + '0');
        }

        private static void ShortToCharArray(char[] text, int start, int value)
        {
            text[start] = (char)(value / 10 + '0');
            text[start + 1] = (char)(value % 10 + '0');
        }
    }
}
