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

namespace System.Net.Mail
{
    internal sealed partial class SmtpConnection
    {
        private static readonly ContextCallback s_AuthenticateCallback = new ContextCallback(AuthenticateCallback);

        private readonly BufferBuilder _bufferBuilder = new BufferBuilder();
        private bool _isConnected;
        private bool _isClosed;
        private bool _isStreamOpen;
        private readonly EventHandler? _onCloseHandler;
        internal SmtpTransport? _parent;
        private readonly SmtpClient? _client;
        private NetworkStream? _networkStream;
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
            _networkStream = _tcpClient.GetStream();
        }

        internal IAsyncResult BeginInitializeConnection(string host, int port, AsyncCallback? callback, object? state)
        {
            return _tcpClient!.BeginConnect(host, port, callback, state);
        }

        internal void EndInitializeConnection(IAsyncResult result)
        {
            _tcpClient!.EndConnect(result);
            _networkStream = _tcpClient.GetStream();
        }

        internal IAsyncResult BeginGetConnection(ContextAwareResult outerResult, AsyncCallback? callback, object? state, string host, int port)
        {
            ConnectAndHandshakeAsyncResult result = new ConnectAndHandshakeAsyncResult(this, host, port, outerResult, callback, state);
            result.GetConnection();
            return result;
        }

        internal IAsyncResult BeginFlush(AsyncCallback? callback, object? state)
        {
            return _networkStream!.BeginWrite(_bufferBuilder.GetBuffer(), 0, _bufferBuilder.Length, callback, state);
        }

        internal void EndFlush(IAsyncResult result)
        {
            _networkStream!.EndWrite(result);
            _bufferBuilder.Reset();
        }

