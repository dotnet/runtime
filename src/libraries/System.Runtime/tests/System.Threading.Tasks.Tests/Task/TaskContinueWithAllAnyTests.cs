// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests.ContinueWithAllAny
{
    #region Helper Classes / Enums

    /// <summary>
    /// Tests for ContinueWhenAny, and ContinueWhenAll
    //  The strategy is simple: create one or more antecedent tasks to pass into ContinueWhen/ContinueWhenAny.
    ///  Then, call ContinueWhen/ContinueWhenAny. Then check whether the continuation ran or not.
    /// </summary>
    public sealed class TaskContinueWithAllAnyTest
    {
        #region Private Fields

        private API _api;                               // the API to be tested
        private TaskType _taskType;                     // the continuation chain type
        private TaskContinuationOptions _tcOption;      // the TaskContinuationOptions given to the ContinueWhenAll/Any
        private TaskScheduler _tm;                      // the TaskScheduler given to the ContinueWhenAll/Any
        private CancellationToken _cancellationToken;   // the CancellationToken given to the ContinueWhenAll/Any
        private Task _continuation = null;

        private TaskInfo[] _taskInfos;                  // task info for each task
        private Task[] _tasks;                          // tasks to be continued from

        #endregion

        #region Constructor

        /// <summary>
        /// Create the test given the parameters
        /// </summary>
        public TaskContinueWithAllAnyTest(TestParameters parameters)
        {
            _api = parameters.Api;
            _taskType = parameters.TaskType;
            _tcOption = parameters.ContinuationOptions;


            // set up the TaskScheduler under which the continuation will be scheduled
            _tm = TaskScheduler.Default;

            // create a new cancellation token for each test
            _cancellationToken = parameters.WithCancellation ? CancellationToken.None : new CancellationToken();

            _taskInfos = parameters.AllTaskInfos;
            _tasks = new Task[parameters.AllTaskInfos.Length];
        }

        #endregion

        /// <summary>
        /// This is the real execution
        /// </summary>
        /// <returns>true for pass, false otherwise</returns>
        internal void RealRun()
        {
            CreateTasks();

            // set up the continuation Action/Func body delegate
            // The continuation task is the one that actually verifies that the antecedent tasks executed.

            Action<Task[]> allCompletedAction = (inputTasks) =>
            {
                VerifyAll(inputTasks);
            };

            Action<Task<double>[]> allCompletedActionT = (inputTasks) =>
            {
                VerifyAllT(inputTasks);
            };

            Action<Task> oneCompletedAction = (inputTask) =>
            {
                VerifyAny(inputTask);
            };

            Action<Task<double>> oneCompletedActionT = (inputTask) =>
            {
                VerifyAnyT(inputTask);
            };

            Func<Task[], bool> allCompletedFunc = (inputTasks) =>
            {
                VerifyAll(inputTasks);
                return true;
            };

            Func<Task<double>[], bool> allCompletedFuncT = (inputTasks) =>
            {
                VerifyAllT(inputTasks);
                return true;
            };

            Func<Task, bool> oneCompletedFunc = (inputTask) =>
            {
                VerifyAny(inputTask);
                return true;
            };

            Func<Task<double>, bool> oneCompletedFuncT = (inputTask) =>
            {
                VerifyAnyT(inputTask);
                return true;
            };

            // invoke various continueWith overloads
            //
            // The long and deep nested if statements can be difficult to follow, but
            // in general it is easier to go back from the instance of the test in the
            // xml file src\QA\PCP\Pfx\Functional\TPL\APIs\Task\Data\TaskContinueWithAllAnyTest.xml
            // and map it to one of the things this is running.
            //
            // That is, given a task type, tm type, api, cancellation, etc. it should be
            // straightforward to figure out what variant of ContinueWhen is called.
            //
            switch (_taskType)
            {
                case TaskType.TaskContinueWithTask:

                    if (_cancellationToken.CanBeCanceled)
                    {
                        if (_api == API.ContinueWhenAll)
                        {
                            _continuation = Task.Factory.ContinueWhenAll(_tasks, allCompletedAction, _cancellationToken);
                        }
                        else // must be API.ContinueWhenAny
                        {
                            _continuation = Task.Factory.ContinueWhenAny(_tasks, oneCompletedAction, _cancellationToken);
                        }
                    }
                    else if (_tm != TaskScheduler.Default)
                    {
                        if (_api == API.ContinueWhenAll)
                        {
                            _continuation = Task.Factory.ContinueWhenAll(_tasks, allCompletedAction, _cancellationToken, _tcOption, _tm);
                        }
                        else // must be API.ContinueWhenAny
                        {
                            _continuation = Task.Factory.ContinueWhenAny(_tasks, oneCompletedAction, _cancellationToken, _tcOption, _tm);
                        }
                    }
                    else if (_tcOption != TaskContinuationOptions.None)
                    {
                        if (_api == API.ContinueWhenAll)
                        {
                            _continuation = Task.Factory.ContinueWhenAll(_tasks, allCompletedAction, _tcOption);
                        }
                        else // must be API.ContinueWhenAny
                        {
                            _continuation = Task.Factory.ContinueWhenAny(_tasks, oneCompletedAction, _tcOption);
                        }
                    }
                    else
                    {
                        if (_api == API.ContinueWhenAll)
                        {
                            _continuation = Task.Factory.ContinueWhenAll(_tasks, allCompletedAction);
                        }
                        else // must be API.ContinueWhenAny
                        {
                            _continuation = Task.Factory.ContinueWhenAny(_tasks, oneCompletedAction);
                        }
                    }

                    break;

                case TaskType.TaskTContinueWithTask:

                    Task<double>[] taskDoubles = new Task<double>[_tasks.Length];
                    for (int i = 0; i < _tasks.Length; i++)
                        taskDoubles[i] = (Task<double>)_tasks[i];

                    if (_cancellationToken.CanBeCanceled)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double>(taskDoubles, allCompletedActionT, _cancellationToken);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double>(taskDoubles, oneCompletedActionT, _cancellationToken);
                    }
                    else if (_tm != TaskScheduler.Default)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double>(taskDoubles, allCompletedActionT, _cancellationToken, _tcOption, _tm);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double>(taskDoubles, oneCompletedActionT, _cancellationToken, _tcOption, _tm);
                    }
                    else if (_tcOption != TaskContinuationOptions.None)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double>(taskDoubles, allCompletedActionT, _tcOption);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double>(taskDoubles, oneCompletedActionT, _tcOption);
                    }
                    else
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double>(taskDoubles, allCompletedActionT);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double>(taskDoubles, oneCompletedActionT);
                    }

                    break;

                case TaskType.TaskContinueWithTaskT_NEW:

                    if (_cancellationToken.CanBeCanceled)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<bool>(_tasks, allCompletedFunc, _cancellationToken);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<bool>(_tasks, oneCompletedFunc, _cancellationToken);
                    }
                    else if (_tm != TaskScheduler.Default)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<bool>(_tasks, allCompletedFunc, _cancellationToken, _tcOption, _tm);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<bool>(_tasks, oneCompletedFunc, _cancellationToken, _tcOption, _tm);
                    }
                    else if (_tcOption != TaskContinuationOptions.None)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<bool>(_tasks, allCompletedFunc, _tcOption);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<bool>(_tasks, oneCompletedFunc, _tcOption);
                    }
                    else
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<bool>(_tasks, allCompletedFunc);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<bool>(_tasks, oneCompletedFunc);
                    }

                    break;

                case TaskType.TaskContinueWithTaskT:

                    if (_cancellationToken.CanBeCanceled)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll(_tasks, allCompletedFunc, _cancellationToken);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny(_tasks, oneCompletedFunc, _cancellationToken);
                    }
                    else if (_tm != TaskScheduler.Default)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll(_tasks, allCompletedFunc, _cancellationToken, _tcOption, _tm);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny(_tasks, oneCompletedFunc, _cancellationToken, _tcOption, _tm);
                    }
                    else if (_tcOption != TaskContinuationOptions.None)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll(_tasks, allCompletedFunc, _tcOption);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny(_tasks, oneCompletedFunc, _tcOption);
                    }
                    else
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll(_tasks, allCompletedFunc);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny(_tasks, oneCompletedFunc);
                    }

                    break;

                case TaskType.TaskTContinueWithTaskT_NEW:

                    Task<double>[] taskDoublesB = new Task<double>[_tasks.Length];
                    for (int i = 0; i < _tasks.Length; i++)
                        taskDoublesB[i] = (Task<double>)_tasks[i];

                    if (_cancellationToken.CanBeCanceled)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double, bool>(taskDoublesB, allCompletedFuncT, _cancellationToken);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double, bool>(taskDoublesB, oneCompletedFuncT, _cancellationToken);
                    }
                    else if (_tm != TaskScheduler.Default)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double, bool>(taskDoublesB, allCompletedFuncT, _cancellationToken, _tcOption, _tm);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double, bool>(taskDoublesB, oneCompletedFuncT, _cancellationToken, _tcOption, _tm);
                    }
                    else if (_tcOption != TaskContinuationOptions.None)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double, bool>(taskDoublesB, allCompletedFuncT, _tcOption);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double, bool>(taskDoublesB, oneCompletedFuncT, _tcOption);
                    }
                    else
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task.Factory.ContinueWhenAll<double, bool>(taskDoublesB, allCompletedFuncT);
                        else // must be API.ContinueWhenAny
                            _continuation = Task.Factory.ContinueWhenAny<double, bool>(taskDoublesB, oneCompletedFuncT);
                    }

                    break;

                case TaskType.TaskTContinueWithTaskT:
                    Task<double>[] taskDoublesC = new Task<double>[_tasks.Length];
                    for (int i = 0; i < _tasks.Length; i++)
                        taskDoublesC[i] = (Task<double>)_tasks[i];

                    if (_cancellationToken.CanBeCanceled)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll<double>(taskDoublesC, allCompletedFuncT, _cancellationToken);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny<double>(taskDoublesC, oneCompletedFuncT, _cancellationToken);
                    }
                    else if (_tm != TaskScheduler.Default)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll<double>(taskDoublesC, allCompletedFuncT, _cancellationToken, _tcOption, _tm);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny<double>(taskDoublesC, oneCompletedFuncT, _cancellationToken, _tcOption, _tm);
                    }
                    else if (_tcOption != TaskContinuationOptions.None)
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll<double>(taskDoublesC, allCompletedFuncT, _tcOption);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny<double>(taskDoublesC, oneCompletedFuncT, _tcOption);
                    }
                    else
                    {
                        if (_api == API.ContinueWhenAll)
                            _continuation = Task<bool>.Factory.ContinueWhenAll<double>(taskDoublesC, allCompletedFuncT);
                        else // must be API.ContinueWhenAny
                            _continuation = Task<bool>.Factory.ContinueWhenAny<double>(taskDoublesC, oneCompletedFuncT);
                    }

                    break;
            }

            // check continuation is non-blocking, i.e., it does not block until all/one tasks finish
            if (_continuation.Status != TaskStatus.WaitingForActivation)
                Assert.Fail(string.Format("continuation task should be created when none task finish"));

            // allow continuation to kick off later
            foreach (Task t in _tasks)
            {
                t.Start();
            }

            bool verified = true;

            // check result
            if (_continuation is Task<bool>)
            {
                verified = ((Task<bool>)_continuation).Result;
            }
            else
            {
                _continuation.Wait();
            }
        }

        #region Private Methods

        private void CreateTasks()
        {
            for (int i = 0; i < _taskInfos.Length; i++)
            {
                int iCopy = i;
                if (_taskType == TaskType.TaskContinueWithTask || _taskType == TaskType.TaskContinueWithTaskT)
                {
                    _taskInfos[i].Task = new Task(() =>
                    {
                        _taskInfos[iCopy].RunWorkload();
                    }, _taskInfos[i].CancellationTokenSource.Token);
                }
                else
                {
                    _taskInfos[i].Task = new Task<double>(() =>
                    {
                        _taskInfos[iCopy].RunWorkload();
                        return _taskInfos[iCopy].Result;
                    }, _taskInfos[i].CancellationTokenSource.Token);
                }

                _tasks[i] = _taskInfos[i].Task;
            }
        }

        // verification for ContinueWhenAll
        private void VerifyAll(Task[] inputTasks)
        {
            int firstIncompleteTaskIndex = -1;
            for (int i = 0; i < inputTasks.Length; i++)
            {
                var task = inputTasks[i];
                if (!task.IsCompleted)
                {
                    firstIncompleteTaskIndex = i;
                    break;
                }
            }

            if (firstIncompleteTaskIndex != -1)
                Assert.Fail(string.Format("ContinueWhenAll contract is broken -- Task at Index = {0} does not finish", firstIncompleteTaskIndex));

            // do the sanity check against the input tasks
            CheckSequence(_tasks, inputTasks);

            Verify();
        }

        // verification for ContinueWhenAll
        private void VerifyAllT(Task<double>[] inputTasks)
        {
            int firstIncompleteTaskIndex = -1;
            for (int i = 0; i < inputTasks.Length; i++)
            {
                var task = inputTasks[i];
                if (!task.IsCompleted)
                {
                    firstIncompleteTaskIndex = i;
                    break;
                }
            }

            if (firstIncompleteTaskIndex != -1)
                Assert.Fail(string.Format("ContinueWhenAll contract is broken -- Task at Index = {0} does not finish", firstIncompleteTaskIndex));

            // do the sanity check against the input tasks
            CheckSequence(_tasks, inputTasks);

            Verify();
        }

        // verification for ContinueWhenAny
        private void VerifyAny(Task inputTask)
        {
            if (!inputTask.IsCompleted)
                Assert.Fail(string.Format("ContinueWhenAny contract is broken -- none task has completed"));

            // do the sanity check against the input task

            bool found = false;
            for (int i = 0; i < _tasks.Length; i++)
            {
                Task task = _tasks[i];
                if (task.Equals(inputTask))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                Assert.Fail(string.Format("input task do not exist in the expected original tasks"));

            Verify();
        }

        // verification for ContinueWhenAny
        private void VerifyAnyT(Task<double> inputTask)
        {
            if (!inputTask.IsCompleted)
                Assert.Fail(string.Format("ContinueWhenAny contract is broken -- none task has completed"));

            // do the sanity check against the input task


            bool found = false;
            for (int i = 0; i < _tasks.Length; i++)
            {
                Task task = _tasks[i];
                if (task.Equals(inputTask))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                Assert.Fail(string.Format("input task do not exist in the expected original tasks"));

            Verify();
        }

        // the common verification shared by ContinueWhenAll/Any
        private void Verify()
        {
            // check against the taskCreationOptions
            TaskCreationOptions option = (TaskCreationOptions)(_tcOption & ~TaskContinuationOptions.ExecuteSynchronously);

            if (TaskContinuationOptions.LazyCancellation != _tcOption)
            {
                if (_continuation.CreationOptions != option)
                    Assert.Fail(string.Format("Wrong TaskCreationOption of {0}, expecting {1}", _continuation.CreationOptions, _tcOption));
            }
            else
            {
                if (_continuation.CreationOptions != TaskCreationOptions.None)
                    Assert.Fail(string.Format("Wrong TaskCreationOption of {0}, expecting {1}", _continuation.CreationOptions, TaskCreationOptions.None));
            }

            // check against the taskScheduler
            // @TODO: add verification for SynchronizedTM, CustomizedTM later
            if (TaskScheduler.Current != _tm)
                Assert.Fail(string.Format("Wrong TaskScheduler of {0}, expecting {1}", TaskScheduler.Current.Id, _tm.Id));

            // check for the workload results
            for (int i = 0; i < _taskInfos.Length; i++)
            {
                TaskInfo ti = _taskInfos[i];

                WorkloadType workType = ti.WorkType;

                if (workType == WorkloadType.Exceptional)
                {
                    try
                    {
                        ti.Task.Wait();

                        // should never come here
                        Assert.Fail(string.Format("excepted TPLTestException in Task at Index = {0}  NOT caught", i));
                    }
                    catch (AggregateException ex)
                    {
                        ex.Flatten().Handle((e) =>
                        {
                            TPLTestException expectedExp = e as TPLTestException;
                            return expectedExp != null && expectedExp.FromTaskId == ti.Task.Id;
                        });
                    }
                }
                else if (workType == WorkloadType.Cancelled)
                {
                    try
                    {
                        ti.Task.Wait();

                        // should never come here
                        Assert.Fail(string.Format("excepted TaskCanceledException in Task at Index = {0}  NOT caught", i));
                    }
                    catch (AggregateException ex)
                    {
                        ex.Flatten().Handle((e) =>
                        {
                            TaskCanceledException expectedExp = e as TaskCanceledException;
                            return expectedExp != null && expectedExp.Task == ti.Task;
                        });
                    }
                }
                else
                {
                    double result = (ti.Task is Task<double>) ? ((Task<double>)(ti.Task)).Result : ti.Result;
                    if (ti.Task.IsCompleted && !CheckResult(result))
                        Assert.Fail(string.Format("Failed result verification in Task at Index = {0}. Task result is {1} TaskStatus is {2}", i, result, ti.Task.Status));

                    //else if (ti.Thread == null && result != -1)
                    //{
                    //    Assert.Fail(string.Format("Result must remain uninitialized for unstarted task"));
                    //}
                }
            }
        }

        private bool CheckResult(double result)
        {
            //Function point comparison cant be done by rounding off to nearest decimal points since
            //1.64 could be represented as 1.63999999 or as 1.6499999999. To perform floating point comparisons,
            //a range has to be defined and check to ensure that the result obtained is within the specified range
            double minLimit = 1.63;
            double maxLimit = 1.65;

            return result > minLimit && result < maxLimit;
        }

        private void CheckSequence(Task[] expected, Task[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        #endregion
    }

    public class TestParameters
    {
        public readonly TaskInfo[] AllTaskInfos;

        public readonly API Api;

        public readonly TaskType TaskType;

        public readonly TaskContinuationOptions ContinuationOptions;

        public readonly bool WithCancellation;

        public TestParameters(API api, TaskType taskType, TaskContinuationOptions continuationOptions, bool withCancellation, TaskInfo[] allTasks)
        {
            Api = api;
            TaskType = taskType;
            ContinuationOptions = continuationOptions;
            WithCancellation = withCancellation;
            AllTaskInfos = allTasks;
        }
    }

    /// <summary>
    /// The Tree node Data type
    ///
    /// While the tree is not restricted to this data type
    /// the implemented tests are using the TaskInfo_ContinueWithAllAny data type for their scenarios
    /// </summary>
    public class TaskInfo
    {
        private static double s_UNINITIALED_RESULT = -1;

        public TaskInfo(WorkloadType workType)
        {
            WorkType = workType;

            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            Result = s_UNINITIALED_RESULT;
        }

        #region Properties

        /// <summary>
        /// The task associated with the current node
        /// </summary>
        public Task Task { get; set; }

        /// <summary>
        /// Current node Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// WorkloadType_ContinueWithAllAny of task associated with the current node
        /// </summary>
        public WorkloadType WorkType { get; private set; }

        /// <summary>
        /// The token associated with the current node's task
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Every node has a cancellation source - its token participate in the task creation
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// While a tasks is correct execute a result is produced
        /// this is the result
        /// </summary>
        public double Result { get; private set; }

        #endregion

        #region Helper Methods

        /// <summary>
        /// The Task workload execution
        /// </summary>
        public void RunWorkload()
        {
            //Thread = Thread.CurrentThread;

            if (WorkType == WorkloadType.Exceptional)
            {
                ThrowException();
            }
            else if (WorkType == WorkloadType.Cancelled)
            {
                CancelSelf(this.CancellationTokenSource, this.CancellationToken);
            }
            else
            {
                // run the workload
                if (Result == s_UNINITIALED_RESULT)
                {
                    Result = ZetaSequence((int)WorkType);
                }
                else  // task re-entry, mark it failed
                {
                    Result = s_UNINITIALED_RESULT;
                }
            }
        }

        public static double ZetaSequence(int n)
        {
            double result = 0;
            for (int i = 1; i < n; i++)
            {
                result += 1.0 / ((double)i * (double)i);
            }

            return result;
        }

        /// <summary>
        /// Cancel self workload. The CancellationToken has been wired such that the source passed to this method
        /// is a source that can actually causes the Task CancellationToken to be canceled. The source could be the
        /// Token's original source, or one of the sources in case of Linked Tokens
        /// </summary>
        /// <param name="cts"></param>
        public static void CancelSelf(CancellationTokenSource cts, CancellationToken ct)
        {
            cts.Cancel();
            throw new OperationCanceledException(ct);
        }

        public static void ThrowException()
        {
            throw new TPLTestException();
        }

        #endregion
    }

    public enum API
    {
        ContinueWhenAll,
        ContinueWhenAny,
    }

    /// <summary>
    /// Every task has an workload associated
    /// These are the workload types used in the task tree
    /// The workload is not common for the whole tree - Every node can have its own workload
    /// </summary>
    public enum WorkloadType
    {
        Exceptional = -2,
        Cancelled = -1,
        VeryLight = 100,     // the number is the N input to the ZetaSequence workload
        Light = 200,
        Medium = 400,
        Heavy = 800,
        VeryHeavy = 1600,
    }

    /// <summary>
    /// TaskType argument tells us what the type of all the antecedent tasks is, and what the type of the continuation task is.
    /// The TaskTypeOption that ends with "NEW" additionally specifies that the ContinueWhen overload to be used is one that
    /// has two type arguments (TAntecedentResult, TResult).
    /// </summary>
    public enum TaskType
    {
        TaskContinueWithTask,          // test Task.Factory.ContinueWhenAll/Any(..., Action,...)
        TaskContinueWithTaskT,         // test Task<T>.Factory.ContinueWhenAll/Any(..., Func<>,...)
        TaskContinueWithTaskT_NEW,     // test new overload of Task.Factory.ContinueWhenAll/Any<TNEW>(..., Func<>,...)
        TaskTContinueWithTask,         // test Task.Factory.Factory.ContinueWhenAll/Any<T>(..., Action,...)
        TaskTContinueWithTaskT,        // test Task<T>.Factory.Factory.ContinueWhenAll/Any<T>(..., Func<>,...)
        TaskTContinueWithTaskT_NEW,    // test new overload of Task.Factory.Factory.ContinueWhenAll/Any<T1, T2>(..., Func<>,...)
    }

    #endregion

    public sealed class TaskContinueWithAllAnyTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest0()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTaskT, TaskContinuationOptions.ExecuteSynchronously, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest1()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node3 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTaskT, TaskContinuationOptions.None, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest2()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Light);
            TaskInfo node2 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node3 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node4 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTaskT, TaskContinuationOptions.PreferFairness, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest3()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTaskT, TaskContinuationOptions.AttachedToParent, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest4()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTaskT, TaskContinuationOptions.LongRunning, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest5()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTaskT_NEW, TaskContinuationOptions.AttachedToParent, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest6()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTaskT_NEW, TaskContinuationOptions.ExecuteSynchronously, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest7()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node3 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node4 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTaskT_NEW, TaskContinuationOptions.None, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest8()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTaskT_NEW, TaskContinuationOptions.LongRunning, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest9()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTask, TaskContinuationOptions.PreferFairness, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest10()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTask, TaskContinuationOptions.AttachedToParent, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest11()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node4 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTask, TaskContinuationOptions.PreferFairness, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest12()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTask, TaskContinuationOptions.ExecuteSynchronously, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest13()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node3 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTask, TaskContinuationOptions.LongRunning, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest14()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Light);
            TaskInfo node2 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node3 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node4 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTask, TaskContinuationOptions.None, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest15()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT, TaskContinuationOptions.AttachedToParent, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest16()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT, TaskContinuationOptions.ExecuteSynchronously, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest17()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT, TaskContinuationOptions.LongRunning, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest18()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTaskT, TaskContinuationOptions.None, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest19()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTaskT, TaskContinuationOptions.PreferFairness, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest20()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.ExecuteSynchronously, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest21()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.LongRunning, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest22()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.None, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest23()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node3 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.PreferFairness, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest24()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.AttachedToParent, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest25()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTask, TaskContinuationOptions.AttachedToParent, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest26()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node7 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node8 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node9 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node10 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, node8, node9, node10, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTask, TaskContinuationOptions.LongRunning, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest27()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node3 = new TaskInfo(WorkloadType.Light);
            TaskInfo node4 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node6 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTask, TaskContinuationOptions.None, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest28()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node7 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node8 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node9 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node10 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, node8, node9, node10, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTask, TaskContinuationOptions.ExecuteSynchronously, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest29()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node7 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node8 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node9 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node10 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, node8, node9, node10, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTask, TaskContinuationOptions.PreferFairness, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest30()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node4 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node5 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node7 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node8 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node9 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node10 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, node8, node9, node10, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTaskT_NEW, TaskContinuationOptions.None, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest31()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTaskT_NEW, TaskContinuationOptions.None, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest32()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.None, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest33()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.None, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest34()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.LazyCancellation, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest35()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskContinueWithTask, TaskContinuationOptions.LazyCancellation, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest36()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskContinueWithTaskT, TaskContinuationOptions.LazyCancellation, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskContinueWithAllAnyTest37()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Exceptional);
            TaskInfo node2 = new TaskInfo(WorkloadType.Cancelled);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTask, TaskContinuationOptions.LazyCancellation, false, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest38()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAny, TaskType.TaskTContinueWithTaskT, TaskContinuationOptions.LazyCancellation, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskContinueWithAllAnyTest39()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters parameters = new TestParameters(API.ContinueWhenAll, TaskType.TaskTContinueWithTaskT_NEW, TaskContinuationOptions.LazyCancellation, true, allTasks);

            TaskContinueWithAllAnyTest test = new TaskContinueWithAllAnyTest(parameters);
            test.RealRun();
        }
    }
}
