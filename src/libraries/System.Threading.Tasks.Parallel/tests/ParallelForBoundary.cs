// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class ParallelForBoundaryUnitTests
    {
        [Theory]
        [OuterLoop]
        [InlineData(API.For64, StartIndexBase.Int16, -1000, 100, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Int16, 0, 5, WorkloadPattern.Random)]
        [InlineData(API.For64, StartIndexBase.Int16, 100, 100, WorkloadPattern.Similar)]

        [InlineData(API.For64, StartIndexBase.Int32, -1000, 5, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Int32, 0, 5, WorkloadPattern.Decreasing)]
        [InlineData(API.For64, StartIndexBase.Int32, 0, 5, WorkloadPattern.Similar)]
        [InlineData(API.For64, StartIndexBase.Int32, 0, 100, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Int32, 0, 100, WorkloadPattern.Random)]
        [InlineData(API.For64, StartIndexBase.Int32, 100, 5, WorkloadPattern.Decreasing)]
        [InlineData(API.For64, StartIndexBase.Int32, 100, 5, WorkloadPattern.Random)]
        [InlineData(API.For64, StartIndexBase.Int32, 100, 5, WorkloadPattern.Similar)]
        [InlineData(API.For64, StartIndexBase.Int32, 100, 100, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Int32, 1000, 5, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Int32, 1000, 5, WorkloadPattern.Similar)]
        [InlineData(API.For64, StartIndexBase.Int32, 1000, 100, WorkloadPattern.Decreasing)]
        [InlineData(API.For64, StartIndexBase.Int32, 1000, 100, WorkloadPattern.Random)]

        [InlineData(API.For64, StartIndexBase.Int64, -1000, 100, WorkloadPattern.Decreasing)]
        [InlineData(API.For64, StartIndexBase.Int64, -1000, 100, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Int64, -1000, 5, WorkloadPattern.Random)]
        [InlineData(API.For64, StartIndexBase.Int64, -100, 5, WorkloadPattern.Decreasing)]
        [InlineData(API.For64, StartIndexBase.Int64, -100, 5, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Int64, -100, 5, WorkloadPattern.Similar)]
        [InlineData(API.For64, StartIndexBase.Int64, -100, 100, WorkloadPattern.Random)]
        [InlineData(API.For64, StartIndexBase.Int64, -100, 100, WorkloadPattern.Similar)]

        [InlineData(API.For64, StartIndexBase.Zero, 0, 5, WorkloadPattern.Increasing)]
        [InlineData(API.For64, StartIndexBase.Zero, 0, 100, WorkloadPattern.Decreasing)]
        [InlineData(API.For64, StartIndexBase.Zero, 0, 100, WorkloadPattern.Similar)]
        [InlineData(API.For64, StartIndexBase.Zero, 100, 5, WorkloadPattern.Random)]

        [InlineData(API.For, StartIndexBase.Int16, -100, 5, WorkloadPattern.Similar)]
        [InlineData(API.For, StartIndexBase.Int16, -1000, 5, WorkloadPattern.Similar)]
        [InlineData(API.For, StartIndexBase.Int16, -1000, 100, WorkloadPattern.Random)]
        [InlineData(API.For, StartIndexBase.Int16, 0, 5, WorkloadPattern.Increasing)]
        [InlineData(API.For, StartIndexBase.Int16, 0, 100, WorkloadPattern.Decreasing)]
        [InlineData(API.For, StartIndexBase.Int16, 100, 5, WorkloadPattern.Increasing)]
        [InlineData(API.For, StartIndexBase.Int16, 100, 100, WorkloadPattern.Random)]
        [InlineData(API.For, StartIndexBase.Int16, 1000, 5, WorkloadPattern.Decreasing)]
        [InlineData(API.For, StartIndexBase.Int16, 1000, 100, WorkloadPattern.Similar)]

        [InlineData(API.For, StartIndexBase.Int32, -1000, 5, WorkloadPattern.Decreasing)]
        [InlineData(API.For, StartIndexBase.Int32, -100, 5, WorkloadPattern.Random)]
        [InlineData(API.For, StartIndexBase.Int32, -100, 100, WorkloadPattern.Decreasing)]
        [InlineData(API.For, StartIndexBase.Int32, -1000, 100, WorkloadPattern.Similar)]

        [InlineData(API.For, StartIndexBase.Zero, 0, 5, WorkloadPattern.Similar)]
        [InlineData(API.For, StartIndexBase.Zero, 100, 100, WorkloadPattern.Decreasing)]
        [InlineData(API.For, StartIndexBase.Zero, 100, 100, WorkloadPattern.Random)]
        [InlineData(API.For, StartIndexBase.Zero, 1000, 5, WorkloadPattern.Decreasing)]
        [InlineData(API.For, StartIndexBase.Zero, 1000, 5, WorkloadPattern.Random)]
        public static void TestFor_Boundary(API api, StartIndexBase startIndexBase, int startIndexOffset, int count, WorkloadPattern workloadPattern)
        {
            var parameters = new TestParameters(api, startIndexBase, startIndexOffset)
            {
                Count = count,
                WorkloadPattern = workloadPattern,
            };
            var test = new ParallelForTest(parameters);
            test.RealRun();
        }
    }
}
