// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe client. Use this to open the client end of a named pipes created with 
    /// NamedPipeServerStream.
    /// </summary>
    public sealed partial class NamedPipeClientStream : PipeStream
    {
        private bool TryConnect(int timeout, CancellationToken cancellationToken)
        {
            // timeout and cancellationToken aren't used as Connect will be very fast,
            // either succeeding immediately if the server is listening or failing
            // immediately if it isn't.  The only delay will be between the time the server
            // has called Bind and Listen, with the latter immediately following the former.
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            SafePipeHandle clientHandle = null;
            try
            {
                socket.Connect(new UnixDomainSocketEndPoint(_normalizedPipePath));
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

            InitializeHandle(clientHandle, isExposed: false, isAsync: (_pipeOptions & PipeOptions.Asynchronous) != 0);
            State = PipeState.Connected;
            return true;
        }

        public int NumberOfServerInstances
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
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
                return InternalHandle?.NamedPipeSocket?.ReceiveBufferSize ?? 0;
            }
        }

        public override int OutBufferSize
        {
            get
            {
                CheckPipePropertyOperations();
                if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);
                return InternalHandle?.NamedPipeSocket?.SendBufferSize ?? 0;
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
