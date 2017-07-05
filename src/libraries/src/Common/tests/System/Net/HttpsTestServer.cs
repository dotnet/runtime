// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public class HttpsTestServer : IDisposable
    {
        public class Options
        {
            public const string DefaultResponseString =
                "HTTP/1.1 200 OK\r\n" +
                "Connection: close\r\n" +
                "\r\n" +
                "<html><head><title>Test Server</title></head><body><h1>TLS test server</h1></body></html>\r\n";

            public IPAddress Address { get; set; } = IPAddress.Loopback;

            public X509Certificate2 ServerCertificate { get; set; } = 
                Configuration.Certificates.GetServerCertificate();

            public SslProtocols AllowedProtocols { get; set; } = SslProtocols.None;

            public bool RequireClientAuthentication { get; set; } = false;

            public SslPolicyErrors IgnoreSslPolicyErrors { get; set; } = SslPolicyErrors.None;

            public int ListenBacklog { get; set; } = 1;
        }

        private Options _options;
        private int _port;
        private TcpListener _listener;
        private VerboseTestLogging _log = VerboseTestLogging.GetInstance();
        
        public HttpsTestServer(Options options = null)
        {
            if (options != null)
            {
                _options = options;
            }
            else
            {
                _options = new Options();
            }
        }

        public SslStream Stream { get; private set; }
        
        public int Port
        {
            get
            {
                if (_port == 0)
                {
                    throw new InvalidOperationException("Server is not yet bound to a port.");
                }

                return _port;
            }
        }

        public void Start()
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("Cannot restart server.");
            }

            _listener = new TcpListener(_options.Address, _port);

            _listener.Start(_options.ListenBacklog);
            var ipEndpoint = (IPEndPoint)_listener.LocalEndpoint;
            _port = ipEndpoint.Port;
            _log.WriteLine("[Server] waiting for connections ({0}:{1})", ipEndpoint.Address, ipEndpoint.Port);
        }

        public async Task AcceptHttpsClientAsync(Func<string, Task<string>> httpConversation = null)
        {
            bool done = false;

            while (!done)
            {
                try
                {
                    using (Socket accepted = await _listener.AcceptSocketAsync().ConfigureAwait(false))
                    {
                        _log.WriteLine("[Server] Client connected.");

                        using (NetworkStream ns = new NetworkStream(accepted, ownsSocket: false))
                        using (Stream = new SslStream(ns, false, RemoteCertificateCallback))
                        {
                            _log.WriteLine(
                                "[Server] Authenticating. Protocols = {0}, Certificate = {1}, ClientCertRequired = {2}",
                                _options.AllowedProtocols,
                                _options.ServerCertificate.Subject,
                                _options.RequireClientAuthentication);

                            await Stream.AuthenticateAsServerAsync(
                                _options.ServerCertificate,
                                _options.RequireClientAuthentication,
                                _options.AllowedProtocols,
                                false).ConfigureAwait(false);

                            _log.WriteLine("[Server] Client authenticated: Protocol: {0}", Stream.SslProtocol);

                            _log.WriteLine("[Server] Starting HTTP conversation.");

                            if (httpConversation == null)
                            {
                                httpConversation = DefaultHttpConversation;
                            }

                            done = await ProcessHttp(httpConversation).ConfigureAwait(false);
                        }

                        accepted.Shutdown(SocketShutdown.Send);
                    }
                }
                catch (IOException ex)
                {
                    // Ignore I/O issues as browsers attempt to connect only to detect crypto information.
                    _log.WriteLine("[Server] Exception: {0}", ex.Message);
                }
            }
        }

        private bool RemoteCertificateCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (!_options.RequireClientAuthentication)
            {
                return true;
            }

            _log.WriteLine("[Server] RemoteCertificateCallback (SslPolicyErrors = {0})", sslPolicyErrors.ToString());
            if ((sslPolicyErrors | _options.IgnoreSslPolicyErrors) == _options.IgnoreSslPolicyErrors)
            {
                return true;
            }

            return false;
        }

        private async Task<bool> ProcessHttp(Func<string, Task<string>> httpConversation)
        {
            while (true)
            {
                var requestBuffer = new byte[2048];
                int bytesRead = await Stream.ReadAsync(requestBuffer, 0, requestBuffer.Length).ConfigureAwait(false);

                string requestString = Encoding.UTF8.GetString(requestBuffer, 0, bytesRead);

                _log.WriteLine("[Server] Received {0} bytes: <{1}>", bytesRead, requestString);

                if (bytesRead == 0)
                {
                    _log.WriteLine("[Server] Received EOF");
                    return false;
                }

                string responseString = await httpConversation(requestString).ConfigureAwait(false);

                if (responseString != null)
                {
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(responseString);

                    await Stream.WriteAsync(responseBuffer, 0, responseBuffer.Length).ConfigureAwait(false);
                    _log.WriteLine("[Server] Replied with {0} bytes.", responseBuffer.Length);
                    return true;
                }
            }
        }

        private Task<string> DefaultHttpConversation(string read)
        {
            return Task.FromResult(Options.DefaultResponseString);
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}
