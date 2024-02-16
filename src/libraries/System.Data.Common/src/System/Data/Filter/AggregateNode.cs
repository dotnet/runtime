// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal enum Aggregate
    {
        None = FunctionId.none,
        Sum = FunctionId.Sum,
        Avg = FunctionId.Avg,
        Min = FunctionId.Min,
        Max = FunctionId.Max,
        Count = FunctionId.Count,
        StDev = FunctionId.StDev,   // Statistical standard deviation
        Var = FunctionId.Var,       // Statistical variance
    }

    internal sealed class AggregateNode : ExpressionNode
    {
        private readonly AggregateType _type;
        private readonly Aggregate _aggregate;
        private readonly bool _local;     // set to true if the aggregate calculated locally (for the current table)

        private readonly string? _relationName;
        private readonly string _columnName;

        // CONSIDER PERF: keep the objects, not names.
        // ? try to drop a column
        private DataTable? _childTable;
        private DataColumn? _column;
        private DataRelation? _relation;

        internal AggregateNode(DataTable? table, FunctionId aggregateType, string columnName) :
            this(table, aggregateType, columnName, true, null)
        {
        }

        internal AggregateNode(DataTable? table, FunctionId aggregateType, string columnName, bool local, string? relationName) : base(table)
        {
            Debug.Assert(columnName != null, "Invalid parameter column name (null).");
            _aggregate = (Aggregate)(int)aggregateType;

            if (aggregateType == FunctionId.Sum)
                _type = AggregateType.Sum;
            else if (aggregateType == FunctionId.Avg)
                _type = AggregateType.Mean;
            else if (aggregateType == FunctionId.Min)
                _type = AggregateType.Min;
            else if (aggregateType == FunctionId.Max)
                _type = AggregateType.Max;
            else if (aggregateType == FunctionId.Count)
                _type = AggregateType.Count;
            else if (aggregateType == FunctionId.Var)
                _type = AggregateType.Var;
            else if (aggregateType == FunctionId.StDev)
                _type = AggregateType.StDev;
            else
            {
                throw ExprException.UndefinedFunction(Function.s_functionName[(int)aggregateType]);
            }

            _local = local;
            _relationName = relationName;
            _columnName = columnName;
        }
        internal override void Bind(DataTable table, List<DataColumn> list)
        {
            BindTable(table);
            if (table == null)
                throw ExprException.AggregateUnbound(ToString()!);

            if (_local)
            {
                _relation = null;
            }
            else
            {
                DataRelationCollection relations;
                relations = table.ChildRelations;

                if (_relationName == null)
                {
                    // must have one and only one relation

                    if (relations.Count > 1)
                    {
                        throw ExprException.UnresolvedRelation(table.TableName, ToString()!);
                    }
                    if (relations.Count == 1)
                    {
                        _relation = relations[0];
                    }
                    else
                    {
                        throw ExprException.AggregateUnbound(ToString()!);
                    }
                }
                else
                {
                    _relation = relations[_relationName];
                }
            }

            _childTable = (_relation == null) ? table : _relation.ChildTable;

            _column = _childTable.Columns[_columnName];

            if (_column == null)
                throw ExprException.UnboundName(_columnName);

            // add column to the dependency list, do not add duplicate columns

            Debug.Assert(_column != null, $"Failed to bind column {_columnName}");

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

        internal static void Bind(DataRelation? relation, List<DataColumn> list)
        {
            if (null != relation)
            {
                // add the ends of the relationship the expression depends on
                foreach (DataColumn c in relation.ChildColumnsReference)
                {
                    if (!list.Contains(c))
                    {
                        list.Add(c);
                    }
                }
                foreach (DataColumn c in relation.ParentColumnsReference)
                {
                    if (!list.Contains(c))
                    {
                        list.Add(c);
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval()
        {
            return Eval(null, DataRowVersion.Default);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(DataRow? row, DataRowVersion version)
        {
            if (_childTable == null)
                throw ExprException.AggregateUnbound(ToString()!);

            DataRow[] rows;

            if (_local)
            {
                rows = new DataRow[_childTable.Rows.Count];
                _childTable.Rows.CopyTo(rows, 0);
            }
            else
            {
                if (row == null)
                {
                    throw ExprException.EvalNoContext();
                }
                if (_relation == null)
                {
                    throw ExprException.AggregateUnbound(ToString()!);
                }
                rows = row.GetChildRows(_relation, version);
            }

            int[] records;
            if (version == DataRowVersion.Proposed)
            {
                version = DataRowVersion.Default;
            }

            List<int> recordList = new List<int>();

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].RowState == DataRowState.Deleted)
                {
                    if (DataRowAction.Rollback != rows[i]._action)
                    {
                        continue;
                    }
                    Debug.Assert(DataRowVersion.Original == version, "wrong version");
                    version = DataRowVersion.Original;
                }
                else if ((DataRowAction.Rollback == rows[i]._action) && (rows[i].RowState == DataRowState.Added))
                {
                    continue;
                }
                if (version == DataRowVersion.Original && rows[i]._oldRecord == -1)
                {
                    continue;
                }
                recordList.Add(rows[i].GetRecordFromVersion(version));
            }
            records = recordList.ToArray();

            return _column!.GetAggregateValue(records, _type);
        }

        // Helper for the DataTable.Compute method
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(int[] records)
        {
            if (_childTable == null)
                throw ExprException.AggregateUnbound(ToString()!);
            if (!_local)
            {
                throw ExprException.ComputeNotAggregate(ToString()!);
            }
            return _column!.GetAggregateValue(records, _type);
        }

        internal override bool IsConstant()
        {
            return false;
        }

        internal override bool IsTableConstant()
        {
            return _local;
        }

        internal override bool HasLocalAggregate()
        {
            return _local;
        }

        internal override bool HasRemoteAggregate()
        {
            return !_local;
        }

        internal override bool DependsOn(DataColumn column)
        {
            if (_column == column)
            {
                return true;
            }
            if (_column!.Computed)
            {
                return _column.DataExpression!.DependsOn(column);
            }
            return false;
        }

        internal override ExpressionNode Optimize()
        {
            return this;
        }
    }
}
