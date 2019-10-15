// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Data
{
    internal sealed class ZeroOpNode : ExpressionNode
    {
        internal readonly int _op;

        internal const int zop_True = 1;
        internal const int zop_False = 0;
        internal const int zop_Null = -1;


        internal ZeroOpNode(int op) : base(null)
        {
            _op = op;
            Debug.Assert(op == Operators.True || op == Operators.False || op == Operators.Null, "Invalid zero-op");
        }

        internal override void Bind(DataTable table, List<DataColumn> list)
        {
        }

        internal override object Eval()
        {
            switch (_op)
            {
                case Operators.True:
                    return true;
                case Operators.False:
                    return false;
                case Operators.Null:
                    return DBNull.Value;
                default:
                    Debug.Assert(_op == Operators.True || _op == Operators.False || _op == Operators.Null, "Invalid zero-op");
                    return DBNull.Value;
            }
        }

        internal override object Eval(DataRow row, DataRowVersion version)
        {
            return Eval();
        }

        internal override object Eval(int[] recordNos)
        {
            return Eval();
        }

        internal override bool IsConstant()
        {
            return true;
        }

        internal override bool IsTableConstant()
        {
            return true;
        }

        internal override bool HasLocalAggregate()
        {
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
            return false;
        }

        internal override ExpressionNode Optimize()
        {
            return this;
        }
    }
}
