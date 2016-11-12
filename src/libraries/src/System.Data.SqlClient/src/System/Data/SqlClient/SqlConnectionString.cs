// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;


namespace System.Data.SqlClient
{
    internal sealed class SqlConnectionString : DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

        internal static class DEFAULT
        {
            internal const ApplicationIntent ApplicationIntent = DbConnectionStringDefaults.ApplicationIntent;
            internal const string Application_Name = TdsEnums.SQL_PROVIDER_NAME;
            internal const string AttachDBFilename = "";
            internal const int Connect_Timeout = ADP.DefaultConnectionTimeout;
            internal const string Current_Language = "";
            internal const string Data_Source = "";
            internal const bool Encrypt = false;
            internal const string FailoverPartner = "";
            internal const string Initial_Catalog = "";
            internal const bool Integrated_Security = false;
            internal const int Load_Balance_Timeout = 0; // default of 0 means don't use
            internal const bool MARS = false;
            internal const int Max_Pool_Size = 100;
            internal const int Min_Pool_Size = 0;
            internal const bool MultiSubnetFailover = DbConnectionStringDefaults.MultiSubnetFailover;
            internal const int Packet_Size = 8000;
            internal const string Password = "";
            internal const bool Persist_Security_Info = false;
            internal const bool Pooling = true;
            internal const bool TrustServerCertificate = false;
            internal const string Type_System_Version = "";
            internal const string User_ID = "";
            internal const bool User_Instance = false;
            internal const bool Replication = false;
            internal const int Connect_Retry_Count = 1;
            internal const int Connect_Retry_Interval = 10;
        }

        // SqlConnection ConnectionString Options
        // keys must be lowercase!
        internal static class KEY
        {
            internal const string ApplicationIntent = "applicationintent";
            internal const string Application_Name = "application name";
            internal const string AsynchronousProcessing = "asynchronous processing";
            internal const string AttachDBFilename = "attachdbfilename";
            internal const string Connect_Timeout = "connect timeout";
            internal const string Connection_Reset = "connection reset";
            internal const string Context_Connection = "context connection";
            internal const string Current_Language = "current language";
            internal const string Data_Source = "data source";
            internal const string Encrypt = "encrypt";
            internal const string Enlist = "enlist";
            internal const string FailoverPartner = "failover partner";
            internal const string Initial_Catalog = "initial catalog";
            internal const string Integrated_Security = "integrated security";
            internal const string Load_Balance_Timeout = "load balance timeout";
            internal const string MARS = "multipleactiveresultsets";
            internal const string Max_Pool_Size = "max pool size";
            internal const string Min_Pool_Size = "min pool size";
            internal const string MultiSubnetFailover = "multisubnetfailover";
            internal const string Network_Library = "network library";
            internal const string Packet_Size = "packet size";
            internal const string Password = "password";
            internal const string Persist_Security_Info = "persist security info";
            internal const string Pooling = "pooling";
            internal const string TransactionBinding = "transaction binding";
            internal const string TrustServerCertificate = "trustservercertificate";
            internal const string Type_System_Version = "type system version";
            internal const string User_ID = "user id";
            internal const string User_Instance = "user instance";
            internal const string Workstation_Id = "workstation id";
            internal const string Replication = "replication";
            internal const string Connect_Retry_Count = "connectretrycount";
            internal const string Connect_Retry_Interval = "connectretryinterval";
        }

        // Constant for the number of duplicate options in the connection string

        private static class SYNONYM
        {
            // application name
            internal const string APP = "app";
            internal const string Async = "async";
            // attachDBFilename
            internal const string EXTENDED_PROPERTIES = "extended properties";
            internal const string INITIAL_FILE_NAME = "initial file name";
            // connect timeout
            internal const string CONNECTION_TIMEOUT = "connection timeout";
            internal const string TIMEOUT = "timeout";
            // current language
            internal const string LANGUAGE = "language";
            // data source
            internal const string ADDR = "addr";
            internal const string ADDRESS = "address";
            internal const string SERVER = "server";
            internal const string NETWORK_ADDRESS = "network address";
            // initial catalog
            internal const string DATABASE = "database";
            // integrated security
            internal const string TRUSTED_CONNECTION = "trusted_connection";
            // load balance timeout
            internal const string Connection_Lifetime = "connection lifetime";
            // network library
            internal const string NET = "net";
            internal const string NETWORK = "network";
            // password
            internal const string Pwd = "pwd";
            // persist security info
            internal const string PERSISTSECURITYINFO = "persistsecurityinfo";
            // user id
            internal const string UID = "uid";
            internal const string User = "user";
            // workstation id
            internal const string WSID = "wsid";
            // make sure to update SynonymCount value below when adding or removing synonyms
        }

