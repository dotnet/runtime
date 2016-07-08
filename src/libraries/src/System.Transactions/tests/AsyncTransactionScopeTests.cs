// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Transactions.Tests
{
    public class AsyncTransactionScopeTests
    {
        // Number of threads to create
        private const int iterations = 5;

        // The work queue that requests will be placed in to be serviced by the background thread
        private static BlockingCollection<Tuple<int, TaskCompletionSource<object>, Transaction>> s_workQueue = new BlockingCollection<Tuple<int, TaskCompletionSource<object>, Transaction>>();

        private static bool s_throwExceptionDefaultOrBeforeAwait;
        private static bool s_throwExceptionAfterAwait;

        /// <summary>
        /// This test case will verify various Async TransactionScope usage with task and async/await and also nested mixed mode(legacy TS and async TS) usage.
        /// </summary>
        [Fact]
        public void AsyncTSTest()
        {
            // Running exception test first to make sure any unintentional leak in ambient transaction during exception will be detected when subsequent test are run.
            HandleException(true, false, () => DoSyncTxWork(true, null));
            HandleException(true, false, () => AssertTransactionNullAndWaitTask(DoAsyncTxWorkAsync(true, null)));
            HandleException(true, false, () => SyncTSL2NestedTxWork(false, false, true, null));
            HandleException(false, true, () => AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, true, false, null)));
            HandleException(true, false, () => AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, true, false, false, null)));
            HandleException(true, false, () => AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, false, null)));
            HandleException(false, true, () => AssertTransactionNullAndWaitTask(DoTaskWorkAsync(false, false, null)));
            HandleException(false, true, () => SyncTSTaskWork(false, null));

            // The following test has Task under TransactionScope and has few variations.  
            DoTaskUnderAsyncTS(false, null);
            Task.Factory.StartNew(() => DoTaskUnderAsyncTS(false, null)).Wait();
            SyncTSDoTaskUnderAsyncTS(false, false, null);
            SyncTSDoTaskUnderAsyncTS(false, true, null);
            Task.Factory.StartNew(() => SyncTSDoTaskUnderAsyncTS(false, true, null)).Wait();

            // Simple Sync TS test  
            DoSyncTxWork(false, null);
            DoSyncTxWork(true, null);

            // Simple Async TS test. "await" points explicitly switches threads across continuations.  
            AssertTransactionNullAndWaitTask(DoAsyncTxWorkAsync(false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTxWorkAsync(true, null));

            // Nested TS test. Parent is Sync TS, child TS can be sync or async.
            SyncTSL2NestedTxWork(false, false, false, null);
            SyncTSL2NestedTxWork(false, false, true, null);
            SyncTSL2NestedTxWork(false, true, false, null);
            SyncTSL2NestedTxWork(false, true, true, null);
            SyncTSL2NestedTxWork(true, false, false, null);
            SyncTSL2NestedTxWork(true, false, true, null);
            SyncTSL2NestedTxWork(true, true, false, null);
            SyncTSL2NestedTxWork(true, true, true, null);

            // 2 level deep nested TS test. Parent is Aync TS, child TS can be sync or async. 
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, false, false, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, false, true, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, true, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, false, false, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, false, true, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, true, false, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, true, true, false, false, null));

            // Introduce various "await" points to switch threads before/after child TransactionScope.
            // Introduce some Task variations by running the test under Task. 
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, true, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, false, true, null));
            Task.Factory.StartNew(() => AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, false, true, null))).Wait();
            AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, true, true, null));

            // 3 level deep nested TS test. 
            SyncTSL3AsyncTSL2NestedTxWork(false, false, true, false, false, true, null);
            Task.Factory.StartNew(() => SyncTSL3AsyncTSL2NestedTxWork(false, false, true, false, false, true, null)).Wait();
            SyncTSL3AsyncTSL2NestedTxWork(false, false, true, false, true, false, null);

            AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, false, true, false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, false, true, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, true, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, true, true, null));
            Task.Factory.StartNew(() => AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, true, true, null))).Wait();

            AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, false, true, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, true, null));
            Task.Factory.StartNew(() => AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, true, null))).Wait();

            // Have bunch of parallel tasks running various nested TS test cases. There parallel tasks are wrapped by a TransactionScope. 
            AssertTransactionNullAndWaitTask(DoTaskWorkAsync(false, false, null));
            SyncTSTaskWork(false, null);
            SyncTSTaskWork(true, null);
            AssertTransactionNullAndWaitTask(DoAsyncTSTaskWorkAsync(false, false, null));
            AssertTransactionNullAndWaitTask(DoAsyncTSTaskWorkAsync(true, false, null));

            // Final test - wrap the DoAsyncTSTaskWorkAsync in syncronous scope
            string txId1 = null;
            string txId2 = null;

            using (TransactionScope scope = new TransactionScope())
            {
                txId1 = AssertAndGetCurrentTransactionId();
                Task task = DoAsyncTSTaskWorkAsync(false, false, txId1);
                txId2 = AssertAndGetCurrentTransactionId();
                task.Wait();
                scope.Complete();
            }

            VerifyTxId(false, null, txId1, txId2);
        }

        [Theory]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        public void AsyncTSAndDependantClone(bool requiresNew, bool syncronizeScope, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            string txId3 = null;
            string txId4 = null;
            string txId5 = null;
            string txId6 = null;
            string txId7 = null;
            Task task2;

            AssertTransaction(txId);

            using (TransactionScope scope1 = new TransactionScope(requiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                DependentTransaction dependentTx = Transaction.Current.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
                Task task1 = Task.Run(delegate
                {
                    try
                    {
                        // Since we use BlockCommitUntilComplete dependent transaction to syncronize the root TransactionScope, the ambient Tx may not be available and will be be disposed and block on Commit.
                        // The flag will ensure we explicitly syncronize before disposing the root TransactionScope and the ambient transaction will still be available in the Task.
                        if (syncronizeScope)
                        {
                            txId2 = AssertAndGetCurrentTransactionId();
                        }

                        using (TransactionScope scope2 = new TransactionScope(dependentTx))
                        {
                            txId3 = AssertAndGetCurrentTransactionId();
                            Task.Delay(10).Wait();
                            scope2.Complete();
                        }

                        if (syncronizeScope)
                        {
                            txId4 = AssertAndGetCurrentTransactionId();
                        }
                    }
                    finally
                    {
                        dependentTx.Complete();
                        dependentTx.Dispose();
                    }

                    if (syncronizeScope)
                    {
                        txId5 = AssertAndGetCurrentTransactionId();
                    }
                });

                task2 = Task.Run(delegate
                {
                    using (TransactionScope scope3 = new TransactionScope(TransactionScopeOption.RequiresNew))
                    {
                        txId7 = AssertAndGetCurrentTransactionId();
                        scope3.Complete();
                    }
                });

                txId6 = AssertAndGetCurrentTransactionId();

                if (syncronizeScope)
                {
                    task1.Wait();
                }
                scope1.Complete();
            }

            task2.Wait();

            VerifyTxId(requiresNew, txId, txId1, txId6);

            Assert.Equal(txId1, txId3);
            Assert.Equal(txId3, txId6);
            if (syncronizeScope)
            {
                Assert.Equal(txId1, txId2);
                Assert.Equal(txId1, txId4);
                Assert.Equal(txId4, txId5);
            }
            Assert.NotEqual(txId1, txId7);

            AssertTransaction(txId);
        }

        [Theory]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        public void NestedAsyncTSAndDependantClone(bool parentrequiresNew, bool childRequiresNew, string txId)
        {
            string txId1;
            string txId2;

            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(parentrequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                AsyncTSAndDependantClone(childRequiresNew, true, txId1);
                txId2 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            AssertTransaction(txId);

            VerifyTxId(parentrequiresNew, txId, txId1, txId2);
        }

        private async Task DoAsyncTSTaskWorkAsync(bool requiresNew, bool parentAsync, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            string txId3 = null;
            string txId4 = null;
            string txId5 = null;

            Task task1;
            int startThreadId, endThreadId;
            bool parentScopePresent = false;

            if (!string.IsNullOrEmpty(txId))
            {
                parentScopePresent = true;
            }

            startThreadId = Environment.CurrentManagedThreadId;

            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(requiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                task1 = Task.Run(delegate
                {
                    txId3 = AssertAndGetCurrentTransactionId();
                    SyncTSTaskWork(requiresNew, txId1);
                    txId4 = AssertAndGetCurrentTransactionId();
                });

                await DoTaskWorkAsync(requiresNew, true, txId1);
                txId5 = AssertAndGetCurrentTransactionId();

                await task1;

                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            VerifyTxId(requiresNew, txId, txId1, txId2);
            Assert.Equal(txId1, txId5);

            endThreadId = Environment.CurrentManagedThreadId;

            if (parentScopePresent)
            {
                if (parentAsync)
                {
                    AssertTransaction(txId);
                }
                else
                {
                    if (startThreadId != endThreadId)
                    {
                        AssertTransactionNull();
                    }
                    else
                    {
                        AssertTransaction(txId);
                    }
                }
            }
            else
            {
                AssertTransactionNull();
            }
        }

        private void SyncTSTaskWork(bool requiresNew, string txId)
        {
            string txId1 = null;
            string txId2 = null;

            using (TransactionScope scope = new TransactionScope(requiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                Task task = DoTaskWorkAsync(requiresNew, false, txId1);
                txId2 = AssertAndGetCurrentTransactionId();
                task.Wait();
                scope.Complete();
            }

            VerifyTxId(requiresNew, txId, txId1, txId2);
        }

        private async Task DoTaskWorkAsync(bool requiresNew, bool parentAsync, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            string txId3 = null;
            string txId4 = null;
            string txId5 = null;
            string txId6 = null;
            string txId7 = null;
            string txId8 = null;
            string txId9 = null;
            string txId10 = null;
            string txId11 = null;
            string txId12 = null;
            string txId13 = null;
            string txId14 = null;
            string txId15 = null;

            Task task1, task2, task3, task4, task5, task6;
            int startThreadId, endThreadId;
            bool parentScopePresent = false;

            if (!string.IsNullOrEmpty(txId))
            {
                parentScopePresent = true;
            }

            startThreadId = Environment.CurrentManagedThreadId;

            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(requiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                task1 = Task.Run(async delegate
                {
                    txId3 = AssertAndGetCurrentTransactionId();
                    await DoAsyncTSL2NestedTxWorkAsync(false, false, true, false, false, txId1);
                    txId4 = AssertAndGetCurrentTransactionId();
                });

                task2 = Task.Run(async delegate
                {
                    txId5 = AssertAndGetCurrentTransactionId();
                    await DoAsyncTSL2NestedTxWorkAsync(true, false, true, false, false, txId5);

                    txId6 = AssertAndGetCurrentTransactionId();
                });

                task3 = Task.Run(delegate
                {
                    txId7 = AssertAndGetCurrentTransactionId();
                    NestedAsyncTSAndDependantClone(false, true, txId7);
                    txId8 = AssertAndGetCurrentTransactionId();
                });

                task4 = Task.Run(delegate
                {
                    txId9 = AssertAndGetCurrentTransactionId();
                    SyncTSL2NestedTxWork(false, false, true, txId6);
                    txId10 = AssertAndGetCurrentTransactionId();

                    Task.Run(delegate
                    {
                        txId15 = AssertAndGetCurrentTransactionId();
                    }).Wait();
                });

                task5 = Task.Run(async delegate
                {
                    txId11 = AssertAndGetCurrentTransactionId();
                    await DoAsyncTSL2NestedTxWorkAsync(false, true, false, false, true, txId11);
                    txId12 = AssertAndGetCurrentTransactionId();
                });

                task6 = Task.Run(delegate
                {
                    txId13 = AssertAndGetCurrentTransactionId();
                    SyncTSL3AsyncTSL2NestedTxWork(false, false, true, false, false, true, txId13);
                    txId14 = AssertAndGetCurrentTransactionId();
                });

                await DoAsyncTSL2NestedTxWorkAsync(false, false, true, false, false, txId1);

                txId2 = AssertAndGetCurrentTransactionId();

                Task.WaitAll(task1, task2, task3, task4, task5, task6);
                scope.Complete();
            }

            VerifyTxId(requiresNew, txId, txId1, txId2);
            VerifyTxId(false, txId1, txId3, txId4);
            VerifyTxId(false, txId1, txId5, txId6);
            VerifyTxId(false, txId1, txId7, txId8);
            VerifyTxId(false, txId1, txId9, txId10);
            VerifyTxId(false, txId1, txId11, txId12);
            VerifyTxId(false, txId1, txId13, txId14);

            Assert.Equal(txId1, txId15);
            endThreadId = Environment.CurrentManagedThreadId;

            if (parentScopePresent)
            {
                if (parentAsync)
                {
                    AssertTransaction(txId);
                }
                else
                {
                    if (startThreadId != endThreadId)
                    {
                        AssertTransactionNull();
                    }
                    else
                    {
                        AssertTransaction(txId);
                    }
                }
            }
            else
            {
                AssertTransactionNull();
            }
        }

        [Fact]
        public void LegacyNestedTxScope()
        {
            string txId1 = null;
            string txId2 = null;
            string txId3 = null;
            string txId4 = null;
            string txId5 = null;
            string txId6 = null;

            Debug.WriteLine("Running NestedScopeTest");
            AssertTransactionNull();

            // Test Sync nested TransactionScope behavior.
            using (TransactionScope scope = new TransactionScope())
            {
                txId1 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            AssertTransactionNull();

            using (TransactionScope scope2 = new TransactionScope(TransactionScopeOption.Required))
            {
                txId2 = AssertAndGetCurrentTransactionId();

                using (TransactionScope scope3 = new TransactionScope(TransactionScopeOption.Suppress))
                {
                    AssertTransactionNull();
                    scope3.Complete();
                }

                txId6 = AssertAndGetCurrentTransactionId();

                using (TransactionScope scope4 = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    txId3 = AssertAndGetCurrentTransactionId();
                    scope4.Complete();
                }

                txId4 = AssertAndGetCurrentTransactionId();
                scope2.Complete();
            }

            AssertTransactionNull();

            using (TransactionScope scope = new TransactionScope())
            {
                txId5 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            AssertTransactionNull();

            Assert.Equal(txId2, txId4);
            Assert.Equal(txId2, txId6);
            Assert.NotEqual(txId1, txId2);
            Assert.NotEqual(txId2, txId3);
            Assert.NotEqual(txId1, txId5);
        }

        [Theory]
        // Async TS nested inside Sync TS
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)]
        // Sync TS nested inside Async TS. 
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress)]
        // Async TS nested inside Async TS
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)]
        public void VerifyNestedTxScope(TransactionScopeOption parentScopeOption, TransactionScopeAsyncFlowOption parentAsyncFlowOption, TransactionScopeOption childScopeOption, TransactionScopeAsyncFlowOption childAsyncFlowOption)
        {
            string txId1;
            string txId2;
            string txId3;

            AssertTransactionNull();

            using (TransactionScope parent = new TransactionScope(parentScopeOption, parentAsyncFlowOption))
            {
                txId1 = AssertAndGetCurrentTransactionId(parentScopeOption);

                using (TransactionScope child = new TransactionScope(childScopeOption, childAsyncFlowOption))
                {
                    txId2 = AssertAndGetCurrentTransactionId(childScopeOption);
                    child.Complete();
                }

                txId3 = AssertAndGetCurrentTransactionId(parentScopeOption);
                parent.Complete();
            }

            AssertTransactionNull();
            Assert.Equal(txId1, txId3);
            switch (childScopeOption)
            {
                case TransactionScopeOption.Required:
                    if (parentScopeOption == TransactionScopeOption.Suppress)
                    {
                        Assert.NotEqual(txId1, txId2);
                    }
                    else
                    {
                        Assert.Equal(txId1, txId2);
                    }
                    break;
                case TransactionScopeOption.RequiresNew:
                    Assert.NotEqual(txId1, txId2);
                    break;
                case TransactionScopeOption.Suppress:
                    if (parentScopeOption == TransactionScopeOption.Suppress)
                    {
                        Assert.Equal(txId1, txId2);
                    }
                    else
                    {
                        Assert.NotEqual(txId1, txId2);
                    }
                    break;
            }
        }

        [Theory]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Suppress)]
        public void VerifyParentTwoChildTest(TransactionScopeAsyncFlowOption parentAsyncFlowOption, TransactionScopeOption child1ScopeOption, TransactionScopeAsyncFlowOption child1AsyncFlowOption, TransactionScopeOption child2ScopeOption, TransactionScopeAsyncFlowOption child2AsyncFlowOption)
        {
            string txId1;
            string txId2;
            string txId3;
            string txId4;
            string txId5;

            AssertTransactionNull();

            using (TransactionScope parent = new TransactionScope(parentAsyncFlowOption))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                using (TransactionScope child1 = new TransactionScope(child1ScopeOption, child1AsyncFlowOption))
                {
                    txId2 = AssertAndGetCurrentTransactionId(child1ScopeOption);
                    child1.Complete();
                }

                txId3 = AssertAndGetCurrentTransactionId();

                using (TransactionScope child2 = new TransactionScope(child2ScopeOption, child2AsyncFlowOption))
                {
                    txId4 = AssertAndGetCurrentTransactionId(child2ScopeOption);
                    child2.Complete();
                }

                txId5 = AssertAndGetCurrentTransactionId();
                parent.Complete();
            }

            AssertTransactionNull();
            Assert.Equal(txId1, txId3);
            Assert.Equal(txId3, txId5);

            if (child1ScopeOption == TransactionScopeOption.Required)
            {
                Assert.Equal(txId1, txId2);
            }
            else
            {
                Assert.NotEqual(txId1, txId2);
                if (child1ScopeOption == TransactionScopeOption.Suppress)
                {
                    Assert.Equal(txId2, null);
                }
            }

            if (child2ScopeOption == TransactionScopeOption.Required)
            {
                Assert.Equal(txId1, txId4);
            }
            else
            {
                Assert.NotEqual(txId1, txId4);
                if (child2ScopeOption == TransactionScopeOption.Suppress)
                {
                    Assert.Equal(txId4, null);
                }
            }
        }

        [Theory]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, false)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, false)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Required, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, false)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Required, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.RequiresNew, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.RequiresNew, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, false)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, false)]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeOption.Suppress, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Suppress, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, true)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, false)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeAsyncFlowOption.Enabled, TransactionScopeOption.Suppress, true)]
        public void VerifyThreeTxScope(TransactionScopeAsyncFlowOption asyncFlowOption1, TransactionScopeAsyncFlowOption asyncFlowOption2, TransactionScopeAsyncFlowOption asyncFlowOption3, TransactionScopeOption scopeOption, bool nested)
        {
            string txId1;
            string txId2;
            string txId3;
            string txId4;
            string txId5;

            AssertTransactionNull();

            if (nested)
            {
                using (TransactionScope scope1 = new TransactionScope(scopeOption, asyncFlowOption1))
                {
                    txId1 = AssertAndGetCurrentTransactionId(scopeOption);
                    using (TransactionScope scope2 = new TransactionScope(scopeOption, asyncFlowOption2))
                    {
                        txId2 = AssertAndGetCurrentTransactionId(scopeOption);
                        using (TransactionScope scope3 = new TransactionScope(scopeOption, asyncFlowOption3))
                        {
                            txId3 = AssertAndGetCurrentTransactionId(scopeOption);
                            scope3.Complete();
                        }

                        txId4 = AssertAndGetCurrentTransactionId(scopeOption);
                        scope2.Complete();
                    }

                    txId5 = AssertAndGetCurrentTransactionId(scopeOption);
                    scope1.Complete();
                }

                AssertTransactionNull();
                Assert.Equal(txId1, txId5);
                Assert.Equal(txId2, txId4);
                if (scopeOption == TransactionScopeOption.RequiresNew)
                {
                    Assert.NotEqual(txId1, txId2);
                    Assert.NotEqual(txId2, txId3);
                }
                else
                {
                    Assert.Equal(txId1, txId2);
                    Assert.Equal(txId2, txId3);
                }
            }
            else
            {
                using (TransactionScope scope1 = new TransactionScope(scopeOption, asyncFlowOption1))
                {
                    txId1 = AssertAndGetCurrentTransactionId(scopeOption);
                    scope1.Complete();
                }

                AssertTransactionNull();
                using (TransactionScope scope2 = new TransactionScope(scopeOption, asyncFlowOption2))
                {
                    txId2 = AssertAndGetCurrentTransactionId(scopeOption);
                    scope2.Complete();
                }

                AssertTransactionNull();
                using (TransactionScope scope3 = new TransactionScope(scopeOption, asyncFlowOption3))
                {
                    txId3 = AssertAndGetCurrentTransactionId(scopeOption);
                    scope3.Complete();
                }

                if (scopeOption == TransactionScopeOption.Suppress)
                {
                    Assert.Equal(txId1, null);
                    Assert.Equal(txId1, txId2);
                    Assert.Equal(txId2, txId3);
                }
                else
                {
                    Assert.NotEqual(txId1, txId2);
                    Assert.NotEqual(txId2, txId3);
                }
            }

            AssertTransactionNull();
        }

        [Theory]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled)]
        public void VerifyBYOT(TransactionScopeAsyncFlowOption asyncFlowOption)
        {
            string txId1;
            string txId2;
            string txId3;

            AssertTransactionNull();
            CommittableTransaction tx = new CommittableTransaction();
            Transaction.Current = tx;

            txId1 = AssertAndGetCurrentTransactionId();
            using (TransactionScope scope = new TransactionScope(asyncFlowOption))
            {
                txId2 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            txId3 = AssertAndGetCurrentTransactionId();
            Transaction.Current = null;
            tx.Commit();

            Assert.Equal(txId1, txId2);
            Assert.Equal(txId2, txId3);

            AssertTransactionNull();
        }

        [Fact]
        public void VerifyBYOTOpenConnSimulationTest()
        {
            // Create threads to do work
            Task[] threads = new Task[iterations];
            for (int i = 0; i < iterations; i++)
            {
                threads[i] = Task.Factory.StartNew((object o) => { SimulateOpenConnTestAsync((int)o).Wait(); }, i, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Wait();
            }
        }

        [Fact]
        public void VerifyBYOTSyncTSNestedAsync()
        {
            string txId1;
            string txId2;

            AssertTransactionNull();

            CommittableTransaction tx = new CommittableTransaction();
            Transaction.Current = tx;

            txId1 = AssertAndGetCurrentTransactionId();
            SyncTSL3AsyncTSL2NestedTxWork(false, false, false, true, false, false, txId1);
            txId2 = AssertAndGetCurrentTransactionId();

            Transaction.Current = null;
            tx.Commit();

            Assert.Equal(txId1, txId2);

            AssertTransactionNull();
        }

        [Fact]
        public void VerifyBYOTAsyncTSNestedAsync()
        {
            string txId1;
            string txId2;

            AssertTransactionNull();
            CommittableTransaction tx = new CommittableTransaction();
            Transaction.Current = tx;

            txId1 = AssertAndGetCurrentTransactionId();
            Task<string> task = DoAsyncTSL3AsyncTSL2NestedTxWorkAsync(false, false, false, true, false, false, txId1);
            txId2 = AssertAndGetCurrentTransactionId();

            string result = task.Result;

            Transaction.Current = null;
            tx.Commit();

            Assert.Equal(txId1, txId2);

            AssertTransactionNull();
        }

        [Theory]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled)]
        public void DoTxQueueWorkItem(TransactionScopeAsyncFlowOption asyncFlowOption)
        {
            string txId1;
            string txId2;

            ManualResetEvent waitCompletion = new ManualResetEvent(false);
            AssertTransactionNull();

            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, asyncFlowOption))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                ThreadSyncObject context = new ThreadSyncObject()
                {
                    Id = txId1,
                    Event = waitCompletion,
                    RootAsyncFlowOption = asyncFlowOption
                };

                Task.Factory.StartNew(DoTxWork, context, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                waitCompletion.WaitOne();

                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            Assert.Equal(txId1, txId2);

            AssertTransactionNull();
        }

        [Theory]
        [InlineData(TransactionScopeAsyncFlowOption.Suppress)]
        [InlineData(TransactionScopeAsyncFlowOption.Enabled)]
        public void DoTxNewThread(TransactionScopeAsyncFlowOption asyncFlowOption)
        {
            string txId1;
            string txId2;

            ManualResetEvent waitCompletion = new ManualResetEvent(false);
            AssertTransactionNull();

            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, asyncFlowOption))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                ThreadSyncObject context = new ThreadSyncObject()
                {
                    Id = txId1,
                    RootAsyncFlowOption = asyncFlowOption
                };

                Task.Factory.StartNew(DoTxWork, context, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Wait();

                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            AssertTransactionNull();
            Assert.Equal(txId1, txId2);
        }

        private static async Task SimulateOpenConnTestAsync(int i)
        {
            string txId1;
            string txId2;

            TaskCompletionSource<object> completionSource = new TaskCompletionSource<object>();

            // Start a transaction - presumably the customer would do this in our code
            using (TransactionScope ts = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                // At some point we realize that we can't complete our work, so enqueue it to be completed later
                s_workQueue.Add(Tuple.Create(i, completionSource, Transaction.Current));

                // If we are the first thread kicked off, then we are also resposible to kick of the background thread which will do the work for us
                if (i == 0)
                {
                    StartWorkProcessingThread();
                }

                // Await for our work to be completed by the background thread, then finish the Tx
                await completionSource.Task;

                txId2 = AssertAndGetCurrentTransactionId();
                ts.Complete();
            }

            Assert.Equal(txId1, txId2);
        }

        private static void StartWorkProcessingThread()
        {
            Task.Factory.StartNew(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    //Get the next item of work
                    var work = s_workQueue.Take();

                    // Set the current transaction, such that anything we call will be aware of it
                    Transaction.Current = work.Item3;

                    // Read the current transaction back and check to see if it is what we set
                    Assert.Equal(Transaction.Current, work.Item3);

                    Debug.WriteLine("{0}: {1}", work.Item1, Transaction.Current == work.Item3);

                    // Tell the other thread that we completed its work
                    work.Item2.SetResult(null);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static void DoTxWork(object obj)
        {
            string txId1 = null;
            string txId2 = null;

            ThreadSyncObject context = (ThreadSyncObject)obj;

            if (context.RootAsyncFlowOption == TransactionScopeAsyncFlowOption.Enabled)
            {
                txId1 = AssertAndGetCurrentTransactionId();
                Assert.Equal(context.Id, txId1);
            }
            else
            {
                AssertTransactionNull();
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId2 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            if (context.RootAsyncFlowOption == TransactionScopeAsyncFlowOption.Enabled)
            {
                string txId3 = AssertAndGetCurrentTransactionId();
                Assert.Equal(txId1, txId2);
                Assert.Equal(txId1, txId3);
            }
            else
            {
                AssertTransactionNull();
            }

            if (context.Event != null)
            {
                context.Event.Set();
            }
        }

        private static string DoSyncTxWork(bool requiresNew, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            string result;
            using (TransactionScope scope = new TransactionScope(requiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                result = DoWork(txId1);
                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            VerifyTxId(requiresNew, txId, txId1, txId2);

            return result;
        }

        private static string DoWork(string txId)
        {
            string txId1 = Transaction.Current != null ? Transaction.Current.TransactionInformation.LocalIdentifier : null;
            Assert.Equal(txId, txId1);

            if (s_throwExceptionDefaultOrBeforeAwait)
            {
                throw new Exception("Sync DoWork exception!");
            }

            return "Hello" + " World";
        }

        private static async Task<string> DoAsyncTxWorkAsync(bool requiresNew, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            string result;
            using (TransactionScope scope = new TransactionScope(requiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                result = await DoAsyncWorkAsync(txId1);
                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            VerifyTxId(requiresNew, txId, txId1, txId2);

            return result;
        }

        private static async Task<string> DoAsyncTSL2NestedTxWorkAsync(bool parentRequiresNew, bool childRequiresNew, bool asyncChild, bool asyncWorkBeforeChild, bool asyncWorkAfterChild, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            string txId3 = null;
            string result;
            using (TransactionScope scope = new TransactionScope(parentRequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                if (asyncWorkBeforeChild)
                {
                    result = await DoAsyncWorkAsync(txId1);
                }

                if (asyncChild)
                {
                    result = await DoAsyncTxWorkAsync(childRequiresNew, txId1);
                }
                else
                {
                    result = DoSyncTxWork(childRequiresNew, txId1);
                }

                txId3 = AssertAndGetCurrentTransactionId();

                if (asyncWorkAfterChild)
                {
                    result = await DoAsyncWorkAsync(txId1);
                }

                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            VerifyTxId(parentRequiresNew, txId, txId1, txId2);
            VerifyTxId(parentRequiresNew, txId, txId1, txId3);

            return result;
        }

        private static string SyncTSL2NestedTxWork(bool parentRequiresNew, bool childRequiresNew, bool asyncChild, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            Task<string> task = null;
            string result = null;
            using (TransactionScope scope = new TransactionScope(parentRequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                if (asyncChild)
                {
                    task = DoAsyncTxWorkAsync(childRequiresNew, txId1);
                }
                else
                {
                    result = DoSyncTxWork(childRequiresNew, txId1);
                }

                if (asyncChild)
                {
                    result = task.Result;
                }

                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            VerifyTxId(parentRequiresNew, txId, txId1, txId2);

            return result;
        }

        private static void SyncTSL3AsyncTSL2NestedTxWork(bool parentRequiresNew, bool childL2RequiresNew, bool childL3RequiresNew, bool asyncChildL3, bool asyncWorkBeforeChildL3, bool asyncWorkAfterChildL3, string txId)
        {
            string txId1;
            string txId2;

            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(parentRequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                Task<string> task = DoAsyncTSL2NestedTxWorkAsync(childL2RequiresNew, childL3RequiresNew, asyncChildL3, asyncWorkBeforeChildL3, asyncWorkAfterChildL3, txId1);
                txId2 = AssertAndGetCurrentTransactionId();

                task.Wait();
                scope.Complete();
            }

            VerifyTxId(parentRequiresNew, txId, txId1, txId2);
            AssertTransaction(txId);
        }

        private static void SyncTSL3SyncTSL2NestedTxWork(bool parentRequiresNew, bool childL2RequiresNew, bool childL3RequiresNew, bool asyncChildL3, string txId)
        {
            string txId1;
            string txId2;

            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(parentRequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                SyncTSL2NestedTxWork(childL2RequiresNew, childL3RequiresNew, asyncChildL3, txId1);
                txId2 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            VerifyTxId(parentRequiresNew, txId, txId1, txId2);
            AssertTransaction(txId);
        }

        private static async Task<string> DoAsyncTSL3AsyncTSL2NestedTxWorkAsync(bool parentRequiresNew, bool childL2RequiresNew, bool childL3RequiresNew, bool asyncChildL3, bool asyncWorkBeforeChildL3, bool asyncWorkAfterChildL3, string txId)
        {
            string txId1;
            string txId2;
            string result = null;
            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(parentRequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                result = await DoAsyncTSL2NestedTxWorkAsync(childL2RequiresNew, childL3RequiresNew, asyncChildL3, asyncWorkBeforeChildL3, asyncWorkAfterChildL3, txId1);

                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            VerifyTxId(parentRequiresNew, txId, txId1, txId2);

            return result;
        }

        private static async Task<string> DoAsyncTSL3SyncTSL2NestedTxWorkAsync(bool parentRequiresNew, bool childL2RequiresNew, bool childL3RequiresNew, bool asyncChildL3, bool asyncWorkBeforeChildL2, bool asyncWorkAfterChildL2, string txId)
        {
            string txId1;
            string txId2;
            string txId3;
            string result = null;

            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(parentRequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();

                if (asyncWorkBeforeChildL2)
                {
                    await DoAsyncWorkAsync(txId1);
                }

                result = SyncTSL2NestedTxWork(childL2RequiresNew, childL3RequiresNew, asyncChildL3, txId1);

                txId3 = AssertAndGetCurrentTransactionId();

                if (asyncWorkAfterChildL2)
                {
                    await DoAsyncWorkAsync(txId1);
                }

                txId2 = AssertAndGetCurrentTransactionId();

                scope.Complete();
            }

            VerifyTxId(parentRequiresNew, txId, txId1, txId2);
            VerifyTxId(parentRequiresNew, txId, txId1, txId3);
            AssertTransaction(txId);

            return result;
        }

        private static async Task<string> DoAsyncWorkAsync(string txId)
        {
            string txId1 = Transaction.Current != null ? Transaction.Current.TransactionInformation.LocalIdentifier : null;
            Assert.Equal(txId, txId1);

            if (s_throwExceptionDefaultOrBeforeAwait)
            {
                throw new Exception("DoAsyncWorkAsync exception before await");
            }

            await Task.Delay(2);

            Task<string> getString = Task.Run(delegate
            {
                txId1 = Transaction.Current != null ? Transaction.Current.TransactionInformation.LocalIdentifier : null;
                Assert.Equal(txId, txId1);
                return DoWork(txId);
            });

            if (s_throwExceptionAfterAwait)
            {
                throw new Exception("DoAsyncWorkAsync exception after await");
            }

            string result = await getString;

            txId1 = Transaction.Current != null ? Transaction.Current.TransactionInformation.LocalIdentifier : null;
            Assert.Equal(txId, txId1);

            return result;
        }

        private static void SyncTSDoTaskUnderAsyncTS(bool parentRequiresNew, bool childRequiresNew, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(parentRequiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                DoTaskUnderAsyncTS(childRequiresNew, txId1);
                txId2 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            VerifyTxId(parentRequiresNew, txId, txId1, txId2);
            AssertTransaction(txId);
        }

        private static void DoTaskUnderAsyncTS(bool requiresNew, string txId)
        {
            string txId1 = null;
            string txId2 = null;
            AssertTransaction(txId);

            using (TransactionScope scope = new TransactionScope(requiresNew ? TransactionScopeOption.RequiresNew : TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                DoALotOfWork(txId1);
                txId2 = AssertAndGetCurrentTransactionId();
                scope.Complete();
            }

            VerifyTxId(requiresNew, txId, txId1, txId2);
            AssertTransaction(txId);
        }

        private static void DoALotOfWork(string txId)
        {
            Task[] a = new Task[32];
            for (int i = 0; i < 32; i++)
            {
                a[i] = new Task(() => DoWork(txId));
                a[i].Start();
            }

            Task.WaitAll(a);
        }

        public static void VerifyTxId(bool requiresNew, string parentTxId, string beforeTxId, string afterTxId)
        {
            Assert.Equal(beforeTxId, afterTxId);
            if (requiresNew)
            {
                Assert.NotEqual(parentTxId, beforeTxId);
            }
            else
            {
                if (!string.IsNullOrEmpty(parentTxId))
                {
                    Assert.Equal(parentTxId, beforeTxId);
                }
            }
        }

        public static void AssertTransaction(string txId)
        {
            if (string.IsNullOrEmpty(txId))
            {
                AssertTransactionNull();
            }
            else
            {
                Assert.Equal(txId, AssertAndGetCurrentTransactionId());
            }
        }
        public static void AssertTransactionNull()
        {
            Assert.Equal(Transaction.Current, null);
        }

        public static void AssertTransactionNotNull()
        {
            Assert.NotEqual(Transaction.Current, null);
        }

        public static string AssertAndGetCurrentTransactionId()
        {
            AssertTransactionNotNull();
            return Transaction.Current.TransactionInformation.LocalIdentifier;
        }

        public static string AssertAndGetCurrentTransactionId(TransactionScopeOption scopeOption)
        {
            if (scopeOption == TransactionScopeOption.Suppress)
            {
                AssertTransactionNull();
                return null;
            }
            else
            {
                AssertTransactionNotNull();
                return Transaction.Current.TransactionInformation.LocalIdentifier;
            }
        }

        public static void AssertTransactionNullAndWaitTask(Task<string> task)
        {
            AssertTransactionNull();
            task.Wait();
            AssertTransactionNull();
        }

        public static void AssertTransactionNullAndWaitTask(Task task)
        {
            AssertTransactionNull();
            task.Wait();
            AssertTransactionNull();
        }

        public static void SetExceptionInjection(bool exceptionDefaultOrBeforeAwait, bool exceptionAfterAwait)
        {
            s_throwExceptionDefaultOrBeforeAwait = exceptionDefaultOrBeforeAwait;
            s_throwExceptionAfterAwait = exceptionAfterAwait;
        }

        public static void ResetExceptionInjection()
        {
            s_throwExceptionDefaultOrBeforeAwait = false;
            s_throwExceptionAfterAwait = false;
        }

        public static void HandleException(bool exceptionDefaultOrBeforeAwait, bool exceptionAfterAwait, Action action)
        {
            bool hasException = false;

            SetExceptionInjection(exceptionDefaultOrBeforeAwait, exceptionAfterAwait);
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                hasException = true;
                Debug.WriteLine("Exception: {0}", ex.Message);
            }

            AssertTransactionNull();
            Assert.Equal<bool>(hasException, true);
            ResetExceptionInjection();
        }
    }

    public class ThreadSyncObject
    {
        public string Id
        {
            get;
            set;
        }

        public ManualResetEvent Event
        {
            get;
            set;
        }

        public TransactionScopeAsyncFlowOption RootAsyncFlowOption
        {
            get;
            set;
        }
    }

    public interface IStatus
    {
        string GetStatus(string id, bool fail);
    }
}
