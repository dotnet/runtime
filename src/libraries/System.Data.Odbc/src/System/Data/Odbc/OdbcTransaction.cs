// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;

namespace System.Data.Odbc
{
    public sealed class OdbcTransaction : DbTransaction
    {
        private OdbcConnection _connection;
        private IsolationLevel _isolevel = IsolationLevel.Unspecified;
        private OdbcConnectionHandle _handle;

        internal OdbcTransaction(OdbcConnection connection, IsolationLevel isolevel, OdbcConnectionHandle handle)
        {
            _connection = connection;
            _isolevel = isolevel;
            _handle = handle;
        }

        public new OdbcConnection Connection
        { // MDAC 66655
            get
            {
                return _connection;
            }
        }

        protected override DbConnection DbConnection
        { // MDAC 66655
            get
            {
                return Connection;
            }
        }

        public override IsolationLevel IsolationLevel
        {
            get
            {
                OdbcConnection connection = _connection;
                if (null == connection)
                {
                    throw ADP.TransactionZombied(this);
                }

                //We need to query for the case where the user didn't set the isolevel
                //BeginTransaction(), but we should also query to see if the driver
                //"rolled" the level to a higher supported one...
                if (IsolationLevel.Unspecified == _isolevel)
                {
                    //Get the isolation level
                    int sql_iso = connection.GetConnectAttr(ODBC32.SQL_ATTR.TXN_ISOLATION, ODBC32.HANDLER.THROW);
                    switch ((ODBC32.SQL_TRANSACTION)sql_iso)
                    {
                        case ODBC32.SQL_TRANSACTION.READ_UNCOMMITTED:
                            _isolevel = IsolationLevel.ReadUncommitted;
                            break;
                        case ODBC32.SQL_TRANSACTION.READ_COMMITTED:
                            _isolevel = IsolationLevel.ReadCommitted;
                            break;
                        case ODBC32.SQL_TRANSACTION.REPEATABLE_READ:
                            _isolevel = IsolationLevel.RepeatableRead;
                            break;
                        case ODBC32.SQL_TRANSACTION.SERIALIZABLE:
                            _isolevel = IsolationLevel.Serializable;
                            break;
                        case ODBC32.SQL_TRANSACTION.SNAPSHOT:
                            _isolevel = IsolationLevel.Snapshot;
                            break;
                        default:
                            throw ODBC.NoMappingForSqlTransactionLevel(sql_iso);
                    };
                }
                return _isolevel;
            }
        }

        public override void Commit()
        {
            OdbcConnection connection = _connection;
            if (null == connection)
            {
                throw ADP.TransactionZombied(this);
            }

            connection.CheckState(ADP.CommitTransaction); // MDAC 68289

            //Note: SQLEndTran success if not actually in a transaction, so we have to throw
            //since the IDbTransaciton spec indicates this is an error for the managed packages
            if (null == _handle)
            {
                throw ODBC.NotInTransaction();
            }

            ODBC32.RetCode retcode = _handle.CompleteTransaction(ODBC32.SQL_COMMIT);
            if (retcode == ODBC32.RetCode.ERROR)
            {
                //If an error has occurred, we will throw an exception in HandleError,
                //and leave the transaction active for the user to retry
                connection.HandleError(_handle, retcode);
            }

            //Transaction is complete...
            connection.LocalTransaction = null;
            _connection = null;
            _handle = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                OdbcConnectionHandle handle = _handle;
                _handle = null;
                if (null != handle)
                {
                    try
                    {
                        ODBC32.RetCode retcode = handle.CompleteTransaction(ODBC32.SQL_ROLLBACK);
                        if (retcode == ODBC32.RetCode.ERROR)
                        {
                            //don't throw an exception here, but trace it so it can be logged
                            if (_connection != null)
                            {
                                Exception e = _connection.HandleErrorNoThrow(handle, retcode);
                                ADP.TraceExceptionWithoutRethrow(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // 
                        if (!ADP.IsCatchableExceptionType(e))
                        {
                            throw;
                        }
                    }
                }
                if (_connection != null)
                {
                    if (_connection.IsOpen)
                    {
                        _connection.LocalTransaction = null;
                    }
                }
                _connection = null;
                _isolevel = IsolationLevel.Unspecified;
            }
            base.Dispose(disposing);
        }

        public override void Rollback()
        {
            OdbcConnection connection = _connection;
            if (null == connection)
            {
                throw ADP.TransactionZombied(this);
            }
            connection.CheckState(ADP.RollbackTransaction); // MDAC 68289

            //Note: SQLEndTran success if not actually in a transaction, so we have to throw
            //since the IDbTransaciton spec indicates this is an error for the managed packages
            if (null == _handle)
            {
                throw ODBC.NotInTransaction();
            }

            ODBC32.RetCode retcode = _handle.CompleteTransaction(ODBC32.SQL_ROLLBACK);
            if (retcode == ODBC32.RetCode.ERROR)
            {
                //If an error has occurred, we will throw an exception in HandleError,
                //and leave the transaction active for the user to retry
                connection.HandleError(_handle, retcode);
            }
            connection.LocalTransaction = null;
            _connection = null;
            _handle = null;
        }
    }
}

