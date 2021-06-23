// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TODO: Enable after System.Private.Xml is annotated
#nullable disable

using System.Diagnostics;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class XMLDiffLoader
    {
        private ArrayList _tables;
        private DataSet _dataSet;
        private DataTable _dataTable;

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void LoadDiffGram(DataSet ds, XmlReader dataTextReader)
        {
            XmlReader reader = DataTextReader.CreateReader(dataTextReader);
            _dataSet = ds;
            while (reader.LocalName == Keywords.SQL_BEFORE && reader.NamespaceURI == Keywords.DFFNS)
            {
                ProcessDiffs(ds, reader);
                reader.Read(); // now the reader points to the error section
            }

            while (reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI == Keywords.DFFNS)
            {
                ProcessErrors(ds, reader);
                Debug.Assert(reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI == Keywords.DFFNS, "something fishy");
                reader.Read(); // pass the end of errors tag
            }
        }


        private void CreateTablesHierarchy(DataTable dt)
        {
            foreach (DataRelation r in dt.ChildRelations)
            {
                if (!_tables.Contains(r.ChildTable))
                {
                    _tables.Add(r.ChildTable);
                    CreateTablesHierarchy(r.ChildTable);
                }
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void LoadDiffGram(DataTable dt, XmlReader dataTextReader)
        {
            XmlReader reader = DataTextReader.CreateReader(dataTextReader);
            _dataTable = dt;
            _tables = new ArrayList();
            _tables.Add(dt);
            CreateTablesHierarchy(dt);

            while (reader.LocalName == Keywords.SQL_BEFORE && reader.NamespaceURI == Keywords.DFFNS)
            {
                ProcessDiffs(_tables, reader);
                reader.Read(); // now the reader points to the error section
            }

            while (reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI == Keywords.DFFNS)
            {
                ProcessErrors(_tables, reader);
                Debug.Assert(reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI == Keywords.DFFNS, "something fishy");
                reader.Read(); // pass the end of errors tag
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void ProcessDiffs(DataSet ds, XmlReader ssync)
        {
            DataTable tableBefore;
            DataRow row;
            int oldRowRecord;
            int pos = -1;

            int iSsyncDepth = ssync.Depth;
            ssync.Read(); // pass the before node.

            SkipWhitespaces(ssync);

            while (iSsyncDepth < ssync.Depth)
            {
                tableBefore = null;
                string diffId = null;

                oldRowRecord = -1;

                // the diffgramm always contains sql:before and sql:after pairs

                diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);
                bool hasErrors = ssync.GetAttribute(Keywords.HASERRORS, Keywords.DFFNS) == Keywords.TRUE;
                oldRowRecord = ReadOldRowData(ds, ref tableBefore, ref pos, ssync);
                if (oldRowRecord == -1)
                    continue;

                if (tableBefore == null)
                    throw ExceptionBuilder.DiffgramMissingSQL();

                row = (DataRow)tableBefore.RowDiffId[diffId];
                if (row != null)
                {
                    row._oldRecord = oldRowRecord;
                    tableBefore._recordManager[oldRowRecord] = row;
                }
                else
                {
                    row = tableBefore.NewEmptyRow();
                    tableBefore._recordManager[oldRowRecord] = row;
                    row._oldRecord = oldRowRecord;
                    row._newRecord = oldRowRecord;
                    tableBefore.Rows.DiffInsertAt(row, pos);
                    row.Delete();
                    if (hasErrors)
                        tableBefore.RowDiffId[diffId] = row;
                }
            }

            return;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void ProcessDiffs(ArrayList tableList, XmlReader ssync)
        {
            DataTable tableBefore;
            DataRow row;
            int oldRowRecord;
            int pos = -1;

            int iSsyncDepth = ssync.Depth;
            ssync.Read(); // pass the before node.

            //SkipWhitespaces(ssync); for given scenario does not require this change, but in fact we should do it.

            while (iSsyncDepth < ssync.Depth)
            {
                tableBefore = null;
                string diffId = null;

                oldRowRecord = -1;

                // the diffgramm always contains sql:before and sql:after pairs

                diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);
                bool hasErrors = ssync.GetAttribute(Keywords.HASERRORS, Keywords.DFFNS) == Keywords.TRUE;
                oldRowRecord = ReadOldRowData(_dataSet, ref tableBefore, ref pos, ssync);
                if (oldRowRecord == -1)
                    continue;

                if (tableBefore == null)
                    throw ExceptionBuilder.DiffgramMissingSQL();

                row = (DataRow)tableBefore.RowDiffId[diffId];

                if (row != null)
                {
                    row._oldRecord = oldRowRecord;
                    tableBefore._recordManager[oldRowRecord] = row;
                }
                else
                {
                    row = tableBefore.NewEmptyRow();
                    tableBefore._recordManager[oldRowRecord] = row;
                    row._oldRecord = oldRowRecord;
                    row._newRecord = oldRowRecord;
                    tableBefore.Rows.DiffInsertAt(row, pos);
                    row.Delete();
                    if (hasErrors)
                        tableBefore.RowDiffId[diffId] = row;
                }
            }

            return;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void ProcessErrors(DataSet ds, XmlReader ssync)
        {
            DataTable table;

            int iSsyncDepth = ssync.Depth;
            ssync.Read(); // pass the before node.

            while (iSsyncDepth < ssync.Depth)
            {
                table = ds.Tables.GetTable(XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI);
                if (table == null)
                    throw ExceptionBuilder.DiffgramMissingSQL();
                string diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);
                DataRow row = (DataRow)table.RowDiffId[diffId];
                string rowError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                if (rowError != null)
                    row.RowError = rowError;
                int iRowDepth = ssync.Depth;
                ssync.Read(); // we may be inside a column
                while (iRowDepth < ssync.Depth)
                {
                    if (XmlNodeType.Element == ssync.NodeType)
                    {
                        DataColumn col = table.Columns[XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI];
                        //if (col == null)
                        // throw exception here
                        string colError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                        row.SetColumnError(col, colError);
                    }

                    ssync.Read();
                }
                while ((ssync.NodeType == XmlNodeType.EndElement) && (iSsyncDepth < ssync.Depth))
                    ssync.Read();
            }

            return;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void ProcessErrors(ArrayList dt, XmlReader ssync)
        {
            DataTable table;

            int iSsyncDepth = ssync.Depth;
            ssync.Read(); // pass the before node.

            while (iSsyncDepth < ssync.Depth)
            {
                table = GetTable(XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI);
                if (table == null)
                    throw ExceptionBuilder.DiffgramMissingSQL();

                string diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);

                DataRow row = (DataRow)table.RowDiffId[diffId];
                if (row == null)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        row = (DataRow)((DataTable)dt[i]).RowDiffId[diffId];
                        if (row != null)
                        {
                            table = row.Table;
                            break;
                        }
                    }
                }
                string rowError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                if (rowError != null)
                    row.RowError = rowError;
                int iRowDepth = ssync.Depth;
                ssync.Read(); // we may be inside a column

                while (iRowDepth < ssync.Depth)
                {
                    if (XmlNodeType.Element == ssync.NodeType)
                    {
                        DataColumn col = table.Columns[XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI];
                        //if (col == null)
                        // throw exception here
                        string colError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                        row.SetColumnError(col, colError);
                    }
                    ssync.Read();
                }
                while ((ssync.NodeType == XmlNodeType.EndElement) && (iSsyncDepth < ssync.Depth))
                    ssync.Read();
            }

            return;
        }
        private DataTable GetTable(string tableName, string ns)
        {
            if (_tables == null)
                return _dataSet.Tables.GetTable(tableName, ns);

            if (_tables.Count == 0)
                return (DataTable)_tables[0];

            for (int i = 0; i < _tables.Count; i++)
            {
                DataTable dt = (DataTable)_tables[i];
                if (string.Equals(dt.TableName, tableName, StringComparison.Ordinal)
                    && string.Equals(dt.Namespace, ns, StringComparison.Ordinal))
                    return dt;
            }
            return null;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private int ReadOldRowData(DataSet ds, ref DataTable table, ref int pos, XmlReader row)
        {
            // read table information
            if (ds != null)
            {
                table = ds.Tables.GetTable(XmlConvert.DecodeName(row.LocalName), row.NamespaceURI);
            }
            else
            {
                table = GetTable(XmlConvert.DecodeName(row.LocalName), row.NamespaceURI);
            }

            if (table == null)
            {
                row.Skip(); // need to skip this element if we dont know about it, before returning -1
                return -1;
            }

            int iRowDepth = row.Depth;
            string value = null;

            value = row.GetAttribute(Keywords.ROWORDER, Keywords.MSDNS);
            if (!string.IsNullOrEmpty(value))
            {
                pos = (int)Convert.ChangeType(value, typeof(int), null);
            }

            int record = table.NewRecord();
            foreach (DataColumn col in table.Columns)
            {
                col[record] = DBNull.Value;
            }

            foreach (DataColumn col in table.Columns)
            {
                if ((col.ColumnMapping == MappingType.Element) ||
                    (col.ColumnMapping == MappingType.SimpleContent))
                    continue;

                if (col.ColumnMapping == MappingType.Hidden)
                {
                    value = row.GetAttribute("hidden" + col.EncodedColumnName, Keywords.MSDNS);
                }
                else
                {
                    value = row.GetAttribute(col.EncodedColumnName, col.Namespace);
                }

                if (value == null)
                {
                    continue;
                }

                col[record] = col.ConvertXmlToObject(value);
            }

            row.Read();
            SkipWhitespaces(row);

            int currentDepth = row.Depth;
            if (currentDepth <= iRowDepth)
            {
                // the node is empty
                if (currentDepth == iRowDepth && row.NodeType == XmlNodeType.EndElement)
                {
                    // read past the EndElement of the current row
                    // note: (currentDepth == iRowDepth) check is needed so we do not skip elements on parent rows.
                    row.Read();
                    SkipWhitespaces(row);
                }
                return record;
            }

            if (table.XmlText != null)
            {
                DataColumn col = table.XmlText;
                col[record] = col.ConvertXmlToObject(row.ReadString());
            }
            else
            {
                while (row.Depth > iRowDepth)
                {
                    string ln = XmlConvert.DecodeName(row.LocalName);
                    string ns = row.NamespaceURI;
                    DataColumn column = table.Columns[ln, ns];

                    if (column == null)
                    {
                        while ((row.NodeType != XmlNodeType.EndElement) && (row.LocalName != ln) && (row.NamespaceURI != ns))
                            row.Read(); // consume the current node
                        row.Read(); // now points to the next column
                        //SkipWhitespaces(row); seems no need, just in case if we see other issue , this will be here as hint
                        continue; // add a read here!
                    }

                    if (column.IsCustomType)
                    {
                        // if column's type is object or column type does not implement IXmlSerializable
                        bool isPolymorphism = (column.DataType == typeof(object) || (row.GetAttribute(Keywords.MSD_INSTANCETYPE, Keywords.MSDNS) != null) ||
                        (row.GetAttribute(Keywords.TYPE, Keywords.XSINS) != null));

                        bool skipped = false;
                        if (column.Table.DataSet != null && column.Table.DataSet._udtIsWrapped)
                        {
                            row.Read(); // if UDT is wrapped, skip the wrapper
                            skipped = true;
                        }

                        XmlRootAttribute xmlAttrib = null;

                        if (!isPolymorphism && !column.ImplementsIXMLSerializable)
                        { // THIS CHECK MAY BE IS WRONG think more
                            // if does not implement IXLSerializable, need to go with XmlSerializer: pass XmlRootAttribute
                            if (skipped)
                            {
                                xmlAttrib = new XmlRootAttribute(row.LocalName);
                                xmlAttrib.Namespace = row.NamespaceURI;
                            }
                            else
                            {
                                xmlAttrib = new XmlRootAttribute(column.EncodedColumnName);
                                xmlAttrib.Namespace = column.Namespace;
                            }
                        }
                        // for else case xmlAttrib MUST be null
                        column[record] = column.ConvertXmlToObject(row, xmlAttrib); // you need to pass null XmlAttib here


                        if (skipped)
                        {
                            row.Read(); // if Wrapper is skipped, skip its end tag
                        }
                    }
                    else
                    {
                        int iColumnDepth = row.Depth;
                        row.Read();

                        // SkipWhitespaces(row);seems no need, just in case if we see other issue , this will be here as hint
                        if (row.Depth > iColumnDepth)
                        { //we are inside the column
                            if (row.NodeType == XmlNodeType.Text || row.NodeType == XmlNodeType.Whitespace || row.NodeType == XmlNodeType.SignificantWhitespace)
                            {
                                string text = row.ReadString();
                                column[record] = column.ConvertXmlToObject(text);

                                row.Read(); // now points to the next column
                            }
                        }
                        else
                        {
                            // <element></element> case
                            if (column.DataType == typeof(string))
                                column[record] = string.Empty;
                        }
                    }
                }
            }
            row.Read(); //now it should point to next row
            SkipWhitespaces(row);
            return record;
        }

        internal void SkipWhitespaces(XmlReader reader)
        {
            while (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace)
            {
                reader.Read();
            }
        }
    }
}
