// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Fakes;
using Microsoft.Extensions.Hosting.Tests;
using Microsoft.Extensions.Hosting.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Hosting.Internal
{
    public class HostTests
    {
        [Fact]
        public async Task HostInjectsHostingEnvironment()
        {
            using (var host = CreateBuilder()
                .UseEnvironment("WithHostingEnvironment")
                .Build())
            {
                await host.StartAsync();
                var env = host.Services.GetService<IHostEnvironment>();
                Assert.Equal("WithHostingEnvironment", env.EnvironmentName);
            }
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            using (var host = CreateBuilder().ConfigureServices((hostContext, services) => services.AddSingleton<IFakeService, FakeService>()).Build())
            {
                Assert.NotNull(host.Services.GetRequiredService<IFakeService>());
            }
        }

        [Fact]
        public void EnvDefaultsToProductionIfNoConfig()
        {
            using (var host = CreateBuilder().Build())
            {
                var env = host.Services.GetService<IHostEnvironment>();
                Assert.Equal(Environments.Production, env.EnvironmentName);
            }
        }

        [Fact]
        public void EnvDefaultsToConfigValueIfSpecified()
        {
            var vals = new Dictionary<string, string>
            {
                { "Environment", Environments.Staging }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            using (var host = CreateBuilder(config).Build())
            {
                var env = host.Services.GetService<IHostEnvironment>();
                Assert.Equal(Environments.Staging, env.EnvironmentName);
            }
        }

        [Fact]
        public async Task IsEnvironment_Extension_Is_Case_Insensitive()
        {
            using (var host = CreateBuilder().Build())
            {
                await host.StartAsync();
                var env = host.Services.GetRequiredService<IHostEnvironment>();
                Assert.True(env.IsEnvironment(Environments.Production));
                Assert.True(env.IsEnvironment("producTion"));
            }
        }

        [Fact]
        public void HostCanBeStarted()
        {
            FakeHostedService service;
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IHostedService, FakeHostedService>();
                })
                .Start())
            {
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.NotNull(host);
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public void HostedServiceCanAcceptSingletonDependencies()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IFakeService, FakeService>();
                    services.AddHostedService<FakeHostedServiceWithDependency>();
                })
                .Start())
            {
            }
        }

        private class FakeHostedServiceWithDependency : IHostedService
        {
            public FakeHostedServiceWithDependency(IFakeService fakeService)
            {
                Assert.NotNull(fakeService);
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task HostedServiceStartNotCalledIfHostNotStarted()
        {
            using (var host = CreateBuilder()
                   .ConfigureServices((hostContext, services) =>
                   {
                       services.AddHostedService<TestHostedService>();
                   })
                   .Build())
            {
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                lifetime.StopApplication();

                var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.False(svc.StartCalled);
                await host.StopAsync();
                Assert.False(svc.StopCalled);
                host.Dispose();
                Assert.False(svc.StopCalled);
                Assert.True(svc.DisposeCalled);
            }
        }

        [Fact]
        public async Task HostedServiceRegisteredAsSingletons()
        {
            using (var host = CreateBuilder()
                   .ConfigureServices((hostContext, services) =>
                   {
                       services.AddHostedService<TestHostedService>();
                   })
                   .Build())
            {
                var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.False(svc.StartCalled);
                await host.StartAsync();
                Assert.True(svc.StartCalled);
                await host.StopAsync();
                Assert.True(svc.StopCalled);
                host.Dispose();
                Assert.True(svc.DisposeCalled);
            }
        }

        [Fact]
        public async Task HostCanBeStoppedWhenNotStarted()
        {
            using (var host = CreateBuilder()
                   .ConfigureServices((hostContext, services) =>
                   {
                       services.AddHostedService<TestHostedService>();
                   })
                   .Build())
            {
                var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.False(svc.StartCalled);
                await host.StopAsync();
                Assert.False(svc.StopCalled);
                host.Dispose();
                Assert.False(svc.StopCalled);
                Assert.True(svc.DisposeCalled);
            }
        }

        [Fact]
        public void HostedServiceRegisteredWithFactory()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IFakeService, FakeService>();
                    services.AddHostedService(s => new FakeHostedServiceWithDependency(s.GetRequiredService<IFakeService>()));
                })
                .Start())
            {
            }
        }

        [Fact]
        public async Task AppCrashesOnStartWhenFirstHostedServiceThrows()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build())
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
                Assert.True(events1[0]);
                Assert.False(events2[0]);
                host.Dispose();
                // Stopping
                Assert.False(events1[1]);
                Assert.False(events2[1]);
            }
        }

        [Fact]
        public async Task StartCanBeCancelled()
        {
            var serviceStarting = new ManualResetEvent(false);
            var startCancelled = new ManualResetEvent(false);
            FakeHostedService service;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService>(_ => new FakeHostedService()
                    {
                        StartAction = ct =>
                        {
                            Assert.False(ct.IsCancellationRequested);
                            serviceStarting.Set();
                            Assert.True(startCancelled.WaitOne(TimeSpan.FromSeconds(5)));
                            ct.ThrowIfCancellationRequested();
                        }
                    });
                })
                .Build())
            {
                var cts = new CancellationTokenSource();

                var startTask = Task.Run(() => host.StartAsync(cts.Token));
                Assert.True(serviceStarting.WaitOne(TimeSpan.FromSeconds(5)));
                cts.Cancel();
                startCancelled.Set();
                await Assert.ThrowsAsync<OperationCanceledException>(() => startTask);

                Assert.NotNull(host);
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task CancellableStart_CancelledByApplicationLifetime()
        {
            var hostedService = new AsyncFakeHostedService();
            var builder = CreateBuilder().ConfigureServices(services =>
            {
                services.AddSingleton<IHostedService>(hostedService);
            });

            using (var host = builder.Build())
            {
                var applicationLifetime = host.Services.GetService<IHostApplicationLifetime>();
                var startTask = host.StartAsync();

                // stop application
                applicationLifetime.StopApplication();

                // complete start task
                hostedService.ContinueStart();

                await Assert.ThrowsAsync<OperationCanceledException>(() => startTask);
                Assert.False(hostedService.IsStartCompleted);
            }
        }

        [Fact]
        public async Task CancellableStart_CancelledByCancellationToken()
        {
            var hostedService = new AsyncFakeHostedService();
            var builder = CreateBuilder().ConfigureServices(services =>
            {
                services.AddSingleton<IHostedService>(hostedService);
            });

            using (var host = builder.Build())
            {
                var cts = new CancellationTokenSource();
                var startTask = host.StartAsync(cts.Token);

                // cancel token
                cts.Cancel();

                // complete start task
                hostedService.ContinueStart();

                await Assert.ThrowsAsync<OperationCanceledException>(() => startTask);
                Assert.False(hostedService.IsStartCompleted);
            }
        }

        [Fact]
        public async Task CancellableStart_CanComplete()
        {
            var hostedService = new AsyncFakeHostedService();
            var builder = CreateBuilder().ConfigureServices(services =>
            {
                services.AddSingleton<IHostedService>(hostedService);
            });

            using (var host = builder.Build())
            {
                var startTask = host.StartAsync();

                // complete start task
                hostedService.ContinueStart();

                await startTask;
                Assert.True(hostedService.IsStartCompleted);
            }
        }

        public class AsyncFakeHostedService : IHostedService
        {
            private TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            public bool IsStartCompleted { get; set; }
            public async Task StartAsync(CancellationToken cancellationToken)
            {
                Assert.False(cancellationToken.IsCancellationRequested);
                await source.Task;
                cancellationToken.ThrowIfCancellationRequested();
                IsStartCompleted = true;
            }
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public void ContinueStart() => source.TrySetResult(null);
        }

        [Fact]
        public async Task HostLifetimeOnStartedDelaysStart()
        {
            var serviceStarting = new ManualResetEvent(false);
            var lifetimeStart = new ManualResetEvent(false);
            var lifetimeContinue = new ManualResetEvent(false);
            FakeHostedService service;
            FakeHostLifetime lifetime;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService>(_ => new FakeHostedService()
                    {
                        StartAction = ct =>
                        {
                            serviceStarting.Set();
                        }
                    });
                    services.AddSingleton<IHostLifetime>(_ => new FakeHostLifetime()
                    {
                        StartAction = ct =>
                        {
                            lifetimeStart.Set();
                            Assert.True(lifetimeContinue.WaitOne(TimeSpan.FromSeconds(5)));
                        }
                    });
                })
                .Build())
            {
                var startTask = Task.Run(() => host.StartAsync());
                Assert.True(lifetimeStart.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.False(serviceStarting.WaitOne(0));

                lifetimeContinue.Set();
                Assert.True(serviceStarting.WaitOne(TimeSpan.FromSeconds(5)));

                await startTask;

                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                lifetime = (FakeHostLifetime)host.Services.GetRequiredService<IHostLifetime>();
                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(0, lifetime.StopCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);

            Assert.Equal(1, lifetime.StartCount);
            Assert.Equal(0, lifetime.StopCount);
        }

        [Fact]
        public async Task HostLifetimeOnStartedCanBeCancelled()
        {
            var serviceStarting = new ManualResetEvent(false);
            var lifetimeStart = new ManualResetEvent(false);
            var lifetimeContinue = new ManualResetEvent(false);
            FakeHostedService service;
            FakeHostLifetime lifetime;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService>(_ => new FakeHostedService()
                    {
                        StartAction = ct =>
                        {
                            serviceStarting.Set();
                        }
                    });
                    services.AddSingleton<IHostLifetime>(_ => new FakeHostLifetime()
                    {
                        StartAction = ct =>
                        {
                            lifetimeStart.Set();
                            WaitHandle.WaitAny(new[] { lifetimeContinue, ct.WaitHandle });
                        }
                    });
                })
                .Build())
            {
                var cts = new CancellationTokenSource();

                var startTask = Task.Run(() => host.StartAsync(cts.Token));

                Assert.True(lifetimeStart.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.False(serviceStarting.WaitOne(0));

                cts.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => startTask);
                Assert.False(serviceStarting.WaitOne(0));

                lifetimeContinue.Set();
                Assert.False(serviceStarting.WaitOne(0));

                Assert.NotNull(host);
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(0, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                lifetime = (FakeHostLifetime)host.Services.GetRequiredService<IHostLifetime>();
                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(0, lifetime.StopCount);
            }

            Assert.Equal(0, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);

            Assert.Equal(1, lifetime.StartCount);
            Assert.Equal(0, lifetime.StopCount);
        }

        [Fact]
        public async Task HostStopAsyncCallsHostLifetimeStopAsync()
        {
            FakeHostedService service;
            FakeHostLifetime lifetime;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService, FakeHostedService>();
                    services.AddSingleton<IHostLifetime, FakeHostLifetime>();
                })
                .Build())
            {
                await host.StartAsync();

                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                lifetime = (FakeHostLifetime)host.Services.GetRequiredService<IHostLifetime>();
                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(0, lifetime.StopCount);

                await host.StopAsync();

                Assert.Equal(1, service.StartCount);
                Assert.Equal(1, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(1, lifetime.StopCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(1, service.StopCount);
            Assert.Equal(1, service.DisposeCount);

            Assert.Equal(1, lifetime.StartCount);
            Assert.Equal(1, lifetime.StopCount);
        }

        [Fact]
        public async Task HostShutsDownWhenTokenTriggers()
        {
            FakeHostedService service;
            using (var host = CreateBuilder()
                .ConfigureServices((services) => services.AddSingleton<IHostedService, FakeHostedService>())
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();

                var cts = new CancellationTokenSource();

                var runInBackground = host.RunAsync(cts.Token);

                // Wait on the host to be started
                lifetime.ApplicationStarted.WaitHandle.WaitOne();

                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                cts.Cancel();

                // Wait on the host to shutdown
                lifetime.ApplicationStopped.WaitHandle.WaitOne();

                // Wait for RunAsync to finish to guarantee Disposal of Host
                await runInBackground;

                Assert.Equal(1, service.StopCount);
                Assert.Equal(1, service.DisposeCount);
            }
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task HostStopAsyncCanBeCancelledEarly()
        {
            var service = new Mock<IHostedService>();
            service.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton(service.Object);
                })
                .Build())
            {
                await host.StartAsync();

                var cts = new CancellationTokenSource();

                var task = host.StopAsync(cts.Token);
                cts.Cancel();

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))));
            }
        }

        [Fact]
        public async Task HostStopAsyncUsesDefaultTimeoutIfGivenTokenDoesNotFire()
        {
            var service = new Mock<IHostedService>();
            service.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(0.5));
                    services.AddSingleton(service.Object);
                })
                .Build())
            {
                await host.StartAsync();

                var cts = new CancellationTokenSource();

                // Purposefully don't trigger cts
                var task = host.StopAsync(cts.Token);

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))));
            }
        }

        [Fact]
        public async Task WebHostStopAsyncUsesDefaultTimeoutIfNoTokenProvided()
        {
            var service = new Mock<IHostedService>();
            service.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(0.5));
                    services.AddSingleton(service.Object);
                })
                .Build())
            {
                await host.StartAsync();

                var task = host.StopAsync();

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))));
            }
        }

        [Fact]
        public async Task HostPropagatesExceptionsThrownWithBackgroundServiceExceptionBehaviorOfStopHost()
        {
            using IHost host = CreateBuilder()
                .ConfigureServices(
                    services =>
                    {
                        services.AddHostedService(_ => new AsyncThrowingService(Task.CompletedTask));
                        services.Configure<HostOptions>(
                            options =>
                            options.BackgroundServiceExceptionBehavior =
                                BackgroundServiceExceptionBehavior.StopHost);
                    })
                .Build();

            await Assert.ThrowsAsync<Exception>(() => host.StartAsync());
        }

        [Fact]
        public async Task HostStopsApplicationWithOneBackgroundServiceErrorAndOthersWithoutError()
        {
            var wasOtherServiceStarted = false;

            TaskCompletionSource<bool> throwingTcs = new();
            TaskCompletionSource<bool> otherTcs = new();

            using IHost host = CreateBuilder()
                .ConfigureServices(
                    services =>
                    {
                        services.AddHostedService(_ => new AsyncThrowingService(throwingTcs.Task));
                        services.AddHostedService(
                            _ => new TestBackgroundService(otherTcs.Task,
                            () =>
                            {
                                wasOtherServiceStarted = true;
                                throwingTcs.SetResult(true);
                            }));
                        services.Configure<HostOptions>(
                            options =>
                            options.BackgroundServiceExceptionBehavior =
                                BackgroundServiceExceptionBehavior.StopHost);
                    })
                .Build();

            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            var wasStartedCalled = false;
            lifetime.ApplicationStarted.Register(() => wasStartedCalled = true);

            var wasStoppingCalled = false;
            lifetime.ApplicationStopping.Register(() =>
            {
                wasStoppingCalled = true;
                otherTcs.SetResult(true);
            });

            // Ensure all completions have been signaled before continuing
            await Task.WhenAll(host.StartAsync(), throwingTcs.Task, otherTcs.Task);

            Assert.True(wasStartedCalled);
            Assert.True(wasStoppingCalled);
            Assert.True(wasOtherServiceStarted);
        }

        [Fact]
        public void HostHandlesExceptionsThrownWithBackgroundServiceExceptionBehaviorOfIgnore()
        {
            var backgroundDelayTaskSource = new TaskCompletionSource<bool>();

            using IHost host = CreateBuilder()
                .ConfigureServices(
                    services =>
                    {
                        services.AddHostedService(
                            _ => new AsyncThrowingService(backgroundDelayTaskSource.Task));

                        services.PostConfigure<HostOptions>(
                          options =>
                            options.BackgroundServiceExceptionBehavior =
                                BackgroundServiceExceptionBehavior.Ignore);
                    })
                .Build();

            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var wasStoppingCalled = false;
            lifetime.ApplicationStopping.Register(() => wasStoppingCalled = true);

            host.Start();

            backgroundDelayTaskSource.SetResult(true);

            Assert.False(wasStoppingCalled);
        }

        [Fact]
        public void HostApplicationLifetimeEventsOrderedCorrectlyDuringShutdown()
        {
            using (var host = CreateBuilder()
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                var applicationStartedEvent = new ManualResetEventSlim(false);
                var applicationStoppingEvent = new ManualResetEventSlim(false);
                var applicationStoppedEvent = new ManualResetEventSlim(false);
                var applicationStartedCompletedBeforeApplicationStopping = false;
                var applicationStoppingCompletedBeforeApplicationStopped = false;
                var applicationStoppedCompletedBeforeRunCompleted = false;

                lifetime.ApplicationStarted.Register(() =>
                {
                    applicationStartedEvent.Set();
                });

                lifetime.ApplicationStopping.Register(() =>
                {
                    // Check whether the applicationStartedEvent has been set
                    applicationStartedCompletedBeforeApplicationStopping = applicationStartedEvent.IsSet;

                    // Simulate work.
                    Thread.Sleep(1000);

                    applicationStoppingEvent.Set();
                });

                lifetime.ApplicationStopped.Register(() =>
                {
                    // Check whether the applicationStoppingEvent has been set
                    applicationStoppingCompletedBeforeApplicationStopped = applicationStoppingEvent.IsSet;
                    applicationStoppedEvent.Set();
                });

                var runHostAndVerifyApplicationStopped = Task.Run(async () =>
                {
                    await host.RunAsync();
                    // Check whether the applicationStoppingEvent has been set
                    applicationStoppedCompletedBeforeRunCompleted = applicationStoppedEvent.IsSet;
                });

                // Wait until application has started to shut down the host
                Assert.True(applicationStartedEvent.Wait(5000));

                // Trigger host shutdown on a separate thread
                Task.Run(() => lifetime.StopApplication());

                // Wait for all events and host.Run() to complete
                Assert.True(runHostAndVerifyApplicationStopped.Wait(5000));

                // Verify Ordering
                Assert.True(applicationStartedCompletedBeforeApplicationStopping);
                Assert.True(applicationStoppingCompletedBeforeApplicationStopped);
                Assert.True(applicationStoppedCompletedBeforeRunCompleted);
            }
        }

        [Fact]
        public async Task HostDisposesServiceProvider()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((s) =>
                {
                    s.AddTransient<IFakeService, FakeService>();
                    s.AddSingleton<IFakeSingletonService, FakeService>();
                })
                .Build())
            {
                await host.StartAsync();

                var singleton = (FakeService)host.Services.GetService<IFakeSingletonService>();
                var transient = (FakeService)host.Services.GetService<IFakeService>();

                Assert.False(singleton.Disposed);
                Assert.False(transient.Disposed);

                await host.StopAsync();

                Assert.False(singleton.Disposed);
                Assert.False(transient.Disposed);

                host.Dispose();

                Assert.True(singleton.Disposed);
                Assert.True(transient.Disposed);
            }
        }

        [Fact]
        public async Task HostNotifiesApplicationStarted()
        {
            using (var host = CreateBuilder()
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IHostApplicationLifetime>();

                Assert.False(applicationLifetime.ApplicationStarted.IsCancellationRequested);

                await host.StartAsync();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            }
        }

        [Fact]
        public async Task HostNotifiesAllIHostApplicationLifetimeCallbacksEvenIfTheyThrow()
        {
            using (var host = CreateBuilder()
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IHostApplicationLifetime>();

                var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
                var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);
                var stopped = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopped);

                await host.StartAsync();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
                Assert.True(started.All(s => s));
                await host.StopAsync();
                Assert.True(stopping.All(s => s));
                host.Dispose();
                Assert.True(stopped.All(s => s));
            }
        }

        [Fact]
        public async Task HostStopApplicationDoesNotFireStopOnHostedService()
        {
            var stoppingCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    Action started = () =>
                    {
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    Action disposing = () =>
                    {
                        disposingCalls++;
                    };

                    services.AddSingleton<IHostedService>(_ => new DelegateHostedService(started, stopping, disposing));
                })
                .Build())
            {
                await host.StartAsync();

                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                lifetime.StopApplication();

                Assert.Equal(0, stoppingCalls);
                Assert.Equal(0, disposingCalls);
            }
            Assert.Equal(0, stoppingCalls);
            Assert.Equal(1, disposingCalls);
        }

        [Fact]
        public async Task HostedServiceCanInjectApplicationLifetime()
        {
            using (var host = CreateBuilder()
                   .ConfigureServices((services) =>
                   {
                       services.AddSingleton<IHostedService, TestHostedService>();
                   })
                   .Build())
            {
                await host.StartAsync();
                var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.True(svc.StartCalled);

                await host.StopAsync();
                Assert.True(svc.StopCalled);
            }
        }

        [Fact]
        public async Task HostShutdownFiresApplicationLifetimeStoppedBeforeHostLifetimeStopped()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;
            var disposingCalls = 0;

            FakeHostLifetime fakeHostLifetime = null;
            ApplicationLifetime applicationLifetime = null;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    Action started = () =>
                    {
                        startedCalls++;
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    Action disposing = () =>
                    {
                        disposingCalls++;
                    };

                    services.AddSingleton<IHostedService>(_ => new DelegateHostedService(started, stopping, disposing));

                    services.AddSingleton<IHostLifetime>(_ =>
                    {
                        fakeHostLifetime = new FakeHostLifetime();

                        fakeHostLifetime.StopAction = () =>
                        {
                            Assert.Equal(1, startedCalls);
                            Assert.Equal(1, stoppingCalls);
                            Assert.True(applicationLifetime.ApplicationStopped.IsCancellationRequested);
                        };
                        return fakeHostLifetime;
                    }
                    );

                })
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                var hostLifetime = host.Services.GetRequiredService<IHostLifetime>();
                applicationLifetime = lifetime as ApplicationLifetime;
                Assert.NotNull(applicationLifetime);

                Assert.Equal(0, startedCalls);

                await host.StartAsync();
                Assert.Equal(1, startedCalls);
                Assert.Equal(0, stoppingCalls);
                Assert.Equal(0, disposingCalls);

                Assert.True(lifetime.ApplicationStarted.IsCancellationRequested);
                Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);
                Assert.False(lifetime.ApplicationStopped.IsCancellationRequested);

                await host.StopAsync();

                Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
                Assert.True(lifetime.ApplicationStopped.IsCancellationRequested);
                Assert.Equal(1, startedCalls);
                Assert.Equal(1, stoppingCalls);
            }
        }

        [Fact]
        public async Task HostStopApplicationFiresStopOnHostedService()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    Action started = () =>
                    {
                        startedCalls++;
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    Action disposing = () =>
                    {
                        disposingCalls++;
                    };

                    services.AddSingleton<IHostedService>(_ => new DelegateHostedService(started, stopping, disposing));
                })
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                Assert.Equal(0, startedCalls);

                await host.StartAsync();
                Assert.Equal(1, startedCalls);
                Assert.Equal(0, stoppingCalls);
                Assert.Equal(0, disposingCalls);

                await host.StopAsync();

                Assert.Equal(1, startedCalls);
                Assert.Equal(1, stoppingCalls);
                Assert.Equal(0, disposingCalls);

                host.Dispose();

                Assert.Equal(1, startedCalls);
                Assert.Equal(1, stoppingCalls);
                Assert.Equal(1, disposingCalls);
            }
        }

        [Fact]
        public async Task HostDisposeApplicationDoesNotFireStopOnHostedService()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    Action started = () =>
                    {
                        startedCalls++;
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    Action disposing = () =>
                    {
                        disposingCalls++;
                    };

                    services.AddSingleton<IHostedService>(_ => new DelegateHostedService(started, stopping, disposing));
                })
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                Assert.Equal(0, startedCalls);
                await host.StartAsync();
                Assert.Equal(1, startedCalls);
                Assert.Equal(0, stoppingCalls);
                Assert.Equal(0, disposingCalls);
                host.Dispose();

                Assert.Equal(0, stoppingCalls);
                Assert.Equal(1, disposingCalls);
            }
        }

        [Fact]
        public async Task HostDoesNotNotifyIHostApplicationLifetimeCallbacksIfIHostedServicesThrow()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IHostApplicationLifetime>();

                var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
                var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);

                await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
                Assert.True(events1[0]);
                Assert.False(events2[0]);
                Assert.False(started.All(s => s));
                host.Dispose();
                Assert.False(events1[1]);
                Assert.False(events2[1]);
                Assert.False(stopping.All(s => s));
            }
        }

        [Fact]
        public async Task Host_InvokesConfigureServicesMethodsOnlyOnce()
        {
            int configureServicesCount = 0;
            using (var host = CreateBuilder()
                .ConfigureServices((services) => configureServicesCount++)
                .Build())
            {
                Assert.Equal(1, configureServicesCount);
                await host.StartAsync();
                var services = host.Services;
                var services2 = host.Services;
                Assert.Equal(1, configureServicesCount);
            }
        }

        [Fact]
        public void Dispose_DisposesAppConfigurationProviders()
        {
            var providerMock = new Mock<ConfigurationProvider>().As<IDisposable>();
            providerMock.Setup(d => d.Dispose());

            var sourceMock = new Mock<IConfigurationSource>();
            sourceMock.Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                .Returns((ConfigurationProvider)providerMock.Object);

            var host = CreateBuilder()
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.Add(sourceMock.Object);
                })
                .Build();

            providerMock.Verify(c => c.Dispose(), Times.Never);

            host.Dispose();

            providerMock.Verify(c => c.Dispose(), Times.AtLeastOnce());
        }

        [Fact]
        public void Dispose_DisposesHostConfigurationProviders()
        {
            var providerMock = new Mock<ConfigurationProvider>().As<IDisposable>();
            providerMock.Setup(d => d.Dispose());

            var sourceMock = new Mock<IConfigurationSource>();
            sourceMock.Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                .Returns((ConfigurationProvider)providerMock.Object);

            var host = CreateBuilder()
                .ConfigureHostConfiguration(configuration =>
                {
                    configuration.Add(sourceMock.Object);
                })
                .Build();

            providerMock.Verify(c => c.Dispose(), Times.Never);

            host.Dispose();

            providerMock.Verify(c => c.Dispose(), Times.AtLeastOnce());
        }
        [Fact]
        public async Task HostCallsDisposeAsyncOnServiceProvider()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<AsyncDisposableService>();
                })
                .Build())
            {
                await host.StartAsync();

                var asyncDisposableService = host.Services.GetService<AsyncDisposableService>();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                await host.StopAsync();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                host.Dispose();

                Assert.True(asyncDisposableService.DisposeAsyncCalled);
            }
        }

        [Fact]
        public async Task HostCallsDisposeAsyncOnServiceProviderWhenDisposeAsyncCalled()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<AsyncDisposableService>();
                })
                .Build())
            {
                await host.StartAsync();

                var asyncDisposableService = host.Services.GetService<AsyncDisposableService>();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                await host.StopAsync();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                await ((IAsyncDisposable)host).DisposeAsync();

                Assert.True(asyncDisposableService.DisposeAsyncCalled);
            }
        }

        [Fact]
        public async Task DisposeAsync_DisposesAppConfigurationProviders()
        {
            var providerMock = new Mock<ConfigurationProvider>().As<IDisposable>();
            providerMock.Setup(d => d.Dispose());

            var sourceMock = new Mock<IConfigurationSource>();
            sourceMock.Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                .Returns((ConfigurationProvider)providerMock.Object);

            var host = CreateBuilder()
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.Add(sourceMock.Object);
                })
                .Build();

            providerMock.Verify(c => c.Dispose(), Times.Never);

            await ((IAsyncDisposable)host).DisposeAsync();

            providerMock.Verify(c => c.Dispose(), Times.AtLeastOnce());
        }

        [Fact]
        public async Task DisposeAsync_DisposesHostConfigurationProviders()
        {
            var providerMock = new Mock<ConfigurationProvider>().As<IDisposable>();
            providerMock.Setup(d => d.Dispose());

            var sourceMock = new Mock<IConfigurationSource>();
            sourceMock.Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                .Returns((ConfigurationProvider)providerMock.Object);

            var host = CreateBuilder()
                .ConfigureHostConfiguration(configuration =>
                {
                    configuration.Add(sourceMock.Object);
                })
                .Build();

            providerMock.Verify(c => c.Dispose(), Times.Never);

            await ((IAsyncDisposable)host).DisposeAsync();

            providerMock.Verify(c => c.Dispose(), Times.AtLeastOnce());
        }

        [Fact]
        public void ThrowExceptionForCustomImplementationOfIHostApplicationLifetime()
        {
            var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();

            Assert.Throws<ArgumentException>(() =>
            {
                CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(hostApplicationLifetimeMock.Object);
                })
                .Build();
            });
        }

        /// <summary>
        /// Tests when a BackgroundService throws an exception asynchronously
        /// (after an await), the exception gets logged correctly.
        /// </summary>
        [Theory]
        [InlineData(BackgroundServiceExceptionBehavior.Ignore, "BackgroundService failed")]
        [InlineData(BackgroundServiceExceptionBehavior.StopHost, "BackgroundService failed", "The HostOptions.BackgroundServiceExceptionBehavior is configured to StopHost")]
        public async Task BackgroundServiceAsyncExceptionGetsLogged(
            BackgroundServiceExceptionBehavior testBehavior,
            params string[] expectedExceptionMessages)
        {
            TestLoggerProvider logger = new TestLoggerProvider();
            var backgroundDelayTaskSource = new TaskCompletionSource<bool>();

            using IHost host = CreateBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(logger);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<HostOptions>(
                        options =>
                        options.BackgroundServiceExceptionBehavior = testBehavior);
                    services.AddHostedService(sp => new AsyncThrowingService(backgroundDelayTaskSource.Task));
                })
                .Start();

            backgroundDelayTaskSource.SetResult(true);

            // give the background service 1 minute to log the failure
            var timeout = TimeSpan.FromMinutes(1);
            Stopwatch sw = Stopwatch.StartNew();

            while (true)
            {
                LogEvent[] events = logger.GetEvents();
                if (expectedExceptionMessages.All(
                        expectedMessage => events.Any(
                            e => e.Message.Contains(expectedMessage))))
                {
                    break;
                }

                Assert.InRange(sw.Elapsed, TimeSpan.Zero, timeout);
                await Task.Delay(TimeSpan.FromMilliseconds(30));
            }
        }

        /// <summary>
        /// Tests that when a BackgroundService is canceled when stopping the host,
        /// no error is logged.
        /// </summary>
        [Fact]
        public async Task HostNoErrorWhenServiceIsCanceledAsPartOfStop()
        {
            TestLoggerProvider logger = new TestLoggerProvider();

            using IHost host = CreateBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(logger);
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<WorkerTemplateService>();
                })
                .Build();

            host.Start();
            await host.StopAsync();

            foreach (LogEvent logEvent in logger.GetEvents())
            {
                Assert.True(logEvent.LogLevel < LogLevel.Error);

                Assert.NotEqual("BackgroundServiceFaulted", logEvent.EventId.Name);
            }
        }

        private IHostBuilder CreateBuilder(IConfiguration config = null)
        {
            return new HostBuilder().ConfigureHostConfiguration(builder => builder.AddConfiguration(config ?? new ConfigurationBuilder().Build()));
        }

        private static bool[] RegisterCallbacksThatThrow(IServiceCollection services)
        {
            bool[] events = new bool[2];

            Action started = () =>
            {
                events[0] = true;
                throw new InvalidOperationException();
            };

            Action stopping = () =>
            {
                events[1] = true;
                throw new InvalidOperationException();
            };

            services.AddSingleton<IHostedService>(new DelegateHostedService(started, stopping, () => { }));

            return events;
        }

        private static bool[] RegisterCallbacksThatThrow(CancellationToken token)
        {
            var signals = new bool[3];
            for (int i = 0; i < signals.Length; i++)
            {
                token.Register(state =>
                {
                    signals[(int)state] = true;
                    throw new InvalidOperationException();
                }, i);
            }

            return signals;
        }

        private class TestHostedService : IHostedService, IDisposable
        {
            private readonly IHostApplicationLifetime _lifetime;

            public TestHostedService(IHostApplicationLifetime lifetime)
            {
                _lifetime = lifetime;
            }

            public bool StartCalled { get; set; }
            public bool StopCalled { get; set; }
            public bool DisposeCalled { get; set; }

            public Task StartAsync(CancellationToken token)
            {
                StartCalled = true;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken token)
            {
                StopCalled = true;
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }

        private class DelegateHostedService : IHostedService, IDisposable
        {
            private readonly Action _started;
            private readonly Action _stopping;
            private readonly Action _disposing;

            public DelegateHostedService(Action started, Action stopping, Action disposing)
            {
                _started = started;
                _stopping = stopping;
                _disposing = disposing;
            }

            public Task StartAsync(CancellationToken token)
            {
                _started();
                return Task.CompletedTask;
            }
            public Task StopAsync(CancellationToken token)
            {
                _stopping();
                return Task.CompletedTask;
            }

            public void Dispose() => _disposing();
        }

        private class AsyncDisposableService : IAsyncDisposable
        {
            public bool DisposeAsyncCalled { get; set; }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                return default;
            }
        }

        private class TestBackgroundService : IHostedService
        {
            private readonly Action _onStart;
            private readonly Task _emulateWorkTask;

            public TestBackgroundService(Task emulateWorkTask, Action onStart)
            {
                _emulateWorkTask = emulateWorkTask;
                _onStart = onStart;
            }

            public async Task StartAsync(CancellationToken stoppingToken)
            {
                _onStart();
                await _emulateWorkTask;
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private class AsyncThrowingService : BackgroundService
        {
            private readonly Task _executeDelayTask;

            public AsyncThrowingService(Task executeDelayTask)
            {
                _executeDelayTask = executeDelayTask;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                await _executeDelayTask;

                throw new Exception("Background Exception");
            }
        }

        /// <summary>
        /// A copy of the default "Worker" template.
        /// </summary>
        private class WorkerTemplateService : BackgroundService
        {
            private readonly ILogger<WorkerTemplateService> _logger;

            public WorkerTemplateService(ILogger<WorkerTemplateService> logger)
            {
                _logger = logger;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
