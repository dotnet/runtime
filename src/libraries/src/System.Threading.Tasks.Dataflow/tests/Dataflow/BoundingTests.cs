// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        //[Fact(Skip = "Outerloop")]
        public void RunBoundingTests()
        {
            var options = new DataflowBlockOptions() { BoundedCapacity = ITargetBlockTestHelper.BOUNDED_CAPACITY };
            var executionOptions = new ExecutionDataflowBlockOptions() { BoundedCapacity = ITargetBlockTestHelper.BOUNDED_CAPACITY };
            var greedyOptions = new GroupingDataflowBlockOptions() { BoundedCapacity = ITargetBlockTestHelper.BOUNDED_CAPACITY, Greedy = true };
            var nonGreedyOptions = new GroupingDataflowBlockOptions() { BoundedCapacity = ITargetBlockTestHelper.BOUNDED_CAPACITY, Greedy = false };

            // "Normal" target blocks
            Assert.True(ITargetBlockTestHelper.TestBoundingTarget<int, int>(new ActionBlock<int>((Action<int>)ITargetBlockTestHelper.BoundingAction, executionOptions), greedy: true));

            // BatchBlock
            Assert.True(ITargetBlockTestHelper.TestBoundingTarget<int, int[]>(new BatchBlock<int>(ITargetBlockTestHelper.BOUNDED_CAPACITY, greedyOptions), greedy: true));
            Assert.True(ITargetBlockTestHelper.TestBoundingTarget<int, int[]>(new BatchBlock<int>(ITargetBlockTestHelper.BOUNDED_CAPACITY, nonGreedyOptions), greedy: false));

            // JoinBlock
            Assert.True(ITargetBlockTestHelper.TestBoundingJoin2<int>(new JoinBlock<int, int>(greedyOptions), greedy: true));
            Assert.True(ITargetBlockTestHelper.TestBoundingJoin3<int>(new JoinBlock<int, int, int>(nonGreedyOptions), greedy: false));

            // JoinBlock.Target
            Assert.True(ITargetBlockTestHelper.TestBoundingGreedyJoinTarget2<int>(new JoinBlock<int, int>(greedyOptions), testedTargetIndex: 1));
            Assert.True(ITargetBlockTestHelper.TestBoundingGreedyJoinTarget3<int>(new JoinBlock<int, int, int>(greedyOptions), testedTargetIndex: 2));
        }
    }
}
