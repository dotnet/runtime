// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stress.Data
{
    public class DataStressConnection : IDisposable
    {
        public DbConnection DbConnection { get; private set; }
        private readonly bool _clearPoolBeforeClose;
        public DataStressConnection(DbConnection conn, bool clearPoolBeforeClose = false)
        {
            if (conn == null)
                throw new ArgumentException("Cannot pass in null DbConnection to make new DataStressConnection!");
            this.DbConnection = conn;
            _clearPoolBeforeClose = clearPoolBeforeClose;
        }

        private short _spid = 0;

        [ThreadStatic]
        private static TrackedRandom t_randomInstance;
        private static TrackedRandom RandomInstance
        {
            get
            {
                if (t_randomInstance == null)
                    t_randomInstance = new TrackedRandom();
                return t_randomInstance;
            }
        }

        public void Open()
        {
            bool sync = RandomInstance.NextBool();

            if (sync)
            {
                OpenSync();
            }
            else
            {
                Task t = OpenAsync();
                AsyncUtils.WaitAndUnwrapException(t);
            }
        }

        public async Task OpenAsync()
        {
            int startMilliseconds = Environment.TickCount;
            try
            {
                await DbConnection.OpenAsync();
            }
            catch (ObjectDisposedException e)
            {
                HandleObjectDisposedException(e, true);
                throw;
            }
            catch (InvalidOperationException e)
            {
                int endMilliseconds = Environment.TickCount;

                // we may be able to handle this exception
                HandleInvalidOperationException(e, startMilliseconds, endMilliseconds, true);
                throw;
            }

            GetSpid();
        }

        private void OpenSync()
        {
            int startMilliseconds = Environment.TickCount;
            try
            {
                DbConnection.Open();
            }
            catch (ObjectDisposedException e)
            {
                HandleObjectDisposedException(e, false);
                throw;
            }
            catch (InvalidOperationException e)
            {
                int endMilliseconds = Environment.TickCount;

                // we may be able to handle this exception
                HandleInvalidOperationException(e, startMilliseconds, endMilliseconds, false);
                throw;
            }

            GetSpid();
        }

        private void HandleObjectDisposedException(ObjectDisposedException e, bool async)
        {
            // Race condition in DbConnectionFactory.TryGetConnection results in an ObjectDisposedException when calling OpenAsync on a non-pooled connection
            string methodName = async ? "OpenAsync()" : "Open()";
            throw DataStressErrors.ProductError(
                "Hit ObjectDisposedException in SqlConnection." + methodName, e);
        }

        private static int s_fastTimeoutCountOpen;      // number of times hit by SqlConnection.Open
        private static int s_fastTimeoutCountOpenAsync; // number of times hit by SqlConnection.OpenAsync
        private static readonly DateTime s_startTime = DateTime.Now;

        private const int MaxFastTimeoutCountPerDay = 200;

        /// <summary>
        /// Handles InvalidOperationException generated from Open or OpenAsync calls.
        /// For any other type of Exception, it simply returns
        /// </summary>
        private void HandleInvalidOperationException(InvalidOperationException e, int startMilliseconds, int endMilliseconds, bool async)
        {
            int elapsedMilliseconds = unchecked(endMilliseconds - startMilliseconds); // unchecked to handle overflow of Environment.TickCount

            // Since InvalidOperationExceptions due to timeout can be caused by issues  
            // (e.g. network hiccup, server unavailable, etc) we need a heuristic to guess whether or not this exception
            // should have happened or not.
            bool wasTimeoutFromPool = (e.GetType() == typeof(InvalidOperationException)) &&
                                      (e.Message.StartsWith("Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool"));

            bool wasTooEarly = (elapsedMilliseconds < ((DbConnection.ConnectionTimeout - 5) * 1000));

            if (wasTimeoutFromPool && wasTooEarly)
            {
                if (async)
                    Interlocked.Increment(ref s_fastTimeoutCountOpenAsync);
                else
                    Interlocked.Increment(ref s_fastTimeoutCountOpen);
            }
        }

        /// <summary>
        /// Gets spid value. 
        /// </summary>
        /// <remarks>
        /// If we want to kill the connection, we get its spid up front before the test case uses the connection. Otherwise if
        /// we try to get the spid when KillConnection is called, then the connection could be in a bad state (e.g. enlisted in
        /// aborted transaction, or has open datareader) and we will fail to get the spid. Also the randomization is put here
        /// instead of in KillConnection because otherwise this method would execute a command for every single connection which
        /// most of the time will not be used later. 
        /// </remarks>
        private void GetSpid()
        {
            if (DbConnection is System.Data.SqlClient.SqlConnection && RandomInstance.Next(0, 20) == 0)
            {
                using (var cmd = DbConnection.CreateCommand())
                {
                    cmd.CommandText = "select @@spid";
                    _spid = (short)cmd.ExecuteScalar();
                }
            }
            else
            {
                _spid = 0;
            }
        }

        /// <summary>
        /// Kills the given connection using "kill [spid]" if the parameter is nonzero
        /// </summary>
        private void KillConnection()
        {
            DataStressErrors.Assert(_spid != 0, "Called KillConnection with spid != 0");

            using (var killerConn = DataTestGroup.Factory.CreateConnection())
            {
                killerConn.Open();

                using (var killerCmd = killerConn.CreateCommand())
                {
                    killerCmd.CommandText = "begin try kill " + _spid + " end try begin catch end catch";
                    killerCmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Kills the given connection using "kill [spid]" if the parameter is nonzero
        /// </summary>
        /// <returns>a Task that is asynchronously killing the connection, or null if the connection is not being killed</returns>
        public Task KillConnectionAsync()
        {
            if (_spid == 0)
                return null;
            else
                return Task.Factory.StartNew(() => KillConnection());
        }

        public void Close()
        {
            if (_spid != 0)
            {
                KillConnection();

                // Wait before putting the connection back in the pool, to ensure that
                // the pool checks the connection the next time it is used.
                Task.Delay(10).ContinueWith((t) => DbConnection.Close());
            }
            else
            {
                // If this is a SqlConnection, and it is a connection with a unique connection string that we will never use again,
                // then call SqlConnection.ClearPool() before closing so that it is fully closed and does not waste client & server resources.
                if (_clearPoolBeforeClose)
                {
                    SqlConnection sqlConn = DbConnection as SqlConnection;
                    if (sqlConn != null) SqlConnection.ClearPool(sqlConn);
                }

                DbConnection.Close();
            }
        }

        public void Dispose()
        {
            Close();
        }

        public DbCommand CreateCommand()
        {
            return DbConnection.CreateCommand();
        }
    }
}
