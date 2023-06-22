// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    // These are mostly integration tests that verify the configuration experience.
    public class AddHttpClientDefaultsTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddHttpClientDefaults_MultipleCalls_SameInstance()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            var d1 = serviceCollection.AddHttpClientDefaults();
            var d2 = serviceCollection.AddHttpClientDefaults();

            // Assert
            Assert.Same(d1, d2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddHttpClientDefaults_WithNameConfig_NameConfigUsed()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act1
            serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com/"));
            serviceCollection.AddHttpClientDefaults().ConfigureHttpClient(c => c.BaseAddress = new Uri("http://default.com/"));

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient("example.com");

            // Assert
            Assert.NotNull(client);
            Assert.Equal("http://example.com/", client.BaseAddress.AbsoluteUri);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddHttpClientDefaults_MultipleConfig_LastWins()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act1
            serviceCollection.AddHttpClientDefaults()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://default1.com/"))
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://default2.com/"));

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient();

            // Assert
            Assert.NotNull(client);
            Assert.Equal("http://default2.com/", client.BaseAddress.AbsoluteUri);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddTypedClient_MultipleTimes_Error()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            var ex = Assert.ThrowsAny<InvalidOperationException>(() => serviceCollection.AddHttpClientDefaults().AddTypedClient<TestTypedClient>());

            // Assert
            Assert.Equal("AddTypedClient isn't supported with AddHttpClientDefaults.", ex.Message);
        }
    }
}
