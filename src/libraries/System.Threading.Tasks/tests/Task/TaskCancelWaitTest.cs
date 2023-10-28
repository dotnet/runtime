// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Summary:
// Implements the exhaustive task cancel and wait scenarios.

using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;   // for Stopwatch
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace System.Threading.Tasks.Tests.CancelWait
{
    /// <summary>
    /// Test class
    /// </summary>
    public sealed class TaskCancelWaitTest
    {
        #region Private Fields

        private API _api;                               // the API_CancelWait to be tested
        private WaitBy _waitBy;                         // the format of Wait
        private int _waitTimeout;                       // the timeout in ms to be waited

        private TaskInfo _taskTree;                     // the _taskTree to track child task cancellation option

        private static readonly int s_deltaTimeOut = 10;

        private bool _taskCompleted;                    // result to record the Wait(timeout) return value
        private AggregateException _caughtException;    // exception thrown during wait
        private CountdownEvent _countdownEvent;         // event to signal the main thread that the whole task tree has been created

        #endregion

        /// <summary>
        /// .ctor
        /// </summary>
        public TaskCancelWaitTest(TestParameters parameters)
        {
            _api = parameters.API_CancelWait;
            _waitBy = parameters.WaitBy_CancelWait;
            _waitTimeout = parameters.WaitTime;
            _taskTree = parameters.RootNode;
            _countdownEvent = new CountdownEvent(CaluateLeafNodes(_taskTree));
        }

        #region Helper Methods

        /// <summary>
        /// The method that performs the tests
        /// Depending on the inputs different test code paths will be exercised
        /// </summary>
        internal void RealRun()
        {
            TaskScheduler tm = TaskScheduler.Default;

            CreateTask(tm, _taskTree);
            // wait the whole task tree to be created
            _countdownEvent.Wait();

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                switch (_api)
                {
                    case API.Cancel:
                        _taskTree.CancellationTokenSource.Cancel();
                        _taskTree.Task.Wait();
                        break;

                    case API.Wait:
                        switch (_waitBy)
                        {
                            case WaitBy.None:
                                _taskTree.Task.Wait();
                                _taskCompleted = true;
                                break;
                            case WaitBy.Millisecond:
                                _taskCompleted = _taskTree.Task.Wait(_waitTimeout);
                                break;
                            case WaitBy.TimeSpan:
                                _taskCompleted = _taskTree.Task.Wait(new TimeSpan(0, 0, 0, 0, _waitTimeout));
                                break;
                        }
                        break;
                }
            }
            catch (AggregateException exp)
            {
                _caughtException = exp.Flatten();
            }
            finally
            {
                sw.Stop();
            }

            if (_waitTimeout != -1)
            {
                long delta = sw.ElapsedMilliseconds - ((long)_waitTimeout + s_deltaTimeOut);

                if (delta > 0)
                {
                    Debug.WriteLine("ElapsedMilliseconds way more than requested Timeout.");
                    Debug.WriteLine("WaitTime= {0} ms, ElapsedTime= {1} ms, Allowed Discrepancy = {2} ms", _waitTimeout, sw.ElapsedMilliseconds, s_deltaTimeOut);
                    Debug.WriteLine("Delta= {0} ms", delta);
                }
                else
                {
                    var delaytask = Task.Delay((int)Math.Abs(delta));  // give delay to allow Context being collected before verification
                    delaytask.Wait();
                }
            }

            Verify();
            _countdownEvent.Dispose();
        }

        /// <summary>
        /// recursively walk the tree and attach the tasks to the nodes
        /// </summary>
        private void CreateTask(TaskScheduler tm, TaskInfo treeNode)
        {
            treeNode.Task = Task.Factory.StartNew(
                delegate (object o)
                {
                    TaskInfo current = (TaskInfo)o;

                    if (current.IsLeaf)
                    {
                        lock (_countdownEvent)
                        {
                            if (!_countdownEvent.IsSet)
                                _countdownEvent.Signal();
                        }
                    }
                    else
                    {
                        // create children tasks
                        foreach (TaskInfo child in current.Children)
                        {
                            if (child.IsRespectParentCancellation)
                            {
                                //
                                // if child to respect parent cancellation we need to wire a linked token
                                //
                                child.CancellationToken =
                                    CancellationTokenSource.CreateLinkedTokenSource(treeNode.CancellationToken, child.CancellationToken).Token;
                            }
                            CreateTask(tm, child);
                        }
                    }

                    if (current.CancelChildren)
                    {
                        try
                        {
                            foreach (TaskInfo child in current.Children)
                            {
                                child.CancellationTokenSource.Cancel();
                            }
                        }
                        finally
                        {
                            lock (_countdownEvent)
                            {
                                // stop the tree creation and let the main thread proceed
                                if (!_countdownEvent.IsSet)
                                {
                                    _countdownEvent.Signal(_countdownEvent.CurrentCount);
                                }
                            }
                        }
                    }

                    // run the workload
                    current.RunWorkload();
                }, treeNode, treeNode.CancellationToken, treeNode.Option, tm);
        }

        /// <summary>
        /// Walk the tree and calculates the tree nodes count
        /// </summary>
        /// <param name="tree"></param>
        private int CaluateLeafNodes(TaskInfo tree)
        {
            if (tree.IsLeaf)
                return 1;

            int sum = 0;
            foreach (TaskInfo child in tree.Children)
                sum += CaluateLeafNodes(child);

            return sum;
        }

        /// <summary>
        /// Verification method
        /// </summary>
        private void Verify()
        {
            switch (_api)
            {
                //root task had the token source cancelled
                case API.Cancel:
                    _taskTree.Traversal(current =>
                    {
                        if (current.Task == null)
                            return;

                        VerifyCancel(current);
                        VerifyResult(current);
                    });
                    Assert.Null(_caughtException);
                    break;

                //root task was calling wait
                case API.Wait:
                    //will be true if the root cancelled itself - through its workload
                    if (_taskTree.CancellationToken.IsCancellationRequested)
                    {
                        _taskTree.Traversal(current =>
                        {
                            if (current.Task == null)
                                return;

                            VerifyTaskCanceledException(current);
                        });
                    }
                    else
                    {
                        _taskTree.Traversal(current =>
                        {
                            if (current.Task == null)
                                return;

                            VerifyWait(current);
                            VerifyResult(current);
                        });
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("unknown API_CancelWait of {0}", _api));
            }
        }

        /// <summary>
        /// Cancel Verification
        /// </summary>
        private void VerifyCancel(TaskInfo current)
        {
            TaskInfo ti = current;

            if (current.Parent == null)
            {
                if (!ti.CancellationToken.IsCancellationRequested)
                    Assert.Fail(string.Format("Root task must be cancel-requested"));
                else if (_countdownEvent.IsSet && ti.Task.IsCanceled)
                    Assert.Fail(string.Format("Root task should not be cancelled when the whole tree has been created"));
            }
            else if (current.Parent.CancelChildren)
            {
                // need to make sure the parent task at least called .Cancel() on the child
                if (!ti.CancellationToken.IsCancellationRequested)
                    Assert.Fail(string.Format("Task which has been explicitly cancel-requested either by parent must have CancellationRequested set as true"));
            }
            else if (ti.IsRespectParentCancellation)
            {
                if (ti.CancellationToken.IsCancellationRequested != current.Parent.CancellationToken.IsCancellationRequested)
                    Assert.Fail(string.Format("Task with RespectParentCancellationcontract is broken"));
            }
            else
            {
                if (ti.CancellationToken.IsCancellationRequested || ti.Task.IsCanceled)
                    Assert.Fail(string.Format("Inner non-directly canceled task which opts out RespectParentCancellationshould not be cancelled"));
            }

            // verify IsCanceled indicate successfully dequeued based on the observing that
            // - Thread is recorded the first thing in the RunWorkload from user delegate
            //if (ti.Task.IsCompleted && (ti.Thread == null) != ti.Task.IsCanceled)
            //    Assert.Fail("IsCanceled contract is broken -- completed task which has the delegate executed can't have IsCanceled return true")
        }

        /// <summary>
        /// Verify the Wait code path
        /// </summary>
        private void VerifyWait(TaskInfo current)
        {
            TaskInfo ti = current;
            TaskInfo parent = current.Parent;
            if (_taskCompleted)
            {
                if (parent == null)
                {
                    Assert.True(ti.Task.IsCompleted, "Root task must complete");
                }
                else if (parent != null && parent.Task.IsCompleted)
                {
                    if ((ti.Option & TaskCreationOptions.AttachedToParent) != 0
                        && !ti.Task.IsCompleted)
                    {
                        Assert.Fail(string.Format("Inner attached task must complete"));
                    }
                }
            }
        }

        private void VerifyTaskCanceledException(TaskInfo current)
        {
            bool expCaught;

            TaskInfo ti = current;
            //a task will get into cancelled state only if:
            //1.Its token was cancelled before as its action to get invoked
            //2.The token was cancelled before the task's action to finish, task observed the cancelled token and threw OCE(token)
            if (ti.Task.Status == TaskStatus.Canceled)
            {
                expCaught = FindException((ex) =>
                {
                    TaskCanceledException expectedExp = ex as TaskCanceledException;
                    return expectedExp != null && expectedExp.Task == ti.Task;
                });

                Assert.True(expCaught, "expected TaskCanceledException in Task.Name = Task " + current.Name + " NOT caught");
            }
            else
            {
                expCaught = FindException((ex) =>
                {
                    TaskCanceledException expectedExp = ex as TaskCanceledException;
                    return expectedExp != null && expectedExp.Task == ti.Task;
                });

                Assert.False(expCaught, "NON-expected TaskCanceledException in Task.Name = Task " + current.Name + " caught");
            }
        }

        private void VerifyResult(TaskInfo current)
        {
            TaskInfo ti = current;
            WorkloadType workType = ti.WorkType;

            if (workType == WorkloadType.Exceptional && _api != API.Cancel)
            {
                bool expCaught = FindException((ex) =>
                {
                    TPLTestException expectedExp = ex as TPLTestException;
                    return expectedExp != null && expectedExp.FromTaskId == ti.Task.Id;
                });

                if (!expCaught)
                    Assert.Fail(string.Format("expected TPLTestException in Task.Name = Task{0} NOT caught", current.Name));
            }
            else
            {
                if (ti.Task.Exception != null && _api == API.Wait)
                    Assert.Fail(string.Format("UNEXPECTED exception in Task.Name = Task{0} caught. Exception: {1}", current.Name, ti.Task.Exception));


                if (ti.Task.IsCanceled && ti.Result != -42)
                {
                    //this means that the task was not scheduled - it was cancelled or it is still in the queue
                    //-42 = UNINITIALED_RESULT
                    Assert.Fail(string.Format("Result must remain uninitialized for unstarted task"));
                }
                else if (ti.Task.IsCompleted)
                {
                    //Function point comparison cant be done by rounding off to nearest decimal points since
                    //1.64 could be represented as 1.63999999 or as 1.6499999999. To perform floating point comparisons,
                    //a range has to be defined and check to ensure that the result obtained is within the specified range
                    double minLimit = 1.63;
                    double maxLimit = 1.65;

                    if (ti.Result < minLimit || ti.Result > maxLimit)
                        Assert.True(ti.Task.IsCanceled || ti.Task.IsFaulted,
                            string.Format(
                                "Expected Result to lie between {0} and {1} for completed task. Actual Result {2}. Using n={3} IsCanceled={4}",
                                minLimit,
                                maxLimit,
                                ti.Result,
                                workType,
                                ti.Task.IsCanceled));
                }
            }
        }

        /// <summary>
        /// Verify the _caughtException against a custom predicate
        /// </summary>
        private bool FindException(Predicate<Exception> exceptionPred)
        {
            if (_caughtException == null)
                return false;  // not caught any exceptions

            foreach (Exception ex in _caughtException.InnerExceptions)
            {
                if (exceptionPred(ex))
                    return true;
            }

            return false;
        }

        #endregion
    }

    #region Helper Classes / Enums

    public class TestParameters
    {
        public readonly int WaitTime;

        public readonly WaitBy WaitBy_CancelWait;

        public readonly TaskInfo RootNode;

        public readonly API API_CancelWait;

        public TestParameters(TaskInfo rootNode, API api_CancelWait, WaitBy waitBy_CancelWait, int waitTime)
        {
            WaitBy_CancelWait = waitBy_CancelWait;
            WaitTime = waitTime;
            RootNode = rootNode;
            API_CancelWait = api_CancelWait;
        }
    }

    /// <summary>
    /// The Tree node Data type
    ///
    /// While the tree is not restricted to this data type
    /// the implemented tests are using the TaskInfo_CancelWait data type for their scenarios
    /// </summary>
    public class TaskInfo
    {
        private static TaskCreationOptions s_DEFAULT_OPTION = TaskCreationOptions.AttachedToParent;
        private static double s_UNINITIALED_RESULT = -42;

        public TaskInfo(TaskInfo parent, string TaskInfo_CancelWaitName, WorkloadType workType, string optionsString)
        {
            Children = new LinkedList<TaskInfo>();
            Result = s_UNINITIALED_RESULT;
            Option = s_DEFAULT_OPTION;
            Name = TaskInfo_CancelWaitName;

            WorkType = workType;
            Parent = parent;
            CancelChildren = false;

            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            if (string.IsNullOrEmpty(optionsString))
                return;

            //
            // Parse Task CreationOptions, if RespectParentCancellation we would want to acknowledge that
            // and passed the remaining options for creation
            //
            string[] options = optionsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            int index = -1;
            for (int i = 0; i < options.Length; i++)
            {
                string o = options[i].Trim(); // remove any whitespace.
                options[i] = o;
                if (o.Equals("RespectParentCancellation", StringComparison.OrdinalIgnoreCase))
                {
                    IsRespectParentCancellation = true;
                    index = i;
                }
            }

            if (index != -1)
            {
                string[] temp = new string[options.Length - 1];
                int excludeIndex = index + 1;
                Array.Copy(options, temp, index);
                int leftToCopy = options.Length - excludeIndex;
                Array.Copy(options, excludeIndex, temp, index, leftToCopy);

                options = temp;
            }

            if (options.Length > 0)
            {
                TaskCreationOptions parsedOptions;
                string joinedOptions = string.Join(",", options);
                bool parsed = Enum.TryParse<TaskCreationOptions>(joinedOptions, out parsedOptions);
                if (!parsed)
                    throw new NotSupportedException("could not parse the options string: " + joinedOptions);

                Option = parsedOptions;
            }
        }

        public TaskInfo(TaskInfo parent, string TaskInfo_CancelWaitName, WorkloadType workType, string optionsString, bool cancelChildren)
            : this(parent, TaskInfo_CancelWaitName, workType, optionsString)
        {
            CancelChildren = cancelChildren;
        }

        #region Properties

        /// <summary>
        /// The task associated with the current node
        /// </summary>
        public Task Task { get; set; }

        /// <summary>
        /// LinkedList representing the children of the current node
        /// </summary>
        public LinkedList<TaskInfo> Children { get; set; }

        /// <summary>
        /// Bool flag indicating is the current node is a leaf
        /// </summary>
        public bool IsLeaf
        {
            get { return Children.Count == 0; }
        }

        /// <summary>
        /// Current node Parent
        /// </summary>
        public TaskInfo Parent { get; set; }

        /// <summary>
        /// Current node Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// TaskCreation option of task associated with the current node
        /// </summary>
        public TaskCreationOptions Option { get; private set; }

        /// <summary>
        /// WorkloadType_CancelWait of task associated with the current node
        /// </summary>
        public WorkloadType WorkType { get; private set; }

        /// <summary>
        /// bool for indicating if the current tasks should initiate its children cancellation
        /// </summary>
        public bool CancelChildren { get; private set; }

        /// <summary>
        /// While a tasks is correct execute a result is produced
        /// this is the result
        /// </summary>
        public double Result { get; private set; }

        /// <summary>
        /// The token associated with the current node's task
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Every node has a cancellation source - its token participate in the task creation
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// bool indicating if the children respect parent cancellation
        /// If true - the children cancellation token will be linkewd with the parent cancellation
        /// so is the parent will get cancelled the children will get as well
        /// </summary>
        public bool IsRespectParentCancellation { get; private set; }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Recursively traverse the tree and compare the current node using the predicate
        /// </summary>
        /// <param name="predicate">the predicate</param>
        /// <param name="report"></param>
        /// <returns></returns>
        public void Traversal(Action<TaskInfo> predicate)
        {
            // check current data.  If it fails check, an exception is thrown and it stops checking.
            // check children
            foreach (TaskInfo child in Children)
            {
                predicate(child);
                child.Traversal(predicate);
            }
        }

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

        internal void AddChildren(TaskInfo[] children)
        {
            foreach (var child in children)
                Children.AddLast(child);
        }

        #endregion
    }

    public enum API
    {
        Cancel,
        Wait,
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

    #endregion
}
