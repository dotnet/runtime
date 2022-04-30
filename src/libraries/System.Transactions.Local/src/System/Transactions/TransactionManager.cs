// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Transactions.Configuration;
using System.Transactions.Distributed;

namespace System.Transactions
{
    public delegate Transaction? HostCurrentTransactionCallback();

    public delegate void TransactionStartedEventHandler(object? sender, TransactionEventArgs e);

    public static class TransactionManager
    {
        // Revovery Information Version
        private const int RecoveryInformationVersion1 = 1;

        // Hashtable of promoted transactions, keyed by identifier guid.  This is used by
        // FindPromotedTransaction to support transaction equivalence when a transaction is
        // serialized and then deserialized back in this app-domain.
        private static Hashtable? s_promotedTransactionTable;

        // Sorted Table of transaction timeouts
        private static TransactionTable? s_transactionTable;

        private static TransactionStartedEventHandler? s_distributedTransactionStartedDelegate;
        public static event TransactionStartedEventHandler? DistributedTransactionStarted
        {
            add
            {
                lock (ClassSyncObject)
                {
                    s_distributedTransactionStartedDelegate = (TransactionStartedEventHandler?)System.Delegate.Combine(s_distributedTransactionStartedDelegate, value);
                    if (value != null)
                    {
                        ProcessExistingTransactions(value);
                    }
                }
            }

            remove
            {
                lock (ClassSyncObject)
                {
                    s_distributedTransactionStartedDelegate = (TransactionStartedEventHandler?)System.Delegate.Remove(s_distributedTransactionStartedDelegate, value);
                }
            }
        }

        internal static void ProcessExistingTransactions(TransactionStartedEventHandler eventHandler)
        {
            lock (PromotedTransactionTable)
            {
                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                IDictionaryEnumerator e = PromotedTransactionTable.GetEnumerator();
                while (e.MoveNext())
                {
                    WeakReference weakRef = (WeakReference)e.Value!;

                    if (weakRef.Target is Transaction tx)
                    {
                        TransactionEventArgs args = new TransactionEventArgs();
                        args._transaction = tx.InternalClone();
                        eventHandler(args._transaction, args);
                    }
                }
            }
        }

        internal static void FireDistributedTransactionStarted(Transaction transaction)
        {
            TransactionStartedEventHandler? localStartedEventHandler = null;
            lock (ClassSyncObject)
            {
                localStartedEventHandler = s_distributedTransactionStartedDelegate;
            }

            if (null != localStartedEventHandler)
            {
                TransactionEventArgs args = new TransactionEventArgs();
                args._transaction = transaction.InternalClone();
                localStartedEventHandler(args._transaction, args);
            }
        }

        // Data storage for current delegate
        internal static HostCurrentTransactionCallback? s_currentDelegate;
        internal static bool s_currentDelegateSet;

        // CurrentDelegate
        //
        // Store a delegate to be used to query for an external current transaction.
        [DisallowNull]
        public static HostCurrentTransactionCallback? HostCurrentCallback
        {
            // get_HostCurrentCallback is used from get_CurrentTransaction, which doesn't have any permission requirements.
            // We don't expose what is returned from this property in that case.  But we don't want just anybody being able
            // to retrieve the value.
            get
            {
                // Note do not add trace notifications to this method.  It is called
                // at the startup of SQLCLR and tracing has too much working set overhead.
                return s_currentDelegate;
            }
            set
            {
                // Note do not add trace notifications to this method.  It is called
                // at the startup of SQLCLR and tracing has too much working set overhead.
                ArgumentNullException.ThrowIfNull(value);

                lock (ClassSyncObject)
                {
                    if (s_currentDelegateSet)
                    {
                        throw new InvalidOperationException(SR.CurrentDelegateSet);
                    }
                    s_currentDelegateSet = true;
                }

                s_currentDelegate = value;
            }
        }

        public static Enlistment Reenlist(
            Guid resourceManagerIdentifier,
            byte[] recoveryInformation,
            IEnlistmentNotification enlistmentNotification)
        {
            if (resourceManagerIdentifier == Guid.Empty)
            {
                throw new ArgumentException(SR.BadResourceManagerId, nameof(resourceManagerIdentifier));
            }

            ArgumentNullException.ThrowIfNull(recoveryInformation);

            ArgumentNullException.ThrowIfNull(enlistmentNotification);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.Reenlist");
                etwLog.TransactionManagerReenlist(resourceManagerIdentifier);
            }

            // Put the recovery information into a stream.
            MemoryStream stream = new MemoryStream(recoveryInformation);
            int recoveryInformationVersion;
            string? nodeName;
            byte[]? resourceManagerRecoveryInformation = null;

