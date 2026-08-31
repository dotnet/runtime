// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class LookupNode : ExpressionNode
    {
        private readonly string? _relationName;    // can be null
        private readonly string _columnName;

        private DataColumn? _column;
        private DataRelation? _relation;

        internal LookupNode(DataTable? table, string columnName, string? relationName) : base(table)
        {
            _relationName = relationName;
            _columnName = columnName;
        }

        internal override void Bind(DataTable table, List<DataColumn> list)
        {
            BindTable(table);
            _column = null;  // clear for rebinding (if original binding was valid)
            _relation = null;

            if (table == null)
                throw ExprException.ExpressionUnbound(ToString()!);

            // First find parent table

            DataRelationCollection relations;
            relations = table.ParentRelations;

            if (_relationName == null)
            {
                // must have one and only one relation

                if (relations.Count > 1)
                {
                    throw ExprException.UnresolvedRelation(table.TableName, ToString()!);
                }
                _relation = relations[0];
            }
            else
            {
                _relation = relations[_relationName];
            }
            if (null == _relation)
            {
                throw ExprException.BindFailure(_relationName!); // this operation is not clone specific, throw generic exception
            }
            DataTable parentTable = _relation.ParentTable;

            Debug.Assert(_relation != null, "Invalid relation: no parent table.");
            Debug.Assert(_columnName != null, "All Lookup expressions have columnName set.");

            _column = parentTable.Columns[_columnName];

            if (_column == null)
                throw ExprException.UnboundName(_columnName);

            // add column to the dependency list

            int i;
            for (i = 0; i < list.Count; i++)
            {
                // walk the list, check if the current column already on the list
                DataColumn dataColumn = list[i];
                if (_column == dataColumn)
                {
                    break;
                }
            }
            if (i >= list.Count)
            {
                list.Add(_column);
            }

            AggregateNode.Bind(_relation, list);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval()
        {
            throw ExprException.EvalNoContext();
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(DataRow? row, DataRowVersion version)
        {
            if (_column == null || _relation == null)
                throw ExprException.ExpressionUnbound(ToString()!);

            DataRow? parent = row!.GetParentRow(_relation, version);
            if (parent == null)
                return DBNull.Value;

            return parent[_column, parent.HasVersion(version) ? version : DataRowVersion.Current];
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(int[] recordNos)
        {
            throw ExprException.ComputeNotAggregate(ToString()!);
        }

        internal override bool IsConstant()
        {
            return false;
        }

        internal override bool IsTableConstant()
        {
            return false;
        }

        internal override bool HasLocalAggregate()
        {
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
            return false;
        }

        internal override bool DependsOn(DataColumn column)
        {
            if (_column == column)
            {
                return true;
            }
            return false;
        }

        internal override ExpressionNode Optimize()
        {
            return this;
        }
    }
}
