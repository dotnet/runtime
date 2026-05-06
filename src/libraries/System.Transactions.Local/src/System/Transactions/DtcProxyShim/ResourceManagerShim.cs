// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

internal sealed class ResourceManagerShim
{
    private readonly DtcProxyShimFactory _shimFactory;

    internal ResourceManagerShim(DtcProxyShimFactory shimFactory)
        => _shimFactory = shimFactory;

    public IResourceManager? ResourceManager { get; set; }

    public void Enlist(
        TransactionShim transactionShim,
        OletxEnlistment managedIdentifier,
        out EnlistmentShim enlistmentShim)
    {
        var pEnlistmentNotifyShim = new EnlistmentNotifyShim(_shimFactory, managedIdentifier);
        var pEnlistmentShim = new EnlistmentShim(pEnlistmentNotifyShim);

        ITransaction transaction = transactionShim.Transaction;
        ResourceManager!.Enlist(transaction, pEnlistmentNotifyShim, out Guid txUow, out OletxTransactionIsolationLevel isoLevel, out ITransactionEnlistmentAsync pEnlistmentAsync);

        pEnlistmentNotifyShim.EnlistmentAsync = pEnlistmentAsync;
        pEnlistmentShim.EnlistmentAsync = pEnlistmentAsync;

        enlistmentShim = pEnlistmentShim;
    }

    public void Reenlist(byte[] prepareInfo, out OletxTransactionOutcome outcome)
    {
        // Call Reenlist on the proxy, waiting for 5 milliseconds for it to get the outcome.  If it doesn't know that outcome in that
        // amount of time, tell the caller we don't know the outcome yet.  The managed code will reschedule the check by using the
        // ReenlistThread.
        try
        {
            ResourceManager!.Reenlist(prepareInfo, (uint)prepareInfo.Length, 5, out OletxXactStat xactStatus);
            outcome = xactStatus switch
            {
                OletxXactStat.XACTSTAT_ABORTED => OletxTransactionOutcome.Aborted,
                OletxXactStat.XACTSTAT_COMMITTED => OletxTransactionOutcome.Committed,
                _ => OletxTransactionOutcome.Aborted
            };
        }
        catch (COMException e) when (e.ErrorCode == OletxHelper.XACT_E_REENLISTTIMEOUT)
        {
            outcome = OletxTransactionOutcome.NotKnownYet;
            return;
        }
    }

    public void ReenlistComplete()
        => ResourceManager!.ReenlistmentComplete();
}
