// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public partial class LifecycleTests
    {
        [Fact]
        public async Task StoppingConcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services
                    .AddHostedService<StoppingTestClass<Impl1>>()
                    .AddHostedService<StoppingTestClass<Impl2>>()
                    .Configure<HostOptions>(opts => opts.ServicesStopConcurrently = true);
            });

            using (IHost host = hostBuilder.Build())
            {
                StoppingTestClass<Impl1>.s_wait1.Wait();
                StoppingTestClass<Impl2>.s_wait1.Wait();
                StoppingTestClass<Impl1>.s_wait2.Wait();
                StoppingTestClass<Impl2>.s_wait2.Wait();
                StoppingTestClass<Impl1>.s_wait3.Wait();
                StoppingTestClass<Impl2>.s_wait3.Wait();

                await host.StartAsync();
                Verify(0, 0, 0, 0, 0, 0);

                // Both run serially until the await.
                Task stop = host.StopAsync();
                Verify(1, 1, 0, 0, 0, 0);
                await Task.Delay(s_superShortDelay);
                Verify(1, 1, 0, 0, 0, 0);

                // Resume and check that both are not finished.
                StoppingTestClass<Impl1>.s_wait1.Release();
                await StoppingTestClass<Impl1>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 0, 0, 0);

                StoppingTestClass<Impl2>.s_wait1.Release();
                await StoppingTestClass<Impl2>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 1, 0, 0);

                // Resume and verify they finish.
                StoppingTestClass<Impl1>.s_wait3.Release();
                StoppingTestClass<Impl2>.s_wait3.Release();
                await stop;
                Verify(1, 1, 1, 1, 1, 1);
                Assert.True(stop.IsCompleted);
            }

            void Verify(int initial1, int initial2, int pause1, int pause2, int final1, int final2)
            {
                Assert.Equal(initial1, StoppingTestClass<Impl1>.s_initialCount);
                Assert.Equal(initial2, StoppingTestClass<Impl2>.s_initialCount);
                Assert.Equal(pause1, StoppingTestClass<Impl1>.s_pauseCount);
                Assert.Equal(pause2, StoppingTestClass<Impl2>.s_pauseCount);
                Assert.Equal(final1, StoppingTestClass<Impl1>.s_finalCount);
                Assert.Equal(final2, StoppingTestClass<Impl2>.s_finalCount);
            }
        }

        private class StoppingTestClass<T> : IHostedLifecycleService
        {
            public static int s_initialCount = 0;
            public static int s_pauseCount = 0;
            public static int s_finalCount = 0;
            public static SemaphoreSlim? s_wait1 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait2 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait3 = new SemaphoreSlim(1);

            public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public async Task StoppingAsync(CancellationToken cancellationToken)
            {
                s_initialCount++;
                await s_wait1.WaitAsync();
                s_pauseCount++;
                s_wait2.Release();
                await s_wait3.WaitAsync();
                s_finalCount++;
            }
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task StopConcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.
                    AddHostedService<StopTestClass<Impl1>>().
                    AddHostedService<StopTestClass<Impl2>>().
                    Configure<HostOptions>(opts => opts.ServicesStopConcurrently = true);
            });

            using (IHost host = hostBuilder.Build())
            {
                StopTestClass<Impl1>.s_wait1.Wait();
                StopTestClass<Impl2>.s_wait1.Wait();
                StopTestClass<Impl1>.s_wait2.Wait();
                StopTestClass<Impl2>.s_wait2.Wait();
                StopTestClass<Impl1>.s_wait3.Wait();
                StopTestClass<Impl2>.s_wait3.Wait();

                await host.StartAsync();
                Verify(0, 0, 0, 0, 0, 0);

                // Both run serially until the await.
                Task stop = host.StopAsync();
                Verify(1, 1, 0, 0, 0, 0);
                await Task.Delay(s_superShortDelay);
                Verify(1, 1, 0, 0, 0, 0);

                // Resume and check that both are not finished.
                StopTestClass<Impl1>.s_wait1.Release();
                await StopTestClass<Impl1>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 0, 0, 0);

                StopTestClass<Impl2>.s_wait1.Release();
                await StopTestClass<Impl2>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 1, 0, 0);

                // Resume and verify they finish.
                StopTestClass<Impl1>.s_wait3.Release();
                StopTestClass<Impl2>.s_wait3.Release();
                await stop;
                Verify(1, 1, 1, 1, 1, 1);
                Assert.True(stop.IsCompleted);
            }

            void Verify(int initial1, int initial2, int pause1, int pause2, int final1, int final2)
            {
                Assert.Equal(initial1, StopTestClass<Impl1>.s_initialCount);
                Assert.Equal(initial2, StopTestClass<Impl2>.s_initialCount);
                Assert.Equal(pause1, StopTestClass<Impl1>.s_pauseCount);
                Assert.Equal(pause2, StopTestClass<Impl2>.s_pauseCount);
                Assert.Equal(final1, StopTestClass<Impl1>.s_finalCount);
                Assert.Equal(final2, StopTestClass<Impl2>.s_finalCount);
            }
        }

        private class StopTestClass<T> : IHostedLifecycleService
        {
            public static int s_initialCount = 0;
            public static int s_pauseCount = 0;
            public static int s_finalCount = 0;
            public static SemaphoreSlim? s_wait1 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait2 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait3 = new SemaphoreSlim(1);

            public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public async Task StopAsync(CancellationToken cancellationToken)
            {
                s_initialCount++;
                await s_wait1.WaitAsync();
                s_pauseCount++;
                s_wait2.Release();
                await s_wait3.WaitAsync();
                s_finalCount++;
            }
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task StopNonconcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.
                    AddHostedService<StopNonconcurrentTestClass<Impl1>>().
                    AddHostedService<StopNonconcurrentTestClass<Impl2>>().
                    Configure<HostOptions>(opts => opts.ServicesStopConcurrently = false);
            });

            using (IHost host = hostBuilder.Build())
            {
                StopNonconcurrentTestClass<Impl1>.s_wait.Wait();
                StopNonconcurrentTestClass<Impl2>.s_wait.Wait();

                await host.StartAsync();
                Verify(0, 0);

                // Both run serially in reverse order.
                Task stop = host.StopAsync();
                Verify(0, 1);
                await Task.Delay(s_superShortDelay);
                Verify(0, 1);

                // Resume and verify they finish.
                StopNonconcurrentTestClass<Impl1>.s_wait.Release();
                StopNonconcurrentTestClass<Impl2>.s_wait.Release();
                await stop;
                Verify(1, 1);
                Assert.True(stop.IsCompleted);
            }

            void Verify(int count1, int count2)
            {
                Assert.Equal(count1, StopNonconcurrentTestClass<Impl1>.s_count);
                Assert.Equal(count2, StopNonconcurrentTestClass<Impl2>.s_count);
            }
        }

        private class StopNonconcurrentTestClass<T> : IHostedLifecycleService
        {
            public static int s_count = 0;
            public static SemaphoreSlim? s_wait = new SemaphoreSlim(1);

            public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public async Task StopAsync(CancellationToken cancellationToken)
            {
                s_count++;
                await s_wait.WaitAsync();
            }
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task StoppedConcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services
                    .AddHostedService<StoppedTestClass<Impl1>>()
                    .AddHostedService<StoppedTestClass<Impl2>>()
                    .Configure<HostOptions>(opts => opts.ServicesStopConcurrently = true);
            });

            using (IHost host = hostBuilder.Build())
            {
                StoppedTestClass<Impl1>.s_wait1.Wait();
                StoppedTestClass<Impl2>.s_wait1.Wait();
                StoppedTestClass<Impl1>.s_wait2.Wait();
                StoppedTestClass<Impl2>.s_wait2.Wait();
                StoppedTestClass<Impl1>.s_wait3.Wait();
                StoppedTestClass<Impl2>.s_wait3.Wait();

                await host.StartAsync();
                Verify(0, 0, 0, 0, 0, 0);

                // Both run serially until the await.
                Task stop = host.StopAsync();
                Verify(1, 1, 0, 0, 0, 0);
                await Task.Delay(s_superShortDelay);
                Verify(1, 1, 0, 0, 0, 0);

                // Resume and check that both are not finished.
                StoppedTestClass<Impl1>.s_wait1.Release();
                await StoppedTestClass<Impl1>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 0, 0, 0);

                StoppedTestClass<Impl2>.s_wait1.Release();
                await StoppedTestClass<Impl2>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 1, 0, 0);

                // Resume and verify they finish.
                StoppedTestClass<Impl1>.s_wait3.Release();
                StoppedTestClass<Impl2>.s_wait3.Release();
                await stop;
                Verify(1, 1, 1, 1, 1, 1);
                Assert.True(stop.IsCompleted);
            }

            void Verify(int initial1, int initial2, int pause1, int pause2, int final1, int final2)
            {
                Assert.Equal(initial1, StoppedTestClass<Impl1>.s_initialCount);
                Assert.Equal(initial2, StoppedTestClass<Impl2>.s_initialCount);
                Assert.Equal(pause1, StoppedTestClass<Impl1>.s_pauseCount);
                Assert.Equal(pause2, StoppedTestClass<Impl2>.s_pauseCount);
                Assert.Equal(final1, StoppedTestClass<Impl1>.s_finalCount);
                Assert.Equal(final2, StoppedTestClass<Impl2>.s_finalCount);
            }
        }

        private class StoppedTestClass<T> : IHostedLifecycleService
        {
            public static int s_initialCount = 0;
            public static int s_pauseCount = 0;
            public static int s_finalCount = 0;
            public static SemaphoreSlim? s_wait1 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait2 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait3 = new SemaphoreSlim(1);

            public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public async Task StoppedAsync(CancellationToken cancellationToken)
            {
                s_initialCount++;
                await s_wait1.WaitAsync();
                s_pauseCount++;
                s_wait2.Release();
                await s_wait3.WaitAsync();
                s_finalCount++;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StopPhasesException(bool throwAfterAsyncCall)
        {
            ExceptionImpl impl = new(throwAfterAsyncCall: throwAfterAsyncCall, throwOnShutdown: true);
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddHostedService((token) => impl);
            });

            using (IHost host = hostBuilder.Build())
            {
                await host.StartAsync();
                AggregateException ex = await Assert.ThrowsAnyAsync<AggregateException>(async () => await host.StopAsync());

                Assert.True(impl.StartingCalled);
                Assert.True(impl.StartCalled);
                Assert.True(impl.StartedCalled);

                // An exception during a stop phase does not prevent the next ones from running.
                Assert.True(impl.StoppingCalled);
                Assert.True(impl.StopCalled);
                Assert.True(impl.StoppedCalled);

                Assert.Equal(3, ex.InnerExceptions.Count);
                Assert.Contains("(ThrowOnStopping)", ex.InnerExceptions[0].Message);
                Assert.Contains("(ThrowOnStop)", ex.InnerExceptions[1].Message);
                Assert.Contains("(ThrowOnStopped)", ex.InnerExceptions[2].Message);
            }
        }
    }
}
