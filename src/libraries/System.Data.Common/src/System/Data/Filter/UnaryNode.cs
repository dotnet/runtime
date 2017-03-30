// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Data.Common;
using System.Data.SqlTypes;

namespace System.Data
{
    internal sealed class UnaryNode : ExpressionNode
    {
        internal readonly int _op;

        internal ExpressionNode _right;

        internal UnaryNode(DataTable table, int op, ExpressionNode right) : base(table)
        {
            _op = op;
            _right = right;
        }

        internal override void Bind(DataTable table, List<DataColumn> list)
        {
            BindTable(table);
            _right.Bind(table, list);
        }

        internal override object Eval()
        {
            return Eval(null, DataRowVersion.Default);
        }

        internal override object Eval(DataRow row, DataRowVersion version)
        {
            return EvalUnaryOp(_op, _right.Eval(row, version));
        }

        internal override object Eval(int[] recordNos)
        {
            return _right.Eval(recordNos);
        }

        private object EvalUnaryOp(int op, object vl)
        {
            object value = DBNull.Value;

            if (DataExpression.IsUnknown(vl))
                return DBNull.Value;

            StorageType storageType;
            switch (op)
            {
                case Operators.Noop:
                    return vl;
                case Operators.UnaryPlus:
                    storageType = DataStorage.GetStorageType(vl.GetType());
                    if (ExpressionNode.IsNumericSql(storageType))
                    {
                        return vl;
                    }
                    throw ExprException.TypeMismatch(ToString());

                case Operators.Negative:
                    // the have to be better way for doing this..
                    storageType = DataStorage.GetStorageType(vl.GetType());
                    if (ExpressionNode.IsNumericSql(storageType))
                    {
                        switch (storageType)
                        {
                            case StorageType.Byte:
                                value = -(byte)vl;
                                break;
                            case StorageType.Int16:
                                value = -(short)vl;
                                break;
                            case StorageType.Int32:
                                value = -(int)vl;
                                break;
                            case StorageType.Int64:
                                value = -(long)vl;
                                break;
                            case StorageType.Single:
                                value = -(float)vl;
                                break;
                            case StorageType.Double:
                                value = -(double)vl;
                                break;
                            case StorageType.Decimal:
                                value = -(decimal)vl;
                                break;
                            case StorageType.SqlDecimal:
                                value = -(SqlDecimal)vl;
                                break;
                            case StorageType.SqlDouble:
                                value = -(SqlDouble)vl;
                                break;
                            case StorageType.SqlSingle:
                                value = -(SqlSingle)vl;
                                break;
                            case StorageType.SqlMoney:
                                value = -(SqlMoney)vl;
                                break;
                            case StorageType.SqlInt64:
                                value = -(SqlInt64)vl;
                                break;
                            case StorageType.SqlInt32:
                                value = -(SqlInt32)vl;
                                break;
                            case StorageType.SqlInt16:
                                value = -(SqlInt16)vl;
                                break;
                            default:
                                Debug.Assert(false, "Missing a type conversion");
                                value = DBNull.Value;
                                break;
                        }
                        return value;
                    }

                    throw ExprException.TypeMismatch(ToString());

                case Operators.Not:
                    if (vl is SqlBoolean)
                    {
                        if (((SqlBoolean)vl).IsFalse)
                        {
                            return SqlBoolean.True;
                        }
                        else if (((SqlBoolean)vl).IsTrue)
                        {
                            return SqlBoolean.False;
                        }
                        throw ExprException.UnsupportedOperator(op);  // or should the result of not SQLNull  be SqlNull ?
                    }
                    else
                    {
                        if (DataExpression.ToBoolean(vl) != false)
                            return false;
                        return true;
                    }

                default:
                    throw ExprException.UnsupportedOperator(op);
            }
        }

        internal override bool IsConstant()
        {
            return (_right.IsConstant());
        }

        internal override bool IsTableConstant()
        {
            return (_right.IsTableConstant());
        }

        internal override bool HasLocalAggregate()
        {
            return (_right.HasLocalAggregate());
        }

        internal override bool HasRemoteAggregate()
        {
            return (_right.HasRemoteAggregate());
        }

        internal override bool DependsOn(DataColumn column)
        {
            return (_right.DependsOn(column));
        }


        internal override ExpressionNode Optimize()
        {
            _right = _right.Optimize();

            if (IsConstant())
            {
                object val = Eval();

                return new ConstNode(table, ValueType.Object, val, false);
            }
            else
                return this;
        }
    }
}
