// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.InteropServices;

namespace System.Data.Odbc
{
    internal struct SQLLEN
    {
        private IntPtr _value;

        internal SQLLEN(int value)
        {
            _value = new IntPtr(value);
        }

        internal SQLLEN(long value)
        {
#if WIN32
            _value = new IntPtr(checked((int)value));
#else
            _value = new IntPtr(value);
#endif
        }

        internal SQLLEN(IntPtr value)
        {
            _value = value;
        }

        public static implicit operator SQLLEN(int value)
        { // 
            return new SQLLEN(value);
        }

        public static explicit operator SQLLEN(long value)
        {
            return new SQLLEN(value);
        }

        public static unsafe implicit operator int (SQLLEN value)
        { // 
#if WIN32
            return (int)value._value.ToInt32();
#else
            long l = (long)value._value.ToInt64();
            return checked((int)l);
#endif
        }

        public static unsafe explicit operator long (SQLLEN value)
        {
            return value._value.ToInt64();
        }

        public unsafe long ToInt64()
        {
            return _value.ToInt64();
        }
    }

    internal sealed class OdbcStatementHandle : OdbcHandle
    {
        internal OdbcStatementHandle(OdbcConnectionHandle connectionHandle) : base(ODBC32.SQL_HANDLE.STMT, connectionHandle)
        {
        }

        internal ODBC32.RetCode BindColumn2(int columnNumber, ODBC32.SQL_C targetType, HandleRef buffer, IntPtr length, IntPtr srLen_or_Ind)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLBindCol(this, checked((ushort)columnNumber), targetType, buffer, length, srLen_or_Ind);
            ODBC.TraceODBC(3, "SQLBindCol", retcode);
            return retcode;
        }

