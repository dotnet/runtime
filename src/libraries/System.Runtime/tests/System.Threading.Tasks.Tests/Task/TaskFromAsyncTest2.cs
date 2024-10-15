// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TaskFromAsyncTest.cs
//
// <OWNER>susanlwo</OWNER>
//
// Test class using UnitTestDriver that ensures that the FromAsync overload methods are tested

using Xunit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests.FromAsync
{
    public partial class TaskFromAsyncTests
    {
        #region Test Methods

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest0()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest3()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest4()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest7()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest8()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.TaskT, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest9()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest12()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest13()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.TaskT, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest14()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest17()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest18()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.TaskT, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest19()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.TaskT, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest20()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest22()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest23()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.TaskT, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest25()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.TaskT, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest27()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest30()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest33()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest34()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest35()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest38()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest39()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest40()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest43()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest44()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest45()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest47()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest48()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.TaskT, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest50()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.TaskT, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest52()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest55()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest56()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest59()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest60()
        {
            TestParameters parameters = new TestParameters(API.APM_T2, TaskType.Task, TaskType.Task, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest61()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest64()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest65()
        {
            TestParameters parameters = new TestParameters(API.APM_T3, TaskType.Task, TaskType.Task, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest66()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest69()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest70()
        {
            TestParameters parameters = new TestParameters(API.APM_T, TaskType.Task, TaskType.Task, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest71()
        {
            TestParameters parameters = new TestParameters(API.APM, TaskType.Task, TaskType.Task, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest72()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest74()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.NullEnd);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest75()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.Task, OverloadChoice.None, ErrorCase.Throwing);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskFromAsyncTest77()
        {
            TestParameters parameters = new TestParameters(API.IAsyncResult, TaskType.Task, TaskType.Task, OverloadChoice.WithTaskOption, ErrorCase.None);
            TaskFromAsyncTest test = new TaskFromAsyncTest(parameters);
            test.RealRun();
        }

        #endregion

    }
}
