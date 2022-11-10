// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class VoterBallotShim
{
    private VoterNotifyShim _voterNotifyShim;

    internal ITransactionVoterBallotAsync2? VoterBallotAsync2 { get; set; }

    internal VoterBallotShim(VoterNotifyShim notifyShim)
        => _voterNotifyShim = notifyShim;

    public void Vote(bool voteYes)
    {
        int voteHr = OletxHelper.S_OK;

        if (!voteYes)
        {
            voteHr = OletxHelper.E_FAIL;
        }

        VoterBallotAsync2!.VoteRequestDone(voteHr, IntPtr.Zero);
    }
}
