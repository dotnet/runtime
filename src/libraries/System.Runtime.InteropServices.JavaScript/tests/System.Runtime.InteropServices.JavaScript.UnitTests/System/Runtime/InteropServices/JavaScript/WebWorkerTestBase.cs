// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class WebWorkerTestBase : IAsyncLifetime
    {
        const int TimeoutMilliseconds = 5000;

        public static bool _isWarmupDone;

        public async Task InitializeAsync()
        {
            if (_isWarmupDone)
            {
                return;
            }
            await Task.Delay(500);
            _isWarmupDone = true;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        protected CancellationTokenSource CreateTestCaseTimeoutSource([CallerMemberName] string memberName = "")
        {
            var start = DateTime.Now;
            var cts = new CancellationTokenSource(TimeoutMilliseconds);
            cts.Token.Register(() =>
            {
                var end = DateTime.Now;
                Console.WriteLine($"Unexpected test case {memberName} timeout after {end - start} ManagedThreadId:{Environment.CurrentManagedThreadId}");
            });
            return cts;
        }

        public static IEnumerable<object[]> GetTargetThreads()
        {
            return Enum.GetValues<ExecutorType>().Select(type => new object[] { new Executor(type) });
        }

        public static IEnumerable<object[]> GetSpecificTargetThreads()
        {
            yield return new object[] { new Executor(ExecutorType.JSWebWorker), new Executor(ExecutorType.Main) };
            yield break;
        }

        public static IEnumerable<object[]> GetTargetThreads2x()
        {
            return Enum.GetValues<ExecutorType>().SelectMany(
                type1 => Enum.GetValues<ExecutorType>().Select(
                    type2 => new object[] { new Executor(type1), new Executor(type2) }));
        }

        protected async Task ActionsInDifferentThreads<T>(Executor executor1, Executor executor2, Func<Task, TaskCompletionSource<T>, Task> e1Job, Func<T, Task> e2Job, CancellationTokenSource cts)
        {
            TaskCompletionSource<T> job1ReadyTCS = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource job2DoneTCS = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var e1Done = false;
            var e2Done = false;
            var e1Failed = false;
            Task e1;
            Task e2;
            T r1;

            async Task ActionsInDifferentThreads1()
            {
                try
                {
                    await e1Job(job2DoneTCS.Task, job1ReadyTCS);
                    if (!job1ReadyTCS.Task.IsCompleted)
                    {
                        job1ReadyTCS.SetResult(default);
                    }
                    await job2DoneTCS.Task;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ActionsInDifferentThreads1 failed\n" + ex);
                    job1ReadyTCS.SetResult(default);
                    e1Failed = true;
                    throw;
                }
                finally
                {
                    e1Done = true;
                }
            }

            async Task ActionsInDifferentThreads2()
            {
                try
                {
                    await e2Job(r1);
                }
                finally
                {
                    e2Done = true;
                }
            }


            e1 = executor1.Execute(ActionsInDifferentThreads1, cts.Token);
            r1 = await job1ReadyTCS.Task.ConfigureAwait(true);
            if (e1Failed || e1.IsFaulted)
            {
                await e1;
            }
            e2 = executor2.Execute(ActionsInDifferentThreads2, cts.Token);

            try
            {
                await e2;
                job2DoneTCS.SetResult();
                await e1;
            }
            catch (Exception ex)
            {
                job2DoneTCS.TrySetException(ex);
                if (ex is OperationCanceledException oce && cts.Token.IsCancellationRequested)
                {
                    throw;
                }
                Console.WriteLine("ActionsInDifferentThreads failed with: \n" + ex);
                if (!e1Done || !e2Done)
                {
                    Console.WriteLine("ActionsInDifferentThreads canceling!");
                    cts.Cancel();
                }
                throw;
            }
        }
    }
}