        internal const int SynonymCount = 18;
        internal const int DeprecatedSynonymCount = 3;


        internal enum TypeSystem
        {
            Latest = 2008,
            SQLServer2000 = 2000,
            SQLServer2005 = 2005,
            SQLServer2008 = 2008,
            SQLServer2012 = 2012,
        }

        internal static class TYPESYSTEMVERSION
        {
            internal const string Latest = "Latest";
            internal const string SQL_Server_2000 = "SQL Server 2000";
            internal const string SQL_Server_2005 = "SQL Server 2005";
            internal const string SQL_Server_2008 = "SQL Server 2008";
            internal const string SQL_Server_2012 = "SQL Server 2012";
        }


        static private Dictionary<string, string> s_sqlClientSynonyms;

        private readonly bool _integratedSecurity;

        private readonly bool _encrypt;
        private readonly bool _trustServerCertificate;
        private readonly bool _mars;
        private readonly bool _persistSecurityInfo;
        private readonly bool _pooling;
        private readonly bool _replication;
        private readonly bool _userInstance;
        private readonly bool _multiSubnetFailover;

        private readonly int _connectTimeout;
        private readonly int _loadBalanceTimeout;
        private readonly int _maxPoolSize;
        private readonly int _minPoolSize;
        private readonly int _packetSize;
        private readonly int _connectRetryCount;
        private readonly int _connectRetryInterval;

        private readonly ApplicationIntent _applicationIntent;
        private readonly string _applicationName;
        private readonly string _attachDBFileName;
        private readonly string _currentLanguage;
        private readonly string _dataSource;
        private readonly string _localDBInstance; // created based on datasource, set to NULL if datasource is not LocalDB 
        private readonly string _failoverPartner;
        private readonly string _initialCatalog;
        private readonly string _password;
        private readonly string _userID;

        private readonly string _workstationId;

