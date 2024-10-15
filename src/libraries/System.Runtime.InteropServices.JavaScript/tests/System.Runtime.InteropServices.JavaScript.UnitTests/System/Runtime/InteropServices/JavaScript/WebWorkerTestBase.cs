// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
                WebWorkerTestHelper.Log($"Unexpected test case {memberName} timeout after {end - start} ManagedThreadId:{Environment.CurrentManagedThreadId}");
            });
            return cts;
        }

        public static IEnumerable<object[]> GetTargetThreads()
        {
            return Enum.GetValues<ExecutorType>().Select(type => new object[] { new Executor(type) });
        }

        public static IEnumerable<object[]> GetBlockingFriendlyTargetThreads()
        {
            yield return new object[] { new Executor(ExecutorType.Main) };
            yield return new object[] { new Executor(ExecutorType.NewThread) };
            yield return new object[] { new Executor(ExecutorType.ThreadPool) };
            // JSWebWorker is missing here because JS can't resolve promises while blocked
        }

        public static IEnumerable<object[]> GetSpecificTargetThreads2x()
        {
            yield return new object[] { new Executor(ExecutorType.Main), new Executor(ExecutorType.Main) };
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
                    WebWorkerTestHelper.Log("ActionsInDifferentThreads1 failed\n" + ex);
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
                if (!e1Done || !e2Done)
                {
                    WebWorkerTestHelper.Log("ActionsInDifferentThreads canceling because of unexpected fail: \n" + ex);
                    cts.Cancel();
                }
                else
                {
                    WebWorkerTestHelper.Log("ActionsInDifferentThreads failed with: \n" + ex);
                }
                throw;
            }
        }

        static void LocalCtsIgnoringCall(Action<CancellationToken> action)
        {
            var cts = new CancellationTokenSource(8);
            try
            {
                action(cts.Token);
            }
            catch (OperationCanceledException exception)
            {
                if (exception.CancellationToken != cts.Token)
                {
                    throw;
                }
                /* ignore the local one */
            }
        }

        public static IEnumerable<NamedCall> BlockingCalls = new List<NamedCall>
        {
            // things that should NOT throw PNSE
            new NamedCall { IsBlocking = false, Name = "Console.WriteLine", Call = delegate (CancellationToken ct) { Console.WriteLine("Blocking"); }},
            new NamedCall { IsBlocking = false, Name = "Directory.GetCurrentDirectory", Call = delegate (CancellationToken ct) { Directory.GetCurrentDirectory(); }},
            new NamedCall { IsBlocking = false, Name = "CancellationTokenSource.ctor", Call = delegate (CancellationToken ct) {
                using var cts = new CancellationTokenSource(8);
            }},
            new NamedCall { IsBlocking = false, Name = "Task.Delay", Call = delegate (CancellationToken ct) {
                Task.Delay(30, ct);
            }},
            new NamedCall { IsBlocking = false, Name = "new Timer", Call = delegate (CancellationToken ct) {
                new Timer((_) => { }, null, 1, -1);
            }},
            new NamedCall { IsBlocking = false, Name = "JSType.DiscardNoWait", Call = delegate (CancellationToken ct) {
                WebWorkerTestHelper.Log("DiscardNoWait");
            }},

            // things which should throw PNSE on sync JSExport and JSWebWorker
            new NamedCall { IsBlocking = true, Name = "Task.Wait", Call = delegate (CancellationToken ct) { Task.Delay(30, ct).Wait(ct); }},
            new NamedCall { IsBlocking = true, Name = "Task.WaitAll", Call = delegate (CancellationToken ct) { Task.WaitAll(Task.Delay(30, ct)); }},
            new NamedCall { IsBlocking = true, Name = "Task.WaitAny", Call = delegate (CancellationToken ct) { Task.WaitAny(Task.Delay(30, ct)); }},
            new NamedCall { IsBlocking = true, Name = "ManualResetEventSlim.Wait", Call = delegate (CancellationToken ct) {
                using var mr = new ManualResetEventSlim(false);
                LocalCtsIgnoringCall(mr.Wait);
            }},
            new NamedCall { IsBlocking = true, Name = "SemaphoreSlim.Wait", Call = delegate (CancellationToken ct) {
                using var sem = new SemaphoreSlim(2);
                LocalCtsIgnoringCall(sem.Wait);
            }},
            new NamedCall { IsBlocking = true, Name = "Mutex.WaitOne", Call = delegate (CancellationToken ct) {
                using var mr = new ManualResetEventSlim(false);
                var mutex = new Mutex();
                var thread = new Thread(() => {
                    mutex.WaitOne();
                    mr.Set();
                    Thread.Sleep(50);
                    mutex.ReleaseMutex();
                });
                thread.Start();
                Thread.ForceBlockingWait(static (b) => ((ManualResetEventSlim)b).Wait(), mr);
                mutex.WaitOne();
            }},
        };

        public static IEnumerable<object[]> GetTargetThreadsAndBlockingCalls()
        {
            foreach (var type in Enum.GetValues<ExecutorType>())
            {
                foreach (var call in BlockingCalls)
                {
                    yield return new object[] { new Executor(type), call };
                }
            }
        }
    }
}
