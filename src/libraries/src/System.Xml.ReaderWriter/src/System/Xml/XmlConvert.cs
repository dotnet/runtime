// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Globalization;
using System.Xml.Schema;
using System.Diagnostics;
using System.Collections;
using System.Text.RegularExpressions;

namespace System.Xml
{
    // ExceptionType enum is used inside XmlConvert to specify which type of exception should be thrown at some of the verification and exception creating methods
    internal enum ExceptionType
    {
        ArgumentException,
        XmlException,
    }

    // Options for serializing and deserializing DateTime
    public enum XmlDateTimeSerializationMode
    {
        Local,
        Utc,
        Unspecified,
        RoundtripKind,
    }

    /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert"]/*' />
    /// <devdoc>
    ///    Encodes and decodes XML names according to
    ///    the "Encoding of arbitrary Unicode Characters in XML Names" specification.
    /// </devdoc>
    public static class XmlConvert
    {
        //
        // Static fields with implicit initialization
        //
        private static XmlCharType s_xmlCharType = XmlCharType.Instance;


        /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.EncodeName"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts names, such
        ///       as DataTable or
        ///       DataColumn names, that contain characters that are not permitted in
        ///       XML names to valid names.</para>
        /// </devdoc>
        public static string EncodeName(string name)
        {
            return EncodeName(name, true/*Name_not_NmToken*/, false/*Local?*/);
        }

        /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.EncodeNmToken"]/*' />
        /// <devdoc>
        ///    <para> Verifies the name is valid
        ///       according to production [7] in the XML spec.</para>
        /// </devdoc>
        public static string EncodeNmToken(string name)
        {
            return EncodeName(name, false/*Name_not_NmToken*/, false/*Local?*/);
        }

        /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.EncodeLocalName"]/*' />
        /// <devdoc>
        ///    <para>Converts names, such as DataTable or DataColumn names, that contain
        ///       characters that are not permitted in XML names to valid names.</para>
        /// </devdoc>
        public static string EncodeLocalName(string name)
        {
            return EncodeName(name, true/*Name_not_NmToken*/, true/*Local?*/);
        }

        /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.DecodeName"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Transforms an XML name into an object name (such as DataTable or DataColumn).</para>
        /// </devdoc>

        public static string DecodeName(string name)
        {
            if (name == null || name.Length == 0)
                return name;

            StringBuilder bufBld = null;

            int length = name.Length;
            int copyPosition = 0;

            int underscorePos = name.IndexOf('_');

            MatchCollection mc = null;
            IEnumerator en = null;
            if (underscorePos >= 0)
            {
                if (s_decodeCharPattern == null)
                {
                    s_decodeCharPattern = new Regex("_[Xx]([0-9a-fA-F]{4}|[0-9a-fA-F]{8})_");
                }
                mc = s_decodeCharPattern.Matches(name, underscorePos);
                en = mc.GetEnumerator();
            }
            else
            {
                return name;
            }
            int matchPos = -1;
            if (en != null && en.MoveNext())
            {
                Match m = (Match)en.Current;
                matchPos = m.Index;
            }

            for (int position = 0; position < length - s_encodedCharLength + 1; position++)
            {
                if (position == matchPos)
                {
                    if (en.MoveNext())
                    {
                        Match m = (Match)en.Current;
                        matchPos = m.Index;
                    }

                    if (bufBld == null)
                    {
                        bufBld = new StringBuilder(length + 20);
                    }
                    bufBld.Append(name, copyPosition, position - copyPosition);

                    if (name[position + 6] != '_')
                    { //_x1234_
                        Int32 u =
                            FromHex(name[position + 2]) * 0x10000000 +
                            FromHex(name[position + 3]) * 0x1000000 +
                            FromHex(name[position + 4]) * 0x100000 +
                            FromHex(name[position + 5]) * 0x10000 +

                            FromHex(name[position + 6]) * 0x1000 +
                            FromHex(name[position + 7]) * 0x100 +
                            FromHex(name[position + 8]) * 0x10 +
                            FromHex(name[position + 9]);

                        if (u >= 0x00010000)
                        {
                            if (u <= 0x0010ffff)
                            { //convert to two chars
                                copyPosition = position + s_encodedCharLength + 4;
                                char lowChar, highChar;
                                XmlCharType.SplitSurrogateChar(u, out lowChar, out highChar);
                                bufBld.Append(highChar);
                                bufBld.Append(lowChar);
                            }
                            //else bad ucs-4 char dont convert
                        }
                        else
                        { //convert to single char
                            copyPosition = position + s_encodedCharLength + 4;
                            bufBld.Append((char)u);
                        }
                        position += s_encodedCharLength - 1 + 4; //just skip
                    }
                    else
                    {
                        copyPosition = position + s_encodedCharLength;
                        bufBld.Append((char)(
                            FromHex(name[position + 2]) * 0x1000 +
                            FromHex(name[position + 3]) * 0x100 +
                            FromHex(name[position + 4]) * 0x10 +
                            FromHex(name[position + 5])));
                        position += s_encodedCharLength - 1;
                    }
                }
            }
            if (copyPosition == 0)
            {
                return name;
            }
            else
            {
                if (copyPosition < length)
                {
                    bufBld.Append(name, copyPosition, length - copyPosition);
                }
                return bufBld.ToString();
            }
        }

