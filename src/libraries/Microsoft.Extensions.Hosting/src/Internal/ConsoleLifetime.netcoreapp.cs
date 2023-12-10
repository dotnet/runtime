// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class ConsoleLifetime : IHostLifetime
    {
        private PosixSignalRegistration? _sigIntRegistration;
        private PosixSignalRegistration? _sigQuitRegistration;
        private PosixSignalRegistration? _sigTermRegistration;
        private ITimer? _shutdownDelayTimer;

        private partial void RegisterShutdownHandlers()
        {
            Action<PosixSignalContext> handler = HandlePosixSignal;
            _sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
            _sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
            _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handler);
        }

        private void HandlePosixSignal(PosixSignalContext context)
        {
            Debug.Assert(context.Signal == PosixSignal.SIGINT || context.Signal == PosixSignal.SIGQUIT || context.Signal == PosixSignal.SIGTERM);

            context.Cancel = true;

            if (HostOptions.ShutdownDelay.HasValue)
            {
                _shutdownDelayTimer = TimeProvider.CreateTimer(state =>
                {
                    Logger.LogInformation("Received {PosixSignal}. Delaying shutdown for {Delay}", context.Signal, HostOptions.ShutdownDelay.Value);
                    ((ConsoleLifetime)state!).ApplicationLifetime.StopApplication();
                },
                this,
                HostOptions.ShutdownDelay.Value,
                Timeout.InfiniteTimeSpan);
            }
            else
            {
                ApplicationLifetime.StopApplication();
            }
        }

        private partial void UnregisterShutdownHandlers()
        {
            _sigIntRegistration?.Dispose();
            _sigQuitRegistration?.Dispose();
            _sigTermRegistration?.Dispose();
            _shutdownDelayTimer?.Dispose();
        }
    }
}
