// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Xml.Schema;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Contains conversion routines used by Xslt.  These conversions fall into several categories:
    ///   1. Internal type to internal type: These are conversions from one of the five Xslt types to another
    ///      of the five types.
    ///   2. External type to internal type: These are conversions from any of the Xsd types to one of the five
    ///      Xslt types.
    ///   3. Internal type to external type: These are conversions from one of the five Xslt types to any of
    ///      of the Xsd types.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class XsltConvert
    {
        internal static readonly Type BooleanType = typeof(bool);
        internal static readonly Type ByteArrayType = typeof(byte[]);
        internal static readonly Type ByteType = typeof(byte);
        internal static readonly Type DateTimeType = typeof(DateTime);
        internal static readonly Type DecimalType = typeof(decimal);
        internal static readonly Type DoubleType = typeof(double);
        internal static readonly Type ICollectionType = typeof(ICollection);
        internal static readonly Type IEnumerableType = typeof(IEnumerable);
        internal static readonly Type IListType = typeof(IList);
        internal static readonly Type Int16Type = typeof(short);
        internal static readonly Type Int32Type = typeof(int);
        internal static readonly Type Int64Type = typeof(long);
        internal static readonly Type IXPathNavigableType = typeof(IXPathNavigable);
        internal static readonly Type ObjectType = typeof(object);
        internal static readonly Type SByteType = typeof(sbyte);
        internal static readonly Type SingleType = typeof(float);
        internal static readonly Type StringType = typeof(string);
        internal static readonly Type TimeSpanType = typeof(TimeSpan);
        internal static readonly Type UInt16Type = typeof(ushort);
        internal static readonly Type UInt32Type = typeof(uint);
        internal static readonly Type UInt64Type = typeof(ulong);
        internal static readonly Type UriType = typeof(Uri);
        internal static readonly Type VoidType = typeof(void);
        internal static readonly Type XmlAtomicValueType = typeof(XmlAtomicValue);
        internal static readonly Type XmlQualifiedNameType = typeof(XmlQualifiedName);
        internal static readonly Type XPathItemType = typeof(XPathItem);
        internal static readonly Type XPathNavigatorArrayType = typeof(XPathNavigator[]);
        internal static readonly Type XPathNavigatorType = typeof(XPathNavigator);
        internal static readonly Type XPathNodeIteratorType = typeof(XPathNodeIterator);


        //------------------------------------------------------------------------
        // ToBoolean (internal type to internal type)
        //------------------------------------------------------------------------

        public static bool ToBoolean(XPathItem item)
        {
            XsltLibrary.CheckXsltValue(item);

            if (item.IsNode)
                return true;

            Type itemType = item.ValueType;

            if (itemType == StringType)
            {
                return item.Value.Length != 0;
            }
            else if (itemType == DoubleType)
            {
                // (x < 0 || 0 < x)  ==  (x != 0) && !Double.IsNaN(x)
                double dbl = item.ValueAsDouble;
                return dbl < 0 || 0 < dbl;
            }
            else
            {
                Debug.Assert(itemType == BooleanType, $"Unexpected type of atomic sequence {itemType}");
                return item.ValueAsBoolean;
            }
        }

        public static bool ToBoolean(IList<XPathItem> listItems)
        {
            XsltLibrary.CheckXsltValue(listItems);

            if (listItems.Count == 0)
                return false;

            return ToBoolean(listItems[0]);
        }


        //------------------------------------------------------------------------
        // ToDouble (internal type to internal type)
        //------------------------------------------------------------------------

        public static double ToDouble(string value)
        {
            return XPathConvert.StringToDouble(value);
        }

        public static double ToDouble(XPathItem item)
        {
            XsltLibrary.CheckXsltValue(item);

            if (item.IsNode)
                return XPathConvert.StringToDouble(item.Value);

            Type itemType = item.ValueType;

            if (itemType == StringType)
            {
                return XPathConvert.StringToDouble(item.Value);
            }
            else if (itemType == DoubleType)
            {
                return item.ValueAsDouble;
            }
            else
            {
                Debug.Assert(itemType == BooleanType, $"Unexpected type of atomic sequence {itemType}");
                return item.ValueAsBoolean ? 1d : 0d;
            }
        }

        public static double ToDouble(IList<XPathItem> listItems)
        {
            XsltLibrary.CheckXsltValue(listItems);

            if (listItems.Count == 0)
                return double.NaN;

            return ToDouble(listItems[0]);
        }


        //------------------------------------------------------------------------
        // ToNode (internal type to internal type)
        //------------------------------------------------------------------------

        public static XPathNavigator ToNode(XPathItem item)
        {
            XsltLibrary.CheckXsltValue(item);

            if (!item.IsNode)
            {
                // Create Navigator over text node containing string value of item
                XPathDocument doc = new XPathDocument();
                XmlRawWriter writer = doc.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames, string.Empty);
                writer.WriteString(ToString(item));
                writer.Close();
                return doc.CreateNavigator();
            }

            RtfNavigator rtf = item as RtfNavigator;
            if (rtf != null)
                return rtf.ToNavigator();

            return (XPathNavigator)item;
        }

        public static XPathNavigator ToNode(IList<XPathItem> listItems)
        {
            XsltLibrary.CheckXsltValue(listItems);

            if (listItems.Count == 1)
                return ToNode(listItems[0]);

            throw new XslTransformException(SR.Xslt_NodeSetNotNode, string.Empty);
        }


        //------------------------------------------------------------------------
        // ToNodes (internal type to internal type)
        //------------------------------------------------------------------------

        public static IList<XPathNavigator> ToNodeSet(XPathItem item)
        {
            return new XmlQueryNodeSequence(ToNode(item));
        }

        public static IList<XPathNavigator> ToNodeSet(IList<XPathItem> listItems)
        {
            XsltLibrary.CheckXsltValue(listItems);

            if (listItems.Count == 1)
                return new XmlQueryNodeSequence(ToNode(listItems[0]));

            return XmlILStorageConverter.ItemsToNavigators(listItems);
        }


        //------------------------------------------------------------------------
        // ToString (internal type to internal type)
        //------------------------------------------------------------------------

        public static string ToString(double value)
        {
            return XPathConvert.DoubleToString(value);
        }

        public static string ToString(XPathItem item)
        {
            XsltLibrary.CheckXsltValue(item);

            // Use XPath 1.0 rules to convert double to string
            if (!item.IsNode && item.ValueType == DoubleType)
                return XPathConvert.DoubleToString(item.ValueAsDouble);

            return item.Value;
        }

        public static string ToString(IList<XPathItem> listItems)
        {
            XsltLibrary.CheckXsltValue(listItems);

            if (listItems.Count == 0)
                return string.Empty;

            return ToString(listItems[0]);
        }


        //------------------------------------------------------------------------
        // External type to internal type
        //------------------------------------------------------------------------

        public static string ToString(DateTime value)
        {
            return (new XsdDateTime(value, XsdDateTimeFlags.DateTime)).ToString();
        }

        public static double ToDouble(decimal value)
        {
            return (double)value;
        }

        public static double ToDouble(int value)
        {
            return (double)value;
        }

        public static double ToDouble(long value)
        {
            return (double)value;
        }


        //------------------------------------------------------------------------
        // Internal type to external type
        //------------------------------------------------------------------------

        public static decimal ToDecimal(double value)
        {
            checked { return (decimal)value; }
        }

        public static int ToInt(double value)
        {
            checked { return (int)value; }
        }

        public static long ToLong(double value)
        {
            checked { return (long)value; }
        }

        public static DateTime ToDateTime(string value)
        {
            return (DateTime)(new XsdDateTime(value, XsdDateTimeFlags.AllXsd));
        }


        //------------------------------------------------------------------------
        // External type to external type
        //------------------------------------------------------------------------

        internal static XmlAtomicValue ConvertToType(XmlAtomicValue value, XmlQueryType destinationType)
        {
            Debug.Assert(destinationType.IsStrict && destinationType.IsAtomicValue, "Can only convert to strict atomic type.");

            // This conversion matrix should match the one in XmlILVisitor.GetXsltConvertMethod
            switch (destinationType.TypeCode)
            {
                case XmlTypeCode.Boolean:
                    switch (value.XmlType.TypeCode)
                    {
                        case XmlTypeCode.Boolean:
                        case XmlTypeCode.Double:
                        case XmlTypeCode.String:
                            return new XmlAtomicValue(destinationType.SchemaType, ToBoolean(value));
                    }
                    break;

                case XmlTypeCode.DateTime:
                    if (value.XmlType.TypeCode == XmlTypeCode.String)
                        return new XmlAtomicValue(destinationType.SchemaType, ToDateTime(value.Value));
                    break;

                case XmlTypeCode.Decimal:
                    if (value.XmlType.TypeCode == XmlTypeCode.Double)
                        return new XmlAtomicValue(destinationType.SchemaType, ToDecimal(value.ValueAsDouble));
                    break;

                case XmlTypeCode.Double:
                    switch (value.XmlType.TypeCode)
                    {
                        case XmlTypeCode.Boolean:
                        case XmlTypeCode.Double:
                        case XmlTypeCode.String:
                            return new XmlAtomicValue(destinationType.SchemaType, ToDouble(value));

                        case XmlTypeCode.Decimal:
                            return new XmlAtomicValue(destinationType.SchemaType, ToDouble((decimal)value.ValueAs(DecimalType, null)));

                        case XmlTypeCode.Int:
                        case XmlTypeCode.Long:
                            return new XmlAtomicValue(destinationType.SchemaType, ToDouble(value.ValueAsLong));
                    }
                    break;

                case XmlTypeCode.Int:
                case XmlTypeCode.Long:
                    if (value.XmlType.TypeCode == XmlTypeCode.Double)
                        return new XmlAtomicValue(destinationType.SchemaType, ToLong(value.ValueAsDouble));
                    break;

                case XmlTypeCode.String:
                    switch (value.XmlType.TypeCode)
                    {
                        case XmlTypeCode.Boolean:
                        case XmlTypeCode.Double:
                        case XmlTypeCode.String:
                            return new XmlAtomicValue(destinationType.SchemaType, ToString(value));

                        case XmlTypeCode.DateTime:
                            return new XmlAtomicValue(destinationType.SchemaType, ToString(value.ValueAsDateTime));
                    }
                    break;
            }

            Debug.Fail("Conversion from " + value.XmlType.QualifiedName.Name + " to " + destinationType + " is not supported.");
            return value;
        }


        //------------------------------------------------------------------------
        // EnsureXXX methods (TreatAs)
        //------------------------------------------------------------------------

        public static IList<XPathNavigator> EnsureNodeSet(IList<XPathItem> listItems)
        {
            XsltLibrary.CheckXsltValue(listItems);

            if (listItems.Count == 1)
            {
                XPathItem item = listItems[0];
                if (!item.IsNode)
                    throw new XslTransformException(SR.XPath_NodeSetExpected, string.Empty);

                if (item is RtfNavigator)
                    throw new XslTransformException(SR.XPath_RtfInPathExpr, string.Empty);
            }

            return XmlILStorageConverter.ItemsToNavigators(listItems);
        }


        //------------------------------------------------------------------------
        // InferXsltType
        //------------------------------------------------------------------------

        /// <summary>
        /// Infer one of the Xslt types from "clrType" -- Boolean, Double, String, Node, Node*, Item*.
        /// </summary>
        internal static XmlQueryType InferXsltType(Type clrType)
        {
            if (clrType == BooleanType) return XmlQueryTypeFactory.BooleanX;
            if (clrType == ByteType) return XmlQueryTypeFactory.DoubleX;
            if (clrType == DecimalType) return XmlQueryTypeFactory.DoubleX;
            if (clrType == DateTimeType) return XmlQueryTypeFactory.StringX;
            if (clrType == DoubleType) return XmlQueryTypeFactory.DoubleX;
            if (clrType == Int16Type) return XmlQueryTypeFactory.DoubleX;
            if (clrType == Int32Type) return XmlQueryTypeFactory.DoubleX;
            if (clrType == Int64Type) return XmlQueryTypeFactory.DoubleX;
            if (clrType == IXPathNavigableType) return XmlQueryTypeFactory.NodeNotRtf;
            if (clrType == SByteType) return XmlQueryTypeFactory.DoubleX;
            if (clrType == SingleType) return XmlQueryTypeFactory.DoubleX;
            if (clrType == StringType) return XmlQueryTypeFactory.StringX;
            if (clrType == UInt16Type) return XmlQueryTypeFactory.DoubleX;
            if (clrType == UInt32Type) return XmlQueryTypeFactory.DoubleX;
            if (clrType == UInt64Type) return XmlQueryTypeFactory.DoubleX;
            if (clrType == XPathNavigatorArrayType) return XmlQueryTypeFactory.NodeSDod;
            if (clrType == XPathNavigatorType) return XmlQueryTypeFactory.NodeNotRtf;
            if (clrType == XPathNodeIteratorType) return XmlQueryTypeFactory.NodeSDod;
            if (clrType.IsEnum) return XmlQueryTypeFactory.DoubleX;
            if (clrType == VoidType) return XmlQueryTypeFactory.Empty;

            return XmlQueryTypeFactory.ItemS;
        }
    }
}
