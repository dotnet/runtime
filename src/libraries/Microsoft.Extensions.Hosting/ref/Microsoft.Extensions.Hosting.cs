// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OptionsBuilderExtensions
    {
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> ValidateOnStart<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>(this Microsoft.Extensions.Options.OptionsBuilder<TOptions> optionsBuilder) where TOptions : class { throw null; }
    }
}
namespace Microsoft.Extensions.Hosting
{
    public enum BackgroundServiceExceptionBehavior
    {
        StopHost = 0,
        Ignore = 1,
    }
    public partial class ConsoleLifetimeOptions
    {
        public ConsoleLifetimeOptions() { }
        public bool SuppressStatusMessages { get { throw null; } set { } }
    }
    public static partial class Host
    {
        public static Microsoft.Extensions.Hosting.HostApplicationBuilder CreateApplicationBuilder() { throw null; }
        public static Microsoft.Extensions.Hosting.HostApplicationBuilder CreateApplicationBuilder(Microsoft.Extensions.Hosting.HostApplicationBuilderSettings? settings) { throw null; }
        public static Microsoft.Extensions.Hosting.HostApplicationBuilder CreateApplicationBuilder(string[]? args) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder CreateDefaultBuilder() { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder CreateDefaultBuilder(string[]? args) { throw null; }
        public static Microsoft.Extensions.Hosting.HostApplicationBuilder CreateEmptyApplicationBuilder(Microsoft.Extensions.Hosting.HostApplicationBuilderSettings? settings) { throw null; }
    }
    public sealed partial class HostApplicationBuilder
    {
        public HostApplicationBuilder() { }
        public HostApplicationBuilder(Microsoft.Extensions.Hosting.HostApplicationBuilderSettings? settings) { }
        public HostApplicationBuilder(string[]? args) { }
        public Microsoft.Extensions.Configuration.ConfigurationManager Configuration { get { throw null; } }
        public Microsoft.Extensions.Hosting.IHostEnvironment Environment { get { throw null; } }
        public Microsoft.Extensions.Logging.ILoggingBuilder Logging { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get { throw null; } }
        public Microsoft.Extensions.Hosting.IHost Build() { throw null; }
        public void ConfigureContainer<TContainerBuilder>(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> factory, System.Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull { }
    }
    public sealed partial class HostApplicationBuilderSettings
    {
        public HostApplicationBuilderSettings() { }
        public string? ApplicationName { get { throw null; } set { } }
        public string[]? Args { get { throw null; } set { } }
        public Microsoft.Extensions.Configuration.ConfigurationManager? Configuration { get { throw null; } set { } }
        public string? ContentRootPath { get { throw null; } set { } }
        public bool DisableDefaults { get { throw null; } set { } }
        public string? EnvironmentName { get { throw null; } set { } }
    }
    public partial class HostBuilder : Microsoft.Extensions.Hosting.IHostBuilder
    {
        public HostBuilder() { }
        public System.Collections.Generic.IDictionary<object, object> Properties { get { throw null; } }
        public Microsoft.Extensions.Hosting.IHost Build() { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureAppConfiguration(System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureContainer<TContainerBuilder>(System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, TContainerBuilder> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureHostConfiguration(System.Action<Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureServices(System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.DependencyInjection.IServiceCollection> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder UseServiceProviderFactory<TContainerBuilder>(System.Func<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull { throw null; }
    }
    public static partial class HostingHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureAppConfiguration(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureContainer<TContainerBuilder>(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<TContainerBuilder> configureDelegate) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureDefaults(this Microsoft.Extensions.Hosting.IHostBuilder builder, string[]? args) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureHostOptions(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.Hosting.HostOptions> configureOptions) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureHostOptions(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.HostOptions> configureOptions) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureLogging(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.Logging.ILoggingBuilder> configureLogging) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureLogging(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Logging.ILoggingBuilder> configureLogging) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureServices(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.DependencyInjection.IServiceCollection> configureDelegate) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Threading.Tasks.Task RunConsoleAsync(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> configureOptions, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Threading.Tasks.Task RunConsoleAsync(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static Microsoft.Extensions.Hosting.IHostBuilder UseConsoleLifetime(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static Microsoft.Extensions.Hosting.IHostBuilder UseConsoleLifetime(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> configureOptions) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseContentRoot(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, string contentRoot) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseDefaultServiceProvider(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseDefaultServiceProvider(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseEnvironment(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, string environment) { throw null; }
    }
    public partial class HostOptions
    {
        public HostOptions() { }
        public Microsoft.Extensions.Hosting.BackgroundServiceExceptionBehavior BackgroundServiceExceptionBehavior { get { throw null; } set { } }
        public bool ServicesStartConcurrently { get { throw null; } set { } }
        public bool ServicesStopConcurrently { get { throw null; } set { } }
        public System.TimeSpan ShutdownTimeout { get { throw null; } set { } }
    }
}
namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class ApplicationLifetime : Microsoft.Extensions.Hosting.IApplicationLifetime, Microsoft.Extensions.Hosting.IHostApplicationLifetime
    {
        public ApplicationLifetime(Microsoft.Extensions.Logging.ILogger<Microsoft.Extensions.Hosting.Internal.ApplicationLifetime> logger) { }
        public System.Threading.CancellationToken ApplicationStarted { get { throw null; } }
        public System.Threading.CancellationToken ApplicationStopped { get { throw null; } }
        public System.Threading.CancellationToken ApplicationStopping { get { throw null; } }
        public void NotifyStarted() { }
        public void NotifyStopped() { }
        public void StopApplication() { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
    public partial class ConsoleLifetime : Microsoft.Extensions.Hosting.IHostLifetime, System.IDisposable
    {
        public ConsoleLifetime(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> options, Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.HostOptions> hostOptions) { }
        public ConsoleLifetime(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> options, Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.HostOptions> hostOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitForStartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class HostingEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment, Microsoft.Extensions.Hosting.IHostingEnvironment
    {
        public HostingEnvironment() { }
        public string ApplicationName { get { throw null; } set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get { throw null; } set { } }
        public string ContentRootPath { get { throw null; } set { } }
        public string EnvironmentName { get { throw null; } set { } }
    }
}
