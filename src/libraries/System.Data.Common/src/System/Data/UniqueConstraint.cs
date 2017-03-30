// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.ComponentModel;

namespace System.Data
{
    /// <summary>
    /// Represents a restriction on a set of columns in which all values must be unique.
    /// </summary>
    [DefaultProperty("ConstraintName")]
    public class UniqueConstraint : Constraint
    {
        private DataKey _key;
        private Index _constraintIndex;
        internal bool _bPrimaryKey = false;

        // Design time serialization
        internal string _constraintName = null;
        internal string[] _columnNames = null;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the specified name and
        /// <see cref='System.Data.DataColumn'/>.
        /// </summary>
        public UniqueConstraint(string name, DataColumn column)
        {
            DataColumn[] columns = new DataColumn[1];
            columns[0] = column;
            Create(name, columns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the specified <see cref='System.Data.DataColumn'/>.
        /// </summary>
        public UniqueConstraint(DataColumn column)
        {
            DataColumn[] columns = new DataColumn[1];
            columns[0] = column;
            Create(null, columns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the specified name and array
        ///    of <see cref='System.Data.DataColumn'/> objects.
        /// </summary>
        public UniqueConstraint(string name, DataColumn[] columns)
        {
            Create(name, columns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the given array of <see cref='System.Data.DataColumn'/>
        /// objects.
        /// </summary>
        public UniqueConstraint(DataColumn[] columns)
        {
            Create(null, columns);
        }

        // Construct design time object
        [Browsable(false)]
        public UniqueConstraint(string name, string[] columnNames, bool isPrimaryKey)
        {
            _constraintName = name;
            _columnNames = columnNames;
            _bPrimaryKey = isPrimaryKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the specified name and
        /// <see cref='System.Data.DataColumn'/>.
        /// </summary>
        public UniqueConstraint(string name, DataColumn column, bool isPrimaryKey)
        {
            DataColumn[] columns = new DataColumn[1];
            columns[0] = column;
            _bPrimaryKey = isPrimaryKey;
            Create(name, columns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the specified <see cref='System.Data.DataColumn'/>.
        /// </summary>
        public UniqueConstraint(DataColumn column, bool isPrimaryKey)
        {
            DataColumn[] columns = new DataColumn[1];
            columns[0] = column;
            _bPrimaryKey = isPrimaryKey;
            Create(null, columns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the specified name and array
        ///    of <see cref='System.Data.DataColumn'/> objects.
        /// </summary>
        public UniqueConstraint(string name, DataColumn[] columns, bool isPrimaryKey)
        {
            _bPrimaryKey = isPrimaryKey;
            Create(name, columns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.UniqueConstraint'/> with the given array of <see cref='System.Data.DataColumn'/>
        /// objects.
        /// </summary>
        public UniqueConstraint(DataColumn[] columns, bool isPrimaryKey)
        {
            _bPrimaryKey = isPrimaryKey;
            Create(null, columns);
        }

        // design time serialization only
        internal string[] ColumnNames
        {
            get
            {
                return _key.GetColumnNames();
            }
        }

        // Use constraint index only for search operations (and use key.GetSortIndex() when enumeration is needed and/or order is important)
        internal Index ConstraintIndex
        {
            get
            {
                AssertConstraintAndKeyIndexes();
                return _constraintIndex;
            }
        }

        [Conditional("DEBUG")]
        private void AssertConstraintAndKeyIndexes()
        {
            Debug.Assert(null != _constraintIndex, "null UniqueConstraint index");

            // ideally, we would like constraintIndex and key.GetSortIndex to share the same index underneath: Debug.Assert(_constraintIndex == key.GetSortIndex)
            // but, there is a scenario where constraint and key indexes are built from the same list of columns but in a different order
            DataColumn[] sortIndexColumns = new DataColumn[_constraintIndex._indexFields.Length];
            for (int i = 0; i < sortIndexColumns.Length; i++)
            {
                sortIndexColumns[i] = _constraintIndex._indexFields[i].Column;
            }
            Debug.Assert(DataKey.ColumnsEqual(_key.ColumnsReference, sortIndexColumns), "UniqueConstraint index columns do not match the key sort index");
        }

        internal void ConstraintIndexClear()
        {
            if (null != _constraintIndex)
            {
                _constraintIndex.RemoveRef();
                _constraintIndex = null;
            }
        }

        internal void ConstraintIndexInitialize()
        {
            if (null == _constraintIndex)
            {
                _constraintIndex = _key.GetSortIndex();
                _constraintIndex.AddRef();
            }

            AssertConstraintAndKeyIndexes();
        }

        internal override void CheckState()
        {
            NonVirtualCheckState();
        }

        private void NonVirtualCheckState()
        {
            _key.CheckState();
        }

        internal override void CheckCanAddToCollection(ConstraintCollection constraints)
        {
        }

        internal override bool CanBeRemovedFromCollection(ConstraintCollection constraints, bool fThrowException)
        {
            if (Equals(constraints.Table._primaryKey))
            {
                Debug.Assert(constraints.Table._primaryKey == this, "If the primary key and this are 'Equal', they should also be '=='");
                if (!fThrowException)
                    return false;
                else
                    throw ExceptionBuilder.RemovePrimaryKey(constraints.Table);
            }
            for (ParentForeignKeyConstraintEnumerator cs = new ParentForeignKeyConstraintEnumerator(Table.DataSet, Table); cs.GetNext();)
            {
                ForeignKeyConstraint constraint = cs.GetForeignKeyConstraint();
                if (!_key.ColumnsEqual(constraint.ParentKey))
                    continue;

                if (!fThrowException)
                    return false;
                else
                    throw ExceptionBuilder.NeededForForeignKeyConstraint(this, constraint);
            }

            return true;
        }

        internal override bool CanEnableConstraint()
        {
            if (Table.EnforceConstraints)
                return ConstraintIndex.CheckUnique();

            return true;
        }

        internal override bool IsConstraintViolated()
        {
            bool result = false;
            Index index = ConstraintIndex;
            if (index.HasDuplicates)
            {
                object[] uniqueKeys = index.GetUniqueKeyValues();

                for (int i = 0; i < uniqueKeys.Length; i++)
                {
                    Range r = index.FindRecords((object[])uniqueKeys[i]);
                    if (1 < r.Count)
                    {
                        DataRow[] rows = index.GetRows(r);
                        string error = ExceptionBuilder.UniqueConstraintViolationText(_key.ColumnsReference, (object[])uniqueKeys[i]);
                        for (int j = 0; j < rows.Length; j++)
                        {
                            rows[j].RowError = error;
                            foreach (DataColumn dataColumn in _key.ColumnsReference)
                            {
                                rows[j].SetColumnError(dataColumn, error);
                            }
                        }
                        result = true;
                    }
                }
            }
            return result;
        }

        internal override void CheckConstraint(DataRow row, DataRowAction action)
        {
            if (Table.EnforceConstraints &&
                (action == DataRowAction.Add ||
                 action == DataRowAction.Change ||
                 (action == DataRowAction.Rollback && row._tempRecord != -1)))
            {
                if (row.HaveValuesChanged(ColumnsReference))
                {
                    if (ConstraintIndex.IsKeyRecordInIndex(row.GetDefaultRecord()))
                    {
                        object[] values = row.GetColumnValues(ColumnsReference);
                        throw ExceptionBuilder.ConstraintViolation(ColumnsReference, values);
                    }
                }
            }
        }

        internal override bool ContainsColumn(DataColumn column)
        {
            return _key.ContainsColumn(column);
        }

        internal override Constraint Clone(DataSet destination)
        {
            return Clone(destination, false);
        }

        internal override Constraint Clone(DataSet destination, bool ignorNSforTableLookup)
        {
            int iDest;
            if (ignorNSforTableLookup)
            {
                iDest = destination.Tables.IndexOf(Table.TableName);
            }
            else
            {
                iDest = destination.Tables.IndexOf(Table.TableName, Table.Namespace, false);// pass false for last param to be backward compatable
            }

            if (iDest < 0)
                return null;
            DataTable table = destination.Tables[iDest];

            int keys = ColumnsReference.Length;
            DataColumn[] columns = new DataColumn[keys];

            for (int i = 0; i < keys; i++)
            {
                DataColumn src = ColumnsReference[i];
                iDest = table.Columns.IndexOf(src.ColumnName);
                if (iDest < 0)
                    return null;
                columns[i] = table.Columns[iDest];
            }

            UniqueConstraint clone = new UniqueConstraint(ConstraintName, columns);

            // ...Extended Properties
            foreach (object key in ExtendedProperties.Keys)
            {
                clone.ExtendedProperties[key] = ExtendedProperties[key];
            }

            return clone;
        }

        internal UniqueConstraint Clone(DataTable table)
        {
            int keys = ColumnsReference.Length;
            DataColumn[] columns = new DataColumn[keys];

            for (int i = 0; i < keys; i++)
            {
                DataColumn src = ColumnsReference[i];
                int iDest = table.Columns.IndexOf(src.ColumnName);
                if (iDest < 0)
                    return null;
                columns[i] = table.Columns[iDest];
            }

            UniqueConstraint clone = new UniqueConstraint(ConstraintName, columns);

            // ...Extended Properties
            foreach (object key in ExtendedProperties.Keys)
            {
                clone.ExtendedProperties[key] = ExtendedProperties[key];
            }

            return clone;
        }

        /// <summary>
        /// Gets the array of columns that this constraint affects.
        /// </summary>
        [ReadOnly(true)]
        public virtual DataColumn[] Columns
        {
            get
            {
                return _key.ToArray();
            }
        }

        internal DataColumn[] ColumnsReference
        {
            get
            {
                return _key.ColumnsReference;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the constraint is on a primary key.
        /// </summary>
        public bool IsPrimaryKey
        {
            get
            {
                if (Table == null)
                {
                    return false;
                }
                return (this == Table._primaryKey);
            }
        }

        private void Create(string constraintName, DataColumn[] columns)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i].Computed)
                {
                    throw ExceptionBuilder.ExpressionInConstraint(columns[i]);
                }
            }
            _key = new DataKey(columns, true);
            ConstraintName = constraintName;
            NonVirtualCheckState();
        }

        /// <summary>
        /// Compares this constraint to a second to determine if both are identical.
        /// </summary>
        public override bool Equals(object key2)
        {
            if (!(key2 is UniqueConstraint))
                return false;

            return Key.ColumnsEqual(((UniqueConstraint)key2).Key);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal override bool InCollection
        {
            set
            {
                base.InCollection = value;
                if (_key.ColumnsReference.Length == 1)
                {
                    _key.ColumnsReference[0].InternalUnique(value);
                }
            }
        }

        internal DataKey Key
        {
            get
            {
                return _key;
            }
        }

        /// <summary>
        /// Gets the table to which this constraint belongs.
        /// </summary>
        [ReadOnly(true)]
        public override DataTable Table
        {
            get
            {
                if (_key.HasValue)
                {
                    return _key.Table;
                }
                return null;
            }
        }

        // misc
    }
}
