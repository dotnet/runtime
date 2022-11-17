// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 0618 // ignore obsolete warning about XmlDataDocument

namespace System.Xml
{
    //
    // Maps XML nodes to schema
    //
    // With the exception of some functions (the most important is SearchMatchingTableSchema) all functions expect that each region rowElem is already associated
    // w/ it's DataRow (basically the test to determine a rowElem is based on a != null associated DataRow). As a result of this, some functions will NOT work properly
    // when they are used on a tree for which rowElem's are not associated w/ a DataRow.
    //

    internal sealed class DataSetMapper
    {
        private Hashtable _tableSchemaMap;   // maps an string (currently this is localName:nsURI) to a DataTable. Used to quickly find if a bound-elem matches any data-table metadata..
        private Hashtable _columnSchemaMap;  // maps a string (table localName:nsURI) to a Hashtable. The 2nd hastable (the one that is stored as data in columnSchemaMap, maps a string to a DataColumn.

        private XmlDataDocument? _doc;        // The document this mapper is related to
        private DataSet? _dataSet;          // The dataset this mapper is related to
        internal const string strReservedXmlns = "http://www.w3.org/2000/xmlns/";

        internal DataSetMapper()
        {
            Debug.Assert(_dataSet == null);
            _tableSchemaMap = new Hashtable();
            _columnSchemaMap = new Hashtable();
        }

        internal void SetupMapping(XmlDataDocument xd, DataSet ds)
        {
            // If are already mapped, forget about our current mapping and re-do it again.
            if (IsMapped())
            {
                _tableSchemaMap = new Hashtable();
                _columnSchemaMap = new Hashtable();
            }
            _doc = xd;
            _dataSet = ds;
            foreach (DataTable t in _dataSet.Tables)
            {
                AddTableSchema(t);

                foreach (DataColumn c in t.Columns)
                {
                    // don't include auto-generated PK & FK to be part of mapping
                    if (!IsNotMapped(c))
                    {
                        AddColumnSchema(c);
                    }
                }
            }
        }

        internal bool IsMapped() => _dataSet != null;

        internal DataTable SearchMatchingTableSchema(string localName, string namespaceURI)
        {
            object tid = GetIdentity(localName, namespaceURI);
            return (DataTable)(_tableSchemaMap[tid]!);
        }

        // SearchMatchingTableSchema function works only when the elem has not been bound to a DataRow. If you want to get the table associated w/ an element after
        // it has been associated w/ a DataRow use GetTableSchemaForElement function.
        // rowElem is the parent region rowElem or null if there is no parent region (in case elem is a row elem, then rowElem will be the parent region; if elem is not
        //    mapped to a DataRow, then rowElem is the region elem is part of)
        //
        // Those are the rules for determing if elem is a row element:
        //  1. node is an element (already meet, since elem is of type XmlElement)
        //  2. If the node is already associated w/ a DataRow, then the node is a row element - not applicable, b/c this function is intended to be called on a
        //    to find out if the node s/b associated w/ a DataRow (see XmlDataDocument.LoadRows)
        //  3. If the node localName/ns matches a DataTable then
        //      3.1 Take the parent region DataTable (in our case rowElem.Row.DataTable)
        //          3.2 If no parent region, then the node is associated w/ a DataTable
        //          3.3 If there is a parent region
        //              3.3.1 If the node has no elem children and no attr other than namespace declaration, and the node can match
        //                  a column from the parent region table, then the node is NOT associated w/ a DataTable (it is a potential DataColumn in the parent region)
        //              3.3.2 Else the node is a row-element (and associated w/ a DataTable / DataRow )
        //
        internal DataTable? SearchMatchingTableSchema(XmlBoundElement? rowElem, XmlBoundElement elem)
        {
            Debug.Assert(elem != null);

            DataTable t = SearchMatchingTableSchema(elem.LocalName, elem.NamespaceURI);
            if (t == null)
            {
                return null;
            }

            if (rowElem == null)
            {
                return t;
            }

            // Currently we expect we map things from top of the tree to the bottom
            Debug.Assert(rowElem.Row != null);

            DataColumn? col = GetColumnSchemaForNode(rowElem, elem);
            if (col == null)
            {
                return t;
            }

            foreach (XmlAttribute a in elem.Attributes)
            {
#if DEBUG
                // Some sanity check to catch errors like namespace attributes have the right localName/namespace value, but a wrong atomized namespace value
                if (a.LocalName == "xmlns")
                {
                    Debug.Assert(a.Prefix != null && a.Prefix.Length == 0);
                    Debug.Assert(a.NamespaceURI == (object)strReservedXmlns);
                }
                if (a.Prefix == "xmlns")
                {
                    Debug.Assert(a.NamespaceURI == (object)strReservedXmlns);
                }
                if (a.NamespaceURI == strReservedXmlns)
                {
                    Debug.Assert(a.NamespaceURI == (object)strReservedXmlns);
                }
#endif
                // No namespace attribute found, so elem cannot be a potential DataColumn, therefore is a row-elem
                if (a.NamespaceURI != (object)strReservedXmlns)
                {
                    return t;
                }
            }

            for (XmlNode? n = elem.FirstChild; n != null; n = n.NextSibling)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    // elem has an element child, so elem cannot be a potential DataColumn, therefore is a row-elem
                    return t;
                }
            }

