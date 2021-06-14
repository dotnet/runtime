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