        internal ODBC32.RetCode BindColumn3(int columnNumber, ODBC32.SQL_C targetType, IntPtr srLen_or_Ind)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLBindCol(this, checked((ushort)columnNumber), targetType, ADP.PtrZero, ADP.PtrZero, srLen_or_Ind);
            ODBC.TraceODBC(3, "SQLBindCol", retcode);
            return retcode;
        }

        internal ODBC32.RetCode BindParameter(short ordinal, short parameterDirection, ODBC32.SQL_C sqlctype, ODBC32.SQL_TYPE sqltype, IntPtr cchSize, IntPtr scale, HandleRef buffer, IntPtr bufferLength, HandleRef intbuffer)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLBindParameter(this,
                                    checked((ushort)ordinal),   // Parameter Number
                                    parameterDirection,         // InputOutputType
                                    sqlctype,                   // ValueType
                                    checked((short)sqltype),    // ParameterType
                                    cchSize,                    // ColumnSize
                                    scale,                      // DecimalDigits
                                    buffer,                     // ParameterValuePtr
                                    bufferLength,               // BufferLength
                                    intbuffer);                 // StrLen_or_IndPtr
            ODBC.TraceODBC(3, "SQLBindParameter", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Cancel()
        {
            // In ODBC3.0 ... a call to SQLCancel when no processing is done has no effect at all
            // (ODBC Programmer's Reference ...)
            ODBC32.RetCode retcode = Interop.Odbc.SQLCancel(this);
            ODBC.TraceODBC(3, "SQLCancel", retcode);
            return retcode;
        }

        internal ODBC32.RetCode CloseCursor()
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLCloseCursor(this);
            ODBC.TraceODBC(3, "SQLCloseCursor", retcode);
            return retcode;
        }

        internal ODBC32.RetCode ColumnAttribute(int columnNumber, short fieldIdentifier, CNativeBuffer characterAttribute, out short stringLength, out SQLLEN numericAttribute)
        {
            IntPtr result;
            ODBC32.RetCode retcode = Interop.Odbc.SQLColAttributeW(this, checked((short)columnNumber), fieldIdentifier, characterAttribute, characterAttribute.ShortLength, out stringLength, out result);
            numericAttribute = new SQLLEN(result);
            ODBC.TraceODBC(3, "SQLColAttributeW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Columns(string tableCatalog,
                                        string tableSchema,
                                        string tableName,
                                        string columnName)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLColumnsW(this,
                                                                     tableCatalog,
                                                                     ODBC.ShortStringLength(tableCatalog),
                                                                     tableSchema,
                                                                     ODBC.ShortStringLength(tableSchema),
                                                                     tableName,
                                                                     ODBC.ShortStringLength(tableName),
                                                                     columnName,
                                                                     ODBC.ShortStringLength(columnName));

            ODBC.TraceODBC(3, "SQLColumnsW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Execute()
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLExecute(this);
            ODBC.TraceODBC(3, "SQLExecute", retcode);
            return retcode;
        }

        internal ODBC32.RetCode ExecuteDirect(string commandText)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLExecDirectW(this, commandText, ODBC32.SQL_NTS);
            ODBC.TraceODBC(3, "SQLExecDirectW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Fetch()
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLFetch(this);
            ODBC.TraceODBC(3, "SQLFetch", retcode);
            return retcode;
        }

        internal ODBC32.RetCode FreeStatement(ODBC32.STMT stmt)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLFreeStmt(this, stmt);
            ODBC.TraceODBC(3, "SQLFreeStmt", retcode);
            return retcode;
        }

        internal ODBC32.RetCode GetData(int index, ODBC32.SQL_C sqlctype, CNativeBuffer buffer, int cb, out IntPtr cbActual)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetData(this,
                            checked((ushort)index),
                            sqlctype,
                            buffer,
                            new IntPtr(cb),
                            out cbActual);
            ODBC.TraceODBC(3, "SQLGetData", retcode);
            return retcode;
        }

        internal ODBC32.RetCode GetStatementAttribute(ODBC32.SQL_ATTR attribute, out IntPtr value, out int stringLength)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetStmtAttrW(this, attribute, out value, ADP.PtrSize, out stringLength);
            ODBC.TraceODBC(3, "SQLGetStmtAttrW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode GetTypeInfo(short fSqlType)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetTypeInfo(this, fSqlType);
            ODBC.TraceODBC(3, "SQLGetTypeInfo", retcode);
            return retcode;
        }

        internal ODBC32.RetCode MoreResults()
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLMoreResults(this);
            ODBC.TraceODBC(3, "SQLMoreResults", retcode);
            return retcode;
        }

        internal ODBC32.RetCode NumberOfResultColumns(out short columnsAffected)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLNumResultCols(this, out columnsAffected);
            ODBC.TraceODBC(3, "SQLNumResultCols", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Prepare(string commandText)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLPrepareW(this, commandText, ODBC32.SQL_NTS);
            ODBC.TraceODBC(3, "SQLPrepareW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode PrimaryKeys(string catalogName, string schemaName, string tableName)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLPrimaryKeysW(this,
                            catalogName, ODBC.ShortStringLength(catalogName),          // CatalogName
                            schemaName, ODBC.ShortStringLength(schemaName),            // SchemaName
                            tableName, ODBC.ShortStringLength(tableName)              // TableName
            );
            ODBC.TraceODBC(3, "SQLPrimaryKeysW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Procedures(string procedureCatalog,
                                           string procedureSchema,
                                           string procedureName)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLProceduresW(this,
                                                                        procedureCatalog,
                                                                        ODBC.ShortStringLength(procedureCatalog),
                                                                        procedureSchema,
                                                                        ODBC.ShortStringLength(procedureSchema),
                                                                        procedureName,
                                                                        ODBC.ShortStringLength(procedureName));

            ODBC.TraceODBC(3, "SQLProceduresW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode ProcedureColumns(string procedureCatalog,
                                                 string procedureSchema,
                                                 string procedureName,
                                                 string columnName)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLProcedureColumnsW(this,
                                                                              procedureCatalog,
                                                                              ODBC.ShortStringLength(procedureCatalog),
                                                                              procedureSchema,
                                                                              ODBC.ShortStringLength(procedureSchema),
                                                                              procedureName,
                                                                              ODBC.ShortStringLength(procedureName),
                                                                              columnName,
                                                                              ODBC.ShortStringLength(columnName));

            ODBC.TraceODBC(3, "SQLProcedureColumnsW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode RowCount(out SQLLEN rowCount)
        {
            IntPtr result;
            ODBC32.RetCode retcode = Interop.Odbc.SQLRowCount(this, out result);
            rowCount = new SQLLEN(result);
            ODBC.TraceODBC(3, "SQLRowCount", retcode);
            return retcode;
        }

        internal ODBC32.RetCode SetStatementAttribute(ODBC32.SQL_ATTR attribute, IntPtr value, ODBC32.SQL_IS stringLength)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLSetStmtAttrW(this, (int)attribute, value, (int)stringLength);
            ODBC.TraceODBC(3, "SQLSetStmtAttrW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode SpecialColumns(string quotedTable)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLSpecialColumnsW(this,
            ODBC32.SQL_SPECIALCOLS.ROWVER, null, 0, null, 0,
            quotedTable, ODBC.ShortStringLength(quotedTable),
            ODBC32.SQL_SCOPE.SESSION, ODBC32.SQL_NULLABILITY.NO_NULLS);
            ODBC.TraceODBC(3, "SQLSpecialColumnsW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Statistics(string tableCatalog,
                                           string tableSchema,
                                           string tableName,
                                           short unique,
                                           short accuracy)
        {
            ODBC32.RetCode retcode;

            // MDAC Bug 75928 - SQLStatisticsW damages the string passed in
            // To protect the tablename we need to pass in a copy of that string

            IntPtr pwszTableName = Marshal.StringToCoTaskMemUni(tableName);
            try
            {
                retcode = Interop.Odbc.SQLStatisticsW(this,
                                                      tableCatalog,
                                                      ODBC.ShortStringLength(tableCatalog),
                                                      tableSchema,
                                                      ODBC.ShortStringLength(tableSchema),
                                                      pwszTableName,
                                                      ODBC.ShortStringLength(tableName),
                                                      unique,
                                                      accuracy);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pwszTableName);
            }

            ODBC.TraceODBC(3, "SQLStatisticsW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode Statistics(string tableName)
        {
            return Statistics(null, null, tableName, (short)ODBC32.SQL_INDEX.UNIQUE, (short)ODBC32.SQL_STATISTICS_RESERVED.ENSURE);
        }

        internal ODBC32.RetCode Tables(string tableCatalog,
                                       string tableSchema,
                                       string tableName,
                                       string tableType)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLTablesW(this,
                                                                    tableCatalog,
                                                                    ODBC.ShortStringLength(tableCatalog),
                                                                    tableSchema,
                                                                    ODBC.ShortStringLength(tableSchema),
                                                                    tableName,
                                                                    ODBC.ShortStringLength(tableName),
                                                                    tableType,
                                                                    ODBC.ShortStringLength(tableType));

            ODBC.TraceODBC(3, "SQLTablesW", retcode);
            return retcode;
        }
    }
}
