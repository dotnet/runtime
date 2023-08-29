// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.XPath;

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
        //------------------------------------------------------------------------
        // ToBoolean (internal type to internal type)
        //------------------------------------------------------------------------

        public static bool ToBoolean(XPathItem item)
        {
            XsltLibrary.CheckXsltValue(item);

            if (item.IsNode)
                return true;

            Type itemType = item.ValueType;

            if (itemType == typeof(string))
            {
                return item.Value.Length != 0;
            }
            else if (itemType == typeof(double))
            {
                // (x < 0 || 0 < x)  ==  (x != 0) && !Double.IsNaN(x)
                double dbl = item.ValueAsDouble;
                return dbl < 0 || 0 < dbl;
            }
            else
            {
                Debug.Assert(itemType == typeof(bool), $"Unexpected type of atomic sequence {itemType}");
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

            if (itemType == typeof(string))
            {
                return XPathConvert.StringToDouble(item.Value);
            }
            else if (itemType == typeof(double))
            {
                return item.ValueAsDouble;
            }
            else
            {
                Debug.Assert(itemType == typeof(bool), $"Unexpected type of atomic sequence {itemType}");
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

            RtfNavigator? rtf = item as RtfNavigator;
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
            if (!item.IsNode && item.ValueType == typeof(double))
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
                            return new XmlAtomicValue(destinationType.SchemaType, ToDouble((decimal)value.ValueAs(typeof(decimal), null)));

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

            Debug.Fail($"Conversion from {value.XmlType.QualifiedName.Name} to {destinationType} is not supported.");
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
            if (clrType == typeof(bool)) return XmlQueryTypeFactory.BooleanX;
            if (clrType == typeof(byte)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(decimal)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(DateTime)) return XmlQueryTypeFactory.StringX;
            if (clrType == typeof(double)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(short)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(int)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(long)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(IXPathNavigable)) return XmlQueryTypeFactory.NodeNotRtf;
            if (clrType == typeof(sbyte)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(float)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(string)) return XmlQueryTypeFactory.StringX;
            if (clrType == typeof(ushort)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(uint)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(ulong)) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(XPathNavigator[])) return XmlQueryTypeFactory.NodeSDod;
            if (clrType == typeof(XPathNavigator)) return XmlQueryTypeFactory.NodeNotRtf;
            if (clrType == typeof(XPathNodeIterator)) return XmlQueryTypeFactory.NodeSDod;
            if (clrType.IsEnum) return XmlQueryTypeFactory.DoubleX;
            if (clrType == typeof(void)) return XmlQueryTypeFactory.Empty;

            return XmlQueryTypeFactory.ItemS;
        }
    }
}
