// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xunit.Performance;

namespace System.Linq.Parallel.Tests
{
    public sealed class JoinPerfTestsUnorderedLeftUnorderedRight : JoinPerfTests
    {
    }

    public sealed class JoinPerfTestsUnorderedLeftOrderedRight : JoinPerfTests
    {
        protected override ParallelQuery<int> CreateRight(int count) => base.CreateRight(count).AsOrdered();
    }

    public sealed class JoinPerfTestsOrderedLeftUnorderedRight : JoinPerfTests
    {
        protected override ParallelQuery<int> CreateLeft(int count) => base.CreateLeft(count).AsOrdered();
    }

    public sealed class JoinPerfTestsShuffledOrderedLeftUnorderedRight : JoinPerfTests
    {
        protected override ParallelQuery<int> CreateLeft(int count) => base.CreateLeft(count).OrderBy(x => x);
    }

    public sealed class JoinPerfTestsOrderedLeftOrderedRight : JoinPerfTests
    {
        protected override ParallelQuery<int> CreateLeft(int count) => base.CreateLeft(count).AsOrdered();
        protected override ParallelQuery<int> CreateRight(int count) => base.CreateRight(count).AsOrdered();

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct__1000()
        {
            CrossProduct(1000);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct__2000()
        {
            CrossProduct(2000);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct_10000()
        {
            CrossProduct(10000);
        }
    }

    public sealed class JoinPerfTestsShuffledOrderedLeftOrderedRight : JoinPerfTests
    {
        protected override ParallelQuery<int> CreateLeft(int count) => base.CreateLeft(count).OrderBy(x => x);
        protected override ParallelQuery<int> CreateRight(int count) => base.CreateRight(count).AsOrdered();

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct__1000()
        {
            CrossProduct(1000);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct__2000()
        {
            CrossProduct(2000);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct_10000()
        {
            CrossProduct(10000);
        }
    }

    public abstract class JoinPerfTests
    {
        const int TotalElementCount = 50_000;
        protected const int CrossProductInnerIterationCount = 100;

        protected virtual ParallelQuery<int> CreateLeft(int count) => UnorderedSources.Default(count);
        protected virtual ParallelQuery<int> CreateRight(int count) => UnorderedSources.Default(count);

        private ParallelQuery<KeyValuePair<int, int>> CreateQuery(int leftCount, int rightsPerLeft)
        {
            return CreateLeft(leftCount).Join(CreateRight(leftCount * rightsPerLeft),
                x => x, y => y % leftCount, (x, y) => KeyValuePair.Create(x, y));
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public void QueryCreation()
        {
            QueryCreation(10);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct____10()
        {
            CrossProduct(10);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct____25()
        {
            CrossProduct(25);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct____50()
        {
            CrossProduct(50);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct___100()
        {
            CrossProduct(100);
        }

        [Benchmark(InnerIterationCount = CrossProductInnerIterationCount), MeasureGCAllocations]
        public void CrossProduct___500()
        {
            CrossProduct(500);
        }

        public void QueryCreation(int rightsPerLeft)
        {
            Debug.Assert(TotalElementCount % rightsPerLeft == 0);
            QueryCreation(TotalElementCount / rightsPerLeft, rightsPerLeft);
        }

        private static volatile ParallelQuery<KeyValuePair<int, int>> _queryCreationResult;

        public void QueryCreation(int leftCount, int rightsPerLeft)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        _queryCreationResult = CreateQuery(leftCount, rightsPerLeft);
                    }
                }
            }
        }

        public void CrossProduct(int rightsPerLeft)
        {
            Debug.Assert(TotalElementCount % rightsPerLeft == 0);
            CrossProduct(TotalElementCount / rightsPerLeft, rightsPerLeft);
        }

        private static volatile int _crossProductResult;

        public void CrossProduct(int leftCount, int rightsPerLeft)
        {
            ParallelQuery<KeyValuePair<int, int>> values = CreateQuery(leftCount, rightsPerLeft);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        _crossProductResult = 0;
                        foreach (KeyValuePair<int, int> pair in values)
                        {
                            _crossProductResult += pair.Key * pair.Value;
                        }
                    }
                }
            }
        }
    }
}
