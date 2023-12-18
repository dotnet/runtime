// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
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
        private bool TryConnect(int _ /* timeout */)
        {
            // timeout isn't used as Connect will be very fast,
            // either succeeding immediately if the server is listening or failing
            // immediately if it isn't.  The only delay will be between the time the server
            // has called Bind and Listen, with the latter immediately following the former.
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            SafePipeHandle? clientHandle = null;
            try
            {
                socket.Connect(new UnixDomainSocketEndPoint(_normalizedPipePath!));
                clientHandle = new SafePipeHandle(socket);
                ConfigureSocket(socket, clientHandle, _direction, 0, 0, _inheritability);
            }
            catch (SocketException e)
            {
                clientHandle?.Dispose();
                socket.Dispose();

                switch (e.SocketErrorCode)
                {
                    // Retryable errors
                    case SocketError.AddressAlreadyInUse:
                    case SocketError.AddressNotAvailable:
                    case SocketError.ConnectionRefused:
                        return false;

                    // Non-retryable errors
                    default:
                        throw;
                }
            }

            try
            {
                ValidateRemotePipeUser(clientHandle);
            }
            catch (Exception)
            {
                clientHandle.Dispose();
                socket.Dispose();
                throw;
            }

            InitializeHandle(clientHandle, isExposed: false, isAsync: (_pipeOptions & PipeOptions.Asynchronous) != 0);
            State = PipeState.Connected;
            return true;
        }

        [SupportedOSPlatform("windows")]
        public int NumberOfServerInstances
        {
            get
            {
                CheckPipePropertyOperations();
                throw new PlatformNotSupportedException(); // no way to determine this accurately
            }
        }

        public override int InBufferSize
        {
            get
            {
                CheckPipePropertyOperations();
                if (!CanRead) throw new NotSupportedException(SR.NotSupported_UnreadableStream);
                return InternalHandle?.PipeSocket.ReceiveBufferSize ?? 0;
            }
        }

        public override int OutBufferSize
        {
            get
            {
                CheckPipePropertyOperations();
                if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);
                return InternalHandle?.PipeSocket.SendBufferSize ?? 0;
            }
        }

        private void ValidateRemotePipeUser(SafePipeHandle handle)
        {
            if (!IsCurrentUserOnly)
                return;

            uint userId = Interop.Sys.GetEUid();
            if (Interop.Sys.GetPeerID(handle, out uint serverOwner) == -1)
            {
                throw CreateExceptionForLastError();
            }

            if (userId != serverOwner)
            {
                throw new UnauthorizedAccessException(SR.UnauthorizedAccess_NotOwnedByCurrentUser);
            }
        }
    }
}
