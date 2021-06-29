// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public sealed class PosixSignalContext
    {
        public PosixSignal Signal
        {
            get;
            internal set;
        }

        /// <summary>
        /// Cancels default handling of the signal.
        /// </summary>
        public bool Cancel
        {
            get;
            set;
        }

        public PosixSignalContext(PosixSignal signal)
        {
            Signal = signal;
        }

        internal PosixSignalContext()
        { }
    }
}
