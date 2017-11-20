// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    public sealed partial class NamedPipeServerStream : PipeStream
    {
        private SharedServer _instance;
        private PipeDirection _direction;
        private PipeOptions _options;
        private int _inBufferSize;
        private int _outBufferSize;
        private HandleInheritability _inheritability;

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

            // We don't have a good way to enforce maxNumberOfServerInstances across processes; we only factor it in
            // for streams created in this process.  Between processes, we behave similarly to maxNumberOfServerInstances == 1,
            // in that the second process to come along and create a stream will find the pipe already in existence and will fail.
            _instance = SharedServer.Get(GetPipePath(".", pipeName), maxNumberOfServerInstances);

            _direction = direction;
            _options = options;
            _inBufferSize = inBufferSize;
            _outBufferSize = outBufferSize;
            _inheritability = inheritability;
        }

        public void WaitForConnection()
        {
            CheckConnectOperationsServer();
            if (State == PipeState.Connected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
            }

            // Use and block on AcceptAsync() rather than using Accept() in order to provide
            // behavior more akin to Windows if the Stream is closed while a connection is pending.
            Socket accepted = _instance.ListeningSocket.AcceptAsync().GetAwaiter().GetResult();
            HandleAcceptedSocket(accepted);
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

            async Task WaitForConnectionAsyncCore() =>
               HandleAcceptedSocket(await _instance.ListeningSocket.AcceptAsync().ConfigureAwait(false));
        }

        private void HandleAcceptedSocket(Socket acceptedSocket)
        {
            var serverHandle = new SafePipeHandle(acceptedSocket);
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

        internal override void DisposeCore(bool disposing) =>
            Interlocked.Exchange(ref _instance, null)?.Dispose(disposing); // interlocked to avoid shared state problems from erroneous double/concurrent disposes

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

            throw CreateExceptionForLastError();
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
                throw CreateExceptionForLastError();
            }

            // set the effective userid of the current (server) process to the clientid
            if (Interop.Sys.SetEUid(peerID) == -1)
            {
                throw CreateExceptionForLastError();
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

        private Exception CreateExceptionForLastError()
        {
            Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
            return error.Error == Interop.Error.ENOTSUP ?
                new PlatformNotSupportedException(SR.Format(SR.PlatformNotSupported_OperatingSystemError, nameof(Interop.Error.ENOTSUP))) :
                Interop.GetExceptionForIoErrno(error, _instance?.PipeName);
        }

        /// <summary>Shared resources for NamedPipeServerStreams in the same process created for the same path.</summary>
        private sealed class SharedServer
        {
            /// <summary>Path to shared instance mapping.</summary>
            private static readonly Dictionary<string, SharedServer> s_servers = new Dictionary<string, SharedServer>();

            /// <summary>The pipe name for this instance.</summary>
            internal string PipeName { get; }
            /// <summary>Gets the shared socket used to accept connections.</summary>
            internal Socket ListeningSocket { get; }

            /// <summary>The maximum number of server streams allowed to use this instance concurrently.</summary>
            private readonly int _maxCount;
            /// <summary>The concurrent number of concurrent streams using this instance.</summary>
            private int _currentCount;

            internal static SharedServer Get(string path, int maxCount)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(maxCount >= 1);

                lock (s_servers)
                {
                    SharedServer server;
                    if (s_servers.TryGetValue(path, out server))
                    {
                        // On Windows, if a subsequent server stream is created for the same pipe and with a different
                        // max count, the subsequent count is largely ignored in that it doesn't change the number of
                        // allowed concurrent instances, however that particular instance being created does take its
                        // own into account, so if its creation would put it over either the original or its own limit,
                        // it's an error that results in an exception.  We do the same for Unix here.
                        if (server._currentCount == server._maxCount)
                        {
                            throw new IOException(SR.IO_AllPipeInstancesAreBusy);
                        }
                        else if (server._currentCount == maxCount)
                        {
                            throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, path));
                        }
                    }
                    else
                    {
                        // No instance exists yet for this path. Create one a new.
                        server = new SharedServer(path, maxCount);
                        s_servers.Add(path, server);
                    }

                    Debug.Assert(server._currentCount >= 0 && server._currentCount < server._maxCount);
                    server._currentCount++;
                    return server;
                }
            }

            internal void Dispose(bool disposing)
            {
                lock (s_servers)
                {
                    Debug.Assert(_currentCount >= 1 && _currentCount <= _maxCount);

                    if (_currentCount == 1)
                    {
                        bool removed = s_servers.Remove(PipeName);
                        Debug.Assert(removed);

                        Interop.Sys.Unlink(PipeName); // ignore any failures

                        if (disposing)
                        {
                            ListeningSocket.Dispose();
                        }
                    }
                    else
                    {
                        _currentCount--;
                    }
                }
            }

            private SharedServer(string path, int maxCount)
            {
                // Binding to an existing path fails, so we need to remove anything left over at this location.
                // There's of course a race condition here, where it could be recreated by someone else between this
                // deletion and the bind below, in which case we'll simply let the bind fail and throw.
                Interop.Sys.Unlink(path); // ignore any failures

                // Start listening for connections on the path.
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    socket.Bind(new UnixDomainSocketEndPoint(path));
                    socket.Listen(int.MaxValue);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }

                PipeName = path;
                ListeningSocket = socket;
                _maxCount = maxCount;
            }
        }
    }
}
