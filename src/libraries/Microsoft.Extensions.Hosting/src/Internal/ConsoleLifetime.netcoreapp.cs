// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class ConsoleLifetime : IHostLifetime
    {
        private PosixSignalRegistration? _sigIntRegistration;
        private PosixSignalRegistration? _sigQuitRegistration;
        private PosixSignalRegistration? _sigTermRegistration;

        private partial void RegisterShutdownHandlers()
        {
            if (!OperatingSystem.IsWasi())
            {
                Action<PosixSignalContext> handler = HandlePosixSignal;
                _sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
                _sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
                _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OperatingSystem.IsWindows() ? HandleWindowsShutdown : handler);
            }
        }

        private void HandlePosixSignal(PosixSignalContext context)
        {
            Debug.Assert(context.Signal == PosixSignal.SIGINT || context.Signal == PosixSignal.SIGQUIT || context.Signal == PosixSignal.SIGTERM);

            context.Cancel = true;
            ApplicationLifetime.StopApplication();
        }

        private void HandleWindowsShutdown(PosixSignalContext context)
        {
            // for SIGTERM on Windows we must block this thread until the application is finished
            // otherwise the process will be killed immediately on return from this handler

            // don't allow Dispose to unregister handlers, since Windows has a lock that prevents the unregistration while this handler is running
            // just leak these, since the process is exiting
            _sigIntRegistration = null;
            _sigQuitRegistration = null;
            _sigTermRegistration = null;

            ApplicationLifetime.StopApplication();

            // We could wait for a signal here, like Dispose as is done in non-netcoreapp case, but those inevitably could have user
            // code that runs after them in the user's Main. Instead we just block this thread completely and let the main routine exit.
            Thread.Sleep(HostOptions.ShutdownTimeout);
        }

        private partial void UnregisterShutdownHandlers()
        {
            _sigIntRegistration?.Dispose();
            _sigQuitRegistration?.Dispose();
            _sigTermRegistration?.Dispose();
        }
    }
}
