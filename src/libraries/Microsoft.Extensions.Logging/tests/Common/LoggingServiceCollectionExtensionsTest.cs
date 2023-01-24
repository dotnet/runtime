// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggingServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddLogging_WrapsServiceCollection()
        {
            var services = new ServiceCollection();

            var callbackCalled = false;
            var loggerBuilder = services.AddLogging(builder =>
            {
                callbackCalled = true;
                Assert.Same(services, builder.Services);
            });
            Assert.True(callbackCalled);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void AddLogging_TestInjectedScopeProvider()
        {
            bool callbackCalled = false;
            var externalScopeProvider = new Mock<IExternalScopeProvider>();
            externalScopeProvider
                .Setup(e => e.Push(It.IsAny<object?>()))
                .Callback(() => callbackCalled = true);
      
            var serviceProvider = new ServiceCollection()
                .AddSingleton(externalScopeProvider.Object)
                .AddLogging()
                .BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<LoggingServiceCollectionExtensionsTest>();
            logger.BeginScope("test scope");

            Assert.True(callbackCalled);
        }

        [Fact]
        public void ClearProviders_RemovesAllProvidersFromServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());

            services.AddLogging(builder => builder.ClearProviders());

            Assert.Empty(services.Where(desctriptor => desctriptor.ServiceType == typeof(ILoggerProvider)));
        }
    }
}
