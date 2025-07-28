// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Provides data for the <see cref="TraceSource.Initializing"/> event.
    /// </summary>
    public sealed class InitializingTraceSourceEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitializingTraceSourceEventArgs"/> class.
        /// </summary>
        /// <param name="traceSource">The trace source that is being initialized.</param>
        public InitializingTraceSourceEventArgs(TraceSource traceSource)
        {
            TraceSource = traceSource;
        }

        /// <summary>
        /// Gets the trace source that is being initialized.
        /// </summary>
        public TraceSource TraceSource { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the trace source was initialized from configuration.
        /// </summary>
        public bool WasInitialized { get; set; }
    }
}
