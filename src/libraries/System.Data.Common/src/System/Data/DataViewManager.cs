// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Data
{
    public class DataViewManager : MarshalByValueComponent, IBindingList, System.ComponentModel.ITypedList
    {
        private DataViewSettingCollection _dataViewSettingsCollection;
        private DataSet _dataSet;
        private DataViewManagerListItemTypeDescriptor _item;
        private bool _locked;
        internal int _nViews = 0;

        private static NotSupportedException s_notSupported = new NotSupportedException();

        public DataViewManager() : this(null, false) { }

        public DataViewManager(DataSet dataSet) : this(dataSet, false) { }

        internal DataViewManager(DataSet dataSet, bool locked)
        {
            GC.SuppressFinalize(this);
            _dataSet = dataSet;
            if (_dataSet != null)
            {
                _dataSet.Tables.CollectionChanged += new CollectionChangeEventHandler(TableCollectionChanged);
                _dataSet.Relations.CollectionChanged += new CollectionChangeEventHandler(RelationCollectionChanged);
            }
            _locked = locked;
            _item = new DataViewManagerListItemTypeDescriptor(this);
            _dataViewSettingsCollection = new DataViewSettingCollection(this);
        }

        [DefaultValue(null)]
        public DataSet DataSet
        {
            get { return _dataSet; }
            set
            {
                if (value == null)
                {
                    throw ExceptionBuilder.SetFailed("DataSet to null");
                }

                if (_locked)
                {
                    throw ExceptionBuilder.SetDataSetFailed();
                }

                if (_dataSet != null)
                {
                    if (_nViews > 0)
                    {
                        throw ExceptionBuilder.CanNotSetDataSet();
                    }

                    _dataSet.Tables.CollectionChanged -= new CollectionChangeEventHandler(TableCollectionChanged);
                    _dataSet.Relations.CollectionChanged -= new CollectionChangeEventHandler(RelationCollectionChanged);
                }

                _dataSet = value;
                _dataSet.Tables.CollectionChanged += new CollectionChangeEventHandler(TableCollectionChanged);
                _dataSet.Relations.CollectionChanged += new CollectionChangeEventHandler(RelationCollectionChanged);
                _dataViewSettingsCollection = new DataViewSettingCollection(this);
                _item.Reset();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public DataViewSettingCollection DataViewSettings => _dataViewSettingsCollection;

        public string DataViewSettingCollectionString
        {
            get
            {
                if (_dataSet == null)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                builder.Append("<DataViewSettingCollectionString>");
                foreach (DataTable dt in _dataSet.Tables)
                {
                    DataViewSetting ds = _dataViewSettingsCollection[dt];
                    builder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "<{0} Sort=\"{1}\" RowFilter=\"{2}\" RowStateFilter=\"{3}\"/>", dt.EncodedTableName, ds.Sort, ds.RowFilter, ds.RowStateFilter);
                }
                builder.Append("</DataViewSettingCollectionString>");
                return builder.ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                var r = new XmlTextReader(new StringReader(value));
                r.WhitespaceHandling = WhitespaceHandling.None;
                r.Read();
                if (r.Name != "DataViewSettingCollectionString")
                {
                    throw ExceptionBuilder.SetFailed(nameof(DataViewSettingCollectionString));
                }

                while (r.Read())
                {
                    if (r.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    string table = XmlConvert.DecodeName(r.LocalName);
                    if (r.MoveToAttribute("Sort"))
                    {
                        _dataViewSettingsCollection[table].Sort = r.Value;
                    }
                    if (r.MoveToAttribute("RowFilter"))
                    {
                        _dataViewSettingsCollection[table].RowFilter = r.Value;
                    }
                    if (r.MoveToAttribute("RowStateFilter"))
                    {
                        _dataViewSettingsCollection[table].RowStateFilter = (DataViewRowState)Enum.Parse(typeof(DataViewRowState), r.Value);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var items = new DataViewManagerListItemTypeDescriptor[1];
            ((ICollection)this).CopyTo(items, 0);
            return items.GetEnumerator();
        }

        int ICollection.Count => 1;

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized => false;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => true;

        void ICollection.CopyTo(Array array, int index)
        {
            array.SetValue(new DataViewManagerListItemTypeDescriptor(this), index);
        }

        object IList.this[int index]
        {
            get { return _item; }
            set { throw ExceptionBuilder.CannotModifyCollection(); }
        }

        int IList.Add(object value)
        {
            throw ExceptionBuilder.CannotModifyCollection();
        }

        void IList.Clear()
        {
            throw ExceptionBuilder.CannotModifyCollection();
        }

        bool IList.Contains(object value) => (value == _item);

        int IList.IndexOf(object value) => (value == _item) ? 1 : -1;

        void IList.Insert(int index, object value)
        {
            throw ExceptionBuilder.CannotModifyCollection();
        }

        void IList.Remove(object value)
        {
            throw ExceptionBuilder.CannotModifyCollection();
        }

        void IList.RemoveAt(int index)
        {
            throw ExceptionBuilder.CannotModifyCollection();
        }

        // ------------- IBindingList: ---------------------------

        bool IBindingList.AllowNew => false;
        object IBindingList.AddNew()
        {
            throw s_notSupported;
        }

        bool IBindingList.AllowEdit => false;

        bool IBindingList.AllowRemove => false;

        bool IBindingList.SupportsChangeNotification => true;

        bool IBindingList.SupportsSearching => false;

        bool IBindingList.SupportsSorting => false;

        bool IBindingList.IsSorted
        {
            get { throw s_notSupported; }
        }

        PropertyDescriptor IBindingList.SortProperty
        {
            get { throw s_notSupported; }
        }

        ListSortDirection IBindingList.SortDirection
        {
            get { throw s_notSupported; }
        }

        public event System.ComponentModel.ListChangedEventHandler ListChanged;

        void IBindingList.AddIndex(PropertyDescriptor property)
        {
            // no operation
        }

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw s_notSupported;
        }

        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            throw s_notSupported;
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
            // no operation
        }

        void IBindingList.RemoveSort()
        {
            throw s_notSupported;
        }

        // SDUB: GetListName and GetItemProperties almost the same in DataView and DataViewManager
        string System.ComponentModel.ITypedList.GetListName(PropertyDescriptor[] listAccessors)
        {
            DataSet dataSet = DataSet;
            if (dataSet == null)
            {
                throw ExceptionBuilder.CanNotUseDataViewManager();
            }

            if (listAccessors == null || listAccessors.Length == 0)
            {
                return dataSet.DataSetName;
            }
            else
            {
                DataTable table = dataSet.FindTable(null, listAccessors, 0);
                if (table != null)
                {
                    return table.TableName;
                }
            }
            return string.Empty;
        }

        PropertyDescriptorCollection System.ComponentModel.ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            DataSet dataSet = DataSet;
            if (dataSet == null)
            {
                throw ExceptionBuilder.CanNotUseDataViewManager();
            }

            if (listAccessors == null || listAccessors.Length == 0)
            {
                return ((ICustomTypeDescriptor)(new DataViewManagerListItemTypeDescriptor(this))).GetProperties();
            }
            else
            {
                DataTable table = dataSet.FindTable(null, listAccessors, 0);
                if (table != null)
                {
                    return table.GetPropertyDescriptorCollection(null);
                }
            }
            return new PropertyDescriptorCollection(null);
        }

        public DataView CreateDataView(DataTable table)
        {
            if (_dataSet == null)
            {
                throw ExceptionBuilder.CanNotUseDataViewManager();
            }

            DataView dataView = new DataView(table);
            dataView.SetDataViewManager(this);
            return dataView;
        }

        protected virtual void OnListChanged(ListChangedEventArgs e)
        {
            try
            {
                ListChanged?.Invoke(this, e);
            }
            catch (Exception f) when (Common.ADP.IsCatchableExceptionType(f))
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(f);
                // ignore the exception
            }
        }

        protected virtual void TableCollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            PropertyDescriptor NullProp = null;
            OnListChanged(
                e.Action == CollectionChangeAction.Add ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataTablePropertyDescriptor((System.Data.DataTable)e.Element)) :
                e.Action == CollectionChangeAction.Refresh ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, NullProp) :
                e.Action == CollectionChangeAction.Remove ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataTablePropertyDescriptor((System.Data.DataTable)e.Element)) :
                /*default*/ null
            );
        }

        protected virtual void RelationCollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            DataRelationPropertyDescriptor NullProp = null;
            OnListChanged(
                e.Action == CollectionChangeAction.Add ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataRelationPropertyDescriptor((System.Data.DataRelation)e.Element)) :
                e.Action == CollectionChangeAction.Refresh ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, NullProp) :
                e.Action == CollectionChangeAction.Remove ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataRelationPropertyDescriptor((System.Data.DataRelation)e.Element)) :
            /*default*/ null
            );
        }
    }
}
