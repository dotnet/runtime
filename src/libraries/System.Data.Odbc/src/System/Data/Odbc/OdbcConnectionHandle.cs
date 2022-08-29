// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.Data.Odbc
{
    internal sealed class OdbcConnectionHandle : OdbcHandle
    {
        private HandleState _handleState;

        private enum HandleState
        {
            Allocated = 0,
            Connected = 1,
            Transacted = 2,
            TransactionInProgress = 3,
        }

        internal OdbcConnectionHandle(OdbcConnection connection, OdbcConnectionString constr, OdbcEnvironmentHandle environmentHandle) : base(ODBC32.SQL_HANDLE.DBC, environmentHandle)
        {
            if (null == connection)
            {
                throw ADP.ArgumentNull(nameof(connection));
            }
            if (null == constr)
            {
                throw ADP.ArgumentNull(nameof(constr));
            }

            ODBC32.SQLRETURN retcode;

            //Set connection timeout (only before open).
            //Note: We use login timeout since its odbc 1.0 option, instead of using
            //connectiontimeout (which affects other things besides just login) and its
            //a odbc 3.0 feature.  The ConnectionTimeout on the managed providers represents
            //the login timeout, nothing more.
            int connectionTimeout = connection.ConnectionTimeout;
            SetConnectionAttribute2(ODBC32.SQL_ATTR.LOGIN_TIMEOUT, (IntPtr)connectionTimeout, (int)ODBC32.SQL_IS.UINTEGER);

            string connectionString = constr.UsersConnectionString(false);

            // Connect to the driver.  (Using the connection string supplied)
            //Note: The driver doesn't filter out the password in the returned connection string
            //so their is no need for us to obtain the returned connection string
            // Prepare to handle a ThreadAbort Exception between SQLDriverConnectW and update of the state variables
            retcode = Connect(connectionString);
            connection.HandleError(this, retcode);
        }

        private ODBC32.SQLRETURN AutoCommitOff()
        {
            ODBC32.SQLRETURN retcode;

            Debug.Assert(HandleState.Connected <= _handleState, "AutoCommitOff while in wrong state?");

            // must call SQLSetConnectAttrW and set _handleState
            try { }
            finally
            {
                retcode = Interop.Odbc.SQLSetConnectAttrW(this, ODBC32.SQL_ATTR.AUTOCOMMIT, ODBC32.SQL_AUTOCOMMIT_OFF, (int)ODBC32.SQL_IS.UINTEGER);
                switch (retcode)
                {
                    case ODBC32.SQLRETURN.SUCCESS:
                    case ODBC32.SQLRETURN.SUCCESS_WITH_INFO:
                        _handleState = HandleState.Transacted;
                        break;
                }
            }
            ODBC.TraceODBC(3, "SQLSetConnectAttrW", retcode);
            return retcode;
        }

        internal ODBC32.SQLRETURN BeginTransaction(ref IsolationLevel isolevel)
        {
            ODBC32.SQLRETURN retcode = ODBC32.SQLRETURN.SUCCESS;
            ODBC32.SQL_ATTR isolationAttribute;
            if (IsolationLevel.Unspecified != isolevel)
            {
                ODBC32.SQL_TRANSACTION sql_iso;
                switch (isolevel)
                {
                    case IsolationLevel.ReadUncommitted:
                        sql_iso = ODBC32.SQL_TRANSACTION.READ_UNCOMMITTED;
                        isolationAttribute = ODBC32.SQL_ATTR.TXN_ISOLATION;
                        break;
                    case IsolationLevel.ReadCommitted:
                        sql_iso = ODBC32.SQL_TRANSACTION.READ_COMMITTED;
                        isolationAttribute = ODBC32.SQL_ATTR.TXN_ISOLATION;
                        break;
                    case IsolationLevel.RepeatableRead:
                        sql_iso = ODBC32.SQL_TRANSACTION.REPEATABLE_READ;
                        isolationAttribute = ODBC32.SQL_ATTR.TXN_ISOLATION;
                        break;
                    case IsolationLevel.Serializable:
                        sql_iso = ODBC32.SQL_TRANSACTION.SERIALIZABLE;
                        isolationAttribute = ODBC32.SQL_ATTR.TXN_ISOLATION;
                        break;
                    case IsolationLevel.Snapshot:
                        sql_iso = ODBC32.SQL_TRANSACTION.SNAPSHOT;
                        // VSDD 414121: Snapshot isolation level must be set through SQL_COPT_SS_TXN_ISOLATION (https://docs.microsoft.com/en-us/sql/relational-databases/native-client-odbc-api/sqlsetconnectattr#sqlcoptsstxnisolation)
                        isolationAttribute = ODBC32.SQL_ATTR.SQL_COPT_SS_TXN_ISOLATION;
                        break;
                    case IsolationLevel.Chaos:
                        throw ODBC.NotSupportedIsolationLevel(isolevel);
                    default:
                        throw ADP.InvalidIsolationLevel(isolevel);
                }

                //Set the isolation level (unless its unspecified)
                retcode = SetConnectionAttribute2(isolationAttribute, (IntPtr)sql_iso, (int)ODBC32.SQL_IS.INTEGER);

                //Note: The Driver can return success_with_info to indicate it "rolled" the
                //isolevel to the next higher value.  If this is the case, we need to requery
                //the value if th euser asks for it...
                //We also still propagate the info, since it could be other info as well...

                if (ODBC32.SQLRETURN.SUCCESS_WITH_INFO == retcode)
                {
                    isolevel = IsolationLevel.Unspecified;
                }
            }

            switch (retcode)
            {
                case ODBC32.SQLRETURN.SUCCESS:
                case ODBC32.SQLRETURN.SUCCESS_WITH_INFO:
                    //Turn off auto-commit (which basically starts the transaction)
                    retcode = AutoCommitOff();
                    _handleState = HandleState.TransactionInProgress;
                    break;
            }
            return retcode;
        }

        internal ODBC32.SQLRETURN CompleteTransaction(short transactionOperation)
        {
            bool mustRelease = false;

            try
            {
                DangerousAddRef(ref mustRelease);
                ODBC32.SQLRETURN retcode = CompleteTransaction(transactionOperation, base.handle);
                return retcode;
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        private ODBC32.SQLRETURN CompleteTransaction(short transactionOperation, IntPtr handle)
        {
            // must only call this code from ReleaseHandle or DangerousAddRef region

            ODBC32.SQLRETURN retcode = ODBC32.SQLRETURN.SUCCESS;

            try { }
            finally
            {
                if (HandleState.TransactionInProgress == _handleState)
                {
                    retcode = Interop.Odbc.SQLEndTran(HandleType, handle, transactionOperation);
                    if ((ODBC32.SQLRETURN.SUCCESS == retcode) || (ODBC32.SQLRETURN.SUCCESS_WITH_INFO == retcode))
                    {
                        _handleState = HandleState.Transacted;
                    }
                }

                if (HandleState.Transacted == _handleState)
                { // AutoCommitOn
                    retcode = Interop.Odbc.SQLSetConnectAttrW(handle, ODBC32.SQL_ATTR.AUTOCOMMIT, ODBC32.SQL_AUTOCOMMIT_ON, (int)ODBC32.SQL_IS.UINTEGER);
                    _handleState = HandleState.Connected;
                }
            }
            //Overactive assert which fires if handle was allocated - but failed to connect to the server
            //it can more legitmately fire if transaction failed to rollback - but there isn't much we can do in that situation
            //Debug.Assert((HandleState.Connected == _handleState) || (HandleState.TransactionInProgress == _handleState), "not expected HandleState.Connected");
            return retcode;
        }
        private ODBC32.SQLRETURN Connect(string connectionString)
        {
            Debug.Assert(HandleState.Allocated == _handleState, "SQLDriverConnect while in wrong state?");

            ODBC32.SQLRETURN retcode;

            try { }
            finally
            {
                retcode = Interop.Odbc.SQLDriverConnectW(this, ADP.PtrZero, connectionString, ODBC32.SQL_NTS, ADP.PtrZero, 0, out _, (short)ODBC32.SQL_DRIVER.NOPROMPT);
                switch (retcode)
                {
                    case ODBC32.SQLRETURN.SUCCESS:
                    case ODBC32.SQLRETURN.SUCCESS_WITH_INFO:
                        _handleState = HandleState.Connected;
                        break;
                }
            }
            ODBC.TraceODBC(3, "SQLDriverConnectW", retcode);
            return retcode;
        }

        protected override bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once and is non-interrutible.

            // must call complete the transaction rollback, change handle state, and disconnect the connection
            CompleteTransaction(ODBC32.SQL_ROLLBACK, handle);

            if ((HandleState.Connected == _handleState) || (HandleState.TransactionInProgress == _handleState))
            {
                Interop.Odbc.SQLDisconnect(handle);
                _handleState = HandleState.Allocated;
            }
            Debug.Assert(HandleState.Allocated == _handleState, "not expected HandleState.Allocated");
            return base.ReleaseHandle();
        }

        internal ODBC32.SQLRETURN GetConnectionAttribute(ODBC32.SQL_ATTR attribute, byte[] buffer, out int cbActual)
        {
            ODBC32.SQLRETURN retcode = Interop.Odbc.SQLGetConnectAttrW(this, attribute, buffer, buffer.Length, out cbActual);
            return retcode;
        }

        internal ODBC32.SQLRETURN GetFunctions(ODBC32.SQL_API fFunction, out short fExists)
        {
            ODBC32.SQLRETURN retcode = Interop.Odbc.SQLGetFunctions(this, fFunction, out fExists);
            ODBC.TraceODBC(3, "SQLGetFunctions", retcode);
            return retcode;
        }

        internal ODBC32.SQLRETURN GetInfo2(ODBC32.SQL_INFO info, byte[] buffer, out short cbActual)
        {
            ODBC32.SQLRETURN retcode = Interop.Odbc.SQLGetInfoW(this, info, buffer, checked((short)buffer.Length), out cbActual);
            return retcode;
        }

        internal ODBC32.SQLRETURN GetInfo1(ODBC32.SQL_INFO info, byte[] buffer)
        {
            ODBC32.SQLRETURN retcode = Interop.Odbc.SQLGetInfoW(this, info, buffer, checked((short)buffer.Length), ADP.PtrZero);
            return retcode;
        }

        internal ODBC32.SQLRETURN SetConnectionAttribute2(ODBC32.SQL_ATTR attribute, IntPtr value, int length)
        {
            ODBC32.SQLRETURN retcode = Interop.Odbc.SQLSetConnectAttrW(this, attribute, value, length);
            ODBC.TraceODBC(3, "SQLSetConnectAttrW", retcode);
            return retcode;
        }

        internal ODBC32.SQLRETURN SetConnectionAttribute3(ODBC32.SQL_ATTR attribute, string buffer, int length)
        {
            ODBC32.SQLRETURN retcode = Interop.Odbc.SQLSetConnectAttrW(this, attribute, buffer, length);
            return retcode;
        }
    }
}
