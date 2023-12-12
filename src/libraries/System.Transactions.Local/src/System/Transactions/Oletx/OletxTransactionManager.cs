// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxTransactionManager
{
    private readonly IsolationLevel _isolationLevelProperty;

    private readonly TimeSpan _timeoutProperty;

    private TransactionOptions _configuredTransactionOptions = default;

    // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
    private static object? _classSyncObject;

    // These have to be static because we can only add an RM with the proxy once, even if we
    // have multiple OletxTransactionManager instances.
    internal static Hashtable? _resourceManagerHashTable;
    public static ReaderWriterLock ResourceManagerHashTableLock = null!;

    internal static volatile bool ProcessingTmDown;

    internal ReaderWriterLock DtcTransactionManagerLock;
    private readonly DtcTransactionManager _dtcTransactionManager;
    internal OletxInternalResourceManager InternalResourceManager;

    internal static DtcProxyShimFactory ProxyShimFactory = null!; // Late initialization

    // Double-checked locking pattern requires volatile for read/write synchronization
    internal static volatile EventWaitHandle? _shimWaitHandle;
    internal static EventWaitHandle ShimWaitHandle
    {
        get
        {
            if (_shimWaitHandle == null)
            {
                lock (ClassSyncObject)
                {
                    _shimWaitHandle ??= new EventWaitHandle(false, EventResetMode.AutoReset);
                }
            }

            return _shimWaitHandle;
        }
    }

    private readonly string? _nodeNameField;

    internal static void ShimNotificationCallback(object? state, bool timeout)
    {
        // First we need to get the notification from the shim factory.
        object? enlistment2 = null;
        ShimNotificationType shimNotificationType = ShimNotificationType.None;
        bool isSinglePhase;
        bool abortingHint;

        byte[]? prepareInfoBuffer = null;

        bool holdingNotificationLock = false;

        DtcProxyShimFactory localProxyShimFactory;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, $"{nameof(OletxTransactionManager)}.{nameof(ShimNotificationCallback)}");
        }

        // This lock doesn't really protect any of our data.  It is here so that if an exception occurs
        // while calling out to the app, we get an escalation to AppDomainUnload.
        Thread.BeginCriticalRegion();
        try
        {
            do
            {
                // Take a local copy of the proxyShimFactory because if we get an RM TMDown notification,
                // we will still hold the critical section in that factory, but processing of the TMDown will
                // cause replacement of the OletxTransactionManager.proxyShimFactory.
                localProxyShimFactory = ProxyShimFactory;
                try
                {
                    Thread.BeginThreadAffinity();
                    try
                    {
                        localProxyShimFactory.GetNotification(
                            out enlistment2,
                            out shimNotificationType,
                            out isSinglePhase,
                            out abortingHint,
                            out holdingNotificationLock,
                            out prepareInfoBuffer);
                    }
                    finally
                    {
                        if (holdingNotificationLock)
                        {
                            if (enlistment2 is OletxInternalResourceManager)
                            {
                                // In this case we know that the TM has gone down and we need to exchange
                                // the native lock for a managed lock.
                                ProcessingTmDown = true;
                                Monitor.Enter(ProxyShimFactory);
                            }
                            else
                            {
                                holdingNotificationLock = false;
                            }
                            localProxyShimFactory.ReleaseNotificationLock();
                        }
                        Thread.EndThreadAffinity();
                    }

                    // If a TM down is being processed it is possible that the native lock
                    // has been exchanged for a managed lock.  In that case we need to attempt
                    // to take a lock to hold up processing more events until the TM down
                    // processing is complete.
                    if (ProcessingTmDown)
                    {
                        lock (ProxyShimFactory)
                        {
                            // We don't do any work under this lock just make sure that we
                            // can take it.
                        }
                    }

                    if (shimNotificationType != ShimNotificationType.None)
                    {
                        // Next, based on the notification type, cast the Handle accordingly and make
                        // the appropriate call on the enlistment.
                        switch (shimNotificationType)
                        {
                            case ShimNotificationType.Phase0RequestNotify:
                                {
                                    if (enlistment2 is OletxPhase0VolatileEnlistmentContainer ph0VolEnlistContainer)
                                    {
                                        ph0VolEnlistContainer.Phase0Request(abortingHint);
                                    }
                                    else
                                    {
                                        if (enlistment2 is OletxEnlistment oletxEnlistment)
                                        {
                                            oletxEnlistment.Phase0Request(abortingHint);
                                        }
                                        else
                                        {
                                            Environment.FailFast(SR.InternalError);
                                        }
                                    }

                                    break;
                                }

                            case ShimNotificationType.VoteRequestNotify:
                                {
                                    if (enlistment2 is OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer)
                                    {
                                        ph1VolEnlistContainer.VoteRequest();
                                    }
                                    else
                                    {
                                        Environment.FailFast(SR.InternalError);
                                    }

                                    break;
                                }

                            case ShimNotificationType.CommittedNotify:
                                {
                                    if (enlistment2 is OutcomeEnlistment outcomeEnlistment)
                                    {
                                        outcomeEnlistment.Committed();
                                    }
                                    else
                                    {
                                        if (enlistment2 is OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer)
                                        {
                                            ph1VolEnlistContainer.Committed();
                                        }
                                        else
                                        {
                                            Environment.FailFast(SR.InternalError);
                                        }
                                    }

                                    break;
                                }

                            case ShimNotificationType.AbortedNotify:
                                {
                                    if (enlistment2 is OutcomeEnlistment outcomeEnlistment)
                                    {
                                        outcomeEnlistment.Aborted();
                                    }
                                    else
                                    {
                                        if (enlistment2 is OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer)
                                        {
                                            ph1VolEnlistContainer.Aborted();
                                        }
                                        // else
                                        // Voters may receive notifications even
                                        // in cases where they therwise respond
                                        // negatively to the vote request.  It is
                                        // also not guaranteed that we will get a
                                        // notification if we do respond negatively.
                                        // The only safe thing to do is to free the
                                        // Handle when we abort the transaction
                                        // with a voter.  These two things together
                                        // mean that we cannot guarantee that this
                                        // Handle will be alive when we get this
                                        // notification.
                                    }

                                    break;
                                }

                            case ShimNotificationType.InDoubtNotify:
                                {
                                    if (enlistment2 is OutcomeEnlistment outcomeEnlistment)
                                    {
                                        outcomeEnlistment.InDoubt();
                                    }
                                    else
                                    {
                                        if (enlistment2 is OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer)
                                        {
                                            ph1VolEnlistContainer.InDoubt();
                                        }
                                        else
                                        {
                                            Environment.FailFast(SR.InternalError);
                                        }
                                    }

                                    break;
                                }

                            case ShimNotificationType.PrepareRequestNotify:
                                {
                                    bool enlistmentDone = true;

                                    if (enlistment2 is OletxEnlistment enlistment)
                                    {
                                        enlistmentDone = enlistment.PrepareRequest(isSinglePhase, prepareInfoBuffer!);
                                    }
                                    else
                                    {
                                        Environment.FailFast(SR.InternalError);
                                    }

                                    break;
                                }

                            case ShimNotificationType.CommitRequestNotify:
                                {
                                    if (enlistment2 is OletxEnlistment enlistment)
                                    {
                                        enlistment.CommitRequest();
                                    }
                                    else
                                    {
                                        Environment.FailFast(SR.InternalError);
                                    }

                                    break;
                                }

                            case ShimNotificationType.AbortRequestNotify:
                                {
                                    if (enlistment2 is OletxEnlistment enlistment)
                                    {
                                        enlistment.AbortRequest();
                                    }
                                    else
                                    {
                                        Environment.FailFast(SR.InternalError);
                                    }

                                    break;
                                }

                            case ShimNotificationType.EnlistmentTmDownNotify:
                                {
                                    if (enlistment2 is OletxEnlistment enlistment)
                                    {
                                        enlistment.TMDown();
                                    }
                                    else
                                    {
                                        Environment.FailFast(SR.InternalError);
                                    }

                                    break;
                                }

                            case ShimNotificationType.ResourceManagerTmDownNotify:
                                {
                                    switch (enlistment2)
                                    {
                                        case OletxResourceManager resourceManager:
                                            resourceManager.TMDown();
                                            break;

                                        case OletxInternalResourceManager internalResourceManager:
                                            internalResourceManager.TMDown();
                                            break;

                                        default:
                                            Environment.FailFast(SR.InternalError);
                                            break;
                                    }

                                    break;
                                }

                            default:
                                {
                                    Environment.FailFast(SR.InternalError);
                                    break;
                                }
                        }
                    }
                }
                finally
                {
                    if (holdingNotificationLock)
                    {
                        holdingNotificationLock = false;
                        ProcessingTmDown = false;
                        Monitor.Exit(ProxyShimFactory);
                    }
                }
            }
            while (shimNotificationType != ShimNotificationType.None);
        }
        finally
        {
            if (holdingNotificationLock)
            {
                holdingNotificationLock = false;
                ProcessingTmDown = false;
                Monitor.Exit(ProxyShimFactory);
            }

            Thread.EndCriticalRegion();
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, $"{nameof(OletxTransactionManager)}.{nameof(ShimNotificationCallback)}");
        }
    }

    internal OletxTransactionManager(string nodeName)
    {
        lock (ClassSyncObject)
        {
            // If we have not already initialized the shim factory and started the notification
            // thread, do so now.
            if (ProxyShimFactory == null)
            {
                ProxyShimFactory = new DtcProxyShimFactory(ShimWaitHandle);

                ThreadPool.UnsafeRegisterWaitForSingleObject(
                    ShimWaitHandle,
                    ShimNotificationCallback,
                    null,
                    -1,
                    false);
            }
        }

        DtcTransactionManagerLock = new ReaderWriterLock();

        _nodeNameField = nodeName;

        // The DTC proxy doesn't like an empty string for node name on 64-bit platforms when
        // running as WOW64.  It treats any non-null node name as a "remote" node and turns off
        // the WOW64 bit, causing problems when reading the registry.  So if we got on empty
        // string for the node name, just treat it as null.
        if (_nodeNameField is { Length: 0 })
        {
            _nodeNameField = null;
        }

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.OleTxTransactionManagerCreate(GetType(), _nodeNameField);
        }

        // Initialize the properties from config.
        _configuredTransactionOptions.IsolationLevel = _isolationLevelProperty = TransactionManager.DefaultIsolationLevel;
        _configuredTransactionOptions.Timeout = _timeoutProperty = TransactionManager.DefaultTimeout;

        InternalResourceManager = new OletxInternalResourceManager(this);

        DtcTransactionManagerLock.AcquireWriterLock(-1);
        try
        {
            _dtcTransactionManager = new DtcTransactionManager(_nodeNameField, this);
        }
        finally
        {
            DtcTransactionManagerLock.ReleaseWriterLock();
        }

        if (_resourceManagerHashTable == null)
        {
            _resourceManagerHashTable = new Hashtable(2);
            ResourceManagerHashTableLock = new ReaderWriterLock();
        }
    }

    internal OletxCommittableTransaction CreateTransaction(TransactionOptions properties)
    {
        OletxCommittableTransaction tx;
        RealOletxTransaction realTransaction;
        TransactionShim? transactionShim = null;
        Guid txIdentifier = Guid.Empty;
        OutcomeEnlistment outcomeEnlistment;

        TransactionManager.ValidateIsolationLevel(properties.IsolationLevel);

        // Never create a transaction with an IsolationLevel of Unspecified.
        if (IsolationLevel.Unspecified == properties.IsolationLevel)
        {
            properties.IsolationLevel = _configuredTransactionOptions.IsolationLevel;
        }

        properties.Timeout = TransactionManager.ValidateTimeout(properties.Timeout);

        DtcTransactionManagerLock.AcquireReaderLock(-1);
        try
        {
            OletxTransactionIsolationLevel oletxIsoLevel = ConvertIsolationLevel(properties.IsolationLevel);
            uint oletxTimeout = DtcTransactionManager.AdjustTimeout(properties.Timeout);

            outcomeEnlistment = new OutcomeEnlistment();
            try
            {
                _dtcTransactionManager.ProxyShimFactory.BeginTransaction(
                    oletxTimeout,
                    oletxIsoLevel,
                    outcomeEnlistment,
                    out txIdentifier,
                    out transactionShim);
            }
            catch (COMException ex)
            {
                ProxyException(ex);
                throw;
            }

            realTransaction = new RealOletxTransaction(
                this,
                transactionShim,
                outcomeEnlistment,
                txIdentifier,
                oletxIsoLevel);
            tx = new OletxCommittableTransaction(realTransaction);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionCreated(TraceSourceType.TraceSourceOleTx, tx.TransactionTraceId, "OletxTransaction");
            }
        }
        finally
        {
            DtcTransactionManagerLock.ReleaseReaderLock();
        }

        return tx;
    }

    internal OletxEnlistment ReenlistTransaction(
        Guid resourceManagerIdentifier,
        byte[] recoveryInformation,
        IEnlistmentNotificationInternal enlistmentNotification)
    {
        ArgumentNullException.ThrowIfNull(recoveryInformation);
        ArgumentNullException.ThrowIfNull(enlistmentNotification);

        // Now go find the resource manager in the collection.
        OletxResourceManager oletxResourceManager = RegisterResourceManager(resourceManagerIdentifier);
        if (oletxResourceManager == null)
        {
            throw new ArgumentException(SR.InvalidArgument, nameof(resourceManagerIdentifier));
        }

        if (oletxResourceManager.RecoveryCompleteCalledByApplication)
        {
            throw new InvalidOperationException(SR.ReenlistAfterRecoveryComplete);
        }

        // Now ask the resource manager to reenlist.
        OletxEnlistment returnValue = oletxResourceManager.Reenlist(recoveryInformation, enlistmentNotification);

        return returnValue;
    }

    internal void ResourceManagerRecoveryComplete(Guid resourceManagerIdentifier)
    {
        OletxResourceManager oletxRm = RegisterResourceManager(resourceManagerIdentifier);

        if (oletxRm.RecoveryCompleteCalledByApplication)
        {
            throw new InvalidOperationException(SR.DuplicateRecoveryComplete);
        }

        oletxRm.RecoveryComplete();
    }

    internal OletxResourceManager RegisterResourceManager(Guid resourceManagerIdentifier)
    {
        OletxResourceManager? oletxResourceManager;

        ResourceManagerHashTableLock.AcquireWriterLock(-1);

        try
        {
            // If this resource manager has already been registered, don't register it again.
            oletxResourceManager = _resourceManagerHashTable![resourceManagerIdentifier] as OletxResourceManager;
            if (oletxResourceManager != null)
            {
                return oletxResourceManager;
            }

            oletxResourceManager = new OletxResourceManager(this, resourceManagerIdentifier);

            _resourceManagerHashTable.Add(resourceManagerIdentifier, oletxResourceManager);
        }
        finally
        {
            ResourceManagerHashTableLock.ReleaseWriterLock();
        }

        return oletxResourceManager;
    }

    internal string? CreationNodeName
        => _nodeNameField;

    internal OletxResourceManager FindOrRegisterResourceManager(Guid resourceManagerIdentifier)
    {
        if (resourceManagerIdentifier == Guid.Empty)
        {
            throw new ArgumentException(SR.BadResourceManagerId, nameof(resourceManagerIdentifier));
        }

        OletxResourceManager? oletxResourceManager;

        ResourceManagerHashTableLock.AcquireReaderLock(-1);
        try
        {
            oletxResourceManager = _resourceManagerHashTable![resourceManagerIdentifier] as OletxResourceManager;
        }
        finally
        {
            ResourceManagerHashTableLock.ReleaseReaderLock();
        }

        if (oletxResourceManager == null)
        {
            return RegisterResourceManager(resourceManagerIdentifier);
        }

        return oletxResourceManager;
    }

    internal DtcTransactionManager DtcTransactionManager
    {
        get
        {
            if (DtcTransactionManagerLock.IsReaderLockHeld || DtcTransactionManagerLock.IsWriterLockHeld)
            {
                if (_dtcTransactionManager == null)
                {
                    throw TransactionException.Create(SR.DtcTransactionManagerUnavailable, null);
                }

                return _dtcTransactionManager;
            }

            // Internal programming error.  A reader or writer lock should be held when this property is invoked.
            throw TransactionException.Create(SR.InternalError, null);
        }
    }

    internal string? NodeName
        => _nodeNameField;

    internal static void ProxyException(COMException comException)
    {
        if (comException.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
            comException.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
        {
            throw TransactionManagerCommunicationException.Create(
                SR.TransactionManagerCommunicationException,
                comException);
        }
        if (comException.ErrorCode == OletxHelper.XACT_E_NETWORK_TX_DISABLED)
        {
            throw TransactionManagerCommunicationException.Create(
                SR.NetworkTransactionsDisabled,
                comException);
        }
        // Else if the error is a transaction oriented error, throw a TransactionException
        if (comException.ErrorCode >= OletxHelper.XACT_E_FIRST &&
            comException.ErrorCode <= OletxHelper.XACT_E_LAST)
        {
            // Special casing XACT_E_NOTRANSACTION
            throw TransactionException.Create(
                OletxHelper.XACT_E_NOTRANSACTION == comException.ErrorCode
                    ? SR.TransactionAlreadyOver
                    : comException.Message,
                comException);
        }
    }

    internal void ReinitializeProxy()
    {
        // This is created by the static constructor.
        DtcTransactionManagerLock.AcquireWriterLock(-1);

        try
        {
            _dtcTransactionManager?.ReleaseProxy();
        }
        finally
        {
            DtcTransactionManagerLock.ReleaseWriterLock();
        }
    }

    internal static OletxTransactionIsolationLevel ConvertIsolationLevel(IsolationLevel isolationLevel)
        => isolationLevel switch
        {
            IsolationLevel.Serializable => OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE,
            IsolationLevel.RepeatableRead => OletxTransactionIsolationLevel.ISOLATIONLEVEL_REPEATABLEREAD,
            IsolationLevel.ReadCommitted => OletxTransactionIsolationLevel.ISOLATIONLEVEL_READCOMMITTED,
            IsolationLevel.ReadUncommitted => OletxTransactionIsolationLevel.ISOLATIONLEVEL_READUNCOMMITTED,
            IsolationLevel.Chaos => OletxTransactionIsolationLevel.ISOLATIONLEVEL_CHAOS,
            IsolationLevel.Unspecified => OletxTransactionIsolationLevel.ISOLATIONLEVEL_UNSPECIFIED,
            _ => OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE
        };

    internal static IsolationLevel ConvertIsolationLevelFromProxyValue(OletxTransactionIsolationLevel proxyIsolationLevel)
        => proxyIsolationLevel switch
        {
            OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE => IsolationLevel.Serializable,
            OletxTransactionIsolationLevel.ISOLATIONLEVEL_REPEATABLEREAD => IsolationLevel.RepeatableRead,
            OletxTransactionIsolationLevel.ISOLATIONLEVEL_READCOMMITTED => IsolationLevel.ReadCommitted,
            OletxTransactionIsolationLevel.ISOLATIONLEVEL_READUNCOMMITTED => IsolationLevel.ReadUncommitted,
            OletxTransactionIsolationLevel.ISOLATIONLEVEL_UNSPECIFIED => IsolationLevel.Unspecified,
            OletxTransactionIsolationLevel.ISOLATIONLEVEL_CHAOS => IsolationLevel.Chaos,
            _ => IsolationLevel.Serializable
        };

    // Helper object for static synchronization
    internal static object ClassSyncObject
    {
        get
        {
            if (_classSyncObject == null)
            {
                object o = new();
                Interlocked.CompareExchange(ref _classSyncObject, o, null);
            }

            return _classSyncObject;
        }
    }
}

