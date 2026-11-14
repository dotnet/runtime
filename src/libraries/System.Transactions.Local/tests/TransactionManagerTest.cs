// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Transactions.Tests
{
    public class TransactionManagerTest
    {
        [Fact]
        public void DefaultTimeout_MaxTimeout_Set_Get()
        {
            TimeSpan tsDefault = TimeSpan.Parse("00:02:00");
            TransactionManager.DefaultTimeout = tsDefault;
            Assert.Equal(tsDefault, TransactionManager.DefaultTimeout);

            TimeSpan tsMax = TimeSpan.Parse("00:30:00");            
            TransactionManager.MaximumTimeout = tsMax;
            Assert.Equal(tsMax, TransactionManager.MaximumTimeout);            

            TimeSpan ts = TransactionManager.MaximumTimeout.Add(TimeSpan.FromMinutes(10));
            TransactionManager.DefaultTimeout = ts;
            Assert.Equal(tsMax, TransactionManager.MaximumTimeout);
            Assert.Equal(TransactionManager.DefaultTimeout, TransactionManager.MaximumTimeout);

            ts = TimeSpan.Parse("-00:01:00");
            Assert.Throws<ArgumentOutOfRangeException>(() => TransactionManager.DefaultTimeout = ts);
            Assert.Throws<ArgumentOutOfRangeException>(() => TransactionManager.MaximumTimeout = ts);
        }

        [OuterLoop] // Takes about 20 seconds
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Timeout_WhileManyThreadsStartTransactions_TransactionTimesOut()
        {
            RemoteExecutor.Invoke(() =>
            {
                int threads = Math.Max(Environment.ProcessorCount * 2, 8);
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    using var tx = new CommittableTransaction(TimeSpan.FromSeconds(1));
                    var clock = Stopwatch.StartNew();

                    // Every round, all threads start a transaction at the same moment with a new timeout value.
                    using var nextRound = new Barrier(threads);
                    var workers = new Thread[threads];
                    for (int i = 0; i < threads; i++)
                    {
                        workers[i] = new Thread(() =>
                        {
                            for (int round = 0; clock.ElapsedMilliseconds < 500; round++)
                            {
                                nextRound.SignalAndWait();
                                using var other = new CommittableTransaction(TimeSpan.FromMilliseconds(10_000 + round * 512));
                            }
                            nextRound.RemoveParticipant();
                        });
                        workers[i].Start();
                    }
                    foreach (Thread worker in workers)
                    {
                        worker.Join();
                    }
                    GC.Collect();

                    while (tx.TransactionInformation.Status == TransactionStatus.Active && clock.Elapsed < TimeSpan.FromSeconds(30))
                    {
                        Thread.Sleep(50);
                    }

                    Assert.Equal(TransactionStatus.Aborted, tx.TransactionInformation.Status);
                }
            }).Dispose();
        }
    }
}