        private static string EncodeName(string name, /*Name_not_NmToken*/ bool first, bool local)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            StringBuilder bufBld = null;
            int length = name.Length;
            int copyPosition = 0;
            int position = 0;

            int underscorePos = name.IndexOf('_');

            MatchCollection mc = null;
            IEnumerator en = null;
            if (underscorePos >= 0)
            {
                if (s_encodeCharPattern == null)
                {
                    s_encodeCharPattern = new Regex("(?<=_)[Xx]([0-9a-fA-F]{4}|[0-9a-fA-F]{8})_");
                }
                mc = s_encodeCharPattern.Matches(name, underscorePos);
                en = mc.GetEnumerator();
            }

            int matchPos = -1;
            if (en != null && en.MoveNext())
            {
                Match m = (Match)en.Current;
                matchPos = m.Index - 1;
            }
            if (first)
            {
                if ((!s_xmlCharType.IsStartNCNameCharXml4e(name[0]) && (local || (!local && name[0] != ':'))) ||
                     matchPos == 0)
                {
                    if (bufBld == null)
                    {
                        bufBld = new StringBuilder(length + 20);
                    }
                    bufBld.Append("_x");
                    if (length > 1 && XmlCharType.IsHighSurrogate(name[0]) && XmlCharType.IsLowSurrogate(name[1]))
                    {
                        int x = name[0];
                        int y = name[1];
                        Int32 u = XmlCharType.CombineSurrogateChar(y, x);
                        bufBld.Append(u.ToString("X8", CultureInfo.InvariantCulture));
                        position++;
                        copyPosition = 2;
                    }
                    else
                    {
                        bufBld.Append(((Int32)name[0]).ToString("X4", CultureInfo.InvariantCulture));
                        copyPosition = 1;
                    }

                    bufBld.Append('_');
                    position++;

                    if (matchPos == 0)
                        if (en.MoveNext())
                        {
                            Match m = (Match)en.Current;
                            matchPos = m.Index - 1;
                        }
                }
            }
            for (; position < length; position++)
            {
                if ((local && !s_xmlCharType.IsNCNameCharXml4e(name[position])) ||
                    (!local && !s_xmlCharType.IsNameCharXml4e(name[position])) ||
                    (matchPos == position))
                {
                    if (bufBld == null)
                    {
                        bufBld = new StringBuilder(length + 20);
                    }
                    if (matchPos == position)
                        if (en.MoveNext())
                        {
                            Match m = (Match)en.Current;
                            matchPos = m.Index - 1;
                        }

                    bufBld.Append(name, copyPosition, position - copyPosition);
                    bufBld.Append("_x");
                    if ((length > position + 1) && XmlCharType.IsHighSurrogate(name[position]) && XmlCharType.IsLowSurrogate(name[position + 1]))
                    {
                        int x = name[position];
                        int y = name[position + 1];
                        Int32 u = XmlCharType.CombineSurrogateChar(y, x);
                        bufBld.Append(u.ToString("X8", CultureInfo.InvariantCulture));
                        copyPosition = position + 2;
                        position++;
                    }
                    else
                    {
                        bufBld.Append(((Int32)name[position]).ToString("X4", CultureInfo.InvariantCulture));
                        copyPosition = position + 1;
                    }
                    bufBld.Append('_');
                }
            }
            if (copyPosition == 0)
            {
                return name;
            }
            else
            {
                if (copyPosition < length)
                {
                    bufBld.Append(name, copyPosition, length - copyPosition);
                }
                return bufBld.ToString();
            }
        }

