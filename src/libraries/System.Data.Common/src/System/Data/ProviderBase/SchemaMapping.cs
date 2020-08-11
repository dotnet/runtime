// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private readonly DataSet? _dataSet; // the current dataset, may be null if we are only filling a DataTable
        private DataTable? _dataTable; // the current DataTable, should never be null

        private readonly DataAdapter _adapter;
        private readonly DataReaderContainer _dataReader;
        private readonly DataTable? _schemaTable;  // will be null if Fill without schema
        private readonly DataTableMapping? _tableMapping;

        // unique (generated) names based from DataReader.GetName(i)
        private readonly string[]? _fieldNames;

        private readonly object?[]? _readerDataValues;
        private object?[]? _mappedDataValues; // array passed to dataRow.AddUpdate(), if needed

        private int[]? _indexMap;     // index map that maps dataValues -> _mappedDataValues, if needed
        private bool[]? _chapterMap;  // which DataReader indexes have chapters

        private int[]? _xmlMap; // map which value in _readerDataValues to convert to a Xml datatype, (SqlXml/XmlDocument)

        private int _mappedMode; // modes as described as above
        private int _mappedLength;

        private readonly LoadOption _loadOption;

        internal SchemaMapping(DataAdapter adapter, DataSet? dataset, DataTable? datatable, DataReaderContainer dataReader, bool keyInfo,
                                    SchemaType schemaType, string? sourceTableName, bool gettingData,
                                    DataColumn? parentChapterColumn, object? parentChapterValue)
        {
            Debug.Assert(adapter != null, nameof(adapter));
            Debug.Assert(dataReader != null, nameof(dataReader));
            Debug.Assert(dataReader.FieldCount > 0, "FieldCount");
            Debug.Assert(dataset != null || datatable != null, "SchemaMapping - null dataSet");
            Debug.Assert(schemaType == SchemaType.Mapped || schemaType == SchemaType.Source, "SetupSchema - invalid schemaType");

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
            if (schemaType == SchemaType.Mapped)
            {
                mappingAction = _adapter.MissingMappingAction;
                schemaAction = _adapter.MissingSchemaAction;
                if (!string.IsNullOrEmpty(sourceTableName))
                {
                    _tableMapping = _adapter.GetTableMappingBySchemaAction(sourceTableName, sourceTableName, mappingAction);
                }
                else if (_dataTable != null)
                {
                    int index = _adapter.IndexOfDataSetTable(_dataTable.TableName);
                    if (index != -1)
                    {
                        _tableMapping = _adapter.TableMappings[index];
                    }
                    else
                    {
                        _tableMapping = mappingAction switch
                        {
                            MissingMappingAction.Passthrough => new DataTableMapping(_dataTable.TableName, _dataTable.TableName),
                            MissingMappingAction.Ignore => null,
                            MissingMappingAction.Error => throw ADP.MissingTableMappingDestination(_dataTable.TableName),
                            _ => throw ADP.InvalidMissingMappingAction(mappingAction),
                        };
                    }
                }
            }
            else if (schemaType == SchemaType.Source)
            {
                mappingAction = System.Data.MissingMappingAction.Passthrough;
                schemaAction = Data.MissingSchemaAction.Add;
                if (!string.IsNullOrEmpty(sourceTableName))
                {
                    _tableMapping = DataTableMappingCollection.GetTableMappingBySchemaAction(null, sourceTableName, sourceTableName, mappingAction);
                }
                else if (_dataTable != null)
                {
                    int index = _adapter.IndexOfDataSetTable(_dataTable.TableName);
                    if (index != -1)
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

            if (_tableMapping != null)
            {
                if (_dataTable == null)
                {
                    _dataTable = _tableMapping.GetDataTableBySchemaAction(_dataSet!, schemaAction);
                }
                if (_dataTable != null)
                {
                    _fieldNames = GenerateFieldNames(dataReader);

                    if (_schemaTable == null)
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

        internal DataTable? DataTable
        {
            get
            {
                return _dataTable;
            }
        }

        internal object?[]? DataValues
        {
            get
            {
                return _readerDataValues;
            }
        }

        internal void ApplyToDataRow(DataRow dataRow)
        {
            DataColumnCollection columns = dataRow.Table.Columns;
            _dataReader.GetValues(_readerDataValues!);

            object?[] mapped = GetMappedValues();
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
                        if (columns[i].Expression.Length == 0)
                        {
                            columns[i].ReadOnly = false;
                        }
                    }

                    for (int i = 0; i < mapped.Length; ++i)
                    {
                        if (mapped[i] is object m)
                        {
                            dataRow[i] = m;
                        }
                    }
                }
                finally
                {
                    // ReadOnly
                    // reset readonly flag on all columns
                    for (int i = 0; i < readOnly.Length; ++i)
                    {
                        if (columns[i].Expression.Length == 0)
                        {
                            columns[i].ReadOnly = readOnly[i];
                        }
                    }
                }
            }
            finally
            { // FreeDataRowChapters
                if (_chapterMap != null)
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
                int k = _indexMap![i];
                if (k >= 0)
                {
                    _mappedDataValues![k] = _readerDataValues![i]; // from reader to dataset
                    if (_chapterMap![i])
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
                _mappedDataValues![i] = _readerDataValues![i]; // from reader to dataset
                if (_chapterMap![i])
                {
                    _mappedDataValues[i] = null; // InvalidCast from DataReader to AutoIncrement DataColumn
                }
            }
        }

        private void MappedIndex()
        { // mode 2
            Debug.Assert(_mappedLength == _indexMap!.Length, "incorrect precomputed length");

            int length = _mappedLength;
            for (int i = 0; i < length; i++)
            {
                int k = _indexMap[i];
                if (k >= 0)
                {
                    _mappedDataValues![k] = _readerDataValues![i]; // from reader to dataset
                }
            }
        }

        private void MappedValues()
        { // mode 1
            Debug.Assert(_mappedLength == Math.Min(_readerDataValues!.Length, _mappedDataValues!.Length), "incorrect precomputed length");

            int length = _mappedLength;
            for (int i = 0; i < length; ++i)
            {
                _mappedDataValues[i] = _readerDataValues[i]; // from reader to dataset
            };
        }

        private object?[] GetMappedValues()
        { // mode 0
            Debug.Assert(_readerDataValues != null);

            if (_xmlMap != null)
            {
                for (int i = 0; i < _xmlMap.Length; ++i)
                {
                    if (_xmlMap[i] != 0)
                    {
                        // get the string/SqlString xml value
                        string? xml = _readerDataValues[i] as string;
                        if ((xml == null) && (_readerDataValues[i] is System.Data.SqlTypes.SqlString x))
                        {
                            if (!x.IsNull)
                            {
                                xml = x.Value;
                            }
                            else
                            {
                                _readerDataValues[i] = _xmlMap[i] switch
                                {
                                    // map strongly typed SqlString.Null to SqlXml.Null
                                    SqlXml => System.Data.SqlTypes.SqlXml.Null,

                                    _ => DBNull.Value,
                                };
                            }
                        }
                        if (xml != null)
                        {
                            switch (_xmlMap[i])
                            {
                                case SqlXml: // turn string into a SqlXml value for DataColumn
                                    System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings();
                                    settings.ConformanceLevel = System.Xml.ConformanceLevel.Fragment;
                                    System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader(xml), settings, (string?)null);
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
                    Debug.Assert(_mappedMode == 0, "incorrect mappedMode");
                    Debug.Assert((_chapterMap == null) && (_indexMap == null) && (_mappedDataValues == null), "incorrect MappedValues");
                    return _readerDataValues;  // from reader to dataset
                case MapDifferentSize:
                    Debug.Assert((_chapterMap == null) && (_indexMap == null) && (_mappedDataValues != null), "incorrect MappedValues");
                    MappedValues();
                    break;
                case MapReorderedValues:
                    Debug.Assert((_chapterMap == null) && (_indexMap != null) && (_mappedDataValues != null), "incorrect MappedValues");
                    MappedIndex();
                    break;
                case MapChapters:
                    Debug.Assert((_chapterMap != null) && (_indexMap == null) && (_mappedDataValues != null), "incorrect MappedValues");
                    MappedChapter();
                    break;
                case MapChaptersReordered:
                    Debug.Assert((_chapterMap != null) && (_indexMap != null) && (_mappedDataValues != null), "incorrect MappedValues");
                    MappedChapterIndex();
                    break;
            }
            return _mappedDataValues!;
        }

        internal void LoadDataRowWithClear()
        {
            // for FillErrorEvent to ensure no values leftover from previous row
            for (int i = 0; i < _readerDataValues!.Length; ++i)
            {
                _readerDataValues[i] = null;
            }
            LoadDataRow();
        }

        internal void LoadDataRow()
        {
            try
            {
                _dataReader.GetValues(_readerDataValues!);
                object?[] mapped = GetMappedValues();

                DataRow dataRow;
                switch (_loadOption)
                {
                    case LoadOption.OverwriteChanges:
                    case LoadOption.PreserveChanges:
                    case LoadOption.Upsert:
                        dataRow = _dataTable!.LoadDataRow(mapped, _loadOption);
                        break;
                    case (LoadOption)4: // true
                        dataRow = _dataTable!.LoadDataRow(mapped, true);
                        break;
                    case (LoadOption)5: // false
                        dataRow = _dataTable!.LoadDataRow(mapped, false);
                        break;
                    default:
                        Debug.Fail("unexpected LoadOption");
                        throw ADP.InvalidLoadOption(_loadOption);
                }
                if ((_chapterMap != null) && (_dataSet != null))
                {
                    LoadDataRowChapters(dataRow);
                }
            }
            finally
            {
                if (_chapterMap != null)
                {
                    FreeDataRowChapters();
                }
            }
        }

        private void FreeDataRowChapters()
        {
            for (int i = 0; i < _chapterMap!.Length; ++i)
            {
                if (_chapterMap[i])
                {
                    IDisposable? disposable = (_readerDataValues![i] as IDisposable);
                    if (disposable != null)
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

            int rowLength = _chapterMap!.Length;
            for (int i = 0; i < rowLength; ++i)
            {
                if (_chapterMap[i])
                {
                    object? readerValue = _readerDataValues![i];
                    if ((readerValue != null) && !Convert.IsDBNull(readerValue))
                    {
                        _readerDataValues[i] = null;

                        using (IDataReader nestedReader = (IDataReader)readerValue)
                        {
                            if (!nestedReader.IsClosed)
                            {
                                Debug.Assert(_dataSet != null, "if chapters, then Fill(DataSet,...) not Fill(DataTable,...)");

                                object parentChapterValue;
                                DataColumn parentChapterColumn;
                                if (_indexMap == null)
                                {
                                    parentChapterColumn = _dataTable!.Columns[i];
                                    parentChapterValue = dataRow[parentChapterColumn];
                                }
                                else
                                {
                                    parentChapterColumn = _dataTable!.Columns[_indexMap[i]];
                                    parentChapterValue = dataRow[parentChapterColumn];
                                }

                                // correct on Fill, not FillFromReader
                                string chapterTableName = _tableMapping!.SourceTable + _fieldNames![i];

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
            Array.Copy(rgcol, tmp, len);
            return tmp;
        }

        private void AddItemToAllowRollback(ref List<object>? items, object value)
        {
            if (items == null)
            {
                items = new List<object>();
            }
            items.Add(value);
        }

        private void RollbackAddedItems(List<object>? items)
        {
            if (items != null)
            {
                for (int i = items.Count - 1; i >= 0; --i)
                {
                    // remove columns that were added now that we are failing
                    if (items[i] != null)
                    {
                        DataColumn? column = (items[i] as DataColumn);
                        if (column != null)
                        {
                            if (column.Table != null)
                            {
                                column.Table.Columns.Remove(column);
                            }
                        }
                        else
                        {
                            DataTable? table = (items[i] as DataTable);
                            if (table != null)
                            {
                                if (table.DataSet != null)
                                {
                                    table.DataSet.Tables.Remove(table);
                                }
                            }
                        }
                    }
                }
            }
        }

        private object[]? SetupSchemaWithoutKeyInfo(MissingMappingAction mappingAction, MissingSchemaAction schemaAction, bool gettingData, DataColumn? parentChapterColumn, object? chapterValue)
        {
            Debug.Assert(_dataTable != null);
            Debug.Assert(_fieldNames != null);
            Debug.Assert(_tableMapping != null);

            int[]? columnIndexMap = null;
            bool[]? chapterIndexMap = null;

            int mappingCount = 0;
            int count = _dataReader.FieldCount;

            object[]? dataValues = null;
            List<object>? addedItems = null;
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

                    if (fieldType == null)
                    {
                        throw ADP.MissingDataReaderFieldType(i);
                    }

                    // if IDataReader, hierarchy exists and we will use an Int32,AutoIncrementColumn in this table
                    if (typeof(IDataReader).IsAssignableFrom(fieldType))
                    {
                        if (chapterIndexMap == null)
                        {
                            chapterIndexMap = new bool[count];
                        }
                        chapterIndexMap[i] = ischapter = true;
                        fieldType = typeof(int);
                    }
                    else if (typeof(System.Data.SqlTypes.SqlXml).IsAssignableFrom(fieldType))
                    {
                        if (_xmlMap == null)
                        { // map to DataColumn with DataType=typeof(SqlXml)
                            _xmlMap = new int[count];
                        }
                        _xmlMap[i] = SqlXml; // track its xml data
                    }
                    else if (typeof(System.Xml.XmlReader).IsAssignableFrom(fieldType))
                    {
                        fieldType = typeof(string); // map to DataColumn with DataType=typeof(string)
                        if (_xmlMap == null)
                        {
                            _xmlMap = new int[count];
                        }
                        _xmlMap[i] = XmlDocument; // track its xml data
                    }

                    DataColumn? dataColumn;
                    if (alwaysCreateColumns)
                    {
                        dataColumn = DataColumnMapping.CreateDataColumnBySchemaAction(_fieldNames[i], _fieldNames[i], _dataTable, fieldType, schemaAction);
                    }
                    else
                    {
                        dataColumn = _tableMapping.GetDataColumn(_fieldNames[i], fieldType, _dataTable, mappingAction, schemaAction);
                    }

                    if (dataColumn == null)
                    {
                        if (columnIndexMap == null)
                        {
                            columnIndexMap = CreateIndexMap(count, i);
                        }
                        columnIndexMap[i] = -1;
                        continue; // null means ignore (mapped to nothing)
                    }
                    else if ((_xmlMap != null) && (_xmlMap[i] != 0))
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
                            if (total == 0)
                            { // not mapping to a specific Xml datatype, get rid of the map
                                _xmlMap = null;
                            }
                        }
                    }

                    if (dataColumn.Table == null)
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


                    if (columnIndexMap != null)
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
                DataColumn? chapterColumn = null;
                if (chapterValue != null)
                { // add the extra column in the child table
                    Type fieldType = chapterValue.GetType();

                    chapterColumn = _tableMapping.GetDataColumn(_tableMapping.SourceTable, fieldType, _dataTable, mappingAction, schemaAction);
                    if (chapterColumn != null)
                    {
                        if (chapterColumn.Table == null)
                        {
                            AddItemToAllowRollback(ref addedItems, chapterColumn);
                            columnCollection.Add(chapterColumn);
                            addDataRelation = (parentChapterColumn != null);
                        }
                        mappingCount++;
                    }
                }

                if (mappingCount > 0)
                {
                    if ((_dataSet != null) && (_dataTable.DataSet == null))
                    {
                        // Allowed to throw exception if DataTable is from wrong DataSet
                        AddItemToAllowRollback(ref addedItems, _dataTable);
                        _dataSet.Tables.Add(_dataTable);
                    }
                    if (gettingData)
                    {
                        if (columnCollection == null)
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
                    AddRelation(parentChapterColumn!, chapterColumn!);
                }
            }
            catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
            {
                RollbackAddedItems(addedItems);
                throw;
            }
            return dataValues;
        }

        private object[]? SetupSchemaWithKeyInfo(MissingMappingAction mappingAction, MissingSchemaAction schemaAction, bool gettingData, DataColumn? parentChapterColumn, object? chapterValue)
        {
            Debug.Assert(_dataTable != null);
            Debug.Assert(_schemaTable != null);
            Debug.Assert(_fieldNames != null);
            Debug.Assert(_tableMapping != null);

            // must sort rows from schema table by ordinal because Jet is sorted by coumn name
            DbSchemaRow[] schemaRows = DbSchemaRow.GetSortedSchemaRows(_schemaTable, _dataReader.ReturnProviderSpecificTypes);
            Debug.Assert(schemaRows != null, "SchemaSetup - null DbSchemaRow[]");
            Debug.Assert(_dataReader.FieldCount <= schemaRows.Length, "unexpected fewer rows in Schema than FieldCount");

            if (schemaRows.Length == 0)
            {
                _dataTable = null;
                return null;
            }

            // Everett behavior, always add a primary key if a primary key didn't exist before
            // Whidbey behavior, same as Everett unless using LoadOption then add primary key only if no columns previously existed
            bool addPrimaryKeys = (((_dataTable.PrimaryKey.Length == 0) && (((int)_loadOption >= 4) || (_dataTable.Rows.Count == 0)))
                                    || (_dataTable.Columns.Count == 0));

            DataColumn[]? keys = null;
            int keyCount = 0;
            bool isPrimary = true; // assume key info (if any) is about a primary key

            string? keyBaseTable = null;
            string? commonBaseTable = null;

            bool keyFromMultiTable = false;
            bool commonFromMultiTable = false;

            int[]? columnIndexMap = null;
            bool[]? chapterIndexMap = null;

            int mappingCount = 0;

            object[]? dataValues = null;
            List<object>? addedItems = null;
            DataColumnCollection columnCollection = _dataTable.Columns;
            try
            {
                for (int sortedIndex = 0; sortedIndex < schemaRows.Length; ++sortedIndex)
                {
                    DbSchemaRow schemaRow = schemaRows[sortedIndex];

                    int unsortedIndex = schemaRow.UnsortedIndex;

                    bool ischapter = false;
                    Type? fieldType = schemaRow.DataType;
                    if (fieldType == null)
                    {
                        fieldType = _dataReader.GetFieldType(sortedIndex);
                    }
                    if (fieldType == null)
                    {
                        throw ADP.MissingDataReaderFieldType(sortedIndex);
                    }

                    // if IDataReader, hierarchy exists and we will use an Int32,AutoIncrementColumn in this table
                    if (typeof(IDataReader).IsAssignableFrom(fieldType))
                    {
                        if (chapterIndexMap == null)
                        {
                            chapterIndexMap = new bool[schemaRows.Length];
                        }
                        chapterIndexMap[unsortedIndex] = ischapter = true;
                        fieldType = typeof(int);
                    }
                    else if (typeof(System.Data.SqlTypes.SqlXml).IsAssignableFrom(fieldType))
                    {
                        if (_xmlMap == null)
                        {
                            _xmlMap = new int[schemaRows.Length];
                        }
                        _xmlMap[sortedIndex] = SqlXml;
                    }
                    else if (typeof(System.Xml.XmlReader).IsAssignableFrom(fieldType))
                    {
                        fieldType = typeof(string);
                        if (_xmlMap == null)
                        {
                            _xmlMap = new int[schemaRows.Length];
                        }
                        _xmlMap[sortedIndex] = XmlDocument;
                    }

                    DataColumn? dataColumn = null;
                    if (!schemaRow.IsHidden)
                    {
                        dataColumn = _tableMapping.GetDataColumn(_fieldNames[sortedIndex], fieldType, _dataTable, mappingAction, schemaAction);
                    }

                    string basetable = /*schemaRow.BaseServerName+schemaRow.BaseCatalogName+schemaRow.BaseSchemaName+*/ schemaRow.BaseTableName;
                    if (dataColumn == null)
                    {
                        if (columnIndexMap == null)
                        {
                            columnIndexMap = CreateIndexMap(schemaRows.Length, unsortedIndex);
                        }
                        columnIndexMap[unsortedIndex] = -1;

                        // if the column is not mapped and it is a key, then don't add any key information
                        if (schemaRow.IsKey)
                        {
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
                    else if ((_xmlMap != null) && (_xmlMap[sortedIndex] != 0))
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
                            if (total == 0)
                            { // not mapping to a specific Xml datatype, get rid of the map
                                _xmlMap = null;
                            }
                        }
                    }

                    if (schemaRow.IsKey)
                    {
                        if (basetable != keyBaseTable)
                        {
                            if (keyBaseTable == null)
                            {
                                keyBaseTable = basetable;
                            }
                            else keyFromMultiTable = true;
                        }
                    }

                    if (ischapter)
                    {
                        if (dataColumn.Table == null)
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
                                if (commonBaseTable == null)
                                {
                                    commonBaseTable = basetable;
                                }
                                else
                                {
                                    commonFromMultiTable = true;
                                }
                            }
                        }
                        if ((int)_loadOption >= 4)
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
                        else if (dataColumn.Table == null)
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
                    if (dataColumn.Table == null)
                    {
                        if ((int)_loadOption < 4)
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

                        // see case 3 above, we do want dataColumn.AllowDBNull not schemaRow.AllowDBNull
                        // otherwise adding PrimaryKey will change AllowDBNull to false
                        if (isPrimary && dataColumn.AllowDBNull)
                        {
                            isPrimary = false;
                        }
                    }

                    if (columnIndexMap != null)
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
                DataColumn? chapterColumn = null;
                if (chapterValue != null)
                { // add the extra column in the child table
                    Type fieldType = chapterValue.GetType();
                    chapterColumn = _tableMapping.GetDataColumn(_tableMapping.SourceTable, fieldType, _dataTable, mappingAction, schemaAction);
                    if (chapterColumn != null)
                    {
                        if (chapterColumn.Table == null)
                        {
                            chapterColumn.ReadOnly = true;
                            chapterColumn.AllowDBNull = false;

                            AddItemToAllowRollback(ref addedItems, chapterColumn);
                            columnCollection.Add(chapterColumn);
                            addDataRelation = (parentChapterColumn != null);
                        }
                        mappingCount++;
                    }
                }

                if (mappingCount > 0)
                {
                    if ((_dataSet != null) && _dataTable.DataSet == null)
                    {
                        AddItemToAllowRollback(ref addedItems, _dataTable);
                        _dataSet.Tables.Add(_dataTable);
                    }
                    // setup the key
                    if (addPrimaryKeys && (keys != null))
                    {
                        if (keyCount < keys.Length)
                        {
                            keys = ResizeColumnArray(keys, keyCount);
                        }

                        if (isPrimary)
                        {
                            _dataTable.PrimaryKey = keys;
                        }
                        else
                        {
                            UniqueConstraint? unique = new UniqueConstraint("", keys);
                            ConstraintCollection constraints = _dataTable.Constraints;
                            int constraintCount = constraints.Count;
                            for (int i = 0; i < constraintCount; ++i)
                            {
                                if (unique.Equals(constraints[i]))
                                {
                                    unique = null;
                                    break;
                                }
                            }
                            if (unique != null)
                            {
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
                    AddRelation(parentChapterColumn!, chapterColumn!);
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
            DataColumn? column;

            column = columns[SchemaTableOptionalColumn.DefaultValue];
            if (column != null)
            {
                targetColumn.DefaultValue = schemaRow[column];
            }

            column = columns[SchemaTableOptionalColumn.AutoIncrementSeed];
            if (column != null)
            {
                object value = schemaRow[column];
                if (value != DBNull.Value)
                {
                    targetColumn.AutoIncrementSeed = ((IConvertible)value).ToInt64(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.AutoIncrementStep];
            if (column != null)
            {
                object value = schemaRow[column];
                if (value != DBNull.Value)
                {
                    targetColumn.AutoIncrementStep = ((IConvertible)value).ToInt64(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.ColumnMapping];
            if (column != null)
            {
                object value = schemaRow[column];
                if (value != DBNull.Value)
                {
                    targetColumn.ColumnMapping = (MappingType)((IConvertible)value).ToInt32(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.BaseColumnNamespace];
            if (column != null)
            {
                object value = schemaRow[column];
                if (value != DBNull.Value)
                {
                    targetColumn.Namespace = ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
                }
            }

            column = columns[SchemaTableOptionalColumn.Expression];
            if (column != null)
            {
                object value = schemaRow[column];
                if (value != DBNull.Value)
                {
                    targetColumn.Expression = ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        private void AddRelation(DataColumn parentChapterColumn, DataColumn chapterColumn)
        {
            if (_dataSet != null)
            {
                string name = /*parentChapterColumn.ColumnName + "_" +*/ chapterColumn.ColumnName;

                DataRelation relation = new DataRelation(name, new DataColumn[] { parentChapterColumn }, new DataColumn[] { chapterColumn }, false);

                int index = 1;
                string tmp = name;
                DataRelationCollection relations = _dataSet.Relations;
                while (relations.IndexOf(tmp) != -1)
                {
                    tmp = name + index;
                    index++;
                }
                relation.RelationName = tmp;
                relations.Add(relation);
            }
        }

        private object[] SetupMapping(int count, DataColumnCollection columnCollection, DataColumn? chapterColumn, object? chapterValue)
        {
            object[] dataValues = new object[count];

            if (_indexMap == null)
            {
                int mappingCount = columnCollection.Count;
                bool hasChapters = (_chapterMap != null);
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
                _mappedMode = ((_chapterMap == null) ? MapReorderedValues : MapChaptersReordered);
                _mappedLength = count;
            }
            if (chapterColumn != null)
            { // value from parent tracked into child table
                _mappedDataValues![chapterColumn.Ordinal] = chapterValue;
            }
            return dataValues;
        }
    }
}
