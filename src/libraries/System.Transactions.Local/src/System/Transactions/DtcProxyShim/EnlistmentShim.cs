// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class EnlistmentShim
{
    private readonly EnlistmentNotifyShim _enlistmentNotifyShim;

    internal ITransactionEnlistmentAsync? EnlistmentAsync { get; set; }

    internal EnlistmentShim(EnlistmentNotifyShim notifyShim)
        => _enlistmentNotifyShim = notifyShim;

    public void PrepareRequestDone(OletxPrepareVoteType voteType)
    {
        var voteHr = OletxHelper.S_OK;
        var releaseEnlistment = false;

        switch (voteType)
        {
            case OletxPrepareVoteType.ReadOnly:
                {
                    // On W2k Proxy may send a spurious aborted notification if the TM goes down.
                    _enlistmentNotifyShim.SetIgnoreSpuriousProxyNotifications();
                    voteHr = OletxHelper.XACT_S_READONLY;
                    break;
                }

            case OletxPrepareVoteType.SinglePhase:
                {
                    // On W2k Proxy may send a spurious aborted notification if the TM goes down.
                    _enlistmentNotifyShim.SetIgnoreSpuriousProxyNotifications();
                    voteHr = OletxHelper.XACT_S_SINGLEPHASE;
                    break;
                }

            case OletxPrepareVoteType.Prepared:
                {
                    voteHr = OletxHelper.S_OK;
                    break;
                }

            case OletxPrepareVoteType.Failed:
                {
                    // Proxy may send a spurious aborted notification if the TM goes down.
                    _enlistmentNotifyShim.SetIgnoreSpuriousProxyNotifications();
                    voteHr = OletxHelper.E_FAIL;
                    break;
                }

            case OletxPrepareVoteType.InDoubt:
                {
                    releaseEnlistment = true;
                    break;
                }

            default:  // unexpected, vote no.
                {
                    voteHr = OletxHelper.E_FAIL;
                    break;
                }
        }

        if (!releaseEnlistment)
        {
            EnlistmentAsync!.PrepareRequestDone(
                voteHr,
                IntPtr.Zero,
                IntPtr.Zero);
        }
    }

    public void CommitRequestDone()
        => EnlistmentAsync!.CommitRequestDone(OletxHelper.S_OK);

    public void AbortRequestDone()
        => EnlistmentAsync!.AbortRequestDone(OletxHelper.S_OK);
}