            // Node is a potential DataColumn in rowElem region
            return null;
        }

        internal DataColumn? GetColumnSchemaForNode(XmlBoundElement rowElem, XmlNode node)
        {
            Debug.Assert(rowElem != null);
            // The caller must make sure that node is not a row-element
            Debug.Assert((node is XmlBoundElement) ? ((XmlBoundElement)node).Row == null : true);

            object tid = GetIdentity(rowElem.LocalName, rowElem.NamespaceURI);
            object cid = GetIdentity(node.LocalName, node.NamespaceURI);

            Hashtable? columns = (Hashtable?)_columnSchemaMap[tid];
            if (columns != null)
            {
                DataColumn? col = (DataColumn?)(columns[cid]);
                if (col == null)
                {
                    return null;
                }

                MappingType mt = col.ColumnMapping;

                if (node.NodeType == XmlNodeType.Attribute && mt == MappingType.Attribute)
                {
                    return col;
                }

                if (node.NodeType == XmlNodeType.Element && mt == MappingType.Element)
                {
                    return col;
                }

                // node's (localName, ns) matches a column, but the MappingType is different (i.e. node is elem, MT is attr)
                return null;
            }
            return null;
        }
        internal static DataTable? GetTableSchemaForElement(XmlElement elem)
        {
            XmlBoundElement? be = elem as XmlBoundElement;
            if (be == null)
            {
                return null;
            }

            return GetTableSchemaForElement(be);
        }

        internal static DataTable? GetTableSchemaForElement(XmlBoundElement be) => be.Row?.Table;

        internal static bool IsNotMapped(DataColumn c) => c.ColumnMapping == MappingType.Hidden;

        // ATTENTION: GetRowFromElement( XmlElement ) and GetRowFromElement( XmlBoundElement ) should have the same functionality and side effects.
        // See this code fragment for why:
        //     XmlBoundElement be = ...;
        //     XmlElement e = be;
        //     GetRowFromElement( be ); // Calls GetRowFromElement( XmlBoundElement )
        //     GetRowFromElement( e );  // Calls GetRowFromElement( XmlElement ), in spite of e being an instance of XmlBoundElement
        internal static DataRow? GetRowFromElement(XmlElement? e) => (e as XmlBoundElement)?.Row;

        internal static DataRow? GetRowFromElement(XmlBoundElement be) => be.Row;

        // Get the row-elem associatd w/ the region node is in.
        // If node is in a region not mapped (like document element node) the function returns false and sets elem to null)
        // This function does not work if the region is not associated w/ a DataRow (it uses DataRow association to know what is the row element associated w/ the region)
        internal static bool GetRegion(XmlNode? node, [NotNullWhen(true)] out XmlBoundElement? rowElem)
        {
            while (node != null)
            {
                XmlBoundElement? be = node as XmlBoundElement;
                // Break if found a region
                if (be != null && GetRowFromElement(be) != null)
                {
                    rowElem = be;
                    return true;
                }

                if (node.NodeType == XmlNodeType.Attribute)
                {
                    node = ((XmlAttribute)node).OwnerElement;
                }
                else
                {
                    node = node.ParentNode;
                }
            }

            rowElem = null;
            return false;
        }

        internal bool IsRegionRadical(XmlBoundElement rowElem)
        {
            // You must pass a row element (which s/b associated w/ a DataRow)
            Debug.Assert(rowElem.Row != null);

            if (rowElem.ElementState == ElementState.Defoliated)
            {
                return true;
            }

            DataTable table = GetTableSchemaForElement(rowElem)!;
            DataColumnCollection columns = table.Columns;
            int iColumn = 0;

            // check column attributes...
            int cAttrs = rowElem.Attributes.Count;
            for (int iAttr = 0; iAttr < cAttrs; iAttr++)
            {
                XmlAttribute attr = rowElem.Attributes[iAttr];

                // only specified attributes are radical
                if (!attr.Specified)
                {
                    return false;
                }

                // only mapped attrs are valid
                DataColumn? schema = GetColumnSchemaForNode(rowElem, attr);
                if (schema == null)
                {
                    return false;
                }

                // check to see if column is in order
                if (!IsNextColumn(columns, ref iColumn, schema))
                {
                    return false;
                }

                // must have exactly one text node (XmlNodeType.Text) child
                XmlNode? fc = attr.FirstChild;
                if (fc == null || fc.NodeType != XmlNodeType.Text || fc.NextSibling != null)
                {
                    return false;
                }
            }

            // check column elements
            iColumn = 0;
            XmlNode? n = rowElem.FirstChild;
            for (; n != null; n = n.NextSibling)
            {
                // only elements can exist in radically structured data
                if (n.NodeType != XmlNodeType.Element)
                {
                    return false;
                }
                XmlElement? e = n as XmlElement;

                // only checking for column mappings in this loop
                if (GetRowFromElement(e) != null)
                {
                    break;
                }

                // element's must have schema to be radically structured
                DataColumn? schema = GetColumnSchemaForNode(rowElem, e!);
                if (schema == null)
                {
                    return false;
                }

                // check to see if column is in order
                if (!IsNextColumn(columns, ref iColumn, schema))
                {
                    return false;
                }

                // must have no attributes
                if (e!.HasAttributes)
                {
                    return false;
                }

                // must have exactly one text node child
                XmlNode? fc = e!.FirstChild;
                if (fc == null || fc.NodeType != XmlNodeType.Text || fc.NextSibling != null)
                {
                    return false;
                }
            }

            // check for remaining sub-regions
            for (; n != null; n = n.NextSibling)
            {
                // only elements can exist in radically structured data
                if (n.NodeType != XmlNodeType.Element)
                {
                    return false;
                }

                // element's must be regions in order to be radially structured
                DataRow? row = GetRowFromElement((XmlElement?)n);
                if (row == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddTableSchema(DataTable table)
        {
            object idTable = GetIdentity(table.EncodedTableName, table.Namespace);
            _tableSchemaMap[idTable] = table;
        }
        private void AddColumnSchema(DataColumn col)
        {
            DataTable table = col.Table!;
            object idTable = GetIdentity(table.EncodedTableName, table.Namespace);
            object idColumn = GetIdentity(col.EncodedColumnName, col.Namespace);

            Hashtable? columns = (Hashtable?)_columnSchemaMap[idTable];
            if (columns == null)
            {
                columns = new Hashtable();
                _columnSchemaMap[idTable] = columns;
            }
            columns[idColumn] = col;
        }
        private static object GetIdentity(string localName, string namespaceURI)
        {
            // we need access to XmlName to make this faster
            return localName + ":" + namespaceURI;
        }

        private static bool IsNextColumn(DataColumnCollection columns, ref int iColumn, DataColumn col)
        {
            for (; iColumn < columns.Count; iColumn++)
            {
                if (columns[iColumn] == col)
                {
                    iColumn++; // advance before we return...
                    return true;
                }
            }

            return false;
        }
    }
}
