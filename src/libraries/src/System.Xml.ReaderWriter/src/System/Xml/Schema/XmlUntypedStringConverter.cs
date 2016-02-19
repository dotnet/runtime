// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;

namespace System.Xml.Schema
{
    // This is an atomic value converted for Silverlight XML core that knows only how to convert to and from string. 
    // It does not recognize XmlAtomicValue or XPathItemType.
    internal class XmlUntypedStringConverter
    {
        // Fields
        private bool _listsAllowed;
        private XmlUntypedStringConverter _listItemConverter;

        // Cached types
        private static readonly Type s_decimalType = typeof(decimal);
        private static readonly Type s_int32Type = typeof(int);
        private static readonly Type s_int64Type = typeof(long);
        private static readonly Type s_stringType = typeof(string);
        private static readonly Type s_objectType = typeof(object);
        private static readonly Type s_byteType = typeof(byte);
        private static readonly Type s_int16Type = typeof(short);
        private static readonly Type s_SByteType = typeof(sbyte);
        private static readonly Type s_UInt16Type = typeof(ushort);
        private static readonly Type s_UInt32Type = typeof(uint);
        private static readonly Type s_UInt64Type = typeof(ulong);
        private static readonly Type s_doubleType = typeof(double);
        private static readonly Type s_singleType = typeof(float);
        private static readonly Type s_dateTimeType = typeof(DateTime);
        private static readonly Type s_dateTimeOffsetType = typeof(DateTimeOffset);
        private static readonly Type s_booleanType = typeof(bool);
        private static readonly Type s_byteArrayType = typeof(Byte[]);
        private static readonly Type s_xmlQualifiedNameType = typeof(XmlQualifiedName);
        private static readonly Type s_uriType = typeof(Uri);
        private static readonly Type s_timeSpanType = typeof(TimeSpan);

        private static readonly string s_untypedStringTypeName = "xdt:untypedAtomic";

        // Static convertor instance
        internal static XmlUntypedStringConverter Instance = new XmlUntypedStringConverter(true);

        private XmlUntypedStringConverter(bool listsAllowed)
        {
            _listsAllowed = listsAllowed;
            if (listsAllowed)
            {
                _listItemConverter = new XmlUntypedStringConverter(false);
            }
        }

        internal string ToString(object value, IXmlNamespaceResolver nsResolver)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Type sourceType = value.GetType();
            //we can consider avoiding the type equality checks and instead doing a "is" check
            //similar to what we did for Uri and XmlQualifiedName below

            if (sourceType == s_booleanType) return XmlConvert.ToString((bool)value);
            if (sourceType == s_byteType) return XmlConvert.ToString((byte)value);
            if (sourceType == s_byteArrayType) return Base64BinaryToString((byte[])value);
            if (sourceType == s_dateTimeType) return DateTimeToString((DateTime)value);
            if (sourceType == s_dateTimeOffsetType) return DateTimeOffsetToString((DateTimeOffset)value);
            if (sourceType == s_decimalType) return XmlConvert.ToString((decimal)value);
            if (sourceType == s_doubleType) return XmlConvert.ToString((double)value);
            if (sourceType == s_int16Type) return XmlConvert.ToString((short)value);
            if (sourceType == s_int32Type) return XmlConvert.ToString((int)value);
            if (sourceType == s_int64Type) return XmlConvert.ToString((long)value);
            if (sourceType == s_SByteType) return XmlConvert.ToString((sbyte)value);
            if (sourceType == s_singleType) return XmlConvert.ToString((float)value);
            if (sourceType == s_stringType) return ((string)value);
            if (sourceType == s_timeSpanType) return DurationToString((TimeSpan)value);
            if (sourceType == s_UInt16Type) return XmlConvert.ToString((ushort)value);
            if (sourceType == s_UInt32Type) return XmlConvert.ToString((uint)value);
            if (sourceType == s_UInt64Type) return XmlConvert.ToString((ulong)value);
            if (value is Uri) return AnyUriToString((Uri)value);
            if (value is XmlQualifiedName) return QNameToString((XmlQualifiedName)value, nsResolver);

            return (string)ListTypeToString(value, nsResolver);
        }

