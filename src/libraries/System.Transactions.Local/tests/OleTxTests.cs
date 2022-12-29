// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Transactions.Tests;

#nullable enable

[PlatformSpecific(TestPlatforms.Windows)]
[SkipOnMono("COM Interop not supported on Mono")]
public class OleTxTests : IClassFixture<OleTxTests.OleTxFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

    public OleTxTests(OleTxFixture fixture)
    {
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    [InlineData(Phase1Vote.Prepared, Phase1Vote.Prepared, EnlistmentOutcome.Committed, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
    [InlineData(Phase1Vote.Prepared, Phase1Vote.ForceRollback, EnlistmentOutcome.Aborted, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
    [InlineData(Phase1Vote.ForceRollback, Phase1Vote.Prepared, EnlistmentOutcome.Aborted, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
    public void Two_durable_enlistments_commit(Phase1Vote vote1, Phase1Vote vote2, EnlistmentOutcome expectedOutcome1, EnlistmentOutcome expectedOutcome2, TransactionStatus expectedTxStatus)
        => Test(() =>
        {
            using var tx = new CommittableTransaction();

            try
            {
                var enlistment1 = new TestEnlistment(vote1, expectedOutcome1);
                var enlistment2 = new TestEnlistment(vote2, expectedOutcome2);

                tx.EnlistDurable(Guid.NewGuid(), enlistment1, EnlistmentOptions.None);
                tx.EnlistDurable(Guid.NewGuid(), enlistment2, EnlistmentOptions.None);

                Assert.Equal(TransactionStatus.Active, tx.TransactionInformation.Status);
                tx.Commit();
            }
            catch (TransactionInDoubtException)
            {
                Assert.Equal(TransactionStatus.InDoubt, expectedTxStatus);
            }
            catch (TransactionAbortedException)
            {
                Assert.Equal(TransactionStatus.Aborted, expectedTxStatus);
            }

            Retry(() => Assert.Equal(expectedTxStatus, tx.TransactionInformation.Status));
        });

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    public void Two_durable_enlistments_rollback()
        => Test(() =>
        {
            using var tx = new CommittableTransaction();

            var enlistment1 = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Aborted);
            var enlistment2 = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Aborted);

            tx.EnlistDurable(Guid.NewGuid(), enlistment1, EnlistmentOptions.None);
            tx.EnlistDurable(Guid.NewGuid(), enlistment2, EnlistmentOptions.None);

            tx.Rollback();

            Assert.False(enlistment1.WasPreparedCalled);
            Assert.False(enlistment2.WasPreparedCalled);

            // This matches the .NET Framework behavior
            Retry(() => Assert.Equal(TransactionStatus.Aborted, tx.TransactionInformation.Status));
        });

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Volatile_and_durable_enlistments(int volatileCount)
        => Test(() =>
        {
            using var tx = new CommittableTransaction();

            if (volatileCount > 0)
            {
                TestEnlistment[] volatiles = new TestEnlistment[volatileCount];
                for (int i = 0; i < volatileCount; i++)
                {
                    // It doesn't matter what we specify for SinglePhaseVote.
                    volatiles[i] = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed);
                    tx.EnlistVolatile(volatiles[i], EnlistmentOptions.None);
                }
            }

            var durable = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed);

            // Creation of two phase durable enlistment attempts to promote to MSDTC
            tx.EnlistDurable(Guid.NewGuid(), durable, EnlistmentOptions.None);

            tx.Commit();

            Retry(() => Assert.Equal(TransactionStatus.Committed, tx.TransactionInformation.Status));
        });

    protected static bool IsRemoteExecutorSupportedAndNotNano => RemoteExecutor.IsSupported && PlatformDetection.IsNotWindowsNanoServer;

    [ConditionalFact(nameof(IsRemoteExecutorSupportedAndNotNano))]
    public void Promotion()
        => PromotionCore();

    // #76010
    [ConditionalFact(nameof(IsRemoteExecutorSupportedAndNotNano))]
    public void Promotion_twice()
    {
        PromotionCore();
        PromotionCore();
    }

    private void PromotionCore()
    {
        Test(() =>
        {
            // This simulates the full promotable flow, as implemented for SQL Server.

            // We are going to spin up two external processes.
            // 1. The 1st external process will create the transaction and save its propagation token to disk.
            // 2. The main process will read that, and propagate the transaction to the 2nd external process.
            // 3. The main process will then notify the 1st external process to commit (as the main's transaction is delegated to it).
            // 4. At that point the MSDTC Commit will be triggered; enlistments on both the 1st and 2nd processes will be notified
            //    to commit, and the transaction status will reflect the committed status in the main process.
            using var tx = new CommittableTransaction();

            string propagationTokenFilePath = Path.GetTempFileName();
            string exportCookieFilePath = Path.GetTempFileName();
            using var waitHandle1 = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "System.Transactions.Tests.OleTxTests.Promotion1");
            using var waitHandle2 = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "System.Transactions.Tests.OleTxTests.Promotion2");
            using var waitHandle3 = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "System.Transactions.Tests.OleTxTests.Promotion3");

            RemoteInvokeHandle? remote1 = null, remote2 = null;

            try
            {
                remote1 = RemoteExecutor.Invoke(Remote1, propagationTokenFilePath, new RemoteInvokeOptions { ExpectedExitCode = 42 });

                // Wait for the external process to start a transaction and save its propagation token
                Assert.True(waitHandle1.WaitOne(Timeout));

                // Enlist the first PSPE. No escalation happens yet, since its the only enlistment.
                var pspe1 = new TestPromotableSinglePhaseNotification(propagationTokenFilePath);
                Assert.True(tx.EnlistPromotableSinglePhase(pspe1));
                Assert.True(pspe1.WasInitializedCalled);
                Assert.False(pspe1.WasPromoteCalled);
                Assert.False(pspe1.WasRollbackCalled);
                Assert.False(pspe1.WasSinglePhaseCommitCalled);

                // Enlist the second PSPE. This returns false and does nothing, since there's already an enlistment.
                var pspe2 = new TestPromotableSinglePhaseNotification(propagationTokenFilePath);
                Assert.False(tx.EnlistPromotableSinglePhase(pspe2));
                Assert.False(pspe2.WasInitializedCalled);
                Assert.False(pspe2.WasPromoteCalled);
                Assert.False(pspe2.WasRollbackCalled);
                Assert.False(pspe2.WasSinglePhaseCommitCalled);

                // Now generate an export cookie for the 2nd external process. This causes escalation and promotion.
                byte[] whereabouts = TransactionInterop.GetWhereabouts();
                byte[] exportCookie = TransactionInterop.GetExportCookie(tx, whereabouts);

                Assert.True(pspe1.WasPromoteCalled);
                Assert.False(pspe1.WasRollbackCalled);
                Assert.False(pspe1.WasSinglePhaseCommitCalled);

                // Write the export cookie and start the 2nd external process, which will read the cookie and enlist in the transaction.
                // Wait for it to complete.
                File.WriteAllBytes(exportCookieFilePath, exportCookie);
                remote2 = RemoteExecutor.Invoke(Remote2, exportCookieFilePath, new RemoteInvokeOptions { ExpectedExitCode = 42 });
                Assert.True(waitHandle2.WaitOne(Timeout));

                // We now have two external processes with enlistments to our distributed transaction. Commit.
                // Since our transaction is delegated to the 1st PSPE enlistment, Sys.Tx will call SinglePhaseCommit on it.
                // In SQL Server this contacts the 1st DB to actually commit the transaction with MSDTC. In this simulation we'll just use a wait handle to trigger this.
                tx.Commit();
                Assert.True(pspe1.WasSinglePhaseCommitCalled);
                waitHandle3.Set();

                Retry(() => Assert.Equal(TransactionStatus.Committed, tx.TransactionInformation.Status));
            }
            catch
            {
                try
                {
                    remote1?.Process.Kill();
                    remote2?.Process.Kill();
                }
                catch
                {
                }

                throw;
            }
            finally
            {
                File.Delete(propagationTokenFilePath);
            }

            // Disposal of the RemoteExecutor handles will wait for the external processes to exit with the right exit code,
            // which will happen when their enlistments receive the commit.
            remote1?.Dispose();
            remote2?.Dispose();
        });

        static void Remote1(string propagationTokenFilePath)
            => Test(() =>
            {
                var outcomeEvent = new AutoResetEvent(false);

                using (var tx = new CommittableTransaction())
                {
                    var enlistment = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed, outcomeReceived: outcomeEvent);
                    tx.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);

                    // We now have an OleTx transaction. Save its propagation token to disk so that the main process can read it when promoting.
                    byte[] propagationToken = TransactionInterop.GetTransmitterPropagationToken(tx);
                    File.WriteAllBytes(propagationTokenFilePath, propagationToken);

                    // Signal to the main process that the propagation token is ready to be read
                    using var waitHandle1 = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "System.Transactions.Tests.OleTxTests.Promotion1");
                    waitHandle1.Set();

                    // The main process will now import our transaction via the propagation token, and propagate it to a 2nd process.
                    // In the main process the transaction is delegated; we're the one who started it, and so we're the one who need to Commit.
                    // When Commit() is called in the main process, that will trigger a SinglePhaseCommit on the PSPE which represents us. In SQL Server this
                    // contacts the DB to actually commit the transaction with MSDTC. In this simulation we'll just use the wait handle again to trigger this.
                    using var waitHandle3 = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "System.Transactions.Tests.OleTxTests.Promotion3");
                    Assert.True(waitHandle3.WaitOne(Timeout));

                    tx.Commit();
                }

                // Wait for the commit to occur on our enlistment, then exit successfully.
                Assert.True(outcomeEvent.WaitOne(Timeout));
                Environment.Exit(42); // 42 is error code expected by RemoteExecutor
            });

        static void Remote2(string exportCookieFilePath)
            => Test(() =>
            {
                var outcomeEvent = new AutoResetEvent(false);

                // Load the export cookie and enlist durably
                byte[] exportCookie = File.ReadAllBytes(exportCookieFilePath);
                using (var tx = TransactionInterop.GetTransactionFromExportCookie(exportCookie))
                {
                    // Now enlist durably. This triggers promotion of the first PSPE, reading the propagation token.
                    var enlistment = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed, outcomeReceived: outcomeEvent);
                    tx.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);

                    // Signal to the main process that we're enlisted and ready to commit
                    using var waitHandle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "System.Transactions.Tests.OleTxTests.Promotion2");
                    waitHandle.Set();
                }

                // Wait for the main process to commit the transaction
                Assert.True(outcomeEvent.WaitOne(Timeout));
                Environment.Exit(42); // 42 is error code expected by RemoteExecutor
            });
    }

    public class TestPromotableSinglePhaseNotification : IPromotableSinglePhaseNotification
    {
        private string _propagationTokenFilePath;

        public TestPromotableSinglePhaseNotification(string propagationTokenFilePath)
            => _propagationTokenFilePath = propagationTokenFilePath;

        public bool WasInitializedCalled { get; private set; }
        public bool WasPromoteCalled { get; private set; }
        public bool WasRollbackCalled { get; private set; }
        public bool WasSinglePhaseCommitCalled { get; private set; }

        public void Initialize()
            => WasInitializedCalled = true;

        public byte[] Promote()
        {
            WasPromoteCalled = true;

            return File.ReadAllBytes(_propagationTokenFilePath);
        }

        public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
            => WasRollbackCalled = true;

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            WasSinglePhaseCommitCalled = true;

            singlePhaseEnlistment.Committed();
        }
    }

    [ConditionalFact(nameof(IsRemoteExecutorSupportedAndNotNano))]
    public void Recovery()
    {
        Test(() =>
        {
            // We are going to spin up an external process to also enlist in the transaction, and then to crash when it
            // receives the commit notification. We will then initiate the recovery flow.

            using var tx = new CommittableTransaction();

            var outcomeEvent1 = new AutoResetEvent(false);
            var enlistment1 = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed, outcomeReceived: outcomeEvent1);
            var guid1 = Guid.NewGuid();
            tx.EnlistDurable(guid1, enlistment1, EnlistmentOptions.None);

            // The propagation token is used to propagate the transaction to that process so it can enlist to our
            // transaction. We also provide the resource manager identifier GUID, and a path where the external process will
            // write the recovery information it will receive from the MSDTC when preparing.
            // We'll need these two elements later in order to Reenlist and trigger recovery.
            byte[] propagationToken = TransactionInterop.GetTransmitterPropagationToken(tx);
            string propagationTokenText = Convert.ToBase64String(propagationToken);
            var guid2 = Guid.NewGuid();
            string secondEnlistmentRecoveryFilePath = Path.GetTempFileName();

            using var waitHandle = new EventWaitHandle(
                initialState: false,
                EventResetMode.ManualReset,
                "System.Transactions.Tests.OleTxTests.Recovery");

            try
            {
                using (RemoteExecutor.Invoke(
                           EnlistAndCrash,
                           propagationTokenText, guid2.ToString(), secondEnlistmentRecoveryFilePath,
                           new RemoteInvokeOptions { ExpectedExitCode = 42 }))
                {
                    // Wait for the external process to enlist in the transaction, it will signal this EventWaitHandle.
                    Assert.True(waitHandle.WaitOne(Timeout));

                    tx.Commit();
                }

                // The other has crashed when the MSDTC notified it to commit.
                // Load the recovery information the other process has written to disk for us and reenlist with
                // the failed RM's Guid to commit.
                var outcomeEvent3 = new AutoResetEvent(false);
                var enlistment3 = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed, outcomeReceived: outcomeEvent3);
                byte[] secondRecoveryInformation = File.ReadAllBytes(secondEnlistmentRecoveryFilePath);
                _ = TransactionManager.Reenlist(guid2, secondRecoveryInformation, enlistment3);
                TransactionManager.RecoveryComplete(guid2);

                Assert.True(outcomeEvent1.WaitOne(Timeout));
                Assert.True(outcomeEvent3.WaitOne(Timeout));
                Assert.Equal(EnlistmentOutcome.Committed, enlistment1.Outcome);
                Assert.Equal(EnlistmentOutcome.Committed, enlistment3.Outcome);
                Assert.Equal(TransactionStatus.Committed, tx.TransactionInformation.Status);

                // Note: verify manually in the MSDTC console that the distributed transaction is gone
                // (i.e. successfully committed),
                // (Start -> Component Services -> Computers -> My Computer -> Distributed Transaction Coordinator ->
                //           Local DTC -> Transaction List)
            }
            finally
            {
                File.Delete(secondEnlistmentRecoveryFilePath);
            }
        });

        static void EnlistAndCrash(string propagationTokenText, string resourceManagerIdentifierGuid, string recoveryInformationFilePath)
            => Test(() =>
            {
                byte[] propagationToken = Convert.FromBase64String(propagationTokenText);
                using var tx = TransactionInterop.GetTransactionFromTransmitterPropagationToken(propagationToken);

                var crashingEnlistment = new CrashingEnlistment(recoveryInformationFilePath);
                tx.EnlistDurable(Guid.Parse(resourceManagerIdentifierGuid), crashingEnlistment, EnlistmentOptions.None);

                // Signal to the main process that we've enlisted and are ready to accept prepare/commit.
                using var waitHandle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, "System.Transactions.Tests.OleTxTests.Recovery");
                waitHandle.Set();

                // We've enlisted, and set it up so that when the MSDTC tells us to commit, the process will crash.
                Thread.Sleep(Timeout);
            });
    }

    public class CrashingEnlistment : IEnlistmentNotification
    {
        private string _recoveryInformationFilePath;

        public CrashingEnlistment(string recoveryInformationFilePath)
            => _recoveryInformationFilePath = recoveryInformationFilePath;

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            // Received a prepare notification from MSDTC, persist the recovery information so that the main process can perform recovery for it.
            File.WriteAllBytes(_recoveryInformationFilePath, preparingEnlistment.RecoveryInformation());

            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
            => Environment.Exit(42); // 42 is error code expected by RemoteExecutor

        public void Rollback(Enlistment enlistment)
            => Environment.Exit(1);

        public void InDoubt(Enlistment enlistment)
            => Environment.Exit(1);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    public void TransmitterPropagationToken()
        => Test(() =>
        {
            using var tx = new CommittableTransaction();

            Assert.Equal(Guid.Empty, tx.TransactionInformation.DistributedIdentifier);

            var propagationToken = TransactionInterop.GetTransmitterPropagationToken(tx);

            Assert.NotEqual(Guid.Empty, tx.TransactionInformation.DistributedIdentifier);

            var tx2 = TransactionInterop.GetTransactionFromTransmitterPropagationToken(propagationToken);

            Assert.Equal(tx.TransactionInformation.DistributedIdentifier, tx2.TransactionInformation.DistributedIdentifier);
        });

    // #76010
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    public void TransactionScope_with_DependentTransaction()
    => Test(() =>
    {
        using var committableTransaction = new CommittableTransaction();
        var propagationToken = TransactionInterop.GetTransmitterPropagationToken(committableTransaction);

        var dependentTransaction = TransactionInterop.GetTransactionFromTransmitterPropagationToken(propagationToken);

        using (var scope = new TransactionScope(dependentTransaction))
        {
            scope.Complete();
        }
    });

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    public void GetExportCookie()
        => Test(() =>
        {
            using var tx = new CommittableTransaction();

            var whereabouts = TransactionInterop.GetWhereabouts();

            Assert.Equal(Guid.Empty, tx.TransactionInformation.DistributedIdentifier);

            var exportCookie = TransactionInterop.GetExportCookie(tx, whereabouts);

            Assert.NotEqual(Guid.Empty, tx.TransactionInformation.DistributedIdentifier);

            var tx2 = TransactionInterop.GetTransactionFromExportCookie(exportCookie);

            Assert.Equal(tx.TransactionInformation.DistributedIdentifier, tx2.TransactionInformation.DistributedIdentifier);
        });

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    public void GetDtcTransaction()
        => Test(() =>
        {
            using var tx = new CommittableTransaction();

            var outcomeReceived = new AutoResetEvent(false);

            var enlistment = new TestEnlistment(
                Phase1Vote.Prepared, EnlistmentOutcome.Committed, outcomeReceived: outcomeReceived);

            Assert.Equal(Guid.Empty, tx.PromoterType);

            tx.EnlistVolatile(enlistment, EnlistmentOptions.None);

            // Forces promotion to MSDTC, returns an ITransaction for use only with System.EnterpriseServices.
            _ = TransactionInterop.GetDtcTransaction(tx);

            Assert.Equal(TransactionStatus.Active, tx.TransactionInformation.Status);
            Assert.Equal(TransactionInterop.PromoterTypeDtc, tx.PromoterType);

            tx.Commit();

            Assert.True(outcomeReceived.WaitOne(Timeout));
            Assert.Equal(EnlistmentOutcome.Committed, enlistment.Outcome);
            Retry(() => Assert.Equal(TransactionStatus.Committed, tx.TransactionInformation.Status));
        });

    [ConditionalFact(nameof(IsRemoteExecutorSupportedAndNotNano))]
    public void Distributed_transactions_require_ImplicitDistributedTransactions_true()
    {
        // Temporarily skip on 32-bit where we have an issue.
        if (!Environment.Is64BitProcess)
        {
            return;
        }

        using var _ = RemoteExecutor.Invoke(() =>
        {
            Assert.False(TransactionManager.ImplicitDistributedTransactions);

            using var tx = new CommittableTransaction();

            Assert.Throws<NotSupportedException>(MinimalOleTxScenario);
        });
    }

    [ConditionalFact(nameof(IsRemoteExecutorSupportedAndNotNano))]
    public void ImplicitDistributedTransactions_cannot_be_changed_after_being_set()
    {
        // Temporarily skip on 32-bit where we have an issue.
        if (!Environment.Is64BitProcess)
        {
            return;
        }

        using var _ = RemoteExecutor.Invoke(() =>
        {
            TransactionManager.ImplicitDistributedTransactions = true;

            Assert.Throws<InvalidOperationException>(() => TransactionManager.ImplicitDistributedTransactions = false);
        });
    }

    [ConditionalFact(nameof(IsRemoteExecutorSupportedAndNotNano))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/77241")]
    public void ImplicitDistributedTransactions_cannot_be_changed_after_being_read_as_true()
    {
        // Temporarily skip on 32-bit where we have an issue.
        if (!Environment.Is64BitProcess)
        {
            return;
        }

        using var _ = RemoteExecutor.Invoke(() =>
        {
            TransactionManager.ImplicitDistributedTransactions = true;

            Test(MinimalOleTxScenario);

            Assert.Throws<InvalidOperationException>(() => TransactionManager.ImplicitDistributedTransactions = false);
            TransactionManager.ImplicitDistributedTransactions = true;
        });
    }

    [ConditionalFact(nameof(IsRemoteExecutorSupportedAndNotNano))]
    public void ImplicitDistributedTransactions_cannot_be_changed_after_being_read_as_false()
    {
        // Temporarily skip on 32-bit where we have an issue.
        if (!Environment.Is64BitProcess)
        {
            return;
        }

        using var _ = RemoteExecutor.Invoke(() =>
        {
            Assert.Throws<NotSupportedException>(MinimalOleTxScenario);

            Assert.Throws<InvalidOperationException>(() => TransactionManager.ImplicitDistributedTransactions = true);
            TransactionManager.ImplicitDistributedTransactions = false;
        });
    }

    private static void Test(Action action)
    {
        // Temporarily skip on 32-bit where we have an issue.
        if (!Environment.Is64BitProcess)
        {
            return;
        }

        if (s_isTestSuiteDisabled)
        {
            return;
        }

        TransactionManager.ImplicitDistributedTransactions = true;

        // In CI, we sometimes get XACT_E_TMNOTAVAILABLE; when it happens, it's typically on the very first
        // attempt to connect to MSDTC (flaky/slow on-demand startup of MSDTC), though not only.
        // This catches that error and retries: 5 minutes of retries, with a second between them.
        int nRetries = 60 * 5;

        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (Exception e) when (e is TransactionManagerCommunicationException or TransactionException { InnerException: TransactionManagerCommunicationException })
            {
                if (--nRetries > 0)
                {
                    Thread.Sleep(1000);

                    continue;
                }

                // We've continuously gotten XACT_E_TMNOTAVAILABLE for the entire retry window - MSDTC is unavailable in some way.
                // We don't want this to make our CI flaky, so we swallow the exception and skip all subsequent tests.
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_CI")) ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT")) ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
                {
                    s_isTestSuiteDisabled = true;

                    return;
                }

                throw;
            }
        }
    }

    // MSDTC is aynchronous, i.e. Commit/Rollback may return before the transaction has actually completed;
    // so allow some time for assertions to succeed.
    private static void Retry(Action action)
    {
        const int Retries = 100;

        for (var i = 0; i < Retries; i++)
        {
            try
            {
                action();
                return;
            }
            catch (EqualException)
            {
                if (i == Retries - 1)
                {
                    throw;
                }

                Thread.Sleep(100);
            }
        }
    }

    static void MinimalOleTxScenario()
    {
        using var tx = new CommittableTransaction();

        var enlistment1 = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed);
        var enlistment2 = new TestEnlistment(Phase1Vote.Prepared, EnlistmentOutcome.Committed);

        tx.EnlistDurable(Guid.NewGuid(), enlistment1, EnlistmentOptions.None);
        tx.EnlistDurable(Guid.NewGuid(), enlistment2, EnlistmentOptions.None);

        tx.Commit();
    }

    public class OleTxFixture
    {
        // In CI, we sometimes get XACT_E_TMNOTAVAILABLE on the very first attempt to connect to MSDTC;
        // this is likely due to on-demand slow startup of MSDTC. Perform pre-test connecting with retry
        // to ensure that MSDTC is properly up when the first test runs.
        public OleTxFixture()
            => Test(MinimalOleTxScenario);
    }

    private static bool s_isTestSuiteDisabled;
}
