// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    public class OptionsBuidlerConfigurationExtensionsTests
    {
        [Fact]
        public static void BindConfiguration_ThrowsForNullBuilder()
        {
            OptionsBuilder<FakeOptions> optionsBuilder = null!;

            Assert.Throws<ArgumentNullException>("optionsBuilder", () =>
            {
                optionsBuilder.BindConfiguration("test");
            });
        }

        [Fact]
        public static void BindConfiguration_ThrowsForNullConfigurationSectionPath()
        {
            var services = new ServiceCollection();
            var optionsBuilder = new OptionsBuilder<FakeOptions>(services, Options.DefaultName);
            string configSectionPath = null!;

            Assert.Throws<ArgumentNullException>("configSectionPath", () =>
            {
                optionsBuilder.BindConfiguration(configSectionPath);
            });
        }

        [Fact]
        public static void BindConfiguration_ReturnsSameBuilderInstance()
        {
            var services = new ServiceCollection();
            var optionsBuilder = new OptionsBuilder<FakeOptions>(services, Options.DefaultName);

            var returnedBuilder = optionsBuilder.BindConfiguration("Test");

            Assert.Same(optionsBuilder, returnedBuilder);
        }

        [Fact]
        public static void BindConfiguration_OptionsMaterializationThrowsIfNoConfigurationInDI()
        {
            var services = new ServiceCollection();
            var optionsBuilder = services.AddOptions<FakeOptions>();

            _ = optionsBuilder.BindConfiguration("Test");
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Assert.ThrowsAny<InvalidOperationException>(() =>
            {
                _ = serviceProvider.GetRequiredService<IOptions<FakeOptions>>();
            });
        }

        [Fact]
        public static void BindConfiguration_UsesConfigurationSectionPath()
        {
            const string configSectionName = "Test";
            const string messageValue = "This is a test";
            var configEntries = new Dictionary<string, string>
            {
                [ConfigurationPath.Combine(configSectionName, nameof(FakeOptions.Message))] = messageValue
            };
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(configEntries)
                .Build());
            var optionsBuilder = services.AddOptions<FakeOptions>();

            _ = optionsBuilder.BindConfiguration(configSectionName);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal(messageValue, options.Message);
        }

        [Fact]
        public static void BindConfiguration_UsesConfigurationRootIfSectionNameIsEmptyString()
        {
            const string messageValue = "This is a test";
            var configEntries = new Dictionary<string, string>
            {
                [nameof(FakeOptions.Message)] = messageValue
            };
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(configEntries)
                .Build());
            var optionsBuilder = services.AddOptions<FakeOptions>();

            _ = optionsBuilder.BindConfiguration(configSectionPath: "");

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal(messageValue, options.Message);
        }
    }
}
