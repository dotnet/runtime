// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.Transactions.Tests
{
    // Ported from Mono

    public class IntResourceManager
    {
        public IntResourceManager(int value)
        {
            _actual = value;
            _guid = Guid.NewGuid();
        }

        private int _actual;
        private int _tmpValue;
        private Transaction _transaction = null;

        public int NumPrepare = 0;
        public int NumRollback = 0;
        public int NumCommit = 0;
        public int NumInDoubt = 0;
        public int NumSingle = 0;

        public int NumInitialize = 0;
        public int NumPromote = 0;
        public int NumEnlistFailed = 0;

        public ResourceManagerType Type = ResourceManagerType.Volatile;
        public bool FailPrepare = false;
        public bool FailWithException = false;
        public bool IgnorePrepare = false;
        public bool IgnoreSPC = false;
        public bool FailSPC = false;
        public bool FailCommit = false;
        public bool FailRollback = false;
        public bool UseSingle = false;
        public Exception ThrowThisException = null;

        private Guid _guid;

        public int Actual
        {
            get { return _actual; }
        }

        public int Value
        {
            get { return _transaction == null ? _actual : _tmpValue; }
            set
            {
                if (Transaction.Current == null)
                {
                    /* Not in a transaction */
                    _actual = value;
                    return;
                }
                /* FIXME: Do what in this case? */
                if (_transaction != null)
                    Debug.WriteLine("WARNING: Setting value more than once");

                if (_transaction != Transaction.Current)
                {
                    _transaction = Transaction.Current;

                    if (Type == ResourceManagerType.Promotable)
                    {
                        _transaction.EnlistPromotableSinglePhase(new PromotableSinglePhaseNotification(this));

                        // TODO: EnlistPromotableSinglePhase will not throw an exception if there is already another PSPE or
                        // a DurableEnlistment. Instead, it returns false. Right now the tests that exercise these scenarios do
                        // not take this into account. This scenario should be taken into account to correctly test EnlistPromotableSinglePhase.
                        // One option is to count a "false" return as an enlistment failure.
                    }
                    else if (UseSingle)
                    {
                        SinglePhaseNotification enlistment = new SinglePhaseNotification(this);
                        if (Type == ResourceManagerType.Volatile)
                            _transaction.EnlistVolatile(enlistment, EnlistmentOptions.None);
                        else
                            _transaction.EnlistDurable(_guid, enlistment, EnlistmentOptions.None);
                    }
                    else
                    {
                        EnlistmentNotification enlistment = new EnlistmentNotification(this);
                        if (Type == ResourceManagerType.Volatile)
                            _transaction.EnlistVolatile(enlistment, EnlistmentOptions.None);
                        else
                            _transaction.EnlistDurable(_guid, enlistment, EnlistmentOptions.None);
                    }
                }
                _tmpValue = value;
            }
        }

        public void Commit()
        {
            _actual = _tmpValue;
            _transaction = null;
        }

        public void Rollback()
        {
            _transaction = null;
        }

        public void CheckSPC(string msg)
        {
            Check(1, 0, 0, 0, 0, 0, 0, msg);
        }

        public void Check2PC(string msg)
        {
            Check(0, 1, 1, 0, 0, 0, 0, msg);
        }

        public void Check(int s, int p, int c, int r, int d, int i, int pr, string msg)
        {
            Assert.Equal(s, NumSingle);
            Assert.Equal(p, NumPrepare);
            Assert.Equal(c, NumCommit);
            Assert.Equal(r, NumRollback);
            Assert.Equal(d, NumInDoubt);
            Assert.Equal(i, NumInitialize);
            Assert.Equal(pr, NumPromote);
        }

        /* Used for volatile RMs */
        public void Check(int p, int c, int r, int d, string msg)
        {
            Check(0, p, c, r, d, 0, 0, msg);
        }
    }

    public class EnlistmentNotification : IEnlistmentNotification
    {
        protected IntResourceManager resource;

        public EnlistmentNotification(IntResourceManager resource)
        {
            this.resource = resource;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            resource.NumPrepare++;
            if (resource.IgnorePrepare)
                return;

            if (resource.FailPrepare)
            {
                if (resource.FailWithException)
                    preparingEnlistment.ForceRollback(resource.ThrowThisException ?? new NotSupportedException());
                else
                    preparingEnlistment.ForceRollback();
            }
            else
            {
                preparingEnlistment.Prepared();
            }
        }

        public void Commit(Enlistment enlistment)
        {
            resource.NumCommit++;
            if (resource.FailCommit)
            {
                if (resource.FailWithException)
                    throw (resource.ThrowThisException ?? new NotSupportedException());
                else
                    return;
            }

            resource.Commit();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            resource.NumRollback++;
            if (resource.FailRollback)
            {
                if (resource.FailWithException)
                    throw (resource.ThrowThisException ?? new NotSupportedException());
                else
                    return;
            }

            resource.Rollback();
        }

        public void InDoubt(Enlistment enlistment)
        {
            resource.NumInDoubt++;
            throw new Exception("IntResourceManager.InDoubt is not implemented.");
        }
    }

    public class SinglePhaseNotification : EnlistmentNotification, ISinglePhaseNotification
    {
        public SinglePhaseNotification(IntResourceManager resource)
            : base(resource)
        {
        }

        public void SinglePhaseCommit(SinglePhaseEnlistment enlistment)
        {
            resource.NumSingle++;
            if (resource.IgnoreSPC)
                return;

            if (resource.FailSPC)
            {
                if (resource.FailWithException)
                    enlistment.Aborted(resource.ThrowThisException ?? new NotSupportedException());
                else
                    enlistment.Aborted();
            }
            else
            {
                resource.Commit();
                enlistment.Committed();
            }
        }
    }

    public class PromotableSinglePhaseNotification : SinglePhaseNotification, IPromotableSinglePhaseNotification
    {
        public PromotableSinglePhaseNotification(IntResourceManager resource)
            : base(resource)
        {
        }

        public void Initialize()
        {
            resource.NumInitialize++;
        }

        public void Rollback(SinglePhaseEnlistment enlistment)
        {
            resource.NumRollback++;
            resource.Rollback();
        }

        public byte[] Promote()
        {
            resource.NumPromote++;
            return new byte[0];
        }
    }

    public enum ResourceManagerType { Volatile, Durable, Promotable };
}

