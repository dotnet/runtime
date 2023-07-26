// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Transactions.Configuration;
#if WINDOWS
using System.Transactions.DtcProxyShim;
#endif
using System.Transactions.Oletx;

namespace System.Transactions
{
    public delegate Transaction? HostCurrentTransactionCallback();

    public delegate void TransactionStartedEventHandler(object? sender, TransactionEventArgs e);

    public static class TransactionManager
    {
        // Revovery Information Version
        private const int RecoveryInformationVersion1 = 1;
        private const int CurrentRecoveryVersion = RecoveryInformationVersion1;

        // Hashtable of promoted transactions, keyed by identifier guid.  This is used by
        // FindPromotedTransaction to support transaction equivalence when a transaction is
        // serialized and then deserialized back in this app-domain.
        private static Hashtable? s_promotedTransactionTable;

        // Sorted Table of transaction timeouts
        private static TransactionTable? s_transactionTable;

        private static TransactionStartedEventHandler? s_distributedTransactionStartedDelegate;

        internal const string DistributedTransactionTrimmingWarning =
            "Distributed transactions support may not be compatible with trimming. If your program creates a distributed transaction via System.Transactions, the correctness of the application cannot be guaranteed after trimming.";

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


        private static OletxTransactionManager CheckTransactionManager(string? nodeName)
        {
            OletxTransactionManager tm = DistributedTransactionManager;
            if (!((tm.NodeName == null && string.IsNullOrEmpty(nodeName)) ||
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
        private static DefaultSettingsSection DefaultSettings => s_defaultSettings ??= DefaultSettingsSection.GetSection();


        private static MachineSettingsSection? s_machineSettings;
        private static MachineSettingsSection MachineSettings => s_machineSettings ??= MachineSettingsSection.GetSection();

        private static bool s_defaultTimeoutValidated;
        private static long s_defaultTimeoutTicks;
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
                    LazyInitializer.EnsureInitialized(ref s_defaultTimeoutTicks, ref s_defaultTimeoutValidated, ref s_classSyncObject, () => ValidateTimeout(DefaultSettingsSection.Timeout).Ticks);
                    if (Interlocked.Read(ref s_defaultTimeoutTicks) != DefaultSettingsSection.Timeout.Ticks)
                    {
                        if (etwLog.IsEnabled())
                        {
                            etwLog.ConfiguredDefaultTimeoutAdjusted();
                        }
                    }
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultTimeout");
                }
                return new TimeSpan(Interlocked.Read(ref s_defaultTimeoutTicks));
            }
            set
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultTimeout");
                }

                Interlocked.Exchange(ref s_defaultTimeoutTicks, ValidateTimeout(value).Ticks);
                if (Interlocked.Read(ref s_defaultTimeoutTicks) != value.Ticks)
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.ConfiguredDefaultTimeoutAdjusted();
                    }
                }

                s_defaultTimeoutValidated = true;

                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultTimeout");
                }
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

                LazyInitializer.EnsureInitialized(ref s_maximumTimeout, ref s_cachedMaxTimeout, ref s_classSyncObject, () => MachineSettingsSection.MaxTimeout);

                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultMaximumTimeout");
                }

                return s_maximumTimeout;
            }
            set
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultMaximumTimeout");
                }

                ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);

                s_cachedMaxTimeout = true;
                s_maximumTimeout = value;
                LazyInitializer.EnsureInitialized(ref s_defaultTimeoutTicks, ref s_defaultTimeoutValidated, ref s_classSyncObject, () => DefaultSettingsSection.Timeout.Ticks);

                long defaultTimeoutTicks = Interlocked.Read(ref s_defaultTimeoutTicks);
                Interlocked.Exchange(ref s_defaultTimeoutTicks, ValidateTimeout(new TimeSpan(defaultTimeoutTicks)).Ticks);
                if (Interlocked.Read(ref s_defaultTimeoutTicks) != defaultTimeoutTicks)
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.ConfiguredDefaultTimeoutAdjusted();
                    }
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultMaximumTimeout");
                }
            }
        }

        /// <summary>
        /// Controls whether usage of System.Transactions APIs that require escalation to a distributed transaction will do so;
        /// if your application requires distributed transaction, opt into using them by setting this to <see langword="true" />.
        /// If set to <see langword="false" /> (the default), escalation to a distributed transaction will throw a <see cref="NotSupportedException" />.
        /// </summary>
#if WINDOWS
        public static bool ImplicitDistributedTransactions
        {
            get => DtcProxyShimFactory.s_transactionConnector is not null;

            [SupportedOSPlatform("windows")]
            [RequiresUnreferencedCode(DistributedTransactionTrimmingWarning)]
            set
            {
                lock (s_implicitDistributedTransactionsLock)
                {
                    // Make sure this flag can only be set once, and that once distributed transactions have been initialized,
                    // it's frozen.
                    if (s_implicitDistributedTransactions is null)
                    {
                        s_implicitDistributedTransactions = value;

                        if (value)
                        {
                            DtcProxyShimFactory.s_transactionConnector ??= new DtcProxyShimFactory.DtcTransactionConnector();
                        }
                    }
                    else if (value != s_implicitDistributedTransactions)
                    {
                        throw new InvalidOperationException(SR.ImplicitDistributedTransactionsCannotBeChanged);
                    }
                }
            }
        }

        internal static bool? s_implicitDistributedTransactions;
        internal static object s_implicitDistributedTransactionsLock = new();
#else
        public static bool ImplicitDistributedTransactions
        {
            get => false;

            [SupportedOSPlatform("windows")]
            [RequiresUnreferencedCode(DistributedTransactionTrimmingWarning)]
            set
            {
                if (value)
                {
                    throw new PlatformNotSupportedException(SR.DistributedNotSupported);
                }
            }
        }
#endif

        // This routine writes the "header" for the recovery information, based on the
        // type of the calling object and its provided parameter collection.  This information
        // we be read back by the static Reenlist method to create the necessary transaction
        // manager object with the right parameters in order to do a ReenlistTransaction call.
        internal static byte[] GetRecoveryInformation(
            string? startupInfo,
            byte[] resourceManagerRecoveryInformation
        )
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, $"{nameof(TransactionManager)}.{nameof(GetRecoveryInformation)}");
            }

            MemoryStream stream = new MemoryStream();
            byte[]? returnValue = null;

            try
            {
                // Manually write the recovery information
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write(CurrentRecoveryVersion);
                if (startupInfo != null)
                {
                    writer.Write(startupInfo);
                }
                else
                {
                    writer.Write("");
                }
                writer.Write(resourceManagerRecoveryInformation);
                writer.Flush();
                returnValue = stream.ToArray();
            }
            finally
            {
                stream.Close();
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, $"{nameof(TransactionManager)}.{nameof(GetRecoveryInformation)}");
            }

            return returnValue;
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
            ArgumentOutOfRangeException.ThrowIfLessThan(transactionTimeout, TimeSpan.Zero);

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

        internal static Transaction FindOrCreatePromotedTransaction(Guid transactionIdentifier, OletxTransaction dtx)
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
        internal static OletxTransactionManager? distributedTransactionManager;
        internal static OletxTransactionManager DistributedTransactionManager =>
            // If the distributed transaction manager is not configured, throw an exception
            LazyInitializer.EnsureInitialized(ref distributedTransactionManager, ref s_classSyncObject,
                () => new OletxTransactionManager(DefaultSettingsSection.DistributedTransactionManagerName));
    }
}
