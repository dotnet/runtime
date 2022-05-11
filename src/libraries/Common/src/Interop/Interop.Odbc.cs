// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data.Odbc;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif
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
        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLAllocHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/IntPtr InputHandle,
            /*SQLHANDLE* */out IntPtr OutputHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLAllocHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/OdbcHandle InputHandle,
            /*SQLHANDLE* */out IntPtr OutputHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial /*SQLRETURN*/ODBC32.SQLRETURN SQLBindCol(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef TargetValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */IntPtr StrLen_or_Ind);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLBindCol(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/IntPtr TargetValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */IntPtr StrLen_or_Ind);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial /*SQLRETURN*/ODBC32.SQLRETURN SQLBindParameter(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ParameterNumber,
            /*SQLSMALLINT*/short ParamDirection,
            /*SQLSMALLINT*/ODBC32.SQL_C SQLCType,
            /*SQLSMALLINT*/short SQLType,
            /*SQLULEN*/IntPtr cbColDef,
            /*SQLSMALLINT*/IntPtr ibScale,
            /*SQLPOINTER*/
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef rgbValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef StrLen_or_Ind);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLCancel(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLCloseCursor(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLColAttributeW(
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

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLColumnsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/short NameLen3,
            /*SQLCHAR* */string ColumnName,
            /*SQLSMALLINT*/short NameLen4);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLDisconnect(
            /*SQLHDBC*/IntPtr ConnectionHandle);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLDriverConnectW(
            /*SQLHDBC*/OdbcConnectionHandle hdbc,
            /*SQLHWND*/IntPtr hwnd,
            /*SQLCHAR* */string connectionstring,
            /*SQLSMALLINT*/short cbConnectionstring,
            /*SQLCHAR* */IntPtr connectionstringout,
            /*SQLSMALLINT*/short cbConnectionstringoutMax,
            /*SQLSMALLINT* */out short cbConnectionstringout,
            /*SQLUSMALLINT*/short fDriverCompletion);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLEndTran(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/IntPtr Handle,
            /*SQLSMALLINT*/short CompletionType);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLExecDirectW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string StatementText,
            /*SQLINTEGER*/int TextLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLExecute(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLFetch(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLFreeHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHSTMT*/IntPtr StatementHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLFreeStmt(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ODBC32.STMT Option);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/byte[] Value,
            /*SQLINTEGER*/int BufferLength,
            /*SQLINTEGER* */out int StringLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetData(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ushort ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/CNativeBuffer TargetValue,
            /*SQLLEN*/IntPtr BufferLength, // sql.h differs from MSDN
            /*SQLLEN* */out IntPtr StrLen_or_Ind);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetDescFieldW(
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLUSMALLINT*/short RecNumber,
            /*SQLUSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
            /*SQLPOINTER*/CNativeBuffer ValuePointer,
            /*SQLINTEGER*/int BufferLength,
            /*SQLINTEGER* */out int StringLength);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLGetDiagRecW(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/OdbcHandle Handle,
            /*SQLSMALLINT*/short RecNumber,
            /*SQLCHAR* */  char[] rchState,
            /*SQLINTEGER* */out int NativeError,
            /*SQLCHAR* */  char[] MessageText,
            /*SQLSMALLINT*/short BufferLength,
            /*SQLSMALLINT* */out short TextLength);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLGetDiagFieldW(
           /*SQLSMALLINT*/ ODBC32.SQL_HANDLE HandleType,
           /*SQLHANDLE*/   OdbcHandle Handle,
           /*SQLSMALLINT*/ short RecNumber,
           /*SQLSMALLINT*/ short DiagIdentifier,
           /*SQLPOINTER*/  char[] rchState,
           /*SQLSMALLINT*/ short BufferLength,
           /*SQLSMALLINT* */ out short StringLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetFunctions(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_API fFunction,
            /*SQLUSMALLINT* */out short pfExists);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetInfoW(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
            /*SQLPOINTER*/byte[] rgbInfoValue,
            /*SQLSMALLINT*/short cbInfoValueMax,
            /*SQLSMALLINT* */out short pcbInfoValue);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetInfoW(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
            /*SQLPOINTER*/byte[] rgbInfoValue,
            /*SQLSMALLINT*/short cbInfoValueMax,
            /*SQLSMALLINT* */IntPtr pcbInfoValue);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetStmtAttrW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/out IntPtr Value,
            /*SQLINTEGER*/int BufferLength,
            /*SQLINTEGER*/out int StringLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLGetTypeInfo(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLSMALLINT*/short fSqlType);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLMoreResults(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLNumResultCols(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLSMALLINT* */out short ColumnCount);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLPrepareW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string StatementText,
            /*SQLINTEGER*/int TextLength);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLPrimaryKeysW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string? CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */ string? SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/short NameLen3);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLProcedureColumnsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */ string? CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */ string? SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */ string? ProcName,
            /*SQLSMALLINT*/short NameLen3,
            /*SQLCHAR* */ string? ColumnName,
            /*SQLSMALLINT*/short NameLen4);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLProceduresW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */ string CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */ string SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */ string ProcName,
            /*SQLSMALLINT*/short NameLen3);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLRowCount(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLLEN* */out IntPtr RowCount);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/string Value,
            /*SQLINTEGER*/int StringLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/int StringLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLSetConnectAttrW( // used only for AutoCommitOn
            /*SQLHBDC*/IntPtr ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/int StringLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial /*SQLRETURN*/ODBC32.SQLRETURN SQLSetDescFieldW(
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLSMALLINT*/short ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
            /*SQLPOINTER*/
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef CharacterAttribute,
            /*SQLINTEGER*/int BufferLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLSetDescFieldW(
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLSMALLINT*/short ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
            /*SQLPOINTER*/IntPtr CharacterAttribute,
            /*SQLINTEGER*/int BufferLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        // user can set SQL_ATTR_CONNECTION_POOLING attribute with envHandle = null, this attribute is process-level attribute
        internal static partial ODBC32.SQLRETURN SQLSetEnvAttr(
            /*SQLHENV*/OdbcEnvironmentHandle EnvironmentHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/ODBC32.SQL_IS StringLength);

        [LibraryImport(Interop.Libraries.Odbc32)]
        internal static partial ODBC32.SQLRETURN SQLSetStmtAttrW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLINTEGER*/int Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/int StringLength);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLSpecialColumnsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ODBC32.SQL_SPECIALCOLS IdentifierType,
            /*SQLCHAR* */string? CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */string? SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/short NameLen3,
            /*SQLUSMALLINT*/ODBC32.SQL_SCOPE Scope,
            /*SQLUSMALLINT*/ ODBC32.SQL_NULLABILITY Nullable);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLStatisticsW(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLCHAR* */string? CatalogName,
            /*SQLSMALLINT*/short NameLen1,
            /*SQLCHAR* */string? SchemaName,
            /*SQLSMALLINT*/short NameLen2,
            /*SQLCHAR* */IntPtr TableName, // IntPtr instead of string because callee may mutate contents
            /*SQLSMALLINT*/short NameLen3,
            /*SQLUSMALLINT*/short Unique,
            /*SQLUSMALLINT*/short Reserved);

        [LibraryImport(Interop.Libraries.Odbc32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ODBC32.SQLRETURN SQLTablesW(
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
