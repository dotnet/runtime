// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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
        public void ClearProviders_RemovesAllProvidersFromServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());

            services.AddLogging(builder => builder.ClearProviders());

            Assert.Empty(services.Where(desctriptor => desctriptor.ServiceType == typeof(ILoggerProvider)));
        }
    }
}
