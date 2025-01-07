// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public partial class LifecycleTests
    {
        private static TimeSpan s_superShortDelay = TimeSpan.FromSeconds(.05);

        // Tests that actually delay this long should be [OuterLoop]:
        private static TimeSpan s_shortDelay = TimeSpan.FromSeconds(.5);
        private static TimeSpan s_longDelay = TimeSpan.FromSeconds(5); 

        public static IHostBuilder CreateHostBuilder(Action<IServiceCollection> configure) =>
            new HostBuilder().ConfigureServices(configure);

        [Fact]
        public async Task HostedService_CallbackOccursOnce()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddHostedService<HostedService_CallbackOccursOnce_Impl>();
                services.AddHostedService<HostedService_CallbackOccursOnce_Impl>();
            });

            using (IHost host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.Equal(1, HostedService_CallbackOccursOnce_Impl.s_callbackCallCount);
        }

        private class HostedService_CallbackOccursOnce_Impl : IHostedService
        {
            public static int s_callbackCallCount = 0;

            public Task StartAsync(CancellationToken cancellationToken)
            {
                s_callbackCallCount++;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CallbackOrder(bool concurrently)
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services
                    .AddHostedService<CallbackOrder_Impl>()
                    .AddSingleton((sp) => sp.GetServices<IHostedService>().OfType<CallbackOrder_Impl>().First())
                    .AddSingleton<IHostLifetime>((sp) => sp.GetServices<IHostedService>().OfType<CallbackOrder_Impl>().First())
                    .Configure<HostOptions>(opts => opts.ServicesStartConcurrently = concurrently)
                    .Configure<HostOptions>(opts => opts.ServicesStopConcurrently = concurrently);
            });
            using (IHost host = hostBuilder.Build())
            {
                CallbackOrder_Impl impl = host.Services.GetService<CallbackOrder_Impl>();

                await host.StartAsync();
                await host.StopAsync();

                Assert.Equal(1, impl._hostWaitForStartAsyncOrder);
                Assert.Equal(2, impl._startingOrder);
                Assert.Equal(3, impl._startOrder);
                Assert.Equal(4, impl._startedOrder);
                Assert.Equal(5, impl._applicationStartedOrder);
                Assert.Equal(6, impl._stoppingOrder);
                Assert.Equal(7, impl._applicationStoppingOrder);
                Assert.Equal(8, impl._stopOrder);
                Assert.Equal(9, impl._stoppedOrder);
                Assert.Equal(10, impl._applicationStoppedOrder);
                Assert.Equal(11, impl._hostStoppedOrder);
            }
        }

        private class CallbackOrder_Impl : IHostedLifecycleService, IHostLifetime
        {
            public int _hostWaitForStartAsyncOrder;
            public int _startingOrder;
            public int _startOrder;
            public int _startedOrder;
            public int _applicationStartedOrder;
            public int _stoppingOrder;
            public int _applicationStoppingOrder;
            public int _stopOrder;
            public int _stoppedOrder;
            public int _applicationStoppedOrder;
            public int _hostStoppedOrder;

            private int _callCount;

            public CallbackOrder_Impl(IServiceProvider provider)
            {
                IHostApplicationLifetime lifetime = provider.GetService<IHostApplicationLifetime>();

                lifetime.ApplicationStarted.Register(() =>
                {
                    _applicationStartedOrder = ++_callCount;
                });

                lifetime.ApplicationStopping.Register(() =>
                {
                    _applicationStoppingOrder = ++_callCount;
                });

                lifetime.ApplicationStopped.Register(() =>
                {
                    _applicationStoppedOrder = ++_callCount;
                });
            }

            Task IHostLifetime.WaitForStartAsync(CancellationToken cancellationToken)
            {
                _hostWaitForStartAsyncOrder = ++_callCount;
                return Task.CompletedTask;
            }

            public Task StartingAsync(CancellationToken cancellationToken)
            {
                _startingOrder = ++_callCount;
                return Task.CompletedTask;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                _startOrder = ++_callCount;
                return Task.CompletedTask;
            }

            public Task StartedAsync(CancellationToken cancellationToken)
            {
                _startedOrder = ++_callCount;
                return Task.CompletedTask;
            }

            public Task StoppingAsync(CancellationToken cancellationToken)
            {
                _stoppingOrder = ++_callCount;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                _stopOrder = ++_callCount;
                return Task.CompletedTask;
            }
            public Task StoppedAsync(CancellationToken cancellationToken)
            {
                _stoppedOrder = ++_callCount;
                return Task.CompletedTask;
            }

            Task IHostLifetime.StopAsync(System.Threading.CancellationToken cancellationToken)
            {
                _hostStoppedOrder = ++_callCount;
                return Task.CompletedTask;
            }
        }

        private class ExceptionImpl : IHostedLifecycleService
        {
            private bool _throwAfterAsyncCall;
            public bool StartingCalled = false;
            public bool StartCalled = false;
            public bool StartedCalled = false;
            public bool StoppingCalled = false;
            public bool StopCalled = false;
            public bool StoppedCalled = false;

            public bool ThrowOnStarting;
            public bool ThrowOnStart;
            public bool ThrowOnStarted;
            public bool ThrowOnShutdown;

            public ExceptionImpl(
                bool throwAfterAsyncCall,
                bool throwOnShutdown)
            {
                _throwAfterAsyncCall = throwAfterAsyncCall;
                ThrowOnShutdown = throwOnShutdown;
            }

            public ExceptionImpl(
                bool throwAfterAsyncCall,
                bool throwOnStarting,
                bool throwOnStart,
                bool throwOnStarted)
            {
                _throwAfterAsyncCall = throwAfterAsyncCall;
                ThrowOnStarting = throwOnStarting;
                ThrowOnStart = throwOnStart;
                ThrowOnStarted = throwOnStarted;
            }

            public async Task StartingAsync(CancellationToken cancellationToken)
            {
                StartingCalled = true;
                if (ThrowOnStarting)
                {
                    if (_throwAfterAsyncCall)
                    {
                        await Task.Delay(s_superShortDelay);
                    }

                    throw new Exception("(ThrowOnStarting)");
                }
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                StartCalled = true;
                if (ThrowOnStart)
                {
                    if (_throwAfterAsyncCall)
                    {
                        await Task.Delay(s_superShortDelay);
                    }

                    throw new Exception("(ThrowOnStart)");
                }
            }

            public async Task StartedAsync(CancellationToken cancellationToken)
            {
                StartedCalled = true;
                if (ThrowOnStarted)
                {
                    if (_throwAfterAsyncCall)
                    {
                        await Task.Delay(s_superShortDelay);
                    }

                    throw new Exception("(ThrowOnStarted)");
                }
            }

            public async Task StoppingAsync(CancellationToken cancellationToken)
            {
                StoppingCalled = true;
                if (ThrowOnShutdown)
                {
                    if (_throwAfterAsyncCall)
                    {
                        await Task.Delay(s_superShortDelay);
                    }

                    throw new Exception("(ThrowOnStopping)");
                }
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                StopCalled = true;
                if (ThrowOnShutdown)
                {
                    if (_throwAfterAsyncCall)
                    {
                        await Task.Delay(s_superShortDelay);
                    }

                    throw new Exception("(ThrowOnStop)");
                }
            }

            public async Task StoppedAsync(CancellationToken cancellationToken)
            {
                StoppedCalled = true;
                if (ThrowOnShutdown)
                {
                    if (_throwAfterAsyncCall)
                    {
                        await Task.Delay(s_superShortDelay);
                    }

                    throw new Exception("(ThrowOnStopped)");
                }
            }
        }

        // These are used to close open generic types:
        private sealed class Impl1 { }
        private sealed class Impl2 { }
    }
}
