// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.Metrics.Tests
{
    public class MetricsAdviceTests
    {
        [Fact]
        public void HistogramAdviceConstructionTest()
        {
            Assert.Throws<ArgumentNullException>(() => new InstrumentAdvice<int> { HistogramBucketBoundaries = null });

            InstrumentAdvice<int> emptyExplicitBucketBoundariesAdvice = new InstrumentAdvice<int> { HistogramBucketBoundaries = Array.Empty<int>() };

            Assert.NotNull(emptyExplicitBucketBoundariesAdvice.HistogramBucketBoundaries);
            Assert.Empty(emptyExplicitBucketBoundariesAdvice.HistogramBucketBoundaries);

            int[] singleExplicitBucketBoundary = new int[] { 0 };

            InstrumentAdvice<int> singleExplicitBucketBoundaryAdvice = new InstrumentAdvice<int> { HistogramBucketBoundaries = singleExplicitBucketBoundary };

            Assert.Equal(singleExplicitBucketBoundary, singleExplicitBucketBoundaryAdvice.HistogramBucketBoundaries);

            // Verify data cannot be mutated after construction
            singleExplicitBucketBoundary[0] = int.MaxValue;
            Assert.NotEqual(singleExplicitBucketBoundary, singleExplicitBucketBoundaryAdvice.HistogramBucketBoundaries);

            int[] invalidBucketBoundariesNondistinctValues = new int[] { 0, 1, 2, 3, 4, 4 };

            Assert.Throws<ArgumentException>(() => new InstrumentAdvice<int> { HistogramBucketBoundaries = invalidBucketBoundariesNondistinctValues });

            int[] invalidBucketBoundariesOrdering = new int[] { 0, 1, 2, 3, 5, 4 };

            Assert.Throws<ArgumentException>(() => new InstrumentAdvice<int> { HistogramBucketBoundaries = invalidBucketBoundariesOrdering });
        }
    }
}
