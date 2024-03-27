// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data
{
    internal sealed class ConstNode : ExpressionNode
    {
        internal readonly object _val;

        internal ConstNode(DataTable? table, ValueType type, object constant) : this(table, type, constant, true)
        {
        }

        internal ConstNode(DataTable? table, ValueType type, object constant, bool fParseQuotes) : base(table)
        {
            switch (type)
            {
                case ValueType.Null:
                    _val = DBNull.Value;
                    break;

                case ValueType.Numeric:
                    _val = SmallestNumeric(constant);
                    break;
                case ValueType.Decimal:
                    _val = SmallestDecimal(constant);
                    break;
                case ValueType.Float:
                    _val = Convert.ToDouble(constant, NumberFormatInfo.InvariantInfo);
                    break;

                case ValueType.Bool:
                    _val = Convert.ToBoolean(constant, CultureInfo.InvariantCulture);
                    break;

                case ValueType.Str:
                    if (fParseQuotes)
                    {
                        // replace '' with one '
                        _val = ((string)constant).Replace("''", "'");
                    }
                    else
                    {
                        _val = (string)constant;
                    }
                    break;

                case ValueType.Date:
                    _val = DateTime.Parse((string)constant, CultureInfo.InvariantCulture);
                    break;

                case ValueType.Object:
                    _val = constant;
                    break;

                default:
                    Debug.Fail("NYI");
                    goto case ValueType.Object;
            }
        }

        internal override void Bind(DataTable table, List<DataColumn> list)
        {
            BindTable(table);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval()
        {
            return _val;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(DataRow? row, DataRowVersion version)
        {
            return Eval();
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
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

        private static object SmallestDecimal(object constant)
        {
            if (null == constant)
            {
                return 0d;
            }
            else
            {
                string? sval = (constant as string);
                if (null != sval)
                {
                    decimal r12;
                    if (decimal.TryParse(sval, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out r12))
                    {
                        return r12;
                    }

                    double r8;
                    if (double.TryParse(sval, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out r8))
                    {
                        return r8;
                    }
                }
                else
                {
                    IConvertible? convertible = (constant as IConvertible);
                    if (null != convertible)
                    {
                        try
                        {
                            return convertible.ToDecimal(NumberFormatInfo.InvariantInfo);
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
                        try
                        {
                            return convertible.ToDouble(NumberFormatInfo.InvariantInfo);
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
                    }
                }
            }
            return constant;
        }

        private static object SmallestNumeric(object constant)
        {
            if (null == constant)
            {
                return 0;
            }
            else
            {
                string? sval = (constant as string);
                if (null != sval)
                {
                    int i4;
                    if (int.TryParse(sval, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i4))
                    {
                        return i4;
                    }
                    long i8;
                    if (long.TryParse(sval, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i8))
                    {
                        return i8;
                    }
                    double r8;
                    if (double.TryParse(sval, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out r8))
                    {
                        return r8;
                    }
                }
                else
                {
                    IConvertible? convertible = (constant as IConvertible);
                    if (null != convertible)
                    {
                        try
                        {
                            return convertible.ToInt32(NumberFormatInfo.InvariantInfo);
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

                        try
                        {
                            return convertible.ToInt64(NumberFormatInfo.InvariantInfo);
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

                        try
                        {
                            return convertible.ToDouble(NumberFormatInfo.InvariantInfo);
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
                    }
                }
            }
            return constant;
        }
    }
}