            try
            {
                BinaryReader reader = new BinaryReader(stream);
                recoveryInformationVersion = reader.ReadInt32();

                if (recoveryInformationVersion == TransactionManager.RecoveryInformationVersion1)
                {
                    nodeName = reader.ReadString();

                    resourceManagerRecoveryInformation = reader.ReadBytes(recoveryInformation.Length - checked((int)stream.Position));
                }
                else
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, nameof(recoveryInformation), string.Empty);
                    }

                    throw new ArgumentException(SR.UnrecognizedRecoveryInformation, nameof(recoveryInformation));
                }
            }
            catch (EndOfStreamException e)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, nameof(recoveryInformation), e.ToString());
                }
                throw new ArgumentException(SR.UnrecognizedRecoveryInformation, nameof(recoveryInformation), e);
            }
            catch (FormatException e)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, nameof(recoveryInformation), e.ToString());
                }
                throw new ArgumentException(SR.UnrecognizedRecoveryInformation, nameof(recoveryInformation), e);
            }
            finally
            {
                stream.Dispose();
            }

            // Now ask the Transaction Manager to reenlist.
            object syncRoot = new object();
            Enlistment returnValue = new Enlistment(enlistmentNotification, syncRoot);
            EnlistmentState.EnlistmentStatePromoted.EnterState(returnValue.InternalEnlistment);

            returnValue.InternalEnlistment.PromotedEnlistment =
                DistributedTransactionManager.ReenlistTransaction(
                    resourceManagerIdentifier,
                    resourceManagerRecoveryInformation,
                    (RecoveringInternalEnlistment)returnValue.InternalEnlistment
                    );

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.Reenlist");
            }

            return returnValue;
        }


        private static DistributedTransactionManager CheckTransactionManager(string? nodeName)
        {
            DistributedTransactionManager tm = DistributedTransactionManager;
            if (!((tm.NodeName == null && (nodeName == null || nodeName.Length == 0)) ||
                  (tm.NodeName != null && tm.NodeName.Equals(nodeName))))
            {
                throw new ArgumentException(SR.InvalidRecoveryInformation, "recoveryInformation");
            }
            return tm;
        }

        public static void RecoveryComplete(Guid resourceManagerIdentifier)
        {
            if (resourceManagerIdentifier == Guid.Empty)
            {
                throw new ArgumentException(SR.BadResourceManagerId, nameof(resourceManagerIdentifier));
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.RecoveryComplete");
                etwLog.TransactionManagerRecoveryComplete(resourceManagerIdentifier);
            }

            DistributedTransactionManager.ResourceManagerRecoveryComplete(resourceManagerIdentifier);

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.RecoveryComplete");
            }
        }


        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        private static object? s_classSyncObject;

        // Helper object for static synchronization
        private static object ClassSyncObject => LazyInitializer.EnsureInitialized(ref s_classSyncObject);

        internal static IsolationLevel DefaultIsolationLevel
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultIsolationLevel");
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultIsolationLevel");
                }

                return IsolationLevel.Serializable;
            }
        }


        private static DefaultSettingsSection? s_defaultSettings;
        private static DefaultSettingsSection DefaultSettings
        {
            get
            {
                if (s_defaultSettings == null)
                {
                    s_defaultSettings = DefaultSettingsSection.GetSection();
                }

                return s_defaultSettings;
            }
        }


        private static MachineSettingsSection? s_machineSettings;
        private static MachineSettingsSection MachineSettings
        {
            get
            {
                if (s_machineSettings == null)
                {
                    s_machineSettings = MachineSettingsSection.GetSection();
                }

                return s_machineSettings;
            }
        }

        private static bool s_defaultTimeoutValidated;
        private static TimeSpan s_defaultTimeout;
        public static TimeSpan DefaultTimeout
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultTimeout");
                }

                if (!s_defaultTimeoutValidated)
                {
                    s_defaultTimeout = ValidateTimeout(DefaultSettingsSection.Timeout);
                    // If the timeout value got adjusted, it must have been greater than MaximumTimeout.
                    if (s_defaultTimeout != DefaultSettingsSection.Timeout)
                    {
                        if (etwLog.IsEnabled())
                        {
                            etwLog.ConfiguredDefaultTimeoutAdjusted();
                        }
                    }
                    s_defaultTimeoutValidated = true;
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultTimeout");
                }
                return s_defaultTimeout;
            }
        }


        private static bool s_cachedMaxTimeout;
        private static TimeSpan s_maximumTimeout;
        public static TimeSpan MaximumTimeout
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultMaximumTimeout");
                }

                LazyInitializer.EnsureInitialized(ref s_maximumTimeout, ref s_cachedMaxTimeout, ref s_classSyncObject, () => DefaultSettingsSection.Timeout);

                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultMaximumTimeout");
                }

                return s_maximumTimeout;
            }
        }

        /// <summary>
        /// This static function throws an ArgumentOutOfRange if the specified IsolationLevel is not within
        /// the range of valid values.
        /// </summary>
        /// <param name="transactionIsolationLevel">
        /// The IsolationLevel value to validate.
        /// </param>
        internal static void ValidateIsolationLevel(IsolationLevel transactionIsolationLevel)
        {
            switch (transactionIsolationLevel)
            {
                case IsolationLevel.Serializable:
                case IsolationLevel.RepeatableRead:
                case IsolationLevel.ReadCommitted:
                case IsolationLevel.ReadUncommitted:
                case IsolationLevel.Unspecified:
                case IsolationLevel.Chaos:
                case IsolationLevel.Snapshot:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transactionIsolationLevel));
            }
        }


        /// <summary>
        /// This static function throws an ArgumentOutOfRange if the specified TimeSpan does not meet
        /// requirements of a valid transaction timeout.  Timeout values must be positive.
        /// </summary>
        /// <param name="transactionTimeout">
        /// The TimeSpan value to validate.
        /// </param>
        internal static TimeSpan ValidateTimeout(TimeSpan transactionTimeout)
        {
            if (transactionTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(transactionTimeout));
            }

            if (MaximumTimeout != TimeSpan.Zero)
            {
                if (transactionTimeout > MaximumTimeout || transactionTimeout == TimeSpan.Zero)
                {
                    return MaximumTimeout;
                }
            }

            return transactionTimeout;
        }

        internal static Transaction? FindPromotedTransaction(Guid transactionIdentifier)
        {
            Hashtable promotedTransactionTable = PromotedTransactionTable;
            WeakReference? weakRef = (WeakReference?)promotedTransactionTable[transactionIdentifier];
            if (null != weakRef)
            {
                if (weakRef.Target is Transaction tx)
                {
                    return tx.InternalClone();
                }
                else  // an old, moldy weak reference.  Let's get rid of it.
                {
                    lock (promotedTransactionTable)
                    {
                        promotedTransactionTable.Remove(transactionIdentifier);
                    }
                }
            }

            return null;
        }

        internal static Transaction FindOrCreatePromotedTransaction(Guid transactionIdentifier, DistributedTransaction dtx)
        {
            Transaction? tx = null;
            Hashtable promotedTransactionTable = PromotedTransactionTable;
            lock (promotedTransactionTable)
            {
                WeakReference? weakRef = (WeakReference?)promotedTransactionTable[transactionIdentifier];
                if (null != weakRef)
                {
                    tx = weakRef.Target as Transaction;
                    if (null != tx)
                    {
                        // If we found a transaction then dispose it
                        return tx.InternalClone();
                    }
                    else
                    {
                        // an old, moldy weak reference.  Let's get rid of it.
                        lock (promotedTransactionTable)
                        {
                            promotedTransactionTable.Remove(transactionIdentifier);
                        }
                    }
                }

                tx = new Transaction(dtx);

                // Since we are adding this reference to the table create an object that will clean that entry up.
                tx._internalTransaction._finalizedObject = new FinalizedObject(tx._internalTransaction, dtx.Identifier);

                weakRef = new WeakReference(tx, false);
                promotedTransactionTable[dtx.Identifier] = weakRef;
            }
            dtx.SavedLtmPromotedTransaction = tx;

            FireDistributedTransactionStarted(tx);

            return tx;
        }

        // Table for promoted transactions
        internal static Hashtable PromotedTransactionTable =>
            LazyInitializer.EnsureInitialized(ref s_promotedTransactionTable, ref s_classSyncObject, () => new Hashtable(100));

        // Table for transaction timeouts
        internal static TransactionTable TransactionTable =>
            LazyInitializer.EnsureInitialized(ref s_transactionTable, ref s_classSyncObject, () => new TransactionTable());

        // Fault in a DistributedTransactionManager if one has not already been created.
        internal static DistributedTransactionManager? distributedTransactionManager;
        internal static DistributedTransactionManager DistributedTransactionManager =>
            // If the distributed transaction manager is not configured, throw an exception
            LazyInitializer.EnsureInitialized(ref distributedTransactionManager, ref s_classSyncObject, () => new DistributedTransactionManager());
    }
}
