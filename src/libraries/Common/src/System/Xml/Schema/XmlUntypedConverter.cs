// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    // SUMMARY
    // =======
    // For each Xml type, there is a set of Clr types that can represent it.  Some of these mappings involve
    // loss of fidelity.  For example, xsd:dateTime can be represented as System.DateTime, but only at the expense
    // of normalizing the time zone.  And xs:duration can be represented as System.TimeSpan, but only at the expense
    // of discarding variations such as "P50H", "P1D26H", "P2D2H", all of which are normalized as "P2D2H".
    //
    // Implementations of this class convert between the various Clr representations of Xml types.  Note that
    // in *no* case is the Xml type ever modified.  Only the Clr type is changed.  This means that in cases where
    // the Xml type is part of the representation (such as XmlAtomicValue), the destination value is guaranteed
    // to have the same Xml type.
    //
    // For all converters, converting to typeof(object) is identical to converting to XmlSchemaType.Datatype.ValueType.
    //
    //
    // ATOMIC MAPPINGS
    // ===============
    //
    // -----------------------------------------------------------------------------------------------------------
    // Source/Destination   System.String                       Other Clr Type
    // -----------------------------------------------------------------------------------------------------------
    // System.String        No-op conversion.                   Use Xsd rules to convert from the string
    //                                                          to primitive, full-fidelity Clr type (use
    //                                                          XmlConvert where possible).  Use Clr rules
    //                                                          to convert to destination type.
    // -----------------------------------------------------------------------------------------------------------
    // Other Clr Type       Use Clr rules to convert from       Use Clr rules to convert from source to
    //                      source type to primitive, full-     destination type.
    //                      fidelity Clr type.  Use Xsd rules
    //                      to convert to a string (use
    //                      XmlConvert where possible).         
    // -----------------------------------------------------------------------------------------------------------
    //
    //
    // LIST MAPPINGS
    // =============
    // The following Clr types can be used to represent Xsd list types: IList, ICollection, IEnumerable, Type[],
    // String.
    //
    // -----------------------------------------------------------------------------------------------------------
    // Source/Destination   System.String                           Clr List Type
    // -----------------------------------------------------------------------------------------------------------
    // System.String        No-op conversion                        Tokenize the string by whitespace, create a
    //                                                              String[] from tokens, and follow List => List 
    //                                                              rules.
    // -----------------------------------------------------------------------------------------------------------
    // Clr List Type        Follow List => String[] rules,          Create destination list having the same length
    //                      then concatenate strings from array,    as the source list.  For each item in the
    //                      separating adjacent strings with a      source list, call the atomic converter to
    //                      single space character.                 convert to the destination type.  The destination
    //                                                              item type for IList, ICollection, IEnumerable
    //                                                              is typeof(object).  The destination item type
    //                                                              for Type[] is Type.
    // -----------------------------------------------------------------------------------------------------------
    //
    //
    // UNION MAPPINGS
    // ==============
    // Union types may only be represented using System.Xml.Schema.XmlAtomicValue or System.String.  Either the
    // source type or the destination type must therefore always be either System.String or
    // System.Xml.Schema.XmlAtomicValue.
    //
    // -----------------------------------------------------------------------------------------------------------
    // Source/Destination   System.String           XmlAtomicValue          Other Clr Type
    // -----------------------------------------------------------------------------------------------------------
    // System.String        No-op conversion        Follow System.String=>  Call ParseValue in order to determine
    //                                              Other Clr Type rules.   the member type.  Call ChangeType on
    //                                                                      the member type's converter to convert
    //                                                                      to desired Clr type.
    // -----------------------------------------------------------------------------------------------------------
    // XmlAtomicValue       Follow XmlAtomicValue   No-op conversion.       Call ReadValueAs, where destinationType
    //                      => Other Clr Type                               is the desired Clr type.
    //                      rules.
    // -----------------------------------------------------------------------------------------------------------
    // Other Clr Type       InvalidCastException   InvalidCastException     InvalidCastException
    // -----------------------------------------------------------------------------------------------------------
    //
    //
    // EXAMPLES
    // ========
    //
    // -----------------------------------------------------------------------------------------------------------
    //            Source    Destination
    // Xml Type   Value     Type             Conversion Steps                  Explanation
    // -----------------------------------------------------------------------------------------------------------
    // xs:int     "10"      Byte             "10" => 10M => (byte) 10          Primitive, full-fidelity for xs:int
    //                                                                         is a truncated decimal
    // -----------------------------------------------------------------------------------------------------------
    // xs:int     "10.10"   Byte             FormatException                   xs:integer parsing rules do not
    //                                                                         allow fractional parts
    // -----------------------------------------------------------------------------------------------------------
    // xs:int     10.10M    Byte             10.10M => (byte) 10               Default Clr rules truncate when
    //                                                                         converting from Decimal to Byte
    // -----------------------------------------------------------------------------------------------------------
    // xs:int     10.10M    Decimal          10.10M => 10.10M                  Decimal => Decimal is no-op
    // -----------------------------------------------------------------------------------------------------------
    // xs:int     10.10M    String           10.10M => 10M => "10"
    // -----------------------------------------------------------------------------------------------------------
    // xs:int     "hello"   String           "hello" => "hello"                String => String is no-op
    // -----------------------------------------------------------------------------------------------------------
    // xs:byte    "300"     Int32            "300" => 300M => (int) 300
    // -----------------------------------------------------------------------------------------------------------
    // xs:byte    300       Byte             300 => 300M => OverflowException  Clr overflows when converting from
    //                                                                         Decimal to Byte
    // -----------------------------------------------------------------------------------------------------------
    // xs:byte    300       XmlAtomicValue   new XmlAtomicValue(xs:byte, 300)  Invalid atomic value created
    // -----------------------------------------------------------------------------------------------------------
    // xs:double  1.234f    String          1.234f => 1.2339999675750732d =>   Converting a Single value to a Double
    //                                      "1.2339999675750732"               value zero-extends it in base-2, so
    //                                                                         "garbage" digits appear when it's
    //                                                                         converted to base-10.
    // -----------------------------------------------------------------------------------------------------------
    // xs:int*    {1, "2",  String          {1, "2", 3.1M} =>                  Delegate to xs:int converter to
    //            3.1M}                     {"1", "2", "3"} => "1 2 3"         convert each item to a string.
    // -----------------------------------------------------------------------------------------------------------
    // xs:int*    "1 2 3"   Int32[]         "1 2 3" => {"1", "2", "3"} =>
    //                                      {1, 2, 3}
    // -----------------------------------------------------------------------------------------------------------
    // xs:int*    {1, "2",  Object[]        {1, "2", 3.1M} =>                  xs:int converter uses Int32 by default,
    //            3.1M}                     {(object)1, (object)2, (object)3}  so returns boxed Int32 values.
    // -----------------------------------------------------------------------------------------------------------
    // (xs:int |  "1 2001"  XmlAtomicValue[]  "1 2001" => {(xs:int) 1,
    // xs:gYear)*                             (xs:gYear) 2001}
    // -----------------------------------------------------------------------------------------------------------
    // (xs:int* | "1 2001"  String          "1 2001"                           No-op conversion even though
    // xs:gYear*)                                                              ParseValue would fail if it were called.
    // -----------------------------------------------------------------------------------------------------------
    // (xs:int* | "1 2001"  Int[]           XmlSchemaException                 ParseValue fails.
    // xs:gYear*)
    // -----------------------------------------------------------------------------------------------------------
    //
    internal static class XmlUntypedConverter
    {
        public static bool ToBoolean(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return XmlConvert.ToBoolean((string)value);
        }

        private static DateTime UntypedAtomicToDateTime(string value)
        {
            return (DateTime)(new XsdDateTime(value, XsdDateTimeFlags.AllXsd));
        }

        public static DateTime ToDateTime(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return UntypedAtomicToDateTime((string)value);
        }

        public static double ToDouble(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return XmlConvert.ToDouble((string)value);
        }

        public static int ToInt32(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return XmlConvert.ToInt32((string)value);
        }

        public static long ToInt64(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return XmlConvert.ToInt64((string)value);
        }

        private static readonly Type DecimalType = typeof(decimal);
        private static readonly Type Int32Type = typeof(int);
        private static readonly Type Int64Type = typeof(long);
        private static readonly Type StringType = typeof(string);
        private static readonly Type ByteType = typeof(byte);
        private static readonly Type Int16Type = typeof(short);
        private static readonly Type SByteType = typeof(sbyte);
        private static readonly Type UInt16Type = typeof(ushort);
        private static readonly Type UInt32Type = typeof(uint);
        private static readonly Type UInt64Type = typeof(ulong);
        private static readonly Type DoubleType = typeof(double);
        private static readonly Type SingleType = typeof(float);
        private static readonly Type DateTimeType = typeof(DateTime);
        private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
        private static readonly Type BooleanType = typeof(bool);
        private static readonly Type ByteArrayType = typeof(Byte[]);
        private static readonly Type XmlQualifiedNameType = typeof(XmlQualifiedName);
        private static readonly Type UriType = typeof(Uri);
        private static readonly Type TimeSpanType = typeof(TimeSpan);

        private static string Base64BinaryToString(byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        private static string DateTimeToString(DateTime value)
        {
            return (new XsdDateTime(value, XsdDateTimeFlags.DateTime)).ToString();
        }

        private static string DateTimeOffsetToString(DateTimeOffset value)
        {
            return (new XsdDateTime(value, XsdDateTimeFlags.DateTime)).ToString();
        }

        private static string QNameToString(XmlQualifiedName qname, IXmlNamespaceResolver nsResolver)
        {
            string prefix;

            if (nsResolver == null)
                return string.Concat("{", qname.Namespace, "}", qname.Name);

            prefix = nsResolver.LookupPrefix(qname.Namespace);
            if (prefix == null)
                throw new InvalidCastException(SR.Format(SR.XmlConvert_TypeNoPrefix, qname.ToString(), qname.Namespace));

            return (prefix.Length != 0) ? string.Concat(prefix, ":", qname.Name) : qname.Name;
        }

        private static string AnyUriToString(Uri value)
        {
            return value.OriginalString;
        }

        public static string ToString(object value, IXmlNamespaceResolver nsResolver)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Type sourceType = value.GetType();

            if (sourceType == BooleanType) return XmlConvert.ToString((bool)value);
            if (sourceType == ByteType) return XmlConvert.ToString((byte)value);
            if (sourceType == ByteArrayType) return Base64BinaryToString((byte[])value);
            if (sourceType == DateTimeType) return DateTimeToString((DateTime)value);
            if (sourceType == DateTimeOffsetType) return DateTimeOffsetToString((DateTimeOffset)value);
            if (sourceType == DecimalType) return XmlConvert.ToString((decimal)value);
            if (sourceType == DoubleType) return XmlConvert.ToString((double)value);
            if (sourceType == Int16Type) return XmlConvert.ToString((short)value);
            if (sourceType == Int32Type) return XmlConvert.ToString((int)value);
            if (sourceType == Int64Type) return XmlConvert.ToString((long)value);
            if (sourceType == SByteType) return XmlConvert.ToString((sbyte)value);
            if (sourceType == SingleType) return XmlConvert.ToString((float)value);
            if (sourceType == StringType) return ((string)value);
            if (sourceType == TimeSpanType) return XmlConvert.ToString((TimeSpan)value);
            if (sourceType == UInt16Type) return XmlConvert.ToString((ushort)value);
            if (sourceType == UInt32Type) return XmlConvert.ToString((uint)value);
            if (sourceType == UInt64Type) return XmlConvert.ToString((ulong)value);
            Uri valueAsUri = value as Uri;
            if (valueAsUri != null) return AnyUriToString(valueAsUri);
            XmlQualifiedName valueAsXmlQualifiedName = value as XmlQualifiedName;
            if (valueAsXmlQualifiedName != null) return QNameToString(valueAsXmlQualifiedName, nsResolver);

            throw new InvalidCastException(SR.Format(SR.XmlConvert_TypeToString, sourceType.Name));
        }

        private static byte[] StringToBase64Binary(string value)
        {
            return Convert.FromBase64String(XmlConvertEx.TrimString(value));
        }

        private static short Int32ToInt16(int value)
        {
            if (value < (int)Int16.MinValue || value > (int)Int16.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "Int16" }));

            return (short)value;
        }

        private static byte Int32ToByte(int value)
        {
            if (value < (int)Byte.MinValue || value > (int)Byte.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "Byte" }));

            return (byte)value;
        }

        private static ulong DecimalToUInt64(decimal value)
        {
            if (value < (decimal)UInt64.MinValue || value > (decimal)UInt64.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "UInt64" }));

            return (ulong)value;
        }

        private static sbyte Int32ToSByte(int value)
        {
            if (value < (int)SByte.MinValue || value > (int)SByte.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "SByte" }));

            return (sbyte)value;
        }

        private static DateTimeOffset UntypedAtomicToDateTimeOffset(string value)
        {
            return (DateTimeOffset)(new XsdDateTime(value, XsdDateTimeFlags.AllXsd));
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

            // Create XmlQualifiedName
            return new XmlQualifiedName(localName, ns);
        }

        private static ushort Int32ToUInt16(int value)
        {
            if (value < (int)UInt16.MinValue || value > (int)UInt16.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "UInt16" }));

            return (ushort)value;
        }

        private static uint Int64ToUInt32(long value)
        {
            if (value < (long)UInt32.MinValue || value > (long)UInt32.MaxValue)
                throw new OverflowException(SR.Format(SR.XmlConvert_Overflow, new string[] { XmlConvert.ToString(value), "UInt32" }));

            return (uint)value;
        }

        public static object ChangeType(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            if (destinationType == BooleanType) return XmlConvert.ToBoolean((string)value);
            if (destinationType == ByteType) return Int32ToByte(XmlConvert.ToInt32((string)value));
            if (destinationType == ByteArrayType) return StringToBase64Binary((string)value);
            if (destinationType == DateTimeType) return UntypedAtomicToDateTime((string)value);
            if (destinationType == DateTimeOffsetType) return XmlConvert.ToDateTimeOffset((string)value);
            if (destinationType == DecimalType) return XmlConvert.ToDecimal((string)value);
            if (destinationType == DoubleType) return XmlConvert.ToDouble((string)value);
            if (destinationType == Int16Type) return Int32ToInt16(XmlConvert.ToInt32((string)value));
            if (destinationType == Int32Type) return XmlConvert.ToInt32((string)value);
            if (destinationType == Int64Type) return XmlConvert.ToInt64((string)value);
            if (destinationType == SByteType) return Int32ToSByte(XmlConvert.ToInt32((string)value));
            if (destinationType == SingleType) return XmlConvert.ToSingle((string)value);
            if (destinationType == TimeSpanType) return XmlConvert.ToTimeSpan((string)value);
            if (destinationType == UInt16Type) return Int32ToUInt16(XmlConvert.ToInt32((string)value));
            if (destinationType == UInt32Type) return Int64ToUInt32(XmlConvert.ToInt64((string)value));
            if (destinationType == UInt64Type) return DecimalToUInt64(XmlConvert.ToDecimal((string)value));
            if (destinationType == UriType) return XmlConvertEx.ToUri((string)value);
            if (destinationType == XmlQualifiedNameType) return StringToQName((string)value, nsResolver);
            if (destinationType == StringType) return ((string)value);

            throw new InvalidCastException(SR.Format(SR.XmlConvert_TypeFromString, destinationType.Name));
        }
    }
}
