// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    public partial class ConfigurationExtensionsTests
    {
        /// <summary>
        /// This is a regression test for https://github.com/dotnet/runtime/issues/90851.
        /// It asserts that the configuration binding source generator properly formats
        /// binding invocation source locations that the generated interceptors replace.
        /// A location issue that's surfaced is emitting the right location of invocations
        /// that are on a different line than the containing binder type or the static
        /// extension binder class (e.g. ConfigurationBinder.Bind).
        /// </summary>
        [Fact]
        public void TestBindingInvocationsWithIrregularCSharpSyntax()
        {
            // Tests binding invocation variants with irregular C# syntax, interspersed with white space.

            // Options builder extensions.

            OptionsBuilder<FakeOptions>? optionsBuilder = CreateOptionsBuilder();

            optionsBuilder
                .Bind(s_emptyConfig, configureBinder: null);

            optionsBuilder
                .Bind(s_emptyConfig);

            optionsBuilder.
                BindConfiguration(configSectionPath: "path",
                _ => { });

            optionsBuilder.
            BindConfiguration(
                configSectionPath: "path",
                _ => { });

            // Service collection extensions.

            IServiceCollection services = new ServiceCollection();

            services
                .Configure<
                    FakeOptions>(
                name: null!, s_emptyConfig);

            services
                .Configure<FakeOptions>(s_emptyConfig)
                .AddOptions<FakeOptions>();

            services.
                Configure<FakeOptions>(
                name: null, s_emptyConfig,
                configureBinder: null);

            // Test extensions class syntax.

            OptionsBuilderConfigurationExtensions
                .Bind(optionsBuilder, s_emptyConfig);

            OptionsBuilderConfigurationExtensions.
                BindConfiguration(optionsBuilder,
                "path");

            OptionsConfigurationServiceCollectionExtensions
                .Configure
                <FakeOptions>(services,
                s_emptyConfig);

            OptionsConfigurationServiceCollectionExtensions
                .Configure<FakeOptions
                >(
                services, s_emptyConfig
                );
        }
    }
}
