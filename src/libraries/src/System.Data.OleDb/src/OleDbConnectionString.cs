// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Runtime.Versioning;

namespace System.Data.OleDb
{
    internal struct SchemaSupport
    {
        internal Guid _schemaRowset;
        internal int _restrictions;
    }

    internal sealed class OleDbConnectionString : DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

        internal static class KEY
        {
            internal const string Asynchronous_Processing = "asynchronous processing";
            internal const string Connect_Timeout = "connect timeout";
            internal const string Data_Provider = "data provider";
            internal const string Data_Source = "data source";
            internal const string Extended_Properties = "extended properties";
            internal const string File_Name = "file name";
            internal const string Initial_Catalog = "initial catalog";
            internal const string Ole_DB_Services = "ole db services";
            internal const string Persist_Security_Info = "persist security info";
            internal const string Prompt = "prompt";
            internal const string Provider = "provider";
            internal const string RemoteProvider = "remote provider";
            internal const string WindowHandle = "window handle";
        }

        // registry key and dword value entry for udl pooling
        private static class UDL
        {
            internal const string Header = "\xfeff[oledb]\r\n; Everything after this line is an OLE DB initstring\r\n";
            internal const string Location = "SOFTWARE\\Microsoft\\DataAccess\\Udl Pooling";
            internal const string Pooling = "Cache Size";

            static internal volatile bool _PoolSizeInit;
            static internal int _PoolSize;

            static internal volatile Dictionary<string, string> _Pool;
            static internal object _PoolLock = new object();
        }

        private static class VALUES
        {
            internal const string NoPrompt = "noprompt";
        }

        // set during ctor
        internal readonly bool PossiblePrompt;
        internal readonly string ActualConnectionString; // cached value passed to GetDataSource

        private readonly string _expandedConnectionString;

        internal SchemaSupport[] _schemaSupport;

        internal int _sqlSupport;
        internal bool _supportMultipleResults;
        internal bool _supportIRow;
        internal bool _hasSqlSupport;
        internal bool _hasSupportMultipleResults, _hasSupportIRow;

        private int _oledbServices;

        // these are cached delegates (per unique connectionstring)
        internal UnsafeNativeMethods.IUnknownQueryInterface DangerousDataSourceIUnknownQueryInterface;
        internal UnsafeNativeMethods.IDBInitializeInitialize DangerousIDBInitializeInitialize;
        internal UnsafeNativeMethods.IDBCreateSessionCreateSession DangerousIDBCreateSessionCreateSession;
        internal UnsafeNativeMethods.IDBCreateCommandCreateCommand DangerousIDBCreateCommandCreateCommand;

        // since IDBCreateCommand interface may not be supported for a particular provider (only IOpenRowset)
        // we cache that fact rather than call QueryInterface on every call to Open
        internal bool HaveQueriedForCreateCommand;

        // SxS: if user specifies a value for "File Name=" (UDL) in connection string, OleDbConnectionString will load the connection string
        // from the UDL file. The UDL file is opened as FileMode.Open, FileAccess.Read, FileShare.Read, allowing concurrent access to it.
        internal OleDbConnectionString(string connectionString, bool validate) : base(connectionString)
        {
            string prompt = this[KEY.Prompt];
            PossiblePrompt = ((!ADP.IsEmpty(prompt) && (0 != String.Compare(prompt, VALUES.NoPrompt, StringComparison.OrdinalIgnoreCase)))
                              || !ADP.IsEmpty(this[KEY.WindowHandle]));

            if (!IsEmpty)
            {
                string udlConnectionString = null;
                if (!validate)
                {
                    int position = 0;
                    string udlFileName = null;
                    _expandedConnectionString = ExpandDataDirectories(ref udlFileName, ref position);

                    if (!ADP.IsEmpty(udlFileName))
                    { // fail via new FileStream vs. GetFullPath
                        udlFileName = ADP.GetFullPath(udlFileName);
                    }
                    if (null != udlFileName)
                    {
                        udlConnectionString = LoadStringFromStorage(udlFileName);

                        if (!ADP.IsEmpty(udlConnectionString))
                        {
                            _expandedConnectionString = _expandedConnectionString.Substring(0, position) + udlConnectionString + ';' + _expandedConnectionString.Substring(position);
                        }
                    }
                }
                if (validate || ADP.IsEmpty(udlConnectionString))
                {
                    ActualConnectionString = ValidateConnectionString(connectionString);
                }
            }
        }

        internal int ConnectTimeout
        {
            get { return base.ConvertValueToInt32(KEY.Connect_Timeout, ADP.DefaultConnectionTimeout); }
        }

        internal string DataSource
        {
            get { return base.ConvertValueToString(KEY.Data_Source, string.Empty); }
        }

        internal string InitialCatalog
        {
            get { return base.ConvertValueToString(KEY.Initial_Catalog, string.Empty); }
        }

        internal string Provider
        {
            get
            {
                Debug.Assert(!ADP.IsEmpty(this[KEY.Provider]), "no Provider");
                return this[KEY.Provider];
            }
        }

