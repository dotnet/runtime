// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class ConsoleLifetime : IHostLifetime
    {
        private PosixSignalRegistration _sigTermRegistration;

        private partial void RegisterShutdownHandlers()
        {
            _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => OnSigTerm(context));
        }

        private void OnSigTerm(PosixSignalContext context)
        {
            Debug.Assert(context.Signal == PosixSignal.SIGTERM);

            context.Cancel = true;
            OnExitSignal();
        }

        private partial void UnregisterShutdownHandlers()
        {
            _sigTermRegistration?.Dispose();
        }
    }
}
