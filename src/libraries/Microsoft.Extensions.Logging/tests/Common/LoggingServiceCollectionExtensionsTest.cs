// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
