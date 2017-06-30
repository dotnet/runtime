// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data
{
    internal sealed class XmlDataLoader
    {
        private DataSet _dataSet;
        private XmlToDatasetMap _nodeToSchemaMap = null;
        private Hashtable _nodeToRowMap;
        private Stack _childRowsStack = null;
        private Hashtable _htableExcludedNS = null;
        private bool _fIsXdr = false;
        internal bool _isDiffgram = false;

        private XmlElement _topMostNode = null;
        private bool _ignoreSchema = false;

        private DataTable _dataTable;
        private bool _isTableLevel = false;
        private bool _fromInference = false;

        internal XmlDataLoader(DataSet dataset, bool IsXdr, bool ignoreSchema)
        {
            // Initialization
            _dataSet = dataset;
            _nodeToRowMap = new Hashtable();
            _fIsXdr = IsXdr;
            _ignoreSchema = ignoreSchema;
        }

        internal XmlDataLoader(DataSet dataset, bool IsXdr, XmlElement topNode, bool ignoreSchema)
        {
            // Initialization
            _dataSet = dataset;
            _nodeToRowMap = new Hashtable();
            _fIsXdr = IsXdr;

            // Allocate the stack and create the mappings            
            _childRowsStack = new Stack(50);

            _topMostNode = topNode;
            _ignoreSchema = ignoreSchema;
        }

        internal XmlDataLoader(DataTable datatable, bool IsXdr, bool ignoreSchema)
        {
            // Initialization
            _dataSet = null;
            _dataTable = datatable;
            _isTableLevel = true;
            _nodeToRowMap = new Hashtable();
            _fIsXdr = IsXdr;
            _ignoreSchema = ignoreSchema;
        }

        internal XmlDataLoader(DataTable datatable, bool IsXdr, XmlElement topNode, bool ignoreSchema)
        {
            // Initialization
            _dataSet = null;
            _dataTable = datatable;
            _isTableLevel = true;
            _nodeToRowMap = new Hashtable();
            _fIsXdr = IsXdr;

            // Allocate the stack and create the mappings

            _childRowsStack = new Stack(50);
            _topMostNode = topNode;
            _ignoreSchema = ignoreSchema;
        }

        internal bool FromInference
        {
            get
            {
                return _fromInference;
            }
            set
            {
                _fromInference = value;
            }
        }

        // after loading, all detached DataRows are attached to their tables
        private void AttachRows(DataRow parentRow, XmlNode parentElement)
        {
            if (parentElement == null)
                return;

            for (XmlNode n = parentElement.FirstChild; n != null; n = n.NextSibling)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    XmlElement e = (XmlElement)n;
                    DataRow r = GetRowFromElement(e);
                    if (r != null && r.RowState == DataRowState.Detached)
                    {
                        if (parentRow != null)
                            r.SetNestedParentRow(parentRow, /*setNonNested*/ false);

                        r.Table.Rows.Add(r);
                    }
                    else if (r == null)
                    {
                        // n is a 'sugar element'
                        AttachRows(parentRow, n);
                    }

                    // attach all detached rows
                    AttachRows(r, n);
                }
            }
        }

        private int CountNonNSAttributes(XmlNode node)
        {
            int count = 0;
            for (int i = 0; i < node.Attributes.Count; i++)
            {
                XmlAttribute attr = node.Attributes[i];
                if (!FExcludedNamespace(node.Attributes[i].NamespaceURI))
                    count++;
            }
            return count;
        }

        private string GetValueForTextOnlyColums(XmlNode n)
        {
            string value = null;

            // don't consider whitespace
            while (n != null && (n.NodeType == XmlNodeType.Whitespace || !IsTextLikeNode(n.NodeType)))
            {
                n = n.NextSibling;
            }

            if (n != null)
            {
                if (IsTextLikeNode(n.NodeType) && (n.NextSibling == null || !IsTextLikeNode(n.NodeType)))
                {
                    // don't use string builder if only one text node exists
                    value = n.Value;
                    n = n.NextSibling;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    while (n != null && IsTextLikeNode(n.NodeType))
                    {
                        sb.Append(n.Value);
                        n = n.NextSibling;
                    }
                    value = sb.ToString();
                }
            }

            if (value == null)
                value = string.Empty;

            return value;
        }

        private string GetInitialTextFromNodes(ref XmlNode n)
        {
            string value = null;

            if (n != null)
            {
                // don't consider whitespace
                while (n.NodeType == XmlNodeType.Whitespace)
                    n = n.NextSibling;

                if (IsTextLikeNode(n.NodeType) && (n.NextSibling == null || !IsTextLikeNode(n.NodeType)))
                {
                    // don't use string builder if only one text node exists
                    value = n.Value;
                    n = n.NextSibling;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    while (n != null && IsTextLikeNode(n.NodeType))
                    {
                        sb.Append(n.Value);
                        n = n.NextSibling;
                    }
                    value = sb.ToString();
                }
            }

            if (value == null)
                value = string.Empty;

            return value;
        }

        private DataColumn GetTextOnlyColumn(DataRow row)
        {
            DataColumnCollection columns = row.Table.Columns;
            int cCols = columns.Count;
            for (int iCol = 0; iCol < cCols; iCol++)
            {
                DataColumn c = columns[iCol];
                if (IsTextOnly(c))
                    return c;
            }
            return null;
        }

        internal DataRow GetRowFromElement(XmlElement e)
        {
            return (DataRow)_nodeToRowMap[e];
        }

        internal bool FColumnElement(XmlElement e)
        {
            if (_nodeToSchemaMap.GetColumnSchema(e, FIgnoreNamespace(e)) == null)
                return false;

            if (CountNonNSAttributes(e) > 0)
                return false;

            for (XmlNode tabNode = e.FirstChild; tabNode != null; tabNode = tabNode.NextSibling)
                if (tabNode is XmlElement)
                    return false;

            return true;
        }

        private bool FExcludedNamespace(string ns)
        {
            if (ns.Equals(Keywords.XSD_XMLNS_NS))
                return true;

            if (_htableExcludedNS == null)
                return false;

            return _htableExcludedNS.Contains(ns);
        }

        private bool FIgnoreNamespace(XmlNode node)
        {
            XmlNode ownerNode;
            if (!_fIsXdr)
                return false;
            if (node is XmlAttribute)
                ownerNode = ((XmlAttribute)node).OwnerElement;
            else
                ownerNode = node;
            if (ownerNode.NamespaceURI.StartsWith("x-schema:#", StringComparison.Ordinal))
                return true;
            else
                return false;
        }

        private bool FIgnoreNamespace(XmlReader node)
        {
            if (_fIsXdr && node.NamespaceURI.StartsWith("x-schema:#", StringComparison.Ordinal))
                return true;
            else
                return false;
        }

        internal bool IsTextLikeNode(XmlNodeType n)
        {
            switch (n)
            {
                case XmlNodeType.EntityReference:
                    throw ExceptionBuilder.FoundEntity();

                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.CDATA:
                    return true;

                default:
                    return false;
            }
        }

        internal bool IsTextOnly(DataColumn c)
        {
            if (c.ColumnMapping != MappingType.SimpleContent)
                return false;
            else
                return true;
        }

        internal void LoadData(XmlDocument xdoc)
        {
            if (xdoc.DocumentElement == null)
                return;

            bool saveEnforce;

            if (_isTableLevel)
            {
                saveEnforce = _dataTable.EnforceConstraints;
                _dataTable.EnforceConstraints = false;
            }
            else
            {
                saveEnforce = _dataSet.EnforceConstraints;
                _dataSet.EnforceConstraints = false;
                _dataSet._fInReadXml = true;
            }

            if (_isTableLevel)
            {
                _nodeToSchemaMap = new XmlToDatasetMap(_dataTable, xdoc.NameTable);
            }
            else
            {
                _nodeToSchemaMap = new XmlToDatasetMap(_dataSet, xdoc.NameTable);
            }
            /*
                        // Top level table or dataset ?
                        XmlElement rootElement = xdoc.DocumentElement;
                        Hashtable tableAtoms = new Hashtable();
                        XmlNode tabNode;
                        if (CountNonNSAttributes (rootElement) > 0)
                            dataSet.fTopLevelTable = true;
                        else {
                            for (tabNode = rootElement.FirstChild; tabNode != null; tabNode = tabNode.NextSibling) {
                                if (tabNode is XmlElement && tabNode.LocalName != Keywords.XSD_SCHEMA) {
                                    object value = tableAtoms[QualifiedName (tabNode.LocalName, tabNode.NamespaceURI)];
                                    if (value == null || (bool)value == false) {
                                        dataSet.fTopLevelTable = true;
                                        break;
                                    }
                                }
                            }
                        }
            */
            DataRow topRow = null;
            if (_isTableLevel || (_dataSet != null && _dataSet._fTopLevelTable))
            {
                XmlElement e = xdoc.DocumentElement;
                DataTable topTable = (DataTable)_nodeToSchemaMap.GetSchemaForNode(e, FIgnoreNamespace(e));
                if (topTable != null)
                {
                    topRow = topTable.CreateEmptyRow(); //enzol perf
                    _nodeToRowMap[e] = topRow;

                    // get all field values.
                    LoadRowData(topRow, e);
                    topTable.Rows.Add(topRow);
                }
            }

            LoadRows(topRow, xdoc.DocumentElement);
            AttachRows(topRow, xdoc.DocumentElement);


            if (_isTableLevel)
            {
                _dataTable.EnforceConstraints = saveEnforce;
            }
            else
            {
                _dataSet._fInReadXml = false;
                _dataSet.EnforceConstraints = saveEnforce;
            }
        }

        private void LoadRowData(DataRow row, XmlElement rowElement)
        {
            XmlNode n;
            DataTable table = row.Table;
            if (FromInference)
                table.Prefix = rowElement.Prefix;

            // keep a list of all columns that get updated
            Hashtable foundColumns = new Hashtable();

            row.BeginEdit();

            // examine all children first
            n = rowElement.FirstChild;

            // Look for data to fill the TextOnly column
            DataColumn column = GetTextOnlyColumn(row);
            if (column != null)
            {
                foundColumns[column] = column;
                string text = GetValueForTextOnlyColums(n);
                if (XMLSchema.GetBooleanAttribute(rowElement, Keywords.XSI_NIL, Keywords.XSINS, false) && string.IsNullOrEmpty(text))
                    row[column] = DBNull.Value;
                else
                    SetRowValueFromXmlText(row, column, text);
            }

            // Walk the region to find elements that map to columns
            while (n != null && n != rowElement)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    XmlElement e = (XmlElement)n;

                    object schema = _nodeToSchemaMap.GetSchemaForNode(e, FIgnoreNamespace(e));
                    if (schema is DataTable)
                    {
                        if (FColumnElement(e))
                            schema = _nodeToSchemaMap.GetColumnSchema(e, FIgnoreNamespace(e));
                    }

                    // if element has its own table mapping, it is a separate region
                    if (schema == null || schema is DataColumn)
                    {
                        // descend to examine child elements
                        n = e.FirstChild;

                        if (schema != null && schema is DataColumn)
                        {
                            DataColumn c = (DataColumn)schema;

                            if (c.Table == row.Table && c.ColumnMapping != MappingType.Attribute && foundColumns[c] == null)
                            {
                                foundColumns[c] = c;
                                string text = GetValueForTextOnlyColums(n);
                                if (XMLSchema.GetBooleanAttribute(e, Keywords.XSI_NIL, Keywords.XSINS, false) && string.IsNullOrEmpty(text))
                                    row[c] = DBNull.Value;
                                else
                                    SetRowValueFromXmlText(row, c, text);
                            }
                        }
                        else if ((schema == null) && (n != null))
                        {
                            continue;
                        }


                        // nothing left down here, continue from element
                        if (n == null)
                            n = e;
                    }
                }

                // if no more siblings, ascend back toward original element (rowElement)
                while (n != rowElement && n.NextSibling == null)
                {
                    n = n.ParentNode;
                }

                if (n != rowElement)
                    n = n.NextSibling;
            }

            //
            // Walk the attributes to find attributes that map to columns.
            //
            foreach (XmlAttribute attr in rowElement.Attributes)
            {
                object schema = _nodeToSchemaMap.GetColumnSchema(attr, FIgnoreNamespace(attr));
                if (schema != null && schema is DataColumn)
                {
                    DataColumn c = (DataColumn)schema;

                    if (c.ColumnMapping == MappingType.Attribute && foundColumns[c] == null)
                    {
                        foundColumns[c] = c;
                        n = attr.FirstChild;
                        SetRowValueFromXmlText(row, c, GetInitialTextFromNodes(ref n));
                    }
                }
            }

            // Null all columns values that aren't represented in the tree
            foreach (DataColumn c in row.Table.Columns)
            {
                if (foundColumns[c] == null && XmlToDatasetMap.IsMappedColumn(c))
                {
                    if (!c.AutoIncrement)
                    {
                        if (c.AllowDBNull)
                        {
                            row[c] = DBNull.Value;
                        }
                        else
                        {
                            row[c] = c.DefaultValue;
                        }
                    }
                    else
                    {
                        c.Init(row._tempRecord);
                    }
                }
            }

            row.EndEdit();
        }


        // load all data from tree structre into datarows
        private void LoadRows(DataRow parentRow, XmlNode parentElement)
        {
            if (parentElement == null)
                return;

            // Skip schema node as well
            if (parentElement.LocalName == Keywords.XSD_SCHEMA && parentElement.NamespaceURI == Keywords.XSDNS ||
                parentElement.LocalName == Keywords.SQL_SYNC && parentElement.NamespaceURI == Keywords.UPDGNS ||
                parentElement.LocalName == Keywords.XDR_SCHEMA && parentElement.NamespaceURI == Keywords.XDRNS)
                return;

            for (XmlNode n = parentElement.FirstChild; n != null; n = n.NextSibling)
            {
                if (n is XmlElement)
                {
                    XmlElement e = (XmlElement)n;
                    object schema = _nodeToSchemaMap.GetSchemaForNode(e, FIgnoreNamespace(e));

                    if (schema != null && schema is DataTable)
                    {
                        DataRow r = GetRowFromElement(e);
                        if (r == null)
                        {
                            // skip columns which has the same name as another table
                            if (parentRow != null && FColumnElement(e))
                                continue;

                            r = ((DataTable)schema).CreateEmptyRow();
                            _nodeToRowMap[e] = r;

                            // get all field values.
                            LoadRowData(r, e);
                        }

                        // recurse down to inner elements
                        LoadRows(r, n);
                    }
                    else
                    {
                        // recurse down to inner elements
                        LoadRows(null, n);
                    }
                }
            }
        }

        private void SetRowValueFromXmlText(DataRow row, DataColumn col, string xmlText)
        {
            row[col] = col.ConvertXmlToObject(xmlText);
        }

        private XmlReader _dataReader = null;
        private object _XSD_XMLNS_NS;
        private object _XDR_SCHEMA;
        private object _XDRNS;
        private object _SQL_SYNC;
        private object _UPDGNS;
        private object _XSD_SCHEMA;
        private object _XSDNS;

        private object _DFFNS;
        private object _MSDNS;
        private object _DIFFID;
        private object _HASCHANGES;
        private object _ROWORDER;

        private void InitNameTable()
        {
            XmlNameTable nameTable = _dataReader.NameTable;

            _XSD_XMLNS_NS = nameTable.Add(Keywords.XSD_XMLNS_NS);
            _XDR_SCHEMA = nameTable.Add(Keywords.XDR_SCHEMA);
            _XDRNS = nameTable.Add(Keywords.XDRNS);
            _SQL_SYNC = nameTable.Add(Keywords.SQL_SYNC);
            _UPDGNS = nameTable.Add(Keywords.UPDGNS);
            _XSD_SCHEMA = nameTable.Add(Keywords.XSD_SCHEMA);
            _XSDNS = nameTable.Add(Keywords.XSDNS);

            _DFFNS = nameTable.Add(Keywords.DFFNS);
            _MSDNS = nameTable.Add(Keywords.MSDNS);
            _DIFFID = nameTable.Add(Keywords.DIFFID);
            _HASCHANGES = nameTable.Add(Keywords.HASCHANGES);
            _ROWORDER = nameTable.Add(Keywords.ROWORDER);
        }

        internal void LoadData(XmlReader reader)
        {
            _dataReader = DataTextReader.CreateReader(reader);

            int entryDepth = _dataReader.Depth;                  // Store current XML element depth so we'll read
                                                                 // correct portion of the XML and no more
            bool fEnforce = _isTableLevel ? _dataTable.EnforceConstraints : _dataSet.EnforceConstraints;
            // Keep constraints status for datataset/table
            InitNameTable();                                    // Adds DataSet namespaces to reader's nametable

            if (_nodeToSchemaMap == null)
            {                      // Create XML to dataset map
                _nodeToSchemaMap = _isTableLevel ? new XmlToDatasetMap(_dataReader.NameTable, _dataTable) :
                                                 new XmlToDatasetMap(_dataReader.NameTable, _dataSet);
            }

            if (_isTableLevel)
            {
                _dataTable.EnforceConstraints = false;           // Disable constraints
            }
            else
            {
                _dataSet.EnforceConstraints = false;             // Disable constraints
                _dataSet._fInReadXml = true;                      // We're in ReadXml now
            }

            if (_topMostNode != null)
            {                          // Do we have top node?
                if (!_isDiffgram && !_isTableLevel)
                {             // Not a diffgram  and not DataSet?
                    DataTable table = _nodeToSchemaMap.GetSchemaForNode(_topMostNode, FIgnoreNamespace(_topMostNode)) as DataTable;
                    // Try to match table in the dataset to this node
                    if (table != null)
                    {                        // Got the table ?
                        LoadTopMostTable(table);                // Load top most node
                    }
                }

                _topMostNode = null;                             // topMostNode is no more. Good riddance.
            }

            while (!_dataReader.EOF)
            {                          // Main XML parsing loop. Check for EOF just in case.
                if (_dataReader.Depth < entryDepth)              // Stop if we have consumed all elements allowed
                    break;

                if (reader.NodeType != XmlNodeType.Element)
                { // Read till Element is found
                    _dataReader.Read();
                    continue;
                }
                DataTable table = _nodeToSchemaMap.GetTableForNode(_dataReader, FIgnoreNamespace(_dataReader));
                // Try to get table for node
                if (table == null)
                {                            // Read till table is found
                    if (!ProcessXsdSchema())                    // Check for schemas...
                        _dataReader.Read();                      // Not found? Read next element.

                    continue;
                }

                LoadTable(table, false /* isNested */);        // Here goes -- load data for this table
                                                               // This is a root table, so it's not nested
            }

            if (_isTableLevel)
            {
                _dataTable.EnforceConstraints = fEnforce;        // Restore constraints and return
            }
            else
            {
                _dataSet._fInReadXml = false;                     // We're done.
                _dataSet.EnforceConstraints = fEnforce;          // Restore constraints and return
            }
        }

        // Loads a top most table. 
        // This is neded because desktop is capable of loading almost anything into the dataset.
        // The top node could be a DataSet element or a Table element. To make things worse,
        // you could have a table with the same name as dataset.
        // Here's how we're going to dig into this mess:
        //
        //                                 TopNode is null ?
        //                                / No           \ Yes
        //                  Table matches TopNode ?       Current node is the table start
        //                 / No                  \ Yes (LoadTopMostTable called in this case only)
        //   Current node is the table start    DataSet name matches one of the tables ?
        //      TopNode is dataset node        / Yes                                  \ No
        //                                    /                                        TopNode is the table
        //      Current node matches column or nested table in the table ?             and current node
        //     / No                                                 \ Yes              is a column or a
        //     TopNode is DataSet                            TopNode is table          nested table
        // 
        // Yes, it is terrible and I don't like it also..

        private void LoadTopMostTable(DataTable table)
        {
            //        /------------------------------- This one is in topMostNode (backed up to XML DOM)
            //  <Table> /----------------------------- We are here on entrance
            //      <Column>Value</Column>
            //      <AnotherColumn>Value</AnotherColumn>
            //  </Table> ...
            //            \------------------------------ We are here on exit           


            Debug.Assert(table != null, "Table to be loaded is null on LoadTopMostTable() entry");
            Debug.Assert(_topMostNode != null, "topMostNode is null on LoadTopMostTable() entry");
            Debug.Assert(!_isDiffgram, "Diffgram mode is on while we have topMostNode table. This is bad.");

            bool topNodeIsTable = _isTableLevel || (_dataSet.DataSetName != table.TableName);
            // If table name we have matches dataset
            // name top node could be a DataSet OR a table.
            // It's a table overwise.
            DataRow row = null;                                 // Data row we're going to add to this table

            bool matchFound = false;                            // Assume we found no matching elements

            int entryDepth = _dataReader.Depth - 1;              // Store current reader depth so we know when to stop reading
                                                                 // Adjust depth by one as we've read top most element 
                                                                 // outside this method. 
            string textNodeValue;                               // Value of a text node we might have

            Debug.Assert(entryDepth >= 0, "Wrong entry Depth for top most element.");

            int entryChild = _childRowsStack.Count;              // Memorize child stack level on entry

            DataColumn c;                                       // Hold column here
            DataColumnCollection collection = table.Columns;    // Hold column collectio here

            object[] foundColumns = new object[collection.Count];
            // This is the columns data we might find
            XmlNode n;                                          // Need this to pass by reference

            foreach (XmlAttribute attr in _topMostNode.Attributes)
            {
                // Check all attributes in this node

                c = _nodeToSchemaMap.GetColumnSchema(attr, FIgnoreNamespace(attr)) as DataColumn;
                // Try to match attribute to column
                if ((c != null) && (c.ColumnMapping == MappingType.Attribute))
                {
                    // If it's a column with attribute mapping
                    n = attr.FirstChild;

                    foundColumns[c.Ordinal] = c.ConvertXmlToObject(GetInitialTextFromNodes(ref n));
                    // Get value
                    matchFound = true;                          // and note we found a matching element
                }
            }

            // Now handle elements. This could be columns or nested tables
            // We'll skip the rest as we have no idea what to do with it.

            // Note: we do not need to read first as we're already as it has been done by caller.

            while (entryDepth < _dataReader.Depth)
            {
                switch (_dataReader.NodeType)
                {                  // Process nodes based on type
                    case XmlNodeType.Element:                       // It's an element
                        object o = _nodeToSchemaMap.GetColumnSchema(table, _dataReader, FIgnoreNamespace(_dataReader));
                        // Get dataset element for this XML element
                        c = o as DataColumn;                        // Perhaps, it's a column?

                        if (c != null)
                        {                          // Do we have matched column in this table?
                                                   // Let's load column data

                            if (foundColumns[c.Ordinal] == null)
                            {
                                // If this column was not found before
                                LoadColumn(c, foundColumns);       // Get column value.
                                matchFound = true;                  // Got matched row.
                            }
                            else
                            {
                                _dataReader.Read();                  // Advance to next element. 
                            }
                        }
                        else
                        {
                            DataTable nestedTable = o as DataTable;
                            // Perhaps, it's a nested table ?
                            if (nestedTable != null)
                            {            // Do we have matched table in DataSet ?
                                LoadTable(nestedTable, true /* isNested */);
                                // Yes. Load nested table (recursive)
                                matchFound = true;                  // Got matched nested table 
                            }
                            else if (ProcessXsdSchema())
                            {          // Check for schema. Skip or load if found.
                                continue;                           // Schema has been found. Process the next element 
                                                                    // we're already at (done by schema processing).
                            }
                            else
                            {                                  // Not a table or column in this table ?
                                if (!(matchFound || topNodeIsTable))
                                {
                                    // Could top node be a DataSet?


                                    return;                         // Assume top node is DataSet 
                                                                    // and stop top node processing
                                }
                                _dataReader.Read();                  // Continue to the next element.
                            }
                        }
                        break;
                    // Oops. Not supported
                    case XmlNodeType.EntityReference:               // Oops. No support for Entity Reference
                        throw ExceptionBuilder.FoundEntity();
                    case XmlNodeType.Text:                          // It looks like a text.
                    case XmlNodeType.Whitespace:                    // This actually could be
                    case XmlNodeType.CDATA:                         // if we have XmlText in our table
                    case XmlNodeType.SignificantWhitespace:
                        textNodeValue = _dataReader.ReadString();
                        // Get text node value.
                        c = table._xmlText;                          // Get XML Text column from our table

                        if (c != null && foundColumns[c.Ordinal] == null)
                        {
                            // If XmlText Column is set
                            // and we do not have data already                
                            foundColumns[c.Ordinal] = c.ConvertXmlToObject(textNodeValue);
                            // Read and store the data
                        }

                        break;
                    default:
                        _dataReader.Read();                  // We don't process that, skip to the next element.
                        break;
                }
            }

            _dataReader.Read();                          // Proceed to the next element.

            // It's the time to populate row with loaded data and add it to the table we'we just read to the table

            for (int i = foundColumns.Length - 1; i >= 0; --i)
            {
                // Check all columns
                if (null == foundColumns[i])
                {              // Got data for this column ?
                    c = collection[i];                      // No. Get column for this index

                    if (c.AllowDBNull && c.ColumnMapping != MappingType.Hidden && !c.AutoIncrement)
                    {
                        foundColumns[i] = DBNull.Value;     // Assign DBNull if possible
                                                            // table.Rows.Add() below will deal
                                                            // with default values and autoincrement
                    }
                }
            }

            row = table.Rows.AddWithColumnEvents(foundColumns);             // Create, populate and add row

            while (entryChild < _childRowsStack.Count)
            {     // Process child rows we might have
                DataRow childRow = (DataRow)_childRowsStack.Pop();
                // Get row from the stack
                bool unchanged = (childRow.RowState == DataRowState.Unchanged);
                // Is data the same as before?
                childRow.SetNestedParentRow(row, /*setNonNested*/ false);
                // Set parent row
                if (unchanged)                              // Restore record if child row's unchanged
                    childRow._oldRecord = childRow._newRecord;
            }
        }

        // Loads a table. 
        // Yes, I know it's a big method. This is done to avoid performance penalty of calling methods 
        // with many arguments and to keep recursion within one method only. To make code readable,
        // this method divided into 3 parts: attribute processing (including diffgram), 
        // nested elements processing and loading data. Please keep it this way.

        private void LoadTable(DataTable table, bool isNested)
        {
            //  <DataSet> /--------------------------- We are here on entrance
            //      <Table>               
            //          <Column>Value</Column>
            //          <AnotherColumn>Value</AnotherColumn>
            //      </Table>    /-------------------------- We are here on exit           
            //      <AnotherTable>
            //      ...
            //      </AnotherTable>
            //      ...
            //  </DataSet> 

            Debug.Assert(table != null, "Table to be loaded is null on LoadTable() entry");

            DataRow row = null;                                 // Data row we're going to add to this table

            int entryDepth = _dataReader.Depth;                  // Store current reader depth so we know when to stop reading
            int entryChild = _childRowsStack.Count;              // Memorize child stack level on entry

            DataColumn c;                                       // Hold column here
            DataColumnCollection collection = table.Columns;    // Hold column collectio here

            object[] foundColumns = new object[collection.Count];
            // This is the columns data we found 
            // This is used to process diffgramms

            int rowOrder = -1;                                  // Row to insert data to
            string diffId = string.Empty;                       // Diffgram ID string
            string hasChanges = null;                           // Changes string
            bool hasErrors = false;                             // Set this in case of problem

            string textNodeValue;                               // Value of a text node we might have

            // Process attributes first                         

            for (int i = _dataReader.AttributeCount - 1; i >= 0; --i)
            {
                // Check all attributes one by one
                _dataReader.MoveToAttribute(i);                  // Get this attribute

                c = _nodeToSchemaMap.GetColumnSchema(table, _dataReader, FIgnoreNamespace(_dataReader)) as DataColumn;
                // Try to get column for this attribute

                if ((c != null) && (c.ColumnMapping == MappingType.Attribute))
                {
                    // Yep, it is a column mapped as attribute
                    // Get value from XML and store it in the object array
                    foundColumns[c.Ordinal] = c.ConvertXmlToObject(_dataReader.Value);
                }                                               // Oops. No column for this element

                if (_isDiffgram)
                {                             // Now handle some diffgram attributes 
                    if (_dataReader.NamespaceURI == Keywords.DFFNS)
                    {
                        switch (_dataReader.LocalName)
                        {
                            case Keywords.DIFFID:                   // Is it a diffgeam ID ?
                                diffId = _dataReader.Value;          // Store ID
                                break;
                            case Keywords.HASCHANGES:               // Has changes attribute ?
                                hasChanges = _dataReader.Value;      // Store value
                                break;
                            case Keywords.HASERRORS:                // Has errors attribute ?
                                hasErrors = (bool)Convert.ChangeType(_dataReader.Value, typeof(bool), CultureInfo.InvariantCulture);
                                // Store value
                                break;
                        }
                    }
                    else if (_dataReader.NamespaceURI == Keywords.MSDNS)
                    {
                        if (_dataReader.LocalName == Keywords.ROWORDER)
                        {
                            // Is it a row order attribute ?
                            rowOrder = (int)Convert.ChangeType(_dataReader.Value, typeof(int), CultureInfo.InvariantCulture);
                            // Store it
                        }
                        else if (_dataReader.LocalName.StartsWith("hidden", StringComparison.Ordinal))
                        {
                            // Hidden column ?
                            c = collection[XmlConvert.DecodeName(_dataReader.LocalName.Substring(6))];
                            // Let's see if we have one. 
                            // We have to decode name before we look it up
                            // We could not use XmlToDataSet map as it contains
                            // no hidden columns
                            if ((c != null) && (c.ColumnMapping == MappingType.Hidden))
                            {
                                // Got column and it is hidden ?
                                foundColumns[c.Ordinal] = c.ConvertXmlToObject(_dataReader.Value);
                            }
                        }
                    }
                }
            }                                                   // Done with attributes

            // Now handle elements. This could be columns or nested tables.

            //  <DataSet> /------------------- We are here after dealing with attributes
            //      <Table foo="FooValue" bar="BarValue">  
            //          <Column>Value</Column>
            //          <AnotherColumn>Value</AnotherColumn>
            //      </Table>
            //  </DataSet>

            if (_dataReader.Read() && entryDepth < _dataReader.Depth)
            {
                // Read to the next element and see if we're inside
                while (entryDepth < _dataReader.Depth)
                {       // Get out as soon as we've processed all nested nodes.
                    switch (_dataReader.NodeType)
                    {              // Process nodes based on type
                        case XmlNodeType.Element:                   // It's an element
                            object o = _nodeToSchemaMap.GetColumnSchema(table, _dataReader, FIgnoreNamespace(_dataReader));
                            // Get dataset element for this XML element
                            c = o as DataColumn;                    // Perhaps, it's a column?

                            if (c != null)
                            {                      // Do we have matched column in this table?
                                                   // Let's load column data
                                if (foundColumns[c.Ordinal] == null)
                                {
                                    // If this column was not found before
                                    LoadColumn(c, foundColumns);
                                    // Get column value
                                }
                                else
                                {
                                    _dataReader.Read();              // Advance to next element. 
                                }
                            }
                            else
                            {
                                DataTable nestedTable = o as DataTable;
                                // Perhaps, it's a nested table ?
                                if (nestedTable != null)
                                {        // Do we have matched nested table in DataSet ?
                                    LoadTable(nestedTable, true /* isNested */);
                                    // Yes. Load nested table (recursive)
                                }                                   // Not a table nor column? Check if it's schema.
                                else if (ProcessXsdSchema())
                                {      // Check for schema. Skip or load if found.
                                    continue;                       // Schema has been found. Process the next element 
                                                                    // we're already at (done by schema processing).
                                }
                                else
                                {
                                    // We've got element which is not supposed to he here according to the schema.
                                    // That might be a table which was misplaced. We should've thrown on that, 
                                    // but we'll try to load it so we could keep compatibility.
                                    // We won't try to match to columns as we have no idea 
                                    // which table this potential column might belong to.
                                    DataTable misplacedTable = _nodeToSchemaMap.GetTableForNode(_dataReader, FIgnoreNamespace(_dataReader));
                                    // Try to get table for node

                                    if (misplacedTable != null)
                                    {   // Got some matching table?
                                        LoadTable(misplacedTable, false /* isNested */);
                                        // While table's XML element is nested,
                                        // the table itself is not. Load it this way.
                                    }
                                    else
                                    {
                                        _dataReader.Read();          // Not a table? Try next element.
                                    }
                                }
                            }
                            break;
                        case XmlNodeType.EntityReference:           // Oops. No support for Entity Reference
                            throw ExceptionBuilder.FoundEntity();
                        case XmlNodeType.Text:                      // It looks like a text.
                        case XmlNodeType.Whitespace:                // This actually could be
                        case XmlNodeType.CDATA:                     // if we have XmlText in our table
                        case XmlNodeType.SignificantWhitespace:
                            textNodeValue = _dataReader.ReadString();
                            // Get text node value.
                            c = table._xmlText;                      // Get XML Text column from our table

                            if (c != null && foundColumns[c.Ordinal] == null)
                            {
                                // If XmlText Column is set
                                // and we do not have data already                
                                foundColumns[c.Ordinal] = c.ConvertXmlToObject(textNodeValue);
                                // Read and store the data
                            }
                            break;
                        default:
                            _dataReader.Read();                  // We don't process that, skip to the next element.
                            break;
                    }
                }

                _dataReader.Read();                              // We're done here, proceed to the next element.
            }

            // It's the time to populate row with loaded data and add it to the table we'we just read to the table

            if (_isDiffgram)
            {                               // In case of diffgram
                row = table.NewRow(table.NewUninitializedRecord());
                // just create an empty row
                row.BeginEdit();                            // and allow it's population with data 

                for (int i = foundColumns.Length - 1; i >= 0; --i)
                {
                    // Check all columns
                    c = collection[i];                      // Get column for this index

                    c[row._tempRecord] = null != foundColumns[i] ? foundColumns[i] : DBNull.Value;
                    // Set column to loaded value of to
                    // DBNull if value is missing.
                }

                row.EndEdit();                              // Done with this row

                table.Rows.DiffInsertAt(row, rowOrder);     // insert data to specific location

                // And do some diff processing
                if (hasChanges == null)
                {                   // No changes ?
                    row._oldRecord = row._newRecord;          // Restore old record
                }

                if ((hasChanges == Keywords.MODIFIED) || hasErrors)
                {
                    table.RowDiffId[diffId] = row;
                }
            }
            else
            {
                for (int i = foundColumns.Length - 1; i >= 0; --i)
                {
                    // Check all columns
                    if (null == foundColumns[i])
                    {          // Got data for this column ?
                        c = collection[i];                  // No. Get column for this index

                        if (c.AllowDBNull && c.ColumnMapping != MappingType.Hidden && !c.AutoIncrement)
                        {
                            foundColumns[i] = DBNull.Value; // Assign DBNull if possible
                                                            // table.Rows.Add() below will deal
                                                            // with default values and autoincrement
                        }
                    }
                }

                row = table.Rows.AddWithColumnEvents(foundColumns);         // Create, populate and add row
            }

            // Data is loaded into the row and row is added to the table at this point

            while (entryChild < _childRowsStack.Count)
            {     // Process child rows we might have
                DataRow childRow = (DataRow)_childRowsStack.Pop();
                // Get row from the stack
                bool unchanged = (childRow.RowState == DataRowState.Unchanged);
                // Is data the same as before?
                childRow.SetNestedParentRow(row, /*setNonNested*/ false);
                // Set parent row

                if (unchanged)                              // Restore record if child row's unchanged
                    childRow._oldRecord = childRow._newRecord;
            }

            if (isNested)                                   // Got parent ?
                _childRowsStack.Push(row);                   // Push row to the stack
        }

        // Returns column value
        private void LoadColumn(DataColumn column, object[] foundColumns)
        {
            //  <DataSet>    /--------------------------------- We are here on entrance
            //      <Table> /
            //          <Column>Value</Column>
            //          <AnotherColumn>Value</AnotherColumn>
            //      </Table>    \------------------------------ We are here on exit
            //  </DataSet>

            //  <Column>                                        If we have something like this
            //      <Foo>FooVal</Foo>                           We would grab first text-like node
            //      Value                                       In this case it would be "FooVal"
            //      <Bar>BarVal</Bar>                           And not "Value" as you might think
            //  </Column>                                       This is how desktop works

            string text = string.Empty;                         // Column text. Assume empty string
            string xsiNilString = null;                         // Possible NIL attribute string

            int entryDepth = _dataReader.Depth;                  // Store depth so we won't read too much

            if (_dataReader.AttributeCount > 0)                  // If have attributes
                xsiNilString = _dataReader.GetAttribute(Keywords.XSI_NIL, Keywords.XSINS);
            // Try to get NIL attribute
            // We have to do it before we move to the next element
            if (column.IsCustomType)
            {                          // Custom type column
                object columnValue = null;                    // Column value we're after. Assume no value.

                string xsiTypeString = null;                    // XSI type name from TYPE attribute
                string typeName = null;                    // Type name from MSD_INSTANCETYPE attribute

                XmlRootAttribute xmlAttrib = null;              // Might need this attribute for XmlSerializer

                if (_dataReader.AttributeCount > 0)
                {            // If have attributes, get attributes we'll need
                    xsiTypeString = _dataReader.GetAttribute(Keywords.TYPE, Keywords.XSINS);
                    typeName = _dataReader.GetAttribute(Keywords.MSD_INSTANCETYPE, Keywords.MSDNS);
                }

                // Check if need to use XmlSerializer. We need to do that if type does not implement IXmlSerializable.
                // We also need to do that if no polymorphism for this type allowed.

                bool useXmlSerializer = !column.ImplementsIXMLSerializable &&
                    !((column.DataType == typeof(object)) || (typeName != null) || (xsiTypeString != null));

                // Check if we have an attribute telling us value is null.

                if ((xsiNilString != null) && XmlConvert.ToBoolean(xsiNilString))
                {
                    if (!useXmlSerializer)
                    {                    // See if need to set typed null.
                        if (typeName != null && typeName.Length > 0)
                        {
                            // Got type name
                            columnValue = SqlUdtStorage.GetStaticNullForUdtType(DataStorage.GetType(typeName));
                        }
                    }

                    if (null == columnValue)
                    {                  // If no value,
                        columnValue = DBNull.Value;             // change to DBNull;
                    }

                    if (!_dataReader.IsEmptyElement)           // In case element is not empty
                        while (_dataReader.Read() && (entryDepth < _dataReader.Depth)) ;
                    // Read current elements
                    _dataReader.Read();                          // And start reading next element.
                }
                else
                {                                          // No NIL attribute. Get value
                    bool skipped = false;

                    if (column.Table.DataSet != null && column.Table.DataSet._udtIsWrapped)
                    {
                        _dataReader.Read(); // if UDT is wrapped, skip the wrapper
                        skipped = true;
                    }

                    if (useXmlSerializer)
                    {                     // Create an attribute for XmlSerializer
                        if (skipped)
                        {
                            xmlAttrib = new XmlRootAttribute(_dataReader.LocalName);
                            xmlAttrib.Namespace = _dataReader.NamespaceURI;
                        }
                        else
                        {
                            xmlAttrib = new XmlRootAttribute(column.EncodedColumnName);
                            xmlAttrib.Namespace = column.Namespace;
                        }
                    }

                    columnValue = column.ConvertXmlToObject(_dataReader, xmlAttrib);
                    // Go get the value
                    if (skipped)
                    {
                        _dataReader.Read(); // if Wrapper is skipped, skip its end tag
                    }
                }

                foundColumns[column.Ordinal] = columnValue;     // Store value
            }
            else
            {                                                  // Not a custom type. 
                if (_dataReader.Read() && entryDepth < _dataReader.Depth)
                {
                    // Read to the next element and see if we're inside.
                    while (entryDepth < _dataReader.Depth)
                    {
                        switch (_dataReader.NodeType)
                        {              // Process nodes based on type
                            case XmlNodeType.Text:                      // It looks like a text. And we need it.
                            case XmlNodeType.Whitespace:
                            case XmlNodeType.CDATA:
                            case XmlNodeType.SignificantWhitespace:
                                if (0 == text.Length)
                                {                 // In case we do not have value already
                                    text = _dataReader.Value;            // Get value.

                                    // See if we have other text nodes near. In most cases this loop will not be executed. 
                                    StringBuilder builder = null;
                                    while (_dataReader.Read() && entryDepth < _dataReader.Depth && IsTextLikeNode(_dataReader.NodeType))
                                    {
                                        if (builder == null)
                                        {
                                            builder = new StringBuilder(text);
                                        }
                                        builder.Append(_dataReader.Value);  // Concatenate other sequential text like
                                                                            // nodes we might have. This is rare.
                                                                            // We're using this instead of dataReader.ReadString()
                                                                            // which would do the same thing but slower.
                                    }

                                    if (builder != null)
                                    {
                                        text = builder.ToString();
                                    }
                                }
                                else
                                {
                                    _dataReader.ReadString();            // We've got column value already. Read this one and ignore it.
                                }
                                break;
                            case XmlNodeType.Element:
                                if (ProcessXsdSchema())
                                {               // Check for schema. Skip or load if found.
                                    continue;                           // Schema has been found. Process the next element 
                                                                        // we're already at (done by schema processing).
                                }
                                else
                                {
                                    // We've got element which is not supposed to he here.
                                    // That might be table which was misplaced.
                                    // Or it might be a column inside column (also misplaced).
                                    object o = _nodeToSchemaMap.GetColumnSchema(column.Table, _dataReader, FIgnoreNamespace(_dataReader));
                                    // Get dataset element for this XML element
                                    DataColumn c = o as DataColumn;     // Perhaps, it's a column?

                                    if (c != null)
                                    {                  // Do we have matched column in this table?
                                                       // Let's load column data

                                        if (foundColumns[c.Ordinal] == null)
                                        {
                                            // If this column was not found before
                                            LoadColumn(c, foundColumns);
                                            // Get column value
                                        }
                                        else
                                        {
                                            _dataReader.Read();          // Already loaded, proceed to the next element
                                        }
                                    }
                                    else
                                    {
                                        DataTable nestedTable = o as DataTable;
                                        // Perhaps, it's a nested table ?
                                        if (nestedTable != null)
                                        {
                                            // Do we have matched table in DataSet ?
                                            LoadTable(nestedTable, true /* isNested */);
                                            // Yes. Load nested table (recursive)
                                        }
                                        else
                                        {                          // Not a nested column nor nested table.    
                                                                   // Let's try other tables in the DataSet

                                            DataTable misplacedTable = _nodeToSchemaMap.GetTableForNode(_dataReader, FIgnoreNamespace(_dataReader));
                                            // Try to get table for node
                                            if (misplacedTable != null)
                                            {
                                                // Got some table to match?
                                                LoadTable(misplacedTable, false /* isNested */);
                                                // While table's XML element is nested,
                                                // the table itself is not. Load it this way.
                                            }
                                            else
                                            {
                                                _dataReader.Read();      // No match? Try next element
                                            }
                                        }
                                    }
                                }
                                break;
                            case XmlNodeType.EntityReference:           // Oops. No support for Entity Reference
                                throw ExceptionBuilder.FoundEntity();
                            default:
                                _dataReader.Read();                      // We don't process that, skip to the next element.
                                break;
                        }
                    }

                    _dataReader.Read();                              // We're done here. To the next element.
                }

                if (0 == text.Length && xsiNilString != null && XmlConvert.ToBoolean(xsiNilString))
                {
                    foundColumns[column.Ordinal] = DBNull.Value;
                    // If no data and NIL attribute is true set value to null
                }
                else
                {
                    foundColumns[column.Ordinal] = column.ConvertXmlToObject(text);
                }
            }
        }

        // Check for schema and skips or loads XSD schema if found. Returns true if schema found.
        // DataReader would be set on the first XML element after the schema of schema was found.
        // If no schema detected, reader's position will not change.
        private bool ProcessXsdSchema()
        {
            if (((object)_dataReader.LocalName == _XSD_SCHEMA && (object)_dataReader.NamespaceURI == _XSDNS))
            {
                // Found XSD schema
                if (_ignoreSchema)
                {                               // Should ignore it?
                    _dataReader.Skip();                              // Yes, skip it
                }
                else
                {                                              // Have to load schema.
                    if (_isTableLevel)
                    {                           // Loading into the DataTable ?
                        _dataTable.ReadXSDSchema(_dataReader, false); // Invoke ReadXSDSchema on a table
                        _nodeToSchemaMap = new XmlToDatasetMap(_dataReader.NameTable, _dataTable);
                    }                                               // Rebuild XML to DataSet map with new schema.
                    else
                    {                                          // Loading into the DataSet ?
                        _dataSet.ReadXSDSchema(_dataReader, false);   // Invoke ReadXSDSchema on a DataSet
                        _nodeToSchemaMap = new XmlToDatasetMap(_dataReader.NameTable, _dataSet);
                    }                                               // Rebuild XML to DataSet map with new schema.
                }
            }
            else if (((object)_dataReader.LocalName == _XDR_SCHEMA && (object)_dataReader.NamespaceURI == _XDRNS) ||
                    ((object)_dataReader.LocalName == _SQL_SYNC && (object)_dataReader.NamespaceURI == _UPDGNS))
            {
                _dataReader.Skip();                                  // Skip XDR or SQL sync 
            }
            else
            {
                return false;                                       // No schema found. That means reader's position 
                                                                    // is unchganged. Report that to the caller.
            }

            return true;                                            // Schema found, reader's position changed.
        }
    }
}
