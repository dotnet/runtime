// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TaskRunSync.cs
//
//
// Test class using UnitTestDriver that ensures that the Runsynchronously method works as excepted
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using Xunit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    #region Helper Classes / Enums

    public enum PreTaskStatus
    {
        Created,     // task has been created
        Continued,   // task is a continuation task
        Running,     // task has started running, could be waiting-to-run in the queue
        Canceled,    // task has been canceled before running
        Completed,   // task has been completed
    }

    public enum PostRunSyncAction
    {
        Wait, //to test you can wait on a task that was run synchronously
        Cancel, //to test you can cancel the token that was used by the task
        ContinueWith, //to test you can continuewith on a task that was run synchronously
    }

    public enum WorkloadType
    {
        CreateChildTask, //Start an attached childTask in the workload
        CreateDetachedChildTask, //start a detached childTask in the workload
        ContinueInside, //Invoke continuewith as the workload inside the task
        RunWithUserScheduler, //create a task with custom task scheduler that runs that task inline
        ThrowException, //throw an exception
    }

    public enum TaskSchedulerType
    {
        Default, //Use the default taskscheduler TaskScheduler.Current
        Null, //pass null as the Task Scheduler
        CustomWithInlineExecution, //Use a custom TaskScheduler that runs the Task inline
        CustomWithoutInlineExecution //Use a custom TaskScheduler that does not run the Task inline
    }

    /// <summary>
    /// An implementation of TaskScheduler that is able to perform RunSynchronously, and
    /// keep track of number of times Task was executed synchronously
    /// </summary>
    public class TaskRunSyncTaskScheduler : TaskScheduler, IDisposable
    {
        public bool AbleToExecuteInline { get; set; }
        public int RunSyncCalledCount { get; set; }
        private Task[] _threads;
        private BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        public TaskRunSyncTaskScheduler(bool ableToExecuteInline)
        {
            AbleToExecuteInline = ableToExecuteInline;

            /*need at least two threads since we might schedule two tasks (parent-child)*/
            int numberOfThreads = Math.Max(Environment.ProcessorCount, 2);
            _threads = new Task[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
            {
                _threads[i] = Task.Run(() =>
                {
                    foreach (var task in _tasks.GetConsumingEnumerable())
                    {
                        if (task.Status == TaskStatus.WaitingToRun)
                        {
                            ExecuteTask(task);
                        }
                    }
                });
            }
        }

        private bool ExecuteTask(Task task)
        {
            return TryExecuteTask(task);
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        public override int MaximumConcurrencyLevel
        {
            get
            {
                return _threads.Length;
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            RunSyncCalledCount++;
            if (taskWasPreviouslyQueued)
            {
                return false;
            }

            if (AbleToExecuteInline)
            {
                return ExecuteTask(task);
            }
            else
            {
                return false;
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks;
        }

        public void Dispose()
        {
            if (_threads != null)
            {
                _tasks.CompleteAdding();

                Task task;
                while (_tasks.TryTake(out task))
                    ;

                Task.WaitAll(_threads);
                _threads = null;
            }
        }
    }

    public class TestParameters_RunSync
    {
        public readonly PreTaskStatus PreTaskStatus;
        public readonly PostRunSyncAction PostRunSyncAction;
        public readonly WorkloadType WorkloadType;
        public readonly TaskCreationOptions TaskCreationOptions;
        public readonly TaskSchedulerType TaskSchedulerType;

        public TestParameters_RunSync(PreTaskStatus preTaskStatus, PostRunSyncAction postRunSyncAction, WorkloadType workType, TaskCreationOptions taskCreationOptions, TaskSchedulerType taskScheduler)
        {
            PreTaskStatus = preTaskStatus;
            PostRunSyncAction = postRunSyncAction;
            WorkloadType = workType;
            TaskCreationOptions = taskCreationOptions;
            TaskSchedulerType = taskScheduler;
        }
    }

    #endregion

    public sealed class TaskRunSyncTest
    {
        private PreTaskStatus _preTaskStatus;
        private PostRunSyncAction _postRunSyncAction;
        private WorkloadType _workloadType;
        private TaskCreationOptions _option;
        private TaskSchedulerType _taskSchedulerType;

        private Task _task;                   // the main task to be run synchronously
        private CancellationTokenSource _cts; // The CancellationTokenSource of which the Token is passed to the Main task
        private int _taskThreadID;

        public TaskRunSyncTest(TestParameters_RunSync parameters)
        {
            _preTaskStatus = parameters.PreTaskStatus;
            _postRunSyncAction = parameters.PostRunSyncAction;
            _workloadType = parameters.WorkloadType;
            _option = parameters.TaskCreationOptions;
            _taskSchedulerType = parameters.TaskSchedulerType;
        }

        /// <summary>
        /// The main test method that execute the API. There are five steps involved in the execution of the test
        /// </summary>
        internal void RealRun()
        {
            TaskScheduler ts = TaskScheduler.Default;
            switch (_taskSchedulerType)
            {
                case TaskSchedulerType.Null:
                    ts = null;
                    break;
                case TaskSchedulerType.CustomWithInlineExecution:
                    ts = new TaskRunSyncTaskScheduler(true);
                    break;
                case TaskSchedulerType.CustomWithoutInlineExecution:
                    ts = new TaskRunSyncTaskScheduler(false);
                    break;
                default:
                    ts = TaskScheduler.Default;
                    break;
            }

            // Stage 1 -- create task
            CreateTask();

            // Stage 2 - start with the pre-action 
            switch (_preTaskStatus)
            {
                case PreTaskStatus.Continued:
                    _task = _task.ContinueWith((t) => { }, _cts.Token, TaskContinuationOptions.None, ts);
                    break;

                case PreTaskStatus.Running:
                    _task.Start(ts);
                    break;

                case PreTaskStatus.Canceled:
                    _cts.Cancel();
                    break;

                case PreTaskStatus.Completed:
                    _task.Start(ts);
                    ((IAsyncResult)_task).AsyncWaitHandle.WaitOne();  // wait on AsyncWaitHandle to avoid getting exp
                    break;
            }

            int expectedThreadID = Environment.CurrentManagedThreadId;

            // Stage 3 -  exercise the API
            try
            {
                if (_taskSchedulerType == TaskSchedulerType.Default)
                    _task.RunSynchronously();
                else
                    _task.RunSynchronously(ts);

                if (ExpectRunSyncFailure)
                    Assert.True(false, string.Format("Fail to throw expected InvalidOperationException"));
                if (_taskSchedulerType == TaskSchedulerType.Null)
                    Assert.True(false, string.Format("Fail to throw expected ArgumentNullException"));
            }
            catch (InvalidOperationException ex)
            {
                if (!ExpectRunSyncFailure)
                    Assert.True(false, string.Format("Caught un-expected InvalidOperationException - {0}", ex));
                else
                {
                    Debug.WriteLine("Caught expected InvalidOperationException");
                    DisposeScheduler(ts);
                    return;
                }
            }
            catch (ArgumentNullException ex)
            {
                if (_taskSchedulerType != TaskSchedulerType.Null)
                    Assert.True(false, string.Format("Caught un-expected ArgumentNullException - {0}", ex));
                else
                {
                    Debug.WriteLine("Caught expected ArgumentNullException");
                    DisposeScheduler(ts);
                    return;
                }
            }

            // Stage 4 -  do verification against Context,  IsCompleted  and the TaskStatus
            if (_taskSchedulerType == TaskSchedulerType.CustomWithInlineExecution)
                Assert.Equal(expectedThreadID, _taskThreadID);
            else if (_taskSchedulerType == TaskSchedulerType.CustomWithoutInlineExecution)
                Assert.NotEqual(expectedThreadID, _taskThreadID);
            else if (_taskThreadID != expectedThreadID)
                Debug.WriteLine("Warning: RunSynchronously request ignored -- Task did not run under the same context");

            Assert.True(_task.IsCompleted, "RunSynchronously contract broken -- Task is not complete when the call return");

            if (_workloadType == WorkloadType.ThrowException)
            {
                if (_task.Status != TaskStatus.Faulted)
                    Assert.True(false, string.Format("Wrong final task status on a faulty workload"));

                CheckExpectedAggregateException(_task.Exception);
                //Assert.True(false, string.Format("Fail to record the test exception in Task.Exception"));
            }
            else
            {
                if (_task.Status != TaskStatus.RanToCompletion)
                    Assert.True(false, string.Format("Wrong final task status on a regular workload"));
            }

            //
            // Extra verification to ensure the Task was RunSynchronously on 
            // specified TaskScheduler
            //
            if (_taskSchedulerType == TaskSchedulerType.CustomWithInlineExecution ||
                _taskSchedulerType == TaskSchedulerType.CustomWithoutInlineExecution)
            {
                if (((TaskRunSyncTaskScheduler)ts).RunSyncCalledCount <= 0)
                    Assert.True(false, string.Format("Task wasn't RunSynchronously with TaskScheduler specified"));
            }

            // Stage 5 -  follow with the post-action
            switch (_postRunSyncAction)
            {
                case PostRunSyncAction.Wait:
                    try
                    {
                        if (_postRunSyncAction == PostRunSyncAction.Wait)
                            _task.Wait(0);
                        if (_workloadType == WorkloadType.ThrowException)
                            Assert.True(false, string.Format("expected failure is not propogated out of Wait"));
                    }
                    catch (AggregateException ae)
                    {
                        CheckExpectedAggregateException(ae);
                    }
                    break;

                case PostRunSyncAction.Cancel:

                    _cts.Cancel();
                    break;

                case PostRunSyncAction.ContinueWith:
                    _task.ContinueWith((t) => { }).Wait();
                    break;
            }

            DisposeScheduler(ts);
        }

        private static void DisposeScheduler(TaskScheduler ts)
        {
            if (ts is TaskRunSyncTaskScheduler)
            {
                ((TaskRunSyncTaskScheduler)ts).Dispose();
            }
        }

        private void CreateTask()
        {
            _cts = new CancellationTokenSource();

            _task = new Task((o) =>
            {
                _taskThreadID = Environment.CurrentManagedThreadId;

                switch (_workloadType)
                {
                    case WorkloadType.CreateChildTask:
                    case WorkloadType.CreateDetachedChildTask:
                        Task.Factory.StartNew(() => { }, _workloadType == WorkloadType.CreateDetachedChildTask
                                                 ? TaskCreationOptions.None
                                                 : TaskCreationOptions.AttachedToParent);
                        break;

                    case WorkloadType.ContinueInside:
                        _task.ContinueWith((t) => { });
                        break;

                    case WorkloadType.RunWithUserScheduler:
                        TaskScheduler ts = new TaskRunSyncTaskScheduler(true);
                        Task.Factory.StartNew(() => { }, _cts.Token, TaskCreationOptions.AttachedToParent, ts).ContinueWith((task) => DisposeScheduler(ts), TaskScheduler.Default);
                        break;

                    case WorkloadType.ThrowException:
                        throw new TPLTestException();
                }
            }, null, _cts.Token, _option);
        }

        private bool ExpectRunSyncFailure
        {
            get
            {
                // The following cases will cause an exception
                // 1. Task already started / canceled / disposed / completed 
                // 2. Task is a continuation task 
                return (_preTaskStatus != PreTaskStatus.Created);
            }
        }

        /// <summary>
        /// Method that checks to ensure that the AggregateException contains TPLException (the one throw by the workload)
        /// </summary>
        /// <param name="ae"></param>
        /// <returns></returns>
        private void CheckExpectedAggregateException(AggregateException ae)
        {
            if (_workloadType == WorkloadType.ThrowException)
                ae.Flatten().Handle((e) => e is TPLTestException);
            else
                Assert.True(false, string.Format("Caught un-expected exception - {0]. Fail to re-propagate the test exception via Wait", ae));
        }
    }

    public class TaskRunSyncTests
    {
        static TaskRunSyncTests()
        {
            // Tests that create tasks which need to run concurrently require us to bump up the number
            // of threads in the pool, or else we need to wait for it to grow dynamically to the desired number
            ThreadPoolHelpers.EnsureMinThreadsAtLeast(10);
        }

        #region Test methods

        [Fact]
        public static void TaskRunSyncTest0()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Canceled, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest1()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Canceled, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest2()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Canceled, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest3()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Completed, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest4()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Completed, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest5()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Completed, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest6()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Continued, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest7()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Continued, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest8()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Continued, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskRunSyncTest9()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Cancel, WorkloadType.ContinueInside, TaskCreationOptions.LongRunning, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskRunSyncTest10()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Cancel, WorkloadType.CreateChildTask, TaskCreationOptions.LongRunning, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskRunSyncTest11()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Cancel, WorkloadType.CreateDetachedChildTask, TaskCreationOptions.AttachedToParent, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskRunSyncTest12()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Cancel, WorkloadType.CreateDetachedChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskRunSyncTest13()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Cancel, WorkloadType.RunWithUserScheduler, TaskCreationOptions.None, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest14()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Cancel, WorkloadType.ThrowException, TaskCreationOptions.AttachedToParent, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest15()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.ContinueWith, WorkloadType.ContinueInside, TaskCreationOptions.None, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskRunSyncTest16()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.ContinueWith, WorkloadType.CreateChildTask, TaskCreationOptions.AttachedToParent, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest17()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.ContinueWith, WorkloadType.CreateDetachedChildTask, TaskCreationOptions.AttachedToParent, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest18()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.ContinueWith, WorkloadType.RunWithUserScheduler, TaskCreationOptions.LongRunning, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest19()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.ContinueWith, WorkloadType.ThrowException, TaskCreationOptions.LongRunning, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest20()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Wait, WorkloadType.ContinueInside, TaskCreationOptions.AttachedToParent, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest21()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.Null);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskRunSyncTest22()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Wait, WorkloadType.CreateDetachedChildTask, TaskCreationOptions.LongRunning, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest23()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Wait, WorkloadType.RunWithUserScheduler, TaskCreationOptions.AttachedToParent, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void TaskRunSyncTest24()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Created, PostRunSyncAction.Wait, WorkloadType.ThrowException, TaskCreationOptions.None, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest28()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Running, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest29()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Running, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.CustomWithoutInlineExecution);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskRunSyncTest30()
        {
            TestParameters_RunSync parameters = new TestParameters_RunSync(PreTaskStatus.Running, PostRunSyncAction.Wait, WorkloadType.CreateChildTask, TaskCreationOptions.None, TaskSchedulerType.Default);
            TaskRunSyncTest test = new TaskRunSyncTest(parameters);
            test.RealRun();
        }

        #endregion
    }
}
