// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http
{
    public class PrimaryHandlerExposureTest
    {
        private bool IsPrimaryHandlerExposed(ServiceCollection serviceCollection, string name)
        {
            var services = serviceCollection.BuildServiceProvider();
            var optionsMonitor = services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();

            var options = optionsMonitor.Get(name);
            return options._primaryHandlerExposed;
        }

        [Fact]
        public void NotExposed()
        {
            var serviceCollection = new ServiceCollection();
            string name = "test";

            serviceCollection.AddHttpClient(name)
                .AddHttpMessageHandler(() => Mock.Of<DelegatingHandler>());

            // ---

            bool primaryHandlerExposed = IsPrimaryHandlerExposed(serviceCollection, name);

            Assert.False(primaryHandlerExposed);
        }

        [Fact]
        public void ExposedByConfigurePrimaryHandler()
        {
            var serviceCollection = new ServiceCollection();
            string name = "test";

            serviceCollection.AddHttpClient(name)
                .AddHttpMessageHandler(() => Mock.Of<DelegatingHandler>())
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()); // exposure

            // ---

            bool primaryHandlerExposed = IsPrimaryHandlerExposed(serviceCollection, name);

            Assert.True(primaryHandlerExposed);
        }

        [Fact]
        public void ExposedByConfigureBuilder()
        {
            var serviceCollection = new ServiceCollection();
            string name = "test";

            serviceCollection.AddHttpClient(name)
                .AddHttpMessageHandler(() => Mock.Of<DelegatingHandler>())
                .ConfigureHttpMessageHandlerBuilder(builder => { }); // exposure

            // ---

            bool primaryHandlerExposed = IsPrimaryHandlerExposed(serviceCollection, name);

            Assert.True(primaryHandlerExposed);
        }

        [Fact]
        public void ExposedByBuilderFilter()
        {
            var serviceCollection = new ServiceCollection();
            string name = "test";

            serviceCollection.AddHttpClient(name)
                .AddHttpMessageHandler(() => Mock.Of<DelegatingHandler>());

            serviceCollection.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, TestHandlerBuilderFilter>()); // exposure

            // ---

            bool primaryHandlerExposed = IsPrimaryHandlerExposed(serviceCollection, name);

            Assert.True(primaryHandlerExposed);
        }

        private class TestHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
        {
            public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
            {
                return builder => next(builder);
            }
        }
    }
}
