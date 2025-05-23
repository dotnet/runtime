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
        public async Task DisposingServiceProvider_DisposesHttpClientFactory_ReleasesResources()
        {
            // Arrange
            var disposeCounter = new DisposeCounter();
            var services = new ServiceCollection();
            services.AddSingleton(disposeCounter);
            
            // Add HttpClient services
            services.AddHttpClient("test-client", client => { })
                .ConfigurePrimaryHttpMessageHandler(() => new DisposeTrackingHandler(disposeCounter));
            
            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            
            // Create clients to initialize handlers
            for (int i = 0; i < ClientCount; i++)
            {
                var client = factory.CreateClient("test-client");
                
                // Use the client to ensure the handler is created
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            }
            
            // Verify handlers were created
            Assert.Equal(ClientCount, disposeCounter.Created);
            
            // Act - dispose the service provider which should dispose the factory
            serviceProvider.Dispose();
            
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
    }
}
