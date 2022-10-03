// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data
{
    /// <summary>
    /// Represents the collection of relations,
    /// each of which allows navigation between related parent/child tables.
    /// </summary>
    [DefaultEvent(nameof(CollectionChanged))]
    [DefaultProperty("Table")]
    [Editor("Microsoft.VSDesigner.Data.Design.DataRelationCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class DataRelationCollection : InternalDataCollectionBase
    {
        private DataRelation? _inTransition;

        private int _defaultNameIndex = 1;

        private CollectionChangeEventHandler? _onCollectionChangedDelegate;
        private CollectionChangeEventHandler? _onCollectionChangingDelegate;

        private static int s_objectTypeCount; // Bid counter
        private readonly int _objectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        internal int ObjectID => _objectID;

        /// <summary>
        /// Gets the relation specified by index.
        /// </summary>
        public abstract DataRelation this[int index] { get; }

        /// <summary>
        /// Gets the relation specified by name.
        /// </summary>
        public abstract DataRelation? this[string? name] { get; }

        /// <summary>
        /// Adds the relation to the collection.
        /// </summary>
        public void Add(DataRelation relation)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelationCollection.Add|API> {0}, relation={1}", ObjectID, (relation != null) ? relation.ObjectID : 0);
            try
            {
                if (_inTransition == relation)
                {
                    return;
                }

                _inTransition = relation;
                try
                {
                    OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, relation));
                    AddCore(relation!);
                    OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Add, relation));
                }
                finally
                {
                    _inTransition = null;
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public virtual void AddRange(DataRelation[]? relations)
        {
            if (relations != null)
            {
                foreach (DataRelation relation in relations)
                {
                    if (relation != null)
                    {
                        Add(relation);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref='System.Data.DataRelation'/> with the
        /// specified name, parent columns,
        /// child columns, and adds it to the collection.
        /// </summary>
        public virtual DataRelation Add(string? name, DataColumn[] parentColumns, DataColumn[] childColumns)
        {
            var relation = new DataRelation(name, parentColumns, childColumns);
            Add(relation);
            return relation;
        }

        /// <summary>
        /// Creates a relation given the parameters and adds it to the collection.  An ArgumentNullException is
        /// thrown if this relation is null.  An ArgumentException is thrown if this relation already belongs to
        /// this collection, belongs to another collection, or if this collection already has a relation with the
        /// same name (case insensitive).
        /// An InvalidRelationException is thrown if the relation can't be created based on the parameters.
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public virtual DataRelation Add(string? name, DataColumn[] parentColumns, DataColumn[] childColumns, bool createConstraints)
        {
            var relation = new DataRelation(name, parentColumns, childColumns, createConstraints);
            Add(relation);
            return relation;
        }

        /// <summary>
        /// Creates a relation given the parameters and adds it to the collection.  The name is defaulted.
        /// An ArgumentException is thrown if
        /// this relation already belongs to this collection or belongs to another collection.
        /// An InvalidConstraintException is thrown if the relation can't be created based on the parameters.
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public virtual DataRelation Add(DataColumn[] parentColumns, DataColumn[] childColumns)
        {
            var relation = new DataRelation(null, parentColumns, childColumns);
            Add(relation);
            return relation;
        }

        /// <summary>
        /// Creates a relation given the parameters and adds it to the collection.
        /// An ArgumentException is thrown if this relation already belongs to
        /// this collection or belongs to another collection.
        /// A DuplicateNameException is thrown if this collection already has a relation with the same
        /// name (case insensitive).
        /// An InvalidConstraintException is thrown if the relation can't be created based on the parameters.
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public virtual DataRelation Add(string? name, DataColumn parentColumn, DataColumn childColumn)
        {
            var relation = new DataRelation(name, parentColumn, childColumn);
            Add(relation);
            return relation;
        }

        /// <summary>
        /// Creates a relation given the parameters and adds it to the collection.
        /// An ArgumentException is thrown if this relation already belongs to
        /// this collection or belongs to another collection.
        /// A DuplicateNameException is thrown if this collection already has a relation with the same
        /// name (case insensitive).
        /// An InvalidConstraintException is thrown if the relation can't be created based on the parameters.
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public virtual DataRelation Add(string? name, DataColumn parentColumn, DataColumn childColumn, bool createConstraints)
        {
            var relation = new DataRelation(name, parentColumn, childColumn, createConstraints);
            Add(relation);
            return relation;
        }

        /// <summary>
        /// Creates a relation given the parameters and adds it to the collection.  The name is defaulted.
        /// An ArgumentException is thrown if
        /// this relation already belongs to this collection or belongs to another collection.
        /// An InvalidConstraintException is thrown if the relation can't be created based on the parameters.
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public virtual DataRelation Add(DataColumn parentColumn, DataColumn childColumn)
        {
            var relation = new DataRelation(null, parentColumn, childColumn);
            Add(relation);
            return relation;
        }

        /// <summary>
        /// Does verification on the table.
        /// An ArgumentNullException is thrown if this relation is null.  An ArgumentException is thrown if this relation
        ///  already belongs to this collection, belongs to another collection.
        /// A DuplicateNameException is thrown if this collection already has a relation with the same
        /// name (case insensitive).
        /// </summary>
        protected virtual void AddCore(DataRelation relation)
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.AddCore|INFO> {0}, relation={1}", ObjectID, (relation != null) ? relation.ObjectID : 0);
            if (relation == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(relation));
            }

            relation.CheckState();
            DataSet dataSet = GetDataSet();
            if (relation.DataSet == dataSet)
            {
                throw ExceptionBuilder.RelationAlreadyInTheDataSet();
            }
            if (relation.DataSet != null)
            {
                throw ExceptionBuilder.RelationAlreadyInOtherDataSet();
            }
            if (relation.ChildTable.Locale.LCID != relation.ParentTable.Locale.LCID ||
                relation.ChildTable.CaseSensitive != relation.ParentTable.CaseSensitive)
            {
                throw ExceptionBuilder.CaseLocaleMismatch();
            }

            if (relation.Nested)
            {
                relation.CheckNamespaceValidityForNestedRelations(relation.ParentTable.Namespace);
                relation.ValidateMultipleNestedRelations();
                relation.ParentTable.ElementColumnCount++;
            }
        }

        public event CollectionChangeEventHandler? CollectionChanged
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.add_CollectionChanged|API> {0}", ObjectID);
                _onCollectionChangedDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.remove_CollectionChanged|API> {0}", ObjectID);
                _onCollectionChangedDelegate -= value;
            }
        }

        internal event CollectionChangeEventHandler? CollectionChanging
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.add_CollectionChanging|INFO> {0}", ObjectID);
                _onCollectionChangingDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.remove_CollectionChanging|INFO> {0}", ObjectID);
                _onCollectionChangingDelegate -= value;
            }
        }

        /// <summary>
        /// Creates a new default name.
        /// </summary>
        internal string AssignName()
        {
            string newName = MakeName(_defaultNameIndex);
            _defaultNameIndex++;
            return newName;
        }

        /// <summary>
        /// Clears the collection of any relations.
        /// </summary>
        public virtual void Clear()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelationCollection.Clear|API> {0}", ObjectID);
            try
            {
                int count = Count;
                OnCollectionChanging(s_refreshEventArgs);
                for (int i = count - 1; i >= 0; i--)
                {
                    _inTransition = this[i];
                    RemoveCore(_inTransition);
                }
                OnCollectionChanged(s_refreshEventArgs);
                _inTransition = null;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        ///  Returns true if this collection has a relation with the given name (case insensitive), false otherwise.
        /// </summary>
        public virtual bool Contains(string? name) => (InternalIndexOf(name) >= 0);

        public void CopyTo(DataRelation[] array, int index)
        {
            if (array == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(array));
            }

            if (index < 0)
            {
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(index));
            }

            ArrayList alist = List;
            if (array.Length - index < alist.Count)
            {
                throw ExceptionBuilder.InvalidOffsetLength();
            }

            for (int i = 0; i < alist.Count; ++i)
            {
                array[index + i] = (DataRelation)alist[i]!;
            }
        }

        /// <summary>
        /// Returns the index of a specified <see cref='System.Data.DataRelation'/>.
        /// </summary>
        public virtual int IndexOf(DataRelation? relation)
        {
            int relationCount = List.Count;
            for (int i = 0; i < relationCount; ++i)
            {
                if (relation == (DataRelation)List[i]!)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the
        /// relation with the given name (case insensitive), or -1 if the relation
        /// doesn't exist in the collection.
        /// </summary>
        public virtual int IndexOf(string? relationName)
        {
            int index = InternalIndexOf(relationName);
            return (index < 0) ? -1 : index;
        }

        internal int InternalIndexOf(string? name)
        {
            int cachedI = -1;
            if ((null != name) && (0 < name.Length))
            {
                int count = List.Count;
                for (int i = 0; i < count; i++)
                {
                    DataRelation relation = (DataRelation)List[i]!;
                    int result = NamesEqual(relation.RelationName, name, false, GetDataSet().Locale);
                    if (result == 1)
                    {
                        return i;
                    }

                    if (result == -1)
                    {
                        cachedI = (cachedI == -1) ? i : -2;
                    }
                }
            }
            return cachedI;
        }

        /// <summary>
        /// Gets the dataSet for this collection.
        /// </summary>
        protected abstract DataSet GetDataSet();

        /// <summary>
        /// Makes a default name with the given index.  e.g. Relation1, Relation2, ... Relationi
        /// </summary>
        private static string MakeName(int index) => index == 1 ?
            "Relation1" :
            "Relation" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// This method is called whenever the collection changes.  Overriders
        /// of this method should call the base implementation of this method.
        /// </summary>
        protected virtual void OnCollectionChanged(CollectionChangeEventArgs ccevent)
        {
            if (_onCollectionChangedDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.OnCollectionChanged|INFO> {0}", ObjectID);
                _onCollectionChangedDelegate(this, ccevent);
            }
        }

        protected virtual void OnCollectionChanging(CollectionChangeEventArgs ccevent)
        {
            if (_onCollectionChangingDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.OnCollectionChanging|INFO> {0}", ObjectID);
                _onCollectionChangingDelegate(this, ccevent);
            }
        }

        /// <summary>
        /// Registers this name as being used in the collection.  Will throw an ArgumentException
        /// if the name is already being used.  Called by Add, All property, and Relation.RelationName property.
        /// if the name is equivalent to the next default name to hand out, we increment our defaultNameIndex.
        /// </summary>
        internal void RegisterName(string name)
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.RegisterName|INFO> {0}, name='{1}'", ObjectID, name);
            Debug.Assert(name != null);

            CultureInfo locale = GetDataSet().Locale;
            int relationCount = Count;
            for (int i = 0; i < relationCount; i++)
            {
                if (NamesEqual(name, this[i].RelationName, true, locale) != 0)
                {
                    throw ExceptionBuilder.DuplicateRelation(this[i].RelationName);
                }
            }
            if (NamesEqual(name, MakeName(_defaultNameIndex), true, locale) != 0)
            {
                _defaultNameIndex++;
            }
        }

        /// <summary>
        /// Verifies if a given relation can be removed from the collection.
        /// </summary>
        public virtual bool CanRemove(DataRelation? relation) => relation != null && relation.DataSet == GetDataSet();

        /// <summary>
        /// Removes the given relation from the collection.
        /// An ArgumentNullException is thrown if this relation is null.  An ArgumentException is thrown
        /// if this relation doesn't belong to this collection.
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public void Remove(DataRelation relation)
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.Remove|API> {0}, relation={1}", ObjectID, (relation != null) ? relation.ObjectID : 0);
            if (_inTransition == relation)
            {
                return;
            }

            _inTransition = relation;
            try
            {
                OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Remove, relation));
                RemoveCore(relation!);
                OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Remove, relation));
            }
            finally
            {
                _inTransition = null;
            }
        }

        /// <summary>
        /// Removes the relation at the given index from the collection.  An IndexOutOfRangeException is
        /// thrown if this collection doesn't have a relation at this index.
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public void RemoveAt(int index)
        {
            DataRelation dr = this[index];
            // TODO: Not needed, this[] throws
            if (dr == null)
            {
                throw ExceptionBuilder.RelationOutOfRange(index);
            }
            Remove(dr);
        }

        /// <summary>
        /// Removes the relation with the given name from the collection.  An IndexOutOfRangeException is
        /// thrown if this collection doesn't have a relation with that name
        /// The CollectionChanged event is fired if it succeeds.
        /// </summary>
        public void Remove(string name)
        {
            DataRelation? dr = this[name];
            if (dr == null)
            {
                throw ExceptionBuilder.RelationNotInTheDataSet(name);
            }
            Remove(dr);
        }

        /// <summary>
        /// Does verification on the relation.
        /// An ArgumentNullException is thrown if this relation is null.  An ArgumentException is thrown
        /// if this relation doesn't belong to this collection.
        /// </summary>
        protected virtual void RemoveCore(DataRelation relation)
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.RemoveCore|INFO> {0}, relation={1}", ObjectID, (relation != null) ? relation.ObjectID : 0);
            if (relation == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(relation));
            }

            DataSet dataSet = GetDataSet();
            if (relation.DataSet != dataSet)
            {
                throw ExceptionBuilder.RelationNotInTheDataSet(relation.RelationName);
            }

            if (relation.Nested)
            {
                relation.ParentTable.ElementColumnCount--;
                relation.ParentTable.Columns.UnregisterName(relation.ChildTable.TableName);
            }
        }

        /// <summary>
        /// Unregisters this name as no longer being used in the collection.  Called by Remove, All property, and
        /// Relation.RelationName property.  If the name is equivalent to the last proposed default name, we walk backwards
        /// to find the next proper default name to use.
        /// </summary>
        internal void UnregisterName(string name)
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.UnregisterName|INFO> {0}, name='{1}'", ObjectID, name);
            if (NamesEqual(name, MakeName(_defaultNameIndex - 1), true, GetDataSet().Locale) != 0)
            {
                do
                {
                    _defaultNameIndex--;
                } while (_defaultNameIndex > 1 && !Contains(MakeName(_defaultNameIndex - 1)));
            }
        }

        internal sealed class DataTableRelationCollection : DataRelationCollection
        {
            private readonly DataTable _table;
            private readonly ArrayList _relations; // For caching purpose only to improve performance
            private readonly bool _fParentCollection;

            internal DataTableRelationCollection(DataTable table, bool fParentCollection)
            {
                if (table == null)
                {
                    throw ExceptionBuilder.RelationTableNull();
                }
                _table = table;
                _fParentCollection = fParentCollection;
                _relations = new ArrayList();
            }

            protected override ArrayList List => _relations;

            private void EnsureDataSet()
            {
                if (_table.DataSet == null)
                {
                    throw ExceptionBuilder.RelationTableWasRemoved();
                }
            }

            protected override DataSet GetDataSet()
            {
                EnsureDataSet();
                return _table.DataSet!;
            }

            public override DataRelation this[int index]
            {
                get
                {
                    if (index >= 0 && index < _relations.Count)
                    {
                        return (DataRelation)_relations[index]!;
                    }
                    throw ExceptionBuilder.RelationOutOfRange(index);
                }
            }

            public override DataRelation? this[string? name]
            {
                get
                {
                    int index = InternalIndexOf(name);
                    if (index == -2)
                    {
                        throw ExceptionBuilder.CaseInsensitiveNameConflict(name!);
                    }
                    return (index < 0) ? null : (DataRelation)List[index]!;
                }
            }

            internal event CollectionChangeEventHandler? RelationPropertyChanged;

            internal void OnRelationPropertyChanged(CollectionChangeEventArgs ccevent)
            {
                if (!_fParentCollection)
                {
                    _table.UpdatePropertyDescriptorCollectionCache();
                }
                RelationPropertyChanged?.Invoke(this, ccevent);
            }

            private void AddCache(DataRelation relation)
            {
                _relations.Add(relation);
                if (!_fParentCollection)
                {
                    _table.UpdatePropertyDescriptorCollectionCache();
                }
            }

            protected override void AddCore(DataRelation relation)
            {
                if (_fParentCollection)
                {
                    if (relation.ChildTable != _table)
                    {
                        throw ExceptionBuilder.ChildTableMismatch();
                    }
                }
                else
                {
                    if (relation.ParentTable != _table)
                    {
                        throw ExceptionBuilder.ParentTableMismatch();
                    }
                }

                GetDataSet().Relations.Add(relation);
                AddCache(relation);
            }

            public override bool CanRemove(DataRelation? relation)
            {
                if (!base.CanRemove(relation))
                {
                    return false;
                }

                Debug.Assert(relation != null);

                if (_fParentCollection)
                {
                    if (relation.ChildTable != _table)
                    {
                        return false;
                    }
                }
                else
                {
                    if (relation.ParentTable != _table)
                    {
                        return false;
                    }
                }

                return true;
            }

            private void RemoveCache(DataRelation relation)
            {
                for (int i = 0; i < _relations.Count; i++)
                {
                    if (relation == _relations[i])
                    {
                        _relations.RemoveAt(i);
                        if (!_fParentCollection)
                        {
                            _table.UpdatePropertyDescriptorCollectionCache();
                        }
                        return;
                    }
                }
                throw ExceptionBuilder.RelationDoesNotExist();
            }

            protected override void RemoveCore(DataRelation relation)
            {
                if (_fParentCollection)
                {
                    if (relation.ChildTable != _table)
                    {
                        throw ExceptionBuilder.ChildTableMismatch();
                    }
                }
                else
                {
                    if (relation.ParentTable != _table)
                    {
                        throw ExceptionBuilder.ParentTableMismatch();
                    }
                }

                GetDataSet().Relations.Remove(relation);
                RemoveCache(relation);
            }
        }

        internal sealed class DataSetRelationCollection : DataRelationCollection
        {
            private readonly DataSet _dataSet;
            private readonly ArrayList _relations;
            private DataRelation[]? _delayLoadingRelations;

            internal DataSetRelationCollection(DataSet dataSet)
            {
                if (dataSet == null)
                {
                    throw ExceptionBuilder.RelationDataSetNull();
                }
                _dataSet = dataSet;
                _relations = new ArrayList();
            }

            protected override ArrayList List => _relations;

            public override void AddRange(DataRelation[]? relations)
            {
                if (_dataSet._fInitInProgress)
                {
                    _delayLoadingRelations = relations;
                    return;
                }

                if (relations != null)
                {
                    foreach (DataRelation relation in relations)
                    {
                        if (relation != null)
                        {
                            Add(relation);
                        }
                    }
                }
            }

            public override void Clear()
            {
                base.Clear();
                if (_dataSet._fInitInProgress && _delayLoadingRelations != null)
                {
                    _delayLoadingRelations = null;
                }
            }

            protected override DataSet GetDataSet() => _dataSet;

            public override DataRelation this[int index]
            {
                get
                {
                    if (index >= 0 && index < _relations.Count)
                    {
                        return (DataRelation)_relations[index]!;
                    }
                    throw ExceptionBuilder.RelationOutOfRange(index);
                }
            }

            public override DataRelation? this[string? name]
            {
                get
                {
                    int index = InternalIndexOf(name);
                    if (index == -2)
                    {
                        throw ExceptionBuilder.CaseInsensitiveNameConflict(name!);
                    }
                    return (index < 0) ? null : (DataRelation)List[index]!;
                }
            }

            protected override void AddCore(DataRelation relation)
            {
                base.AddCore(relation);
                if (relation.ChildTable.DataSet != _dataSet || relation.ParentTable.DataSet != _dataSet)
                {
                    throw ExceptionBuilder.ForeignRelation();
                }

                relation.CheckState();
                if (relation.Nested)
                {
                    relation.CheckNestedRelations();
                }

                if (relation._relationName.Length == 0)
                {
                    relation._relationName = AssignName();
                }
                else
                {
                    RegisterName(relation._relationName);
                }

                DataKey childKey = relation.ChildKey;

                for (int i = 0; i < _relations.Count; i++)
                {
                    if (childKey.ColumnsEqual(((DataRelation)_relations[i]!).ChildKey))
                    {
                        if (relation.ParentKey.ColumnsEqual(((DataRelation)_relations[i]!).ParentKey))
                            throw ExceptionBuilder.RelationAlreadyExists();
                    }
                }

                _relations.Add(relation);
                ((DataTableRelationCollection)(relation.ParentTable.ChildRelations)).Add(relation); // Caching in ParentTable -> ChildRelations
                ((DataTableRelationCollection)(relation.ChildTable.ParentRelations)).Add(relation); // Caching in ChildTable -> ParentRelations

                relation.SetDataSet(_dataSet);
                relation.ChildKey.GetSortIndex().AddRef();
                if (relation.Nested)
                {
                    relation.ChildTable.CacheNestedParent();
                }

                ForeignKeyConstraint? foreignKey = relation.ChildTable.Constraints.FindForeignKeyConstraint(relation.ParentColumnsReference, relation.ChildColumnsReference);
                if (relation._createConstraints)
                {
                    if (foreignKey == null)
                    {
                        relation.ChildTable.Constraints.Add(foreignKey = new ForeignKeyConstraint(relation.ParentColumnsReference, relation.ChildColumnsReference));

                        // try to name the fk constraint the same as the parent relation:
                        try
                        {
                            foreignKey.ConstraintName = relation.RelationName;
                        }
                        catch (Exception e) when (Common.ADP.IsCatchableExceptionType(e))
                        {
                            ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                        }
                    }
                }
                UniqueConstraint? key = relation.ParentTable.Constraints.FindKeyConstraint(relation.ParentColumnsReference);
                relation.SetParentKeyConstraint(key);
                relation.SetChildKeyConstraint(foreignKey);
            }

            protected override void RemoveCore(DataRelation relation)
            {
                base.RemoveCore(relation);

                _dataSet.OnRemoveRelationHack(relation);

                relation.SetDataSet(null);
                relation.ChildKey.GetSortIndex().RemoveRef();
                if (relation.Nested)
                {
                    relation.ChildTable.CacheNestedParent();
                }

                for (int i = 0; i < _relations.Count; i++)
                {
                    if (relation == _relations[i])
                    {
                        _relations.RemoveAt(i);
                        ((DataTableRelationCollection)(relation.ParentTable.ChildRelations)).Remove(relation); // Remove Cache from ParentTable -> ChildRelations
                        ((DataTableRelationCollection)(relation.ChildTable.ParentRelations)).Remove(relation); // Removing Cache from ChildTable -> ParentRelations
                        if (relation.Nested)
                        {
                            relation.ChildTable.CacheNestedParent();
                        }

                        UnregisterName(relation.RelationName);

                        relation.SetParentKeyConstraint(null);
                        relation.SetChildKeyConstraint(null);

                        return;
                    }
                }
                throw ExceptionBuilder.RelationDoesNotExist();
            }

            internal void FinishInitRelations()
            {
                if (_delayLoadingRelations == null)
                {
                    return;
                }

                DataRelation rel;
                int colCount;
                DataColumn[] parents, childs;
                for (int i = 0; i < _delayLoadingRelations.Length; i++)
                {
                    rel = _delayLoadingRelations[i];
                    if (rel._parentColumnNames == null || rel._childColumnNames == null)
                    {
                        Add(rel);
                        continue;
                    }

                    colCount = rel._parentColumnNames.Length;
                    parents = new DataColumn[colCount];
                    childs = new DataColumn[colCount];

                    for (int j = 0; j < colCount; j++)
                    {
                        if (rel._parentTableNamespace == null)
                        {
                            parents[j] = _dataSet.Tables[rel._parentTableName!]!.Columns[rel._parentColumnNames[j]]!;
                        }
                        else
                        {
                            parents[j] = _dataSet.Tables[rel._parentTableName!, rel._parentTableNamespace]!.Columns[rel._parentColumnNames[j]]!;
                        }

                        if (rel._childTableNamespace == null)
                        {
                            childs[j] = _dataSet.Tables[rel._childTableName!]!.Columns[rel._childColumnNames[j]]!;
                        }
                        else
                        {
                            childs[j] = _dataSet.Tables[rel._childTableName!, rel._childTableNamespace]!.Columns[rel._childColumnNames[j]]!;
                        }
                    }

                    DataRelation newRelation = new DataRelation(rel._relationName, parents, childs, false);
                    newRelation.Nested = rel._nested;
                    Add(newRelation);
                }

                _delayLoadingRelations = null;
            }
        }
    }
}
