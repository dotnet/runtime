// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

internal sealed partial class DtcProxyShimFactory
{
    // Used to synchronize access to the proxy.  This is necessary in
    // initialization because the proxy doesn't like multiple simultaneous callers
    // of GetWhereabouts[Size].  We could have this situation in cases where
    // there are multiple app domains being ititialized in the same process
    // at the same time.
    private static readonly object _proxyInitLock = new();

    // This object will perform the actual distributed transaction connection.
    // It will be set only if TransactionManager.ImplicitDefaultTransactions
    // is set to true, allowing the relevant code to be trimmed otherwise.
    internal static ITransactionConnector? s_transactionConnector;

    // Lock to protect access to listOfNotifications.
    private readonly object _notificationLock = new();

    private readonly ConcurrentQueue<NotificationShimBase> _notifications = new();

    private readonly ConcurrentQueue<ITransactionOptions> _cachedOptions = new();
    private readonly ConcurrentQueue<ITransactionTransmitter> _cachedTransmitters = new();
    private readonly ConcurrentQueue<ITransactionReceiver> _cachedReceivers = new();

    private static readonly int s_maxCachedInterfaces = Environment.ProcessorCount * 2;

    private readonly EventWaitHandle _eventHandle;

    private ITransactionDispenser _transactionDispenser = null!; // Late-initialized in ConnectToProxy

    internal DtcProxyShimFactory(EventWaitHandle notificationEventHandle)
        => _eventHandle = notificationEventHandle;

    // https://learn.microsoft.com/previous-versions/windows/desktop/ms678898(v=vs.85)
    [LibraryImport(Interop.Libraries.Xolehlp, StringMarshalling = StringMarshalling.Utf16)]
    [RequiresUnreferencedCode(TransactionManager.DistributedTransactionTrimmingWarning)]
    private static unsafe partial int DtcGetTransactionManagerExW(
        [MarshalAs(UnmanagedType.LPWStr)] string? pszHost,
        [MarshalAs(UnmanagedType.LPWStr)] string? pszTmName,
        in Guid riid,
        int grfOptions,
        void* pvConfigPararms,
        [MarshalAs(UnmanagedType.Interface)] out ITransactionDispenser ppvObject);

    [RequiresUnreferencedCode(TransactionManager.DistributedTransactionTrimmingWarning)]
    private static unsafe void DtcGetTransactionManager(string? nodeName, out ITransactionDispenser localDispenser) => Marshal.ThrowExceptionForHR(DtcGetTransactionManagerExW(nodeName, null, Guids.IID_ITransactionDispenser_Guid, 0, null, out localDispenser));

    public void ConnectToProxy(
        string? nodeName,
        Guid resourceManagerIdentifier,
        object managedIdentifier,
        out bool nodeNameMatches,
        out byte[] whereabouts,
        out ResourceManagerShim resourceManagerShim)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
        {
            throw new PlatformNotSupportedException(SR.DistributedNotSupportedOn32Bits);
        }

        lock (TransactionManager.s_implicitDistributedTransactionsLock)
        {
            if (s_transactionConnector is null)
            {
                // We set TransactionManager.ImplicitDistributedTransactionsInternal, so that any attempt to change it
                // later will cause an exception.
                TransactionManager.s_implicitDistributedTransactions = false;

                throw new NotSupportedException(SR.ImplicitDistributedTransactionsDisabled);
            }
        }

