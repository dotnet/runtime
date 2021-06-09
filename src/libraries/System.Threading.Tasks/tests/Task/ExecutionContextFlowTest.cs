// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/30122")]
        [Fact]
        public static async Task TaskDropsExecutionContextUponCompletion()
        {
            // Create a finalizable object that'll be referenced by captured ExecutionContext,
            // run a task and wait for it, and then hold on to that task while forcing GCs and finalizers.
            // We want to make sure that holding on to the resulting Task doesn't keep
            // that finalizable object alive.

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Task t = null;

            Thread runner = new Thread(() =>
            {
                var state = new InvokeActionOnFinalization { Action = () => tcs.SetResult() };
                var al = new AsyncLocal<object>(){ Value = state }; // ensure the object is stored in ExecutionContext
                t = Task.Run(() => { }); // run a task that'll capture EC
                al.Value = null;
            }) { IsBackground = true };

            runner.Start();
            runner.Join();

            await t; // wait for the task method to complete and clear out its state

            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60)); // finalizable object should have been collected and finalized
            }
            catch (Exception e)
            {
                Environment.FailFast("Look at the created dump", e);
            }

            GC.KeepAlive(t); // ensure the object is stored in the state machine
        }

        public static IEnumerable<object[]> TaskCompletionSourceDoesntCaptureExecutionContext_MemberData()
        {
            yield return new object[] { new Func<TaskCompletionSource<int>>(() => new TaskCompletionSource<int>()) };
            yield return new object[] { new Func<TaskCompletionSource<int>>(() => new TaskCompletionSource<int>(new object())) };
            yield return new object[] { new Func<TaskCompletionSource<int>>(() => new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)) };
            yield return new object[] { new Func<TaskCompletionSource<int>>(() => new TaskCompletionSource<int>(new object(), TaskCreationOptions.RunContinuationsAsynchronously)) };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        [MemberData(nameof(TaskCompletionSourceDoesntCaptureExecutionContext_MemberData))]
        public static async Task TaskCompletionSourceDoesntCaptureExecutionContext(Func<TaskCompletionSource<int>> tcsFactory)
        {
            // Create a finalizable object that'll be referenced by captured ExecutionContext,
            // create a TCS, and then hold on to that while forcing GCs and finalizers.
            // We want to make sure that holding on to the resulting TCS doesn't keep
            // that finalizable object alive.

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            TaskCompletionSource<int> t = null;
            await Task.Run(delegate // avoid any issues with the stack keeping the object alive
            {
                var state = new InvokeActionOnFinalization { Action = () => tcs.SetResult() };
                var al = new AsyncLocal<object> { Value = state }; // ensure the object is stored in ExecutionContext
                t = tcsFactory(); // create the TCS that shouldn't capture ExecutionContext
                al.Value = null;
            });

            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60)); // finalizable object should have been collected and finalized
            GC.KeepAlive(t); // ensure the TCS is stored in the state machine
        }

        private sealed class InlineTaskScheduler : TaskScheduler
        {
            protected override void QueueTask(Task task) => TryExecuteTask(task);
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => TryExecuteTask(task);
            protected override IEnumerable<Task> GetScheduledTasks() => null;
        }
    }
}