        private readonly TypeSystem _typeSystemVersion;
        internal SqlConnectionString(string connectionString) : base(connectionString, GetParseSynonyms())
        {
            ThrowUnsupportedIfKeywordSet(KEY.AsynchronousProcessing);
            ThrowUnsupportedIfKeywordSet(KEY.Connection_Reset);
            ThrowUnsupportedIfKeywordSet(KEY.Context_Connection);
            ThrowUnsupportedIfKeywordSet(KEY.Enlist);
            ThrowUnsupportedIfKeywordSet(KEY.TransactionBinding);

            // Network Library has its own special error message
            if (ContainsKey(KEY.Network_Library))
            {
                throw SQL.NetworkLibraryKeywordNotSupported();
            }

            _integratedSecurity = ConvertValueToIntegratedSecurity();
#if MANAGED_SNI
            if(_integratedSecurity)
            {
                throw SQL.UnsupportedKeyword(KEY.Integrated_Security);
            }
#endif
            _encrypt = ConvertValueToBoolean(KEY.Encrypt, DEFAULT.Encrypt);
            _mars = ConvertValueToBoolean(KEY.MARS, DEFAULT.MARS);
            _persistSecurityInfo = ConvertValueToBoolean(KEY.Persist_Security_Info, DEFAULT.Persist_Security_Info);
            _pooling = ConvertValueToBoolean(KEY.Pooling, DEFAULT.Pooling);
            _replication = ConvertValueToBoolean(KEY.Replication, DEFAULT.Replication);
            _userInstance = ConvertValueToBoolean(KEY.User_Instance, DEFAULT.User_Instance);
            _multiSubnetFailover = ConvertValueToBoolean(KEY.MultiSubnetFailover, DEFAULT.MultiSubnetFailover);

            _connectTimeout = ConvertValueToInt32(KEY.Connect_Timeout, DEFAULT.Connect_Timeout);
            _loadBalanceTimeout = ConvertValueToInt32(KEY.Load_Balance_Timeout, DEFAULT.Load_Balance_Timeout);
            _maxPoolSize = ConvertValueToInt32(KEY.Max_Pool_Size, DEFAULT.Max_Pool_Size);
            _minPoolSize = ConvertValueToInt32(KEY.Min_Pool_Size, DEFAULT.Min_Pool_Size);
            _packetSize = ConvertValueToInt32(KEY.Packet_Size, DEFAULT.Packet_Size);
            _connectRetryCount = ConvertValueToInt32(KEY.Connect_Retry_Count, DEFAULT.Connect_Retry_Count);
            _connectRetryInterval = ConvertValueToInt32(KEY.Connect_Retry_Interval, DEFAULT.Connect_Retry_Interval);

            _applicationIntent = ConvertValueToApplicationIntent();
            _applicationName = ConvertValueToString(KEY.Application_Name, DEFAULT.Application_Name);
            _attachDBFileName = ConvertValueToString(KEY.AttachDBFilename, DEFAULT.AttachDBFilename);
            _currentLanguage = ConvertValueToString(KEY.Current_Language, DEFAULT.Current_Language);
            _dataSource = ConvertValueToString(KEY.Data_Source, DEFAULT.Data_Source);
            _localDBInstance = LocalDBAPI.GetLocalDbInstanceNameFromServerName(_dataSource);
            _failoverPartner = ConvertValueToString(KEY.FailoverPartner, DEFAULT.FailoverPartner);
            _initialCatalog = ConvertValueToString(KEY.Initial_Catalog, DEFAULT.Initial_Catalog);
            _password = ConvertValueToString(KEY.Password, DEFAULT.Password);
            _trustServerCertificate = ConvertValueToBoolean(KEY.TrustServerCertificate, DEFAULT.TrustServerCertificate);

            // Temporary string - this value is stored internally as an enum.
            string typeSystemVersionString = ConvertValueToString(KEY.Type_System_Version, null);

            _userID = ConvertValueToString(KEY.User_ID, DEFAULT.User_ID);
            _workstationId = ConvertValueToString(KEY.Workstation_Id, null);



            if (_loadBalanceTimeout < 0)
            {
                throw ADP.InvalidConnectionOptionValue(KEY.Load_Balance_Timeout);
            }

            if (_connectTimeout < 0)
            {
                throw ADP.InvalidConnectionOptionValue(KEY.Connect_Timeout);
            }

            if (_maxPoolSize < 1)
            {
                throw ADP.InvalidConnectionOptionValue(KEY.Max_Pool_Size);
            }

            if (_minPoolSize < 0)
            {
                throw ADP.InvalidConnectionOptionValue(KEY.Min_Pool_Size);
            }
            if (_maxPoolSize < _minPoolSize)
            {
                throw ADP.InvalidMinMaxPoolSizeValues();
            }

            if ((_packetSize < TdsEnums.MIN_PACKET_SIZE) || (TdsEnums.MAX_PACKET_SIZE < _packetSize))
            {
                throw SQL.InvalidPacketSizeValue();
            }


            ValidateValueLength(_applicationName, TdsEnums.MAXLEN_APPNAME, KEY.Application_Name);
            ValidateValueLength(_currentLanguage, TdsEnums.MAXLEN_LANGUAGE, KEY.Current_Language);
            ValidateValueLength(_dataSource, TdsEnums.MAXLEN_SERVERNAME, KEY.Data_Source);
            ValidateValueLength(_failoverPartner, TdsEnums.MAXLEN_SERVERNAME, KEY.FailoverPartner);
            ValidateValueLength(_initialCatalog, TdsEnums.MAXLEN_DATABASE, KEY.Initial_Catalog);
            ValidateValueLength(_password, TdsEnums.MAXLEN_PASSWORD, KEY.Password);
            ValidateValueLength(_userID, TdsEnums.MAXLEN_USERNAME, KEY.User_ID);
            if (null != _workstationId)
            {
                ValidateValueLength(_workstationId, TdsEnums.MAXLEN_HOSTNAME, KEY.Workstation_Id);
            }

            if (!String.Equals(DEFAULT.FailoverPartner, _failoverPartner, StringComparison.OrdinalIgnoreCase))
            {
                // fail-over partner is set

                if (_multiSubnetFailover)
                {
                    throw SQL.MultiSubnetFailoverWithFailoverPartner(serverProvidedFailoverPartner: false, internalConnection: null);
                }

                if (String.Equals(DEFAULT.Initial_Catalog, _initialCatalog, StringComparison.OrdinalIgnoreCase))
                {
                    throw ADP.MissingConnectionOptionValue(KEY.FailoverPartner, KEY.Initial_Catalog);
                }
            }

            if (0 <= _attachDBFileName.IndexOf('|'))
            {
                throw ADP.InvalidConnectionOptionValue(KEY.AttachDBFilename);
            }
            else
            {
                ValidateValueLength(_attachDBFileName, TdsEnums.MAXLEN_ATTACHDBFILE, KEY.AttachDBFilename);
            }


            if (true == _userInstance && !string.IsNullOrEmpty(_failoverPartner))
            {
                throw SQL.UserInstanceFailoverNotCompatible();
            }

            if (string.IsNullOrEmpty(typeSystemVersionString))
            {
                typeSystemVersionString = DbConnectionStringDefaults.TypeSystemVersion;
            }

            if (typeSystemVersionString.Equals(TYPESYSTEMVERSION.Latest, StringComparison.OrdinalIgnoreCase))
            {
                _typeSystemVersion = TypeSystem.Latest;
            }
            else if (typeSystemVersionString.Equals(TYPESYSTEMVERSION.SQL_Server_2000, StringComparison.OrdinalIgnoreCase))
            {
                _typeSystemVersion = TypeSystem.SQLServer2000;
            }
            else if (typeSystemVersionString.Equals(TYPESYSTEMVERSION.SQL_Server_2005, StringComparison.OrdinalIgnoreCase))
            {
                _typeSystemVersion = TypeSystem.SQLServer2005;
            }
            else if (typeSystemVersionString.Equals(TYPESYSTEMVERSION.SQL_Server_2008, StringComparison.OrdinalIgnoreCase))
            {
                _typeSystemVersion = TypeSystem.SQLServer2008;
            }
            else if (typeSystemVersionString.Equals(TYPESYSTEMVERSION.SQL_Server_2012, StringComparison.OrdinalIgnoreCase))
            {
                _typeSystemVersion = TypeSystem.SQLServer2012;
            }
            else
            {
                throw ADP.InvalidConnectionOptionValue(KEY.Type_System_Version);
            }


            if (_applicationIntent == ApplicationIntent.ReadOnly && !String.IsNullOrEmpty(_failoverPartner))
                throw SQL.ROR_FailoverNotSupportedConnString();

            if ((_connectRetryCount < 0) || (_connectRetryCount > 255))
            {
                throw ADP.InvalidConnectRetryCountValue();
            }

            if ((_connectRetryInterval < 1) || (_connectRetryInterval > 60))
            {
                throw ADP.InvalidConnectRetryIntervalValue();
            }
        }

