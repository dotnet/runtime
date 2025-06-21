// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Http.Tests
{
    public class HttpClientFactoryDisposeTests
    {
        private const int ClientCount = 5;

        [Fact]
        public async Task DisposingServiceProvider_DisposesHttpClientFactory_ReleasesResources()
        {
            // Arrange
            var disposeCounter = new DisposeCounter();
            var services = new ServiceCollection();
            services.AddSingleton(disposeCounter);
            
            // Add a scoped service that will be disposed when handlers are disposed
            services.AddScoped<ITestScopedService, TestScopedService>();
            
            // Register handlers for multiple named clients
            for (int i = 0; i < ClientCount; i++)
            {
                string clientName = $"test-client-{i}";
                services.AddHttpClient(clientName, client => { })
                    .ConfigurePrimaryHttpMessageHandler(provider => new DisposeTrackingHandler(disposeCounter, provider.GetRequiredService<ITestScopedService>()));
            }
            
            // Build service provider and create test factory
            var serviceProvider = services.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
            var filters = serviceProvider.GetServices<IHttpMessageHandlerBuilderFilter>();
            
            var factory = new TestHttpClientFactory(serviceProvider, scopeFactory, optionsMonitor, filters)
            {
                EnableExpiryTimer = true,
                EnableCleanupTimer = true
            };
            
            // Create clients to initialize active handlers
            var clients = new List<HttpClient>();
            for (int i = 0; i < ClientCount; i++)
            {
                string clientName = $"test-client-{i}";
                var client = factory.CreateClient(clientName);
                clients.Add(client);
                
                // Use the client to ensure the handler is created
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            }
            
            // Verify handlers were created
            Assert.Equal(ClientCount, disposeCounter.Created);
            Assert.Equal(ClientCount, disposeCounter.ScopedServicesCreated);
            
            // Force expiry of all active handlers
            var activeEntries = new List<(ActiveHandlerTrackingEntry, TaskCompletionSource<ActiveHandlerTrackingEntry>, Task)>();
            foreach (var kvp in factory.ActiveEntryState)
            {
                activeEntries.Add((kvp.Key, kvp.Value.Item1, kvp.Value.Item2));
                kvp.Value.Item1.SetResult(kvp.Key);
            }
            
            // Wait for all handlers to expire
            foreach (var entry in activeEntries)
            {
                await entry.Item3;
            }
            
            // Verify all handlers are now expired
            Assert.Equal(ClientCount, factory._expiredHandlers.Count);
            Assert.Empty(factory._activeHandlers);
            
            // Clear client references to allow GC
            clients.Clear();
            activeEntries.Clear();
            
            // Create new clients (these will be active handlers)
            for (int i = 0; i < ClientCount; i++)
            {
                string clientName = $"test-client-{i}";
                var client = factory.CreateClient(clientName);
                
                // Use the client to ensure the handler is created
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            }
            
            // Verify we now have both expired and active handlers
            Assert.Equal(ClientCount, factory._expiredHandlers.Count); // expired handlers
            Assert.Equal(ClientCount, factory._activeHandlers.Count); // active handlers
            Assert.Equal(ClientCount * 2, disposeCounter.Created); // total handlers created
            Assert.Equal(ClientCount * 2, disposeCounter.ScopedServicesCreated); // total scoped services created
            
            // Act - dispose the factory which should dispose all handlers and their scopes
            factory.Dispose();
            
            // Assert - all handlers and scoped services should be disposed
            Assert.Equal(disposeCounter.Created, disposeCounter.Disposed);
            Assert.Equal(disposeCounter.ScopedServicesCreated, disposeCounter.ScopedServicesDisposed);
        }

        private interface ITestScopedService : IDisposable
        {
            void DoSomething();
        }

        private class TestScopedService : ITestScopedService
        {
            private readonly DisposeCounter _counter;
            private bool _disposed;

            public TestScopedService(DisposeCounter counter)
            {
                _counter = counter;
                _counter.IncrementScopedServicesCreated();
            }

            public void DoSomething()
            {
                // Test method to ensure service is being used
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _counter.IncrementScopedServicesDisposed();
                }
            }
        }

        private class DisposeCounter
        {
            private int _created;
            private int _disposed;
            private int _scopedServicesCreated;
            private int _scopedServicesDisposed;

            public int Created => Interlocked.CompareExchange(ref _created, 0, 0);
            public int Disposed => Interlocked.CompareExchange(ref _disposed, 0, 0);
            public int ScopedServicesCreated => Interlocked.CompareExchange(ref _scopedServicesCreated, 0, 0);
            public int ScopedServicesDisposed => Interlocked.CompareExchange(ref _scopedServicesDisposed, 0, 0);

            public void IncrementCreated()
            {
                Interlocked.Increment(ref _created);
            }

            public void IncrementDisposed()
            {
                Interlocked.Increment(ref _disposed);
            }

            public void IncrementScopedServicesCreated()
            {
                Interlocked.Increment(ref _scopedServicesCreated);
            }

            public void IncrementScopedServicesDisposed()
            {
                Interlocked.Increment(ref _scopedServicesDisposed);
            }
        }

        private class DisposeTrackingHandler : HttpMessageHandler
        {
            private readonly DisposeCounter _counter;
            private readonly ITestScopedService _scopedService;

            public DisposeTrackingHandler(DisposeCounter counter, ITestScopedService scopedService)
            {
                _counter = counter;
                _scopedService = scopedService;
                _counter.IncrementCreated();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _counter.IncrementDisposed();
                }

                base.Dispose(disposing);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Use the scoped service to create the dependency
                _scopedService.DoSomething();
                
                // Just return a simple response for test purposes
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }

        // Test factory from DefaultHttpClientFactoryTest.cs
        private class TestHttpClientFactory : DefaultHttpClientFactory
        {
            public TestHttpClientFactory(
                IServiceProvider services,
                IServiceScopeFactory scopeFactory,
                IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor,
                IEnumerable<IHttpMessageHandlerBuilderFilter> filters)
                : base(services, scopeFactory, optionsMonitor, filters)
            {
                ActiveEntryState = new Dictionary<ActiveHandlerTrackingEntry, (TaskCompletionSource<ActiveHandlerTrackingEntry>, Task)>();
                CleanupTimerStarted = new ManualResetEventSlim(initialState: false);
            }

            public bool EnableExpiryTimer { get; set; }

            public bool EnableCleanupTimer { get; set; }

            public ManualResetEventSlim CleanupTimerStarted { get; }

            public Dictionary<ActiveHandlerTrackingEntry, (TaskCompletionSource<ActiveHandlerTrackingEntry>, Task)> ActiveEntryState { get; }

            internal override void StartHandlerEntryTimer(ActiveHandlerTrackingEntry entry)
            {
                if (EnableExpiryTimer)
                {
                    lock (ActiveEntryState)
                    {
                        if (ActiveEntryState.ContainsKey(entry))
                        {
                            // Timer already started.
                            return;
                        }

                        // Rather than using the actual timer on the actual entry, let's fake it with async.
                        var completionSource = new TaskCompletionSource<ActiveHandlerTrackingEntry>();
                        var expiryTask = completionSource.Task.ContinueWith(t =>
                        {
                            var e = t.Result;
                            ExpiryTimer_Tick(e);

                            lock (ActiveEntryState)
                            {
                                ActiveEntryState.Remove(e);
                            }
                        });

                        ActiveEntryState.Add(entry, (completionSource, expiryTask));
                    }
                }
            }

            internal override void StartCleanupTimer()
            {
                if (EnableCleanupTimer)
                {
                    CleanupTimerStarted.Set();
                }
            }

            internal override void StopCleanupTimer()
            {
                if (EnableCleanupTimer)
                {
                    Assert.True(CleanupTimerStarted.IsSet, "Cleanup timer started");
                    CleanupTimerStarted.Reset();
                }
            }
        }
    }
}
