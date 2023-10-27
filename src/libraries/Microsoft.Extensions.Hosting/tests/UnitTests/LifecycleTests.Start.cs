// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public partial class LifecycleTests
    {
        [Fact]
        public async Task StartingConcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services
                    .AddHostedService<StartingTestClass<Impl1>>()
                    .AddHostedService<StartingTestClass<Impl2>>()
                    .Configure<HostOptions>(opts => opts.ServicesStartConcurrently = true);
            });

            using (IHost host = hostBuilder.Build())
            {
                StartingTestClass<Impl1>.s_wait1.Wait();
                StartingTestClass<Impl2>.s_wait1.Wait();
                StartingTestClass<Impl1>.s_wait2.Wait();
                StartingTestClass<Impl2>.s_wait2.Wait();
                StartingTestClass<Impl1>.s_wait3.Wait();
                StartingTestClass<Impl2>.s_wait3.Wait();

                Verify(0, 0, 0, 0, 0, 0);

                // Both run serially until the await.
                Task start = host.StartAsync();
                Verify(1, 1, 0, 0, 0, 0);
                await Task.Delay(s_superShortDelay);
                Verify(1, 1, 0, 0, 0, 0);

                // Resume and check that both are not finished.
                StartingTestClass<Impl1>.s_wait1.Release();
                await StartingTestClass<Impl1>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 0, 0, 0);

                StartingTestClass<Impl2>.s_wait1.Release();
                await StartingTestClass<Impl2>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 1, 0, 0);

                // Resume and verify they finish.
                StartingTestClass<Impl1>.s_wait3.Release();
                StartingTestClass<Impl2>.s_wait3.Release();
                await start;
                Verify(1, 1, 1, 1, 1, 1);
                Assert.True(start.IsCompleted);
            }

            void Verify(int initial1, int initial2, int pause1, int pause2, int final1, int final2)
            {
                Assert.Equal(initial1, StartingTestClass<Impl1>.s_initialCount);
                Assert.Equal(initial2, StartingTestClass<Impl2>.s_initialCount);
                Assert.Equal(pause1, StartingTestClass<Impl1>.s_pauseCount);
                Assert.Equal(pause2, StartingTestClass<Impl2>.s_pauseCount);
                Assert.Equal(final1, StartingTestClass<Impl1>.s_finalCount);
                Assert.Equal(final2, StartingTestClass<Impl2>.s_finalCount);
            }
        }

        private class StartingTestClass<T> : IHostedLifecycleService
        {
            public static int s_initialCount = 0;
            public static int s_pauseCount = 0;
            public static int s_finalCount = 0;
            public static SemaphoreSlim? s_wait1 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait2 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait3 = new SemaphoreSlim(1);

            public async Task StartingAsync(CancellationToken cancellationToken)
            {
                s_initialCount++;
                await s_wait1.WaitAsync();
                s_pauseCount++;
                s_wait2.Release();
                await s_wait3.WaitAsync();
                s_finalCount++;
            }
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task StartConcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.
                    AddHostedService<StartTestClass<Impl1>>().
                    AddHostedService<StartTestClass<Impl2>>().
                    Configure<HostOptions>(opts => opts.ServicesStartConcurrently = true);
            });

            using (IHost host = hostBuilder.Build())
            {
                StartTestClass<Impl1>.s_wait1.Wait();
                StartTestClass<Impl2>.s_wait1.Wait();
                StartTestClass<Impl1>.s_wait2.Wait();
                StartTestClass<Impl2>.s_wait2.Wait();
                StartTestClass<Impl1>.s_wait3.Wait();
                StartTestClass<Impl2>.s_wait3.Wait();

                Verify(0, 0, 0, 0, 0, 0);

                // Both run serially until the await.
                Task start = host.StartAsync();
                Verify(1, 1, 0, 0, 0, 0);
                Verify(1, 1, 0, 0, 0, 0);

                // Resume and check that both are not finished.
                StartTestClass<Impl1>.s_wait1.Release();
                await StartTestClass<Impl1>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 0, 0, 0);

                StartTestClass<Impl2>.s_wait1.Release();
                await StartTestClass<Impl2>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 1, 0, 0);

                // Resume and verify they finish.
                StartTestClass<Impl1>.s_wait3.Release();
                StartTestClass<Impl2>.s_wait3.Release();
                await start;
                Verify(1, 1, 1, 1, 1, 1);
                Assert.True(start.IsCompleted);
            }

            void Verify(int initial1, int initial2, int pause1, int pause2, int final1, int final2)
            {
                Assert.Equal(initial1, StartTestClass<Impl1>.s_initialCount);
                Assert.Equal(initial2, StartTestClass<Impl2>.s_initialCount);
                Assert.Equal(pause1, StartTestClass<Impl1>.s_pauseCount);
                Assert.Equal(pause2, StartTestClass<Impl2>.s_pauseCount);
                Assert.Equal(final1, StartTestClass<Impl1>.s_finalCount);
                Assert.Equal(final2, StartTestClass<Impl2>.s_finalCount);
            }
        }

        private class StartTestClass<T> : IHostedLifecycleService
        {
            public static int s_initialCount = 0;
            public static int s_pauseCount = 0;
            public static int s_finalCount = 0;
            public static SemaphoreSlim? s_wait1 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait2 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait3 = new SemaphoreSlim(1);

            public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public async Task StartAsync(CancellationToken cancellationToken)
            {
                s_initialCount++;
                await s_wait1.WaitAsync();
                s_pauseCount++;
                s_wait2.Release();
                await s_wait3.WaitAsync();
                s_finalCount++;
            }
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task StartNonconcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.
                    AddHostedService<StartNonconcurrentTestClass<Impl1>>().
                    AddHostedService<StartNonconcurrentTestClass<Impl2>>().
                    Configure<HostOptions>(opts => opts.ServicesStartConcurrently = false);
            });

            using (IHost host = hostBuilder.Build())
            {
                StartNonconcurrentTestClass<Impl1>.s_wait.Wait();
                StartNonconcurrentTestClass<Impl2>.s_wait.Wait();

                Verify(0, 0);

                // Both run serially.
                Task start = host.StartAsync();
                Verify(1, 0);
                await Task.Delay(s_superShortDelay);
                Verify(1, 0);

                // Resume and verify they finish.
                StartNonconcurrentTestClass<Impl1>.s_wait.Release();
                StartNonconcurrentTestClass<Impl2>.s_wait.Release();
                await start;
                Verify(1, 1);
                Assert.True(start.IsCompleted);
            }

            void Verify(int count1, int count2)
            {
                Assert.Equal(count1, StartNonconcurrentTestClass<Impl1>.s_count);
                Assert.Equal(count2, StartNonconcurrentTestClass<Impl2>.s_count);
            }
        }

        private class StartNonconcurrentTestClass<T> : IHostedLifecycleService
        {
            public static int s_count = 0;
            public static SemaphoreSlim? s_wait = new SemaphoreSlim(1);

            public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public async Task StartAsync(CancellationToken cancellationToken)
            {
                s_count++;
                await s_wait.WaitAsync();
            }
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public async Task StartedConcurrently()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services
                    .AddHostedService<StartedTestClass<Impl1>>()
                    .AddHostedService<StartedTestClass<Impl2>>()
                    .Configure<HostOptions>(opts => opts.ServicesStartConcurrently = true);
            });

            using (IHost host = hostBuilder.Build())
            {
                StartedTestClass<Impl1>.s_wait1.Wait();
                StartedTestClass<Impl2>.s_wait1.Wait();
                StartedTestClass<Impl1>.s_wait2.Wait();
                StartedTestClass<Impl2>.s_wait2.Wait();
                StartedTestClass<Impl1>.s_wait3.Wait();
                StartedTestClass<Impl2>.s_wait3.Wait();

                Verify(0, 0, 0, 0, 0, 0);

                // Both run serially until the await.
                Task start = host.StartAsync();
                Verify(1, 1, 0, 0, 0, 0);
                await Task.Delay(s_superShortDelay);
                Verify(1, 1, 0, 0, 0, 0);

                // Resume and check that both are not finished.
                StartedTestClass<Impl1>.s_wait1.Release();
                await StartedTestClass<Impl1>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 0, 0, 0);

                StartedTestClass<Impl2>.s_wait1.Release();
                await StartedTestClass<Impl2>.s_wait2.WaitAsync();
                Verify(1, 1, 1, 1, 0, 0);

                // Resume and verify they finish.
                StartedTestClass<Impl1>.s_wait3.Release();
                StartedTestClass<Impl2>.s_wait3.Release();
                await start;
                Verify(1, 1, 1, 1, 1, 1);
                Assert.True(start.IsCompleted);
            }

            void Verify(int initial1, int initial2, int pause1, int pause2, int final1, int final2)
            {
                Assert.Equal(initial1, StartedTestClass<Impl1>.s_initialCount);
                Assert.Equal(initial2, StartedTestClass<Impl2>.s_initialCount);
                Assert.Equal(pause1, StartedTestClass<Impl1>.s_pauseCount);
                Assert.Equal(pause2, StartedTestClass<Impl2>.s_pauseCount);
                Assert.Equal(final1, StartedTestClass<Impl1>.s_finalCount);
                Assert.Equal(final2, StartedTestClass<Impl2>.s_finalCount);
            }
        }

        private class StartedTestClass<T> : IHostedLifecycleService
        {
            public static int s_initialCount = 0;
            public static int s_pauseCount = 0;
            public static int s_finalCount = 0;
            public static SemaphoreSlim? s_wait1 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait2 = new SemaphoreSlim(1);
            public static SemaphoreSlim? s_wait3 = new SemaphoreSlim(1);

            public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public async Task StartedAsync(CancellationToken cancellationToken)
            {
                s_initialCount++;
                await s_wait1.WaitAsync();
                s_pauseCount++;
                s_wait2.Release();
                await s_wait3.WaitAsync();
                s_finalCount++;
            }
            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StartPhasesException_Starting(bool throwAfterAsyncCall)
        {
            ExceptionImpl impl = new(throwAfterAsyncCall: throwAfterAsyncCall,
                throwOnStarting: true, throwOnStart: false, throwOnStarted: false);

            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddHostedService((token) => impl);
            });

            using (IHost host = hostBuilder.Build())
            {
                Exception ex = await Assert.ThrowsAnyAsync<Exception>(async () => await host.StartAsync());

                Assert.True(impl.StartingCalled);
                Assert.False(impl.StartCalled);
                Assert.False(impl.StartedCalled);

                Assert.Contains("(ThrowOnStarting)", ex.Message);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StartPhasesException_Start(bool throwAfterAsyncCall)
        {
            ExceptionImpl impl = new(throwAfterAsyncCall: throwAfterAsyncCall,
                throwOnStarting: false, throwOnStart: true, throwOnStarted: false);

            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddHostedService((token) => impl);
            });

            using (IHost host = hostBuilder.Build())
            {
                Exception ex = await Assert.ThrowsAnyAsync<Exception>(async () => await host.StartAsync());

                Assert.True(impl.StartingCalled);
                Assert.True(impl.StartCalled);
                Assert.False(impl.StartedCalled);

                Assert.Contains("(ThrowOnStart)", ex.Message);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StartPhasesException_Started(bool throwAfterAsyncCall)
        {
            ExceptionImpl impl = new(throwAfterAsyncCall: throwAfterAsyncCall,
                throwOnStarting: false, throwOnStart: false, throwOnStarted: true);

            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddHostedService((token) => impl);
            });

            using (IHost host = hostBuilder.Build())
            {
                Exception ex = await Assert.ThrowsAnyAsync<Exception>(async () => await host.StartAsync());

                Assert.True(impl.StartingCalled);
                Assert.True(impl.StartCalled);
                Assert.True(impl.StartedCalled);

                Assert.Contains("(ThrowOnStarted)", ex.Message);
            }
        }

        [Fact]
        public async Task ValidateOnStartAbortsChain()
        {
            ExceptionImpl impl = new(throwAfterAsyncCall: false, throwOnStarting: false, throwOnStart: false, throwOnStarted: false);
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddHostedService((token) => impl)
                .AddOptions<ComplexOptions>()
                .Validate(o => o.Boolean)
                .ValidateOnStart();
            });

            using (IHost host = hostBuilder.Build())
            {
                await Assert.ThrowsAnyAsync<OptionsValidationException>(async () => await host.StartAsync());
                Assert.False(impl.StartingCalled);
            }
        }
    }
}