internal sealed class OletxInternalResourceManager
{
    private readonly OletxTransactionManager _oletxTm;

    internal Guid Identifier { get; }

    internal ResourceManagerShim? ResourceManagerShim;

    internal OletxInternalResourceManager(OletxTransactionManager oletxTm)
    {
        _oletxTm = oletxTm;
        Identifier = Guid.NewGuid();
    }

    public void TMDown()
    {
        // Let's set ourselves up for reinitialization with the proxy by releasing our
        // reference to the resource manager shim, which will release its reference
        // to the proxy when it destructs.
        ResourceManagerShim = null;

        // We need to look through all the transactions and tell them about
        // the TMDown so they can tell their Phase0VolatileEnlistmentContainers.
        Transaction? tx;
        RealOletxTransaction realTx;
        IDictionaryEnumerator tableEnum;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxInternalResourceManager)}.{nameof(TMDown)}");
        }

        // make a local copy of the hash table to avoid possible deadlocks when we lock both the global hash table
        // and the transaction object.
        Hashtable txHashTable;
        lock (TransactionManager.PromotedTransactionTable.SyncRoot)
        {
            txHashTable = (Hashtable)TransactionManager.PromotedTransactionTable.Clone();
        }

        // No need to lock my hashtable, nobody is going to change it.
        tableEnum = txHashTable.GetEnumerator();
        while (tableEnum.MoveNext())
        {
            WeakReference? txWeakRef = (WeakReference?)tableEnum.Value;
            if (txWeakRef != null)
            {
                tx = (Transaction?)txWeakRef.Target;
                if (tx != null)
                {
                    realTx = tx._internalTransaction.PromotedTransaction!.RealOletxTransaction;
                    // Only deal with transactions owned by my OletxTm.
                    if (realTx.OletxTransactionManagerInstance == _oletxTm)
                    {
                        realTx.TMDown();
                    }
                }
            }
        }

        // Now make a local copy of the hash table of resource managers and tell each of them.  This is to
        // deal with Durable EDPR=true (phase0) enlistments.  Each RM will also get a TMDown, but it will
        // come AFTER the "buggy" Phase0Request with abortHint=true - COMPlus bug 36760/36758.
        Hashtable? rmHashTable = null;
        if (OletxTransactionManager._resourceManagerHashTable != null)
        {
            OletxTransactionManager.ResourceManagerHashTableLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                rmHashTable = (Hashtable)OletxTransactionManager._resourceManagerHashTable.Clone();
            }
            finally
            {
                OletxTransactionManager.ResourceManagerHashTableLock.ReleaseReaderLock();
            }
        }

        if (rmHashTable != null)
        {
            // No need to lock my hashtable, nobody is going to change it.
            tableEnum = rmHashTable.GetEnumerator();
            while (tableEnum.MoveNext())
            {
                OletxResourceManager? oletxRM = (OletxResourceManager?)tableEnum.Value;
                // When the RM spins through its enlistments, it will need to make sure that
                // the enlistment is for this particular TM.
                oletxRM?.TMDownFromInternalRM(_oletxTm);
            }
        }

        // Now let's reinitialize the shim.
        _oletxTm.DtcTransactionManagerLock.AcquireWriterLock(-1);
        try
        {
            _oletxTm.ReinitializeProxy();
        }
        finally
        {
            _oletxTm.DtcTransactionManagerLock.ReleaseWriterLock();
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxInternalResourceManager)}.{nameof(TMDown)}");
        }
    }

    internal void CallReenlistComplete()
        => ResourceManagerShim!.ReenlistComplete();
}
