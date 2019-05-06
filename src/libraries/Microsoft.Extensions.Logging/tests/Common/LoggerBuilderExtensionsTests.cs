// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerBuilderExtensionsTests
    {
        [Fact]
        public void AddConsole_BuilderExtensionAddsSingleSetOfServicesWhenCalledTwice()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddConsole());
            var count = serviceCollection.Count;
            serviceCollection.AddLogging(builder => builder.AddConsole());

            Assert.Equal(count, serviceCollection.Count);
        }

        [Fact]
        public void AddDebug_BuilderExtensionAddsSingleSetOfServicesWhenCalledTwice()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddDebug());
            var count = serviceCollection.Count;
            serviceCollection.AddLogging(builder => builder.AddDebug());

            Assert.Equal(count, serviceCollection.Count);
        }

        [Fact]
        public void CaptureScopesDefaultsToTrue()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddConfiguration(new ConfigurationBuilder().Build()));
            var options = serviceCollection.BuildServiceProvider().GetRequiredService<IOptions<LoggerFilterOptions>>();

            Assert.True(options.Value.CaptureScopes);
        }
    }
}
