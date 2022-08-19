// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Provides data for the <see cref="TraceSource.Initializing"/> event.
    /// </summary>
    public sealed class InitializingTraceSourceEventArgs : EventArgs
    {
        public InitializingTraceSourceEventArgs(TraceSource traceSource)
        {
            TraceSource = traceSource;
        }

        public TraceSource TraceSource { get; }
        public bool WasInitialized { get; set; }
    }
}
