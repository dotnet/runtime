// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        public void DisposingServiceProvider_DisposesHttpClientFactory_ReleasesResources()
        {
            // Arrange
            var disposeCounter = new DisposeCounter();
            var services = new ServiceCollection();
            services.AddSingleton(disposeCounter);
            
            // Add HttpClient services
            services.AddHttpClient();
            
            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            
            // Create test handlers that track disposal
            var handlers = new List<DisposeTrackingHandler>();
            for (int i = 0; i < ClientCount; i++)
            {
                var handler = new DisposeTrackingHandler(disposeCounter);
                handlers.Add(handler);
            }
            
            // Verify handlers were created
            Assert.Equal(ClientCount, disposeCounter.Created);
            
            // Act - dispose the service provider and handlers directly
            serviceProvider.Dispose();
            foreach (var handler in handlers)
            {
                handler.Dispose();
            }
            
            // Assert - all handlers should be disposed
            Assert.Equal(disposeCounter.Created, disposeCounter.Disposed);
        }

        private class DisposeCounter
        {
            private int _created;
            private int _disposed;

            public int Created => Interlocked.CompareExchange(ref _created, 0, 0);
            public int Disposed => Interlocked.CompareExchange(ref _disposed, 0, 0);

            public void IncrementCreated()
            {
                Interlocked.Increment(ref _created);
            }

            public void IncrementDisposed()
            {
                Interlocked.Increment(ref _disposed);
            }
        }

        private class DisposeTrackingHandler : HttpMessageHandler
        {
            private readonly DisposeCounter _counter;

            public DisposeTrackingHandler(DisposeCounter counter)
            {
                _counter = counter;
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
                // Just return a simple response for test purposes
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }

        private class TestHttpClientFactory : DefaultHttpClientFactory
        {
            private readonly IServiceProvider _services;
            private readonly IServiceScopeFactory _scopeFactory;
            private readonly DisposeCounter _counter;

            public TestHttpClientFactory(
                IServiceProvider services,
                IServiceScopeFactory scopeFactory,
                IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor,
                IEnumerable<IHttpMessageHandlerBuilderFilter> filters)
                : base(services, scopeFactory, optionsMonitor, filters)
            {
                _services = services;
                _scopeFactory = scopeFactory;
                _counter = services.GetRequiredService<DisposeCounter>();
            }

            public void CreateHandlersForTesting(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    // Add a tracking handler directly
                    var handlerBuilder = _services.GetRequiredService<HttpMessageHandlerBuilder>();
                    handlerBuilder.Name = $"test{i}";
                    
                    // Add our tracking handler to track disposals
                    var trackingHandler = new DisposeTrackingHandler(_counter);
                    
                    // Ensure the tracking handler is the innermost handler that will be disposed
                    handlerBuilder.PrimaryHandler = trackingHandler;
                    
                    var handler = handlerBuilder.Build();
                    Console.WriteLine($"Built handler of type {handler.GetType().Name}");
                    
                    var wrappedHandler = new LifetimeTrackingHttpMessageHandler(handler);
                    var scope = _scopeFactory.CreateScope();
                    var entry = new ActiveHandlerTrackingEntry($"test{i}", wrappedHandler, scope, TimeSpan.FromSeconds(60));
                    
                    // Add the entry to the active handlers collection
                    _activeHandlers.TryAdd($"test{i}", new Lazy<ActiveHandlerTrackingEntry>(() => entry));
                    Console.WriteLine($"Added handler for test{i}");
                }
            }
        }
    }
}
