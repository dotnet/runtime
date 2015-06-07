// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream
    {
        internal void InitializeBufferSize(SafePipeHandle handle, int bufferSize)
        {
            // Nop.  We can't configure the capacity, and as it's just advisory, ignore it.
        }

        private int InBufferSizeCore { get { throw new PlatformNotSupportedException(); } }

        private int OutBufferSizeCore { get { throw new PlatformNotSupportedException(); } }
    }
}
