// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    public sealed class OleDbTransaction : DbTransaction
    {
        private readonly OleDbTransaction? _parentTransaction; // strong reference to keep parent alive
        private readonly System.Data.IsolationLevel _isolationLevel;

        private WeakReference? _nestedTransaction; // child transactions
        private WrappedTransaction? _transaction;

        internal OleDbConnection _parentConnection;

        private sealed class WrappedTransaction : WrappedIUnknown
        {
            private bool _mustComplete;

            internal WrappedTransaction(UnsafeNativeMethods.ITransactionLocal transaction, int isolevel, out OleDbHResult hr) : base(transaction)
            {
                int transactionLevel = 0;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { }
                finally
                {
                    hr = transaction.StartTransaction(isolevel, 0, IntPtr.Zero, out transactionLevel);
                    if (hr >= 0)
                    {
                        _mustComplete = true;
                    }
                }
            }

            internal bool MustComplete
            {
                get { return _mustComplete; }
            }

            internal OleDbHResult Abort()
            {
                Debug.Assert(_mustComplete, "transaction already completed");
                OleDbHResult hr;
                bool mustRelease = false;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    DangerousAddRef(ref mustRelease);
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    { }
                    finally
                    {
                        hr = (OleDbHResult)NativeOledbWrapper.ITransactionAbort(DangerousGetHandle());
                        _mustComplete = false;
                    }
                }
                finally
                {
                    if (mustRelease)
                    {
                        DangerousRelease();
                    }
                }
                return hr;
            }

            internal OleDbHResult Commit()
            {
                Debug.Assert(_mustComplete, "transaction already completed");
                OleDbHResult hr;
                bool mustRelease = false;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    DangerousAddRef(ref mustRelease);
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    { }
                    finally
                    {
                        hr = (OleDbHResult)NativeOledbWrapper.ITransactionCommit(DangerousGetHandle());
                        if (((int)hr >= 0) || (hr == OleDbHResult.XACT_E_NOTRANSACTION))
                        {
                            _mustComplete = false;
                        }
                    }
                }
                finally
                {
                    if (mustRelease)
                    {
                        DangerousRelease();
                    }
                }
                return hr;
            }

            protected override bool ReleaseHandle()
            {
                if (_mustComplete && (base.handle != IntPtr.Zero))
                {
                    NativeOledbWrapper.ITransactionAbort(base.handle);
                    _mustComplete = false;
                }
                return base.ReleaseHandle();
            }
        }

        internal OleDbTransaction(OleDbConnection connection, OleDbTransaction? transaction, IsolationLevel isolevel)
        {
            _parentConnection = connection;
            _parentTransaction = transaction;

            switch (isolevel)
            {
                case IsolationLevel.Unspecified: // OLE DB doesn't support this isolevel on local transactions
                    isolevel = IsolationLevel.ReadCommitted;
                    break;
                case IsolationLevel.Chaos:
                case IsolationLevel.ReadUncommitted:
                case IsolationLevel.ReadCommitted:
                case IsolationLevel.RepeatableRead:
                case IsolationLevel.Serializable:
                case IsolationLevel.Snapshot:
                    break;
                default:
                    throw ADP.InvalidIsolationLevel(isolevel);
            }
            _isolationLevel = isolevel;
        }

        public new OleDbConnection Connection
        {
            get
            {
                return _parentConnection;
            }
        }

        protected override DbConnection DbConnection
        {
            get
            {
                return Connection;
            }
        }

        public override IsolationLevel IsolationLevel
        {
            get
            {
                if (_transaction == null)
                {
                    throw ADP.TransactionZombied(this);
                }
                return _isolationLevel;
            }
        }

        internal OleDbTransaction? Parent
        {
            get
            {
                return _parentTransaction;
            }
        }

        public OleDbTransaction Begin(IsolationLevel isolevel)
        {
            if (_transaction == null)
            {
                throw ADP.TransactionZombied(this);
            }
            else if ((_nestedTransaction != null) && _nestedTransaction.IsAlive)
            {
                throw ADP.ParallelTransactionsNotSupported(Connection);
            }
            // either the connection will be open or this will be a zombie

            OleDbTransaction transaction = new OleDbTransaction(_parentConnection, this, isolevel);
            _nestedTransaction = new WeakReference(transaction, false);

            UnsafeNativeMethods.ITransactionLocal? wrapper = null;
            try
            {
                wrapper = (UnsafeNativeMethods.ITransactionLocal)_transaction.ComWrapper();
                transaction.BeginInternal(wrapper);
            }
            finally
            {
                if (wrapper != null)
                {
                    Marshal.ReleaseComObject(wrapper);
                }
            }
            return transaction;
        }

        public OleDbTransaction Begin()
        {
            return Begin(IsolationLevel.ReadCommitted);
        }

        internal void BeginInternal(UnsafeNativeMethods.ITransactionLocal transaction)
        {
            OleDbHResult hr;
            _transaction = new WrappedTransaction(transaction, (int)_isolationLevel, out hr);
            if (hr < 0)
            {
                _transaction.Dispose();
                _transaction = null;
                ProcessResults(hr);
            }
        }

        public override void Commit()
        {
            if (_transaction == null)
            {
                throw ADP.TransactionZombied(this);
            }
            CommitInternal();
        }

        private void CommitInternal()
        {
            if (_transaction == null)
            {
                return;
            }
            if (_nestedTransaction != null)
            {
                OleDbTransaction? transaction = (OleDbTransaction?)_nestedTransaction.Target;
                if ((transaction != null) && _nestedTransaction.IsAlive)
                {
                    transaction.CommitInternal();
                }
                _nestedTransaction = null;
            }
            OleDbHResult hr = _transaction.Commit();
            if (!_transaction.MustComplete)
            {
                _transaction.Dispose();
                _transaction = null;

                DisposeManaged();
            }
            if (hr < 0)
            {
                // if an exception is thrown, user can try to commit their transaction again
                ProcessResults(hr);
            }
        }

        /*public OleDbCommand CreateCommand() {
            OleDbCommand cmd = Connection.CreateCommand();
            cmd.Transaction = this;
            return cmd;
        }

        IDbCommand IDbTransaction.CreateCommand() {
            return CreateCommand();
        }*/

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeManaged();
                RollbackInternal(false);
            }
            base.Dispose(disposing);
        }

        private void DisposeManaged()
        {
            if (_parentTransaction != null)
            {
                _parentTransaction._nestedTransaction = null;
                //_parentTransaction = null;
            }
            else if (_parentConnection != null)
            {
                _parentConnection.LocalTransaction = null;
            }
            _parentConnection = null!;
        }

        private void ProcessResults(OleDbHResult hr)
        {
            Exception? e = OleDbConnection.ProcessResults(hr, _parentConnection, this);
            if (e != null)
            { throw e; }
        }

        public override void Rollback()
        {
            if (_transaction == null)
            {
                throw ADP.TransactionZombied(this);
            }
            DisposeManaged();
            RollbackInternal(true); // no recover if this throws an exception
        }

        /*protected virtual*/
        internal OleDbHResult RollbackInternal(bool exceptionHandling)
        {
            OleDbHResult hr = 0;
            if (_transaction != null)
            {
                if (_nestedTransaction != null)
                {
                    OleDbTransaction? transaction = (OleDbTransaction?)_nestedTransaction.Target;
                    if ((transaction != null) && _nestedTransaction.IsAlive)
                    {
                        hr = transaction.RollbackInternal(exceptionHandling);
                        if (exceptionHandling && (hr < 0))
                        {
                            SafeNativeMethods.Wrapper.ClearErrorInfo();
                            return hr;
                        }
                    }
                    _nestedTransaction = null;
                }
                hr = _transaction.Abort();
                _transaction.Dispose();
                _transaction = null;
                if (hr < 0)
                {
                    if (exceptionHandling)
                    {
                        ProcessResults(hr);
                    }
                    else
                    {
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                    }
                }
            }
            return hr;
        }

        internal static OleDbTransaction TransactionLast(OleDbTransaction head)
        {
            if (head._nestedTransaction != null)
            {
                OleDbTransaction? current = (OleDbTransaction?)head._nestedTransaction.Target;
                if ((current != null) && head._nestedTransaction.IsAlive)
                {
                    return TransactionLast(current);
                }
            }
            return head;
        }

        internal static OleDbTransaction? TransactionUpdate(OleDbTransaction? transaction)
        {
            if ((transaction != null) && (transaction._transaction == null))
            {
                return null;
            }
            return transaction;
        }
    }
}
