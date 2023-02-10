// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

internal sealed class TransactionShim
{
    private DtcProxyShimFactory _shimFactory;
    private TransactionNotifyShim _transactionNotifyShim;

    internal ITransaction Transaction { get; set; }

    internal TransactionShim(DtcProxyShimFactory shimFactory, TransactionNotifyShim notifyShim, ITransaction transaction)
    {
        _shimFactory = shimFactory;
        _transactionNotifyShim = notifyShim;
        Transaction = transaction;
    }

    public void Commit()
        => Transaction.Commit(false, OletxXacttc.XACTTC_ASYNC, 0);

    public void Abort()
        => Transaction.Abort(IntPtr.Zero, false, false);

    public void CreateVoter(OletxPhase1VolatileEnlistmentContainer managedIdentifier, out VoterBallotShim voterBallotShim)
    {
        var voterNotifyShim = new VoterNotifyShim(_shimFactory, managedIdentifier);
        var voterShim = new VoterBallotShim(voterNotifyShim);
        _shimFactory.VoterFactory.Create(Transaction, voterNotifyShim, out ITransactionVoterBallotAsync2 voterBallot);
        voterShim.VoterBallotAsync2 = voterBallot;
        voterBallotShim = voterShim;
    }

    public void Export(byte[] whereabouts, out byte[] cookieBuffer)
    {
        _shimFactory.ExportFactory.Create((uint)whereabouts.Length, whereabouts, out ITransactionExport export);

        uint cookieSizeULong = 0;

        OletxHelper.Retry(() => export.Export(Transaction, out cookieSizeULong));

        var cookieSize = (uint)cookieSizeULong;
        var buffer = new byte[cookieSize];
        uint bytesUsed = 0;

        OletxHelper.Retry(() => export.GetTransactionCookie(Transaction, cookieSize, buffer, out bytesUsed));

        cookieBuffer = buffer;
    }

    public void GetITransactionNative(out IDtcTransaction transactionNative)
    {
        var cloner = (ITransactionCloner)Transaction;
        cloner.CloneWithCommitDisabled(out ITransaction returnTransaction);

        transactionNative = (IDtcTransaction)returnTransaction;
    }

    public unsafe byte[] GetPropagationToken()
    {
        ITransactionTransmitter transmitter = _shimFactory.GetCachedTransmitter(Transaction);

        try
        {
            transmitter.GetPropagationTokenSize(out uint propagationTokenSizeULong);

            var propagationTokenSize = (int)propagationTokenSizeULong;
            var propagationToken = new byte[propagationTokenSize];

            transmitter.MarshalPropagationToken((uint)propagationTokenSize, propagationToken, out uint propagationTokenSizeUsed);

            return propagationToken;
        }
        finally
        {
            _shimFactory.ReturnCachedTransmitter(transmitter);
        }
    }

    public void Phase0Enlist(object managedIdentifier, out Phase0EnlistmentShim phase0EnlistmentShim)
    {
        var phase0Factory = (ITransactionPhase0Factory)Transaction;
        var phase0NotifyShim = new Phase0NotifyShim(_shimFactory, managedIdentifier);
        var phase0Shim = new Phase0EnlistmentShim(phase0NotifyShim);

        phase0Factory.Create(phase0NotifyShim, out ITransactionPhase0EnlistmentAsync phase0Async);
        phase0Shim.Phase0EnlistmentAsync = phase0Async;

        phase0Async.Enable();
        phase0Async.WaitForEnlistment();

        phase0EnlistmentShim = phase0Shim;
    }
}
