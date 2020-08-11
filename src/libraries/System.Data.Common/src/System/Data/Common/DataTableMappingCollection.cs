// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    [ListBindable(false)]
    public sealed class DataTableMappingCollection : MarshalByRefObject, ITableMappingCollection
    {
        private List<DataTableMapping>? _items; // delay creation until AddWithoutEvents, Insert, CopyTo, GetEnumerator

        public DataTableMappingCollection() { }

        // explicit ICollection implementation
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        // explicit IList implementation
        bool IList.IsReadOnly => false;
        bool IList.IsFixedSize => false;
        object? IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                ValidateType(value);
                this[index] = (DataTableMapping)value;
            }
        }

        object ITableMappingCollection.this[string index]
        {
            get { return this[index]; }
            set
            {
                ValidateType(value);
                this[index] = (DataTableMapping)value;
            }
        }
        ITableMapping ITableMappingCollection.Add(string sourceTableName, string dataSetTableName) =>
            Add(sourceTableName, dataSetTableName);

        ITableMapping ITableMappingCollection.GetByDataSetTable(string dataSetTableName) =>
            GetByDataSetTable(dataSetTableName);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Count => (_items != null) ? _items.Count : 0;

        private Type ItemType => typeof(DataTableMapping);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTableMapping this[int index]
        {
            get
            {
                RangeCheck(index);
                return _items![index];
            }
            set
            {
                RangeCheck(index);
                Replace(index, value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTableMapping this[string sourceTable]
        {
            get
            {
                int index = RangeCheck(sourceTable);
                return _items![index];
            }
            set
            {
                int index = RangeCheck(sourceTable);
                Replace(index, value);
            }
        }

        public int Add(object? value)
        {
            ValidateType(value);
            Add((DataTableMapping)value);
            return Count - 1;
        }

        private DataTableMapping Add(DataTableMapping value)
        {
            AddWithoutEvents(value);
            return value;
        }

        public void AddRange(DataTableMapping[] values) => AddEnumerableRange(values, false);

        public void AddRange(System.Array values) => AddEnumerableRange(values, false);

        private void AddEnumerableRange(IEnumerable values, bool doClone)
        {
            if (values == null)
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
                    AddWithoutEvents(value.Clone() as DataTableMapping);
                }
            }
            else
            {
                foreach (DataTableMapping value in values)
                {
                    AddWithoutEvents(value);
                }
            }
        }

        public DataTableMapping Add(string? sourceTable, string? dataSetTable) =>
            Add(new DataTableMapping(sourceTable, dataSetTable));

        private void AddWithoutEvents(DataTableMapping? value)
        {
            Validate(-1, value);
            value.Parent = this;
            ArrayList().Add(value);
        }

        // implemented as a method, not as a property because the VS7 debugger
        // object browser calls properties to display their value, and we want this delayed
        private List<DataTableMapping> ArrayList() => _items ?? (_items = new List<DataTableMapping>());

        public void Clear()
        {
            if (Count > 0)
            {
                ClearWithoutEvents();
            }
        }

        private void ClearWithoutEvents()
        {
            if (_items != null)
            {
                foreach (DataTableMapping item in _items)
                {
                    item.Parent = null;
                }
                _items.Clear();
            }
        }

        public bool Contains(string? value) => (IndexOf(value) != -1);

        public bool Contains(object? value) => (IndexOf(value) != -1);

        public void CopyTo(Array array, int index) => ((ICollection)ArrayList()).CopyTo(array, index);

        public void CopyTo(DataTableMapping[] array, int index) => ArrayList().CopyTo(array, index);

        public DataTableMapping GetByDataSetTable(string dataSetTable)
        {
            int index = IndexOfDataSetTable(dataSetTable);
            if (index < 0)
            {
                throw ADP.TablesDataSetTable(dataSetTable);
            }
            return _items![index];
        }

        public IEnumerator GetEnumerator() => ArrayList().GetEnumerator();

        public int IndexOf(object? value)
        {
            if (value != null)
            {
                ValidateType(value);
                for (int i = 0; i < Count; ++i)
                {
                    if (_items![i] == value)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public int IndexOf(string? sourceTable)
        {
            if (!string.IsNullOrEmpty(sourceTable))
            {
                for (int i = 0; i < Count; ++i)
                {
                    string value = _items![i].SourceTable;
                    if ((value != null) && (ADP.SrcCompare(sourceTable, value) == 0))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public int IndexOfDataSetTable(string? dataSetTable)
        {
            if (!string.IsNullOrEmpty(dataSetTable))
            {
                for (int i = 0; i < Count; ++i)
                {
                    string value = _items![i].DataSetTable;
                    if ((value != null) && (ADP.DstCompare(dataSetTable, value) == 0))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public void Insert(int index, object? value)
        {
            ValidateType(value);
            Insert(index, (DataTableMapping)value);
        }

        public void Insert(int index, DataTableMapping value)
        {
            if (value == null)
            {
                throw ADP.TablesAddNullAttempt(nameof(value));
            }
            Validate(-1, value);
            value.Parent = this;
            ArrayList().Insert(index, value);
        }

        private void RangeCheck(int index)
        {
            if ((index < 0) || (Count <= index))
            {
                throw ADP.TablesIndexInt32(index, this);
            }
        }

        private int RangeCheck(string sourceTable)
        {
            int index = IndexOf(sourceTable);
            if (index < 0)
            {
                throw ADP.TablesSourceIndex(sourceTable);
            }
            return index;
        }

        public void RemoveAt(int index)
        {
            RangeCheck(index);
            RemoveIndex(index);
        }

        public void RemoveAt(string sourceTable)
        {
            int index = RangeCheck(sourceTable);
            RemoveIndex(index);
        }

        private void RemoveIndex(int index)
        {
            Debug.Assert((_items != null) && (index >= 0) && (index < Count), "RemoveIndex, invalid");
            _items[index].Parent = null;
            _items.RemoveAt(index);
        }

        public void Remove(object? value)
        {
            ValidateType(value);
            Remove((DataTableMapping)value);
        }

        public void Remove(DataTableMapping value)
        {
            if (value == null)
            {
                throw ADP.TablesAddNullAttempt(nameof(value));
            }
            int index = IndexOf(value);

            if (index != -1)
            {
                RemoveIndex(index);
            }
            else
            {
                throw ADP.CollectionRemoveInvalidObject(ItemType, this);
            }
        }

        private void Replace(int index, DataTableMapping newValue)
        {
            Validate(index, newValue);
            _items![index].Parent = null;
            newValue.Parent = this;
            _items[index] = newValue;
        }

        private void ValidateType([NotNull] object? value)
        {
            if (value == null)
            {
                throw ADP.TablesAddNullAttempt(nameof(value));
            }
            else if (!ItemType.IsInstanceOfType(value))
            {
                throw ADP.NotADataTableMapping(value);
            }
        }

        private void Validate(int index, [NotNull] DataTableMapping? value)
        {
            if (value == null)
            {
                throw ADP.TablesAddNullAttempt(nameof(value));
            }
            if (value.Parent != null)
            {
                if (this != value.Parent)
                {
                    throw ADP.TablesIsNotParent(this);
                }
                else if (index != IndexOf(value))
                {
                    throw ADP.TablesIsParent(this);
                }
            }
            string name = value.SourceTable;
            if (string.IsNullOrEmpty(name))
            {
                index = 1;
                do
                {
                    name = ADP.SourceTable + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    index++;
                } while (IndexOf(name) != -1);
                value.SourceTable = name;
            }
            else
            {
                ValidateSourceTable(index, name);
            }
        }

        internal void ValidateSourceTable(int index, string? value)
        {
            int pindex = IndexOf(value);
            if ((pindex != -1) && (index != pindex))
            {
                // must be non-null and unique
                throw ADP.TablesUniqueSourceTable(value);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static DataTableMapping? GetTableMappingBySchemaAction(DataTableMappingCollection? tableMappings, string sourceTable, string dataSetTable, MissingMappingAction mappingAction)
        {
            if (tableMappings != null)
            {
                int index = tableMappings.IndexOf(sourceTable);
                if (index != -1)
                {
                    return tableMappings._items![index];
                }
            }
            if (string.IsNullOrEmpty(sourceTable))
            {
                throw ADP.InvalidSourceTable(nameof(sourceTable));
            }
            switch (mappingAction)
            {
                case MissingMappingAction.Passthrough:
                    return new DataTableMapping(sourceTable, dataSetTable);

                case MissingMappingAction.Ignore:
                    return null;

                case MissingMappingAction.Error:
                    throw ADP.MissingTableMapping(sourceTable);

                default:
                    throw ADP.InvalidMissingMappingAction(mappingAction);
            }
        }
    }
}
