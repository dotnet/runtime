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
    [Editor("Microsoft.VSDesigner.Data.Design.DataTableMappingCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
        public int Count => (null != _items) ? _items.Count : 0;

        private static Type ItemType => typeof(DataTableMapping);

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

        private void AddEnumerableRange(Array values, bool doClone)
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
        private List<DataTableMapping> ArrayList() => _items ??= new List<DataTableMapping>();

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
                foreach (DataTableMapping item in _items)
                {
                    item.Parent = null;
                }
                _items.Clear();
            }
        }

        public bool Contains(string? value) => (-1 != IndexOf(value));

        public bool Contains(object? value) => (-1 != IndexOf(value));

        public void CopyTo(Array array, int index) => ((ICollection)ArrayList()).CopyTo(array, index);

        public void CopyTo(DataTableMapping[] array, int index) => ArrayList().CopyTo(array, index);

        public DataTableMapping GetByDataSetTable(string dataSetTable)
        {
            int index = IndexOfDataSetTable(dataSetTable);
            if (0 > index)
            {
                throw ADP.TablesDataSetTable(dataSetTable);
            }
            return _items![index];
        }

        public IEnumerator GetEnumerator() => ArrayList().GetEnumerator();

        public int IndexOf(object? value)
        {
            if (null != value)
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
                    if ((null != value) && (0 == ADP.SrcCompare(sourceTable, value)))
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
                    if ((null != value) && (0 == ADP.DstCompare(dataSetTable, value)))
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
            if (null == value)
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
            Debug.Assert((null != _items) && (0 <= index) && (index < Count), "RemoveIndex, invalid");
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
            if (null == value)
            {
                throw ADP.TablesAddNullAttempt(nameof(value));
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

        private void Replace(int index, DataTableMapping newValue)
        {
            Validate(index, newValue);
            _items![index].Parent = null;
            newValue.Parent = this;
            _items[index] = newValue;
        }

        private static void ValidateType([NotNull] object? value)
        {
            if (null == value)
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
            if (null == value)
            {
                throw ADP.TablesAddNullAttempt(nameof(value));
            }
            if (null != value.Parent)
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
                } while (-1 != IndexOf(name));
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
            if ((-1 != pindex) && (index != pindex))
            {
                // must be non-null and unique
                throw ADP.TablesUniqueSourceTable(value);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static DataTableMapping? GetTableMappingBySchemaAction(DataTableMappingCollection? tableMappings, string sourceTable, string dataSetTable, MissingMappingAction mappingAction)
        {
            if (null != tableMappings)
            {
                int index = tableMappings.IndexOf(sourceTable);
                if (-1 != index)
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
