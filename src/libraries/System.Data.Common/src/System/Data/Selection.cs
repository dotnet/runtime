// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Data
{
    internal readonly struct IndexField : IEquatable<IndexField>
    {
        public readonly DataColumn Column;
        public readonly bool IsDescending; // false = Asc; true = Desc what is default value for this?

        internal IndexField(DataColumn column, bool isDescending)
        {
            Debug.Assert(column != null, "null column");

            Column = column;
            IsDescending = isDescending;
        }

        public static bool operator ==(IndexField if1, IndexField if2) => if1.Equals(if2);

        public static bool operator !=(IndexField if1, IndexField if2) => !if1.Equals(if2);

        // must override Equals if == operator is defined
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is IndexField other && Equals(other);

        public bool Equals(IndexField other) => Column == other.Column && IsDescending == other.IsDescending;

        // must override GetHashCode if Equals is redefined
        public override int GetHashCode() =>
            Column.GetHashCode() ^ IsDescending.GetHashCode();
    }

    internal sealed class Index
    {
        private sealed class IndexTree : RBTree<int>
        {
            private readonly Index _index;

            internal IndexTree(Index index) : base(TreeAccessMethod.KEY_SEARCH_AND_INDEX)
            {
                _index = index;
            }

            protected override int CompareNode(int record1, int record2) =>
                _index.CompareRecords(record1, record2);

            protected override int CompareSatelliteTreeNode(int record1, int record2) =>
                _index.CompareDuplicateRecords(record1, record2);
        }

        // these constants are used to update a DataRow when the record and Row are known, but don't match
        private const int DoNotReplaceCompareRecord = 0;
        private const int ReplaceNewRecordForCompare = 1;
        private const int ReplaceOldRecordForCompare = 2;

        private readonly DataTable _table;
        internal readonly IndexField[] _indexFields;

        /// <summary>Allow a user implemented comparison of two DataRow</summary>
        /// <remarks>User must use correct DataRowVersion in comparison or index corruption will happen</remarks>
        private readonly System.Comparison<DataRow>? _comparison;

        private readonly DataViewRowState _recordStates;
        private readonly WeakReference? _rowFilter;
        private IndexTree _records = null!; // Always initialized in InitRecords
        private int _recordCount;
        private int _refCount;

        private readonly Listeners<DataViewListener> _listeners;

        private bool _suspendEvents;

        private readonly bool _isSharable;
        private readonly bool _hasRemoteAggregate;

        internal const int MaskBits = unchecked(0x7FFFFFFF);

        private static int s_objectTypeCount; // Bid counter
        private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

        public Index(DataTable table, IndexField[] indexFields, DataViewRowState recordStates, IFilter? rowFilter) :
            this(table, indexFields, null, recordStates, rowFilter)
        {
        }

        public Index(DataTable table, System.Comparison<DataRow> comparison, DataViewRowState recordStates, IFilter? rowFilter) :
            this(table, GetAllFields(table.Columns), comparison, recordStates, rowFilter)
        {
        }

        // for the delegate methods, we don't know what the dependent columns are - so all columns are dependent
        private static IndexField[] GetAllFields(DataColumnCollection columns)
        {
            IndexField[] fields = new IndexField[columns.Count];
            for (int i = 0; i < fields.Length; ++i)
            {
                fields[i] = new IndexField(columns[i], false);
            }
            return fields;
        }

        private Index(DataTable table, IndexField[] indexFields, System.Comparison<DataRow>? comparison, DataViewRowState recordStates, IFilter? rowFilter)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.Index|API> {0}, table={1}, recordStates={2}",
                            ObjectID, (table != null) ? table.ObjectID : 0, recordStates);
            Debug.Assert(indexFields != null);
            Debug.Assert(null != table, "null table");
            if ((recordStates &
                 (~(DataViewRowState.CurrentRows | DataViewRowState.OriginalRows))) != 0)
            {
                throw ExceptionBuilder.RecordStateRange();
            }
            _table = table;
            _listeners = new Listeners<DataViewListener>(ObjectID, listener => null != listener);

            _indexFields = indexFields;
            _recordStates = recordStates;
            _comparison = comparison;

            _isSharable = (rowFilter == null) && (comparison == null); // a filter or comparison make an index unsharable
            if (null != rowFilter)
            {
                _rowFilter = new WeakReference(rowFilter);
                DataExpression? expr = (rowFilter as DataExpression);
                if (null != expr)
                {
                    _hasRemoteAggregate = expr.HasRemoteAggregate();
                }
            }
            InitRecords(rowFilter);

            // do not AddRef in ctor, every caller should be responsible to AddRef it
            // if caller does not AddRef, it is expected to be a one-time read operation because the index won't be maintained on writes
        }

        public bool Equal(IndexField[] indexDesc, DataViewRowState recordStates, IFilter? rowFilter)
        {
            if (!_isSharable ||
                _indexFields.Length != indexDesc.Length ||
                _recordStates != recordStates ||
                null != rowFilter)
            {
                return false;
            }

            for (int loop = 0; loop < _indexFields.Length; loop++)
            {
                if (_indexFields[loop].Column != indexDesc[loop].Column ||
                    _indexFields[loop].IsDescending != indexDesc[loop].IsDescending)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool HasRemoteAggregate => _hasRemoteAggregate;

        internal int ObjectID => _objectID;

        public DataViewRowState RecordStates => _recordStates;

        public IFilter? RowFilter => (IFilter?)(_rowFilter?.Target);

        public int GetRecord(int recordIndex)
        {
            Debug.Assert(recordIndex >= 0 && recordIndex < _recordCount, "recordIndex out of range");
            return _records[recordIndex];
        }

        public bool HasDuplicates => _records.HasDuplicates;

        public int RecordCount => _recordCount;

        public bool IsSharable => _isSharable;

        private bool AcceptRecord(int record) => AcceptRecord(record, RowFilter);

        private bool AcceptRecord(int record, IFilter? filter)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.AcceptRecord|API> {0}, record={1}", ObjectID, record);
            if (filter == null)
            {
                return true;
            }

            DataRow? row = _table._recordManager[record];

            if (row == null)
            {
                return true;
            }

            DataRowVersion version = DataRowVersion.Default;
            if (row._oldRecord == record)
            {
                version = DataRowVersion.Original;
            }
            else if (row._newRecord == record)
            {
                version = DataRowVersion.Current;
            }
            else if (row._tempRecord == record)
            {
                version = DataRowVersion.Proposed;
            }

            return filter.Invoke(row, version);
        }

        /// <remarks>Only call from inside a lock(this)</remarks>
        internal void ListChangedAdd(DataViewListener listener) => _listeners.Add(listener);

        /// <remarks>Only call from inside a lock(this)</remarks>
        internal void ListChangedRemove(DataViewListener listener) => _listeners.Remove(listener);

        public int RefCount => _refCount;

        public void AddRef()
        {
            DataCommonEventSource.Log.Trace("<ds.Index.AddRef|API> {0}", ObjectID);
            _table._indexesLock.EnterWriteLock();
            try
            {
                Debug.Assert(0 <= _refCount, "AddRef on disposed index");
                Debug.Assert(null != _records, "null records");
                if (_refCount == 0)
                {
                    _table.ShadowIndexCopy();
                    _table._indexes.Add(this);
                }
                _refCount++;
            }
            finally
            {
                _table._indexesLock.ExitWriteLock();
            }
        }

        public int RemoveRef()
        {
            DataCommonEventSource.Log.Trace("<ds.Index.RemoveRef|API> {0}", ObjectID);
            int count;
            _table._indexesLock.EnterWriteLock();
            try
            {
                count = --_refCount;
                if (_refCount <= 0)
                {
                    _table.ShadowIndexCopy();
                    _table._indexes.Remove(this);
                }
            }
            finally
            {
                _table._indexesLock.ExitWriteLock();
            }
            return count;
        }

        private void ApplyChangeAction(int record, int action, int changeRecord)
        {
            if (action != 0)
            {
                if (action > 0)
                {
                    if (AcceptRecord(record))
                    {
                        InsertRecord(record, true);
                    }
                }
                else if ((null != _comparison) && (-1 != record))
                {
                    // when removing a record, the DataRow has already been updated to the newer record
                    // depending on changeRecord, either the new or old record needs be backdated to record
                    // for Comparison<DataRow> to operate correctly
                    DeleteRecord(GetIndex(record, changeRecord));
                }
                else
                {
                    // unnecessary codepath other than keeping original code path for redbits
                    DeleteRecord(GetIndex(record));
                }
            }
        }

        public bool CheckUnique()
        {
#if DEBUG
            Debug.Assert(_records.CheckUnique(_records.root) != HasDuplicates, "CheckUnique difference");
#endif
            return !HasDuplicates;
        }

        // only used for main tree compare, not satalite tree
        private int CompareRecords(int record1, int record2)
        {
            if (null != _comparison)
            {
                return CompareDataRows(record1, record2);
            }
            if (0 < _indexFields.Length)
            {
                for (int i = 0; i < _indexFields.Length; i++)
                {
                    int c = _indexFields[i].Column.Compare(record1, record2);
                    if (c != 0)
                    {
                        return (_indexFields[i].IsDescending ? -c : c);
                    }
                }
                return 0;
            }
            else
            {
                var (row1, row2) = (_table._recordManager[record1], _table._recordManager[record2]);

                Debug.Assert(null != row1, "record1 no datarow");
                Debug.Assert(null != row2, "record2 no datarow");

                // Need to use compare because subtraction will wrap
                // to positive for very large neg numbers, etc.
                return _table.Rows.IndexOf(row1).CompareTo(_table.Rows.IndexOf(row2));
            }
        }

        private int CompareDataRows(int record1, int record2)
        {
            _table._recordManager.VerifyRecord(record1, _table._recordManager[record1]);
            _table._recordManager.VerifyRecord(record2, _table._recordManager[record2]);
            return _comparison!(_table._recordManager[record1], _table._recordManager[record2]);
        }


        // PS: same as previous CompareRecords, except it compares row state if needed
        // only used for satalite tree compare
        private int CompareDuplicateRecords(int record1, int record2)
        {
#if DEBUG
            if (null != _comparison)
            {
                Debug.Assert(0 == CompareDataRows(record1, record2), "duplicate record not a duplicate by user function");
            }
            else if (record1 != record2)
            {
                for (int i = 0; i < _indexFields.Length; i++)
                {
                    int c = _indexFields[i].Column.Compare(record1, record2);
                    Debug.Assert(0 == c, "duplicate record not a duplicate");
                }
            }
#endif
            var (row1, row2) = (_table._recordManager[record1], _table._recordManager[record2]);
            Debug.Assert(null != row1, "record1 no datarow");
            Debug.Assert(null != row2, "record2 no datarow");

            if (null == row1)
            {
                return ((null == row2) ? 0 : -1);
            }
            else if (null == row2)
            {
                return 1;
            }

            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            int diff = row1.rowID.CompareTo(row2.rowID);

            // if they're two records in the same row, we need to be able to distinguish them.
            if ((diff == 0) && (record1 != record2))
            {
                diff = ((int)row1.GetRecordState(record1)).CompareTo((int)row2.GetRecordState(record2));
            }
            return diff;
        }

        private int CompareRecordToKey(int record1, object?[] vals)
        {
            for (int i = 0; i < _indexFields.Length; i++)
            {
                int c = _indexFields[i].Column.CompareValueTo(record1, vals[i]);
                if (c != 0)
                {
                    return (_indexFields[i].IsDescending ? -c : c);
                }
            }
            return 0;
        }

        // DeleteRecordFromIndex deletes the given record from index and does not fire any Event. IT SHOULD NOT FIRE EVENT
        public void DeleteRecordFromIndex(int recordIndex)
        {
            // this is for expression use, to maintain expression columns's sort , filter etc. do not fire event
            DeleteRecord(recordIndex, false);
        }

        // old and existing DeleteRecord behavior, we can not use this for silently deleting
        private void DeleteRecord(int recordIndex)
        {
            DeleteRecord(recordIndex, true);
        }

        private void DeleteRecord(int recordIndex, bool fireEvent)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.DeleteRecord|INFO> {0}, recordIndex={1}, fireEvent={2}", ObjectID, recordIndex, fireEvent);

            if (recordIndex >= 0)
            {
                _recordCount--;
                int record = _records.DeleteByIndex(recordIndex);

                MaintainDataView(ListChangedType.ItemDeleted, record, !fireEvent);

                if (fireEvent)
                {
                    OnListChanged(ListChangedType.ItemDeleted, recordIndex);
                }
            }
        }

        // this improves performance by allowing DataView to iterating instead of computing for records over index
        // this will also allow Linq over DataSet to enumerate over the index
        // avoid boxing by returning RBTreeEnumerator (a struct) instead of IEnumerator<int>
        public RBTree<int>.RBTreeEnumerator GetEnumerator(int startIndex) =>
            new IndexTree.RBTreeEnumerator(_records, startIndex);

        // What it actually does is find the index in the records[] that
        // this record inhabits, and if it doesn't, suggests what index it would
        // inhabit while setting the high bit.
        public int GetIndex(int record) => _records.GetIndexByKey(record);

        /// <summary>
        /// When searching by value for a specific record, the DataRow may require backdating to reflect the appropriate state
        /// otherwise on Delete of a DataRow in the Added state, would result in the <see cref="System.Comparison&lt;DataRow&gt;"/> where the row
        /// reflection record would be in the Detached instead of Added state.
        /// </summary>
        private int GetIndex(int record, int changeRecord)
        {
            Debug.Assert(null != _comparison, "missing comparison");

            int index;
            DataRow row = _table._recordManager[record]!;

            int a = row._newRecord;
            int b = row._oldRecord;
            try
            {
                switch (changeRecord)
                {
                    case ReplaceNewRecordForCompare:
                        row._newRecord = record;
                        break;
                    case ReplaceOldRecordForCompare:
                        row._oldRecord = record;
                        break;
                }
                _table._recordManager.VerifyRecord(record, row);

                index = _records.GetIndexByKey(record);
            }
            finally
            {
                switch (changeRecord)
                {
                    case ReplaceNewRecordForCompare:
                        Debug.Assert(record == row._newRecord, "newRecord has change during GetIndex");
                        row._newRecord = a;
                        break;
                    case ReplaceOldRecordForCompare:
                        Debug.Assert(record == row._oldRecord, "oldRecord has change during GetIndex");
                        row._oldRecord = b;
                        break;
                }
#if DEBUG
                if (-1 != a)
                {
                    _table._recordManager.VerifyRecord(a, row);
                }
#endif
            }
            return index;
        }

        public object[] GetUniqueKeyValues()
        {
            if (_indexFields == null || _indexFields.Length == 0)
            {
                return Array.Empty<object>();
            }
            List<object[]> list = new List<object[]>();
            GetUniqueKeyValues(list, _records.root);
            return list.ToArray();
        }

        /// <summary>
        /// Find index of maintree node that matches key in record
        /// </summary>
        public int FindRecord(int record)
        {
            int nodeId = _records.Search(record);
            if (nodeId != IndexTree.NIL)
                return _records.GetIndexByNode(nodeId); //always returns the First record index
            else
                return -1;
        }

        public int FindRecordByKey(object? key)
        {
            int nodeId = FindNodeByKey(key);
            if (IndexTree.NIL != nodeId)
            {
                return _records.GetIndexByNode(nodeId);
            }
            return -1; // return -1 to user indicating record not found
        }

        public int FindRecordByKey(object?[] key)
        {
            int nodeId = FindNodeByKeys(key);
            if (IndexTree.NIL != nodeId)
            {
                return _records.GetIndexByNode(nodeId);
            }
            return -1; // return -1 to user indicating record not found
        }

        private int FindNodeByKey(object? originalKey)
        {
            int x, c;
            if (_indexFields.Length != 1)
            {
                throw ExceptionBuilder.IndexKeyLength(_indexFields.Length, 1);
            }

            x = _records.root;
            if (IndexTree.NIL != x)
            {
                // otherwise storage may not exist
                DataColumn column = _indexFields[0].Column;
                object? key = column.ConvertValue(originalKey);

                x = _records.root;
                if (_indexFields[0].IsDescending)
                {
                    while (IndexTree.NIL != x)
                    {
                        c = column.CompareValueTo(_records.Key(x), key);
                        if (c == 0) { break; }
                        if (c < 0) { x = _records.Left(x); } // < for decsending
                        else { x = _records.Right(x); }
                    }
                }
                else
                {
                    while (IndexTree.NIL != x)
                    {
                        c = column.CompareValueTo(_records.Key(x), key);
                        if (c == 0) { break; }
                        if (c > 0) { x = _records.Left(x); } // > for ascending
                        else { x = _records.Right(x); }
                    }
                }
            }
            return x;
        }

        private int FindNodeByKeys(object?[]? originalKey)
        {
            int x, c;
            c = ((null != originalKey) ? originalKey.Length : 0);
            if (originalKey is null || 0 == c || _indexFields.Length != c)
            {
                throw ExceptionBuilder.IndexKeyLength(_indexFields.Length, c);
            }

            x = _records.root;
            if (IndexTree.NIL != x)
            {
                // otherwise storage may not exist
                // copy array to avoid changing original
                object?[] key = new object?[originalKey.Length];
                for (int i = 0; i < originalKey.Length; ++i)
                {
                    key[i] = _indexFields[i].Column.ConvertValue(originalKey[i]);
                }

                x = _records.root;
                while (IndexTree.NIL != x)
                {
                    c = CompareRecordToKey(_records.Key(x), key);
                    if (c == 0) { break; }
                    if (c > 0) { x = _records.Left(x); }
                    else { x = _records.Right(x); }
                }
            }
            return x;
        }

        private int FindNodeByKeyRecord(int record)
        {
            int x, c;
            x = _records.root;
            if (IndexTree.NIL != x)
            {
                // otherwise storage may not exist
                x = _records.root;
                while (IndexTree.NIL != x)
                {
                    c = CompareRecords(_records.Key(x), record);
                    if (c == 0) { break; }
                    if (c > 0) { x = _records.Left(x); }
                    else { x = _records.Right(x); }
                }
            }
            return x;
        }

        internal delegate int ComparisonBySelector<TKey, TRow>(TKey key, TRow row) where TRow : DataRow;

        /// <summary>This method exists for LinqDataView to keep a level of abstraction away from the RBTree</summary>
        internal Range FindRecords<TKey, TRow>(ComparisonBySelector<TKey, TRow> comparison, TKey key) where TRow : DataRow
        {
            int x = _records.root;
            while (IndexTree.NIL != x)
            {
                int c = comparison(key, (TRow)_table._recordManager[_records.Key(x)]!);
                if (c == 0) { break; }
                if (c < 0) { x = _records.Left(x); }
                else { x = _records.Right(x); }
            }
            return GetRangeFromNode(x);
        }

        private Range GetRangeFromNode(int nodeId)
        {
            // fill range with the min and max indexes of matching record (i.e min and max of satellite tree)
            // min index is the index of the node in main tree, and max is the min + size of satellite tree-1

            if (IndexTree.NIL == nodeId)
            {
                return default;
            }
            int recordIndex = _records.GetIndexByNode(nodeId);

            if (_records.Next(nodeId) == IndexTree.NIL)
                return new Range(recordIndex, recordIndex);

            int span = _records.SubTreeSize(_records.Next(nodeId));
            return new Range(recordIndex, recordIndex + span - 1);
        }

        public Range FindRecords(object? key)
        {
            int nodeId = FindNodeByKey(key);    // main tree node associated with key
            return GetRangeFromNode(nodeId);
        }

        public Range FindRecords(object?[] key)
        {
            int nodeId = FindNodeByKeys(key);    // main tree node associated with key
            return GetRangeFromNode(nodeId);
        }

        internal void FireResetEvent()
        {
            DataCommonEventSource.Log.Trace("<ds.Index.FireResetEvent|API> {0}", ObjectID);
            if (DoListChanged)
            {
                OnListChanged(DataView.s_resetEventArgs);
            }
        }

        private int GetChangeAction(DataViewRowState oldState, DataViewRowState newState)
        {
            int oldIncluded = ((int)_recordStates & (int)oldState) == 0 ? 0 : 1;
            int newIncluded = ((int)_recordStates & (int)newState) == 0 ? 0 : 1;
            return newIncluded - oldIncluded;
        }

        /// <summary>Determine if the record that needs backdating is the newRecord or oldRecord or neither</summary>
        private static int GetReplaceAction(DataViewRowState oldState)
        {
            return ((0 != (DataViewRowState.CurrentRows & oldState)) ? ReplaceNewRecordForCompare :    // Added/ModifiedCurrent/Unchanged
                    ((0 != (DataViewRowState.OriginalRows & oldState)) ? ReplaceOldRecordForCompare :   // Deleted/ModififedOriginal
                      DoNotReplaceCompareRecord));                                                      // None
        }

        public DataRow GetRow(int i) => _table._recordManager[GetRecord(i)];

        public DataRow[] GetRows(object[] values) => GetRows(FindRecords(values));

        public DataRow[] GetRows(Range range)
        {
            DataRow[] newRows = _table.NewRowArray(range.Count);
            if (0 < newRows.Length)
            {
                RBTree<int>.RBTreeEnumerator iterator = GetEnumerator(range.Min);
                for (int i = 0; i < newRows.Length && iterator.MoveNext(); i++)
                {
                    newRows[i] = _table._recordManager[iterator.Current];
                }
            }
            return newRows;
        }

        private void InitRecords(IFilter? filter)
        {
            DataViewRowState states = _recordStates;

            // this improves performance when the is no filter, like with the default view (creating after rows added)
            // we know the records are in the correct order, just append to end, duplicates not possible
            bool append = (0 == _indexFields.Length);

            _records = new IndexTree(this);

            _recordCount = 0;

            // this improves performance by iterating of the index instead of computing record by index
            foreach (DataRow b in _table.Rows)
            {
                int record = -1;
                if (b._oldRecord == b._newRecord)
                {
                    if ((states & DataViewRowState.Unchanged) != 0)
                    {
                        record = b._oldRecord;
                    }
                }
                else if (b._oldRecord == -1)
                {
                    if ((states & DataViewRowState.Added) != 0)
                    {
                        record = b._newRecord;
                    }
                }
                else if (b._newRecord == -1)
                {
                    if ((states & DataViewRowState.Deleted) != 0)
                    {
                        record = b._oldRecord;
                    }
                }
                else
                {
                    if ((states & DataViewRowState.ModifiedCurrent) != 0)
                    {
                        record = b._newRecord;
                    }
                    else if ((states & DataViewRowState.ModifiedOriginal) != 0)
                    {
                        record = b._oldRecord;
                    }
                }
                if (record != -1 && AcceptRecord(record, filter))
                {
                    _records.InsertAt(-1, record, append);
                    _recordCount++;
                }
            }
        }


        // InsertRecordToIndex inserts the given record to index and does not fire any Event. IT SHOULD NOT FIRE EVENT
        // I added this since I can not use existing InsertRecord which is not silent operation
        // it returns the position that record is inserted
        public int InsertRecordToIndex(int record)
        {
            int pos = -1;
            if (AcceptRecord(record))
            {
                pos = InsertRecord(record, false);
            }
            return pos;
        }

        // existing functionality, it calls the overload with fireEvent== true, so it still fires the event
        private int InsertRecord(int record, bool fireEvent)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.InsertRecord|INFO> {0}, record={1}, fireEvent={2}", ObjectID, record, fireEvent);

            // this improves performance when the is no filter, like with the default view (creating before rows added)
            // we know can append when the new record is the last row in table, normal insertion pattern
            bool append = false;
            if ((0 == _indexFields.Length) && (null != _table))
            {
                DataRow row = _table._recordManager[record];
                append = (_table.Rows.IndexOf(row) + 1 == _table.Rows.Count);
            }
            int nodeId = _records.InsertAt(-1, record, append);

            _recordCount++;

            MaintainDataView(ListChangedType.ItemAdded, record, !fireEvent);

            if (fireEvent)
            {
                if (DoListChanged)
                {
                    OnListChanged(ListChangedType.ItemAdded, _records.GetIndexByNode(nodeId));
                }
                return 0;
            }
            else
            {
                return _records.GetIndexByNode(nodeId);
            }
        }


        // Search for specified key
        public bool IsKeyInIndex(object key)
        {
            int x_id = FindNodeByKey(key);
            return (IndexTree.NIL != x_id);
        }

        public bool IsKeyInIndex(object[] key)
        {
            int x_id = FindNodeByKeys(key);
            return (IndexTree.NIL != x_id);
        }

        public bool IsKeyRecordInIndex(int record)
        {
            int x_id = FindNodeByKeyRecord(record);
            return (IndexTree.NIL != x_id);
        }

        private bool DoListChanged => (!_suspendEvents && _listeners.HasListeners && !_table.AreIndexEventsSuspended);

        private void OnListChanged(ListChangedType changedType, int newIndex, int oldIndex)
        {
            if (DoListChanged)
            {
                OnListChanged(new ListChangedEventArgs(changedType, newIndex, oldIndex));
            }
        }

        private void OnListChanged(ListChangedType changedType, int index)
        {
            if (DoListChanged)
            {
                OnListChanged(new ListChangedEventArgs(changedType, index));
            }
        }

        private void OnListChanged(ListChangedEventArgs e)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.OnListChanged|INFO> {0}", ObjectID);
            Debug.Assert(DoListChanged, "supposed to check DoListChanged before calling to delay create ListChangedEventArgs");

            _listeners.Notify(e, false, false,
                delegate (DataViewListener listener, ListChangedEventArgs args, bool arg2, bool arg3)
                {
                    listener.IndexListChanged(args);
                });
        }

        private void MaintainDataView(ListChangedType changedType, int record, bool trackAddRemove)
        {
            Debug.Assert(-1 <= record, "bad record#");

            _listeners.Notify(changedType, ((0 <= record) ? _table._recordManager[record] : null), trackAddRemove,
                delegate (DataViewListener listener, ListChangedType type, DataRow? row, bool track)
                {
                    listener.MaintainDataView(changedType, row, track);
                });
        }

        public void Reset()
        {
            DataCommonEventSource.Log.Trace("<ds.Index.Reset|API> {0}", ObjectID);
            InitRecords(RowFilter);
            MaintainDataView(ListChangedType.Reset, -1, false);
            FireResetEvent();
        }

        public void RecordChanged(int record)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.RecordChanged|API> {0}, record={1}", ObjectID, record);
            if (DoListChanged)
            {
                int index = GetIndex(record);
                if (index >= 0)
                {
                    OnListChanged(ListChangedType.ItemChanged, index);
                }
            }
        }

        // new RecordChanged which takes oldIndex and newIndex and fires _onListChanged
        public void RecordChanged(int oldIndex, int newIndex)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.RecordChanged|API> {0}, oldIndex={1}, newIndex={2}", ObjectID, oldIndex, newIndex);

            if (oldIndex > -1 || newIndex > -1)
            {
                // no need to fire if it was not and will not be in index: this check means at least one version should be in index
                if (oldIndex == newIndex)
                {
                    OnListChanged(ListChangedType.ItemChanged, newIndex, oldIndex);
                }
                else if (oldIndex == -1)
                {
                    // it is added
                    OnListChanged(ListChangedType.ItemAdded, newIndex, oldIndex);
                }
                else if (newIndex == -1)
                {
                    OnListChanged(ListChangedType.ItemDeleted, oldIndex);
                }
                else
                {
                    OnListChanged(ListChangedType.ItemMoved, newIndex, oldIndex);
                }
            }
        }

        public void RecordStateChanged(int record, DataViewRowState oldState, DataViewRowState newState)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.RecordStateChanged|API> {0}, record={1}, oldState={2}, newState={3}", ObjectID, record, oldState, newState);

            int action = GetChangeAction(oldState, newState);
            ApplyChangeAction(record, action, GetReplaceAction(oldState));
        }

        public void RecordStateChanged(int oldRecord, DataViewRowState oldOldState, DataViewRowState oldNewState,
                                       int newRecord, DataViewRowState newOldState, DataViewRowState newNewState)
        {
            DataCommonEventSource.Log.Trace("<ds.Index.RecordStateChanged|API> {0}, oldRecord={1}, oldOldState={2}, oldNewState={3}, newRecord={4}, newOldState={5}, newNewState={6}",
                ObjectID, oldRecord, oldOldState, oldNewState, newRecord, newOldState, newNewState);

            Debug.Assert((-1 == oldRecord) || (-1 == newRecord) ||
                         _table._recordManager[oldRecord] == _table._recordManager[newRecord],
                         "not the same DataRow when updating oldRecord and newRecord");

            int oldAction = GetChangeAction(oldOldState, oldNewState);
            int newAction = GetChangeAction(newOldState, newNewState);
            if (oldAction == -1 && newAction == 1 && AcceptRecord(newRecord))
            {
                int oldRecordIndex;
                if ((null != _comparison) && oldAction < 0)
                { // when oldRecord is being removed, allow GetIndexByKey updating the DataRow for Comparison<DataRow>
                    oldRecordIndex = GetIndex(oldRecord, GetReplaceAction(oldOldState));
                }
                else
                {
                    oldRecordIndex = GetIndex(oldRecord);
                }

                if ((null == _comparison) && oldRecordIndex != -1 && CompareRecords(oldRecord, newRecord) == 0)
                {
                    _records.UpdateNodeKey(oldRecord, newRecord);    //change in place, as Both records have same key value

                    int commonIndexLocation = GetIndex(newRecord);
                    OnListChanged(ListChangedType.ItemChanged, commonIndexLocation, commonIndexLocation);
                }
                else
                {
                    _suspendEvents = true;
                    if (oldRecordIndex != -1)
                    {
                        _records.DeleteByIndex(oldRecordIndex); // DeleteByIndex doesn't require searching by key
                        _recordCount--;
                    }

                    _records.Insert(newRecord);
                    _recordCount++;

                    _suspendEvents = false;

                    int newRecordIndex = GetIndex(newRecord);
                    if (oldRecordIndex == newRecordIndex)
                    { // if the position is the same
                        OnListChanged(ListChangedType.ItemChanged, newRecordIndex, oldRecordIndex); // be carefull remove oldrecord index if needed
                    }
                    else
                    {
                        if (oldRecordIndex == -1)
                        {
                            MaintainDataView(ListChangedType.ItemAdded, newRecord, false);
                            OnListChanged(ListChangedType.ItemAdded, GetIndex(newRecord)); // oldLocation would be -1
                        }
                        else
                        {
                            OnListChanged(ListChangedType.ItemMoved, newRecordIndex, oldRecordIndex);
                        }
                    }
                }
            }
            else
            {
                ApplyChangeAction(oldRecord, oldAction, GetReplaceAction(oldOldState));
                ApplyChangeAction(newRecord, newAction, GetReplaceAction(newOldState));
            }
        }

        internal DataTable Table => _table;

        private void GetUniqueKeyValues(List<object[]> list, int curNodeId)
        {
            if (curNodeId != IndexTree.NIL)
            {
                GetUniqueKeyValues(list, _records.Left(curNodeId));

                int record = _records.Key(curNodeId);
                object[] element = new object[_indexFields.Length]; // number of columns in PK
                for (int j = 0; j < element.Length; ++j)
                {
                    element[j] = _indexFields[j].Column[record];
                }
                list.Add(element);

                GetUniqueKeyValues(list, _records.Right(curNodeId));
            }
        }

        internal static int IndexOfReference<T>(List<T?>? list, T item) where T : class
        {
            if (null != list)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (ReferenceEquals(list[i], item))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        internal static bool ContainsReference<T>(List<T?> list, T item) where T : class
        {
            return (0 <= IndexOfReference(list, item));
        }
    }

    internal sealed class Listeners<TElem> where TElem : class
    {
        private readonly List<TElem?> _listeners;
        private readonly Func<TElem?, bool> _filter;
        private readonly int _objectID;
        private int _listenerReaderCount;

        /// <summary>Wish this was defined in mscorlib.dll instead of System.Core.dll</summary>
        internal delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

        /// <summary>Wish this was defined in mscorlib.dll instead of System.Core.dll</summary>
        internal delegate TResult Func<T1, TResult>(T1 arg1);

        internal Listeners(int ObjectID, Func<TElem?, bool> notifyFilter)
        {
            _listeners = new List<TElem?>();
            _filter = notifyFilter;
            _objectID = ObjectID;
            _listenerReaderCount = 0;
        }

        internal bool HasListeners => (0 < _listeners.Count);

        /// <remarks>Only call from inside a lock</remarks>
        internal void Add(TElem listener)
        {
            Debug.Assert(null != listener, "null listener");
            Debug.Assert(!Index.ContainsReference(_listeners, listener), "already contains reference");
            _listeners.Add(listener);
        }

        internal int IndexOfReference(TElem listener)
        {
            return Index.IndexOfReference(_listeners, listener);
        }

        /// <remarks>Only call from inside a lock</remarks>
        internal void Remove(TElem listener)
        {
            Debug.Assert(null != listener, "null listener");

            int index = IndexOfReference(listener);
            Debug.Assert(0 <= index, "listeners don't contain listener");
            _listeners[index] = null;

            if (0 == _listenerReaderCount)
            {
                _listeners.RemoveAt(index);
                _listeners.TrimExcess();
            }
        }

        /// <summary>
        /// Write operation which means user must control multi-thread and we can assume single thread
        /// </summary>
        internal void Notify<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Action<TElem, T1, T2, T3> action)
        {
            Debug.Assert(null != action, "no action");
            Debug.Assert(0 <= _listenerReaderCount, "negative _listEventCount");

            int count = _listeners.Count;
            if (0 < count)
            {
                int nullIndex = -1;

                // protect against listeners shrinking via Remove
                _listenerReaderCount++;
                try
                {
                    // protect against listeners growing via Add since new listeners will already have the Notify in progress
                    for (int i = 0; i < count; ++i)
                    {
                        // protect against listener being set to null (instead of being removed)
                        TElem? listener = _listeners[i];
                        if (_filter(listener))
                        {
                            // perform the action on each listener
                            // some actions may throw an exception blocking remaining listeners from being notified (just like events)
                            action(listener!, arg1, arg2, arg3);
                        }
                        else
                        {
                            _listeners[i] = null;
                            nullIndex = i;
                        }
                    }
                }
                finally
                {
                    _listenerReaderCount--;
                }
                if (0 == _listenerReaderCount)
                {
                    RemoveNullListeners(nullIndex);
                }
            }
        }

        private void RemoveNullListeners(int nullIndex)
        {
            Debug.Assert((-1 == nullIndex) || (null == _listeners[nullIndex]), "non-null listener");
            Debug.Assert(0 == _listenerReaderCount, "0 < _listenerReaderCount");
            for (int i = nullIndex; 0 <= i; --i)
            {
                if (null == _listeners[i])
                {
                    _listeners.RemoveAt(i);
                }
            }
        }
    }
}
