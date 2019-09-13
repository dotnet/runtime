// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    public static partial class LoggingBuilderExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConfiguration(this Microsoft.Extensions.Logging.ILoggingBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.Configuration
{
    public partial interface ILoggerProviderConfigurationFactory
    {
        Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(System.Type providerType);
    }
    public partial interface ILoggerProviderConfiguration<T>
    {
        Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }
    }
    public static partial class LoggerProviderOptions
    {
        public static void RegisterProviderOptions<TOptions, TProvider>(Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TOptions : class { }
    }
    public partial class LoggerProviderOptionsChangeTokenSource<TOptions, TProvider> : Microsoft.Extensions.Options.ConfigurationChangeTokenSource<TOptions>
    {
        public LoggerProviderOptionsChangeTokenSource(Microsoft.Extensions.Logging.Configuration.ILoggerProviderConfiguration<TProvider> providerConfiguration) : base (default(Microsoft.Extensions.Configuration.IConfiguration)) { }
    }
    public static partial class LoggingBuilderConfigurationExtensions
    {
        public static void AddConfiguration(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { }
    }
}
