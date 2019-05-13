// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data.Odbc;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

internal static partial class Interop
{
    internal static partial class Odbc
    {

        //
        // ODBC32
        //
        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLAllocHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/IntPtr InputHandle,
            /*SQLHANDLE* */out IntPtr OutputHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLAllocHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/OdbcHandle InputHandle,
            /*SQLHANDLE* */out IntPtr OutputHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLBindCol(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/HandleRef TargetValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */IntPtr StrLen_or_Ind);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLBindCol(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/IntPtr TargetValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */IntPtr StrLen_or_Ind);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLBindParameter(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ParameterNumber,
            /*SQLSMALLINT*/short ParamDirection,
            /*SQLSMALLINT*/ODBC32.SQL_C SQLCType,
            /*SQLSMALLINT*/short SQLType,
            /*SQLULEN*/IntPtr cbColDef,
            /*SQLSMALLINT*/IntPtr ibScale,
            /*SQLPOINTER*/HandleRef rgbValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */HandleRef StrLen_or_Ind);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLCancel(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLCloseCursor(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLColAttributeW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/short ColumnNumber,
            /*SQLUSMALLINT*/short FieldIdentifier,
            /*SQLPOINTER*/CNativeBuffer CharacterAttribute,
            /*SQLSMALLINT*/short BufferLength,
            /*SQLSMALLINT* */out short StringLength,
            /*SQLPOINTER*/out IntPtr NumericAttribute);

        // note: in sql.h this is defined differently for the 64Bit platform.
        // However, for us the code is not different for SQLPOINTER or SQLLEN ...
        // frome sql.h:
        // #ifdef _WIN64
        // SQLRETURN  SQL_API SQLColAttribute (SQLHSTMT StatementHandle,
        //            SQLUSMALLINT ColumnNumber, SQLUSMALLINT FieldIdentifier,
        //            SQLPOINTER CharacterAttribute, SQLSMALLINT BufferLength,
        //            SQLSMALLINT *StringLength, SQLLEN *NumericAttribute);
        // #else
        // SQLRETURN  SQL_API SQLColAttribute (SQLHSTMT StatementHandle,
        //            SQLUSMALLINT ColumnNumber, SQLUSMALLINT FieldIdentifier,
        //            SQLPOINTER CharacterAttribute, SQLSMALLINT BufferLength,
        //            SQLSMALLINT *StringLength, SQLPOINTER NumericAttribute);
        // #endif

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLColumnsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/short NameLen3,
            /*SQLCHAR* */string ColumnName,
            /*SQLSMALLINT*/short NameLen4);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLDisconnect(
            /*SQLHDBC*/IntPtr ConnectionHandle);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLDriverConnectW(
            /*SQLHDBC*/OdbcConnectionHandle hdbc,
            /*SQLHWND*/IntPtr hwnd,
            /*SQLCHAR* */string connectionstring,
            /*SQLSMALLINT*/short cbConnectionstring,
            /*SQLCHAR* */IntPtr connectionstringout,
            /*SQLSMALLINT*/short cbConnectionstringoutMax,
            /*SQLSMALLINT* */out short cbConnectionstringout,
            /*SQLUSMALLINT*/short fDriverCompletion);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLEndTran(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/IntPtr Handle,
            /*SQLSMALLINT*/short CompletionType);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLExecDirectW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string   StatementText,
            /*SQLINTEGER*/int TextLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLExecute(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLFetch(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLFreeHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHSTMT*/IntPtr StatementHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLFreeStmt(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ODBC32.STMT Option);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/byte[] Value,
            /*SQLINTEGER*/int BufferLength,
            /*SQLINTEGER* */out int StringLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetData(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/CNativeBuffer TargetValue,
            /*SQLLEN*/IntPtr BufferLength, // sql.h differs from MSDN
            /*SQLLEN* */out IntPtr StrLen_or_Ind);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetDescFieldW(
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLUSMALLINT*/short RecNumber,
            /*SQLUSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
            /*SQLPOINTER*/CNativeBuffer ValuePointer,
            /*SQLINTEGER*/int BufferLength,
            /*SQLINTEGER* */out int StringLength);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetDiagRecW(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/OdbcHandle Handle,
            /*SQLSMALLINT*/short RecNumber,
            /*SQLCHAR* */  [Out] StringBuilder rchState,
            /*SQLINTEGER* */out int NativeError,
            /*SQLCHAR* */  [Out] StringBuilder MessageText,
            /*SQLSMALLINT*/short BufferLength,
            /*SQLSMALLINT* */out short TextLength);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetDiagFieldW(
           /*SQLSMALLINT*/ ODBC32.SQL_HANDLE HandleType,
           /*SQLHANDLE*/   OdbcHandle Handle,
           /*SQLSMALLINT*/ short RecNumber,
           /*SQLSMALLINT*/ short DiagIdentifier,
           /*SQLPOINTER*/  [Out] StringBuilder rchState,
           /*SQLSMALLINT*/ short BufferLength,
           /*SQLSMALLINT* */ out short StringLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetFunctions(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_API fFunction,
            /*SQLUSMALLINT* */out short pfExists);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetInfoW(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
            /*SQLPOINTER*/byte[] rgbInfoValue,
            /*SQLSMALLINT*/short cbInfoValueMax,
            /*SQLSMALLINT* */out short pcbInfoValue);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetInfoW(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
            /*SQLPOINTER*/byte[] rgbInfoValue,
            /*SQLSMALLINT*/short cbInfoValueMax,
            /*SQLSMALLINT* */IntPtr pcbInfoValue);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetStmtAttrW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/out IntPtr Value,
            /*SQLINTEGER*/int BufferLength,
            /*SQLINTEGER*/out int StringLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLGetTypeInfo(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLSMALLINT*/short fSqlType);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLMoreResults(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLNumResultCols(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLSMALLINT* */out short ColumnCount);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLPrepareW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string   StatementText,
            /*SQLINTEGER*/int TextLength);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLPrimaryKeysW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */ string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/short NameLen3);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLProcedureColumnsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */ string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */ string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */ string ProcName,
            /*SQLSMALLINT*/short NameLen3,
            /*SQLCHAR* */ string ColumnName,
            /*SQLSMALLINT*/short NameLen4);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLProceduresW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */ string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */ string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */ string ProcName,
            /*SQLSMALLINT*/short NameLen3);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLRowCount(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLLEN* */out IntPtr RowCount);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/System.Transactions.IDtcTransaction Value,
            /*SQLINTEGER*/int StringLength);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/string Value,
            /*SQLINTEGER*/int StringLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/int StringLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW( // used only for AutoCommitOn
            /*SQLHBDC*/IntPtr ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/int StringLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetDescFieldW(
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLSMALLINT*/short ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
            /*SQLPOINTER*/HandleRef CharacterAttribute,
            /*SQLINTEGER*/int BufferLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetDescFieldW(
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLSMALLINT*/short ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
            /*SQLPOINTER*/IntPtr CharacterAttribute,
            /*SQLINTEGER*/int BufferLength);

        [DllImport(Interop.Libraries.Odbc32)]
        // user can set SQL_ATTR_CONNECTION_POOLING attribute with envHandle = null, this attribute is process-level attribute
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetEnvAttr(
            /*SQLHENV*/OdbcEnvironmentHandle EnvironmentHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/ODBC32.SQL_IS StringLength);

        [DllImport(Interop.Libraries.Odbc32)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSetStmtAttrW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLINTEGER*/int Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/int StringLength);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLSpecialColumnsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ODBC32.SQL_SPECIALCOLS IdentifierType,
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/short NameLen3,
            /*SQLUSMALLINT*/ODBC32.SQL_SCOPE Scope,
            /*SQLUSMALLINT*/ ODBC32.SQL_NULLABILITY Nullable);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLStatisticsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */IntPtr TableName, // IntPtr instead of string because callee may mutate contents
            /*SQLSMALLINT*/short NameLen3,
            /*SQLUSMALLINT*/short Unique,
            /*SQLUSMALLINT*/short Reserved);

        [DllImport(Interop.Libraries.Odbc32, CharSet = CharSet.Unicode)]
        internal static extern /*SQLRETURN*/ODBC32.RetCode SQLTablesW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/short NameLen3,
            /*SQLCHAR* */string TableType,
            /*SQLSMALLINT*/short NameLen4);
    }
}
