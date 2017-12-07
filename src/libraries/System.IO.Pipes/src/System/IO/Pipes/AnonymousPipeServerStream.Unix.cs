// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private unsafe void Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
        {
            Debug.Assert(direction != PipeDirection.InOut, "Anonymous pipe direction shouldn't be InOut");
            // Ignore bufferSize.  It's optional, and the fcntl F_SETPIPE_SZ for changing it is Linux specific.

            SafePipeHandle serverHandle, clientHandle;
            if (direction == PipeDirection.In)
                CreateAnonymousPipe(inheritability, reader: out serverHandle, writer: out clientHandle);
            else
                CreateAnonymousPipe(inheritability, reader: out clientHandle, writer: out serverHandle);

            // Configure the pipe.  For buffer size, the size applies to the pipe, rather than to 
            // just one end's file descriptor, so we only need to do this with one of the handles.
            // bufferSize is just advisory and ignored if platform does not support setting pipe capacity via fcntl.
            if (bufferSize > 0 && Interop.Sys.Fcntl.CanGetSetPipeSz)
            {
                CheckPipeCall(Interop.Sys.Fcntl.SetPipeSz(serverHandle, bufferSize));
            }

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
