// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;

namespace System.Data
{
    internal abstract class ExpressionNode
    {
        private DataTable _table;

        protected ExpressionNode(DataTable table)
        {
            _table = table;
        }

        internal IFormatProvider FormatProvider
        {
            get
            {
                return ((null != _table) ? _table.FormatProvider : System.Globalization.CultureInfo.CurrentCulture);
            }
        }

        internal virtual bool IsSqlColumn
        {
            get
            {
                return false;
            }
        }

        protected DataTable table
        {
            get { return _table; }
        }

        protected void BindTable(DataTable table)
        {
            // when the expression is created, DataColumn may not be associated with a table yet
            _table = table;
        }

        internal abstract void Bind(DataTable table, List<DataColumn> list);
        internal abstract object Eval();
        internal abstract object Eval(DataRow row, DataRowVersion version);
        internal abstract object Eval(int[] recordNos);
        internal abstract bool IsConstant();
        internal abstract bool IsTableConstant();
        internal abstract bool HasLocalAggregate();
        internal abstract bool HasRemoteAggregate();
        internal abstract ExpressionNode Optimize();
        internal virtual bool DependsOn(DataColumn column)
        {
            return false;
        }

        internal static bool IsInteger(StorageType type)
        {
            return (type == StorageType.Int16 ||
                type == StorageType.Int32 ||
                type == StorageType.Int64 ||
                type == StorageType.UInt16 ||
                type == StorageType.UInt32 ||
                type == StorageType.UInt64 ||
                type == StorageType.SByte ||
                type == StorageType.Byte);
        }

        internal static bool IsIntegerSql(StorageType type)
        {
            return (type == StorageType.Int16 ||
                type == StorageType.Int32 ||
                type == StorageType.Int64 ||
                type == StorageType.UInt16 ||
                type == StorageType.UInt32 ||
                type == StorageType.UInt64 ||
                type == StorageType.SByte ||
                type == StorageType.Byte ||
                type == StorageType.SqlInt64 ||
                type == StorageType.SqlInt32 ||
                type == StorageType.SqlInt16 ||
                type == StorageType.SqlByte);
        }

        internal static bool IsSigned(StorageType type)
        {
            return (type == StorageType.Int16 ||
                type == StorageType.Int32 ||
                type == StorageType.Int64 ||
                type == StorageType.SByte ||
                IsFloat(type));
        }

        internal static bool IsSignedSql(StorageType type)
        {
            return (type == StorageType.Int16 || // IsSigned(type)
                type == StorageType.Int32 ||
                type == StorageType.Int64 ||
                type == StorageType.SByte ||
                type == StorageType.SqlInt64 ||
                type == StorageType.SqlInt32 ||
                type == StorageType.SqlInt16 ||
                IsFloatSql(type));
        }

        internal static bool IsUnsigned(StorageType type)
        {
            return (type == StorageType.UInt16 ||
                   type == StorageType.UInt32 ||
                   type == StorageType.UInt64 ||
                   type == StorageType.Byte);
        }

        internal static bool IsUnsignedSql(StorageType type)
        {
            return (type == StorageType.UInt16 ||
                   type == StorageType.UInt32 ||
                   type == StorageType.UInt64 ||
                   type == StorageType.SqlByte ||// SqlByte represents an 8-bit unsigned integer, in the range of 0 through 255,
                   type == StorageType.Byte);
        }

        internal static bool IsNumeric(StorageType type)
        {
            return (IsFloat(type) ||
                   IsInteger(type));
        }

        internal static bool IsNumericSql(StorageType type)
        {
            return (IsFloatSql(type) ||
                   IsIntegerSql(type));
        }

        internal static bool IsFloat(StorageType type)
        {
            return (type == StorageType.Single ||
                type == StorageType.Double ||
                type == StorageType.Decimal);
        }

        internal static bool IsFloatSql(StorageType type)
        {
            return (type == StorageType.Single ||
                type == StorageType.Double ||
                type == StorageType.Decimal ||
                type == StorageType.SqlDouble ||
                type == StorageType.SqlDecimal || // I expect decimal to be Integer!
                type == StorageType.SqlMoney ||   // if decimal is here, this should be definitely here!
                type == StorageType.SqlSingle);
        }
    }
}
