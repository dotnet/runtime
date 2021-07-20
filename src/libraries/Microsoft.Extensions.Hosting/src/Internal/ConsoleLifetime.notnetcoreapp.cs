// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class ConsoleLifetime : IHostLifetime
    {
        private partial void RegisterShutdownHandlers()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            ApplicationLifetime.StopApplication();

            if (!_shutdownBlock.WaitOne(HostOptions.ShutdownTimeout))
            {
                Logger.LogInformation("Waiting for the host to be disposed. Ensure all 'IHost' instances are wrapped in 'using' blocks.");
            }

            // wait one more time after the above error message, but only for ShutdownTimeout, so it doesn't hang forever
            _shutdownBlock.WaitOne(HostOptions.ShutdownTimeout);

            // On Linux if the shutdown is triggered by SIGTERM then that's signaled with the 143 exit code.
            // Suppress that since we shut down gracefully. https://github.com/dotnet/aspnetcore/issues/6526

            // This only applies to non-net6.0+, since in net6.0 we added a handler specific for SIGTERM,
            // so we don't need to reset the ExitCode in ProcessExit anymore.
            System.Environment.ExitCode = 0;
        }

        private partial void UnregisterShutdownHandlers()
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }
    }
}
