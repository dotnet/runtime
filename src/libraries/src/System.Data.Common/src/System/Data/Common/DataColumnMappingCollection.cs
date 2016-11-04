// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Data.Common
{
    public sealed class DataColumnMappingCollection : MarshalByRefObject, IColumnMappingCollection
    {
        private List<DataColumnMapping> _items; // delay creation until AddWithoutEvents, Insert, CopyTo, GetEnumerator

        public DataColumnMappingCollection()
        {
        }

        // explicit ICollection implementation
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }
        object ICollection.SyncRoot
        {
            get { return this; }
        }

        // explicit IList implementation
        bool IList.IsReadOnly
        {
            get { return false; }
        }
        bool IList.IsFixedSize
        {
            get { return false; }
        }
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                ValidateType(value);
                this[index] = (DataColumnMapping)value;
            }
        }

        // explicit IColumnMappingCollection implementation
        object IColumnMappingCollection.this[string index]
        {
            get
            {
                return this[index];
            }
            set
            {
                ValidateType(value);
                this[index] = (DataColumnMapping)value;
            }
        }
        IColumnMapping IColumnMappingCollection.Add(string sourceColumnName, string dataSetColumnName)
        {
            return Add(sourceColumnName, dataSetColumnName);
        }
        IColumnMapping IColumnMappingCollection.GetByDataSetColumn(string dataSetColumnName)
        {
            return GetByDataSetColumn(dataSetColumnName);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Count
        {
            get
            {
                return ((null != _items) ? _items.Count : 0);
            }
        }

        private Type ItemType
        {
            get { return typeof(DataColumnMapping); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataColumnMapping this[int index]
        {
            get
            {
                RangeCheck(index);
                return _items[index];
            }
            set
            {
                RangeCheck(index);
                Replace(index, value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataColumnMapping this[string sourceColumn]
        {
            get
            {
                int index = RangeCheck(sourceColumn);
                return _items[index];
            }
            set
            {
                int index = RangeCheck(sourceColumn);
                Replace(index, value);
            }
        }

        public int Add(object value)
        {
            ValidateType(value);
            Add((DataColumnMapping)value);
            return Count - 1;
        }

        private DataColumnMapping Add(DataColumnMapping value)
        {
            AddWithoutEvents(value);
            return value;
        }

        public DataColumnMapping Add(string sourceColumn, string dataSetColumn)
        {
            return Add(new DataColumnMapping(sourceColumn, dataSetColumn));
        }

        public void AddRange(DataColumnMapping[] values)
        {
            AddEnumerableRange(values, false);
        }

        public void AddRange(System.Array values)
        {
            AddEnumerableRange(values, false);
        }

        /*/// <include file='doc\DataColumnMappingCollection.uex' path='docs/doc[@for="DataColumnMappingCollection.AddCloneOfRange"]/*' />
        public void AddCloneOfRange(IEnumerable values) {
            AddEnumerableRange(values, true);
        }*/

        private void AddEnumerableRange(IEnumerable values, bool doClone)
        {
            if (null == values)
            {
                throw ADP.ArgumentNull(nameof(values));
            }
            foreach (object value in values)
            {
                ValidateType(value);
            }
            if (doClone)
            {
                foreach (ICloneable value in values)
                {
                    AddWithoutEvents(value.Clone() as DataColumnMapping);
                }
            }
            else
            {
                foreach (DataColumnMapping value in values)
                {
                    AddWithoutEvents(value);
                }
            }
        }

        private void AddWithoutEvents(DataColumnMapping value)
        {
            Validate(-1, value);
            value.Parent = this;
            ArrayList().Add(value);
        }

        // implemented as a method, not as a property because the VS7 debugger
        // object browser calls properties to display their value, and we want this delayed
        private List<DataColumnMapping> ArrayList()
        {
            if (null == _items)
            {
                _items = new List<DataColumnMapping>();
            }
            return _items;
        }

        public void Clear()
        {
            if (0 < Count)
            {
                ClearWithoutEvents();
            }
        }

        private void ClearWithoutEvents()
        {
            if (null != _items)
            {
                foreach (DataColumnMapping item in _items)
                {
                    item.Parent = null;
                }
                _items.Clear();
            }
        }

        public bool Contains(string value)
        {
            return (-1 != IndexOf(value));
        }

        public bool Contains(object value)
        {
            return (-1 != IndexOf(value));
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)ArrayList()).CopyTo(array, index);
        }

        public void CopyTo(DataColumnMapping[] array, int index)
        {
            ArrayList().CopyTo(array, index);
        }

        public DataColumnMapping GetByDataSetColumn(string value)
        {
            int index = IndexOfDataSetColumn(value);
            if (0 > index)
            {
                throw ADP.ColumnsDataSetColumn(value);
            }
            return _items[index];
        }

        public IEnumerator GetEnumerator()
        {
            return ArrayList().GetEnumerator();
        }

        public int IndexOf(object value)
        {
            if (null != value)
            {
                ValidateType(value);
                for (int i = 0; i < Count; ++i)
                {
                    if (_items[i] == value)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public int IndexOf(string sourceColumn)
        {
            if (!string.IsNullOrEmpty(sourceColumn))
            {
                int count = Count;
                for (int i = 0; i < count; ++i)
                {
                    if (0 == ADP.SrcCompare(sourceColumn, _items[i].SourceColumn))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public int IndexOfDataSetColumn(string dataSetColumn)
        {
            if (!string.IsNullOrEmpty(dataSetColumn))
            {
                int count = Count;
                for (int i = 0; i < count; ++i)
                {
                    if (0 == ADP.DstCompare(dataSetColumn, _items[i].DataSetColumn))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public void Insert(int index, object value)
        {
            ValidateType(value);
            Insert(index, (DataColumnMapping)value);
        }

        public void Insert(int index, DataColumnMapping value)
        {
            if (null == value)
            {
                throw ADP.ColumnsAddNullAttempt(nameof(value));
            }
            Validate(-1, value);
            value.Parent = this;
            ArrayList().Insert(index, value);
        }

        private void RangeCheck(int index)
        {
            if ((index < 0) || (Count <= index))
            {
                throw ADP.ColumnsIndexInt32(index, this);
            }
        }

        private int RangeCheck(string sourceColumn)
        {
            int index = IndexOf(sourceColumn);
            if (index < 0)
            {
                throw ADP.ColumnsIndexSource(sourceColumn);
            }
            return index;
        }

        public void RemoveAt(int index)
        {
            RangeCheck(index);
            RemoveIndex(index);
        }

        public void RemoveAt(string sourceColumn)
        {
            int index = RangeCheck(sourceColumn);
            RemoveIndex(index);
        }

        private void RemoveIndex(int index)
        {
            Debug.Assert((null != _items) && (0 <= index) && (index < Count), "RemoveIndex, invalid");
            _items[index].Parent = null;
            _items.RemoveAt(index);
        }

        public void Remove(object value)
        {
            ValidateType(value);
            Remove((DataColumnMapping)value);
        }

        public void Remove(DataColumnMapping value)
        {
            if (null == value)
            {
                throw ADP.ColumnsAddNullAttempt(nameof(value));
            }
            int index = IndexOf(value);

            if (-1 != index)
            {
                RemoveIndex(index);
            }
            else
            {
                throw ADP.CollectionRemoveInvalidObject(ItemType, this);
            }
        }

        private void Replace(int index, DataColumnMapping newValue)
        {
            Debug.Assert((null != _items) && (0 <= index) && (index < Count), "RemoveIndex, invalid");
            Validate(index, newValue);
            _items[index].Parent = null;
            newValue.Parent = this;
            _items[index] = newValue;
        }

        private void ValidateType(object value)
        {
            if (null == value)
            {
                throw ADP.ColumnsAddNullAttempt(nameof(value));
            }
            else if (!ItemType.IsInstanceOfType(value))
            {
                throw ADP.NotADataColumnMapping(value);
            }
        }

        private void Validate(int index, DataColumnMapping value)
        {
            if (null == value)
            {
                throw ADP.ColumnsAddNullAttempt(nameof(value));
            }
            if (null != value.Parent)
            {
                if (this != value.Parent)
                {
                    throw ADP.ColumnsIsNotParent(this);
                }
                else if (index != IndexOf(value))
                {
                    throw ADP.ColumnsIsParent(this);
                }
            }

            string name = value.SourceColumn;
            if (string.IsNullOrEmpty(name))
            {
                index = 1;
                do
                {
                    name = ADP.SourceColumn + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    index++;
                } while (-1 != IndexOf(name));
                value.SourceColumn = name;
            }
            else
            {
                ValidateSourceColumn(index, name);
            }
        }

        internal void ValidateSourceColumn(int index, string value)
        {
            int pindex = IndexOf(value);
            if ((-1 != pindex) && (index != pindex))
            { // must be non-null and unique
                throw ADP.ColumnsUniqueSourceColumn(value);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static DataColumn GetDataColumn(DataColumnMappingCollection columnMappings, string sourceColumn, Type dataType, DataTable dataTable, MissingMappingAction mappingAction, MissingSchemaAction schemaAction)
        {
            if (null != columnMappings)
            {
                int index = columnMappings.IndexOf(sourceColumn);
                if (-1 != index)
                {
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceInfo)
                    {
                        Debug.WriteLine($"mapping match on SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    return columnMappings._items[index].GetDataColumnBySchemaAction(dataTable, dataType, schemaAction);
                }
            }
            if (string.IsNullOrEmpty(sourceColumn))
            {
                throw ADP.InvalidSourceColumn(nameof(sourceColumn));
            }
            switch (mappingAction)
            {
                case MissingMappingAction.Passthrough:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceInfo)
                    {
                        Debug.WriteLine($"mapping passthrough of SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    return DataColumnMapping.GetDataColumnBySchemaAction(sourceColumn, sourceColumn, dataTable, dataType, schemaAction);

                case MissingMappingAction.Ignore:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceWarning)
                    {
                        Debug.WriteLine($"mapping filter of SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    return null;

                case MissingMappingAction.Error:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceError)
                    {
                        Debug.WriteLine($"mapping error on SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    throw ADP.MissingColumnMapping(sourceColumn);
            }
            throw ADP.InvalidMissingMappingAction(mappingAction);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static DataColumnMapping GetColumnMappingBySchemaAction(DataColumnMappingCollection columnMappings, string sourceColumn, MissingMappingAction mappingAction)
        {
            if (null != columnMappings)
            {
                int index = columnMappings.IndexOf(sourceColumn);
                if (-1 != index)
                {
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceInfo)
                    {
                        Debug.WriteLine($"mapping match on SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    return columnMappings._items[index];
                }
            }
            if (string.IsNullOrEmpty(sourceColumn))
            {
                throw ADP.InvalidSourceColumn(nameof(sourceColumn));
            }
            switch (mappingAction)
            {
                case MissingMappingAction.Passthrough:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceInfo)
                    {
                        Debug.WriteLine($"mapping passthrough of SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    return new DataColumnMapping(sourceColumn, sourceColumn);

                case MissingMappingAction.Ignore:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceWarning)
                    {
                        Debug.WriteLine($"mapping filter of SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    return null;

                case MissingMappingAction.Error:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceError)
                    {
                        Debug.WriteLine($"mapping error on SourceColumn \"{sourceColumn}\"");
                    }
#endif
                    throw ADP.MissingColumnMapping(sourceColumn);
            }
            throw ADP.InvalidMissingMappingAction(mappingAction);
        }
    }
}
