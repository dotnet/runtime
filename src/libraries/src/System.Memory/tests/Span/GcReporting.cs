// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static class SpanGcReportingTests
    {
        /// <summary>
        /// This is a simple sanity test to check that GC reporting for Span is not completely broken, it is not meant to be
        /// comprehensive.
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void DelegateTest()
        {
            DelegateTest(100000, 10000);
        }

        /// <summary>
        /// Intended entry point for stress tests, with counts appropriate for GCStress=3. The counts in
        /// <see cref="DelegateTest"/> are too high for running that test in a reasonable amount of time. This one runs in a
        /// reasonable amount of time for a long-duration stress run.
        /// </summary>
        public static void DelegateTest_Stress()
        {
            DelegateTest(100, 100);
        }

        private static void DelegateTest(int iterationCount, int objectCount)
        {
            object[] objects = new object[objectCount];
            Random rng = new Random();
            var delegateTestCore =
                new DelegateTestCoreDelegate(DelegateTest_Core) +
                new DelegateTestCoreDelegate(DelegateTest_Core);

            for (int i = 0; i < iterationCount; i++)
            {
                DelegateTest_CreateSomeObjects(objects, rng);
                delegateTestCore(new Span<int>(new int[] { 1, 2, 3 }), objects, rng);
            }
        }

        private delegate void DelegateTestCoreDelegate(Span<int> span, object[] objects, Random rng);

        private static void DelegateTest_Core(Span<int> span, object[] objects, Random rng)
        {
            ReadOnlySpan<int> initialSpan = span;

            DelegateTest_CreateSomeObjects(objects, rng);

            int sum = 0;
            for (int i = 0; i < span.Length; ++i)
            {
                sum += span[i];
            }
            Assert.Equal(1 + 2 + 3, sum);

            Assert.True(span == initialSpan);
        }

        private static void DelegateTest_CreateSomeObjects(object[] objects, Random rng)
        {
            for (int i = 0; i < 100; ++i)
            {
                objects[rng.Next(objects.Length)] = new object();
            }
        }
    }
}