        internal void Flush()
        {
            _networkStream!.Write(_bufferBuilder.GetBuffer(), 0, _bufferBuilder.Length);
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
                                    QuitCommand.Send(this);
                                }
                            }
                            finally
                            {
                                //free cbt buffer
                                _networkStream?.Close();
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

        internal void GetConnection(string host, int port)
        {
            if (_isConnected)
            {
                throw new InvalidOperationException(SR.SmtpAlreadyConnected);
            }

            InitializeConnection(host, port);
            _responseReader = new SmtpReplyReaderFactory(_networkStream!);

            LineInfo info = _responseReader.GetNextReplyReader().ReadLine();

            switch (info.StatusCode)
            {
                case SmtpStatusCode.ServiceReady:
                    break;
                default:
                    throw new SmtpException(info.StatusCode, info.Line, true);
            }

            try
            {
                _extensions = EHelloCommand.Send(this, _client!._clientDomain);
                ParseExtensions(_extensions);
            }
            catch (SmtpException e)
            {
                if ((e.StatusCode != SmtpStatusCode.CommandUnrecognized)
                    && (e.StatusCode != SmtpStatusCode.CommandNotImplemented))
                {
                    throw;
                }

                HelloCommand.Send(this, _client!._clientDomain);
                //if ehello isn't supported, assume basic login
                _supportedAuth = SupportedAuth.Login;
            }

            if (_enableSsl)
            {
                if (!_serverSupportsStartTls)
                {
                    // Either TLS is already established or server does not support TLS
                    if (!(_networkStream is TlsStream))
                    {
                        throw new SmtpException(SR.MailServerDoesNotSupportStartTls);
                    }
                }

                StartTlsCommand.Send(this);
                TlsStream tlsStream = new TlsStream(_networkStream!, _tcpClient!.Client, host, _clientCertificates);
                tlsStream.AuthenticateAsClient();
                _networkStream = tlsStream;
                _responseReader = new SmtpReplyReaderFactory(_networkStream);

                // According to RFC 3207: The client SHOULD send an EHLO command
                // as the first command after a successful TLS negotiation.
                _extensions = EHelloCommand.Send(this, _client._clientDomain);
                ParseExtensions(_extensions);
            }

            // if no credentials were supplied, try anonymous
            // servers don't appear to anounce that they support anonymous login.
            if (_credentials != null)
            {
                for (int i = 0; i < _authenticationModules.Length; i++)
                {
                    //only authenticate if the auth protocol is supported  - chadmu
                    if (!AuthSupported(_authenticationModules[i]))
                    {
                        continue;
                    }

                    NetworkCredential? credential = _credentials.GetCredential(host, port, _authenticationModules[i].AuthenticationType);
                    if (credential == null)
                        continue;

                    Authorization? auth = SetContextAndTryAuthenticate(_authenticationModules[i], credential, null);

                    if (auth != null && auth.Message != null)
                    {
                        info = AuthCommand.Send(this, _authenticationModules[i].AuthenticationType, auth.Message);

                        if (info.StatusCode == SmtpStatusCode.CommandParameterNotImplemented)
                        {
                            continue;
                        }

                        while ((int)info.StatusCode == 334)
                        {
                            auth = _authenticationModules[i].Authenticate(info.Line, null, this, _client.TargetName, null);
                            if (auth == null)
                            {
                                throw new SmtpException(SR.SmtpAuthenticationFailed);
                            }
                            info = AuthCommand.Send(this, auth.Message);

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

        private Authorization? SetContextAndTryAuthenticate(ISmtpAuthenticationModule module, NetworkCredential? credential, ContextAwareResult? context)
        {
            // We may need to restore user thread token here
            if (ReferenceEquals(credential, CredentialCache.DefaultNetworkCredentials))
            {
#if DEBUG
                Debug.Assert(context == null || context.IdentityRequested, "Authentication required when it wasn't expected.  (Maybe Credentials was changed on another thread?)");
#endif
                try
                {
                    ExecutionContext? x = context == null ? null : context.ContextCopy;
                    if (x != null)
                    {
                        AuthenticateCallbackContext authenticationContext =
                            new AuthenticateCallbackContext(this, module, credential, _client!.TargetName, null);

                        ExecutionContext.Run(x, s_AuthenticateCallback, authenticationContext);
                        return authenticationContext._result;
                    }
                    else
                    {
                        return module.Authenticate(null, credential, this, _client!.TargetName, null);
                    }
                }
                catch
                {
                    // Prevent the impersonation from leaking to upstack exception filters.
                    throw;
                }
            }

            return module.Authenticate(null, credential, this, _client!.TargetName, null);
        }

        private static void AuthenticateCallback(object? state)
        {
            AuthenticateCallbackContext context = (AuthenticateCallbackContext)state!;
            context._result = context._module.Authenticate(null, context._credential, context._thisPtr, context._spn, context._token);
        }

        private sealed class AuthenticateCallbackContext
        {
            internal AuthenticateCallbackContext(SmtpConnection thisPtr, ISmtpAuthenticationModule module, NetworkCredential credential, string? spn, ChannelBinding? Token)
            {
                _thisPtr = thisPtr;
                _module = module;
                _credential = credential;
                _spn = spn;
                _token = Token;

                _result = null;
            }

            internal readonly SmtpConnection _thisPtr;
            internal readonly ISmtpAuthenticationModule _module;
            internal readonly NetworkCredential _credential;
            internal readonly string? _spn;
            internal readonly ChannelBinding? _token;

            internal Authorization? _result;
        }

        internal void EndGetConnection(IAsyncResult result)
        {
            ConnectAndHandshakeAsyncResult.End(result);
        }

        internal Stream GetClosableStream()
        {
            ClosableStream cs = new ClosableStream(_networkStream!, _onCloseHandler);
            _isStreamOpen = true;
            return cs;
        }

        private void OnClose(object? sender, EventArgs args)
        {
            _isStreamOpen = false;

            DataStopCommand.Send(this);
        }

        private sealed class ConnectAndHandshakeAsyncResult : LazyAsyncResult
        {
            private string? _authResponse;
            private readonly SmtpConnection _connection;
            private int _currentModule = -1;
            private readonly int _port;
            private static readonly AsyncCallback s_handshakeCallback = new AsyncCallback(HandshakeCallback);
            private static readonly AsyncCallback s_sendEHelloCallback = new AsyncCallback(SendEHelloCallback);
            private static readonly AsyncCallback s_sendHelloCallback = new AsyncCallback(SendHelloCallback);
            private static readonly AsyncCallback s_authenticateCallback = new AsyncCallback(AuthenticateCallback);
            private static readonly AsyncCallback s_authenticateContinueCallback = new AsyncCallback(AuthenticateContinueCallback);
            private readonly string _host;

            private readonly ContextAwareResult _outerResult;


            internal ConnectAndHandshakeAsyncResult(SmtpConnection connection, string host, int port, ContextAwareResult outerResult, AsyncCallback? callback, object? state) :
                base(null, state, callback)
            {
                _connection = connection;
                _host = host;
                _port = port;

                _outerResult = outerResult;
            }

            internal static void End(IAsyncResult result)
            {
                ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result;
                object? connectResult = thisPtr.InternalWaitForCompletion();
                if (connectResult is Exception e)
                {
                    ExceptionDispatchInfo.Throw(e);
                }
            }

            internal void GetConnection()
            {
                if (_connection._isConnected)
                {
                    throw new InvalidOperationException(SR.SmtpAlreadyConnected);
                }

                InitializeConnection();
            }

            private void InitializeConnection()
            {
                IAsyncResult result = _connection.BeginInitializeConnection(_host, _port, InitializeConnectionCallback, this);
                if (result.CompletedSynchronously)
                {
                    try
                    {
                        _connection.EndInitializeConnection(result);
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Connect returned");

                        Handshake();
                    }
                    catch (Exception e)
                    {
                        InvokeCallback(e);
                    }
                }
            }

            private static void InitializeConnectionCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        thisPtr._connection.EndInitializeConnection(result);
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Connect returned {thisPtr}");

                        thisPtr.Handshake();
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            private void Handshake()
            {
                _connection._responseReader = new SmtpReplyReaderFactory(_connection._networkStream!);

                SmtpReplyReader reader = _connection.Reader!.GetNextReplyReader();
                IAsyncResult result = reader.BeginReadLine(s_handshakeCallback, this);
                if (!result.CompletedSynchronously)
                {
                    return;
                }

                LineInfo info = reader.EndReadLine(result);

                if (info.StatusCode != SmtpStatusCode.ServiceReady)
                {
                    throw new SmtpException(info.StatusCode, info.Line, true);
                }
                try
                {
                    if (!SendEHello())
                    {
                        return;
                    }
                }
                catch
                {
                    if (!SendHello())
                    {
                        return;
                    }
                }
            }

            private static void HandshakeCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        try
                        {
                            LineInfo info = thisPtr._connection.Reader!.CurrentReader!.EndReadLine(result);
                            if (info.StatusCode != SmtpStatusCode.ServiceReady)
                            {
                                thisPtr.InvokeCallback(new SmtpException(info.StatusCode, info.Line, true));
                                return;
                            }
                            if (!thisPtr.SendEHello())
                            {
                                return;
                            }
                        }
                        catch (SmtpException)
                        {
                            if (!thisPtr.SendHello())
                            {
                                return;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            private bool SendEHello()
            {
                IAsyncResult result = EHelloCommand.BeginSend(_connection, _connection._client!._clientDomain, s_sendEHelloCallback, this);
                if (result.CompletedSynchronously)
                {
                    _connection._extensions = EHelloCommand.EndSend(result);
                    _connection.ParseExtensions(_connection._extensions);
                    // If we already have a TlsStream, this is the second EHLO cmd
                    // that we sent after TLS handshake compelted. So skip TLS and
                    // continue with Authenticate.
                    if (_connection._networkStream is TlsStream)
                    {
                        Authenticate();
                        return true;
                    }

                    if (_connection.EnableSsl)
                    {
                        if (!_connection._serverSupportsStartTls)
                        {
                            // Either TLS is already established or server does not support TLS
                            if (!(_connection._networkStream is TlsStream))
                            {
                                throw new SmtpException(SR.MailServerDoesNotSupportStartTls);
                            }
                        }

                        SendStartTls();
                    }
                    else
                    {
                        Authenticate();
                    }
                    return true;
                }
                return false;
            }

            private static void SendEHelloCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        try
                        {
                            thisPtr._connection._extensions = EHelloCommand.EndSend(result);
                            thisPtr._connection.ParseExtensions(thisPtr._connection._extensions);

                            // If we already have a SSlStream, this is the second EHLO cmd
                            // that we sent after TLS handshake compelted. So skip TLS and
                            // continue with Authenticate.
                            if (thisPtr._connection._networkStream is TlsStream)
                            {
                                thisPtr.Authenticate();
                                return;
                            }
                        }

                        catch (SmtpException e)
                        {
                            if ((e.StatusCode != SmtpStatusCode.CommandUnrecognized)
                                && (e.StatusCode != SmtpStatusCode.CommandNotImplemented))
                            {
                                throw;
                            }

                            if (!thisPtr.SendHello())
                            {
                                return;
                            }
                        }


                        if (thisPtr._connection.EnableSsl)
                        {
                            if (!thisPtr._connection._serverSupportsStartTls)
                            {
                                // Either TLS is already established or server does not support TLS
                                if (!(thisPtr._connection._networkStream is TlsStream))
                                {
                                    throw new SmtpException(SR.MailServerDoesNotSupportStartTls);
                                }
                            }

                            thisPtr.SendStartTls();
                        }
                        else
                        {
                            thisPtr.Authenticate();
                        }
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            private bool SendHello()
            {
                IAsyncResult result = HelloCommand.BeginSend(_connection, _connection._client!._clientDomain, s_sendHelloCallback, this);
                //if ehello isn't supported, assume basic auth
                if (result.CompletedSynchronously)
                {
                    _connection._supportedAuth = SupportedAuth.Login;
                    HelloCommand.EndSend(result);
                    Authenticate();
                    return true;
                }
                return false;
            }

            private static void SendHelloCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        HelloCommand.EndSend(result);
                        thisPtr.Authenticate();
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            private bool SendStartTls()
            {
                IAsyncResult result = StartTlsCommand.BeginSend(_connection, SendStartTlsCallback, this);
                if (result.CompletedSynchronously)
                {
                    StartTlsCommand.EndSend(result);
                    TlsStreamAuthenticate();
                    return true;
                }
                return false;
            }

            private static void SendStartTlsCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        StartTlsCommand.EndSend(result);
                        thisPtr.TlsStreamAuthenticate();
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            private bool TlsStreamAuthenticate()
            {
                _connection._networkStream = new TlsStream(_connection._networkStream!, _connection._tcpClient!.Client, _host, _connection._clientCertificates);
                IAsyncResult result = ((TlsStream)_connection._networkStream).BeginAuthenticateAsClient(TlsStreamAuthenticateCallback, this);
                if (result.CompletedSynchronously)
                {
                    ((TlsStream)_connection._networkStream).EndAuthenticateAsClient(result);
                    _connection._responseReader = new SmtpReplyReaderFactory(_connection._networkStream);
                    SendEHello();
                    return true;
                }
                return false;
            }

            private static void TlsStreamAuthenticateCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        (thisPtr._connection._networkStream as TlsStream)!.EndAuthenticateAsClient(result);
                        thisPtr._connection._responseReader = new SmtpReplyReaderFactory(thisPtr._connection._networkStream);
                        thisPtr.SendEHello();
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            private void Authenticate()
            {
                //if no credentials were supplied, try anonymous
                //servers don't appear to anounce that they support anonymous login.
                if (_connection._credentials != null)
                {
                    while (++_currentModule < _connection._authenticationModules.Length)
                    {
                        //only authenticate if the auth protocol is supported
                        ISmtpAuthenticationModule module = _connection._authenticationModules[_currentModule];
                        if (!_connection.AuthSupported(module))
                        {
                            continue;
                        }

                        NetworkCredential? credential = _connection._credentials.GetCredential(_host, _port, module.AuthenticationType);
                        if (credential == null)
                            continue;
                        Authorization? auth = _connection.SetContextAndTryAuthenticate(module, credential, _outerResult);

                        if (auth != null && auth.Message != null)
                        {
                            IAsyncResult result = AuthCommand.BeginSend(_connection, _connection._authenticationModules[_currentModule].AuthenticationType, auth.Message, s_authenticateCallback, this);
                            if (!result.CompletedSynchronously)
                            {
                                return;
                            }

                            LineInfo info = AuthCommand.EndSend(result);

                            if ((int)info.StatusCode == 334)
                            {
                                _authResponse = info.Line;
                                if (!AuthenticateContinue())
                                {
                                    return;
                                }
                            }
                            else if ((int)info.StatusCode == 235)
                            {
                                module.CloseContext(_connection);
                                _connection._isConnected = true;
                                break;
                            }
                        }
                    }
                }

                _connection._isConnected = true;
                InvokeCallback();
            }

            private static void AuthenticateCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        LineInfo info = AuthCommand.EndSend(result);

                        if ((int)info.StatusCode == 334)
                        {
                            thisPtr._authResponse = info.Line;
                            if (!thisPtr.AuthenticateContinue())
                            {
                                return;
                            }
                        }
                        else if ((int)info.StatusCode == 235)
                        {
                            thisPtr._connection._authenticationModules[thisPtr._currentModule].CloseContext(thisPtr._connection);
                            thisPtr._connection._isConnected = true;
                            thisPtr.InvokeCallback();
                            return;
                        }

                        thisPtr.Authenticate();
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            private bool AuthenticateContinue()
            {
                while (true)
                {
                    // We don't need credential on the continued auth assuming they were captured on the first call.
                    // That should always work, otherwise what if a new credential has been returned?
                    Authorization? auth = _connection._authenticationModules[_currentModule].Authenticate(_authResponse, null, _connection, _connection._client!.TargetName, null);
                    if (auth == null)
                    {
                        throw new SmtpException(SR.SmtpAuthenticationFailed);
                    }

                    IAsyncResult result = AuthCommand.BeginSend(_connection, auth.Message, s_authenticateContinueCallback, this);
                    if (!result.CompletedSynchronously)
                    {
                        return false;
                    }

                    LineInfo info = AuthCommand.EndSend(result);
                    if ((int)info.StatusCode == 235)
                    {
                        _connection._authenticationModules[_currentModule].CloseContext(_connection);
                        _connection._isConnected = true;
                        InvokeCallback();
                        return false;
                    }
                    else if ((int)info.StatusCode != 334)
                    {
                        return true;
                    }
                    _authResponse = info.Line;
                }
            }

            private static void AuthenticateContinueCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ConnectAndHandshakeAsyncResult thisPtr = (ConnectAndHandshakeAsyncResult)result.AsyncState!;
                    try
                    {
                        LineInfo info = AuthCommand.EndSend(result);
                        if ((int)info.StatusCode == 235)
                        {
                            thisPtr._connection._authenticationModules[thisPtr._currentModule].CloseContext(thisPtr._connection);
                            thisPtr._connection._isConnected = true;
                            thisPtr.InvokeCallback();
                            return;
                        }
                        else if ((int)info.StatusCode == 334)
                        {
                            thisPtr._authResponse = info.Line;
                            if (!thisPtr.AuthenticateContinue())
                            {
                                return;
                            }
                        }
                        thisPtr.Authenticate();
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }
        }
    }
}
