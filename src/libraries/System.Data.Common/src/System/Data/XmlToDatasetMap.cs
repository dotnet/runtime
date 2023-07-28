// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    // This is an internal helper class used during Xml load to DataSet/DataDocument.
    // XmlToDatasetMap class provides functionality for binding elemants/attributes
    // to DataTable / DataColumn
    internal sealed class XmlToDatasetMap
    {
        private sealed class XmlNodeIdentety
        {
            public string LocalName;
            public string? NamespaceURI;
            public XmlNodeIdentety(string localName, string? namespaceURI)
            {
                LocalName = localName;
                NamespaceURI = namespaceURI;
            }
            public override int GetHashCode()
            {
                return LocalName.GetHashCode();
            }
            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                XmlNodeIdentety id = (XmlNodeIdentety)obj!;
                return (
                  (string.Equals(LocalName, id.LocalName, StringComparison.OrdinalIgnoreCase)) &&
                  (string.Equals(NamespaceURI, id.NamespaceURI, StringComparison.OrdinalIgnoreCase))
                );
            }
        }

        // This class exist to avoid alocatin of XmlNodeIdentety to every access to the hash table.
        // Unfortunately XmlNode doesn't export single identety object.
        internal sealed class XmlNodeIdHashtable : Hashtable
        {
            private readonly XmlNodeIdentety _id = new XmlNodeIdentety(string.Empty, string.Empty);
            public XmlNodeIdHashtable(int capacity)
                : base(capacity)
            { }
            public object? this[XmlNode node]
            {
                get
                {
                    _id.LocalName = node.LocalName;
                    _id.NamespaceURI = node.NamespaceURI;
                    return this[_id];
                }
            }

            public object? this[XmlReader dataReader]
            {
                get
                {
                    _id.LocalName = dataReader.LocalName;
                    _id.NamespaceURI = dataReader.NamespaceURI;
                    return this[_id];
                }
            }

            public object? this[DataTable table]
            {
                get
                {
                    _id.LocalName = table.EncodedTableName;
                    _id.NamespaceURI = table.Namespace;
                    return this[_id];
                }
            }

            public object? this[string name]
            {
                get
                {
                    _id.LocalName = name;
                    _id.NamespaceURI = string.Empty;
                    return this[_id];
                }
            }
        }

        private sealed class TableSchemaInfo
        {
            public DataTable TableSchema;
            public XmlNodeIdHashtable ColumnsSchemaMap;
            public TableSchemaInfo(DataTable tableSchema)
            {
                TableSchema = tableSchema;
                ColumnsSchemaMap = new XmlNodeIdHashtable(tableSchema.Columns.Count);
            }
        }

        private XmlNodeIdHashtable _tableSchemaMap;              // Holds all the tables information

        private TableSchemaInfo? _lastTableSchemaInfo;

        // Used to infer schema

        public XmlToDatasetMap(DataSet dataSet, XmlNameTable nameTable)
        {
            Debug.Assert(dataSet != null, "DataSet can't be null");
            Debug.Assert(nameTable != null, "NameTable can't be null");
            BuildIdentityMap(dataSet, nameTable);
        }

        // Used to read data with known schema

        public XmlToDatasetMap(XmlNameTable nameTable, DataSet dataSet)
        {
            Debug.Assert(dataSet != null, "DataSet can't be null");
            Debug.Assert(nameTable != null, "NameTable can't be null");
            BuildIdentityMap(nameTable, dataSet);
        }

        // Used to infer schema

        public XmlToDatasetMap(DataTable dataTable, XmlNameTable nameTable)
        {
            Debug.Assert(dataTable != null, "DataTable can't be null");
            Debug.Assert(nameTable != null, "NameTable can't be null");
            BuildIdentityMap(dataTable, nameTable);
        }

        // Used to read data with known schema

        public XmlToDatasetMap(XmlNameTable nameTable, DataTable dataTable)
        {
            Debug.Assert(dataTable != null, "DataTable can't be null");
            Debug.Assert(nameTable != null, "NameTable can't be null");
            BuildIdentityMap(nameTable, dataTable);
        }
        internal static bool IsMappedColumn(DataColumn c)
        {
            return (c.ColumnMapping != MappingType.Hidden);
        }

        // Used to infere schema

        private TableSchemaInfo? AddTableSchema(DataTable table, XmlNameTable nameTable)
        {
            // SDUB: Because in our case reader already read the document all names that we can meet in the
            //       document already has an entry in NameTable.
            //       If in future we will build identity map before reading XML we can replace Get() to Add()
            // Sdub: GetIdentity is called from two places: BuildIdentityMap() and LoadRows()
            //       First case deals with decoded names; Second one with encoded names.
            //       We decided encoded names in first case (instead of decoding them in second)
            //       because it save us time in LoadRows(). We have, as usual, more data them schemas
            string? tableLocalName = nameTable.Get(table.EncodedTableName);
            string? tableNamespace = nameTable.Get(table.Namespace);
            if (tableLocalName == null)
            {
                // because name of this table isn't present in XML we don't need mapping for it.
                // Less mapping faster we work.
                return null;
            }
            TableSchemaInfo tableSchemaInfo = new TableSchemaInfo(table);
            _tableSchemaMap[new XmlNodeIdentety(tableLocalName, tableNamespace)] = tableSchemaInfo;
            return tableSchemaInfo;
        }

        private TableSchemaInfo AddTableSchema(XmlNameTable nameTable, DataTable table)
        {
            // Enzol:This is the opposite of the previous function:
            //       we populate the nametable so that the hash comparison can happen as
            //       object comparison instead of strings.
            // Sdub: GetIdentity is called from two places: BuildIdentityMap() and LoadRows()
            //       First case deals with decoded names; Second one with encoded names.
            //       We decided encoded names in first case (instead of decoding them in second)
            //       because it save us time in LoadRows(). We have, as usual, more data them schemas

            string _tableLocalName = table.EncodedTableName;            // Table name

            string tableLocalName =
                nameTable.Get(_tableLocalName) ??     // Look it up in nametable
                nameTable.Add(_tableLocalName);       // If not found, add it

            table._encodedTableName = tableLocalName;                    // And set it back

            string? tableNamespace = nameTable.Get(table.Namespace);     // Look ip table namespace

            if (tableNamespace == null)
            {                               // If not found
                tableNamespace = nameTable.Add(table.Namespace);        // Add it
            }
            else
            {
                if (table._tableNamespace != null)                       // Update table namespace
                    table._tableNamespace = tableNamespace;
            }


            TableSchemaInfo tableSchemaInfo = new TableSchemaInfo(table);
            // Create new table schema info
            _tableSchemaMap[new XmlNodeIdentety(tableLocalName, tableNamespace)] = tableSchemaInfo;
            // And add it to the hashtable
            return tableSchemaInfo;                                     // Return it as we have to populate
                                                                        // Column schema map and Child table
                                                                        // schema map in it
        }

        private static bool AddColumnSchema(DataColumn col, XmlNameTable nameTable, XmlNodeIdHashtable columns)
        {
            string? columnLocalName = nameTable.Get(col.EncodedColumnName);
            string? columnNamespace = nameTable.Get(col.Namespace);
            if (columnLocalName == null)
            {
                return false;
            }
            XmlNodeIdentety idColumn = new XmlNodeIdentety(columnLocalName, columnNamespace);

            columns[idColumn] = col;

            if (col.ColumnName.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
            {
                HandleSpecialColumn(col, nameTable, columns);
            }


            return true;
        }

        private static bool AddColumnSchema(XmlNameTable nameTable, DataColumn col, XmlNodeIdHashtable columns)
        {
            string _columnLocalName = XmlConvert.EncodeLocalName(col.ColumnName);
            string columnLocalName =
                nameTable.Get(_columnLocalName) ?? // Look it up in a name table
                nameTable.Add(_columnLocalName);   // Not found? Add it

            col._encodedColumnName = columnLocalName;                            // And set it back

            string? columnNamespace = nameTable.Get(col.Namespace);             // Get column namespace from nametable

            if (columnNamespace == null)
            {                                       // Not found ?
                columnNamespace = nameTable.Add(col.Namespace);                 // Add it
            }
            else
            {
                if (col._columnUri != null)                                    // Update namespace
                    col._columnUri = columnNamespace;
            }
            // Create XmlNodeIdentety
            // for this column
            XmlNodeIdentety idColumn = new XmlNodeIdentety(columnLocalName, columnNamespace);
            columns[idColumn] = col;                                            // And add it to hashtable

            if (col.ColumnName.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
            {
                HandleSpecialColumn(col, nameTable, columns);
            }

            return true;
        }

        [MemberNotNull(nameof(_tableSchemaMap))]
        private void BuildIdentityMap(DataSet dataSet, XmlNameTable nameTable)
        {
            _tableSchemaMap = new XmlNodeIdHashtable(dataSet.Tables.Count);

            foreach (DataTable t in dataSet.Tables)
            {
                TableSchemaInfo? tableSchemaInfo = AddTableSchema(t, nameTable);
                if (tableSchemaInfo != null)
                {
                    foreach (DataColumn c in t.Columns)
                    {
                        // don't include auto-generated PK, FK and any hidden columns to be part of mapping
                        if (IsMappedColumn(c))
                        {
                            AddColumnSchema(c, nameTable, tableSchemaInfo.ColumnsSchemaMap);
                        }
                    }
                }
            }
        }

        // This one is used while reading data with preloaded schema
        [MemberNotNull(nameof(_tableSchemaMap))]
        private void BuildIdentityMap(XmlNameTable nameTable, DataSet dataSet)
        {
            _tableSchemaMap = new XmlNodeIdHashtable(dataSet.Tables.Count);
            // This hash table contains
            // tables schemas as TableSchemaInfo objects
            // These objects holds reference to the table.
            // Hash tables with columns schema maps
            // and child tables schema maps

            string dsNamespace =
                nameTable.Get(dataSet.Namespace) ?? // Attempt to look up DataSet namespace in the name table
                nameTable.Add(dataSet.Namespace);   //  Found? Nope. Add it

            dataSet._namespaceURI = dsNamespace;    // Set a DataSet namespace URI

            foreach (DataTable t in dataSet.Tables)
            {                    // For each table
                TableSchemaInfo tableSchemaInfo = AddTableSchema(nameTable, t);
                // Add table schema info to hash table

                if (tableSchemaInfo != null)
                {
                    foreach (DataColumn c in t.Columns)
                    {              // Add column schema map
                        // don't include auto-generated PK, FK and any hidden columns to be part of mapping
                        if (IsMappedColumn(c))
                        {                        // If mapped column
                            AddColumnSchema(nameTable, c, tableSchemaInfo.ColumnsSchemaMap);
                        }                                               // Add it to the map
                    }

                    // Add child nested tables to the schema

                    foreach (DataRelation r in t.ChildRelations)
                    {     // Do we have a child tables ?
                        if (r.Nested)
                        {                                 // Is it nested?
                            // don't include non nested tables

                            // Handle namespaces and names as usuall

                            string _tableLocalName = XmlConvert.EncodeLocalName(r.ChildTable.TableName);
                            string? tableLocalName =
                                nameTable.Get(_tableLocalName) ??
                                nameTable.Add(_tableLocalName);

                            string? tableNamespace =
                                nameTable.Get(r.ChildTable.Namespace) ??
                                nameTable.Add(r.ChildTable.Namespace);

                            XmlNodeIdentety idTable = new XmlNodeIdentety(tableLocalName, tableNamespace);
                            tableSchemaInfo.ColumnsSchemaMap[idTable] = r.ChildTable;
                        }
                    }
                }
            }
        }

        // Used for inference
        [MemberNotNull(nameof(_tableSchemaMap))]
        private void BuildIdentityMap(DataTable dataTable, XmlNameTable nameTable)
        {
            _tableSchemaMap = new XmlNodeIdHashtable(1);

            TableSchemaInfo? tableSchemaInfo = AddTableSchema(dataTable, nameTable);
            if (tableSchemaInfo != null)
            {
                foreach (DataColumn c in dataTable.Columns)
                {
                    // don't include auto-generated PK, FK and any hidden columns to be part of mapping
                    if (IsMappedColumn(c))
                    {
                        AddColumnSchema(c, nameTable, tableSchemaInfo.ColumnsSchemaMap);
                    }
                }
            }
        }

        // This one is used while reading data with preloaded schema
        [MemberNotNull(nameof(_tableSchemaMap))]
        private void BuildIdentityMap(XmlNameTable nameTable, DataTable dataTable)
        {
            ArrayList tableList = GetSelfAndDescendants(dataTable);     // Get list of tables we're loading
                                                                        // This includes our table and
                                                                        // related tables tree

            _tableSchemaMap = new XmlNodeIdHashtable(tableList.Count);
            // Create hash table to hold all
            // tables to load.

            foreach (DataTable t in tableList)
            {                        // For each table
                TableSchemaInfo tableSchemaInfo = AddTableSchema(nameTable, t);
                // Create schema info
                if (tableSchemaInfo != null)
                {
                    foreach (DataColumn c in t.Columns)
                    {              // Add column information
                        // don't include auto-generated PK, FK and any hidden columns to be part of mapping
                        if (IsMappedColumn(c))
                        {
                            AddColumnSchema(nameTable, c, tableSchemaInfo.ColumnsSchemaMap);
                        }
                    }

                    foreach (DataRelation r in t.ChildRelations)
                    {     // Add nested tables information
                        if (r.Nested)
                        {                                 // Is it nested?
                            // don't include non nested tables

                            // Handle namespaces and names as usuall

                            string _tableLocalName = XmlConvert.EncodeLocalName(r.ChildTable.TableName);

                            string? tableLocalName =
                                nameTable.Get(_tableLocalName) ??
                                nameTable.Add(_tableLocalName);

                            string? tableNamespace =
                                nameTable.Get(r.ChildTable.Namespace) ??
                                nameTable.Add(r.ChildTable.Namespace);

                            XmlNodeIdentety idTable = new XmlNodeIdentety(tableLocalName, tableNamespace);
                            tableSchemaInfo.ColumnsSchemaMap[idTable] = r.ChildTable;
                        }
                    }
                }
            }
        }

        private static ArrayList GetSelfAndDescendants(DataTable dt)
        { // breadth-first
            ArrayList tableList = new ArrayList();
            tableList.Add(dt);
            int nCounter = 0;

            while (nCounter < tableList.Count)
            {
                foreach (DataRelation childRelations in ((DataTable)tableList[nCounter]!).ChildRelations)
                {
                    if (!tableList.Contains(childRelations.ChildTable))
                        tableList.Add(childRelations.ChildTable);
                }
                nCounter++;
            }

            return tableList;
        }
        // Used to infer schema and top most node

        public object? GetColumnSchema(XmlNode node, bool fIgnoreNamespace)
        {
            Debug.Assert(node != null, "Argument validation");
            TableSchemaInfo? tableSchemaInfo;

            XmlNode? nodeRegion = (node.NodeType == XmlNodeType.Attribute) ? ((XmlAttribute)node).OwnerElement : node.ParentNode;

            do
            {
                if (nodeRegion == null || nodeRegion.NodeType != XmlNodeType.Element)
                {
                    return null;
                }
                tableSchemaInfo = (TableSchemaInfo?)(fIgnoreNamespace ? _tableSchemaMap[nodeRegion.LocalName] : _tableSchemaMap[nodeRegion]);

                nodeRegion = nodeRegion.ParentNode;
            } while (tableSchemaInfo == null);

            if (fIgnoreNamespace)
                return tableSchemaInfo.ColumnsSchemaMap[node.LocalName];
            else
                return tableSchemaInfo.ColumnsSchemaMap[node];
        }


        public object? GetColumnSchema(DataTable table, XmlReader dataReader, bool fIgnoreNamespace)
        {
            if ((_lastTableSchemaInfo == null) || (_lastTableSchemaInfo.TableSchema != table))
            {
                _lastTableSchemaInfo = (TableSchemaInfo)(fIgnoreNamespace ? _tableSchemaMap[table.EncodedTableName]! : _tableSchemaMap[table]!);
            }

            if (fIgnoreNamespace)
                return _lastTableSchemaInfo.ColumnsSchemaMap[dataReader.LocalName];
            return _lastTableSchemaInfo.ColumnsSchemaMap[dataReader];
        }

        // Used to infer schema

        public object? GetSchemaForNode(XmlNode node, bool fIgnoreNamespace)
        {
            TableSchemaInfo? tableSchemaInfo = null;

            if (node.NodeType == XmlNodeType.Element)
            {         // If element
                tableSchemaInfo = (TableSchemaInfo?)(fIgnoreNamespace ? _tableSchemaMap[node.LocalName] : _tableSchemaMap[node]);
            }                                                   // Look up table schema info for it

            if (tableSchemaInfo != null)
            {                      // Got info ?
                return tableSchemaInfo.TableSchema;             // Yes, Return table
            }

            return GetColumnSchema(node, fIgnoreNamespace);     // Attempt to locate column
        }

        public DataTable? GetTableForNode(XmlReader node, bool fIgnoreNamespace)
        {
            TableSchemaInfo? tableSchemaInfo = (TableSchemaInfo?)(fIgnoreNamespace ? _tableSchemaMap[node.LocalName] : _tableSchemaMap[node]);
            if (tableSchemaInfo != null)
            {
                _lastTableSchemaInfo = tableSchemaInfo;
                return _lastTableSchemaInfo.TableSchema;
            }
            return null;
        }

        private static void HandleSpecialColumn(DataColumn col, XmlNameTable nameTable, XmlNodeIdHashtable columns)
        {
            // if column name starts with xml, we encode it manually and add it for look up
            Debug.Assert(col.ColumnName.StartsWith("xml", StringComparison.OrdinalIgnoreCase), "column name should start with xml");
            string tempColumnName;

            if ('x' == col.ColumnName[0])
            {
                tempColumnName = "_x0078_"; // lower case xml... -> _x0078_ml...
            }
            else
            {
                tempColumnName = "_x0058_"; // upper case Xml... -> _x0058_ml...
            }

            tempColumnName = string.Concat(tempColumnName, col.ColumnName.AsSpan(1));

            if (nameTable.Get(tempColumnName) == null)
            {
                nameTable.Add(tempColumnName);
            }
            string? columnNamespace = nameTable.Get(col.Namespace);
            XmlNodeIdentety idColumn = new XmlNodeIdentety(tempColumnName, columnNamespace);
            columns[idColumn] = col;
        }
    }
}
