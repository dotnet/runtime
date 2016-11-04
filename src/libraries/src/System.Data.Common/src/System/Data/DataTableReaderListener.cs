// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Data
{
    internal sealed class DataTableReaderListener
    {
        private DataTable _currentDataTable = null;
        private bool _isSubscribed = false;
        private WeakReference _readerWeak;

        internal DataTableReaderListener(DataTableReader reader)
        {
            if (reader == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(DataTableReader));
            }
            if (_currentDataTable != null)
            {
                UnSubscribeEvents();
            }
            _readerWeak = new WeakReference(reader);
            _currentDataTable = reader.CurrentDataTable;
            if (_currentDataTable != null)
            {
                SubscribeEvents();
            }
        }

        internal void CleanUp() => UnSubscribeEvents();

        internal void UpdataTable(DataTable datatable)
        {
            if (datatable == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(DataTable));
            }

            UnSubscribeEvents();
            _currentDataTable = datatable;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_currentDataTable == null)
            {
                return;
            }
            if (_isSubscribed)
            {
                return;
            }
            _currentDataTable.Columns.ColumnPropertyChanged += new CollectionChangeEventHandler(SchemaChanged);
            _currentDataTable.Columns.CollectionChanged += new CollectionChangeEventHandler(SchemaChanged);

            _currentDataTable.RowChanged += new DataRowChangeEventHandler(DataChanged);
            _currentDataTable.RowDeleted += new DataRowChangeEventHandler(DataChanged);

            _currentDataTable.TableCleared += new DataTableClearEventHandler(DataTableCleared);
            _isSubscribed = true;
        }

        private void UnSubscribeEvents()
        {
            if (_currentDataTable == null)
            {
                return;
            }
            if (!_isSubscribed)
            {
                return;
            }

            _currentDataTable.Columns.ColumnPropertyChanged -= new CollectionChangeEventHandler(SchemaChanged);
            _currentDataTable.Columns.CollectionChanged -= new CollectionChangeEventHandler(SchemaChanged);

            _currentDataTable.RowChanged -= new DataRowChangeEventHandler(DataChanged);
            _currentDataTable.RowDeleted -= new DataRowChangeEventHandler(DataChanged);

            _currentDataTable.TableCleared -= new DataTableClearEventHandler(DataTableCleared);
            _isSubscribed = false;
        }

        private void DataTableCleared(object sender, DataTableClearEventArgs e)
        {
            DataTableReader reader = (DataTableReader)_readerWeak.Target;
            if (reader != null)
            {
                reader.DataTableCleared();
            }
            else
            {
                UnSubscribeEvents();
            }
        }

        private void SchemaChanged(object sender, CollectionChangeEventArgs e)
        {
            DataTableReader reader = (DataTableReader)_readerWeak.Target;
            if (reader != null)
            {
                reader.SchemaChanged();
            }
            else
            {
                UnSubscribeEvents();
            }
        }

        private void DataChanged(object sender, DataRowChangeEventArgs args)
        {
            DataTableReader reader = (DataTableReader)_readerWeak.Target;
            if (reader != null)
            {
                reader.DataChanged(args);
            }
            else
            {
                UnSubscribeEvents();
            }
        }
    }
}
