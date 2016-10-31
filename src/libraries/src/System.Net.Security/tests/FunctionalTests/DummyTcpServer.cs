// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // Callback method that is called when the server receives data from a connected client.  
    // The callback method should return a byte array and the number of bytes to send from that array.
    public delegate void DummyTcpServerReceiveCallback(byte[] bufferReceived, int bytesReceived, Stream stream);

    // Provides a dummy TCP/IP server that accepts connections and supports SSL/TLS.
    // It normally echoes data received but can be configured to write a byte array 
    // specified by a callback method.
    public class DummyTcpServer : IDisposable
    {
        private readonly string _creationStack = Environment.StackTrace;
        private VerboseTestLogging _log;
        private TcpListener _listener;
        private bool _useSsl;
        private SslProtocols _sslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
        private EncryptionPolicy _sslEncryptionPolicy;
        private IPEndPoint _remoteEndPoint;
        private DummyTcpServerReceiveCallback _receiveCallback;

        private void StartListener(IPEndPoint endPoint)
        {
            _listener = new TcpListener(endPoint);
            _listener.Start(5);
            _log.WriteLine("Server {0} listening", endPoint.Address.ToString());
            _listener.AcceptTcpClientAsync().ContinueWith(t => OnAccept(t), TaskScheduler.Default);
        }

        public DummyTcpServer(IPEndPoint endPoint) : this(endPoint, null)
        {
        }

        public DummyTcpServer(IPEndPoint endPoint, EncryptionPolicy? sslEncryptionPolicy)
        {
            _log = VerboseTestLogging.GetInstance();

            if (sslEncryptionPolicy != null)
            {
                _remoteEndPoint = endPoint;
                _useSsl = true;
                _sslEncryptionPolicy = (EncryptionPolicy)sslEncryptionPolicy;
            }

            StartListener(endPoint);
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)_listener.LocalEndpoint; }
        }

        public SslProtocols SslProtocols
        {
            get { return _sslProtocols; }
            set { _sslProtocols = value; }
        }

        protected DummyTcpServerReceiveCallback ReceiveCallback
        {
            get { return _receiveCallback; }
            set { _receiveCallback = value; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Stop();
            }
        }

        protected virtual void OnClientAccepted(TcpClient client)
        {
        }

        private void OnAuthenticate(Task result, ClientState state)
        {
            SslStream sslStream = (SslStream)state.Stream;

            try
            {
                result.GetAwaiter().GetResult();
                _log.WriteLine("Server authenticated to client with encryption cipher: {0} {1}-bit strength",
                    sslStream.CipherAlgorithm, sslStream.CipherStrength);

                // Start listening for data from the client connection.
                sslStream.BeginRead(state.ReceiveBuffer, 0, state.ReceiveBuffer.Length, OnReceive, state);
            }
            catch (AuthenticationException authEx)
            {
                _log.WriteLine(
                    "Server disconnecting from client during authentication.  No shared SSL/TLS algorithm. ({0})",
                    authEx);
            }
            catch (Exception ex)
            {
                _log.WriteLine("Server disconnecting from client during authentication.  Exception: {0}",
                    ex.Message);
            }
            finally
            {
                state.Dispose();
            }
        }

        private void OnAccept(Task<TcpClient> result)
        {
            TcpClient client = null;

            // Accept current connection
            try
            {
                client = result.Result;
            }
            catch
            {
            }

            // If we have a connection, then process it
            if (client != null)
            {
                OnClientAccepted(client);

                ClientState state;

                // Start authentication for SSL?
                if (_useSsl)
                {
                    state = new ClientState(client, _sslEncryptionPolicy);
                    _log.WriteLine("Server: starting SSL authentication.");


                    SslStream sslStream = null;
                    X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate();

                    try
                    {
                        sslStream = (SslStream)state.Stream;

                        _log.WriteLine("Server: attempting to open SslStream.");
                        sslStream.AuthenticateAsServerAsync(certificate, false, _sslProtocols, false).ContinueWith(t =>
                        {
                            certificate.Dispose();
                            OnAuthenticate(t, state);
                        }, TaskScheduler.Default);
                    }
                    catch (Exception ex)
                    {
                        _log.WriteLine("Server: Exception: {0}", ex);
                        certificate.Dispose();
                        state.Dispose(); // close connection to client
                    }
                }
                else
                {
                    state = new ClientState(client);

                    // Start listening for data from the client connection
                    try
                    {
                        state.Stream.BeginRead(state.ReceiveBuffer, 0, state.ReceiveBuffer.Length, OnReceive, state);
                    }
                    catch
                    {
                    }
                }
            }

            // Listen for more client connections
            try
            {
                _listener.AcceptTcpClientAsync().ContinueWith(t => OnAccept(t), TaskScheduler.Default);
            }
            catch
            {
            }
        }

        private void OnReceive(IAsyncResult result)
        {
            ClientState state = (ClientState)result.AsyncState;

            try
            {
                int bytesReceived = state.Stream.EndRead(result);
                if (bytesReceived == 0)
                {
                    state.Dispose();
                    return;
                }

                if (_receiveCallback != null)
                {
                    _receiveCallback(state.ReceiveBuffer, bytesReceived, state.Stream);
                }
                else
                {
                    // Echo back what we received
                    state.Stream.Write(state.ReceiveBuffer, 0, bytesReceived);
                }

                // Read more from client (asynchronous)
                state.Stream.BeginRead(state.ReceiveBuffer, 0, state.ReceiveBuffer.Length, OnReceive, state);
            }
            catch (IOException)
            {
                state.Dispose();
                return;
            }
            catch (SocketException)
            {
                state.Dispose();
                return;
            }
            catch (ObjectDisposedException)
            {
                state.Dispose();
                return;
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(
                    $"Exception in {nameof(DummyTcpServer)} created with stack: {_creationStack}",
                    e);
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool AlwaysValidServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;  // allow everything
        }

        private class ClientState
        {
            private TcpClient _tcpClient;
            private byte[] _receiveBuffer;
            private bool _useSsl;
            private SslStream _sslStream;
            private bool _closed;

            public ClientState(TcpClient client)
            {
                _tcpClient = client;
                _receiveBuffer = new byte[1024];
                _useSsl = false;
                _closed = false;
            }

            public ClientState(TcpClient client, EncryptionPolicy sslEncryptionPolicy)
            {
                _tcpClient = client;
                _receiveBuffer = new byte[1024];
                _useSsl = true;
                _sslStream = new SslStream(client.GetStream(), false, AlwaysValidServerCertificate, null, sslEncryptionPolicy);
                _closed = false;
            }

            public void Dispose()
            {
                if (!_closed)
                {
                    if (_useSsl)
                    {
                        _sslStream.Dispose();
                    }

                    _tcpClient.Dispose();
                    _closed = true;
                }
            }

            public TcpClient TcpClient
            {
                get { return _tcpClient; }
            }

            public byte[] ReceiveBuffer
            {
                get { return _receiveBuffer; }
            }

            public bool UseSsl
            {
                get { return _useSsl; }
            }

            public bool Closed
            {
                get { return _closed; }
            }

            public Stream Stream
            {
                get
                {
                    if (_useSsl)
                    {
                        return _sslStream;
                    }
                    else
                    {
                        return _tcpClient.GetStream();
                    }
                }
            }
        }
    }
}