        // This c-tor is used to create SSE and user instance connection strings when user instance is set to true
        // BUG (VSTFDevDiv) 479687: Using TransactionScope with Linq2SQL against user instances fails with "connection has been broken" message
        internal SqlConnectionString(SqlConnectionString connectionOptions, string dataSource, bool userInstance) : base(connectionOptions)
        {
            _integratedSecurity = connectionOptions._integratedSecurity;
            _encrypt = connectionOptions._encrypt;

            _mars = connectionOptions._mars;
            _persistSecurityInfo = connectionOptions._persistSecurityInfo;
            _pooling = connectionOptions._pooling;
            _replication = connectionOptions._replication;
            _userInstance = userInstance;
            _connectTimeout = connectionOptions._connectTimeout;
            _loadBalanceTimeout = connectionOptions._loadBalanceTimeout;
            _maxPoolSize = connectionOptions._maxPoolSize;
            _minPoolSize = connectionOptions._minPoolSize;
            _multiSubnetFailover = connectionOptions._multiSubnetFailover;
            _packetSize = connectionOptions._packetSize;
            _applicationName = connectionOptions._applicationName;
            _attachDBFileName = connectionOptions._attachDBFileName;
            _currentLanguage = connectionOptions._currentLanguage;
            _dataSource = dataSource;
            _localDBInstance = LocalDBAPI.GetLocalDbInstanceNameFromServerName(_dataSource);
            _failoverPartner = connectionOptions._failoverPartner;
            _initialCatalog = connectionOptions._initialCatalog;
            _password = connectionOptions._password;
            _userID = connectionOptions._userID;
            _workstationId = connectionOptions._workstationId;
            _typeSystemVersion = connectionOptions._typeSystemVersion;
            _applicationIntent = connectionOptions._applicationIntent;
            _connectRetryCount = connectionOptions._connectRetryCount;
            _connectRetryInterval = connectionOptions._connectRetryInterval;

            ValidateValueLength(_dataSource, TdsEnums.MAXLEN_SERVERNAME, KEY.Data_Source);
        }

