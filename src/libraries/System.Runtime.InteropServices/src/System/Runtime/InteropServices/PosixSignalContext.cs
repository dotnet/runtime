// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public sealed class PosixSignalContext
    {
        public PosixSignal Signal
        {
            get;
        }

        public bool Cancel
        {
            get;
            set; // TODO: should this throw if Canceling doesn't do anything?
        }

        public PosixSignalContext(PosixSignal signal)
        {
            Signal = signal;
        }
    }
}
