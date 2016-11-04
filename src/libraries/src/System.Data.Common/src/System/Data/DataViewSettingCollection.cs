// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Collections;

namespace System.Data
{
    public class DataViewSettingCollection : ICollection
    {
        private readonly DataViewManager _dataViewManager;
        private readonly Hashtable _list = new Hashtable();

        internal DataViewSettingCollection(DataViewManager dataViewManager)
        {
            if (dataViewManager == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(dataViewManager));
            }
            _dataViewManager = dataViewManager;
        }

        public virtual DataViewSetting this[DataTable table]
        {
            get
            {
                if (table == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(table));
                }
                DataViewSetting dataViewSetting = (DataViewSetting)_list[table];
                if (dataViewSetting == null)
                {
                    dataViewSetting = new DataViewSetting();
                    this[table] = dataViewSetting;
                }
                return dataViewSetting;
            }
            set
            {
                if (table == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(table));
                }
                value.SetDataViewManager(_dataViewManager);
                value.SetDataTable(table);
                _list[table] = value;
            }
        }

        private DataTable GetTable(string tableName)
        {
            DataTable dt = null;
            DataSet ds = _dataViewManager.DataSet;
            if (ds != null)
            {
                dt = ds.Tables[tableName];
            }
            return dt;
        }

        private DataTable GetTable(int index)
        {
            DataTable dt = null;
            DataSet ds = _dataViewManager.DataSet;
            if (ds != null)
            {
                dt = ds.Tables[index];
            }
            return dt;
        }

        public virtual DataViewSetting this[string tableName]
        {
            get
            {
                DataTable dt = GetTable(tableName);
                if (dt != null)
                {
                    return this[dt];
                }
                return null;
            }
        }

        public virtual DataViewSetting this[int index]
        {
            get
            {
                DataTable dt = GetTable(index);
                if (dt != null)
                {
                    return this[dt];
                }
                return null;
            }
            set
            {
                DataTable dt = GetTable(index);
                if (dt != null)
                {
                    this[dt] = value;
                }
            }
        }

        public void CopyTo(Array ar, int index)
        {
            IEnumerator Enumerator = GetEnumerator();
            while (Enumerator.MoveNext())
            {
                ar.SetValue(Enumerator.Current, index++);
            }
        }

        public void CopyTo(DataViewSetting[] ar, int index)
        {
            IEnumerator Enumerator = GetEnumerator();
            while (Enumerator.MoveNext())
            {
                ar.SetValue(Enumerator.Current, index++);
            }
        }

        [Browsable(false)]
        public virtual int Count
        {
            get
            {
                DataSet ds = _dataViewManager.DataSet;
                return (ds == null) ? 0 : ds.Tables.Count;
            }
        }

        public IEnumerator GetEnumerator()
        {
            // I have to do something here.
            return new DataViewSettingsEnumerator(_dataViewManager);
        }

        [Browsable(false)]
        public bool IsReadOnly => true;

        [Browsable(false)]
        public bool IsSynchronized => false; // so the user will know that it has to lock this object

        [Browsable(false)]
        public object SyncRoot => this;

        internal void Remove(DataTable table)
        {
            _list.Remove(table);
        }

        private sealed class DataViewSettingsEnumerator : IEnumerator
        {
            private DataViewSettingCollection _dataViewSettings;
            private IEnumerator _tableEnumerator;
            public DataViewSettingsEnumerator(DataViewManager dvm)
            {
                DataSet ds = dvm.DataSet;
                if (ds != null)
                {
                    _dataViewSettings = dvm.DataViewSettings;
                    _tableEnumerator = dvm.DataSet.Tables.GetEnumerator();
                }
                else
                {
                    _dataViewSettings = null;
                    _tableEnumerator = Array.Empty<DataTable>().GetEnumerator();
                }
            }
            public bool MoveNext() => _tableEnumerator.MoveNext();

            public void Reset() => _tableEnumerator.Reset();

            public object Current => _dataViewSettings[(DataTable)_tableEnumerator.Current];
        }
    }
}
