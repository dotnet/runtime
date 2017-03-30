// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Test class using UnitTestDriver that ensures all the public ctor of Task, Future and
// promise are properly working
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public sealed class TaskCreateTests
    {
        #region Test Methods

        [Fact]
        public static void TaskCreateTest0()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest1()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest2()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest3()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest4()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }
        [Fact]
        public static void TaskCreateTest5()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.InvalidTaskOption,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest6()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.NullAction,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest7()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.NullTaskManager,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }

        [Fact]
        public static void TaskCreateTest8()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest9()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest10()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest11()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest12()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest13()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest14()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest15()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest16()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest17()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest18()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest19()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest20()
        {
            TestParameters parameters = new TestParameters(TaskType.FutureT)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }
        [Fact]
        public static void TaskCreateTest21()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.InvalidTaskOption,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest22()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.NullAction,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest23()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.NullTaskManager,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }

        [Fact]
        public static void TaskCreateTest24()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest25()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest26()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest27()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest28()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest29()
        {
            TestParameters parameters = new TestParameters(TaskType.Future)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest30()
        {
            TestParameters parameters = new TestParameters(TaskType.Promise)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest31()
        {
            TestParameters parameters = new TestParameters(TaskType.Promise)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest32()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest33()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest34()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest35()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }

        [Fact]
        public static void TaskCreateTest36()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.CreateTask();
        }
        [Fact]
        public static void TaskCreateTest37()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.InvalidTaskOption,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest38()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.MultipleTaskStart,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest39()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.NullAction,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest40()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.NullTaskManager,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest41()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.StartOnContinueWith,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }
        [Fact]
        public static void TaskCreateTest42()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.StartOnPromise,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.ExceptionTests();
        }

        [Fact]
        public static void TaskCreateTest43()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest44()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest45()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest46()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest47()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest48()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartNewTask();
        }

        [Fact]
        public static void TaskCreateTest49()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest50()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = false,
                HasActionState = false,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest51()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = false,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest52()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = false,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest53()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = false,
                HasActionState = true,
                HasTaskManager = false,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        [Fact]
        public static void TaskCreateTest54()
        {
            TestParameters parameters = new TestParameters(TaskType.Task)
            {
                HasCancellationToken = true,
                HasActionState = true,
                HasTaskManager = true,
                HasTaskOption = true,
                ExceptionTestsAction = ExceptionTestsAction.None,
            };
            TaskCreateTest test = new TaskCreateTest(parameters);
            test.StartTask();
        }

        #endregion

        internal class TestParameters
        {
            internal readonly TaskType TaskType;

            internal bool HasTaskManager;

            internal bool HasActionState;

            internal bool HasTaskOption;

            internal bool HasCancellationToken;

            internal ExceptionTestsAction ExceptionTestsAction;

            internal TestParameters(TaskType taskType)
            {
                TaskType = taskType;
            }
        }

        /// <summary>
        /// Class that verifies that all the public constructors of Task, future, promise and futureT are working correctly
        /// The test creates the test object (Task, Future, promise) using various ctor and ensures that they were 
        /// created and can be started. All the negative cases (exceptional cases are also covered in this test set).
        /// </summary>
        internal sealed class TaskCreateTest
        {
            #region Member Variables

            //Bunch of constants that is used to simulate work done by TPL Task (does some funky maths
            // (1 + 1/(2*2) + 1/(3*3) + ... +  1/(1000000*1000000) and verifies that the result is 
            //equals to Math.Pow (Math.PI, 2) / 6) aka not important from TPL test perspective
            private const int ZETA_SEED = 1000000;

            /// <summary>
            /// Used to save the results that is returned by the Task upon completion (used to verify that task ran successfully)
            /// </summary>
            private double _result;

            /// <summary>
            /// Used to store the Task that is under test. initialized using method CreateTaskHelper()
            /// </summary>
            private Task _task;

            /// <summary>
            /// Used to differentiate what type of task to test
            /// </summary>
            private readonly TaskType _taskType = TaskType.Task;

            private readonly bool _hasTaskManager;

            private readonly bool _hasActionState;

            private readonly bool _hasCancellationToken;

            private readonly bool _hasTaskOption;

            private readonly ExceptionTestsAction _exceptionTestsAction;

            /// <summary>
            /// Need a cancellationTokenSource to test the APIs that accept a cancellationTokens
            /// </summary>
            private CancellationTokenSource _cts;

            #endregion

            #region Constructor

            public TaskCreateTest(TestParameters parameters)
            {
                _taskType = parameters.TaskType;
                _hasTaskManager = parameters.HasTaskManager;
                _hasActionState = parameters.HasActionState;
                _hasTaskOption = parameters.HasTaskOption;
                _hasCancellationToken = parameters.HasCancellationToken;
                _exceptionTestsAction = parameters.ExceptionTestsAction;
            }

            #endregion

            #region Test Methods (These are the ones that are actually invoked)

            /// <summary>
            /// Test that creates a Task, Future and Promise using various Ctor and ensures that the task was created successfully
            /// </summary>
            /// <returns>indicates whether test passed or failed (invoking ctor was success or not)</returns>
            internal void CreateTask()
            {
                //Using the parameters specified in the XML input file create a Task, Future or promise 
                _task = CreateTaskHelper();

                // Checks whether the task was created, initialized with specified action
                Assert.NotNull(_task);

                // If token was set on the Task, we should be able to cancel the Task
                if (_cts != null)
                {
                    _cts.Cancel();

                    if (!_task.IsCanceled || _task.Status != TaskStatus.Canceled)
                        Assert.True(false, string.Format("Task Token doesn't matched TokenSource's Token"));
                }
            }

            /// <summary>
            /// Tests to ensure that TaskTypes can be created, started and they run to completion successfully
            /// </summary>
            /// <returns></returns>
            internal void StartTask()
            {
                // It is not allowed to Start Task multiple times, so this test will not try to do that
                // instead it is part of exception testing
                Debug.WriteLine("Testing Start() on Task, HasTaskManager={0}", _hasTaskManager);
                _task = CreateTaskHelper();

                if (_hasTaskManager)
                    _task.Start(TaskScheduler.Default);
                else
                    _task.Start();

                _task.Wait();

                // Verification
                CheckResult();
            }

            /// <summary>
            /// Tests to ensure that TaskTypes can be created using the StartNew static TaskFactory method 
            /// </summary>
            /// <returns></returns>
            internal void StartNewTask()
            {
                CancellationTokenSource cts = new CancellationTokenSource();

                Debug.WriteLine("Start new Task, HasActionState={0}, HasTaskOption={1}, HasTaskManager={2}, TaskType={3}, HasCancellationToken={4}",
                    _hasActionState, _hasTaskOption, _hasTaskManager, _taskType, _hasCancellationToken);

                if (!_hasActionState)
                {
                    if (_hasTaskManager && _hasTaskOption && _hasCancellationToken)
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(Work, cts.Token, TaskCreationOptions.None, TaskScheduler.Default);
                                break;
                            case TaskType.FutureT:
                                _task = Task<double>.Factory.StartNew(FutureWork, cts.Token, TaskCreationOptions.None, TaskScheduler.Default);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWork, cts.Token, TaskCreationOptions.None, TaskScheduler.Default);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                    else if (_hasTaskOption)
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(Work, TaskCreationOptions.None);
                                break;
                            case TaskType.FutureT:
                                _task = Task<double>.Factory.StartNew(FutureWork, TaskCreationOptions.None);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWork, TaskCreationOptions.None);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                    else if (_hasCancellationToken)
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(Work, cts.Token);
                                break;
                            case TaskType.FutureT:
                                _task = (Task<double>)Task<double>.Factory.StartNew(FutureWork, cts.Token);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWork, cts.Token);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                    else
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(Work);
                                break;
                            case TaskType.FutureT:
                                _task = (Task<double>)Task<double>.Factory.StartNew(FutureWork);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWork);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                }
                else
                {
                    if (_hasTaskManager && _hasTaskOption && _hasCancellationToken)
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(WorkWithState, ZETA_SEED, cts.Token, TaskCreationOptions.None, TaskScheduler.Default);
                                break;
                            case TaskType.FutureT:
                                _task = Task<double>.Factory.StartNew(FutureWorkWithState, ZETA_SEED, cts.Token, TaskCreationOptions.None, TaskScheduler.Default);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWorkWithState, ZETA_SEED, cts.Token, TaskCreationOptions.None, TaskScheduler.Default);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                    else if (_hasTaskOption)
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(WorkWithState, ZETA_SEED, TaskCreationOptions.None);
                                break;
                            case TaskType.FutureT:
                                _task = Task<double>.Factory.StartNew(FutureWorkWithState, ZETA_SEED, TaskCreationOptions.None);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWorkWithState, ZETA_SEED, TaskCreationOptions.None);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                    else if (_hasCancellationToken)
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(Work, cts.Token);
                                break;
                            case TaskType.FutureT:
                                _task = Task<double>.Factory.StartNew(FutureWorkWithState, ZETA_SEED, cts.Token);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWorkWithState, ZETA_SEED, cts.Token);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                    else
                    {
                        switch (_taskType)
                        {
                            case TaskType.Task:
                                _task = Task.Factory.StartNew(WorkWithState, ZETA_SEED);
                                break;
                            case TaskType.FutureT:
                                _task = Task<double>.Factory.StartNew(FutureWorkWithState, ZETA_SEED);
                                break;
                            case TaskType.Future:
                                _task = Task.Factory.StartNew<double>(FutureWorkWithState, ZETA_SEED);
                                break;
                            default:
                                throw new NotSupportedException("DOes not support this type: " + _taskType);
                        }
                    }
                }

                _task.Wait();
                CheckResult();
            }

            /// <summary>
            /// Test to ensure that exceptions are thrown for invalid ctor parameters
            /// </summary>
            /// <returns></returns>
            internal void ExceptionTests()
            {
                switch (_exceptionTestsAction)
                {
                    case ExceptionTestsAction.NullAction:
                        Debug.WriteLine("Test passing null Action/Func to Constructor and StartNew() of {0}", _taskType);
                        if (_taskType != TaskType.Promise)
                        {
                            //
                            // For Constructor
                            // 
                            if (_taskType != TaskType.Future)
                            {
                                try
                                {
                                    if (_taskType == TaskType.Task)
                                        _task = new Task(null);
                                    else if (_taskType == TaskType.FutureT)
                                        _task = new Task<double>(null);

                                    Assert.True(false, string.Format("Able to pass null Action/Func to Constructor of {0}, when expecting exception", _taskType));
                                }
                                catch (ArgumentNullException)
                                {
                                    Debug.WriteLine("Exception throws as expected when trying to pass null Action/Func to Constructor of {0}", _taskType);
                                }
                            }
                            //
                            // For StartNew
                            // 
                            try
                            {
                                Action o = null;
                                Func<double> o2 = null;

                                if (_taskType == TaskType.Task)
                                    _task = Task.Factory.StartNew(o);
                                else if (_taskType == TaskType.FutureT)
                                    _task = Task<double>.Factory.StartNew(o2);
                                else if (_taskType == TaskType.Future)
                                    _task = Task.Factory.StartNew<double>(o2);

                                Assert.True(false, string.Format("Able to pass null Action/Func to StartNew() of {0}, when expecting exception", _taskType));
                            }
                            catch (ArgumentNullException)
                            {
                                Debug.WriteLine("Exception throws as expected when trying to pass null Action/Func to StartNew() of {0}", _taskType);
                            }
                        }
                        break;

                    case ExceptionTestsAction.InvalidTaskOption:
                        int invalidOption = 9999; //Used to create Invalid TaskCreationOption

                        Debug.WriteLine("Test passing invalid TaskCreationOptions {0} to Constructor and StartNew() of {1}", invalidOption, _taskType);
                        if (_taskType != TaskType.Promise)
                        {
                            //
                            // For Constructor
                            // 
                            if (_taskType != TaskType.Future)
                            {
                                try
                                {
                                    if (_taskType == TaskType.Task)
                                        _task = new Task(Work, (TaskCreationOptions)invalidOption);
                                    else if (_taskType == TaskType.FutureT)
                                        _task = new Task<double>(FutureWork, (TaskCreationOptions)invalidOption);

                                    Assert.True(false, string.Format("Able to pass invalid TaskCreationOptions to Constructor of {0}, when expecting exception", _taskType));
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    Debug.WriteLine("Exception throws as expected when trying to pass invalid TaskCreationOptions to Constructor of {0}", _taskType);
                                }
                            }
                            //
                            // For StartNew
                            // 
                            try
                            {
                                if (_taskType == TaskType.Task)
                                    _task = Task.Factory.StartNew(Work, (TaskCreationOptions)invalidOption);
                                else if (_taskType == TaskType.FutureT)
                                    _task = Task<double>.Factory.StartNew(FutureWork, (TaskCreationOptions)invalidOption);
                                else if (_taskType == TaskType.Future)
                                    _task = Task.Factory.StartNew<double>(FutureWork, (TaskCreationOptions)invalidOption);

                                Assert.True(false, string.Format("Able to pass invalid TaskCreationOptions to StartNew() of {0}, when expecting exception", _taskType));
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                Debug.WriteLine("Exception throws as expected when trying to pass invalid TaskCreationOptions to StartNew() of {0}", _taskType);
                            }
                        }
                        break;

                    case ExceptionTestsAction.NullTaskManager:
                        Debug.WriteLine("Test passing null TaskManager to Start() and StartNew() on {0}", _taskType);
                        TMExceptionTestHelper(null, "null");
                        break;

                    case ExceptionTestsAction.MultipleTaskStart:
                        Debug.WriteLine("Testing multiple Start() on {0}", _taskType);
                        try
                        {
                            _task = CreateTaskHelper();
                            _task.Start();
                            _task.Start();
                            Assert.True(false, string.Format("Able to Start {0} multiple times, when expecting exception", _taskType));
                        }
                        catch (InvalidOperationException)
                        {
                            Debug.WriteLine("Exception throws as expected when trying to Start {0} multiple times", _taskType);
                        }
                        break;

                    case ExceptionTestsAction.StartOnPromise:
                        Debug.WriteLine("Testing Start() on Promise");
                        try
                        {
                            TaskCompletionSource<double> f = new TaskCompletionSource<double>();
                            f.Task.Start();
                            Assert.True(false, string.Format("Able to Start a Promise, when expecting exception"));
                        }
                        catch (System.InvalidOperationException)
                        {
                            Debug.WriteLine("Exception throws as expected when trying to Start a Promise");
                        }
                        break;

                    case ExceptionTestsAction.StartOnContinueWith:
                        Debug.WriteLine("Testing Start() on ContinueWith Task");
                        try
                        {
                            Task t = CreateTaskHelper().ContinueWith(delegate { Work(); });
                            t.Start();
                            Assert.True(false, string.Format("Able to start task manually on ContinueWith Task, when expecting exception"));
                        }
                        catch (InvalidOperationException)
                        {
                            Debug.WriteLine("Exception throws as expected when trying to start ContinueWith Task manually");
                        }
                        break;
                    default:
                        Assert.True(false, string.Format("Invalid Exception Test Action given, {0}", _exceptionTestsAction));
                        break;
                }
            }

            #endregion

            #region Helper Methods

            private void TMExceptionTestHelper(TaskScheduler tm, string tmInvalidMessage)
            {
                if (_taskType != TaskType.Promise)
                {
                    //
                    // For Start()
                    // 
                    if (_taskType != TaskType.Future)
                    {
                        try
                        {
                            _task = CreateTaskHelper();
                            _task.Start(tm);

                            Assert.True(false, string.Format("Able to pass {0} TaskManager to Start() on {1}, when expecting exception", tmInvalidMessage, _taskType));
                        }
                        catch (ArgumentNullException)
                        {
                            if (tmInvalidMessage.Equals("null", StringComparison.OrdinalIgnoreCase))
                                Debug.WriteLine("Exception ArgumentNullException throws as expected when trying to pass {0} TaskManager to Start() on {1}", tmInvalidMessage, _taskType);
                            else
                                throw;
                        }
                        catch (InvalidOperationException)
                        {
                            if (tmInvalidMessage.Equals("disposed", StringComparison.OrdinalIgnoreCase))
                                Debug.WriteLine("Exception InvalidOperationException throws as expected when trying to pass {0} TaskManager to Start() on {1}", tmInvalidMessage, _taskType);
                            else
                                throw;
                        }
                    }

                    //
                    // For StartNew()
                    // 
                    try
                    {
                        CancellationToken token = new CancellationToken();

                        if (_taskType == TaskType.Task)
                            _task = Task.Factory.StartNew(Work, token, TaskCreationOptions.None, tm);
                        else if (_taskType == TaskType.FutureT)
                            _task = Task<double>.Factory.StartNew(FutureWork, token, TaskCreationOptions.None, tm);
                        else if (_taskType == TaskType.Future)
                            _task = Task.Factory.StartNew<double>(FutureWork, token, TaskCreationOptions.None, tm);

                        Assert.True(false, string.Format("Able to pass {0} TaskManager to StartNew() on {1}, when expecting exception", tmInvalidMessage, _taskType));
                    }
                    catch (ArgumentNullException)
                    {
                        if (tmInvalidMessage.Equals("null", StringComparison.OrdinalIgnoreCase))
                            Debug.WriteLine("Exception ArgumentNullException throws as expected when trying to pass {0} TaskManager to StartNew() on {1}", tmInvalidMessage, _taskType);
                        else
                            throw;
                    }
                    catch (InvalidOperationException)
                    {
                        if (tmInvalidMessage.Equals("disposed", StringComparison.OrdinalIgnoreCase))
                            Debug.WriteLine("Exception InvalidOperationException throws as expected when trying to pass {0} TaskManager to StartNew() on {1}", tmInvalidMessage, _taskType);
                        else
                            throw;
                    }
                }
            }

            /// <summary>
            /// Helper function that creates Task/Future based on test parameters
            /// </summary>
            /// <returns></returns>
            private Task CreateTaskHelper()
            {
                CancellationToken token = CancellationToken.None;
                Task newTask;

                Debug.WriteLine("Creating Task, HasActionState={0}, HasTaskOption={1}, TaskType={2}", _hasActionState, _hasTaskOption, _taskType);

                if (_taskType == TaskType.Promise)
                {
                    if (_hasTaskOption && _hasActionState)
                        newTask = (new TaskCompletionSource<double>(new object(), TaskCreationOptions.None)).Task;
                    else if (_hasTaskOption)
                        newTask = (new TaskCompletionSource<double>(TaskCreationOptions.None)).Task;
                    else if (_hasActionState)
                        newTask = (new TaskCompletionSource<double>(new object())).Task;
                    else
                        newTask = (new TaskCompletionSource<double>()).Task;
                }
                else
                {
                    if (!_hasActionState)
                    {
                        if (_hasTaskOption && _hasCancellationToken)
                        {
                            _cts = new CancellationTokenSource();
                            token = _cts.Token;
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(Work, token, TaskCreationOptions.None);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWork, token, TaskCreationOptions.None);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                        else if (_hasTaskOption)
                        {
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(Work, TaskCreationOptions.None);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWork, TaskCreationOptions.None);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                        else if (_hasCancellationToken)
                        {
                            _cts = new CancellationTokenSource();
                            token = _cts.Token;
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(Work, token);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWork, token);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                        else
                        {
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(Work);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWork);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                    }
                    else
                    {
                        if (_hasTaskOption && _hasCancellationToken)
                        {
                            _cts = new CancellationTokenSource();
                            token = _cts.Token;
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(WorkWithState, ZETA_SEED, token, TaskCreationOptions.None);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWorkWithState, ZETA_SEED, token, TaskCreationOptions.None);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                        else if (_hasTaskOption)
                        {
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(WorkWithState, ZETA_SEED, TaskCreationOptions.None);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWorkWithState, ZETA_SEED, TaskCreationOptions.None);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                        else if (_hasCancellationToken)
                        {
                            _cts = new CancellationTokenSource();
                            token = _cts.Token;
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(WorkWithState, ZETA_SEED, token);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWorkWithState, ZETA_SEED, token);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                        else
                        {
                            switch (_taskType)
                            {
                                case TaskType.Task:
                                    newTask = new Task(WorkWithState, ZETA_SEED);
                                    break;
                                case TaskType.FutureT:
                                    newTask = new Task<double>(FutureWorkWithState, ZETA_SEED);
                                    break;
                                default:
                                    throw new ArgumentException("Cannot create an instance of static type: " + _taskType);
                            }
                        }
                    }
                }

                return newTask;
            }

            private void Work()
            {
                WorkWithState(ZETA_SEED);
            }

            private void WorkWithState(object o)
            {
                // There is a rare case when the main thread (that does the assignment of Task to task) is not
                //executed before the delegate is invoked. This spinwait guards against such cases
                while (_task == null)
                {
                    SpinWait.SpinUntil(() => false, 100);
                }

                if (TaskScheduler.Current == TaskScheduler.Default
                       && _task.CreationOptions == TaskCreationOptions.None)
                {
                    //The Workloads are defined in the common folder
                    _result = ZetaSequence((int)o);
                }
            }

            private double FutureWork()
            {
                return FutureWorkWithState(ZETA_SEED);
            }

            private double FutureWorkWithState(object o)
            {
                // Waiting until the task is assigned on StartNew scenario
                while (_task == null)
                {
                    SpinWait.SpinUntil(() => false, 100);
                }

                if (TaskScheduler.Current == TaskScheduler.Default
                       && _task.CreationOptions == TaskCreationOptions.None)
                {
                    return ZetaSequence((int)o);
                }
                else
                {
                    return 0;
                }
            }

            /// <summary>
            /// Method used to verify that task was created
            /// </summary>
            /// <returns></returns>
            private bool IsTaskCreated()
            {
                return _task != null;
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
            /// Method used to verify that task returns the expected result (which means task ran successfully)
            /// </summary>
            /// <returns></returns>
            private void CheckResult()
            {
                double actualResult = 0;

                switch (_taskType)
                {
                    case TaskType.Task:
                        actualResult = _result;
                        break;
                    case TaskType.FutureT:
                    case TaskType.Future:
                        actualResult = ((Task<double>)_task).Result;
                        break;
                    default:
                        throw new NotSupportedException("Mismatch type, " + _taskType + " doesn't have value that can be verified");
                }

                //Function point comparison cant be done by rounding off to nearest decimal points since
                //1.64 could be represented as 1.63999999 or as 1.6499999999. To perform floating point comparisons, 
                //a range has to be defined and check to ensure that the result obtained is within the specified range
                double minLimit = 1.63;
                double maxLimit = 1.65;

                if (actualResult > minLimit && actualResult < maxLimit)
                    Debug.WriteLine("Result matched");
                else
                    Assert.True(false, string.Format("Result mismatched, expecting to lie between {0} and {1} but got {2}", minLimit, maxLimit, actualResult));
            }

            #endregion
        }

        /// <summary>
        /// Type of Task types to test for
        /// </summary>
        internal enum TaskType
        {
            Task,
            Future,
            FutureT,
            Promise
        }

        /// <summary>
        /// Types of exception test actions to test for
        /// </summary>
        internal enum ExceptionTestsAction
        {
            None,
            NullAction,
            InvalidTaskOption,
            NullTaskManager,
            MultipleTaskStart,
            StartOnPromise,
            StartOnContinueWith
        }
    }
}
