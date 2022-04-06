// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    /// <summary>
    /// Represents a collection of <see cref='System.Data.DataColumn'/>
    /// objects for a <see cref='System.Data.DataTable'/>.
    /// </summary>
    [DefaultEvent(nameof(CollectionChanged))]
    [Editor("Microsoft.VSDesigner.Data.Design.ColumnsCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class DataColumnCollection : InternalDataCollectionBase
    {
        private readonly DataTable _table;
        private readonly ArrayList _list = new ArrayList();
        private int _defaultNameIndex = 1;
        private DataColumn?[]? _delayedAddRangeColumns;

        private readonly Dictionary<string, DataColumn?> _columnFromName;     // Links names to columns

        private bool _fInClear;

        private DataColumn[] _columnsImplementingIChangeTracking = Array.Empty<DataColumn>();
        private int _nColumnsImplementingIChangeTracking;
        private int _nColumnsImplementingIRevertibleChangeTracking;

        /// <summary>
        /// DataColumnCollection constructor.  Used only by DataTable.
        /// </summary>
        internal DataColumnCollection(DataTable table)
        {
            _table = table;
            _columnFromName = new Dictionary<string, DataColumn?>();
        }

        /// <summary>
        /// Gets the list of the collection items.
        /// </summary>
        protected override ArrayList List => _list;

        internal DataColumn[] ColumnsImplementingIChangeTracking => _columnsImplementingIChangeTracking;

        internal int ColumnsImplementingIChangeTrackingCount => _nColumnsImplementingIChangeTracking;

        internal int ColumnsImplementingIRevertibleChangeTrackingCount => _nColumnsImplementingIRevertibleChangeTracking;

        /// <summary>
        /// Gets the <see cref='System.Data.DataColumn'/>
        /// from the collection at the specified index.
        /// </summary>
        public DataColumn this[int index]
        {
            get
            {
                try
                {
                    // Perf: use the readonly _list field directly and let ArrayList check the range
                    return (DataColumn)_list[index]!;
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw ExceptionBuilder.ColumnOutOfRange(index);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Data.DataColumn'/> from the collection with the specified name.
        /// </summary>
        public DataColumn? this[string name]
        {
            get
            {
                if (null == name)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(name));
                }

                DataColumn? column;
                if ((!_columnFromName.TryGetValue(name, out column)) || (column == null))
                {
                    // Case-Insensitive compares
                    int index = IndexOfCaseInsensitive(name);
                    if (0 <= index)
                    {
                        column = (DataColumn)_list[index]!;
                    }
                    else if (-2 == index)
                    {
                        throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
                    }
                }

                return column;
            }
        }

        internal DataColumn? this[string name, string ns]
        {
            get
            {
                DataColumn? column;
                if ((_columnFromName.TryGetValue(name, out column)) && (column != null) && (column.Namespace == ns))
                {
                    return column;
                }

                return null;
            }
        }

        internal void EnsureAdditionalCapacity(int capacity)
        {
            if (_list.Capacity < capacity + _list.Count)
            {
                _list.Capacity = capacity + _list.Count;
            }
        }

        /// <summary>
        /// Adds the specified <see cref='System.Data.DataColumn'/>
        /// to the columns collection.
        /// </summary>
        public void Add(DataColumn column)
        {
            AddAt(-1, column);
        }

        internal void AddAt(int index, DataColumn column)
        {
            if (column != null && column.ColumnMapping == MappingType.SimpleContent)
            {
                if (_table.XmlText != null && _table.XmlText != column)
                {
                    throw ExceptionBuilder.CannotAddColumn3();
                }

                if (_table.ElementColumnCount > 0)
                {
                    throw ExceptionBuilder.CannotAddColumn4(column.ColumnName);
                }

                OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, column));
                BaseAdd(column);
                if (index != -1)
                {
                    ArrayAdd(index, column);
                }
                else
                {
                    ArrayAdd(column);
                }

                _table.XmlText = column;
            }
            else
            {
                OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, column));
                BaseAdd(column);
                if (index != -1)
                {
                    ArrayAdd(index, column);
                }
                else
                {
                    ArrayAdd(column);
                }

                // if the column is an element increase the internal dataTable counter
                if (column.ColumnMapping == MappingType.Element)
                {
                    _table.ElementColumnCount++;
                }
            }
            if (!_table.fInitInProgress && column != null && column.Computed)
            {
                column.CopyExpressionFrom(column);
            }
            OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Add, column));
        }

        public void AddRange(DataColumn[] columns)
        {
            if (_table.fInitInProgress)
            {
                _delayedAddRangeColumns = columns;
                return;
            }

            if (columns != null)
            {
                foreach (DataColumn column in columns)
                {
                    if (column != null)
                    {
                        Add(column);
                    }
                }
            }
        }

        /// <summary>
        /// Creates and adds a <see cref='System.Data.DataColumn'/>
        /// with the specified name, type, and compute expression to the columns collection.
        /// </summary>
        [RequiresUnreferencedCode("Members might be trimmed for some data types or expressions.")]
        public DataColumn Add(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type, string expression)
        {
            var column = new DataColumn(columnName, type, expression);
            Add(column);
            return column;
        }

        /// <summary>
        /// Creates and adds a <see cref='System.Data.DataColumn'/>
        /// with the
        /// specified name and type to the columns collection.
        /// </summary>
        public DataColumn Add(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            var column = new DataColumn(columnName, type);
            Add(column);
            return column;
        }

        /// <summary>
        /// Creates and adds a <see cref='System.Data.DataColumn'/>
        /// with the specified name to the columns collection.
        /// </summary>
        public DataColumn Add(string? columnName)
        {
            var column = new DataColumn(columnName);
            Add(column);
            return column;
        }

        /// <summary>
        /// Creates and adds a <see cref='System.Data.DataColumn'/> to a columns collection.
        /// </summary>
        public DataColumn Add()
        {
            var column = new DataColumn();
            Add(column);
            return column;
        }


        /// <summary>
        /// Occurs when the columns collection changes, either by adding or removing a column.
        /// </summary>
        public event CollectionChangeEventHandler? CollectionChanged;

        internal event CollectionChangeEventHandler? CollectionChanging;
        internal event CollectionChangeEventHandler? ColumnPropertyChanged;

        /// <summary>
        ///  Adds the column to the columns array.
        /// </summary>
        private void ArrayAdd(DataColumn column)
        {
            _list.Add(column);
            column.SetOrdinalInternal(_list.Count - 1);
            CheckIChangeTracking(column);
        }

        private void ArrayAdd(int index, DataColumn column)
        {
            _list.Insert(index, column);
            CheckIChangeTracking(column);
        }

        private void ArrayRemove(DataColumn column)
        {
            column.SetOrdinalInternal(-1);
            _list.Remove(column);

            int count = _list.Count;
            for (int i = 0; i < count; i++)
            {
                ((DataColumn)_list[i]!).SetOrdinalInternal(i);
            }

            if (column.ImplementsIChangeTracking)
            {
                RemoveColumnsImplementingIChangeTrackingList(column);
            }
        }

        /// <summary>
        /// Creates a new default name.
        /// </summary>
        internal string AssignName()
        {
            string newName = MakeName(_defaultNameIndex++);

            while (_columnFromName.ContainsKey(newName))
            {
                newName = MakeName(_defaultNameIndex++);
            }

            return newName;
        }

        /// <summary>
        /// Does verification on the column and it's name, and points the column at the dataSet that owns this collection.
        /// An ArgumentNullException is thrown if this column is null.  An ArgumentException is thrown if this column
        /// already belongs to this collection, belongs to another collection.
        /// A DuplicateNameException is thrown if this collection already has a column with the same
        /// name (case insensitive).
        /// </summary>
        private void BaseAdd([NotNull] DataColumn? column)
        {
            if (column == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(column));
            }
            if (column._table == _table)
            {
                throw ExceptionBuilder.CannotAddColumn1(column.ColumnName);
            }
            if (column._table != null)
            {
                throw ExceptionBuilder.CannotAddColumn2(column.ColumnName);
            }

            if (column.ColumnName.Length == 0)
            {
                column.ColumnName = AssignName();
            }

            RegisterColumnName(column.ColumnName, column);
            try
            {
                column.SetTable(_table);
                if (!_table.fInitInProgress && column.Computed)
                {
                    if (column.DataExpression!.DependsOn(column))
                    {
                        throw ExceptionBuilder.ExpressionCircular();
                    }
                }

                if (0 < _table.RecordCapacity)
                {
                    // adding a column to table with existing rows
                    column.SetCapacity(_table.RecordCapacity);
                }

                // fill column with default value.
                for (int record = 0; record < _table.RecordCapacity; record++)
                {
                    column.InitializeRecord(record);
                }
            }
            catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
            {
                UnregisterName(column.ColumnName);
                throw;
            }
        }

        /// <summary>
        /// BaseGroupSwitch will intelligently remove and add tables from the collection.
        /// </summary>
        private void BaseGroupSwitch(DataColumn[] oldArray, int oldLength, DataColumn[] newArray, int newLength)
        {
            // We're doing a smart diff of oldArray and newArray to find out what
            // should be removed.  We'll pass through oldArray and see if it exists
            // in newArray, and if not, do remove work.  newBase is an opt. in case
            // the arrays have similar prefixes.
            int newBase = 0;
            for (int oldCur = 0; oldCur < oldLength; oldCur++)
            {
                bool found = false;
                for (int newCur = newBase; newCur < newLength; newCur++)
                {
                    if (oldArray[oldCur] == newArray[newCur])
                    {
                        if (newBase == newCur)
                        {
                            newBase++;
                        }
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    // This means it's in oldArray and not newArray.  Remove it.
                    if (oldArray[oldCur].Table == _table)
                    {
                        BaseRemove(oldArray[oldCur]);
                        _list.Remove(oldArray[oldCur]);
                        oldArray[oldCur].SetOrdinalInternal(-1);
                    }
                }
            }

            // Now, let's pass through news and those that don't belong, add them.
            for (int newCur = 0; newCur < newLength; newCur++)
            {
                if (newArray[newCur].Table != _table)
                {
                    BaseAdd(newArray[newCur]);
                    _list.Add(newArray[newCur]);
                }
                newArray[newCur].SetOrdinalInternal(newCur);
            }
        }

        /// <summary>
        /// Does verification on the column and it's name, and clears the column's dataSet pointer.
        /// An ArgumentNullException is thrown if this column is null.  An ArgumentException is thrown
        /// if this column doesn't belong to this collection or if this column is part of a relationship.
        /// An ArgumentException is thrown if another column's compute expression depends on this column.
        /// </summary>
        private void BaseRemove(DataColumn column)
        {
            if (CanRemove(column, true))
            {
                // remove
                if (column._errors > 0)
                {
                    for (int i = 0; i < _table.Rows.Count; i++)
                    {
                        _table.Rows[i].ClearError(column);
                    }
                }
                UnregisterName(column.ColumnName);
                column.SetTable(null);
            }
        }

        /// <summary>
        /// Checks if a given column can be removed from the collection.
        /// </summary>
        public bool CanRemove(DataColumn? column) => CanRemove(column, false);

        internal bool CanRemove(DataColumn? column, bool fThrowException)
        {
            if (column == null)
            {
                if (!fThrowException)
                {
                    return false;
                }
                else
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(column));
                }
            }

            if (column._table != _table)
            {
                if (!fThrowException)
                {
                    return false;
                }
                else
                {
                    throw ExceptionBuilder.CannotRemoveColumn();
                }
            }

            // allow subclasses to complain first.
            _table.OnRemoveColumnInternal(column);

            // We need to make sure the column is not involved in any Relations or Constriants
            if (_table._primaryKey != null && _table._primaryKey.Key.ContainsColumn(column))
            {
                if (!fThrowException)
                {
                    return false;
                }
                else
                {
                    throw ExceptionBuilder.CannotRemovePrimaryKey();
                }
            }

            for (int i = 0; i < _table.ParentRelations.Count; i++)
            {
                if (_table.ParentRelations[i].ChildKey.ContainsColumn(column))
                {
                    if (!fThrowException)
                        return false;
                    else
                        throw ExceptionBuilder.CannotRemoveChildKey(_table.ParentRelations[i].RelationName);
                }
            }

            for (int i = 0; i < _table.ChildRelations.Count; i++)
            {
                if (_table.ChildRelations[i].ParentKey.ContainsColumn(column))
                {
                    if (!fThrowException)
                        return false;
                    else
                        throw ExceptionBuilder.CannotRemoveChildKey(_table.ChildRelations[i].RelationName);
                }
            }

            for (int i = 0; i < _table.Constraints.Count; i++)
            {
                if (_table.Constraints[i].ContainsColumn(column))
                    if (!fThrowException)
                        return false;
                    else
                        throw ExceptionBuilder.CannotRemoveConstraint(_table.Constraints[i].ConstraintName, _table.Constraints[i].Table!.TableName);
            }

            if (_table.DataSet != null)
            {
                for (ParentForeignKeyConstraintEnumerator en = new ParentForeignKeyConstraintEnumerator(_table.DataSet, _table); en.GetNext();)
                {
                    Constraint constraint = en.GetConstraint();
                    if (((ForeignKeyConstraint)constraint).ParentKey.ContainsColumn(column))
                        if (!fThrowException)
                            return false;
                        else
                            throw ExceptionBuilder.CannotRemoveConstraint(constraint.ConstraintName, constraint.Table!.TableName);
                }
            }

            if (column._dependentColumns != null)
            {
                for (int i = 0; i < column._dependentColumns.Count; i++)
                {
                    DataColumn col = column._dependentColumns[i];
                    if (_fInClear && (col.Table == _table || col.Table == null))
                    {
                        continue;
                    }

                    if (col.Table == null)
                    {
                        continue;
                    }

                    Debug.Assert(col.Computed, "invalid (non an expression) column in the expression dependent columns");
                    DataExpression? expr = col.DataExpression;
                    if ((expr != null) && (expr.DependsOn(column)))
                    {
                        if (!fThrowException)
                            return false;
                        else
                            throw ExceptionBuilder.CannotRemoveExpression(col.ColumnName, col.Expression);
                    }
                }
            }

            // you can't remove a column participating in an index,
            // while index events are suspended else the indexes won't be properly maintained.
            // However, all the above checks should catch those participating columns.
            // except when a column is in a DataView RowFilter or Sort clause
            foreach (Index _ in _table.LiveIndexes) { }

            return true;
        }

        private void CheckIChangeTracking(DataColumn column)
        {
            if (column.ImplementsIRevertibleChangeTracking)
            {
                _nColumnsImplementingIRevertibleChangeTracking++;
                _nColumnsImplementingIChangeTracking++;
                AddColumnsImplementingIChangeTrackingList(column);
            }
            else if (column.ImplementsIChangeTracking)
            {
                _nColumnsImplementingIChangeTracking++;
                AddColumnsImplementingIChangeTrackingList(column);
            }
        }

        /// <summary>
        /// Clears the collection of any columns.
        /// </summary>
        public void Clear()
        {
            int oldLength = _list.Count;

            DataColumn[] columns = new DataColumn[_list.Count];
            _list.CopyTo(columns, 0);

            OnCollectionChanging(s_refreshEventArgs);

            if (_table.fInitInProgress && _delayedAddRangeColumns != null)
            {
                _delayedAddRangeColumns = null;
            }

            try
            {
                // this will smartly add and remove the appropriate tables.
                _fInClear = true;
                BaseGroupSwitch(columns, oldLength, Array.Empty<DataColumn>(), 0);
                _fInClear = false;
            }
            catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
            {
                // something messed up: restore to old values and throw
                _fInClear = false;
                BaseGroupSwitch(Array.Empty<DataColumn>(), 0, columns, oldLength);
                _list.Clear();
                for (int i = 0; i < oldLength; i++)
                {
                    _list.Add(columns[i]);
                }
                throw;
            }

            _list.Clear();
            _table.ElementColumnCount = 0;
            OnCollectionChanged(s_refreshEventArgs);
        }

        /// <summary>
        /// Checks whether the collection contains a column with the specified name.
        /// </summary>
        public bool Contains(string name)
        {
            DataColumn? column;
            if ((_columnFromName.TryGetValue(name, out column)) && (column != null))
            {
                return true;
            }

            return (IndexOfCaseInsensitive(name) >= 0);
        }

        internal bool Contains(string name, bool caseSensitive)
        {
            DataColumn? column;
            if ((_columnFromName.TryGetValue(name, out column)) && (column != null))
            {
                return true;
            }

            // above check did case sensitive check
            return caseSensitive ? false : (IndexOfCaseInsensitive(name) >= 0);
        }

        public void CopyTo(DataColumn[] array, int index)
        {
            if (array == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(array));
            }
            if (index < 0)
            {
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(index));
            }
            if (array.Length - index < _list.Count)
            {
                throw ExceptionBuilder.InvalidOffsetLength();
            }

            for (int i = 0; i < _list.Count; ++i)
            {
                array[index + i] = (DataColumn)_list[i]!;
            }
        }

        /// <summary>
        /// Returns the index of a specified <see cref='System.Data.DataColumn'/>.
        /// </summary>
        public int IndexOf(DataColumn? column)
        {
            int columnCount = _list.Count;
            for (int i = 0; i < columnCount; ++i)
            {
                if (column == (DataColumn)_list[i]!)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of a column specified by name.
        /// </summary>
        public int IndexOf(string? columnName)
        {
            if ((null != columnName) && (0 < columnName.Length))
            {
                int count = Count;
                DataColumn? column;
                if ((_columnFromName.TryGetValue(columnName, out column)) && (column != null))
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (column == _list[j])
                        {
                            return j;
                        }
                    }
                }
                else
                {
                    int res = IndexOfCaseInsensitive(columnName);
                    return (res < 0) ? -1 : res;
                }
            }
            return -1;
        }

        internal int IndexOfCaseInsensitive(string name)
        {
            int hashcode = _table.GetSpecialHashCode(name);
            int cachedI = -1;
            for (int i = 0; i < Count; i++)
            {
                DataColumn column = (DataColumn)_list[i]!;
                if ((hashcode == 0 || column._hashCode == 0 || column._hashCode == hashcode) &&
                   NamesEqual(column.ColumnName, name, false, _table.Locale) != 0)
                {
                    if (cachedI == -1)
                    {
                        cachedI = i;
                    }
                    else
                    {
                        return -2;
                    }
                }
            }
            return cachedI;
        }

        internal void FinishInitCollection()
        {
            if (_delayedAddRangeColumns != null)
            {
                foreach (DataColumn? column in _delayedAddRangeColumns)
                {
                    if (column != null)
                    {
                        Add(column);
                    }
                }

                foreach (DataColumn? column in _delayedAddRangeColumns)
                {
                    if (column != null)
                    {
                        column.FinishInitInProgress();
                    }
                }

                _delayedAddRangeColumns = null;
            }
        }

        /// <summary>
        /// Makes a default name with the given index.  e.g. Column1, Column2, ... Columni
        /// </summary>
        private static string MakeName(int index) => index == 1 ?
                "Column1" :
                "Column" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

        internal void MoveTo(DataColumn column, int newPosition)
        {
            if (0 > newPosition || newPosition > Count - 1)
            {
                throw ExceptionBuilder.InvalidOrdinal("ordinal", newPosition);
            }

            if (column.ImplementsIChangeTracking)
            {
                RemoveColumnsImplementingIChangeTrackingList(column);
            }

            _list.Remove(column);
            _list.Insert(newPosition, column);
            int count = _list.Count;
            for (int i = 0; i < count; i++)
            {
                ((DataColumn)_list[i]!).SetOrdinalInternal(i);
            }

            CheckIChangeTracking(column);
            OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, column));
        }

        /// <summary>
        /// Raises the <see cref='System.Data.DataColumnCollection.OnCollectionChanged'/> event.
        /// </summary>
        private void OnCollectionChanged(CollectionChangeEventArgs ccevent)
        {
            _table.UpdatePropertyDescriptorCollectionCache();

            CollectionChanged?.Invoke(this, ccevent);
        }

        private void OnCollectionChanging(CollectionChangeEventArgs ccevent)
        {
            CollectionChanging?.Invoke(this, ccevent);
        }

        internal void OnColumnPropertyChanged(CollectionChangeEventArgs ccevent)
        {
            _table.UpdatePropertyDescriptorCollectionCache();
            ColumnPropertyChanged?.Invoke(this, ccevent);
        }

        /// <summary>
        /// Registers this name as being used in the collection.  Will throw an ArgumentException
        /// if the name is already being used.  Called by Add, All property, and Column.ColumnName property.
        /// if the name is equivalent to the next default name to hand out, we increment our defaultNameIndex.
        /// NOTE: To add a child table, pass column as null
        /// </summary>
        internal void RegisterColumnName(string name, DataColumn? column)
        {
            Debug.Assert(name != null);

            try
            {
                _columnFromName.Add(name, column);

                if (null != column)
                {
                    column._hashCode = _table.GetSpecialHashCode(name);
                }
            }
            catch (ArgumentException)
            {
                // Argument exception means that there is already an existing key
                if (_columnFromName[name] != null)
                {
                    if (column != null)
                    {
                        throw ExceptionBuilder.CannotAddDuplicate(name);
                    }
                    else
                    {
                        throw ExceptionBuilder.CannotAddDuplicate3(name);
                    }
                }
                throw ExceptionBuilder.CannotAddDuplicate2(name);
            }

            // If we're adding a child table, then update defaultNameIndex to avoid colisions between the child table and auto-generated column names
            if ((column == null) && NamesEqual(name, MakeName(_defaultNameIndex), true, _table.Locale) != 0)
            {
                do
                {
                    _defaultNameIndex++;
                } while (Contains(MakeName(_defaultNameIndex)));
            }
        }

        internal bool CanRegisterName(string name)
        {
            Debug.Assert(name != null, "Must specify a name");
            return (!_columnFromName.ContainsKey(name));
        }

        /// <summary>
        /// Removes the specified <see cref='System.Data.DataColumn'/>
        /// from the collection.
        /// </summary>
        public void Remove(DataColumn column)
        {
            OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Remove, column));
            BaseRemove(column);
            ArrayRemove(column);
            OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Remove, column));
            // if the column is an element decrease the internal dataTable counter
            if (column.ColumnMapping == MappingType.Element)
            {
                _table.ElementColumnCount--;
            }
        }

        /// <summary>
        /// Removes the column at the specified index from the collection.
        /// </summary>
        public void RemoveAt(int index)
        {
            DataColumn dc = this[index];
            if (dc == null)
            {
                throw ExceptionBuilder.ColumnOutOfRange(index);
            }
            Remove(dc);
        }

        /// <summary>
        /// Removes the column with the specified name from the collection.
        /// </summary>
        public void Remove(string name)
        {
            DataColumn? dc = this[name];
            if (dc == null)
            {
                throw ExceptionBuilder.ColumnNotInTheTable(name, _table.TableName);
            }
            Remove(dc);
        }

        /// <summary>
        /// Unregisters this name as no longer being used in the collection.  Called by Remove, All property, and
        /// Column.ColumnName property.  If the name is equivalent to the last proposed default name, we walk backwards
        /// to find the next proper default name to use.
        /// </summary>
        internal void UnregisterName(string name)
        {
            _columnFromName.Remove(name);

            if (NamesEqual(name, MakeName(_defaultNameIndex - 1), true, _table.Locale) != 0)
            {
                do
                {
                    _defaultNameIndex--;
                } while (_defaultNameIndex > 1 && !Contains(MakeName(_defaultNameIndex - 1)));
            }
        }

        private void AddColumnsImplementingIChangeTrackingList(DataColumn dataColumn)
        {
            DataColumn[] columns = _columnsImplementingIChangeTracking;
            DataColumn[] tempColumns = new DataColumn[columns.Length + 1];
            columns.CopyTo(tempColumns, 0);
            tempColumns[columns.Length] = dataColumn;
            _columnsImplementingIChangeTracking = tempColumns;
        }

        private void RemoveColumnsImplementingIChangeTrackingList(DataColumn dataColumn)
        {
            DataColumn[] columns = _columnsImplementingIChangeTracking;
            DataColumn[] tempColumns = new DataColumn[columns.Length - 1];
            for (int i = 0, j = 0; i < columns.Length; i++)
            {
                if (columns[i] != dataColumn)
                {
                    tempColumns[j++] = columns[i];
                }
            }
            _columnsImplementingIChangeTracking = tempColumns;
        }
    }
}
