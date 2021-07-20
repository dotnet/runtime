// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class NameNode : ExpressionNode
    {
        internal string _name;
        internal bool _found;
        internal DataColumn? _column;

        internal NameNode(DataTable? table, char[] text, int start, int pos) : base(table)
        {
            _name = ParseName(text, start, pos);
        }

        internal NameNode(DataTable? table, string name) : base(table)
        {
            _name = name;
        }

        internal override bool IsSqlColumn
        {
            get
            {
                return _column!.IsSqlType;
            }
        }

        internal override void Bind(DataTable table, List<DataColumn> list)
        {
            BindTable(table);
            if (table == null)
                throw ExprException.UnboundName(_name);

            try
            {
                _column = table.Columns[_name];
            }
            catch (Exception e)
            {
                _found = false;
                if (!Common.ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
                throw ExprException.UnboundName(_name);
            }

            if (_column == null)
                throw ExprException.UnboundName(_name);

            _name = _column.ColumnName;
            _found = true;

            // add column to the dependency list, do not add duplicate columns
            Debug.Assert(_column != null, $"Failed to bind column {_name}");

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
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval()
        {
            // can not eval column without ROW value;
            throw ExprException.EvalNoContext();
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(DataRow? row, DataRowVersion version)
        {
            if (!_found)
            {
                throw ExprException.UnboundName(_name);
            }

            if (row == null)
            {
                if (IsTableConstant()) // this column is TableConstant Aggregate Function
                    return _column!.DataExpression!.Evaluate();
                else
                {
                    throw ExprException.UnboundName(_name);
                }
            }

            return _column![row.GetRecordFromVersion(version)];
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(int[] records)
        {
            throw ExprException.ComputeNotAggregate(ToString()!);
        }

        internal override bool IsConstant()
        {
            return false;
        }

        internal override bool IsTableConstant()
        {
            if (_column != null && _column.Computed)
            {
                return _column.DataExpression!.IsTableAggregate();
            }
            return false;
        }

        internal override bool HasLocalAggregate()
        {
            if (_column != null && _column.Computed)
            {
                return _column.DataExpression!.HasLocalAggregate();
            }
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
            if (_column != null && _column.Computed)
            {
                return _column.DataExpression!.HasRemoteAggregate();
            }
            return false;
        }

        internal override bool DependsOn(DataColumn column)
        {
            if (_column == column)
                return true;

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

        /// <summary>
        ///     Parses given name and checks it validity
        /// </summary>
        internal static string ParseName(char[] text, int start, int pos)
        {
            char esc = '\0';
            string charsToEscape = string.Empty;
            int saveStart = start;
            int savePos = pos;

            if (text[start] == '`')
            {
                start = checked((start + 1));
                pos = checked((pos - 1));
                esc = '\\';
                charsToEscape = "`";
            }
            else if (text[start] == '[')
            {
                start = checked((start + 1));
                pos = checked((pos - 1));
                esc = '\\';
                charsToEscape = "]\\";
            }

            if (esc != '\0')
            {
                // scan the name in search for the ESC
                int posEcho = start;

                for (int i = start; i < pos; i++)
                {
                    if (text[i] == esc)
                    {
                        if (i + 1 < pos && charsToEscape.Contains(text[i + 1]))
                        {
                            i++;
                        }
                    }
                    text[posEcho] = text[i];
                    posEcho++;
                }
                pos = posEcho;
            }

            if (pos == start)
                throw ExprException.InvalidName(new string(text, saveStart, savePos - saveStart));

            return new string(text, start, pos - start);
        }
    }
}
