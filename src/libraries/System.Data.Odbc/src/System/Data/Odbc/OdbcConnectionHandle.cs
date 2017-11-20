// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                throw ADP.ArgumentNull("connection");
            }
            if (null == constr)
            {
                throw ADP.ArgumentNull("constr");
            }

            ODBC32.RetCode retcode;

            //Set connection timeout (only before open).
            //Note: We use login timeout since its odbc 1.0 option, instead of using
            //connectiontimeout (which affects other things besides just login) and its
            //a odbc 3.0 feature.  The ConnectionTimeout on the managed providers represents
            //the login timeout, nothing more.
            int connectionTimeout = connection.ConnectionTimeout;
            retcode = SetConnectionAttribute2(ODBC32.SQL_ATTR.LOGIN_TIMEOUT, (IntPtr)connectionTimeout, (Int32)ODBC32.SQL_IS.UINTEGER);

            string connectionString = constr.UsersConnectionString(false);

            // Connect to the driver.  (Using the connection string supplied)
            //Note: The driver doesn't filter out the password in the returned connection string
            //so their is no need for us to obtain the returned connection string
            // Prepare to handle a ThreadAbort Exception between SQLDriverConnectW and update of the state variables
            retcode = Connect(connectionString);
            connection.HandleError(this, retcode);
        }

        private ODBC32.RetCode AutoCommitOff()
        {
            ODBC32.RetCode retcode;

            Debug.Assert(HandleState.Connected <= _handleState, "AutoCommitOff while in wrong state?");

            // Avoid runtime injected errors in the following block.
            // must call SQLSetConnectAttrW and set _handleState
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                retcode = Interop.Odbc.SQLSetConnectAttrW(this, ODBC32.SQL_ATTR.AUTOCOMMIT, ODBC32.SQL_AUTOCOMMIT_OFF, (Int32)ODBC32.SQL_IS.UINTEGER);
                switch (retcode)
                {
                    case ODBC32.RetCode.SUCCESS:
                    case ODBC32.RetCode.SUCCESS_WITH_INFO:
                        _handleState = HandleState.Transacted;
                        break;
                }
            }
            ODBC.TraceODBC(3, "SQLSetConnectAttrW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode BeginTransaction(ref IsolationLevel isolevel)
        {
            ODBC32.RetCode retcode = ODBC32.RetCode.SUCCESS;
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
                        // VSDD 414121: Snapshot isolation level must be set through SQL_COPT_SS_TXN_ISOLATION (http://msdn.microsoft.com/en-us/library/ms131709.aspx)
                        isolationAttribute = ODBC32.SQL_ATTR.SQL_COPT_SS_TXN_ISOLATION;
                        break;
                    case IsolationLevel.Chaos:
                        throw ODBC.NotSupportedIsolationLevel(isolevel);
                    default:
                        throw ADP.InvalidIsolationLevel(isolevel);
                }

                //Set the isolation level (unless its unspecified)
                retcode = SetConnectionAttribute2(isolationAttribute, (IntPtr)sql_iso, (Int32)ODBC32.SQL_IS.INTEGER);

                //Note: The Driver can return success_with_info to indicate it "rolled" the
                //isolevel to the next higher value.  If this is the case, we need to requery
                //the value if th euser asks for it...
                //We also still propagate the info, since it could be other info as well...

                if (ODBC32.RetCode.SUCCESS_WITH_INFO == retcode)
                {
                    isolevel = IsolationLevel.Unspecified;
                }
            }

            switch (retcode)
            {
                case ODBC32.RetCode.SUCCESS:
                case ODBC32.RetCode.SUCCESS_WITH_INFO:
                    //Turn off auto-commit (which basically starts the transaction)
                    retcode = AutoCommitOff();
                    _handleState = HandleState.TransactionInProgress;
                    break;
            }
            return retcode;
        }

        internal ODBC32.RetCode CompleteTransaction(short transactionOperation)
        {
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);
                ODBC32.RetCode retcode = CompleteTransaction(transactionOperation, base.handle);
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

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private ODBC32.RetCode CompleteTransaction(short transactionOperation, IntPtr handle)
        {
            // must only call this code from ReleaseHandle or DangerousAddRef region

            ODBC32.RetCode retcode = ODBC32.RetCode.SUCCESS;

            // using ConstrainedRegions to make the native ODBC call and change the _handleState
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                if (HandleState.TransactionInProgress == _handleState)
                {
                    retcode = Interop.Odbc.SQLEndTran(HandleType, handle, transactionOperation);
                    if ((ODBC32.RetCode.SUCCESS == retcode) || (ODBC32.RetCode.SUCCESS_WITH_INFO == retcode))
                    {
                        _handleState = HandleState.Transacted;
                    }
                }

                if (HandleState.Transacted == _handleState)
                { // AutoCommitOn
                    retcode = Interop.Odbc.SQLSetConnectAttrW(handle, ODBC32.SQL_ATTR.AUTOCOMMIT, ODBC32.SQL_AUTOCOMMIT_ON, (Int32)ODBC32.SQL_IS.UINTEGER);
                    _handleState = HandleState.Connected;
                }
            }
            //Overactive assert which fires if handle was allocated - but failed to connect to the server
            //it can more legitmately fire if transaction failed to rollback - but there isn't much we can do in that situation
            //Debug.Assert((HandleState.Connected == _handleState) || (HandleState.TransactionInProgress == _handleState), "not expected HandleState.Connected");
            return retcode;
        }
        private ODBC32.RetCode Connect(string connectionString)
        {
            Debug.Assert(HandleState.Allocated == _handleState, "SQLDriverConnect while in wrong state?");

            ODBC32.RetCode retcode;

            // Avoid runtime injected errors in the following block.
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                short cbActualSize;
                retcode = Interop.Odbc.SQLDriverConnectW(this, ADP.PtrZero, connectionString, ODBC32.SQL_NTS, ADP.PtrZero, 0, out cbActualSize, (short)ODBC32.SQL_DRIVER.NOPROMPT);
                switch (retcode)
                {
                    case ODBC32.RetCode.SUCCESS:
                    case ODBC32.RetCode.SUCCESS_WITH_INFO:
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
            ODBC32.RetCode retcode;

            // must call complete the transaction rollback, change handle state, and disconnect the connection
            retcode = CompleteTransaction(ODBC32.SQL_ROLLBACK, handle);

            if ((HandleState.Connected == _handleState) || (HandleState.TransactionInProgress == _handleState))
            {
                retcode = Interop.Odbc.SQLDisconnect(handle);
                _handleState = HandleState.Allocated;
            }
            Debug.Assert(HandleState.Allocated == _handleState, "not expected HandleState.Allocated");
            return base.ReleaseHandle();
        }

        internal ODBC32.RetCode GetConnectionAttribute(ODBC32.SQL_ATTR attribute, byte[] buffer, out int cbActual)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetConnectAttrW(this, attribute, buffer, buffer.Length, out cbActual);
            return retcode;
        }

        internal ODBC32.RetCode GetFunctions(ODBC32.SQL_API fFunction, out Int16 fExists)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetFunctions(this, fFunction, out fExists);
            ODBC.TraceODBC(3, "SQLGetFunctions", retcode);
            return retcode;
        }

        internal ODBC32.RetCode GetInfo2(ODBC32.SQL_INFO info, byte[] buffer, out short cbActual)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetInfoW(this, info, buffer, checked((short)buffer.Length), out cbActual);
            return retcode;
        }

        internal ODBC32.RetCode GetInfo1(ODBC32.SQL_INFO info, byte[] buffer)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetInfoW(this, info, buffer, checked((short)buffer.Length), ADP.PtrZero);
            return retcode;
        }

        internal ODBC32.RetCode SetConnectionAttribute2(ODBC32.SQL_ATTR attribute, IntPtr value, Int32 length)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLSetConnectAttrW(this, attribute, value, length);
            ODBC.TraceODBC(3, "SQLSetConnectAttrW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode SetConnectionAttribute3(ODBC32.SQL_ATTR attribute, string buffer, Int32 length)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLSetConnectAttrW(this, attribute, buffer, length);
            return retcode;
        }

        internal ODBC32.RetCode SetConnectionAttribute4(ODBC32.SQL_ATTR attribute, System.Transactions.IDtcTransaction transaction, Int32 length)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLSetConnectAttrW(this, attribute, transaction, length);
            ODBC.TraceODBC(3, "SQLSetConnectAttrW", retcode);
            return retcode;
        }
    }
}
