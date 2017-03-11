// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Transactions.Tests
{
    // Ported from Mono

    public class AsyncTest : IDisposable
    {
        public AsyncTest()
        {
            s_delayedException = null;
            s_called = false;
            s_mr.Reset();
            s_state = 0;
            Transaction.Current = null;
        }

        public void Dispose()
        {
            Transaction.Current = null;
        }

        [Fact]
        public void AsyncFail1()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IntResourceManager irm = new IntResourceManager(1);

                CommittableTransaction ct = new CommittableTransaction();
                /* Set ambient Tx */
                Transaction.Current = ct;

                /* Enlist */
                irm.Value = 2;

                IAsyncResult ar = ct.BeginCommit(null, null);
                IAsyncResult ar2 = ct.BeginCommit(null, null);
            });
        }


        [Fact]
        public void AsyncFail2()
        {
            Assert.Throws<TransactionAbortedException>(() =>
            {
                IntResourceManager irm = new IntResourceManager(1);

                CommittableTransaction ct = new CommittableTransaction();
                /* Set ambient Tx */
                Transaction.Current = ct;

                /* Enlist */
                irm.Value = 2;
                irm.FailPrepare = true;

                IAsyncResult ar = ct.BeginCommit(null, null);

                ct.EndCommit(ar);
            });
        }

        private AsyncCallback _callback = null;
        private static int s_state = 0;
        /* Callback called ? */
        private static bool s_called = false;
        private static ManualResetEvent s_mr = new ManualResetEvent(false);
        private static Exception s_delayedException;

        private static void CommitCallback(IAsyncResult ar)
        {
            s_called = true;
            CommittableTransaction ct = ar as CommittableTransaction;
            try
            {
                s_state = (int)ar.AsyncState;
                ct.EndCommit(ar);
            }
            catch (Exception e)
            {
                s_delayedException = e;
            }
            finally
            {
                s_mr.Set();
            }
        }

        [Fact]
        public void AsyncFail3()
        {
            s_delayedException = null;
            IntResourceManager irm = new IntResourceManager(1);

            CommittableTransaction ct = new CommittableTransaction();
            /* Set ambient Tx */
            Transaction.Current = ct;

            /* Enlist */
            irm.Value = 2;
            irm.FailPrepare = true;

            _callback = new AsyncCallback(CommitCallback);
            IAsyncResult ar = ct.BeginCommit(_callback, 5);
            s_mr.WaitOne(new TimeSpan(0, 0, 60));

            Assert.True(s_called, "callback not called");
            Assert.Equal(5, s_state);

            Assert.IsType<TransactionAbortedException>(s_delayedException);
        }

        [Fact]
        public void Async1()
        {
            IntResourceManager irm = new IntResourceManager(1);

            CommittableTransaction ct = new CommittableTransaction();
            /* Set ambient Tx */
            Transaction.Current = ct;
            /* Enlist */
            irm.Value = 2;

            _callback = new AsyncCallback(CommitCallback);
            IAsyncResult ar = ct.BeginCommit(_callback, 5);
            s_mr.WaitOne(new TimeSpan(0, 2, 0));

            Assert.True(s_called, "callback not called");
            Assert.Equal(5, s_state);

            if (s_delayedException != null)
                throw new Exception("", s_delayedException);
        }

        [Fact]
        public void Async2()
        {
            IntResourceManager irm = new IntResourceManager(1);

            CommittableTransaction ct = new CommittableTransaction();

            using (TransactionScope scope = new TransactionScope(ct))
            {
                irm.Value = 2;

                //scope.Complete ();

                IAsyncResult ar = ct.BeginCommit(null, null);
                Assert.Throws<TransactionAbortedException>(() => ct.EndCommit(ar));
                irm.Check(0, 0, 1, 0, "irm");
            }
        }

        [Fact]
        public void Async3()
        {
            IntResourceManager irm = new IntResourceManager(1);

            CommittableTransaction ct = new CommittableTransaction();
            /* Set ambient Tx */
            Transaction.Current = ct;

            /* Enlist */
            irm.Value = 2;

            IAsyncResult ar = ct.BeginCommit(null, null);
            ct.EndCommit(ar);

            irm.Check(1, 1, 0, 0, "irm");
        }

        [Fact]
        public void Async4()
        {
            IntResourceManager irm = new IntResourceManager(1);

            CommittableTransaction ct = new CommittableTransaction();
            /* Set ambient Tx */
            Transaction.Current = ct;

            /* Enlist */
            irm.Value = 2;

            IAsyncResult ar = ct.BeginCommit(null, null);
            ar.AsyncWaitHandle.WaitOne();
            Assert.True(ar.IsCompleted);

            irm.Check(1, 1, 0, 0, "irm");
        }

        [Fact]
        public void Async5()
        {
            IntResourceManager irm = new IntResourceManager(1);

            CommittableTransaction ct = new CommittableTransaction();
            /* Set ambient Tx */
            Transaction.Current = ct;

            /* Enlist */
            irm.Value = 2;
            irm.FailPrepare = true;

            IAsyncResult ar = ct.BeginCommit(null, null);
            ar.AsyncWaitHandle.WaitOne();
            Assert.True(ar.IsCompleted);

            CommittableTransaction ctx = ar as CommittableTransaction;
            Assert.Throws<TransactionAbortedException>(() => ctx.EndCommit(ar));
            irm.Check(1, 0, 0, 0, "irm");
        }
    }
}
