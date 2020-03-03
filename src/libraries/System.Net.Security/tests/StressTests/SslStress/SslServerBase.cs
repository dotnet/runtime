// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace SslStress
{
    public abstract class SslServerBase : IAsyncDisposable
    {
        public const string Hostname = "contoso.com";

        protected readonly Configuration _config;

        private readonly X509Certificate2 _certificate;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly SemaphoreSlim _acceptConnectionLatch = new SemaphoreSlim(initialCount: 1);
        private readonly Lazy<Task> _serverTask;

        public EndPoint ServerEndpoint => _listener.LocalEndpoint;

        public SslServerBase(Configuration config)
        {
            if (config.MaxConnections < 1) throw new ArgumentOutOfRangeException(nameof(config.MaxConnections));

            _config = config;
            _certificate = CreateSelfSignedCertificate();
            _listener = new TcpListener(config.ServerEndpoint);
            _serverTask = new Lazy<Task>(Task.Run(StartCore));
        }

        protected abstract Task HandleConnection(SslStream sslStream, TcpClient client, CancellationToken token);

        protected virtual async Task<SslStream> EstablishSslStream(Stream networkStream, CancellationToken token)
        {
            var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
            var serverOptions = new SslServerAuthenticationOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 },
                ServerCertificate = _certificate,
            };

            await sslStream.AuthenticateAsServerAsync(serverOptions, token);
            return sslStream;
        }

        public void Start()
        {
            if (_cts.IsCancellationRequested) throw new ObjectDisposedException(nameof(SslServerBase));
            _ = _serverTask.Value;
        }

        public async ValueTask StopAsync()
        {
            _cts.Cancel();
            await _serverTask.Value;
        }

        public Task Task
        {
            get
            {
                if (!_serverTask.IsValueCreated) throw new InvalidOperationException("Server has not been started yet");
                return _serverTask.Value;
            }
        }

        public ValueTask DisposeAsync() => StopAsync();

        private async Task StartCore()
        {
            _listener.Start();
            IEnumerable<Task> workers = Enumerable.Range(1, 2 * _config.MaxConnections).Select(_ => RunSingleWorker());
            try
            {
                await Task.WhenAll(workers);
            }
            finally
            {
                _listener.Stop();
            }

            async Task RunSingleWorker()
            {
                while(!_cts.IsCancellationRequested)
                {
                    try
                    {
                        using TcpClient client = await AcceptTcpClientAsync(_cts.Token);
                        using SslStream stream = await EstablishSslStream(client.GetStream(), _cts.Token);
                        await HandleConnection(stream, client, _cts.Token);
                    }
                    catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                    {

                    }
                    catch (Exception e)
                    {
                        if (_config.LogServer)
                        {
                            lock (Console.Out)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine($"Server: unhandled exception: {e}");
                                Console.WriteLine();
                                Console.ResetColor();
                            }
                        }
                    }
                }
            }

            // workaround for TcpListener not accepting cancellation tokens
            async Task<TcpClient> AcceptTcpClientAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    if (_listener.Pending())
                    {
                        // Need to ensure AcceptTcpClientAsync() returns immediately,
                        // so we synchronize here to avoid races between workers
                        await _acceptConnectionLatch.WaitAsync(token);
                        try
                        {
                            if (_listener.Pending())
                                return await _listener.AcceptTcpClientAsync();
                        }
                        finally
                        {
                            _acceptConnectionLatch.Release();
                        }
                    }

                    await Task.Delay(20);
                }

                token.ThrowIfCancellationRequested();
                throw new Exception("internal error");
            }
        }

        protected virtual X509Certificate2 CreateSelfSignedCertificate()
        {
            using var rsa = RSA.Create();
            var certReq = new CertificateRequest($"CN={Hostname}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            certReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            certReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
            X509Certificate2 cert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cert = new X509Certificate2(cert.Export(X509ContentType.Pfx));
            }

            return cert;
        }
    }
}
