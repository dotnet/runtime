// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Data.SqlTypes;
using System.Data.Common;

namespace System.Data
{
    internal class BinaryNode : ExpressionNode
    {
        internal int _op;

        internal ExpressionNode _left;
        internal ExpressionNode _right;

        internal BinaryNode(DataTable table, int op, ExpressionNode left, ExpressionNode right) : base(table)
        {
            _op = op;
            _left = left;
            _right = right;
        }

        internal override void Bind(DataTable table, List<DataColumn> list)
        {
            BindTable(table);
            _left.Bind(table, list);
            _right.Bind(table, list);
        }

        internal override object Eval()
        {
            return Eval(null, DataRowVersion.Default);
        }

        internal override object Eval(DataRow row, DataRowVersion version)
        {
            return EvalBinaryOp(_op, _left, _right, row, version, null);
        }

        internal override object Eval(int[] recordNos)
        {
            return EvalBinaryOp(_op, _left, _right, null, DataRowVersion.Default, recordNos);
        }

        internal override bool IsConstant()
        {
            // CONSIDER: for string operations we depend on the local info
            return (_left.IsConstant() && _right.IsConstant());
        }

        internal override bool IsTableConstant()
        {
            return (_left.IsTableConstant() && _right.IsTableConstant());
        }
        internal override bool HasLocalAggregate()
        {
            return (_left.HasLocalAggregate() || _right.HasLocalAggregate());
        }

        internal override bool HasRemoteAggregate()
        {
            return (_left.HasRemoteAggregate() || _right.HasRemoteAggregate());
        }

        internal override bool DependsOn(DataColumn column)
        {
            if (_left.DependsOn(column))
                return true;
            return _right.DependsOn(column);
        }

        internal override ExpressionNode Optimize()
        {
            _left = _left.Optimize();

            if (_op == Operators.Is)
            {
                // only 'Is Null' or 'Is Not Null' are valid
                if (_right is UnaryNode)
                {
                    UnaryNode un = (UnaryNode)_right;
                    if (un._op != Operators.Not)
                    {
                        throw ExprException.InvalidIsSyntax();
                    }
                    _op = Operators.IsNot;
                    _right = un._right;
                }
                if (_right is ZeroOpNode)
                {
                    if (((ZeroOpNode)_right)._op != Operators.Null)
                    {
                        throw ExprException.InvalidIsSyntax();
                    }
                }
                else
                {
                    throw ExprException.InvalidIsSyntax();
                }
            }
            else
            {
                _right = _right.Optimize();
            }


            if (IsConstant())
            {
                object val = Eval();

                if (val == DBNull.Value)
                {
                    return new ZeroOpNode(Operators.Null);
                }

                if (val is bool)
                {
                    if ((bool)val)
                        return new ZeroOpNode(Operators.True);
                    else
                        return new ZeroOpNode(Operators.False);
                }
                return new ConstNode(table, ValueType.Object, val, false);
            }
            else
                return this;
        }

        internal void SetTypeMismatchError(int op, Type left, Type right)
        {
            throw ExprException.TypeMismatchInBinop(op, left, right);
        }

        private static object Eval(ExpressionNode expr, DataRow row, DataRowVersion version, int[] recordNos)
        {
            if (recordNos == null)
            {
                return expr.Eval(row, version);
            }
            else
            {
                return expr.Eval(recordNos);
            }
        }

        internal int BinaryCompare(object vLeft, object vRight, StorageType resultType, int op)
        {
            return BinaryCompare(vLeft, vRight, resultType, op, null);
        }

