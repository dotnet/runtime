// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        /// <summary>
        /// Verifies that the TypeForwardedTo attribute is defined correctly by ensuring we can
        /// reference the ILoggingBuilder type through the Microsoft.Extensions.Logging.dll.
        /// </summary>
        [Fact]
        public void TypeForwardIsCorrect()
        {
            Type builderType = Type.GetType("Microsoft.Extensions.Logging.ILoggingBuilder, Microsoft.Extensions.Logging");
            Assert.Equal(typeof(ILoggingBuilder), builderType);
        }
    }
}
