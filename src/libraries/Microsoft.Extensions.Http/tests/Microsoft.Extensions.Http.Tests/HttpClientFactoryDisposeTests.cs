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
            services.AddTransient<DisposeTrackingHandler>();

            // Use a custom test factory that allows direct handler creation without waiting
            services.AddHttpClient();
            services.AddSingleton<IHttpClientFactory>(sp => 
            {
                // Replace default factory with test factory
                var options = sp.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var filters = sp.GetServices<IHttpMessageHandlerBuilderFilter>();
                return new TestHttpClientFactory(sp, scopeFactory, options, filters);
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = (TestHttpClientFactory)serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Act - create clients with different names to avoid handler reuse
            for (int i = 0; i < ClientCount; i++)
            {
                using HttpClient client = factory.CreateClient($"test{i}");
                // No need to make actual HTTP requests
            }

            // Force handlers to be created
            factory.CreateHandlersForTesting(ClientCount);

            // Pre-check: verify handlers were created
            Assert.Equal(ClientCount, disposeCounter.Created);

            // Act - dispose the service provider
            serviceProvider.Dispose();

            // Assert - all handlers should be disposed
            Assert.Equal(disposeCounter.Created, disposeCounter.Disposed);
        }

        private class DisposeCounter
        {
            private int _created;
            private int _disposed;

            public int Created => _created;
            public int Disposed => _disposed;

            public void IncrementCreated()
            {
                Interlocked.Increment(ref _created);
            }

            public void IncrementDisposed()
            {
                Interlocked.Increment(ref _disposed);
            }
        }

        private class DisposeTrackingHandler : DelegatingHandler
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

            // Create handlers immediately without waiting for expiry
            public void CreateHandlersForTesting(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    // Create with direct handler creation to ensure we get our tracking handler
                    var options = new HttpClientFactoryOptions
                    {
                        HttpMessageHandlerBuilderActions =
                        {
                            builder => builder.AdditionalHandlers.Add(new DisposeTrackingHandler(_counter))
                        }
                    };

                    var handlerBuilder = _services.GetRequiredService<HttpMessageHandlerBuilder>();
                    handlerBuilder.Name = $"test{i}";

                    foreach (var action in options.HttpMessageHandlerBuilderActions)
                    {
                        action(handlerBuilder);
                    }

                    var handler = new LifetimeTrackingHttpMessageHandler(handlerBuilder.Build());
                    var scope = _scopeFactory.CreateScope();
                    var entry = new ActiveHandlerTrackingEntry($"test{i}", handler, scope, TimeSpan.FromSeconds(60));
                    
                    // Add the entry to the active handlers collection
                    _activeHandlers.TryAdd($"test{i}", new Lazy<ActiveHandlerTrackingEntry>(() => entry));
                }
            }
        }
    }
}
