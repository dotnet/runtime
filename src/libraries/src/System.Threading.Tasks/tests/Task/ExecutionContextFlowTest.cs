// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class ExecutionContextFlowTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SuppressFlow_TaskCapturesContextAccordingly(bool suppressFlow)
        {
            Assert.False(ExecutionContext.IsFlowSuppressed());
            if (suppressFlow) ExecutionContext.SuppressFlow();
            try
            {
                var asyncLocal = new AsyncLocal<int>();
                Task.Factory.StartNew(() => asyncLocal.Value = 42, CancellationToken.None, TaskCreationOptions.None, new InlineTaskScheduler()).Wait();
                Assert.Equal(suppressFlow ? 42 : 0, asyncLocal.Value);
            }
            finally
            {
                if (suppressFlow) ExecutionContext.RestoreFlow();
            }
        }

        private sealed class InlineTaskScheduler : TaskScheduler
        {
            protected override void QueueTask(Task task) => TryExecuteTask(task);
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => TryExecuteTask(task);
            protected override IEnumerable<Task> GetScheduledTasks() => null;
        }
    }
}
