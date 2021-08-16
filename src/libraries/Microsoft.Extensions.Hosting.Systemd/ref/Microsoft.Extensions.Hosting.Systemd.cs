// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Hosting
{
    public static partial class SystemdHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder UseSystemd(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder) { throw null; }
    }
}
namespace Microsoft.Extensions.Hosting.Systemd
{
    public partial interface ISystemdNotifier
    {
        bool IsEnabled { get; }
        void Notify(Microsoft.Extensions.Hosting.Systemd.ServiceState state);
    }
    public partial struct ServiceState
    {
        private object _dummy;
        private int _dummyPrimitive;
        public static readonly Microsoft.Extensions.Hosting.Systemd.ServiceState Ready;
        public static readonly Microsoft.Extensions.Hosting.Systemd.ServiceState Stopping;
        public ServiceState(string state) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class SystemdHelpers
    {
        public static bool IsSystemdService() { throw null; }
    }
    public partial class SystemdLifetime : Microsoft.Extensions.Hosting.IHostLifetime, System.IDisposable
    {
        public SystemdLifetime(Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Hosting.Systemd.ISystemdNotifier systemdNotifier, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitForStartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class SystemdNotifier : Microsoft.Extensions.Hosting.Systemd.ISystemdNotifier
    {
        public SystemdNotifier() { }
        public bool IsEnabled { get { throw null; } }
        public void Notify(Microsoft.Extensions.Hosting.Systemd.ServiceState state) { }
    }
}
