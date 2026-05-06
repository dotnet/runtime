// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Transactions.Tests;

#nullable enable

[SkipOnPlatform(TestPlatforms.Windows, "These tests assert that OleTx operations properly throw PlatformNotSupportedException on non-Windows platforms")]
public class OleTxNonWindowsUnsupportedTests
{
    [Fact]
    public void Durable_enlistment()
    {
        var tx = new CommittableTransaction();

        // Votes and outcomes don't matter, the 2nd enlistment fails in non-Windows
        var enlistment1 = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Aborted);

        Assert.Throws<PlatformNotSupportedException>(() => tx.EnlistDurable(Guid.NewGuid(), enlistment1, EnlistmentOptions.None));
        Assert.Equal(TransactionStatus.Aborted, tx.TransactionInformation.Status);
    }

    [Fact]
    public void Promotable_enlistments()
    {
        var tx = new CommittableTransaction();

        var promotableEnlistment1 = new TestPromotableSinglePhaseEnlistment(() => new byte[24], EnlistmentOutcome.Aborted);
        var promotableEnlistment2 = new TestPromotableSinglePhaseEnlistment(null, EnlistmentOutcome.Aborted);

        // 1st promotable enlistment - no distributed transaction yet.
        Assert.True(tx.EnlistPromotableSinglePhase(promotableEnlistment1));
        Assert.True(promotableEnlistment1.InitializedCalled);

        // 2nd promotable enlistment returns false.
        tx.EnlistPromotableSinglePhase(promotableEnlistment2);
        Assert.False(promotableEnlistment2.InitializedCalled);

        // Now enlist a durable enlistment, this will cause the escalation to a distributed transaction and fail on non-Windows.
        var durableEnlistment = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Aborted);
        Assert.Throws<PlatformNotSupportedException>(() => tx.EnlistDurable(Guid.NewGuid(), durableEnlistment, EnlistmentOptions.None));

        Assert.True(promotableEnlistment1.PromoteCalled);
        Assert.False(promotableEnlistment2.PromoteCalled);

        Assert.Equal(TransactionStatus.Aborted, tx.TransactionInformation.Status);
    }

    [Fact]
    public void TransmitterPropagationToken()
        => Assert.Throws<PlatformNotSupportedException>(() =>
            TransactionInterop.GetTransmitterPropagationToken(new CommittableTransaction()));

    [Fact]
    public void GetWhereabouts()
        => Assert.Throws<PlatformNotSupportedException>(TransactionInterop.GetWhereabouts);

    [Fact]
    public void GetExportCookie()
        => Assert.Throws<PlatformNotSupportedException>(() =>
            TransactionInterop.GetExportCookie(new CommittableTransaction(), new byte[200]));

    [Fact]
    public void GetDtcTransaction()
        => Assert.Throws<PlatformNotSupportedException>(() =>
            TransactionInterop.GetDtcTransaction(new CommittableTransaction()));
}
