// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeClientStream"/> class with the specified pipe and server names,
        /// the desired <see cref="PipeAccessRights"/>, and the specified impersonation level and inheritability.
        /// </summary>
        /// <param name="serverName">The name of the remote computer to connect to, or "." to specify the local computer.</param>
        /// <param name="pipeName">The name of the pipe.</param>
        /// <param name="desiredAccessRights">One of the enumeration values that specifies the desired access rights of the pipe.</param>
        /// <param name="options">One of the enumeration values that determines how to open or create the pipe.</param>
        /// <param name="impersonationLevel">One of the enumeration values that determines the security impersonation level.</param>
        /// <param name="inheritability">One of the enumeration values that determines whether the underlying handle will be inheritable by child processes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pipeName"/> or <paramref name="serverName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pipeName"/> or <paramref name="serverName"/> is a zero-length string.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pipeName"/> is set to "anonymous".</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="desiredAccessRights"/> is not a valid <see cref="PipeAccessRights"/> value.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="PipeOptions"/> value.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="impersonationLevel"/> is not a valid <see cref="TokenImpersonationLevel"/> value.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inheritability"/> is not a valid <see cref="HandleInheritability"/> value.</exception>
        /// <remarks>
        /// The pipe direction for this constructor is determined by the <paramref name="desiredAccessRights"/> parameter.
        /// If the <paramref name="desiredAccessRights"/> parameter specifies <see cref="PipeAccessRights.ReadData"/>,
        /// the pipe direction is <see cref="PipeDirection.In"/>. If the <paramref name="desiredAccessRights"/> parameter
        /// specifies <see cref="PipeAccessRights.WriteData"/>, the pipe direction is <see cref="PipeDirection.Out"/>.
        /// If the value of <paramref name="desiredAccessRights"/> specifies both <see cref="PipeAccessRights.ReadData"/>
        /// and <see cref="PipeAccessRights.WriteData"/>, the pipe direction is <see cref="PipeDirection.InOut"/>.
        /// </remarks>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public NamedPipeClientStream(string serverName, string pipeName, PipeAccessRights desiredAccessRights,
            PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
            : this(serverName, pipeName, DirectionFromRights(desiredAccessRights), options, impersonationLevel, inheritability)
        {
            _accessRights = (int)desiredAccessRights;
        }

        private static PipeDirection DirectionFromRights(PipeAccessRights desiredAccessRights, [CallerArgumentExpression(nameof(desiredAccessRights))] string? argumentName = null)
        {
            // Validate the desiredAccessRights parameter here to ensure an invalid value does not result
            // in an argument exception being thrown for the direction argument
            // Throw if there are any unrecognized bits
            // Throw if neither ReadData nor WriteData are specified, as this will result in an invalid PipeDirection
            if ((desiredAccessRights & ~(PipeAccessRights.FullControl | PipeAccessRights.AccessSystemSecurity)) != 0 ||
                ((desiredAccessRights & (PipeAccessRights.ReadData | PipeAccessRights.WriteData)) == 0))
            {
                throw new ArgumentOutOfRangeException(argumentName, SR.ArgumentOutOfRange_NeedValidPipeAccessRights);
            }

            PipeDirection direction = 0;

            if ((desiredAccessRights & PipeAccessRights.ReadData) != 0)
            {
                direction |= PipeDirection.In;
            }
            if ((desiredAccessRights & PipeAccessRights.WriteData) != 0)
            {
                direction |= PipeDirection.Out;
            }

            return direction;
        }

        private static int AccessRightsFromDirection(PipeDirection direction)
        {
            int access = 0;

            if ((PipeDirection.In & direction) != 0)
            {
                access |= Interop.Kernel32.GenericOperations.GENERIC_READ;
            }
            if ((PipeDirection.Out & direction) != 0)
            {
                access |= Interop.Kernel32.GenericOperations.GENERIC_WRITE;
            }

            return access;
        }

        // Waits for a pipe instance to become available. This method may return before WaitForConnection is called
        // on the server end, but WaitForConnection will not return until we have returned.  Any data written to the
        // pipe by us after we have connected but before the server has called WaitForConnection will be available
        // to the server after it calls WaitForConnection.
        private bool TryConnect(int timeout)
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

            SafePipeHandle handle = CreateNamedPipeClient(_normalizedPipePath, ref secAttrs, _pipeFlags, _accessRights);

            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastPInvokeError();

                handle.Dispose();

                // CreateFileW: "If the CreateNamedPipe function was not successfully called on the server prior to this operation,
                // a pipe will not exist and CreateFile will fail with ERROR_FILE_NOT_FOUND"
                // WaitNamedPipeW: "If no instances of the specified named pipe exist,
                // the WaitNamedPipe function returns immediately, regardless of the time-out value."
                // We know that no instances exist, so we just quit without calling WaitNamedPipeW.
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                {
                    return false;
                }

                if (errorCode != Interop.Errors.ERROR_PIPE_BUSY)
                {
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                if (!Interop.Kernel32.WaitNamedPipe(_normalizedPipePath, timeout))
                {
                    errorCode = Marshal.GetLastPInvokeError();

                    if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND || // server has been closed
                        errorCode == Interop.Errors.ERROR_SEM_TIMEOUT)
                    {
                        return false;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                // Pipe server should be free. Let's try to connect to it.
                handle = CreateNamedPipeClient(_normalizedPipePath, ref secAttrs, _pipeFlags, _accessRights);

                if (handle.IsInvalid)
                {
                    errorCode = Marshal.GetLastPInvokeError();

                    handle.Dispose();

                    // WaitNamedPipe: "A subsequent CreateFile call to the pipe can fail,
                    // because the instance was closed by the server or opened by another client."
                    if (errorCode == Interop.Errors.ERROR_PIPE_BUSY || // opened by another client
                        errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND) // server has been closed
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

            static SafePipeHandle CreateNamedPipeClient(string? path, ref Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs, int pipeFlags, int access)
                => Interop.Kernel32.CreateNamedPipeClient(path, access, FileShare.None, ref secAttrs, FileMode.Open, pipeFlags, hTemplateFile: IntPtr.Zero);
        }

        [SupportedOSPlatform("windows")]
        public unsafe int NumberOfServerInstances
        {
            get
            {
                CheckPipePropertyOperations();

                // NOTE: MSDN says that GetNamedPipeHandleState requires that the pipe handle has
                // GENERIC_READ access, but we don't check for that because sometimes it works without
                // GERERIC_READ access. [Edit: Seems like CreateFile slaps on a READ_ATTRIBUTES
                // access request before calling NTCreateFile, so all NamedPipeClientStreams can read
                // this if they are created (on WinXP SP2 at least)]
                uint numInstances;
                if (!Interop.Kernel32.GetNamedPipeHandleStateW(InternalHandle!, null, &numInstances, null, null, null, 0))
                {
                    throw WinIOError(Marshal.GetLastPInvokeError());
                }

                return (int)numInstances;
            }
        }

        private void ValidateRemotePipeUser()
        {
            if (!IsCurrentUserOnly)
                return;

            PipeSecurity accessControl = this.GetAccessControl();
            IdentityReference? remoteOwnerSid = accessControl.GetOwner(typeof(SecurityIdentifier));
            using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
            {
                SecurityIdentifier? currentUserSid = currentIdentity.Owner;
                if (remoteOwnerSid != currentUserSid)
                {
                    State = PipeState.Closed;
                    throw new UnauthorizedAccessException(SR.UnauthorizedAccess_NotOwnedByCurrentUser);
                }
            }
        }
    }
}
