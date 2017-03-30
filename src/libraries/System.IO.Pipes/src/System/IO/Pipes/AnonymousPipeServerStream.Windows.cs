// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            Debug.Assert(bufferSize >= 0, "bufferSize is negative");

            bool bSuccess;
            SafePipeHandle serverHandle;
            SafePipeHandle newServerHandle;

            // Create the two pipe handles that make up the anonymous pipe.
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(inheritability);
            if (direction == PipeDirection.In)
            {
                bSuccess = Interop.Kernel32.CreatePipe(out serverHandle, out _clientHandle, ref secAttrs, bufferSize);
            }
            else
            {
                bSuccess = Interop.Kernel32.CreatePipe(out _clientHandle, out serverHandle, ref secAttrs, bufferSize);
            }

            if (!bSuccess)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            // Duplicate the server handle to make it not inheritable.  Note: We need to do this so that the child 
            // process doesn't end up getting another copy of the server handle.  If it were to get a copy, the
            // OS wouldn't be able to inform the child that the server has closed its handle because it will see
            // that there is still one server handle that is open.  
            bSuccess = Interop.Kernel32.DuplicateHandle(Interop.Kernel32.GetCurrentProcess(), serverHandle, Interop.Kernel32.GetCurrentProcess(),
                out newServerHandle, 0, false, Interop.Kernel32.HandleOptions.DUPLICATE_SAME_ACCESS);

            if (!bSuccess)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            // Close the inheritable server handle.
            serverHandle.Dispose();

            InitializeHandle(newServerHandle, false, false);

            State = PipeState.Connected;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
