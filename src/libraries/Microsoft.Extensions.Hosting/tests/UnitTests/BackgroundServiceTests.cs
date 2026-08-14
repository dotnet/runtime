// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class BackgroundServiceTests
    {
        public static bool IsThreadingAndRemoteExecutorSupported =>
            PlatformDetection.IsMultithreadingSupported && RemoteExecutor.IsSupported;

        [Fact]
        public void StartReturnsCompletedTask()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            var task = service.StartAsync(CancellationToken.None);

            Assert.True(task.IsCompleted);
            Assert.False(tcs.Task.IsCompleted);

            // Complete the task
            tcs.TrySetResult(null);
        }

        [Fact]
        public async Task StartCancelledThrowsTaskCanceledException()
        {
            var ct = new CancellationToken(true);
            var service = new TrackingBackgroundService();

            Task startTask = service.StartAsync(ct);

            Assert.True(startTask.IsCompleted);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecuteTask);
            Assert.True(service.ExecuteTask.IsCanceled);
            Assert.False(service.ExecuteInvocation.IsCompleted);
        }

        [Fact]
        public async Task StopAsyncWithoutStartAsyncNoops()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsyncStopsBackgroundService()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            await service.StartAsync(CancellationToken.None);

            Assert.False(service.ExecuteTask.IsCompleted);

            await service.StopAsync(CancellationToken.None);

            Assert.True(service.ExecuteTask.IsCompleted);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public async Task StopAsyncStopsEvenIfTaskNeverEnds()
        {
            var service = new IgnoreCancellationService();

            await service.StartAsync(CancellationToken.None);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await service.StopAsync(cts.Token);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public async Task StopAsyncThrowsIfCancellationCallbackThrows()
        {
            var service = new ThrowOnCancellationService();

            await service.StartAsync(CancellationToken.None);
            await service.WaitForExecuteTask;

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await Assert.ThrowsAsync<AggregateException>(() => service.StopAsync(cts.Token));

            Assert.Equal(2, service.TokenCalls);
        }

        [Fact]
        public async Task StartAsyncThenDisposeTriggersCancelledToken()
        {
            var service = new WaitForCancelledTokenService();

            await service.StartAsync(CancellationToken.None);

            service.Dispose();
        }

        [Fact]
        public async Task StartAsyncThenCancelShouldCancelExecutingTask()
        {
            var tokenSource = new CancellationTokenSource();

            var service = new WaitForCancelledTokenService();

            await service.StartAsync(tokenSource.Token);
            await service.WaitForExecuteTask;

            tokenSource.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(() => service.ExecutingTask);
        }

        [Fact]
        public void CreateAndDisposeShouldNotThrow()
        {
            var service = new WaitForCancelledTokenService();

            service.Dispose();
        }

        [Fact]
        public async Task StartSynchronousAndStop()
        {
            var tokenSource = new CancellationTokenSource();
            var service = new MySynchronousBackgroundService();

            // should not block the start thread;
            await service.StartAsync(tokenSource.Token);
            await service.WaitForExecuteTask;
            await service.StopAsync(CancellationToken.None);

            Assert.True(service.WaitForEndExecuteTask.IsCompleted);
        }

        [Fact]
        public async Task StartSynchronousExecuteShouldBeCancelable()
        {
            var tokenSource = new CancellationTokenSource();
            var service = new MySynchronousBackgroundService();

            await service.StartAsync(tokenSource.Token);
            await service.WaitForExecuteTask;

            tokenSource.Cancel();

            await service.WaitForEndExecuteTask;
        }

        [ConditionalTheory(typeof(BackgroundServiceTests), nameof(IsThreadingAndRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void ExecuteAsyncRunsWhenImmediatelyStoppedOrDisposed(bool dispose)
        {
            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["DOTNET_ThreadPool_UseWindowsThreadPool"] = "0";

            using var _ = RemoteExecutor.Invoke((string disposeString) =>
            {
                ThreadPool.GetMinThreads(out int originalMinWorkerThreads, out int originalMinCompletionPortThreads);
                ThreadPool.GetMaxThreads(out int originalMaxWorkerThreads, out int originalMaxCompletionPortThreads);
                Assert.True(ThreadPool.SetMinThreads(1, originalMinCompletionPortThreads));
                Assert.True(ThreadPool.SetMaxThreads(1, originalMaxCompletionPortThreads));

                using var blockerEntered = new ManualResetEventSlim();
                using var releaseBlocker = new ManualResetEventSlim();

                try
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        blockerEntered.Set();
                        releaseBlocker.Wait();
                    });
                    Assert.True(blockerEntered.Wait(RemoteExecutor.FailWaitTimeoutMilliseconds));

                    int startThreadId = Environment.CurrentManagedThreadId;
                    var service = new TrackingBackgroundService();
                    service.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

                    Task stopTask;
                    if (bool.Parse(disposeString))
                    {
                        service.Dispose();
                        stopTask = service.ExecuteTask;
                    }
                    else
                    {
                        stopTask = service.StopAsync(CancellationToken.None);
                    }

                    releaseBlocker.Set();
                    stopTask.GetAwaiter().GetResult();

                    (int invocationCount, int threadId, bool isThreadPoolThread, bool isCancellationRequested) =
                        service.ExecuteInvocation.GetAwaiter().GetResult();
                    Assert.Equal(1, invocationCount);
                    Assert.NotEqual(startThreadId, threadId);
                    Assert.True(isThreadPoolThread);
                    Assert.True(isCancellationRequested);
                }
                finally
                {
                    releaseBlocker.Set();
                    ThreadPool.SetMaxThreads(originalMaxWorkerThreads, originalMaxCompletionPortThreads);
                    ThreadPool.SetMinThreads(originalMinWorkerThreads, originalMinCompletionPortThreads);
                }
            }, dispose.ToString(), options);
        }

        private sealed class TrackingBackgroundService : BackgroundService
        {
            private readonly TaskCompletionSource<(int InvocationCount, int ThreadId, bool IsThreadPoolThread, bool IsCancellationRequested)> _executeInvocation =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _invocationCount;

            public Task<(int InvocationCount, int ThreadId, bool IsThreadPoolThread, bool IsCancellationRequested)> ExecuteInvocation => _executeInvocation.Task;

            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                _executeInvocation.SetResult((
                    Interlocked.Increment(ref _invocationCount),
                    Environment.CurrentManagedThreadId,
                    Thread.CurrentThread.IsThreadPoolThread,
                    stoppingToken.IsCancellationRequested));
                return Task.CompletedTask;
            }
        }

        private class WaitForCancelledTokenService : BackgroundService
        {
            private TaskCompletionSource<object> _waitForExecuteTask = new TaskCompletionSource<object>();

            public Task ExecutingTask { get; private set; }

            public Task WaitForExecuteTask => _waitForExecuteTask.Task;

            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                ExecutingTask = Task.Delay(Timeout.Infinite, stoppingToken);

                _waitForExecuteTask.TrySetResult(null);

                return ExecutingTask;
            }
        }

        private class ThrowOnCancellationService : BackgroundService
        {
            private TaskCompletionSource<object> _waitForExecuteTask = new TaskCompletionSource<object>();

            public Task WaitForExecuteTask => _waitForExecuteTask.Task;

            public int TokenCalls { get; set; }

            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                stoppingToken.Register(() =>
                {
                    TokenCalls++;
                    throw new InvalidOperationException();
                });

                stoppingToken.Register(() =>
                {
                    TokenCalls++;
                });

                _waitForExecuteTask.TrySetResult(null);

                return new TaskCompletionSource<object>().Task;
            }
        }

        private class IgnoreCancellationService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                return new TaskCompletionSource<object>().Task;
            }
        }

        private class MyBackgroundService : BackgroundService
        {
            private readonly Task _task;

            public MyBackgroundService(Task task)
            {
                _task = task;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                await ExecuteCore(stoppingToken);
            }

            private async Task ExecuteCore(CancellationToken stoppingToken)
            {
                var task = await Task.WhenAny(_task, Task.Delay(Timeout.Infinite, stoppingToken));

                await task;
            }
        }

        private class MySynchronousBackgroundService : BackgroundService
        {
            private TaskCompletionSource<object> _waitForExecuteTask = new TaskCompletionSource<object>();
            private TaskCompletionSource<object> _waitForEndExecuteTask = new TaskCompletionSource<object>();

            public Task WaitForExecuteTask => _waitForExecuteTask.Task;
            public Task WaitForEndExecuteTask => _waitForEndExecuteTask.Task;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                _waitForExecuteTask.TrySetResult(null);
                stoppingToken.WaitHandle.WaitOne();
                _waitForEndExecuteTask.TrySetResult(null);
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        }
    }
}
