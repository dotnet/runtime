// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Summary: Test suite for the below scenario:
// An array of tasks that can have different workloads
// WaitAny and WaitAll
//      - with/without timeout is performed on them
//      - the allowed delta for cases with timeout:10 ms
// Scheduler used : current ( ThreadPool)
//
// Observations:
// 1. The input data for tasks does not contain any Exceptional or Cancelled tasks.
// 2. WaitAll on array with cancelled tasks can be found at: Functional\TPL\YetiTests\TaskWithYeti\TaskWithYeti.cs
// 3. WaitAny/WaitAll with token tests can be found at:Functional\TPL\YetiTests\TaskCancellation\TaskCancellation.cs

using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests.WaitAllAny
{
    #region Helper Classes / Methods

    /// <summary>
    /// the waiting type
    /// </summary>
    public enum API
    {
        WaitAll,
        WaitAny,
    }

    /// <summary>
    /// Waiting type
    /// </summary>
    public enum WaitBy
    {
        None,
        TimeSpan,
        Millisecond,
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

    public class TestParameters_WaitAllAny
    {
        public readonly TaskInfo[] AllTaskInfos;

        public readonly API Api;

        public readonly WaitBy WaitBy;

        public readonly int WaitTime;

        public TestParameters_WaitAllAny(API api, int waitTime, WaitBy waitBy, TaskInfo[] allTasks)
        {
            Api = api;
            WaitBy = waitBy;
            WaitTime = waitTime;
            AllTaskInfos = allTasks;
        }
    }

    /// <summary>
    /// The Tree node Data type
    ///
    /// While the tree is not restricted to this data type
    /// the implemented tests are using the TaskInfo_WaitAllAny data type for their scenarios
    /// </summary>
    public class TaskInfo
    {
        private static double s_UNINITIALED_RESULT = -1;

        public TaskInfo(WorkloadType workType)
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            Result = s_UNINITIALED_RESULT;
            WorkType = workType;
        }

        #region Properties

        /// <summary>
        /// The task associated with the current node
        /// </summary>
        public Task Task { get; set; }

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

        public WorkloadType WorkType { get; private set; }

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
                CancelSelf(CancellationTokenSource, CancellationToken);
            }
            else
            {
                // run the workload
                if (Result == s_UNINITIALED_RESULT)
                {
                    Result = ZetaSequence((int)WorkType, CancellationToken);
                }
                else  // task re-entry, mark it failed
                {
                    Result = s_UNINITIALED_RESULT;
                }
            }
        }

        public static double ZetaSequence(int n, CancellationToken token)
        {
            double result = 0;
            for (int i = 1; i < n; i++)
            {
                if (token.IsCancellationRequested)
                    return s_UNINITIALED_RESULT;
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

    public sealed class TaskWaitAllAnyTest
    {
        #region Private Fields

        private const int MAX_DELAY_TIMEOUT = 10;

        private readonly API _api;                  // the API_WaitAllAny to be tested
        private readonly WaitBy _waitBy;            // the format of Wait
        private readonly int _waitTimeout;          // the timeout in ms to be waited
        private readonly TaskInfo[] _taskInfos;     // task info for each task
        private readonly Task[] _tasks;             // _tasks to be waited

        private bool _taskWaitAllReturn;            // result to record the WaitAll(timeout) return value
        private int _taskWaitAnyReturn;             // result to record the WaitAny(timeout) return value

        private AggregateException _caughtException; // exception thrown during wait

        #endregion

        public TaskWaitAllAnyTest(TestParameters_WaitAllAny parameters)
        {
            _api = parameters.Api;
            _waitBy = parameters.WaitBy;
            _waitTimeout = parameters.WaitTime;

            _taskInfos = parameters.AllTaskInfos;
            _tasks = new Task[parameters.AllTaskInfos.Length];
        }

        /// <summary>
        /// The method that will run the scenario
        /// </summary>
        /// <returns></returns>
        internal void RealRun()
        {
            //create the tasks
            CreateTask();
            Stopwatch sw = Stopwatch.StartNew();
            //start the wait in a try/catch
            try
            {
                switch (_api)
                {
                    case API.WaitAll:
                        switch (_waitBy)
                        {
                            case WaitBy.None:
                                Task.WaitAll(_tasks);
                                _taskWaitAllReturn = true;
                                break;
                            case WaitBy.Millisecond:
                                _taskWaitAllReturn = Task.WaitAll(_tasks, _waitTimeout);
                                break;
                            case WaitBy.TimeSpan:
                                _taskWaitAllReturn = Task.WaitAll(_tasks, new TimeSpan(0, 0, 0, 0, _waitTimeout));
                                break;
                        }
                        break;
                    case API.WaitAny:
                        switch (_waitBy)
                        {
                            case WaitBy.None:
                                //save the returned task index
                                _taskWaitAnyReturn = Task.WaitAny(_tasks);
                                break;
                            case WaitBy.Millisecond:
                                //save the returned task index
                                _taskWaitAnyReturn = Task.WaitAny(_tasks, _waitTimeout);
                                break;
                            case WaitBy.TimeSpan:
                                //save the returned task index
                                _taskWaitAnyReturn = Task.WaitAny(_tasks, new TimeSpan(0, 0, 0, 0, _waitTimeout));
                                break;
                        }
                        break;
                }
            }
            catch (AggregateException exp)
            {
                _caughtException = exp;
            }
            finally
            {
                sw.Stop();
            }

            long maxTimeout = (long)(_waitTimeout + MAX_DELAY_TIMEOUT);
            if (_waitTimeout != -1 && sw.ElapsedMilliseconds > maxTimeout)
            {
                Debug.WriteLine("ElapsedMilliseconds way more than requested Timeout.");
                Debug.WriteLine("Max Timeout: {0}ms + {1}ms, Actual Time: {2}ms",
                    _waitTimeout, MAX_DELAY_TIMEOUT, sw.ElapsedMilliseconds);
            }

            Verify();

            CleanUpTasks();
        }

        private void CleanUpTasks()
        {
            foreach (TaskInfo taskInfo in _taskInfos)
            {
                if (taskInfo.Task.Status == TaskStatus.Running)
                {
                    taskInfo.CancellationTokenSource.Cancel();
                    try { taskInfo.Task.GetAwaiter().GetResult(); }
                    catch (OperationCanceledException) { }
                }
            }
        }

        /// <summary>
        /// create an array of tasks
        /// </summary>
        private void CreateTask()
        {
            for (int i = 0; i < _taskInfos.Length; i++)
            {
                int iCopy = i;
                _taskInfos[i].Task = Task.Factory.StartNew(
                   delegate (object o)
                   {
                       _taskInfos[iCopy].RunWorkload();
                   }, string.Concat("Task_", iCopy), _taskInfos[iCopy].CancellationTokenSource.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);

                _tasks[i] = _taskInfos[i].Task;
            }
        }

        /// <summary>
        /// the scenario verification
        ///
        /// - all the tasks should be completed in case of waitAll with -1 timeout
        /// - the returned index form WaitAny should correspond to a completed task
        /// - in case of Cancelled  and Exception tests the right exceptions should be got for WaitAll
        /// </summary>
        /// <returns></returns>
        private void Verify()
        {
            // verification for WaitAll
            bool allShouldFinish = (_api == API.WaitAll && _taskWaitAllReturn);

            // verification for WaitAny
            Task thisShouldFinish = (_api == API.WaitAny && _taskWaitAnyReturn != -1) ? _taskInfos[_taskWaitAnyReturn].Task : null;

            Dictionary<int, Task> faultyTasks = new Dictionary<int, Task>();
            bool expCaught = false;

            for (int i = 0; i < _taskInfos.Length; i++)
            {
                TaskInfo ti = _taskInfos[i];

                if (allShouldFinish && !ti.Task.IsCompleted)
                    Assert.Fail(string.Format("WaitAll contract is broken -- Task at Index = {0} does not finish", i));

                if (thisShouldFinish == ti.Task && !ti.Task.IsCompleted)
                    Assert.Fail(string.Format("WaitAny contract is broken -- Task at Index = {0} does not finish", i));

                WorkloadType workType = ti.WorkType;

                if (workType == WorkloadType.Exceptional)
                {
                    // verify whether exception has(not) been propagated
                    expCaught = VerifyException((ex) =>
                    {
                        TPLTestException expectedExp = ex as TPLTestException;
                        return expectedExp != null && expectedExp.FromTaskId == ti.Task.Id;
                    });

                    if (_api == API.WaitAll)
                    {
                        if (!expCaught)
                            Assert.Fail(string.Format("excepted TPLTestException in Task at Index = {0}  NOT caught", i));
                    }
                    else // must be API_WaitAllAny.WaitAny
                    {
                        //waitAny will not fail if a number of tasks were exceptional
                        if (expCaught)
                            Assert.Fail(string.Format("Unexpected TPLTestException in Task at Index = {0} caught", i));

                        //need to check it eventually to prevent it from crashing the finalizer
                        faultyTasks.Add(i, ti.Task);
                    }
                }
                else if (workType == WorkloadType.Cancelled)
                {
                    expCaught = VerifyException((ex) =>
                    {
                        TaskCanceledException expectedExp = ex as TaskCanceledException;
                        return expectedExp != null && expectedExp.Task == ti.Task;
                    });

                    if (_api == API.WaitAll)
                        Assert.True(expCaught, "excepted TaskCanceledException in Task at Index = " + i + " NOT caught");
                    else // must be API_WaitAllAny.WaitAny
                    {
                        if (expCaught) //waitAny will not fail if a number of tasks were cancelled
                            Assert.False(expCaught, "Unexpected TaskCanceledException in Task at Index = " + i + " caught");
                    }
                }
                else if (ti.Task.IsCompleted && !CheckResult(ti.Result))
                    Assert.Fail(string.Format("Failed result verification in Task at Index = {0}", i));
            }

            if (!expCaught && _caughtException != null)
                Assert.Fail(string.Format("Caught unexpected exception of {0}", _caughtException));

            // second pass on the faulty tasks
            if (faultyTasks.Count > 0)
            {
                List<WaitHandle> faultyTaskHandles = new List<WaitHandle>();
                foreach (var task in faultyTasks.Values)
                    faultyTaskHandles.Add(((IAsyncResult)task).AsyncWaitHandle);

                WaitHandle.WaitAll(faultyTaskHandles.ToArray());

                foreach (var tasks in faultyTasks)
                {
                    if (!(tasks.Value.Exception.InnerException is TPLTestException))
                        Assert.Fail(string.Format("Unexpected Exception in Task at Index = {0} caught", tasks.Key));
                }
            }
        }

        /// <summary>
        /// verify the exception using a custom defined predicate
        /// It walks the whole inner exceptions list saved in _caughtException
        /// </summary>
        private bool VerifyException(Predicate<Exception> isExpectedExp)
        {
            if (_caughtException == null)
            {
                return false;
            }

            foreach (Exception ex in _caughtException.InnerExceptions)
            {
                if (isExpectedExp(ex)) return true;
            }

            return false;
        }

        /// <summary>
        /// Verify the result for a correct running task
        /// </summary>
        private bool CheckResult(double result)
        {
            //Function point comparison cant be done by rounding off to nearest decimal points since
            //1.64 could be represented as 1.63999999 or as 1.6499999999. To perform floating point comparisons,
            //a range has to be defined and check to ensure that the result obtained is within the specified range
            double minLimit = 1.63;
            double maxLimit = 1.65;

            return result > minLimit && result < maxLimit;
        }
    }

    #endregion

    public sealed class TaskWaitAllAny
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny0()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny1()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny2()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny3()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny4()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny5()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny6()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny7()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, -1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny8()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 0, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny9()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 0, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny10()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 0, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny11()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.Light);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 0, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny12()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 197, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny13()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 197, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny14()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 197, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny15()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 197, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny16()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 197, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny17()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny18()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny19()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny20()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny21()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 47, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny22()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 47, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny23()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 47, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny24()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 47, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny25()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 47, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny26()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 7, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny27()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 7, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny28()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 7, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny29()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.Light);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAll, 7, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny30()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny31()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node8 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node9 = new TaskInfo(WorkloadType.Light);
            TaskInfo node10 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node11 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node12 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node13 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node14 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node15 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node16 = new TaskInfo(WorkloadType.Light);
            TaskInfo node17 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node18 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node19 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node20 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node21 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node22 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node23 = new TaskInfo(WorkloadType.Light);
            TaskInfo node24 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node25 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node26 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node27 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node28 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node29 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node30 = new TaskInfo(WorkloadType.Light);
            TaskInfo node31 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node32 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node33 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node34 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node35 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node36 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node37 = new TaskInfo(WorkloadType.Light);
            TaskInfo node38 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node39 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node40 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node41 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node42 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node43 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node44 = new TaskInfo(WorkloadType.Light);
            TaskInfo node45 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node46 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node47 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node48 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node49 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node50 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node51 = new TaskInfo(WorkloadType.Light);
            TaskInfo node52 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node53 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node54 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node55 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node56 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node57 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node58 = new TaskInfo(WorkloadType.Light);
            TaskInfo node59 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node60 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node61 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node62 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node63 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node64 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node65 = new TaskInfo(WorkloadType.Light);
            TaskInfo node66 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, node8, node9, node10, node11, node12, node13, node14, node15, node16, node17, node18, node19, node20, node21, node22, node23, node24, node25, node26, node27, node28, node29, node30, node31, node32, node33, node34, node35, node36, node37, node38, node39, node40, node41, node42, node43, node44, node45, node46, node47, node48, node49, node50, node51, node52, node53, node54, node55, node56, node57, node58, node59, node60, node61, node62, node63, node64, node65, node66, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny32()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny33()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny34()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny35()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny36()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.Light);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.None, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny37()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, -1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny38()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 0, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny39()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node3 = new TaskInfo(WorkloadType.Light);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 0, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny40()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 0, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny41()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 0, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny42()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 197, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny43()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 197, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny44()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 197, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void TaskWaitAllAny45()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node8 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node9 = new TaskInfo(WorkloadType.Light);
            TaskInfo node10 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node11 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node12 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node13 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node14 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node15 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node16 = new TaskInfo(WorkloadType.Light);
            TaskInfo node17 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node18 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node19 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node20 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node21 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node22 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node23 = new TaskInfo(WorkloadType.Light);
            TaskInfo node24 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node25 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node26 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node27 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node28 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node29 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node30 = new TaskInfo(WorkloadType.Light);
            TaskInfo node31 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node32 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node33 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node34 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node35 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node36 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node37 = new TaskInfo(WorkloadType.Light);
            TaskInfo node38 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node39 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node40 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node41 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node42 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node43 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node44 = new TaskInfo(WorkloadType.Light);
            TaskInfo node45 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node46 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node47 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node48 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node49 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node50 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node51 = new TaskInfo(WorkloadType.Light);
            TaskInfo node52 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node53 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node54 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node55 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node56 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node57 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node58 = new TaskInfo(WorkloadType.Light);
            TaskInfo node59 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node60 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node61 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node62 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node63 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node64 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node65 = new TaskInfo(WorkloadType.Light);
            TaskInfo node66 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node67 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node68 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node69 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, node8, node9, node10, node11, node12, node13, node14, node15, node16, node17, node18, node19, node20, node21, node22, node23, node24, node25, node26, node27, node28, node29, node30, node31, node32, node33, node34, node35, node36, node37, node38, node39, node40, node41, node42, node43, node44, node45, node46, node47, node48, node49, node50, node51, node52, node53, node54, node55, node56, node57, node58, node59, node60, node61, node62, node63, node64, node65, node66, node67, node68, node69, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 197, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny46()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny47()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny48()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 1, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny49()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 1, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny50()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 47, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny51()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Light);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 47, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny52()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 47, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny53()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 47, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny54()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.VeryHeavy);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 47, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny55()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 7, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny56()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo[] allTasks = new[] { node1, node2, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 7, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAllAny57()
        {
            TaskInfo node1 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node2 = new TaskInfo(WorkloadType.Light);
            TaskInfo node3 = new TaskInfo(WorkloadType.Heavy);
            TaskInfo node4 = new TaskInfo(WorkloadType.Medium);
            TaskInfo node5 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node6 = new TaskInfo(WorkloadType.VeryLight);
            TaskInfo node7 = new TaskInfo(WorkloadType.Medium);
            TaskInfo[] allTasks = new[] { node1, node2, node3, node4, node5, node6, node7, };
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 7, WaitBy.Millisecond, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskWaitAllAny58()
        {
            TaskInfo[] allTasks = new TaskInfo[0];
            TestParameters_WaitAllAny parameters = new TestParameters_WaitAllAny(API.WaitAny, 7, WaitBy.TimeSpan, allTasks);
            TaskWaitAllAnyTest test = new TaskWaitAllAnyTest(parameters);
            test.RealRun();
        }
    }
}
