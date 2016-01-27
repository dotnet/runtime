// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class ParallelForUnitTests
    {
        [Theory]
        [InlineData(API.For64, StartIndexBase.Int32, 0, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.For64, StartIndexBase.Int32, 10, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.For64, StartIndexBase.Int32, 10, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.For64, StartIndexBase.Int32, 1, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.For64, StartIndexBase.Int32, 1, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.For64, StartIndexBase.Int32, 2, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.For64, StartIndexBase.Int32, 2, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.For64, StartIndexBase.Int32, 97, WithParallelOption.None, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.For64, StartIndexBase.Int32, 97, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.None)]

        [InlineData(API.For, StartIndexBase.Zero, 0, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.For, StartIndexBase.Zero, 10, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.For, StartIndexBase.Zero, 10, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.For, StartIndexBase.Zero, 10, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.For, StartIndexBase.Zero, 1, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.For, StartIndexBase.Zero, 1, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.For, StartIndexBase.Zero, 2, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.For, StartIndexBase.Zero, 2, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.For, StartIndexBase.Zero, 97, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.For, StartIndexBase.Zero, 97, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.HasFinally)]

        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 0, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 10, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 10, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 1, WithParallelOption.None, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 1, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 2, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 2, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 97, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.ForeachOnArray, StartIndexBase.Zero, 97, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.None)]

        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 0, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 10, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 10, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 1, WithParallelOption.None, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 1, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 2, WithParallelOption.None, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 2, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 97, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.ForeachOnList, StartIndexBase.Zero, 97, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.HasFinally)]

        [InlineData(API.Foreach, StartIndexBase.Zero, 0, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 10, WithParallelOption.None, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 10, WithParallelOption.None, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 10, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 1, WithParallelOption.None, ActionWithState.None, ActionWithLocal.None)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 1, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.HasFinally)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 2, WithParallelOption.None, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 2, WithParallelOption.WithDOP, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 2, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.None)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 97, WithParallelOption.None, ActionWithState.None, ActionWithLocal.HasFinally)]
        [InlineData(API.Foreach, StartIndexBase.Zero, 97, WithParallelOption.WithDOP, ActionWithState.Stop, ActionWithLocal.None)]
        public static void ParrallelFor(API api, StartIndexBase startIndexBase, int count, WithParallelOption parallelOption, ActionWithState stateOption, ActionWithLocal localOption)
        {
            var parameters = new TestParameters(api, startIndexBase)
            {
                Count = count,
                ParallelOption = parallelOption,
                StateOption = stateOption,
                LocalOption = localOption
            };
            var test = new ParallelForTest(parameters);
            test.RealRun();
        }
    }
}
