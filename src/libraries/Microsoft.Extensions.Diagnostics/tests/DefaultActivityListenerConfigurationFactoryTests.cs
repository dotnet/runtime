// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tracing.Tests
{
    public class DefaultActivityListenerConfigurationFactoryTests
    {
        [Fact]
        public void GetConfigurationReturnsSectionScopedToListener()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MyListener:EnabledTracing:Default"] = "true",
                    ["OtherListener:EnabledTracing:Default"] = "false",
                })
                .Build();

            using ServiceProvider serviceProvider = BuildProvider(configuration);
            ActivityListenerConfigurationFactory factory = serviceProvider.GetRequiredService<ActivityListenerConfigurationFactory>();

            IConfiguration listenerConfiguration = factory.GetConfiguration("MyListener");

            Assert.Equal("true", listenerConfiguration["EnabledTracing:Default"]);
            Assert.Null(listenerConfiguration["OtherListener:EnabledTracing:Default"]);
        }

        [Fact]
        public void GetConfigurationMergesConfigurationsWithLastWinning()
        {
            IConfiguration first = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MyListener:EnabledTracing:Source"] = "true",
                    ["MyListener:EnabledTracing:Shared"] = "true",
                })
                .Build();
            IConfiguration second = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MyListener:EnabledTracing:Shared"] = "false",
                })
                .Build();

            using ServiceProvider serviceProvider = BuildProvider(first, second);
            ActivityListenerConfigurationFactory factory = serviceProvider.GetRequiredService<ActivityListenerConfigurationFactory>();

            IConfiguration listenerConfiguration = factory.GetConfiguration("MyListener");

            Assert.Equal("true", listenerConfiguration["EnabledTracing:Source"]);
            Assert.Equal("false", listenerConfiguration["EnabledTracing:Shared"]);
        }

        [Fact]
        public void GetConfigurationThrowsForNullListenerName()
        {
            using ServiceProvider serviceProvider = BuildProvider(new ConfigurationBuilder().Build());
            ActivityListenerConfigurationFactory factory = serviceProvider.GetRequiredService<ActivityListenerConfigurationFactory>();

            Assert.Throws<ArgumentNullException>(() => factory.GetConfiguration(null!));
        }

        private static ServiceProvider BuildProvider(params IConfiguration[] configurations) =>
            new ServiceCollection()
                .AddTracing(builder =>
                {
                    foreach (IConfiguration configuration in configurations)
                    {
                        builder.AddConfiguration(configuration);
                    }
                })
                .BuildServiceProvider();
    }
}
