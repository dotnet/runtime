// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Data
{
    internal sealed class RecordManager
    {
        private readonly DataTable _table;

        private int _lastFreeRecord;
        private int _minimumCapacity = 50;
        private int _recordCapacity = 0;
        private readonly List<int> _freeRecordList = new List<int>();

        private DataRow[] _rows;

        internal RecordManager(DataTable table)
        {
            if (table == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(table));
            }
            _table = table;
        }

        private void GrowRecordCapacity()
        {
            RecordCapacity = NewCapacity(_recordCapacity) < NormalizedMinimumCapacity(_minimumCapacity) ?
                NormalizedMinimumCapacity(_minimumCapacity) :
                NewCapacity(_recordCapacity);

            // set up internal map : record --> row
            DataRow[] newRows = _table.NewRowArray(_recordCapacity);
            if (_rows != null)
            {
                Array.Copy(_rows, 0, newRows, 0, Math.Min(_lastFreeRecord, _rows.Length));
            }
            _rows = newRows;
        }

        internal int LastFreeRecord => _lastFreeRecord;

        internal int MinimumCapacity
        {
            get { return _minimumCapacity; }
            set
            {
                if (_minimumCapacity != value)
                {
                    if (value < 0)
                    {
                        throw ExceptionBuilder.NegativeMinimumCapacity();
                    }
                    _minimumCapacity = value;
                }
            }
        }

        internal int RecordCapacity
        {
            get { return _recordCapacity; }
            set
            {
                if (_recordCapacity != value)
                {
                    for (int i = 0; i < _table.Columns.Count; i++)
                    {
                        _table.Columns[i].SetCapacity(value);
                    }
                    _recordCapacity = value;
                }
            }
        }

        internal static int NewCapacity(int capacity) =>
            (capacity < 128) ? 128 : (capacity + capacity);

        // Normalization: 64, 256, 1024, 2k, 3k, ....
        private int NormalizedMinimumCapacity(int capacity)
        {
            if (capacity < 1024 - 10)
            {
                if (capacity < 256 - 10)
                {
                    if (capacity < 54)
                        return 64;
                    return 256;
                }
                return 1024;
            }

            return (((capacity + 10) >> 10) + 1) << 10;
        }
        internal int NewRecordBase()
        {
            int record;
            if (_freeRecordList.Count != 0)
            {
                record = _freeRecordList[_freeRecordList.Count - 1];
                _freeRecordList.RemoveAt(_freeRecordList.Count - 1);
            }
            else
            {
                if (_lastFreeRecord >= _recordCapacity)
                {
                    GrowRecordCapacity();
                }
                record = _lastFreeRecord;
                _lastFreeRecord++;
            }
            Debug.Assert(record >= 0 && record < _recordCapacity, "NewRecord: Invalid record");
            return record;
        }

        internal void FreeRecord(ref int record)
        {
            Debug.Assert(-1 <= record && record < _recordCapacity, "invalid record");
            //            Debug.Assert(record < lastFreeRecord, "Attempt to Free() <outofbounds> record");
            if (-1 != record)
            {
                this[record] = null;

                int count = _table._columnCollection.Count;
                for (int i = 0; i < count; ++i)
                {
                    _table._columnCollection[i].FreeRecord(record);
                }

                // if freeing the last record, recycle it
                if (_lastFreeRecord == record + 1)
                {
                    _lastFreeRecord--;
                }
                else if (record < _lastFreeRecord)
                {
                    _freeRecordList.Add(record);
                }
                record = -1;
            }
        }

        internal void Clear(bool clearAll)
        {
            if (clearAll)
            {
                for (int record = 0; record < _recordCapacity; ++record)
                {
                    _rows[record] = null;
                }
                int count = _table._columnCollection.Count;
                for (int i = 0; i < count; ++i)
                {
                    // this improves performance by caching the column instead of obtaining it for each row
                    DataColumn column = _table._columnCollection[i];
                    for (int record = 0; record < _recordCapacity; ++record)
                    {
                        column.FreeRecord(record);
                    }
                }
                _lastFreeRecord = 0;
                _freeRecordList.Clear();
            }
            else
            { // just clear attached rows
                _freeRecordList.Capacity = _freeRecordList.Count + _table.Rows.Count;
                for (int record = 0; record < _recordCapacity; ++record)
                {
                    if (_rows[record] != null && _rows[record].rowID != -1)
                    {
                        int tempRecord = record;
                        FreeRecord(ref tempRecord);
                    }
                }
            }
        }

        internal DataRow this[int record]
        {
            get
            {
                Debug.Assert(record >= 0 && record < _rows.Length, "Invalid record number");
                return _rows[record];
            }
            set
            {
                Debug.Assert(record >= 0 && record < _rows.Length, "Invalid record number");
                _rows[record] = value;
            }
        }

        internal void SetKeyValues(int record, DataKey key, object[] keyValues)
        {
            for (int i = 0; i < keyValues.Length; i++)
            {
                key.ColumnsReference[i][record] = keyValues[i];
            }
        }

        // Increases AutoIncrementCurrent
        internal int ImportRecord(DataTable src, int record)
        {
            return CopyRecord(src, record, -1);
        }

        // No impact on AutoIncrementCurrent if over written
        internal int CopyRecord(DataTable src, int record, int copy)
        {
            Debug.Assert(src != null, "Can not Merge record without a table");

            if (record == -1)
            {
                return copy;
            }
            int newRecord = -1;
            try
            {
                newRecord = copy == -1 ?
                    _table.NewUninitializedRecord() :
                    copy;

                int count = _table.Columns.Count;
                for (int i = 0; i < count; ++i)
                {
                    DataColumn dstColumn = _table.Columns[i];
                    DataColumn srcColumn = src.Columns[dstColumn.ColumnName];
                    if (null != srcColumn)
                    {
                        object value = srcColumn[record];
                        ICloneable cloneableObject = value as ICloneable;
                        if (null != cloneableObject)
                        {
                            dstColumn[newRecord] = cloneableObject.Clone();
                        }
                        else
                        {
                            dstColumn[newRecord] = value;
                        }
                    }
                    else if (-1 == copy)
                    {
                        dstColumn.Init(newRecord);
                    }
                }
            }
            catch (Exception e) when (Common.ADP.IsCatchableOrSecurityExceptionType(e))
            {
                if (-1 == copy)
                {
                    FreeRecord(ref newRecord);
                }
                throw;
            }
            return newRecord;
        }

        internal void SetRowCache(DataRow[] newRows)
        {
            _rows = newRows;
            _lastFreeRecord = _rows.Length;
            _recordCapacity = _lastFreeRecord;
        }

        [Conditional("DEBUG")]
        internal void VerifyRecord(int record)
        {
            Debug.Assert((record < _lastFreeRecord) && (-1 == _freeRecordList.IndexOf(record)), "accessing free record");
            Debug.Assert((null == _rows[record]) ||
                         (record == _rows[record]._oldRecord) ||
                         (record == _rows[record]._newRecord) ||
                         (record == _rows[record]._tempRecord), "record of a different row");
        }

        [Conditional("DEBUG")]
        internal void VerifyRecord(int record, DataRow row)
        {
            Debug.Assert((record < _lastFreeRecord) && (-1 == _freeRecordList.IndexOf(record)), "accessing free record");
            Debug.Assert((null == _rows[record]) || (row == _rows[record]), "record of a different row");
        }
    }
}
