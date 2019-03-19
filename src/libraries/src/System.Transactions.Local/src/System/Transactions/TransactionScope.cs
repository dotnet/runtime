// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Transactions
{
    public enum TransactionScopeOption
    {
        Required,
        RequiresNew,
        Suppress,
    }

    //
    //  The legacy TransactionScope uses TLS to store the ambient transaction. TLS data doesn't flow across thread continuations and hence legacy TransactionScope does not compose well with
    //  new .NET async programming model constructs like Tasks and async/await. To enable TransactionScope to work with Task and async/await, a new TransactionScopeAsyncFlowOption
    //  is introduced. When users opt-in the async flow option, ambient transaction will automatically flow across thread continuations and user can compose TransactionScope with Task and/or    
    //  async/await constructs. 
    // 
    public enum TransactionScopeAsyncFlowOption
    {
        Suppress, // Ambient transaction will be stored in TLS and will not flow across thread continuations. 
        Enabled,  // Ambient transaction will be stored in CallContext and will flow across thread continuations. This option will enable TransactionScope to compose well with Task and async/await.
    }

    public enum EnterpriseServicesInteropOption
    {
        None = 0,
        Automatic = 1,
        Full = 2
    }

    public sealed class TransactionScope : IDisposable
    {
        public TransactionScope() : this(TransactionScopeOption.Required)
        {
        }

        public TransactionScope(TransactionScopeOption scopeOption)
            : this(scopeOption, TransactionScopeAsyncFlowOption.Suppress)
        {
        }

        public TransactionScope(TransactionScopeAsyncFlowOption asyncFlowOption)
            : this(TransactionScopeOption.Required, asyncFlowOption)
        {
        }

        public TransactionScope(
            TransactionScopeOption scopeOption,
            TransactionScopeAsyncFlowOption asyncFlowOption
            )
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }

            ValidateAndSetAsyncFlowOption(asyncFlowOption);

            if (NeedToCreateTransaction(scopeOption))
            {
                _committableTransaction = new CommittableTransaction();
                _expectedCurrent = _committableTransaction.Clone();
            }

            if (null == _expectedCurrent)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
                }
            }
            else
            {
                TransactionScopeResult scopeResult;

                if (null == _committableTransaction)
                {
                    scopeResult = TransactionScopeResult.UsingExistingCurrent;
                }
                else
                {
                    scopeResult = TransactionScopeResult.CreatedTransaction;
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, scopeResult);
                }
            }
 
            PushScope();

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        public TransactionScope(TransactionScopeOption scopeOption, TimeSpan scopeTimeout)
            : this(scopeOption, scopeTimeout, TransactionScopeAsyncFlowOption.Suppress)
        {
        }

        public TransactionScope(
            TransactionScopeOption scopeOption,
            TimeSpan scopeTimeout,
            TransactionScopeAsyncFlowOption asyncFlowOption
            )
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }

            ValidateScopeTimeout(nameof(scopeTimeout), scopeTimeout);
            TimeSpan txTimeout = TransactionManager.ValidateTimeout(scopeTimeout);

            ValidateAndSetAsyncFlowOption(asyncFlowOption);

            if (NeedToCreateTransaction(scopeOption))
            {
                _committableTransaction = new CommittableTransaction(txTimeout);
                _expectedCurrent = _committableTransaction.Clone();
            }

            if ((null != _expectedCurrent) && (null == _committableTransaction) && (TimeSpan.Zero != scopeTimeout))
            {
                // BUGBUG: Scopes should not use individual timers
                _scopeTimer = new Timer(
                    TimerCallback,
                    this,
                    scopeTimeout,
                    TimeSpan.Zero);
            }

            if (null == _expectedCurrent)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
                }
            }
            else
            {
                TransactionScopeResult scopeResult;

                if (null == _committableTransaction)
                {
                    scopeResult = TransactionScopeResult.UsingExistingCurrent;
                }
                else
                {
                    scopeResult = TransactionScopeResult.CreatedTransaction;
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, scopeResult);
                }
            }

            PushScope();

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        public TransactionScope(TransactionScopeOption scopeOption, TransactionOptions transactionOptions)
            : this(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Suppress)
        {
        }

        public TransactionScope(
            TransactionScopeOption scopeOption,
            TransactionOptions transactionOptions,
            TransactionScopeAsyncFlowOption asyncFlowOption
            )
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }

            ValidateScopeTimeout("transactionOptions.Timeout", transactionOptions.Timeout);
            TimeSpan scopeTimeout = transactionOptions.Timeout;

            transactionOptions.Timeout = TransactionManager.ValidateTimeout(transactionOptions.Timeout);
            TransactionManager.ValidateIsolationLevel(transactionOptions.IsolationLevel);

            ValidateAndSetAsyncFlowOption(asyncFlowOption);

            if (NeedToCreateTransaction(scopeOption))
            {
                _committableTransaction = new CommittableTransaction(transactionOptions);
                _expectedCurrent = _committableTransaction.Clone();
            }
            else
            {
                if (null != _expectedCurrent)
                {
                    // If the requested IsolationLevel is stronger than that of the specified transaction, throw.
                    if ((IsolationLevel.Unspecified != transactionOptions.IsolationLevel) && (_expectedCurrent.IsolationLevel != transactionOptions.IsolationLevel))
                    {
                        throw new ArgumentException(SR.TransactionScopeIsolationLevelDifferentFromTransaction, "transactionOptions.IsolationLevel");
                    }
                }
            }

            if ((null != _expectedCurrent) && (null == _committableTransaction) && (TimeSpan.Zero != scopeTimeout))
            {
                // BUGBUG: Scopes should use a shared timer
                _scopeTimer = new Timer(
                    TimerCallback,
                    this,
                    scopeTimeout,
                    TimeSpan.Zero);
            }

            if (null == _expectedCurrent)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
                }
            }
            else
            {
                TransactionScopeResult scopeResult;

                if (null == _committableTransaction)
                {
                    scopeResult = TransactionScopeResult.UsingExistingCurrent;
                }
                else
                {
                    scopeResult = TransactionScopeResult.CreatedTransaction;
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, scopeResult);
                }
            }

            PushScope();

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        public TransactionScope(
            TransactionScopeOption scopeOption,
            TransactionOptions transactionOptions,
            EnterpriseServicesInteropOption interopOption)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }
 
            ValidateScopeTimeout("transactionOptions.Timeout", transactionOptions.Timeout);
            TimeSpan scopeTimeout = transactionOptions.Timeout;

            transactionOptions.Timeout = TransactionManager.ValidateTimeout(transactionOptions.Timeout);
            TransactionManager.ValidateIsolationLevel(transactionOptions.IsolationLevel);

            ValidateInteropOption(interopOption);
            _interopModeSpecified = true;
            _interopOption = interopOption;

            if (NeedToCreateTransaction(scopeOption))
            {
                _committableTransaction = new CommittableTransaction(transactionOptions);
                _expectedCurrent = _committableTransaction.Clone();
            }
            else
            {
                if (null != _expectedCurrent)
                {
                    // If the requested IsolationLevel is stronger than that of the specified transaction, throw.
                    if ((IsolationLevel.Unspecified != transactionOptions.IsolationLevel) && (_expectedCurrent.IsolationLevel != transactionOptions.IsolationLevel))
                    {
                        throw new ArgumentException(SR.TransactionScopeIsolationLevelDifferentFromTransaction, "transactionOptions.IsolationLevel");
                    }
                }
            }

            if ((null != _expectedCurrent) && (null == _committableTransaction) && (TimeSpan.Zero != scopeTimeout))
            {
                // BUGBUG: Scopes should use a shared timer
                _scopeTimer = new Timer(
                    TimerCallback,
                    this,
                    scopeTimeout,
                    TimeSpan.Zero);
            }

            if (null == _expectedCurrent)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
                }
            }
            else
            {
                TransactionScopeResult scopeResult;

                if (null == _committableTransaction)
                {
                    scopeResult = TransactionScopeResult.UsingExistingCurrent;
                }
                else
                {
                    scopeResult = TransactionScopeResult.CreatedTransaction;
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, scopeResult);
                }
            }

            PushScope();

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        public TransactionScope(Transaction transactionToUse)
            : this(transactionToUse, TransactionScopeAsyncFlowOption.Suppress)
        {
        }

        public TransactionScope(
            Transaction transactionToUse,
            TransactionScopeAsyncFlowOption asyncFlowOption
            )
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }

            ValidateAndSetAsyncFlowOption(asyncFlowOption);

            Initialize(
                transactionToUse,
                TimeSpan.Zero,
                false);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        public TransactionScope(Transaction transactionToUse, TimeSpan scopeTimeout)
            : this(transactionToUse, scopeTimeout, TransactionScopeAsyncFlowOption.Suppress)
        {
        }

        public TransactionScope(
            Transaction transactionToUse,
            TimeSpan scopeTimeout,
            TransactionScopeAsyncFlowOption asyncFlowOption)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }

            ValidateAndSetAsyncFlowOption(asyncFlowOption);

            Initialize(
                transactionToUse,
                scopeTimeout,
                false);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        public TransactionScope(
            Transaction transactionToUse,
            TimeSpan scopeTimeout,
            EnterpriseServicesInteropOption interopOption
        )
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }

            ValidateInteropOption(interopOption);
            _interopOption = interopOption;

            Initialize(
                transactionToUse,
                scopeTimeout,
                true);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        private bool NeedToCreateTransaction(TransactionScopeOption scopeOption)
        {
            bool retVal = false;

            CommonInitialize();

            // If the options specify NoTransactionNeeded, that trumps everything else.
            switch (scopeOption)
            {
                case TransactionScopeOption.Suppress:
                    _expectedCurrent = null;
                    retVal = false;
                    break;

                case TransactionScopeOption.Required:
                    _expectedCurrent = _savedCurrent;
                    // If current is null, we need to create one.
                    if (null == _expectedCurrent)
                    {
                        retVal = true;
                    }
                    break;

                case TransactionScopeOption.RequiresNew:
                    retVal = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(scopeOption));
            }

            return retVal;
        }

        private void Initialize(
            Transaction transactionToUse,
            TimeSpan scopeTimeout,
            bool interopModeSpecified)
        {
            if (null == transactionToUse)
            {
                throw new ArgumentNullException(nameof(transactionToUse));
            }

            ValidateScopeTimeout(nameof(scopeTimeout), scopeTimeout);

            CommonInitialize();

            if (TimeSpan.Zero != scopeTimeout)
            {
                _scopeTimer = new Timer(
                    TimerCallback,
                    this,
                    scopeTimeout,
                    TimeSpan.Zero
                    );
            }

            _expectedCurrent = transactionToUse;
            _interopModeSpecified = interopModeSpecified;

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, TransactionScopeResult.TransactionPassed);
            }

            PushScope();
        }


        // We don't have a finalizer (~TransactionScope) because all it would be able to do is try to 
        // operate on other managed objects (the transaction), which is not safe to do because they may
        // already have been finalized.

        public void Dispose()
        {
            bool successful = false;

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }
            if (_disposed)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
                }
                return;
            }

            // Dispose for a scope can only be called on the thread where the scope was created.
            if ((_scopeThread != Thread.CurrentThread) && !AsyncFlowEnabled)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.InvalidOperation("TransactionScope", "InvalidScopeThread");
                }

                throw new InvalidOperationException(SR.InvalidScopeThread);
            }

            Exception exToThrow = null;

            try
            {
                // Single threaded from this point
                _disposed = true;

                // First, lets pop the "stack" of TransactionScopes and dispose each one that is above us in
                // the stack, making sure they are NOT consistent before disposing them.

                // Optimize the first lookup by getting both the actual current scope and actual current
                // transaction at the same time.
                TransactionScope actualCurrentScope = _threadContextData.CurrentScope;
                Transaction contextTransaction = null;
                Transaction current = Transaction.FastGetTransaction(actualCurrentScope, _threadContextData, out contextTransaction);

                if (!Equals(actualCurrentScope))
                {
                    // Ok this is bad.  But just how bad is it.  The worst case scenario is that someone is
                    // poping scopes out of order and has placed a new transaction in the top level scope.
                    // Check for that now.
                    if (actualCurrentScope == null)
                    {
                        // Something must have gone wrong trying to clean up a bad scope
                        // stack previously.
                        // Make a best effort to abort the active transaction.
                        Transaction rollbackTransaction = _committableTransaction;
                        if (rollbackTransaction == null)
                        {
                            rollbackTransaction = _dependentTransaction;
                        }
                        Debug.Assert(rollbackTransaction != null);
                        rollbackTransaction.Rollback();

                        successful = true;
                        throw TransactionException.CreateInvalidOperationException(
                            TraceSourceType.TraceSourceBase, SR.TransactionScopeInvalidNesting, null, rollbackTransaction.DistributedTxId);
                    }
                    // Verify that expectedCurrent is the same as the "current" current if we the interopOption value is None.
                    else if (EnterpriseServicesInteropOption.None == actualCurrentScope._interopOption)
                    {
                        if (((null != actualCurrentScope._expectedCurrent) && (!actualCurrentScope._expectedCurrent.Equals(current)))
                            ||
                            ((null != current) && (null == actualCurrentScope._expectedCurrent))
                            )
                        {
                            TransactionTraceIdentifier myId;
                            TransactionTraceIdentifier currentId;

                            if (null == current)
                            {
                                currentId = TransactionTraceIdentifier.Empty;
                            }
                            else
                            {
                                currentId = current.TransactionTraceId;
                            }

                            if (null == _expectedCurrent)
                            {
                                myId = TransactionTraceIdentifier.Empty;
                            }
                            else
                            {
                                myId = _expectedCurrent.TransactionTraceId;
                            }

                            if (etwLog.IsEnabled())
                            {
                                etwLog.TransactionScopeCurrentChanged(currentId, myId);
                            }

                            exToThrow = TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, SR.TransactionScopeIncorrectCurrent, null,
                                current == null ? Guid.Empty : current.DistributedTxId);

                            // If there is a current transaction, abort it.
                            if (null != current)
                            {
                                try
                                {
                                    current.Rollback();
                                }
                                catch (TransactionException)
                                {
                                    // we are already going to throw and exception, so just ignore this one.
                                }
                                catch (ObjectDisposedException)
                                {
                                    // Dito
                                }
                            }
                        }
                    }

                    // Now fix up the scopes
                    while (!Equals(actualCurrentScope))
                    {
                        if (null == exToThrow)
                        {
                            exToThrow = TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, SR.TransactionScopeInvalidNesting, null,
                                current == null ? Guid.Empty : current.DistributedTxId);
                        }

                        if (null == actualCurrentScope._expectedCurrent)
                        {
                            if (etwLog.IsEnabled())
                            {
                                etwLog.TransactionScopeNestedIncorrectly(TransactionTraceIdentifier.Empty);
                            }
                        }
                        else
                        {
                            if (etwLog.IsEnabled())
                            {
                                etwLog.TransactionScopeNestedIncorrectly(actualCurrentScope._expectedCurrent.TransactionTraceId);
                            }
                        }

                        actualCurrentScope._complete = false;
                        try
                        {
                            actualCurrentScope.InternalDispose();
                        }
                        catch (TransactionException)
                        {
                            // we are already going to throw an exception, so just ignore this one.
                        }

                        actualCurrentScope = _threadContextData.CurrentScope;

                        // We want to fail this scope, too, because work may have been done in one of these other
                        // nested scopes that really should have been done in my scope.
                        _complete = false;
                    }
                }
                else
                {
                    // Verify that expectedCurrent is the same as the "current" current if we the interopOption value is None.
                    // If we got here, actualCurrentScope is the same as "this".
                    if (EnterpriseServicesInteropOption.None == _interopOption)
                    {
                        if (((null != _expectedCurrent) && (!_expectedCurrent.Equals(current)))
                            || ((null != current) && (null == _expectedCurrent))
                            )
                        {
                            TransactionTraceIdentifier myId;
                            TransactionTraceIdentifier currentId;

                            if (null == current)
                            {
                                currentId = TransactionTraceIdentifier.Empty;
                            }
                            else
                            {
                                currentId = current.TransactionTraceId;
                            }

                            if (null == _expectedCurrent)
                            {
                                myId = TransactionTraceIdentifier.Empty;
                            }
                            else
                            {
                                myId = _expectedCurrent.TransactionTraceId;
                            }

                            if (etwLog.IsEnabled())
                            {
                                etwLog.TransactionScopeCurrentChanged(currentId, myId);
                            }

                            if (null == exToThrow)
                            {
                                exToThrow = TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, SR.TransactionScopeIncorrectCurrent, null,
                                    current == null ? Guid.Empty : current.DistributedTxId);
                            }

                            // If there is a current transaction, abort it.
                            if (null != current)
                            {
                                try
                                {
                                    current.Rollback();
                                }
                                catch (TransactionException)
                                {
                                    // we are already going to throw and exception, so just ignore this one.
                                }
                                catch (ObjectDisposedException)
                                {
                                    // Dito
                                }
                            }
                            // Set consistent to false so that the subsequent call to
                            // InternalDispose below will rollback this.expectedCurrent.
                            _complete = false;
                        }
                    }
                }
                successful = true;
            }
            finally
            {
                if (!successful)
                {
                    PopScope();
                }
            }

            // No try..catch here.  Just let any exception thrown by InternalDispose go out.
            InternalDispose();

            if (null != exToThrow)
            {
                throw exToThrow;
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        private void InternalDispose()
        {
            // Set this if it is called internally.
            _disposed = true;

            try
            {
                PopScope();

                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (null == _expectedCurrent)
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.TransactionScopeDisposed(TransactionTraceIdentifier.Empty);
                    }
                }
                else
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.TransactionScopeDisposed(_expectedCurrent.TransactionTraceId);
                    }
                }

                // If Transaction.Current is not null, we have work to do.  Otherwise, we don't, except to replace
                // the previous value.
                if (null != _expectedCurrent)
                {
                    if (!_complete)
                    {
                        if (etwLog.IsEnabled())
                        {
                            etwLog.TransactionScopeIncomplete(_expectedCurrent.TransactionTraceId);
                        }

                        //
                        // Note: Rollback is not called on expected current because someone could conceiveably
                        //       dispose expectedCurrent out from under the transaction scope.
                        //
                        Transaction rollbackTransaction = _committableTransaction;
                        if (rollbackTransaction == null)
                        {
                            rollbackTransaction = _dependentTransaction;
                        }
                        Debug.Assert(rollbackTransaction != null);
                        rollbackTransaction.Rollback();
                    }
                    else
                    {
                        // If we are supposed to commit on dispose, cast to CommittableTransaction and commit it.
                        if (null != _committableTransaction)
                        {
                            _committableTransaction.Commit();
                        }
                        else
                        {
                            Debug.Assert(null != _dependentTransaction, "null != this.dependentTransaction");
                            _dependentTransaction.Complete();
                        }
                    }
                }
            }
            finally
            {
                if (null != _scopeTimer)
                {
                    _scopeTimer.Dispose();
                }

                if (null != _committableTransaction)
                {
                    _committableTransaction.Dispose();

                    // If we created the committable transaction then we placed a clone in expectedCurrent
                    // and it needs to be disposed as well.
                    _expectedCurrent.Dispose();
                }

                if (null != _dependentTransaction)
                {
                    _dependentTransaction.Dispose();
                }
            }
        }

        public void Complete()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, this);
            }
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionScope));
            }

            if (_complete)
            {
                throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, SR.DisposeScope, null);
            }

            _complete = true;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, this);
            }
        }

        private static void TimerCallback(object state)
        {
            TransactionScope scope = state as TransactionScope;
            if (null == scope)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeInternalError("TransactionScopeTimerObjectInvalid");
                }

                throw TransactionException.Create(TraceSourceType.TraceSourceBase, SR.InternalError + SR.TransactionScopeTimerObjectInvalid, null);
            }

            scope.Timeout();
        }

        private void Timeout()
        {
            if ((!_complete) && (null != _expectedCurrent))
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionScopeTimeout(_expectedCurrent.TransactionTraceId);
                }
                try
                {
                    _expectedCurrent.Rollback();
                }
                catch (ObjectDisposedException ex)
                {
                    // Tolerate the fact that the transaction has already been disposed.
                    if (etwLog.IsEnabled())
                    {
                        etwLog.ExceptionConsumed(TraceSourceType.TraceSourceBase, ex);
                    }
                }
                catch (TransactionException txEx)
                {
                    // Tolerate transaction exceptions
                    if (etwLog.IsEnabled())
                    {
                        etwLog.ExceptionConsumed(TraceSourceType.TraceSourceBase, txEx);
                    }
                }
            }
        }

        private void CommonInitialize()
        {
            ContextKey = new ContextKey();
            _complete = false;
            _dependentTransaction = null;
            _disposed = false;
            _committableTransaction = null;
            _expectedCurrent = null;
            _scopeTimer = null;
            _scopeThread = Thread.CurrentThread;

            Transaction.GetCurrentTransactionAndScope(
                            AsyncFlowEnabled ? TxLookup.DefaultCallContext : TxLookup.DefaultTLS,
                            out _savedCurrent,
                            out _savedCurrentScope,
                            out _contextTransaction
                            );

            // Calling validate here as we need to make sure the existing parent ambient transaction scope is already looked up to see if we have ES interop enabled.  
            ValidateAsyncFlowOptionAndESInteropOption();
        }

        // PushScope
        //
        // Push a transaction scope onto the stack.
        private void PushScope()
        {
            // Fixup the interop mode before we set current.
            if (!_interopModeSpecified)
            {
                // Transaction.InteropMode will take the interop mode on
                // for the scope in currentScope into account.
                _interopOption = Transaction.InteropMode(_savedCurrentScope);
            }

            // async function yield at await points and main thread can continue execution. We need to make sure the TLS data are restored appropriately.
            SaveTLSContextData();

            if (AsyncFlowEnabled)
            {
                // Async Flow is enabled and CallContext will be used for ambient transaction. 
                _threadContextData = CallContextCurrentData.CreateOrGetCurrentData(ContextKey);

                if (_savedCurrentScope == null && _savedCurrent == null)
                {
                    // Clear TLS data so that transaction doesn't leak from current thread.
                    ContextData.TLSCurrentData = null;
                }
            }
            else
            {
                // Legacy TransactionScope. Use TLS to track ambient transaction context.
                _threadContextData = ContextData.TLSCurrentData;
                CallContextCurrentData.ClearCurrentData(ContextKey, false);
            }

            // This call needs to be done first
            SetCurrent(_expectedCurrent);
            _threadContextData.CurrentScope = this;
        }

        // PopScope
        //
        // Pop the current transaction scope off the top of the stack
        private void PopScope()
        {
            bool shouldRestoreContextData = true;

            // Clear the current TransactionScope CallContext data
            if (AsyncFlowEnabled)
            {
                CallContextCurrentData.ClearCurrentData(ContextKey, true);
            }

            if (_scopeThread == Thread.CurrentThread)
            {
                // async function yield at await points and main thread can continue execution. We need to make sure the TLS data are restored appropriately.
                // Restore the TLS only if the thread Ids match.
                RestoreSavedTLSContextData();
            }

            // Restore threadContextData to parent CallContext or TLS data
            if (_savedCurrentScope != null)
            {
                if (_savedCurrentScope.AsyncFlowEnabled)
                {
                    _threadContextData = CallContextCurrentData.CreateOrGetCurrentData(_savedCurrentScope.ContextKey);
                }
                else
                {
                    if (_savedCurrentScope._scopeThread != Thread.CurrentThread)
                    {
                        // Clear TLS data so that transaction doesn't leak from current thread.
                        shouldRestoreContextData = false;
                        ContextData.TLSCurrentData = null;
                    }
                    else
                    {
                        _threadContextData = ContextData.TLSCurrentData;
                    }

                    CallContextCurrentData.ClearCurrentData(_savedCurrentScope.ContextKey, false);
                }
            }
            else
            {
                // No parent TransactionScope present

                // Clear any CallContext data
                CallContextCurrentData.ClearCurrentData(null, false);

                if (_scopeThread != Thread.CurrentThread)
                {
                    // Clear TLS data so that transaction doesn't leak from current thread.
                    shouldRestoreContextData = false;
                    ContextData.TLSCurrentData = null;
                }
                else
                {
                    // Restore the current data to TLS.
                    ContextData.TLSCurrentData = _threadContextData;
                }
            }

            // prevent restoring the context in an unexpected thread due to thread switch during TransactionScope's Dispose 
            if (shouldRestoreContextData)
            {
                _threadContextData.CurrentScope = _savedCurrentScope;
                RestoreCurrent();
            }
        }

        // SetCurrent
        //
        // Place the given value in current by whatever means necessary for interop mode.
        private void SetCurrent(Transaction newCurrent)
        {
            // Keep a dependent clone of current if we don't have one and we are not committable
            if (_dependentTransaction == null && _committableTransaction == null)
            {
                if (newCurrent != null)
                {
                    _dependentTransaction = newCurrent.DependentClone(DependentCloneOption.RollbackIfNotComplete);
                }
            }

            switch (_interopOption)
            {
                case EnterpriseServicesInteropOption.None:
                    _threadContextData.CurrentTransaction = newCurrent;
                    break;

                case EnterpriseServicesInteropOption.Automatic:
                    EnterpriseServices.VerifyEnterpriseServicesOk();
                    if (EnterpriseServices.UseServiceDomainForCurrent())
                    {
                        EnterpriseServices.PushServiceDomain(newCurrent);
                    }
                    else
                    {
                        _threadContextData.CurrentTransaction = newCurrent;
                    }
                    break;

                case EnterpriseServicesInteropOption.Full:
                    EnterpriseServices.VerifyEnterpriseServicesOk();
                    EnterpriseServices.PushServiceDomain(newCurrent);
                    break;
            }
        }

        private void SaveTLSContextData()
        {
            if (_savedTLSContextData == null)
            {
                _savedTLSContextData = new ContextData(false);
            }

            _savedTLSContextData.CurrentScope = ContextData.TLSCurrentData.CurrentScope;
            _savedTLSContextData.CurrentTransaction = ContextData.TLSCurrentData.CurrentTransaction;
            _savedTLSContextData.DefaultComContextState = ContextData.TLSCurrentData.DefaultComContextState;
            _savedTLSContextData.WeakDefaultComContext = ContextData.TLSCurrentData.WeakDefaultComContext;
        }

        private void RestoreSavedTLSContextData()
        {
            if (_savedTLSContextData != null)
            {
                ContextData.TLSCurrentData.CurrentScope = _savedTLSContextData.CurrentScope;
                ContextData.TLSCurrentData.CurrentTransaction = _savedTLSContextData.CurrentTransaction;
                ContextData.TLSCurrentData.DefaultComContextState = _savedTLSContextData.DefaultComContextState;
                ContextData.TLSCurrentData.WeakDefaultComContext = _savedTLSContextData.WeakDefaultComContext;
            }
        }

        // RestoreCurrent
        //
        // Restore current to it's previous value depending on how it was changed for this scope.
        private void RestoreCurrent()
        {
            if (EnterpriseServices.CreatedServiceDomain)
            {
                EnterpriseServices.LeaveServiceDomain();
            }

            // Only restore the value that was actually in the context.
            _threadContextData.CurrentTransaction = _contextTransaction;
        }


        // ValidateInteropOption
        //
        // Validate a given interop Option
        private void ValidateInteropOption(EnterpriseServicesInteropOption interopOption)
        {
            if (interopOption < EnterpriseServicesInteropOption.None || interopOption > EnterpriseServicesInteropOption.Full)
            {
                throw new ArgumentOutOfRangeException(nameof(interopOption));
            }
        }


        // ValidateScopeTimeout
        //
        // Scope timeouts are not governed by MaxTimeout and therefore need a special validate function
        private void ValidateScopeTimeout(string paramName, TimeSpan scopeTimeout)
        {
            if (scopeTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        private void ValidateAndSetAsyncFlowOption(TransactionScopeAsyncFlowOption asyncFlowOption)
        {
            if (asyncFlowOption < TransactionScopeAsyncFlowOption.Suppress || asyncFlowOption > TransactionScopeAsyncFlowOption.Enabled)
            {
                throw new ArgumentOutOfRangeException(nameof(asyncFlowOption));
            }

            if (asyncFlowOption == TransactionScopeAsyncFlowOption.Enabled)
            {
                AsyncFlowEnabled = true;
            }
        }

        // The validate method assumes that the existing parent ambient transaction scope is already looked up.  
        private void ValidateAsyncFlowOptionAndESInteropOption()
        {
            if (AsyncFlowEnabled)
            {
                EnterpriseServicesInteropOption currentInteropOption = _interopOption;
                if (!_interopModeSpecified)
                {
                    // Transaction.InteropMode will take the interop mode on
                    // for the scope in currentScope into account.
                    currentInteropOption = Transaction.InteropMode(_savedCurrentScope);
                }

                if (currentInteropOption != EnterpriseServicesInteropOption.None)
                {
                    throw new NotSupportedException(SR.AsyncFlowAndESInteropNotSupported);
                }
            }
        }

        // Denotes the action to take when the scope is disposed.
        private bool _complete;
        internal bool ScopeComplete
        {
            get
            {
                return _complete;
            }
        }

        // Storage location for the previous current transaction.
        private Transaction _savedCurrent;

        // To ensure that we don't restore a value for current that was
        // returned to us by an external entity keep the value that was actually
        // in TLS when the scope was created.
        private Transaction _contextTransaction;

        // Storage for the value to restore to current
        private TransactionScope _savedCurrentScope;

        // Store a reference to the context data object for this scope.
        private ContextData _threadContextData;

        private ContextData _savedTLSContextData;

        // Store a reference to the value that this scope expects for current
        private Transaction _expectedCurrent;

        // Store a reference to the committable form of this transaction if 
        // the scope made one.
        private CommittableTransaction _committableTransaction;

        // Store a reference to the scopes transaction guard.
        private DependentTransaction _dependentTransaction;

        // Note when the scope is disposed.
        private bool _disposed;

        // BUGBUG: A shared timer should be used.
        // Individual timer for this scope.
        private Timer _scopeTimer;

        // Store a reference to the thread on which the scope was created so that we can
        // check to make sure that the dispose pattern for scope is being used correctly.
        private Thread _scopeThread;

        // Store the interop mode for this transaction scope.
        private bool _interopModeSpecified = false;
        private EnterpriseServicesInteropOption _interopOption;
        internal EnterpriseServicesInteropOption InteropMode
        {
            get
            {
                return _interopOption;
            }
        }

        internal ContextKey ContextKey
        {
            get;
            private set;
        }

        internal bool AsyncFlowEnabled
        {
            get;
            private set;
        }
    }
}
