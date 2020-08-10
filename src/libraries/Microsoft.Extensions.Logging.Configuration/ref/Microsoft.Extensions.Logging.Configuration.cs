// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

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
