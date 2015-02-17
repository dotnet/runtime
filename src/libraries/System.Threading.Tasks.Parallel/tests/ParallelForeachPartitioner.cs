// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using CoreFXTestLibrary;

using System;

namespace System.Threading.Tasks.Test.Unit
{

    public sealed class ParallelForeachPartitioner
    {
        [Fact]
        public static void ParallelForeachPartitioner0()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ChunkSize = -1,
                PartitionerType = PartitionerType.IListBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForeachPartitioner1()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ChunkSize = 10,
                PartitionerType = PartitionerType.IEnumerableOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner2()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ChunkSize = 1,
                PartitionerType = PartitionerType.ArrayBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner3()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ChunkSize = 3,
                PartitionerType = PartitionerType.RangePartitioner,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner4()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 10,
                ChunkSize = 97,
                PartitionerType = PartitionerType.RangePartitioner,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner5()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ChunkSize = -1,
                PartitionerType = PartitionerType.ArrayBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner6()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ChunkSize = -1,
                PartitionerType = PartitionerType.RangePartitioner,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner7()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ChunkSize = 10,
                PartitionerType = PartitionerType.IListBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner8()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ChunkSize = 1,
                PartitionerType = PartitionerType.IEnumerableOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner9()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ChunkSize = 3,
                PartitionerType = PartitionerType.IEnumerableOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelForeachPartitioner10()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 1,
                ChunkSize = 97,
                PartitionerType = PartitionerType.IEnumerableOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner11()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ChunkSize = -1,
                PartitionerType = PartitionerType.IEnumerableOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner12()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ChunkSize = 10,
                PartitionerType = PartitionerType.ArrayBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner13()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ChunkSize = 1,
                PartitionerType = PartitionerType.RangePartitioner,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner14()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ChunkSize = 3,
                PartitionerType = PartitionerType.IListBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner15()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 2,
                ChunkSize = 97,
                PartitionerType = PartitionerType.IListBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner16()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 97,
                ChunkSize = -1,
                PartitionerType = PartitionerType.IEnumerableOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.None,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner17()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 97,
                ChunkSize = 10,
                PartitionerType = PartitionerType.RangePartitioner,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.None,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner18()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 97,
                ChunkSize = 1,
                PartitionerType = PartitionerType.IListBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.None,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner19()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 97,
                ChunkSize = 3,
                PartitionerType = PartitionerType.ArrayBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelForeachPartitioner20()
        {
            TestParameters parameters = new TestParameters(API.Foreach, StartIndexBase.Zero)
            {
                Count = 97,
                ChunkSize = 97,
                PartitionerType = PartitionerType.ArrayBalancedOOB,
                ParallelForeachDataSourceType = DataSourceType.Partitioner,
                ParallelOption = WithParallelOption.WithDOP,
                LocalOption = ActionWithLocal.HasFinally,
                StateOption = ActionWithState.Stop,
            };
            ParallelForTest test = new ParallelForTest(parameters);
            test.RealRun();
        }
    }
}