        internal int BinaryCompare(object vLeft, object vRight, StorageType resultType, int op, CompareInfo comparer)
        {
            int result = 0;
            try
            {
                if (!DataStorage.IsSqlType(resultType))
                {
                    switch (resultType)
                    {
                        case StorageType.SByte:
                        case StorageType.Int16:
                        case StorageType.Int32:
                        case StorageType.Byte:
                        case StorageType.UInt16:
                            return Convert.ToInt32(vLeft, FormatProvider).CompareTo(Convert.ToInt32(vRight, FormatProvider));
                        case StorageType.Int64:
                        case StorageType.UInt32:
                        case StorageType.UInt64:
                        case StorageType.Decimal:
                            return decimal.Compare(Convert.ToDecimal(vLeft, FormatProvider), Convert.ToDecimal(vRight, FormatProvider));
                        case StorageType.Char:
                            return Convert.ToInt32(vLeft, FormatProvider).CompareTo(Convert.ToInt32(vRight, FormatProvider));
                        case StorageType.Double:
                            return Convert.ToDouble(vLeft, FormatProvider).CompareTo(Convert.ToDouble(vRight, FormatProvider));
                        case StorageType.Single:
                            return Convert.ToSingle(vLeft, FormatProvider).CompareTo(Convert.ToSingle(vRight, FormatProvider));
                        case StorageType.DateTime:
                            return DateTime.Compare(Convert.ToDateTime(vLeft, FormatProvider), Convert.ToDateTime(vRight, FormatProvider));
                        case StorageType.DateTimeOffset:
                            // DTO can only be compared to DTO, other cases: cast Exception
                            return DateTimeOffset.Compare((DateTimeOffset)vLeft, (DateTimeOffset)vRight);
                        case StorageType.String:
                            return table.Compare(Convert.ToString(vLeft, FormatProvider), Convert.ToString(vRight, FormatProvider), comparer);
                        case StorageType.Guid:
                            return ((Guid)vLeft).CompareTo((Guid)vRight);
                        case StorageType.Boolean:
                            if (op == Operators.EqualTo || op == Operators.NotEqual)
                            {
                                return Convert.ToInt32(DataExpression.ToBoolean(vLeft), FormatProvider) -
                                       Convert.ToInt32(DataExpression.ToBoolean(vRight), FormatProvider);
                            }
                            break;
                    }
                }
                else
                {
                    switch (resultType)
                    {
                        case StorageType.SByte:
                        case StorageType.Int16:
                        case StorageType.Int32:
                        case StorageType.Byte:
                        case StorageType.UInt16:
                        case StorageType.SqlByte:
                        case StorageType.SqlInt16:
                        case StorageType.SqlInt32:
                            return SqlConvert.ConvertToSqlInt32(vLeft).CompareTo(SqlConvert.ConvertToSqlInt32(vRight));
                        case StorageType.Int64:
                        case StorageType.UInt32:
                        case StorageType.SqlInt64:
                            return SqlConvert.ConvertToSqlInt64(vLeft).CompareTo(SqlConvert.ConvertToSqlInt64(vRight));
                        case StorageType.UInt64:
                        case StorageType.SqlDecimal:
                            return SqlConvert.ConvertToSqlDecimal(vLeft).CompareTo(SqlConvert.ConvertToSqlDecimal(vRight));
                        case StorageType.SqlDouble:
                            return SqlConvert.ConvertToSqlDouble(vLeft).CompareTo(SqlConvert.ConvertToSqlDouble(vRight));
                        case StorageType.SqlSingle:
                            return SqlConvert.ConvertToSqlSingle(vLeft).CompareTo(SqlConvert.ConvertToSqlSingle(vRight));
                        case StorageType.SqlString:
                            return table.Compare(vLeft.ToString(), vRight.ToString());
                        case StorageType.SqlGuid:
                            return ((SqlGuid)vLeft).CompareTo(vRight);
                        case StorageType.SqlBoolean:
                            if (op == Operators.EqualTo || op == Operators.NotEqual)
                            {
                                result = 1;
                                if (((vLeft.GetType() == typeof(SqlBoolean)) && ((vRight.GetType() == typeof(SqlBoolean)) || (vRight.GetType() == typeof(bool)))) ||
                                    ((vRight.GetType() == typeof(SqlBoolean)) && ((vLeft.GetType() == typeof(SqlBoolean)) || (vLeft.GetType() == typeof(bool)))))
                                {
                                    return SqlConvert.ConvertToSqlBoolean(vLeft).CompareTo(SqlConvert.ConvertToSqlBoolean(vRight));
                                }
                            }
                            break;
                        case StorageType.SqlBinary:
                            return SqlConvert.ConvertToSqlBinary(vLeft).CompareTo(SqlConvert.ConvertToSqlBinary(vRight));
                        case StorageType.SqlDateTime:
                            return SqlConvert.ConvertToSqlDateTime(vLeft).CompareTo(SqlConvert.ConvertToSqlDateTime(vRight));
                        case StorageType.SqlMoney:
                            return SqlConvert.ConvertToSqlMoney(vLeft).CompareTo(SqlConvert.ConvertToSqlMoney(vRight));
                    }
                }
            }
            catch (System.ArgumentException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (System.FormatException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (System.InvalidCastException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (System.OverflowException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (System.Data.EvaluateException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            SetTypeMismatchError(op, vLeft.GetType(), vRight.GetType());
            return result;
        }

        private object EvalBinaryOp(int op, ExpressionNode left, ExpressionNode right, DataRow row, DataRowVersion version, int[] recordNos)
        {
            object vLeft;
            object vRight;
            StorageType resultType;

            /*
            special case for OR and AND operators: we don't want to evaluate
            both right and left operands, because we can shortcut :
                for OR  operator If one of the operands is true the result is true
                for AND operator If one of rhe operands is flase the result is false
            CONSIDER : in the shortcut case do we want to type-check the other operand?
            */

            if (op != Operators.Or && op != Operators.And && op != Operators.In && op != Operators.Is && op != Operators.IsNot)
            {
                vLeft = BinaryNode.Eval(left, row, version, recordNos);
                vRight = BinaryNode.Eval(right, row, version, recordNos);
                Type typeofLeft = vLeft.GetType();
                Type typeofRight = vRight.GetType();

                StorageType leftStorage = DataStorage.GetStorageType(typeofLeft);
                StorageType rightStorage = DataStorage.GetStorageType(typeofRight);

                bool leftIsSqlType = DataStorage.IsSqlType(leftStorage);
                bool rightIsSqlType = DataStorage.IsSqlType(rightStorage);

                //    special case of handling NULLS, currently only OR operator can work with NULLS
                if (leftIsSqlType && DataStorage.IsObjectSqlNull(vLeft))
                {
                    return vLeft;
                }
                else if (rightIsSqlType && DataStorage.IsObjectSqlNull(vRight))
                {
                    return vRight;
                }
                else if ((vLeft == DBNull.Value) || (vRight == DBNull.Value))
                {
                    return DBNull.Value;
                }

                if (leftIsSqlType || rightIsSqlType)
                {
                    resultType = ResultSqlType(leftStorage, rightStorage, (left is ConstNode), (right is ConstNode), op);
                }
                else
                {
                    resultType = ResultType(leftStorage, rightStorage, (left is ConstNode), (right is ConstNode), op);
                }

                if (StorageType.Empty == resultType)
                {
                    SetTypeMismatchError(op, typeofLeft, typeofRight);
                }
            }
            else
            {
                vLeft = vRight = DBNull.Value;
                resultType = StorageType.Empty; // shouldn't we make it boolean?
            }

            object value = DBNull.Value;
            bool typeMismatch = false;

            try
            {
                switch (op)
                {
                    case Operators.Plus:
                        switch (resultType)
                        {
                            case StorageType.Byte:
                                {
                                    value = Convert.ToByte((Convert.ToByte(vLeft, FormatProvider) + Convert.ToByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.SByte:
                                {
                                    value = Convert.ToSByte((Convert.ToSByte(vLeft, FormatProvider) + Convert.ToSByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int16:
                                {
                                    value = Convert.ToInt16((Convert.ToInt16(vLeft, FormatProvider) + Convert.ToInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.UInt16:
                                {
                                    value = Convert.ToUInt16((Convert.ToUInt16(vLeft, FormatProvider) + Convert.ToUInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int32:
                                {
                                    checked { value = Convert.ToInt32(vLeft, FormatProvider) + Convert.ToInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.UInt32:
                                {
                                    checked { value = Convert.ToUInt32(vLeft, FormatProvider) + Convert.ToUInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.UInt64:
                                {
                                    checked { value = Convert.ToUInt64(vLeft, FormatProvider) + Convert.ToUInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Int64:
                                {
                                    checked { value = Convert.ToInt64(vLeft, FormatProvider) + Convert.ToInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Decimal:
                                {
                                    checked { value = Convert.ToDecimal(vLeft, FormatProvider) + Convert.ToDecimal(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Single:
                                {
                                    checked { value = Convert.ToSingle(vLeft, FormatProvider) + Convert.ToSingle(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Double:
                                {
                                    checked { value = Convert.ToDouble(vLeft, FormatProvider) + Convert.ToDouble(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.String:
                            case StorageType.Char:
                                {
                                    value = Convert.ToString(vLeft, FormatProvider) + Convert.ToString(vRight, FormatProvider);
                                    break;
                                }
                            case StorageType.DateTime:
                                {
                                    // one of the operands should be a DateTime, and an other a TimeSpan

                                    if (vLeft is TimeSpan && vRight is DateTime)
                                    {
                                        value = (DateTime)vRight + (TimeSpan)vLeft;
                                    }
                                    else if (vLeft is DateTime && vRight is TimeSpan)
                                    {
                                        value = (DateTime)vLeft + (TimeSpan)vRight;
                                    }
                                    else
                                    {
                                        typeMismatch = true;
                                    }
                                    break;
                                }
                            case StorageType.TimeSpan:
                                {
                                    value = (TimeSpan)vLeft + (TimeSpan)vRight;
                                    break;
                                }
                            case StorageType.SqlInt16:
                                {
                                    value = (SqlConvert.ConvertToSqlInt16(vLeft) + SqlConvert.ConvertToSqlInt16(vRight));
                                    break;
                                }
                            case StorageType.SqlInt32:
                                {
                                    value = (SqlConvert.ConvertToSqlInt32(vLeft) + SqlConvert.ConvertToSqlInt32(vRight));
                                    break;
                                }
                            case StorageType.SqlInt64:
                                {
                                    value = (SqlConvert.ConvertToSqlInt64(vLeft) + SqlConvert.ConvertToSqlInt64(vRight));
                                    break;
                                }
                            case StorageType.SqlDouble:
                                {
                                    value = (SqlConvert.ConvertToSqlDouble(vLeft) + SqlConvert.ConvertToSqlDouble(vRight));
                                    break;
                                }
                            case StorageType.SqlSingle:
                                {
                                    value = (SqlConvert.ConvertToSqlSingle(vLeft) + SqlConvert.ConvertToSqlSingle(vRight));
                                    break;
                                }
                            case StorageType.SqlDecimal:
                                {
                                    value = (SqlConvert.ConvertToSqlDecimal(vLeft) + SqlConvert.ConvertToSqlDecimal(vRight));
                                    break;
                                }
                            case StorageType.SqlMoney:
                                {
                                    value = (SqlConvert.ConvertToSqlMoney(vLeft) + SqlConvert.ConvertToSqlMoney(vRight));
                                    break;
                                }
                            case StorageType.SqlByte:
                                {
                                    value = (SqlConvert.ConvertToSqlByte(vLeft) + SqlConvert.ConvertToSqlByte(vRight));
                                    break;
                                }
                            case StorageType.SqlString:
                                {
                                    value = (SqlConvert.ConvertToSqlString(vLeft) + SqlConvert.ConvertToSqlString(vRight));
                                    break;
                                }
                            case StorageType.SqlDateTime:
                                {
                                    if (vLeft is TimeSpan && vRight is SqlDateTime)
                                    {
                                        SqlDateTime rValue = SqlConvert.ConvertToSqlDateTime(vRight);
                                        value = SqlConvert.ConvertToSqlDateTime((DateTime)rValue.Value + (TimeSpan)vLeft);
                                    }
                                    else if (vLeft is SqlDateTime && vRight is TimeSpan)
                                    {
                                        SqlDateTime lValue = SqlConvert.ConvertToSqlDateTime(vLeft);
                                        value = SqlConvert.ConvertToSqlDateTime((DateTime)lValue.Value + (TimeSpan)vRight);
                                    }
                                    else
                                    {
                                        typeMismatch = true;
                                    }
                                    break;
                                }
                            default:
                                {
                                    typeMismatch = true;
                                    break;
                                }
                        }
                        break; // Operators.Plus

                    case Operators.Minus:
                        switch (resultType)
                        {
                            case StorageType.Byte:
                                {
                                    value = Convert.ToByte((Convert.ToByte(vLeft, FormatProvider) - Convert.ToByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.SqlByte:
                                {
                                    value = (SqlConvert.ConvertToSqlByte(vLeft) - SqlConvert.ConvertToSqlByte(vRight));
                                    break;
                                }
                            case StorageType.SByte:
                                {
                                    value = Convert.ToSByte((Convert.ToSByte(vLeft, FormatProvider) - Convert.ToSByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int16:
                                {
                                    value = Convert.ToInt16((Convert.ToInt16(vLeft, FormatProvider) - Convert.ToInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.SqlInt16:
                                {
                                    value = (SqlConvert.ConvertToSqlInt16(vLeft) - SqlConvert.ConvertToSqlInt16(vRight));
                                    break;
                                }
                            case StorageType.UInt16:
                                {
                                    value = Convert.ToUInt16((Convert.ToUInt16(vLeft, FormatProvider) - Convert.ToUInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int32:
                                {
                                    checked { value = Convert.ToInt32(vLeft, FormatProvider) - Convert.ToInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlInt32:
                                {
                                    value = (SqlConvert.ConvertToSqlInt32(vLeft) - SqlConvert.ConvertToSqlInt32(vRight));
                                    break;
                                }
                            case StorageType.UInt32:
                                {
                                    checked { value = Convert.ToUInt32(vLeft, FormatProvider) - Convert.ToUInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Int64:
                                {
                                    checked { value = Convert.ToInt64(vLeft, FormatProvider) - Convert.ToInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlInt64:
                                {
                                    value = (SqlConvert.ConvertToSqlInt64(vLeft) - SqlConvert.ConvertToSqlInt64(vRight));
                                    break;
                                }
                            case StorageType.UInt64:
                                {
                                    checked { value = Convert.ToUInt64(vLeft, FormatProvider) - Convert.ToUInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Decimal:
                                {
                                    checked { value = Convert.ToDecimal(vLeft, FormatProvider) - Convert.ToDecimal(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlDecimal:
                                {
                                    value = (SqlConvert.ConvertToSqlDecimal(vLeft) - SqlConvert.ConvertToSqlDecimal(vRight));
                                    break;
                                }
                            case StorageType.Single:
                                {
                                    checked { value = Convert.ToSingle(vLeft, FormatProvider) - Convert.ToSingle(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlSingle:
                                {
                                    value = (SqlConvert.ConvertToSqlSingle(vLeft) - SqlConvert.ConvertToSqlSingle(vRight));
                                    break;
                                }
                            case StorageType.Double:
                                {
                                    checked { value = Convert.ToDouble(vLeft, FormatProvider) - Convert.ToDouble(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlDouble:
                                {
                                    value = (SqlConvert.ConvertToSqlDouble(vLeft) - SqlConvert.ConvertToSqlDouble(vRight));
                                    break;
                                }
                            case StorageType.SqlMoney:
                                {
                                    value = (SqlConvert.ConvertToSqlMoney(vLeft) - SqlConvert.ConvertToSqlMoney(vRight));
                                    break;
                                }
                            case StorageType.DateTime:
                                {
                                    value = (DateTime)vLeft - (TimeSpan)vRight;
                                    break;
                                }
                            case StorageType.TimeSpan:
                                {
                                    if (vLeft is DateTime)
                                    {
                                        value = (DateTime)vLeft - (DateTime)vRight;
                                    }
                                    else
                                        value = (TimeSpan)vLeft - (TimeSpan)vRight;
                                    break;
                                }
                            case StorageType.SqlDateTime:
                                {
                                    if (vLeft is TimeSpan && vRight is SqlDateTime)
                                    {
                                        SqlDateTime rValue = SqlConvert.ConvertToSqlDateTime(vRight);
                                        value = SqlConvert.ConvertToSqlDateTime((DateTime)rValue.Value - (TimeSpan)vLeft);
                                    }
                                    else if (vLeft is SqlDateTime && vRight is TimeSpan)
                                    {
                                        SqlDateTime lValue = SqlConvert.ConvertToSqlDateTime(vLeft);
                                        value = SqlConvert.ConvertToSqlDateTime((DateTime)lValue.Value - (TimeSpan)vRight);
                                    }
                                    else
                                    {
                                        typeMismatch = true;
                                    }
                                    break;
                                }
                            default:
                                {
                                    typeMismatch = true;
                                    break;
                                }
                        }
                        break; // Operators.Minus

                    case Operators.Multiply:
                        switch (resultType)
                        {
                            case StorageType.Byte:
                                {
                                    value = Convert.ToByte((Convert.ToByte(vLeft, FormatProvider) * Convert.ToByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.SqlByte:
                                {
                                    value = (SqlConvert.ConvertToSqlByte(vLeft) * SqlConvert.ConvertToSqlByte(vRight));
                                    break;
                                }
                            case StorageType.SByte:
                                {
                                    value = Convert.ToSByte((Convert.ToSByte(vLeft, FormatProvider) * Convert.ToSByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int16:
                                {
                                    value = Convert.ToInt16((Convert.ToInt16(vLeft, FormatProvider) * Convert.ToInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.SqlInt16:
                                {
                                    value = (SqlConvert.ConvertToSqlInt16(vLeft) * SqlConvert.ConvertToSqlInt16(vRight));
                                    break;
                                }
                            case StorageType.UInt16:
                                {
                                    value = Convert.ToUInt16((Convert.ToUInt16(vLeft, FormatProvider) * Convert.ToUInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int32:
                                {
                                    checked { value = Convert.ToInt32(vLeft, FormatProvider) * Convert.ToInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlInt32:
                                {
                                    value = (SqlConvert.ConvertToSqlInt32(vLeft) * SqlConvert.ConvertToSqlInt32(vRight));
                                    break;
                                }
                            case StorageType.UInt32:
                                {
                                    checked { value = Convert.ToUInt32(vLeft, FormatProvider) * Convert.ToUInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Int64:
                                {
                                    checked { value = Convert.ToInt64(vLeft, FormatProvider) * Convert.ToInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlInt64:
                                {
                                    value = (SqlConvert.ConvertToSqlInt64(vLeft) * SqlConvert.ConvertToSqlInt64(vRight));
                                    break;
                                }
                            case StorageType.UInt64:
                                {
                                    checked { value = Convert.ToUInt64(vLeft, FormatProvider) * Convert.ToUInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Decimal:
                                {
                                    checked { value = Convert.ToDecimal(vLeft, FormatProvider) * Convert.ToDecimal(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlDecimal:
                                {
                                    value = (SqlConvert.ConvertToSqlDecimal(vLeft) * SqlConvert.ConvertToSqlDecimal(vRight));
                                    break;
                                }
                            case StorageType.Single:
                                {
                                    checked { value = Convert.ToSingle(vLeft, FormatProvider) * Convert.ToSingle(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlSingle:
                                {
                                    value = (SqlConvert.ConvertToSqlSingle(vLeft) * SqlConvert.ConvertToSqlSingle(vRight));
                                    break;
                                }
                            case StorageType.SqlMoney:
                                {
                                    value = (SqlConvert.ConvertToSqlMoney(vLeft) * SqlConvert.ConvertToSqlMoney(vRight));
                                    break;
                                }
                            case StorageType.Double:
                                {
                                    checked { value = Convert.ToDouble(vLeft, FormatProvider) * Convert.ToDouble(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlDouble:
                                {
                                    value = (SqlConvert.ConvertToSqlDouble(vLeft) * SqlConvert.ConvertToSqlDouble(vRight));
                                    break;
                                }
                            default:
                                {
                                    typeMismatch = true;
                                    break;
                                }
                        }
                        break; // Operators.Multiply

                    case Operators.Divide:
                        switch (resultType)
                        {
                            case StorageType.Byte:
                                {
                                    value = Convert.ToByte((Convert.ToByte(vLeft, FormatProvider) / Convert.ToByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.SqlByte:
                                {
                                    value = (SqlConvert.ConvertToSqlByte(vLeft) / SqlConvert.ConvertToSqlByte(vRight));
                                    break;
                                }
                            case StorageType.SByte:
                                {
                                    value = Convert.ToSByte((Convert.ToSByte(vLeft, FormatProvider) / Convert.ToSByte(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int16:
                                {
                                    value = Convert.ToInt16((Convert.ToInt16(vLeft, FormatProvider) / Convert.ToInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.SqlInt16:
                                {
                                    value = (SqlConvert.ConvertToSqlInt16(vLeft) / SqlConvert.ConvertToSqlInt16(vRight));
                                    break;
                                }
                            case StorageType.UInt16:
                                {
                                    value = Convert.ToUInt16((Convert.ToUInt16(vLeft, FormatProvider) / Convert.ToUInt16(vRight, FormatProvider)), FormatProvider);
                                    break;
                                }
                            case StorageType.Int32:
                                {
                                    checked { value = Convert.ToInt32(vLeft, FormatProvider) / Convert.ToInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlInt32:
                                {
                                    value = (SqlConvert.ConvertToSqlInt32(vLeft) / SqlConvert.ConvertToSqlInt32(vRight));
                                    break;
                                }
                            case StorageType.UInt32:
                                {
                                    checked { value = Convert.ToUInt32(vLeft, FormatProvider) / Convert.ToUInt32(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.UInt64:
                                {
                                    checked { value = Convert.ToUInt64(vLeft, FormatProvider) / Convert.ToUInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.Int64:
                                {
                                    checked { value = Convert.ToInt64(vLeft, FormatProvider) / Convert.ToInt64(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlInt64:
                                {
                                    value = (SqlConvert.ConvertToSqlInt64(vLeft) / SqlConvert.ConvertToSqlInt64(vRight));
                                    break;
                                }
                            case StorageType.Decimal:
                                {
                                    checked { value = Convert.ToDecimal(vLeft, FormatProvider) / Convert.ToDecimal(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlDecimal:
                                {
                                    value = (SqlConvert.ConvertToSqlDecimal(vLeft) / SqlConvert.ConvertToSqlDecimal(vRight));
                                    break;
                                }
                            case StorageType.Single:
                                {
                                    checked { value = Convert.ToSingle(vLeft, FormatProvider) / Convert.ToSingle(vRight, FormatProvider); }
                                    break;
                                }
                            case StorageType.SqlSingle:
                                {
                                    value = (SqlConvert.ConvertToSqlSingle(vLeft) / SqlConvert.ConvertToSqlSingle(vRight));
                                    break;
                                }
                            case StorageType.SqlMoney:
                                {
                                    value = (SqlConvert.ConvertToSqlMoney(vLeft) / SqlConvert.ConvertToSqlMoney(vRight));
                                    break;
                                }
                            case StorageType.Double:
                                {
                                    double b = Convert.ToDouble(vRight, FormatProvider);
                                    checked { value = Convert.ToDouble(vLeft, FormatProvider) / b; }
                                    break;
                                }
                            case StorageType.SqlDouble:
                                {
                                    value = (SqlConvert.ConvertToSqlDouble(vLeft) / SqlConvert.ConvertToSqlDouble(vRight));
                                    break;
                                }
                            default:
                                {
                                    typeMismatch = true;
                                    break;
                                }
                        }
                        break; // Operators.Divide

                    case Operators.EqualTo:
                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)) ||
                             (vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight)))
                            return DBNull.Value;
                        return (0 == BinaryCompare(vLeft, vRight, resultType, Operators.EqualTo));

                    case Operators.GreaterThen:
                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)) ||
                             (vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight)))
                            return DBNull.Value;
                        return (0 < BinaryCompare(vLeft, vRight, resultType, op));

                    case Operators.LessThen:
                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)) ||
                             (vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight)))
                            return DBNull.Value;
                        return (0 > BinaryCompare(vLeft, vRight, resultType, op));

                    case Operators.GreaterOrEqual:
                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)) ||
                             (vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight)))
                            return DBNull.Value;
                        return (0 <= BinaryCompare(vLeft, vRight, resultType, op));

                    case Operators.LessOrEqual:
                        if (((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft))) ||
                             ((vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight))))
                            return DBNull.Value;
                        return (0 >= BinaryCompare(vLeft, vRight, resultType, op));

                    case Operators.NotEqual:
                        if (((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft))) ||
                             ((vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight))))
                            return DBNull.Value;
                        return (0 != BinaryCompare(vLeft, vRight, resultType, op));

                    case Operators.Is:
                        vLeft = BinaryNode.Eval(left, row, version, recordNos);
                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)))
                        {
                            return true;
                        }
                        return false;

                    case Operators.IsNot:
                        vLeft = BinaryNode.Eval(left, row, version, recordNos);
                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)))
                        {
                            return false;
                        }
                        return true;

                    case Operators.And:
                        /*
                        special case evaluating of the AND operator: we don't want to evaluate
                        both right and left operands, because we can shortcut :
                            If one of the operands is flase the result is false
                        CONSIDER : in the shortcut case do we want to type-check the other operand?
                        */
                        vLeft = BinaryNode.Eval(left, row, version, recordNos);
                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)))
                            return DBNull.Value;

                        if ((!(vLeft is bool)) && (!(vLeft is SqlBoolean)))
                        {
                            vRight = BinaryNode.Eval(right, row, version, recordNos);
                            typeMismatch = true;
                            break;
                        }

                        if (vLeft is bool)
                        {
                            if ((bool)vLeft == false)
                            {
                                value = false;
                                break;
                            }
                        }
                        else
                        {
                            if (((SqlBoolean)vLeft).IsFalse)
                            {
                                value = false;
                                break;
                            }
                        }
                        vRight = BinaryNode.Eval(right, row, version, recordNos);
                        if ((vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight)))
                            return DBNull.Value;

                        if ((!(vRight is bool)) && (!(vRight is SqlBoolean)))
                        {
                            typeMismatch = true;
                            break;
                        }

                        if (vRight is bool)
                        {
                            value = (bool)vRight;
                            break;
                        }
                        else
                        {
                            value = ((SqlBoolean)vRight).IsTrue;
                        }
                        break;
                    case Operators.Or:
                        /*
                        special case evaluating the OR operator: we don't want to evaluate
                        both right and left operands, because we can shortcut :
                            If one of the operands is true the result is true
                        CONSIDER : in the shortcut case do we want to type-check the other operand?
                        */

                        vLeft = BinaryNode.Eval(left, row, version, recordNos);

                        if ((vLeft != DBNull.Value) && (!DataStorage.IsObjectSqlNull(vLeft)))
                        {
                            if ((!(vLeft is bool)) && (!(vLeft is SqlBoolean)))
                            {
                                vRight = BinaryNode.Eval(right, row, version, recordNos);
                                typeMismatch = true;
                                break;
                            }

                            if ((bool)vLeft == true)
                            {
                                value = true;
                                break;
                            }
                        }

                        vRight = BinaryNode.Eval(right, row, version, recordNos);
                        if ((vRight == DBNull.Value) || (DataStorage.IsObjectSqlNull(vRight)))
                            return vLeft;

                        if ((vLeft == DBNull.Value) || (DataStorage.IsObjectSqlNull(vLeft)))
                            return vRight;

                        if ((!(vRight is bool)) && (!(vRight is SqlBoolean)))
                        {
                            typeMismatch = true;
                            break;
                        }

                        value = (vRight is bool) ? ((bool)vRight) : (((SqlBoolean)vRight).IsTrue);
                        break;

                    /*  for M3, use original code , in below,  and make sure to have two different code path; increases perf

                                        vLeft = BinaryNode.Eval(left, row, version, recordNos);
                                        if (vLeft != DBNull.Value) {
                                            if (!(vLeft is bool)) {
                                                vRight = BinaryNode.Eval(right, row, version, recordNos);
                                                typeMismatch = true;
                                                break;
                                            }

                                            if ((bool)vLeft == true) {
                                                value = true;
                                                break;
                                            }
                                        }

                                        vRight = BinaryNode.Eval(right, row, version, recordNos);
                                        if (vRight == DBNull.Value)
                                            return vLeft;

                                        if (vLeft == DBNull.Value)
                                            return vRight;

                                        if (!(vRight is bool)) {
                                            typeMismatch = true;
                                            break;
                                        }

                                        value = (bool)vRight;
                                        break;
                    */

                    case Operators.Modulo:
                        if (ExpressionNode.IsIntegerSql(resultType))
                        {
                            if (resultType == StorageType.UInt64)
                            {
                                value = Convert.ToUInt64(vLeft, FormatProvider) % Convert.ToUInt64(vRight, FormatProvider);
                            }
                            else if (DataStorage.IsSqlType(resultType))
                            {
                                SqlInt64 res = (SqlConvert.ConvertToSqlInt64(vLeft) % SqlConvert.ConvertToSqlInt64(vRight));

                                if (resultType == StorageType.SqlInt32)
                                {
                                    value = res.ToSqlInt32();
                                }
                                else if (resultType == StorageType.SqlInt16)
                                {
                                    value = res.ToSqlInt16();
                                }
                                else if (resultType == StorageType.SqlByte)
                                {
                                    value = res.ToSqlByte();
                                }
                                else
                                {
                                    value = res;
                                }
                            }
                            else
                            {
                                value = Convert.ToInt64(vLeft, FormatProvider) % Convert.ToInt64(vRight, FormatProvider);
                                value = Convert.ChangeType(value, DataStorage.GetTypeStorage(resultType), FormatProvider);
                            }
                        }
                        else
                        {
                            typeMismatch = true;
                        }
                        break;

                    case Operators.In:
                        /*
                        special case evaluating of the IN operator: the right have to be IN function node
                        */


                        if (!(right is FunctionNode))
                        {
                            // this is more like an Assert: should never happens, so we do not care about "nice" Exseptions
                            throw ExprException.InWithoutParentheses();
                        }

                        vLeft = BinaryNode.Eval(left, row, version, recordNos);

                        if ((vLeft == DBNull.Value) || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)))
                            return DBNull.Value;

                        /* validate IN parameters : must all be constant expressions */

                        value = false;

                        FunctionNode into = (FunctionNode)right;

                        for (int i = 0; i < into._argumentCount; i++)
                        {
                            vRight = into._arguments[i].Eval();


                            if ((vRight == DBNull.Value) || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight)))
                                continue;
                            Debug.Assert((!DataStorage.IsObjectNull(vLeft)) && (!DataStorage.IsObjectNull(vRight)), "Impossible.");

                            resultType = DataStorage.GetStorageType(vLeft.GetType());

                            if (0 == BinaryCompare(vLeft, vRight, resultType, Operators.EqualTo))
                            {
                                value = true;
                                break;
                            }
                        }
                        break;

                    default:
                        throw ExprException.UnsupportedOperator(op);
                }
            }
            catch (OverflowException)
            {
                throw ExprException.Overflow(DataStorage.GetTypeStorage(resultType));
            }
            if (typeMismatch)
            {
                SetTypeMismatchError(op, vLeft.GetType(), vRight.GetType());
            }

            return value;
        }

        // Data type precedence rules specify which data type is converted to the other.
        // The data type with the lower precedence is converted to the data type with the higher precedence.
        // If the conversion is not a supported implicit conversion, an error is returned.
        // When both operand expressions have the same data type, the result of the operation has that data type.
        // This is the precedence order for the DataSet numeric data types:

        private enum DataTypePrecedence
        {
            SqlDateTime = 25,
            DateTimeOffset = 24,
            DateTime = 23,
            TimeSpan = 20,
            SqlDouble = 19,
            Double = 18,
            SqlSingle = 17,
            Single = 16,
            SqlDecimal = 15,
            Decimal = 14,
            SqlMoney = 13,
            UInt64 = 12,
            SqlInt64 = 11,
            Int64 = 10,
            UInt32 = 9,
            SqlInt32 = 8,
            Int32 = 7,
            UInt16 = 6,
            SqlInt16 = 5,
            Int16 = 4,
            Byte = 3,
            SqlByte = 2,
            SByte = 1,
            Error = 0,
            SqlBoolean = -1,
            Boolean = -2,
            SqlGuid = -3,
            SqlString = -4,
            String = -5,
            SqlXml = -6,
            SqlChars = -7,
            Char = -8,
            SqlBytes = -9,
            SqlBinary = -10,
        }

        private DataTypePrecedence GetPrecedence(StorageType storageType)
        {
            switch (storageType)
            {
                case StorageType.Boolean: return DataTypePrecedence.Boolean;
                case StorageType.Char: return DataTypePrecedence.Char;
                case StorageType.SByte: return DataTypePrecedence.SByte;
                case StorageType.Byte: return DataTypePrecedence.Byte;
                case StorageType.Int16: return DataTypePrecedence.Int16;
                case StorageType.UInt16: return DataTypePrecedence.UInt16;
                case StorageType.Int32: return DataTypePrecedence.Int32;
                case StorageType.UInt32: return DataTypePrecedence.UInt32;
                case StorageType.Int64: return DataTypePrecedence.Int64;
                case StorageType.UInt64: return DataTypePrecedence.UInt64;
                case StorageType.Single: return DataTypePrecedence.Single;
                case StorageType.Double: return DataTypePrecedence.Double;
                case StorageType.Decimal: return DataTypePrecedence.Decimal;
                case StorageType.DateTime: return DataTypePrecedence.DateTime;
                case StorageType.DateTimeOffset: return DataTypePrecedence.DateTimeOffset;
                case StorageType.TimeSpan: return DataTypePrecedence.TimeSpan;
                case StorageType.String: return DataTypePrecedence.String;
                case StorageType.SqlBinary: return DataTypePrecedence.SqlBinary;
                case StorageType.SqlBoolean: return DataTypePrecedence.SqlBoolean;
                case StorageType.SqlByte: return DataTypePrecedence.SqlByte;
                case StorageType.SqlBytes: return DataTypePrecedence.SqlBytes;
                case StorageType.SqlChars: return DataTypePrecedence.SqlChars;
                case StorageType.SqlDateTime: return DataTypePrecedence.SqlDateTime;
                case StorageType.SqlDecimal: return DataTypePrecedence.SqlDecimal;
                case StorageType.SqlDouble: return DataTypePrecedence.SqlDouble;
                case StorageType.SqlGuid: return DataTypePrecedence.SqlGuid;
                case StorageType.SqlInt16: return DataTypePrecedence.SqlInt16;
                case StorageType.SqlInt32: return DataTypePrecedence.SqlInt32;
                case StorageType.SqlInt64: return DataTypePrecedence.SqlInt64;
                case StorageType.SqlMoney: return DataTypePrecedence.SqlMoney;
                case StorageType.SqlSingle: return DataTypePrecedence.SqlSingle;
                case StorageType.SqlString: return DataTypePrecedence.SqlString;
                //            case StorageType.SqlXml: return DataTypePrecedence.SqlXml;
                case StorageType.Empty:
                case StorageType.Object:
                case StorageType.DBNull:
                default: return DataTypePrecedence.Error;
            }
        }

        private static StorageType GetPrecedenceType(DataTypePrecedence code)
        {
            switch (code)
            {
                case DataTypePrecedence.Error: return StorageType.Empty;
                case DataTypePrecedence.SByte: return StorageType.SByte;
                case DataTypePrecedence.Byte: return StorageType.Byte;
                case DataTypePrecedence.Int16: return StorageType.Int16;
                case DataTypePrecedence.UInt16: return StorageType.UInt16;
                case DataTypePrecedence.Int32: return StorageType.Int32;
                case DataTypePrecedence.UInt32: return StorageType.UInt32;
                case DataTypePrecedence.Int64: return StorageType.Int64;
                case DataTypePrecedence.UInt64: return StorageType.UInt64;
                case DataTypePrecedence.Decimal: return StorageType.Decimal;
                case DataTypePrecedence.Single: return StorageType.Single;
                case DataTypePrecedence.Double: return StorageType.Double;

                case DataTypePrecedence.Boolean: return StorageType.Boolean;
                case DataTypePrecedence.String: return StorageType.String;
                case DataTypePrecedence.Char: return StorageType.Char;

                case DataTypePrecedence.DateTimeOffset: return StorageType.DateTimeOffset;
                case DataTypePrecedence.DateTime: return StorageType.DateTime;
                case DataTypePrecedence.TimeSpan: return StorageType.TimeSpan;

                case DataTypePrecedence.SqlDateTime: return StorageType.SqlDateTime;
                case DataTypePrecedence.SqlDouble: return StorageType.SqlDouble;
                case DataTypePrecedence.SqlSingle: return StorageType.SqlSingle;
                case DataTypePrecedence.SqlDecimal: return StorageType.SqlDecimal;
                case DataTypePrecedence.SqlInt64: return StorageType.SqlInt64;
                case DataTypePrecedence.SqlInt32: return StorageType.SqlInt32;
                case DataTypePrecedence.SqlInt16: return StorageType.SqlInt16;
                case DataTypePrecedence.SqlByte: return StorageType.SqlByte;
                case DataTypePrecedence.SqlBoolean: return StorageType.SqlBoolean;
                case DataTypePrecedence.SqlString: return StorageType.SqlString;
                case DataTypePrecedence.SqlGuid: return StorageType.SqlGuid;
                case DataTypePrecedence.SqlBinary: return StorageType.SqlBinary;
                case DataTypePrecedence.SqlMoney: return StorageType.SqlMoney;
                default:
                    Debug.Assert(false, "Invalid (unmapped) precedence " + code.ToString());
                    goto case DataTypePrecedence.Error;
            }
        }

        private bool IsMixed(StorageType left, StorageType right)
        {
            return ((IsSigned(left) && IsUnsigned(right)) ||
                    (IsUnsigned(left) && IsSigned(right)));
        }

        private bool IsMixedSql(StorageType left, StorageType right)
        {
            return ((IsSignedSql(left) && IsUnsignedSql(right)) ||
                    (IsUnsignedSql(left) && IsSignedSql(right)));
        }

        internal StorageType ResultType(StorageType left, StorageType right, bool lc, bool rc, int op)
        {
            if ((left == StorageType.Guid) && (right == StorageType.Guid) && Operators.IsRelational(op))
                return left;
            if ((left == StorageType.String) && (right == StorageType.Guid) && Operators.IsRelational(op))
                return left;
            if ((left == StorageType.Guid) && (right == StorageType.String) && Operators.IsRelational(op))
                return right;

            int leftPrecedence = (int)GetPrecedence(left);
            if (leftPrecedence == (int)DataTypePrecedence.Error)
            {
                return StorageType.Empty;
            }

            int rightPrecedence = (int)GetPrecedence(right);
            if (rightPrecedence == (int)DataTypePrecedence.Error)
            {
                return StorageType.Empty;
            }

            if (Operators.IsLogical(op))
            {
                if (left == StorageType.Boolean && right == StorageType.Boolean)
                    return StorageType.Boolean;
                else
                    return StorageType.Empty;
            }
            if ((left == StorageType.DateTimeOffset) || (right == StorageType.DateTimeOffset))
            {
                // Rules to handle DateTimeOffset:
                // we only allow Relational operations to operate only on DTO vs DTO
                // all other operations: "exception"
                if (Operators.IsRelational(op) && left == StorageType.DateTimeOffset && right == StorageType.DateTimeOffset)
                    return StorageType.DateTimeOffset;
                return StorageType.Empty;
            }

            if ((op == Operators.Plus) && ((left == StorageType.String) || (right == StorageType.String)))
                return StorageType.String;

            DataTypePrecedence higherPrec = (DataTypePrecedence)Math.Max(leftPrecedence, rightPrecedence);

            StorageType result = GetPrecedenceType(higherPrec);

            if (Operators.IsArithmetical(op))
            {
                if (result != StorageType.String && result != StorageType.Char)
                {
                    if (!IsNumeric(left))
                        return StorageType.Empty;
                    if (!IsNumeric(right))
                        return StorageType.Empty;
                }
            }

            // if the operation is a division the result should be at least a double

            if ((op == Operators.Divide) && IsInteger(result))
            {
                return StorageType.Double;
            }

            if (IsMixed(left, right))
            {
                // we are dealing with one signed and one unsigned type so
                // try to see if one of them is a ConstNode
                if (lc && (!rc))
                {
                    return right;
                }
                else if ((!lc) && rc)
                {
                    return left;
                }

                if (IsUnsigned(result))
                {
                    if (higherPrec < DataTypePrecedence.UInt64)
                        // left and right are mixed integers but with the same length
                        // so promote to the next signed type
                        result = GetPrecedenceType(higherPrec + 1);
                    else
                        throw ExprException.AmbiguousBinop(op, DataStorage.GetTypeStorage(left), DataStorage.GetTypeStorage(right));
                }
            }

            return result;
        }

        internal StorageType ResultSqlType(StorageType left, StorageType right, bool lc, bool rc, int op)
        {
            int leftPrecedence = (int)GetPrecedence(left);
            if (leftPrecedence == (int)DataTypePrecedence.Error)
            {
                return StorageType.Empty;
            }

            int rightPrecedence = (int)GetPrecedence(right);
            if (rightPrecedence == (int)DataTypePrecedence.Error)
            {
                return StorageType.Empty;
            }

            if (Operators.IsLogical(op))
            {
                if ((left != StorageType.Boolean && left != StorageType.SqlBoolean) || (right != StorageType.Boolean && right != StorageType.SqlBoolean))
                    return StorageType.Empty;
                if (left == StorageType.Boolean && right == StorageType.Boolean)
                    return StorageType.Boolean;
                return StorageType.SqlBoolean;
            }

            if (op == Operators.Plus)
            {
                if ((left == StorageType.SqlString) || (right == StorageType.SqlString))
                    return StorageType.SqlString;
                if ((left == StorageType.String) || (right == StorageType.String))
                    return StorageType.String;
            }
            //SqlBinary is operable just with SqlBinary
            if ((left == StorageType.SqlBinary && right != StorageType.SqlBinary) || (left != StorageType.SqlBinary && right == StorageType.SqlBinary))
                return StorageType.Empty;
            //SqlGuid is operable just with SqlGuid
            if ((left == StorageType.SqlGuid && right != StorageType.SqlGuid) || (left != StorageType.SqlGuid && right == StorageType.SqlGuid))
                return StorageType.Empty;

            if ((leftPrecedence > (int)DataTypePrecedence.SqlDouble && rightPrecedence < (int)DataTypePrecedence.TimeSpan))
            {
                return StorageType.Empty;
            }

            if ((leftPrecedence < (int)DataTypePrecedence.TimeSpan && rightPrecedence > (int)DataTypePrecedence.SqlDouble))
            {
                return StorageType.Empty;
            }

            if (leftPrecedence > (int)DataTypePrecedence.SqlDouble)
            {
                if (op == Operators.Plus || op == Operators.Minus)
                {
                    if (left == StorageType.TimeSpan)
                        return right;
                    if (right == StorageType.TimeSpan)
                        return left;
                    return StorageType.Empty; // for plus or minus operations for  time types, one of them MUST be time span
                }

                if (!Operators.IsRelational(op))
                    return StorageType.Empty; // we just have relational operations amoung time types
                return left;
            }
            // time types finished
            // continue with numerical types, numbers

            DataTypePrecedence higherPrec = (DataTypePrecedence)Math.Max(leftPrecedence, rightPrecedence);

            StorageType result = GetPrecedenceType(higherPrec);
            // if we have at least one Sql type, the intermediate result should be Sql type
            result = GetPrecedenceType((DataTypePrecedence)SqlResultType((int)higherPrec));

            if (Operators.IsArithmetical(op))
            {
                if (result != StorageType.String && result != StorageType.Char && result != StorageType.SqlString)
                {
                    if (!IsNumericSql(left))
                        return StorageType.Empty;
                    if (!IsNumericSql(right))
                        return StorageType.Empty;
                }
            }

            // if the operation is a division the result should be at least a double
            if ((op == Operators.Divide) && IsIntegerSql(result))
            {
                return StorageType.SqlDouble;
            }

            if (result == StorageType.SqlMoney)
            {
                if ((left != StorageType.SqlMoney) && (right != StorageType.SqlMoney))
                    result = StorageType.SqlDecimal;
            }

            if (IsMixedSql(left, right))
            {
                // we are dealing with one signed and one unsigned type so
                // try to see if one of them is a ConstNode

                if (IsUnsignedSql(result))
                {
                    if (higherPrec < DataTypePrecedence.UInt64)
                        // left and right are mixed integers but with the same length
                        // so promote to the next signed type
                        result = GetPrecedenceType(higherPrec + 1);
                    else
                        throw ExprException.AmbiguousBinop(op, DataStorage.GetTypeStorage(left), DataStorage.GetTypeStorage(right));
                }
            }

            return result;
        }

        private int SqlResultType(int typeCode)
        {
            switch (typeCode)
            {
                case 23: return 24;
                case 20: return 21;
                case 18: return 19;
                case 16: return 17;
                case 14: return 15;
                case 12: return 13;
                case 9:
                case 10: return 11;
                case 6:
                case 7: return 8;
                case 3:
                case 4: return 5;
                case 1: return 2;
                case -2: return -1;
                case -5: return -4;
                case -8: return -7;
                default: return typeCode;
            }
        }
    }

    internal sealed class LikeNode : BinaryNode
    {
        // like kinds
        internal const int match_left = 1;      // <STR>*
        internal const int match_right = 2;     // *<STR>
        internal const int match_middle = 3;    // *<STR>*
        internal const int match_exact = 4;    // <STR>
        internal const int match_all = 5;      // *

        private int _kind;
        private string _pattern = null;

        internal LikeNode(DataTable table, int op, ExpressionNode left, ExpressionNode right)
        : base(table, op, left, right)
        {
        }

        internal override object Eval(DataRow row, DataRowVersion version)
        {
            object vRight;
            object vLeft = _left.Eval(row, version);
            string substring;


            if ((vLeft == DBNull.Value) || (_left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)))
                return DBNull.Value;

            if (_pattern == null)
            {
                vRight = _right.Eval(row, version);

                if (!(vRight is string) && !(vRight is SqlString))
                {
                    SetTypeMismatchError(_op, vLeft.GetType(), vRight.GetType());
                }

                if (vRight == DBNull.Value || DataStorage.IsObjectSqlNull(vRight))
                    return DBNull.Value;
                string rightStr = (string)SqlConvert.ChangeType2(vRight, StorageType.String, typeof(string), FormatProvider);


                // need to convert like pattern to a string

                // Parce the original pattern, and get the constant part of it..
                substring = AnalyzePattern(rightStr);

                if (_right.IsConstant())
                    _pattern = substring;
            }
            else
            {
                substring = _pattern;
            }

            if (!(vLeft is string) && !(vLeft is SqlString))
            {
                SetTypeMismatchError(_op, vLeft.GetType(), typeof(string));
            }

            // WhiteSpace Chars Include : 0x9, 0xA, 0xB, 0xC, 0xD, 0x20, 0xA0, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A, 0x200B, 0x3000, and 0xFEFF.
            char[] trimChars = new char[2] { (char)0x20, (char)0x3000 };
            string tempStr;
            if (vLeft is SqlString)
                tempStr = ((SqlString)vLeft).Value;
            else
                tempStr = (string)vLeft;

            string s1 = (tempStr).TrimEnd(trimChars);

            switch (_kind)
            {
                case match_all:
                    return true;
                case match_exact:
                    return (0 == table.Compare(s1, substring));
                case match_middle:
                    return (0 <= table.IndexOf(s1, substring));
                case match_left:
                    return (0 == table.IndexOf(s1, substring));
                case match_right:
                    string s2 = substring.TrimEnd(trimChars);
                    return table.IsSuffix(s1, s2);
                default:
                    Debug.Assert(false, "Unexpected LIKE kind");
                    return DBNull.Value;
            }
        }

        internal string AnalyzePattern(string pat)
        {
            int length = pat.Length;
            char[] patchars = new char[length + 1];
            pat.CopyTo(0, patchars, 0, length);
            patchars[length] = (char)0;
            string substring = null;

            char[] constchars = new char[length + 1];
            int newLength = 0;

            int stars = 0;

            int i = 0;

            while (i < length)
            {
                if (patchars[i] == '*' || patchars[i] == '%')
                {
                    // replace conseq. * or % with one..
                    while ((patchars[i] == '*' || patchars[i] == '%') && i < length)
                        i++;

                    // we allowing only *str* pattern
                    if ((i < length && newLength > 0) || stars >= 2)
                    {
                        // we have a star inside string constant..
                        throw ExprException.InvalidPattern(pat);
                    }
                    stars++;
                }
                else if (patchars[i] == '[')
                {
                    i++;
                    if (i >= length)
                    {
                        throw ExprException.InvalidPattern(pat);
                    }
                    constchars[newLength++] = patchars[i++];

                    if (i >= length)
                    {
                        throw ExprException.InvalidPattern(pat);
                    }

                    if (patchars[i] != ']')
                    {
                        throw ExprException.InvalidPattern(pat);
                    }
                    i++;
                }
                else
                {
                    constchars[newLength++] = patchars[i];
                    i++;
                }
            }

            substring = new string(constchars, 0, newLength);

            if (stars == 0)
            {
                _kind = match_exact;
            }
            else
            {
                if (newLength > 0)
                {
                    if (patchars[0] == '*' || patchars[0] == '%')
                    {
                        if (patchars[length - 1] == '*' || patchars[length - 1] == '%')
                        {
                            _kind = match_middle;
                        }
                        else
                        {
                            _kind = match_right;
                        }
                    }
                    else
                    {
                        Debug.Assert(patchars[length - 1] == '*' || patchars[length - 1] == '%', "Invalid LIKE pattern formed.. ");
                        _kind = match_left;
                    }
                }
                else
                {
                    _kind = match_all;
                }
            }
            return substring;
        }
    }
}
