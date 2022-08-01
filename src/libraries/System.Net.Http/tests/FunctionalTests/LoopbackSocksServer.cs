// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    /// <summary>
    /// Provides a test-only SOCKS4/5 proxy.
    /// </summary>
    internal class LoopbackSocksServer : IAsyncDisposable
    {
        private readonly Socket _listener;
        private readonly List<Task> _connectionTasks = new();
        private readonly TaskCompletionSource _serverStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Port { get; }

        private string? _username, _password;

        public LoopbackSocksServer(string? username = null, string? password = null)
        {
            if (password != null && username == null)
            {
                throw new ArgumentException("Password must be used together with username.", nameof(password));
            }

            _username = username;
            _password = password;

            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _listener.Listen();

            var ep = (IPEndPoint)_listener.LocalEndPoint;
            Port = ep.Port;

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Socket s = await _listener.AcceptAsync().ConfigureAwait(false);

                        _connectionTasks.Add(Task.Run(async () =>
                        {
                            using (var ns = new NetworkStream(s, ownsSocket: true))
                            {
                                await ProcessRequest(s, ns).ConfigureAwait(false);
                            }
                        }));
                    }
                    catch
                    {
                        break;
                    }
                }

                _serverStopped.SetResult();
            });
        }

        private async Task ProcessRequest(Socket clientSocket, NetworkStream ns)
        {
            int version = await ns.ReadByteAsync().ConfigureAwait(false);

            await (version switch
            {
                4 => ProcessSocks4Request(clientSocket, ns),
                5 => ProcessSocks5Request(clientSocket, ns),
                -1 => throw new Exception("Early EOF"),
                _ => throw new Exception("Bad request version")
            }).ConfigureAwait(false);
        }

        private async Task ProcessSocks4Request(Socket clientSocket, NetworkStream ns)
        {
            byte[] buffer = new byte[7];
            await ns.ReadExactlyAsync(buffer).ConfigureAwait(false);

            if (buffer[0] != 1)
                throw new Exception("Only CONNECT is supported.");

            int port = (buffer[1] << 8) + buffer[2];
            // formats ip into string to ensure we get the correct order
            string remoteHost = $"{buffer[3]}.{buffer[4]}.{buffer[5]}.{buffer[6]}";

            byte[] usernameBuffer = new byte[1024];
            int usernameBytes = 0;
            while (true)
            {
                int usernameByte = await ns.ReadByteAsync().ConfigureAwait(false);
                if (usernameByte == 0)
                    break;
                if (usernameByte == -1)
                    throw new Exception("Early EOF");
                usernameBuffer[usernameBytes++] = (byte)usernameByte;
            }

            if (remoteHost.StartsWith("0.0.0") && remoteHost != "0.0.0.0")
            {
                byte[] hostBuffer = new byte[1024];
                int hostnameBytes = 0;

                while (true)
                {
                    int b = await ns.ReadByteAsync().ConfigureAwait(false);
                    if (b == -1)
                        throw new Exception("Early EOF");
                    if (b == 0)
                        break;

                    hostBuffer[hostnameBytes++] = (byte)b;
                }

                remoteHost = Encoding.UTF8.GetString(hostBuffer.AsSpan(0, hostnameBytes));
            }

            if (_username != null)
            {
                string username = Encoding.UTF8.GetString(usernameBuffer.AsSpan(0, usernameBytes));
                if (username != _username)
                {
                    ns.WriteByte(4);
                    buffer[0] = 93;
                    await ns.WriteAsync(buffer).ConfigureAwait(false);
                    return;
                }
            }

            ns.WriteByte(4);
            buffer[0] = 90;
            await ns.WriteAsync(buffer).ConfigureAwait(false);

            await RelayHttpTraffic(clientSocket, ns, remoteHost, port).ConfigureAwait(false);
        }

        private async Task ProcessSocks5Request(Socket clientSocket, NetworkStream ns)
        {
            int nMethods = await ns.ReadByteAsync().ConfigureAwait(false);
            if (nMethods == -1)
                throw new Exception("Early EOF");

            byte[] buffer = new byte[1024];
            await ns.ReadExactlyAsync(buffer.AsMemory(0, nMethods)).ConfigureAwait(false);

            byte expectedAuthMethod = _username == null ? (byte)0 : (byte)2;
            if (!buffer.AsSpan(0, nMethods).Contains(expectedAuthMethod))
            {
                await ns.WriteAsync(new byte[] { 5, 0xFF }).ConfigureAwait(false);
                return;
            }

            await ns.WriteAsync(new byte[] { 5, expectedAuthMethod }).ConfigureAwait(false);

            if (_username != null)
            {
                if (await ns.ReadByteAsync().ConfigureAwait(false) != 1)
                    throw new Exception("Bad subnegotiation version.");

                int usernameLength = await ns.ReadByteAsync().ConfigureAwait(false);
                await ns.ReadExactlyAsync(buffer.AsMemory(0, usernameLength)).ConfigureAwait(false);
                string username = Encoding.UTF8.GetString(buffer.AsSpan(0, usernameLength));

                int passwordLength = await ns.ReadByteAsync().ConfigureAwait(false);
                await ns.ReadExactlyAsync(buffer.AsMemory(0, passwordLength)).ConfigureAwait(false);
                string password = Encoding.UTF8.GetString(buffer.AsSpan(0, passwordLength));

                if (username != _username || password != _password)
                {
                    await ns.WriteAsync(new byte[] { 1, 1 }).ConfigureAwait(false);
                    throw new Exception("Invalid credentials.");
                }

                await ns.WriteAsync(new byte[] { 1, 0 }).ConfigureAwait(false);
            }

            await ns.ReadExactlyAsync(buffer.AsMemory(0, 4)).ConfigureAwait(false);
            if (buffer[0] != 5)
                throw new Exception("Bad protocol version.");
            if (buffer[1] != 1)
                throw new Exception("Only CONNECT is supported.");

            string remoteHost;
            switch (buffer[3])
            {
                case 1:
                    await ns.ReadExactlyAsync(buffer.AsMemory(0, 4)).ConfigureAwait(false);
                    remoteHost = new IPAddress(buffer.AsSpan(0, 4)).ToString();
                    break;
                case 4:
                    await ns.ReadExactlyAsync(buffer.AsMemory(0, 16)).ConfigureAwait(false);
                    remoteHost = new IPAddress(buffer.AsSpan(0, 16)).ToString();
                    break;
                case 3:
                    int length = await ns.ReadByteAsync().ConfigureAwait(false);
                    if (length == -1)
                        throw new Exception("Early EOF");
                    await ns.ReadExactlyAsync(buffer.AsMemory(0, length)).ConfigureAwait(false);
                    remoteHost = Encoding.UTF8.GetString(buffer.AsSpan(0, length));
                    break;

                default:
                    throw new Exception("Unknown address type.");
            }

            await ns.ReadExactlyAsync(buffer.AsMemory(0, 2)).ConfigureAwait(false);
            int port = (buffer[0] << 8) + buffer[1];

            await ns.WriteAsync(new byte[] { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 }).ConfigureAwait(false);

            await RelayHttpTraffic(clientSocket, ns, remoteHost, port).ConfigureAwait(false);
        }

        private async Task RelayHttpTraffic(Socket clientSocket, NetworkStream clientStream, string remoteHost, int remotePort)
        {
            // Open connection to destination server.
            using var serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            await serverSocket.ConnectAsync(remoteHost, remotePort).ConfigureAwait(false);
            var serverStream = new NetworkStream(serverSocket);

            // Relay traffic to/from client and destination server.
            Task clientCopyTask = Task.Run(async () =>
            {
                try
                {
                    await clientStream.CopyToAsync(serverStream).ConfigureAwait(false);
                    serverSocket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception ex)
                {
                    HandleExceptions(ex);
                }
            });

            Task serverCopyTask = Task.Run(async () =>
            {
                try
                {
                    await serverStream.CopyToAsync(clientStream).ConfigureAwait(false);
                    clientSocket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception ex)
                {
                    HandleExceptions(ex);
                }
            });

            await Task.WhenAll(new[] { clientCopyTask, serverCopyTask }).ConfigureAwait(false);

            /// <summary>Closes sockets to cause both tasks to end, and eats connection reset/aborted errors.</summary>
            void HandleExceptions(Exception ex)
            {
                SocketError sockErr = (ex.InnerException as SocketException)?.SocketErrorCode ?? SocketError.Success;

                // If aborted, the other task failed and is asking this task to end.
                if (sockErr == SocketError.OperationAborted)
                {
                    return;
                }

                // Ask the other task to end by disposing, causing OperationAborted.
                try
                {
                    clientSocket.Close();
                }
                catch (ObjectDisposedException)
                {
                }

                try
                {
                    serverSocket.Close();
                }
                catch (ObjectDisposedException)
                {
                }

                // Eat reset/abort.
                if (sockErr != SocketError.ConnectionReset && sockErr != SocketError.ConnectionAborted)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Dispose();
            await _serverStopped.Task;

            List<Exception> exceptions = new();
            foreach (Task task in _connectionTasks)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
