// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public partial class LifecycleTests
    {
        [OuterLoop("Uses Task.Delay")]
        [Theory]
        [InlineData(TimeoutService.Phase.Starting)]
        [InlineData(TimeoutService.Phase.Start)]
        [InlineData(TimeoutService.Phase.Started)]
        public async Task StartTimeoutClass_WithValue(TimeoutService.Phase phase)
        {
            var host = new HostBuilder()
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService((token) => new TimeoutService(phase));
                     services.Configure<HostOptions>((opts) =>
                     {
                         opts.StartupTimeout = s_shortDelay;
                     });
                 })
                 .UseConsoleLifetime()
                 .Build();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await host.StartAsync());
        }

        [OuterLoop("Uses Task.Delay")]
        [Theory]
        [InlineData(TimeoutService.Phase.Start)]
        public async Task StartTimeoutClass_WithValue_Concurrently(TimeoutService.Phase phase)
        {
            var host = new HostBuilder()
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService((token) => new TimeoutService(phase));
                     services.Configure<HostOptions>((opts) =>
                     {
                         opts.StartupTimeout = s_shortDelay;
                         opts.ServicesStartConcurrently = true;
                     });
                 })
                 .UseConsoleLifetime()
                 .Build();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await host.StartAsync());
        }

        [OuterLoop("Uses Task.Delay")]
        [Theory]
        [InlineData(TimeoutService.Phase.Stopping)]
        [InlineData(TimeoutService.Phase.Stop)]
        [InlineData(TimeoutService.Phase.Stopped)]
        public async Task StopTimeoutClass_WithValue(TimeoutService.Phase phase)
        {
            var host = new HostBuilder()
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService((token) => new TimeoutService(phase));
                     services.Configure<HostOptions>((opts) =>
                     {
                         opts.ShutdownTimeout = s_shortDelay;
                     });
                 })
                 .UseConsoleLifetime()
                 .Build();

            await host.StartAsync();
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await host.StopAsync());
        }

        [OuterLoop("Uses Task.Delay")]
        [Theory]
        [InlineData(TimeoutService.Phase.Stop)]
        public async Task StopTimeoutClass_WithValue_Concurrently(TimeoutService.Phase phase)
        {
            var host = new HostBuilder()
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService((token) => new TimeoutService(phase));
                     services.Configure<HostOptions>((opts) =>
                     {
                         opts.ShutdownTimeout = s_shortDelay;
                         opts.ServicesStopConcurrently = true;
                     });
                 })
                 .UseConsoleLifetime()
                 .Build();

            await host.StartAsync();
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await host.StopAsync());
        }

        public class TimeoutService : IHostedLifecycleService
        {
            private Phase _phase;

            public TimeoutService(Phase phase)
            {
                _phase = phase;
            }

            public async Task StartingAsync(CancellationToken cancellationToken)
            {
                if (_phase == Phase.Starting)
                {
                    await Task.Delay(s_longDelay, cancellationToken);
                }
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                if (_phase == Phase.Start)
                {
                    await Task.Delay(s_longDelay, cancellationToken);
                }
            }

            public async Task StartedAsync(CancellationToken cancellationToken)
            {
                if (_phase == Phase.Started)
                {
                    await Task.Delay(s_longDelay, cancellationToken);
                }
            }

            public async Task StoppingAsync(CancellationToken cancellationToken)
            {
                if (_phase == Phase.Stopping)
                {
                    await Task.Delay(s_longDelay, cancellationToken);
                }
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                if (_phase == Phase.Stop)
                {
                    await Task.Delay(s_longDelay, cancellationToken);
                }
            }

            public async Task StoppedAsync(CancellationToken cancellationToken)
            {
                if (_phase == Phase.Stopped)
                {
                    await Task.Delay(s_longDelay, cancellationToken);
                }
            }

            public enum Phase
            {
                Starting,
                Start,
                Started,
                Stopping,
                Stop,
                Stopped
            }
        }
    }
}
