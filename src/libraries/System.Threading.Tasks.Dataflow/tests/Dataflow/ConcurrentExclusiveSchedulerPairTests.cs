// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Reflection;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        // [Fact(Skip = "Outerloop")]
        public void RunConcurrentExclusiveSchedulerPairRWTests()
        {
            // Validate reader tasks get scheduled concurrently
            {
                bool localPassed = true;
                foreach (var cesp in new[] { new ConcurrentExclusiveSchedulerPair(), new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, -1) })
                {
                    try
                    {
                        int numToSchedule = Environment.ProcessorCount; // doesn't validate much on a single core, but that's ok
                        Barrier b = new Barrier(numToSchedule);
                        var tasks = new Task[numToSchedule];
                        for (int i = 0; i < numToSchedule; i++)
                        {
                            tasks[i] = Task.Factory.StartNew(() =>
                            {
                                localPassed &= b.SignalAndWait(1000);
                            }, CancellationToken.None, TaskCreationOptions.None, cesp.ConcurrentScheduler);
                        }
                        Task.WaitAll(tasks);
                    }
                    finally { cesp.Complete(); }
                }

                Assert.True(localPassed, string.Format("{0}: Test concurrent readers", localPassed ? "Success" : "Failure"));
            }

            // Validate reader tasks don't go above max concurrency level
            {
                bool localPassed = true;
                int mcl = 2;
                var cesp = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, mcl);
                try
                {
                    int numToSchedule = 2;
                    int concurrentTasks = 0;
                    var tasks = new Task[numToSchedule];
                    for (int i = 0; i < numToSchedule; i++)
                    {
                        tasks[i] = Task.Factory.StartNew(() =>
                        {
                            Interlocked.Increment(ref concurrentTasks);
                            Task.Delay(1).Wait();
                            if (concurrentTasks > mcl) localPassed = false;
                            Task.Delay(1).Wait();
                            Interlocked.Decrement(ref concurrentTasks);
                        }, CancellationToken.None, TaskCreationOptions.None, cesp.ConcurrentScheduler);
                    }
                    Task.WaitAll(tasks);
                }
                finally { cesp.Complete(); }
                Assert.True(localPassed, string.Format("{0}: Test concurrent readers stay below maximum", localPassed ? "Success" : "Failure"));
            }

            // Validate writers tasks don't run concurrently with each other
            {
                bool localPassed = true;
                var cesp = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default);
                try
                {
                    int numToSchedule = 2;
                    int concurrentTasks = 0;
                    var tasks = new Task[numToSchedule];
                    for (int i = 0; i < numToSchedule; i++)
                    {
                        tasks[i] = Task.Factory.StartNew(() =>
                        {
                            Interlocked.Increment(ref concurrentTasks);
                            Task.Delay(100).Wait();
                            if (concurrentTasks > 1) localPassed &= false;
                            Task.Delay(100).Wait();
                            Interlocked.Decrement(ref concurrentTasks);
                        }, CancellationToken.None, TaskCreationOptions.None, cesp.ExclusiveScheduler);
                    }
                    Task.WaitAll(tasks);
                }
                finally { cesp.Complete(); }
                Assert.True(localPassed, string.Format("{0}: Test writers don't run concurrently with each other", localPassed ? "Success" : "Failure"));
            }

            // Validate writers tasks don't run concurrently with readers or writers
            {
                bool localPassed = true;
                var cesp = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default);

                int numWritersToSchedule = 2;
                int numReadersToSchedule = 3;
                int runningTasks = 0;

                var readerTasks = new Task[numReadersToSchedule];
                for (int i = 0; i < numReadersToSchedule; i++)
                {
                    readerTasks[i] = Task.Factory.StartNew(() =>
                    {
                        Interlocked.Increment(ref runningTasks);
                        Task.Delay(1).Wait();
                        Interlocked.Decrement(ref runningTasks);
                    }, CancellationToken.None, TaskCreationOptions.None, cesp.ConcurrentScheduler);
                }

                var writerTasks = new Task[numWritersToSchedule];
                for (int i = 0; i < numWritersToSchedule; i++)
                {
                    writerTasks[i] = Task.Factory.StartNew(() =>
                    {
                        Interlocked.Increment(ref runningTasks);
                        Task.Delay(100).Wait();
                        if (runningTasks > 1) localPassed &= false;
                        Task.Delay(100).Wait();
                        Interlocked.Decrement(ref runningTasks);
                    }, CancellationToken.None, TaskCreationOptions.None, cesp.ExclusiveScheduler);
                }

                Task.WaitAll(writerTasks);
                Task.WaitAll(readerTasks);

                // not completing the cesp here, just so that some of our tests don't
                // and so that we don't hide any issues by always completing

                Assert.True(localPassed, string.Format("{0}: Test writers don't run concurrently with anything", localPassed ? "Success" : "Failure"));
            }
        }

        // [Fact(Skip = "outerloop")]
        public void RunConcurrentExclusiveSchedulerPairTests()
        {
            // Validate invalid arguments
            {
                Assert.Throws<ArgumentNullException>(() => new ConcurrentExclusiveSchedulerPair(null));
                Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, -2));
                Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, 1, -2));
                Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, 1, 0));
            }

            // Validate completion prevents more tasks
            {
                bool localPassed = true;
                ConcurrentExclusiveSchedulerPair cesp = new ConcurrentExclusiveSchedulerPair();
                cesp.Complete();
                Assert.Throws<TaskSchedulerException>(() => Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, cesp.ConcurrentScheduler).Wait());
                Assert.Throws<TaskSchedulerException>(() => Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, cesp.ExclusiveScheduler).Wait());
                Assert.True(localPassed, string.Format("{0}: Completion prevents more tasks", localPassed ? "Success" : "Failure"));
            }

            // Validate completion allows existing scheduled tasks to complete
            {
                bool localPassed = true;
                ConcurrentExclusiveSchedulerPair cesp = new ConcurrentExclusiveSchedulerPair();
                int tasksToSchedule = 2;
                int count = 0;
                var tasks = new Task[tasksToSchedule];
                using (var mres = new ManualResetEventSlim())
                {
                    for (int i = 0; i < tasksToSchedule; i++)
                    {
                        tasks[i] = Task.Factory.StartNew(() =>
                        {
                            mres.Wait();
                            Interlocked.Increment(ref count);
                        }, CancellationToken.None, TaskCreationOptions.None, cesp.ExclusiveScheduler);
                    }
                    cesp.Complete();
                    Assert.True(count == 0, "No tasks should have completed yet");
                    mres.Set();
                    Task.WaitAll(tasks);
                    Assert.True(count == tasksToSchedule, "All of the tasks should have executed");
                    cesp.Completion.Wait();
                }
                Assert.True(localPassed, string.Format("{0}: Completion allows existing tasks to complete", localPassed ? "Success" : "Failure"));
            }

            //  Validate MCL handling
            {
                bool localPassed = true;
                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(new ControllableMclTaskScheduler(4), 8);
                    localPassed &= cesp.ConcurrentScheduler.MaximumConcurrencyLevel == 4;
                    localPassed &= cesp.ExclusiveScheduler.MaximumConcurrencyLevel == 1;
                }

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(new ControllableMclTaskScheduler(8), 4);
                    localPassed &= cesp.ConcurrentScheduler.MaximumConcurrencyLevel == 4;
                    localPassed &= cesp.ExclusiveScheduler.MaximumConcurrencyLevel == 1;
                }

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, -1);
                    localPassed &= cesp.ConcurrentScheduler.MaximumConcurrencyLevel == Int32.MaxValue;
                    localPassed &= cesp.ExclusiveScheduler.MaximumConcurrencyLevel == 1;
                }

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(new ControllableMclTaskScheduler(-2), -1);
                    localPassed &= cesp.ConcurrentScheduler.MaximumConcurrencyLevel == Int32.MaxValue;
                    localPassed &= cesp.ExclusiveScheduler.MaximumConcurrencyLevel == 1;
                }

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(new ControllableMclTaskScheduler(-2), 1);
                    localPassed &= cesp.ConcurrentScheduler.MaximumConcurrencyLevel == 1;
                    localPassed &= cesp.ExclusiveScheduler.MaximumConcurrencyLevel == 1;
                }

                Assert.True(localPassed, string.Format("{0}: Max Concurrency Level corner cases handling correctly", localPassed ? "Success" : "Failure"));
            }

            // Validate queueing when layering on a faulty scheduler
            {
                bool localPassed = true;
                var ts = new ControllableTaskScheduler();

                // NOTE: We're using new pair instances on each iteration to avoid code paths
                // where a replica task gets queued up in the scheduler.  If that happens while the underlying
                // scheduler is FailQueueing==true, the task used internally by CESP will fault
                // and will go unobserved.  This is by design and we don't want it bringing down the tests.
                var cesp1 = new ConcurrentExclusiveSchedulerPair(ts);
                var cesp2 = new ConcurrentExclusiveSchedulerPair(ts);
                foreach (var cesp in new[] { cesp1, cesp2 })
                {
                    var scheduler = cesp == cesp1 ? cesp1.ConcurrentScheduler : cesp2.ExclusiveScheduler;

                    // Queue a task that will cause the CESP to fail queueing to its underlying scheduler
                    ts.FailQueueing = true;
                    Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, scheduler);

                    try
                    {
                        Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, scheduler);
                        localPassed = false;
                    }
                    catch (Exception exc)
                    {
                        Assert.True(
                            exc is TaskSchedulerException && ((TaskSchedulerException)exc).InnerException is InvalidOperationException,
                            "Expected a TaskSchedulerException containing an InvalidOperationException");
                    }

                    Assert.True(SpinWait.SpinUntil(() => cesp.Completion.IsCompleted, 1), "Excepted CESP to complete in allotted time");
                    Assert.True(cesp.Completion.Exception != null, "Excepted CESP task to have exceptions");
                }

                Assert.True(localPassed, string.Format("{0}: Test queueing layering on faulty scheduler", localPassed ? "Success" : "Failure"));
            }

            // Validate inlining when layering on a faulty scheduler
            {
                bool localPassed = true;
                var ts = new ControllableTaskScheduler();

                // NOTE: We're using new pair instances on each iteration to avoid code paths
                // where a replica task gets queued up in the scheduler.  If that happens while the underlying
                // scheduler is FailQueueing==true, the task used internally by CESP will fault
                // and will go unobserved.  This is by design and we don't want it bringing down the tests.
                var cesp1 = new ConcurrentExclusiveSchedulerPair(ts);
                var cesp2 = new ConcurrentExclusiveSchedulerPair(ts);
                foreach (var cesp in new[] { cesp1, cesp2 })
                {
                    var scheduler = cesp == cesp1 ? cesp1.ConcurrentScheduler : cesp2.ExclusiveScheduler;

                    // Inline a task that will cause the CESP to fail queueing to its underlying scheduler
                    ts.FailQueueing = false;
                    Task.Factory.StartNew(() =>
                    {
                        ts.FailQueueing = true;
                        Task t = new Task(() => { });
                        try
                        {
                            t.RunSynchronously(scheduler);
                            localPassed = false;
                        }
                        catch (Exception exc)
                        {
                            Assert.True(
                                exc is TaskSchedulerException && ((TaskSchedulerException)exc).InnerException is TaskSchedulerException,
                                "Excepted a TaskSchedulerException to contain another TaskSchedulerException");
                        }
                        Assert.True(t.IsCompleted, "Expected the queued task to be completed");
                        Assert.True(t.Exception != null, "Expected the queued task to be faulted");
                    }, CancellationToken.None, TaskCreationOptions.None, scheduler).Wait();

                    ts.FailQueueing = false;
                    Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, scheduler).Wait();
                    cesp.Complete();

                    Assert.True(SpinWait.SpinUntil(() => cesp.Completion.IsCompleted, 1), "Expected the CESP to complete in the allotted time");
                    Assert.True(cesp.Completion.Exception == null, "Expected the task to not be faulted and have no exceptions");
                }

                Assert.True(localPassed, string.Format("{0}: Test inlining layering on faulty scheduler", localPassed ? "Success" : "Failure"));
            }

            // Validate tasks on the same scheduler waiting on each other
            {
                bool localPassed = true;
                foreach (var underlyingScheduler in new[] { TaskScheduler.Default, new ControllableTaskScheduler() { FailQueueing = false } })
                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(underlyingScheduler);
                    TaskRecursion(2, new ParallelOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    cesp.Complete();

                    cesp = new ConcurrentExclusiveSchedulerPair(underlyingScheduler);
                    TaskRecursion(2, new ParallelOptions { TaskScheduler = cesp.ConcurrentScheduler });
                    cesp.Complete();
                }
                Assert.True(localPassed, string.Format("{0}: Recursively waiting on same scheduler", localPassed ? "Success" : "Failure"));
            }

            // Exercise additional inlining code paths
            {
                bool localPassed = true;
                foreach (var underlyingScheduler in new[] { TaskScheduler.Default, new ControllableTaskScheduler() { FailQueueing = false } })
                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(underlyingScheduler);
                    Task.Factory.StartNew(() => { }).ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, cesp.ExclusiveScheduler).Wait();
                    Task.Factory.StartNew(() => { }).ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, cesp.ConcurrentScheduler).Wait();

                    var t = new Task(() => { });
                    t.RunSynchronously(cesp.ConcurrentScheduler);
                    t.Wait();

                    t = new Task(() => { });
                    t.RunSynchronously(cesp.ExclusiveScheduler);
                    t.Wait();

                    Task.Factory.StartNew(() =>
                    {
                        new Task(() => { }, TaskCreationOptions.AttachedToParent).RunSynchronously(cesp.ConcurrentScheduler);
                    }, CancellationToken.None, TaskCreationOptions.None, cesp.ConcurrentScheduler).Wait();

                    Task.Factory.StartNew(() =>
                    {
                        new Task(() => { }, TaskCreationOptions.AttachedToParent).RunSynchronously(cesp.ExclusiveScheduler);
                    }, CancellationToken.None, TaskCreationOptions.None, cesp.ExclusiveScheduler).Wait();
                }
                Assert.True(localPassed, string.Format("{0}: Additional inlining code paths", localPassed ? "Success" : "Failure"));
            }
        }

        private static void TaskRecursion(int depth, ParallelOptions options)
        {
            if (depth <= 0) return;
            Parallel.Invoke(
                options,
                () => TaskRecursion(depth - 1, options),
                () => TaskRecursion(depth - 1, options));
        }

        private class ControllableMclTaskScheduler : TaskScheduler
        {
            private int _m_mcl;

            public ControllableMclTaskScheduler(int mcl) { _m_mcl = mcl; }

            public override int MaximumConcurrencyLevel { get { return _m_mcl; } }

            protected override IEnumerable<Task> GetScheduledTasks() { throw new NotImplementedException(); }
            protected override void QueueTask(Task task) { throw new NotImplementedException(); }
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) { throw new NotImplementedException(); }
        }

        internal class ControllableTaskScheduler : TaskScheduler
        {
            public ControllableTaskScheduler()
            {
                FailQueueing = false;
            }

            protected override IEnumerable<Task> GetScheduledTasks() { return null; }

            protected override void QueueTask(Task task)
            {
                if (FailQueueing) throw new InvalidOperationException("m_failQueueing == true");
                else ThreadPool.QueueUserWorkItem(delegate { TryExecuteTask(task); });
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                if (FailQueueing) throw new InvalidOperationException("m_failQueueing == true");
                else return TryExecuteTask(task);
            }

            public bool FailQueueing { get; set; }
        }
    }
}