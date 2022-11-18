// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    public class OptionsBuilderConfigurationExtensionsTests
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
            OptionsBuilder<FakeOptions> optionsBuilder = new(services, Options.DefaultName);
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
            OptionsBuilder<FakeOptions> optionsBuilder = new(services, Options.DefaultName);

            OptionsBuilder<FakeOptions> returnedBuilder = optionsBuilder.BindConfiguration("Test");

            Assert.Same(optionsBuilder, returnedBuilder);
        }

        [Fact]
        public static void BindConfiguration_OptionsMaterializationThrowsIfNoConfigurationInDI()
        {
            var services = new ServiceCollection();
            OptionsBuilder<FakeOptions> optionsBuilder = services.AddOptions<FakeOptions>();

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
            var configEntries = new Dictionary<string, string?>
            {
                [ConfigurationPath.Combine(configSectionName, nameof(FakeOptions.Message))] = messageValue
            };
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(configEntries)
                .Build());
            OptionsBuilder<FakeOptions> optionsBuilder = services.AddOptions<FakeOptions>();

            _ = optionsBuilder.BindConfiguration(configSectionName);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            FakeOptions options = serviceProvider.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal(messageValue, options.Message);
        }

        [Fact]
        public static void BindConfiguration_UsesConfigurationRootIfSectionNameIsEmptyString()
        {
            const string messageValue = "This is a test";
            var configEntries = new Dictionary<string, string?>
            {
                [nameof(FakeOptions.Message)] = messageValue
            };
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(configEntries)
                .Build());
            OptionsBuilder<FakeOptions> optionsBuilder = services.AddOptions<FakeOptions>();

            _ = optionsBuilder.BindConfiguration(configSectionPath: "");

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            FakeOptions options = serviceProvider.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal(messageValue, options.Message);
        }

        [Fact]
        public static void BindConfiguration_UpdatesOptionOnConfigurationUpdateWithEmptySectionName()
        {
            const string messageValue1 = "This is a test";
            const string messageValue2 = "This is the message after update";

            FakeConfigurationSource configSource = new()
            {
                InitialData = new Dictionary<string, string?>
                {
                    [nameof(FakeOptions.Message)] = messageValue1,
                }
            };

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                    .Add(configSource)
                    .Build());
            OptionsBuilder<FakeOptions> optionsBuilder = services.AddOptions<FakeOptions>();
            _ = optionsBuilder.BindConfiguration(configSectionPath: "");
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            bool updateHasRun = false;
            optionsMonitor.OnChange((opts, name) =>
            {
                updateHasRun = true;
            });
            FakeOptions optionsValue1 = optionsMonitor.CurrentValue;
            Assert.Equal(messageValue1, optionsValue1.Message);
            configSource.Provider.Set(nameof(FakeOptions.Message), messageValue2);
            FakeOptions optionsValue2 = optionsMonitor.CurrentValue;
            Assert.True(updateHasRun);
            Assert.Equal(messageValue2, optionsValue2.Message);
        }

        [Fact]
        public static void BindConfiguration_UpdatesOptionOnConfigurationUpdate()
        {
            const string configSectionName = "Test";
            string messageConfigKey = ConfigurationPath.Combine(configSectionName, nameof(FakeOptions.Message));
            const string messageValue1 = "This is a test";
            const string messageValue2 = "This is the message after update";

            FakeConfigurationSource configSource = new()
            {
                InitialData = new Dictionary<string, string?>
                {
                    [messageConfigKey] = messageValue1
                }
            };

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                    .Add(configSource)
                    .Build());
            OptionsBuilder<FakeOptions> optionsBuilder = services.AddOptions<FakeOptions>();
            _ = optionsBuilder.BindConfiguration(configSectionName);
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            bool updateHasRun = false;
            optionsMonitor.OnChange((opts, name) =>
            {
                updateHasRun = true;
            });
            FakeOptions optionsValue1 = optionsMonitor.CurrentValue;
            Assert.Equal(messageValue1, optionsValue1.Message);
            configSource.Provider.Set(messageConfigKey, messageValue2);
            FakeOptions optionsValue2 = optionsMonitor.CurrentValue;
            Assert.True(updateHasRun);
            Assert.Equal(messageValue2, optionsValue2.Message);
        }
    }
}
