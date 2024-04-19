// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks.Tests.Status
{
    #region Helper Classes / Enum

    public class TestParameters
    {
        public readonly TestAction TestAction;
        public MyTaskCreationOptions ChildTaskCreationOptions;
        public bool CreateChildTask;
        public bool IsPromise;
        public TaskStatus? FinalTaskStatus;
        public TaskStatus? FinalChildTaskStatus;
        public TaskStatus? FinalPromiseStatus;

        public TestParameters(TestAction testAction)
        {
            TestAction = testAction;
        }
    }

    public enum TestAction
    {
        None,
        CompletedTask,
        CancelTask,
        CancelTaskAndAcknowledge,
        CancelScheduledTask,
        CancelCreatedTask,
        FailedTask,
        FailedChildTask
    }

    public enum MyTaskCreationOptions
    {
        None = TaskCreationOptions.None,
        RespectParentCancellation = -2,
        AttachedToParent = TaskCreationOptions.AttachedToParent
    }

    public class StatusTestException : Exception { }

    #endregion

    public sealed class TaskStatusTest
    {
        #region Private Fields
        private Task _task;
        private Task _childTask;
        private TaskCompletionSource<int> _promise;
        private CancellationToken _childTaskToken;

        private readonly MyTaskCreationOptions _childCreationOptions;

        private readonly bool _createChildTask;
        private readonly bool _isPromise;

        private TaskStatus? _finalTaskStatus;
        private TaskStatus? _finalChildTaskStatus;
        private TaskStatus? _finalPromiseStatus;

        private volatile TestAction _testAction;
        private readonly ManualResetEventSlim _mre;
        private readonly CancellationTokenSource _taskCts;

        #endregion

        public TaskStatusTest(TestParameters parameters)
        {
            _testAction = parameters.TestAction;
            _childCreationOptions = parameters.ChildTaskCreationOptions;
            _createChildTask = parameters.CreateChildTask;
            _isPromise = parameters.IsPromise;

            _finalTaskStatus = parameters.FinalTaskStatus;
            _finalChildTaskStatus = parameters.FinalChildTaskStatus;
            _finalPromiseStatus = parameters.FinalPromiseStatus;

            _mre = new ManualResetEventSlim(false);
            _taskCts = new CancellationTokenSource();
            _childTaskToken = new CancellationToken(false);
        }

        internal void RealRun()
        {
            if (_isPromise)
            {
                _promise = new TaskCompletionSource<int>();
            }
            else
            {
                _task = new Task(TaskRun, _taskCts.Token);
            }

            if (_testAction != TestAction.None)
            {
                try
                {
                    bool executeTask = false;

                    if (_isPromise)
                    {
                        switch (_testAction)
                        {
                            case TestAction.CompletedTask:
                                _promise.SetResult(1);
                                break;
                            case TestAction.FailedTask:
                                _promise.SetException(new StatusTestException());
                                break;
                        }
                    }
                    else
                    {
                        if (_testAction == TestAction.CancelScheduledTask)
                        {
                            CancelWaitingToRunTaskScheduler scheduler = new CancelWaitingToRunTaskScheduler();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            scheduler.Cancellation = cts;

                            // Replace _task with a task that has a custom scheduler
                            _task = Task.Factory.StartNew(() => { }, cts.Token, TaskCreationOptions.None, scheduler);

                            try { _task.GetAwaiter().GetResult(); }
                            catch (Exception ex)
                            {
                                if (ex is OperationCanceledException)
                                    Debug.WriteLine("OperationCanceledException Exception was thrown as expected");
                                else
                                    Assert.Fail(string.Format("Unexpected exception was thrown: \n{0}", ex.ToString()));
                            }
                        }
                        else if (_testAction == TestAction.CancelCreatedTask)
                        {
                            _taskCts.Cancel();
                        }
                        else //When the TestAction is CompletedTask and IsPromise is false, the code will reach this point
                        {
                            executeTask = true;
                            _task.Start();
                        }
                    }

                    if (_task != null && executeTask)
                    {
                        _mre.Wait();
                        //
                        // Current Task status is WaitingForChildrenToComplete if Task didn't Cancel/Faulted and Child was created
                        // without Detached options and current status of the child isn't RanToCompletion or Faulted yet
                        //

                        Task.Delay(1).Wait();

                        if (_createChildTask &&
                            _childTask != null &&
                            _testAction != TestAction.CancelTask &&
                            _testAction != TestAction.CancelTaskAndAcknowledge &&
                            _testAction != TestAction.FailedTask &&
                            _childCreationOptions == MyTaskCreationOptions.AttachedToParent &&
                            _childTask.Status != TaskStatus.RanToCompletion &&
                            _childTask.Status != TaskStatus.Faulted)
                        {
                            //we may have reach this point too soon, let's keep spinning until the status changes.
                            while (_task.Status == TaskStatus.Running)
                            {
                                Task.Delay(1).Wait();
                            }

                            //
                            // If we're still waiting for children our Status should reflect so. For this verification, the
                            // parent task's status needs to be read before the child task's status (they are volatile loads) to
                            // make the child task's status more recent, since the child task may complete during the status
                            // reads.
                            //
                            if (_task.Status != TaskStatus.WaitingForChildrenToComplete && !_childTask.IsCompleted)
                            {
                                Assert.Fail(string.Format("Expecting current Task status to be WaitingForChildren but getting {0}", _task.Status.ToString()));
                            }
                        }
                        _task.Wait();
                    }
                }
                catch (AggregateException exp)
                {
                    if ((_testAction == TestAction.CancelTaskAndAcknowledge || _testAction == TestAction.CancelScheduledTask || _testAction == TestAction.CancelCreatedTask) &&
                        exp.Flatten().InnerException.GetType() == typeof(TaskCanceledException))
                    {
                        Debug.WriteLine("TaskCanceledException Exception was thrown as expected");
                    }
                    else if ((_testAction == TestAction.FailedTask || _testAction == TestAction.FailedChildTask) && _task.IsFaulted &&
                        exp.Flatten().InnerException.GetType() == typeof(StatusTestException))
                    {
                        Debug.WriteLine("StatusTestException Exception was thrown as expected");
                    }
                    else
                    {
                        Assert.Fail(string.Format("Unexpected exception was thrown: \n{0}", exp.ToString()));
                    }
                }

                try
                {
                    //
                    // Need to wait for Children task if it was created with Default option (Detached by default),
                    // or current task was either canceled or failed
                    //
                    if (_createChildTask &&
                        (_childCreationOptions == MyTaskCreationOptions.None ||
                        _testAction == TestAction.CancelTask ||
                        _testAction == TestAction.CancelTaskAndAcknowledge ||
                        _testAction == TestAction.FailedTask))
                    {
                        _childTask.Wait();
                    }
                }
                catch (AggregateException exp)
                {
                    if (((_testAction == TestAction.CancelTask || _testAction == TestAction.CancelTaskAndAcknowledge) &&
                        _childCreationOptions == MyTaskCreationOptions.RespectParentCancellation) &&
                        exp.Flatten().InnerException.GetType() == typeof(TaskCanceledException))
                    {
                        Debug.WriteLine("TaskCanceledException Exception was thrown as expected");
                    }
                    else if (_testAction == TestAction.FailedChildTask && _childTask.IsFaulted &&
                        exp.Flatten().InnerException.GetType() == typeof(StatusTestException))
                    {
                        Debug.WriteLine("StatusTestException Exception was thrown as expected");
                    }
                    else
                    {
                        Assert.Fail(string.Format("Unexpected exception was thrown: \n{0}", exp.ToString()));
                    }
                }
            }

            //
            // Verification
            //
            if (_finalTaskStatus != null && _finalTaskStatus.Value != _task.Status)
            {
                Assert.Fail(string.Format("Expecting Task final Status to be {0}, while getting {1}", _finalTaskStatus.Value, _task.Status));
            }
            if (_finalChildTaskStatus != null && _finalChildTaskStatus.Value != _childTask.Status)
            {
                Assert.Fail(string.Format("Expecting Child Task final Status to be {0}, while getting {1}", _finalChildTaskStatus.Value, _childTask.Status));
            }
            if (_finalPromiseStatus != null && _finalPromiseStatus.Value != _promise.Task.Status)
            {
                Assert.Fail(string.Format("Expecting Promise Status to be {0}, while getting {1}", _finalPromiseStatus.Value, _promise.Task.Status));
            }

            //
            // Extra verifications for Cancel Task
            //
            if (_task != null && _task.Status == TaskStatus.Canceled && _task.IsCanceled != true)
            {
                Assert.Fail(string.Format("Task final Status is Canceled, expecting IsCanceled property to be True as well"));
            }
            if (_childTask != null && _childTask.Status == TaskStatus.Canceled && _childTask.IsCanceled != true)
            {
                Assert.Fail(string.Format("Child Task final Status is Canceled, expecting IsCanceled property to be True as well"));
            }

            //
            // Extra verification for faulted Promise
            //
            if (_isPromise && _testAction == TestAction.FailedTask)
            {
                //
                // If promise with Exception, read the exception so we don't
                // crash on Finalizer
                //
                AggregateException exp = _promise.Task.Exception;
                if (!_promise.Task.IsFaulted || exp == null)
                {
                    Assert.Fail(string.Format("No Exception found on promise"));
                }
                else if (exp.Flatten().InnerException.GetType() == typeof(StatusTestException))
                {
                    Debug.WriteLine("StatusTestException Exception was thrown as expected");
                }
                else
                {
                    Assert.Fail(string.Format("Exception on promise has mismatched type, expecting StatusTestException, actual: {0}", exp.Flatten().InnerException.GetType()));
                }
            }
        }

        private void TaskRun()
        {
            try
            {
                ManualResetEventSlim childMre = new ManualResetEventSlim(initialState: false);

                if (_createChildTask)
                {
                    TaskCreationOptions childTCO = (TaskCreationOptions)(int)_childCreationOptions;

                    //
                    // Pass the same token used by parent to child Task to simulate RespectParentCancellation
                    //
                    if (_childCreationOptions == MyTaskCreationOptions.RespectParentCancellation)
                    {
                        _childTaskToken = _taskCts.Token;
                        childTCO = TaskCreationOptions.AttachedToParent;
                    }

                    _childTask = new Task(ChildTaskRun, childMre, _childTaskToken, childTCO);

                    if (_childTask.Status != TaskStatus.Created)
                    {
                        Assert.Fail(string.Format("Expecting Child Task status to be Created while getting {0}", _childTask.Status.ToString()));
                    }

                    _childTask.Start();

                    if (_testAction != TestAction.CancelTask && _testAction != TestAction.CancelTaskAndAcknowledge)
                    {
                        //
                        // if cancel action, release the child task after calling Cancel()
                        //
                        childMre.Set();
                    }
                }

                if (_task.Status != TaskStatus.Running)
                {
                    Assert.Fail(string.Format("Expecting Current Task status to be Running while getting {0}", _task.Status.ToString()));
                }

                switch (_testAction)
                {
                    case TestAction.CancelTask:
                        _taskCts.Cancel();
                        childMre.Set();

                        break;
                    case TestAction.CancelTaskAndAcknowledge:
                        _taskCts.Cancel();
                        childMre.Set();

                        if (_taskCts.Token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(_taskCts.Token);
                        }
                        break;
                    case TestAction.FailedTask:
                        throw new StatusTestException();
                }
            }
            finally
            {
                _mre.Set();
            }
            return;
        }

        private void ChildTaskRun(object o)
        {
            (o as ManualResetEventSlim)?.Wait();

            if (_childTask.Status != TaskStatus.Running)
            {
                Assert.Fail(string.Format("Expecting Child Task status to be Running while getting {0}", _childTask.Status.ToString()));
            }
            switch (_testAction)
            {
                case TestAction.FailedChildTask:
                    throw new StatusTestException();
            }

            //
            // Sleep for a few milliseconds to simulate a child task executing
            //
            Task t = Task.Delay(1);
            t.Wait();

            if (_childTaskToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(_childTaskToken);
            }
            return;
        }
    }

    // Custom task scheduler that allows a task to be cancelled before queuing it
    internal class CancelWaitingToRunTaskScheduler : TaskScheduler
    {
        public CancellationTokenSource Cancellation;

        protected override void QueueTask(Task task)
        {
            Cancellation.Cancel();
            TryExecuteTask(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) { return false; }
        protected override IEnumerable<Task> GetScheduledTasks() { return null; }
    }

    public sealed class TaskStatusTests
    {
        [Fact]
        public static void TaskStatus0()
        {
            TestParameters parameters = new TestParameters(TestAction.None)
            {
                FinalTaskStatus = TaskStatus.Created,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskStatus1()
        {
            TestParameters parameters = new TestParameters(TestAction.None)
            {
                IsPromise = true,
                FinalPromiseStatus = TaskStatus.WaitingForActivation,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskStatus2()
        {
            TestParameters parameters = new TestParameters(TestAction.CompletedTask)
            {
                FinalTaskStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskStatus3()
        {
            TestParameters parameters = new TestParameters(TestAction.CompletedTask)
            {
                ChildTaskCreationOptions = MyTaskCreationOptions.AttachedToParent,
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.RanToCompletion,
                FinalChildTaskStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskStatus4()
        {
            TestParameters parameters = new TestParameters(TestAction.CompletedTask)
            {
                IsPromise = true,
                FinalPromiseStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskStatus5()
        {
            TestParameters parameters = new TestParameters(TestAction.FailedTask)
            {
                IsPromise = true,
                FinalPromiseStatus = TaskStatus.Faulted,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskStatus6()
        {
            TestParameters parameters = new TestParameters(TestAction.CancelCreatedTask)
            {
                FinalTaskStatus = TaskStatus.Canceled,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskStatus7()
        {
            TestParameters parameters = new TestParameters(TestAction.CancelScheduledTask)
            {
                FinalTaskStatus = TaskStatus.Canceled,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskStatus8()
        {
            TestParameters parameters = new TestParameters(TestAction.CancelTask)
            {
                FinalTaskStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskStatus9()
        {
            TestParameters parameters = new TestParameters(TestAction.CancelTask)
            {
                ChildTaskCreationOptions = MyTaskCreationOptions.AttachedToParent,
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.RanToCompletion,
                FinalChildTaskStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskStatus10()
        {
            TestParameters parameters = new TestParameters(TestAction.CancelTask)
            {
                ChildTaskCreationOptions = MyTaskCreationOptions.RespectParentCancellation,
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.RanToCompletion,
                FinalChildTaskStatus = TaskStatus.Canceled,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskStatus11()
        {
            TestParameters parameters = new TestParameters(TestAction.FailedTask)
            {
                FinalTaskStatus = TaskStatus.Faulted,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskStatus12()
        {
            TestParameters parameters = new TestParameters(TestAction.FailedTask)
            {
                ChildTaskCreationOptions = MyTaskCreationOptions.AttachedToParent,
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.Faulted,
                FinalChildTaskStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskStatus13()
        {
            TestParameters parameters = new TestParameters(TestAction.FailedTask)
            {
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.Faulted,
                FinalChildTaskStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskStatus14()
        {
            TestParameters parameters = new TestParameters(TestAction.FailedChildTask)
            {
                ChildTaskCreationOptions = MyTaskCreationOptions.AttachedToParent,
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.Faulted,
                FinalChildTaskStatus = TaskStatus.Faulted,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskStatus15()
        {
            TestParameters parameters = new TestParameters(TestAction.FailedChildTask)
            {
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.RanToCompletion,
                FinalChildTaskStatus = TaskStatus.Faulted,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskStatus16()
        {
            TestParameters parameters = new TestParameters(TestAction.CancelTaskAndAcknowledge)
            {
                ChildTaskCreationOptions = MyTaskCreationOptions.AttachedToParent,
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.Canceled,
                FinalChildTaskStatus = TaskStatus.RanToCompletion,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskStatus17()
        {
            TestParameters parameters = new TestParameters(TestAction.CancelTaskAndAcknowledge)
            {
                ChildTaskCreationOptions = MyTaskCreationOptions.RespectParentCancellation,
                CreateChildTask = true,
                FinalTaskStatus = TaskStatus.Canceled,
                FinalChildTaskStatus = TaskStatus.Canceled,
            };
            TaskStatusTest test = new TaskStatusTest(parameters);
            test.RealRun();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(Status_IsProperties_Match_MemberData))]
        public void Status_IsProperties_Match(StrongBox<Task> taskBox)
        {
            // The StrongBox<Task> is a workaround for xunit trying to automatically
            // Dispose of any IDisposable passed into a theory, but Task doesn't like
            // being Dispose'd when it's not in a final state.
            Task task = taskBox.Value;

            if (task.IsCompletedSuccessfully)
            {
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            }
            else if (task.IsFaulted)
            {
                Assert.Equal(TaskStatus.Faulted, task.Status);
            }
            else if (task.IsCanceled)
            {
                Assert.Equal(TaskStatus.Canceled, task.Status);
            }

            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    Assert.True(task.IsCompleted, "Expected IsCompleted to be true");
                    Assert.True(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be true");
                    Assert.False(task.IsFaulted, "Expected IsFaulted to be false");
                    Assert.False(task.IsCanceled, "Expected IsCanceled to be false");
                    break;

                case TaskStatus.Faulted:
                    Assert.True(task.IsCompleted, "Expected IsCompleted to be true");
                    Assert.False(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be false");
                    Assert.True(task.IsFaulted, "Expected IsFaulted to be true");
                    Assert.False(task.IsCanceled, "Expected IsCanceled to be false");
                    break;

                case TaskStatus.Canceled:
                    Assert.True(task.IsCompleted, "Expected IsCompleted to be true");
                    Assert.False(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be false");
                    Assert.False(task.IsFaulted, "Expected IsFaulted to be false");
                    Assert.True(task.IsCanceled, "Expected IsCanceled to be true");
                    break;

                default:
                    Assert.False(task.IsCompleted, "Expected IsCompleted to be false");
                    Assert.False(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be false");
                    Assert.False(task.IsFaulted, "Expected IsFaulted to be false");
                    Assert.False(task.IsCanceled, "Expected IsCanceled to be false");
                    break;
            }
        }

        public static IEnumerable<object[]> Status_IsProperties_Match_MemberData()
        {
            yield return new object[] { new StrongBox<Task>(Task.CompletedTask) };

            yield return new object[] { new StrongBox<Task>(new Task(() => { })) };

            yield return new object[] { new StrongBox<Task>(new TaskCompletionSource<int>().Task) };

            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetResult(42);
                yield return new object[] { new StrongBox<Task>(tcs.Task) };
            }

            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetException(new Exception());
                yield return new object[] { new StrongBox<Task>(tcs.Task) };
            }

            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetCanceled();
                yield return new object[] { new StrongBox<Task>(tcs.Task) };
            }

            {
                var t = Task.Run(() => { });
                t.Wait();
                yield return new object[] { new StrongBox<Task>(t) };
            }

            {
                var atmb = new AsyncTaskMethodBuilder<bool>();
                atmb.SetResult(true);
                yield return new object[] { new StrongBox<Task>(atmb.Task) };
            }
        }
    }
}
