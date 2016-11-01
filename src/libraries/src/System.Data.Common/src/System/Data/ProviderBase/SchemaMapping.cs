// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.ProviderBase
{
    internal sealed class SchemaMapping
    {
        // DataColumns match in length and name order as the DataReader, no chapters
        private const int MapExactMatch = 0;

        // DataColumns has different length, but correct name order as the DataReader, no chapters
        private const int MapDifferentSize = 1;

        // DataColumns may have different length, but a differant name ordering as the DataReader, no chapters
        private const int MapReorderedValues = 2;

        // DataColumns may have different length, but correct name order as the DataReader, with chapters
        private const int MapChapters = 3;

        // DataColumns may have different length, but a differant name ordering as the DataReader, with chapters
        private const int MapChaptersReordered = 4;

        // map xml string data to DataColumn with DataType=typeof(SqlXml)
        private const int SqlXml = 1;

        // map xml string data to DataColumn with DataType=typeof(XmlDocument)
        private const int XmlDocument = 2;

        private readonly DataSet _dataSet; // the current dataset, may be null if we are only filling a DataTable
        private DataTable _dataTable; // the current DataTable, should never be null

        private readonly DataAdapter _adapter;
        private readonly DataReaderContainer _dataReader;
        private readonly DataTable _schemaTable;  // will be null if Fill without schema
        private readonly DataTableMapping _tableMapping;

        // unique (generated) names based from DataReader.GetName(i)
        private readonly string[] _fieldNames;

        private readonly object[] _readerDataValues;
        private object[] _mappedDataValues; // array passed to dataRow.AddUpdate(), if needed

        private int[] _indexMap;     // index map that maps dataValues -> _mappedDataValues, if needed
        private bool[] _chapterMap;  // which DataReader indexes have chapters

        private int[] _xmlMap; // map which value in _readerDataValues to convert to a Xml datatype, (SqlXml/XmlDocument)

        private int _mappedMode; // modes as described as above
        private int _mappedLength;

        private readonly LoadOption _loadOption;

        internal SchemaMapping(DataAdapter adapter, DataSet dataset, DataTable datatable, DataReaderContainer dataReader, bool keyInfo,
                                    SchemaType schemaType, string sourceTableName, bool gettingData,
                                    DataColumn parentChapterColumn, object parentChapterValue)
        {
            Debug.Assert(null != adapter, nameof(adapter));
            Debug.Assert(null != dataReader, nameof(dataReader));
            Debug.Assert(0 < dataReader.FieldCount, "FieldCount");
            Debug.Assert(null != dataset || null != datatable, "SchemaMapping - null dataSet");
            Debug.Assert(SchemaType.Mapped == schemaType || SchemaType.Source == schemaType, "SetupSchema - invalid schemaType");

            _dataSet = dataset;     // setting DataSet implies chapters are supported
            _dataTable = datatable; // setting only DataTable, not DataSet implies chapters are not supported
            _adapter = adapter;
            _dataReader = dataReader;

            if (keyInfo)
            {
                _schemaTable = dataReader.GetSchemaTable();
            }

            if (adapter.ShouldSerializeFillLoadOption())
            {
                _loadOption = adapter.FillLoadOption;
            }
            else if (adapter.AcceptChangesDuringFill)
            {
                _loadOption = (LoadOption)4; // true
            }
            else
            {
                _loadOption = (LoadOption)5; //false
            }

            MissingMappingAction mappingAction;
            MissingSchemaAction schemaAction;
            if (SchemaType.Mapped == schemaType)
            {
                mappingAction = _adapter.MissingMappingAction;
                schemaAction = _adapter.MissingSchemaAction;
                if (!string.IsNullOrEmpty(sourceTableName))
                {
                    _tableMapping = _adapter.GetTableMappingBySchemaAction(sourceTableName, sourceTableName, mappingAction);
                }
                else if (null != _dataTable)
                {
                    int index = _adapter.IndexOfDataSetTable(_dataTable.TableName);
                    if (-1 != index)
                    {
                        _tableMapping = _adapter.TableMappings[index];
                    }
                    else
                    {
                        switch (mappingAction)
                        {
                            case MissingMappingAction.Passthrough:
                                _tableMapping = new DataTableMapping(_dataTable.TableName, _dataTable.TableName);
                                break;
                            case MissingMappingAction.Ignore:
                                _tableMapping = null;
                                break;
                            case MissingMappingAction.Error:
                                throw ADP.MissingTableMappingDestination(_dataTable.TableName);
                            default:
                                throw ADP.InvalidMissingMappingAction(mappingAction);
                        }
                    }
                }
            }
            else if (SchemaType.Source == schemaType)
            {
                mappingAction = System.Data.MissingMappingAction.Passthrough;
                schemaAction = Data.MissingSchemaAction.Add;
                if (!string.IsNullOrEmpty(sourceTableName))
                {
                    _tableMapping = DataTableMappingCollection.GetTableMappingBySchemaAction(null, sourceTableName, sourceTableName, mappingAction);
                }
                else if (null != _dataTable)
                {
                    int index = _adapter.IndexOfDataSetTable(_dataTable.TableName);
                    if (-1 != index)
                    {
                        _tableMapping = _adapter.TableMappings[index];
                    }
                    else
                    {
                        _tableMapping = new DataTableMapping(_dataTable.TableName, _dataTable.TableName);
                    }
                }
            }
            else
            {
                throw ADP.InvalidSchemaType(schemaType);
            }

            if (null != _tableMapping)
            {
                if (null == _dataTable)
                {
                    _dataTable = _tableMapping.GetDataTableBySchemaAction(_dataSet, schemaAction);
                }
                if (null != _dataTable)
                {
                    _fieldNames = GenerateFieldNames(dataReader);

                    if (null == _schemaTable)
                    {
                        _readerDataValues = SetupSchemaWithoutKeyInfo(mappingAction, schemaAction, gettingData, parentChapterColumn, parentChapterValue);
                    }
                    else
                    {
                        _readerDataValues = SetupSchemaWithKeyInfo(mappingAction, schemaAction, gettingData, parentChapterColumn, parentChapterValue);
                    }
                }
                // else (null == _dataTable) which means ignore (mapped to nothing)
            }
        }

        internal DataReaderContainer DataReader
        {
            get
            {
                return _dataReader;
            }
        }

        internal DataTable DataTable
        {
            get
            {
                return _dataTable;
            }
        }

        internal object[] DataValues
        {
            get
            {
                return _readerDataValues;
            }
        }

        internal void ApplyToDataRow(DataRow dataRow)
        {
            DataColumnCollection columns = dataRow.Table.Columns;
            _dataReader.GetValues(_readerDataValues);

            object[] mapped = GetMappedValues();
            bool[] readOnly = new bool[mapped.Length];
            for (int i = 0; i < readOnly.Length; ++i)
            {
                readOnly[i] = columns[i].ReadOnly;
            }

            try
            {
                try
                {
                    // allow all columns to be written to
                    for (int i = 0; i < readOnly.Length; ++i)
                    {
                        if (0 == columns[i].Expression.Length)
                        {
                            columns[i].ReadOnly = false;
                        }
                    }

                    for (int i = 0; i < mapped.Length; ++i)
                    {
                        if (null != mapped[i])
                        {
                            dataRow[i] = mapped[i];
                        }
                    }
                }
                finally
                {
                    // ReadOnly
                    // reset readonly flag on all columns
                    for (int i = 0; i < readOnly.Length; ++i)
                    {
                        if (0 == columns[i].Expression.Length)
                        {
                            columns[i].ReadOnly = readOnly[i];
                        }
                    }
                }
            }
            finally
            { // FreeDataRowChapters
                if (null != _chapterMap)
                {
                    FreeDataRowChapters();
                }
            }
        }

        private void MappedChapterIndex()
        { // mode 4
            int length = _mappedLength;

            for (int i = 0; i < length; i++)
            {
                int k = _indexMap[i];
                if (0 <= k)
                {
                    _mappedDataValues[k] = _readerDataValues[i]; // from reader to dataset
                    if (_chapterMap[i])
                    {
                        _mappedDataValues[k] = null; // InvalidCast from DataReader to AutoIncrement DataColumn
                    }
                }
            }
        }

        private void MappedChapter()
        { // mode 3
            int length = _mappedLength;

            for (int i = 0; i < length; i++)
            {
                _mappedDataValues[i] = _readerDataValues[i]; // from reader to dataset
                if (_chapterMap[i])
                {
                    _mappedDataValues[i] = null; // InvalidCast from DataReader to AutoIncrement DataColumn
                }
            }
        }

        private void MappedIndex()
        { // mode 2
            Debug.Assert(_mappedLength == _indexMap.Length, "incorrect precomputed length");

            int length = _mappedLength;
            for (int i = 0; i < length; i++)
            {
                int k = _indexMap[i];
                if (0 <= k)
                {
                    _mappedDataValues[k] = _readerDataValues[i]; // from reader to dataset
                }
            }
        }

        private void MappedValues()
        { // mode 1
            Debug.Assert(_mappedLength == Math.Min(_readerDataValues.Length, _mappedDataValues.Length), "incorrect precomputed length");

            int length = _mappedLength;
            for (int i = 0; i < length; ++i)
            {
                _mappedDataValues[i] = _readerDataValues[i]; // from reader to dataset
            };
        }

        private object[] GetMappedValues()
        { // mode 0
            if (null != _xmlMap)
            {
                for (int i = 0; i < _xmlMap.Length; ++i)
                {
                    if (0 != _xmlMap[i])
                    {
                        // get the string/SqlString xml value
                        string xml = _readerDataValues[i] as string;
                        if ((null == xml) && (_readerDataValues[i] is System.Data.SqlTypes.SqlString))
                        {
                            System.Data.SqlTypes.SqlString x = (System.Data.SqlTypes.SqlString)_readerDataValues[i];
                            if (!x.IsNull)
                            {
                                xml = x.Value;
                            }
                            else
                            {
                                switch (_xmlMap[i])
                                {
                                    case SqlXml:
                                        // map strongly typed SqlString.Null to SqlXml.Null
                                        _readerDataValues[i] = System.Data.SqlTypes.SqlXml.Null;
                                        break;
                                    default:
                                        _readerDataValues[i] = DBNull.Value;
                                        break;
                                }
                            }
                        }
                        if (null != xml)
                        {
                            switch (_xmlMap[i])
                            {
                                case SqlXml: // turn string into a SqlXml value for DataColumn
                                    System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings();
                                    settings.ConformanceLevel = System.Xml.ConformanceLevel.Fragment;
                                    System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader(xml), settings, (string)null);
                                    _readerDataValues[i] = new System.Data.SqlTypes.SqlXml(reader);
                                    break;
                                case XmlDocument: // turn string into XmlDocument value for DataColumn
                                    System.Xml.XmlDocument document = new System.Xml.XmlDocument();
                                    document.LoadXml(xml);
                                    _readerDataValues[i] = document;
                                    break;
                            }
                            // default: let value fallthrough to DataSet which may fail with ArgumentException
                        }
                    }
                }
            }

            switch (_mappedMode)
            {
                default:
                case MapExactMatch:
                    Debug.Assert(0 == _mappedMode, "incorrect mappedMode");
                    Debug.Assert((null == _chapterMap) && (null == _indexMap) && (null == _mappedDataValues), "incorrect MappedValues");
                    return _readerDataValues;  // from reader to dataset
                case MapDifferentSize:
                    Debug.Assert((null == _chapterMap) && (null == _indexMap) && (null != _mappedDataValues), "incorrect MappedValues");
                    MappedValues();
                    break;
                case MapReorderedValues:
                    Debug.Assert((null == _chapterMap) && (null != _indexMap) && (null != _mappedDataValues), "incorrect MappedValues");
                    MappedIndex();
                    break;
                case MapChapters:
                    Debug.Assert((null != _chapterMap) && (null == _indexMap) && (null != _mappedDataValues), "incorrect MappedValues");
                    MappedChapter();
                    break;
                case MapChaptersReordered:
                    Debug.Assert((null != _chapterMap) && (null != _indexMap) && (null != _mappedDataValues), "incorrect MappedValues");
                    MappedChapterIndex();
                    break;
            }
            return _mappedDataValues;
        }

        internal void LoadDataRowWithClear()
        {
            // for FillErrorEvent to ensure no values leftover from previous row
            for (int i = 0; i < _readerDataValues.Length; ++i)
            {
                _readerDataValues[i] = null;
            }
            LoadDataRow();
        }

        internal void LoadDataRow()
        {
            try
            {
                _dataReader.GetValues(_readerDataValues);
                object[] mapped = GetMappedValues();

                DataRow dataRow;
                switch (_loadOption)
                {
                    case LoadOption.OverwriteChanges:
                    case LoadOption.PreserveChanges:
                    case LoadOption.Upsert:
                        dataRow = _dataTable.LoadDataRow(mapped, _loadOption);
                        break;
                    case (LoadOption)4: // true
                        dataRow = _dataTable.LoadDataRow(mapped, true);
                        break;
                    case (LoadOption)5: // false
                        dataRow = _dataTable.LoadDataRow(mapped, false);
                        break;
                    default:
                        Debug.Assert(false, "unexpected LoadOption");
                        throw ADP.InvalidLoadOption(_loadOption);
                }
                if ((null != _chapterMap) && (null != _dataSet))
                {
                    LoadDataRowChapters(dataRow);
                }
            }
            finally
            {
                if (null != _chapterMap)
                {
                    FreeDataRowChapters();
                }
            }
        }

        private void FreeDataRowChapters()
        {
            for (int i = 0; i < _chapterMap.Length; ++i)
            {
                if (_chapterMap[i])
                {
                    IDisposable disposable = (_readerDataValues[i] as IDisposable);
                    if (null != disposable)
                    {
                        _readerDataValues[i] = null;
                        disposable.Dispose();
                    }
                }
            }
        }

        internal int LoadDataRowChapters(DataRow dataRow)
        {
            int datarowadded = 0;

            int rowLength = _chapterMap.Length;
            for (int i = 0; i < rowLength; ++i)
            {
                if (_chapterMap[i])
                {
                    object readerValue = _readerDataValues[i];
                    if ((null != readerValue) && !Convert.IsDBNull(readerValue))
                    {
                        _readerDataValues[i] = null;

                        using (IDataReader nestedReader = (IDataReader)readerValue)
                        {
                            if (!nestedReader.IsClosed)
                            {
                                Debug.Assert(null != _dataSet, "if chapters, then Fill(DataSet,...) not Fill(DataTable,...)");

                                object parentChapterValue;
                                DataColumn parentChapterColumn;
                                if (null == _indexMap)
                                {
                                    parentChapterColumn = _dataTable.Columns[i];
                                    parentChapterValue = dataRow[parentChapterColumn];
                                }
                                else
                                {
                                    parentChapterColumn = _dataTable.Columns[_indexMap[i]];
                                    parentChapterValue = dataRow[parentChapterColumn];
                                }

                                // correct on Fill, not FillFromReader
                                string chapterTableName = _tableMapping.SourceTable + _fieldNames[i];

                                DataReaderContainer readerHandler = DataReaderContainer.Create(nestedReader, _dataReader.ReturnProviderSpecificTypes);
                                datarowadded += _adapter.FillFromReader(_dataSet, null, chapterTableName, readerHandler, 0, 0, parentChapterColumn, parentChapterValue);
                            }
                        }
                    }
                }
            }
            return datarowadded;
        }

        private int[] CreateIndexMap(int count, int index)
        {
            int[] values = new int[count];
            for (int i = 0; i < index; ++i)
            {
                values[i] = i;
            }
            return values;
        }

        private static string[] GenerateFieldNames(DataReaderContainer dataReader)
        {
            string[] fieldNames = new string[dataReader.FieldCount];
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                fieldNames[i] = dataReader.GetName(i);
            }
            ADP.BuildSchemaTableInfoTableNames(fieldNames);
            return fieldNames;
        }

        private DataColumn[] ResizeColumnArray(DataColumn[] rgcol, int len)
        {
            Debug.Assert(rgcol != null, "invalid call to ResizeArray");
            Debug.Assert(len <= rgcol.Length, "invalid len passed to ResizeArray");
            var tmp = new DataColumn[len];
            Array.Copy(rgcol, 0, tmp, 0, len);
            return tmp;
        }

        private void AddItemToAllowRollback(ref List<object> items, object value)
        {
            if (null == items)
            {
                items = new List<object>();
            }
            items.Add(value);
        }

        private void RollbackAddedItems(List<object> items)
        {
            if (null != items)
            {
                for (int i = items.Count - 1; 0 <= i; --i)
                {
                    // remove columns that were added now that we are failing
                    if (null != items[i])
                    {
                        DataColumn column = (items[i] as DataColumn);
                        if (null != column)
                        {
                            if (null != column.Table)
                            {
                                column.Table.Columns.Remove(column);
                            }
                        }
                        else
                        {
                            DataTable table = (items[i] as DataTable);
                            if (null != table)
                            {
                                if (null != table.DataSet)
                                {
                                    table.DataSet.Tables.Remove(table);
                                }
                            }
                        }
                    }
                }
            }
        }

        private object[] SetupSchemaWithoutKeyInfo(MissingMappingAction mappingAction, MissingSchemaAction schemaAction, bool gettingData, DataColumn parentChapterColumn, object chapterValue)
        {
            int[] columnIndexMap = null;
            bool[] chapterIndexMap = null;

            int mappingCount = 0;
            int count = _dataReader.FieldCount;

            object[] dataValues = null;
            List<object> addedItems = null;
            try
            {
                DataColumnCollection columnCollection = _dataTable.Columns;
                columnCollection.EnsureAdditionalCapacity(count + (chapterValue != null ? 1 : 0));
                // We can always just create column if there are no existing column or column mappings, and the mapping action is passthrough
                bool alwaysCreateColumns = ((_dataTable.Columns.Count == 0) && ((_tableMapping.ColumnMappings == null) || (_tableMapping.ColumnMappings.Count == 0)) && (mappingAction == MissingMappingAction.Passthrough));

                for (int i = 0; i < count; ++i)
                {
                    bool ischapter = false;
                    Type fieldType = _dataReader.GetFieldType(i);

                    if (null == fieldType)
                    {
                        throw ADP.MissingDataReaderFieldType(i);
                    }

                    // if IDataReader, hierarchy exists and we will use an Int32,AutoIncrementColumn in this table
                    if (typeof(IDataReader).IsAssignableFrom(fieldType))
                    {
                        if (null == chapterIndexMap)
                        {
                            chapterIndexMap = new bool[count];
                        }
                        chapterIndexMap[i] = ischapter = true;
                        fieldType = typeof(int);
                    }
                    else if (typeof(System.Data.SqlTypes.SqlXml).IsAssignableFrom(fieldType))
                    {
                        if (null == _xmlMap)
                        { // map to DataColumn with DataType=typeof(SqlXml)
                            _xmlMap = new int[count];
                        }
                        _xmlMap[i] = SqlXml; // track its xml data
                    }
                    else if (typeof(System.Xml.XmlReader).IsAssignableFrom(fieldType))
                    {
                        fieldType = typeof(string); // map to DataColumn with DataType=typeof(string)
                        if (null == _xmlMap)
                        {
                            _xmlMap = new int[count];
                        }
                        _xmlMap[i] = XmlDocument; // track its xml data
                    }

                    DataColumn dataColumn;
                    if (alwaysCreateColumns)
                    {
                        dataColumn = DataColumnMapping.CreateDataColumnBySchemaAction(_fieldNames[i], _fieldNames[i], _dataTable, fieldType, schemaAction);
                    }
                    else
                    {
                        dataColumn = _tableMapping.GetDataColumn(_fieldNames[i], fieldType, _dataTable, mappingAction, schemaAction);
                    }

                    if (null == dataColumn)
                    {
                        if (null == columnIndexMap)
                        {
                            columnIndexMap = CreateIndexMap(count, i);
                        }
                        columnIndexMap[i] = -1;
                        continue; // null means ignore (mapped to nothing)
                    }
                    else if ((null != _xmlMap) && (0 != _xmlMap[i]))
                    {
                        if (typeof(System.Data.SqlTypes.SqlXml) == dataColumn.DataType)
                        {
                            _xmlMap[i] = SqlXml;
                        }
                        else if (typeof(System.Xml.XmlDocument) == dataColumn.DataType)
                        {
                            _xmlMap[i] = XmlDocument;
                        }
                        else
                        {
                            _xmlMap[i] = 0; // datacolumn is not a specific Xml dataType, i.e. string

                            int total = 0;
                            for (int x = 0; x < _xmlMap.Length; ++x)
                            {
                                total += _xmlMap[x];
                            }
                            if (0 == total)
                            { // not mapping to a specific Xml datatype, get rid of the map
                                _xmlMap = null;
                            }
                        }
                    }

                    if (null == dataColumn.Table)
                    {
                        if (ischapter)
                        {
                            dataColumn.AllowDBNull = false;
                            dataColumn.AutoIncrement = true;
                            dataColumn.ReadOnly = true;
                        }
                        AddItemToAllowRollback(ref addedItems, dataColumn);
                        columnCollection.Add(dataColumn);
                    }
                    else if (ischapter && !dataColumn.AutoIncrement)
                    {
                        throw ADP.FillChapterAutoIncrement();
                    }


                    if (null != columnIndexMap)
                    {
                        columnIndexMap[i] = dataColumn.Ordinal;
                    }
                    else if (i != dataColumn.Ordinal)
                    {
                        columnIndexMap = CreateIndexMap(count, i);
                        columnIndexMap[i] = dataColumn.Ordinal;
                    }
                    // else i == dataColumn.Ordinal and columnIndexMap can be optimized out

                    mappingCount++;
                }
                bool addDataRelation = false;
                DataColumn chapterColumn = null;
                if (null != chapterValue)
                { // add the extra column in the child table
                    Type fieldType = chapterValue.GetType();

                    chapterColumn = _tableMapping.GetDataColumn(_tableMapping.SourceTable, fieldType, _dataTable, mappingAction, schemaAction);
                    if (null != chapterColumn)
                    {
                        if (null == chapterColumn.Table)
                        {
                            AddItemToAllowRollback(ref addedItems, chapterColumn);
                            columnCollection.Add(chapterColumn);
                            addDataRelation = (null != parentChapterColumn);
                        }
                        mappingCount++;
                    }
                }

                if (0 < mappingCount)
                {
                    if ((null != _dataSet) && (null == _dataTable.DataSet))
                    {
                        // Allowed to throw exception if DataTable is from wrong DataSet
                        AddItemToAllowRollback(ref addedItems, _dataTable);
                        _dataSet.Tables.Add(_dataTable);
                    }
                    if (gettingData)
                    {
                        if (null == columnCollection)
                        {
                            columnCollection = _dataTable.Columns;
                        }
                        _indexMap = columnIndexMap;
                        _chapterMap = chapterIndexMap;
                        dataValues = SetupMapping(count, columnCollection, chapterColumn, chapterValue);
                    }
                    else
                    {
                        // debug only, but for retail debug ability
                        _mappedMode = -1;
                    }
                }
                else
                {
                    _dataTable = null;
                }

                if (addDataRelation)
                {
                    AddRelation(parentChapterColumn, chapterColumn);
                }
            }
            catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
            {
                RollbackAddedItems(addedItems);
                throw;
            }
            return dataValues;
        }

        private object[] SetupSchemaWithKeyInfo(MissingMappingAction mappingAction, MissingSchemaAction schemaAction, bool gettingData, DataColumn parentChapterColumn, object chapterValue)
        {
            // must sort rows from schema table by ordinal because Jet is sorted by coumn name
            DbSchemaRow[] schemaRows = DbSchemaRow.GetSortedSchemaRows(_schemaTable, _dataReader.ReturnProviderSpecificTypes);
            Debug.Assert(null != schemaRows, "SchemaSetup - null DbSchemaRow[]");
            Debug.Assert(_dataReader.FieldCount <= schemaRows.Length, "unexpected fewer rows in Schema than FieldCount");

            if (0 == schemaRows.Length)
            {
                _dataTable = null;
                return null;
            }

            // Everett behavior, always add a primary key if a primary key didn't exist before
            // Whidbey behavior, same as Everett unless using LoadOption then add primary key only if no columns previously existed
            bool addPrimaryKeys = (((0 == _dataTable.PrimaryKey.Length) && ((4 <= (int)_loadOption) || (0 == _dataTable.Rows.Count)))
                                    || (0 == _dataTable.Columns.Count));

            DataColumn[] keys = null;
            int keyCount = 0;
            bool isPrimary = true; // assume key info (if any) is about a primary key

            string keyBaseTable = null;
            string commonBaseTable = null;

            bool keyFromMultiTable = false;
            bool commonFromMultiTable = false;

            int[] columnIndexMap = null;
            bool[] chapterIndexMap = null;

            int mappingCount = 0;

            object[] dataValues = null;
            List<object> addedItems = null;
            DataColumnCollection columnCollection = _dataTable.Columns;
            try
            {
                for (int sortedIndex = 0; sortedIndex < schemaRows.Length; ++sortedIndex)
                {
                    DbSchemaRow schemaRow = schemaRows[sortedIndex];

                    int unsortedIndex = schemaRow.UnsortedIndex;

                    bool ischapter = false;
                    Type fieldType = schemaRow.DataType;
                    if (null == fieldType)
                    {
                        fieldType = _dataReader.GetFieldType(sortedIndex);
                    }
                    if (null == fieldType)
                    {
                        throw ADP.MissingDataReaderFieldType(sortedIndex);
                    }

                    // if IDataReader, hierarchy exists and we will use an Int32,AutoIncrementColumn in this table
                    if (typeof(IDataReader).IsAssignableFrom(fieldType))
                    {
                        if (null == chapterIndexMap)
                        {
                            chapterIndexMap = new bool[schemaRows.Length];
                        }
                        chapterIndexMap[unsortedIndex] = ischapter = true;
                        fieldType = typeof(int);
                    }
                    else if (typeof(System.Data.SqlTypes.SqlXml).IsAssignableFrom(fieldType))
                    {
                        if (null == _xmlMap)
                        {
                            _xmlMap = new int[schemaRows.Length];
                        }
                        _xmlMap[sortedIndex] = SqlXml;
                    }
                    else if (typeof(System.Xml.XmlReader).IsAssignableFrom(fieldType))
                    {
                        fieldType = typeof(string);
                        if (null == _xmlMap)
                        {
                            _xmlMap = new int[schemaRows.Length];
                        }
                        _xmlMap[sortedIndex] = XmlDocument;
                    }

                    DataColumn dataColumn = null;
                    if (!schemaRow.IsHidden)
                    {
                        dataColumn = _tableMapping.GetDataColumn(_fieldNames[sortedIndex], fieldType, _dataTable, mappingAction, schemaAction);
                    }

                    string basetable = /*schemaRow.BaseServerName+schemaRow.BaseCatalogName+schemaRow.BaseSchemaName+*/ schemaRow.BaseTableName;
                    if (null == dataColumn)
                    {
                        if (null == columnIndexMap)
                        {
                            columnIndexMap = CreateIndexMap(schemaRows.Length, unsortedIndex);
                        }
                        columnIndexMap[unsortedIndex] = -1;

                        // if the column is not mapped and it is a key, then don't add any key information
                        if (schemaRow.IsKey)
                        {
#if DEBUG
                            if (AdapterSwitches.DataSchema.TraceVerbose)
                            {
                                Debug.WriteLine("SetupSchema: partial primary key detected");
                            }
#endif
                            // if the hidden key comes from a different table - don't throw away the primary key
                            // example SELECT [T2].[ID], [T2].[ProdID], [T2].[VendorName] FROM [Vendor] AS [T2], [Prod] AS [T1] WHERE (([T1].[ProdID] = [T2].[ProdID]))
                            if (keyFromMultiTable || (schemaRow.BaseTableName == keyBaseTable))
                            {
                                addPrimaryKeys = false; // don't add any future keys now
                                keys = null; // get rid of any keys we've seen
                            }
                        }
                        continue; // null means ignore (mapped to nothing)
                    }
                    else if ((null != _xmlMap) && (0 != _xmlMap[sortedIndex]))
                    {
                        if (typeof(System.Data.SqlTypes.SqlXml) == dataColumn.DataType)
                        {
                            _xmlMap[sortedIndex] = SqlXml;
                        }
                        else if (typeof(System.Xml.XmlDocument) == dataColumn.DataType)
                        {
                            _xmlMap[sortedIndex] = XmlDocument;
                        }
                        else
                        {
                            _xmlMap[sortedIndex] = 0; // datacolumn is not a specific Xml dataType, i.e. string

                            int total = 0;
                            for (int x = 0; x < _xmlMap.Length; ++x)
                            {
                                total += _xmlMap[x];
                            }
                            if (0 == total)
                            { // not mapping to a specific Xml datatype, get rid of the map
                                _xmlMap = null;
                            }
                        }
                    }

                    if (schemaRow.IsKey)
                    {
                        if (basetable != keyBaseTable)
                        {
                            if (null == keyBaseTable)
                            {
                                keyBaseTable = basetable;
                            }
                            else keyFromMultiTable = true;
                        }
                    }

                    if (ischapter)
                    {
                        if (null == dataColumn.Table)
                        {
                            dataColumn.AllowDBNull = false;
                            dataColumn.AutoIncrement = true;
                            dataColumn.ReadOnly = true;
                        }
                        else if (!dataColumn.AutoIncrement)
                        {
                            throw ADP.FillChapterAutoIncrement();
                        }
                    }
                    else
                    {
                        if (!commonFromMultiTable)
                        {
                            if ((basetable != commonBaseTable) && (!string.IsNullOrEmpty(basetable)))
                            {
                                if (null == commonBaseTable)
                                {
                                    commonBaseTable = basetable;
                                }
                                else
                                {
                                    commonFromMultiTable = true;
                                }
                            }
                        }
                        if (4 <= (int)_loadOption)
                        {
                            if (schemaRow.IsAutoIncrement && DataColumn.IsAutoIncrementType(fieldType))
                            {
                                // CONSIDER: use T-SQL "IDENT_INCR('table_or_view')" and "IDENT_SEED('table_or_view')"
                                //           functions to obtain the actual increment and seed values
                                dataColumn.AutoIncrement = true;

                                if (!schemaRow.AllowDBNull)
                                {
                                    dataColumn.AllowDBNull = false;
                                }
                            }

                            // setup maxLength, only for string columns since this is all the DataSet supports
                            if (fieldType == typeof(string))
                            {
                                // schemaRow.Size is count of characters for string columns, count of bytes otherwise
                                dataColumn.MaxLength = schemaRow.Size > 0 ? schemaRow.Size : -1;
                            }

                            if (schemaRow.IsReadOnly)
                            {
                                dataColumn.ReadOnly = true;
                            }
                            if (!schemaRow.AllowDBNull && (!schemaRow.IsReadOnly || schemaRow.IsKey))
                            {
                                dataColumn.AllowDBNull = false;
                            }

                            if (schemaRow.IsUnique && !schemaRow.IsKey && !fieldType.IsArray)
                            {
                                // note, arrays are not comparable so only mark non-arrays as unique, ie timestamp columns
                                // are unique, but not comparable
                                dataColumn.Unique = true;

                                if (!schemaRow.AllowDBNull)
                                {
                                    dataColumn.AllowDBNull = false;
                                }
                            }
                        }
                        else if (null == dataColumn.Table)
                        {
                            dataColumn.AutoIncrement = schemaRow.IsAutoIncrement;
                            dataColumn.AllowDBNull = schemaRow.AllowDBNull;
                            dataColumn.ReadOnly = schemaRow.IsReadOnly;
                            dataColumn.Unique = schemaRow.IsUnique;

                            if (fieldType == typeof(string) || (fieldType == typeof(SqlTypes.SqlString)))
                            {
                                // schemaRow.Size is count of characters for string columns, count of bytes otherwise
                                dataColumn.MaxLength = schemaRow.Size;
                            }
                        }
                    }
                    if (null == dataColumn.Table)
                    {
                        if (4 > (int)_loadOption)
                        {
                            AddAdditionalProperties(dataColumn, schemaRow.DataRow);
                        }
                        AddItemToAllowRollback(ref addedItems, dataColumn);
                        columnCollection.Add(dataColumn);
                    }

                    // The server sends us one key per table according to these rules.
                    //
                    // 1. If the table has a primary key, the server sends us this key.
                    // 2. If the table has a primary key and a unique key, it sends us the primary key
                    // 3. if the table has no primary key but has a unique key, it sends us the unique key
                    //
                    // In case 3, we will promote a unique key to a primary key IFF all the columns that compose
                    // that key are not nullable since no columns in a primary key can be null.  If one or more
                    // of the keys is nullable, then we will add a unique constraint.
                    //
                    if (addPrimaryKeys && schemaRow.IsKey)
                    {
                        if (keys == null)
                        {
                            keys = new DataColumn[schemaRows.Length];
                        }
                        keys[keyCount++] = dataColumn;
#if DEBUG
                        if (AdapterSwitches.DataSchema.TraceVerbose)
                        {
                            Debug.WriteLine("SetupSchema: building list of " + ((isPrimary) ? "PrimaryKey" : "UniqueConstraint"));
                        }
#endif
                        // see case 3 above, we do want dataColumn.AllowDBNull not schemaRow.AllowDBNull
                        // otherwise adding PrimaryKey will change AllowDBNull to false
                        if (isPrimary && dataColumn.AllowDBNull)
                        {
#if DEBUG
                            if (AdapterSwitches.DataSchema.TraceVerbose)
                            {
                                Debug.WriteLine("SetupSchema: changing PrimaryKey into UniqueContraint");
                            }
#endif
                            isPrimary = false;
                        }
                    }

                    if (null != columnIndexMap)
                    {
                        columnIndexMap[unsortedIndex] = dataColumn.Ordinal;
                    }
                    else if (unsortedIndex != dataColumn.Ordinal)
                    {
                        columnIndexMap = CreateIndexMap(schemaRows.Length, unsortedIndex);
                        columnIndexMap[unsortedIndex] = dataColumn.Ordinal;
                    }
                    mappingCount++;
                }

                bool addDataRelation = false;
                DataColumn chapterColumn = null;
                if (null != chapterValue)
                { // add the extra column in the child table
                    Type fieldType = chapterValue.GetType();
                    chapterColumn = _tableMapping.GetDataColumn(_tableMapping.SourceTable, fieldType, _dataTable, mappingAction, schemaAction);
                    if (null != chapterColumn)
                    {
                        if (null == chapterColumn.Table)
                        {
                            chapterColumn.ReadOnly = true;
                            chapterColumn.AllowDBNull = false;

                            AddItemToAllowRollback(ref addedItems, chapterColumn);
                            columnCollection.Add(chapterColumn);
                            addDataRelation = (null != parentChapterColumn);
                        }
                        mappingCount++;
                    }
                }

                if (0 < mappingCount)
                {
                    if ((null != _dataSet) && null == _dataTable.DataSet)
                    {
                        AddItemToAllowRollback(ref addedItems, _dataTable);
                        _dataSet.Tables.Add(_dataTable);
                    }
                    // setup the key
                    if (addPrimaryKeys && (null != keys))
                    {
                        if (keyCount < keys.Length)
                        {
                            keys = ResizeColumnArray(keys, keyCount);
                        }

                        if (isPrimary)
                        {
#if DEBUG
                            if (AdapterSwitches.DataSchema.TraceVerbose)
                            {
                                Debug.WriteLine("SetupSchema: set_PrimaryKey");
                            }
#endif
                            _dataTable.PrimaryKey = keys;
                        }
                        else
                        {
                            UniqueConstraint unique = new UniqueConstraint("", keys);
                            ConstraintCollection constraints = _dataTable.Constraints;
                            int constraintCount = constraints.Count;
                            for (int i = 0; i < constraintCount; ++i)
                            {
                                if (unique.Equals(constraints[i]))
                                {
#if DEBUG
                                    if (AdapterSwitches.DataSchema.TraceVerbose)
                                    {
                                        Debug.WriteLine("SetupSchema: duplicate Contraint detected");
                                    }
#endif
                                    unique = null;
                                    break;
                                }
                            }
                            if (null != unique)
                            {
#if DEBUG
                                if (AdapterSwitches.DataSchema.TraceVerbose)
                                {
                                    Debug.WriteLine("SetupSchema: adding new UniqueConstraint");
                                }
#endif
                                constraints.Add(unique);
                            }
                        }
                    }
                    if (!commonFromMultiTable && !string.IsNullOrEmpty(commonBaseTable) && string.IsNullOrEmpty(_dataTable.TableName))
                    {
                        _dataTable.TableName = commonBaseTable;
                    }
                    if (gettingData)
                    {
                        _indexMap = columnIndexMap;
                        _chapterMap = chapterIndexMap;
                        dataValues = SetupMapping(schemaRows.Length, columnCollection, chapterColumn, chapterValue);
                    }
                    else
                    {
                        // debug only, but for retail debug ability
                        _mappedMode = -1;
                    }
                }
                else
                {
                    _dataTable = null;
                }
                if (addDataRelation)
                {
                    AddRelation(parentChapterColumn, chapterColumn);
                }
            }
            catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
            {
                RollbackAddedItems(addedItems);
                throw;
            }
            return dataValues;
        }

        private void AddAdditionalProperties(DataColumn targetColumn, DataRow schemaRow)
        {
            DataColumnCollection columns = schemaRow.Table.Columns;
            DataColumn column;

            column = columns[SchemaTableOptionalColumn.DefaultValue];
            if (null != column)
            {
                targetColumn.DefaultValue = schemaRow[column];
            }

            column = columns[SchemaTableOptionalColumn.AutoIncrementSeed];
            if (null != column)
            {
                object value = schemaRow[column];
                if (DBNull.Value != value)
                {
                    targetColumn.AutoIncrementSeed = ((IConvertible)value).ToInt64(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.AutoIncrementStep];
            if (null != column)
            {
                object value = schemaRow[column];
                if (DBNull.Value != value)
                {
                    targetColumn.AutoIncrementStep = ((IConvertible)value).ToInt64(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.ColumnMapping];
            if (null != column)
            {
                object value = schemaRow[column];
                if (DBNull.Value != value)
                {
                    targetColumn.ColumnMapping = (MappingType)((IConvertible)value).ToInt32(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.BaseColumnNamespace];
            if (null != column)
            {
                object value = schemaRow[column];
                if (DBNull.Value != value)
                {
                    targetColumn.Namespace = ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.Expression];
            if (null != column)
            {
                object value = schemaRow[column];
                if (DBNull.Value != value)
                {
                    targetColumn.Expression = ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        private void AddRelation(DataColumn parentChapterColumn, DataColumn chapterColumn)
        {
            if (null != _dataSet)
            {
                string name = /*parentChapterColumn.ColumnName + "_" +*/ chapterColumn.ColumnName;

                DataRelation relation = new DataRelation(name, new DataColumn[] { parentChapterColumn }, new DataColumn[] { chapterColumn }, false);

                int index = 1;
                string tmp = name;
                DataRelationCollection relations = _dataSet.Relations;
                while (-1 != relations.IndexOf(tmp))
                {
                    tmp = name + index;
                    index++;
                }
                relation.RelationName = tmp;
                relations.Add(relation);
            }
        }

        private object[] SetupMapping(int count, DataColumnCollection columnCollection, DataColumn chapterColumn, object chapterValue)
        {
            object[] dataValues = new object[count];

            if (null == _indexMap)
            {
                int mappingCount = columnCollection.Count;
                bool hasChapters = (null != _chapterMap);
                if ((count != mappingCount) || hasChapters)
                {
                    _mappedDataValues = new object[mappingCount];
                    if (hasChapters)
                    {
                        _mappedMode = MapChapters;
                        _mappedLength = count;
                    }
                    else
                    {
                        _mappedMode = MapDifferentSize;
                        _mappedLength = Math.Min(count, mappingCount);
                    }
                }
                else
                {
                    _mappedMode = MapExactMatch; /* _mappedLength doesn't matter */
                }
            }
            else
            {
                _mappedDataValues = new object[columnCollection.Count];
                _mappedMode = ((null == _chapterMap) ? MapReorderedValues : MapChaptersReordered);
                _mappedLength = count;
            }
            if (null != chapterColumn)
            { // value from parent tracked into child table
                _mappedDataValues[chapterColumn.Ordinal] = chapterValue;
            }
            return dataValues;
        }
    }
}
