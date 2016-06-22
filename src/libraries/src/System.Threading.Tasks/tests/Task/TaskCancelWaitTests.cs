// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;

namespace System.Threading.Tasks.Tests.CancelWait
{
    public sealed class TaskCancelWaitTestCases
    {
        #region Test Methods

        [Fact]
        public static void TaskCancelWait1()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, string.Empty, true);

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, string.Empty);

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait2()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait3()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "AttachedToParent");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Light, "LongRunning");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait4()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void TaskCancelWait5()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskCancelWait6()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Medium, "LongRunning, AttachedToParent");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Light, "None");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait7()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryLight, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskCancelWait8()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Light, "AttachedToParent");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "AttachedToParent");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "LongRunning");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskCancelWait9()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskCancelWait10()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait11()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.VeryLight, "RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Medium, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskCancelWait12()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait13()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Medium, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait14()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void TaskCancelWait15()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait16()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait17()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.VeryHeavy, "AttachedToParent");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Medium, "LongRunning");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void TaskCancelWait18()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait19()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait20()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "None");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryHeavy, "LongRunning");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Heavy, "None");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait21()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "AttachedToParent");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait22()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void TaskCancelWait23()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait24()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "None");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }
        [Fact]
        public static void TaskCancelWait25()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryLight, "AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.VeryLight, "LongRunning, AttachedToParent");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Cancel, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait26()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait27()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "AttachedToParent");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "AttachedToParent");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait28()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "None");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Light, "None");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Medium, "AttachedToParent");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait29()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "None");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait30()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Heavy, "AttachedToParent");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Light, "None");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait31()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait32()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait33()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Light, "None");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "None");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Light, "AttachedToParent");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait34()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryHeavy, "None");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait35()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait36()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "None");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait37()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.None, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait38()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait39()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "LongRunning");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait40()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Light, "AttachedToParent");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait41()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "None");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait42()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait43()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "None");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Light, "AttachedToParent");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, -1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait44()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait45()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "None");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Light, "None");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryLight, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait46()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait47()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Medium, "None");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryHeavy, "None");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait48()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "LongRunning");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "AttachedToParent");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Heavy, "None");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "None");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryLight, "None");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait49()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait50()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "None");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "LongRunning, AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Heavy, "None");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait51()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait52()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "AttachedToParent");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.VeryLight, "LongRunning");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait53()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait54()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "None");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryLight, "LongRunning");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Light, "LongRunning");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Medium, "None");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait55()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait56()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait57()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait58()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "None");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "None");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Heavy, "None");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait59()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait60()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "AttachedToParent");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait61()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "None");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Heavy, "LongRunning, AttachedToParent");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Medium, "LongRunning, AttachedToParent");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait62()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "None");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait63()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.VeryHeavy, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait64()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "None");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Medium, "LongRunning, AttachedToParent");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Light, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait65()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "None");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Medium, "LongRunning, AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "None");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Medium, "RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait66()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "None");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "None");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait67()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 0);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait68()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "LongRunning, AttachedToParent");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Heavy, "AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "None");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryLight, "LongRunning");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Light, "LongRunning, AttachedToParent");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait69()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "None");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait70()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "None");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Medium, "None");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Light, "LongRunning");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait71()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "AttachedToParent");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryLight, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Light, "RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait72()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "LongRunning, AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Light, "RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryLight, "LongRunning, AttachedToParent");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait73()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryHeavy, "None");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryHeavy, "AttachedToParent");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait74()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "AttachedToParent");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait75()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait76()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait77()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "LongRunning, AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.VeryLight, "LongRunning");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryHeavy, "None");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait78()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "None");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "None");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait79()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "LongRunning");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Medium, "LongRunning");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Heavy, "AttachedToParent");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 97);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait80()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait81()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryLight, "LongRunning");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait82()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "None");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Light, "None");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Light, "LongRunning");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryLight, "AttachedToParent");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Light, "RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait83()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "None");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait84()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait85()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait86()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning, AttachedToParent");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Light, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait87()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait88()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait89()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait90()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "None");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait91()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait92()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 197);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait93()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait94()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "None");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait95()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "AttachedToParent");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait96()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Medium, "LongRunning");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait97()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "None");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "LongRunning");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Medium, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait98()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait99()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait100()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, AttachedToParent");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Light, "AttachedToParent");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait101()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "None");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait102()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Light, "LongRunning");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryLight, "LongRunning, AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Medium, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait103()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Light, "AttachedToParent");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Medium, "AttachedToParent");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait104()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait105()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait106()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "LongRunning");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait107()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "LongRunning, AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Medium, "LongRunning");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Light, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait108()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "None");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryHeavy, "LongRunning");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Heavy, "LongRunning, AttachedToParent");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Heavy, "None");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait109()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "None");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "None");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "None");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Light, "AttachedToParent");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Light, "LongRunning");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait110()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait111()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryLight, "LongRunning");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait112()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "LongRunning");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.VeryLight, "None");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait113()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait114()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait115()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait116()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Medium, "AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.VeryLight, "RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait117()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "None");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Light, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 1);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait118()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryLight, "RespectParentCancellation");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Heavy, "RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryLight, "AttachedToParent");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait119()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Heavy, "None");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Medium, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait120()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait121()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait122()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait123()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "None");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait124()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "None");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait125()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait126()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "None");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait127()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Medium, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Medium, "LongRunning");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Light, "RespectParentCancellation");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait128()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Heavy, "None");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait129()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "LongRunning");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Light, "RespectParentCancellation");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryHeavy, "LongRunning");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Heavy, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait130()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.VeryLight, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.Millisecond, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait131()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.VeryHeavy, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.VeryHeavy, "RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Heavy, "LongRunning, AttachedToParent");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait132()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning, AttachedToParent");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Medium, "AttachedToParent");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait133()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "LongRunning, AttachedToParent");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryLight, "LongRunning, RespectParentCancellation");

            TaskInfo node_4 = new TaskInfo(node, "node_4", WorkloadType.VeryLight, "LongRunning");

            TaskInfo node_5 = new TaskInfo(node, "node_5", WorkloadType.Light, "LongRunning");

            TaskInfo node_6 = new TaskInfo(node, "node_6", WorkloadType.VeryHeavy, "RespectParentCancellation");

            TaskInfo node_7 = new TaskInfo(node, "node_7", WorkloadType.Light, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, node_3, node_4, node_5, node_6, node_7, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait134()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait135()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "LongRunning");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "None");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "RespectParentCancellation");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "None");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Heavy, "LongRunning");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Light, "LongRunning");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait136()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryHeavy, "LongRunning, RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Light, "RespectParentCancellation");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.Medium, "LongRunning, AttachedToParent");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.Light, "LongRunning, AttachedToParent");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Light, "LongRunning, AttachedToParent");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.VeryLight, "RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait137()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.Heavy, "RespectParentCancellation");

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait138()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Light, "None");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryLight, "AttachedToParent");
            TaskInfo node_1_2 = new TaskInfo(node_1, "node_1_2", WorkloadType.Light, "None");
            node_1.AddChildren(new[] { node_1_1, node_1_2, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_1 = new TaskInfo(node_2, "node_2_1", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            TaskInfo node_2_2 = new TaskInfo(node_2, "node_2_2", WorkloadType.Medium, "AttachedToParent");
            node_2.AddChildren(new[] { node_2_1, node_2_2, });

            node.AddChildren(new[] { node_1, node_2, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait139()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.VeryLight, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "LongRunning, AttachedToParent");
            TaskInfo node_1_1 = new TaskInfo(node_1, "node_1_1", WorkloadType.VeryHeavy, "None");
            node_1.AddChildren(new[] { node_1_1, });

            TaskInfo node_2 = new TaskInfo(node, "node_2", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_3 = new TaskInfo(node, "node_3", WorkloadType.VeryHeavy, "LongRunning");
            TaskInfo node_3_1 = new TaskInfo(node_3, "node_3_1", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");
            TaskInfo node_3_2 = new TaskInfo(node_3, "node_3_2", WorkloadType.Light, "LongRunning, RespectParentCancellation");
            node_3.AddChildren(new[] { node_3_1, node_3_2, });

            node.AddChildren(new[] { node_1, node_2, node_3, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait140()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Medium, "RespectParentCancellation");

            TaskInfo node_1 = new TaskInfo(node, "node_1", WorkloadType.VeryHeavy, "RespectParentCancellation");

            node.AddChildren(new[] { node_1, });
            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskCancelWait141()
        {
            TaskInfo node = new TaskInfo(null, "node", WorkloadType.Heavy, "LongRunning, RespectParentCancellation");

            TestParameters parameters = new TestParameters(node, API.Wait, WaitBy.TimeSpan, 27);

            TaskCancelWaitTest test = new TaskCancelWaitTest(parameters);
            test.RealRun();
        }

        #endregion
    }
}
