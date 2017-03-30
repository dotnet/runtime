// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Data.Common;

namespace System.Data
{
    /// <summary>
    /// Represents an action restriction enforced on a set of columns in a primary key/foreign key relationship when
    /// a value or row is either deleted or updated.
    /// </summary>
    [DefaultProperty(nameof(ConstraintName))]
    public class ForeignKeyConstraint : Constraint
    {
        // constants
        internal const Rule Rule_Default = Rule.Cascade;
        internal const AcceptRejectRule AcceptRejectRule_Default = AcceptRejectRule.None;

        // properties
        internal Rule _deleteRule = Rule_Default;
        internal Rule _updateRule = Rule_Default;
        internal AcceptRejectRule _acceptRejectRule = AcceptRejectRule_Default;
        private DataKey _childKey;
        private DataKey _parentKey;

        // Design time serialization
        internal string _constraintName = null;
        internal string[] _parentColumnNames = null;
        internal string[] _childColumnNames = null;
        internal string _parentTableName = null;
        internal string _parentTableNamespace = null;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.ForeignKeyConstraint'/> class with the specified parent and
        /// child <see cref='System.Data.DataColumn'/> objects.
        /// </summary>
        public ForeignKeyConstraint(DataColumn parentColumn, DataColumn childColumn) : this(null, parentColumn, childColumn)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.ForeignKeyConstraint'/> class with the specified name,
        /// parent and child <see cref='System.Data.DataColumn'/> objects.
        /// </summary>
        public ForeignKeyConstraint(string constraintName, DataColumn parentColumn, DataColumn childColumn)
        {
            DataColumn[] parentColumns = new DataColumn[] { parentColumn };
            DataColumn[] childColumns = new DataColumn[] { childColumn };
            Create(constraintName, parentColumns, childColumns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.ForeignKeyConstraint'/> class with the specified arrays
        /// of parent and child <see cref='System.Data.DataColumn'/> objects.
        /// </summary>
        public ForeignKeyConstraint(DataColumn[] parentColumns, DataColumn[] childColumns) : this(null, parentColumns, childColumns)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.ForeignKeyConstraint'/> class with the specified name,
        /// and arrays of parent and child <see cref='System.Data.DataColumn'/> objects.
        /// </summary>
        public ForeignKeyConstraint(string constraintName, DataColumn[] parentColumns, DataColumn[] childColumns)
        {
            Create(constraintName, parentColumns, childColumns);
        }

        // construct design time object
        [Browsable(false)]
        public ForeignKeyConstraint(string constraintName, string parentTableName, string[] parentColumnNames, string[] childColumnNames,
                                    AcceptRejectRule acceptRejectRule, Rule deleteRule, Rule updateRule)
        {
            _constraintName = constraintName;
            _parentColumnNames = parentColumnNames;
            _childColumnNames = childColumnNames;
            _parentTableName = parentTableName;
            _acceptRejectRule = acceptRejectRule;
            _deleteRule = deleteRule;
            _updateRule = updateRule;
        }

        // construct design time object
        [Browsable(false)]
        public ForeignKeyConstraint(string constraintName, string parentTableName, string parentTableNamespace, string[] parentColumnNames,
                                    string[] childColumnNames, AcceptRejectRule acceptRejectRule, Rule deleteRule, Rule updateRule)
        {
            _constraintName = constraintName;
            _parentColumnNames = parentColumnNames;
            _childColumnNames = childColumnNames;
            _parentTableName = parentTableName;
            _parentTableNamespace = parentTableNamespace;
            _acceptRejectRule = acceptRejectRule;
            _deleteRule = deleteRule;
            _updateRule = updateRule;
        }

        /// <summary>
        /// The internal constraint object for the child table.
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
        /// Gets the child columns of this constraint.
        /// </summary>
        [ReadOnly(true)]
        public virtual DataColumn[] Columns
        {
            get
            {
                CheckStateForProperty();
                return _childKey.ToArray();
            }
        }

        /// <summary>
        /// Gets the child table of this constraint.
        /// </summary>
        [ReadOnly(true)]
        public override DataTable Table
        {
            get
            {
                CheckStateForProperty();
                return _childKey.Table;
            }
        }

        internal string[] ParentColumnNames => _parentKey.GetColumnNames();

        internal string[] ChildColumnNames => _childKey.GetColumnNames();

        internal override void CheckCanAddToCollection(ConstraintCollection constraints)
        {
            if (Table != constraints.Table)
            {
                throw ExceptionBuilder.ConstraintAddFailed(constraints.Table);
            }
            if (Table.Locale.LCID != RelatedTable.Locale.LCID || Table.CaseSensitive != RelatedTable.CaseSensitive)
            {
                throw ExceptionBuilder.CaseLocaleMismatch();
            }
        }

        internal override bool CanBeRemovedFromCollection(ConstraintCollection constraints, bool fThrowException) => true;

        internal bool IsKeyNull(object[] values)
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

        internal override bool IsConstraintViolated()
        {
            Index childIndex = _childKey.GetSortIndex();
            object[] uniqueChildKeys = childIndex.GetUniqueKeyValues();
            bool errors = false;

            Index parentIndex = _parentKey.GetSortIndex();
            for (int i = 0; i < uniqueChildKeys.Length; i++)
            {
                object[] childValues = (object[])uniqueChildKeys[i];

                if (!IsKeyNull(childValues))
                {
                    if (!parentIndex.IsKeyInIndex(childValues))
                    {
                        DataRow[] rows = childIndex.GetRows(childIndex.FindRecords(childValues));
                        string error = SR.Format(SR.DataConstraint_ForeignKeyViolation, ConstraintName, ExceptionBuilder.KeysToString(childValues));
                        for (int j = 0; j < rows.Length; j++)
                        {
                            rows[j].RowError = error;
                        }
                        errors = true;
                    }
                }
            }
            return errors;
        }

        internal override bool CanEnableConstraint()
        {
            if (Table.DataSet == null || !Table.DataSet.EnforceConstraints)
            {
                return true;
            }

            Index childIndex = _childKey.GetSortIndex();
            object[] uniqueChildKeys = childIndex.GetUniqueKeyValues();

            Index parentIndex = _parentKey.GetSortIndex();
            for (int i = 0; i < uniqueChildKeys.Length; i++)
            {
                object[] childValues = (object[])uniqueChildKeys[i];

                if (!IsKeyNull(childValues) && !parentIndex.IsKeyInIndex(childValues))
                {
                    return false;
                }
            }
            return true;
        }

        internal void CascadeCommit(DataRow row)
        {
            if (row.RowState == DataRowState.Detached)
            {
                return;
            }

            if (_acceptRejectRule == AcceptRejectRule.Cascade)
            {
                Index childIndex = _childKey.GetSortIndex(row.RowState == DataRowState.Deleted ? DataViewRowState.Deleted : DataViewRowState.CurrentRows);
                object[] key = row.GetKeyValues(_parentKey, row.RowState == DataRowState.Deleted ? DataRowVersion.Original : DataRowVersion.Default);
                if (IsKeyNull(key))
                {
                    return;
                }

                Range range = childIndex.FindRecords(key);
                if (!range.IsNull)
                {
                    // Self-referencing table has suspendIndexEvents, in the multi-table scenario the child table hasn't
                    // this allows the self-ref table to maintain the index while in the child-table doesn't
                    DataRow[] rows = childIndex.GetRows(range);
                    foreach (DataRow childRow in rows)
                    {
                        if (DataRowState.Detached != childRow.RowState)
                        {
                            if (childRow._inCascade)
                                continue;
                            childRow.AcceptChanges();
                        }
                    }
                }
            }
        }

        internal void CascadeDelete(DataRow row)
        {
            if (-1 == row._newRecord)
            {
                return;
            }

            object[] currentKey = row.GetKeyValues(_parentKey, DataRowVersion.Current);
            if (IsKeyNull(currentKey))
            {
                return;
            }

            Index childIndex = _childKey.GetSortIndex();
            switch (DeleteRule)
            {
                case Rule.None:
                    {
                        if (row.Table.DataSet.EnforceConstraints)
                        {
                            // if we're not cascading deletes, we should throw if we're going to strand a child row under enforceConstraints.
                            Range range = childIndex.FindRecords(currentKey);
                            if (!range.IsNull)
                            {
                                if (range.Count == 1 && childIndex.GetRow(range.Min) == row)
                                    return;

                                throw ExceptionBuilder.FailedCascadeDelete(ConstraintName);
                            }
                        }
                        break;
                    }

                case Rule.Cascade:
                    {
                        object[] key = row.GetKeyValues(_parentKey, DataRowVersion.Default);
                        Range range = childIndex.FindRecords(key);
                        if (!range.IsNull)
                        {
                            DataRow[] rows = childIndex.GetRows(range);

                            for (int j = 0; j < rows.Length; j++)
                            {
                                DataRow r = rows[j];
                                if (r._inCascade)
                                    continue;
                                r.Table.DeleteRow(r);
                            }
                        }
                        break;
                    }

                case Rule.SetNull:
                    {
                        object[] proposedKey = new object[_childKey.ColumnsReference.Length];
                        for (int i = 0; i < _childKey.ColumnsReference.Length; i++)
                            proposedKey[i] = DBNull.Value;
                        Range range = childIndex.FindRecords(currentKey);
                        if (!range.IsNull)
                        {
                            DataRow[] rows = childIndex.GetRows(range);
                            for (int j = 0; j < rows.Length; j++)
                            {
                                // if (rows[j].inCascade)
                                //    continue;
                                if (row != rows[j])
                                    rows[j].SetKeyValues(_childKey, proposedKey);
                            }
                        }
                        break;
                    }
                case Rule.SetDefault:
                    {
                        object[] proposedKey = new object[_childKey.ColumnsReference.Length];
                        for (int i = 0; i < _childKey.ColumnsReference.Length; i++)
                            proposedKey[i] = _childKey.ColumnsReference[i].DefaultValue;
                        Range range = childIndex.FindRecords(currentKey);
                        if (!range.IsNull)
                        {
                            DataRow[] rows = childIndex.GetRows(range);
                            for (int j = 0; j < rows.Length; j++)
                            {
                                // if (rows[j].inCascade)
                                //    continue;
                                if (row != rows[j])
                                    rows[j].SetKeyValues(_childKey, proposedKey);
                            }
                        }
                        break;
                    }
                default:
                    {
                        Debug.Assert(false, "Unknown Rule value");
                        break;
                    }
            }
        }

        internal void CascadeRollback(DataRow row)
        {
            Index childIndex = _childKey.GetSortIndex(row.RowState == DataRowState.Deleted ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
            object[] key = row.GetKeyValues(_parentKey, row.RowState == DataRowState.Modified ? DataRowVersion.Current : DataRowVersion.Default);

            if (IsKeyNull(key))
            {
                return;
            }

            Range range = childIndex.FindRecords(key);
            if (_acceptRejectRule == AcceptRejectRule.Cascade)
            {
                if (!range.IsNull)
                {
                    DataRow[] rows = childIndex.GetRows(range);
                    for (int j = 0; j < rows.Length; j++)
                    {
                        if (rows[j]._inCascade)
                            continue;
                        rows[j].RejectChanges();
                    }
                }
            }
            else
            {
                // AcceptRejectRule.None
                if (row.RowState != DataRowState.Deleted && row.Table.DataSet.EnforceConstraints)
                {
                    if (!range.IsNull)
                    {
                        if (range.Count == 1 && childIndex.GetRow(range.Min) == row)
                            return;

                        if (row.HasKeyChanged(_parentKey))
                        {// if key is not changed, this will not cause child to be stranded
                            throw ExceptionBuilder.FailedCascadeUpdate(ConstraintName);
                        }
                    }
                }
            }
        }

        internal void CascadeUpdate(DataRow row)
        {
            if (-1 == row._newRecord)
            {
                return;
            }

            object[] currentKey = row.GetKeyValues(_parentKey, DataRowVersion.Current);
            if (!Table.DataSet._fInReadXml && IsKeyNull(currentKey))
            {
                return;
            }

            Index childIndex = _childKey.GetSortIndex();
            switch (UpdateRule)
            {
                case Rule.None:
                    {
                        if (row.Table.DataSet.EnforceConstraints)
                        {
                            // if we're not cascading deletes, we should throw if we're going to strand a child row under enforceConstraints.
                            Range range = childIndex.FindRecords(currentKey);
                            if (!range.IsNull)
                            {
                                throw ExceptionBuilder.FailedCascadeUpdate(ConstraintName);
                            }
                        }
                        break;
                    }

                case Rule.Cascade:
                    {
                        Range range = childIndex.FindRecords(currentKey);
                        if (!range.IsNull)
                        {
                            object[] proposedKey = row.GetKeyValues(_parentKey, DataRowVersion.Proposed);
                            DataRow[] rows = childIndex.GetRows(range);
                            for (int j = 0; j < rows.Length; j++)
                            {
                                // if (rows[j].inCascade)
                                //    continue;
                                rows[j].SetKeyValues(_childKey, proposedKey);
                            }
                        }
                        break;
                    }

                case Rule.SetNull:
                    {
                        object[] proposedKey = new object[_childKey.ColumnsReference.Length];
                        for (int i = 0; i < _childKey.ColumnsReference.Length; i++)
                            proposedKey[i] = DBNull.Value;
                        Range range = childIndex.FindRecords(currentKey);
                        if (!range.IsNull)
                        {
                            DataRow[] rows = childIndex.GetRows(range);
                            for (int j = 0; j < rows.Length; j++)
                            {
                                // if (rows[j].inCascade)
                                //    continue;
                                rows[j].SetKeyValues(_childKey, proposedKey);
                            }
                        }
                        break;
                    }
                case Rule.SetDefault:
                    {
                        object[] proposedKey = new object[_childKey.ColumnsReference.Length];
                        for (int i = 0; i < _childKey.ColumnsReference.Length; i++)
                            proposedKey[i] = _childKey.ColumnsReference[i].DefaultValue;
                        Range range = childIndex.FindRecords(currentKey);
                        if (!range.IsNull)
                        {
                            DataRow[] rows = childIndex.GetRows(range);
                            for (int j = 0; j < rows.Length; j++)
                            {
                                // if (rows[j].inCascade)
                                //    continue;
                                rows[j].SetKeyValues(_childKey, proposedKey);
                            }
                        }
                        break;
                    }
                default:
                    {
                        Debug.Assert(false, "Unknown Rule value");
                        break;
                    }
            }
        }

        internal void CheckCanClearParentTable(DataTable table)
        {
            if (Table.DataSet.EnforceConstraints && Table.Rows.Count > 0)
            {
                throw ExceptionBuilder.FailedClearParentTable(table.TableName, ConstraintName, Table.TableName);
            }
        }

        internal void CheckCanRemoveParentRow(DataRow row)
        {
            Debug.Assert(Table.DataSet != null, "Relation " + ConstraintName + " isn't part of a DataSet, so this check shouldn't be happening.");
            if (!Table.DataSet.EnforceConstraints)
            {
                return;
            }
            if (DataRelation.GetChildRows(ParentKey, ChildKey, row, DataRowVersion.Default).Length > 0)
            {
                throw ExceptionBuilder.RemoveParentRow(this);
            }
        }

        internal void CheckCascade(DataRow row, DataRowAction action)
        {
            Debug.Assert(Table.DataSet != null, "ForeignKeyConstraint " + ConstraintName + " isn't part of a DataSet, so this check shouldn't be happening.");

            if (row._inCascade)
            {
                return;
            }

            row._inCascade = true;
            try
            {
                if (action == DataRowAction.Change)
                {
                    if (row.HasKeyChanged(_parentKey))
                    {
                        CascadeUpdate(row);
                    }
                }
                else if (action == DataRowAction.Delete)
                {
                    CascadeDelete(row);
                }
                else if (action == DataRowAction.Commit)
                {
                    CascadeCommit(row);
                }
                else if (action == DataRowAction.Rollback)
                {
                    CascadeRollback(row);
                }
                else if (action == DataRowAction.Add)
                {
                }
                else
                {
                    Debug.Assert(false, "attempt to cascade unknown action: " + action.ToString());
                }
            }
            finally
            {
                row._inCascade = false;
            }
        }

        internal override void CheckConstraint(DataRow childRow, DataRowAction action)
        {
            if ((action == DataRowAction.Change ||
                 action == DataRowAction.Add ||
                 action == DataRowAction.Rollback) &&
                Table.DataSet != null && Table.DataSet.EnforceConstraints &&
                childRow.HasKeyChanged(_childKey))
            {
                // This branch is for cascading case verification.
                DataRowVersion version = (action == DataRowAction.Rollback) ? DataRowVersion.Original : DataRowVersion.Current;
                object[] childKeyValues = childRow.GetKeyValues(_childKey);
                // check to see if this is just a change to my parent's proposed value.
                if (childRow.HasVersion(version))
                {
                    // this is the new proposed value for the parent.
                    DataRow parentRow = DataRelation.GetParentRow(ParentKey, ChildKey, childRow, version);
                    if (parentRow != null && parentRow._inCascade)
                    {
                        object[] parentKeyValues = parentRow.GetKeyValues(_parentKey, action == DataRowAction.Rollback ? version : DataRowVersion.Default);

                        int parentKeyValuesRecord = childRow.Table.NewRecord();
                        childRow.Table.SetKeyValues(_childKey, parentKeyValues, parentKeyValuesRecord);
                        if (_childKey.RecordsEqual(childRow._tempRecord, parentKeyValuesRecord))
                        {
                            return;
                        }
                    }
                }

                // now check to see if someone exists... it will have to be in a parent row's current, not a proposed.
                object[] childValues = childRow.GetKeyValues(_childKey);
                if (!IsKeyNull(childValues))
                {
                    Index parentIndex = _parentKey.GetSortIndex();
                    if (!parentIndex.IsKeyInIndex(childValues))
                    {
                        // could be self-join constraint
                        if (_childKey.Table == _parentKey.Table && childRow._tempRecord != -1)
                        {
                            int lo = 0;
                            for (lo = 0; lo < childValues.Length; lo++)
                            {
                                DataColumn column = _parentKey.ColumnsReference[lo];
                                object value = column.ConvertValue(childValues[lo]);
                                if (0 != column.CompareValueTo(childRow._tempRecord, value))
                                {
                                    break;
                                }
                            }
                            if (lo == childValues.Length)
                            {
                                return;
                            }
                        }
                        throw ExceptionBuilder.ForeignKeyViolation(ConstraintName, childKeyValues);
                    }
                }
            }
        }

        private void NonVirtualCheckState()
        {
            if (_DataSet == null)
            {
                // Make sure columns arrays are valid
                _parentKey.CheckState();
                _childKey.CheckState();

                if (_parentKey.Table.DataSet != _childKey.Table.DataSet)
                {
                    throw ExceptionBuilder.TablesInDifferentSets();
                }

                for (int i = 0; i < _parentKey.ColumnsReference.Length; i++)
                {
                    if (_parentKey.ColumnsReference[i].DataType != _childKey.ColumnsReference[i].DataType ||
                        ((_parentKey.ColumnsReference[i].DataType == typeof(DateTime)) && (_parentKey.ColumnsReference[i].DateTimeMode != _childKey.ColumnsReference[i].DateTimeMode) && ((_parentKey.ColumnsReference[i].DateTimeMode & _childKey.ColumnsReference[i].DateTimeMode) != DataSetDateTime.Unspecified)))
                        throw ExceptionBuilder.ColumnsTypeMismatch();
                }

                if (_childKey.ColumnsEqual(_parentKey))
                {
                    throw ExceptionBuilder.KeyColumnsIdentical();
                }
            }
        }

        // If we're not in a DataSet relations collection, we need to verify on every property get that we're
        // still a good relation object.
        internal override void CheckState() => NonVirtualCheckState();

        /// <summary>
        /// Indicates what kind of action should take place across
        /// this constraint when <see cref='System.Data.DataTable.AcceptChanges'/>
        /// is invoked.
        /// </summary>
        [DefaultValue(AcceptRejectRule_Default)]
        public virtual AcceptRejectRule AcceptRejectRule
        {
            get
            {
                CheckStateForProperty();
                return _acceptRejectRule;
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case AcceptRejectRule.None:
                    case AcceptRejectRule.Cascade:
                        _acceptRejectRule = value;
                        break;
                    default:
                        throw ADP.InvalidAcceptRejectRule(value);
                }
            }
        }

        internal override bool ContainsColumn(DataColumn column) =>
            _parentKey.ContainsColumn(column) || _childKey.ContainsColumn(column);

        internal override Constraint Clone(DataSet destination) => Clone(destination, false);

        internal override Constraint Clone(DataSet destination, bool ignorNSforTableLookup)
        {
            int iDest;
            if (ignorNSforTableLookup)
            {
                iDest = destination.Tables.IndexOf(Table.TableName);
            }
            else
            {
                iDest = destination.Tables.IndexOf(Table.TableName, Table.Namespace, false); // pass false for last param 
                // to be backward compatable, otherwise meay cause new exception
            }

            if (iDest < 0)
            {
                return null;
            }

            DataTable table = destination.Tables[iDest];
            if (ignorNSforTableLookup)
            {
                iDest = destination.Tables.IndexOf(RelatedTable.TableName);
            }
            else
            {
                iDest = destination.Tables.IndexOf(RelatedTable.TableName, RelatedTable.Namespace, false);// pass false for last param 
            }
            if (iDest < 0)
            {
                return null;
            }

            DataTable relatedTable = destination.Tables[iDest];

            int keys = Columns.Length;
            DataColumn[] columns = new DataColumn[keys];
            DataColumn[] relatedColumns = new DataColumn[keys];

            for (int i = 0; i < keys; i++)
            {
                DataColumn src = Columns[i];
                iDest = table.Columns.IndexOf(src.ColumnName);
                if (iDest < 0)
                {
                    return null;
                }
                columns[i] = table.Columns[iDest];

                src = RelatedColumnsReference[i];
                iDest = relatedTable.Columns.IndexOf(src.ColumnName);
                if (iDest < 0)
                {
                    return null;
                }
                relatedColumns[i] = relatedTable.Columns[iDest];
            }
            ForeignKeyConstraint clone = new ForeignKeyConstraint(ConstraintName, relatedColumns, columns);
            clone.UpdateRule = UpdateRule;
            clone.DeleteRule = DeleteRule;
            clone.AcceptRejectRule = AcceptRejectRule;

            // ...Extended Properties
            foreach (object key in ExtendedProperties.Keys)
            {
                clone.ExtendedProperties[key] = ExtendedProperties[key];
            }

            return clone;
        }


        internal ForeignKeyConstraint Clone(DataTable destination)
        {
            Debug.Assert(Table == RelatedTable, "We call this clone just if we have the same datatable as parent and child ");
            int keys = Columns.Length;
            DataColumn[] columns = new DataColumn[keys];
            DataColumn[] relatedColumns = new DataColumn[keys];

            int iDest = 0;

            for (int i = 0; i < keys; i++)
            {
                DataColumn src = Columns[i];
                iDest = destination.Columns.IndexOf(src.ColumnName);
                if (iDest < 0)
                {
                    return null;
                }
                columns[i] = destination.Columns[iDest];

                src = RelatedColumnsReference[i];
                iDest = destination.Columns.IndexOf(src.ColumnName);
                if (iDest < 0)
                {
                    return null;
                }
                relatedColumns[i] = destination.Columns[iDest];
            }
            ForeignKeyConstraint clone = new ForeignKeyConstraint(ConstraintName, relatedColumns, columns);
            clone.UpdateRule = UpdateRule;
            clone.DeleteRule = DeleteRule;
            clone.AcceptRejectRule = AcceptRejectRule;

            // ...Extended Properties
            foreach (object key in ExtendedProperties.Keys)
            {
                clone.ExtendedProperties[key] = ExtendedProperties[key];
            }

            return clone;
        }

        private void Create(string relationName, DataColumn[] parentColumns, DataColumn[] childColumns)
        {
            if (parentColumns.Length == 0 || childColumns.Length == 0)
            {
                throw ExceptionBuilder.KeyLengthZero();
            }
            if (parentColumns.Length != childColumns.Length)
            {
                throw ExceptionBuilder.KeyLengthMismatch();
            }

            for (int i = 0; i < parentColumns.Length; i++)
            {
                if (parentColumns[i].Computed)
                {
                    throw ExceptionBuilder.ExpressionInConstraint(parentColumns[i]);
                }
                if (childColumns[i].Computed)
                {
                    throw ExceptionBuilder.ExpressionInConstraint(childColumns[i]);
                }
            }

            _parentKey = new DataKey(parentColumns, true);
            _childKey = new DataKey(childColumns, true);

            ConstraintName = relationName;

            NonVirtualCheckState();
        }

        /// <summary>
        ///  Gets or sets the action that occurs across this constraint when a row is deleted.
        /// </summary>
        [DefaultValue(Rule_Default)]
        public virtual Rule DeleteRule
        {
            get
            {
                CheckStateForProperty();
                return _deleteRule;
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case Rule.None:
                    case Rule.Cascade:
                    case Rule.SetNull:
                    case Rule.SetDefault:
                        _deleteRule = value;
                        break;
                    default:
                        throw ADP.InvalidRule(value);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current <see cref='System.Data.ForeignKeyConstraint'/> is identical to the specified object.
        /// </summary>
        public override bool Equals(object key)
        {
            if (!(key is ForeignKeyConstraint))
            {
                return false;
            }
            ForeignKeyConstraint key2 = (ForeignKeyConstraint)key;

            // The ParentKey and ChildKey completely identify the ForeignKeyConstraint
            return ParentKey.ColumnsEqual(key2.ParentKey) && ChildKey.ColumnsEqual(key2.ChildKey);
        }

        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// The parent columns of this constraint.
        /// </summary>
        [ReadOnly(true)]
        public virtual DataColumn[] RelatedColumns
        {
            get
            {
                CheckStateForProperty();
                return _parentKey.ToArray();
            }
        }

        internal DataColumn[] RelatedColumnsReference
        {
            get
            {
                CheckStateForProperty();
                return _parentKey.ColumnsReference;
            }
        }

        /// <summary>
        /// The internal key object for the parent table.
        /// </summary>
        internal DataKey ParentKey
        {
            get
            {
                CheckStateForProperty();
                return _parentKey;
            }
        }

        internal DataRelation FindParentRelation()
        {
            DataRelationCollection rels = Table.ParentRelations;

            for (int i = 0; i < rels.Count; i++)
            {
                if (rels[i].ChildKeyConstraint == this)
                {
                    return rels[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the parent table of this constraint.
        /// </summary>
        [ReadOnly(true)]
        public virtual DataTable RelatedTable
        {
            get
            {
                CheckStateForProperty();
                return _parentKey.Table;
            }
        }

        /// <summary>
        /// Gets or sets the action that occurs across this constraint on when a row is updated.
        /// </summary>
        [DefaultValue(Rule_Default)]
        public virtual Rule UpdateRule
        {
            get
            {
                CheckStateForProperty();
                return _updateRule;
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case Rule.None:
                    case Rule.Cascade:
                    case Rule.SetNull:
                    case Rule.SetDefault:
                        _updateRule = value;
                        break;
                    default:
                        throw ADP.InvalidRule(value);
                }
            }
        }
    }
}
