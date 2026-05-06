// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    /// <summary>
    /// Establishes server endpoint for runtime instances to connect when
    /// configured to provide diagnostic endpoints in reverse mode.
    /// </summary>
    internal sealed class ReversedDiagnosticsServer : IAsyncDisposable
    {
        // The amount of time to allow parsing of the advertise data before cancelling. This allows the server to
        // remain responsive in case the advertise data is incomplete and the stream is not closed.
        private static readonly TimeSpan ParseAdvertiseTimeout = TimeSpan.FromMilliseconds(250);

        private readonly CancellationTokenSource _disposalSource = new CancellationTokenSource();
        private readonly HandleableCollection<IpcEndpointInfo> _endpointInfos = new HandleableCollection<IpcEndpointInfo>();
        private readonly ConcurrentDictionary<Guid, HandleableCollection<Stream>> _streamCollections = new ConcurrentDictionary<Guid, HandleableCollection<Stream>>();
        private readonly string _address;

        private bool _disposed = false;
        private Task _listenTask;
        private bool _enableTcpIpProtocol = false;

        /// <summary>
        /// Constructs the <see cref="ReversedDiagnosticsServer"/> instance with an endpoint bound
        /// to the location specified by <paramref name="address"/>.
        /// </summary>
        /// <param name="address">
        /// The server endpoint.
        /// On Windows, this can be a full pipe path or the name without the "\\.\pipe\" prefix.
        /// On all other systems, this must be the full file path of the socket.
        /// </param>
        public ReversedDiagnosticsServer(string address)
        {
            _address = address;
        }

        /// <summary>
        /// Constructs the <see cref="ReversedDiagnosticsServer"/> instance with an endpoint bound
        /// to the location specified by <paramref name="address"/>.
        /// </summary>
        /// <param name="address">
        /// The server endpoint.
        /// On Windows, this can be a full pipe path or the name without the "\\.\pipe\" prefix.
        /// On all other systems, this must be the full file path of the socket.
        /// When TcpIp is enabled, this can also be host:port of the listening socket.
        /// </param>
        /// <param name="enableTcpIpProtocol">
        /// Add TcpIp as a supported protocol for ReversedDiagnosticServer. When enabled, address will
        /// be analyzed and if on format host:port, ReversedDiagnosticServer will try to bind
        /// a TcpIp listener to host and port.
        ///
        /// </param>
        public ReversedDiagnosticsServer(string address, bool enableTcpIpProtocol)
        {
            _address = address;
            _enableTcpIpProtocol = enableTcpIpProtocol;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposalSource.Cancel();

                if (null != _listenTask)
                {
                    try
                    {
                        await _listenTask.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail(ex.Message);
                    }
                }

                _endpointInfos.Dispose();

                foreach (HandleableCollection<Stream> streamCollection in _streamCollections.Values)
                {
                    streamCollection.Dispose();
                }

                _streamCollections.Clear();

                _disposalSource.Dispose();

                _disposed = true;
            }
        }

        /// <summary>
        /// Starts listening at the address for new connections.
        /// </summary>
        public void Start()
        {
            Start(MaxAllowedConnections);
        }

        /// <summary>
        /// Starts listening at the address for new connections.
        /// </summary>
        /// <param name="maxConnections">The maximum number of connections the server will support.</param>
        public void Start(int maxConnections)
        {
            VerifyNotDisposed();

            if (IsStarted)
            {
                throw new InvalidOperationException(nameof(ReversedDiagnosticsServer.Start) + " method can only be called once.");
            }

            _listenTask = ListenAsync(maxConnections, _disposalSource.Token);
            if (_listenTask.IsFaulted)
                _listenTask.Wait(); // Rethrow aggregated exception.
        }

        /// <summary>
        /// Gets endpoint information when a new runtime instance connects to the server.
        /// </summary>
        /// <param name="timeout">The amount of time to wait before cancelling the accept operation.</param>
        /// <returns>An <see cref="IpcEndpointInfo"/> value that contains information about the new runtime instance connection.</returns>
        public IpcEndpointInfo Accept(TimeSpan timeout)
        {
            VerifyNotDisposed();
            VerifyIsStarted();

            return _endpointInfos.Handle(timeout);
        }

        /// <summary>
        /// Gets endpoint information when a new runtime instance connects to the server.
        /// </summary>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>A task that completes with a <see cref="IpcEndpointInfo"/> value that contains information about the new runtime instance connection.</returns>
        public Task<IpcEndpointInfo> AcceptAsync(CancellationToken token)
        {
            VerifyNotDisposed();
            VerifyIsStarted();

            return _endpointInfos.HandleAsync(token);
        }

        /// <summary>
        /// Removes endpoint information from the server so that it is no longer tracked.
        /// </summary>
        /// <param name="runtimeCookie">The runtime instance cookie that corresponds to the endpoint to be removed.</param>
        /// <returns>True if the endpoint existed and was removed; otherwise false.</returns>
        public bool RemoveConnection(Guid runtimeCookie)
        {
            VerifyNotDisposed();
            VerifyIsStarted();

            if (_streamCollections.TryRemove(runtimeCookie, out HandleableCollection<Stream> streamCollection))
            {
                streamCollection.Dispose();
                return true;
            }
            return false;
        }

        private void VerifyNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ReversedDiagnosticsServer));
            }
        }

        private void VerifyIsStarted()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException(nameof(ReversedDiagnosticsServer.Start) + " method must be called before invoking this operation.");
            }
        }

        /// <summary>
        /// Listens at the address for new connections.
        /// </summary>
        /// <param name="maxConnections">The maximum number of connections the server will support.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>A task that completes when the server is no longer listening at the address.</returns>
        private async Task ListenAsync(int maxConnections, CancellationToken token)
        {
            // This disposal shuts down the transport in case an exception is thrown.
            using var transport = IpcServerTransport.Create(_address, maxConnections, _enableTcpIpProtocol, TransportCallback);
            // This disposal shuts down the transport in case of cancellation; causes the transport
            // to not recreate the server stream before the AcceptAsync call observes the cancellation.
            using var _ = token.Register(() => transport.Dispose());

            while (!token.IsCancellationRequested)
            {
                Stream stream = null;
                IpcAdvertise advertise = null;
                try
                {
                    stream = await transport.AcceptAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                    // The advertise data could be incomplete if the runtime shuts down before completely writing
                    // the information. Catch the exception and continue waiting for a new connection.
                }

                if (null != stream)
                {
                    // Cancel parsing of advertise data after timeout period to
                    // mitigate runtimes that write partial data and do not close the stream (avoid waiting forever).
                    using var parseCancellationSource = new CancellationTokenSource();
                    using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, parseCancellationSource.Token);
                    try
                    {
                        parseCancellationSource.CancelAfter(ParseAdvertiseTimeout);

                        advertise = await IpcAdvertise.ParseAsync(stream, linkedSource.Token).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        stream.Dispose();
                    }
                }

                if (null != advertise)
                {
                    Guid runtimeCookie = advertise.RuntimeInstanceCookie;
                    int pid = unchecked((int)advertise.ProcessId);

                    // The valueFactory parameter of the GetOrAdd overload that uses Func<TKey, TValue> valueFactory
                    // does not execute the factory under a lock thus it is not thread-safe. Create the collection and
                    // use a thread-safe version of GetOrAdd; use equality comparison on the result to determine if
                    // the new collection was added to the dictionary or if an existing one was returned.
                    var newStreamCollection = new HandleableCollection<Stream>();
                    var streamCollection = _streamCollections.GetOrAdd(runtimeCookie, newStreamCollection);

                    try
                    {
                        streamCollection.ClearItems();
                        streamCollection.Add(stream);

                        if (newStreamCollection == streamCollection)
                        {
                            ServerIpcEndpoint endpoint = new ServerIpcEndpoint(this, runtimeCookie);
                            _endpointInfos.Add(new IpcEndpointInfo(endpoint, pid, runtimeCookie));
                        }
                        else
                        {
                            newStreamCollection.Dispose();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // The stream collection could be disposed by RemoveConnection which would cause an
                        // ObjectDisposedException to be thrown if trying to clear/add the stream.
                        stream.Dispose();
                    }
                }
            }
        }

        private HandleableCollection<Stream> GetStreams(Guid runtimeCookie)
        {
            return _streamCollections.GetOrAdd(runtimeCookie, _ => new HandleableCollection<Stream>());
        }

        internal Stream Connect(Guid runtimeInstanceCookie, TimeSpan timeout)
        {
            VerifyNotDisposed();
            VerifyIsStarted();

            return GetStreams(runtimeInstanceCookie).Handle(timeout);
        }

        internal Task<Stream> ConnectAsync(Guid runtimeInstanceCookie, CancellationToken token)
        {
            VerifyNotDisposed();
            VerifyIsStarted();

            return GetStreams(runtimeInstanceCookie).HandleAsync(token);
        }

        internal void WaitForConnection(Guid runtimeInstanceCookie, TimeSpan timeout)
        {
            VerifyNotDisposed();
            VerifyIsStarted();

            GetStreams(runtimeInstanceCookie).Handle(WaitForConnectionHandler, timeout);
        }

        internal async Task WaitForConnectionAsync(Guid runtimeInstanceCookie, CancellationToken token)
        {
            VerifyNotDisposed();
            VerifyIsStarted();

            await GetStreams(runtimeInstanceCookie).HandleAsync(WaitForConnectionHandler, token).ConfigureAwait(false);
        }

        private static bool WaitForConnectionHandler(Stream item, out bool removeItem)
        {
            if (!TestStream(item))
            {
                item?.Dispose();
                removeItem = true;
                return false;
            }

            removeItem = false;
            return true;
        }

        private static bool TestStream(Stream stream)
        {
            if (null == stream)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (stream is ExposedSocketNetworkStream networkStream)
            {
                // Update Connected state of socket by sending non-blocking zero-byte data.
                Socket socket = networkStream.Socket;
                bool blocking = socket.Blocking;
                try
                {
                    socket.Blocking = false;
                    socket.Send(Array.Empty<byte>(), 0, SocketFlags.None);
                }
                catch (Exception)
                {
                }
                finally
                {
                    socket.Blocking = blocking;
                }
                return socket.Connected;
            }
            else if (stream is PipeStream pipeStream)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Pipe stream should only be used on Windows.");

                // PeekNamedPipe will return false if the pipe is disconnected/broken.
                return NativeMethods.PeekNamedPipe(
                    pipeStream.SafePipeHandle,
                    null,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }

            return false;
        }

        private bool IsStarted => null != _listenTask;

        public static int MaxAllowedConnections = IpcServerTransport.MaxAllowedConnections;

        internal IIpcServerTransportCallbackInternal TransportCallback { get; set; }
    }
}
