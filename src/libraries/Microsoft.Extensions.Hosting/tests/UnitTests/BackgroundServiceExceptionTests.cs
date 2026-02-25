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

            var host = builder.Build();
            await host.StartAsync();

            // Wait for the background service to fail
            await Task.Delay(TimeSpan.FromMilliseconds(200));

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

            var host = builder.Build();
            await host.StartAsync();

            // Wait a bit for the background service to fail
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
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
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
