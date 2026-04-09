// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

#nullable enable

namespace System.Transactions.Tests
{
    public enum Phase1Vote { Prepared, ForceRollback, Done };
    public enum SinglePhaseVote { Committed, Aborted, InDoubt };
    public enum EnlistmentOutcome { Committed, Aborted, InDoubt };

    public class TestSinglePhaseEnlistment : ISinglePhaseNotification
    {
        Phase1Vote _phase1Vote;
        SinglePhaseVote _singlePhaseVote;
        EnlistmentOutcome _expectedOutcome;

        public TestSinglePhaseEnlistment(Phase1Vote phase1Vote, SinglePhaseVote singlePhaseVote, EnlistmentOutcome expectedOutcome)
        {
            _phase1Vote = phase1Vote;
            _singlePhaseVote = singlePhaseVote;
            _expectedOutcome = expectedOutcome;
        }

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            switch (_singlePhaseVote)
            {
                case SinglePhaseVote.Committed:
                    {
                        singlePhaseEnlistment.Committed();
                        break;
                    }
                case SinglePhaseVote.Aborted:
                    {
                        singlePhaseEnlistment.Aborted();
                        break;
                    }
                case SinglePhaseVote.InDoubt:
                    {
                        singlePhaseEnlistment.InDoubt();
                        break;
                    }
            }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            switch (_phase1Vote)
            {
                case Phase1Vote.Prepared:
                    {
                        preparingEnlistment.Prepared();
                        break;
                    }
                case Phase1Vote.ForceRollback:
                    {
                        preparingEnlistment.ForceRollback();
                        break;
                    }
                case Phase1Vote.Done:
                    {
                        preparingEnlistment.Done();
                        break;
                    }
            }
        }

        public void Commit(Enlistment enlistment)
        {
            Assert.Equal(EnlistmentOutcome.Committed, _expectedOutcome);
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            Assert.Equal(EnlistmentOutcome.Aborted, _expectedOutcome);
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Assert.Equal(EnlistmentOutcome.InDoubt, _expectedOutcome);
            enlistment.Done();
        }
    }

    public class TestEnlistment : IEnlistmentNotification
    {
        readonly Phase1Vote _phase1Vote;
        readonly EnlistmentOutcome _expectedOutcome;
        readonly bool _volatileEnlistDuringPrepare;
        readonly bool _expectEnlistToSucceed;
        readonly AutoResetEvent? _outcomeReceived;
        readonly Transaction _txToEnlist;

        public TestEnlistment(
            Phase1Vote phase1Vote,
            EnlistmentOutcome expectedOutcome,
            bool volatileEnlistDuringPrepare = false,
            bool expectEnlistToSucceed = true,
            AutoResetEvent? outcomeReceived = null)
        {
            _phase1Vote = phase1Vote;
            _expectedOutcome = expectedOutcome;
            _volatileEnlistDuringPrepare = volatileEnlistDuringPrepare;
            _expectEnlistToSucceed = expectEnlistToSucceed;
            _outcomeReceived = outcomeReceived;
            _txToEnlist = Transaction.Current!;
        }

        public EnlistmentOutcome? Outcome { get; private set; }
        public bool WasPreparedCalled { get; private set; }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            WasPreparedCalled = true;

            switch (_phase1Vote)
            {
                case Phase1Vote.Prepared:
                    {
                        if (_volatileEnlistDuringPrepare)
                        {
                            TestEnlistment newVol = new TestEnlistment(_phase1Vote, _expectedOutcome);
                            try
                            {
                                _txToEnlist.EnlistVolatile(newVol, EnlistmentOptions.None);
                                Assert.True(_expectEnlistToSucceed);
                            }
                            catch (Exception)
                            {
                                Assert.False(_expectEnlistToSucceed);
                            }
                        }
                        preparingEnlistment.Prepared();
                        break;
                    }
                case Phase1Vote.ForceRollback:
                    {
                        _outcomeReceived?.Set();
                        preparingEnlistment.ForceRollback();
                        break;
                    }
                case Phase1Vote.Done:
                    {
                        _outcomeReceived?.Set();
                        preparingEnlistment.Done();
                        break;
                    }
            }
        }

        public void Commit(Enlistment enlistment)
        {
            Outcome = EnlistmentOutcome.Committed;
            Assert.Equal(EnlistmentOutcome.Committed, _expectedOutcome);
            _outcomeReceived?.Set();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            Outcome = EnlistmentOutcome.Aborted;
            Assert.Equal(EnlistmentOutcome.Aborted, _expectedOutcome);
            _outcomeReceived?.Set();
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Outcome = EnlistmentOutcome.InDoubt;
            Assert.Equal(EnlistmentOutcome.InDoubt, _expectedOutcome);
            _outcomeReceived?.Set();
            enlistment.Done();
        }
    }

    public class TestPromotableSinglePhaseEnlistment : IPromotableSinglePhaseNotification
    {
        private readonly Func<byte[]>? _promoteDelegate;
        private EnlistmentOutcome _expectedOutcome;
        private AutoResetEvent? _outcomeReceived;

        public bool InitializedCalled { get; private set; }
        public bool PromoteCalled { get; private set; }

        public TestPromotableSinglePhaseEnlistment(Func<byte[]>? promoteDelegate, EnlistmentOutcome expectedOutcome, AutoResetEvent? outcomeReceived = null)
        {
            _promoteDelegate = promoteDelegate;
           _expectedOutcome = expectedOutcome;
           _outcomeReceived = outcomeReceived;
        }

        public void Initialize()
            => InitializedCalled = true;

        public byte[]? Promote()
        {
            PromoteCalled = true;

            if (_promoteDelegate is null)
            {
                Assert.Fail("Promote called but no promotion delegate was provided");
            }

            return _promoteDelegate();
        }

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            Assert.Equal(EnlistmentOutcome.Committed, _expectedOutcome);

            _outcomeReceived?.Set();

            singlePhaseEnlistment.Done();
        }

        public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            Assert.Equal(EnlistmentOutcome.Aborted, _expectedOutcome);

            _outcomeReceived?.Set();

            singlePhaseEnlistment.Done();
        }
    }
}
