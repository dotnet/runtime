// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream
    {
        internal void InitializeBufferSize(SafePipeHandle handle, int bufferSize)
        {
            if (bufferSize > 0)
            {
                SysCall(handle, (fd, _, size) => Interop.libc.fcntl(fd, Interop.libc.FcntlCommands.F_SETPIPE_SZ, size), 
                    IntPtr.Zero, bufferSize);
            }
        }

        private int InBufferSizeCore { get { return GetPipeBufferSize(); } }

        private int OutBufferSizeCore { get { return GetPipeBufferSize(); } }

        private int GetPipeBufferSize()
        {
            // If we have a handle, get the capacity of the pipe (there's no distinction between in/out direction).
            // If we don't, the pipe has been created but not yet connected (in the case of named pipes),
            // so just return the buffer size that was passed to the constructor.
            return _handle != null ?
                (int)SysCall(_handle, (fd, _, __) => Interop.libc.fcntl(fd, Interop.libc.FcntlCommands.F_GETPIPE_SZ)) :
                _outBufferSize;
        }
    }
}
