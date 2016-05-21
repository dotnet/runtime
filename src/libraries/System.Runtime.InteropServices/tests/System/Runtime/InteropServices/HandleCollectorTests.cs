// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices
{
    public static class HandleCollectorTests
    {
        private const int LowLimitSize = 20;
        private const int HighLimitSize = 100000;

        [Fact]
        public static void NegativeInitialThresholdCtor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HandleCollector("NegativeInitial", -1));
        }

        [Fact]
        public static void NegateMaximumThresholdCtor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HandleCollector("NegativeMax", 0, -1));
        }

        [Fact]
        public static void InitialGreaterThanMaxThresholdCtor()
        {
            Assert.Throws<ArgumentException>(() => new HandleCollector("InitialGreaterThanMax", 100, 1));
        }

        [Fact]
        public static void SimplePropertyValidation()
        {
            string name = "ExampleName";
            int initial = 10;
            int max = 20;

            HandleCollector collector = new HandleCollector(name, initial, max);

            Assert.Equal(0, collector.Count);
            Assert.Equal(name, collector.Name);
            Assert.Equal(initial, collector.InitialThreshold);
            Assert.Equal(max, collector.MaximumThreshold);
        }

        [Fact]
        public static void NullNameCtor()
        {
            HandleCollector collector = new HandleCollector(null, 0, 0);
            Assert.Equal(string.Empty, collector.Name);
        }

        [Fact]
        public static void EmptyRemoval()
        {
            HandleCollector collector = new HandleCollector("EmptyRemoval", 10);
            Assert.Throws<InvalidOperationException>(() => collector.Remove());
        }

        [Fact]
        [OuterLoop]
        public static void CountOverflow()
        {
            HandleCollector collector = new HandleCollector("CountOverflow", int.MaxValue);
            for (int i = 0; i < Int32.MaxValue; i++)
            {
                collector.Add();
            }

            Assert.Throws<InvalidOperationException>(() => collector.Add());
        }

        [Fact]
        public static void TestHandleCollector()
        {
            Tuple<int, int, int> intialGcState = new Tuple<int, int, int>(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2));

            HandleCollector lowLimitCollector = new HandleCollector("LowLimit.Collector", LowLimitSize);
            for (int i = 0; i < LowLimitSize + 1; ++i)
            {
                HandleLimitTester hlt = new HandleLimitTester(lowLimitCollector);
            }

            Tuple<int, int, int> postLowLimitState = new Tuple<int, int, int>(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2));

            Assert.True(intialGcState.Item1 + intialGcState.Item2 + intialGcState.Item3 < postLowLimitState.Item1 + postLowLimitState.Item2 + postLowLimitState.Item3, "Low limit handle did not trigger a GC");

            HandleCollector highLimitCollector = new HandleCollector("HighLimit.Collector", HighLimitSize);
            for (int i = 0; i < HighLimitSize + 10; ++i)
            {
                HandleLimitTester hlt = new HandleLimitTester(highLimitCollector);
            }

            Tuple<int, int, int> postHighLimitState = new Tuple<int, int, int>(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2));

            Assert.True(postLowLimitState.Item1 + postLowLimitState.Item2 + postLowLimitState.Item3 < postHighLimitState.Item1 + postHighLimitState.Item2 + postHighLimitState.Item3, "High limit handle did not trigger a GC");
        }

        private sealed class HandleLimitTester
        {
            private HandleCollector _collector;

            internal HandleLimitTester(HandleCollector collector)
            {
                _collector = collector;
                _collector.Add();
                GC.KeepAlive(this);
            }

            ~HandleLimitTester()
            {
                _collector.Remove();
            }
        }
    }
}
