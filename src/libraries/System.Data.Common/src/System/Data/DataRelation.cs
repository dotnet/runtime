// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************************************
Rules for Multiple Nested Parent, enforce following constraints

1) At all times, only 1(ONE) FK can be NON-Null in a row.
2) NULL FK values are not associated with PARENT(x), even if PK is NULL in Parent
3) Enforce <rule 1> when
        a) Any FK value is changed
        b) A relation created that result in Multiple Nested Child

WriteXml

1) WriteXml will throw if <rule 1> is violated
2) if NON-Null FK has parentRow (boolean check) print as Nested, else it will get written as normal row

additional notes:
We decided to enforce the rule 1 just if Xml being persisted
******************************************************************************************************/

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Data.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Data
{
    [DefaultProperty(nameof(RelationName))]
    [Editor("Microsoft.VSDesigner.Data.Design.DataRelationEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [TypeConverter(typeof(RelationshipConverter))]
    public class DataRelation
    {
        // properties
        private DataSet? _dataSet;
        internal PropertyCollection? _extendedProperties;
        internal string _relationName = string.Empty;

        // state
        private DataKey _childKey;
        private DataKey _parentKey;
        private UniqueConstraint? _parentKeyConstraint;
        private ForeignKeyConstraint? _childKeyConstraint;

        // Design time serialization
        internal string[]? _parentColumnNames;
        internal string[]? _childColumnNames;
        internal string? _parentTableName;
        internal string? _childTableName;
        internal string? _parentTableNamespace;
        internal string? _childTableNamespace;

        /// <summary>
        /// This stores whether the  child element appears beneath the parent in the XML persisted files.
        /// </summary>
        internal bool _nested;

        /// <summary>
        /// This stores whether the relationship should make sure that KeyConstraints and ForeignKeyConstraints
        /// exist when added to the ConstraintsCollections of the table.
        /// </summary>
        internal bool _createConstraints;

        private bool _checkMultipleNested = true;

        private static int s_objectTypeCount; // Bid counter
        private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataRelation'/> class using the specified name,
        /// parent, and child columns.
        /// </summary>
        public DataRelation(string? relationName, DataColumn parentColumn, DataColumn childColumn) :
            this(relationName, parentColumn, childColumn, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataRelation'/> class using the specified name, parent, and child columns, and
        /// value to create constraints.
        /// </summary>
        public DataRelation(string? relationName, DataColumn parentColumn, DataColumn childColumn, bool createConstraints)
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelation.DataRelation|API> {0}, relationName='{1}', parentColumn={2}, childColumn={3}, createConstraints={4}",
                            ObjectID, relationName, (parentColumn != null) ? parentColumn.ObjectID : 0, (childColumn != null) ? childColumn.ObjectID : 0,
                            createConstraints);

            DataColumn[] parentColumns = new DataColumn[1];
            parentColumns[0] = parentColumn!;
            DataColumn[] childColumns = new DataColumn[1];
            childColumns[0] = childColumn!;
            Create(relationName, parentColumns, childColumns, createConstraints);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataRelation'/> class using the specified name
        /// and matched arrays of parent and child columns.
        /// </summary>
        public DataRelation(string? relationName, DataColumn[] parentColumns, DataColumn[] childColumns) :
            this(relationName, parentColumns, childColumns, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataRelation'/> class using the specified name, matched arrays of parent
        /// and child columns, and value to create constraints.
        /// </summary>
        public DataRelation(string? relationName, DataColumn[] parentColumns, DataColumn[] childColumns, bool createConstraints)
        {
            Create(relationName, parentColumns, childColumns, createConstraints);
        }

        [Browsable(false)] // design-time ctor
        public DataRelation(string relationName, string? parentTableName, string? childTableName, string[]? parentColumnNames, string[]? childColumnNames, bool nested)
        {
            _relationName = relationName;
            _parentColumnNames = parentColumnNames;
            _childColumnNames = childColumnNames;
            _parentTableName = parentTableName;
            _childTableName = childTableName;
            _nested = nested;
        }

        [Browsable(false)] // design-time ctor
        public DataRelation(string relationName, string? parentTableName, string? parentTableNamespace, string? childTableName, string? childTableNamespace, string[]? parentColumnNames, string[]? childColumnNames, bool nested)
        {
            _relationName = relationName;
            _parentColumnNames = parentColumnNames;
            _childColumnNames = childColumnNames;
            _parentTableName = parentTableName;
            _childTableName = childTableName;
            _parentTableNamespace = parentTableNamespace;
            _childTableNamespace = childTableNamespace;
            _nested = nested;
        }

        /// <summary>
        /// Gets the child columns of this relation.
        /// </summary>
        public virtual DataColumn[] ChildColumns
        {
            get
            {
                CheckStateForProperty();
                return _childKey.ToArray();
            }
        }

        internal DataColumn[] ChildColumnsReference
        {
            get
            {
                CheckStateForProperty();
                return _childKey.ColumnsReference;
            }
        }

        /// <summary>
        /// The internal Key object for the child table.
        /// </summary>
        internal DataKey ChildKey
        {
            get
            {
                CheckStateForProperty();
                return _childKey;
            }
        }

        /// <summary>
        /// Gets the child table of this relation.
        /// </summary>
        public virtual DataTable ChildTable
        {
            get
            {
                CheckStateForProperty();
                return _childKey.Table;
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Data.DataSet'/> to which the relations' collection belongs to.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public virtual DataSet? DataSet
        {
            get
            {
                CheckStateForProperty();
                return _dataSet;
            }
        }

        internal string[] ParentColumnNames => _parentKey.GetColumnNames();

        internal string[] ChildColumnNames => _childKey.GetColumnNames();

        private static bool IsKeyNull(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!DataStorage.IsObjectNull(values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the child rows for the parent row across the relation using the version given
        /// </summary>
        internal static DataRow[] GetChildRows(DataKey parentKey, DataKey childKey, DataRow parentRow, DataRowVersion version)
        {
            object[] values = parentRow.GetKeyValues(parentKey, version);
            if (IsKeyNull(values))
            {
                return childKey.Table.NewRowArray(0);
            }

            Index index = childKey.GetSortIndex((version == DataRowVersion.Original) ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
            return index.GetRows(values);
        }

        /// <summary>
        /// Gets the parent rows for the given child row across the relation using the version given
        /// </summary>
        internal static DataRow[] GetParentRows(DataKey parentKey, DataKey childKey, DataRow childRow, DataRowVersion version)
        {
            object[] values = childRow.GetKeyValues(childKey, version);
            if (IsKeyNull(values))
            {
                return parentKey.Table.NewRowArray(0);
            }

            Index index = parentKey.GetSortIndex((version == DataRowVersion.Original) ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
            return index.GetRows(values);
        }

        internal static DataRow? GetParentRow(DataKey parentKey, DataKey childKey, DataRow childRow, DataRowVersion version)
        {
            if (!childRow.HasVersion((version == DataRowVersion.Original) ? DataRowVersion.Original : DataRowVersion.Current))
            {
                if (childRow._tempRecord == -1)
                {
                    return null;
                }
            }

            object[] values = childRow.GetKeyValues(childKey, version);
            if (IsKeyNull(values))
            {
                return null;
            }

            Index index = parentKey.GetSortIndex((version == DataRowVersion.Original) ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
            Range range = index.FindRecords(values);
            if (range.IsNull)
            {
                return null;
            }

            if (range.Count > 1)
            {
                throw ExceptionBuilder.MultipleParents();
            }

            return parentKey.Table._recordManager[index.GetRecord(range.Min)];
        }


        /// <summary>
        /// Internally sets the DataSet pointer.
        /// </summary>
        internal void SetDataSet(DataSet? dataSet)
        {
            if (_dataSet != dataSet)
            {
                _dataSet = dataSet;
            }
        }

        internal void SetParentRowRecords(DataRow childRow, DataRow parentRow)
        {
            object[] parentKeyValues = parentRow.GetKeyValues(ParentKey);
            if (childRow._tempRecord != -1)
            {
                RecordManager.SetKeyValues(childRow._tempRecord, ChildKey, parentKeyValues);
            }
            if (childRow._newRecord != -1)
            {
                RecordManager.SetKeyValues(childRow._newRecord, ChildKey, parentKeyValues);
            }
            if (childRow._oldRecord != -1)
            {
                RecordManager.SetKeyValues(childRow._oldRecord, ChildKey, parentKeyValues);
            }
        }

        /// <summary>
        /// Gets the parent columns of this relation.
        /// </summary>
        public virtual DataColumn[] ParentColumns
        {
            get
            {
                CheckStateForProperty();
                return _parentKey.ToArray();
            }
        }

        internal DataColumn[] ParentColumnsReference => _parentKey.ColumnsReference;

        /// <summary>
        /// The internal constraint object for the parent table.
        /// </summary>
        internal DataKey ParentKey
        {
            get
            {
                CheckStateForProperty();
                return _parentKey;
            }
        }

        /// <summary>
        /// Gets the parent table of this relation.
        /// </summary>
        public virtual DataTable ParentTable
        {
            get
            {
                CheckStateForProperty();
                return _parentKey.Table;
            }
        }

        /// <summary>
        /// Gets or sets the name used to look up this relation in the parent
        /// data set's <see cref='System.Data.DataRelationCollection'/>.
        /// </summary>
        [DefaultValue("")]
        [AllowNull]
        public virtual string RelationName
        {
            get
            {
                CheckStateForProperty();
                return _relationName;
            }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelation.set_RelationName|API> {0}, '{1}'", ObjectID, value);
                try
                {
                    if (value == null)
                    {
                        value = string.Empty;
                    }

                    CultureInfo locale = (_dataSet != null ? _dataSet.Locale : CultureInfo.CurrentCulture);
                    if (string.Compare(_relationName, value, true, locale) != 0)
                    {
                        if (_dataSet != null)
                        {
                            if (value.Length == 0)
                            {
                                throw ExceptionBuilder.NoRelationName();
                            }

                            _dataSet.Relations.RegisterName(value);
                            if (_relationName.Length != 0)
                            {
                                _dataSet.Relations.UnregisterName(_relationName);
                            }
                        }
                        _relationName = value;
                        ((DataRelationCollection.DataTableRelationCollection)(ParentTable.ChildRelations)).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
                        ((DataRelationCollection.DataTableRelationCollection)(ChildTable.ParentRelations)).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
                    }
                    else if (string.Compare(_relationName, value, false, locale) != 0)
                    {
                        _relationName = value;
                        ((DataRelationCollection.DataTableRelationCollection)(ParentTable.ChildRelations)).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
                        ((DataRelationCollection.DataTableRelationCollection)(ChildTable.ParentRelations)).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }
        internal void CheckNamespaceValidityForNestedRelations(string ns)
        {
            foreach (DataRelation rel in ChildTable.ParentRelations)
            {
                if (rel == this || rel.Nested)
                {
                    if (rel.ParentTable.Namespace != ns)
                    {
                        throw ExceptionBuilder.InValidNestedRelation(ChildTable.TableName);
                    }
                }
            }
        }

        internal void CheckNestedRelations()
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelation.CheckNestedRelations|INFO> {0}", ObjectID);

            Debug.Assert(DataSet == null || !_nested, "this relation supposed to be not in dataset or not nested");
            // 1. There is no other relation (R) that has this.ChildTable as R.ChildTable
            //  This is not valid for Whidbey anymore so the code has been removed

            // 2. There is no loop in nested relations

            if (ChildTable == ParentTable)
            {
                if (string.Compare(ChildTable.TableName, ChildTable.DataSet!.DataSetName, true, ChildTable.DataSet.Locale) == 0)
                    throw ExceptionBuilder.SelfnestedDatasetConflictingName(ChildTable.TableName);
                return; //allow self join tables.
            }

            List<DataTable> list = new List<DataTable>();
            list.Add(ChildTable);

            // We have already checked for nested relaion UP
            for (int i = 0; i < list.Count; ++i)
            {
                DataRelation[] relations = list[i].NestedParentRelations;
                foreach (DataRelation rel in relations)
                {
                    if (rel.ParentTable == ChildTable && rel.ChildTable != ChildTable)
                    {
                        throw ExceptionBuilder.LoopInNestedRelations(ChildTable.TableName);
                    }
                    if (!list.Contains(rel.ParentTable))
                    {
                        // check for self nested
                        list.Add(rel.ParentTable);
                    }
                }
            }
        }

        /********************
          The Namespace of a table nested inside multiple parents can be
          1. Explicitly specified
          2. Inherited from Parent Table
          3. Empty (Form = unqualified case)
          However, Schema does not allow (3) to be a global element and multiple nested child has to be a global element.
          Therefore we'll reduce case (3) to (2) if all parents have same namespace else throw.
         ********************/

        /// <summary>
        /// Gets or sets a value indicating whether relations are nested.
        /// </summary>
        [DefaultValue(false)]
        public virtual bool Nested
        {
            get
            {
                CheckStateForProperty();
                return _nested;
            }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelation.set_Nested|API> {0}, {1}", ObjectID, value);
                try
                {
                    if (_nested != value)
                    {
                        if (_dataSet != null)
                        {
                            if (value)
                            {
                                if (ChildTable.IsNamespaceInherited())
                                { // if not added to collection, don't do this check
                                    CheckNamespaceValidityForNestedRelations(ParentTable.Namespace);
                                }
                                Debug.Assert(ChildTable != null, "On a DataSet, but not on Table. Bad state");
                                ForeignKeyConstraint? constraint = ChildTable.Constraints.FindForeignKeyConstraint(ChildKey.ColumnsReference, ParentKey.ColumnsReference);
                                if (constraint != null)
                                {
                                    constraint.CheckConstraint();
                                }
                                ValidateMultipleNestedRelations();
                            }
                        }
                        if (!value && (_parentKey.ColumnsReference[0].ColumnMapping == MappingType.Hidden))
                        {
                            throw ExceptionBuilder.RelationNestedReadOnly();
                        }

                        if (value)
                        {
                            ParentTable.Columns.RegisterColumnName(ChildTable.TableName, null);
                        }
                        else
                        {
                            ParentTable.Columns.UnregisterName(ChildTable.TableName);
                        }
                        RaisePropertyChanging(nameof(Nested));

                        if (value)
                        {
                            CheckNestedRelations();
                            if (DataSet != null)
                                if (ParentTable == ChildTable)
                                {
                                    foreach (DataRow row in ChildTable.Rows)
                                    {
                                        row.CheckForLoops(this);
                                    }

                                    if (ChildTable.DataSet != null && (string.Compare(ChildTable.TableName, ChildTable.DataSet.DataSetName, true, ChildTable.DataSet.Locale) == 0))
                                    {
                                        throw ExceptionBuilder.DatasetConflictingName(DataSet.DataSetName);
                                    }
                                    ChildTable._fNestedInDataset = false;
                                }
                                else
                                {
                                    foreach (DataRow row in ChildTable.Rows)
                                    {
                                        row.GetParentRow(this);
                                    }
                                }

                            ParentTable.ElementColumnCount++;
                        }
                        else
                        {
                            ParentTable.ElementColumnCount--;
                        }

                        _nested = value;
                        ChildTable.CacheNestedParent();
                        if (value)
                        {
                            if (string.IsNullOrEmpty(ChildTable.Namespace) && ((ChildTable.NestedParentsCount > 1) ||
                                ((ChildTable.NestedParentsCount > 0) && !(ChildTable.DataSet!.Relations.Contains(RelationName)))))
                            {
                                string? parentNs = null;
                                foreach (DataRelation rel in ChildTable.ParentRelations)
                                {
                                    if (rel.Nested)
                                    {
                                        if (null == parentNs)
                                        {
                                            parentNs = rel.ParentTable.Namespace;
                                        }
                                        else
                                        {
                                            if (!string.Equals(parentNs, rel.ParentTable.Namespace, StringComparison.Ordinal))
                                            {
                                                _nested = false;
                                                throw ExceptionBuilder.InvalidParentNamespaceinNestedRelation(ChildTable.TableName);
                                            }
                                        }
                                    }
                                }
                                // if not already in memory , form == unqualified
                                if (CheckMultipleNested && ChildTable._tableNamespace != null && ChildTable._tableNamespace.Length == 0)
                                {
                                    throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
                                }
                                ChildTable._tableNamespace = null; // if we dont throw, then let it inherit the Namespace
                            }
                        }
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        /// <summary>
        /// Gets the constraint which ensures values in a column are unique.
        /// </summary>
        public virtual UniqueConstraint? ParentKeyConstraint
        {
            get
            {
                CheckStateForProperty();
                return _parentKeyConstraint;
            }
        }

        internal void SetParentKeyConstraint(UniqueConstraint? value)
        {
            Debug.Assert(_parentKeyConstraint == null || value == null, "ParentKeyConstraint should not have been set already.");
            _parentKeyConstraint = value;
        }


        /// <summary>
        /// Gets the <see cref='System.Data.ForeignKeyConstraint'/> for the relation.
        /// </summary>
        public virtual ForeignKeyConstraint? ChildKeyConstraint
        {
            get
            {
                CheckStateForProperty();
                return _childKeyConstraint;
            }
        }

        /// <summary>
        /// Gets the collection of custom user information.
        /// </summary>
        [Browsable(false)]
        public PropertyCollection ExtendedProperties => _extendedProperties ??= new PropertyCollection();

        internal bool CheckMultipleNested
        {
            get { return _checkMultipleNested; }
            set { _checkMultipleNested = value; }
        }

        internal void SetChildKeyConstraint(ForeignKeyConstraint? value)
        {
            Debug.Assert(_childKeyConstraint == null || value == null, "ChildKeyConstraint should not have been set already.");
            _childKeyConstraint = value;
        }

        internal event PropertyChangedEventHandler? PropertyChanging;

        // If we're not in a dataSet relations collection, we need to verify on every property get that we're
        // still a good relation object.
        internal void CheckState()
        {
            if (_dataSet == null)
            {
                _parentKey.CheckState();
                _childKey.CheckState();

                if (_parentKey.Table.DataSet != _childKey.Table.DataSet)
                {
                    throw ExceptionBuilder.RelationDataSetMismatch();
                }

                if (_childKey.ColumnsEqual(_parentKey))
                {
                    throw ExceptionBuilder.KeyColumnsIdentical();
                }

                for (int i = 0; i < _parentKey.ColumnsReference.Length; i++)
                {
                    if ((_parentKey.ColumnsReference[i].DataType != _childKey.ColumnsReference[i].DataType) ||
                        ((_parentKey.ColumnsReference[i].DataType == typeof(DateTime)) &&
                        (_parentKey.ColumnsReference[i].DateTimeMode != _childKey.ColumnsReference[i].DateTimeMode) &&
                        ((_parentKey.ColumnsReference[i].DateTimeMode & _childKey.ColumnsReference[i].DateTimeMode) != DataSetDateTime.Unspecified)))
                    {
                        // allow unspecified and unspecifiedlocal
                        throw ExceptionBuilder.ColumnsTypeMismatch();
                    }
                }
            }
        }

        /// <summary>
        /// Checks to ensure the DataRelation is a valid object, even if it doesn't
        /// belong to a <see cref='System.Data.DataSet'/>.
        /// </summary>
        protected void CheckStateForProperty()
        {
            try
            {
                CheckState();
            }
            catch (Exception e) when (ADP.IsCatchableExceptionType(e))
            {
                throw ExceptionBuilder.BadObjectPropertyAccess(e.Message);
            }
        }

        private void Create(string? relationName, DataColumn[] parentColumns, DataColumn[] childColumns, bool createConstraints)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelation.Create|INFO> {0}, relationName='{1}', createConstraints={2}", ObjectID, relationName, createConstraints);
            try
            {
                _parentKey = new DataKey(parentColumns, true);
                _childKey = new DataKey(childColumns, true);

                if (parentColumns.Length != childColumns.Length)
                {
                    throw ExceptionBuilder.KeyLengthMismatch();
                }

                for (int i = 0; i < parentColumns.Length; i++)
                {
                    if ((parentColumns[i].Table!.DataSet == null) || (childColumns[i].Table!.DataSet == null))
                    {
                        throw ExceptionBuilder.ParentOrChildColumnsDoNotHaveDataSet();
                    }
                }

                CheckState();

                _relationName = relationName ?? "";
                _createConstraints = createConstraints;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal DataRelation Clone(DataSet destination)
        {
            DataCommonEventSource.Log.Trace("<ds.DataRelation.Clone|INFO> {0}, destination={1}", ObjectID, (destination != null) ? destination.ObjectID : 0);
            Debug.Assert(destination != null);

            DataTable parent = destination.Tables[ParentTable.TableName, ParentTable.Namespace]!;
            DataTable child = destination.Tables[ChildTable.TableName, ChildTable.Namespace]!;
            int keyLength = _parentKey.ColumnsReference.Length;

            DataColumn[] parentColumns = new DataColumn[keyLength];
            DataColumn[] childColumns = new DataColumn[keyLength];

            for (int i = 0; i < keyLength; i++)
            {
                parentColumns[i] = parent.Columns[ParentKey.ColumnsReference[i].ColumnName]!;
                childColumns[i] = child.Columns[ChildKey.ColumnsReference[i].ColumnName]!;
            }

            DataRelation clone = new DataRelation(_relationName, parentColumns, childColumns, false);

            clone.CheckMultipleNested = false; // disable the check  in clone as it is already created
            clone.Nested = Nested;
            clone.CheckMultipleNested = true; // enable the check

            // ...Extended Properties
            if (_extendedProperties != null)
            {
                foreach (object key in _extendedProperties.Keys)
                {
                    clone.ExtendedProperties[key] = _extendedProperties[key];
                }
            }
            return clone;
        }

        protected internal void OnPropertyChanging(PropertyChangedEventArgs pcevent)
        {
            if (PropertyChanging != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataRelation.OnPropertyChanging|INFO> {0}", ObjectID);
                PropertyChanging(this, pcevent);
            }
        }

        protected internal void RaisePropertyChanging(string name)
        {
            OnPropertyChanging(new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// </summary>
        public override string ToString() => RelationName;

        internal void ValidateMultipleNestedRelations()
        {
            // find all nested relations that this child table has
            // if this relation is the only relation it has, then fine,
            // otherwise check if all relations are created from XSD, without using Key/KeyRef
            // check all keys to see autogenerated

            if (!Nested || !CheckMultipleNested) // no need for this verification
            {
                return;
            }

            if (0 < ChildTable.NestedParentRelations.Length)
            {
                DataColumn[] childCols = ChildColumns;
                if (childCols.Length != 1 || !IsAutoGenerated(childCols[0]))
                {
                    throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
                }

                if (!XmlTreeGen.AutoGenerated(this))
                {
                    throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
                }

                foreach (Constraint cs in ChildTable.Constraints)
                {
                    if (cs is ForeignKeyConstraint)
                    {
                        ForeignKeyConstraint fk = (ForeignKeyConstraint)cs;
                        if (!XmlTreeGen.AutoGenerated(fk, true))
                        {
                            throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
                        }
                    }
                    else
                    {
                        UniqueConstraint unique = (UniqueConstraint)cs;
                        if (!XmlTreeGen.AutoGenerated(unique))
                        {
                            throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
                        }
                    }
                }
            }
        }

        private bool IsAutoGenerated(DataColumn col)
        {
            if (col.ColumnMapping != MappingType.Hidden)
            {
                return false;
            }

            if (col.DataType != typeof(int))
            {
                return false;
            }

            string generatedname = col.Table!.TableName + "_Id";

            if ((col.ColumnName == generatedname) || (col.ColumnName == generatedname + "_0"))
            {
                return true;
            }

            generatedname = ParentColumnsReference[0].Table!.TableName + "_Id";
            if ((col.ColumnName == generatedname) || (col.ColumnName == generatedname + "_0"))
            {
                return true;
            }

            return false;
        }

        internal int ObjectID => _objectID;
    }
}
