// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides data for a <see cref="PosixSignalRegistration"/> event.
    /// </summary>
    public sealed class PosixSignalContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PosixSignalContext"/> class.
        /// </summary>
        public PosixSignalContext(PosixSignal signal) => Signal = signal;

        /// <summary>
        /// Gets the signal that occurred.
        /// </summary>
        public PosixSignal Signal { get; internal set; }

        /// <summary>
        /// Gets or sets a value that indicates whether to cancel the default handling of the signal. The default is <see langword="false"/>.
        /// </summary>
        public bool Cancel { get; set; }
    }
}
