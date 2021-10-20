// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    /// <summary>
    /// Represents the collection of tables for the <see cref='System.Data.DataSet'/>.
    /// </summary>
    [DefaultEvent(nameof(CollectionChanged))]
    [Editor("Microsoft.VSDesigner.Data.Design.TablesCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [ListBindable(false)]
    public sealed class DataTableCollection : InternalDataCollectionBase
    {
        private readonly DataSet _dataSet;
        private readonly ArrayList _list = new ArrayList();
        private int _defaultNameIndex = 1;
        private DataTable?[]? _delayedAddRangeTables;

        private CollectionChangeEventHandler? _onCollectionChangedDelegate;
        private CollectionChangeEventHandler? _onCollectionChangingDelegate;

        private static int s_objectTypeCount; // Bid counter
        private readonly int _objectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        /// <summary>
        /// DataTableCollection constructor.  Used only by DataSet.
        /// </summary>
        internal DataTableCollection(DataSet dataSet)
        {
            DataCommonEventSource.Log.Trace("<ds.DataTableCollection.DataTableCollection|INFO> {0}, dataSet={1}", ObjectID, (dataSet != null) ? dataSet.ObjectID : 0);
            _dataSet = dataSet!;
        }

        /// <summary>
        /// Gets the tables in the collection as an object.
        /// </summary>
        protected override ArrayList List => _list;

        internal int ObjectID => _objectID;

        /// <summary>
        /// Gets the table specified by its index.
        /// </summary>
        public DataTable this[int index]
        {
            get
            {
                try
                {
                    // Perf: use the readonly _list field directly and let ArrayList check the range
                    return (DataTable)_list[index]!;
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw ExceptionBuilder.TableOutOfRange(index);
                }
            }
        }

        /// <summary>
        /// Gets the table in the collection with the given name (not case-sensitive).
        /// </summary>
        public DataTable? this[string? name]
        {
            get
            {
                int index = InternalIndexOf(name);
                if (index == -2)
                {
                    throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
                }
                if (index == -3)
                {
                    throw ExceptionBuilder.NamespaceNameConflict(name);
                }
                return (index < 0) ? null : (DataTable)_list[index]!;
            }
        }

        public DataTable? this[string? name, string tableNamespace]
        {
            get
            {
                if (tableNamespace == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(tableNamespace));
                }

                int index = InternalIndexOf(name, tableNamespace);
                if (index == -2)
                {
                    throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
                }
                return (index < 0) ? null : (DataTable)_list[index]!;
            }
        }

        // Case-sensitive search in Schema, data and diffgram loading
        internal DataTable? GetTable(string name, string ns)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                DataTable table = (DataTable)_list[i]!;
                if (table.TableName == name && table.Namespace == ns)
                {
                    return table;
                }
            }
            return null;
        }

        // Case-sensitive smart search: it will look for a table using the ns only if required to
        // resolve a conflict
        internal DataTable? GetTableSmart(string name, string? ns)
        {
            int fCount = 0;
            DataTable? fTable = null;
            for (int i = 0; i < _list.Count; i++)
            {
                DataTable table = (DataTable)_list[i]!;
                if (table.TableName == name)
                {
                    if (table.Namespace == ns)
                    {
                        return table;
                    }
                    fCount++;
                    fTable = table;
                }
            }
            // if we get here we didn't match the namespace
            // so return the table only if fCount==1 (it's the only one)
            return (fCount == 1) ? fTable : null;
        }

        /// <summary>
        /// Adds the specified table to the collection.
        /// </summary>
        public void Add(DataTable table)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Add|API> {0}, table={1}", ObjectID, (table != null) ? table.ObjectID : 0);
            try
            {
                OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, table));
                BaseAdd(table!);
                ArrayAdd(table);

                if (table.SetLocaleValue(_dataSet.Locale, false, false) ||
                    table.SetCaseSensitiveValue(_dataSet.CaseSensitive, false, false))
                {
                    table.ResetIndexes();
                }
                OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Add, table));
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void AddRange(DataTable?[]? tables)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.AddRange|API> {0}", ObjectID);
            try
            {
                if (_dataSet._fInitInProgress)
                {
                    _delayedAddRangeTables = tables;
                    return;
                }

                if (tables != null)
                {
                    foreach (DataTable? table in tables)
                    {
                        if (table != null)
                        {
                            Add(table);
                        }
                    }
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Creates a table with the given name and adds it to the collection.
        /// </summary>
        public DataTable Add(string? name)
        {
            DataTable table = new DataTable(name);
            Add(table);
            return table;
        }

        public DataTable Add(string? name, string? tableNamespace)
        {
            DataTable table = new DataTable(name, tableNamespace);
            Add(table);
            return table;
        }

        /// <summary>
        /// Creates a new table with a default name and adds it to the collection.
        /// </summary>
        public DataTable Add()
        {
            DataTable table = new DataTable();
            Add(table);
            return table;
        }

        /// <summary>
        /// Occurs when the collection is changed.
        /// </summary>
        public event CollectionChangeEventHandler? CollectionChanged
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTableCollection.add_CollectionChanged|API> {0}", ObjectID);
                _onCollectionChangedDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTableCollection.remove_CollectionChanged|API> {0}", ObjectID);
                _onCollectionChangedDelegate -= value;
            }
        }

        public event CollectionChangeEventHandler? CollectionChanging
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTableCollection.add_CollectionChanging|API> {0}", ObjectID);
                _onCollectionChangingDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTableCollection.remove_CollectionChanging|API> {0}", ObjectID);
                _onCollectionChangingDelegate -= value;
            }
        }

        private void ArrayAdd(DataTable table) => _list.Add(table);

        /// <summary>
        /// Creates a new default name.
        /// </summary>
        internal string AssignName()
        {
            string? newName = null;
            while (Contains(newName = MakeName(_defaultNameIndex)))
            {
                _defaultNameIndex++;
            }
            return newName;
        }

        /// <summary>
        /// Does verification on the table and it's name, and points the table at the dataSet that owns this collection.
        /// An ArgumentNullException is thrown if this table is null.  An ArgumentException is thrown if this table
        /// already belongs to this collection, belongs to another collection.
        /// A DuplicateNameException is thrown if this collection already has a table with the same
        /// name (case insensitive).
        /// </summary>
        private void BaseAdd([NotNull] DataTable table)
        {
            if (table == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(table));
            }
            if (table.DataSet == _dataSet)
            {
                throw ExceptionBuilder.TableAlreadyInTheDataSet();
            }
            if (table.DataSet != null)
            {
                throw ExceptionBuilder.TableAlreadyInOtherDataSet();
            }

            if (table.TableName.Length == 0)
            {
                table.TableName = AssignName();
            }
            else
            {
                if (NamesEqual(table.TableName, _dataSet.DataSetName, false, _dataSet.Locale) != 0 && !table._fNestedInDataset)
                {
                    throw ExceptionBuilder.DatasetConflictingName(_dataSet.DataSetName);
                }
                RegisterName(table.TableName, table.Namespace);
            }

            table.SetDataSet(_dataSet);

            //must run thru the document incorporating the addition of this data table
            //must make sure there is no other schema component which have the same
            // identity as this table (for example, there must not be a table with the
            // same identity as a column in this schema.
        }

        /// <summary>
        /// BaseGroupSwitch will intelligently remove and add tables from the collection.
        /// </summary>
        private void BaseGroupSwitch(DataTable[] oldArray, int oldLength, DataTable[] newArray, int newLength)
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
                    if (oldArray[oldCur].DataSet == _dataSet)
                    {
                        BaseRemove(oldArray[oldCur]);
                    }
                }
            }

            // Now, let's pass through news and those that don't belong, add them.
            for (int newCur = 0; newCur < newLength; newCur++)
            {
                if (newArray[newCur].DataSet != _dataSet)
                {
                    BaseAdd(newArray[newCur]);
                    _list.Add(newArray[newCur]);
                }
            }
        }

        /// <summary>
        /// Does verification on the table and it's name, and clears the table's dataSet pointer.
        /// An ArgumentNullException is thrown if this table is null.  An ArgumentException is thrown
        /// if this table doesn't belong to this collection or if this table is part of a relationship.
        /// </summary>
        private void BaseRemove(DataTable? table)
        {
            if (CanRemove(table, true))
            {
                UnregisterName(table.TableName);
                table.SetDataSet(null);
            }
            _list.Remove(table);
            _dataSet.OnRemovedTable(table!);
        }

        /// <summary>
        /// Verifies if a given table can be removed from the collection.
        /// </summary>
        public bool CanRemove(DataTable? table) => CanRemove(table, false);

        internal bool CanRemove([NotNullWhen(true)] DataTable? table, bool fThrowException)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.CanRemove|INFO> {0}, table={1}, fThrowException={2}", ObjectID, (table != null) ? table.ObjectID : 0, fThrowException);
            try
            {
                if (table == null)
                {
                    if (!fThrowException)
                    {
                        return false;
                    }
                    throw ExceptionBuilder.ArgumentNull(nameof(table));
                }
                if (table.DataSet != _dataSet)
                {
                    if (!fThrowException)
                    {
                        return false;
                    }
                    throw ExceptionBuilder.TableNotInTheDataSet(table.TableName);
                }

                // allow subclasses to throw.
                _dataSet.OnRemoveTable(table);

                if (table.ChildRelations.Count != 0 || table.ParentRelations.Count != 0)
                {
                    if (!fThrowException)
                    {
                        return false;
                    }
                    throw ExceptionBuilder.TableInRelation();
                }

                for (ParentForeignKeyConstraintEnumerator constraints = new ParentForeignKeyConstraintEnumerator(_dataSet, table); constraints.GetNext();)
                {
                    ForeignKeyConstraint constraint = constraints.GetForeignKeyConstraint();
                    if (constraint.Table == table && constraint.RelatedTable == table) // we can go with (constraint.Table ==  constraint.RelatedTable)
                    {
                        continue;
                    }
                    if (!fThrowException)
                    {
                        return false;
                    }
                    else
                    {
                        throw ExceptionBuilder.TableInConstraint(table, constraint);
                    }
                }

                for (ChildForeignKeyConstraintEnumerator constraints = new ChildForeignKeyConstraintEnumerator(_dataSet, table); constraints.GetNext();)
                {
                    ForeignKeyConstraint constraint = constraints.GetForeignKeyConstraint();
                    if (constraint.Table == table && constraint.RelatedTable == table)
                    {
                        continue;
                    }

                    if (!fThrowException)
                    {
                        return false;
                    }
                    else
                    {
                        throw ExceptionBuilder.TableInConstraint(table, constraint);
                    }
                }

                return true;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Clears the collection of any tables.
        /// </summary>
        public void Clear()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Clear|API> {0}", ObjectID);
            try
            {
                int oldLength = _list.Count;
                DataTable[] tables = new DataTable[_list.Count];
                _list.CopyTo(tables, 0);

                OnCollectionChanging(s_refreshEventArgs);

                if (_dataSet._fInitInProgress && _delayedAddRangeTables != null)
                {
                    _delayedAddRangeTables = null;
                }

                BaseGroupSwitch(tables, oldLength, Array.Empty<DataTable>(), 0);
                _list.Clear();

                OnCollectionChanged(s_refreshEventArgs);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Checks if a table, specified by name, exists in the collection.
        /// </summary>
        public bool Contains(string? name) => (InternalIndexOf(name) >= 0);

        public bool Contains(string name, string tableNamespace)
        {
            if (name == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(name));
            }

            if (tableNamespace == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(tableNamespace));
            }

            return (InternalIndexOf(name, tableNamespace) >= 0);
        }

        internal bool Contains(string name, string? tableNamespace, bool checkProperty, bool caseSensitive)
        {
            if (!caseSensitive)
            {
                return (InternalIndexOf(name) >= 0);
            }

            // Case-Sensitive compare
            int count = _list.Count;
            for (int i = 0; i < count; i++)
            {
                DataTable table = (DataTable)_list[i]!;
                // this may be needed to check whether the cascading is creating some conflicts
                string? ns = checkProperty ? table.Namespace : table._tableNamespace;
                if (NamesEqual(table.TableName, name, true, _dataSet.Locale) == 1 && (ns == tableNamespace))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool Contains(string name, bool caseSensitive)
        {
            if (!caseSensitive)
            {
                return (InternalIndexOf(name) >= 0);
            }

            // Case-Sensitive compare
            int count = _list.Count;
            for (int i = 0; i < count; i++)
            {
                DataTable table = (DataTable)_list[i]!;
                if (NamesEqual(table.TableName, name, true, _dataSet.Locale) == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(DataTable[] array, int index)
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
                array[index + i] = (DataTable)_list[i]!;
            }
        }

        /// <summary>
        /// Returns the index of a specified <see cref='System.Data.DataTable'/>.
        /// </summary>
        public int IndexOf(DataTable? table)
        {
            int tableCount = _list.Count;
            for (int i = 0; i < tableCount; ++i)
            {
                if (table == (DataTable)_list[i]!)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the
        /// table with the given name (case insensitive), or -1 if the table
        /// doesn't exist in the collection.
        /// </summary>
        public int IndexOf(string? tableName)
        {
            int index = InternalIndexOf(tableName);
            return (index < 0) ? -1 : index;
        }

        public int IndexOf(string tableName, string tableNamespace) => IndexOf(tableName, tableNamespace, true);

        internal int IndexOf(string tableName, string tableNamespace, bool chekforNull)
        {
            // this should be public! why it is missing?
            if (chekforNull)
            {
                if (tableName == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(tableName));
                }
                if (tableNamespace == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(tableNamespace));
                }
            }
            int index = InternalIndexOf(tableName, tableNamespace);
            return (index < 0) ? -1 : index;
        }

        internal void ReplaceFromInference(List<DataTable> tableList)
        {
            Debug.Assert(_list.Count == tableList.Count, "Both lists should have equal numbers of tables");
            _list.Clear();
            _list.AddRange(tableList);
        }

        // Return value:
        //      >= 0: find the match
        //        -1: No match
        //        -2: At least two matches with different cases
        //        -3: At least two matches with different namespaces
        internal int InternalIndexOf(string? tableName)
        {
            int cachedI = -1;
            if ((null != tableName) && (0 < tableName.Length))
            {
                int count = _list.Count;
                int result = 0;
                for (int i = 0; i < count; i++)
                {
                    DataTable table = (DataTable)_list[i]!;
                    result = NamesEqual(table.TableName, tableName, false, _dataSet.Locale);
                    if (result == 1)
                    {
                        // ok, we have found a table with the same name.
                        // let's see if there are any others with the same name
                        // if any let's return (-3) otherwise...
                        for (int j = i + 1; j < count; j++)
                        {
                            DataTable dupTable = (DataTable)_list[j]!;
                            if (NamesEqual(dupTable.TableName, tableName, false, _dataSet.Locale) == 1)
                                return -3;
                        }
                        //... let's just return i
                        return i;
                    }

                    if (result == -1)
                        cachedI = (cachedI == -1) ? i : -2;
                }
            }
            return cachedI;
        }

        // Return value:
        //      >= 0: find the match
        //        -1: No match
        //        -2: At least two matches with different cases
        internal int InternalIndexOf(string? tableName, string tableNamespace)
        {
            int cachedI = -1;
            if ((null != tableName) && (0 < tableName.Length))
            {
                int count = _list.Count;
                int result = 0;
                for (int i = 0; i < count; i++)
                {
                    DataTable table = (DataTable)_list[i]!;
                    result = NamesEqual(table.TableName, tableName, false, _dataSet.Locale);
                    if ((result == 1) && (table.Namespace == tableNamespace))
                        return i;

                    if ((result == -1) && (table.Namespace == tableNamespace))
                        cachedI = (cachedI == -1) ? i : -2;
                }
            }
            return cachedI;
        }

        internal void FinishInitCollection()
        {
            if (_delayedAddRangeTables != null)
            {
                foreach (DataTable? table in _delayedAddRangeTables)
                {
                    if (table != null)
                    {
                        Add(table);
                    }
                }
                _delayedAddRangeTables = null;
            }
        }

        /// <summary>
        /// Makes a default name with the given index.  e.g. Table1, Table2, ... Tablei
        /// </summary>
        private string MakeName(int index) => 1 == index ?
            "Table1" :
            "Table" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// Raises the <see cref='System.Data.DataTableCollection.OnCollectionChanged'/> event.
        /// </summary>
        private void OnCollectionChanged(CollectionChangeEventArgs ccevent)
        {
            if (_onCollectionChangedDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTableCollection.OnCollectionChanged|INFO> {0}", ObjectID);
                _onCollectionChangedDelegate(this, ccevent);
            }
        }

        private void OnCollectionChanging(CollectionChangeEventArgs ccevent)
        {
            if (_onCollectionChangingDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTableCollection.OnCollectionChanging|INFO> {0}", ObjectID);
                _onCollectionChangingDelegate(this, ccevent);
            }
        }

        /// <summary>
        /// Registers this name as being used in the collection.  Will throw an ArgumentException
        /// if the name is already being used.  Called by Add, All property, and Table.TableName property.
        /// if the name is equivalent to the next default name to hand out, we increment our defaultNameIndex.
        /// </summary>
        internal void RegisterName(string name, string tbNamespace)
        {
            DataCommonEventSource.Log.Trace("<ds.DataTableCollection.RegisterName|INFO> {0}, name='{1}', tbNamespace='{2}'", ObjectID, name, tbNamespace);
            Debug.Assert(name != null);

            CultureInfo locale = _dataSet.Locale;
            int tableCount = _list.Count;
            for (int i = 0; i < tableCount; i++)
            {
                DataTable table = (DataTable)_list[i]!;
                if (NamesEqual(name, table.TableName, true, locale) != 0 && (tbNamespace == table.Namespace))
                {
                    throw ExceptionBuilder.DuplicateTableName(((DataTable)_list[i]!).TableName);
                }
            }
            if (NamesEqual(name, MakeName(_defaultNameIndex), true, locale) != 0)
            {
                _defaultNameIndex++;
            }
        }

        /// <summary>
        /// Removes the specified table from the collection.
        /// </summary>
        public void Remove(DataTable table)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Remove|API> {0}, table={1}", ObjectID, (table != null) ? table.ObjectID : 0);
            try
            {
                OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Remove, table));
                BaseRemove(table);
                OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Remove, table));
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Removes the table at the given index from the collection
        /// </summary>
        public void RemoveAt(int index)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.RemoveAt|API> {0}, index={1}", ObjectID, index);
            try
            {
                DataTable dt = this[index];
                if (dt == null)
                {
                    throw ExceptionBuilder.TableOutOfRange(index);
                }
                Remove(dt);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Removes the table with a specified name from the collection.
        /// </summary>
        public void Remove(string name)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Remove|API> {0}, name='{1}'", ObjectID, name);
            try
            {
                DataTable? dt = this[name];
                if (dt == null)
                {
                    throw ExceptionBuilder.TableNotInTheDataSet(name);
                }
                Remove(dt);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void Remove(string name, string tableNamespace)
        {
            if (name == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(name));
            }
            if (tableNamespace == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(tableNamespace));
            }
            DataTable? dt = this[name, tableNamespace];
            if (dt == null)
            {
                throw ExceptionBuilder.TableNotInTheDataSet(name);
            }
            Remove(dt);
        }

        /// <summary>
        /// Unregisters this name as no longer being used in the collection.  Called by Remove, All property, and
        /// Table.TableName property.  If the name is equivalent to the last proposed default name, we walk backwards
        /// to find the next proper default name to  use.
        /// </summary>
        internal void UnregisterName(string name)
        {
            DataCommonEventSource.Log.Trace("<ds.DataTableCollection.UnregisterName|INFO> {0}, name='{1}'", ObjectID, name);
            if (NamesEqual(name, MakeName(_defaultNameIndex - 1), true, _dataSet.Locale) != 0)
            {
                do
                {
                    _defaultNameIndex--;
                } while (_defaultNameIndex > 1 && !Contains(MakeName(_defaultNameIndex - 1)));
            }
        }
    }
}
