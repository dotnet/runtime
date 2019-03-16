// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting
{
    public partial class ConsoleLifetimeOptions
    {
        public ConsoleLifetimeOptions() { }
        public bool SuppressStatusMessages { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class Host
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder CreateDefaultBuilder() { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder CreateDefaultBuilder(string[] args) { throw null; }
    }
    public partial class HostBuilder : Microsoft.Extensions.Hosting.IHostBuilder
    {
        public HostBuilder() { }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Hosting.IHost Build() { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureAppConfiguration(System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureContainer<TContainerBuilder>(System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, TContainerBuilder> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureHostConfiguration(System.Action<Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder ConfigureServices(System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.DependencyInjection.IServiceCollection> configureDelegate) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> factory) { throw null; }
        public Microsoft.Extensions.Hosting.IHostBuilder UseServiceProviderFactory<TContainerBuilder>(System.Func<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder>> factory) { throw null; }
    }
    public static partial class HostingHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureAppConfiguration(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureContainer<TContainerBuilder>(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<TContainerBuilder> configureDelegate) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureLogging(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.Logging.ILoggingBuilder> configureLogging) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureLogging(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Logging.ILoggingBuilder> configureLogging) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureServices(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.DependencyInjection.IServiceCollection> configureDelegate) { throw null; }
        public static System.Threading.Tasks.Task RunConsoleAsync(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> configureOptions, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task RunConsoleAsync(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseConsoleLifetime(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseConsoleLifetime(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> configureOptions) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseContentRoot(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, string contentRoot) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseDefaultServiceProvider(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseDefaultServiceProvider(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.HostBuilderContext, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseEnvironment(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, string environment) { throw null; }
    }
    public partial class HostOptions
    {
        public HostOptions() { }
        public System.TimeSpan ShutdownTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
    public partial class ConsoleLifetime : Microsoft.Extensions.Hosting.IHostLifetime, System.IDisposable
    {
        public ConsoleLifetime(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> options, Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime) { }
        public ConsoleLifetime(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions> options, Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitForStartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class HostingEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment, Microsoft.Extensions.Hosting.IHostingEnvironment
    {
        public HostingEnvironment() { }
        public string ApplicationName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ContentRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EnvironmentName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
