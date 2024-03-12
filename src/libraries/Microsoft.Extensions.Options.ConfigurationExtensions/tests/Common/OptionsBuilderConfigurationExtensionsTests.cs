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
                optionsBuilder
                    .BindConfiguration(configSectionPath);
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

#if !BUILDING_SOURCE_GENERATOR_TESTS
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            // This leads to an indirect call to ConfigurationBinder.Bind located in a different assembly. Not supported by source generator.
            FakeOptions options = serviceProvider.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal(messageValue, options.Message);
#endif
        }

        [Fact]
        public static void BindConfiguration_UpdatesOptionOnConfigurationUpdateWithEmptySectionName()
        {
            const string messageValue1 = "This is a test";

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

#if !BUILDING_SOURCE_GENERATOR_TESTS
            const string messageValue2 = "This is the message after update";

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            bool updateHasRun = false;
            optionsMonitor.OnChange((opts, name) =>
            {
                updateHasRun = true;
            });

            // This leads to an indirect call to ConfigurationBinder.Bind located in a different assembly. Not supported by source generator.
            FakeOptions optionsValue1 = optionsMonitor.CurrentValue;
            Assert.Equal(messageValue1, optionsValue1.Message);
            configSource.Provider.Set(nameof(FakeOptions.Message), messageValue2);
            FakeOptions optionsValue2 = optionsMonitor.CurrentValue;
            Assert.True(updateHasRun);
            Assert.Equal(messageValue2, optionsValue2.Message);
#endif
        }

        [Fact]
        public static void BindConfiguration_UpdatesOptionOnConfigurationUpdate()
        {
            const string configSectionNameDefaultName = "Test1";
            const string configSectionNameCustomName = "Test2";

            string messageConfigKeyDefaultName = ConfigurationPath.Combine(configSectionNameDefaultName, nameof(FakeOptions.Message));
            string messageConfigKeyCustomName = ConfigurationPath.Combine(configSectionNameCustomName, nameof(FakeOptions.Message));

            const string messageValueDefaultName1 = "This is a test (default options name)";
            const string messageValueDefaultName2 = "This is the message after update (default options name)";
            const string messageValueCustomName1 = "This is a test (custom options name)";
            const string messageValueCustomName2 = "This is the message after update (custom options name)";

            const string customOptionsName = "custom";

            FakeConfigurationSource configSource = new()
            {
                InitialData = new Dictionary<string, string?>
                {
                    [messageConfigKeyDefaultName] = messageValueDefaultName1,
                    [messageConfigKeyCustomName] = messageValueCustomName1
                }
            };

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .Add(configSource)
                .Build());
            _ = services.AddOptions<FakeOptions>()
                .BindConfiguration(configSectionNameDefaultName);
            _ = services.AddOptions<FakeOptions>(customOptionsName)
                .BindConfiguration(configSectionNameCustomName);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            var updatedOptionsNames = new HashSet<string?>();
            optionsMonitor.OnChange((opts, name) =>
            {
                updatedOptionsNames.Add(name);
            });

            FakeOptions optionsValueDefaultName1 = optionsMonitor.CurrentValue;
            Assert.Equal(messageValueDefaultName1, optionsValueDefaultName1.Message);
            FakeOptions optionsValueCustomName1 = optionsMonitor.Get(customOptionsName);
            Assert.Equal(messageValueCustomName1, optionsValueCustomName1.Message);

            configSource.Provider.Set(messageConfigKeyDefaultName, messageValueDefaultName2);
            configSource.Provider.Set(messageConfigKeyCustomName, messageValueCustomName2);

            FakeOptions optionsValueDefaultName2 = optionsMonitor.CurrentValue;
            Assert.Equal(messageValueDefaultName2, optionsValueDefaultName2.Message);
            FakeOptions optionsValueCustomName2 = optionsMonitor.Get(customOptionsName);
            Assert.Equal(messageValueCustomName2, optionsValueCustomName2.Message);

            Assert.Contains(Options.DefaultName, updatedOptionsNames);
            Assert.Contains(customOptionsName, updatedOptionsNames);
        }
    }
}
