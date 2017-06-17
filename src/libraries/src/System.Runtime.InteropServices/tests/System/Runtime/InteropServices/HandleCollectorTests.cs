// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices
{
    public class HandleCollectorTests
    {
        private const int LowLimitSize = 20;
        private const int HighLimitSize = 100000;

        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 10)]
        [InlineData("InitialThreshold", int.MaxValue)]
        public void Ctor_Name_InitialThreshold(string name, int initialThreshold)
        {
            var handleCollector = new HandleCollector(name, initialThreshold);
            Assert.Equal(0, handleCollector.Count);
            Assert.Equal(name ?? string.Empty, handleCollector.Name);
            Assert.Equal(initialThreshold, handleCollector.InitialThreshold);
            Assert.Equal(int.MaxValue, handleCollector.MaximumThreshold);
        }

        [Theory]
        [InlineData(null, 0, 0)]
        [InlineData("", 10, 15)]
        [InlineData("InitialThreshold", 1, 2)]
        public void Ctor_Name_InitialThreshold_MaximumThreshold(string name, int initialThreshold, int maximumThreshold)
        {
            var handleCollector = new HandleCollector(name, initialThreshold, maximumThreshold);
            Assert.Equal(0, handleCollector.Count);
            Assert.Equal(name ?? string.Empty, handleCollector.Name);
            Assert.Equal(initialThreshold, handleCollector.InitialThreshold);
            Assert.Equal(maximumThreshold, handleCollector.MaximumThreshold);
        }

        [Fact]
        public void Ctor_NegativeInitialThreshold_ThrowsArgumentOufORangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialThreshold", () => new HandleCollector("NegativeInitial", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialThreshold", () => new HandleCollector("NegativeInitial", -1, 0));
        }

        [Fact]
        public static void Ctor_NegativeMaximumThreshold_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maximumThreshold", () => new HandleCollector("NegativeMax", 0, -1));
        }

        [Fact]
        public static void Ctor_InitialThresholdGreaterThanMaximumThreshold_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new HandleCollector("InitialGreaterThanMax", 100, 1));
        }

        [Fact]
        public void AddRemove_AcrossMultipleGenerations_Success()
        {
            var collector = new HandleCollector("name", 0);
            collector.Add();
            Assert.Equal(1, collector.Count);

            collector.Add();
            Assert.Equal(2, collector.Count);

            collector.Add();
            Assert.Equal(3, collector.Count);

            collector.Remove();
            Assert.Equal(2, collector.Count);

            collector.Remove();
            Assert.Equal(1, collector.Count);

            collector.Remove();
            Assert.Equal(0, collector.Count);
        }

        [Fact]
        public static void Remove_EmptyCollection_ThrowsInvalidOperationException()
        {
            HandleCollector collector = new HandleCollector("EmptyRemoval", 10);
            Assert.Throws<InvalidOperationException>(() => collector.Remove());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot,"Reflects on private member handleCount")]
        public static void Add_Overflows_ThrowsInvalidOperationException()
        {
            HandleCollector collector = new HandleCollector("CountOverflow", int.MaxValue);

            // We could iterate here 2B times calling Add, but that often takes over 100s
            // Instead, for testing purposes, we reach into the HandleCollector via reflection
            // to make it think it's already been called int.MaxValue - 10 times.  We then
            // only have to call Add 10 times rather than int.MaxValue times, and the test
            // completes very quickly.  If we ever need to run the test on a platform that
            // doesn't support such reflection, we can revert to the super-long running test
            // or find another workaround.

            const int ToAdd = 10; // int.MaxValue
            {
                // Jump HandleCollector instance forward until it almost overflows
                TypeInfo type = typeof(HandleCollector).GetTypeInfo();
                FieldInfo handleCount =
                    type.GetDeclaredField("_handleCount") ?? // corefx
                    type.GetDeclaredField("handleCount");    // desktop
                Assert.NotNull(handleCount);
                handleCount.SetValue(collector, int.MaxValue - ToAdd);
            }

            for (int i = 0; i < ToAdd; i++)
            {
                collector.Add();
            }

            Assert.Throws<InvalidOperationException>(() => collector.Add());
        }

        [Fact]
        public static void TestHandleCollector()
        {
            (int gen0, int gen1, int gen2) initialGcState = (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

            HandleCollector lowLimitCollector = new HandleCollector("LowLimit.Collector", LowLimitSize);
            for (int i = 0; i < LowLimitSize + 1; ++i)
            {
                HandleLimitTester hlt = new HandleLimitTester(lowLimitCollector);
            }

            (int gen0, int gen1, int gen2) postLowLimitState = (GC.CollectionCount(0),GC.CollectionCount(1), GC.CollectionCount(2));

            Assert.True(initialGcState.gen0 + initialGcState.gen1 + initialGcState.gen2 < postLowLimitState.gen0 + postLowLimitState.gen1 + postLowLimitState.gen2, "Low limit handle did not trigger a GC");

            HandleCollector highLimitCollector = new HandleCollector("HighLimit.Collector", HighLimitSize);
            for (int i = 0; i < HighLimitSize + 10; ++i)
            {
                HandleLimitTester hlt = new HandleLimitTester(highLimitCollector);
            }

            (int gen0, int gen1, int gen2) postHighLimitState = (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

            Assert.True(postLowLimitState.gen0 + postLowLimitState.gen1 + postLowLimitState.gen2 < postHighLimitState.gen0 + postHighLimitState.gen1 + postHighLimitState.gen2, "High limit handle did not trigger a GC");
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

            ~HandleLimitTester() => _collector.Remove();
        }
    }
}
