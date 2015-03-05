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
                    bool created = false;
                    
                    // If the caller asked for the handle to be non-inheritable, try to use pipe2 to create the pipe.
                    // Depending on the OS, it may not exist.
                    if ((inheritability & HandleInheritability.Inheritable) == 0)
                    {
                        try
                        {
                            while (Interop.CheckIo(Interop.libc.pipe2(fdsptr, (int)Interop.libc.OpenFlags.O_CLOEXEC))) ;
                            created = true;
                        }
                        catch (MissingMethodException) { } // pipe2 is Linux only
                    }

                    // Fall back to using pipe if either the handle can be inherited or if pipe2 wasn't available to 
                    // create it as non-inheritable.  We don't just want to fail if we can't make the handle
                    // non-inheritable as non-inheritance is the default.
                    if (!created)
                    {
                        while (Interop.CheckIo(Interop.libc.pipe(fdsptr))) ;
                    }
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
