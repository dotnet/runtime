// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerProviderConfigurationFactoryTest
    {
        [Fact]
        public void ChangeTokenFiresWhenSectionAdded()
        {
            var callbackCalled = false;
            var source = new MemoryConfigurationSource();
            var configuration = new ConfigurationBuilder().Add(source).Build();
            var provider = (MemoryConfigurationProvider) configuration.Providers.Single();

            var serviceCollection = new  ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddConfiguration(configuration));
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var loggerProviderConfiguration = serviceProvider.GetService<ILoggerProviderConfiguration<ConsoleLoggerProvider>>();
            loggerProviderConfiguration.Configuration.GetReloadToken().RegisterChangeCallback(o => callbackCalled = true, null);

            provider.Add("Console:IncludeScopes", "false");
            configuration.Reload();

            Assert.True(callbackCalled);
        }
    }
}
