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
            Assert.Throws<ArgumentNullException>(() => new HistogramAdvice<int>(explicitBucketBoundaries: null));

            HistogramAdvice<int> emptyExplicitBucketBoundariesAdvice = new HistogramAdvice<int>(explicitBucketBoundaries: Array.Empty<int>());

            Assert.NotNull(emptyExplicitBucketBoundariesAdvice.ExplicitBucketBoundaries);
            Assert.Empty(emptyExplicitBucketBoundariesAdvice.ExplicitBucketBoundaries);

            int[] singleExplicitBucketBoundary = new int[] { 0 };

            HistogramAdvice<int> singleExplicitBucketBoundaryAdvice = new HistogramAdvice<int>(singleExplicitBucketBoundary);

            Assert.Equal(singleExplicitBucketBoundary, singleExplicitBucketBoundaryAdvice.ExplicitBucketBoundaries);

            // Verify data cannot be mutated after construction
            singleExplicitBucketBoundary[0] = int.MaxValue;
            Assert.NotEqual(singleExplicitBucketBoundary, singleExplicitBucketBoundaryAdvice.ExplicitBucketBoundaries);

            int[] invalidBucketBoundariesNondistinctValues = new int[] { 0, 1, 2, 3, 4, 4 };

            Assert.Throws<ArgumentException>(() => new HistogramAdvice<int>(invalidBucketBoundariesNondistinctValues));

            int[] invalidBucketBoundariesOrdering = new int[] { 0, 1, 2, 3, 5, 4 };

            Assert.Throws<ArgumentException>(() => new HistogramAdvice<int>(invalidBucketBoundariesOrdering));
        }
    }
}
