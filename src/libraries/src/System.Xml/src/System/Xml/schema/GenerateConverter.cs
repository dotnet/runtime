// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.ValueConverter {
    using System;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.XPath;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.CodeDom.Compiler; // for IndentedTextWriter

    public class Generator {
        private IndentedTextWriter w;

        public static Type[] InterfaceTypes = {typeof(Boolean), typeof(DateTime), typeof(Decimal), typeof(Double), typeof(Int32), typeof(Int64), typeof(Single), typeof(String), typeof(Object)};

        // xs:decimal and derived types
        public static ConversionRuleGroup Numeric10RuleGroup =
            new ConversionRuleGroup("XmlNumeric10Converter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "Decimal",              "Decimal",              "((decimal) value)"},
            new string[] {"*",                      "Int32",                "Decimal",              "((decimal) (int) value)"},
            new string[] {"*",                      "Int64",                "Decimal",              "((decimal) (long) value)"},
            new string[] {"*",                      "String",               "Decimal",              "XmlConvert.ToInteger((string) value)"},
            new string[] {"xs:decimal",             "String",               "Decimal",              "XmlConvert.ToDecimal((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "Decimal",              "((decimal) ((XmlAtomicValue) value).ValueAs(DecimalType))"},

            new string[] {"*",                      "Decimal",              "Int32",                "DecimalToInt32((decimal) value)"},
            new string[] {"*",                      "Int32",                "Int32",                "((int) value)"},
            new string[] {"*",                      "Int64",                "Int32",                "Int64ToInt32((long) value)"},
            new string[] {"*",                      "String",               "Int32",                "XmlConvert.ToInt32((string) value)"},
            new string[] {"xs:decimal",             "String",               "Int32",                "DecimalToInt32(XmlConvert.ToDecimal((string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "Int32",                "((XmlAtomicValue) value).ValueAsInt"},

            new string[] {"*",                      "Decimal",              "Int64",                "DecimalToInt64((decimal) value)"},
            new string[] {"*",                      "Int32",                "Int64",                "((long) (int) value)"},
            new string[] {"*",                      "Int64",                "Int64",                "((long) value)"},
            new string[] {"*",                      "String",               "Int64",                "XmlConvert.ToInt64((string) value)"},
            new string[] {"xs:decimal",             "String",               "Int64",                "DecimalToInt64(XmlConvert.ToDecimal((string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "Int64",                "((XmlAtomicValue) value).ValueAsLong"},

            new string[] {"*",                      "Decimal",              "String",               "XmlConvert.ToString(decimal.Truncate((decimal) value))"},
            new string[] {"xs:decimal",             "Decimal",              "String",               "XmlConvert.ToString((decimal) value)"},
            new string[] {"*",                      "Int32",                "String",               "XmlConvert.ToString((int) value)"},
            new string[] {"*",                      "Int64",                "String",               "XmlConvert.ToString((long) value)"},
            new string[] {"*",                      "String",               "String",               "((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "String",               "((XmlAtomicValue) value).Value"},

            new string[] {"*",                      "Decimal",              "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"*",                      "Int32",                "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (int) value))"},
            new string[] {"*",                      "Int64",                "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (long) value))"},
            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},

            new string[] {"*",                      "Decimal",              "XPathItem",            "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"*",                      "Int32",                "XPathItem",            "(new XmlAtomicValue(SchemaType, (int) value))"},
            new string[] {"*",                      "Int64",                "XPathItem",            "(new XmlAtomicValue(SchemaType, (long) value))"},
            new string[] {"*",                      "String",               "XPathItem",            "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},

            new string[] {"*",                      "*",                    "Byte",                 "Int32ToByte(this.ToInt32(value))"},
            new string[] {"*",                      "*",                    "Int16",                "Int32ToInt16(this.ToInt32(value))"},
            new string[] {"*",                      "*",                    "SByte",                "Int32ToSByte(this.ToInt32(value))"},
            new string[] {"*",                      "*",                    "UInt16",               "Int32ToUInt16(this.ToInt32(value))"},
            new string[] {"*",                      "*",                    "UInt32",               "Int64ToUInt32(this.ToInt64(value))"},
            new string[] {"*",                      "*",                    "UInt64",               "DecimalToUInt64(this.ToDecimal(value))"},

            new string[] {"*",                      "Byte",                 "*",                    "this.ChangeType((int) (byte) value, destinationType)"},
            new string[] {"*",                      "Int16",                "*",                    "this.ChangeType((int) (short) value, destinationType)"},
            new string[] {"*",                      "SByte",                "*",                    "this.ChangeType((int) (sbyte) value, destinationType)"},
            new string[] {"*",                      "UInt16",               "*",                    "this.ChangeType((int) (ushort) value, destinationType)"},
            new string[] {"*",                      "UInt32",               "*",                    "this.ChangeType((long) (uint) value, destinationType)"},
            new string[] {"*",                      "UInt64",               "*",                    "this.ChangeType((decimal) (ulong) value, destinationType)"},
            });

        // xs:double, xs:float, and derived types
        public static ConversionRuleGroup Numeric2RuleGroup =
            new ConversionRuleGroup("XmlNumeric2Converter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "Double",               "Double",               "((double) value)"},
            new string[] {"*",                      "Single",               "Double",               "((double) (float) value)"},
            new string[] {"*",                      "String",               "Double",               "XmlConvert.ToDouble((string) value)"},
            new string[] {"xs:float",               "String",               "Double",               "((double) XmlConvert.ToSingle((string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "Double",               "((XmlAtomicValue) value).ValueAsDouble"},

            new string[] {"*",                      "Double",               "Single",               "((float) (double) value)"},
            new string[] {"*",                      "Single",               "Single",               "((float) value)"},
            new string[] {"*",                      "String",               "Single",               "((float) XmlConvert.ToDouble((string) value))"},
            new string[] {"xs:float",               "String",               "Single",               "XmlConvert.ToSingle((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "Single",               "((float) ((XmlAtomicValue) value).ValueAs(SingleType))"},

            new string[] {"*",                      "Double",               "String",               "XmlConvert.ToString((double) value)"},
            new string[] {"xs:float",               "Double",               "String",               "XmlConvert.ToString(ToSingle((double) value))"},
            new string[] {"*",                      "Single",               "String",               "XmlConvert.ToString((double) (float) value)"},
            new string[] {"xs:float",               "Single",               "String",               "XmlConvert.ToString((float) value)"},
            new string[] {"*",                      "String",               "String",               "((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "String",               "((XmlAtomicValue) value).Value"},

            new string[] {"*",                      "Double",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (double) value))"},
            new string[] {"*",                      "Single",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},

            new string[] {"*",                      "Double",               "XPathItem",            "(new XmlAtomicValue(SchemaType, (double) value))"},
            new string[] {"*",                      "Single",               "XPathItem",            "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"*",                      "String",               "XPathItem",            "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},
            });

        // xs:dateTime, xs:date, xs:time, xs:gDay, xs:gMonth, xs:gMOnthDay, xs:gYear, xs:gYearMonth, and derived types
        public static ConversionRuleGroup DateTimeRuleGroup =
            new ConversionRuleGroup("XmlDateTimeConverter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "DateTime",             "DateTime",             "((DateTime) value)"},
            new string[] {"*",                      "String",               "DateTime",             "StringToDateTime((string) value)"},
            new string[] {"xs:date",                "String",               "DateTime",             "StringToDate((string) value)"},
            new string[] {"xs:time",                "String",               "DateTime",             "StringToTime((string) value)"},
            new string[] {"xs:gDay",                "String",               "DateTime",             "StringToGDay((string) value)"},
            new string[] {"xs:gMonth",              "String",               "DateTime",             "StringToGMonth((string) value)"},
            new string[] {"xs:gMonthDay",           "String",               "DateTime",             "StringToGMonthDay((string) value)"},
            new string[] {"xs:gYear",               "String",               "DateTime",             "StringToGYear((string) value)"},
            new string[] {"xs:gYearMonth",          "String",               "DateTime",             "StringToGYearMonth((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "DateTime",             "((XmlAtomicValue) value).ValueAsDateTime"},

            new string[] {"*",                      "DateTime",             "String",               "DateTimeToString((DateTime) value)"},
            new string[] {"xs:date",                "DateTime",             "String",               "DateToString((DateTime) value)"},
            new string[] {"xs:time",                "DateTime",             "String",               "TimeToString((DateTime) value)"},
            new string[] {"xs:gDay",                "DateTime",             "String",               "GDayToString((DateTime) value)"},
            new string[] {"xs:gMonth",              "DateTime",             "String",               "GMonthToString((DateTime) value)"},
            new string[] {"xs:gMonthDay",           "DateTime",             "String",               "GMonthDayToString((DateTime) value)"},
            new string[] {"xs:gYear",               "DateTime",             "String",               "GYearToString((DateTime) value)"},
            new string[] {"xs:gYearMonth",          "DateTime",             "String",               "GYearMonthToString((DateTime) value)"},
            new string[] {"*",                      "String",               "String",               "((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "String",               "((XmlAtomicValue) value).Value"},

            new string[] {"*",                      "DateTime",             "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (DateTime) value))"},
            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},

            new string[] {"*",                      "DateTime",             "XPathItem",            "(new XmlAtomicValue(SchemaType, (DateTime) value))"},
            new string[] {"*",                      "String",               "XPathItem",            "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},
            });

        // xs:boolean and derived types
        public static ConversionRuleGroup BooleanRuleGroup =
            new ConversionRuleGroup("XmlBooleanConverter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "Boolean",              "Boolean",              "((bool) value)"},
            new string[] {"*",                      "String",               "Boolean",              "XmlConvert.ToBoolean((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "Boolean",              "((XmlAtomicValue) value).ValueAsBoolean"},

            new string[] {"*",                      "Boolean",              "String",               "XmlConvert.ToString((bool) value)"},
            new string[] {"*",                      "String",               "String",               "((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "String",               "((XmlAtomicValue) value).Value"},

            new string[] {"*",                      "Boolean",              "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (bool) value))"},
            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},

            new string[] {"*",                      "Boolean",              "XPathItem",            "(new XmlAtomicValue(SchemaType, (bool) value))"},
            new string[] {"*",                      "String",               "XPathItem",            "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},
            });

        // xs:base64Binary, xs:hexBinary, xs:NOTATION, xs:QName, xs:anyUri, xs:duration, and derived types
        public static ConversionRuleGroup MiscRuleGroup =
            new ConversionRuleGroup("XmlMiscConverter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"xs:base64Binary",        "ByteArray",            "ByteArray",            "((byte[]) value)"},
            new string[] {"xs:hexBinary",           "ByteArray",            "ByteArray",            "((byte[]) value)"},
            new string[] {"xs:base64Binary",        "String",               "ByteArray",            "StringToBase64Binary((string) value)"},
            new string[] {"xs:hexBinary",           "String",               "ByteArray",            "StringToHexBinary((string) value)"},

            new string[] {"xs:NOTATION",            "String",               "XmlQualifiedName",     "StringToQName((string) value, nsResolver)"},
            new string[] {"xs:QName",               "String",               "XmlQualifiedName",     "StringToQName((string) value, nsResolver)"},
            new string[] {"xs:NOTATION",            "XmlQualifiedName",     "XmlQualifiedName",     "((XmlQualifiedName) value)"},
            new string[] {"xs:QName",               "XmlQualifiedName",     "XmlQualifiedName",     "((XmlQualifiedName) value)"},

            new string[] {"xs:base64Binary",        "ByteArray",            "String",               "Base64BinaryToString((byte[]) value)"},
            new string[] {"xs:hexBinary",           "ByteArray",            "String",               "XmlConvert.ToBinHexString((byte[]) value)"},
            new string[] {"*",                      "String",               "String",               "(string) value"},
            new string[] {"xs:anyURI",              "Uri",                  "String",               "AnyUriToString((Uri) value)"},
            new string[] {"xs:dayTimeDuration",     "TimeSpan",             "String",               "DayTimeDurationToString((TimeSpan) value)"},
            new string[] {"xs:duration",            "TimeSpan",             "String",               "DurationToString((TimeSpan) value)"},
            new string[] {"xs:yearMonthDuration",   "TimeSpan",             "String",               "YearMonthDurationToString((TimeSpan) value)"},
            new string[] {"xs:NOTATION",            "XmlQualifiedName",     "String",               "QNameToString((XmlQualifiedName) value, nsResolver)"},
            new string[] {"xs:QName",               "XmlQualifiedName",     "String",               "QNameToString((XmlQualifiedName) value, nsResolver)"},

            new string[] {"xs:dayTimeDuration",     "String",               "TimeSpan",             "StringToDayTimeDuration((string) value)"},
            new string[] {"xs:duration",            "String",               "TimeSpan",             "StringToDuration((string) value)"},
            new string[] {"xs:yearMonthDuration",   "String",               "TimeSpan",             "StringToYearMonthDuration((string) value)"},
            new string[] {"xs:dayTimeDuration",     "TimeSpan",             "TimeSpan",             "((TimeSpan) value)"},
            new string[] {"xs:duration",            "TimeSpan",             "TimeSpan",             "((TimeSpan) value)"},
            new string[] {"xs:yearMonthDuration",   "TimeSpan",             "TimeSpan",             "((TimeSpan) value)"},

            new string[] {"xs:anyURI",              "String",               "Uri",                  "XmlConvert.ToUri((string) value)"},
            new string[] {"xs:anyURI",              "Uri",                  "Uri",                  "((Uri) value)"},

            new string[] {"xs:base64Binary",        "ByteArray",            "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"xs:hexBinary",           "ByteArray",            "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (string)value, nsResolver))"},
            new string[] {"xs:dayTimeDuration",     "TimeSpan",             "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"xs:duration",            "TimeSpan",             "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"xs:yearMonthDuration",   "TimeSpan",             "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"xs:anyURI",              "Uri",                  "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},
            new string[] {"xs:NOTATION",            "XmlQualifiedName",     "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value, nsResolver))"},
            new string[] {"xs:QName",               "XmlQualifiedName",     "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, value, nsResolver))"},

            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},

            new string[] {"*",                      "*",                    "XPathItem",            "((XPathItem) this.ChangeType(value, XmlAtomicValueType, nsResolver))"},

            new string[] {"*",                      "XmlAtomicValue",       "*",                    "((XmlAtomicValue) value).ValueAs(destinationType, nsResolver)"},
            });

        // xs:string and derived types
        public static ConversionRuleGroup StringRuleGroup =
            new ConversionRuleGroup("XmlStringConverter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "String",               "String",               "((string) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "String",               "((XmlAtomicValue) value).Value"},

            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},

            new string[] {"*",                      "String",               "XPathItem",            "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},
            });

        // xs:untypedAtomic
        public static ConversionRuleGroup UntypedRuleGroup =
            new ConversionRuleGroup("XmlUntypedConverter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "String",               "Boolean",              "XmlConvert.ToBoolean((string) value)"},
            new string[] {"*",                      "String",               "Byte",                 "Int32ToByte(XmlConvert.ToInt32((string) value))"},
            new string[] {"*",                      "String",               "ByteArray",            "StringToBase64Binary((string) value)"},
            new string[] {"*",                      "String",               "DateTime",             "UntypedAtomicToDateTime((string) value)"},
            new string[] {"*",                      "String",               "Decimal",              "XmlConvert.ToDecimal((string) value)"},
            new string[] {"*",                      "String",               "Double",               "XmlConvert.ToDouble((string) value)"},
            new string[] {"*",                      "String",               "Int16",                "Int32ToInt16(XmlConvert.ToInt32((string) value))"},
            new string[] {"*",                      "String",               "Int32",                "XmlConvert.ToInt32((string) value)"},
            new string[] {"*",                      "String",               "Int64",                "XmlConvert.ToInt64((string) value)"},
            new string[] {"*",                      "String",               "SByte",                "Int32ToSByte(XmlConvert.ToInt32((string) value))"},
            new string[] {"*",                      "String",               "Single",               "XmlConvert.ToSingle((string) value)"},
            new string[] {"*",                      "String",               "TimeSpan",             "StringToDuration((string) value)"},
            new string[] {"*",                      "String",               "UInt16",               "Int32ToUInt16(XmlConvert.ToInt32((string) value))"},
            new string[] {"*",                      "String",               "UInt32",               "Int64ToUInt32(XmlConvert.ToInt64((string) value))"},
            new string[] {"*",                      "String",               "UInt64",               "DecimalToUInt64(XmlConvert.ToDecimal((string) value))"},
            new string[] {"*",                      "String",               "Uri",                  "XmlConvert.ToUri((string) value)"},
            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, (string) value))"},
            new string[] {"*",                      "String",               "XmlQualifiedName",     "StringToQName((string) value, nsResolver)"},
            new string[] {"*",                      "String",               "XPathItem",            "(new XmlAtomicValue(SchemaType, (string) value))"},

            new string[] {"*",                      "Boolean",              "String",               "XmlConvert.ToString((bool) value)"},
            new string[] {"*",                      "Byte",                 "String",               "XmlConvert.ToString((byte) value)"},
            new string[] {"*",                      "ByteArray",            "String",               "Base64BinaryToString((byte[]) value)"},
            new string[] {"*",                      "DateTime",             "String",               "DateTimeToString((DateTime) value)"},
            new string[] {"*",                      "Decimal",              "String",               "XmlConvert.ToString((decimal) value)"},
            new string[] {"*",                      "Double",               "String",               "XmlConvert.ToString((double) value)"},
            new string[] {"*",                      "Int16",                "String",               "XmlConvert.ToString((short) value)"},
            new string[] {"*",                      "Int32",                "String",               "XmlConvert.ToString((int) value)"},
            new string[] {"*",                      "Int64",                "String",               "XmlConvert.ToString((long) value)"},
            new string[] {"*",                      "SByte",                "String",               "XmlConvert.ToString((sbyte) value)"},
            new string[] {"*",                      "Single",               "String",               "XmlConvert.ToString((float) value)"},
            new string[] {"*",                      "String",               "String",               "((string) value)"},
            new string[] {"*",                      "TimeSpan",             "String",               "DurationToString((TimeSpan) value)"},
            new string[] {"*",                      "UInt16",               "String",               "XmlConvert.ToString((ushort) value)"},
            new string[] {"*",                      "UInt32",               "String",               "XmlConvert.ToString((uint) value)"},
            new string[] {"*",                      "UInt64",               "String",               "XmlConvert.ToString((ulong) value)"},
            new string[] {"*",                      "Uri",                  "String",               "AnyUriToString((Uri) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "String",               "((string) ((XmlAtomicValue) value).ValueAs(StringType, nsResolver))"},
            new string[] {"*",                      "XmlQualifiedName",     "String",               "QNameToString((XmlQualifiedName) value, nsResolver)"},

            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},
            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},

            new string[] {"*",                      "*",                    "XmlAtomicValue",       "(new XmlAtomicValue(SchemaType, this.ToString(value, nsResolver)))"},
            new string[] {"*",                      "*",                    "XPathItem",            "(new XmlAtomicValue(SchemaType, this.ToString(value, nsResolver)))"},
            new string[] {"*",                      "XmlAtomicValue",       "*",                    "((XmlAtomicValue) value).ValueAs(destinationType, nsResolver)"},
            });

        // node
        public static ConversionRuleGroup NodeRuleGroup =
            new ConversionRuleGroup("XmlNodeConverter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "XPathNavigator",       "XPathNavigator",       "((XPathNavigator) value)"},

            new string[] {"*",                      "XPathNavigator",       "XPathItem",            "((XPathItem) value)"},
            });

        // item, xs:anyAtomicType
        public static ConversionRuleGroup AnyRuleGroup =
            new ConversionRuleGroup("XmlAnyConverter", new string[][]
            {
            //            Xml Type                  Source Clr Type         Destination Clr Type    Conversion Logic
            // =====================================================================================================
            new string[] {"*",                      "XmlAtomicValue",       "Boolean",              "((XmlAtomicValue) value).ValueAsBoolean"},
            new string[] {"*",                      "XmlAtomicValue",       "DateTime",             "((XmlAtomicValue) value).ValueAsDateTime"},
            new string[] {"*",                      "XmlAtomicValue",       "Decimal",              "((decimal) ((XmlAtomicValue) value).ValueAs(DecimalType))"},
            new string[] {"*",                      "XmlAtomicValue",       "Double",               "((XmlAtomicValue) value).ValueAsDouble"},
            new string[] {"*",                      "XmlAtomicValue",       "Int32",                "((XmlAtomicValue) value).ValueAsInt"},
            new string[] {"*",                      "XmlAtomicValue",       "Int64",                "((XmlAtomicValue) value).ValueAsLong"},
            new string[] {"*",                      "XmlAtomicValue",       "Single",               "((float) ((XmlAtomicValue) value).ValueAs(SingleType))"},
            new string[] {"*",                      "XmlAtomicValue",       "XmlAtomicValue",       "((XmlAtomicValue) value)"},

            new string[] {"*",                      "Boolean",              "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean), (bool) value))"},
            new string[] {"*",                      "Byte",                 "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.UnsignedByte), value))"},
            new string[] {"*",                      "ByteArray",            "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Base64Binary), value))"},
            new string[] {"*",                      "DateTime",             "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.DateTime), (DateTime) value))"},
            new string[] {"*",                      "Decimal",              "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Decimal), value))"},
            new string[] {"*",                      "Double",               "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Double), (double) value))"},
            new string[] {"*",                      "Int16",                "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Short), value))"},
            new string[] {"*",                      "Int32",                "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Int), (int) value))"},
            new string[] {"*",                      "Int64",                "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Long), (long) value))"},
            new string[] {"*",                      "SByte",                "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Byte), value))"},
            new string[] {"*",                      "Single",               "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Float), value))"},
            new string[] {"*",                      "String",               "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), (string) value))"},
            new string[] {"*",                      "TimeSpan",             "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Duration), value))"},
            new string[] {"*",                      "UInt16",               "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.UnsignedShort), value))"},
            new string[] {"*",                      "UInt32",               "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.UnsignedInt), value))"},
            new string[] {"*",                      "UInt64",               "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.UnsignedLong), value))"},
            new string[] {"*",                      "Uri",                  "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.AnyUri), value))"},
            new string[] {"*",                      "XmlQualifiedName",     "XmlAtomicValue",       "(new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.QName), value, nsResolver))"},

            new string[] {"*",                      "XmlAtomicValue",       "XPathItem",            "((XmlAtomicValue) value)"},
            new string[] {"*",                      "XPathNavigator",       "XPathItem",            "((XPathNavigator) value)"},

            new string[] {"*",                      "XPathNavigator",       "XPathNavigator",       "ToNavigator((XPathNavigator) value)"},

            new string[] {"*",                      "XmlAtomicValue",       "*",                    "((XmlAtomicValue) value).ValueAs(destinationType, nsResolver)"},
            new string[] {"*",                      "*",                    "XPathItem",            "((XPathItem) this.ChangeType(value, XmlAtomicValueType, nsResolver))"},
            });

        public static ConversionRuleGroup[] ConversionsRules = {
            Numeric10RuleGroup,
            Numeric2RuleGroup,
            DateTimeRuleGroup,
            BooleanRuleGroup,
            MiscRuleGroup,
            StringRuleGroup,
            UntypedRuleGroup,
            NodeRuleGroup,
            AnyRuleGroup,
        };

        public static void Main() {
            (new Generator()).Generate();
        }

        public void Generate() {
            AutoGenWriter autoGenWriter;

            //-----------------------------------------------
            // XmlBaseConverter
            //-----------------------------------------------

            // Output list of all CLR types used by the generated code
            autoGenWriter = new AutoGenWriter("XmlValueConverter.cs", "AUTOGENERATED_XMLBASECONVERTER");
            this.w = autoGenWriter.OpenIndented();

            List<Type> uniqueTypes = new List<Type>();
            foreach (ConversionRuleGroup group in ConversionsRules) {
                foreach (Type tSrc in group.FindUniqueSourceTypes(null))
                    if (!uniqueTypes.Contains(tSrc)) uniqueTypes.Add(tSrc);

                foreach (Type tDst in group.FindUniqueDestinationTypes(null))
                    if (!uniqueTypes.Contains(tDst)) uniqueTypes.Add(tDst);
            }

            foreach (Type t in uniqueTypes)
                this.w.WriteLine("protected static readonly Type " + ClrTypeName(t) + "Type = typeof(" + ClrTypeToCSharpName(t) + ");");

            this.w.WriteLine();

            // Output default methods which call ChangeType
            foreach (Type tDst in InterfaceTypes) {
                foreach (Type tSrc in InterfaceTypes) {
                    if (tDst == typeof(object) && tSrc == typeof(object))
                        continue;

                    StartMethodSignature(tSrc, tDst);
                    this.w.Write("return (" + ClrTypeToCSharpName(tDst) + ") ChangeType((object) value, ");

                    if (tDst == typeof(object))
                        this.w.Write("destinationType");
                    else
                        this.w.Write(ClrTypeName(tDst) + "Type");

                    this.w.WriteLine(", " + (MethodHasResolver(tSrc, tDst) ? "nsResolver" : "null") + "); }");
                }

                if (tDst == typeof(string)) {
                    this.w.WriteLine("public override string ToString(string value) {return this.ToString(value, null); }");
                    this.w.WriteLine("public override string ToString(object value) {return this.ToString(value, null); }");
                }

                if (tDst == typeof(object)) {
                    this.w.WriteLine("public override object ChangeType(string value, Type destinationType) {return this.ChangeType(value, destinationType, null); }");
                    this.w.WriteLine("public override object ChangeType(object value, Type destinationType) {return this.ChangeType(value, destinationType, null); }");
                }

                this.w.WriteLine();
            }

            autoGenWriter.Close();


            //-----------------------------------------------
            // Other Converters
            //-----------------------------------------------

            foreach (ConversionRuleGroup group in ConversionsRules) {
                IList<Type> uniqueSourceTypes, uniqueDestTypes;

                autoGenWriter = new AutoGenWriter("XmlValueConverter.cs", "AUTOGENERATED_" + group.Name.ToUpper());
                this.w = autoGenWriter.OpenIndented();

                foreach (Type tDst in InterfaceTypes) {
                    // Handle ChangeType methods later
                    if (tDst == typeof(object))
                        continue;

                    this.w.WriteLine();
                    this.w.WriteLine("//-----------------------------------------------");
                    this.w.WriteLine("// To" + ClrTypeName(tDst));
                    this.w.WriteLine("//-----------------------------------------------");
                    this.w.WriteLine();

                    // Create strongly-typed ToXXX methods
                    foreach (Type tSrc in InterfaceTypes) {
                        // Handle ToXXX(object value) method later
                        if (tSrc == typeof(object))
                            continue;

                        IList<ConversionRule> rules = group.Find(XmlTypeCode.None, tSrc, tDst);
                        if (rules.Count > 0) {
                            ConversionRule defaultRule = FindDefaultRule(rules);
                            if (defaultRule == null)
                                throw new Exception("If conversion from " + tSrc.Name + " to " + tDst.Name + " exists, a default conversion should also be defined.");

                            // ToXXX(T value)
                            StartMethod(tSrc, tDst);
                            GenerateConversions(defaultRule, rules);
                            EndMethod();
                        }
                    }

                    // Gather all unique source types which have destination type "tDst"
                    uniqueSourceTypes = group.FindUniqueSourceTypes(tDst);
                    if (uniqueSourceTypes.Count > 0) {
                        // ToXXX(object value);
                        StartMethod(typeof(object), tDst);

                        this.w.WriteLine("Type sourceType = value.GetType();");
                        this.w.WriteLine();

                        foreach (Type tSrc in uniqueSourceTypes)
                            GenerateConversionsTo(group.Find(XmlTypeCode.None, tSrc, tDst));

                        // If wildcard destination conversions exist, then delegate to ChangeTypeWildcardDestination method to handle them
                        this.w.WriteLine();
                        this.w.Write("return (" + ClrTypeToCSharpName(tDst) + ") ");
                        this.w.Write(group.FindUniqueSourceTypes(typeof(object)).Count > 0 ? "ChangeTypeWildcardDestination" : "ChangeListType");
                        this.w.WriteLine("(value, " + ClrTypeName(tDst) + "Type, " + (MethodHasResolver(typeof(object), tDst) ? "nsResolver);" : "null);"));

                        EndMethod();
                    }
                    else {
                        this.w.WriteLine("// This converter does not support conversions to " + ClrTypeName(tDst) + ".");
                    }

                    this.w.WriteLine();
                }

                this.w.WriteLine();
                this.w.WriteLine("//-----------------------------------------------");
                this.w.WriteLine("// ChangeType");
                this.w.WriteLine("//-----------------------------------------------");
                this.w.WriteLine();

                foreach (Type tSrc in InterfaceTypes) {
                    // Handle ChangeType(object) later
                    if (tSrc == typeof(object))
                        continue;

                    // Gather all unique destination types which have source type "tSrc"
                    uniqueDestTypes = group.FindUniqueDestinationTypes(tSrc);
                    if (uniqueDestTypes.Count > 0) {
                        // ChangeType(T value, Type destinationType);
                        StartMethod(tSrc, typeof(object));

                        this.w.WriteLine("if (destinationType == ObjectType) destinationType = DefaultClrType;");

                        foreach (Type tDst in uniqueDestTypes)
                            GenerateConversionsFrom(group.Find(XmlTypeCode.None, tSrc, tDst));

                        // If wildcard source conversions exist, then delegate to ChangeTypeWildcardSource method to handle them
                        this.w.WriteLine();
                        if (group.FindUniqueDestinationTypes(typeof(object)).Count > 0)
                            this.w.Write("return ChangeTypeWildcardSource(value, destinationType, ");
                        else
                            this.w.Write("return ChangeListType(value, destinationType, ");

                        this.w.WriteLine(MethodHasResolver(typeof(object), tSrc) ? "nsResolver);" : "null);");

                        EndMethod();

                        this.w.WriteLine();
                    }
                }

                // object ChangeType(object value, Type destinationType, IXmlNamespaceResolver resolver);
                StartMethod(typeof(object), typeof(object));

                this.w.WriteLine("Type sourceType = value.GetType();");
                this.w.WriteLine();

                // Generate conversions to destinationType
                this.w.WriteLine("if (destinationType == ObjectType) destinationType = DefaultClrType;");

                // Strongly-typed destinations
                foreach (Type tDst in group.FindUniqueDestinationTypes(null)) {
                    // Only output conversions if the destination is not a wildcard
                    if (tDst == typeof(object))
                        continue;

                    // Get source types that can be converted to the destination type
                    uniqueSourceTypes = group.FindUniqueSourceTypes(tDst);

                    // Remove wildcard source rules, as they are handled later
                    int i = 0;
                    while (i < uniqueSourceTypes.Count) {
                        if (uniqueSourceTypes[i] == typeof(object))
                            uniqueSourceTypes.RemoveAt(i);
                        else
                            i++;
                    }

                    if (uniqueSourceTypes.Count != 0) {
                        if (IsInterfaceMethod(tDst) && uniqueSourceTypes.Count > 1) {
                            this.w.Write("if (destinationType == " + ClrTypeName(tDst) + "Type) ");
                            this.w.WriteLine("return this.To" + ClrTypeName(tDst) + "(value" + (MethodHasResolver(tDst, tDst) ? ", nsResolver);" : ");"));
                        }
                        else {
                            this.w.WriteLine("if (destinationType == " + ClrTypeName(tDst) + "Type) {");
                            this.w.Indent++;

                            foreach (Type tSrc in uniqueSourceTypes) {
                                GenerateConversionsTo(group.Find(XmlTypeCode.None, tSrc, tDst));
                            }

                            this.w.Indent--;
                            this.w.WriteLine("}");
                        }
                    }
                }

                // Generate conversions from wildcard source types
                foreach (Type tDst in group.FindUniqueDestinationTypes(typeof(object)))
                    GenerateConversionsFrom(group.Find(XmlTypeCode.None, typeof(object), tDst));

                // Generate conversions to wildcard destination types
                foreach (Type tSrc in group.FindUniqueSourceTypes(typeof(object)))
                    GenerateConversionsTo(group.Find(XmlTypeCode.None, tSrc, typeof(object)));

                this.w.WriteLine();
                this.w.WriteLine("return ChangeListType(value, destinationType, nsResolver);");

                EndMethod();

                uniqueSourceTypes = group.FindUniqueSourceTypes(typeof(object));
                uniqueDestTypes = group.FindUniqueDestinationTypes(typeof(object));

                if (uniqueSourceTypes.Count != 0 || uniqueDestTypes.Count != 0) {
                    this.w.WriteLine();
                    this.w.WriteLine();
                    this.w.WriteLine("//-----------------------------------------------");
                    this.w.WriteLine("// Helpers");
                    this.w.WriteLine("//-----------------------------------------------");
                    this.w.WriteLine();

                    // Generate ChangeTypeWildcardDestination method, which performs conversions that are the same no matter what the destination type is
                    if (uniqueSourceTypes.Count != 0) {
                        this.w.WriteLine("private object ChangeTypeWildcardDestination(object value, Type destinationType, IXmlNamespaceResolver nsResolver) {");
                        this.w.Indent++;

                        this.w.WriteLine("Type sourceType = value.GetType();");
                        this.w.WriteLine();

                        foreach (Type tSrc in uniqueSourceTypes)
                            GenerateConversionsTo(group.Find(XmlTypeCode.None, tSrc, typeof(object)));

                        this.w.WriteLine();
                        this.w.WriteLine("return ChangeListType(value, destinationType, nsResolver);");

                        this.w.Indent--;
                        this.w.WriteLine("}");
                    }

                    // Generate ChangeTypeWildcardSource method, which performs conversions that are the same no matter what the source type is
                    if (uniqueDestTypes.Count != 0) {
                        this.w.WriteLine("private object ChangeTypeWildcardSource(object value, Type destinationType, IXmlNamespaceResolver nsResolver) {");
                        this.w.Indent++;

                        foreach (Type tDst in uniqueDestTypes)
                            GenerateConversionsFrom(group.Find(XmlTypeCode.None, typeof(object), tDst));

                        this.w.WriteLine();
                        this.w.WriteLine("return ChangeListType(value, destinationType, nsResolver);");

                        this.w.Indent--;
                        this.w.WriteLine("}");
                    }
                }

                autoGenWriter.Close();
            }
        }

        private void StartMethod(Type typeSrc, Type typeDst) {
            StartMethodSignature(typeSrc, typeDst);

            this.w.WriteLine();
            this.w.Indent++;

            if (!typeSrc.IsValueType) {
                this.w.WriteLine("if (value == null) throw new ArgumentNullException(\"value\");");

                if (typeDst != typeof(object))
                    this.w.WriteLine();
            }

            if (typeDst == typeof(object)) {
                this.w.WriteLine("if (destinationType == null) throw new ArgumentNullException(\"destinationType\");");
                this.w.WriteLine();
            }
        }

        private void StartMethodSignature(Type typeSrc, Type typeDst) {
            string methName, methSig;

            methSig = ClrTypeToCSharpName(typeSrc) + " value";

            if (typeDst == typeof(object)) {
                methName = "ChangeType";
                methSig += ", Type destinationType";
            }
            else {
                methName = "To" + ClrTypeName(typeDst);
            }

            if (MethodHasResolver(typeSrc, typeDst))
                methSig += ", IXmlNamespaceResolver nsResolver";

            this.w.Write("public override " + ClrTypeToCSharpName(typeDst) + " " + methName + "(" + methSig + ") {");
        }

        private void EndMethod() {
            this.w.Indent--;
            this.w.WriteLine("}");
        }

        private bool MethodHasResolver(Type typeSrc, Type typeDst) {
            if (typeSrc == typeof(object) || typeSrc == typeof(string)) {
                if (typeDst == typeof(object) || typeDst == typeof(string))
                    return true;
            }
            return false;
        }

        private ConversionRule FindDefaultRule(IList<ConversionRule> rules) {
            foreach (ConversionRule rule in rules) {
                if (rule.XmlType == XmlTypeCode.Item)
                    return rule;
            }
            return null;
        }

        private void GenerateConversions(ConversionRule defaultRule, IList<ConversionRule> rulesSwitch) {
            int cnt = rulesSwitch.Count;

            // Don't need to test TypeCode for default rule
            if (defaultRule != null)
                cnt--;

            if (cnt > 0) {
                if (cnt > 1) {
                    this.w.WriteLine("switch (TypeCode) {");
                }

                foreach (ConversionRule ruleSwitch in rulesSwitch) {
                    if (ruleSwitch != defaultRule) {
                        if (cnt > 1) {
                            this.w.Indent++;
                            this.w.WriteLine("case XmlTypeCode." + ruleSwitch.XmlType + ": return " + ruleSwitch.ConversionExpression + ";");
                            this.w.Indent--;
                        }
                        else {
                            this.w.WriteLine("if (TypeCode == XmlTypeCode." + ruleSwitch.XmlType + ") return " + ruleSwitch.ConversionExpression + ";");
                        }
                    }
                }

                if (cnt > 1) {
                    this.w.WriteLine("}");
                }
            }

            if (defaultRule != null)
                this.w.WriteLine("return " + defaultRule.ConversionExpression + ";");
        }

        private void GenerateConversionsTo(IList<ConversionRule> rules) {
            GenerateConversionsToFrom(rules, false);
        }

        private void GenerateConversionsFrom(IList<ConversionRule> rules) {
            GenerateConversionsToFrom(rules, true);
        }

        private void GenerateConversionsToFrom(IList<ConversionRule> rules, bool isFrom) {
            ConversionRule defaultRule = FindDefaultRule(rules);
            Type tSrc, tDst;

            // If no conversions exist, then don't generate anything
            if (rules.Count == 0)
                return;

            tSrc = rules[0].SourceType;
            tDst = rules[0].DestinationType;

            if (isFrom)
                this.w.Write("if (destinationType == " + ClrTypeName(tDst) + "Type) ");
            else
                this.w.Write("if (" + GenerateSourceTypeMatch(tSrc) + ") ");

            if (rules.Count > 1 && IsInterfaceMethod(tSrc) && IsInterfaceMethod(tDst)) {
                // There exists an interface method already which performs switch, so call it
                this.w.Write("return this.To" + ClrTypeName(tDst) + "((" + ClrTypeToCSharpName(tSrc) + ") value");
                this.w.WriteLine(MethodHasResolver(tSrc, tDst) ? ", nsResolver);" : ");");
            }
            else {
                // Inline the conversion
                if (rules.Count == 1) {
                    GenerateConversions(defaultRule, rules);
                }
                else {
                    this.w.WriteLine("{");
                    this.w.Indent++;

                    GenerateConversions(defaultRule, rules);
                    
                    this.w.Indent--;
                    this.w.WriteLine("}");
                }
            }
        }

        private string GenerateSourceTypeMatch(Type type) {
            if (type.IsValueType || type.IsSealed)
                return "sourceType == " + ClrTypeName(type) + "Type";

            if (type.IsInterface)
                return ClrTypeName(type) + "Type.IsAssignableFrom(sourceType)";

            return "IsDerivedFrom(sourceType, " + ClrTypeName(type) + "Type)";
        }

        private static string ClrTypeName(Type type) {
            if (type.IsArray)
                return type.GetElementType().Name + "Array";

            return type.Name;
        }

        private static string ClrTypeToCSharpName(Type type) {
            if (type == typeof(String)) return "string";
            if (type == typeof(SByte)) return "sbyte";
            if (type == typeof(Int16)) return "short";
            if (type == typeof(Int32)) return "int";
            if (type == typeof(Int64)) return "long";
            if (type == typeof(Byte)) return "byte";
            if (type == typeof(UInt16)) return "ushort";
            if (type == typeof(UInt32)) return "uint";
            if (type == typeof(UInt64)) return "ulong";
            if (type == typeof(Double)) return "double";
            if (type == typeof(Single)) return "float";
            if (type == typeof(Decimal)) return "decimal";
            if (type == typeof(Object)) return "object";
            if (type == typeof(Boolean)) return "bool";

            return type.Name;
        }

        private static bool IsInterfaceMethod(Type type) {
            return ((IList) InterfaceTypes).Contains(type);
        }
    }

    public class ConversionRuleGroup {
        private string groupName;
        private List<ConversionRule> rules;

        public ConversionRuleGroup(string groupName, string[][] rules) {
            this.groupName = groupName;
            this.rules = new List<ConversionRule>();

            foreach (string[] rule in rules) {
                XmlTypeCode xmlType = XmlTypeNameToTypeCode(rule[0]);
                Type clrTypeSrc = ClrTypeNameToType(rule[1]);
                Type clrTypeDst = ClrTypeNameToType(rule[2]);
                string convExpr = rule[3];

                AddRule(new ConversionRule(xmlType, clrTypeSrc, clrTypeDst, convExpr));
            }
        }

        public string Name {
            get { return this.groupName; }
        }

        public IList<Type> FindUniqueSourceTypes(Type tDst) {
            List<Type> types = new List<Type>();

            foreach (ConversionRule rule in Find(XmlTypeCode.None, null, tDst)) {
                if (!types.Contains(rule.SourceType))
                    types.Add(rule.SourceType);
            }

            return types;
        }

        public IList<Type> FindUniqueDestinationTypes(Type tSrc) {
            List<Type> types = new List<Type>();

            foreach (ConversionRule rule in Find(XmlTypeCode.None, tSrc, null)) {
                if (!types.Contains(rule.DestinationType))
                    types.Add(rule.DestinationType);
            }

            return types;
        }

        public IList<ConversionRule> Find(XmlTypeCode code, Type tSrc, Type tDst) {
            List<ConversionRule> subset = new List<ConversionRule>();

            foreach (ConversionRule rule in this.rules) {
                if (code == XmlTypeCode.None || code == rule.XmlType) {
                    if (tSrc == null || tSrc == rule.SourceType) {
                        if (tDst == null || tDst == rule.DestinationType) {
                            subset.Add(rule);
                        }
                    }
                }
            }

            return subset;
        }

        private void AddRule(ConversionRule ruleAdd) {
            for (int i = 0; i < this.rules.Count; i++) {
                ConversionRule rule = this.rules[i];

                if (rule.XmlType == ruleAdd.XmlType) {
                    if (rule.SourceType == ruleAdd.SourceType) {
                        if (rule.DestinationType == ruleAdd.DestinationType) {
                            // Override previous rule with new rule
                            this.rules[i] = ruleAdd;
                            return;
                        }
                    }
                }
            }

            this.rules.Add(ruleAdd);
        }

        private static XmlTypeCode XmlTypeNameToTypeCode(string name) {
            int idx;

            if (name == "*")
                return XmlTypeCode.Item;

            idx = name.IndexOf(':');
            if (idx != -1)
                name = name.Substring(idx + 1);

            return (XmlTypeCode) Enum.Parse(typeof(XmlTypeCode), name, true);
        }

        private static Type ClrTypeNameToType(string name) {
            Type type = Type.GetType("System." + name);
            if (type != null) return type;

            type = Type.GetType("System.IO." + name);
            if (type != null) return type;

            if (name == "*") return typeof(object);
            if (name == "ByteArray") return typeof(byte[]);
            if (name == "XmlQualifiedName") return typeof(XmlQualifiedName);
            if (name == "XmlAtomicValue") return typeof(XmlAtomicValue);
            if (name == "XPathNavigator") return typeof(XPathNavigator);
            if (name == "XPathItem") return typeof(XPathItem);
            if (name == "Uri") return typeof(Uri);
            if (name == "IEnumerable") return typeof(IEnumerable);

            throw new Exception("Unknown type " + name);
        }
    }

    public class ConversionRule {
        private XmlTypeCode xmlType;
        private Type clrTypeSrc, clrTypeDst;
        private string convExpr;

        public ConversionRule(XmlTypeCode xmlType, Type clrTypeSrc, Type clrTypeDst, string convExpr) {
            this.xmlType = xmlType;
            this.clrTypeSrc = clrTypeSrc;
            this.clrTypeDst = clrTypeDst;
            this.convExpr = convExpr;
        }

        public XmlTypeCode XmlType {
            get { return this.xmlType; }
        }

        public Type SourceType {
            get { return this.clrTypeSrc; }
        }

        public Type DestinationType {
            get { return this.clrTypeDst; }
        }

        public string ConversionExpression {
            get { return this.convExpr; }
        }
    }

    public class AutoGenWriter {
        private static char[] Whitespace = { ' ' };
        private FileStream fs;
        private StreamReader r;
        private StringWriter sw;
        private string regionName;
        private int indent;

        public AutoGenWriter(string fileName, string regionName) : this(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite), regionName) {
        }

        public AutoGenWriter(FileStream fs, string regionName) {
            if (!fs.CanSeek || !fs.CanRead || !fs.CanWrite)
                throw new Exception("Internal error: Unable to seek/read/write this filestream");
            this.fs = fs;
            this.r = new StreamReader(fs);
            this.sw = new StringWriter();
            this.regionName = regionName;
        }

        public int Indent { get { return this.indent; } }

        public TextWriter Open() {
            // Seek to the autogenerated region within the file
            for (string s = this.r.ReadLine(); s != null; s = this.r.ReadLine()) {
                this.sw.WriteLine(s);
                if (s.Trim(Whitespace).StartsWith("#region " + this.regionName)) {
                    if (s[0] != '#')
                        this.indent = s.IndexOf('#') / 4;
                    break;
                }
            }
            return this.sw;
        }

        internal IndentedTextWriter OpenIndented() {
            TextWriter w = Open();
            IndentedTextWriter iw = new IndentedTextWriter(w);
            iw.Indent = this.indent;
            for (int i = 0; i < this.indent; i++)
                w.Write("    ");
            return iw;
        }

        internal void Close() {
            // End the autogenerated region
            for (string s = this.r.ReadLine(); s != null; s = this.r.ReadLine()) {
                string ss = s.Trim(Whitespace);
                if (ss.StartsWith("#endregion")) {
                    this.sw.WriteLine(s);
                    break;
                }
            }

            for (string s = r.ReadLine(); s != null; s = this.r.ReadLine()) {
                this.sw.WriteLine(s);
            }

            this.fs.SetLength(0);
            this.fs.Seek(0, SeekOrigin.Begin);
            StreamWriter w = new StreamWriter(fs);
            w.Write(this.sw.ToString());
            w.Close();
            this.sw.Close();
        }
    }
}