        private static readonly int s_encodedCharLength = 7; // ("_xFFFF_".Length);
        private static volatile Regex s_encodeCharPattern;
        private static volatile Regex s_decodeCharPattern;
        private static int FromHex(char digit)
        {
            return (digit <= '9')
            ? ((int)digit - (int)'0')
            : (((digit <= 'F')
                ? ((int)digit - (int)'A')
                : ((int)digit - (int)'a'))
               + 10);
        }

        internal static byte[] FromBinHexString(string s)
        {
            return FromBinHexString(s, true);
        }

        internal static byte[] FromBinHexString(string s, bool allowOddCount)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            return BinHexDecoder.Decode(s.ToCharArray(), allowOddCount);
        }

        internal static string ToBinHexString(byte[] inArray)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(nameof(inArray));
            }
            return BinHexEncoder.Encode(inArray, 0, inArray.Length);
        }

        //
        // Verification methods for strings
        // 
        /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.VerifyName"]/*' />
        /// <devdoc>
        ///    <para>
        ///    </para>
        /// </devdoc>
        public static string VerifyName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentNullException(nameof(name), SR.Xml_EmptyName);
            }

            // parse name
            int endPos = ValidateNames.ParseNameNoNamespaces(name, 0);

            if (endPos != name.Length)
            {
                // did not parse to the end -> there is invalid character at endPos
                throw CreateInvalidNameCharException(name, endPos, ExceptionType.XmlException);
            }
            return name;
        }



        internal static string VerifyQName(string name, ExceptionType exceptionType)
        {
            if (name == null || name.Length == 0)
            {
                throw new ArgumentNullException(nameof(name));
            }

            int colonPosition = -1;

            int endPos = ValidateNames.ParseQName(name, 0, out colonPosition);
            if (endPos != name.Length)
            {
                throw CreateException(SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(name, endPos), exceptionType, 0, endPos + 1);
            }
            return name;
        }

        /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.VerifyNCName"]/*' />
        /// <devdoc>
        ///    <para>
        ///    </para>
        /// </devdoc>
        public static string VerifyNCName(string name)
        {
            return VerifyNCName(name, ExceptionType.XmlException);
        }

        internal static string VerifyNCName(string name, ExceptionType exceptionType)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentNullException(nameof(name), SR.Xml_EmptyLocalName);
            }

            int end = ValidateNames.ParseNCName(name, 0);

            if (end != name.Length)
            {
                // If the string is not a valid NCName, then throw or return false
                throw CreateInvalidNameCharException(name, end, exceptionType);
            }

            return name;
        }


        /// <include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.VerifyNMTOKEN"]/*' />
        /// <devdoc>
        ///    <para>
        ///    </para>
        /// </devdoc>
        public static string VerifyNMTOKEN(string name)
        {
            return VerifyNMTOKEN(name, ExceptionType.XmlException);
        }

        internal static string VerifyNMTOKEN(string name, ExceptionType exceptionType)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw CreateException(SR.Xml_InvalidNmToken, name, exceptionType);
            }

            int endPos = ValidateNames.ParseNmtokenNoNamespaces(name, 0);

            if (endPos != name.Length)
            {
                throw CreateException(SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(name, endPos), exceptionType, 0, endPos + 1);
            }
            return name;
        }


        // Verification method for XML characters as defined in XML spec production [2] Char.
        // Throws XmlException if invalid character is found, otherwise returns the input string.
        public static string VerifyXmlChars(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            VerifyCharData(content, ExceptionType.XmlException);
            return content;
        }

        // Verification method for XML public ID characters as defined in XML spec production [13] PubidChar.
        // Throws XmlException if invalid character is found, otherwise returns the input string.
        public static string VerifyPublicId(string publicId)
        {
            if (publicId == null)
            {
                throw new ArgumentNullException(nameof(publicId));
            }

            // returns the position of invalid character or -1
            int pos = s_xmlCharType.IsPublicId(publicId);
            if (pos != -1)
            {
                throw CreateInvalidCharException(publicId, pos, ExceptionType.XmlException);
            }

            return publicId;
        }

        // Verification method for XML whitespace characters as defined in XML spec production [3] S.
        // Throws XmlException if invalid character is found, otherwise returns the input string.
        public static string VerifyWhitespace(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            // returns the position of invalid character or -1
            int pos = s_xmlCharType.IsOnlyWhitespaceWithPos(content);
            if (pos != -1)
            {
                throw new XmlException(SR.Xml_InvalidWhitespaceCharacter, XmlException.BuildCharExceptionArgs(content, pos), 0, pos + 1);
            }
            return content;
        }