        internal object FromString(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            if (destinationType == s_objectType) destinationType = typeof(string);
            if (destinationType == s_booleanType) return XmlConvert.ToBoolean((string)value);
            if (destinationType == s_byteType) return Int32ToByte(XmlConvert.ToInt32((string)value));
            if (destinationType == s_byteArrayType) return StringToBase64Binary((string)value);
            if (destinationType == s_dateTimeType) return StringToDateTime((string)value);
            if (destinationType == s_dateTimeOffsetType) return StringToDateTimeOffset((string)value);
            if (destinationType == s_decimalType) return XmlConvert.ToDecimal((string)value);
            if (destinationType == s_doubleType) return XmlConvert.ToDouble((string)value);
            if (destinationType == s_int16Type) return Int32ToInt16(XmlConvert.ToInt32((string)value));
            if (destinationType == s_int32Type) return XmlConvert.ToInt32((string)value);
            if (destinationType == s_int64Type) return XmlConvert.ToInt64((string)value);
            if (destinationType == s_SByteType) return Int32ToSByte(XmlConvert.ToInt32((string)value));
            if (destinationType == s_singleType) return XmlConvert.ToSingle((string)value);
            if (destinationType == s_timeSpanType) return StringToDuration((string)value);
            if (destinationType == s_UInt16Type) return Int32ToUInt16(XmlConvert.ToInt32((string)value));
            if (destinationType == s_UInt32Type) return Int64ToUInt32(XmlConvert.ToInt64((string)value));
            if (destinationType == s_UInt64Type) return DecimalToUInt64(XmlConvert.ToDecimal((string)value));
            if (destinationType == s_uriType) return XmlConvert.ToUri((string)value);
            if (destinationType == s_xmlQualifiedNameType) return StringToQName((string)value, nsResolver);
            if (destinationType == s_stringType) return ((string)value);

            return StringToListType(value, destinationType, nsResolver);
        }

        private byte Int32ToByte(int value)
        {
            if (value < (int)Byte.MinValue || value > (int)Byte.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, XmlConvert.ToString(value), "Byte"));

