// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    public partial class ConfigurationExtensionsTests
    {
        private static IConfiguration s_emptyConfig { get; } = new ConfigurationBuilder().Build();

        [Fact]
        public void TestNullHandling_OptionsBuilderExt_Bind()
        {
            // Null options builder.
            OptionsBuilder<FakeOptions>? optionsBuilder = null;
            Assert.Throws<ArgumentNullException>(() => optionsBuilder!.Bind(s_emptyConfig));
            Assert.Throws<ArgumentNullException>(() => optionsBuilder!.Bind(s_emptyConfig, _ => { }));

            // Null configuration.
            optionsBuilder = CreateOptionsBuilder();
            Assert.Throws<ArgumentNullException>(() => optionsBuilder.Bind(config: null!));
            Assert.Throws<ArgumentNullException>(() => optionsBuilder.Bind(config: null!, _ => { }));

            // Null configureBinder.
            optionsBuilder.Bind(s_emptyConfig, configureBinder: null);
        }

        [Fact]
        public void TestNullHandling_OptionsBuilderExt_BindConfiguration()
        {
            // Null options builder.
            string configSectionPath = "FakeSectionPath";
            OptionsBuilder<FakeOptions>? optionsBuilder = null;
            Assert.Throws<ArgumentNullException>(() => optionsBuilder!.BindConfiguration(configSectionPath));

            // Null config section path.
            optionsBuilder = CreateOptionsBuilder();
            Assert.Throws<ArgumentNullException>(() => optionsBuilder.BindConfiguration(configSectionPath: null!));

            // Null configureBinder.
            optionsBuilder.BindConfiguration(configSectionPath, configureBinder: null);
        }

        [Fact]
        public void TestNullHandling_IServiceCollectionExt_Configure()
        {
            // Null services
            IServiceCollection? services = null;
            string name = "Name";
            Assert.Throws<ArgumentNullException>(() => services!.Configure<FakeOptions>(s_emptyConfig));
            Assert.Throws<ArgumentNullException>(() => services!.Configure<FakeOptions>(name, s_emptyConfig));

            // Null config.
            services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.Configure<FakeOptions>(config: null!));
            Assert.Throws<ArgumentNullException>(() => services.Configure<FakeOptions>(name, config: null!));

            // Null name.
            services.Configure<FakeOptions>(name: null!, s_emptyConfig);

            // Null configureBinder.
            services.Configure<FakeOptions>(s_emptyConfig, configureBinder: null);
            services.Configure<FakeOptions>(name, s_emptyConfig, configureBinder: null);
        }

        private static OptionsBuilder<FakeOptions> CreateOptionsBuilder()
        {
            var services = new ServiceCollection();
            return new OptionsBuilder<FakeOptions>(services, Options.DefaultName);
        }
    }
}
