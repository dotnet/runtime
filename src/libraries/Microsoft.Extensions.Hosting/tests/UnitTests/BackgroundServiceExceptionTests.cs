// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class BackgroundServiceExceptionTests
    {
        /// <summary>
        /// Tests that when a BackgroundService throws an exception synchronously (without await),
        /// the host propagates the exception.
        /// </summary>
        [Fact]
        public async Task BackgroundService_SynchronousException_ThrowsException()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<SynchronousFailureService>();
                });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await builder.Build().RunAsync();
            });
        }

        /// <summary>
        /// Tests that when a BackgroundService throws an exception asynchronously (after an await),
        /// the host propagates the exception.
        /// This is the main issue that was reported in GitHub issue #67146.
        /// </summary>
        [Fact]
        public async Task BackgroundService_AsynchronousException_ThrowsException()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<AsynchronousFailureService>();
                });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await builder.Build().RunAsync();
            });
        }

        /// <summary>
        /// Tests that when a BackgroundService throws an exception asynchronously,
        /// StopAsync propagates the exception when StopHost behavior is configured.
        /// </summary>
        [Fact]
        public async Task BackgroundService_AsynchronousException_StopAsync_ThrowsException()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<AsynchronousFailureService>();
                });

            using var host = builder.Build();
            await host.StartAsync();

            // Wait for the host to react to the background service failure
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var stoppingTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            lifetime.ApplicationStopping.Register(() => stoppingTcs.TrySetResult(null));
            Assert.Equal(stoppingTcs.Task, await Task.WhenAny(stoppingTcs.Task, Task.Delay(TimeSpan.FromSeconds(10))));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await host.StopAsync();
            });
        }

        /// <summary>
        /// Tests that when a BackgroundService throws an exception asynchronously,
        /// calling StopAsync twice propagates the exception both times when StopHost behavior is configured.
        /// </summary>
        [Fact]
        public async Task BackgroundService_AsynchronousException_StopTwiceAsync_ThrowsException()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<AsynchronousFailureService>();
                });

            using var host = builder.Build();
            await host.StartAsync();

            // Wait for the host to react to the background service failure
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var stoppingTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            lifetime.ApplicationStopping.Register(() => stoppingTcs.TrySetResult(null));
            Assert.Equal(stoppingTcs.Task, await Task.WhenAny(stoppingTcs.Task, Task.Delay(TimeSpan.FromSeconds(10))));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await host.StopAsync();
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await host.StopAsync();
            });
        }

        /// <summary>
        /// Tests that when multiple BackgroundServices throw exceptions,
        /// the host aggregates them into an AggregateException.
        /// </summary>
        [Fact]
        public async Task BackgroundService_MultipleExceptions_ThrowsAggregateException()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<AsynchronousFailureService>();
                    services.AddHostedService<SecondAsynchronousFailureService>();
                    services.AddHostedService<ThirdAsynchronousFailureService>();
                });

            var aggregateException = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await builder.Build().RunAsync();
            });

            Assert.Equal(3, aggregateException.InnerExceptions.Count);

            Assert.All(aggregateException.InnerExceptions, ex =>
                Assert.IsType<InvalidOperationException>(ex));
        }

        /// <summary>
        /// Tests that when a BackgroundService throws an exception during execution
        /// and another service throws during StopAsync, the host aggregates them into an AggregateException.
        /// </summary>
        [Fact]
        public async Task BackgroundServiceExceptionAndStopException_ThrowsAggregateException()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<AsynchronousFailureService>();
                    services.AddHostedService<StopFailureService>();
                });

            var aggregateException = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await builder.Build().RunAsync();
            });

            Assert.Equal(2, aggregateException.InnerExceptions.Count);

            Assert.All(aggregateException.InnerExceptions, ex =>
                Assert.IsType<InvalidOperationException>(ex));
        }

        /// <summary>
        /// Regression test for a race where the fire-and-forget TryExecuteBackgroundServiceAsync
        /// has not yet recorded its exception by the time Host.StopAsync reads the exception list.
        /// DelayedMonitorFaultService overrides ExecuteTask so that the monitoring task sees a
        /// separately-controlled task that faults 200ms after StopAsync returns,
        /// reproducing the window in which the exception would be lost without the fix.
        /// </summary>
        [Fact]
        public async Task BackgroundService_DelayedMonitoringException_ThrowsAggregateException()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<SynchronousFailureService>();
                    services.AddHostedService<DelayedMonitorFaultService>();
                });

            var aggregateException = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await builder.Build().RunAsync();
            });

            Assert.Equal(2, aggregateException.InnerExceptions.Count);

            Assert.All(aggregateException.InnerExceptions, ex =>
                Assert.IsType<InvalidOperationException>(ex));
        }

        /// <summary>
        /// Tests that when a BackgroundService throws an exception with Ignore behavior,
        /// the host does not throw and continues to run until stopped.
        /// </summary>
        [Fact]
        public async Task BackgroundService_IgnoreException_DoesNotThrow()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                    });
                    services.AddHostedService<AsynchronousFailureService>();
                    services.AddHostedService<SuccessfulService>();
                });

            await builder.Build().RunAsync();
        }

        /// <summary>
        /// Tests that when a BackgroundService is configured to Ignore exceptions,
        /// the host does not throw even when the service fails.
        /// </summary>
        [Fact]
        public async Task BackgroundService_IgnoreException_StopAsync_DoesNotThrow()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                        options.ShutdownTimeout = TimeSpan.FromSeconds(1);
                    });
                    services.AddHostedService<AsynchronousFailureService>();
                });

            using var host = builder.Build();
            await host.StartAsync();

            // Wait a bit for the background service to fail.
            // This shouldn't cause flakiness: bad order of operations could cause the test to succeed when it should fail, but it shouldn't cause the test to fail when it should succeed.
            // Note that waiting for a signal from the service here wouldn't be enough; we also need to wait for the host to process the exception.
            await Task.Delay(TimeSpan.FromMilliseconds(200));

            await host.StopAsync();
        }

        /// <summary>
        /// Tests that when a BackgroundService completes successfully,
        /// the host does not throw an exception.
        /// </summary>
        [Fact]
        public async Task BackgroundService_SuccessfulCompletion_DoesNotThrow()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HostOptions>(options =>
                    {
                        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                    });
                    services.AddHostedService<SuccessfulService>();
                });

            await builder.Build().RunAsync();
        }

        private class SynchronousFailureService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                // Throw synchronously (no await before the exception)
                throw new InvalidOperationException("Synchronous failure");
            }
        }

        private class AsynchronousFailureService : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                // Await before throwing to make the exception asynchronous
                // Ignore the cancellation token to ensure this service throws even if the host is trying to shut down
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                throw new InvalidOperationException("Asynchronous failure");
            }
        }

        private class SecondAsynchronousFailureService : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                // Ignore the cancellation token to ensure this service throws even if the host is trying to shut down
                await Task.Delay(TimeSpan.FromMilliseconds(150));
                throw new InvalidOperationException("Second asynchronous failure");
            }
        }

        private class ThirdAsynchronousFailureService : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                // Ignore the cancellation token to ensure this service throws even if the host is trying to shut down
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                throw new InvalidOperationException("Third asynchronous failure");
            }
        }

        private class StopFailureService : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Stop failure");
        }

        /// <summary>
        /// A BackgroundService that overrides <see cref="ExecuteTask"/> to return a separately
        /// controlled task. The internal _executeTask (used by BackgroundService.StopAsync) completes
        /// normally on cancellation, but the overridden ExecuteTask (monitored by
        /// TryExecuteBackgroundServiceAsync) faults 200ms after StopAsync, usually
        /// reproducing the race window.
        /// </summary>
        private class DelayedMonitorFaultService : BackgroundService
        {
            private readonly TaskCompletionSource<object?> _monitorTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public override Task? ExecuteTask => _monitorTcs.Task;

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                }
            }

            public override async Task StopAsync(CancellationToken cancellationToken)
            {
                await base.StopAsync(cancellationToken);
                _ = Task.Run(async () =>
                {
                    // This is testing that ExecuteTask delays stopping of the host, so it can't be triggered by a deterministic signal.
                    // It shouldn't cause any flakiness: incorrect ordering could cause the test to succeed when it should fail, but it shouldn't cause the test to fail when it should succeed.
                    await Task.Delay(200);
                    _monitorTcs.TrySetException(new InvalidOperationException("Delayed monitor failure"));
                });
            }
        }

        private class SuccessfulService : BackgroundService
        {
            private readonly IHostApplicationLifetime _lifetime;

            public SuccessfulService(IHostApplicationLifetime lifetime)
            {
                _lifetime = lifetime;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                // Exit normally without throwing - signal the host to stop
                _lifetime.StopApplication();
            }
        }
    }
}