#if XML10_FIFTH_EDITION
        public static bool IsStartNCNameSurrogatePair(char lowChar, char highChar)
        {
            return xmlCharType.IsNCNameSurrogateChar(lowChar, highChar);
        }
#endif


#if XML10_FIFTH_EDITION
        public static bool IsNCNameSurrogatePair(char lowChar, char highChar)
        {
            return xmlCharType.IsNCNameSurrogateChar(lowChar, highChar);
        }
#endif


        // Value convertors:
        //
        // String representation of Base types in XML (xsd) sometimes differ from
        // one common language runtime offer and for all types it has to be locale independent.
        // o -- means that XmlConvert pass through to common language runtime converter with InvariantInfo FormatInfo
        // x -- means we doing something special to make a convertion.
        //
        // From:  To: Bol Chr SBy Byt I16 U16 I32 U32 I64 U64 Sgl Dbl Dec Dat Tim Str uid
        // ------------------------------------------------------------------------------
        // Boolean                                                                 x
        // Char                                                                    o
        // SByte                                                                   o
        // Byte                                                                    o
        // Int16                                                                   o
        // UInt16                                                                  o
        // Int32                                                                   o
        // UInt32                                                                  o
        // Int64                                                                   o
        // UInt64                                                                  o
        // Single                                                                  x
        // Double                                                                  x
        // Decimal                                                                 o
        // DateTime                                                                x
        // String      x   o   o   o   o   o   o   o   o   o   o   x   x   o   o       x
        // Guid                                                                    x
        // -----------------------------------------------------------------------------

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Boolean value)
        {
            return value ? "true" : "false";
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Char value)
        {
            return value.ToString();
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Decimal value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static string ToString(SByte value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Int16 value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString5"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Int32 value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString15"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Int64 value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString6"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Byte value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString7"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static string ToString(UInt16 value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString8"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static string ToString(UInt32 value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static string ToString(UInt64 value)
        {
            return value.ToString(null, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString9"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Single value)
        {
            if (Single.IsNegativeInfinity(value)) return "-INF";
            if (Single.IsPositiveInfinity(value)) return "INF";
            if (IsNegativeZero((double)value))
            {
                return ("-0");
            }
            return value.ToString("R", NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString10"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Double value)
        {
            if (Double.IsNegativeInfinity(value)) return "-INF";
            if (Double.IsPositiveInfinity(value)) return "INF";
            if (IsNegativeZero(value))
            {
                return ("-0");
            }
            return value.ToString("R", NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString11"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(TimeSpan value)
        {
            return new XsdDuration(value).ToString();
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString14"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(DateTime value, XmlDateTimeSerializationMode dateTimeOption)
        {
            switch (dateTimeOption)
            {
                case XmlDateTimeSerializationMode.Local:
                    value = SwitchToLocalTime(value);
                    break;

                case XmlDateTimeSerializationMode.Utc:
                    value = SwitchToUtcTime(value);
                    break;

                case XmlDateTimeSerializationMode.Unspecified:
                    value = new DateTime(value.Ticks, DateTimeKind.Unspecified);
                    break;

                case XmlDateTimeSerializationMode.RoundtripKind:
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.Sch_InvalidDateTimeOption, dateTimeOption, "dateTimeOption"));
            }
            XsdDateTime xsdDateTime = new XsdDateTime(value, XsdDateTimeFlags.DateTime);
            return xsdDateTime.ToString();
        }

        public static string ToString(DateTimeOffset value)
        {
            XsdDateTime xsdDateTime = new XsdDateTime(value);
            return xsdDateTime.ToString();
        }

        public static string ToString(DateTimeOffset value, string format)
        {
            return value.ToString(format, DateTimeFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToString15"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(Guid value)
        {
            return value.ToString();
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToBoolean"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Boolean ToBoolean(string s)
        {
            s = TrimString(s);
            if (s == "1" || s == "true") return true;
            if (s == "0" || s == "false") return false;
            throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Boolean"));
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToChar"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Char ToChar(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (s.Length != 1)
            {
                throw new FormatException(SR.XmlConvert_NotOneCharString);
            }
            return s[0];
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToDecimal"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Decimal ToDecimal(string s)
        {
            return Decimal.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToSByte"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static SByte ToSByte(string s)
        {
            return SByte.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToInt16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Int16 ToInt16(string s)
        {
            return Int16.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToInt32"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Int32 ToInt32(string s)
        {
            return Int32.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToInt64"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Int64 ToInt64(string s)
        {
            return Int64.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToByte"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Byte ToByte(string s)
        {
            return Byte.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToUInt16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static UInt16 ToUInt16(string s)
        {
            return UInt16.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToUInt32"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static UInt32 ToUInt32(string s)
        {
            return UInt32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }



        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToUInt64"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public static UInt64 ToUInt64(string s)
        {
            return UInt64.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToSingle"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Single ToSingle(string s)
        {
            s = TrimString(s);
            if (s == "-INF") return Single.NegativeInfinity;
            if (s == "INF") return Single.PositiveInfinity;
            float f = Single.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, NumberFormatInfo.InvariantInfo);
            if (f == 0 && s[0] == '-')
            {
                return -0f;
            }
            return f;
        }


        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToDouble"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Double ToDouble(string s)
        {
            s = TrimString(s);
            if (s == "-INF") return Double.NegativeInfinity;
            if (s == "INF") return Double.PositiveInfinity;
            double dVal = Double.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
            if (dVal == 0 && s[0] == '-')
            {
                return -0d;
            }
            return dVal;
        }



        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToTimeSpan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static TimeSpan ToTimeSpan(string s)
        {
            XsdDuration duration;
            TimeSpan timeSpan;

            try
            {
                duration = new XsdDuration(s);
            }
            catch (Exception)
            {
                // Remap exception for v1 compatibility
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "TimeSpan"));
            }

            timeSpan = duration.ToTimeSpan();

            return timeSpan;
        }




        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToDateTime3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static DateTime ToDateTime(string s, XmlDateTimeSerializationMode dateTimeOption)
        {
            XsdDateTime xsdDateTime = new XsdDateTime(s, XsdDateTimeFlags.AllXsd);
            DateTime dt = (DateTime)xsdDateTime;

            switch (dateTimeOption)
            {
                case XmlDateTimeSerializationMode.Local:
                    dt = SwitchToLocalTime(dt);
                    break;

                case XmlDateTimeSerializationMode.Utc:
                    dt = SwitchToUtcTime(dt);
                    break;

                case XmlDateTimeSerializationMode.Unspecified:
                    dt = new DateTime(dt.Ticks, DateTimeKind.Unspecified);
                    break;

                case XmlDateTimeSerializationMode.RoundtripKind:
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.Sch_InvalidDateTimeOption, dateTimeOption, "dateTimeOption"));
            }
            return dt;
        }

        public static DateTimeOffset ToDateTimeOffset(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            XsdDateTime xsdDateTime = new XsdDateTime(s, XsdDateTimeFlags.AllXsd);
            DateTimeOffset dateTimeOffset = (DateTimeOffset)xsdDateTime;
            return dateTimeOffset;
        }

        public static DateTimeOffset ToDateTimeOffset(string s, string format)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            return DateTimeOffset.ParseExact(s, format, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite);
        }

        public static DateTimeOffset ToDateTimeOffset(string s, string[] formats)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            return DateTimeOffset.ParseExact(s, formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite);
        }

        ///<include file='doc\XmlConvert.uex' path='docs/doc[@for="XmlConvert.ToGuid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Guid ToGuid(string s)
        {
            return new Guid(s);
        }


        private static DateTime SwitchToLocalTime(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Local:
                    return value;

                case DateTimeKind.Unspecified:
                    return new DateTime(value.Ticks, DateTimeKind.Local);

                case DateTimeKind.Utc:
                    return value.ToLocalTime();
            }
            return value;
        }

        private static DateTime SwitchToUtcTime(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Utc:
                    return value;

                case DateTimeKind.Unspecified:
                    return new DateTime(value.Ticks, DateTimeKind.Utc);

                case DateTimeKind.Local:
                    return value.ToUniversalTime();
            }
            return value;
        }

        internal static Uri ToUri(string s)
        {
            if (s != null && s.Length > 0)
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


        // Compares the given character interval and string and returns true if the characters are identical
        internal static bool StrEqual(char[] chars, int strPos1, int strLen1, string str2)
        {
            if (strLen1 != str2.Length)
            {
                return false;
            }

            int i = 0;
            while (i < strLen1 && chars[strPos1 + i] == str2[i])
            {
                i++;
            }
            return i == strLen1;
        }

        // XML whitespace characters, <spec>http://www.w3.org/TR/REC-xml#NT-S</spec>
        internal static readonly char[] WhitespaceChars = new char[] { ' ', '\t', '\n', '\r' };

        // Trim a string using XML whitespace characters
        internal static string TrimString(string value)
        {
            return value.Trim(WhitespaceChars);
        }

        // Trim beginning of a string using XML whitespace characters
        internal static string TrimStringStart(string value)
        {
            return value.TrimStart(WhitespaceChars);
        }

        // Trim end of a string using XML whitespace characters
        internal static string TrimStringEnd(string value)
        {
            return value.TrimEnd(WhitespaceChars);
        }

        // Split a string into a whitespace-separated list of tokens
        internal static string[] SplitString(string value)
        {
            return value.Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static string[] SplitString(string value, StringSplitOptions splitStringOptions)
        {
            return value.Split(WhitespaceChars, splitStringOptions);
        }

        internal static bool IsNegativeZero(double value)
        {
            // Simple equals function will report that -0 is equal to +0, so compare bits instead
            if (value == 0 && DoubleToInt64Bits(value) == DoubleToInt64Bits(-0e0))
            {
                return true;
            }
            return false;
        }

#if !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecuritySafeCritical]
#endif
        private static unsafe long DoubleToInt64Bits(double value)
        {
            // NOTE: BitConverter.DoubleToInt64Bits is missing in Silverlight
            return *((long*)&value);
        }

        internal static void VerifyCharData(string data, ExceptionType exceptionType)
        {
            VerifyCharData(data, exceptionType, exceptionType);
        }

        internal static void VerifyCharData(string data, ExceptionType invCharExceptionType, ExceptionType invSurrogateExceptionType)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            int i = 0;
            int len = data.Length;
            for (; ;)
            {
                while (i < len && s_xmlCharType.IsCharData(data[i]))
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

        internal static void VerifyCharData(char[] data, int offset, int len, ExceptionType exceptionType)
        {
            if (data == null || len == 0)
            {
                return;
            }

            int i = offset;
            int endPos = offset + len;
            for (; ;)
            {
                while (i < endPos && s_xmlCharType.IsCharData(data[i]))
                {
                    i++;
                }
                if (i == endPos)
                {
                    return;
                }

                char ch = data[i];
                if (XmlCharType.IsHighSurrogate(ch))
                {
                    if (i + 1 == endPos)
                    {
                        throw CreateException(SR.Xml_InvalidSurrogateMissingLowChar, exceptionType, 0, offset - i + 1);
                    }
                    ch = data[i + 1];
                    if (XmlCharType.IsLowSurrogate(ch))
                    {
                        i += 2;
                        continue;
                    }
                    else
                    {
                        throw CreateInvalidSurrogatePairException(data[i + 1], data[i], exceptionType, 0, offset - i + 1);
                    }
                }
                throw CreateInvalidCharException(data, len, i, exceptionType);
            }
        }


        internal static Exception CreateException(string res, ExceptionType exceptionType)
        {
            return CreateException(res, exceptionType, 0, 0);
        }

        internal static Exception CreateException(string res, ExceptionType exceptionType, int lineNo, int linePos)
        {
            switch (exceptionType)
            {
                case ExceptionType.ArgumentException:
                    return new ArgumentException(res);
                case ExceptionType.XmlException:
                default:
                    return new XmlException(res, string.Empty, lineNo, linePos);
            }
        }

        internal static Exception CreateException(string res, string arg, ExceptionType exceptionType)
        {
            return CreateException(res, arg, exceptionType, 0, 0);
        }

        internal static Exception CreateException(string res, string arg, ExceptionType exceptionType, int lineNo, int linePos)
        {
            switch (exceptionType)
            {
                case ExceptionType.ArgumentException:
                    return new ArgumentException(SR.Format(res, arg));
                case ExceptionType.XmlException:
                default:
                    return new XmlException(res, arg, lineNo, linePos);
            }
        }

        internal static Exception CreateException(string res, string[] args, ExceptionType exceptionType)
        {
            return CreateException(res, args, exceptionType, 0, 0);
        }

        internal static Exception CreateException(string res, string[] args, ExceptionType exceptionType, int lineNo, int linePos)
        {
            switch (exceptionType)
            {
                case ExceptionType.ArgumentException:
                    return new ArgumentException(SR.Format(res, args));
                case ExceptionType.XmlException:
                default:
                    return new XmlException(res, args, lineNo, linePos);
            }
        }

        internal static Exception CreateInvalidSurrogatePairException(char low, char hi)
        {
            return CreateInvalidSurrogatePairException(low, hi, ExceptionType.ArgumentException);
        }

        internal static Exception CreateInvalidSurrogatePairException(char low, char hi, ExceptionType exceptionType)
        {
            return CreateInvalidSurrogatePairException(low, hi, exceptionType, 0, 0);
        }

        internal static Exception CreateInvalidSurrogatePairException(char low, char hi, ExceptionType exceptionType, int lineNo, int linePos)
        {
            string[] args = new string[] {
                ((uint)hi).ToString( "X", CultureInfo.InvariantCulture ),
                ((uint)low).ToString( "X", CultureInfo.InvariantCulture )
            };
            return CreateException(SR.Xml_InvalidSurrogatePairWithArgs, args, exceptionType, lineNo, linePos);
        }

        internal static Exception CreateInvalidHighSurrogateCharException(char hi)
        {
            return CreateInvalidHighSurrogateCharException(hi, ExceptionType.ArgumentException);
        }

        internal static Exception CreateInvalidHighSurrogateCharException(char hi, ExceptionType exceptionType)
        {
            return CreateInvalidHighSurrogateCharException(hi, exceptionType, 0, 0);
        }

        internal static Exception CreateInvalidHighSurrogateCharException(char hi, ExceptionType exceptionType, int lineNo, int linePos)
        {
            return CreateException(SR.Xml_InvalidSurrogateHighChar, ((uint)hi).ToString("X", CultureInfo.InvariantCulture), exceptionType, lineNo, linePos);
        }

        internal static Exception CreateInvalidCharException(char[] data, int length, int invCharPos)
        {
            return CreateInvalidCharException(data, length, invCharPos, ExceptionType.ArgumentException);
        }

        internal static Exception CreateInvalidCharException(char[] data, int length, int invCharPos, ExceptionType exceptionType)
        {
            return CreateException(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(data, length, invCharPos), exceptionType, 0, invCharPos + 1);
        }

        internal static Exception CreateInvalidCharException(string data, int invCharPos)
        {
            return CreateInvalidCharException(data, invCharPos, ExceptionType.ArgumentException);
        }

        internal static Exception CreateInvalidCharException(string data, int invCharPos, ExceptionType exceptionType)
        {
            return CreateException(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(data, invCharPos), exceptionType, 0, invCharPos + 1);
        }

        internal static Exception CreateInvalidCharException(char invChar, char nextChar)
        {
            return CreateInvalidCharException(invChar, nextChar, ExceptionType.ArgumentException);
        }

        internal static Exception CreateInvalidCharException(char invChar, char nextChar, ExceptionType exceptionType)
        {
            return CreateException(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(invChar, nextChar), exceptionType);
        }

        internal static Exception CreateInvalidNameCharException(string name, int index, ExceptionType exceptionType)
        {
            return CreateException(index == 0 ? SR.Xml_BadStartNameChar : SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(name, index), exceptionType, 0, index + 1);
        }

        internal static ArgumentException CreateInvalidNameArgumentException(string name, string argumentName)
        {
            return (name == null) ? new ArgumentNullException(argumentName) : new ArgumentException(SR.Xml_EmptyName, argumentName);
        }
    }
}
