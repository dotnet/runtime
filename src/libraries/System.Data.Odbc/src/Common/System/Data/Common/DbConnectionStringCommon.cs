// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Data.Common
{
    internal static class DbConnectionStringDefaults
    {
        // all
        //        internal const string NamedConnection           = "";

        // Odbc
        internal const string Driver = "";
        internal const string Dsn = "";

        // OleDb
        internal const bool AdoNetPooler = false;
        internal const string FileName = "";
        internal const int OleDbServices = ~(/*DBPROPVAL_OS_AGR_AFTERSESSION*/0x00000008 | /*DBPROPVAL_OS_CLIENTCURSOR*/0x00000004); // -13
        internal const string Provider = "";

        // OracleClient
        internal const bool Unicode = false;
        internal const bool OmitOracleConnectionName = false;

        // SqlClient
        //internal const ApplicationIntent ApplicationIntent = System.Data.SqlClient.ApplicationIntent.ReadWrite;
        internal const string ApplicationName = ".Net SqlClient Data Provider";
        internal const bool AsynchronousProcessing = false;
        internal const string AttachDBFilename = "";
        internal const int ConnectTimeout = 15;
        internal const bool ConnectionReset = true;
        internal const bool ContextConnection = false;
        internal const string CurrentLanguage = "";
        internal const string DataSource = "";
        internal const bool Encrypt = false;
        internal const bool Enlist = true;
        internal const string FailoverPartner = "";
        internal const string InitialCatalog = "";
        internal const bool IntegratedSecurity = false;
        internal const int LoadBalanceTimeout = 0; // default of 0 means don't use
        internal const bool MultipleActiveResultSets = false;
        internal const bool MultiSubnetFailover = false;
        internal const bool TransparentNetworkIPResolution = true;
        internal const int MaxPoolSize = 100;
        internal const int MinPoolSize = 0;
        internal const string NetworkLibrary = "";
        internal const int PacketSize = 8000;
        internal const string Password = "";
        internal const bool PersistSecurityInfo = false;
        internal const bool Pooling = true;
        internal const bool TrustServerCertificate = false;
        internal const string TypeSystemVersion = "Latest";
        internal const string UserID = "";
        internal const bool UserInstance = false;
        internal const bool Replication = false;
        internal const string WorkstationID = "";
        internal const string TransactionBinding = "Implicit Unbind";
        internal const int ConnectRetryCount = 1;
        internal const int ConnectRetryInterval = 10;
        //internal static readonly SqlAuthenticationMethod Authentication = SqlAuthenticationMethod.NotSpecified;
        //internal static readonly SqlConnectionColumnEncryptionSetting ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Disabled;
        //internal const PoolBlockingPeriod PoolBlockingPeriod = SqlClient.PoolBlockingPeriod.Auto;
    }

    internal static class DbConnectionStringKeywords
    {
        // all
        //        internal const string NamedConnection           = "Named Connection";

        // Odbc
        internal const string Driver = "Driver";
        internal const string Dsn = "Dsn";
        internal const string FileDsn = "FileDsn";
        internal const string SaveFile = "SaveFile";

        // OleDb
        internal const string FileName = "File Name";
        internal const string OleDbServices = "OLE DB Services";
        internal const string Provider = "Provider";

        // OracleClient
        internal const string Unicode = "Unicode";
        internal const string OmitOracleConnectionName = "Omit Oracle Connection Name";

        // SqlClient
        internal const string ApplicationIntent = "ApplicationIntent";
        internal const string ApplicationName = "Application Name";
        internal const string AsynchronousProcessing = "Asynchronous Processing";
        internal const string AttachDBFilename = "AttachDbFilename";
        internal const string ConnectTimeout = "Connect Timeout";
        internal const string ConnectionReset = "Connection Reset";
        internal const string ContextConnection = "Context Connection";
        internal const string CurrentLanguage = "Current Language";
        internal const string Encrypt = "Encrypt";
        internal const string FailoverPartner = "Failover Partner";
        internal const string InitialCatalog = "Initial Catalog";
        internal const string MultipleActiveResultSets = "MultipleActiveResultSets";
        internal const string MultiSubnetFailover = "MultiSubnetFailover";
        internal const string TransparentNetworkIPResolution = "TransparentNetworkIPResolution";
        internal const string NetworkLibrary = "Network Library";
        internal const string PacketSize = "Packet Size";
        internal const string Replication = "Replication";
        internal const string TransactionBinding = "Transaction Binding";
        internal const string TrustServerCertificate = "TrustServerCertificate";
        internal const string TypeSystemVersion = "Type System Version";
        internal const string UserInstance = "User Instance";
        internal const string WorkstationID = "Workstation ID";
        internal const string ConnectRetryCount = "ConnectRetryCount";
        internal const string ConnectRetryInterval = "ConnectRetryInterval";
        internal const string Authentication = "Authentication";
        internal const string Certificate = "Certificate";
        internal const string ColumnEncryptionSetting = "Column Encryption Setting";
        internal const string PoolBlockingPeriod = "PoolBlockingPeriod";

        // common keywords (OleDb, OracleClient, SqlClient)
        internal const string DataSource = "Data Source";
        internal const string IntegratedSecurity = "Integrated Security";
        internal const string Password = "Password";
        internal const string PersistSecurityInfo = "Persist Security Info";
        internal const string UserID = "User ID";

        // managed pooling (OracleClient, SqlClient)
        internal const string Enlist = "Enlist";
        internal const string LoadBalanceTimeout = "Load Balance Timeout";
        internal const string MaxPoolSize = "Max Pool Size";
        internal const string Pooling = "Pooling";
        internal const string MinPoolSize = "Min Pool Size";
    }

    internal static class DbConnectionOptionKeywords
    {
        // Odbc
        internal const string Driver = "driver";
        internal const string Pwd = "pwd";
        internal const string UID = "uid";

        // OleDb
        internal const string DataProvider = "data provider";
        internal const string ExtendedProperties = "extended properties";
        internal const string FileName = "file name";
        internal const string Provider = "provider";
        internal const string RemoteProvider = "remote provider";

        // common keywords (OleDb, OracleClient, SqlClient)
        internal const string Password = "password";
        internal const string UserID = "user id";
    }


    internal static class DbConnectionStringBuilderUtil
    {
        internal static bool ConvertToBoolean(object value)
        {
            Debug.Assert(null != value, "ConvertToBoolean(null)");
            string? svalue = (value as string);
            if (null != svalue)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "true") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "yes"))
                {
                    return true;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "false") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "no"))
                {
                    return false;
                }
                else
                {
                    string tmp = svalue.Trim();  // Remove leading & trailing white space.
                    if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "true") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "yes"))
                    {
                        return true;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "false") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "no"))
                    {
                        return false;
                    }
                }
                return bool.Parse(svalue);
            }

            try
            {
                return ((IConvertible)value).ToBoolean(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(bool), e);
            }
        }

        internal static bool ConvertToIntegratedSecurity(object value)
        {
            Debug.Assert(null != value, "ConvertToIntegratedSecurity(null)");
            string? svalue = (value as string);
            if (null != svalue)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "sspi") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "true") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "yes"))
                {
                    return true;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "false") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "no"))
                {
                    return false;
                }
                else
                {
                    string tmp = svalue.Trim();  // Remove leading & trailing white space.
                    if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "sspi") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "true") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "yes"))
                    {
                        return true;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "false") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "no"))
                    {
                        return false;
                    }
                }
                return bool.Parse(svalue);
            }

            try
            {
                return ((IConvertible)value).ToBoolean(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(bool), e);
            }
        }

        internal static int ConvertToInt32(object value)
        {
            try
            {
                return ((IConvertible)value).ToInt32(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(int), e);
            }
        }

        internal static string ConvertToString(object value)
        {
            try
            {
                return ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(string), e);
            }
        }
    }
}