            return (byte)value;
        }

        private short Int32ToInt16(int value)
        {
            if (value < (int)Int16.MinValue || value > (int)Int16.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, XmlConvert.ToString(value), "Int16"));

            return (short)value;
        }

        private sbyte Int32ToSByte(int value)
        {
            if (value < (int)SByte.MinValue || value > (int)SByte.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, XmlConvert.ToString(value), "SByte"));

            return (sbyte)value;
        }

        private ushort Int32ToUInt16(int value)
        {
            if (value < (int)UInt16.MinValue || value > (int)UInt16.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, XmlConvert.ToString(value), "UInt16"));

            return (ushort)value;
        }

        private uint Int64ToUInt32(long value)
        {
            if (value < (long)UInt32.MinValue || value > (long)UInt32.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, XmlConvert.ToString(value), "UInt32"));

            return (uint)value;
        }

        private ulong DecimalToUInt64(decimal value)
        {
            if (value < (decimal)UInt64.MinValue || value > (decimal)UInt64.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, XmlConvert.ToString(value), "UInt64"));

            return (ulong)value;
        }

        private string Base64BinaryToString(byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        private byte[] StringToBase64Binary(string value)
        {
            return Convert.FromBase64String(XmlConvert.TrimString(value));
        }

        private string DateTimeToString(DateTime value)
        {
            return (new XsdDateTime(value, XsdDateTimeFlags.DateTime)).ToString();
        }

        private static DateTime StringToDateTime(string value)
        {
            return (DateTime)(new XsdDateTime(value, XsdDateTimeFlags.AllXsd));
        }

        private static string DateTimeOffsetToString(DateTimeOffset value)
        {
            return (new XsdDateTime(value, XsdDateTimeFlags.DateTime)).ToString();
        }

        private static DateTimeOffset StringToDateTimeOffset(string value)
        {
            return (DateTimeOffset)(new XsdDateTime(value, XsdDateTimeFlags.AllXsd));
        }

        private string DurationToString(TimeSpan value)
        {
            return new XsdDuration(value, XsdDuration.DurationType.Duration).ToString(XsdDuration.DurationType.Duration);
        }

        private TimeSpan StringToDuration(string value)
        {
            return new XsdDuration(value, XsdDuration.DurationType.Duration).ToTimeSpan(XsdDuration.DurationType.Duration);
        }

        private string AnyUriToString(Uri value)
        {
            return value.OriginalString;
        }

        protected static string QNameToString(XmlQualifiedName qname, IXmlNamespaceResolver nsResolver)
        {
            string prefix;

            if (nsResolver == null)
            {
                return string.Concat("{", qname.Namespace, "}", qname.Name);
            }

            prefix = nsResolver.LookupPrefix(qname.Namespace);
            if (prefix == null)
            {
                throw new InvalidCastException(SR.Format(SR.XmlConvert_TypeNoPrefix, qname.ToString(), qname.Namespace));
            }

            return (prefix.Length != 0) ? string.Concat(prefix, ":", qname.Name) : qname.Name;
        }

        private static XmlQualifiedName StringToQName(string value, IXmlNamespaceResolver nsResolver)
        {
            string prefix, localName, ns;

            value = value.Trim();

            // Parse prefix:localName
            try
            {
                ValidateNames.ParseQNameThrow(value, out prefix, out localName);
            }
            catch (XmlException e)
            {
                throw new FormatException(e.Message);
            }

            // Throw error if no namespaces are in scope
            if (nsResolver == null)
                throw new InvalidCastException(SR.Format(SR.XmlConvert_TypeNoNamespace, value, prefix));

            // Lookup namespace
            ns = nsResolver.LookupNamespace(prefix);
            if (ns == null)
                throw new InvalidCastException(SR.Format(SR.XmlConvert_TypeNoNamespace, value, prefix));

            // Create XmlQualfiedName
            return new XmlQualifiedName(localName, ns);
        }

        private string ListTypeToString(object value, IXmlNamespaceResolver nsResolver)
        {
            if (!_listsAllowed || !(value is IEnumerable))
            {
                throw CreateInvalidClrMappingException(value.GetType(), typeof(string));
            }

            StringBuilder bldr = new StringBuilder();

            foreach (object item in ((IEnumerable)value))
            {
                // skip null values
                if (item != null)
                {
                    // Separate values by single space character
                    if (bldr.Length != 0)
                        bldr.Append(' ');

                    // Append string value of next item in the list
                    bldr.Append(_listItemConverter.ToString(item, nsResolver));
                }
            }

            return bldr.ToString();
        }

        private object StringToListType(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
        {
            if (_listsAllowed && destinationType.IsArray)
            {
                Type itemTypeDst = destinationType.GetElementType();

                if (itemTypeDst == s_objectType) return ToArray<object>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_booleanType) return ToArray<bool>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_byteType) return ToArray<byte>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_byteArrayType) return ToArray<byte[]>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_dateTimeType) return ToArray<DateTime>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_dateTimeOffsetType) return ToArray<DateTimeOffset>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_decimalType) return ToArray<decimal>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_doubleType) return ToArray<double>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_int16Type) return ToArray<short>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_int32Type) return ToArray<int>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_int64Type) return ToArray<long>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_SByteType) return ToArray<sbyte>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_singleType) return ToArray<float>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_stringType) return ToArray<string>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_timeSpanType) return ToArray<TimeSpan>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_UInt16Type) return ToArray<ushort>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_UInt32Type) return ToArray<uint>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_UInt64Type) return ToArray<ulong>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_uriType) return ToArray<Uri>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
                if (itemTypeDst == s_xmlQualifiedNameType) return ToArray<XmlQualifiedName>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
            }
            throw CreateInvalidClrMappingException(typeof(string), destinationType);
        }

        private T[] ToArray<T>(string[] stringArray, IXmlNamespaceResolver nsResolver)
        {
            T[] arrDst = new T[stringArray.Length];
            for (int i = 0; i < stringArray.Length; i++)
            {
                arrDst[i] = (T)_listItemConverter.FromString(stringArray[i], typeof(T), nsResolver);
            }
            return arrDst;
        }



        private Exception CreateInvalidClrMappingException(Type sourceType, Type destinationType)
        {
            return new InvalidCastException(SR.Format(SR.XmlConvert_TypeListBadMapping2, s_untypedStringTypeName, sourceType.ToString(), destinationType.ToString()));
        }
    }
}

