// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    [TypeConverter((typeof(ExpandableObjectConverter)))]
    public class DataViewSetting
    {
        private DataViewManager? _dataViewManager;
        private DataTable? _table;
        private string _sort = string.Empty;
        private string _rowFilter = string.Empty;
        private DataViewRowState _rowStateFilter = DataViewRowState.CurrentRows;
        private bool _applyDefaultSort;

        internal DataViewSetting() { }

        public bool ApplyDefaultSort
        {
            get { return _applyDefaultSort; }
            set
            {
                if (_applyDefaultSort != value)
                {
                    _applyDefaultSort = value;
                }
            }
        }

        [Browsable(false)]
        public DataViewManager? DataViewManager => _dataViewManager;

        internal void SetDataViewManager(DataViewManager dataViewManager)
        {
            if (_dataViewManager != dataViewManager)
            {
                _dataViewManager = dataViewManager;
            }
        }

        [Browsable(false)]
        public DataTable? Table => _table;

        internal void SetDataTable(DataTable table)
        {
            if (_table != table)
            {
                _table = table;
            }
        }

        [AllowNull]
        public string RowFilter
        {
            get { return _rowFilter; }
            [RequiresUnreferencedCode(Select.RequiresUnreferencedCodeMessage)]
            set
            {
                value ??= string.Empty;

                if (_rowFilter != value)
                {
                    _rowFilter = value;
                }
            }
        }

        public DataViewRowState RowStateFilter
        {
            get { return _rowStateFilter; }
            set
            {
                if (_rowStateFilter != value)
                {
                    _rowStateFilter = value;
                }
            }
        }

        [AllowNull]
        public string Sort
        {
            get { return _sort; }
            set
            {
                value ??= string.Empty;

                if (_sort != value)
                {
                    _sort = value;
                }
            }
        }
    }
}
