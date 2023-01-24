// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace System.Data
{
    /// <summary>
    /// ConstraintEnumerator is an object for enumerating all constraints in a DataSet
    /// </summary>
    internal class ConstraintEnumerator
    {
        private IEnumerator? _tables;
        private IEnumerator? _constraints;
        private Constraint? _currentObject;

        public ConstraintEnumerator(DataSet? dataSet)
        {
            _tables = dataSet?.Tables.GetEnumerator();
            _currentObject = null;
        }

        public bool GetNext()
        {
            Constraint candidate;
            _currentObject = null;
            while (_tables != null)
            {
                if (_constraints == null)
                {
                    if (!_tables.MoveNext())
                    {
                        _tables = null;
                        return false;
                    }
                    _constraints = ((DataTable)_tables.Current).Constraints.GetEnumerator();
                }

                if (!_constraints.MoveNext())
                {
                    _constraints = null;
                    continue;
                }

                Debug.Assert(_constraints.Current is Constraint, "ConstraintEnumerator, contains object which is not constraint");
                candidate = (Constraint)_constraints.Current;
                if (IsValidCandidate(candidate))
                {
                    _currentObject = candidate;
                    return true;
                }
            }
            return false;
        }

        public Constraint GetConstraint()
        {
            // If currentObject is null we are before first GetNext or after last GetNext--consumer is bad
            Debug.Assert(_currentObject != null, "GetObject should never be called w/ null currentObject.");
            return _currentObject;
        }

        protected virtual bool IsValidCandidate(Constraint constraint) => true;

        protected Constraint? CurrentObject => _currentObject;
    }

    internal class ForeignKeyConstraintEnumerator : ConstraintEnumerator
    {
        public ForeignKeyConstraintEnumerator(DataSet? dataSet) : base(dataSet) { }

        protected override bool IsValidCandidate(Constraint constraint) => constraint is ForeignKeyConstraint;

        public ForeignKeyConstraint GetForeignKeyConstraint()
        {
            // If CurrentObject is null we are before first GetNext or after last GetNext--consumer is bad
            Debug.Assert(CurrentObject != null, "GetObject should never be called w/ null currentObject.");
            return (ForeignKeyConstraint)CurrentObject;
        }
    }

    internal sealed class ChildForeignKeyConstraintEnumerator : ForeignKeyConstraintEnumerator
    {
        // this is the table to do comparisons against
        private readonly DataTable _table;

        public ChildForeignKeyConstraintEnumerator(DataSet dataSet, DataTable inTable) : base(dataSet)
        {
            _table = inTable;
        }

        protected override bool IsValidCandidate(Constraint constraint) =>
            ((constraint is ForeignKeyConstraint) && (((ForeignKeyConstraint)constraint).Table == _table));
    }

    internal sealed class ParentForeignKeyConstraintEnumerator : ForeignKeyConstraintEnumerator
    {
        // this is the table to do comparisons against
        private readonly DataTable _table;

        public ParentForeignKeyConstraintEnumerator(DataSet? dataSet, DataTable inTable) : base(dataSet)
        {
            _table = inTable;
        }

        protected override bool IsValidCandidate(Constraint constraint) =>
            ((constraint is ForeignKeyConstraint) && (((ForeignKeyConstraint)constraint).RelatedTable == _table));
    }
}
