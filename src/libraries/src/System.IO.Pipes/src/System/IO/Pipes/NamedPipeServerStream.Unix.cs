// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe server
    /// </summary>
    public sealed partial class NamedPipeServerStream : PipeStream
    {
        private string _path;
        private PipeDirection _direction;
        private PipeOptions _options;
        private int _inBufferSize;
        private int _outBufferSize;
        private HandleInheritability _inheritability;

        [SecurityCritical]
        private void Create(string pipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize,
                HandleInheritability inheritability)
        {
            Debug.Assert(pipeName != null && pipeName.Length != 0, "fullPipeName is null or empty");
            Debug.Assert(direction >= PipeDirection.In && direction <= PipeDirection.InOut, "invalid pipe direction");
            Debug.Assert(inBufferSize >= 0, "inBufferSize is negative");
            Debug.Assert(outBufferSize >= 0, "outBufferSize is negative");
            Debug.Assert((maxNumberOfServerInstances >= 1) || (maxNumberOfServerInstances == MaxAllowedServerInstances), "maxNumberOfServerInstances is invalid");
            Debug.Assert(transmissionMode >= PipeTransmissionMode.Byte && transmissionMode <= PipeTransmissionMode.Message, "transmissionMode is out of range");

            if (transmissionMode == PipeTransmissionMode.Message)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_MessageTransmissionMode);
            }

            // NOTE: We don't have a good way to enforce maxNumberOfServerInstances, and don't currently try.
            // It's a Windows-specific concept.

            _path = GetPipePath(".", pipeName);
            _direction = direction;
            _options = options;
            _inBufferSize = inBufferSize;
            _outBufferSize = outBufferSize;
            _inheritability = inheritability;
        }

        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
        public void WaitForConnection()
        {
            CheckConnectOperationsServer();
            if (State == PipeState.Connected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
            }

            // Binding to an existing path fails, so we need to remove anything left over at this location.
            // There's of course a race condition here, where it could be recreated by someone else between this
            // deletion and the bind below, in which case we'll simply let the bind fail and throw.
            Interop.Sys.Unlink(_path); // ignore any failures
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                socket.Bind(new UnixDomainSocketEndPoint(_path));
                socket.Listen(1);

                Socket acceptedSocket = socket.Accept();
                SafePipeHandle serverHandle = new SafePipeHandle(acceptedSocket);
                try
                {
                    ConfigureSocket(acceptedSocket, serverHandle, _direction, _inBufferSize, _outBufferSize, _inheritability);
                }
                catch
                {
                    serverHandle.Dispose();
                    acceptedSocket.Dispose();
                    throw;
                }
                
                InitializeHandle(serverHandle, isExposed: false, isAsync: (_options & PipeOptions.Asynchronous) != 0);
                State = PipeState.Connected;
            }
            finally
            {
                // Bind will have created a file.  Now that the client is connected, it's no longer necessary, so get rid of it.
                Interop.Sys.Unlink(_path); // ignore any failures; worst case is we leave a tmp file

                // Clean up the listening socket
                socket.Dispose();
            }
        }

        public Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            CheckConnectOperationsServer();
            if (State == PipeState.Connected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
            }

            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                WaitForConnectionAsyncCore();
        }

        private async Task WaitForConnectionAsyncCore()
        {   
            // This is the same implementation as is in WaitForConnection(), but using Socket.AcceptAsync
            // instead of Socket.Accept.
             
            // Binding to an existing path fails, so we need to remove anything left over at this location.
            // There's of course a race condition here, where it could be recreated by someone else between this
            // deletion and the bind below, in which case we'll simply let the bind fail and throw.
            Interop.Sys.Unlink(_path); // ignore any failures
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                socket.Bind(new UnixDomainSocketEndPoint(_path));
                socket.Listen(1);

                Socket acceptedSocket = await socket.AcceptAsync().ConfigureAwait(false);
                SafePipeHandle serverHandle = new SafePipeHandle(acceptedSocket);
                ConfigureSocket(acceptedSocket, serverHandle, _direction, _inBufferSize, _outBufferSize, _inheritability);

                InitializeHandle(serverHandle, isExposed: false, isAsync: (_options & PipeOptions.Asynchronous) != 0);
                State = PipeState.Connected;
            }
            finally
            {
                // Bind will have created a file.  Now that the client is connected, it's no longer necessary, so get rid of it.
                Interop.Sys.Unlink(_path); // ignore any failures; worst case is we leave a tmp file

                // Clean up the listening socket
                socket.Dispose();
            }
        }

        [SecurityCritical]
        public void Disconnect()
        {
            CheckDisconnectOperations();
            State = PipeState.Disconnected;
            InternalHandle.Dispose();
            InitializeHandle(null, false, false);
        }

        // Gets the username of the connected client.  Not that we will not have access to the client's 
        // username until it has written at least once to the pipe (and has set its impersonationLevel 
        // argument appropriately). 
        [SecurityCritical]
        public string GetImpersonationUserName()
        {
            CheckWriteOperations();

            SafeHandle handle = InternalHandle?.NamedPipeSocketHandle;
            if (handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }

            string name = Interop.Sys.GetPeerUserName(handle);
            if (name != null)
            {
                return name;
            }

            Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
            throw error.Error == Interop.Error.ENOTSUP ?
                new PlatformNotSupportedException() :
                Interop.GetExceptionForIoErrno(error, _path);
        }

        public override int InBufferSize
        {
            get
            {
                CheckPipePropertyOperations();
                if (!CanRead) throw new NotSupportedException(SR.NotSupported_UnreadableStream);
                return InternalHandle?.NamedPipeSocket?.ReceiveBufferSize ?? _inBufferSize;
            }
        }

        public override int OutBufferSize
        {
            get
            {
                CheckPipePropertyOperations();
                if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);
                return InternalHandle?.NamedPipeSocket?.SendBufferSize ?? _outBufferSize;
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        // This method calls a delegate while impersonating the client.
        public void RunAsClient(PipeStreamImpersonationWorker impersonationWorker)
        {
            CheckWriteOperations();
            SafeHandle handle = InternalHandle?.NamedPipeSocketHandle;
            if (handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }
            // Get the current effective ID to fallback to after the impersonationWorker is run
            uint currentEUID = Interop.Sys.GetEUid();

            // Get the userid of the client process at the end of the pipe
            uint peerID;
            if (Interop.Sys.GetPeerID(handle, out peerID) == -1)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                throw error.Error == Interop.Error.ENOTSUP ?
                    new PlatformNotSupportedException() :
                    Interop.GetExceptionForIoErrno(error, _path);
            }

            // set the effective userid of the current (server) process to the clientid
            if (Interop.Sys.SetEUid(peerID) == -1)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                throw error.Error == Interop.Error.ENOTSUP ?
                    new PlatformNotSupportedException() :
                    Interop.GetExceptionForIoErrno(error, _path);
            }

            try
            {
                impersonationWorker();
            }
            finally
            {
                // set the userid of the current (server) process back to its original value
                Interop.Sys.SetEUid(currentEUID);
            }
        }
    }
}
