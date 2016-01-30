// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class ParallelForeachPartitionerTests
    {
        [Theory]
        [OuterLoop]
        [InlineData(10, 1, PartitionerType.ArrayBalancedOOB, WithParallelOption.None, ActionWithLocal.None, ActionWithState.None)]
        [InlineData(1, -1, PartitionerType.ArrayBalancedOOB, WithParallelOption.None, ActionWithLocal.None, ActionWithState.None)]
        [InlineData(97, 3, PartitionerType.ArrayBalancedOOB, WithParallelOption.WithDOP, ActionWithLocal.HasFinally, ActionWithState.Stop)]
        [InlineData(97, 97, PartitionerType.ArrayBalancedOOB, WithParallelOption.WithDOP, ActionWithLocal.HasFinally, ActionWithState.Stop)]
        [InlineData(2, 10, PartitionerType.ArrayBalancedOOB, WithParallelOption.WithDOP, ActionWithLocal.None, ActionWithState.Stop)]

        [InlineData(10, 10, PartitionerType.IEnumerableOOB, WithParallelOption.WithDOP, ActionWithLocal.None, ActionWithState.Stop)]
        [InlineData(1, 1, PartitionerType.IEnumerableOOB, WithParallelOption.None, ActionWithLocal.HasFinally, ActionWithState.Stop)]
        [InlineData(1, 3, PartitionerType.IEnumerableOOB, WithParallelOption.WithDOP, ActionWithLocal.None, ActionWithState.None)]
        [InlineData(1, 97, PartitionerType.IEnumerableOOB, WithParallelOption.None, ActionWithLocal.None, ActionWithState.None)]
        [InlineData(2, -1, PartitionerType.IEnumerableOOB, WithParallelOption.None, ActionWithLocal.None, ActionWithState.None)]
        [InlineData(97, -1, PartitionerType.IEnumerableOOB, WithParallelOption.WithDOP, ActionWithLocal.None, ActionWithState.None)]

        [InlineData(10, -1, PartitionerType.IListBalancedOOB, WithParallelOption.WithDOP, ActionWithLocal.HasFinally, ActionWithState.None)]
        [InlineData(1, 10, PartitionerType.IListBalancedOOB, WithParallelOption.None, ActionWithLocal.None, ActionWithState.Stop)]
        [InlineData(2, 3, PartitionerType.IListBalancedOOB, WithParallelOption.None, ActionWithLocal.HasFinally, ActionWithState.None)]
        [InlineData(2, 97, PartitionerType.IListBalancedOOB, WithParallelOption.WithDOP, ActionWithLocal.None, ActionWithState.Stop)]
        [InlineData(97, 1, PartitionerType.IListBalancedOOB, WithParallelOption.None, ActionWithLocal.HasFinally, ActionWithState.Stop)]
        
        [InlineData(10, 3, PartitionerType.RangePartitioner, WithParallelOption.WithDOP, ActionWithLocal.None, ActionWithState.None)]
        [InlineData(10, 97, PartitionerType.RangePartitioner, WithParallelOption.None, ActionWithLocal.HasFinally, ActionWithState.Stop)]
        [InlineData(1, -1, PartitionerType.RangePartitioner, WithParallelOption.WithDOP, ActionWithLocal.HasFinally, ActionWithState.Stop)]
        [InlineData(2, 1, PartitionerType.RangePartitioner, WithParallelOption.WithDOP, ActionWithLocal.HasFinally, ActionWithState.None)]
        [InlineData(97, 10, PartitionerType.RangePartitioner, WithParallelOption.None, ActionWithLocal.HasFinally, ActionWithState.None)]
        public static void TestForeach_Partitioner(int count, int chunkSize, PartitionerType partitionerType, WithParallelOption parallelOption, ActionWithLocal localOption, ActionWithState stateOption)
        {
            var parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = count,
                ChunkSize = chunkSize,
                PartitionerType = partitionerType,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = parallelOption,
                LocalOption = localOption,
                StateOption = stateOption,
            };
            var test = new ParallelForTest(parameters);
            test.RealRun();
        }
    }
}
