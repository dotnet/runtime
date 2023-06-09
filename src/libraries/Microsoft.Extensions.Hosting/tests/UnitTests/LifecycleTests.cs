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
            public bool ThrowOnStopping;
            public bool ThrowOnStop;
            public bool ThrowOnStopped;

            public ExceptionImpl(
                bool throwAfterAsyncCall,
                bool throwOnStarting,
                bool throwOnStart,
                bool throwOnStarted,
                bool throwOnStopping,
                bool throwOnStop,
                bool throwOnStopped)
            {
                _throwAfterAsyncCall = throwAfterAsyncCall;
                ThrowOnStarting = throwOnStarting;
                ThrowOnStart = throwOnStart;
                ThrowOnStarted = throwOnStarted;
                ThrowOnStopping = throwOnStopping;
                ThrowOnStop = throwOnStop;
                ThrowOnStopped = throwOnStopped;
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
                if (ThrowOnStopping)
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
                if (ThrowOnStop)
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
                if (ThrowOnStopped)
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
