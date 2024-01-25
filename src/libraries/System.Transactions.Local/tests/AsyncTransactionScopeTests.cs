// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Transactions.Tests
{
    public class AsyncTransactionScopeTests
    {
        // Number of threads to create
        private const int iterations = 5;

        // The work queue that requests will be placed in to be serviced by the background thread
        private static BlockingCollection<Tuple<int, TaskCompletionSource, Transaction>> s_workQueue = new BlockingCollection<Tuple<int, TaskCompletionSource, Transaction>>();

        private static bool s_throwExceptionDefaultOrBeforeAwait;
        private static bool s_throwExceptionAfterAwait;

        public AsyncTransactionScopeTests()
        {
            // Make sure we start with Transaction.Current = null.
            Transaction.Current = null;
        }

        protected void Dispose(bool disposing)
        {
            Transaction.Current = null;
        }

        /// <summary>
        /// This test case will verify various Async TransactionScope usage with task and async/await and also nested mixed mode(legacy TS and async TS) usage.
        /// </summary>
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(18)]
        [InlineData(19)]
        [InlineData(20)]
        [InlineData(21)]
        [InlineData(22)]
        [InlineData(23)]
        [InlineData(24)]
        [InlineData(25)]
        [InlineData(26)]
        [InlineData(27)]
        [InlineData(28)]
        [InlineData(29)]
        [InlineData(30)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(34)]
        [InlineData(35)]
        [InlineData(36)]
        [InlineData(37)]
        [InlineData(38)]
        [InlineData(39)]
        [InlineData(40)]
        [InlineData(41)]
        [InlineData(42)]
        [InlineData(43)]
        [InlineData(44)]
        [InlineData(45)]
        [InlineData(46)]
        [InlineData(47)]
        [InlineData(48)]
        [InlineData(49)]
        [InlineData(50)]
        [InlineData(51)]
        [InlineData(52)]
        [InlineData(53)]
        [InlineData(54)]
        public void AsyncTSTest(int variation)
        {
            RemoteExecutor.Invoke(variationString =>
            {
                using (var listener = new TestEventListener(new Guid("8ac2d80a-1f1a-431b-ace4-bff8824aef0b"), System.Diagnostics.Tracing.EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    try
                    {
                        listener.RunWithCallback(events.Enqueue, () =>
                        {
                            switch (int.Parse(variationString))
                            {
                                // Running exception test first to make sure any unintentional leak in ambient transaction during exception will be detected when subsequent test are run.
                                case 0:
                                    {
                                        HandleException(true, false, () => DoSyncTxWork(true, null));
                                        break;
                                    }
                                case 1:
                                    {
                                        HandleException(true, false, () => AssertTransactionNullAndWaitTask(DoAsyncTxWorkAsync(true, null)));
                                        break;
                                    }
                                case 2:
                                    {
                                        HandleException(true, false, () => SyncTSL2NestedTxWork(false, false, true, null));
                                        break;
                                    }
                                case 3:
                                    {
                                        HandleException(false, true, () => AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, true, false, null)));
                                        break;
                                    }
                                case 4:
                                    {
                                        HandleException(true, false, () => AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, true, false, false, null)));
                                        break;
                                    }
                                case 5:
                                    {
                                        HandleException(true, false, () => AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, false, null)));
                                        break;
                                    }
                                case 6:
                                    {
                                        HandleException(false, true, () => AssertTransactionNullAndWaitTask(DoTaskWorkAsync(false, false, null)));
                                        break;
                                    }
                                case 7:
                                    {
                                        HandleException(false, true, () => SyncTSTaskWork(false, null));
                                        break;
                                    }

                                // The following test has Task under TransactionScope and has few variations.
                                case 8:
                                    {
                                        DoTaskUnderAsyncTS(false, null);
                                        break;
                                    }
                                case 9:
                                    {
                                        Task.Factory.StartNew(() => DoTaskUnderAsyncTS(false, null)).Wait();
                                        break;
                                    }
                                case 10:
                                    {
                                        SyncTSDoTaskUnderAsyncTS(false, false, null);
                                        break;
                                    }
                                case 11:
                                    {
                                        SyncTSDoTaskUnderAsyncTS(false, true, null);
                                        break;
                                    }
                                case 12:
                                    {
                                        Task.Factory.StartNew(() => SyncTSDoTaskUnderAsyncTS(false, true, null)).Wait();
                                        break;
                                    }

                                // Simple Sync TS test
                                case 13:
                                    {
                                        DoSyncTxWork(false, null);
                                        break;
                                    }
                                case 14:
                                    {
                                        DoSyncTxWork(true, null);
                                        break;
                                    }

                                // Simple Async TS test. "await" points explicitly switches threads across continuations.
                                case 15:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTxWorkAsync(false, null));
                                        break;
                                    }
                                case 16:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTxWorkAsync(true, null));
                                        break;
                                    }

                                // Nested TS test. Parent is Sync TS, child TS can be sync or async.
                                case 17:
                                    {
                                        SyncTSL2NestedTxWork(false, false, false, null);
                                        break;
                                    }
                                case 18:
                                    {
                                        SyncTSL2NestedTxWork(false, false, true, null);
                                        break;
                                    }
                                case 19:
                                    {
                                        SyncTSL2NestedTxWork(false, true, false, null);
                                        break;
                                    }
                                case 20:
                                    {
                                        SyncTSL2NestedTxWork(false, true, true, null);
                                        break;
                                    }
                                case 21:
                                    {
                                        SyncTSL2NestedTxWork(true, false, false, null);
                                        break;
                                    }
                                case 22:
                                    {
                                        SyncTSL2NestedTxWork(true, false, true, null);
                                        break;
                                    }
                                case 23:
                                    {
                                        SyncTSL2NestedTxWork(true, true, false, null);
                                        break;
                                    }
                                case 24:
                                    {
                                        SyncTSL2NestedTxWork(true, true, true, null);
                                        break;
                                    }

                                // 2 level deep nested TS test. Parent is Aync TS, child TS can be sync or async.
                                case 25:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, false, false, false, false, null));
                                        break;
                                    }
                                case 26:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, false, true, false, false, null));
                                        break;
                                    }
                                case 27:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, false, false, null));
                                        break;
                                    }
                                case 28:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, true, false, false, null));
                                        break;
                                    }
                                case 29:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, false, false, false, false, null));
                                        break;
                                    }
                                case 30:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, false, true, false, false, null));
                                        break;
                                    }
                                case 31:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, true, false, false, false, null));
                                        break;
                                    }
                                case 32:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(true, true, true, false, false, null));
                                        break;
                                    }

                                // Introduce various "await" points to switch threads before/after child TransactionScope.
                                // Introduce some Task variations by running the test under Task.
                                case 33:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, true, false, null));
                                        break;
                                    }
                                case 34:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, false, true, null));
                                        break;
                                    }
                                case 35:
                                    {
                                        Task.Factory.StartNew(() => AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, false, true, null))).Wait();
                                        break;
                                    }
                                case 36:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL2NestedTxWorkAsync(false, true, false, true, true, null));
                                        break;
                                    }

                                // 3 level deep nested TS test.
                                case 37:
                                    {
                                        SyncTSL3AsyncTSL2NestedTxWork(false, false, true, false, false, true, null);
                                        break;
                                    }
                                case 38:
                                    {
                                        Task.Factory.StartNew(() => SyncTSL3AsyncTSL2NestedTxWork(false, false, true, false, false, true, null)).Wait();
                                        break;
                                    }
                                case 39:
                                    {
                                        SyncTSL3AsyncTSL2NestedTxWork(false, false, true, false, true, false, null);
                                        break;
                                    }

                                case 40:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, false, true, false, false, null));
                                        break;
                                    }
                                case 41:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, false, true, null));
                                        break;
                                    }
                                case 42:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, true, false, null));
                                        break;
                                    }
                                case 43:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, true, true, null));
                                        break;
                                    }
                                case 44:
                                    {
                                        Task.Factory.StartNew(() => AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, false, true, false, true, true, null))).Wait();
                                        break;
                                    }

                                case 45:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, false, true, null));
                                        break;
                                    }
                                case 46:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, false, null));
                                        break;
                                    }
                                case 47:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, true, null));
                                        break;
                                    }
                                case 48:
                                    {
                                        Task.Factory.StartNew(() => AssertTransactionNullAndWaitTask(DoAsyncTSL3SyncTSL2NestedTxWorkAsync(false, true, true, false, true, true, null))).Wait();
                                        break;
                                    }

                                // Have bunch of parallel tasks running various nested TS test cases. There parallel tasks are wrapped by a TransactionScope.
                                case 49:
                                    {
                                        AssertTransactionNullAndWaitTask(DoTaskWorkAsync(false, false, null));
                                        break;
                                    }
                                case 50:
                                    {
                                        SyncTSTaskWork(false, null);
                                        break;
                                    }
                                case 51:
                                    {
                                        SyncTSTaskWork(true, null);
                                        break;
                                    }
                                case 52:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSTaskWorkAsync(false, false, null));
                                        break;
                                    }
                                case 53:
                                    {
                                        AssertTransactionNullAndWaitTask(DoAsyncTSTaskWorkAsync(true, false, null));
                                        break;
                                    }

                                // Final test - wrap the DoAsyncTSTaskWorkAsync in synchronous scope
                                case 54:
                                    {
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
                                        break;
                                    }
                            }
                        });
                    }
                    catch (Exception exc)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Test failed with events:");
                        foreach (EventWrittenEventArgs actualevent in events)
                        {
                            sb.AppendLine($"{actualevent.Opcode} : {string.Format(actualevent.Message, actualevent.Payload.ToArray())}");
                        }
                        throw new Exception(sb.ToString(), exc);
                    }
                }
            }, variation.ToString()).Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        public void AsyncTSAndDependantClone(bool requiresNew, bool synchronizeScope, string txId)
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
                        // Since we use BlockCommitUntilComplete dependent transaction to synchronize the root TransactionScope, the ambient Tx may not be available and will be disposed and block on Commit.
                        // The flag will ensure we explicitly synchronize before disposing the root TransactionScope and the ambient transaction will still be available in the Task.
                        if (synchronizeScope)
                        {
                            txId2 = AssertAndGetCurrentTransactionId();
                        }

                        using (TransactionScope scope2 = new TransactionScope(dependentTx))
                        {
                            txId3 = AssertAndGetCurrentTransactionId();
                            Task.Delay(10).Wait();
                            scope2.Complete();
                        }

                        if (synchronizeScope)
                        {
                            txId4 = AssertAndGetCurrentTransactionId();
                        }
                    }
                    finally
                    {
                        dependentTx.Complete();
                        dependentTx.Dispose();
                    }

                    if (synchronizeScope)
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

                if (synchronizeScope)
                {
                    task1.Wait();
                }
                scope1.Complete();
            }

            task2.Wait();

            VerifyTxId(requiresNew, txId, txId1, txId6);

            Assert.Equal(txId1, txId3);
            Assert.Equal(txId3, txId6);
            if (synchronizeScope)
            {
                Assert.Equal(txId1, txId2);
                Assert.Equal(txId1, txId4);
                Assert.Equal(txId4, txId5);
            }
            Assert.NotEqual(txId1, txId7);

            AssertTransaction(txId);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
                    Assert.Null(txId2);
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
                    Assert.Null(txId4);
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
                    Assert.Null(txId1);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/97513", typeof(PlatformDetection), nameof(PlatformDetection.IsWasmThreadingSupported))]
        public async Task VerifyBYOTSyncTSNestedAsync()
        {
            string txId1;
            string txId2;

            await Task.Run(delegate
            {
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
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task VerifyBYOTAsyncTSNestedAsync()
        {
            string txId1;
            string txId2;

            AssertTransactionNull();
            CommittableTransaction tx = new CommittableTransaction();
            Transaction.Current = tx;

            txId1 = AssertAndGetCurrentTransactionId();
            Task<string> task = DoAsyncTSL3AsyncTSL2NestedTxWorkAsync(false, false, false, true, false, false, txId1);
            txId2 = AssertAndGetCurrentTransactionId();

            await task;

            Transaction.Current = null;
            tx.Commit();

            Assert.Equal(txId1, txId2);

            AssertTransactionNull();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

            TaskCompletionSource completionSource = new TaskCompletionSource();

            // Start a transaction - presumably the customer would do this in our code
            using (TransactionScope ts = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                txId1 = AssertAndGetCurrentTransactionId();
                // At some point we realize that we can't complete our work, so enqueue it to be completed later
                s_workQueue.Add(Tuple.Create(i, completionSource, Transaction.Current));

                // If we are the first thread kicked off, then we are also responsible to kick of the background thread which will do the work for us
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
                    work.Item2.SetResult();
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

        private static void VerifyTxId(bool requiresNew, string parentTxId, string beforeTxId, string afterTxId)
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

        private static void AssertTransaction(string txId)
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
        private static void AssertTransactionNull()
        {
            Assert.Null(Transaction.Current);
        }

        private static void AssertTransactionNotNull()
        {
            Assert.NotNull(Transaction.Current);
        }

        private static string AssertAndGetCurrentTransactionId()
        {
            AssertTransactionNotNull();
            return Transaction.Current.TransactionInformation.LocalIdentifier;
        }

        private static string AssertAndGetCurrentTransactionId(TransactionScopeOption scopeOption)
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

        private static void AssertTransactionNullAndWaitTask(Task<string> task)
        {
            AssertTransactionNull();
            task.Wait();
            AssertTransactionNull();
        }

        private static void AssertTransactionNullAndWaitTask(Task task)
        {
            AssertTransactionNull();
            task.Wait();
            AssertTransactionNull();
        }

        private static void SetExceptionInjection(bool exceptionDefaultOrBeforeAwait, bool exceptionAfterAwait)
        {
            s_throwExceptionDefaultOrBeforeAwait = exceptionDefaultOrBeforeAwait;
            s_throwExceptionAfterAwait = exceptionAfterAwait;
        }

        private static void ResetExceptionInjection()
        {
            s_throwExceptionDefaultOrBeforeAwait = false;
            s_throwExceptionAfterAwait = false;
        }

        private static void HandleException(bool exceptionDefaultOrBeforeAwait, bool exceptionAfterAwait, Action action)
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
            Assert.True(hasException);
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