        internal bool IntegratedSecurity { get { return _integratedSecurity; } }

        // We always initialize in Async mode so that both synchronous and asynchronous methods
        // will work.  In the future we can deprecate the keyword entirely.
        internal bool Asynchronous { get { return true; } }

        // SQLPT 41700: Ignore ResetConnection=False, always reset the connection for security
        internal bool ConnectionReset { get { return true; } }
        //        internal bool EnableUdtDownload { get { return _enableUdtDownload;} }
        internal bool Encrypt { get { return _encrypt; } }
        internal bool TrustServerCertificate { get { return _trustServerCertificate; } }
        internal bool MARS { get { return _mars; } }
        internal bool MultiSubnetFailover { get { return _multiSubnetFailover; } }

        internal bool PersistSecurityInfo { get { return _persistSecurityInfo; } }
        internal bool Pooling { get { return _pooling; } }
        internal bool Replication { get { return _replication; } }
        internal bool UserInstance { get { return _userInstance; } }

        internal int ConnectTimeout { get { return _connectTimeout; } }
        internal int LoadBalanceTimeout { get { return _loadBalanceTimeout; } }
        internal int MaxPoolSize { get { return _maxPoolSize; } }
        internal int MinPoolSize { get { return _minPoolSize; } }
        internal int PacketSize { get { return _packetSize; } }
        internal int ConnectRetryCount { get { return _connectRetryCount; } }
        internal int ConnectRetryInterval { get { return _connectRetryInterval; } }

        internal ApplicationIntent ApplicationIntent { get { return _applicationIntent; } }
        internal string ApplicationName { get { return _applicationName; } }
        internal string AttachDBFilename { get { return _attachDBFileName; } }
        internal string CurrentLanguage { get { return _currentLanguage; } }
        internal string DataSource { get { return _dataSource; } }
        internal string LocalDBInstance { get { return _localDBInstance; } }
        internal string FailoverPartner { get { return _failoverPartner; } }
        internal string InitialCatalog { get { return _initialCatalog; } }
        internal string Password { get { return _password; } }
        internal string UserID { get { return _userID; } }
        internal string WorkstationId { get { return _workstationId; } }

        internal TypeSystem TypeSystemVersion { get { return _typeSystemVersion; } }

