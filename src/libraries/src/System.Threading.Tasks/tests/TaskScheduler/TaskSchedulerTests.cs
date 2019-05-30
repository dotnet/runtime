// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq;

namespace System.Threading.Tasks.Tests
{
    //
    // Task scheduler basics.
    //
    public static class TaskSchedulerTests
    {
        // Just ensure we eventually complete when many blocked tasks are created.
        [OuterLoop]
        [Fact]
        public static void RunBlockedInjectionTest()
        {
            Debug.WriteLine("* RunBlockedInjectionTest() -- if it deadlocks, it failed");

            ManualResetEvent mre = new ManualResetEvent(false);

            // we need to run this test in a local task scheduler, because it needs to perform 
            // the verification based on a known number of initially available threads.
            //
            //
            // @TODO: When we reach the _planB branch we need to add a trick here using ThreadPool.SetMaxThread
            //        to bring down the TP worker count. This is because previous activity in the test process might have 
            //        injected workers.
            TaskScheduler tm = TaskScheduler.Default;

            // Create many tasks blocked on the MRE.

            int processorCount = Environment.ProcessorCount;
            Task[] tasks = new Task[processorCount];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(delegate { mre.WaitOne(); }, CancellationToken.None, TaskCreationOptions.None, tm);
            }

            // Create one task that signals the MRE, and wait for it.
            Task.Factory.StartNew(delegate { mre.Set(); }, CancellationToken.None, TaskCreationOptions.None, tm).Wait();

            // Lastly, wait for the others to complete.
            Task.WaitAll(tasks);
        }

        [Fact]
        public static void RunBuggySchedulerTests()
        {
            Debug.WriteLine("* RunBuggySchedulerTests()");

            BuggyTaskScheduler bts = new BuggyTaskScheduler();
            Task t1 = new Task(delegate { });
            Task t2 = new Task(delegate { });

            //
            // Test Task.Start(buggy scheduler)
            //
            Debug.WriteLine("  -- testing Task.Start(buggy scheduler)");
            try
            {
                t1.Start(bts);
                Assert.True(false, string.Format("    > FAILED.  No exception thrown."));
            }
            catch (TaskSchedulerException)
            {
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("    > FAILED. Wrong exception thrown (expected TaskSchedulerException): {0}", e));
            }

            if (t1.Status != TaskStatus.Faulted)
            {
                Assert.True(false, string.Format("    > FAILED. Task ended up in wrong status (expected Faulted): {0}", t1.Status));
            }


