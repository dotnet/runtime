// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.SqlClient.ManualTesting.Tests.SystemDataInternals;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class InternalConnectionWrapper
    {
        private static Dictionary<string, string> s_killByTSqlConnectionStrings = new Dictionary<string, string>();
        private static ReaderWriterLockSlim s_killByTSqlConnectionStringsLock = new ReaderWriterLockSlim();

        private object _internalConnection = null;
        private object _spid = null;

        /// <summary>
        /// Gets the internal connection associated with the given SqlConnection
        /// </summary>
        /// <param name="connection">Live outer connection to grab the inner connection from</param>
        /// <param name="supportKillByTSql">If true then we will query the server for this connection's SPID details (to be used in the KillConnectionByTSql method)</param>
        public InternalConnectionWrapper(SqlConnection connection, bool supportKillByTSql = false)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _internalConnection = connection.GetInternalConnection();
            ConnectionString = connection.ConnectionString;

            if (supportKillByTSql)
            {
                // Save the SPID for later use
                using (SqlCommand command = new SqlCommand("SELECT @@SPID", connection))
                {
                    _spid = command.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Gets the connection pool this internal connection is in
        /// </summary>
        public ConnectionPoolWrapper ConnectionPool
        { get { return new ConnectionPoolWrapper(_internalConnection, ConnectionString); } }

        /// <summary>
        /// Is this internal connection associated with the given SqlConnection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool IsInternalConnectionOf(SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            return (_internalConnection == connection.GetInternalConnection());
        }


        /// <summary>
        /// The connection string used to create this connection
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// True if the connection is still alive, otherwise false
        /// NOTE: Do NOT use this on a connection that is currently in use (There is a Debug.Assert and it will always return true)
        /// NOTE: If the connection is dead, it will be marked as 'broken'
        /// </summary>
        public bool IsConnectionAlive()
        {
            return ConnectionHelper.IsConnectionAlive(_internalConnection);
        }

        /// <summary>
        /// Will attempt to kill the connection
        /// </summary>
        public void KillConnection()
        {
            object tdsParser = ConnectionHelper.GetParser(_internalConnection);
            object stateObject = TdsParserHelper.GetStateObject(tdsParser);
            object sessionHandle = TdsParserStateObjectHelper.GetSessionHandle(stateObject);

            Assembly systemDotData = Assembly.Load(new AssemblyName(typeof(SqlConnection).GetTypeInfo().Assembly.FullName));
            Type sniHandleType = systemDotData.GetType("System.Data.SqlClient.SNI.SNIHandle");
            MethodInfo killConn = sniHandleType.GetMethod("KillConnection");

            if (killConn != null)
            {
                killConn.Invoke(sessionHandle, null);
            }
            else
            {
                throw new InvalidOperationException("Error: Could not find SNI KillConnection test hook. This operation is only supported in debug builds.");
            }
            // Ensure kill occurs outside of check connection window
            Thread.Sleep(100);
        }

        /// <summary>
        /// Requests that the server kills this connection
        /// NOTE: InternalConnectionWrapper must be created with SupportKillByTSql enabled
        /// </summary>
        public void KillConnectionByTSql()
        {
            if (_spid != null)
            {
                using (SqlConnection connection = new SqlConnection(GetKillByTSqlConnectionString()))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(string.Format("KILL {0}", _spid), connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                // Ensure kill occurs outside of check connection window
                Thread.Sleep(100);
            }
            else
            {
                throw new InvalidOperationException("Kill by TSql not enabled on this InternalConnectionWrapper");
            }
        }

        /// <summary>
        /// Gets a connection string that can be used to send a command to the server to kill this connection
        /// </summary>
        /// <returns>A connection string</returns>
        private string GetKillByTSqlConnectionString()
        {
            string killConnectionString = null;
            bool containsConnectionString = true;

            try
            {
                s_killByTSqlConnectionStringsLock.EnterReadLock();
                containsConnectionString = s_killByTSqlConnectionStrings.TryGetValue(ConnectionString, out killConnectionString);
            }
            finally
            {
                s_killByTSqlConnectionStringsLock.ExitReadLock();
            }
            if (!containsConnectionString)
            {
                killConnectionString = CreateKillByTSqlConnectionString(ConnectionString);

                try
                {
                    s_killByTSqlConnectionStringsLock.EnterWriteLock();
                    s_killByTSqlConnectionStrings.TryAdd(ConnectionString, killConnectionString);
                }
                finally
                {
                    s_killByTSqlConnectionStringsLock.ExitWriteLock();
                }
            }

            return killConnectionString;
        }

        /// <summary>
        /// Converts a connection string for a format which is appropriate to kill another connection with (i.e. non-pooled, no transactions)
        /// </summary>
        /// <param name="connectionString">Base connection string to convert</param>
        /// <returns>The converted connection string</returns>
        private static string CreateKillByTSqlConnectionString(string connectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            // Avoid tampering with the connection pool
            builder.Pooling = false;
            return builder.ConnectionString;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            bool areEquals = false;

            InternalConnectionWrapper objAsWrapper = obj as InternalConnectionWrapper;
            if ((objAsWrapper != null) && (objAsWrapper._internalConnection == _internalConnection))
                areEquals = true;

            return areEquals;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return _internalConnection.GetHashCode();
        }
    }
}