        internal int OleDbServices
        {
            get
            {
                return _oledbServices;
            }
        }

        internal SchemaSupport[] SchemaSupport
        { // OleDbConnection.GetSchemaRowsetInformation
            get { return _schemaSupport; }
            set { _schemaSupport = value; }
        }

        protected internal override string Expand()
        {
            if (null != _expandedConnectionString)
            {
                return _expandedConnectionString;
            }
            else
            {
                return base.Expand();
            }
        }

        internal int GetSqlSupport(OleDbConnection connection)
        {
            int sqlSupport = _sqlSupport;
            if (!_hasSqlSupport)
            {
                object value = connection.GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_SQLSUPPORT);
                if (value is Int32)
                { // not OleDbPropertyStatus
                    sqlSupport = (int)value;
                }
                _sqlSupport = sqlSupport;
                _hasSqlSupport = true;
            }
            return sqlSupport;
        }

        internal bool GetSupportIRow(OleDbConnection connection, OleDbCommand command)
        {
            bool supportIRow = _supportIRow;
            if (!_hasSupportIRow)
            {
                object value = command.GetPropertyValue(OleDbPropertySetGuid.Rowset, ODB.DBPROP_IRow);

                // SQLOLEDB always returns VARIANT_FALSE for DBPROP_IROW, so base the answer on existance
                supportIRow = !(value is OleDbPropertyStatus);
                _supportIRow = supportIRow;
                _hasSupportIRow = true;
            }
            return supportIRow;
        }

        internal bool GetSupportMultipleResults(OleDbConnection connection)
        {
            bool supportMultipleResults = _supportMultipleResults;
            if (!_hasSupportMultipleResults)
            {
                object value = connection.GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_MULTIPLERESULTS);
                if (value is Int32)
                {// not OleDbPropertyStatus
                    supportMultipleResults = (ODB.DBPROPVAL_MR_NOTSUPPORTED != (int)value);
                }
                _supportMultipleResults = supportMultipleResults;
                _hasSupportMultipleResults = true;
            }
            return supportMultipleResults;
        }

        static private int UdlPoolSize
        {
            // SxS: UdpPoolSize reads registry value to get the pool size
            get
            {
                int poolsize = UDL._PoolSize;
                if (!UDL._PoolSizeInit)
                {
                    object value = ADP.LocalMachineRegistryValue(UDL.Location, UDL.Pooling);
                    if (value is Int32)
                    {
                        poolsize = (int)value;
                        poolsize = ((0 < poolsize) ? poolsize : 0);
                        UDL._PoolSize = poolsize;
                    }
                    UDL._PoolSizeInit = true;
                }
                return poolsize;
            }
        }

        static private string LoadStringFromStorage(string udlfilename)
        {
            string udlConnectionString = null;
            Dictionary<string, string> udlcache = UDL._Pool;

            if ((null == udlcache) || !udlcache.TryGetValue(udlfilename, out udlConnectionString))
            {
                udlConnectionString = LoadStringFromFileStorage(udlfilename);
                if (null != udlConnectionString)
                {
                    Debug.Assert(!ADP.IsEmpty(udlfilename), "empty filename didn't fail");

                    if (0 < UdlPoolSize)
                    {
                        Debug.Assert(udlfilename == ADP.GetFullPath(udlfilename), "only cache full path filenames");

                        if (null == udlcache)
                        {
                            udlcache = new Dictionary<string, string>();
                            udlcache[udlfilename] = udlConnectionString;

                            lock (UDL._PoolLock)
                            {
                                if (null != UDL._Pool)
                                {
                                    udlcache = UDL._Pool;
                                }
                                else
                                {
                                    UDL._Pool = udlcache;
                                    udlcache = null;
                                }
                            }
                        }
                        if (null != udlcache)
                        {
                            lock (udlcache)
                            {
                                udlcache[udlfilename] = udlConnectionString;
                            }
                        }
                    }
                }
            }
            return udlConnectionString;
        }

        static private string LoadStringFromFileStorage(string udlfilename)
        {
            // Microsoft Data Link File Format
            // The first two lines of a .udl file must have exactly the following contents in order to work properly:
            //  [oledb]
            //  ; Everything after this line is an OLE DB initstring
            //
            string connectionString = null;
            Exception failure = null;
            try
            {
                int hdrlength = ADP.CharSize * UDL.Header.Length;
                using (FileStream fstream = new FileStream(udlfilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long length = fstream.Length;
                    if (length < hdrlength || (0 != length % ADP.CharSize))
                    {
                        failure = ADP.InvalidUDL();
                    }
                    else
                    {
                        byte[] bytes = new Byte[hdrlength];
                        int count = fstream.Read(bytes, 0, bytes.Length);
                        if (count < hdrlength)
                        {
                            failure = ADP.InvalidUDL();
                        }
                        else if (System.Text.Encoding.Unicode.GetString(bytes, 0, hdrlength) != UDL.Header)
                        {
                            failure = ADP.InvalidUDL();
                        }
                        else
                        { // please verify header before allocating memory block for connection string
                            bytes = new Byte[length - hdrlength];
                            count = fstream.Read(bytes, 0, bytes.Length);
                            connectionString = System.Text.Encoding.Unicode.GetString(bytes, 0, count);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // UNDONE - should not be catching all exceptions!!!
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                throw ADP.UdlFileError(e);
            }
            if (null != failure)
            {
                throw failure;
            }
            return connectionString.Trim();
        }

        private string ValidateConnectionString(string connectionString)
        {
            if (ConvertValueToBoolean(KEY.Asynchronous_Processing, false))
            {
                throw ODB.AsynchronousNotSupported();
            }

            int connectTimeout = ConvertValueToInt32(KEY.Connect_Timeout, 0);
            if (connectTimeout < 0)
            {
                throw ADP.InvalidConnectTimeoutValue();
            }

            string progid = ConvertValueToString(KEY.Data_Provider, null);
            if (null != progid)
            {
                progid = progid.Trim();
                if (0 < progid.Length)
                { // don't fail on empty 'Data Provider' value
                    ValidateProvider(progid);
                }
            }
            progid = ConvertValueToString(KEY.RemoteProvider, null);
            if (null != progid)
            {
                progid = progid.Trim();
                if (0 < progid.Length)
                { // don't fail on empty 'Data Provider' value
                    ValidateProvider(progid);
                }
            }
            progid = ConvertValueToString(KEY.Provider, string.Empty).Trim();
            ValidateProvider(progid); // will fail on empty 'Provider' value

            // initialize to default
            // If the value is not provided in connection string and OleDbServices registry key has not been set by the provider,
            // the default for the provider is -1 (all services are ON).
            // our default is -13, we turn off ODB.DBPROPVAL_OS_AGR_AFTERSESSION and ODB.DBPROPVAL_OS_CLIENTCURSOR flags
            _oledbServices = DbConnectionStringDefaults.OleDbServices;

            bool hasOleDBServices = (base.ContainsKey(KEY.Ole_DB_Services) && !ADP.IsEmpty((string)base[KEY.Ole_DB_Services]));
            if (!hasOleDBServices)
            { // don't touch registry if they have OLE DB Services
                string classid = (string)ADP.ClassesRootRegistryValue(progid + "\\CLSID", String.Empty);
                if ((null != classid) && (0 < classid.Length))
                {
                    // CLSID detection of 'Microsoft OLE DB Provider for ODBC Drivers'
                    Guid classidProvider = new Guid(classid);
                    if (ODB.CLSID_MSDASQL == classidProvider)
                    {
                        throw ODB.MSDASQLNotSupported();
                    }
                    object tmp = ADP.ClassesRootRegistryValue("CLSID\\{" + classidProvider.ToString("D", CultureInfo.InvariantCulture) + "}", ODB.OLEDB_SERVICES);
                    if (null != tmp)
                    {
                        // @devnote: some providers like MSDataShape don't have the OLEDB_SERVICES value
                        // the MSDataShape provider doesn't support the 'Ole Db Services' keyword
                        // hence, if the value doesn't exist - don't prepend to string
                        try
                        {
                            _oledbServices = (int)tmp;
                        }
                        catch (InvalidCastException e)
                        {
                            ADP.TraceExceptionWithoutRethrow(e);
                        }
                        _oledbServices &= ~(ODB.DBPROPVAL_OS_AGR_AFTERSESSION | ODB.DBPROPVAL_OS_CLIENTCURSOR);

                        StringBuilder builder = new StringBuilder();
                        builder.Append(KEY.Ole_DB_Services);
                        builder.Append("=");
                        builder.Append(_oledbServices.ToString(CultureInfo.InvariantCulture));
                        builder.Append(";");
                        builder.Append(connectionString);
                        connectionString = builder.ToString();
                    }
                }
            }
            else
            {
                // parse the Ole Db Services value from connection string
                _oledbServices = ConvertValueToInt32(KEY.Ole_DB_Services, DbConnectionStringDefaults.OleDbServices);
            }

            return connectionString;
        }

        internal static bool IsMSDASQL(string progid)
        {
            return (("msdasql" == progid) || progid.StartsWith("msdasql.", StringComparison.Ordinal) || ("microsoft ole db provider for odbc drivers" == progid));
        }

        static private void ValidateProvider(string progid)
        {
            if (ADP.IsEmpty(progid))
            {
                throw ODB.NoProviderSpecified();
            }
            if (ODB.MaxProgIdLength <= progid.Length)
            {
                throw ODB.InvalidProviderSpecified();
            }
            progid = progid.ToLower(CultureInfo.InvariantCulture);
            if (IsMSDASQL(progid))
            {
                // fail msdasql even if not on the machine.
                throw ODB.MSDASQLNotSupported();
            }
        }

        static internal void ReleaseObjectPool()
        {
            UDL._PoolSizeInit = false;
            UDL._Pool = null;
        }
    }
}

