// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Transactions.Tests
{
    public class LTMEnlistmentTests : IDisposable
    {
        const int MaxTransactionCommitTimeoutInSeconds = 5;

        public LTMEnlistmentTests()
        {
            // Make sure we start with Transaction.Current = null.
            Transaction.Current = null;
        }

        public void Dispose()
        {
            Transaction.Current = null;
        }

        [Theory]
        [InlineData(1, EnlistmentOptions.None, Phase1Vote.Prepared, SinglePhaseVote.Committed, true, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
        [InlineData(2, EnlistmentOptions.None, Phase1Vote.Prepared, SinglePhaseVote.Committed, true, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
        [InlineData(1, EnlistmentOptions.None, Phase1Vote.Prepared, SinglePhaseVote.Aborted, true, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
        [InlineData(2, EnlistmentOptions.None, Phase1Vote.Prepared, SinglePhaseVote.Aborted, true, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
        [InlineData(1, EnlistmentOptions.EnlistDuringPrepareRequired, Phase1Vote.Prepared, SinglePhaseVote.Committed, true, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
        [InlineData(2, EnlistmentOptions.EnlistDuringPrepareRequired, Phase1Vote.Prepared, SinglePhaseVote.Committed, true, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
        public void SinglePhaseDurable(int volatileCount, EnlistmentOptions volatileEnlistmentOption, Phase1Vote volatilePhase1Vote, SinglePhaseVote singlePhaseVote, bool commit, EnlistmentOutcome expectedVolatileOutcome, TransactionStatus expectedTxStatus)
        {
            Transaction tx = null;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    tx = Transaction.Current.Clone();

                    if (volatileCount > 0)
                    {
                        TestSinglePhaseEnlistment[] volatiles = new TestSinglePhaseEnlistment[volatileCount];
                        for (int i = 0; i < volatileCount; i++)
                        {
                            // It doesn't matter what we specify for SinglePhaseVote.
                            volatiles[i] = new TestSinglePhaseEnlistment(volatilePhase1Vote, SinglePhaseVote.InDoubt, expectedVolatileOutcome);
                            tx.EnlistVolatile(volatiles[i], volatileEnlistmentOption);
                        }
                    }

                    // Doesn't really matter what we specify for EnlistmentOutcome here. This is an SPC, so Phase2 won't happen for this enlistment.
                    TestSinglePhaseEnlistment durable = new TestSinglePhaseEnlistment(Phase1Vote.Prepared, singlePhaseVote, EnlistmentOutcome.Committed);
                    tx.EnlistDurable(Guid.NewGuid(), durable, EnlistmentOptions.None);

                    if (commit)
                    {
                        ts.Complete();
                    }
                }
            }
            catch (TransactionInDoubtException)
            {
                Assert.Equal(TransactionStatus.InDoubt, expectedTxStatus);
            }
            catch (TransactionAbortedException)
            {
                Assert.Equal(TransactionStatus.Aborted, expectedTxStatus);
            }


            Assert.NotNull(tx);
            Assert.Equal(expectedTxStatus, tx.TransactionInformation.Status);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50969", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        // This test needs to change once we have promotion support.
        // Right now any attempt to create a two phase durable enlistment will attempt to promote and will fail because promotion is not supported. This results in the transaction being
        // aborted.
        [InlineData(0, EnlistmentOptions.None, EnlistmentOptions.None, Phase1Vote.Prepared, true, EnlistmentOutcome.Aborted, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
        [InlineData(1, EnlistmentOptions.None, EnlistmentOptions.None, Phase1Vote.Prepared, true, EnlistmentOutcome.Aborted, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
        [InlineData(2, EnlistmentOptions.None, EnlistmentOptions.None, Phase1Vote.Prepared, true, EnlistmentOutcome.Aborted, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
        public void TwoPhaseDurable(int volatileCount, EnlistmentOptions volatileEnlistmentOption, EnlistmentOptions durableEnlistmentOption, Phase1Vote volatilePhase1Vote, bool commit, EnlistmentOutcome expectedVolatileOutcome, EnlistmentOutcome expectedDurableOutcome, TransactionStatus expectedTxStatus)
        {
            Transaction tx = null;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    tx = Transaction.Current.Clone();

                    if (volatileCount > 0)
                    {
                        TestEnlistment[] volatiles = new TestEnlistment[volatileCount];
                        for (int i = 0; i < volatileCount; i++)
                        {
                            // It doesn't matter what we specify for SinglePhaseVote.
                            volatiles[i] = new TestEnlistment(volatilePhase1Vote, expectedVolatileOutcome);
                            tx.EnlistVolatile(volatiles[i], volatileEnlistmentOption);
                        }
                    }

                    TestEnlistment durable = new TestEnlistment(Phase1Vote.Prepared, expectedDurableOutcome);
                    // This needs to change once we have promotion support.
                    Assert.Throws<PlatformNotSupportedException>(() => // Creation of two phase durable enlistment attempts to promote to MSDTC
                    {
                        tx.EnlistDurable(Guid.NewGuid(), durable, durableEnlistmentOption);
                    });

                    if (commit)
                    {
                        ts.Complete();
                    }
                }
            }
            catch (TransactionInDoubtException)
            {
                Assert.Equal(TransactionStatus.InDoubt, expectedTxStatus);
            }
            catch (TransactionAbortedException)
            {
                Assert.Equal(TransactionStatus.Aborted, expectedTxStatus);
            }

            Assert.NotNull(tx);
            Assert.Equal(expectedTxStatus, tx.TransactionInformation.Status);
        }

        [Theory]
        [InlineData(EnlistmentOptions.EnlistDuringPrepareRequired, Phase1Vote.Prepared, true, true, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
        [InlineData(EnlistmentOptions.None, Phase1Vote.Prepared, false, true, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
        public void EnlistDuringPhase0(EnlistmentOptions enlistmentOption, Phase1Vote phase1Vote, bool expectPhase0EnlistSuccess, bool commit, EnlistmentOutcome expectedOutcome, TransactionStatus expectedTxStatus)
        {
            Transaction tx = null;
            AutoResetEvent outcomeEvent = null;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    tx = Transaction.Current.Clone();
                    outcomeEvent = new AutoResetEvent(false);
                    TestEnlistment enlistment = new TestEnlistment(phase1Vote, expectedOutcome, true, expectPhase0EnlistSuccess, outcomeEvent);
                    tx.EnlistVolatile(enlistment, enlistmentOption);

                    if (commit)
                    {
                        ts.Complete();
                    }
                }
            }
            catch (TransactionInDoubtException)
            {
                Assert.Equal(TransactionStatus.InDoubt, expectedTxStatus);
            }
            catch (TransactionAbortedException)
            {
                Assert.Equal(TransactionStatus.Aborted, expectedTxStatus);
            }

            Assert.True(outcomeEvent.WaitOne(TimeSpan.FromSeconds(MaxTransactionCommitTimeoutInSeconds)));
            Assert.NotNull(tx);
            Assert.Equal(expectedTxStatus, tx.TransactionInformation.Status);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(5, EnlistmentOptions.None, Phase1Vote.Prepared, Phase1Vote.Prepared, true, EnlistmentOutcome.Committed, TransactionStatus.Committed)]
        [InlineData(5, EnlistmentOptions.None, Phase1Vote.Prepared, Phase1Vote.ForceRollback, true, EnlistmentOutcome.Aborted, TransactionStatus.Aborted)]
        public void EnlistVolatile(int volatileCount, EnlistmentOptions enlistmentOption, Phase1Vote volatilePhase1Vote, Phase1Vote lastPhase1Vote, bool commit, EnlistmentOutcome expectedEnlistmentOutcome, TransactionStatus expectedTxStatus)
        {
            AutoResetEvent[] outcomeEvents = null;
            Transaction tx = null;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    tx = Transaction.Current.Clone();

                    if (volatileCount > 0)
                    {
                        TestEnlistment[] volatiles = new TestEnlistment[volatileCount];
                        outcomeEvents = new AutoResetEvent[volatileCount];
                        for (int i = 0; i < volatileCount-1; i++)
                        {
                            outcomeEvents[i] = new AutoResetEvent(false);
                            volatiles[i] = new TestEnlistment(volatilePhase1Vote, expectedEnlistmentOutcome, false, true, outcomeEvents[i]);
                            tx.EnlistVolatile(volatiles[i], enlistmentOption);
                        }

                        outcomeEvents[volatileCount-1] = new AutoResetEvent(false);
                        volatiles[volatileCount - 1] = new TestEnlistment(lastPhase1Vote, expectedEnlistmentOutcome, false, true, outcomeEvents[volatileCount-1]);
                        tx.EnlistVolatile(volatiles[volatileCount - 1], enlistmentOption);
                    }

                    if (commit)
                    {
                        ts.Complete();
                    }
                }
            }
            catch (TransactionInDoubtException)
            {
                Assert.Equal(TransactionStatus.InDoubt, expectedTxStatus);
            }
            catch (TransactionAbortedException)
            {
                Assert.Equal(TransactionStatus.Aborted, expectedTxStatus);
            }

            Task.Run(() => // in case current thread is STA thread, where WaitHandle.WaitAll isn't supported
            {
                Assert.True(WaitHandle.WaitAll(outcomeEvents, TimeSpan.FromSeconds(MaxTransactionCommitTimeoutInSeconds)));
            }).GetAwaiter().GetResult();

            Assert.NotNull(tx);
            Assert.Equal(expectedTxStatus, tx.TransactionInformation.Status);
        }

    }
}
