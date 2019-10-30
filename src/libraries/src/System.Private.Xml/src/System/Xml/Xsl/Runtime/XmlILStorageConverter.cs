// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// This is a simple convenience wrapper internal class that contains static helper methods that get a value
    /// converter from XmlQueryRuntime and use it convert among several physical Clr representations for
    /// the same logical Xml type.  For example, an external function might have an argument typed as
    /// xs:integer, with Clr type Decimal.  Since ILGen stores xs:integer as Clr type Int64 instead of
    /// Decimal, a conversion to the desired storage type must take place.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class XmlILStorageConverter
    {
        //-----------------------------------------------
        // ToAtomicValue
        //-----------------------------------------------

        public static XmlAtomicValue StringToAtomicValue(string value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue DecimalToAtomicValue(decimal value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue Int64ToAtomicValue(long value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue Int32ToAtomicValue(int value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue BooleanToAtomicValue(bool value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue DoubleToAtomicValue(double value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue SingleToAtomicValue(float value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue DateTimeToAtomicValue(DateTime value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue XmlQualifiedNameToAtomicValue(XmlQualifiedName value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue TimeSpanToAtomicValue(TimeSpan value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static XmlAtomicValue BytesToAtomicValue(byte[] value, int index, XmlQueryRuntime runtime)
        {
            return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
        }

        public static IList<XPathItem> NavigatorsToItems(IList<XPathNavigator> listNavigators)
        {
            // Check to see if the navigator cache implements IList<XPathItem>
            IList<XPathItem> listItems = listNavigators as IList<XPathItem>;
            if (listItems != null)
                return listItems;

            // Create XmlQueryNodeSequence, which does implement IList<XPathItem>
            return new XmlQueryNodeSequence(listNavigators);
        }

        public static IList<XPathNavigator> ItemsToNavigators(IList<XPathItem> listItems)
        {
            // Check to see if the navigator cache implements IList<XPathNavigator>
            IList<XPathNavigator> listNavs = listItems as IList<XPathNavigator>;
            if (listNavs != null)
                return listNavs;

            // Create XmlQueryNodeSequence, which does implement IList<XPathNavigator>
            XmlQueryNodeSequence seq = new XmlQueryNodeSequence(listItems.Count);
            for (int i = 0; i < listItems.Count; i++)
                seq.Add((XPathNavigator)listItems[i]);

            return seq;
        }
    }
}