        // This dictionary is meant to be read-only translation of parsed string
        // keywords/synonyms to a known keyword string.
        internal static Dictionary<string, string> GetParseSynonyms()
        {
            Dictionary<string, string> synonyms = s_sqlClientSynonyms;
            if (null == synonyms)
            {
                int count = SqlConnectionStringBuilder.KeywordsCount + SqlConnectionStringBuilder.DeprecatedKeywordsCount + SynonymCount + DeprecatedSynonymCount;
                synonyms = new Dictionary<string, string>(count)
                {
                    { KEY.ApplicationIntent, KEY.ApplicationIntent },
                    { KEY.Application_Name, KEY.Application_Name },
                    { KEY.AsynchronousProcessing, KEY.AsynchronousProcessing },
                    { KEY.AttachDBFilename, KEY.AttachDBFilename },
                    { KEY.Connect_Timeout, KEY.Connect_Timeout },
                    { KEY.Connection_Reset, KEY.Connection_Reset },
                    { KEY.Context_Connection, KEY.Context_Connection },
                    { KEY.Current_Language, KEY.Current_Language },
                    { KEY.Data_Source, KEY.Data_Source },
                    { KEY.Encrypt, KEY.Encrypt },
                    { KEY.Enlist, KEY.Enlist },
                    { KEY.FailoverPartner, KEY.FailoverPartner },
                    { KEY.Initial_Catalog, KEY.Initial_Catalog },
                    { KEY.Integrated_Security, KEY.Integrated_Security },
                    { KEY.Load_Balance_Timeout, KEY.Load_Balance_Timeout },
                    { KEY.MARS, KEY.MARS },
                    { KEY.Max_Pool_Size, KEY.Max_Pool_Size },
                    { KEY.Min_Pool_Size, KEY.Min_Pool_Size },
                    { KEY.MultiSubnetFailover, KEY.MultiSubnetFailover },
                    { KEY.Network_Library, KEY.Network_Library },
                    { KEY.Packet_Size, KEY.Packet_Size },
                    { KEY.Password, KEY.Password },
                    { KEY.Persist_Security_Info, KEY.Persist_Security_Info },
                    { KEY.Pooling, KEY.Pooling },
                    { KEY.Replication, KEY.Replication },
                    { KEY.TrustServerCertificate, KEY.TrustServerCertificate },
                    { KEY.TransactionBinding, KEY.TransactionBinding },
                    { KEY.Type_System_Version, KEY.Type_System_Version },
                    { KEY.User_ID, KEY.User_ID },
                    { KEY.User_Instance, KEY.User_Instance },
                    { KEY.Workstation_Id, KEY.Workstation_Id },
                    { KEY.Connect_Retry_Count, KEY.Connect_Retry_Count },
                    { KEY.Connect_Retry_Interval, KEY.Connect_Retry_Interval },

                    { SYNONYM.APP, KEY.Application_Name },
                    { SYNONYM.Async, KEY.AsynchronousProcessing },
                    { SYNONYM.EXTENDED_PROPERTIES, KEY.AttachDBFilename },
                    { SYNONYM.INITIAL_FILE_NAME, KEY.AttachDBFilename },
                    { SYNONYM.CONNECTION_TIMEOUT, KEY.Connect_Timeout },
                    { SYNONYM.TIMEOUT, KEY.Connect_Timeout },
                    { SYNONYM.LANGUAGE, KEY.Current_Language },
                    { SYNONYM.ADDR, KEY.Data_Source },
                    { SYNONYM.ADDRESS, KEY.Data_Source },
                    { SYNONYM.NETWORK_ADDRESS, KEY.Data_Source },
                    { SYNONYM.SERVER, KEY.Data_Source },
                    { SYNONYM.DATABASE, KEY.Initial_Catalog },
                    { SYNONYM.TRUSTED_CONNECTION, KEY.Integrated_Security },
                    { SYNONYM.Connection_Lifetime, KEY.Load_Balance_Timeout },
                    { SYNONYM.NET, KEY.Network_Library },
                    { SYNONYM.NETWORK, KEY.Network_Library },
                    { SYNONYM.Pwd, KEY.Password },
                    { SYNONYM.PERSISTSECURITYINFO, KEY.Persist_Security_Info },
                    { SYNONYM.UID, KEY.User_ID },
                    { SYNONYM.User, KEY.User_ID },
                    { SYNONYM.WSID, KEY.Workstation_Id }
                };
                Debug.Assert(synonyms.Count == count, "incorrect initial ParseSynonyms size");
                s_sqlClientSynonyms = synonyms;
            }
            return synonyms;
        }

        internal string ObtainWorkstationId()
        {
            // If not supplied by the user, the default value is the MachineName
            // Note: In Longhorn you'll be able to rename a machine without
            // rebooting.  Therefore, don't cache this machine name.
            string result = WorkstationId;
            if (null == result)
            {
                // permission to obtain Environment.MachineName is Asserted
                // since permission to open the connection has been granted
                // the information is shared with the server, but not directly with the user
                result = ADP.MachineName();
                ValidateValueLength(result, TdsEnums.MAXLEN_HOSTNAME, KEY.Workstation_Id);
            }
            return result;
        }


        private void ValidateValueLength(string value, int limit, string key)
        {
            if (limit < value.Length)
            {
                throw ADP.InvalidConnectionOptionValueLength(key, limit);
            }
        }


        internal System.Data.SqlClient.ApplicationIntent ConvertValueToApplicationIntent()
        {
            string value;
            if (!TryGetParsetableValue(KEY.ApplicationIntent, out value))
            {
                return DEFAULT.ApplicationIntent;
            }

            // when wrong value is used in the connection string provided to SqlConnection.ConnectionString or c-tor,
            // wrap Format and Overflow exceptions with Argument one, to be consistent with rest of the keyword types (like int and bool)
            try
            {
                return DbConnectionStringBuilderUtil.ConvertToApplicationIntent(KEY.ApplicationIntent, value);
            }
            catch (FormatException e)
            {
                throw ADP.InvalidConnectionOptionValue(KEY.ApplicationIntent, e);
            }
            catch (OverflowException e)
            {
                throw ADP.InvalidConnectionOptionValue(KEY.ApplicationIntent, e);
            }
            // ArgumentException and other types are raised as is (no wrapping)
        }
        internal void ThrowUnsupportedIfKeywordSet(string keyword)
        {
            if (ContainsKey(keyword))
            {
                throw SQL.UnsupportedKeyword(keyword);
            }
        }
    }
}

