// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Data
{
    public sealed class DataRowCollection : InternalDataCollectionBase
    {
        private sealed class DataRowTree : RBTree<DataRow>
        {
            internal DataRowTree() : base(TreeAccessMethod.INDEX_ONLY) { }

            protected override int CompareNode(DataRow? record1, DataRow? record2)
            {
                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.CompareNodeInDataRowTree);
            }
            protected override int CompareSatelliteTreeNode(DataRow? record1, DataRow? record2)
            {
                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.CompareSatelliteTreeNodeInDataRowTree);
            }
        }

        private readonly DataTable _table;
        private readonly DataRowTree _list = new DataRowTree();
        internal int _nullInList;

        /// <summary>
        /// Creates the DataRowCollection for the given table.
        /// </summary>
        internal DataRowCollection(DataTable table)
        {
            _table = table;
        }

        public override int Count => _list.Count;

        /// <summary>
        /// Gets the row at the specified index.
        /// </summary>
        public DataRow this[int index] => _list[index];

        /// <summary>
        /// Adds the specified <see cref='System.Data.DataRow'/> to the <see cref='System.Data.DataRowCollection'/> object.
        /// </summary>
        public void Add(DataRow row) => _table.AddRow(row, -1);

        public void InsertAt(DataRow row, int pos)
        {
            if (pos < 0)
            {
                throw ExceptionBuilder.RowInsertOutOfRange(pos);
            }

            if (pos >= _list.Count)
            {
                _table.AddRow(row, -1);
            }
            else
            {
                _table.InsertRow(row, -1, pos);
            }
        }

        internal void DiffInsertAt(DataRow row, int pos)
        {
            if ((pos < 0) || (pos == _list.Count))
            {
                _table.AddRow(row, pos > -1 ? pos + 1 : -1);
                return;
            }

            if (_table.NestedParentRelations.Length > 0)
            { // get in this trouble only if  table has a nested parent
              // get into trouble if table has JUST a nested parent? how about multi parent!
                if (pos < _list.Count)
                {
                    if (_list[pos] != null)
                    {
                        throw ExceptionBuilder.RowInsertTwice(pos, _table.TableName);
                    }
                    _list.RemoveAt(pos);
                    _nullInList--;
                    _table.InsertRow(row, pos + 1, pos);
                }
                else
                {
                    while (pos > _list.Count)
                    {
                        _list.Add(null);
                        _nullInList++;
                    }
                    _table.AddRow(row, pos + 1);
                }
            }
            else
            {
                _table.InsertRow(row, pos + 1, pos > _list.Count ? -1 : pos);
            }
        }

        public int IndexOf(DataRow? row) => (null == row) || (row.Table != _table) || ((0 == row.RBTreeNodeId) && (row.RowState == DataRowState.Detached)) ?
            -1 :
            _list.IndexOf(row.RBTreeNodeId, row);

        /// <summary>
        /// Creates a row using specified values and adds it to the <see cref='System.Data.DataRowCollection'/>.
        /// </summary>
        internal DataRow AddWithColumnEvents(params object[] values)
        {
            DataRow row = _table.NewRow(-1);
            row.ItemArray = values;
            _table.AddRow(row, -1);
            return row;
        }

        public DataRow Add(params object?[] values)
        {
            int record = _table.NewRecordFromArray(values);
            DataRow row = _table.NewRow(record);
            _table.AddRow(row, -1);
            return row;
        }

        internal void ArrayAdd(DataRow row) => row.RBTreeNodeId = _list.Add(row);

        internal void ArrayInsert(DataRow row, int pos) => row.RBTreeNodeId = _list.Insert(pos, row);

        internal void ArrayClear() => _list.Clear();

        internal void ArrayRemove(DataRow row)
        {
            if (row.RBTreeNodeId == 0)
            {
                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.AttachedNodeWithZerorbTreeNodeId);
            }
            _list.RBDelete(row.RBTreeNodeId);
            row.RBTreeNodeId = 0;
        }

        /// <summary>
        /// Gets the row specified by the primary key value.
        /// </summary>
        public DataRow? Find(object? key) => _table.FindByPrimaryKey(key);

        /// <summary>
        /// Gets the row containing the specified primary key values.
        /// </summary>
        public DataRow? Find(object?[] keys) => _table.FindByPrimaryKey(keys);

        /// <summary>
        /// Clears the collection of all rows.
        /// </summary>
        public void Clear() => _table.Clear(false);

        /// <summary>
        /// Gets a value indicating whether the primary key of any row in the
        /// collection contains the specified value.
        /// </summary>
        public bool Contains(object? key) => (_table.FindByPrimaryKey(key) != null);

        /// <summary>
        /// Gets a value indicating if the <see cref='System.Data.DataRow'/> with
        /// the specified primary key values exists.
        /// </summary>
        public bool Contains(object?[] keys) => (_table.FindByPrimaryKey(keys) != null);

        public override void CopyTo(Array ar, int index) => _list.CopyTo(ar, index);

        public void CopyTo(DataRow[] array, int index) => _list.CopyTo(array, index);

        public override IEnumerator GetEnumerator() => _list.GetEnumerator();

        /// <summary>
        /// Removes the specified <see cref='System.Data.DataRow'/> from the collection.
        /// </summary>
        public void Remove(DataRow row)
        {
            if ((null == row) || (row.Table != _table) || (-1 == row.rowID))
            {
                throw ExceptionBuilder.RowOutOfRange();
            }

            if ((row.RowState != DataRowState.Deleted) && (row.RowState != DataRowState.Detached))
            {
                row.Delete();
            }

            if (row.RowState != DataRowState.Detached)
            {
                row.AcceptChanges();
            }
        }

        /// <summary>
        /// Removes the row with the specified index from the collection.
        /// </summary>
        public void RemoveAt(int index) => Remove(this[index]);
    }
}
