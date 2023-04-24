// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class ConsoleLifetime : IHostLifetime
    {
        private readonly ManualResetEvent _shutdownBlock = new ManualResetEvent(false);

        private partial void RegisterShutdownHandlers()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            ApplicationLifetime.StopApplication();

            // Don't block in process shutdown for CTRL+C/SIGINT since we can set e.Cancel to true
            // we assume that application code will unwind once StopApplication signals the token
            _shutdownBlock.Set();
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            ApplicationLifetime.StopApplication();
            if (!_shutdownBlock.WaitOne(HostOptions.ShutdownTimeout))
            {
                Logger.LogInformation("Waiting for the host to be disposed. Ensure all 'IHost' instances are wrapped in 'using' blocks.");
            }

            // wait one more time after the above log message, but only for ShutdownTimeout, so it doesn't hang forever
            _shutdownBlock.WaitOne(HostOptions.ShutdownTimeout);

            // On Linux if the shutdown is triggered by SIGTERM then that's signaled with the 143 exit code.
            // Suppress that since we shut down gracefully. https://github.com/dotnet/aspnetcore/issues/6526
            System.Environment.ExitCode = 0;
        }

        private partial void UnregisterShutdownHandlers()
        {
            _shutdownBlock.Set();

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
    }
}
