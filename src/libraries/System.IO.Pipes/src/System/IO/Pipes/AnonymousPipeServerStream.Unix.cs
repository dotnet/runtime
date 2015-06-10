// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Security;

namespace System.IO.Pipes
{
    /// <summary>
    /// Anonymous pipe server stream
    /// </summary>
    public sealed partial class AnonymousPipeServerStream : PipeStream
    {
        // Creates the anonymous pipe.
        [SecurityCritical]
        private void Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
        {
            Debug.Assert(direction != PipeDirection.InOut, "Anonymous pipe direction shouldn't be InOut");
            // Ignore bufferSize.  It's optional, and the fcntl F_SETPIPE_SZ for changing it is Linux specific.

            // Use pipe or pipe2 to create our anonymous pipe
            int[] fds = new int[2];
            unsafe
            {
                fixed (int* fdsptr = fds)
                {
                    CreatePipe(inheritability, fdsptr);
                }
            }

            // Create SafePipeHandles for each end of the pipe.  Which ends goes with server and which goes with
            // client depends on the direction of the pipe.
            SafePipeHandle serverHandle = new SafePipeHandle(
                (IntPtr)fds[direction == PipeDirection.In ? Interop.libc.ReadEndOfPipe : Interop.libc.WriteEndOfPipe], 
                ownsHandle: true);
            SafePipeHandle clientHandle = new SafePipeHandle(
                (IntPtr)fds[direction == PipeDirection.In ? Interop.libc.WriteEndOfPipe : Interop.libc.ReadEndOfPipe], 
                ownsHandle: true);

            // Configure the pipe.  For buffer size, the size applies to the pipe, rather than to 
            // just one end's file descriptor, so we only need to do this with one of the handles.
            InitializeBufferSize(serverHandle, bufferSize);

            // We're connected.  Finish initialization using the newly created handles.
            InitializeHandle(serverHandle, isExposed: false, isAsync: false);
            _clientHandle = clientHandle;
            State = PipeState.Connected;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
