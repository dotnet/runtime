// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mail
{
    internal sealed partial class SmtpConnection
    {
        private readonly BufferBuilder _bufferBuilder = new BufferBuilder();
        private bool _isConnected;
        private bool _isClosed;
        private bool _isStreamOpen;
        private readonly EventHandler? _onCloseHandler;
        internal SmtpTransport? _parent;
        private readonly SmtpClient? _client;
        private Stream? _stream;
        internal TcpClient? _tcpClient;
        private SmtpReplyReaderFactory? _responseReader;

        private readonly ICredentialsByHost? _credentials;
        private string[]? _extensions;
        private bool _enableSsl;
        private X509CertificateCollection? _clientCertificates;

        internal SmtpConnection(SmtpTransport parent, SmtpClient client, ICredentialsByHost? credentials, ISmtpAuthenticationModule[] authenticationModules)
        {
            _client = client;
            _credentials = credentials;
            _authenticationModules = authenticationModules;
            _parent = parent;
            _tcpClient = new TcpClient();
            _onCloseHandler = new EventHandler(OnClose);
        }

        internal BufferBuilder BufferBuilder => _bufferBuilder;

        internal bool IsConnected => _isConnected;

        internal bool IsStreamOpen => _isStreamOpen;

        internal SmtpReplyReaderFactory? Reader => _responseReader;

        internal bool EnableSsl
        {
            get
            {
                return _enableSsl;
            }
            set
            {
                _enableSsl = value;
            }
        }

        internal X509CertificateCollection? ClientCertificates
        {
            get
            {
                return _clientCertificates;
            }
            set
            {
                _clientCertificates = value;
            }
        }

        internal void InitializeConnection(string host, int port)
        {
            _tcpClient!.Connect(host, port);
            _stream = _tcpClient.GetStream();
        }

        internal async Task InitializeConnectionAsync(string host, int port)
        {
            await _tcpClient!.ConnectAsync(host, port).ConfigureAwait(false);
            _stream = _tcpClient.GetStream();
        }

        internal async Task GetConnectionAsync<TIOAdapter>(string host, int port, CancellationToken cancellationToken = default)
            where TIOAdapter : IReadWriteAdapter
        {
            if (_isConnected)
            {
                throw new InvalidOperationException(SR.SmtpAlreadyConnected);
            }

            bool isAsync = typeof(TIOAdapter) == typeof(AsyncReadWriteAdapter);

            // Initialize the connection
            if (isAsync)
            {
                await InitializeConnectionAsync(host, port).ConfigureAwait(false);
            }
            else
            {
                InitializeConnection(host, port);
            }

            _responseReader = new SmtpReplyReaderFactory(_stream!);

            // Read the initial greeting
            LineInfo info = await _responseReader.GetNextReplyReader().ReadLineAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);

            switch (info.StatusCode)
            {
                case SmtpStatusCode.ServiceReady:
                    break;
                default:
                    throw new SmtpException(info.StatusCode, info.Line, true);
            }

            // Try EHLO first
            try
            {
                _extensions = await EHelloCommand.SendAsync<TIOAdapter>(this, _client!._clientDomain, cancellationToken).ConfigureAwait(false);
                ParseExtensions(_extensions);
            }
            catch (SmtpException e)
            {
                if ((e.StatusCode != SmtpStatusCode.CommandUnrecognized)
                    && (e.StatusCode != SmtpStatusCode.CommandNotImplemented))
                {
                    throw;
                }

                // Fall back to HELO if EHLO fails
                await HelloCommand.SendAsync<TIOAdapter>(this, _client!._clientDomain, cancellationToken).ConfigureAwait(false);
                // If ehello isn't supported, assume basic login
                _supportedAuth = SupportedAuth.Login;
            }

            // Handle SSL/TLS
            if (_enableSsl)
            {
                if (!_serverSupportsStartTls)
                {
                    // Either TLS is already established or server does not support TLS
                    if (!(_stream is SslStream))
                    {
                        throw new SmtpException(SR.MailServerDoesNotSupportStartTls);
                    }
                }

                await StartTlsCommand.SendAsync<TIOAdapter>(this, cancellationToken).ConfigureAwait(false);

#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
                SslStream sslStream = new SslStream(_stream!, false, ServicePointManager.ServerCertificateValidationCallback);
                if (isAsync)
                {
                    // If we are using async, we need to use the async version of AuthenticateAsClientAsync
                    await sslStream.AuthenticateAsClientAsync(
                        new SslClientAuthenticationOptions
                        {
                            TargetHost = host,
                            ClientCertificates = _clientCertificates,
                            EnabledSslProtocols = (SslProtocols)ServicePointManager.SecurityProtocol, // enums use same values
                            CertificateRevocationCheckMode = ServicePointManager.CheckCertificateRevocationList ?
                                X509RevocationMode.Online : X509RevocationMode.NoCheck,
                        },
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Synchronous version
                    sslStream.AuthenticateAsClient(host, _clientCertificates, (SslProtocols)ServicePointManager.SecurityProtocol, ServicePointManager.CheckCertificateRevocationList);
                }
#pragma warning restore SYSLIB0014 // ServicePointManager is obsolete

                _stream = sslStream;
                _responseReader = new SmtpReplyReaderFactory(_stream);

                // According to RFC 3207: The client SHOULD send an EHLO command
                // as the first command after a successful TLS negotiation.
                _extensions = await EHelloCommand.SendAsync<TIOAdapter>(this, _client!._clientDomain, cancellationToken).ConfigureAwait(false);
                ParseExtensions(_extensions);
            }

            // If credentials were supplied, attempt authentication
            if (_credentials != null)
            {
                for (int i = 0; i < _authenticationModules.Length; i++)
                {
                    // Only authenticate if the auth protocol is supported
                    if (!AuthSupported(_authenticationModules[i]))
                    {
                        continue;
                    }

                    NetworkCredential? credential = _credentials.GetCredential(host, port, _authenticationModules[i].AuthenticationType);
                    if (credential == null)
                        continue;

                    Authorization? auth = SetContextAndTryAuthenticate(_authenticationModules[i], credential);

                    if (auth != null && auth.Message != null)
                    {
                        info = await AuthCommand.SendAsync<TIOAdapter>(this, _authenticationModules[i].AuthenticationType, auth.Message, cancellationToken).ConfigureAwait(false);

                        if (info.StatusCode == SmtpStatusCode.CommandParameterNotImplemented)
                        {
                            continue;
                        }

                        while ((int)info.StatusCode == 334)
                        {
                            auth = _authenticationModules[i].Authenticate(info.Line, null, this, _client!.TargetName, null);
                            if (auth == null)
                            {
                                throw new SmtpException(SR.SmtpAuthenticationFailed);
                            }
                            info = await AuthCommand.SendAsync<TIOAdapter>(this, auth.Message, cancellationToken).ConfigureAwait(false);

                            if ((int)info.StatusCode == 235)
                            {
                                _authenticationModules[i].CloseContext(this);
                                _isConnected = true;
                                return;
                            }
                        }
                    }
                }
            }

            _isConnected = true;
        }

        internal async Task FlushAsync<TIOAdapter>(CancellationToken cancellationToken = default) where TIOAdapter : IReadWriteAdapter
        {
            await TIOAdapter.WriteAsync(_stream!, _bufferBuilder.GetBuffer().AsMemory(0, _bufferBuilder.Length), cancellationToken).ConfigureAwait(false);
            _bufferBuilder.Reset();
        }

        private void ShutdownConnection(bool isAbort)
        {
            if (!_isClosed)
            {
                lock (this)
                {
                    if (!_isClosed && _tcpClient != null)
                    {
                        try
                        {
                            try
                            {
                                if (isAbort)
                                {
                                    // Must destroy manually since sending a QUIT here might not be
                                    // interpreted correctly by the server if it's in the middle of a
                                    // DATA command or some similar situation.  This may send a RST
                                    // but this is ok in this situation.  Do not reuse this connection
                                    _tcpClient.LingerState = new LingerOption(true, 0);
                                }
                                else
                                {
                                    // Gracefully close the transmission channel
                                    _tcpClient.Client.Blocking = false;
                                    QuitCommand.SendAsync<SyncReadWriteAdapter>(this).GetAwaiter().GetResult();
                                }
                            }
                            finally
                            {
                                //free cbt buffer
                                _stream?.Close();
                                _tcpClient.Dispose();
                            }
                        }
                        catch (IOException)
                        {
                            // Network failure
                        }
                        catch (ObjectDisposedException)
                        {
                            // See https://github.com/dotnet/runtime/issues/30732, and potentially
                            // catch additional exception types here if need demonstrates.
                        }
                    }

                    _isClosed = true;
                }
            }

            _isConnected = false;
        }

        internal void ReleaseConnection()
        {
            ShutdownConnection(false);
        }

        internal void Abort()
        {
            ShutdownConnection(true);
        }

        private Authorization? SetContextAndTryAuthenticate(ISmtpAuthenticationModule module, NetworkCredential? credential)
        {
            return module.Authenticate(null, credential, this, _client!.TargetName, null);
        }

        internal Stream GetClosableStream()
        {
            ClosableStream cs = new ClosableStream(_stream!, _onCloseHandler);
            _isStreamOpen = true;
            return cs;
        }

        private void OnClose(object? sender, EventArgs args)
        {
            _isStreamOpen = false;

            DataStopCommand.SendAsync<SyncReadWriteAdapter>(this).GetAwaiter().GetResult();
        }
    }
}
