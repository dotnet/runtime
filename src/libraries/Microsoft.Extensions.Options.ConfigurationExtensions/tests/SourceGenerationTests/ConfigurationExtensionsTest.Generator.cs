// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    public partial class ConfigurationExtensionsTests
    {
        // These are regression tests for https://github.com/dotnet/runtime/issues/90851
        // Source Generator Interceptors rely on identifying an accurate invocation
        // source location (line and character positions). These tests cover newline
        // and whitespace scenarios to ensure the interceptors get wired up correctly.

        [Fact]
        public void TestBindingInvocationsWithNewlines_BindExtension()
        {
            OptionsBuilder<FakeOptions>? optionsBuilder = CreateOptionsBuilder();

            // Newline between instance and invocation using configureBinder argument (with the dot on the first line)
            optionsBuilder.
                Bind(s_emptyConfig, configureBinder: null);

            // Newline between instance and invocation using configureBinder argument (with the dot on the second line)
            optionsBuilder
                .Bind(s_emptyConfig, configureBinder: null);

            // Newline between instance and invocation (with the dot on the first line)
            optionsBuilder.
                Bind(s_emptyConfig);

            // Newline between instance and invocation (with the dot on the second line)
            optionsBuilder
                .Bind(s_emptyConfig);

            // Newlines in every place possible
            optionsBuilder
                .
                Bind
                (
                    s_emptyConfig
                    ,
                    configureBinder
                    :
                    null
                )
                ;
        }

        [Fact]
        public void TestBindingInvocationsWithNewlines_BindConfigurationExtension()
        {
            OptionsBuilder<FakeOptions>? optionsBuilder = CreateOptionsBuilder();

            // Newline between instance and invocation using configureBinder argument (with the dot on the first line)
            optionsBuilder.
                BindConfiguration(configSectionPath: "path",
                _ => { });

            // Newline between instance and invocation using configureBinder argument (with the dot on the second line)
            optionsBuilder
                .BindConfiguration(configSectionPath: "path",
                _ => { });

            // Newlines between the instance and invocation and within the arguments. No indentation before invocation.
            optionsBuilder.
            BindConfiguration(
                configSectionPath: "path",
                _ => { });

            // Newlines in every place possible
            optionsBuilder
                .
                BindConfiguration
                (
                    configSectionPath
                    :
                    "path"
                    ,
                    _
                    =>
                    {
                    }
                )
                ;
        }

        [Fact]
        public void TestBindingInvocationsWithNewlines_ConfigureExtension()
        {
            OptionsBuilder<FakeOptions>? optionsBuilder = CreateOptionsBuilder();
            IServiceCollection services = new ServiceCollection();

            // Newlines between each method call
            services
                .Configure<FakeOptions>(s_emptyConfig)
                .AddOptions<FakeOptions>();

            // Newlines in every place possible
            services
                .
                Configure
                <
                    FakeOptions
                >
                (
                    name
                    :
                    null!
                    ,
                    s_emptyConfig
                )
                ;
        }

        [Fact]
        public void TestBindingInvocationsWithNewlines_StaticCalls()
        {
            OptionsBuilder<FakeOptions>? optionsBuilder = CreateOptionsBuilder();
            IServiceCollection services = new ServiceCollection();

            // Bind: Newlines in every place possible
            OptionsBuilderConfigurationExtensions
                .
                Bind
                (
                    optionsBuilder
                    ,
                    s_emptyConfig
                )
                ;

            // // BindConfiguration: Newlines in every place possible
            OptionsBuilderConfigurationExtensions
                .
                BindConfiguration
                (
                    optionsBuilder
                    ,
                    "path"
                );

            // Configure: Newlines in every place possible
            OptionsConfigurationServiceCollectionExtensions
                .
                Configure
                <
                    FakeOptions
                >
                (
                    services
                    ,
                    s_emptyConfig
                )
                ;
        }

        [Fact]
        public void TestBindAndConfigureWithNamedParameters()
        {
            OptionsBuilder<FakeOptions>? optionsBuilder = CreateOptionsBuilder();
            IServiceCollection services = new ServiceCollection();

            OptionsBuilderConfigurationExtensions.Bind(config: s_emptyConfig, optionsBuilder: optionsBuilder);
            OptionsBuilderConfigurationExtensions.Bind(configureBinder: _ => { }, config: s_emptyConfig, optionsBuilder: optionsBuilder);

            OptionsBuilderConfigurationExtensions.BindConfiguration(configureBinder: _ => { }, configSectionPath: "path", optionsBuilder: optionsBuilder);

            OptionsConfigurationServiceCollectionExtensions.Configure<FakeOptions>(config: s_emptyConfig, services: services);
            OptionsConfigurationServiceCollectionExtensions.Configure<FakeOptions>(name: "", config: s_emptyConfig, services: services);
            OptionsConfigurationServiceCollectionExtensions.Configure<FakeOptions>(configureBinder: _ => { }, config: s_emptyConfig, services: services);
            OptionsConfigurationServiceCollectionExtensions.Configure<FakeOptions>(name: "", configureBinder: _ => { }, config: s_emptyConfig, services: services);
        }
    }
}