            Debug.WriteLine("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                t1.Wait();
                Assert.True(false, string.Format("    > FAILED.  No exception thrown from Wait()."));
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    Assert.True(false, string.Format("    > FAILED.  Wrong inner exception thrown from Wait(): {0}", ae.InnerExceptions[0].GetType().Name));
                }
            }

            //
            // Test Task.RunSynchronously(buggy scheduler)
            //
            Debug.WriteLine("  -- testing Task.RunSynchronously(buggy scheduler)");
            try
            {
                t2.RunSynchronously(bts);
                Assert.True(false, string.Format("    > FAILED.  No exception thrown."));
            }
            catch (TaskSchedulerException) { }
            catch (Exception e)
            {
                Assert.True(false, string.Format("    > FAILED. Wrong exception thrown (expected TaskSchedulerException): {0}", e));
            }

            if (t2.Status != TaskStatus.Faulted)
            {
                Assert.True(false, string.Format("    > FAILED. Task ended up in wrong status (expected Faulted): {0}", t1.Status));
            }

            Debug.WriteLine("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                t2.Wait();
                Assert.True(false, string.Format("    > FAILED.  No exception thrown from Wait()."));
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    Assert.True(false, string.Format("    > FAILED.  Wrong inner exception thrown from Wait(): {0}", ae.InnerExceptions[0].GetType().Name));
                }
            }

            //
            // Test StartNew(buggy scheduler)
            //
            Debug.WriteLine("  -- testing Task.Factory.StartNew(buggy scheduler)");
            try
            {
                Task t3 = Task.Factory.StartNew(delegate { }, CancellationToken.None, TaskCreationOptions.None, bts);
                Assert.True(false, string.Format("    > FAILED.  No exception thrown."));
            }
            catch (TaskSchedulerException) { }
            catch (Exception e)
            {
                Assert.True(false, string.Format("    > FAILED. Wrong exception thrown (expected TaskSchedulerException): {0}", e));
            }

            //
            // Test continuations
            //
            Debug.WriteLine("  -- testing Task.ContinueWith(buggy scheduler)");
            Task completedTask = Task.Factory.StartNew(delegate { });
            completedTask.Wait();

            Task tc1 = completedTask.ContinueWith(delegate { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, bts);

            Debug.WriteLine("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                tc1.Wait();
                Assert.True(false, string.Format("    > FAILED.  No exception thrown (sync)."));
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    Assert.True(false, string.Format("    > FAILED.  Wrong inner exception thrown from Wait() (sync): {0}", ae.InnerExceptions[0].GetType().Name));
                }
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("    > FAILED.  Wrong exception thrown (sync): {0}", e));
            }

            Task tc2 = completedTask.ContinueWith(delegate { }, CancellationToken.None, TaskContinuationOptions.None, bts);

            Debug.WriteLine("    -- Waiting on Faulted task (there's a problem if we deadlock)...");
            try
            {
                tc2.Wait();
                Assert.True(false, string.Format("    > FAILED.  No exception thrown (async)."));
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerExceptions[0] is TaskSchedulerException))
                {
                    Assert.True(false, string.Format("    > FAILED.  Wrong inner exception thrown from Wait() (async): {0}", ae.InnerExceptions[0].GetType().Name));
                }
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("    > FAILED.  Wrong exception thrown (async): {0}", e));
            }

            // Test Wait()/inlining
            Debug.WriteLine("  -- testing Task.Wait(task started on buggy scheduler)");
            BuggyTaskScheduler bts2 = new BuggyTaskScheduler(false); // won't throw on QueueTask
            Task t4 = new Task(delegate { });
            t4.Start(bts2);
            try
            {
                t4.Wait();
                Assert.True(false, string.Format("    > FAILED.  Expected inlining exception"));
            }
            catch (TaskSchedulerException) { }
            catch (Exception e)
            {
                Assert.True(false, string.Format("    > FAILED.  Wrong exception thrown: {0}", e));
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunSynchronizationContextTaskSchedulerTests()
        {
            // Remember the current SynchronizationContext, so that we can restore it
            SynchronizationContext previousSC = SynchronizationContext.Current;

            // Now make up a "real" SynchronizationContext and install it
            SimpleSynchronizationContext newSC = new SimpleSynchronizationContext();
            SetSynchronizationContext(newSC);

            // Create a scheduler based on the current SC
            TaskScheduler scTS = TaskScheduler.FromCurrentSynchronizationContext();

            //
            // Launch a Task on scTS, make sure that it is processed in the expected fashion
            //
            bool sideEffect = false;
            Task task = Task.Factory.StartNew(() => { sideEffect = true; }, CancellationToken.None, TaskCreationOptions.None, scTS);

            Exception ex = null;

            try
            {
                task.Wait();
            }
            catch (Exception e)
            {
                ex = e;
            }

            Assert.True(task.IsCompleted, "Expected task to have completed");
            Assert.True(ex == null, "Did not expect exception on Wait");
            Assert.True(sideEffect, "Task appears not to have run");
            Assert.True(newSC.PostCount == 1, "Expected exactly one post to underlying SynchronizationContext");

            // 
            // Run a Task synchronously on scTS, make sure that it completes
            //
            sideEffect = false;
            Task syncTask = new Task(() => { sideEffect = true; });

            ex = null;
            try
            {
                syncTask.RunSynchronously(scTS);
            }
            catch (Exception e)
            {
                ex = e;
            }

            Assert.True(task.IsCompleted, "Expected task to have completed");
            Assert.True(ex == null, "Did not expect exception on RunSynchronously");
            Assert.True(sideEffect, "Task appears not to have run");
            Assert.True(newSC.PostCount == 1, "Did not expect a new Post to underlying SynchronizationContext");

            //
            // Miscellaneous things to test
            //
            Assert.True(scTS.MaximumConcurrencyLevel == 1, "Expected scTS.MaximumConcurrencyLevel to be 1");

            // restore original SC
            SetSynchronizationContext(previousSC);
        }

        [Fact]
        public static void RunSynchronizationContextTaskSchedulerTests_Negative()
        {
            // Remember the current SynchronizationContext, so that we can restore it
            SynchronizationContext previousSC = SynchronizationContext.Current;

            //
            // Test exceptions on construction of SCTaskScheduler
            //
            SetSynchronizationContext(null);
            Assert.Throws<InvalidOperationException>(
               () => { TaskScheduler.FromCurrentSynchronizationContext(); });
        }

        [Fact]
        public static void GetTaskSchedulersForDebugger_ReturnsDefaultScheduler()
        {
            MethodInfo getTaskSchedulersForDebuggerMethod = typeof(TaskScheduler).GetTypeInfo().GetDeclaredMethod("GetTaskSchedulersForDebugger");
            TaskScheduler[] foundSchedulers = getTaskSchedulersForDebuggerMethod.Invoke(null, null) as TaskScheduler[];
            Assert.NotNull(foundSchedulers);
            Assert.Contains(TaskScheduler.Default, foundSchedulers);
        }

        [ConditionalFact(nameof(DebuggerIsAttached))]
        public static void GetTaskSchedulersForDebugger_DebuggerAttached_ReturnsAllSchedulers()
        {
            MethodInfo getTaskSchedulersForDebuggerMethod = typeof(TaskScheduler).GetTypeInfo().GetDeclaredMethod("GetTaskSchedulersForDebugger");

            var cesp = new ConcurrentExclusiveSchedulerPair();
            TaskScheduler[] foundSchedulers = getTaskSchedulersForDebuggerMethod.Invoke(null, null) as TaskScheduler[];
            Assert.NotNull(foundSchedulers);
            Assert.Contains(TaskScheduler.Default, foundSchedulers);
            Assert.Contains(cesp.ConcurrentScheduler, foundSchedulers);
            Assert.Contains(cesp.ExclusiveScheduler, foundSchedulers);

            GC.KeepAlive(cesp);
        }

        [ConditionalFact(nameof(DebuggerIsAttached))]
        public static void GetScheduledTasksForDebugger_DebuggerAttached_ReturnsTasksFromCustomSchedulers()
        {
            var nonExecutingScheduler = new BuggyTaskScheduler(faultQueues: false);

            Task[] queuedTasks =
                (from i in Enumerable.Range(0, 10)
                 select Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, nonExecutingScheduler)).ToArray();

            MethodInfo getScheduledTasksForDebuggerMethod = typeof(TaskScheduler).GetTypeInfo().GetDeclaredMethod("GetScheduledTasksForDebugger");
            Task[] foundTasks = getScheduledTasksForDebuggerMethod.Invoke(nonExecutingScheduler, null) as Task[];
            Assert.Superset(new HashSet<Task>(queuedTasks), new HashSet<Task>(foundTasks));

            GC.KeepAlive(nonExecutingScheduler);
        }

        private static bool DebuggerIsAttached { get { return Debugger.IsAttached; } }

        #region Helper Methods / Helper Classes

        // Buggy task scheduler to make sure that we handle QueueTask()/TryExecuteTaskInline()
        // exceptions correctly.  Used in RunBuggySchedulerTests() below.
        public class BuggyTaskScheduler : TaskScheduler
        {
            private readonly ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();

            private bool _faultQueues;
            protected override void QueueTask(Task task)
            {
                if (_faultQueues)
                    throw new InvalidOperationException("I don't queue tasks!");
                // else do nothing other than store the task -- still a pretty buggy scheduler!!
                _tasks.Enqueue(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                throw new ArgumentException("I am your worst nightmare!");
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return _tasks;
            }

            public BuggyTaskScheduler()
                : this(true)
            {
            }

            public BuggyTaskScheduler(bool faultQueues)
            {
                _faultQueues = faultQueues;
            }
        }

        private class SimpleSynchronizationContext : SynchronizationContext
        {
            private int _postCount = 0;

            public override void Post(SendOrPostCallback d, object state)
            {
                _postCount++;
                base.Post(d, state);
            }

            public int PostCount { get { return _postCount; } }
        }

        private static void SetSynchronizationContext(SynchronizationContext sc)
        {
            SynchronizationContext.SetSynchronizationContext(sc);
        }

        #endregion
    }
}
