// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Security.Tests;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public class HttpsTestClient
    {
        public class Options
        {
            public Options(EndPoint remoteEndPoint)
            {
                RemoteEndpoint = remoteEndPoint;
            }

            public const string DefaultRequestStringTemplate =
@"GET / HTTP/1.0
Host: {0}
User-Agent: Testing application

";

            public string ServerName { get; set; }

            public EndPoint RemoteEndpoint { get; }

            public X509Certificate2 ClientCertificate { get; set; } =
                Configuration.Certificates.GetClientCertificate();

            public SslProtocols AllowedProtocols { get; set; } = SslProtocols.None;

            public SslPolicyErrors IgnoreSslPolicyErrors { get; set; } = SslPolicyErrors.None;
        }

        private Options _options;
        private int _requestCount = 0;
        private VerboseTestLogging _log = VerboseTestLogging.GetInstance();

        public SslStream Stream { get; private set; }

        public HttpsTestClient(Options options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task HttpsRequestAsync(Func<string, Task<string>> httpConversation = null)
        {
            _log.WriteLine("[Client] Disabling SslPolicyErrors: {0}", _options.IgnoreSslPolicyErrors.ToString());

            if (httpConversation == null)
            {
                httpConversation = DefaultHttpConversation;
            }

            using (var certValidationPolicy = new SslStreamCertificatePolicy(_options.IgnoreSslPolicyErrors))
            using (var tcp = new TcpClient())
            {
                await ConnectToHostAsync(tcp);

                using (Stream = new SslStream(tcp.GetStream(), false, certValidationPolicy.SslStreamCallback))
                {
                    X509CertificateCollection clientCerts = null;

                    if (_options.ClientCertificate != null)
                    {
                        clientCerts = new X509CertificateCollection();
                        clientCerts.Add(_options.ClientCertificate);
                    }

                    _log.WriteLine(
                        "[Client] Connected. Authenticating: server={0}; clientCert={1}",
                        _options.ServerName,
                        _options.ClientCertificate != null ? _options.ClientCertificate.Subject : "<null>");

                    try
                    {
                        await Stream.AuthenticateAsClientAsync(
                            _options.ServerName,
                            clientCerts,
                            _options.AllowedProtocols,
                            checkCertificateRevocation: false).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _log.WriteLine("[Client] Exception : {0}", ex);
                        throw ex;
                    }

                    _log.WriteLine("[Client] Authenticated protocol {0}", Stream.SslProtocol);

                    int bytesRead = 0;
                    string responseString = null;

                    while (true)
                    {
                        string requestString = await httpConversation(responseString).ConfigureAwait(false);
                        if (requestString == null)
                        {
                            return;
                        }

                        if (requestString.Length > 0)
                        {
                            byte[] requestBuffer = Encoding.UTF8.GetBytes(requestString);

                            _log.WriteLine("[Client] Sending request ({0} Bytes)", requestBuffer.Length);
                            await Stream.WriteAsync(requestBuffer, 0, requestBuffer.Length).ConfigureAwait(false);
                        }

                        _log.WriteLine("[Client] Waiting for reply...");

                        byte[] responseBuffer = new byte[2048];
                        bytesRead = await Stream.ReadAsync(responseBuffer, 0, responseBuffer.Length).ConfigureAwait(false);
                        responseString = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                        _log.WriteLine("[Client] {0} Bytes, Response: <{1}>", bytesRead, responseString);
                    }
                }
            }
        }

        private async Task ConnectToHostAsync(TcpClient tcp)
        {
            string hostName = null;

            if (_options.RemoteEndpoint is DnsEndPoint)
            {
                var dns = (DnsEndPoint)_options.RemoteEndpoint;
                hostName = dns.Host;

                _log.WriteLine("[Client] Connecting to {0}:{1}", hostName, dns.Port);
                await tcp.ConnectAsync(hostName, dns.Port).ConfigureAwait(false);
            }
            else
            {
                var ip = (IPEndPoint)_options.RemoteEndpoint;
                hostName = ip.Address.ToString();
                _log.WriteLine("[Client] Connecting to {0}:{1}", hostName, ip.Port);
                await tcp.ConnectAsync(hostName, ip.Port).ConfigureAwait(false);
            }

            if (_options.ServerName == null)
            {
                _options.ServerName = hostName;
            }
        }

        private Task<string> DefaultHttpConversation(string read)
        {
            if (_requestCount == 1 && read.Length == 0)
            {
                throw new InvalidOperationException("Https empty response.");
            }

            string requestString = null;
            if (read == null)
            {
                requestString = string.Format(Options.DefaultRequestStringTemplate, _options.ServerName);
            }

            _requestCount++;
            return Task.FromResult(requestString);
        }
    }
}
