// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    /// <summary>
    /// Anonymous pipe server stream
    /// </summary>
    public sealed partial class AnonymousPipeServerStream : PipeStream
    {
        // Creates the anonymous pipe.
        private void Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
        {
            Debug.Assert(direction != PipeDirection.InOut, "Anonymous pipe direction shouldn't be InOut");
            // Ignore bufferSize.  It's optional, and the fcntl F_SETPIPE_SZ for changing it is Linux specific.

            SafePipeHandle? serverHandle = null, clientHandle = null;
            try
            {
                if (direction == PipeDirection.In)
                {
                    CreateAnonymousPipe(reader: out serverHandle, writer: out clientHandle);
                }
                else
                {
                    CreateAnonymousPipe(reader: out clientHandle, writer: out serverHandle);
                }
            }
            catch
            {
                serverHandle?.Dispose();
                clientHandle?.Dispose();
                throw;
            }

            // We always create pipes with both file descriptors being O_CLOEXEC.
            // If inheritability is requested, we clear the O_CLOEXEC flag
            // from the child descriptor so that it can be passed to a child process.
            // We assume that the HandleInheritability only applies to the child fd,
            // as if we allowed the server fd to be inherited, then when this process
            // closes its end of the pipe, the client won't receive an EOF or broken
            // pipe notification, as the child will still have open its dup of the fd.
            if (inheritability == HandleInheritability.Inheritable &&
                Interop.Sys.Fcntl.SetFD(clientHandle, 0) == -1)
            {
                throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
            }

            // Configure the pipe.  For buffer size, the size applies to the pipe, rather than to
            // just one end's file descriptor, so we only need to do this with one of the handles.
            // bufferSize is just advisory and ignored if platform does not support setting pipe capacity via fcntl.
            if (bufferSize > 0 && Interop.Sys.Fcntl.CanGetSetPipeSz)
            {
                Interop.Sys.Fcntl.SetPipeSz(serverHandle, bufferSize); // advisory, ignore errors
            }

            // We're connected.  Finish initialization using the newly created handles.
            InitializeHandle(serverHandle, isExposed: false, isAsync: false);
            _clientHandle = clientHandle;
            State = PipeState.Connected;
        }
    }
}
