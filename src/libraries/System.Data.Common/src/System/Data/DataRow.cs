// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data
{
    /// <summary>
    /// Represents a row of data in a <see cref='System.Data.DataTable'/>.
    /// </summary>
    public class DataRow
    {
        private readonly DataTable _table;
        private readonly DataColumnCollection _columns;

        internal int _oldRecord = -1;
        internal int _newRecord = -1;
        internal int _tempRecord;
        internal long _rowID = -1;

        internal DataRowAction _action;

        internal bool _inChangingEvent;
        internal bool _inDeletingEvent;
        internal bool _inCascade;

        private DataColumn? _lastChangedColumn; // last successfully changed column
        private int _countColumnChange;        // number of columns changed during edit mode

        private DataError? _error;
        private object? _element;

        private int _rbTreeNodeId; // if row is not detached, Id used for computing index in rows collection

        private static int s_objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        /// <summary>
        /// Initializes a new instance of the DataRow.
        /// Constructs a row from the builder. Only for internal usage..
        /// </summary>
        protected internal DataRow(DataRowBuilder builder)
        {
            _tempRecord = builder._record;
            _table = builder._table;
            _columns = _table.Columns;
        }

        internal XmlBoundElement? Element
        {
            get { return (XmlBoundElement?)_element; }
            set { _element = value; }
        }

        internal DataColumn? LastChangedColumn
        {
            get
            {
                // last successfully changed column or if multiple columns changed: null
                return _countColumnChange != 1 ? null : _lastChangedColumn;
            }
            set
            {
                _countColumnChange++;
                _lastChangedColumn = value;
            }
        }

        internal bool HasPropertyChanged => 0 < _countColumnChange;

        internal int RBTreeNodeId
        {
            get { return _rbTreeNodeId; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataRow.set_RBTreeNodeId|INFO> {0}, value={1}", _objectID, value);
                _rbTreeNodeId = value;
            }
        }

        /// <summary>
        /// Gets or sets the custom error description for a row.
        /// </summary>
        [AllowNull]
        public string RowError
        {
            get { return _error == null ? string.Empty : _error.Text; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataRow.set_RowError|API> {0}, value='{1}'", _objectID, value);
                if (_error == null)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        _error = new DataError(value);
                    }
                    RowErrorChanged();
                }
                else if (_error.Text != value)
                {
                    _error.Text = value;
                    RowErrorChanged();
                }
            }
        }

        private void RowErrorChanged()
        {
            // We don't know wich record was used by view index. try to use both.
            if (_oldRecord != -1)
            {
                _table.RecordChanged(_oldRecord);
            }
            if (_newRecord != -1)
            {
                _table.RecordChanged(_newRecord);
            }
        }

        internal long rowID
        {
            get { return _rowID; }
            set
            {
                ResetLastChangedColumn();
                _rowID = value;
            }
        }

        /// <summary>
        /// Gets the current state of the row in regards to its relationship to the table.
        /// </summary>
        public DataRowState RowState
        {
            get
            {
                if (_oldRecord == _newRecord)
                {
                    if (_oldRecord == -1)
                    {
                        return DataRowState.Detached; // 2
                    }
                    if (0 < _columns.ColumnsImplementingIChangeTrackingCount)
                    {
                        foreach (DataColumn dc in _columns.ColumnsImplementingIChangeTracking)
                        {
                            object value = this[dc];
                            if ((DBNull.Value != value) && ((IChangeTracking)value).IsChanged)
                            {
                                return DataRowState.Modified; // 3 + _columns.columnsImplementingIChangeTracking.Count
                            }
                        }
                    }
                    return DataRowState.Unchanged; // 3
                }
                else if (_oldRecord == -1)
                {
                    return DataRowState.Added; // 2
                }
                else if (_newRecord == -1)
                {
                    return DataRowState.Deleted; // 3
                }
                return DataRowState.Modified; // 3
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Data.DataTable'/>
        /// for which this row has a schema.
        /// </summary>
        public DataTable Table => _table;

        /// <summary>
        /// Gets or sets the data stored in the column specified by index.
        /// </summary>
        [AllowNull]
        public object this[int columnIndex]
        {
            get
            {
                DataColumn column = _columns[columnIndex];
                int record = GetDefaultRecord();
                _table._recordManager.VerifyRecord(record, this);
                VerifyValueFromStorage(column, DataRowVersion.Default, column[record]);
                return column[record];
            }
            set
            {
                DataColumn column = _columns[columnIndex];
                this[column] = value;
            }
        }

        internal void CheckForLoops(DataRelation rel)
        {
            // don't check for loops in the diffgram
            // because there may be some holes in the rowCollection
            // and index creation may fail. The check will be done
            // after all the loading is done _and_ we are sure there
            // are no holes in the collection.
            if (_table._fInLoadDiffgram || (_table.DataSet != null && _table.DataSet._fInLoadDiffgram))
            {
                return;
            }

            int count = _table.Rows.Count, i = 0;
            // need to optimize this for count > 100
            DataRow? parent = GetParentRow(rel);
            while (parent != null)
            {
                if ((parent == this) || (i > count))
                {
                    throw ExceptionBuilder.NestedCircular(_table.TableName);
                }
                i++;
                parent = parent.GetParentRow(rel);
            }
        }

        internal int GetNestedParentCount()
        {
            int count = 0;
            DataRelation[] nestedParentRelations = _table.NestedParentRelations;
            foreach (DataRelation rel in nestedParentRelations)
            {
                if (rel == null) // don't like this but done for backward code compatability
                {
                    continue;
                }
                if (rel.ParentTable == _table) // self-nested table
                {
                    CheckForLoops(rel);
                }
                DataRow? row = GetParentRow(rel);
                if (row != null)
                {
                    count++;
                }
            }
            return count;
            // Rule 1: At all times, only ONE FK  "(in a row) can be non-Null
            // we won't allow a row to have multiple parents, as we cant handle it , also in diffgram
        }

        /// <summary>
        /// Gets or sets the data stored in the column specified by name.
        /// </summary>
        [AllowNull]
        public object this[string columnName]
        {
            get
            {
                DataColumn column = GetDataColumn(columnName);
                int record = GetDefaultRecord();
                _table._recordManager.VerifyRecord(record, this);
                VerifyValueFromStorage(column, DataRowVersion.Default, column[record]);
                return column[record];
            }
            set
            {
                DataColumn column = GetDataColumn(columnName);
                this[column] = value;
            }
        }

        /// <summary>
        /// Gets or sets the data stored in the specified <see cref='System.Data.DataColumn'/>.
        /// </summary>
        [AllowNull]
        public object this[DataColumn column]
        {
            get
            {
                CheckColumn(column);
                int record = GetDefaultRecord();
                _table._recordManager.VerifyRecord(record, this);
                VerifyValueFromStorage(column, DataRowVersion.Default, column[record]);
                return column[record];
            }
            set
            {
                CheckColumn(column);
                if (_inChangingEvent)
                {
                    throw ExceptionBuilder.EditInRowChanging();
                }
                if ((-1 != rowID) && column.ReadOnly)
                {
                    throw ExceptionBuilder.ReadOnly(column.ColumnName);
                }

                // allow users to tailor the proposed value, or throw an exception.
                // note we intentionally do not try/catch this event.
                // note: we also allow user to do anything at this point
                // infinite loops are possible if user calls Item or ItemArray during the event
                DataColumnChangeEventArgs? e = null;
                if (_table.NeedColumnChangeEvents)
                {
                    e = new DataColumnChangeEventArgs(this, column, value);
                    _table.OnColumnChanging(e);
                }

                if (column.Table != _table)
                {
                    // user removed column from table during OnColumnChanging event
                    throw ExceptionBuilder.ColumnNotInTheTable(column.ColumnName, _table.TableName);
                }
                if ((-1 != rowID) && column.ReadOnly)
                {
                    // user adds row to table during OnColumnChanging event
                    throw ExceptionBuilder.ReadOnly(column.ColumnName);
                }

                object? proposed = ((null != e) ? e.ProposedValue : value);
                if (null == proposed)
                {
                    if (column.IsValueType)
                    {
                        throw ExceptionBuilder.CannotSetToNull(column);
                    }
                    proposed = DBNull.Value;
                }

                bool immediate = BeginEditInternal();
                try
                {
                    int record = GetProposedRecordNo();
                    _table._recordManager.VerifyRecord(record, this);
                    column[record] = proposed;
                }
                catch (Exception e1) when (Common.ADP.IsCatchableOrSecurityExceptionType(e1))
                {
                    if (immediate)
                    {
                        Debug.Assert(!_inChangingEvent, "how are we in a changing event to cancel?");
                        Debug.Assert(-1 != _tempRecord, "how no propsed record to cancel?");
                        CancelEdit();
                    }
                    throw;
                }
                LastChangedColumn = column;

                // note: we intentionally do not try/catch this event.
                // infinite loops are possible if user calls Item or ItemArray during the event
                if (null != e)
                {
                    _table.OnColumnChanged(e); // user may call CancelEdit or EndEdit
                }

                if (immediate)
                {
                    Debug.Assert(!_inChangingEvent, "how are we in a changing event to end?");
                    EndEdit();
                }
            }
        }

        /// <summary>
        /// Gets the data stored in the column, specified by index and version of the data to retrieve.
        /// </summary>
        [AllowNull]
        public object this[int columnIndex, DataRowVersion version]
        {
            get
            {
                DataColumn column = _columns[columnIndex];
                int record = GetRecordFromVersion(version);
                _table._recordManager.VerifyRecord(record, this);
                VerifyValueFromStorage(column, version, column[record]);
                return column[record];
            }
        }

        /// <summary>
        ///  Gets the specified version of data stored in the named column.
        /// </summary>
        [AllowNull]
        public object this[string columnName, DataRowVersion version]
        {
            get
            {
                DataColumn column = GetDataColumn(columnName);
                int record = GetRecordFromVersion(version);
                _table._recordManager.VerifyRecord(record, this);
                VerifyValueFromStorage(column, version, column[record]);
                return column[record];
            }
        }

        /// <summary>
        /// Gets the specified version of data stored in the specified <see cref='System.Data.DataColumn'/>.
        /// </summary>
        [AllowNull]
        public object this[DataColumn column, DataRowVersion version]
        {
            get
            {
                CheckColumn(column);
                int record = GetRecordFromVersion(version);
                _table._recordManager.VerifyRecord(record, this);
                VerifyValueFromStorage(column, version, column[record]);
                return column[record];
            }
        }

        /// <summary>
        /// Gets or sets all of the values for this row through an array.
        /// </summary>
        /// <remarks>
        /// Arrays returned from this property will never contain <see langword="null" />.
        /// When setting this property, <see langword="null" /> indicates that the current column value
        /// should be left as-is. <see cref="DBNull.Value" /> can be used to set the the value to null.
        /// </remarks>
        public object?[] ItemArray
        {
            get
            {
                int record = GetDefaultRecord();
                _table._recordManager.VerifyRecord(record, this);
                object[] values = new object[_columns.Count];
                for (int i = 0; i < values.Length; i++)
                {
                    DataColumn column = _columns[i];
                    VerifyValueFromStorage(column, DataRowVersion.Default, column[record]);
                    values[i] = column[record];
                }
                return values;
            }
            set
            {
                if (null == value)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(ItemArray));
                }
                if (_columns.Count < value.Length)
                {
                    throw ExceptionBuilder.ValueArrayLength();
                }
                DataColumnChangeEventArgs? e = null;
                if (_table.NeedColumnChangeEvents)
                {
                    e = new DataColumnChangeEventArgs(this);
                }
                bool immediate = BeginEditInternal();

                for (int i = 0; i < value.Length; ++i)
                {
                    // Empty means don't change the row.
                    if (value[i] is object item)
                    {
                        // may throw exception if user removes column from table during event
                        DataColumn column = _columns[i];

                        if ((-1 != rowID) && column.ReadOnly)
                        {
                            throw ExceptionBuilder.ReadOnly(column.ColumnName);
                        }

                        // allow users to tailor the proposed value, or throw an exception.
                        // note: we intentionally do not try/catch this event.
                        // note: we also allow user to do anything at this point
                        // infinite loops are possible if user calls Item or ItemArray during the event
                        if (null != e)
                        {
                            e.InitializeColumnChangeEvent(column, item);
                            _table.OnColumnChanging(e);
                        }

                        if (column.Table != _table)
                        {
                            // user removed column from table during OnColumnChanging event
                            throw ExceptionBuilder.ColumnNotInTheTable(column.ColumnName, _table.TableName);
                        }
                        if ((-1 != rowID) && column.ReadOnly)
                        {
                            // user adds row to table during OnColumnChanging event
                            throw ExceptionBuilder.ReadOnly(column.ColumnName);
                        }
                        if (_tempRecord == -1)
                        {
                            // user affected CancelEdit or EndEdit during OnColumnChanging event of the last value
                            BeginEditInternal();
                        }

                        object? proposed = (null != e) ? e.ProposedValue : item;
                        if (null == proposed)
                        {
                            if (column.IsValueType)
                            {
                                throw ExceptionBuilder.CannotSetToNull(column);
                            }
                            proposed = DBNull.Value;
                        }

                        try
                        {
                            // must get proposed record after each event because user may have
                            // called EndEdit(), AcceptChanges(), BeginEdit() during the event
                            int record = GetProposedRecordNo();
                            _table._recordManager.VerifyRecord(record, this);
                            column[record] = proposed;
                        }
                        catch (Exception e1) when (Common.ADP.IsCatchableOrSecurityExceptionType(e1))
                        {
                            if (immediate)
                            {
                                Debug.Assert(!_inChangingEvent, "how are we in a changing event to cancel?");
                                Debug.Assert(-1 != _tempRecord, "how no propsed record to cancel?");
                                CancelEdit();
                            }
                            throw;
                        }
                        LastChangedColumn = column;

                        // note: we intentionally do not try/catch this event.
                        // infinite loops are possible if user calls Item or ItemArray during the event
                        if (null != e)
                        {
                            _table.OnColumnChanged(e);  // user may call CancelEdit or EndEdit
                        }
                    }
                }

                // proposed breaking change: if (immediate){ EndEdit(); } because table currently always fires RowChangedEvent
                Debug.Assert(!_inChangingEvent, "how are we in a changing event to end?");
                EndEdit();
            }
        }

        /// <summary>
        /// Commits all the changes made to this row since the last time <see cref='System.Data.DataRow.AcceptChanges'/> was called.
        /// </summary>
        public void AcceptChanges()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRow.AcceptChanges|API> {0}", _objectID);
            try
            {
                EndEdit();

                if (RowState != DataRowState.Detached && RowState != DataRowState.Deleted)
                {
                    if (_columns.ColumnsImplementingIChangeTrackingCount > 0)
                    {
                        foreach (DataColumn dc in _columns.ColumnsImplementingIChangeTracking)
                        {
                            object value = this[dc];
                            if (DBNull.Value != value)
                            {
                                IChangeTracking tracking = (IChangeTracking)value;
                                if (tracking.IsChanged)
                                {
                                    tracking.AcceptChanges();
                                }
                            }
                        }
                    }
                }
                _table.CommitRow(this);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Begins an edit operation on a <see cref='System.Data.DataRow'/>object.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void BeginEdit() => BeginEditInternal();

        private bool BeginEditInternal()
        {
            if (_inChangingEvent)
            {
                throw ExceptionBuilder.BeginEditInRowChanging();
            }
            if (_tempRecord != -1)
            {
                if (_tempRecord < _table._recordManager.LastFreeRecord)
                {
                    return false; // we will not call EndEdit
                }
                else
                {
                    // partial fix for detached row after Table.Clear scenario
                    // in debug, it will have asserted earlier, but with this
                    // it will go get a new record for editing
                    _tempRecord = -1;
                }
                // shifted VerifyRecord to first make the correction, then verify
                _table._recordManager.VerifyRecord(_tempRecord, this);
            }

            if (_oldRecord != -1 && _newRecord == -1)
            {
                throw ExceptionBuilder.DeletedRowInaccessible();
            }

            ResetLastChangedColumn(); // shouldn't have to do this

            _tempRecord = _table.NewRecord(_newRecord);
            Debug.Assert(-1 != _tempRecord, "missing temp record");
            Debug.Assert(0 == _countColumnChange, "unexpected column change count");
            Debug.Assert(null == _lastChangedColumn, "unexpected last column change");
            return true;
        }

        /// <summary>
        /// Cancels the current edit on the row.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void CancelEdit()
        {
            if (_inChangingEvent)
            {
                throw ExceptionBuilder.CancelEditInRowChanging();
            }

            _table.FreeRecord(ref _tempRecord);
            Debug.Assert(-1 == _tempRecord, "unexpected temp record");
            ResetLastChangedColumn();
        }

        private void CheckColumn(DataColumn column)
        {
            if (column == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(column));
            }

            if (column.Table != _table)
            {
                throw ExceptionBuilder.ColumnNotInTheTable(column.ColumnName, _table.TableName);
            }
        }

        /// <summary>
        /// Throws a RowNotInTableException if row isn't in table.
        /// </summary>
        internal void CheckInTable()
        {
            if (rowID == -1)
            {
                throw ExceptionBuilder.RowNotInTheTable();
            }
        }

        /// <summary>
        /// Deletes the row.
        /// </summary>
        public void Delete()
        {
            if (_inDeletingEvent)
            {
                throw ExceptionBuilder.DeleteInRowDeleting();
            }

            if (_newRecord == -1)
            {
                return;
            }

            _table.DeleteRow(this);
        }

        /// <summary>
        /// Ends the edit occurring on the row.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void EndEdit()
        {
            if (_inChangingEvent)
            {
                throw ExceptionBuilder.EndEditInRowChanging();
            }

            if (_newRecord == -1)
            {
                return; // this is meaningless, detatched row case
            }

            if (_tempRecord != -1)
            {
                try
                {
                    // suppressing the ensure property changed because it's possible that no values have been modified
                    _table.SetNewRecord(this, _tempRecord, suppressEnsurePropertyChanged: true);
                }
                finally
                {
                    // a constraint violation may be thrown during SetNewRecord
                    ResetLastChangedColumn();
                }
            }
        }

        /// <summary>
        /// Sets the error description for a column specified by index.
        /// </summary>
        public void SetColumnError(int columnIndex, string? error)
        {
            DataColumn column = _columns[columnIndex];
            if (column == null)
            {
                throw ExceptionBuilder.ColumnOutOfRange(columnIndex);
            }
            SetColumnError(column, error);
        }

        /// <summary>
        /// Sets the error description for a column specified by name.
        /// </summary>
        public void SetColumnError(string columnName, string? error)
        {
            DataColumn column = GetDataColumn(columnName);
            SetColumnError(column, error);
        }

        /// <summary>
        /// Sets the error description for a column specified as a <see cref='System.Data.DataColumn'/>.
        /// </summary>
        public void SetColumnError(DataColumn column, string? error)
        {
            CheckColumn(column);

            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRow.SetColumnError|API> {0}, column={1}, error='{2}'", _objectID, column.ObjectID, error);
            try
            {
                if (_error == null) _error = new DataError();
                if (GetColumnError(column) != error)
                {
                    _error.SetColumnError(column, error);
                    RowErrorChanged();
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Gets the error description for the column specified by index.
        /// </summary>
        public string GetColumnError(int columnIndex) => GetColumnError(_columns[columnIndex]);

        /// <summary>
        /// Gets the error description for a column, specified by name.
        /// </summary>
        public string GetColumnError(string columnName) => GetColumnError(GetDataColumn(columnName));

        /// <summary>
        /// Gets the error description of the specified <see cref='System.Data.DataColumn'/>.
        /// </summary>
        public string GetColumnError(DataColumn column)
        {
            CheckColumn(column);
            if (_error == null) _error = new DataError();
            return _error.GetColumnError(column);
        }

        /// <summary>
        /// Clears the errors for the row, including the <see cref='System.Data.DataRow.RowError'/>
        /// and errors set with <see cref='System.Data.DataRow.SetColumnError(DataColumn, string)'/>
        /// </summary>
        public void ClearErrors()
        {
            if (_error != null)
            {
                _error.Clear();
                RowErrorChanged();
            }
        }

        internal void ClearError(DataColumn column)
        {
            if (_error != null)
            {
                _error.Clear(column);
                RowErrorChanged();
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are errors in a columns collection.
        /// </summary>
        public bool HasErrors => _error == null ? false : _error.HasErrors;

        /// <summary>
        /// Gets an array of columns that have errors.
        /// </summary>
        public DataColumn[] GetColumnsInError() => _error == null ?
            Array.Empty<DataColumn>() : _error.GetColumnsInError();

        public DataRow[] GetChildRows(string? relationName) =>
            GetChildRows(_table.ChildRelations[relationName], DataRowVersion.Default);

        public DataRow[] GetChildRows(string? relationName, DataRowVersion version) =>
            GetChildRows(_table.ChildRelations[relationName], version);

        /// <summary>
        /// Gets the child rows of this <see cref='System.Data.DataRow'/> using the
        /// specified <see cref='System.Data.DataRelation'/>.
        /// </summary>
        public DataRow[] GetChildRows(DataRelation? relation) =>
            GetChildRows(relation, DataRowVersion.Default);

        /// <summary>
        /// Gets the child rows of this <see cref='System.Data.DataRow'/> using the specified <see cref='System.Data.DataRelation'/> and the specified <see cref='System.Data.DataRowVersion'/>
        /// </summary>
        public DataRow[] GetChildRows(DataRelation? relation, DataRowVersion version)
        {
            if (relation == null)
            {
                return _table.NewRowArray(0);
            }

            if (relation.DataSet != _table.DataSet)
            {
                throw ExceptionBuilder.RowNotInTheDataSet();
            }
            if (relation.ParentKey.Table != _table)
            {
                throw ExceptionBuilder.RelationForeignTable(relation.ParentTable.TableName, _table.TableName);
            }
            return DataRelation.GetChildRows(relation.ParentKey, relation.ChildKey, this, version);
        }

        internal DataColumn GetDataColumn(string columnName)
        {
            DataColumn? column = _columns[columnName];
            if (null != column)
            {
                return column;
            }
            throw ExceptionBuilder.ColumnNotInTheTable(columnName, _table.TableName);
        }

        public DataRow? GetParentRow(string? relationName) =>
            GetParentRow(_table.ParentRelations[relationName], DataRowVersion.Default);

        public DataRow? GetParentRow(string? relationName, DataRowVersion version) =>
            GetParentRow(_table.ParentRelations[relationName], version);

        /// <summary>
        /// Gets the parent row of this <see cref='System.Data.DataRow'/> using the specified <see cref='System.Data.DataRelation'/> .
        /// </summary>
        public DataRow? GetParentRow(DataRelation? relation) =>
            GetParentRow(relation, DataRowVersion.Default);

        /// <summary>
        /// Gets the parent row of this <see cref='System.Data.DataRow'/>
        /// using the specified <see cref='System.Data.DataRelation'/> and <see cref='System.Data.DataRowVersion'/>.
        /// </summary>
        public DataRow? GetParentRow(DataRelation? relation, DataRowVersion version)
        {
            if (relation == null)
            {
                return null;
            }

            if (relation.DataSet != _table.DataSet)
            {
                throw ExceptionBuilder.RelationForeignRow();
            }

            if (relation.ChildKey.Table != _table)
            {
                throw ExceptionBuilder.GetParentRowTableMismatch(relation.ChildTable.TableName, _table.TableName);
            }

            return DataRelation.GetParentRow(relation.ParentKey, relation.ChildKey, this, version);
        }

        // a multiple nested child table's row can have only one non-null FK per row. So table has multiple
        // parents, but a row can have only one parent. Same nested row cannot below to 2 parent rows.
        internal DataRow? GetNestedParentRow(DataRowVersion version)
        {
            // 1) Walk over all FKs and get the non-null. 2) Get the relation. 3) Get the parent Row.
            DataRelation[] nestedParentRelations = _table.NestedParentRelations;
            foreach (DataRelation rel in nestedParentRelations)
            {
                if (rel == null) // don't like this but done for backward code compatability
                {
                    continue;
                }
                if (rel.ParentTable == _table) // self-nested table
                {
                    CheckForLoops(rel);
                }

                DataRow? row = GetParentRow(rel, version);
                if (row != null)
                {
                    return row;
                }
            }
            return null; // Rule 1: At all times, only ONE FK  "(in a row) can be non-Null
        }
        // No Nested in 1-many

        public DataRow[] GetParentRows(string? relationName) =>
            GetParentRows(_table.ParentRelations[relationName], DataRowVersion.Default);

        public DataRow[] GetParentRows(string? relationName, DataRowVersion version) =>
            GetParentRows(_table.ParentRelations[relationName], version);

        /// <summary>
        /// Gets the parent rows of this <see cref='System.Data.DataRow'/> using the specified <see cref='System.Data.DataRelation'/> .
        /// </summary>
        public DataRow[] GetParentRows(DataRelation? relation) =>
            GetParentRows(relation, DataRowVersion.Default);

        /// <summary>
        /// Gets the parent rows of this <see cref='System.Data.DataRow'/> using the specified <see cref='System.Data.DataRelation'/> .
        /// </summary>
        public DataRow[] GetParentRows(DataRelation? relation, DataRowVersion version)
        {
            if (relation == null)
            {
                return _table.NewRowArray(0);
            }

            if (relation.DataSet != _table.DataSet)
            {
                throw ExceptionBuilder.RowNotInTheDataSet();
            }

            if (relation.ChildKey.Table != _table)
            {
                throw ExceptionBuilder.GetParentRowTableMismatch(relation.ChildTable.TableName, _table.TableName);
            }

            return DataRelation.GetParentRows(relation.ParentKey, relation.ChildKey, this, version);
        }

        internal object[] GetColumnValues(DataColumn[] columns) =>
            GetColumnValues(columns, DataRowVersion.Default);

        internal object[] GetColumnValues(DataColumn[] columns, DataRowVersion version)
        {
            DataKey key = new DataKey(columns, false); // temporary key, don't copy columns
            return GetKeyValues(key, version);
        }

        internal object[] GetKeyValues(DataKey key)
        {
            int record = GetDefaultRecord();
            return key.GetKeyValues(record);
        }

        internal object[] GetKeyValues(DataKey key, DataRowVersion version)
        {
            int record = GetRecordFromVersion(version);
            return key.GetKeyValues(record);
        }

        internal int GetCurrentRecordNo()
        {
            if (_newRecord == -1)
            {
                throw ExceptionBuilder.NoCurrentData();
            }
            return _newRecord;
        }

        internal int GetDefaultRecord()
        {
            if (_tempRecord != -1)
            {
                return _tempRecord;
            }
            if (_newRecord != -1)
            {
                return _newRecord;
            }

            // If row has oldRecord - this is deleted row.
            throw _oldRecord == -1 ?
                ExceptionBuilder.RowRemovedFromTheTable() :
                ExceptionBuilder.DeletedRowInaccessible();
        }

        internal int GetOriginalRecordNo()
        {
            if (_oldRecord == -1)
            {
                throw ExceptionBuilder.NoOriginalData();
            }
            return _oldRecord;
        }

        private int GetProposedRecordNo()
        {
            if (_tempRecord == -1)
            {
                throw ExceptionBuilder.NoProposedData();
            }
            return _tempRecord;
        }

        internal int GetRecordFromVersion(DataRowVersion version)
        {
            return version switch
            {
                DataRowVersion.Original => GetOriginalRecordNo(),
                DataRowVersion.Current => GetCurrentRecordNo(),
                DataRowVersion.Proposed => GetProposedRecordNo(),
                DataRowVersion.Default => GetDefaultRecord(),
                _ => throw ExceptionBuilder.InvalidRowVersion(),
            };
        }

        internal DataRowVersion GetDefaultRowVersion(DataViewRowState viewState)
        {
            if (_oldRecord == _newRecord)
            {
                if (_oldRecord == -1)
                {
                    // should be DataView.addNewRow
                    return DataRowVersion.Default;
                }
                return DataRowVersion.Default;
            }
            else if (_oldRecord == -1)
            {
                Debug.Assert(0 != (DataViewRowState.Added & viewState), "not DataViewRowState.Added");
                return DataRowVersion.Default;
            }
            else if (_newRecord == -1)
            {
                Debug.Assert(_action == DataRowAction.Rollback || 0 != (DataViewRowState.Deleted & viewState), "not DataViewRowState.Deleted");
                return DataRowVersion.Original;
            }
            else if (0 != (DataViewRowState.ModifiedCurrent & viewState))
            {
                return DataRowVersion.Default;
            }
            else
            {
                Debug.Assert(0 != (DataViewRowState.ModifiedOriginal & viewState), "not DataViewRowState.ModifiedOriginal");
                return DataRowVersion.Original;
            }
        }

        internal DataViewRowState GetRecordState(int record)
        {
            if (record == -1)
            {
                return DataViewRowState.None;
            }

            if (record == _oldRecord && record == _newRecord)
            {
                return DataViewRowState.Unchanged;
            }

            if (record == _oldRecord)
            {
                return (_newRecord != -1) ? DataViewRowState.ModifiedOriginal : DataViewRowState.Deleted;
            }

            if (record == _newRecord)
            {
                return (_oldRecord != -1) ? DataViewRowState.ModifiedCurrent : DataViewRowState.Added;
            }

            return DataViewRowState.None;
        }

        internal bool HasKeyChanged(DataKey key) =>
            HasKeyChanged(key, DataRowVersion.Current, DataRowVersion.Proposed);

        internal bool HasKeyChanged(DataKey key, DataRowVersion version1, DataRowVersion version2)
        {
            if (!HasVersion(version1) || !HasVersion(version2))
            {
                return true;
            }

            return !key.RecordsEqual(GetRecordFromVersion(version1), GetRecordFromVersion(version2));
        }

        /// <summary>
        /// Gets a value indicating whether a specified version exists.
        /// </summary>
        public bool HasVersion(DataRowVersion version)
        {
            return version switch
            {
                DataRowVersion.Original => (_oldRecord != -1),
                DataRowVersion.Current => (_newRecord != -1),
                DataRowVersion.Proposed => (_tempRecord != -1),
                DataRowVersion.Default => (_tempRecord != -1 || _newRecord != -1),
                _ => throw ExceptionBuilder.InvalidRowVersion(),
            };
        }

        internal bool HasChanges()
        {
            if (!HasVersion(DataRowVersion.Original) || !HasVersion(DataRowVersion.Current))
            {
                return true; // if does not have original, its added row, if does not have current, its deleted row so it has changes
            }
            foreach (DataColumn dc in Table.Columns)
            {
                if (dc.Compare(_oldRecord, _newRecord) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool HaveValuesChanged(DataColumn[] columns) =>
            HaveValuesChanged(columns, DataRowVersion.Current, DataRowVersion.Proposed);

        internal bool HaveValuesChanged(DataColumn[] columns, DataRowVersion version1, DataRowVersion version2)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                CheckColumn(columns[i]);
            }
            DataKey key = new DataKey(columns, false); // temporary key, don't copy columns
            return HasKeyChanged(key, version1, version2);
        }

        /// <summary>
        /// Gets a value indicating whether the column at the specified index contains a
        /// null value.
        /// </summary>
        public bool IsNull(int columnIndex)
        {
            DataColumn column = _columns[columnIndex];
            int record = GetDefaultRecord();
            return column.IsNull(record);
        }

        /// <summary>
        /// Gets a value indicating whether the named column contains a null value.
        /// </summary>
        public bool IsNull(string columnName)
        {
            DataColumn column = GetDataColumn(columnName);
            int record = GetDefaultRecord();
            return column.IsNull(record);
        }

        /// <summary>
        /// Gets a value indicating whether the specified <see cref='System.Data.DataColumn'/>
        /// contains a null value.
        /// </summary>
        public bool IsNull(DataColumn column)
        {
            CheckColumn(column);
            int record = GetDefaultRecord();
            return column.IsNull(record);
        }

        public bool IsNull(DataColumn column, DataRowVersion version)
        {
            CheckColumn(column);
            int record = GetRecordFromVersion(version);
            return column.IsNull(record);
        }

        /// <summary>
        /// Rejects all changes made to the row since <see cref='System.Data.DataRow.AcceptChanges'/>
        /// was last called.
        /// </summary>
        public void RejectChanges()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRow.RejectChanges|API> {0}", _objectID);
            try
            {
                if (RowState != DataRowState.Detached)
                {
                    if (_columns.ColumnsImplementingIChangeTrackingCount != _columns.ColumnsImplementingIRevertibleChangeTrackingCount)
                    {
                        foreach (DataColumn dc in _columns.ColumnsImplementingIChangeTracking)
                        {
                            if (!dc.ImplementsIRevertibleChangeTracking)
                            {
                                object? value = null;
                                if (RowState != DataRowState.Deleted)
                                    value = this[dc];
                                else
                                    value = this[dc, DataRowVersion.Original];
                                if (DBNull.Value != value)
                                {
                                    if (((IChangeTracking)value).IsChanged)
                                    {
                                        throw ExceptionBuilder.UDTImplementsIChangeTrackingButnotIRevertible(dc.DataType.AssemblyQualifiedName!);
                                    }
                                }
                            }
                        }
                    }
                    foreach (DataColumn dc in _columns.ColumnsImplementingIChangeTracking)
                    {
                        object? value = null;
                        if (RowState != DataRowState.Deleted)
                            value = this[dc];
                        else
                            value = this[dc, DataRowVersion.Original];
                        if (DBNull.Value != value)
                        {
                            IChangeTracking tracking = (IChangeTracking)value;
                            if (tracking.IsChanged)
                            {
                                ((IRevertibleChangeTracking)value).RejectChanges();
                            }
                        }
                    }
                }
                _table.RollbackRow(this);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal void ResetLastChangedColumn()
        {
            _lastChangedColumn = null;
            _countColumnChange = 0;
        }

        internal void SetKeyValues(DataKey key, object[] keyValues)
        {
            bool fFirstCall = true;
            bool immediate = (_tempRecord == -1);

            for (int i = 0; i < keyValues.Length; i++)
            {
                object value = this[key.ColumnsReference[i]];
                if (!value.Equals(keyValues[i]))
                {
                    if (immediate && fFirstCall)
                    {
                        fFirstCall = false;
                        BeginEditInternal();
                    }
                    this[key.ColumnsReference[i]] = keyValues[i];
                }
            }
            if (!fFirstCall)
            {
                EndEdit();
            }
        }

        /// <summary>
        /// Sets the specified column's value to a null value.
        /// </summary>
        protected void SetNull(DataColumn column)
        {
            this[column] = DBNull.Value;
        }

        internal void SetNestedParentRow(DataRow? parentRow, bool setNonNested)
        {
            if (parentRow == null)
            {
                SetParentRowToDBNull();
                return;
            }

            foreach (DataRelation relation in _table.ParentRelations)
            {
                if (relation.Nested || setNonNested)
                {
                    if (relation.ParentKey.Table == parentRow._table)
                    {
                        object[] parentKeyValues = parentRow.GetKeyValues(relation.ParentKey);
                        SetKeyValues(relation.ChildKey, parentKeyValues);

                        if (relation.Nested)
                        {
                            if (parentRow._table == _table)
                            {
                                CheckForLoops(relation);
                            }
                            else
                            {
                                GetParentRow(relation);
                            }
                        }
                    }
                }
            }
        }

        public void SetParentRow(DataRow? parentRow)
        {
            SetNestedParentRow(parentRow, true);
        }

        /// <summary>
        /// Sets current row's parent row with specified relation.
        /// </summary>
        public void SetParentRow(DataRow? parentRow, DataRelation? relation)
        {
            if (relation == null)
            {
                SetParentRow(parentRow);
                return;
            }

            if (parentRow == null)
            {
                SetParentRowToDBNull(relation);
                return;
            }

            if (_table.DataSet != parentRow._table.DataSet)
            {
                throw ExceptionBuilder.ParentRowNotInTheDataSet();
            }

            if (relation.ChildKey.Table != _table)
            {
                throw ExceptionBuilder.SetParentRowTableMismatch(relation.ChildKey.Table.TableName, _table.TableName);
            }

            if (relation.ParentKey.Table != parentRow._table)
            {
                throw ExceptionBuilder.SetParentRowTableMismatch(relation.ParentKey.Table.TableName, parentRow._table.TableName);
            }

            object[] parentKeyValues = parentRow.GetKeyValues(relation.ParentKey);
            SetKeyValues(relation.ChildKey, parentKeyValues);
        }

        internal void SetParentRowToDBNull()
        {
            foreach (DataRelation relation in _table.ParentRelations)
            {
                SetParentRowToDBNull(relation);
            }
        }

        internal void SetParentRowToDBNull(DataRelation relation)
        {
            Debug.Assert(relation != null, "The relation should not be null here.");

            if (relation.ChildKey.Table != _table)
            {
                throw ExceptionBuilder.SetParentRowTableMismatch(relation.ChildKey.Table.TableName, _table.TableName);
            }

            object[] parentKeyValues = new object[1];
            parentKeyValues[0] = DBNull.Value;
            SetKeyValues(relation.ChildKey, parentKeyValues);
        }

        public void SetAdded()
        {
            if (RowState == DataRowState.Unchanged)
            {
                _table.SetOldRecord(this, -1);
            }
            else
            {
                throw ExceptionBuilder.SetAddedAndModifiedCalledOnnonUnchanged();
            }
        }

        public void SetModified()
        {
            if (RowState == DataRowState.Unchanged)
            {
                _tempRecord = _table.NewRecord(_newRecord);
                if (_tempRecord != -1)
                {
                    // suppressing the ensure property changed because no values have changed
                    _table.SetNewRecord(this, _tempRecord, suppressEnsurePropertyChanged: true);
                }
            }
            else
            {
                throw ExceptionBuilder.SetAddedAndModifiedCalledOnnonUnchanged();
            }
        }

        // RecordList contains the empty column storage needed. We need to copy the existing record values into this storage.

        internal int CopyValuesIntoStore(ArrayList storeList, ArrayList nullbitList, int storeIndex)
        {
            int recordCount = 0;
            if (_oldRecord != -1)
            {
                //Copy original record for the row in Unchanged, Modified, Deleted state.
                for (int i = 0; i < _columns.Count; i++)
                {
                    _columns[i].CopyValueIntoStore(_oldRecord, storeList[i]!, (BitArray)nullbitList[i]!, storeIndex);
                }
                recordCount++;
                storeIndex++;
            }

            DataRowState state = RowState;
            if ((DataRowState.Added == state) || (DataRowState.Modified == state))
            {
                //Copy current record for the row in Added, Modified state.
                for (int i = 0; i < _columns.Count; i++)
                {
                    _columns[i].CopyValueIntoStore(_newRecord, storeList[i]!, (BitArray)nullbitList[i]!, storeIndex);
                }
                recordCount++;
                storeIndex++;
            }

            if (-1 != _tempRecord)
            {
                //Copy temp record for the row in edit mode.
                for (int i = 0; i < _columns.Count; i++)
                {
                    _columns[i].CopyValueIntoStore(_tempRecord, storeList[i]!, (BitArray)nullbitList[i]!, storeIndex);
                }
                recordCount++;
                storeIndex++;
            }

            return recordCount;
        }

        [Conditional("DEBUG")]
        private void VerifyValueFromStorage(DataColumn column, DataRowVersion version, object valueFromStorage)
        {
            // ignore deleted rows by adding "newRecord != -1" condition - we do not evaluate computed rows if they are deleted
            if (column.DataExpression != null && !_inChangingEvent && _tempRecord == -1 && _newRecord != -1)
            {
                // for unchanged rows, check current if original is asked for.
                // this is because by design, there is only single storage for an unchanged row.
                if (version == DataRowVersion.Original && _oldRecord == _newRecord)
                {
                    version = DataRowVersion.Current;
                }

                Debug.Assert(valueFromStorage.Equals(column.DataExpression.Evaluate(this, version)),
                    "Value from storage does lazily computed expression value");
            }
        }
    }

    public sealed class DataRowBuilder
    {
        internal readonly DataTable _table;
        internal int _record;

        internal DataRowBuilder(DataTable table, int record)
        {
            _table = table;
            _record = record;
        }
    }
}
