// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using CoreFXTestLibrary;

using System;

namespace System.Threading.Tasks.Test.Unit
{

    public sealed class ParallelForUnitTests
    {
        [Fact]
        public static void ParallelForTest0()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 0,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest1()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 10,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest2()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 10,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest3()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 1,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest4()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 1,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest5()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 2,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest6()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 2,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest7()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 97,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest8()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32)
            {
                Count = 97,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest9()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 0,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest10()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest11()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest12()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest13()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest14()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest15()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest16()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest17()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest18()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest19()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 0,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest20()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest21()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest22()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest23()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest24()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest25()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest26()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest27()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnArray, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest28()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 0,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest29()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest30()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest31()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest32()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest33()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest34()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest35()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest36()
        {
            TestParameters parameters = new TestParameters(API.ForeachOnList, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest37()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 0,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest38()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest39()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest40()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest41()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest42()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest43()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest44()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest45()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest46()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.None,
                StateOption = ActionWithState.None,
                LocalOption = ActionWithLocal.HasFinally,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForTest47()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 97,
                ParallelOption = WithParallelOption.WithDOP,
                StateOption = ActionWithState.Stop,
                LocalOption = ActionWithLocal.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }
    }
}
