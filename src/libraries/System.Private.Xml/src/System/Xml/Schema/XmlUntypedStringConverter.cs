// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed class XmlUntypedStringConverter
    {
        // Fields
        private readonly bool _listsAllowed;
        private readonly XmlUntypedStringConverter? _listItemConverter;

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
        private static readonly Type s_byteArrayType = typeof(byte[]);
        private static readonly Type s_xmlQualifiedNameType = typeof(XmlQualifiedName);
        private static readonly Type s_uriType = typeof(Uri);
        private static readonly Type s_timeSpanType = typeof(TimeSpan);

        private const string UntypedStringTypeName = "xdt:untypedAtomic";

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
            if (value < (int)byte.MinValue || value > (int)byte.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "Byte" }));

            return (byte)value;
        }

        private short Int32ToInt16(int value)
        {
            if (value < (int)short.MinValue || value > (int)short.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "Int16" }));

            return (short)value;
        }

        private sbyte Int32ToSByte(int value)
        {
            if (value < (int)sbyte.MinValue || value > (int)sbyte.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "SByte" }));

            return (sbyte)value;
        }

        private ushort Int32ToUInt16(int value)
        {
            if (value < (int)ushort.MinValue || value > (int)ushort.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "UInt16" }));

            return (ushort)value;
        }

        private uint Int64ToUInt32(long value)
        {
            if (value < (long)uint.MinValue || value > (long)uint.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "UInt32" }));

            return (uint)value;
        }

        private ulong DecimalToUInt64(decimal value)
        {
            if (value < (decimal)ulong.MinValue || value > (decimal)ulong.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "UInt64" }));

            return (ulong)value;
        }

        private byte[] StringToBase64Binary(string value)
        {
            return Convert.FromBase64String(XmlConvert.TrimString(value));
        }

        private static DateTime StringToDateTime(string value)
        {
            return (DateTime)(new XsdDateTime(value, XsdDateTimeFlags.AllXsd));
        }

        private static DateTimeOffset StringToDateTimeOffset(string value)
        {
            return (DateTimeOffset)(new XsdDateTime(value, XsdDateTimeFlags.AllXsd));
        }

        private TimeSpan StringToDuration(string value)
        {
            return new XsdDuration(value, XsdDuration.DurationType.Duration).ToTimeSpan(XsdDuration.DurationType.Duration);
        }

        private static XmlQualifiedName StringToQName(string value, IXmlNamespaceResolver nsResolver)
        {
            string prefix, localName;
            string? ns;

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

        private object StringToListType(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
        {
            if (_listsAllowed && destinationType.IsArray)
            {
                Type? itemTypeDst = destinationType.GetElementType();

                // Different StringSplitOption needs to be used because of following bugs:
                // 566053: Behavior change between SL2 and Dev10 in the way string arrays are deserialized by the XmlReader.ReadContentsAs method
                // 643697: Deserialization of typed arrays by the XmlReader.ReadContentsAs method fails
                //
                // In Silverligt 2 the XmlConvert.SplitString was not using the StringSplitOptions, which is the same as using StringSplitOptions.None.
                // What it meant is that whenever there is a double space between two values in the input string it turned into
                // an string.Empty entry in the intermediate string array. In Dev10 empty entries were always removed (StringSplitOptions.RemoveEmptyEntries).
                //
                // Moving forward in coreclr we'll use Dev10 behavior which empty entries were always removed (StringSplitOptions.RemoveEmptyEntries).
                // we didn't quirk the change because we discover not many apps using ReadContentAs with string array type parameter
                //
                // The types Object, Byte[], String and Uri can be successfully deserialized from string.Empty, so we need to preserve the
                // Silverlight 2 behavior for back-compat (=use StringSplitOptions.None). All the other array types failed to deserialize
                // from string.Empty in Silverlight 2 (threw an exception), so we can fix all of these as they are not breaking changes
                // (=use StringSplitOptions.RemoveEmptyEntries).

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
                arrDst[i] = (T)_listItemConverter!.FromString(stringArray[i], typeof(T), nsResolver);
            }
            return arrDst;
        }

        private Exception CreateInvalidClrMappingException(Type sourceType, Type destinationType)
        {
            return new InvalidCastException(SR.Format(SR.XmlConvert_TypeListBadMapping2, UntypedStringTypeName, sourceType.Name, destinationType.Name));
        }
    }
}
