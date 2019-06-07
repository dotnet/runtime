// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe client. Use this to open the client end of a named pipes created with 
    /// NamedPipeServerStream.
    /// </summary>
    public sealed partial class NamedPipeClientStream : PipeStream
    {
        // Waits for a pipe instance to become available. This method may return before WaitForConnection is called
        // on the server end, but WaitForConnection will not return until we have returned.  Any data written to the
        // pipe by us after we have connected but before the server has called WaitForConnection will be available
        // to the server after it calls WaitForConnection. 
        private bool TryConnect(int timeout, CancellationToken cancellationToken)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(_inheritability);

            // PipeOptions.CurrentUserOnly is special since it doesn't match directly to a corresponding Win32 valid flag.
            // Remove it, while keeping others untouched since historically this has been used as a way to pass flags to
            // CreateNamedPipeClient that were not defined in the enumeration.
            int _pipeFlags = (int)(_pipeOptions & ~PipeOptions.CurrentUserOnly);
            if (_impersonationLevel != TokenImpersonationLevel.None)
            {
                _pipeFlags |= Interop.Kernel32.SecurityOptions.SECURITY_SQOS_PRESENT;
                _pipeFlags |= (((int)_impersonationLevel - 1) << 16);
            }

            int access = 0;
            if ((PipeDirection.In & _direction) != 0)
            {
                access |= Interop.Kernel32.GenericOperations.GENERIC_READ;
            }
            if ((PipeDirection.Out & _direction) != 0)
            {
                access |= Interop.Kernel32.GenericOperations.GENERIC_WRITE;
            }

            // Let's try to connect first
            SafePipeHandle handle = Interop.Kernel32.CreateNamedPipeClient(_normalizedPipePath,
                                        access,           // read and write access
                                        0,                  // sharing: none
                                        ref secAttrs,           // security attributes
                                        FileMode.Open,      // open existing 
                                        _pipeFlags,         // impersonation flags
                                        IntPtr.Zero);  // template file: null

            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode != Interop.Errors.ERROR_PIPE_BUSY &&
                    errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND)
                {
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                if (!Interop.Kernel32.WaitNamedPipe(_normalizedPipePath, timeout))
                {
                    errorCode = Marshal.GetLastWin32Error();

                    // Server is not yet created or a timeout occurred before a pipe instance was available.
                    if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND ||
                        errorCode == Interop.Errors.ERROR_SEM_TIMEOUT)
                    {
                        return false;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                // Pipe server should be free.  Let's try to connect to it.
                handle = Interop.Kernel32.CreateNamedPipeClient(_normalizedPipePath,
                                            access,           // read and write access
                                            0,                  // sharing: none
                                            ref secAttrs,           // security attributes
                                            FileMode.Open,      // open existing 
                                            _pipeFlags,         // impersonation flags
                                            IntPtr.Zero);  // template file: null

                if (handle.IsInvalid)
                {
                    errorCode = Marshal.GetLastWin32Error();

                    // Handle the possible race condition of someone else connecting to the server 
                    // between our calls to WaitNamedPipe & CreateFile.
                    if (errorCode == Interop.Errors.ERROR_PIPE_BUSY)
                    {
                        return false;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
            }

            // Success!
            InitializeHandle(handle, false, (_pipeOptions & PipeOptions.Asynchronous) != 0);
            State = PipeState.Connected;
            ValidateRemotePipeUser();
            return true;
        }

        public unsafe int NumberOfServerInstances
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();

                // NOTE: MSDN says that GetNamedPipeHandleState requires that the pipe handle has 
                // GENERIC_READ access, but we don't check for that because sometimes it works without
                // GERERIC_READ access. [Edit: Seems like CreateFile slaps on a READ_ATTRIBUTES 
                // access request before calling NTCreateFile, so all NamedPipeClientStreams can read
                // this if they are created (on WinXP SP2 at least)] 
                uint numInstances;
                if (!Interop.Kernel32.GetNamedPipeHandleStateW(InternalHandle, null, &numInstances, null, null, null, 0))
                {
                    throw WinIOError(Marshal.GetLastWin32Error());
                }

                return (int)numInstances;
            }
        }

        private void ValidateRemotePipeUser()
        {
            if (!IsCurrentUserOnly)
                return;

            PipeSecurity accessControl = this.GetAccessControl();
            IdentityReference remoteOwnerSid = accessControl.GetOwner(typeof(SecurityIdentifier));
            using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
            {
                SecurityIdentifier currentUserSid = currentIdentity.Owner;
                if (remoteOwnerSid != currentUserSid)
                {
                    State = PipeState.Closed;
                    throw new UnauthorizedAccessException(SR.UnauthorizedAccess_NotOwnedByCurrentUser);
                }
            }
        }
    }
}