        s_transactionConnector.ConnectToProxyCore(this, nodeName, resourceManagerIdentifier, managedIdentifier, out nodeNameMatches, out whereabouts, out resourceManagerShim);
    }

    [RequiresUnreferencedCode(TransactionManager.DistributedTransactionTrimmingWarning)]
    private void ConnectToProxyCore(
        string? nodeName,
        Guid resourceManagerIdentifier,
        object managedIdentifier,
        out bool nodeNameMatches,
        out byte[] whereabouts,
        out ResourceManagerShim resourceManagerShim)
    {
        lock (_proxyInitLock)
        {
            DtcGetTransactionManager(nodeName, out ITransactionDispenser? localDispenser);

            // Check to make sure the node name matches.
            if (nodeName is not null)
            {
                var pTmNodeName = (ITmNodeName)localDispenser;
                pTmNodeName.GetNodeNameSize(out uint tmNodeNameLength);
                pTmNodeName.GetNodeName(tmNodeNameLength, out string tmNodeName);

                nodeNameMatches = tmNodeName == nodeName;
            }
            else
            {
                nodeNameMatches = true;
            }

            var pImportWhereabouts = (ITransactionImportWhereabouts)localDispenser;

            // Adding retry logic as a work around for MSDTC's GetWhereAbouts/GetWhereAboutsSize API
            // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
            uint whereaboutsSize = 0;
            OletxHelper.Retry(() => pImportWhereabouts.GetWhereaboutsSize(out whereaboutsSize));

            var tmpWhereabouts = new byte[(int)whereaboutsSize];

            // Adding retry logic as a work around for MSDTC's GetWhereAbouts/GetWhereAboutsSize API
            // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
            OletxHelper.Retry(() =>
            {
                pImportWhereabouts.GetWhereabouts(whereaboutsSize, tmpWhereabouts, out uint pcbUsed);
                Debug.Assert(pcbUsed == tmpWhereabouts.Length);
            });

            // Now we need to create the internal resource manager.
            var rmFactory = (IResourceManagerFactory2)localDispenser;

            var rmNotifyShim = new ResourceManagerNotifyShim(this, managedIdentifier);
            var rmShim = new ResourceManagerShim(this);

            OletxHelper.Retry(() =>
            {
                rmFactory.CreateEx(
                    resourceManagerIdentifier,
                    "System.Transactions.InternalRM",
                    rmNotifyShim,
                    Guids.IID_IResourceManager_Guid,
                    out object? rm);

                rmShim.ResourceManager = (IResourceManager)rm;
            });

            resourceManagerShim = rmShim;
            _transactionDispenser = localDispenser;
            whereabouts = tmpWhereabouts;
        }
    }

    internal void NewNotification(NotificationShimBase notification)
    {
        lock (_notificationLock)
        {
            _notifications.Enqueue(notification);
        }

        _eventHandle.Set();
    }

    public void ReleaseNotificationLock()
        => Monitor.Exit(_notificationLock);

    public void BeginTransaction(
        uint timeout,
        OletxTransactionIsolationLevel isolationLevel,
        object? managedIdentifier,
        out Guid transactionIdentifier,
        out TransactionShim transactionShim)
    {
        ITransactionOptions options = GetCachedOptions();

        try
        {
            var xactopt = new Xactopt(timeout, string.Empty);
            options.SetOptions(xactopt);

            _transactionDispenser.BeginTransaction(IntPtr.Zero, isolationLevel, OletxTransactionIsoFlags.ISOFLAG_NONE, options, out ITransaction? pTx);

            SetupTransaction(pTx, managedIdentifier, out transactionIdentifier, out OletxTransactionIsolationLevel localIsoLevel, out transactionShim);
        }
        finally
        {
            ReturnCachedOptions(options);
        }
    }

    public void CreateResourceManager(
        Guid resourceManagerIdentifier,
        OletxResourceManager managedIdentifier,
        out ResourceManagerShim resourceManagerShim)
    {
        var rmFactory = (IResourceManagerFactory2)_transactionDispenser;

        var rmNotifyShim = new ResourceManagerNotifyShim(this, managedIdentifier);
        var rmShim = new ResourceManagerShim(this);

        OletxHelper.Retry(() =>
        {
            rmFactory.CreateEx(
                resourceManagerIdentifier,
                "System.Transactions.ResourceManager",
                rmNotifyShim,
                Guids.IID_IResourceManager_Guid,
                out object? rm);

            rmShim.ResourceManager = (IResourceManager)rm;
        });

        resourceManagerShim = rmShim;
    }

    public void Import(
        byte[] cookie,
        OutcomeEnlistment managedIdentifier,
        out Guid transactionIdentifier,
        out OletxTransactionIsolationLevel isolationLevel,
        out TransactionShim transactionShim)
    {
        var txImport = (ITransactionImport)_transactionDispenser;
        txImport.Import(Convert.ToUInt32(cookie.Length), cookie, Guids.IID_ITransaction_Guid, out object? tx);

        SetupTransaction((ITransaction)tx, managedIdentifier, out transactionIdentifier, out isolationLevel, out transactionShim);
    }

    public void ReceiveTransaction(
        byte[] propagationToken,
        OutcomeEnlistment managedIdentifier,
        out Guid transactionIdentifier,
        out OletxTransactionIsolationLevel isolationLevel,
        out TransactionShim transactionShim)
    {
        ITransactionReceiver receiver = GetCachedReceiver();

        try
        {
            receiver.UnmarshalPropagationToken(
                Convert.ToUInt32(propagationToken.Length),
                propagationToken,
                out ITransaction? tx);

            SetupTransaction(tx, managedIdentifier, out transactionIdentifier, out isolationLevel, out transactionShim);
        }
        finally
        {
            ReturnCachedReceiver(receiver);
        }
    }

    public void CreateTransactionShim(
        IDtcTransaction transactionNative,
        OutcomeEnlistment managedIdentifier,
        out Guid transactionIdentifier,
        out OletxTransactionIsolationLevel isolationLevel,
        out TransactionShim transactionShim)
    {
        var cloner = (ITransactionCloner)TransactionInterop.GetITransactionFromIDtcTransaction(transactionNative);
        cloner.CloneWithCommitDisabled(out ITransaction transaction);

        SetupTransaction(transaction, managedIdentifier, out transactionIdentifier, out isolationLevel, out transactionShim);
    }

    internal ITransactionExportFactory ExportFactory
        => (ITransactionExportFactory)_transactionDispenser;

    internal ITransactionVoterFactory2 VoterFactory
        => (ITransactionVoterFactory2)_transactionDispenser;

    public void GetNotification(
        out object? managedIdentifier,
        out ShimNotificationType shimNotificationType,
        out bool isSinglePhase,
        out bool abortingHint,
        out bool releaseLock,
        out byte[]? prepareInfo)
    {
        managedIdentifier = null;
        shimNotificationType = ShimNotificationType.None;
        isSinglePhase = false;
        abortingHint = false;
        releaseLock = false;
        prepareInfo = null;

        Monitor.Enter(_notificationLock);

        bool entryRemoved = _notifications.TryDequeue(out NotificationShimBase? notification);
        if (entryRemoved)
        {
            managedIdentifier = notification!.EnlistmentIdentifier;
            shimNotificationType = notification.NotificationType;
            isSinglePhase = notification.IsSinglePhase;
            abortingHint = notification.AbortingHint;
            prepareInfo = notification.PrepareInfo;
        }

        // We release the lock if we didn't find an entry or if the notification type
        // is NOT ResourceManagerTMDownNotify.  If it is a ResourceManagerTMDownNotify, the managed
        // code will call ReleaseNotificationLock after processing the TMDown.  We need to prevent
        // other notifications from being processed while we are processing TMDown.  But we don't want
        // to force 3 roundtrips to this NotificationShimFactory for all notifications ( 1 to grab the lock,
        // one to get the notification, and one to release the lock).
        if (!entryRemoved || shimNotificationType != ShimNotificationType.ResourceManagerTmDownNotify)
        {
            Monitor.Exit(_notificationLock);
        }
        else
        {
            releaseLock = true;
        }
    }

    private void SetupTransaction(
        ITransaction transaction,
        object? managedIdentifier,
        out Guid pTransactionIdentifier,
        out OletxTransactionIsolationLevel pIsolationLevel,
        out TransactionShim ppTransactionShim)
    {
        var transactionNotifyShim = new TransactionNotifyShim(this, managedIdentifier);

        // Get the transaction id.
        transaction.GetTransactionInfo(out OletxXactTransInfo xactInfo);

        // Register for outcome events.
        var pContainer = (IConnectionPointContainer)TransactionInterop.GetDtcTransaction(transaction);
        var guid = Guids.IID_ITransactionOutcomeEvents_Guid;
        pContainer.FindConnectionPoint(ref guid, out IConnectionPoint? pConnPoint);
        pConnPoint!.Advise(transactionNotifyShim, out int connPointCookie);

        var transactionShim = new TransactionShim(this, transactionNotifyShim, transaction);
        pTransactionIdentifier = xactInfo.Uow;
        pIsolationLevel = xactInfo.IsoLevel;
        ppTransactionShim = transactionShim;
    }

    private ITransactionOptions GetCachedOptions()
    {
        if (_cachedOptions.TryDequeue(out ITransactionOptions? options))
        {
            return options;
        }

        _transactionDispenser.GetOptionsObject(out ITransactionOptions? transactionOptions);
        return transactionOptions;
    }

    internal void ReturnCachedOptions(ITransactionOptions options)
        => _cachedOptions.Enqueue(options);

    internal ITransactionTransmitter GetCachedTransmitter(ITransaction transaction)
    {
        if (!_cachedTransmitters.TryDequeue(out ITransactionTransmitter? transmitter))
        {
            var transmitterFactory = (ITransactionTransmitterFactory)_transactionDispenser;
            transmitterFactory.Create(out transmitter);
        }

        transmitter.Set(transaction);

        return transmitter;
    }

    internal void ReturnCachedTransmitter(ITransactionTransmitter transmitter)
    {
        // Note that due to race conditions, we may end up enqueuing above s_maxCachedInterfaces.
        // This is benign, as this is only a best-effort cache, and there are no negative consequences.
        if (_cachedTransmitters.Count < s_maxCachedInterfaces)
        {
            transmitter.Reset();
            _cachedTransmitters.Enqueue(transmitter);
        }
    }

    internal ITransactionReceiver GetCachedReceiver()
    {
        if (_cachedReceivers.TryDequeue(out ITransactionReceiver? receiver))
        {
            return receiver;
        }

        var receiverFactory = (ITransactionReceiverFactory)_transactionDispenser;
        receiverFactory.Create(out ITransactionReceiver transactionReceiver);

        return transactionReceiver;
    }

    internal void ReturnCachedReceiver(ITransactionReceiver receiver)
    {
        // Note that due to race conditions, we may end up enqueuing above s_maxCachedInterfaces.
        // This is benign, as this is only a best-effort cache, and there are no negative consequences.
        if (_cachedReceivers.Count < s_maxCachedInterfaces)
        {
            receiver.Reset();
            _cachedReceivers.Enqueue(receiver);
        }
    }

    internal interface ITransactionConnector
    {
        void ConnectToProxyCore(
            DtcProxyShimFactory proxyShimFactory,
            string? nodeName,
            Guid resourceManagerIdentifier,
            object managedIdentifier,
            out bool nodeNameMatches,
            out byte[] whereabouts,
            out ResourceManagerShim resourceManagerShim);
    }

    [RequiresUnreferencedCode(TransactionManager.DistributedTransactionTrimmingWarning)]
    internal sealed class DtcTransactionConnector : ITransactionConnector
    {
        public void ConnectToProxyCore(
            DtcProxyShimFactory proxyShimFactory,
            string? nodeName,
            Guid resourceManagerIdentifier,
            object managedIdentifier,
            out bool nodeNameMatches,
            out byte[] whereabouts,
            out ResourceManagerShim resourceManagerShim)
        {
            proxyShimFactory.ConnectToProxyCore(
                nodeName,
                resourceManagerIdentifier,
                managedIdentifier,
                out nodeNameMatches,
                out whereabouts,
                out resourceManagerShim);
        }
    }
}
