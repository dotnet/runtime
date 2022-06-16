// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Data
{
    /// <summary>
    /// Represents a databindable, customized view of a <see cref='System.Data.DataTable'/>
    /// for sorting, filtering, searching, editing, and navigation.
    /// </summary>
    [Designer("Microsoft.VSDesigner.Data.VS.DataViewDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [DefaultProperty(nameof(Table))]
    [DefaultEvent("PositionChanged")]
    [Editor("Microsoft.VSDesigner.Data.Design.DataSourceEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DataView : MarshalByValueComponent, IBindingListView, System.ComponentModel.ITypedList, ISupportInitializeNotification
    {
        private DataViewManager? _dataViewManager;
        private DataTable? _table;
        private bool _locked;
        private Index? _index;
        private Dictionary<string, Index>? _findIndexes;

        private string _sort = string.Empty;

        /// <summary>Allow a user implemented comparison of two DataRow</summary>
        /// <remarks>User must use correct DataRowVersion in comparison or index corruption will happen</remarks>
        private System.Comparison<DataRow>? _comparison;

        /// <summary>
        /// IFilter will allow LinqDataView to wrap <see cref='System.Predicate&lt;DataRow&gt;'/> instead of using a DataExpression
        /// </summary>
        private IFilter? _rowFilter;

        private DataViewRowState _recordStates = DataViewRowState.CurrentRows;

        private bool _shouldOpen = true;
        private bool _open;
        private bool _allowNew = true;
        private bool _allowEdit = true;
        private bool _allowDelete = true;
        private bool _applyDefaultSort;

        internal DataRow? _addNewRow;
        private ListChangedEventArgs? _addNewMoved;

        private System.ComponentModel.ListChangedEventHandler? _onListChanged;
        internal static ListChangedEventArgs s_resetEventArgs = new ListChangedEventArgs(ListChangedType.Reset, -1);

        private DataTable? _delayedTable;
        private string? _delayedRowFilter;
        private string? _delayedSort;
        private DataViewRowState _delayedRecordStates = (DataViewRowState)(-1);
        private bool _fInitInProgress;
        private bool _fEndInitInProgress;

        /// <summary>
        /// You can't delay create the DataRowView instances since multiple thread read access is valid
        /// and each thread must obtain the same DataRowView instance and we want to avoid (inter)locking.
        /// </summary>
        /// <remarks>
        /// In V1.1, the DataRowView[] was recreated after every change.  Each DataRowView was bound to a DataRow.
        /// In V2.0 Whidbey, the DataRowView retained but bound to an index instead of DataRow, allowing the DataRow to vary.
        /// In V2.0 Orcas, the DataRowView retained and bound to a DataRow, allowing the index to vary.
        /// </remarks>
        private Dictionary<DataRow, DataRowView> _rowViewCache = new Dictionary<DataRow, DataRowView>(DataRowReferenceComparer.s_default);

        /// <summary>
        /// This collection allows expression maintenance to (add / remove) from the index when it really should be a (change / move).
        /// </summary>
        private readonly Dictionary<DataRow, DataRowView> _rowViewBuffer = new Dictionary<DataRow, DataRowView>(DataRowReferenceComparer.s_default);

        private sealed class DataRowReferenceComparer : IEqualityComparer<DataRow>
        {
            internal static readonly DataRowReferenceComparer s_default = new DataRowReferenceComparer();

            private DataRowReferenceComparer() { }

            public bool Equals(DataRow? x, DataRow? y) => x == (object?)y;

            public int GetHashCode(DataRow obj) => obj._objectID;
        }

        private readonly DataViewListener _dvListener;

        private static int s_objectTypeCount; // Bid counter
        private readonly int _objectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        internal DataView(DataTable? table, bool locked)
        {
            GC.SuppressFinalize(this);
            DataCommonEventSource.Log.Trace("<ds.DataView.DataView|INFO> {0}, table={1}, locked={2}", ObjectID, (table != null) ? table.ObjectID : 0, locked);

            _dvListener = new DataViewListener(this);
            _locked = locked;
            _table = table;
            _dvListener.RegisterMetaDataEvents(_table);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataView'/> class.
        /// </summary>
        public DataView() : this(null)
        {
            SetIndex2("", DataViewRowState.CurrentRows, null, true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataView'/> class with the
        ///    specified <see cref='System.Data.DataTable'/>.
        /// </summary>
        public DataView(DataTable? table) : this(table, false)
        {
            SetIndex2("", DataViewRowState.CurrentRows, null, true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataView'/> class with the
        ///    specified <see cref='System.Data.DataTable'/>.
        /// </summary>
        [RequiresUnreferencedCode(Select.RequiresUnreferencedCodeMessage)]
        public DataView(DataTable table, string? RowFilter, string? Sort, DataViewRowState RowState)
        {
            GC.SuppressFinalize(this);
            DataCommonEventSource.Log.Trace("<ds.DataView.DataView|API> {0}, table={1}, RowFilter='{2}', Sort='{3}', RowState={4}",
                ObjectID, (table != null) ? table.ObjectID : 0, RowFilter, Sort, RowState);

            if (table == null)
            {
                throw ExceptionBuilder.CanNotUse();
            }

            _dvListener = new DataViewListener(this);
            _locked = false;
            _table = table;
            _dvListener.RegisterMetaDataEvents(_table);

            if ((((int)RowState) & ((int)~(DataViewRowState.CurrentRows | DataViewRowState.OriginalRows))) != 0)
            {
                throw ExceptionBuilder.RecordStateRange();
            }
            else if ((((int)RowState) & ((int)DataViewRowState.ModifiedOriginal)) != 0 &&
                     (((int)RowState) & ((int)DataViewRowState.ModifiedCurrent)) != 0)
            {
                throw ExceptionBuilder.SetRowStateFilter();
            }

            if (Sort == null)
            {
                Sort = string.Empty;
            }

            if (RowFilter == null)
            {
                RowFilter = string.Empty;
            }

            DataExpression newFilter = new DataExpression(table, RowFilter);
            SetIndex(Sort, RowState, newFilter);
        }

        internal DataView(DataTable table, System.Predicate<DataRow>? predicate, System.Comparison<DataRow>? comparison, DataViewRowState RowState)
        {
            GC.SuppressFinalize(this);
            DataCommonEventSource.Log.Trace("<ds.DataView.DataView|API> %d#, table=%d, RowState=%d{ds.DataViewRowState}\n",
                           ObjectID, (table != null) ? table.ObjectID : 0, (int)RowState);

            if (table == null)
            {
                throw ExceptionBuilder.CanNotUse();
            }

            _dvListener = new DataViewListener(this);
            _locked = false;
            _table = table;
            _dvListener.RegisterMetaDataEvents(table);

            if ((((int)RowState) & ((int)~(DataViewRowState.CurrentRows | DataViewRowState.OriginalRows))) != 0)
            {
                throw ExceptionBuilder.RecordStateRange();
            }
            else if ((((int)RowState) & ((int)DataViewRowState.ModifiedOriginal)) != 0 &&
                     (((int)RowState) & ((int)DataViewRowState.ModifiedCurrent)) != 0)
            {
                throw ExceptionBuilder.SetRowStateFilter();
            }

            _comparison = comparison;
            SetIndex2("", RowState, ((null != predicate) ? new RowPredicateFilter(predicate) : null), true);
        }

        /// <summary>
        /// Sets or gets a value indicating whether deletes are allowed.
        /// </summary>
        [DefaultValue(true)]
        public bool AllowDelete
        {
            get { return _allowDelete; }
            set
            {
                if (_allowDelete != value)
                {
                    _allowDelete = value;
                    OnListChanged(s_resetEventArgs);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use the default sort.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue(false)]
        public bool ApplyDefaultSort
        {
            get { return _applyDefaultSort; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataView.set_ApplyDefaultSort|API> {0}, {1}", ObjectID, value);
                if (_applyDefaultSort != value)
                {
                    _comparison = null; // clear the delegate to allow the Sort string to be effective
                    _applyDefaultSort = value;
                    UpdateIndex(true);
                    OnListChanged(s_resetEventArgs);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether edits are allowed.
        /// </summary>
        [DefaultValue(true)]
        public bool AllowEdit
        {
            get { return _allowEdit; }
            set
            {
                if (_allowEdit != value)
                {
                    _allowEdit = value;
                    OnListChanged(s_resetEventArgs);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the new rows can
        /// be added using the <see cref='System.Data.DataView.AddNew'/>
        /// method.
        /// </summary>
        [DefaultValue(true)]
        public bool AllowNew
        {
            get { return _allowNew; }
            set
            {
                if (_allowNew != value)
                {
                    _allowNew = value;
                    OnListChanged(s_resetEventArgs);
                }
            }
        }

        /// <summary>
        /// Gets the number of records in the <see cref='System.Data.DataView'/>.
        /// </summary>
        [Browsable(false)]
        public int Count
        {
            get
            {
                Debug.Assert(_rowViewCache.Count == CountFromIndex, "DataView.Count mismatch");
                return _rowViewCache.Count;
            }
        }

        private int CountFromIndex => ((null != _index) ? _index.RecordCount : 0) + ((null != _addNewRow) ? 1 : 0);

        /// <summary>
        /// Gets the <see cref='System.Data.DataViewManager'/> associated with this <see cref='System.Data.DataView'/> .
        /// </summary>
        [Browsable(false)]
        public DataViewManager? DataViewManager => _dataViewManager;

        [Browsable(false)]
        public bool IsInitialized => !_fInitInProgress;

        /// <summary>
        /// Gets a value indicating whether the data source is currently open and
        /// projecting views of data on the <see cref='System.Data.DataTable'/>.
        /// </summary>
        [Browsable(false)]
        protected bool IsOpen => _open;

        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Gets or sets the expression used to filter which rows are viewed in the <see cref='System.Data.DataView'/>.
        /// </summary>
        [DefaultValue("")]
        public virtual string? RowFilter
        {
            get
            {
                DataExpression? expression = (_rowFilter as DataExpression);
                return (expression == null ? "" : expression.Expression); // CONSIDER: return optimized expression here
            }
            [RequiresUnreferencedCode(Select.RequiresUnreferencedCodeMessage)]
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                DataCommonEventSource.Log.Trace("<ds.DataView.set_RowFilter|API> {0}, '{1}'", ObjectID, value);

                if (_fInitInProgress)
                {
                    _delayedRowFilter = value;
                    return;
                }

                CultureInfo locale = (_table != null ? _table.Locale : CultureInfo.CurrentCulture);
                if (null == _rowFilter || (string.Compare(RowFilter, value, false, locale) != 0))
                {
                    DataExpression newFilter = new DataExpression(_table, value);
                    SetIndex(_sort, _recordStates, newFilter);
                }
            }
        }

        #region RowPredicateFilter
        /// <summary>
        /// The predicate delegate that will determine if a DataRow should be contained within the view.
        /// This RowPredicate property is mutually exclusive with the RowFilter property.
        /// </summary>
        internal Predicate<DataRow>? RowPredicate
        {
            get
            {
                RowPredicateFilter? filter = (GetFilter() as RowPredicateFilter);
                return ((null != filter) ? filter._predicateFilter : null);
            }
            set
            {
                if (!ReferenceEquals(RowPredicate, value))
                {
                    SetIndex(Sort, RowStateFilter, ((null != value) ? new RowPredicateFilter(value) : null));
                }
            }
        }

        private sealed class RowPredicateFilter : System.Data.IFilter
        {
            internal readonly Predicate<DataRow> _predicateFilter;

            /// <summary></summary>
            internal RowPredicateFilter(Predicate<DataRow> predicate)
            {
                Debug.Assert(null != predicate, "null predicate");
                _predicateFilter = predicate;
            }

            /// <summary></summary>
            bool IFilter.Invoke(DataRow row, DataRowVersion version)
            {
                Debug.Assert(DataRowVersion.Default != version, "not expecting Default");
                Debug.Assert(DataRowVersion.Proposed != version, "not expecting Proposed");
                return _predicateFilter(row);
            }
        }
        #endregion

        /// <summary>
        /// Gets or sets the row state filter used in the <see cref='System.Data.DataView'/>.
        /// </summary>
        [DefaultValue(DataViewRowState.CurrentRows)]
        public DataViewRowState RowStateFilter
        {
            get { return _recordStates; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataView.set_RowStateFilter|API> {0}, {1}", ObjectID, value);
                if (_fInitInProgress)
                {
                    _delayedRecordStates = value;
                    return;
                }

                if ((((int)value) & ((int)~(DataViewRowState.CurrentRows | DataViewRowState.OriginalRows))) != 0)
                {
                    throw ExceptionBuilder.RecordStateRange();
                }
                else if ((((int)value) & ((int)DataViewRowState.ModifiedOriginal)) != 0 &&
                        (((int)value) & ((int)DataViewRowState.ModifiedCurrent)) != 0)
                {
                    throw ExceptionBuilder.SetRowStateFilter();
                }

                if (_recordStates != value)
                {
                    SetIndex(_sort, value, _rowFilter);
                }
            }
        }

        /// <summary>
        /// Gets or sets the sort column or columns, and sort order for the table.
        /// </summary>
        [AllowNull]
        [DefaultValue("")]
        public string Sort
        {
            get
            {
                if (_sort.Length == 0 && _applyDefaultSort && _table != null && _table._primaryIndex.Length > 0)
                {
                    return DataTable.FormatSortString(_table._primaryIndex);
                }
                else
                {
                    return _sort;
                }
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                DataCommonEventSource.Log.Trace("<ds.DataView.set_Sort|API> {0}, '{1}'", ObjectID, value);

                if (_fInitInProgress)
                {
                    _delayedSort = value;
                    return;
                }

                CultureInfo locale = (_table != null ? _table.Locale : CultureInfo.CurrentCulture);
                if (string.Compare(_sort, value, false, locale) != 0 || (null != _comparison))
                {
                    CheckSort(value);
                    _comparison = null; // clear the delegate to allow the Sort string to be effective
                    SetIndex(value, _recordStates, _rowFilter);
                }
            }
        }

        /// <summary>Allow a user implemented comparison of two DataRow</summary>
        /// <remarks>User must use correct DataRowVersion in comparison or index corruption will happen</remarks>
        internal System.Comparison<DataRow>? SortComparison
        {
            get { return _comparison; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataView.set_SortComparison|API> {0}", ObjectID);
                if (!ReferenceEquals(_comparison, value))
                {
                    _comparison = value;
                    SetIndex("", _recordStates, _rowFilter);
                }
            }
        }

        object ICollection.SyncRoot => this;

        /// <summary>
        /// Gets or sets the source <see cref='System.Data.DataTable'/>.
        /// </summary>
        [TypeConverterAttribute(typeof(DataTableTypeConverter))]
        [DefaultValue(null)]
        [RefreshProperties(RefreshProperties.All)]
        public DataTable? Table
        {
            get { return _table; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataView.set_Table|API> {0}, {1}", ObjectID, (value != null) ? value.ObjectID : 0);
                if (_fInitInProgress && value != null)
                {
                    _delayedTable = value;
                    return;
                }

                if (_locked)
                {
                    throw ExceptionBuilder.SetTable();
                }
                if (_dataViewManager != null)
                {
                    throw ExceptionBuilder.CanNotSetTable();
                }
                if (value != null && value.TableName.Length == 0)
                {
                    throw ExceptionBuilder.CanNotBindTable();
                }

                if (_table != value)
                {
                    _dvListener.UnregisterMetaDataEvents();
                    _table = value;
                    if (_table != null)
                    {
                        _dvListener.RegisterMetaDataEvents(_table);
                    }

                    SetIndex2("", DataViewRowState.CurrentRows, null, false);
                    if (_table != null)
                    {
                        OnListChanged(new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, new DataTablePropertyDescriptor(_table)));
                    }
                    // index was updated without firing the reset, fire it now
                    OnListChanged(s_resetEventArgs);
                }
            }
        }

        object? IList.this[int recordIndex]
        {
            get { return this[recordIndex]; }
            set { throw ExceptionBuilder.SetIListObject(); }
        }

        /// <summary>
        /// Gets a row of data from a specified table.
        /// </summary>
        public DataRowView this[int recordIndex] => GetRowView(GetRow(recordIndex));

        /// <summary>
        /// Adds a new row of data to view.
        /// </summary>
        /// <remarks>
        /// Only one new row of data allowed at a time, so previous new row will be added to row collection.
        /// Unsupported pattern: dataTable.Rows.Add(dataView.AddNew().Row)
        /// </remarks>
        public virtual DataRowView AddNew()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.AddNew|API> {0}", ObjectID);
            try
            {
                CheckOpen();

                if (!AllowNew)
                {
                    throw ExceptionBuilder.AddNewNotAllowNull();
                }
                if (_addNewRow != null)
                {
                    _rowViewCache[_addNewRow].EndEdit();
                }

                Debug.Assert(null == _addNewRow, "AddNew addNewRow is not null");

                // TODO: This will throw NRE if _table isn't set (e.g. default ctor)
                _addNewRow = _table!.NewRow();
                DataRowView drv = new DataRowView(this, _addNewRow);
                _rowViewCache.Add(_addNewRow, drv);
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, IndexOf(drv)));
                return drv;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void BeginInit()
        {
            _fInitInProgress = true;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Warning related to RowFilter has already been shown when RowFilter was delay set.")]
        public void EndInit()
        {
            if (_delayedTable != null && _delayedTable.fInitInProgress)
            {
                _delayedTable._delayedViews.Add(this);
                return;
            }

            _fInitInProgress = false;
            _fEndInitInProgress = true;
            if (_delayedTable != null)
            {
                Table = _delayedTable;
                _delayedTable = null;
            }
            if (_delayedSort != null)
            {
                Sort = _delayedSort;
                _delayedSort = null;
            }
            if (_delayedRowFilter != null)
            {
                RowFilter = _delayedRowFilter;
                _delayedRowFilter = null;
            }
            if (_delayedRecordStates != (DataViewRowState)(-1))
            {
                RowStateFilter = _delayedRecordStates;
                _delayedRecordStates = (DataViewRowState)(-1);
            }
            _fEndInitInProgress = false;

            SetIndex(Sort, RowStateFilter, _rowFilter);
            OnInitialized();
        }

        private void CheckOpen()
        {
            if (!IsOpen) throw ExceptionBuilder.NotOpen();
        }

        private void CheckSort(string sort)
        {
            if (_table == null)
            {
                throw ExceptionBuilder.CanNotUse();
            }
            if (sort.Length == 0)
            {
                return;
            }
            _table.ParseSortString(sort);
        }

        /// <summary>
        /// Closes the <see cref='System.Data.DataView'/>
        /// </summary>
        protected void Close()
        {
            _shouldOpen = false;
            UpdateIndex();
            _dvListener.UnregisterMetaDataEvents();
        }

        public void CopyTo(Array array, int index)
        {
            if (null != _index)
            {
                RBTree<int>.RBTreeEnumerator iterator = _index.GetEnumerator(0);
                while (iterator.MoveNext())
                {
                    array.SetValue(GetRowView(iterator.Current), index);
                    checked
                    {
                        index++;
                    }
                }
            }
            if (null != _addNewRow)
            {
                array.SetValue(_rowViewCache[_addNewRow], index);
            }
        }

        private void CopyTo(DataRowView[] array, int index)
        {
            if (null != _index)
            {
                RBTree<int>.RBTreeEnumerator iterator = _index.GetEnumerator(0);
                while (iterator.MoveNext())
                {
                    array[index] = GetRowView(iterator.Current);
                    checked
                    {
                        index++;
                    }
                }
            }
            if (null != _addNewRow)
            {
                array[index] = _rowViewCache[_addNewRow];
            }
        }

        /// <summary>
        /// Deletes a row at the specified index.
        /// </summary>
        public void Delete(int index) => Delete(GetRow(index));

        internal void Delete(DataRow row)
        {
            if (null != row)
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.Delete|API> {0}, row={1}", ObjectID, row._objectID);
                try
                {
                    CheckOpen();
                    if (row == _addNewRow)
                    {
                        FinishAddNew(false);
                        return;
                    }
                    if (!AllowDelete)
                    {
                        throw ExceptionBuilder.CanNotDelete();
                    }
                    row.Delete();
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Finds a row in the <see cref='System.Data.DataView'/> by the specified primary key value.
        /// </summary>
        public int Find(object? key) => FindByKey(key);

        /// <summary>Find index of a DataRowView instance that matches the specified primary key value.</summary>
        // TODO: This will throw NRE if _index isn't set (e.g. default ctor)
        internal virtual int FindByKey(object? key) => _index!.FindRecordByKey(key);

        /// <summary>
        /// Finds a row in the <see cref='System.Data.DataView'/> by the specified primary key values.
        /// </summary>
        public int Find(object?[] key) => FindByKey(key);

        /// <summary>Find index of a DataRowView instance that matches the specified primary key values.</summary>
        // TODO: This will throw NRE if _index isn't set (e.g. default ctor)
        internal virtual int FindByKey(object?[] key) => _index!.FindRecordByKey(key);

        /// <summary>
        /// Finds a row in the <see cref='System.Data.DataView'/> by the specified primary key value.
        /// </summary>
        public DataRowView[] FindRows(object? key) => FindRowsByKey(new object?[] { key });

        /// <summary>
        /// Finds a row in the <see cref='System.Data.DataView'/> by the specified primary key values.
        /// </summary>
        public DataRowView[] FindRows(object?[] key) => FindRowsByKey(key);

        /// <summary>Find DataRowView instances that match the specified primary key values.</summary>
        internal virtual DataRowView[] FindRowsByKey(object?[] key)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.FindRows|API> {0}", ObjectID);
            try
            {
                // TODO: This will throw NRE if _index isn't set (e.g. default ctor)
                Range range = _index!.FindRecords(key);
                return GetDataRowViewFromRange(range);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>This method exists for LinqDataView to keep a level of abstraction away from the RBTree</summary>
        internal Range FindRecords<TKey, TRow>(Index.ComparisonBySelector<TKey, TRow> comparison, TKey key) where TRow : DataRow
        {
            // TODO: This will throw NRE if _index isn't set (e.g. default ctor)
            return _index!.FindRecords(comparison, key);
        }

        /// <summary>Convert a Range into a DataRowView[].</summary>
        internal DataRowView[] GetDataRowViewFromRange(Range range)
        {
            if (range.IsNull)
            {
                return Array.Empty<DataRowView>();
            }

            var rows = new DataRowView[range.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = this[i + range.Min];
            }
            return rows;
        }

        internal void FinishAddNew(bool success)
        {
            Debug.Assert(null != _addNewRow, "null addNewRow");
            DataCommonEventSource.Log.Trace("<ds.DataView.FinishAddNew|INFO> {0}, success={1}", ObjectID, success);

            DataRow newRow = _addNewRow;
            if (success)
            {
                if (DataRowState.Detached == newRow.RowState)
                {
                    // MaintainDataView will translate the ItemAdded from the RowCollection into
                    // into either an ItemMoved or no event, since it didn't change position.
                    // also possible it's added to the RowCollection but filtered out of the view.
                    _table!.Rows.Add(newRow);
                }
                else
                {
                    // this means that the record was added to the table by different means and not part of view
                    newRow.EndEdit();
                }
            }

            if (newRow == _addNewRow)
            {
                // this means that the record did not get to the view
                bool flag = _rowViewCache.Remove(_addNewRow);
                Debug.Assert(flag, "didn't remove addNewRow");
                _addNewRow = null;

                if (!success)
                {
                    newRow.CancelEdit();
                }
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, Count));
            }
        }

        /// <summary>
        /// xGets an enumerator for this <see cref='System.Data.DataView'/>.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            // V1.1 compatability: returning List<DataRowView>.GetEnumerator() from RowViewCache
            // prevents users from changing data without invalidating the enumerator
            // aka don't 'return this.RowViewCache.GetEnumerator()'
            var temp = new DataRowView[Count];
            CopyTo(temp, 0);
            return temp.GetEnumerator();
        }

        #region IList

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        int IList.Add(object? value)
        {
            if (value == null)
            {
                // null is default value, so we AddNew.
                AddNew();
                return Count - 1;
            }
            throw ExceptionBuilder.AddExternalObject();
        }

        void IList.Clear()
        {
            throw ExceptionBuilder.CanNotClear();
        }

        bool IList.Contains(object? value) => (0 <= IndexOf(value as DataRowView));

        int IList.IndexOf(object? value) => IndexOf(value as DataRowView);

        /// <summary>Return positional index of a <see cref="DataRowView"/> in this DataView</summary>
        /// <remarks>Behavioral change: will now return -1 once a DataRowView becomes detached.</remarks>
        internal int IndexOf(DataRowView? rowview)
        {
            if (null != rowview)
            {
                if (ReferenceEquals(_addNewRow, rowview.Row))
                {
                    return Count - 1;
                }
                if ((null != _index) && (DataRowState.Detached != rowview.Row.RowState))
                {
                    DataRowView? cached; // verify the DataRowView is one we currently track - not something previously detached
                    if (_rowViewCache.TryGetValue(rowview.Row, out cached) && cached == (object)rowview)
                    {
                        return IndexOfDataRowView(rowview);
                    }
                }
            }
            return -1;
        }

        private int IndexOfDataRowView(DataRowView rowview)
        {
            // rowview.GetRecord() may return the proposed record
            // the index will only contain the original or current record, never proposed.
            // return index.GetIndex(rowview.GetRecord());
            return _index!.GetIndex(rowview.Row.GetRecordFromVersion(rowview.Row.GetDefaultRowVersion(RowStateFilter) & ~DataRowVersion.Proposed));
        }

        void IList.Insert(int index, object? value)
        {
            throw ExceptionBuilder.InsertExternalObject();
        }

        void IList.Remove(object? value)
        {
            int index = IndexOf(value as DataRowView);
            if (0 <= index)
            {
                // must delegate to IList.RemoveAt
                ((IList)this).RemoveAt(index);
            }
            else
            {
                throw ExceptionBuilder.RemoveExternalObject();
            }
        }

        void IList.RemoveAt(int index) => Delete(index);

        internal Index? GetFindIndex(string column, bool keepIndex)
        {
            if (_findIndexes == null)
            {
                _findIndexes = new Dictionary<string, Index>();
            }

            Index? findIndex;
            if (_findIndexes.TryGetValue(column, out findIndex))
            {
                if (!keepIndex)
                {
                    _findIndexes.Remove(column);
                    findIndex.RemoveRef();
                    if (findIndex.RefCount == 1)
                    { // if we have created it and we are removing it, refCount is (1)
                        findIndex.RemoveRef(); // if we are reusing the index created by others, refcount is (2)
                    }
                }
            }
            else
            {
                if (keepIndex)
                {
                    findIndex = _table!.GetIndex(column, _recordStates, GetFilter());
                    _findIndexes[column] = findIndex;
                    findIndex.AddRef();
                }
            }
            return findIndex;
        }

        #endregion

        #region IBindingList implementation
        bool IBindingList.AllowNew => AllowNew;
        object IBindingList.AddNew() => AddNew();
        bool IBindingList.AllowEdit => AllowEdit;
        bool IBindingList.AllowRemove => AllowDelete;

        bool IBindingList.SupportsChangeNotification => true;
        bool IBindingList.SupportsSearching => true;
        bool IBindingList.SupportsSorting => true;
        bool IBindingList.IsSorted => Sort.Length != 0;

        PropertyDescriptor? IBindingList.SortProperty => GetSortProperty();

        internal PropertyDescriptor? GetSortProperty()
        {
            if (_table != null && _index != null && _index._indexFields.Length == 1)
            {
                return new DataColumnPropertyDescriptor(_index._indexFields[0].Column);
            }
            return null;
        }

        ListSortDirection IBindingList.SortDirection => (_index!._indexFields.Length == 1 && _index._indexFields[0].IsDescending) ?
            ListSortDirection.Descending :
            ListSortDirection.Ascending;
        #endregion

        #region ListChanged & Initialized events

        /// <summary>
        /// Occurs when the list managed by the <see cref='System.Data.DataView'/> changes.
        /// </summary>
        public event ListChangedEventHandler? ListChanged
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataView.add_ListChanged|API> {0}", ObjectID);
                _onListChanged += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataView.remove_ListChanged|API> {0}", ObjectID);
                _onListChanged -= value;
            }
        }

        public event EventHandler? Initialized;

        #endregion

        #region IBindingList implementation

        void IBindingList.AddIndex(PropertyDescriptor property) => GetFindIndex(property.Name, keepIndex: true);

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            Sort = CreateSortString(property, direction);
        }

        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            // NOTE: this function had keepIndex previosely
            if (property != null)
            {
                bool created = false;
                Index? findIndex = null;
                try
                {
                    if ((null == _findIndexes) || !_findIndexes.TryGetValue(property.Name, out findIndex))
                    {
                        created = true;
                        findIndex = _table!.GetIndex(property.Name, _recordStates, GetFilter());
                        findIndex.AddRef();
                    }
                    Range recordRange = findIndex.FindRecords(key);

                    if (!recordRange.IsNull)
                    {
                        // check to see if key is equal
                        return _index!.GetIndex(findIndex.GetRecord(recordRange.Min));
                    }
                }
                finally
                {
                    if (created && (null != findIndex))
                    {
                        findIndex.RemoveRef();
                        if (findIndex.RefCount == 1)
                        {
                            // if we have created it and we are removing it, refCount is (1)
                            findIndex.RemoveRef(); // if we are reusing the index created by others, refcount is (2)
                        }
                    }
                }
            }
            return -1;
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
            // Ups: If we don't have index yet we will create it before destroing; Fix this later
            GetFindIndex(property.Name, /*keepIndex:*/false);
        }

        void IBindingList.RemoveSort()
        {
            DataCommonEventSource.Log.Trace("<ds.DataView.RemoveSort|API> {0}", ObjectID);
            Sort = string.Empty;
        }

        #endregion

        #region Additional method and properties for new interface IBindingListView

        void IBindingListView.ApplySort(ListSortDescriptionCollection sorts)
        {
            if (sorts == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(sorts));
            }

            var sortString = new StringBuilder();
            bool addCommaToString = false;
            foreach (ListSortDescription sort in sorts)
            {
                if (sort == null)
                {
                    throw ExceptionBuilder.ArgumentContainsNull(nameof(sorts));
                }
                PropertyDescriptor? property = sort.PropertyDescriptor;

                if (property == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(PropertyDescriptor));
                }

                if (!_table!.Columns.Contains(property.Name))
                {
                    // just check if column does not exist, we will handle duplicate column in Sort
                    throw ExceptionBuilder.ColumnToSortIsOutOfRange(property.Name);
                }
                ListSortDirection direction = sort.SortDirection;

                if (addCommaToString) // (sortStr.Length != 0)
                {
                    sortString.Append(',');
                }
                sortString.Append(CreateSortString(property, direction));

                if (!addCommaToString)
                {
                    addCommaToString = true;
                }
            }
            Sort = sortString.ToString(); // what if we dont have any valid sort criteira? we would reset the sort
        }

        private static string CreateSortString(PropertyDescriptor property, ListSortDirection direction)
        {
            Debug.Assert(property != null, "property is null");

            return direction == ListSortDirection.Descending ?
                $"[{property.Name}] DESC" :
                $"[{property.Name}]";
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Safe because filter is set to empty string.")]
        void IBindingListView.RemoveFilter()
        {
            DataCommonEventSource.Log.Trace("<ds.DataView.RemoveFilter|API> {0}", ObjectID);
            RowFilter = string.Empty;
        }

        string? IBindingListView.Filter
        {
            get { return RowFilter; }
            [RequiresUnreferencedCode(Select.RequiresUnreferencedCodeMessage)]
            set { RowFilter = value; }
        }

        ListSortDescriptionCollection IBindingListView.SortDescriptions => GetSortDescriptions();

        internal ListSortDescriptionCollection GetSortDescriptions()
        {
            ListSortDescription[] sortDescArray = Array.Empty<ListSortDescription>();
            if (_table != null && _index != null && _index._indexFields.Length > 0)
            {
                sortDescArray = new ListSortDescription[_index._indexFields.Length];
                for (int i = 0; i < _index._indexFields.Length; i++)
                {
                    DataColumnPropertyDescriptor columnProperty = new DataColumnPropertyDescriptor(_index._indexFields[i].Column);
                    if (_index._indexFields[i].IsDescending)
                    {
                        sortDescArray[i] = new ListSortDescription(columnProperty, ListSortDirection.Descending);
                    }
                    else
                    {
                        sortDescArray[i] = new ListSortDescription(columnProperty, ListSortDirection.Ascending);
                    }
                }
            }
            return new ListSortDescriptionCollection(sortDescArray);
        }


        bool IBindingListView.SupportsAdvancedSorting => true;

        bool IBindingListView.SupportsFiltering => true;

        #endregion

        #region ITypedList

        string System.ComponentModel.ITypedList.GetListName(PropertyDescriptor[] listAccessors)
        {
            if (_table != null)
            {
                if (listAccessors == null || listAccessors.Length == 0)
                {
                    return _table.TableName;
                }
                else
                {
                    DataSet? dataSet = _table.DataSet;
                    if (dataSet != null)
                    {
                        DataTable? foundTable = dataSet.FindTable(_table, listAccessors, 0);
                        if (foundTable != null)
                        {
                            return foundTable.TableName;
                        }
                    }
                }
            }
            return string.Empty;
        }

        PropertyDescriptorCollection System.ComponentModel.ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            if (_table != null)
            {
                if (listAccessors == null || listAccessors.Length == 0)
                {
                    return _table.GetPropertyDescriptorCollection(null);
                }
                else
                {
                    DataSet? dataSet = _table.DataSet;
                    if (dataSet == null)
                    {
                        return new PropertyDescriptorCollection(null);
                    }

                    DataTable? foundTable = dataSet.FindTable(_table, listAccessors, 0);
                    if (foundTable != null)
                    {
                        return foundTable.GetPropertyDescriptorCollection(null);
                    }
                }
            }
            return new PropertyDescriptorCollection(null);
        }

        #endregion

        /// <summary>
        /// Gets the filter for the <see cref='System.Data.DataView'/>.
        /// </summary>
        internal virtual IFilter? GetFilter() => _rowFilter;

        private int GetRecord(int recordIndex)
        {
            if (unchecked((uint)Count <= (uint)recordIndex))
            {
                throw ExceptionBuilder.RowOutOfRange(recordIndex);
            }

            return recordIndex == _index!.RecordCount ?
                _addNewRow!.GetDefaultRecord() :
                _index.GetRecord(recordIndex);
        }

        /// <exception cref="IndexOutOfRangeException"></exception>
        internal DataRow GetRow(int index)
        {
            int count = Count;
            if (unchecked((uint)count <= (uint)index))
            {
                throw ExceptionBuilder.GetElementIndex(index);
            }
            if ((index == (count - 1)) && (_addNewRow != null))
            {
                // if we could rely on tempRecord being registered with recordManager
                // then this special case code would go away
                return _addNewRow;
            }
            return _table!._recordManager[GetRecord(index)]!;
        }

        private DataRowView GetRowView(int record) => GetRowView(_table!._recordManager[record]!);

        private DataRowView GetRowView(DataRow dr) => _rowViewCache[dr];

        protected virtual void IndexListChanged(object sender, ListChangedEventArgs e)
        {
            if (ListChangedType.Reset != e.ListChangedType)
            {
                OnListChanged(e);
            }

            if (_addNewRow != null && _index!.RecordCount == 0)
            {
                FinishAddNew(false);
            }

            if (ListChangedType.Reset == e.ListChangedType)
            {
                OnListChanged(e);
            }
        }

        internal void IndexListChangedInternal(ListChangedEventArgs e)
        {
            _rowViewBuffer.Clear();

            if ((ListChangedType.ItemAdded == e.ListChangedType) && (null != _addNewMoved))
            {
                if (_addNewMoved.NewIndex == _addNewMoved.OldIndex)
                {
                    // ItemAdded for addNewRow which didn't change position
                    // RowStateChange only triggers RowChanged, not ListChanged
                }
                else
                {
                    // translate the ItemAdded into ItemMoved for addNewRow adding into sorted collection
                    ListChangedEventArgs f = _addNewMoved;
                    _addNewMoved = null;
                    IndexListChanged(this, f);
                }
            }
            // the ItemAdded has to fire twice for AddNewRow (public IBindingList API documentation)
            IndexListChanged(this, e);
        }

        internal void MaintainDataView(ListChangedType changedType, DataRow? row, bool trackAddRemove)
        {
            DataRowView? buffer = null;
            switch (changedType)
            {
                case ListChangedType.ItemAdded:
                    Debug.Assert(null != row, "MaintainDataView.ItemAdded with null DataRow");
                    if (trackAddRemove)
                    {
                        if (_rowViewBuffer.TryGetValue(row, out buffer))
                        {
                            // help turn expression add/remove into a changed/move
                            bool flag = _rowViewBuffer.Remove(row);
                            Debug.Assert(flag, "row actually removed");
                        }
                    }
                    if (row == _addNewRow)
                    {
                        // DataView.AddNew().Row was added to DataRowCollection
                        int index = IndexOfDataRowView(_rowViewCache[_addNewRow]);
                        Debug.Assert(0 <= index, "ItemAdded was actually deleted");

                        _addNewRow = null;
                        _addNewMoved = new ListChangedEventArgs(ListChangedType.ItemMoved, index, Count - 1);
                    }
                    else if (!_rowViewCache.ContainsKey(row))
                    {
                        _rowViewCache.Add(row, buffer ?? new DataRowView(this, row));
                    }
                    else
                    {
                        Debug.Fail("ItemAdded DataRow already in view");
                    }
                    break;
                case ListChangedType.ItemDeleted:
                    Debug.Assert(null != row, "MaintainDataView.ItemDeleted with null DataRow");
                    Debug.Assert(row != _addNewRow, "addNewRow being deleted");

                    if (trackAddRemove)
                    {
                        // help turn expression add/remove into a changed/move
                        _rowViewCache.TryGetValue(row, out buffer);
                        if (null != buffer)
                        {
                            _rowViewBuffer.Add(row, buffer);
                        }
                        else
                        {
                            Debug.Fail("ItemDeleted DataRow not in view tracking");
                        }
                    }
                    if (!_rowViewCache.Remove(row))
                    {
                        Debug.Fail("ItemDeleted DataRow not in view");
                    }
                    break;
                case ListChangedType.Reset:
                    Debug.Assert(null == row, "MaintainDataView.Reset with non-null DataRow");
                    ResetRowViewCache();
                    break;
                case ListChangedType.ItemChanged:
                case ListChangedType.ItemMoved:
                    break;
                case ListChangedType.PropertyDescriptorAdded:
                case ListChangedType.PropertyDescriptorChanged:
                case ListChangedType.PropertyDescriptorDeleted:
                    Debug.Fail("unexpected");
                    break;
            }
        }

        /// <summary>
        /// Raises the <see cref="System.Data.DataView.ListChanged" /> event.
        /// </summary>
        protected virtual void OnListChanged(ListChangedEventArgs e)
        {
            DataCommonEventSource.Log.Trace("<ds.DataView.OnListChanged|INFO> {0}, ListChangedType={1}", ObjectID, e.ListChangedType);
            try
            {
                DataColumn? col = null;
                string? propertyName = null;
                switch (e.ListChangedType)
                {
                    case ListChangedType.ItemChanged:
                    // ItemChanged - a column value changed (0 <= e.OldIndex)
                    // ItemChanged - a DataRow.RowError changed (-1 == e.OldIndex)
                    // ItemChanged - RowState changed (e.NewIndex == e.OldIndex)

                    case ListChangedType.ItemMoved:
                        // ItemMoved - a column value affecting sort order changed
                        // ItemMoved - a state change in equivalent fields
                        Debug.Assert(((ListChangedType.ItemChanged == e.ListChangedType) && ((e.NewIndex == e.OldIndex) || (-1 == e.OldIndex))) ||
                                     (ListChangedType.ItemMoved == e.ListChangedType && (e.NewIndex != e.OldIndex) && (0 <= e.OldIndex)),
                                     "unexpected ItemChanged|ItemMoved");

                        Debug.Assert(0 <= e.NewIndex, "negative NewIndex");
                        if (0 <= e.NewIndex)
                        {
                            DataRow dr = GetRow(e.NewIndex);
                            if (dr.HasPropertyChanged)
                            {
                                col = dr.LastChangedColumn;
                                propertyName = (null != col) ? col.ColumnName : string.Empty;
                            }
                        }

                        break;

                    case ListChangedType.ItemAdded:
                    case ListChangedType.ItemDeleted:
                    case ListChangedType.PropertyDescriptorAdded:
                    case ListChangedType.PropertyDescriptorChanged:
                    case ListChangedType.PropertyDescriptorDeleted:
                    case ListChangedType.Reset:
                        break;
                }

                if (_onListChanged != null)
                {
                    if ((col != null) && (e.NewIndex == e.OldIndex))
                    {
                        ListChangedEventArgs newEventArg = new ListChangedEventArgs(e.ListChangedType, e.NewIndex, new DataColumnPropertyDescriptor(col));
                        _onListChanged(this, newEventArg);
                    }
                    else
                    {
                        _onListChanged(this, e);
                    }
                }
                if (null != propertyName)
                {
                    // empty string if more than 1 column changed
                    this[e.NewIndex].RaisePropertyChangedEvent(propertyName);
                }
            }
            catch (Exception f) when (Common.ADP.IsCatchableExceptionType(f))
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(f); // ignore the exception
            }
        }

        private void OnInitialized()
        {
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Opens a <see cref='System.Data.DataView'/>.
        /// </summary>
        protected void Open()
        {
            _shouldOpen = true;
            UpdateIndex();
            _dvListener.RegisterMetaDataEvents(_table);
        }

        protected void Reset()
        {
            if (IsOpen)
            {
                _index!.Reset();
            }
        }

        internal void ResetRowViewCache()
        {
            Dictionary<DataRow, DataRowView> rvc = new Dictionary<DataRow, DataRowView>(CountFromIndex, DataRowReferenceComparer.s_default);
            DataRowView? drv;

            if (null != _index)
            {
                // this improves performance by iterating of the index instead of computing record by index
                RBTree<int>.RBTreeEnumerator iterator = _index.GetEnumerator(0);
                while (iterator.MoveNext())
                {
                    DataRow row = _table!._recordManager[iterator.Current]!;
                    if (!_rowViewCache.TryGetValue(row, out drv))
                    {
                        drv = new DataRowView(this, row);
                    }
                    rvc.Add(row, drv);
                }
            }
            if (null != _addNewRow)
            {
                _rowViewCache.TryGetValue(_addNewRow, out drv);
                Debug.Assert(null != drv, "didn't contain addNewRow");
                rvc.Add(_addNewRow, drv);
            }
            Debug.Assert(rvc.Count == CountFromIndex, "didn't add expected count");
            _rowViewCache = rvc;
        }

        internal void SetDataViewManager(DataViewManager? dataViewManager)
        {
            if (_table == null)
                throw ExceptionBuilder.CanNotUse();

            if (_dataViewManager != dataViewManager)
            {
                if (dataViewManager != null)
                {
                    dataViewManager._nViews--;
                }

                _dataViewManager = dataViewManager;
                if (dataViewManager != null)
                {
                    dataViewManager._nViews++;
                    DataViewSetting dataViewSetting = dataViewManager.DataViewSettings[_table];
                    try
                    {
                        // sdub: check that we will not do unnesasary operation here if dataViewSetting.Sort == this.Sort ...
                        _applyDefaultSort = dataViewSetting.ApplyDefaultSort;
                        DataExpression newFilter = CreateDataExpressionFromDataViewSettings(dataViewSetting);
                        SetIndex(dataViewSetting.Sort, dataViewSetting.RowStateFilter, newFilter);
                    }
                    catch (Exception e) when (Common.ADP.IsCatchableExceptionType(e))
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e); // ignore the exception
                    }
                    _locked = true;
                }
                else
                {
                    SetIndex("", DataViewRowState.CurrentRows, null);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "RowFilter is marked as unsafe because it can be used in DataExpression so that we only display warning when user is assigning an expression" +
                " which means that in here we're either assigning empty filter which is safe or user has already seen a warning.")]
        private DataExpression CreateDataExpressionFromDataViewSettings(DataViewSetting dataViewSetting)
        {
            return new DataExpression(_table, dataViewSetting.RowFilter);
        }

        internal virtual void SetIndex(string newSort, DataViewRowState newRowStates, IFilter? newRowFilter)
        {
            SetIndex2(newSort, newRowStates, newRowFilter, true);
        }

        internal void SetIndex2(string newSort, DataViewRowState newRowStates, IFilter? newRowFilter, bool fireEvent)
        {
            DataCommonEventSource.Log.Trace("<ds.DataView.SetIndex|INFO> {0}, newSort='{1}', newRowStates={2}", ObjectID, newSort, newRowStates);
            _sort = newSort;
            _recordStates = newRowStates;
            _rowFilter = newRowFilter;

            Debug.Assert((0 == (DataViewRowState.ModifiedCurrent & newRowStates)) ||
                         (0 == (DataViewRowState.ModifiedOriginal & newRowStates)),
                         "asking DataViewRowState for both Original & Current records");

            if (_fEndInitInProgress)
            {
                return;
            }

            if (fireEvent)
            {
                // old code path for virtual UpdateIndex
                UpdateIndex(true);
            }
            else
            {
                // new code path for RelatedView
                Debug.Assert(null == _comparison, "RelatedView should not have a comparison function");
                UpdateIndex(true, false);
            }

            if (null != _findIndexes)
            {
                Dictionary<string, Index> indexes = _findIndexes;
                _findIndexes = null;

                foreach (KeyValuePair<string, Index> entry in indexes)
                {
                    entry.Value.RemoveRef();
                }
            }
        }

        protected void UpdateIndex() => UpdateIndex(false);

        protected virtual void UpdateIndex(bool force) => UpdateIndex(force, true);

        internal void UpdateIndex(bool force, bool fireEvent)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.UpdateIndex|INFO> {0}, force={1}", ObjectID, force);
            try
            {
                if (_open != _shouldOpen || force)
                {
                    _open = _shouldOpen;
                    Index? newIndex = null;
                    if (_open)
                    {
                        if (_table != null)
                        {
                            if (null != SortComparison)
                            {
                                // because an Index with a Comparison<DataRow is not sharable, directly create the index here
                                newIndex = new Index(_table, SortComparison, ((DataViewRowState)_recordStates), GetFilter());

                                // bump the addref from 0 to 1 to added to table index collection
                                // the bump from 1 to 2 will happen via DataViewListener.RegisterListChangedEvent
                                newIndex.AddRef();
                            }
                            else
                            {
                                newIndex = _table.GetIndex(Sort, ((DataViewRowState)_recordStates), GetFilter());
                            }
                        }
                    }

                    if (_index == newIndex)
                    {
                        return;
                    }

                    if (_index != null)
                    {
                        _dvListener.UnregisterListChangedEvent();
                    }

                    _index = newIndex;

                    if (_index != null)
                    {
                        _dvListener.RegisterListChangedEvent(_index);
                    }

                    ResetRowViewCache();

                    if (fireEvent)
                    {
                        OnListChanged(s_resetEventArgs);
                    }
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal void ChildRelationCollectionChanged(object? sender, CollectionChangeEventArgs e)
        {
            DataRelationPropertyDescriptor? NullProp = null;
            OnListChanged(
                e.Action == CollectionChangeAction.Add ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataRelationPropertyDescriptor((System.Data.DataRelation)e.Element!)) :
                e.Action == CollectionChangeAction.Refresh ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, NullProp) :
                e.Action == CollectionChangeAction.Remove ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataRelationPropertyDescriptor((System.Data.DataRelation)e.Element!)) :
            /*default*/ null! // TODO: This will cause an NRE
            );
        }

        internal void ParentRelationCollectionChanged(object? sender, CollectionChangeEventArgs e)
        {
            DataRelationPropertyDescriptor? NullProp = null;
            OnListChanged(
                e.Action == CollectionChangeAction.Add ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataRelationPropertyDescriptor((System.Data.DataRelation)e.Element!)) :
                e.Action == CollectionChangeAction.Refresh ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, NullProp) :
                e.Action == CollectionChangeAction.Remove ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataRelationPropertyDescriptor((System.Data.DataRelation)e.Element!)) :
            /*default*/ null! // TODO: This will cause an NRE
            );
        }

        protected virtual void ColumnCollectionChanged(object? sender, CollectionChangeEventArgs e)
        {
            DataColumnPropertyDescriptor? NullProp = null;
            OnListChanged(
                e.Action == CollectionChangeAction.Add ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataColumnPropertyDescriptor((System.Data.DataColumn)e.Element!)) :
                e.Action == CollectionChangeAction.Refresh ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, NullProp) :
                e.Action == CollectionChangeAction.Remove ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataColumnPropertyDescriptor((System.Data.DataColumn)e.Element!)) :
                /*default*/ null! // TODO: This will cause an NRE
            );
        }

        internal void ColumnCollectionChangedInternal(object? sender, CollectionChangeEventArgs e) =>
            ColumnCollectionChanged(sender, e);

        public DataTable ToTable() =>
            ToTable(null, false, Array.Empty<string>());

        public DataTable ToTable(string? tableName) =>
            ToTable(tableName, false, Array.Empty<string>());

        public DataTable ToTable(bool distinct, params string[] columnNames) =>
            ToTable(null, distinct, columnNames);

        public DataTable ToTable(string? tableName, bool distinct, params string[] columnNames)
        {
            DataCommonEventSource.Log.Trace("<ds.DataView.ToTable|API> {0}, TableName='{1}', distinct={2}", ObjectID, tableName, distinct);

            if (columnNames == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(columnNames));
            }

            DataTable dt = new DataTable();
            dt.Locale = _table!.Locale;
            dt.CaseSensitive = _table.CaseSensitive;
            dt.TableName = tableName ?? _table.TableName;
            dt.Namespace = _table.Namespace;
            dt.Prefix = _table.Prefix;

            if (columnNames.Length == 0)
            {
                columnNames = new string[Table!.Columns.Count];
                for (int i = 0; i < columnNames.Length; i++)
                {
                    columnNames[i] = Table.Columns[i].ColumnName;
                }
            }

            int[] columnIndexes = new int[columnNames.Length];

            List<object[]> rowlist = new List<object[]>();

            for (int i = 0; i < columnNames.Length; i++)
            {
                DataColumn? dc = Table!.Columns[columnNames[i]];
                if (dc == null)
                {
                    throw ExceptionBuilder.ColumnNotInTheUnderlyingTable(columnNames[i], Table.TableName);
                }
                dt.Columns.Add(dc.Clone());
                columnIndexes[i] = Table.Columns.IndexOf(dc);
            }

            foreach (DataRowView drview in this)
            {
                object[] o = new object[columnNames.Length];

                for (int j = 0; j < columnIndexes.Length; j++)
                {
                    o[j] = drview[columnIndexes[j]];
                }
                if (!distinct || !RowExist(rowlist, o))
                {
                    dt.Rows.Add(o);
                    rowlist.Add(o);
                }
            }

            return dt;
        }

        private static bool RowExist(List<object[]> arraylist, object[] objectArray)
        {
            for (int i = 0; i < arraylist.Count; i++)
            {
                object[] rows = arraylist[i];
                bool retval = true;
                for (int j = 0; j < objectArray.Length; j++)
                {
                    retval &= (rows[j].Equals(objectArray[j]));
                }
                if (retval)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// If <paramref name="view"/> is equivalent to the current view with regards to all properties.
        /// <see cref="RowFilter"/> and <see cref="Sort"/> may differ by <see cref="StringComparison.OrdinalIgnoreCase"/>.
        /// </summary>
        public virtual bool Equals(DataView? view)
        {
            if ((null == view) ||
               Table != view.Table ||
               Count != view.Count ||
               !string.Equals(RowFilter, view.RowFilter, StringComparison.OrdinalIgnoreCase) ||  // case insensitive
               !string.Equals(Sort, view.Sort, StringComparison.OrdinalIgnoreCase) ||  // case insensitive
               !ReferenceEquals(SortComparison, view.SortComparison) ||
               !ReferenceEquals(RowPredicate, view.RowPredicate) ||
               RowStateFilter != view.RowStateFilter ||
               DataViewManager != view.DataViewManager ||
               AllowDelete != view.AllowDelete ||
               AllowNew != view.AllowNew ||
               AllowEdit != view.AllowEdit)
            {
                return false;
            }
            return true;
        }

        internal int ObjectID => _objectID;
    }
}
