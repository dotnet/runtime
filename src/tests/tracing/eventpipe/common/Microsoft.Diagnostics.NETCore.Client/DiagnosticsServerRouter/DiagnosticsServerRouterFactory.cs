// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal class RuntimeTimeoutException : TimeoutException
    {
        public RuntimeTimeoutException(int TimeoutMs)
            : base(string.Format("No new runtime endpoints connected, waited {0} ms", TimeoutMs))
        { }
    }

    internal class BackendStreamTimeoutException : TimeoutException
    {
        public BackendStreamTimeoutException(int TimeoutMs)
            : base(string.Format("No new back end streams available, waited {0} ms", TimeoutMs))
        { }
    }

    /// <summary>
    /// Base class representing a Diagnostics Server router factory.
    /// </summary>
    internal class DiagnosticsServerRouterFactory
    {
        int IsStreamConnectedTimeoutMs { get; set; } = 500;

        public virtual string IpcAddress { get; }

        public virtual string TcpAddress { get; }

        public virtual ILogger Logger { get; }

        public virtual Task Start(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public virtual Task Stop()
        {
            throw new NotImplementedException();
        }

        public virtual void Reset()
        {
            throw new NotImplementedException();
        }

        public virtual Task<Router> CreateRouterAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected bool IsStreamConnected(Stream stream, CancellationToken token)
        {
            bool connected = true;

            if (stream is NamedPipeServerStream || stream is NamedPipeClientStream)
            {
                PipeStream pipeStream = stream as PipeStream;

                // PeekNamedPipe will return false if the pipe is disconnected/broken.
                connected = NativeMethods.PeekNamedPipe(
                    pipeStream.SafePipeHandle,
                    null,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            else if (stream is ExposedSocketNetworkStream networkStream)
            {
                bool blockingState = networkStream.Socket.Blocking;
                try
                {
                    // Check connection read state by peek one byte. Will return 0 in case connection is closed.
                    // A closed connection could also raise exception, but then socket connected state should
                    // be set to false.
                    networkStream.Socket.Blocking = false;
                    if (networkStream.Socket.Receive(new byte[1], 0, 1, System.Net.Sockets.SocketFlags.Peek) == 0)
                        connected = false;

                    // Check connection write state by sending non-blocking zero-byte data.
                    // A closed connection should raise exception, but then socket connected state should
                    // be set to false.
                    if (connected)
                        networkStream.Socket.Send(Array.Empty<byte>(), 0, System.Net.Sockets.SocketFlags.None);
                }
                catch (Exception)
                {
                    connected = networkStream.Socket.Connected;
                }
                finally
                {
                    networkStream.Socket.Blocking = blockingState;
                }
            }
            else
            {
                connected = false;
            }

            return connected;
        }

        protected async Task IsStreamConnectedAsync(Stream stream, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Check if tcp stream connection is still available.
                if (!IsStreamConnected(stream, token))
                {
                    throw new EndOfStreamException();
                }

                try
                {
                    // Wait before rechecking connection.
                    await Task.Delay(IsStreamConnectedTimeoutMs, token).ConfigureAwait(false);
                }
                catch { }
            }
        }

        protected bool IsCompletedSuccessfully(Task t)
        {
#if NETCOREAPP2_0_OR_GREATER
            return t.IsCompletedSuccessfully;
#else
            return t.IsCompleted && !t.IsCanceled && !t.IsFaulted;
#endif
        }
    }

    /// <summary>
    /// This class represent a TCP/IP server endpoint used when building up router instances.
    /// </summary>
    internal class TcpServerRouterFactory : IIpcServerTransportCallbackInternal
    {
        protected readonly ILogger _logger;

        string _tcpServerAddress;

        ReversedDiagnosticsServer _tcpServer;
        IpcEndpointInfo _tcpServerEndpointInfo;

        bool _auto_shutdown;

        int RuntimeTimeoutMs { get; set; } = 60000;
        int TcpServerTimeoutMs { get; set; } = 5000;

        public Guid RuntimeInstanceId
        {
            get { return _tcpServerEndpointInfo.RuntimeInstanceCookie; }
        }

        public int RuntimeProcessId
        {
            get { return _tcpServerEndpointInfo.ProcessId; }
        }

        public string TcpServerAddress
        {
            get { return _tcpServerAddress; }
        }

        public delegate TcpServerRouterFactory CreateInstanceDelegate(string tcpServer, int runtimeTimeoutMs, ILogger logger);

        public static TcpServerRouterFactory CreateDefaultInstance(string tcpServer, int runtimeTimeoutMs, ILogger logger)
        {
            return new TcpServerRouterFactory(tcpServer, runtimeTimeoutMs, logger);
        }

        public TcpServerRouterFactory(string tcpServer, int runtimeTimeoutMs, ILogger logger)
        {
            _logger = logger;

            _tcpServerAddress = IpcTcpSocketEndPoint.NormalizeTcpIpEndPoint(string.IsNullOrEmpty(tcpServer) ? "127.0.0.1:0" : tcpServer);

            _auto_shutdown = runtimeTimeoutMs != Timeout.Infinite;
            if (runtimeTimeoutMs != Timeout.Infinite)
                RuntimeTimeoutMs = runtimeTimeoutMs;

            _tcpServer = new ReversedDiagnosticsServer(_tcpServerAddress, enableTcpIpProtocol : true);
            _tcpServerEndpointInfo = new IpcEndpointInfo();
            _tcpServer.TransportCallback = this;
        }

        public virtual void Start()
        {
            _tcpServer.Start();
        }

        public virtual async Task Stop()
        {
            await _tcpServer.DisposeAsync().ConfigureAwait(false);
        }

        public void Reset()
        {
            if (_tcpServerEndpointInfo.Endpoint != null)
            {
                _tcpServer.RemoveConnection(_tcpServerEndpointInfo.RuntimeInstanceCookie);
                _tcpServerEndpointInfo = new IpcEndpointInfo();
            }
        }

        public async Task<Stream> AcceptTcpStreamAsync(CancellationToken token)
        {
            Stream tcpServerStream;

            _logger?.LogDebug($"Waiting for a new tcp connection at endpoint \"{_tcpServerAddress}\".");

            if (_tcpServerEndpointInfo.Endpoint == null)
            {
                using var acceptTimeoutTokenSource = new CancellationTokenSource();
                using var acceptTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, acceptTimeoutTokenSource.Token);

                try
                {
                    // If no new runtime instance connects, timeout.
                    acceptTimeoutTokenSource.CancelAfter(RuntimeTimeoutMs);
                    _tcpServerEndpointInfo = await _tcpServer.AcceptAsync(acceptTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (acceptTimeoutTokenSource.IsCancellationRequested)
                    {
                        _logger?.LogDebug("No runtime instance connected before timeout.");

                        if (_auto_shutdown)
                            throw new RuntimeTimeoutException(RuntimeTimeoutMs);
                    }

                    throw;
                }
            }

            using var connectTimeoutTokenSource = new CancellationTokenSource();
            using var connectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, connectTimeoutTokenSource.Token);

            try
            {
                // Get next connected tcp stream. Should timeout if no endpoint appears within timeout.
                // If that happens we need to remove endpoint since it might indicate a unresponsive runtime.
                connectTimeoutTokenSource.CancelAfter(TcpServerTimeoutMs);
                tcpServerStream = await _tcpServerEndpointInfo.Endpoint.ConnectAsync(connectTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (connectTimeoutTokenSource.IsCancellationRequested)
                {
                    _logger?.LogDebug("No tcp stream connected before timeout.");
                    throw new BackendStreamTimeoutException(TcpServerTimeoutMs);
                }

                throw;
            }

            if (tcpServerStream != null)
                _logger?.LogDebug($"Successfully connected tcp stream, runtime id={RuntimeInstanceId}, runtime pid={RuntimeProcessId}.");

            return tcpServerStream;
        }

        public void CreatedNewServer(EndPoint localEP)
        {
            if (localEP is IPEndPoint ipEP)
                _tcpServerAddress = _tcpServerAddress.Replace(":0", string.Format(":{0}", ipEP.Port));
        }
    }

    /// <summary>
    /// This class represent a TCP/IP client endpoint used when building up router instances.
    /// </summary>
    internal class TcpClientRouterFactory
    {
        protected readonly ILogger _logger;

        protected readonly string _tcpClientAddress;

        protected bool _auto_shutdown;

        protected int TcpClientTimeoutMs { get; set; } = Timeout.Infinite;

        protected int TcpClientRetryTimeoutMs { get; set; } = 500;

        public delegate TcpClientRouterFactory CreateInstanceDelegate(string tcpClient, int runtimeTimeoutMs, ILogger logger);

        public static TcpClientRouterFactory CreateDefaultInstance(string tcpClient, int runtimeTimeoutMs, ILogger logger)
        {
            return new TcpClientRouterFactory(tcpClient, runtimeTimeoutMs, logger);
        }

        public string TcpClientAddress {
            get { return _tcpClientAddress; }
        }

        public TcpClientRouterFactory(string tcpClient, int runtimeTimeoutMs, ILogger logger)
        {
            _logger = logger;
            _tcpClientAddress = IpcTcpSocketEndPoint.NormalizeTcpIpEndPoint(string.IsNullOrEmpty(tcpClient) ? "127.0.0.1:" + string.Format("{0}", 56000 + (Process.GetCurrentProcess().Id % 1000)) : tcpClient);
            _auto_shutdown = runtimeTimeoutMs != Timeout.Infinite;
            if (runtimeTimeoutMs != Timeout.Infinite)
                TcpClientTimeoutMs = runtimeTimeoutMs;
        }

        public virtual async Task<Stream> ConnectTcpStreamAsync(CancellationToken token)
        {
            return await ConnectTcpStreamAsyncInternal(token, _auto_shutdown).ConfigureAwait(false);
        }

        public virtual async Task<Stream> ConnectTcpStreamAsync(CancellationToken token, bool retry)
        {
            return await ConnectTcpStreamAsyncInternal(token, retry).ConfigureAwait(false);
        }

        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }

        async Task<Stream> ConnectTcpStreamAsyncInternal(CancellationToken token, bool retry)
        {
            Stream tcpClientStream = null;

            _logger?.LogDebug($"Connecting new tcp endpoint \"{_tcpClientAddress}\".");

            IpcTcpSocketEndPoint clientTcpEndPoint = new IpcTcpSocketEndPoint(_tcpClientAddress);
            Socket clientSocket = null;

            using var connectTimeoutTokenSource = new CancellationTokenSource();
            using var connectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, connectTimeoutTokenSource.Token);

            connectTimeoutTokenSource.CancelAfter(TcpClientTimeoutMs);

            do
            {
                clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    await ConnectAsyncInternal(clientSocket, clientTcpEndPoint, connectTokenSource.Token).ConfigureAwait(false);
                    retry = false;
                }
                catch (Exception)
                {
                    clientSocket?.Dispose();

                    if (connectTimeoutTokenSource.IsCancellationRequested)
                    {
                        _logger?.LogDebug("No tcp stream connected, timing out.");

                        if (_auto_shutdown)
                            throw new RuntimeTimeoutException(TcpClientTimeoutMs);

                        throw new TimeoutException();
                    }

                    // If we are not doing retries when runtime is unavailable, fail right away, this will
                    // break any accepted IPC connections, making sure client is notified and could reconnect.
                    // If not, retry until succeed or time out.
                    if (!retry)
                    {
                        _logger?.LogTrace($"Failed connecting {_tcpClientAddress}.");
                        throw;
                    }

                    _logger?.LogTrace($"Failed connecting {_tcpClientAddress}, wait {TcpClientRetryTimeoutMs} ms before retrying.");

                    // If we get an error (without hitting timeout above), most likely due to unavailable listener.
                    // Delay execution to prevent to rapid retry attempts.
                    await Task.Delay(TcpClientRetryTimeoutMs, token).ConfigureAwait(false);
                }
            }
            while (retry);

            tcpClientStream = new ExposedSocketNetworkStream(clientSocket, ownsSocket: true);
            _logger?.LogDebug("Successfully connected tcp stream.");

            return tcpClientStream;
        }

        async Task ConnectAsyncInternal(Socket clientSocket, EndPoint remoteEP, CancellationToken token)
        {
            using (token.Register(() => clientSocket.Close(0)))
            {
                try
                {
                    Func<AsyncCallback, object, IAsyncResult> beginConnect = (callback, state) =>
                    {
                        return clientSocket.BeginConnect(remoteEP, callback, state);
                    };
                    await Task.Factory.FromAsync(beginConnect, clientSocket.EndConnect, this).ConfigureAwait(false);
                }
                // When the socket is closed, the FromAsync logic will try to call EndAccept on the socket,
                // but that will throw an ObjectDisposedException. Only catch the exception if due to cancellation.
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    // First check if the cancellation token caused the closing of the socket,
                    // then rethrow the exception if it did not.
                    token.ThrowIfCancellationRequested();
                }
            }
        }
    }

    /// <summary>
    /// This class represent a IPC server endpoint used when building up router instances.
    /// </summary>
    internal class IpcServerRouterFactory
    {
        readonly ILogger _logger;

        readonly string _ipcServerPath;

        IpcServerTransport _ipcServer;

        int IpcServerTimeoutMs { get; set; } = Timeout.Infinite;

        public string IpcServerPath {
            get { return _ipcServerPath; }
        }

        public IpcServerRouterFactory(string ipcServer, ILogger logger)
        {
            if (string.IsNullOrEmpty(ipcServer))
                throw new ArgumentException("Missing IPC server path.");

            _logger = logger;
            _ipcServerPath = ipcServer;

            _ipcServer = IpcServerTransport.Create(_ipcServerPath, IpcServerTransport.MaxAllowedConnections, false);
        }

        public void Start()
        {
        }

        public void Stop()
        {
            _ipcServer?.Dispose();
        }

        public async Task<Stream> AcceptIpcStreamAsync(CancellationToken token)
        {
            Stream ipcServerStream = null;

            _logger?.LogDebug($"Waiting for new ipc connection at endpoint \"{_ipcServerPath}\".");


            using var connectTimeoutTokenSource = new CancellationTokenSource();
            using var connectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, connectTimeoutTokenSource.Token);

            try
            {
                connectTimeoutTokenSource.CancelAfter(IpcServerTimeoutMs);
                ipcServerStream = await _ipcServer.AcceptAsync(connectTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                ipcServerStream?.Dispose();

                if (connectTimeoutTokenSource.IsCancellationRequested)
                {
                    _logger?.LogDebug("No ipc stream connected, timing out.");
                    throw new TimeoutException();
                }

                throw;
            }

            if (ipcServerStream != null)
                _logger?.LogDebug("Successfully connected ipc stream.");

            return ipcServerStream;
        }
    }

    /// <summary>
    /// This class represent a IPC client endpoint used when building up router instances.
    /// </summary>
    internal class IpcClientRouterFactory
    {
        readonly ILogger _logger;

        readonly string _ipcClientPath;

        int IpcClientTimeoutMs { get; set; } = Timeout.Infinite;

        int IpcClientRetryTimeoutMs { get; set; } = 500;

        public string IpcClientPath {
            get { return _ipcClientPath; }
        }

        public IpcClientRouterFactory(string ipcClient, ILogger logger)
        {
            _logger = logger;
            _ipcClientPath = ipcClient;
        }

        public async Task<Stream> ConnectIpcStreamAsync(CancellationToken token)
        {
            Stream ipcClientStream = null;

            _logger?.LogDebug($"Connecting new ipc endpoint \"{_ipcClientPath}\".");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var namedPipe = new NamedPipeClientStream(
                    ".",
                    _ipcClientPath,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous,
                    TokenImpersonationLevel.Impersonation);

                try
                {
                    await namedPipe.ConnectAsync(IpcClientTimeoutMs, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    namedPipe?.Dispose();

                    if (ex is TimeoutException)
                        _logger?.LogDebug("No ipc stream connected, timing out.");

                    throw;
                }

                ipcClientStream = namedPipe;
            }
            else
            {
                bool retry = false;
                IpcUnixDomainSocket unixDomainSocket;

                using var connectTimeoutTokenSource = new CancellationTokenSource();
                using var connectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, connectTimeoutTokenSource.Token);

                connectTimeoutTokenSource.CancelAfter(IpcClientTimeoutMs);

                do
                {
                    unixDomainSocket = new IpcUnixDomainSocket();

                    try
                    {
                        await unixDomainSocket.ConnectAsync(new IpcUnixDomainSocketEndPoint(_ipcClientPath), token).ConfigureAwait(false);
                        retry = false;
                    }
                    catch (Exception)
                    {
                        unixDomainSocket?.Dispose();

                        if (connectTimeoutTokenSource.IsCancellationRequested)
                        {
                            _logger?.LogDebug("No ipc stream connected, timing out.");
                            throw new TimeoutException();
                        }

                        _logger?.LogTrace($"Failed connecting {_ipcClientPath}, wait {IpcClientRetryTimeoutMs} ms before retrying.");

                        // If we get an error (without hitting timeout above), most likely due to unavailable listener.
                        // Delay execution to prevent to rapid retry attempts.
                        await Task.Delay(IpcClientRetryTimeoutMs, token).ConfigureAwait(false);

                        retry = true;
                    }
                }
                while (retry);

                ipcClientStream = new ExposedSocketNetworkStream(unixDomainSocket, ownsSocket: true);
            }

            if (ipcClientStream != null)
                _logger?.LogDebug("Successfully connected ipc stream.");

            return ipcClientStream;
        }
    }

    /// <summary>
    /// This class creates IPC Server - TCP Server router instances.
    /// Supports NamedPipes/UnixDomainSocket server and TCP/IP server.
    /// </summary>
    internal class IpcServerTcpServerRouterFactory : DiagnosticsServerRouterFactory
    {
        ILogger _logger;
        TcpServerRouterFactory _tcpServerRouterFactory;
        IpcServerRouterFactory _ipcServerRouterFactory;

        public IpcServerTcpServerRouterFactory(string ipcServer, string tcpServer, int runtimeTimeoutMs, TcpServerRouterFactory.CreateInstanceDelegate factory, ILogger logger)
        {
            _logger = logger;
            _tcpServerRouterFactory = factory(tcpServer, runtimeTimeoutMs, logger);
            _ipcServerRouterFactory = new IpcServerRouterFactory(ipcServer, logger);
        }

        public override string IpcAddress
        {
            get
            {
                return _ipcServerRouterFactory.IpcServerPath;
            }
        }

        public override string TcpAddress
        {
            get
            {
                return _tcpServerRouterFactory.TcpServerAddress;
            }
        }

        public override ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public override Task Start(CancellationToken token)
        {
            _tcpServerRouterFactory.Start();
            _ipcServerRouterFactory.Start();

            _logger?.LogInformation($"Starting IPC server ({_ipcServerRouterFactory.IpcServerPath}) <--> TCP server ({_tcpServerRouterFactory.TcpServerAddress}) router.");

            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            _logger?.LogInformation($"Stopping IPC server ({_ipcServerRouterFactory.IpcServerPath}) <--> TCP server ({_tcpServerRouterFactory.TcpServerAddress}) router.");
            _ipcServerRouterFactory.Stop();
            return _tcpServerRouterFactory.Stop();
        }

        public override void Reset()
        {
            _tcpServerRouterFactory.Reset();
        }

        public override async Task<Router> CreateRouterAsync(CancellationToken token)
        {
            Stream tcpServerStream = null;
            Stream ipcServerStream = null;

            _logger?.LogDebug($"Trying to create new router instance.");

            try
            {
                using CancellationTokenSource cancelRouter = CancellationTokenSource.CreateLinkedTokenSource(token);

                // Get new tcp server endpoint.
                using var tcpServerStreamTask = _tcpServerRouterFactory.AcceptTcpStreamAsync(cancelRouter.Token);

                // Get new ipc server endpoint.
                using var ipcServerStreamTask = _ipcServerRouterFactory.AcceptIpcStreamAsync(cancelRouter.Token);

                await Task.WhenAny(ipcServerStreamTask, tcpServerStreamTask).ConfigureAwait(false);

                if (IsCompletedSuccessfully(ipcServerStreamTask) && IsCompletedSuccessfully(tcpServerStreamTask))
                {
                    ipcServerStream = ipcServerStreamTask.Result;
                    tcpServerStream = tcpServerStreamTask.Result;
                }
                else if (IsCompletedSuccessfully(ipcServerStreamTask))
                {
                    ipcServerStream = ipcServerStreamTask.Result;

                    // We have a valid ipc stream and a pending tcp accept. Wait for completion
                    // or disconnect of ipc stream.
                    using var checkIpcStreamTask = IsStreamConnectedAsync(ipcServerStream, cancelRouter.Token);

                    // Wait for at least completion of one task.
                    await Task.WhenAny(tcpServerStreamTask, checkIpcStreamTask).ConfigureAwait(false);

                    // Cancel out any pending tasks not yet completed.
                    cancelRouter.Cancel();

                    try
                    {
                        await Task.WhenAll(tcpServerStreamTask, checkIpcStreamTask).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Check if we have an accepted tcp stream.
                        if (IsCompletedSuccessfully(tcpServerStreamTask))
                            tcpServerStreamTask.Result?.Dispose();

                        if (checkIpcStreamTask.IsFaulted)
                        {
                            _logger?.LogInformation("Broken ipc connection detected, aborting tcp connection.");
                            checkIpcStreamTask.GetAwaiter().GetResult();
                        }

                        throw;
                    }

                    tcpServerStream = tcpServerStreamTask.Result;
                }
                else if (IsCompletedSuccessfully(tcpServerStreamTask))
                {
                    tcpServerStream = tcpServerStreamTask.Result;

                    // We have a valid tcp stream and a pending ipc accept. Wait for completion
                    // or disconnect of tcp stream.
                    using var checkTcpStreamTask = IsStreamConnectedAsync(tcpServerStream, cancelRouter.Token);

                    // Wait for at least completion of one task.
                    await Task.WhenAny(ipcServerStreamTask, checkTcpStreamTask).ConfigureAwait(false);

                    // Cancel out any pending tasks not yet completed.
                    cancelRouter.Cancel();

                    try
                    {
                        await Task.WhenAll(ipcServerStreamTask, checkTcpStreamTask).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Check if we have an accepted ipc stream.
                        if (IsCompletedSuccessfully(ipcServerStreamTask))
                            ipcServerStreamTask.Result?.Dispose();

                        if (checkTcpStreamTask.IsFaulted)
                        {
                            _logger?.LogInformation("Broken tcp connection detected, aborting ipc connection.");
                            checkTcpStreamTask.GetAwaiter().GetResult();
                        }

                        throw;
                    }

                    ipcServerStream = ipcServerStreamTask.Result;
                }
                else
                {
                    // Error case, cancel out. wait and throw exception.
                    cancelRouter.Cancel();
                    try
                    {
                        await Task.WhenAll(ipcServerStreamTask, tcpServerStreamTask).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Check if we have an ipc stream.
                        if (IsCompletedSuccessfully(ipcServerStreamTask))
                            ipcServerStreamTask.Result?.Dispose();
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                _logger?.LogDebug("Failed creating new router instance.");

                // Cleanup and rethrow.
                ipcServerStream?.Dispose();
                tcpServerStream?.Dispose();

                throw;
            }

            // Create new router.
            _logger?.LogDebug("New router instance successfully created.");

            return new Router(ipcServerStream, tcpServerStream, _logger);
        }
    }

    /// <summary>
    /// This class creates IPC Server - TCP Client router instances.
    /// Supports NamedPipes/UnixDomainSocket server and TCP/IP client.
    /// </summary>
    internal class IpcServerTcpClientRouterFactory : DiagnosticsServerRouterFactory
    {
        ILogger _logger;
        IpcServerRouterFactory _ipcServerRouterFactory;
        TcpClientRouterFactory _tcpClientRouterFactory;

        public IpcServerTcpClientRouterFactory(string ipcServer, string tcpClient, int runtimeTimeoutMs, TcpClientRouterFactory.CreateInstanceDelegate factory, ILogger logger)
        {
            _logger = logger;
            _ipcServerRouterFactory = new IpcServerRouterFactory(ipcServer, logger);
            _tcpClientRouterFactory = factory(tcpClient, runtimeTimeoutMs, logger);
        }

        public override string IpcAddress
        {
            get
            {
                return _ipcServerRouterFactory.IpcServerPath;
            }
        }

        public override string TcpAddress
        {
            get
            {
                return _tcpClientRouterFactory.TcpClientAddress;
            }
        }

        public override ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public override Task Start(CancellationToken token)
        {
            _ipcServerRouterFactory.Start();
            _tcpClientRouterFactory.Start();
            _logger?.LogInformation($"Starting IPC server ({_ipcServerRouterFactory.IpcServerPath}) <--> TCP client ({_tcpClientRouterFactory.TcpClientAddress}) router.");

            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            _logger?.LogInformation($"Stopping IPC server ({_ipcServerRouterFactory.IpcServerPath}) <--> TCP client ({_tcpClientRouterFactory.TcpClientAddress}) router.");
            _tcpClientRouterFactory.Stop();
            _ipcServerRouterFactory.Stop();
            return Task.CompletedTask;
        }

        public override async Task<Router> CreateRouterAsync(CancellationToken token)
        {
            Stream tcpClientStream = null;
            Stream ipcServerStream = null;

            _logger?.LogDebug("Trying to create a new router instance.");

            try
            {
                using CancellationTokenSource cancelRouter = CancellationTokenSource.CreateLinkedTokenSource(token);

                // Get new server endpoint.
                ipcServerStream = await _ipcServerRouterFactory.AcceptIpcStreamAsync(cancelRouter.Token).ConfigureAwait(false);

                // Get new client endpoint.
                using var tcpClientStreamTask = _tcpClientRouterFactory.ConnectTcpStreamAsync(cancelRouter.Token);

                // We have a valid ipc stream and a pending tcp stream. Wait for completion
                // or disconnect of ipc stream.
                using var checkIpcStreamTask = IsStreamConnectedAsync(ipcServerStream, cancelRouter.Token);

                // Wait for at least completion of one task.
                await Task.WhenAny(tcpClientStreamTask, checkIpcStreamTask).ConfigureAwait(false);

                // Cancel out any pending tasks not yet completed.
                cancelRouter.Cancel();

                try
                {
                    await Task.WhenAll(tcpClientStreamTask, checkIpcStreamTask).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Check if we have an accepted tcp stream.
                    if (IsCompletedSuccessfully(tcpClientStreamTask))
                        tcpClientStreamTask.Result?.Dispose();

                    if (checkIpcStreamTask.IsFaulted)
                    {
                        _logger?.LogInformation("Broken ipc connection detected, aborting tcp connection.");
                        checkIpcStreamTask.GetAwaiter().GetResult();
                    }

                    throw;
                }

                tcpClientStream = tcpClientStreamTask.Result;
            }
            catch (Exception)
            {
                _logger?.LogDebug("Failed creating new router instance.");

                // Cleanup and rethrow.
                ipcServerStream?.Dispose();
                tcpClientStream?.Dispose();

                throw;
            }

            // Create new router.
            _logger?.LogDebug("New router instance successfully created.");

            return new Router(ipcServerStream, tcpClientStream, _logger);
        }
    }

    /// <summary>
    /// This class creates IPC Client - TCP Server router instances.
    /// Supports NamedPipes/UnixDomainSocket client and TCP/IP server.
    /// </summary>
    internal class IpcClientTcpServerRouterFactory : DiagnosticsServerRouterFactory
    {
        ILogger _logger;
        IpcClientRouterFactory _ipcClientRouterFactory;
        TcpServerRouterFactory _tcpServerRouterFactory;

        public IpcClientTcpServerRouterFactory(string ipcClient, string tcpServer, int runtimeTimeoutMs, TcpServerRouterFactory.CreateInstanceDelegate factory, ILogger logger)
        {
            _logger = logger;
            _ipcClientRouterFactory = new IpcClientRouterFactory(ipcClient, logger);
            _tcpServerRouterFactory = factory(tcpServer, runtimeTimeoutMs, logger);
        }

        public override string IpcAddress
        {
            get
            {
                return _ipcClientRouterFactory.IpcClientPath;
            }
        }

        public override string TcpAddress
        {
            get
            {
                return _tcpServerRouterFactory.TcpServerAddress;
            }
        }

        public override ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public override Task Start(CancellationToken token)
        {
            if (string.IsNullOrEmpty(_ipcClientRouterFactory.IpcClientPath))
                throw new ArgumentException("No IPC client path specified.");

            _tcpServerRouterFactory.Start();

            _logger?.LogInformation($"Starting IPC client ({_ipcClientRouterFactory.IpcClientPath}) <--> TCP server ({_tcpServerRouterFactory.TcpServerAddress}) router.");

            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            _logger?.LogInformation($"Stopping IPC client ({_ipcClientRouterFactory.IpcClientPath}) <--> TCP server ({_tcpServerRouterFactory.TcpServerAddress}) router.");
            return _tcpServerRouterFactory.Stop();
        }

        public override void Reset()
        {
            _tcpServerRouterFactory.Reset();
        }

        public override async Task<Router> CreateRouterAsync(CancellationToken token)
        {
            Stream tcpServerStream = null;
            Stream ipcClientStream = null;

            _logger?.LogDebug("Trying to create a new router instance.");

            try
            {
                using CancellationTokenSource cancelRouter = CancellationTokenSource.CreateLinkedTokenSource(token);

                // Get new server endpoint.
                tcpServerStream = await _tcpServerRouterFactory.AcceptTcpStreamAsync(cancelRouter.Token).ConfigureAwait(false);

                // Get new client endpoint.
                using var ipcClientStreamTask = _ipcClientRouterFactory.ConnectIpcStreamAsync(cancelRouter.Token);

                // We have a valid tcp stream and a pending ipc stream. Wait for completion
                // or disconnect of tcp stream.
                using var checkTcpStreamTask = IsStreamConnectedAsync(tcpServerStream, cancelRouter.Token);

                // Wait for at least completion of one task.
                await Task.WhenAny(ipcClientStreamTask, checkTcpStreamTask).ConfigureAwait(false);

                // Cancel out any pending tasks not yet completed.
                cancelRouter.Cancel();

                try
                {
                    await Task.WhenAll(ipcClientStreamTask, checkTcpStreamTask).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Check if we have an accepted ipc stream.
                    if (IsCompletedSuccessfully(ipcClientStreamTask))
                        ipcClientStreamTask.Result?.Dispose();

                    if (checkTcpStreamTask.IsFaulted)
                    {
                        _logger?.LogInformation("Broken tcp connection detected, aborting ipc connection.");
                        checkTcpStreamTask.GetAwaiter().GetResult();
                    }

                    throw;
                }

                ipcClientStream = ipcClientStreamTask.Result;

                try
                {
                    // TcpServer consumes advertise message, needs to be replayed back to ipc client.
                    await IpcAdvertise.SerializeAsync(ipcClientStream, _tcpServerRouterFactory.RuntimeInstanceId, (ulong)_tcpServerRouterFactory.RuntimeProcessId, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    _logger?.LogDebug("Failed sending advertise message.");
                    throw;
                }
            }
            catch (Exception)
            {
                _logger?.LogDebug("Failed creating new router instance.");

                // Cleanup and rethrow.
                tcpServerStream?.Dispose();
                ipcClientStream?.Dispose();

                throw;
            }

            // Create new router.
            _logger?.LogDebug("New router instance successfully created.");

            return new Router(ipcClientStream, tcpServerStream, _logger, (ulong)IpcAdvertise.V1SizeInBytes);
        }
    }

    /// <summary>
    /// This class creates IPC Client - TCP Client router instances.
    /// Supports NamedPipes/UnixDomainSocket client and TCP/IP client.
    /// </summary>
    internal class IpcClientTcpClientRouterFactory : DiagnosticsServerRouterFactory
    {
        Guid _runtimeInstanceId;
        ulong _runtimeProcessId;
        ILogger _logger;
        IpcClientRouterFactory _ipcClientRouterFactory;
        TcpClientRouterFactory _tcpClientRouterFactory;

        public IpcClientTcpClientRouterFactory(string ipcClient, string tcpClient, int runtimeTimeoutMs, TcpClientRouterFactory.CreateInstanceDelegate factory, ILogger logger)
        {
            _runtimeInstanceId = Guid.Empty;
            _runtimeProcessId = 0;
            _logger = logger;
            _ipcClientRouterFactory = new IpcClientRouterFactory(ipcClient, logger);
            _tcpClientRouterFactory = factory(tcpClient, runtimeTimeoutMs, logger);
        }

        public override string IpcAddress {
            get
            {
                return _ipcClientRouterFactory.IpcClientPath;
            }
        }

        public override string TcpAddress {
            get
            {
                return _tcpClientRouterFactory.TcpClientAddress;
            }
        }

        public override ILogger Logger {
            get
            {
                return _logger;
            }
        }

        public override Task Start(CancellationToken token)
        {
            _tcpClientRouterFactory.Start();
            _logger?.LogInformation($"Starting IPC client ({_ipcClientRouterFactory.IpcClientPath}) <--> TCP client ({_tcpClientRouterFactory.TcpClientAddress}) router.");

            var requestRuntimeInfo = new Task(() =>
            {
                try
                {
                    _logger?.LogDebug($"Requesting runtime process information.");

                    // Get new tcp client endpoint.
                    using var tcpClientStream = _tcpClientRouterFactory.ConnectTcpStreamAsync(token).Result;

                    // Request process info.
                    IpcMessage message = new IpcMessage(DiagnosticsServerCommandSet.Process, (byte)ProcessCommandId.GetProcessInfo);

                    byte[] buffer = message.Serialize();
                    tcpClientStream.Write(buffer, 0, buffer.Length);

                    var response = IpcMessage.Parse(tcpClientStream);
                    if ((DiagnosticsServerResponseId)response.Header.CommandId == DiagnosticsServerResponseId.OK)
                    {
                        var info = ProcessInfo.ParseV1(response.Payload);

                        _runtimeProcessId = info.ProcessId;
                        _runtimeInstanceId = info.RuntimeInstanceCookie;

                        _logger?.LogDebug($"Retrieved runtime process information, pid={_runtimeProcessId}, cookie={_runtimeInstanceId}.");
                    }
                    else
                    {
                        throw new ServerErrorException("Failed to retrieve runtime process info.");
                    }
                }
                catch (Exception)
                {
                    _runtimeProcessId = (ulong)Process.GetCurrentProcess().Id;
                    _runtimeInstanceId = Guid.NewGuid();
                    _logger?.LogWarning($"Failed to retrieve runtime process info, fallback to current process information, pid={_runtimeProcessId}, cookie={_runtimeInstanceId}.");
                }
            });

            requestRuntimeInfo.Start();
            return requestRuntimeInfo;
        }

        public override Task Stop()
        {
            _logger?.LogInformation($"Stopping IPC client ({_ipcClientRouterFactory.IpcClientPath}) <--> TCP client ({_tcpClientRouterFactory.TcpClientAddress}) router.");
            _tcpClientRouterFactory.Stop();
            return Task.CompletedTask;
        }

        public override async Task<Router> CreateRouterAsync(CancellationToken token)
        {
            Stream tcpClientStream = null;
            Stream ipcClientStream = null;

            _logger?.LogDebug("Trying to create a new router instance.");

            try
            {
                using CancellationTokenSource cancelRouter = CancellationTokenSource.CreateLinkedTokenSource(token);

                // Get new tcp client endpoint.
                tcpClientStream = await _tcpClientRouterFactory.ConnectTcpStreamAsync(cancelRouter.Token, true).ConfigureAwait(false);

                // Get new ipc client endpoint.
                using var ipcClientStreamTask = _ipcClientRouterFactory.ConnectIpcStreamAsync(cancelRouter.Token);

                // We have a valid tcp stream and a pending ipc stream. Wait for completion
                // or disconnect of tcp stream.
                using var checkTcpStreamTask = IsStreamConnectedAsync(tcpClientStream, cancelRouter.Token);

                // Wait for at least completion of one task.
                await Task.WhenAny(ipcClientStreamTask, checkTcpStreamTask).ConfigureAwait(false);

                // Cancel out any pending tasks not yet completed.
                cancelRouter.Cancel();

                try
                {
                    await Task.WhenAll(ipcClientStreamTask, checkTcpStreamTask).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Check if we have an accepted ipc stream.
                    if (IsCompletedSuccessfully(ipcClientStreamTask))
                        ipcClientStreamTask.Result?.Dispose();

                    if (checkTcpStreamTask.IsFaulted)
                    {
                        _logger?.LogInformation("Broken tcp connection detected, aborting ipc connection.");
                        checkTcpStreamTask.GetAwaiter().GetResult();
                    }

                    throw;
                }

                ipcClientStream = ipcClientStreamTask.Result;

                try
                {
                    await IpcAdvertise.SerializeAsync(ipcClientStream, _runtimeInstanceId, _runtimeProcessId, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    _logger?.LogDebug("Failed sending advertise message.");
                    throw;
                }
            }
            catch (Exception)
            {
                _logger?.LogDebug("Failed creating new router instance.");

                // Cleanup and rethrow.
                tcpClientStream?.Dispose();
                ipcClientStream?.Dispose();

                throw;
            }

            // Create new router.
            _logger?.LogDebug("New router instance successfully created.");

            return new Router(ipcClientStream, tcpClientStream, _logger, (ulong)IpcAdvertise.V1SizeInBytes);
        }
    }

    internal class Router : IDisposable
    {
        readonly ILogger _logger;

        Stream _frontendStream = null;
        Stream _backendStream = null;

        Task _backendReadFrontendWriteTask = null;
        Task _frontendReadBackendWriteTask = null;

        CancellationTokenSource _cancelRouterTokenSource = null;

        bool _disposed = false;

        ulong _backendToFrontendByteTransfer;
        ulong _frontendToBackendByteTransfer;

        static int s_routerInstanceCount;

        public TaskCompletionSource<bool> RouterTaskCompleted { get; }

        public Router(Stream frontendStream, Stream backendStream, ILogger logger, ulong initBackendToFrontendByteTransfer = 0, ulong initFrontendToBackendByteTransfer = 0)
        {
            _logger = logger;

            _frontendStream = frontendStream;
            _backendStream = backendStream;

            _cancelRouterTokenSource = new CancellationTokenSource();

            RouterTaskCompleted = new TaskCompletionSource<bool>();

            _backendToFrontendByteTransfer = initBackendToFrontendByteTransfer;
            _frontendToBackendByteTransfer = initFrontendToBackendByteTransfer;

            Interlocked.Increment(ref s_routerInstanceCount);
        }

        public void Start()
        {
            if (_backendReadFrontendWriteTask != null || _frontendReadBackendWriteTask != null || _disposed)
                throw new InvalidOperationException();

            _backendReadFrontendWriteTask = BackendReadFrontendWrite(_cancelRouterTokenSource.Token);
            _frontendReadBackendWriteTask = FrontendReadBackendWrite(_cancelRouterTokenSource.Token);
        }

        public async void Stop()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Router));

            _cancelRouterTokenSource.Cancel();

            List<Task> runningTasks = new List<Task>();

            if (_backendReadFrontendWriteTask != null)
                runningTasks.Add(_backendReadFrontendWriteTask);

            if (_frontendReadBackendWriteTask != null)
                runningTasks.Add(_frontendReadBackendWriteTask);

            await Task.WhenAll(runningTasks.ToArray()).ConfigureAwait(false);

            _backendReadFrontendWriteTask?.Dispose();
            _frontendReadBackendWriteTask?.Dispose();

            RouterTaskCompleted?.TrySetResult(true);

            _backendReadFrontendWriteTask = null;
            _frontendReadBackendWriteTask = null;
        }

        public bool IsRunning {
            get
            {
                if (_backendReadFrontendWriteTask == null || _frontendReadBackendWriteTask == null || _disposed)
                    return false;

                return !_backendReadFrontendWriteTask.IsCompleted && !_frontendReadBackendWriteTask.IsCompleted;
            }
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();

                _cancelRouterTokenSource.Dispose();

                _backendStream?.Dispose();
                _frontendStream?.Dispose();

                _disposed = true;

                Interlocked.Decrement(ref s_routerInstanceCount);

                _logger?.LogTrace($"Disposed stats: Back End->Front End {_backendToFrontendByteTransfer} bytes, Front End->Back End {_frontendToBackendByteTransfer} bytes.");
                _logger?.LogTrace($"Active instances: {s_routerInstanceCount}");
            }
        }

        async Task BackendReadFrontendWrite(CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (!token.IsCancellationRequested)
                {
                    _logger?.LogTrace("Start reading bytes from back end.");

                    int bytesRead = await _backendStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);

                    _logger?.LogTrace($"Read {bytesRead} bytes from back end.");

                    // Check for end of stream indicating that remote end disconnected.
                    if (bytesRead == 0)
                    {
                        _logger?.LogTrace("Back end disconnected.");
                        break;
                    }

                    _backendToFrontendByteTransfer += (ulong)bytesRead;

                    _logger?.LogTrace($"Start writing {bytesRead} bytes to front end.");

                    await _frontendStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                    await _frontendStream.FlushAsync().ConfigureAwait(false);

                    _logger?.LogTrace($"Wrote {bytesRead} bytes to front end.");
                }
            }
            catch (Exception)
            {
                // Completing task will trigger dispose of instance and cleanup.
                // Faliure mainly consists of closed/disposed streams and cancelation requests.
                // Just make sure task gets complete, nothing more needs to be in response to these exceptions.
                _logger?.LogTrace("Failed stream operation. Completing task.");
            }

            RouterTaskCompleted?.TrySetResult(true);
        }

        async Task FrontendReadBackendWrite(CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (!token.IsCancellationRequested)
                {
                    _logger?.LogTrace("Start reading bytes from front end.");

                    int bytesRead = await _frontendStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);

                    _logger?.LogTrace($"Read {bytesRead} bytes from front end.");

                    // Check for end of stream indicating that remote end disconnected.
                    if (bytesRead == 0)
                    {
                        _logger?.LogTrace("Front end disconnected.");
                        break;
                    }

                    _frontendToBackendByteTransfer += (ulong)bytesRead;

                    _logger?.LogTrace($"Start writing {bytesRead} bytes to back end.");

                    await _backendStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                    await _backendStream.FlushAsync().ConfigureAwait(false);

                    _logger?.LogTrace($"Wrote {bytesRead} bytes to back end.");
                }
            }
            catch (Exception)
            {
                // Completing task will trigger dispose of instance and cleanup.
                // Faliure mainly consists of closed/disposed streams and cancelation requests.
                // Just make sure task gets complete, nothing more needs to be in response to these exceptions.
                _logger?.LogTrace("Failed stream operation. Completing task.");
            }

            RouterTaskCompleted?.TrySetResult(true);
        }
    }
}
