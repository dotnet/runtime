// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Test class using UnitTestDriver that ensures that the FromAsync overload methods are tested
//
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+

using Xunit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests.FromAsync
{
    public sealed class TaskFromAsyncTest
    {
        #region Private Field

        private readonly API _api;              //used to determine which API_FromAsync method is being tested
        private readonly TaskType _sourceType;
        private readonly TaskType _fromAsyncType;
        private readonly ErrorCase _errorCase;
        private readonly OverloadChoice _overloadChoice;

        private Task _task = null;              // the task created out of FromAsync

        private readonly List<object> _expectedInputs = new List<object>(); // store the expected input for roundtrip verification

        private const int TestInteger = 0;      // const used in APM with 1/2/3 args tests
        private const double TestDouble = 1.0;  // const used in APM with 2/3 args tests
        private const bool TestBoolean = true;  // const used in APM with 3 args tests

        // helper to remember what TaskCreationOptions/TaskScheduler to verify against
        internal class TaskOptionAndScheduler
        {
            public TaskCreationOptions Option;
            public TaskScheduler Scheduler;
        }

        // used for cover various overload.
        internal readonly TaskCreationOptions TestOption = TaskCreationOptions.None;

        #endregion

        #region Constructor

        public TaskFromAsyncTest(TestParameters parameters)
        {
            _api = parameters.Api;
            _sourceType = parameters.SourceTaskType;
            _fromAsyncType = parameters.FromAsyncTaskType;
            _errorCase = parameters.ErrorCase;
            _overloadChoice = parameters.OverloadChoice;
        }

        #endregion

        /// <summary>
        /// Executes the test and marks the test as pass/fail
        /// </summary>
        internal void RealRun()
        {
            if (_errorCase == ErrorCase.NullBegin || _errorCase == ErrorCase.NullEnd)
            {
                try
                {
                    //In case of error conditions we except the method to throw exception
                    RunAPMTest();

                    Assert.Fail(string.Format("Failed to catch ArgumentNullException"));
                }
                catch (ArgumentNullException)
                {
                    Debug.WriteLine("Caught ArgumentNullException as expected");
                }
            }
            else if (_errorCase == ErrorCase.Throwing)
            {
                //This should throw an exception
                RunAPMTest();

                //block until the expcetion is thrown
                ((IAsyncResult)_task).AsyncWaitHandle.WaitOne();  // avoid Wait() as we are using Exception property directly

                AggregateException exp = _task.Exception;

                if (exp != null &&
                    exp.InnerExceptions.Count == 1 &&
                    exp.InnerExceptions[0] is TPLTestException)
                {
                    Debug.WriteLine("Caught AggregateException as expected");
                }
                else
                {
                    Assert.Fail(string.Format("Failed to catch AggregateException"));
                }
            }
            else
            {
                AsyncWork work = RunAPMTest();

                _task.Wait(); //block for the task to run to completion.

                if (_sourceType == TaskType.Task)
                    SequenceEquals(_expectedInputs, work.Inputs);
                else
                    SequenceEquals(_expectedInputs, ((Task<ReadOnlyCollection<object>>)_task).Result);

                //verify Overload / State

                TaskCreationOptions expectedOption = TaskCreationOptions.None;
                if (_overloadChoice == OverloadChoice.WithTaskOption)
                    expectedOption = TestOption;

                if (_task.CreationOptions != expectedOption)
                    Assert.Fail(string.Format("task is not created with expected TestOption"));

                if (((TaskOptionAndScheduler)(work.ObservedState)).Option != expectedOption)
                    Assert.Fail(string.Format("state verification failed on Option"));

                TaskScheduler expectedScheduler = TaskScheduler.Default;

                if (work.ObservedTaskScheduler != expectedScheduler)
                    Assert.Fail(string.Format("task is not running under expected TestOption"));

                if (((TaskOptionAndScheduler)(work.ObservedState)).Scheduler != expectedScheduler)
                    Assert.Fail(string.Format("state verification failed on Scheduler"));
            }
        }

        #region Private Helpers

        private static void SequenceEquals(List<object> expectedItems, ReadOnlyCollection<object> actualItems)
        {
            Assert.Equal(expectedItems.Count, actualItems.Count);
            for (int i = 0; i < expectedItems.Count; i++)
            {
                Assert.Equal(expectedItems[i], actualItems[i]);
            }
        }

        /// <summary>
        /// Method that creates the various task using the FRomAsync methods
        /// </summary>
        /// <returns></returns>
        private AsyncWork RunAPMTest()
        {
            switch (_api)
            {
                case API.APM:
                    if (_sourceType == TaskType.Task)
                    {
                        if (_fromAsyncType == TaskType.Task)
                        {
                            AsyncAction action = new AsyncAction(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync((Func<AsyncCallback, object, IAsyncResult>)null, action.EndInvoke, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync(action.BeginInvoke, null, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync(action.BeginInvoke, action.EndInvoke, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync(action.BeginInvoke, action.EndInvoke, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM");
                                }
                            }

                            return action;
                        }
                        else // must be FromAsync type of TaskType_FromAsync.TaskT
                        {
                            AsyncFunc func = new AsyncFunc(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync<ReadOnlyCollection<object>>((Func<AsyncCallback, object, IAsyncResult>)null, func.EndInvoke, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync<ReadOnlyCollection<object>>(func.BeginInvoke, null, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync<ReadOnlyCollection<object>>(func.BeginInvoke, func.EndInvoke, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync<ReadOnlyCollection<object>>(func.BeginInvoke, func.EndInvoke, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM");
                                }
                            }

                            return func;
                        }
                    }
                    else // must be TaskType_FromAsync.TaskT
                    {
                        AsyncFunc func = new AsyncFunc(_errorCase == ErrorCase.Throwing);

                        if (_errorCase == ErrorCase.NullBegin)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync((Func<AsyncCallback, object, IAsyncResult>)null, func.EndInvoke, null);
                        else if (_errorCase == ErrorCase.NullEnd)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync(func.BeginInvoke, null, null);
                        else
                        {
                            switch (_overloadChoice)
                            {
                                case OverloadChoice.None:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func.BeginInvoke, func.EndInvoke, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                    break;

                                case OverloadChoice.WithTaskOption:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func.BeginInvoke, func.EndInvoke, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("invalid overloadChoice for APM");
                            }
                        }

                        return func;
                    }

                case API.APM_T:

                    _expectedInputs.Add(TestInteger);

                    if (_sourceType == TaskType.Task)
                    {
                        if (_fromAsyncType == TaskType.Task)
                        {
                            AsyncAction<int> action1 = new AsyncAction<int>(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync((Func<int, AsyncCallback, object, IAsyncResult>)null, action1.EndInvoke, TestInteger, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync(action1.BeginInvoke, null, TestInteger, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync(action1.BeginInvoke, action1.EndInvoke, TestInteger, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync(action1.BeginInvoke, action1.EndInvoke, TestInteger, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T");
                                }
                            }

                            return action1;
                        }
                        else // must be FromAsync type of TaskType_FromAsync.TaskT
                        {
                            AsyncFunc<int> func1 = new AsyncFunc<int>(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync<int, ReadOnlyCollection<object>>((Func<int, AsyncCallback, object, IAsyncResult>)null, func1.EndInvoke, TestInteger, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync<int, ReadOnlyCollection<object>>(func1.BeginInvoke, null, TestInteger, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync<int, ReadOnlyCollection<object>>(func1.BeginInvoke, func1.EndInvoke, TestInteger, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync<int, ReadOnlyCollection<object>>(func1.BeginInvoke, func1.EndInvoke, TestInteger, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T");
                                }
                            }

                            return func1;
                        }
                    }
                    else // must be TaskType_FromAsync.TaskT
                    {
                        AsyncFunc<int> func1 = new AsyncFunc<int>(_errorCase == ErrorCase.Throwing);

                        if (_errorCase == ErrorCase.NullBegin)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync((Func<int, AsyncCallback, object, IAsyncResult>)null, func1.EndInvoke, TestInteger, null);
                        else if (_errorCase == ErrorCase.NullEnd)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync(func1.BeginInvoke, null, TestInteger, null);
                        else
                        {
                            switch (_overloadChoice)
                            {
                                case OverloadChoice.None:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func1.BeginInvoke, func1.EndInvoke, TestInteger, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                    break;

                                case OverloadChoice.WithTaskOption:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func1.BeginInvoke, func1.EndInvoke, TestInteger, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T");
                            }
                        }

                        return func1;
                    }


                case API.APM_T2: //Two variables

                    _expectedInputs.Add(TestInteger);
                    _expectedInputs.Add(TestDouble);

                    if (_sourceType == TaskType.Task)
                    {
                        if (_fromAsyncType == TaskType.Task)
                        {
                            AsyncAction<int, double> action2 = new AsyncAction<int, double>(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync((Func<int, double, AsyncCallback, object, IAsyncResult>)null, action2.EndInvoke, TestInteger, TestDouble, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync(action2.BeginInvoke, null, TestInteger, TestDouble, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync(action2.BeginInvoke, action2.EndInvoke, TestInteger, TestDouble, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync(action2.BeginInvoke, action2.EndInvoke, TestInteger, TestDouble, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T2");
                                }
                            }

                            return action2;
                        }
                        else // must be FromAsync type of TaskType_FromAsync.TaskT
                        {
                            AsyncFunc<int, double> func2 = new AsyncFunc<int, double>(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync<int, double, ReadOnlyCollection<object>>((Func<int, double, AsyncCallback, object, IAsyncResult>)null, func2.EndInvoke, TestInteger, TestDouble, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync<int, double, ReadOnlyCollection<object>>(func2.BeginInvoke, null, TestInteger, TestDouble, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync<int, double, ReadOnlyCollection<object>>(func2.BeginInvoke, func2.EndInvoke, TestInteger, TestDouble, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync<int, double, ReadOnlyCollection<object>>(func2.BeginInvoke, func2.EndInvoke, TestInteger, TestDouble, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T2");
                                }
                            }

                            return func2;
                        }
                    }
                    else // must be TaskType_FromAsync.TaskT
                    {
                        AsyncFunc<int, double> func2 = new AsyncFunc<int, double>(_errorCase == ErrorCase.Throwing);

                        if (_errorCase == ErrorCase.NullBegin)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync((Func<int, double, AsyncCallback, object, IAsyncResult>)null, func2.EndInvoke, TestInteger, TestDouble, null);
                        else if (_errorCase == ErrorCase.NullEnd)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync(func2.BeginInvoke, null, TestInteger, TestDouble, null);
                        else
                        {
                            switch (_overloadChoice)
                            {
                                case OverloadChoice.None:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func2.BeginInvoke, func2.EndInvoke, TestInteger, TestDouble, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                    break;

                                case OverloadChoice.WithTaskOption:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func2.BeginInvoke, func2.EndInvoke, TestInteger, TestDouble, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T2");
                            }
                        }

                        return func2;
                    }


                case API.APM_T3:

                    _expectedInputs.Add(TestInteger);
                    _expectedInputs.Add(TestDouble);
                    _expectedInputs.Add(TestBoolean);

                    if (_sourceType == TaskType.Task)
                    {
                        if (_fromAsyncType == TaskType.Task)
                        {
                            AsyncAction<int, double, bool> action3 = new AsyncAction<int, double, bool>(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync((Func<int, double, bool, AsyncCallback, object, IAsyncResult>)null, action3.EndInvoke, TestInteger, TestDouble, TestBoolean, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync(action3.BeginInvoke, null, TestInteger, TestDouble, TestBoolean, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync(action3.BeginInvoke, action3.EndInvoke, TestInteger, TestDouble, TestBoolean, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync(action3.BeginInvoke, action3.EndInvoke, TestInteger, TestDouble, TestBoolean, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T3");
                                }
                            }

                            return action3;
                        }
                        else // must be FromAsync type of TaskType_FromAsync.TaskT
                        {
                            AsyncFunc<int, double, bool> func3 = new AsyncFunc<int, double, bool>(_errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync<int, double, bool, ReadOnlyCollection<object>>((Func<int, double, bool, AsyncCallback, object, IAsyncResult>)null, func3.EndInvoke, TestInteger, TestDouble, TestBoolean, null);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync<int, double, bool, ReadOnlyCollection<object>>(func3.BeginInvoke, null, TestInteger, TestDouble, TestBoolean, null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync<int, double, bool, ReadOnlyCollection<object>>(func3.BeginInvoke, func3.EndInvoke, TestInteger, TestDouble, TestBoolean, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync<int, double, bool, ReadOnlyCollection<object>>(func3.BeginInvoke, func3.EndInvoke, TestInteger, TestDouble, TestBoolean, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T3");
                                }
                            }

                            return func3;
                        }
                    }
                    else // must be TaskType_FromAsync.TaskT
                    {
                        AsyncFunc<int, double, bool> func3 = new AsyncFunc<int, double, bool>(_errorCase == ErrorCase.Throwing);

                        if (_errorCase == ErrorCase.NullBegin)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync((Func<int, double, bool, AsyncCallback, object, IAsyncResult>)null, func3.EndInvoke, TestInteger, TestDouble, TestBoolean, null);
                        else if (_errorCase == ErrorCase.NullEnd)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync(func3.BeginInvoke, null, TestInteger, TestDouble, TestBoolean, null);
                        else
                        {
                            switch (_overloadChoice)
                            {
                                case OverloadChoice.None:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func3.BeginInvoke, func3.EndInvoke, TestInteger, TestDouble, TestBoolean, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None });
                                    break;

                                case OverloadChoice.WithTaskOption:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func3.BeginInvoke, func3.EndInvoke, TestInteger, TestDouble, TestBoolean, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }, TestOption);
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("invalid overloadChoice for APM_T3");
                            }
                        }

                        return func3;
                    }

                case API.IAsyncResult:

                    // put the current params into the expectedInputs
                    object[] inputs = new object[] { _api, _sourceType, _errorCase };
                    _expectedInputs.AddRange(inputs);

                    if (_sourceType == TaskType.Task)
                    {
                        if (_fromAsyncType == TaskType.Task)
                        {
                            AsyncAction action = new AsyncAction(inputs, _errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync((IAsyncResult)null, action.EndInvoke);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync(action.BeginInvoke(null, null), null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync(action.BeginInvoke(null, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None }), action.EndInvoke);
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync(action.BeginInvoke(null, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }), action.EndInvoke, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for IAsyncResult");
                                }
                            }

                            return action;
                        }
                        else // must be FromAsync type of TaskType_FromAsync.TaskT
                        {
                            AsyncFunc func = new AsyncFunc(inputs, _errorCase == ErrorCase.Throwing);

                            if (_errorCase == ErrorCase.NullBegin)
                                Task.Factory.FromAsync<ReadOnlyCollection<object>>((IAsyncResult)null, func.EndInvoke);
                            else if (_errorCase == ErrorCase.NullEnd)
                                Task.Factory.FromAsync<ReadOnlyCollection<object>>(func.BeginInvoke(null, null), null);
                            else
                            {
                                switch (_overloadChoice)
                                {
                                    case OverloadChoice.None:
                                        _task = Task.Factory.FromAsync<ReadOnlyCollection<object>>(func.BeginInvoke(null, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None }), func.EndInvoke);
                                        break;

                                    case OverloadChoice.WithTaskOption:
                                        _task = Task.Factory.FromAsync<ReadOnlyCollection<object>>(func.BeginInvoke(null, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }), func.EndInvoke, TestOption);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException("invalid overloadChoice for IAsyncResult");
                                }
                            }

                            return func;
                        }
                    }
                    else // must be TaskType_FromAsync.TaskT
                    {
                        AsyncFunc func = new AsyncFunc(inputs, _errorCase == ErrorCase.Throwing);

                        if (_errorCase == ErrorCase.NullBegin)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync((IAsyncResult)null, func.EndInvoke);
                        else if (_errorCase == ErrorCase.NullEnd)
                            Task<ReadOnlyCollection<object>>.Factory.FromAsync(func.BeginInvoke(null, null), null);
                        else
                        {
                            switch (_overloadChoice)
                            {
                                case OverloadChoice.None:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func.BeginInvoke(null, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TaskCreationOptions.None }), func.EndInvoke);
                                    break;

                                case OverloadChoice.WithTaskOption:
                                    _task = Task<ReadOnlyCollection<object>>.Factory.FromAsync(func.BeginInvoke(null, new TaskOptionAndScheduler { Scheduler = TaskScheduler.Default, Option = TestOption }), func.EndInvoke, TestOption);
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("invalid overloadChoice for IAsyncResult");
                            }
                        }

                        return func;
                    }


                default:
                    throw new ArgumentException("unknown api to test");
            }
        }

        #endregion
    }

    #region Helper Classes / Enums

    public enum TaskType
    {
        Task,         // test the API_FromAsyncs on the Task class
        TaskT,        // test the API_FromAsyncs on the Task<T> class
    }

    public enum API
    {
        IAsyncResult,  // test the API_FromAsync takes in an IAsyncResult
        APM,           // test the API_FromAsync takes in the begin/endMethod with no arg
        APM_T,         // test the API_FromAsync takes in the begin/endMethod with 1 arg
        APM_T2,        // test the API_FromAsync takes in the begin/endMethod with 2 args
        APM_T3,        // test the API_FromAsync takes in the begin/endMethod with 3 args
    }

    public enum OverloadChoice
    {
        None,                        // test the overload with NO TaskScheduler and NO TaskOption
        WithTaskOption,              // test the overload with TaskOption
        //REMOVED because the throttled TaskScheduler does not work with the Win8P surface area.
        //WithTaskOptionAndScheduler,  // test the overload with both TaskScheduler and TaskOption.
        //WithTaskScheduler,           // test the overload with TaskScheduler.
    }

    public enum ErrorCase
    {
        None,         // not an error case
        Throwing,     // the async work delegate will throw
        NullBegin,    // pass in a null beginMethod
        NullEnd,      // pass in a null endMethod
    }

    public class TestParameters
    {
        public readonly API Api;
        public readonly TaskType SourceTaskType;
        public readonly TaskType FromAsyncTaskType;
        public readonly ErrorCase ErrorCase;
        public readonly OverloadChoice OverloadChoice;
        public TestParameters(API api, TaskType sourceTask, TaskType fromAsyncTask, OverloadChoice overloadChoice, ErrorCase errorCase)
        {
            Api = api;
            SourceTaskType = sourceTask;
            FromAsyncTaskType = fromAsyncTask;
            ErrorCase = errorCase;
            OverloadChoice = overloadChoice;
        }
    }

    #endregion

    public partial class TaskFromAsyncTests
    {
        #region Test Methods

        [Fact]
        public static void TaskFromAsyncTest1()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest2()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }



        [Fact]
        public static void TaskFromAsyncTest5()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest6()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskFromAsyncTest10()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest11()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskFromAsyncTest15()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest16()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskFromAsyncTest21()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskFromAsyncTest28()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest29()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest31()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest32()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest36()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest37()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest41()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest42()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest46()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskFromAsyncTest53()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest54()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest57()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest58()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest62()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest63()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest67()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest68()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskFromAsyncTest73()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullBegin);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        #endregion
    }
}
