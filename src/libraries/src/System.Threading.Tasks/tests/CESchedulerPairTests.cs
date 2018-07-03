// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// CESchedulerPairTests.cs
// Tests Ported from the TPL test bed
//
// Summary:
// Implements the tests for the new scheduler ConcurrentExclusiveSchedulerPair 
//
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security;

using Xunit;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class TrackingTaskScheduler : TaskScheduler
    {
        public TrackingTaskScheduler(int maxConLevel)
        {
            //We need to set the value to 1 so that each time a scheduler is created, its tasks will start with one. 
            _counter = 1;
            if (maxConLevel < 1 && maxConLevel != -1/*infinite*/)
                throw new ArgumentException("Maximum concurrency level should between 1 and int32.Maxvalue");

            _maxConcurrencyLevel = maxConLevel;
        }


        protected override void QueueTask(Task task)
        {
            if (task == null) throw new ArgumentNullException("When requesting to QueueTask, the input task can not be null");
            Task.Factory.StartNew(() =>
            {
                lock (_lockObj) //Locking so that if multiple threads in threadpool does not incorrectly increment the counter.
                {
                    //store the current value of the counter (This becomes the unique ID for this scheduler's Task)
                    SchedulerID.Value = _counter;
                    _counter++;
                }
                ExecuteTask(task); //Extracted out due to security attribute reason.
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        private void ExecuteTask(Task task)
        {
            base.TryExecuteTask(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued) return false;
            return TryExecuteTask(task);
        }


        //public int SchedulerID
        //{
        //	get;
        //	set;
        //}

        protected override IEnumerable<Task> GetScheduledTasks() { return null; }
        private object _lockObj = new object();
        private int _counter = 1; //This is used to keep track of how many scheduler tasks were created
        public ThreadLocal<int> SchedulerID = new ThreadLocal<int>(); //This is the ID of the scheduler. 

        /// <summary>The maximum concurrency level for the scheduler.</summary>
        private readonly int _maxConcurrencyLevel;
        public override int MaximumConcurrencyLevel { get { return _maxConcurrencyLevel; } }
    }

    public class CESchedulerPairTests
    {
        #region Test cases

        /// <summary>
        /// Test to ensure that ConcurrentExclusiveSchedulerPair can be created using user defined parameters
        /// and those parameters are respected when tasks are executed
        /// </summary>
        /// <remarks>maxItemsPerTask and which scheduler is used are verified in other testcases</remarks>
        [Theory]
        [InlineData("default")]
        [InlineData("scheduler")]
        [InlineData("maxconcurrent")]
        [InlineData("all")]
        public static void TestCreationOptions(string ctorType)
        {
            ConcurrentExclusiveSchedulerPair schedPair = null;
            //Need to define the default values since these values are passed to the verification methods
            TaskScheduler scheduler = TaskScheduler.Default;
            int maxConcurrentLevel = Environment.ProcessorCount;

            //Based on input args, use one of the ctor overloads
            switch (ctorType.ToLower())
            {
                case "default":
                    schedPair = new ConcurrentExclusiveSchedulerPair();
                    break;
                case "scheduler":
                    schedPair = new ConcurrentExclusiveSchedulerPair(scheduler);
                    break;
                case "maxconcurrent":
                    maxConcurrentLevel = 2;
                    schedPair = new ConcurrentExclusiveSchedulerPair(scheduler, maxConcurrentLevel);
                    break;
                case "all":
                    maxConcurrentLevel = int.MaxValue;
                    schedPair = new ConcurrentExclusiveSchedulerPair(scheduler, -1/*MaxConcurrentLevel*/, -1/*MaxItemsPerTask*/); //-1 gets converted to Int32.MaxValue
                    break;
                default:
                    throw new NotImplementedException(string.Format("The option specified {0} to create the ConcurrentExclusiveSchedulerPair is invalid", ctorType));
            }

            //Create the factories that use the exclusive scheduler and the concurrent scheduler. We test to ensure
            //that the ConcurrentExclusiveSchedulerPair created are valid by scheduling work on them.
            TaskFactory writers = new TaskFactory(schedPair.ExclusiveScheduler);
            TaskFactory readers = new TaskFactory(schedPair.ConcurrentScheduler);

            List<Task> taskList = new List<Task>(); //Store all tasks created, to enable wait until all of them are finished

            // Schedule some dummy work that should be run with as much parallelism as possible
            for (int i = 0; i < 50; i++)
            {
                //In the current design, when there are no more tasks to execute, the Task used by concurrentexclusive scheduler dies
                //by sleeping we simulate some non trivial work that takes time and causes the concurrentexclusive scheduler Task 
                //to stay around for addition work.
                taskList.Add(readers.StartNew(() => { var sw = new SpinWait(); while (!sw.NextSpinWillYield) sw.SpinOnce() ; }));
            }
            // Schedule work where each item must be run when no other items are running
            for (int i = 0; i < 10; i++) taskList.Add(writers.StartNew(() => { var sw = new SpinWait(); while (!sw.NextSpinWillYield) sw.SpinOnce(); }));

            //Wait on the tasks to finish to ensure that the ConcurrentExclusiveSchedulerPair created can schedule and execute tasks without issues
            foreach (var item in taskList)
            {
                item.Wait();
            }

            //verify that maxconcurrency was respected.
            if (ctorType == "maxconcurrent")
            {
                Assert.Equal(maxConcurrentLevel, schedPair.ConcurrentScheduler.MaximumConcurrencyLevel);
            }
            Assert.Equal(1, schedPair.ExclusiveScheduler.MaximumConcurrencyLevel);

            //verify that the schedulers have not completed
            Assert.False(schedPair.Completion.IsCompleted, "The schedulers should not have completed as a completion request was not issued.");

            //complete the scheduler and make sure it shuts down successfully
            schedPair.Complete();
            schedPair.Completion.Wait();

            //make sure no additional work may be scheduled
            foreach (var schedPairScheduler in new TaskScheduler[] { schedPair.ConcurrentScheduler, schedPair.ExclusiveScheduler })
            {
                Exception caughtException = null;
                try
                {
                    Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, schedPairScheduler);
                }
                catch (Exception exc)
                {
                    caughtException = exc;
                }
                Assert.True(
                    caughtException is TaskSchedulerException && caughtException.InnerException is InvalidOperationException,
                    "Queueing after completion should fail");
            }
        }

        /// <summary>
        /// Test to verify that only up to maxItemsPerTask are executed by a single ConcurrentExclusiveScheduler Task
        /// </summary>
        /// <remarks>In ConcurrentExclusiveSchedulerPair, each tasks scheduled are run under an internal Task. The basic idea for the test
        /// is that each time ConcurrentExclusiveScheduler is called QueueTasK a counter (which acts as scheduler's Task id) is incremented.
        /// When a task executes, it observes the parent Task Id and if it matches the one its local cache, it increments its local counter (which tracks
        /// the items executed by a ConcurrentExclusiveScheduler Task). At any given time the Task's local counter cant exceed maxItemsPerTask</remarks>
        [Theory]
        [InlineData(4, 1, true)]
        [InlineData(1, 4, true)]
        [InlineData(4, 1, false)]
        [InlineData(1, 4, false)]
        public static void TestMaxItemsPerTask(int maxConcurrency, int maxItemsPerTask, bool completeBeforeTaskWait)
        {
            //Create a custom TaskScheduler with specified max concurrency (TrackingTaskScheduler is defined in Common\tools\CommonUtils\TPLTestSchedulers.cs)
            TrackingTaskScheduler scheduler = new TrackingTaskScheduler(maxConcurrency);
            //We need to use the custom scheduler to achieve the results. As a by-product, we test to ensure custom schedulers are supported
            ConcurrentExclusiveSchedulerPair schedPair = new ConcurrentExclusiveSchedulerPair(scheduler, maxConcurrency, maxItemsPerTask);
            TaskFactory readers = new TaskFactory(schedPair.ConcurrentScheduler); //get reader and writer schedulers
            TaskFactory writers = new TaskFactory(schedPair.ExclusiveScheduler);

            //These are threadlocals to ensure that no concurrency side effects occur
            ThreadLocal<int> itemsExecutedCount = new ThreadLocal<int>(); //Track the items executed by CEScheduler Task
            ThreadLocal<int> schedulerIDInsideTask = new ThreadLocal<int>(); //Used to store the Scheduler ID observed by a Task Executed by CEScheduler Task

            //Work done by both reader and writer tasks  
            Action work = () =>
            {
                //Get the id of the parent Task (which is the task created by the scheduler). Each task run by the scheduler task should
                //see the same SchedulerID value since they are run on the same thread
                int id = ((TrackingTaskScheduler)scheduler).SchedulerID.Value;
                if (id == schedulerIDInsideTask.Value)
                { //since ids match, this is one more Task being executed by the CEScheduler Task
                    itemsExecutedCount.Value = ++itemsExecutedCount.Value;
                    //This does not need to be thread safe since we are looking to ensure that only n number of tasks were executed and not the order
                    //in which they were executed. Also asserting inside the thread is fine since we just want the test to be marked as failure
                    Assert.True(itemsExecutedCount.Value <= maxItemsPerTask, string.Format("itemsExecutedCount={0} cant be greater than maxValue={1}. Parent TaskID={2}",
                        itemsExecutedCount, maxItemsPerTask, id));
                }
                else
                { //Since ids don't match, this is the first Task being executed in the CEScheduler Task
                    schedulerIDInsideTask.Value = id; //cache the scheduler ID seen by the thread, so other tasks running in same thread can see this
                    itemsExecutedCount.Value = 1;
                }
                //Give enough time for a Task to stay around, so that other tasks will be executed by the same CEScheduler Task
                //or else the CESchedulerTask will die and each Task might get executed by a different CEScheduler Task. This does not affect the 
                //verifications, but its increases the chance of finding a bug if the maxItemPerTask is not respected
                new ManualResetEvent(false).WaitOne(1);
            };

            List<Task> taskList = new List<Task>();
            int maxConcurrentTasks = maxConcurrency * maxItemsPerTask * 5;
            int maxExclusiveTasks = maxConcurrency * maxItemsPerTask * 2;

            // Schedule Tasks in both concurrent and exclusive mode
            for (int i = 0; i < maxConcurrentTasks; i++)
                taskList.Add(readers.StartNew(work));
            for (int i = 0; i < maxExclusiveTasks; i++)
                taskList.Add(writers.StartNew(work));

            if (completeBeforeTaskWait)
            {
                schedPair.Complete();
                schedPair.Completion.Wait();
                Assert.True(taskList.TrueForAll(t => t.IsCompleted), "All tasks should have completed for scheduler to complete");
            }

            //finally wait for all of the tasks, to ensure they all executed properly
            Task.WaitAll(taskList.ToArray());

            if (!completeBeforeTaskWait)
            {
                schedPair.Complete();
                schedPair.Completion.Wait();
                Assert.True(taskList.TrueForAll(t => t.IsCompleted), "All tasks should have completed for scheduler to complete");
            }
        }

        /// <summary>
        /// When user specifies a concurrency level above the level allowed by the task scheduler, the concurrency level should be set
        /// to the concurrencylevel specified in the taskscheduler. Also tests that the maxConcurrencyLevel specified was respected
        /// </summary>
        [Fact]
        public static void TestLowerConcurrencyLevel()
        {
            //a custom scheduler with maxConcurrencyLevel of one
            int customSchedulerConcurrency = 1;
            TrackingTaskScheduler scheduler = new TrackingTaskScheduler(customSchedulerConcurrency);
            // specify a maxConcurrencyLevel > TaskScheduler's maxconcurrencyLevel to ensure the pair takes the min of the two
            ConcurrentExclusiveSchedulerPair schedPair = new ConcurrentExclusiveSchedulerPair(scheduler, int.MaxValue);
            Assert.Equal(scheduler.MaximumConcurrencyLevel, schedPair.ConcurrentScheduler.MaximumConcurrencyLevel);

            //Now schedule a reader task that would block and verify that more reader tasks scheduled are not executed 
            //(as long as the first task is blocked)
            TaskFactory readers = new TaskFactory(schedPair.ConcurrentScheduler);
            ManualResetEvent blockReaderTaskEvent = new ManualResetEvent(false);
            ManualResetEvent blockMainThreadEvent = new ManualResetEvent(false);

            //Add a reader tasks that would block
            readers.StartNew(() => { blockMainThreadEvent.Set(); blockReaderTaskEvent.WaitOne(); });
            blockMainThreadEvent.WaitOne(); // wait for the blockedTask to start execution

            //Now add more reader tasks
            int maxConcurrentTasks = Environment.ProcessorCount;
            List<Task> taskList = new List<Task>();
            for (int i = 0; i < maxConcurrentTasks; i++)
                taskList.Add(readers.StartNew(() => { })); //schedule some dummy reader tasks

            foreach (Task task in taskList)
            {
                bool wasTaskStarted = (task.Status != TaskStatus.Running) && (task.Status != TaskStatus.RanToCompletion);
                Assert.True(wasTaskStarted, string.Format("Additional reader tasks should not start when scheduler concurrency is {0} and a reader task is blocked", customSchedulerConcurrency));
            }

            //finally unblock the blocjedTask and wait for all of the tasks, to ensure they all executed properly
            blockReaderTaskEvent.Set();
            Task.WaitAll(taskList.ToArray());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestConcurrentBlockage(bool useReader)
        {
            ConcurrentExclusiveSchedulerPair schedPair = new ConcurrentExclusiveSchedulerPair();
            TaskFactory readers = new TaskFactory(schedPair.ConcurrentScheduler);
            TaskFactory writers = new TaskFactory(schedPair.ExclusiveScheduler);
            ManualResetEvent blockExclusiveTaskEvent = new ManualResetEvent(false);
            ManualResetEvent blockMainThreadEvent = new ManualResetEvent(false);
            ManualResetEvent blockMre = new ManualResetEvent(false);

            //Schedule a concurrent task and ensure that it is executed, just for fun
            Task<bool> conTask = readers.StartNew<bool>(() => { new ManualResetEvent(false).WaitOne(10); ; return true; });
            conTask.Wait();
            Assert.True(conTask.Result, "The concurrenttask when executed successfully should have returned true");

            //Now scehdule an exclusive task that is blocked(thereby preventing other concurrent tasks to finish)
            Task<bool> exclusiveTask = writers.StartNew<bool>(() => { blockMainThreadEvent.Set(); blockExclusiveTaskEvent.WaitOne(); return true; });

            //With exclusive task in execution mode, schedule a number of concurrent tasks and ensure they are not executed
            blockMainThreadEvent.WaitOne();
            List<Task> taskList = new List<Task>();
            for (int i = 0; i < 20; i++) taskList.Add(readers.StartNew<bool>(() => { blockMre.WaitOne(10); return true; }));

            foreach (Task task in taskList)
            {
                bool wasTaskStarted = (task.Status != TaskStatus.Running) && (task.Status != TaskStatus.RanToCompletion);
                Assert.True(wasTaskStarted, "Concurrent tasks should not be executed when an exclusive task is getting executed");
            }

            blockExclusiveTaskEvent.Set();
            Task.WaitAll(taskList.ToArray());
        }

        [Theory]
        [MemberData(nameof(ApiType))]
        public static void TestIntegration(string apiType, bool useReader)
        {
            Debug.WriteLine(string.Format(" Running apiType:{0} useReader:{1}", apiType, useReader));
            int taskCount = Environment.ProcessorCount; //To get varying number of tasks as a function of cores
            ConcurrentExclusiveSchedulerPair schedPair = new ConcurrentExclusiveSchedulerPair();
            CountdownEvent cde = new CountdownEvent(taskCount); //Used to track how many tasks were executed
            Action work = () => { cde.Signal(); }; //Work done by all APIs
            //Choose the right scheduler to use based on input parameter
            TaskScheduler scheduler = useReader ? schedPair.ConcurrentScheduler : schedPair.ExclusiveScheduler;

            SelectAPI2Target(apiType, taskCount, scheduler, work);
            cde.Wait(); //This will cause the test to block (and timeout) until all tasks are finished
        }



        /// <summary>
        /// Test to ensure that invalid parameters result in exceptions
        /// </summary>
        [Fact]
        public static void TestInvalidParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new ConcurrentExclusiveSchedulerPair(null)); //TargetScheduler is null
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, 0)); //maxConcurrencyLevel is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, -2)); //maxConcurrencyLevel is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, -1, 0)); //maxItemsPerTask  is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, -1, -2)); //maxItemsPerTask  is invalid
        }

        /// <summary>
        /// Test to ensure completion task works successfully
        /// </summary>
        [Fact]
        public static void TestCompletionTask()
        {
            // Completion tasks is valid after initialization
            {
                var cesp = new ConcurrentExclusiveSchedulerPair();
                Assert.True(cesp.Completion != null, "CompletionTask should never be null (after initialization)");
                Assert.True(!cesp.Completion.IsCompleted, "CompletionTask should not have completed");
            }

            // Completion task is valid after complete is called
            {
                var cesp = new ConcurrentExclusiveSchedulerPair();
                cesp.Complete();
                Assert.True(cesp.Completion != null, "CompletionTask should never be null (after complete)");
                cesp.Completion.Wait();
            }

            // Complete method may be called multiple times, and CompletionTask still completes
            {
                var cesp = new ConcurrentExclusiveSchedulerPair();
                for (int i = 0; i < 20; i++) cesp.Complete(); // ensure multiple calls to Complete succeed
                Assert.True(cesp.Completion != null, "CompletionTask should never be null (after multiple completes)");
                cesp.Completion.Wait();
            }

            // Can create a bunch of schedulers, do work on them all, complete them all, and they all complete
            {
                var cesps = new ConcurrentExclusiveSchedulerPair[100];
                for (int i = 0; i < cesps.Length; i++)
                {
                    cesps[i] = new ConcurrentExclusiveSchedulerPair();
                }
                for (int i = 0; i < cesps.Length; i++)
                {
                    Action work = () => new ManualResetEvent(false).WaitOne(2); ;
                    Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.None, cesps[i].ConcurrentScheduler);
                    Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.None, cesps[i].ExclusiveScheduler);
                }
                for (int i = 0; i < cesps.Length; i++)
                {
                    cesps[i].Complete();
                    cesps[i].Completion.Wait();
                }
            }

            // Validate that CESP does not implement IDisposable
            Assert.Equal(null, new ConcurrentExclusiveSchedulerPair() as IDisposable);
        }

        /// <summary>
        /// Ensure that CESPs can be layered on other CESPs.
        /// </summary
        [Fact]
        public static void TestSchedulerNesting()
        {
            // Create a hierarchical set of scheduler pairs
            var cespParent = new ConcurrentExclusiveSchedulerPair();

            var cespChild1 = new ConcurrentExclusiveSchedulerPair(cespParent.ConcurrentScheduler);
            var cespChild1Child1 = new ConcurrentExclusiveSchedulerPair(cespChild1.ConcurrentScheduler);
            var cespChild1Child2 = new ConcurrentExclusiveSchedulerPair(cespChild1.ExclusiveScheduler);

            var cespChild2 = new ConcurrentExclusiveSchedulerPair(cespParent.ExclusiveScheduler);
            var cespChild2Child1 = new ConcurrentExclusiveSchedulerPair(cespChild2.ConcurrentScheduler);
            var cespChild2Child2 = new ConcurrentExclusiveSchedulerPair(cespChild2.ExclusiveScheduler);

            // these are ordered such that we will complete the child schedulers before we complete their parents.  That way
            // we don't complete a parent that's still in use.
            var cesps = new[] {
                cespChild1Child1,
                cespChild1Child2,
                cespChild1,
                cespChild2Child1,
                cespChild2Child2,
                cespChild2,
                cespParent,
            };

            // Get the schedulers from all of the pairs
            List<TaskScheduler> schedulers = new List<TaskScheduler>();
            foreach (var s in cesps)
            {
                schedulers.Add(s.ConcurrentScheduler);
                schedulers.Add(s.ExclusiveScheduler);
            }

            // Keep track of all created tasks
            var tasks = new List<Task>();

            // Queue lots of work to each scheduler
            foreach (var scheduler in schedulers)
            {
                // Create a function that schedules and inlines recursively queued tasks
                Action<int> recursiveWork = null;
                recursiveWork = depth =>
                {
                    if (depth > 0)
                    {
                        Action work = () =>
                        {
                            var sw = new SpinWait();
                            while (!sw.NextSpinWillYield) sw.SpinOnce();
                            recursiveWork(depth - 1);
                        };

                        TaskFactory factory = new TaskFactory(scheduler);
                        Debug.WriteLine(string.Format("Start tasks in scheduler {0}", scheduler.Id));
                        Task t1 = factory.StartNew(work); Task t2 = factory.StartNew(work); Task t3 = factory.StartNew(work);
                        Task.WaitAll(t1, t2, t3);
                    }
                };

                for (int i = 0; i < 2; i++)
                {
                    tasks.Add(Task.Factory.StartNew(() => recursiveWork(2), CancellationToken.None, TaskCreationOptions.None, scheduler));
                }
            }

            // Wait for all tasks to complete, then complete the schedulers
            Task.WaitAll(tasks.ToArray());
            foreach (var cesp in cesps)
            {
                cesp.Complete();
                cesp.Completion.Wait();
            }
        }

        /// <summary>
        /// Ensure that continuations and parent/children which hop between concurrent and exclusive work correctly.
        /// EH
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestConcurrentExclusiveChain(bool syncContinuations)
        {
            var scheduler = new TrackingTaskScheduler(Environment.ProcessorCount);
            var cesp = new ConcurrentExclusiveSchedulerPair(scheduler);

            // continuations
            {
                var starter = new Task(() => { });
                var t = starter;
                for (int i = 0; i < 10; i++)
                {
                    t = t.ContinueWith(delegate { }, CancellationToken.None, syncContinuations ? TaskContinuationOptions.ExecuteSynchronously : TaskContinuationOptions.None, cesp.ConcurrentScheduler);
                    t = t.ContinueWith(delegate { }, CancellationToken.None, syncContinuations ? TaskContinuationOptions.ExecuteSynchronously : TaskContinuationOptions.None, cesp.ExclusiveScheduler);
                }
                starter.Start(cesp.ExclusiveScheduler);
                t.Wait();
            }

            // parent/child
            {
                var errorString = "hello faulty world";
                var root = Task.Factory.StartNew(() =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        Task.Factory.StartNew(() =>
                        {
                            Task.Factory.StartNew(() =>
                            {
                                Task.Factory.StartNew(() =>
                                {
                                    Task.Factory.StartNew(() =>
                                    {
                                        Task.Factory.StartNew(() =>
                                        {
                                            throw new InvalidOperationException(errorString);
                                        }, CancellationToken.None, TaskCreationOptions.AttachedToParent, cesp.ExclusiveScheduler).Wait();
                                    }, CancellationToken.None, TaskCreationOptions.AttachedToParent, cesp.ExclusiveScheduler);
                                }, CancellationToken.None, TaskCreationOptions.AttachedToParent, cesp.ConcurrentScheduler);
                            }, CancellationToken.None, TaskCreationOptions.AttachedToParent, cesp.ExclusiveScheduler);
                        }, CancellationToken.None, TaskCreationOptions.AttachedToParent, cesp.ConcurrentScheduler);
                    }, CancellationToken.None, TaskCreationOptions.AttachedToParent, cesp.ExclusiveScheduler);
                }, CancellationToken.None, TaskCreationOptions.None, cesp.ConcurrentScheduler);

                ((IAsyncResult)root).AsyncWaitHandle.WaitOne();
                Assert.True(root.IsFaulted, "Root should have been faulted by child's error");
                var ae = root.Exception.Flatten();
                Assert.True(ae.InnerException is InvalidOperationException && ae.InnerException.Message == errorString,
                    "Child's exception should have propagated to the root.");
            }
        }
        #endregion

        #region Helper Methods

        public static void SelectAPI2Target(string apiType, int taskCount, TaskScheduler scheduler, Action work)
        {
            switch (apiType)
            {
                case "StartNew":
                    for (int i = 0; i < taskCount; i++) new TaskFactory(scheduler).StartNew(() => { work(); });
                    break;
                case "Start":
                    for (int i = 0; i < taskCount; i++) new Task(() => { work(); }).Start(scheduler);
                    break;
                case "ContinueWith":
                    for (int i = 0; i < taskCount; i++)
                    {
                        new TaskFactory().StartNew(() => { }).ContinueWith((t) => { work(); }, scheduler);
                    }
                    break;
                case "FromAsync":
                    for (int i = 0; i < taskCount; i++)
                    {
                        new TaskFactory(scheduler).FromAsync(Task.Factory.StartNew(() => { }), (iar) => { work(); });
                    }
                    break;
                case "ContinueWhenAll":
                    for (int i = 0; i < taskCount; i++)
                    {
                        new TaskFactory(scheduler).ContinueWhenAll(new Task[] { Task.Factory.StartNew(() => { }) }, (t) => { work(); });
                    }
                    break;
                case "ContinueWhenAny":
                    for (int i = 0; i < taskCount; i++)
                    {
                        new TaskFactory(scheduler).ContinueWhenAny(new Task[] { Task.Factory.StartNew(() => { }) }, (t) => { work(); });
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Api name specified {0} is invalid or is of incorrect case", apiType));
            }
        }

        /// <summary>
        /// Used to provide parameters for the TestIntegration test
        /// </summary>
        public static IEnumerable<object[]> ApiType
        {
            get
            {
                List<object[]> values = new List<object[]>();
                foreach (string apiType in new string[] {
                    "StartNew", "Start", "ContinueWith", /* FromAsync: Not supported in .NET Native */ "ContinueWhenAll", "ContinueWhenAny" })
                {
                    foreach (bool useReader in new bool[] { true, false })
                    {
                        values.Add(new object[] { apiType, useReader });
                    }
                }

                return values;
            }
        }

        #endregion
    }
}
