// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using CoreFXTestLibrary;

using System;

namespace System.Threading.Tasks.Test.Unit
{
    public sealed class ParallelForBoundary
    {
        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary0()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int16, -100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary1()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int16, 0)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary2()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int16, 1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary3()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int16, 100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary4()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, -100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary5()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, -100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary6()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 0)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary7()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary8()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary9()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary10()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary11()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary12()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary13()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Zero, 1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary14()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int16, -1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary15()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int16, 100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary16()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 0)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary17()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 0)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary18()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary19()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary20()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary21()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary22()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary23()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary24()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary25()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Zero, 0)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary26()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Zero, 0)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary27()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int16, 0)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary28()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, -1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary29()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 0)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary30()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 0)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary31()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary32()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary33()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary34()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary35()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int32, 100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary36()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary37()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary38()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary39()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Int64, -100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary40()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Zero, 0)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary41()
        {
            TestParameters parameters = new TestParameters(API.For64, StartIndexBase.Zero, 100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary42()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary43()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, -100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary44()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, 1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary45()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int32, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary46()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int32, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary47()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int32, -1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary48()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 0)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary49()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 0)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary50()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 1000)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary51()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary52()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 100)
            {
                Count = 1000,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary53()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, -1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary54()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, -100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary55()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, 0)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary56()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, 1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary57()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, 100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary58()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int32, -1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary59()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int32, -100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary60()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 1000)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary61()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary62()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 100)
            {
                Count = 100,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary63()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, -1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary64()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, -100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary65()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, 0)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary66()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, 1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary67()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int16, 100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Increasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary68()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int32, -1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary69()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Int32, -100)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary70()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 0)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Similar,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary71()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Decreasing,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForBoundary72()
        {
            TestParameters parameters = new TestParameters(API.For, StartIndexBase.Zero, 1000)
            {
                Count = 5,
                WorkloadPattern = WorkloadPattern.Random,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }
    }
}
