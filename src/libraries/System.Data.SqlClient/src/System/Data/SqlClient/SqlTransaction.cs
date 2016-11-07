// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    public sealed class SqlTransaction : DbTransaction
    {
        private static readonly DiagnosticListener s_diagnosticListener = new DiagnosticListener(SqlClientDiagnosticListenerExtensions.DiagnosticListenerName);

        internal readonly IsolationLevel _isolationLevel = IsolationLevel.ReadCommitted;

        private SqlInternalTransaction _internalTransaction;
        private SqlConnection _connection;

        private bool _isFromAPI;

        internal SqlTransaction(SqlInternalConnection internalConnection, SqlConnection con,
                                IsolationLevel iso, SqlInternalTransaction internalTransaction)
        {
            _isolationLevel = iso;
            _connection = con;

            if (internalTransaction == null)
            {
                _internalTransaction = new SqlInternalTransaction(internalConnection, TransactionType.LocalFromAPI, this);
            }
            else
            {
                Debug.Assert(internalConnection.CurrentTransaction == internalTransaction, "Unexpected Parser.CurrentTransaction state!");
                _internalTransaction = internalTransaction;
                _internalTransaction.InitParent(this);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // PROPERTIES
        ////////////////////////////////////////////////////////////////////////////////////////

        new public SqlConnection Connection
        {
            get
            {
                if (IsZombied)
                {
                    return null;
                }
                else
                {
                    return _connection;
                }
            }
        }

        override protected DbConnection DbConnection
        {
            get
            {
                return Connection;
            }
        }

        internal SqlInternalTransaction InternalTransaction
        {
            get
            {
                return _internalTransaction;
            }
        }

        override public IsolationLevel IsolationLevel
        {
            get
            {
                ZombieCheck();
                return _isolationLevel;
            }
        }

        private bool IsYukonPartialZombie
        {
            get
            {
                return (null != _internalTransaction && _internalTransaction.IsCompleted);
            }
        }

        internal bool IsZombied
        {
            get
            {
                return (null == _internalTransaction || _internalTransaction.IsCompleted);
            }
        }


        internal SqlStatistics Statistics
        {
            get
            {
                if (null != _connection)
                {
                    if (_connection.StatisticsEnabled)
                    {
                        return _connection.Statistics;
                    }
                }
                return null;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // PUBLIC METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        override public void Commit()
        {
            Exception e = null;
            Guid operationId = s_diagnosticListener.WriteTransactionCommitBefore(_isolationLevel, _connection);

            ZombieCheck();

            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                _isFromAPI = true;

                _internalTransaction.Commit();
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                {
                    s_diagnosticListener.WriteTransactionCommitError(operationId, _isolationLevel, _connection, e);
                }
                else
                {
                    s_diagnosticListener.WriteTransactionCommitAfter(operationId, _isolationLevel, _connection);
                }

                _isFromAPI = false;

                SqlStatistics.StopTimer(statistics);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsZombied && !IsYukonPartialZombie)
                {
                    _internalTransaction.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        override public void Rollback()
        {
            Exception e = null;
            Guid operationId = s_diagnosticListener.WriteTransactionRollbackBefore(_isolationLevel, _connection, null);

            if (IsYukonPartialZombie)
            {
                // Put something in the trace in case a customer has an issue
                _internalTransaction = null; // yukon zombification
            }
            else
            {
                ZombieCheck();

                SqlStatistics statistics = null;
                try
                {
                    statistics = SqlStatistics.StartTimer(Statistics);

                    _isFromAPI = true;

                    _internalTransaction.Rollback();
                }
                catch (Exception ex)
                {
                    e = ex;
                    throw;
                }
                finally
                {
                    if (e != null)
                    {
                        s_diagnosticListener.WriteTransactionRollbackError(operationId, _isolationLevel, _connection, null, e);
                    }
                    else
                    {
                        s_diagnosticListener.WriteTransactionRollbackAfter(operationId, _isolationLevel, _connection, null);
                    }
                    _isFromAPI = false;

                    SqlStatistics.StopTimer(statistics);
                }
            }
        }

        public void Rollback(string transactionName)
        {
            Exception e = null;
            Guid operationId = s_diagnosticListener.WriteTransactionRollbackBefore(_isolationLevel, _connection, transactionName);

            ZombieCheck();

            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                _isFromAPI = true;

                _internalTransaction.Rollback(transactionName);
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                {
                    s_diagnosticListener.WriteTransactionRollbackError(operationId, _isolationLevel, _connection, transactionName, e);
                }
                else
                {
                    s_diagnosticListener.WriteTransactionRollbackAfter(operationId, _isolationLevel, _connection, transactionName);
                }

                _isFromAPI = false;

                SqlStatistics.StopTimer(statistics);
            }
        }

        public void Save(string savePointName)
        {
            ZombieCheck();

            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                _internalTransaction.Save(savePointName);
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // INTERNAL METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        internal void Zombie()
        {
            // For Yukon, we have to defer "zombification" until
            //                 we get past the users' next rollback, else we'll
            //                 throw an exception there that is a breaking change.
            //                 Of course, if the connection is already closed, 
            //                 then we're free to zombify...
            SqlInternalConnection internalConnection = (_connection.InnerConnection as SqlInternalConnection);
            if (internalConnection == null || _isFromAPI)
            {
                _internalTransaction = null; // pre-yukon zombification
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // PRIVATE METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        private void ZombieCheck()
        {
            // If this transaction has been completed, throw exception since it is unusable.
            if (IsZombied)
            {
                if (IsYukonPartialZombie)
                {
                    _internalTransaction = null; // yukon zombification
                }

                throw ADP.TransactionZombied(this);
            }
        }
    }
}

