// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.OleDb
{
    internal static class ODB
    {
        // OleDbCommand
        internal static void CommandParameterStatus(StringBuilder builder, int index, DBStatus status)
        {
            switch (status)
            {
                case DBStatus.S_OK:
                case DBStatus.S_ISNULL:
                case DBStatus.S_IGNORE:
                    break;

                case DBStatus.E_BADACCESSOR:
                    builder.Append(SR.Format(SR.OleDb_CommandParameterBadAccessor, index.ToString(CultureInfo.InvariantCulture), ""));
                    builder.Append(Environment.NewLine);
                    break;

                case DBStatus.E_CANTCONVERTVALUE:
                    builder.Append(SR.Format(SR.OleDb_CommandParameterCantConvertValue, index.ToString(CultureInfo.InvariantCulture), ""));
                    builder.Append(Environment.NewLine);
                    break;

                case DBStatus.E_SIGNMISMATCH:
                    builder.Append(SR.Format(SR.OleDb_CommandParameterSignMismatch, index.ToString(CultureInfo.InvariantCulture), ""));
                    builder.Append(Environment.NewLine);
                    break;

                case DBStatus.E_DATAOVERFLOW:
                    builder.Append(SR.Format(SR.OleDb_CommandParameterDataOverflow, index.ToString(CultureInfo.InvariantCulture), ""));
                    builder.Append(Environment.NewLine);
                    break;

                case DBStatus.E_CANTCREATE:
                    Debug.Assert(false, "CommandParameterStatus: unexpected E_CANTCREATE");
                    goto default;

                case DBStatus.E_UNAVAILABLE:
                    builder.Append(SR.Format(SR.OleDb_CommandParameterUnavailable, index.ToString(CultureInfo.InvariantCulture), ""));
                    builder.Append(Environment.NewLine);
                    break;

                case DBStatus.E_PERMISSIONDENIED:
                    Debug.Assert(false, "CommandParameterStatus: unexpected E_PERMISSIONDENIED");
                    goto default;

                case DBStatus.E_INTEGRITYVIOLATION:
                    Debug.Assert(false, "CommandParameterStatus: unexpected E_INTEGRITYVIOLATION");
                    goto default;

                case DBStatus.E_SCHEMAVIOLATION:
                    Debug.Assert(false, "CommandParameterStatus: unexpected E_SCHEMAVIOLATION");
                    goto default;

                case DBStatus.E_BADSTATUS:
                    Debug.Assert(false, "CommandParameterStatus: unexpected E_BADSTATUS");
                    goto default;

                case DBStatus.S_DEFAULT:
                    builder.Append(SR.Format(SR.OleDb_CommandParameterDefault, index.ToString(CultureInfo.InvariantCulture), ""));
                    builder.Append(Environment.NewLine);
                    break;

                default:
                    builder.Append(SR.Format(SR.OleDb_CommandParameterError, index.ToString(CultureInfo.InvariantCulture), status.ToString()));
                    builder.Append(Environment.NewLine);
                    break;
            }
        }
        internal static Exception CommandParameterStatus(string value, Exception inner)
        {
            if (ADP.IsEmpty(value))
            { return inner; }
            return ADP.InvalidOperation(value, inner);
        }
        internal static Exception UninitializedParameters(int index, OleDbType dbtype)
        {
            return ADP.InvalidOperation(SR.Format(SR.OleDb_UninitializedParameters, index.ToString(CultureInfo.InvariantCulture), dbtype.ToString()));
        }
        internal static Exception BadStatus_ParamAcc(int index, DBBindStatus status)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_BadStatus_ParamAcc, index.ToString(CultureInfo.InvariantCulture), status.ToString()));
        }
        internal static Exception NoProviderSupportForParameters(string provider, Exception? inner)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_NoProviderSupportForParameters, provider), inner);
        }
        internal static Exception NoProviderSupportForSProcResetParameters(string provider)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_NoProviderSupportForSProcResetParameters, provider));
        }

        // OleDbProperties
        internal static void PropsetSetFailure(StringBuilder builder, string description, OleDbPropertyStatus status)
        {
            if (OleDbPropertyStatus.Ok == status)
            {
                return;
            }
            switch (status)
            {
                case OleDbPropertyStatus.NotSupported:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyNotSupported, description));
                    break;
                case OleDbPropertyStatus.BadValue:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyBadValue, description));
                    break;
                case OleDbPropertyStatus.BadOption:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyBadOption, description));
                    break;
                case OleDbPropertyStatus.BadColumn:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyBadColumn, description));
                    break;
                case OleDbPropertyStatus.NotAllSettable:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyNotAllSettable, description));
                    break;
                case OleDbPropertyStatus.NotSettable:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyNotSettable, description));
                    break;
                case OleDbPropertyStatus.NotSet:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyNotSet, description));
                    break;
                case OleDbPropertyStatus.Conflicting:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyConflicting, description));
                    break;
                case OleDbPropertyStatus.NotAvailable:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyNotAvailable, description));
                    break;
                default:
                    if (0 < builder.Length)
                    { builder.Append(Environment.NewLine); }
                    builder.Append(SR.Format(SR.OleDb_PropertyStatusUnknown, ((int)status).ToString(CultureInfo.InvariantCulture)));
                    break;
            }
        }
        internal static Exception PropsetSetFailure(string value, Exception inner)
        {
            if (ADP.IsEmpty(value))
            { return inner; }
            return ADP.InvalidOperation(value, inner);
        }

        // OleDbConnection
        internal static ArgumentException SchemaRowsetsNotSupported(string provider)
        {
            return ADP.Argument(SR.Format(SR.OleDb_SchemaRowsetsNotSupported, "IDBSchemaRowset", provider));
        }
        internal static OleDbException NoErrorInformation(string? provider, OleDbHResult hr, Exception? inner)
        {
            OleDbException e;
            if (!ADP.IsEmpty(provider))
            {
                e = new OleDbException(SR.Format(SR.OleDb_NoErrorInformation2, provider, ODB.ELookup(hr)), hr, inner);
            }
            else
            {
                e = new OleDbException(SR.Format(SR.OleDb_NoErrorInformation, ODB.ELookup(hr)), hr, inner);
            }
            ADP.TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidOperationException MDACNotAvailable(Exception? inner)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_MDACNotAvailable), inner);
        }
        internal static ArgumentException MSDASQLNotSupported()
        {
            return ADP.Argument(SR.Format(SR.OleDb_MSDASQLNotSupported));
        }
        internal static InvalidOperationException CommandTextNotSupported(string provider, Exception? inner)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_CommandTextNotSupported, provider), inner);
        }
        internal static InvalidOperationException PossiblePromptNotUserInteractive()
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_PossiblePromptNotUserInteractive));
        }
        internal static InvalidOperationException ProviderUnavailable(string provider, Exception? inner)
        {
            //return new OleDbException(SR.Format(SR.OleDb_ProviderUnavailable, provider), (int)OleDbHResult.CO_E_CLASSSTRING, inner);
            return ADP.DataAdapter(SR.Format(SR.OleDb_ProviderUnavailable, provider), inner);
        }
        internal static InvalidOperationException TransactionsNotSupported(string provider, Exception? inner)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_TransactionsNotSupported, provider), inner);
        }
        internal static ArgumentException AsynchronousNotSupported()
        {
            return ADP.Argument(SR.Format(SR.OleDb_AsynchronousNotSupported));
        }
        internal static ArgumentException NoProviderSpecified()
        {
            return ADP.Argument(SR.Format(SR.OleDb_NoProviderSpecified));
        }
        internal static ArgumentException InvalidProviderSpecified()
        {
            return ADP.Argument(SR.Format(SR.OleDb_InvalidProviderSpecified));
        }
        internal static ArgumentException InvalidRestrictionsDbInfoKeywords(string parameter)
        {
            return ADP.Argument(SR.Format(SR.OleDb_InvalidRestrictionsDbInfoKeywords), parameter);
        }
        internal static ArgumentException InvalidRestrictionsDbInfoLiteral(string parameter)
        {
            return ADP.Argument(SR.Format(SR.OleDb_InvalidRestrictionsDbInfoLiteral), parameter);
        }
        internal static ArgumentException InvalidRestrictionsSchemaGuids(string parameter)
        {
            return ADP.Argument(SR.Format(SR.OleDb_InvalidRestrictionsSchemaGuids), parameter);
        }
        internal static ArgumentException NotSupportedSchemaTable(Guid schema, OleDbConnection connection)
        {
            return ADP.Argument(SR.Format(SR.OleDb_NotSupportedSchemaTable, OleDbSchemaGuid.GetTextFromValue(schema), connection.Provider));
        }

        // OleDbParameter
        internal static Exception InvalidOleDbType(OleDbType value)
        {
            return ADP.InvalidEnumerationValue(typeof(OleDbType), (int)value);
        }

        // Getting Data
        internal static InvalidOperationException BadAccessor()
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_BadAccessor));
        }
        internal static InvalidCastException ConversionRequired()
        {
            return ADP.InvalidCast();
        }
        internal static InvalidCastException CantConvertValue()
        {
            return ADP.InvalidCast(SR.Format(SR.OleDb_CantConvertValue));
        }
        internal static InvalidOperationException SignMismatch(Type type)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_SignMismatch, type.Name));
        }
        internal static InvalidOperationException DataOverflow(Type type)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_DataOverflow, type.Name));
        }
        internal static InvalidOperationException CantCreate(Type type)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_CantCreate, type.Name));
        }
        internal static InvalidOperationException Unavailable(Type type)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_Unavailable, type.Name));
        }
        internal static InvalidOperationException UnexpectedStatusValue(DBStatus status)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_UnexpectedStatusValue, status.ToString()));
        }
        internal static InvalidOperationException GVtUnknown(int wType)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_GVtUnknown, wType.ToString("X4", CultureInfo.InvariantCulture), wType.ToString(CultureInfo.InvariantCulture)));
        }
        internal static InvalidOperationException SVtUnknown(int wType)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_SVtUnknown, wType.ToString("X4", CultureInfo.InvariantCulture), wType.ToString(CultureInfo.InvariantCulture)));
        }

        // OleDbDataReader
        internal static InvalidOperationException BadStatusRowAccessor(int i, DBBindStatus rowStatus)
        {
            return ADP.DataAdapter(SR.Format(SR.OleDb_BadStatusRowAccessor, i.ToString(CultureInfo.InvariantCulture), rowStatus.ToString()));
        }
        internal static InvalidOperationException ThreadApartmentState(Exception innerException)
        {
            return ADP.InvalidOperation(SR.Format(SR.OleDb_ThreadApartmentState), innerException);
        }

        // OleDbDataAdapter
        internal static ArgumentException Fill_NotADODB(string parameter)
        {
            return ADP.Argument(SR.Format(SR.OleDb_Fill_NotADODB), parameter);
        }
        internal static ArgumentException Fill_EmptyRecordSet(string parameter, Exception innerException)
        {
            return ADP.Argument(SR.Format(SR.OleDb_Fill_EmptyRecordSet, "IRowset"), parameter, innerException);
        }
        internal static ArgumentException Fill_EmptyRecord(string parameter, Exception innerException)
        {
            return ADP.Argument(SR.Format(SR.OleDb_Fill_EmptyRecord), parameter, innerException);
        }

        internal static string NoErrorMessage(OleDbHResult errorcode)
        {
            return SR.Format(SR.OleDb_NoErrorMessage, ODB.ELookup(errorcode));
        }
        internal static string FailedGetDescription(OleDbHResult errorcode)
        {
            return SR.Format(SR.OleDb_FailedGetDescription, ODB.ELookup(errorcode));
        }
        internal static string FailedGetSource(OleDbHResult errorcode)
        {
            return SR.Format(SR.OleDb_FailedGetSource, ODB.ELookup(errorcode));
        }

        internal static InvalidOperationException DBBindingGetVector()
        {
            return ADP.InvalidOperation(SR.Format(SR.OleDb_DBBindingGetVector));
        }

        internal static OleDbHResult GetErrorDescription(UnsafeNativeMethods.IErrorInfo errorInfo, OleDbHResult hresult, out string message)
        {
            OleDbHResult hr = errorInfo.GetDescription(out message!);
            if (((int)hr < 0) && ADP.IsEmpty(message))
            {
                message = FailedGetDescription(hr) + Environment.NewLine + ODB.ELookup(hresult);
            }
            if (ADP.IsEmpty(message))
            {
                message = ODB.ELookup(hresult);
            }
            return hr;
        }

        // OleDbEnumerator
        internal static ArgumentException ISourcesRowsetNotSupported()
        {
            throw ADP.Argument(SR.OleDb_ISourcesRowsetNotSupported);
        }

        // OleDbMetaDataFactory
        internal static InvalidOperationException IDBInfoNotSupported()
        {
            return ADP.InvalidOperation(SR.Format(SR.OleDb_IDBInfoNotSupported));
        }

        // explicitly used error codes
        internal const int ADODB_AlreadyClosedError = unchecked((int)0x800A0E78);
        internal const int ADODB_NextResultError = unchecked((int)0x800A0CB3);

        // internal command states
        internal const int InternalStateExecuting = (int)(ConnectionState.Open | ConnectionState.Executing);
        internal const int InternalStateFetching = (int)(ConnectionState.Open | ConnectionState.Fetching);
        internal const int InternalStateClosed = (int)(ConnectionState.Closed);

        internal const int ExecutedIMultipleResults = 0;
        internal const int ExecutedIRowset = 1;
        internal const int ExecutedIRow = 2;
        internal const int PrepareICommandText = 3;

        // internal connection states, a superset of the command states
        internal const int InternalStateExecutingNot = (int)~(ConnectionState.Executing);
        internal const int InternalStateFetchingNot = (int)~(ConnectionState.Fetching);
        internal const int InternalStateConnecting = (int)(ConnectionState.Connecting);
        internal const int InternalStateOpen = (int)(ConnectionState.Open);

        // constants used to trigger from binding as WSTR to BYREF|WSTR
        // used by OleDbCommand, OleDbDataReader
        internal const int LargeDataSize = (1 << 13); // 8K
        internal const int CacheIncrement = 10;

        // constants used by OleDbDataReader
        internal static readonly IntPtr DBRESULTFLAG_DEFAULT = IntPtr.Zero;

        internal const short VARIANT_TRUE = -1;
        internal const short VARIANT_FALSE = 0;

        // OleDbConnection constants
        internal const int CLSCTX_ALL = /*CLSCTX_INPROC_SERVER*/1 | /*CLSCTX_INPROC_HANDLER*/2 | /*CLSCTX_LOCAL_SERVER*/4 | /*CLSCTX_REMOTE_SERVER*/16;
        internal const int MaxProgIdLength = 255;

        internal const int DBLITERAL_CATALOG_SEPARATOR = 3;
        internal const int DBLITERAL_QUOTE_PREFIX = 15;
        internal const int DBLITERAL_QUOTE_SUFFIX = 28;
        internal const int DBLITERAL_SCHEMA_SEPARATOR = 27;
        internal const int DBLITERAL_TABLE_NAME = 17;
        internal const int DBPROP_ACCESSORDER = 0xe7;

        internal const int DBPROP_AUTH_CACHE_AUTHINFO = 0x5;
        internal const int DBPROP_AUTH_ENCRYPT_PASSWORD = 0x6;
        internal const int DBPROP_AUTH_INTEGRATED = 0x7;
        internal const int DBPROP_AUTH_MASK_PASSWORD = 0x8;
        internal const int DBPROP_AUTH_PASSWORD = 0x9;
        internal const int DBPROP_AUTH_PERSIST_ENCRYPTED = 0xa;
        internal const int DBPROP_AUTH_PERSIST_SENSITIVE_AUTHINFO = 0xb;
        internal const int DBPROP_AUTH_USERID = 0xc;

        internal const int DBPROP_CATALOGLOCATION = 0x16;
        internal const int DBPROP_COMMANDTIMEOUT = 0x22;
        internal const int DBPROP_CONNECTIONSTATUS = 0xf4;
        internal const int DBPROP_CURRENTCATALOG = 0x25;
        internal const int DBPROP_DATASOURCENAME = 0x26;
        internal const int DBPROP_DBMSNAME = 0x28;
        internal const int DBPROP_DBMSVER = 0x29;
        internal const int DBPROP_GROUPBY = 0x2c;
        internal const int DBPROP_HIDDENCOLUMNS = 0x102;
        internal const int DBPROP_IColumnsRowset = 0x7b;
        internal const int DBPROP_IDENTIFIERCASE = 0x2e;

        internal const int DBPROP_INIT_ASYNCH = 0xc8;
        internal const int DBPROP_INIT_BINDFLAGS = 0x10e;
        internal const int DBPROP_INIT_CATALOG = 0xe9;
        internal const int DBPROP_INIT_DATASOURCE = 0x3b;
        internal const int DBPROP_INIT_GENERALTIMEOUT = 0x11c;
        internal const int DBPROP_INIT_HWND = 0x3c;
        internal const int DBPROP_INIT_IMPERSONATION_LEVEL = 0x3d;
        internal const int DBPROP_INIT_LCID = 0xba;
        internal const int DBPROP_INIT_LOCATION = 0x3e;
        internal const int DBPROP_INIT_LOCKOWNER = 0x10f;
        internal const int DBPROP_INIT_MODE = 0x3f;
        internal const int DBPROP_INIT_OLEDBSERVICES = 0xf8;
        internal const int DBPROP_INIT_PROMPT = 0x40;
        internal const int DBPROP_INIT_PROTECTION_LEVEL = 0x41;
        internal const int DBPROP_INIT_PROVIDERSTRING = 0xa0;
        internal const int DBPROP_INIT_TIMEOUT = 0x42;

        internal const int DBPROP_IRow = 0x107;
        internal const int DBPROP_MAXROWS = 0x49;
        internal const int DBPROP_MULTIPLERESULTS = 0xc4;
        internal const int DBPROP_ORDERBYCOLUNSINSELECT = 0x55;
        internal const int DBPROP_PROVIDERFILENAME = 0x60;
        internal const int DBPROP_QUOTEDIDENTIFIERCASE = 0x64;
        internal const int DBPROP_RESETDATASOURCE = 0xf7;
        internal const int DBPROP_SQLSUPPORT = 0x6d;
        internal const int DBPROP_UNIQUEROWS = 0xee;

        // property status
        internal const int DBPROPSTATUS_OK = 0;
        internal const int DBPROPSTATUS_NOTSUPPORTED = 1;
        internal const int DBPROPSTATUS_BADVALUE = 2;
        internal const int DBPROPSTATUS_BADOPTION = 3;
        internal const int DBPROPSTATUS_BADCOLUMN = 4;
        internal const int DBPROPSTATUS_NOTALLSETTABLE = 5;
        internal const int DBPROPSTATUS_NOTSETTABLE = 6;
        internal const int DBPROPSTATUS_NOTSET = 7;
        internal const int DBPROPSTATUS_CONFLICTING = 8;
        internal const int DBPROPSTATUS_NOTAVAILABLE = 9;

        internal const int DBPROPOPTIONS_REQUIRED = 0;
        internal const int DBPROPOPTIONS_OPTIONAL = 1;

        internal const int DBPROPFLAGS_WRITE = 0x400;
        internal const int DBPROPFLAGS_SESSION = 0x1000;

        // misc. property values
        internal const int DBPROPVAL_AO_RANDOM = 2;

        internal const int DBPROPVAL_CL_END = 2;
        internal const int DBPROPVAL_CL_START = 1;

        internal const int DBPROPVAL_CS_COMMUNICATIONFAILURE = 2;
        internal const int DBPROPVAL_CS_INITIALIZED = 1;
        internal const int DBPROPVAL_CS_UNINITIALIZED = 0;

        internal const int DBPROPVAL_GB_COLLATE = 16;
        internal const int DBPROPVAL_GB_CONTAINS_SELECT = 4;
        internal const int DBPROPVAL_GB_EQUALS_SELECT = 2;
        internal const int DBPROPVAL_GB_NO_RELATION = 8;
        internal const int DBPROPVAL_GB_NOT_SUPPORTED = 1;

        internal const int DBPROPVAL_IC_LOWER = 2;
        internal const int DBPROPVAL_IC_MIXED = 8;
        internal const int DBPROPVAL_IC_SENSITIVE = 4;
        internal const int DBPROPVAL_IC_UPPER = 1;

        internal const int DBPROPVAL_IN_ALLOWNULL = 0x00000000;
        /*internal const int DBPROPVAL_IN_DISALLOWNULL  = 0x00000001;
        internal const int DBPROPVAL_IN_IGNORENULL    = 0x00000002;
        internal const int DBPROPVAL_IN_IGNOREANYNULL = 0x00000004;*/
        internal const int DBPROPVAL_MR_NOTSUPPORTED = 0;

        internal const int DBPROPVAL_RD_RESETALL = unchecked((int)0xffffffff);

        internal const int DBPROPVAL_OS_RESOURCEPOOLING = 0x00000001;
        internal const int DBPROPVAL_OS_TXNENLISTMENT = 0x00000002;
        internal const int DBPROPVAL_OS_CLIENTCURSOR = 0x00000004;
        internal const int DBPROPVAL_OS_AGR_AFTERSESSION = 0x00000008;
        internal const int DBPROPVAL_SQL_ODBC_MINIMUM = 1;
        internal const int DBPROPVAL_SQL_ESCAPECLAUSES = 0x00000100;

        // OLE DB providers never return pGuid-style bindings.
        // They are provided as a convenient shortcut for consumers supplying bindings all covered by the same GUID (for example, when creating bindings to access data).
        internal const int DBKIND_GUID_NAME = 0;
        internal const int DBKIND_GUID_PROPID = 1;
        internal const int DBKIND_NAME = 2;
        internal const int DBKIND_PGUID_NAME = 3;
        internal const int DBKIND_PGUID_PROPID = 4;
        internal const int DBKIND_PROPID = 5;
        internal const int DBKIND_GUID = 6;

        internal const int DBCOLUMNFLAGS_ISBOOKMARK = 0x01;
        internal const int DBCOLUMNFLAGS_ISLONG = 0x80;
        internal const int DBCOLUMNFLAGS_ISFIXEDLENGTH = 0x10;
        internal const int DBCOLUMNFLAGS_ISNULLABLE = 0x20;
        internal const int DBCOLUMNFLAGS_ISROWSET = 0x100000;
        internal const int DBCOLUMNFLAGS_ISROW = 0x200000;
        internal const int DBCOLUMNFLAGS_ISROWSET_DBCOLUMNFLAGS_ISROW = /*DBCOLUMNFLAGS_ISROWSET*/0x100000 | /*DBCOLUMNFLAGS_ISROW*/0x200000;
        internal const int DBCOLUMNFLAGS_ISLONG_DBCOLUMNFLAGS_ISSTREAM = /*DBCOLUMNFLAGS_ISLONG*/0x80 | /*DBCOLUMNFLAGS_ISSTREAM*/0x80000;
        internal const int DBCOLUMNFLAGS_ISROWID_DBCOLUMNFLAGS_ISROWVER = /*DBCOLUMNFLAGS_ISROWID*/0x100 | /*DBCOLUMNFLAGS_ISROWVER*/0x200;
        internal const int DBCOLUMNFLAGS_WRITE_DBCOLUMNFLAGS_WRITEUNKNOWN = /*DBCOLUMNFLAGS_WRITE*/0x4 | /*DBCOLUMNFLAGS_WRITEUNKNOWN*/0x8;
        internal const int DBCOLUMNFLAGS_ISNULLABLE_DBCOLUMNFLAGS_MAYBENULL = /*DBCOLUMNFLAGS_ISNULLABLE*/0x20 | /*DBCOLUMNFLAGS_MAYBENULL*/0x40;

        // accessor constants
        internal const int DBACCESSOR_ROWDATA = 0x2;
        internal const int DBACCESSOR_PARAMETERDATA = 0x4;

        // commandbuilder constants
        internal const int DBPARAMTYPE_INPUT = 0x01;
        internal const int DBPARAMTYPE_INPUTOUTPUT = 0x02;
        internal const int DBPARAMTYPE_OUTPUT = 0x03;
        internal const int DBPARAMTYPE_RETURNVALUE = 0x04;

        // parameter constants
        /*internal const int DBPARAMIO_NOTPARAM = 0;
        internal const int DBPARAMIO_INPUT = 0x1;
        internal const int DBPARAMIO_OUTPUT = 0x2;*/

        /*internal const int DBPARAMFLAGS_ISINPUT = 0x1;
        internal const int DBPARAMFLAGS_ISOUTPUT = 0x2;
        internal const int DBPARAMFLAGS_ISSIGNED = 0x10;
        internal const int DBPARAMFLAGS_ISNULLABLE = 0x40;
        internal const int DBPARAMFLAGS_ISLONG = 0x80;*/

        internal const int ParameterDirectionFlag = 3;

        // values of the searchable column in the provider types schema rowset
        internal const uint DB_UNSEARCHABLE = 1;
        internal const uint DB_LIKE_ONLY = 2;
        internal const uint DB_ALL_EXCEPT_LIKE = 3;
        internal const uint DB_SEARCHABLE = 4;

        internal static readonly IntPtr DB_INVALID_HACCESSOR = ADP.PtrZero;
        internal static readonly IntPtr DB_NULL_HCHAPTER = ADP.PtrZero;
        internal static readonly IntPtr DB_NULL_HROW = ADP.PtrZero;

        internal static readonly bool IsRunningOnX86 = RuntimeInformation.ProcessArchitecture == Architecture.X86;

        /*internal static readonly int SizeOf_tagDBPARAMINFO = Marshal.SizeOf(typeof(tagDBPARAMINFO));*/
        internal static readonly int SizeOf_tagDBBINDING = Marshal.SizeOf(typeof(tagDBBINDING));
        internal static readonly int SizeOf_tagDBCOLUMNINFO = Marshal.SizeOf(typeof(tagDBCOLUMNINFO));
        internal static readonly int SizeOf_tagDBLITERALINFO = Marshal.SizeOf(typeof(tagDBLITERALINFO));
        internal static readonly int SizeOf_tagDBPROPSET = Marshal.SizeOf(typeof(tagDBPROPSET));
        internal static readonly int SizeOf_tagDBPROP = IsRunningOnX86 ? Marshal.SizeOf(typeof(tagDBPROP_x86)) : Marshal.SizeOf(typeof(tagDBPROP));
        internal static readonly int SizeOf_tagDBPROPINFOSET = Marshal.SizeOf(typeof(tagDBPROPINFOSET));
        internal static readonly int SizeOf_tagDBPROPINFO = IsRunningOnX86 ? Marshal.SizeOf(typeof(tagDBPROPINFO_x86)) : Marshal.SizeOf(typeof(tagDBPROPINFO));
        internal static readonly int SizeOf_tagDBPROPIDSET = Marshal.SizeOf(typeof(tagDBPROPIDSET));
        internal static readonly int SizeOf_Guid = Marshal.SizeOf(typeof(Guid));
        internal static readonly int SizeOf_Variant = 8 + (2 * ADP.PtrSize); // 16 on 32bit, 24 on 64bit

        internal static readonly int OffsetOf_tagDBPROP_Status = IsRunningOnX86 ? Marshal.OffsetOf(typeof(tagDBPROP_x86), "dwStatus").ToInt32() : Marshal.OffsetOf(typeof(tagDBPROP), "dwStatus").ToInt32();
        internal static readonly int OffsetOf_tagDBPROP_Value = IsRunningOnX86 ? Marshal.OffsetOf(typeof(tagDBPROP_x86), "vValue").ToInt32() : Marshal.OffsetOf(typeof(tagDBPROP), "vValue").ToInt32();
        internal static readonly int OffsetOf_tagDBPROPSET_Properties = Marshal.OffsetOf(typeof(tagDBPROPSET), "rgProperties").ToInt32();
        internal static readonly int OffsetOf_tagDBPROPINFO_Value = IsRunningOnX86 ? Marshal.OffsetOf(typeof(tagDBPROPINFO_x86), "vValue").ToInt32() : Marshal.OffsetOf(typeof(tagDBPROPINFO), "vValue").ToInt32();
        internal static readonly int OffsetOf_tagDBPROPIDSET_PropertySet = Marshal.OffsetOf(typeof(tagDBPROPIDSET), "guidPropertySet").ToInt32();
        internal static readonly int OffsetOf_tagDBLITERALINFO_it = Marshal.OffsetOf(typeof(tagDBLITERALINFO), "it").ToInt32();
        internal static readonly int OffsetOf_tagDBBINDING_obValue = Marshal.OffsetOf(typeof(tagDBBINDING), "obValue").ToInt32();
        internal static readonly int OffsetOf_tagDBBINDING_wType = Marshal.OffsetOf(typeof(tagDBBINDING), "wType").ToInt32();

        internal static Guid IID_NULL = Guid.Empty;
        internal static Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        internal static Guid IID_IDBInitialize = new Guid(0x0C733A8B, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);
        internal static Guid IID_IDBCreateSession = new Guid(0x0C733A5D, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);
        internal static Guid IID_IDBCreateCommand = new Guid(0x0C733A1D, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);
        internal static Guid IID_ICommandText = new Guid(0x0C733A27, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);
        internal static Guid IID_IMultipleResults = new Guid(0x0C733A90, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);
        internal static Guid IID_IRow = new Guid(0x0C733AB4, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);
        internal static Guid IID_IRowset = new Guid(0x0C733A7C, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);
        internal static Guid IID_ISQLErrorInfo = new Guid(0x0C733A74, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);

        internal static Guid CLSID_DataLinks = new Guid(0x2206CDB2, 0x19C1, 0x11D1, 0x89, 0xE0, 0x00, 0xC0, 0x4F, 0xD7, 0xA8, 0x29);

        internal static Guid DBGUID_DEFAULT = new Guid(0xc8b521fb, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        internal static Guid DBGUID_ROWSET = new Guid(0xc8b522f6, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        internal static Guid DBGUID_ROW = new Guid(0xc8b522f7, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        internal static Guid DBGUID_ROWDEFAULTSTREAM = new Guid(0x0C733AB7, 0x2A1C, 0x11CE, 0xAD, 0xE5, 0x00, 0xAA, 0x00, 0x44, 0x77, 0x3D);

        internal static readonly Guid CLSID_MSDASQL = new Guid(0xc8b522cb, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        internal static readonly object DBCOL_SPECIALCOL = new Guid(0xc8b52232, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        internal static readonly char[] ErrorTrimCharacters = new char[] { '\r', '\n', '\0' };

        // used by ConnectionString hashtable, must be all lowercase
        internal const string Asynchronous_Processing = "asynchronous processing";
        internal const string AttachDBFileName = "attachdbfilename";
        internal const string Connect_Timeout = "connect timeout";
        internal const string Data_Source = "data source";
        internal const string File_Name = "file name";
        internal const string Initial_Catalog = "initial catalog";
        internal const string Password = "password";
        internal const string Persist_Security_Info = "persist security info";
        internal const string Provider = "provider";
        internal const string Pwd = "pwd";
        internal const string User_ID = "user id";

        // used by OleDbConnection as property names
        internal const string Current_Catalog = "current catalog";
        internal const string DBMS_Version = "dbms version";
        internal const string Properties = "Properties";

        // used by OleDbConnection to create and verify OLE DB Services
        internal const string DataLinks_CLSID = "CLSID\\{2206CDB2-19C1-11D1-89E0-00C04FD7A829}\\InprocServer32";
        internal const string OLEDB_SERVICES = "OLEDB_SERVICES";

        // used by OleDbConnection to eliminate post-open detection of 'Microsoft OLE DB Provider for ODBC Drivers'
        internal const string DefaultDescription_MSDASQL = "microsoft ole db provider for odbc drivers";
        internal const string MSDASQL = "msdasql";
        internal const string MSDASQLdot = "msdasql.";

        // used by OleDbPermission
        internal const string _Add = "add";
        internal const string _Keyword = "keyword";
        internal const string _Name = "name";
        internal const string _Value = "value";

        // IColumnsRowset column names
        internal const string DBCOLUMN_BASECATALOGNAME = "DBCOLUMN_BASECATALOGNAME";
        internal const string DBCOLUMN_BASECOLUMNNAME = "DBCOLUMN_BASECOLUMNNAME";
        internal const string DBCOLUMN_BASESCHEMANAME = "DBCOLUMN_BASESCHEMANAME";
        internal const string DBCOLUMN_BASETABLENAME = "DBCOLUMN_BASETABLENAME";
        internal const string DBCOLUMN_COLUMNSIZE = "DBCOLUMN_COLUMNSIZE";
        internal const string DBCOLUMN_FLAGS = "DBCOLUMN_FLAGS";
        internal const string DBCOLUMN_GUID = "DBCOLUMN_GUID";
        internal const string DBCOLUMN_IDNAME = "DBCOLUMN_IDNAME";
        internal const string DBCOLUMN_ISAUTOINCREMENT = "DBCOLUMN_ISAUTOINCREMENT";
        internal const string DBCOLUMN_ISUNIQUE = "DBCOLUMN_ISUNIQUE";
        internal const string DBCOLUMN_KEYCOLUMN = "DBCOLUMN_KEYCOLUMN";
        internal const string DBCOLUMN_NAME = "DBCOLUMN_NAME";
        internal const string DBCOLUMN_NUMBER = "DBCOLUMN_NUMBER";
        internal const string DBCOLUMN_PRECISION = "DBCOLUMN_PRECISION";
        internal const string DBCOLUMN_PROPID = "DBCOLUMN_PROPID";
        internal const string DBCOLUMN_SCALE = "DBCOLUMN_SCALE";
        internal const string DBCOLUMN_TYPE = "DBCOLUMN_TYPE";
        internal const string DBCOLUMN_TYPEINFO = "DBCOLUMN_TYPEINFO";

        // ISchemaRowset.GetRowset(OleDbSchemaGuid.Indexes) column names
        internal const string PRIMARY_KEY = "PRIMARY_KEY";
        internal const string UNIQUE = "UNIQUE";
        internal const string COLUMN_NAME = "COLUMN_NAME";
        internal const string NULLS = "NULLS";
        internal const string INDEX_NAME = "INDEX_NAME";

        // ISchemaRowset.GetSchemaRowset(OleDbSchemaGuid.Procedure_Parameters) column names
        internal const string PARAMETER_NAME = "PARAMETER_NAME";
        internal const string ORDINAL_POSITION = "ORDINAL_POSITION";
        internal const string PARAMETER_TYPE = "PARAMETER_TYPE";
        internal const string IS_NULLABLE = "IS_NULLABLE";
        internal const string DATA_TYPE = "DATA_TYPE";
        internal const string CHARACTER_MAXIMUM_LENGTH = "CHARACTER_MAXIMUM_LENGTH";
        internal const string NUMERIC_PRECISION = "NUMERIC_PRECISION";
        internal const string NUMERIC_SCALE = "NUMERIC_SCALE";
        internal const string TYPE_NAME = "TYPE_NAME";

        // DataTable.Select to sort on ordinal position for OleDbSchemaGuid.Procedure_Parameters
        internal const string ORDINAL_POSITION_ASC = "ORDINAL_POSITION ASC";

        // OleDbConnection.GetOleDbSchemmaTable(OleDbSchemaGuid.SchemaGuids) table and column names
        internal const string SchemaGuids = "SchemaGuids";
        internal const string Schema = "Schema";
        internal const string RestrictionSupport = "RestrictionSupport";

        // OleDbConnection.GetOleDbSchemmaTable(OleDbSchemaGuid.DbInfoKeywords) table and column names
        internal const string DbInfoKeywords = "DbInfoKeywords";
        internal const string Keyword = "Keyword";

        // Debug error string writeline
        internal static string ELookup(OleDbHResult hr)
        {
            string result = hr.ToString();
            if (char.IsDigit(result[0]))
            {
                result = "";
            }

            return $"{result}(0x{(int)hr:X8})";
        }

#if DEBUG
        private static readonly Hashtable g_wlookpup = new Hashtable();
        internal static string WLookup(short id)
        {
            string? value = (string?)g_wlookpup[id];
            if (null == value)
            {
                g_wlookpup[id] = value = $"0x{(short)id:X2} {(short)id} {(DBTypeEnum)id}";
            }
            return value;
        }

        private enum DBTypeEnum
        {
            EMPTY = 0,       //
            NULL = 1,       //
            I2 = 2,       //
            I4 = 3,       //
            R4 = 4,       //
            R8 = 5,       //
            CY = 6,       //
            DATE = 7,       //
            BSTR = 8,       //
            IDISPATCH = 9,       //
            ERROR = 10,      //
            BOOL = 11,      //
            VARIANT = 12,      //
            IUNKNOWN = 13,      //
            DECIMAL = 14,      //
            I1 = 16,      //
            UI1 = 17,      //
            UI2 = 18,      //
            UI4 = 19,      //
            I8 = 20,      //
            UI8 = 21,      //
            FILETIME = 64,      // 2.0
            GUID = 72,      //
            BYTES = 128,     //
            STR = 129,     //
            WSTR = 130,     //
            NUMERIC = 131,     // with potential overflow
            UDT = 132,     // should never be encountered
            DBDATE = 133,     //
            DBTIME = 134,     //
            DBTIMESTAMP = 135,     // granularity reduced from 1ns to 100ns (sql is 3.33 milli seconds)
            HCHAPTER = 136,     // 1.5
            PROPVARIANT = 138,     // 2.0 - as variant
            VARNUMERIC = 139,     // 2.0 - as string else ConversionException

            BYREF_I2 = 0x4002,
            BYREF_I4 = 0x4003,
            BYREF_R4 = 0x4004,
            BYREF_R8 = 0x4005,
            BYREF_CY = 0x4006,
            BYREF_DATE = 0x4007,
            BYREF_BSTR = 0x4008,
            BYREF_IDISPATCH = 0x4009,
            BYREF_ERROR = 0x400a,
            BYREF_BOOL = 0x400b,
            BYREF_VARIANT = 0x400c,
            BYREF_IUNKNOWN = 0x400d,
            BYREF_DECIMAL = 0x400e,
            BYREF_I1 = 0x4010,
            BYREF_UI1 = 0x4011,
            BYREF_UI2 = 0x4012,
            BYREF_UI4 = 0x4013,
            BYREF_I8 = 0x4014,
            BYREF_UI8 = 0x4015,
            BYREF_FILETIME = 0x4040,
            BYREF_GUID = 0x4048,
            BYREF_BYTES = 0x4080,
            BYREF_STR = 0x4081,
            BYREF_WSTR = 0x4082,
            BYREF_NUMERIC = 0x4083,
            BYREF_UDT = 0x4084,
            BYREF_DBDATE = 0x4085,
            BYREF_DBTIME = 0x4086,
            BYREF_DBTIMESTAMP = 0x4087,
            BYREF_HCHAPTER = 0x4088,
            BYREF_PROPVARIANT = 0x408a,
            BYREF_VARNUMERIC = 0x408b,

            VECTOR = 0x1000,
            ARRAY = 0x2000,
            BYREF = 0x4000,  //
            RESERVED = 0x8000,  // SystemException
        }
#endif
    }
}
