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

        [Fact]
        public void AddLogging_InjectScopeProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(IExternalScopeProvider), Mock.Of<IExternalScopeProvider>());

            var callbackCalled = false;
            var loggerBuilder = services.AddLogging(builder =>
            {
                callbackCalled = true;
            });

            Assert.True(callbackCalled);
        }

        [Fact]
        public void AddLogging_TestInjectedScopeProvider()
        {
            var testScope = "test scope";
            var services = new ServiceCollection();
            var externalScopeProvider = new Mock<IExternalScopeProvider>();
            var callbackCalled = false;
            externalScopeProvider
                .Setup(e => e.Push(It.IsAny<object?>()))
                .Callback(() => callbackCalled = true);

            services.AddSingleton(externalScopeProvider.Object);
            var loggerBuilder = services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<LoggingServiceCollectionExtensionsTest>();
            logger.BeginScope(testScope);

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
