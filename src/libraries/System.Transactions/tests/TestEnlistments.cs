// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
            Assert.Equal(_expectedOutcome, EnlistmentOutcome.Committed);
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            Assert.Equal(_expectedOutcome, EnlistmentOutcome.Aborted);
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Assert.Equal(_expectedOutcome, EnlistmentOutcome.InDoubt);
            enlistment.Done();
        }
    }

    public class TestEnlistment : IEnlistmentNotification
    {
        Phase1Vote _phase1Vote;
        EnlistmentOutcome _expectedOutcome;
        bool _volatileEnlistDuringPrepare;
        bool _expectEnlistToSucceed;
        AutoResetEvent _outcomeReceived;
        Transaction _txToEnlist;

        public TestEnlistment(Phase1Vote phase1Vote, EnlistmentOutcome expectedOutcome, bool volatileEnlistDuringPrepare = false, bool expectEnlistToSucceed = true, AutoResetEvent outcomeReceived = null)
        {
            _phase1Vote = phase1Vote;
            _expectedOutcome = expectedOutcome;
            _volatileEnlistDuringPrepare = volatileEnlistDuringPrepare;
            _expectEnlistToSucceed = expectEnlistToSucceed;
            _outcomeReceived = outcomeReceived;
            _txToEnlist = Transaction.Current;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
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
                                Assert.Equal(_expectEnlistToSucceed, true);
                            }
                            catch (Exception)
                            {
                                Assert.Equal(_expectEnlistToSucceed, false);
                            }
                        }
                        preparingEnlistment.Prepared();
                        break;
                    }
                case Phase1Vote.ForceRollback:
                    {
                        if (_outcomeReceived != null)
                        {
                            _outcomeReceived.Set();
                        }
                        preparingEnlistment.ForceRollback();
                        break;
                    }
                case Phase1Vote.Done:
                    {
                        if (_outcomeReceived != null)
                        {
                            _outcomeReceived.Set();
                        }
                        preparingEnlistment.Done();
                        break;
                    }
            }
        }

        public void Commit(Enlistment enlistment)
        {
            Assert.Equal(_expectedOutcome, EnlistmentOutcome.Committed);
            if (_outcomeReceived != null)
            {
                _outcomeReceived.Set();
            }
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            Assert.Equal(_expectedOutcome, EnlistmentOutcome.Aborted);
            if (_outcomeReceived != null)
            {
                _outcomeReceived.Set();
            }
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Assert.Equal(_expectedOutcome, EnlistmentOutcome.InDoubt);
            if (_outcomeReceived != null)
            {
                _outcomeReceived.Set();
            }
            enlistment.Done();
        }
    }
}