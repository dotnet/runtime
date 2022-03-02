// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// This file is auto-generated and any changes to it will be lost.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Hosting
{
    public static partial class WindowsServiceLifetimeHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder UseWindowsService(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder) { throw null; }
        public static Microsoft.Extensions.Hosting.IHostBuilder UseWindowsService(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Hosting.WindowsServiceLifetimeOptions> configure) { throw null; }
    }
    public partial class WindowsServiceLifetimeOptions
    {
        public WindowsServiceLifetimeOptions() { }
        public string ServiceName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.Extensions.Hosting.WindowsServices
{
    public static partial class WindowsServiceHelpers
    {
        [System.Runtime.Versioning.SupportedOSPlatformGuardAttribute("windows")]
        public static bool IsWindowsService() { throw null; }
    }
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
    public partial class WindowsServiceLifetime : System.ServiceProcess.ServiceBase, Microsoft.Extensions.Hosting.IHostLifetime
    {
        public WindowsServiceLifetime(Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.HostOptions> optionsAccessor) { }
        public WindowsServiceLifetime(Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.HostOptions> optionsAccessor, Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor) { }
        protected override void Dispose(bool disposing) { }
        protected override void OnShutdown() { }
        protected override void OnStart(string[] args) { }
        protected override void OnStop() { }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitForStartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
