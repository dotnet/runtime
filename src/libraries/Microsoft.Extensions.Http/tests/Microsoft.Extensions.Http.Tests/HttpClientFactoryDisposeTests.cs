// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Http.Tests
{
    public class HttpClientFactoryDisposeTests
    {
        [Fact]
        public async Task DisposingServiceProvider_DisposesHttpClientFactory_ReleasesResources()
        {
            // Arrange
            var disposeCounter = new DisposeCounter();
            var services = new ServiceCollection();
            services.AddSingleton(disposeCounter);
            services.AddTransient<DisposeTrackingHandler>();

            services.AddHttpClient("test")
                .AddHttpMessageHandler<DisposeTrackingHandler>()
                .SetHandlerLifetime(TimeSpan.FromMilliseconds(50)); // Very short to ensure quick expiration

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Act - create and use clients
            for (int i = 0; i < 5; i++)
            {
                using var client = factory.CreateClient("test");
                try
                {
                    using var cts = new CancellationTokenSource(millisecondsDelay: 1);
                    await client.GetAsync("http://example.com/", cts.Token)
                        .ContinueWith(t => { }); // Ignore errors
                }
                catch
                {
                    // Ignore errors
                }
            }

            // Wait for handlers to expire
            await Task.Delay(100);
            
            // Pre-check: verify handlers were created
            Assert.Equal(5, disposeCounter.Created);

            // Act - dispose the service provider
            serviceProvider.Dispose();

            // Assert - all handlers should be disposed
            Assert.Equal(disposeCounter.Created, disposeCounter.Disposed);

            // No lingering resources should exist - no need for garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.Equal(disposeCounter.Created, disposeCounter.Disposed);
        }

        private class DisposeCounter
        {
            public int Created { get; set; }
            public int Disposed { get; set; }
        }

        private class DisposeTrackingHandler : DelegatingHandler
        {
            private readonly DisposeCounter _counter;

            public DisposeTrackingHandler(DisposeCounter counter)
            {
                _counter = counter;
                Interlocked.Increment(ref _counter.Created);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Interlocked.Increment(ref _counter.Disposed);
                }

                base.Dispose(disposing);
            }
        }
    }
}