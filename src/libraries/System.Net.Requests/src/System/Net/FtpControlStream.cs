// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal enum FtpLoginState : byte
    {
        NotLoggedIn,
        LoggedIn,
        LoggedInButNeedsRelogin,
        ReloginFailed
    };

    /// <summary>
    /// <para>
    ///     The FtpControlStream class implements a basic FTP connection,
    ///     This means basic command sending and parsing.
    /// </para>
    /// </summary>
    internal sealed class FtpControlStream : CommandStream
    {
        private Socket? _dataSocket;
        private IPEndPoint? _passiveEndPoint;
        private TlsStream? _tlsStream;

        private StringBuilder? _bannerMessage;
        private StringBuilder? _welcomeMessage;
        private StringBuilder? _exitMessage;
        private WeakReference? _credentials;
        private string _currentTypeSetting = string.Empty;

        private long _contentLength = -1;
        private DateTime _lastModified;
        private bool _dataHandshakeStarted;
        private string? _loginDirectory;
        private string? _establishedServerDirectory;
        private string? _requestedServerDirectory;
        private Uri? _responseUri;

        private FtpLoginState _loginState = FtpLoginState.NotLoggedIn;

        internal FtpStatusCode StatusCode;
        internal string? StatusLine;

        internal NetworkCredential? Credentials
        {
            get
            {
                if (_credentials != null && _credentials.IsAlive)
                {
                    return (NetworkCredential?)_credentials.Target;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_credentials == null)
                {
                    _credentials = new WeakReference(null);
                }
                _credentials.Target = value;
            }
        }

        private static readonly AsyncCallback s_acceptCallbackDelegate = new AsyncCallback(AcceptCallback);
        private static readonly AsyncCallback s_connectCallbackDelegate = new AsyncCallback(ConnectCallback);
        private static readonly AsyncCallback s_SSLHandshakeCallback = new AsyncCallback(SSLHandshakeCallback);

        internal FtpControlStream(NetworkStream client)
            : base(client)
        {
        }

        /// <summary>
        ///    <para>Closes the connecting socket to generate an error.</para>
        /// </summary>
        internal void AbortConnect()
        {
            Socket? socket = _dataSocket;
            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        /// <summary>
        /// Provides a wrapper for the async accept operations.
        /// </summary>
        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            FtpControlStream connection = (FtpControlStream)asyncResult.AsyncState!;
            Socket listenSocket = connection._dataSocket!;
            try
            {
                connection._dataSocket = listenSocket.EndAccept(asyncResult);
                if (!connection.ServerAddress.Equals(((IPEndPoint)connection._dataSocket.RemoteEndPoint!).Address))
                {
                    connection._dataSocket.Close();
                    throw new WebException(SR.net_ftp_active_address_different, WebExceptionStatus.ProtocolError);
                }
                connection.ContinueCommandPipeline();
            }
            catch (Exception e)
            {
                connection.CloseSocket();
                connection.InvokeRequestCallback(e);
            }
            finally
            {
                listenSocket.Close();
            }
        }

        /// <summary>
        ///    <para>Provides a wrapper for the async accept operations</para>
        /// </summary>
        private static void ConnectCallback(IAsyncResult asyncResult)
        {
            FtpControlStream connection = (FtpControlStream)asyncResult.AsyncState!;
            try
            {
                connection._dataSocket!.EndConnect(asyncResult);
                connection.ContinueCommandPipeline();
            }
            catch (Exception e)
            {
                connection.CloseSocket();
                connection.InvokeRequestCallback(e);
            }
        }

        private static void SSLHandshakeCallback(IAsyncResult asyncResult)
        {
            FtpControlStream connection = (FtpControlStream)asyncResult.AsyncState!;
            try
            {
                connection._tlsStream!.EndAuthenticateAsClient(asyncResult);
                connection.ContinueCommandPipeline();
            }
            catch (Exception e)
            {
                connection.CloseSocket();
                connection.InvokeRequestCallback(e);
            }
        }

        //    Creates a FtpDataStream object, constructs a TLS stream if needed.
        //    In case SSL and ASYNC we delay sigaling the user stream until the handshake is done.
        private PipelineInstruction QueueOrCreateFtpDataStream(ref Stream? stream)
        {
            if (_dataSocket == null)
                throw new InternalException();

            //
            // Re-entered pipeline with completed read on the TlsStream
            //
            if (_tlsStream != null)
            {
                stream = new FtpDataStream(_tlsStream, (FtpWebRequest)_request!, IsFtpDataStreamWriteable());
                _tlsStream = null;
                return PipelineInstruction.GiveStream;
            }

            NetworkStream networkStream = new NetworkStream(_dataSocket, true);

            if (UsingSecureStream)
            {
                FtpWebRequest request = (FtpWebRequest)_request!;

                TlsStream tlsStream = new TlsStream(networkStream, _dataSocket, request.RequestUri.Host, request.ClientCertificates);
                networkStream = tlsStream;

                if (_isAsync)
                {
                    _tlsStream = tlsStream;

                    tlsStream.BeginAuthenticateAsClient(s_SSLHandshakeCallback, this);
                    return PipelineInstruction.Pause;
                }
                else
                {
                    tlsStream.AuthenticateAsClient();
                }
            }

            stream = new FtpDataStream(networkStream, (FtpWebRequest)_request!, IsFtpDataStreamWriteable());
            return PipelineInstruction.GiveStream;
        }

        protected override void ClearState()
        {
            _contentLength = -1;
            _lastModified = DateTime.MinValue;
            _responseUri = null;
            _dataHandshakeStarted = false;
            StatusCode = FtpStatusCode.Undefined;
            StatusLine = null;

            _dataSocket = null;
            _passiveEndPoint = null;
            _tlsStream = null;

            base.ClearState();
        }

        //    This is called by underlying base class code, each time a new response is received from the wire or a protocol stage is resumed.
        //    This function controls the setting up of a data socket/connection, and of saving off the server responses.
        protected override PipelineInstruction PipelineCallback(PipelineEntry? entry, ResponseDescription? response, bool timeout, ref Stream? stream)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Command:{entry?.Command} Description:{response?.StatusDescription}");

            // null response is not expected
            if (response == null)
                return PipelineInstruction.Abort;

            FtpStatusCode status = (FtpStatusCode)response.Status;

            //
            // Update global "current status" for FtpWebRequest
            //
            if (status != FtpStatusCode.ClosingControl)
            {
                // A 221 status won't be reflected on the user FTP response
                // Anything else will (by design?)
                StatusCode = status;
                StatusLine = response.StatusDescription;
            }

            // If the status code is outside the range defined in RFC (1xx to 5xx) throw
            if (response.InvalidStatusCode)
                throw new WebException(SR.net_InvalidStatusCode, WebExceptionStatus.ProtocolError);

            // Update the banner message if any, this is a little hack because the "entry" param is null
            if (_index == -1)
            {
                if (status == FtpStatusCode.SendUserCommand)
                {
                    _bannerMessage = new StringBuilder();
                    _bannerMessage.Append(StatusLine);
                    return PipelineInstruction.Advance;
                }
                else if (status == FtpStatusCode.ServiceTemporarilyNotAvailable)
                {
                    return PipelineInstruction.Reread;
                }
                else
                    throw GenerateException(status, response.StatusDescription, null);
            }

            //
            // Check for the result of our attempt to use UTF8
            //
            if (entry!.Command == "OPTS utf8 on\r\n")
            {
                if (response.PositiveCompletion)
                {
                    Encoding = Encoding.UTF8;
                }
                else
                {
                    Encoding = Encoding.Default;
                }
                return PipelineInstruction.Advance;
            }

            // If we are already logged in and the server returns 530 then
            // the server does not support re-issuing a USER command,
            // tear down the connection and start all over again
            if (entry.Command.IndexOf("USER", StringComparison.Ordinal) != -1)
            {
                // The server may not require a password for this user, so bypass the password command
                if (status == FtpStatusCode.LoggedInProceed)
                {
                    _loginState = FtpLoginState.LoggedIn;
                    _index++;
                }
            }

            //
            // Throw on an error with possible recovery option
            //
            if (response.TransientFailure || response.PermanentFailure)
            {
                if (status == FtpStatusCode.ServiceNotAvailable)
                {
                    MarkAsRecoverableFailure();
                }
                throw GenerateException(status, response.StatusDescription, null);
            }

            if (_loginState != FtpLoginState.LoggedIn
                && entry.Command.IndexOf("PASS", StringComparison.Ordinal) != -1)
            {
                // Note the fact that we logged in
                if (status == FtpStatusCode.NeedLoginAccount || status == FtpStatusCode.LoggedInProceed)
                    _loginState = FtpLoginState.LoggedIn;
                else
                    throw GenerateException(status, response.StatusDescription, null);
            }

            //
            // Parse special cases
            //
            if (entry.HasFlag(PipelineEntryFlags.CreateDataConnection) && (response.PositiveCompletion || response.PositiveIntermediate))
            {
                bool isSocketReady;
                PipelineInstruction result = QueueOrCreateDataConection(entry, response, timeout, ref stream, out isSocketReady);
                if (!isSocketReady)
                    return result;
                // otherwise we have a stream to create
            }
            //
            // This is part of the above case and it's all about giving data stream back
            //
            if (status == FtpStatusCode.OpeningData || status == FtpStatusCode.DataAlreadyOpen)
            {
                if (_dataSocket == null)
                {
                    return PipelineInstruction.Abort;
                }
                if (!entry.HasFlag(PipelineEntryFlags.GiveDataStream))
                {
                    _abortReason = SR.Format(SR.net_ftp_invalid_status_response, status, entry.Command);
                    return PipelineInstruction.Abort;
                }

                // Parse out the Content length, if we can
                TryUpdateContentLength(response.StatusDescription!);

                // Parse out the file name, when it is returned and use it for our ResponseUri
                FtpWebRequest request = (FtpWebRequest)_request!;
                if (request.MethodInfo.ShouldParseForResponseUri)
                {
                    TryUpdateResponseUri(response.StatusDescription!, request);
                }

                return QueueOrCreateFtpDataStream(ref stream);
            }


            //
            // Parse responses by status code exclusivelly
            //

            // Update welcome message
            if (status == FtpStatusCode.LoggedInProceed)
            {
                _welcomeMessage!.Append(StatusLine);
            }
            // OR set the user response ExitMessage
            else if (status == FtpStatusCode.ClosingControl)
            {
                _exitMessage!.Append(response.StatusDescription);
                // And close the control stream socket on "QUIT"
                CloseSocket();
            }
            // OR set us up for SSL/TLS, after this we'll be writing securely
            else if (status == FtpStatusCode.ServerWantsSecureSession)
            {
                // If NetworkStream is a TlsStream, then this must be in the async callback
                // from completing the SSL handshake.
                // So just let the pipeline continue.
                if (!(NetworkStream is TlsStream))
                {
                    FtpWebRequest request = (FtpWebRequest)_request!;
                    TlsStream tlsStream = new TlsStream(NetworkStream, Socket, request.RequestUri.Host, request.ClientCertificates);

                    if (_isAsync)
                    {
                        tlsStream.BeginAuthenticateAsClient(ar =>
                        {
                            try
                            {
                                tlsStream.EndAuthenticateAsClient(ar);
                                NetworkStream = tlsStream;
                                this.ContinueCommandPipeline();
                            }
                            catch (Exception e)
                            {
                                this.CloseSocket();
                                this.InvokeRequestCallback(e);
                            }
                        }, null);

                        return PipelineInstruction.Pause;
                    }
                    else
                    {
                        tlsStream.AuthenticateAsClient();
                        NetworkStream = tlsStream;
                    }
                }
            }
            // OR parse out the file size or file time, usually a result of sending SIZE/MDTM commands
            else if (status == FtpStatusCode.FileStatus)
            {
                if (entry.Command.StartsWith("SIZE ", StringComparison.Ordinal))
                {
                    _contentLength = GetContentLengthFrom213Response(response.StatusDescription!);
                }
                else if (entry.Command.StartsWith("MDTM ", StringComparison.Ordinal))
                {
                    _lastModified = GetLastModifiedFrom213Response(response.StatusDescription!);
                }
            }
            // OR parse out our login directory
            else if (status == FtpStatusCode.PathnameCreated)
            {
                if (entry.Command == "PWD\r\n" && !entry.HasFlag(PipelineEntryFlags.UserCommand))
                {
                    _loginDirectory = GetLoginDirectory(response.StatusDescription!);
                }
            }
            // Asserting we have some positive response
            else
            {
                // We only use CWD to reset ourselves back to the login directory.
                if (entry.Command.IndexOf("CWD", StringComparison.Ordinal) != -1)
                {
                    _establishedServerDirectory = _requestedServerDirectory;
                }
            }

            // Intermediate responses require rereading
            if (response.PositiveIntermediate || (!UsingSecureStream && entry.Command == "AUTH TLS\r\n"))
            {
                return PipelineInstruction.Reread;
            }

            return PipelineInstruction.Advance;
        }

        /// <summary>
        ///    <para>Creates an array of commands, that will be sent to the server</para>
        /// </summary>
        protected override PipelineEntry[] BuildCommandsList(WebRequest req)
        {
            bool resetLoggedInState = false;
            FtpWebRequest request = (FtpWebRequest)req;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);

            _responseUri = request.RequestUri;

            var commandList = new List<PipelineEntry>();

            if (request.EnableSsl && !UsingSecureStream)
            {
                commandList.Add(new PipelineEntry(FormatFtpCommand("AUTH", "TLS")));
                // According to RFC we need to re-authorize with USER/PASS after we re-authenticate.
                resetLoggedInState = true;
            }

            if (resetLoggedInState)
            {
                _loginDirectory = null;
                _establishedServerDirectory = null;
                _requestedServerDirectory = null;
                _currentTypeSetting = string.Empty;
                if (_loginState == FtpLoginState.LoggedIn)
                    _loginState = FtpLoginState.LoggedInButNeedsRelogin;
            }

            if (_loginState != FtpLoginState.LoggedIn)
            {
                Credentials = request.Credentials!.GetCredential(request.RequestUri, "basic");
                _welcomeMessage = new StringBuilder();
                _exitMessage = new StringBuilder();

                string domainUserName = string.Empty;
                string password = string.Empty;

                if (Credentials != null)
                {
                    domainUserName = Credentials.UserName;
                    string domain = Credentials.Domain;
                    if (!string.IsNullOrEmpty(domain))
                    {
                        domainUserName = domain + "\\" + domainUserName;
                    }

                    password = Credentials.Password;
                }

                if (domainUserName.Length == 0 && password.Length == 0)
                {
                    domainUserName = "anonymous";
                    // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Anonymous FTP credential in production code.")]
                    password = "anonymous@";
                }

                commandList.Add(new PipelineEntry(FormatFtpCommand("USER", domainUserName)));
                commandList.Add(new PipelineEntry(FormatFtpCommand("PASS", password), PipelineEntryFlags.DontLogParameter));

                // If SSL, always configure data channel encryption after authentication to maximum RFC compatibility.   The RFC allows for
                // PBSZ/PROT commands to come either before or after the USER/PASS, but some servers require USER/PASS immediately after
                // the AUTH TLS command.
                if (request.EnableSsl && !UsingSecureStream)
                {
                    commandList.Add(new PipelineEntry(FormatFtpCommand("PBSZ", "0")));
                    commandList.Add(new PipelineEntry(FormatFtpCommand("PROT", "P")));
                }

                commandList.Add(new PipelineEntry(FormatFtpCommand("OPTS", "utf8 on")));
                commandList.Add(new PipelineEntry(FormatFtpCommand("PWD", null)));
            }

            GetPathOption getPathOption = GetPathOption.Normal;

            if (request.MethodInfo.HasFlag(FtpMethodFlags.DoesNotTakeParameter))
            {
                getPathOption = GetPathOption.AssumeNoFilename;
            }
            else if (request.MethodInfo.HasFlag(FtpMethodFlags.ParameterIsDirectory))
            {
                getPathOption = GetPathOption.AssumeFilename;
            }

            string requestPath;
            string requestDirectory;
            string requestFilename;

            GetPathInfo(getPathOption, request.RequestUri, out requestPath, out requestDirectory, out requestFilename);

            if (requestFilename.Length == 0 && request.MethodInfo.HasFlag(FtpMethodFlags.TakesParameter))
                throw new WebException(SR.net_ftp_invalid_uri);

            // We optimize for having the current working directory staying at the login directory.  This ensure that
            // our relative paths work right and reduces unnecessary CWD commands.
            // Usually, we don't change the working directory except for some FTP commands.  If necessary,
            // we need to reset our working directory back to the login directory.
            if (_establishedServerDirectory != null && _loginDirectory != null && _establishedServerDirectory != _loginDirectory)
            {
                commandList.Add(new PipelineEntry(FormatFtpCommand("CWD", _loginDirectory), PipelineEntryFlags.UserCommand));
                _requestedServerDirectory = _loginDirectory;
            }

            // For most commands, we don't need to navigate to the directory since we pass in the full
            // path as part of the FTP protocol command.   However,  some commands require it.
            if (request.MethodInfo.HasFlag(FtpMethodFlags.MustChangeWorkingDirectoryToPath) && requestDirectory.Length > 0)
            {
                commandList.Add(new PipelineEntry(FormatFtpCommand("CWD", requestDirectory), PipelineEntryFlags.UserCommand));
                _requestedServerDirectory = requestDirectory;
            }

            if (!request.MethodInfo.IsCommandOnly)
            {
                string requestedTypeSetting = request.UseBinary ? "I" : "A";
                if (_currentTypeSetting != requestedTypeSetting)
                {
                    commandList.Add(new PipelineEntry(FormatFtpCommand("TYPE", requestedTypeSetting)));
                    _currentTypeSetting = requestedTypeSetting;
                }

                if (request.UsePassive)
                {
                    string passiveCommand = (ServerAddress.AddressFamily == AddressFamily.InterNetwork || ServerAddress.IsIPv4MappedToIPv6) ? "PASV" : "EPSV";
                    commandList.Add(new PipelineEntry(FormatFtpCommand(passiveCommand, null), PipelineEntryFlags.CreateDataConnection));
                }
                else
                {
                    string portCommand = (ServerAddress.AddressFamily == AddressFamily.InterNetwork || ServerAddress.IsIPv4MappedToIPv6) ? "PORT" : "EPRT";
                    CreateFtpListenerSocket(request);
                    commandList.Add(new PipelineEntry(FormatFtpCommand(portCommand, GetPortCommandLine(request))));
                }

                if (request.ContentOffset > 0)
                {
                    // REST command must always be the last sent before the main file command is sent.
                    commandList.Add(new PipelineEntry(FormatFtpCommand("REST", request.ContentOffset.ToString(CultureInfo.InvariantCulture))));
                }
            }

            PipelineEntryFlags flags = PipelineEntryFlags.UserCommand;
            if (!request.MethodInfo.IsCommandOnly)
            {
                flags |= PipelineEntryFlags.GiveDataStream;
                if (!request.UsePassive)
                    flags |= PipelineEntryFlags.CreateDataConnection;
            }

            if (request.MethodInfo.Operation == FtpOperation.Rename)
            {
                string baseDir = (requestDirectory.Length == 0)
                    ? string.Empty : requestDirectory + "/";
                commandList.Add(new PipelineEntry(FormatFtpCommand("RNFR", baseDir + requestFilename), flags));

                string renameTo;
                if (request.RenameTo is not null && request.RenameTo.StartsWith('/'))
                {
                    renameTo = request.RenameTo; // Absolute path
                }
                else
                {
                    renameTo = baseDir + request.RenameTo; // Relative path
                }
                commandList.Add(new PipelineEntry(FormatFtpCommand("RNTO", renameTo), flags));
            }
            else if (request.MethodInfo.HasFlag(FtpMethodFlags.DoesNotTakeParameter))
            {
                commandList.Add(new PipelineEntry(FormatFtpCommand(request.Method, string.Empty), flags));
            }
            else if (request.MethodInfo.HasFlag(FtpMethodFlags.MustChangeWorkingDirectoryToPath))
            {
                commandList.Add(new PipelineEntry(FormatFtpCommand(request.Method, requestFilename), flags));
            }
            else
            {
                commandList.Add(new PipelineEntry(FormatFtpCommand(request.Method, requestPath), flags));
            }

            commandList.Add(new PipelineEntry(FormatFtpCommand("QUIT", null)));

            return commandList.ToArray();
        }

        private PipelineInstruction QueueOrCreateDataConection(PipelineEntry entry, ResponseDescription response, bool timeout, ref Stream? stream, out bool isSocketReady)
        {
            isSocketReady = false;
            if (_dataHandshakeStarted)
            {
                isSocketReady = true;
                return PipelineInstruction.Pause; //if we already started then this is re-entering into the callback where we proceed with the stream
            }

            _dataHandshakeStarted = true;

            // Handle passive responses by parsing the port and later doing a Connect(...)
            bool isPassive = false;
            int port = -1;

            if (entry.Command == "PASV\r\n" || entry.Command == "EPSV\r\n")
            {
                if (!response.PositiveCompletion)
                {
                    _abortReason = SR.Format(SR.net_ftp_server_failed_passive, response.Status);
                    return PipelineInstruction.Abort;
                }
                if (entry.Command == "PASV\r\n")
                {
                    port = GetPortV4(response.StatusDescription!);
                }
                else
                {
                    port = GetPortV6(response.StatusDescription!);
                }

                isPassive = true;
            }

            if (isPassive)
            {
                Debug.Assert(port != -1, "'port' not set.");

                try
                {
                    _dataSocket = CreateFtpDataSocket((FtpWebRequest)_request!, Socket);
                }
                catch (ObjectDisposedException)
                {
                    throw ExceptionHelper.RequestAbortedException;
                }

                IPEndPoint localEndPoint = new IPEndPoint(((IPEndPoint)Socket.LocalEndPoint!).Address, 0);
                _dataSocket.Bind(localEndPoint);

                _passiveEndPoint = new IPEndPoint(ServerAddress, port);
            }

            PipelineInstruction result;

            if (_passiveEndPoint != null)
            {
                IPEndPoint passiveEndPoint = _passiveEndPoint;
                _passiveEndPoint = null;
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "starting Connect()");
                if (_isAsync)
                {
                    _dataSocket!.BeginConnect(passiveEndPoint, s_connectCallbackDelegate, this);
                    result = PipelineInstruction.Pause;
                }
                else
                {
                    _dataSocket!.Connect(passiveEndPoint);
                    result = PipelineInstruction.Advance; // for passive mode we end up going to the next command
                }
            }
            else
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "starting Accept()");

                if (_isAsync)
                {
                    _dataSocket!.BeginAccept(s_acceptCallbackDelegate, this);
                    result = PipelineInstruction.Pause;
                }
                else
                {
                    Socket listenSocket = _dataSocket!;
                    try
                    {
                        _dataSocket = _dataSocket!.Accept();
                        if (!ServerAddress.Equals(((IPEndPoint)_dataSocket.RemoteEndPoint!).Address))
                        {
                            _dataSocket.Close();
                            throw new WebException(SR.net_ftp_active_address_different, WebExceptionStatus.ProtocolError);
                        }
                        isSocketReady = true;   // for active mode we end up creating a stream before advancing the pipeline
                        result = PipelineInstruction.Pause;
                    }
                    finally
                    {
                        listenSocket.Close();
                    }
                }
            }
            return result;
        }

        private enum GetPathOption
        {
            Normal,
            AssumeFilename,
            AssumeNoFilename
        }

        /// <summary>
        ///    <para>Gets the path component of the Uri</para>
        /// </summary>
        private static void GetPathInfo(GetPathOption pathOption,
                                                           Uri uri,
                                                           out string path,
                                                           out string directory,
                                                           out string filename)
        {
            path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            int index = path.LastIndexOf('/');

            if (pathOption == GetPathOption.AssumeFilename &&
                index != -1 && index == path.Length - 1)
            {
                // Remove last '/' and continue normal processing
                path = path.Substring(0, path.Length - 1);
                index = path.LastIndexOf('/');
            }

            // split path into directory and filename
            if (pathOption == GetPathOption.AssumeNoFilename)
            {
                directory = path;
                filename = string.Empty;
            }
            else
            {
                directory = path.Substring(0, index + 1);
                filename = path.Substring(index + 1);
            }

            // strip off trailing '/' on directory if present
            if (directory.Length > 1 && directory.EndsWith('/'))
                directory = directory.Substring(0, directory.Length - 1);
        }

        //
        /// <summary>
        ///    <para>Formats an IP address (contained in a UInt32) to a FTP style command string</para>
        /// </summary>
        private static string FormatAddress(IPAddress address, int Port)
        {
            byte[] localAddressInBytes = address.GetAddressBytes();

            // produces a string in FTP IPAddress/Port encoding (a1, a2, a3, a4, p1, p2), for sending as a parameter
            // to the port command.
            StringBuilder sb = new StringBuilder(32);
            for (int i = address.IsIPv4MappedToIPv6 ? 12 : 0; i < localAddressInBytes.Length; i++)
            {
                sb.Append(localAddressInBytes[i]);
                sb.Append(',');
            }
            sb.Append(Port / 256);
            sb.Append(',');
            sb.Append(Port % 256);
            return sb.ToString();
        }

        /// <summary>
        ///    <para>Formats an IP address (v6) to a FTP style command string
        ///    Looks something in this form: |2|1080::8:800:200C:417A|5282| </para>
        ///    |2|4567::0123:5678:0123:5678|0123|
        /// </summary>
        private static string FormatAddressV6(IPAddress address, int port)
        {
            return
                "|2|" +
                address.ToString() +
                "|" +
                port.ToString(NumberFormatInfo.InvariantInfo) +
                "|";
        }

        internal long ContentLength
        {
            get
            {
                return _contentLength;
            }
        }

        internal DateTime LastModified
        {
            get
            {
                return _lastModified;
            }
        }

        internal Uri ResponseUri
        {
            get
            {
                return _responseUri!;
            }
        }

        /// <summary>
        ///    <para>Returns the server message sent before user credentials are sent</para>
        /// </summary>
        internal string? BannerMessage
        {
            get
            {
                return (_bannerMessage != null) ? _bannerMessage.ToString() : null;
            }
        }

        /// <summary>
        ///    <para>Returns the server message sent after user credentials are sent</para>
        /// </summary>
        internal string? WelcomeMessage
        {
            get
            {
                return (_welcomeMessage != null) ? _welcomeMessage.ToString() : null;
            }
        }

        /// <summary>
        ///    <para>Returns the exit sent message on shutdown</para>
        /// </summary>
        internal string? ExitMessage
        {
            get
            {
                return (_exitMessage != null) ? _exitMessage.ToString() : null;
            }
        }

        /// <summary>
        ///    <para>Parses a response string for content length</para>
        /// </summary>
        private static long GetContentLengthFrom213Response(string responseString)
        {
            string[] parsedList = responseString.Split(' ');
            if (parsedList.Length < 2)
                throw new FormatException(SR.Format(SR.net_ftp_response_invalid_format, responseString));
            return Convert.ToInt64(parsedList[1], NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        ///    <para>Parses a response string for last modified time</para>
        /// </summary>
        private DateTime GetLastModifiedFrom213Response(string str)
        {
            DateTime dateTime = _lastModified;
            string[] parsedList = str.Split(new char[] { ' ', '.' });
            if (parsedList.Length < 2)
            {
                return dateTime;
            }
            string dateTimeLine = parsedList[1];
            if (dateTimeLine.Length < 14)
            {
                return dateTime;
            }
            int year = Convert.ToInt32(dateTimeLine.Substring(0, 4), NumberFormatInfo.InvariantInfo);
            int month = Convert.ToInt16(dateTimeLine.Substring(4, 2), NumberFormatInfo.InvariantInfo);
            int day = Convert.ToInt16(dateTimeLine.Substring(6, 2), NumberFormatInfo.InvariantInfo);
            int hour = Convert.ToInt16(dateTimeLine.Substring(8, 2), NumberFormatInfo.InvariantInfo);
            int minute = Convert.ToInt16(dateTimeLine.Substring(10, 2), NumberFormatInfo.InvariantInfo);
            int second = Convert.ToInt16(dateTimeLine.Substring(12, 2), NumberFormatInfo.InvariantInfo);
            int millisecond = 0;
            if (parsedList.Length > 2)
            {
                millisecond = Convert.ToInt16(parsedList[2], NumberFormatInfo.InvariantInfo);
            }
            try
            {
                dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);
                dateTime = dateTime.ToLocalTime(); // must be handled in local time
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (ArgumentException)
            {
            }
            return dateTime;
        }

        /// <summary>
        ///    <para>Attempts to find the response Uri
        ///     Typical string looks like this, need to get trailing filename
        ///     "150 Opening BINARY mode data connection for FTP46.tmp."</para>
        /// </summary>
        private void TryUpdateResponseUri(string str, FtpWebRequest request)
        {
            Uri baseUri = request.RequestUri;
            //
            // Not sure what we are doing here but I guess the logic is IIS centric
            //
            int start = str.IndexOf("for ", StringComparison.Ordinal);
            if (start == -1)
                return;
            start += 4;
            int end = str.LastIndexOf('(');
            if (end == -1)
                end = str.Length;
            if (end <= start)
                return;

            string filename = str.Substring(start, end - start);
            filename = filename.TrimEnd(new char[] { ' ', '.', '\r', '\n' });
            // Do minimal escaping that we need to get a valid Uri
            // when combined with the baseUri
            string escapedFilename;
            escapedFilename = filename.Replace("%", "%25");
            escapedFilename = escapedFilename.Replace("#", "%23");

            // help us out if the user forgot to add a slash to the directory name
            string originalPath = baseUri.AbsolutePath;
            if (originalPath.Length > 0 && !originalPath.EndsWith('/'))
            {
                UriBuilder uriBuilder = new UriBuilder(baseUri);
                uriBuilder.Path = originalPath + "/";
                baseUri = uriBuilder.Uri;
            }

            Uri? newUri;
            if (!Uri.TryCreate(baseUri, escapedFilename, out newUri))
            {
                throw new FormatException(SR.Format(SR.net_ftp_invalid_response_filename, filename));
            }
            else
            {
                if (!baseUri.IsBaseOf(newUri) ||
                     baseUri.Segments.Length != newUri.Segments.Length - 1)
                {
                    throw new FormatException(SR.Format(SR.net_ftp_invalid_response_filename, filename));
                }
                else
                {
                    _responseUri = newUri;
                }
            }
        }

        /// <summary>
        ///    <para>Parses a response string for content length</para>
        /// </summary>
        private void TryUpdateContentLength(string str)
        {
            int pos1 = str.LastIndexOf('(');
            if (pos1 != -1)
            {
                int pos2 = str.IndexOf(" bytes).", StringComparison.Ordinal);
                if (pos2 != -1 && pos2 > pos1)
                {
                    pos1++;
                    long result;
                    if (long.TryParse(str.AsSpan(pos1, pos2 - pos1),
                                        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                                        NumberFormatInfo.InvariantInfo, out result))
                    {
                        _contentLength = result;
                    }
                }
            }
        }

        /// <summary>
        ///    <para>Parses a response string for our login dir in " "</para>
        /// </summary>
        private static string GetLoginDirectory(string str)
        {
            int firstQuote = str.IndexOf('"');
            int lastQuote = str.LastIndexOf('"');
            if (firstQuote != -1 && lastQuote != -1 && firstQuote != lastQuote)
            {
                return str.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        ///    <para>Parses a response string for a port number</para>
        /// </summary>
        private static int GetPortV4(string responseString)
        {
            string[] parsedList = responseString.Split(new char[] { ' ', '(', ',', ')' });

            // We need at least the status code and the port
            if (parsedList.Length <= 7)
            {
                throw new FormatException(SR.Format(SR.net_ftp_response_invalid_format, responseString));
            }

            int index = parsedList.Length - 1;
            // skip the last non-number token (e.g. terminating '.')
            if (!char.IsNumber(parsedList[index], 0))
                index--;

            int port = Convert.ToByte(parsedList[index--], NumberFormatInfo.InvariantInfo);
            port = port |
                   (Convert.ToByte(parsedList[index--], NumberFormatInfo.InvariantInfo) << 8);

            return port;
        }

        /// <summary>
        ///    <para>Parses a response string for a port number</para>
        /// </summary>
        private static int GetPortV6(string responseString)
        {
            int pos1 = responseString.LastIndexOf('(');
            int pos2 = responseString.LastIndexOf(')');
            if (pos1 == -1 || pos2 <= pos1)
                throw new FormatException(SR.Format(SR.net_ftp_response_invalid_format, responseString));

            // addressInfo will contain a string of format "|||<tcp-port>|"
            string addressInfo = responseString.Substring(pos1 + 1, pos2 - pos1 - 1);

            string[] parsedList = addressInfo.Split('|');
            if (parsedList.Length < 4)
                throw new FormatException(SR.Format(SR.net_ftp_response_invalid_format, responseString));

            return Convert.ToInt32(parsedList[3], NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        ///    <para>Creates the Listener socket</para>
        /// </summary>
        private void CreateFtpListenerSocket(FtpWebRequest request)
        {
            // Gets an IPEndPoint for the local host for the data socket to bind to.
            IPEndPoint epListener = new IPEndPoint(((IPEndPoint)Socket.LocalEndPoint!).Address, 0);
            try
            {
                _dataSocket = CreateFtpDataSocket(request, Socket);
            }
            catch (ObjectDisposedException)
            {
                throw ExceptionHelper.RequestAbortedException;
            }

            // Binds the data socket to the local end point.
            _dataSocket.Bind(epListener);
            _dataSocket.Listen(1); // Put the dataSocket in Listen mode
        }

        /// <summary>
        ///    <para>Builds a command line to send to the server with proper port and IP address of client</para>
        /// </summary>
        private string GetPortCommandLine(FtpWebRequest request)
        {
            try
            {
                // retrieves the IP address of the local endpoint
                IPEndPoint localEP = (IPEndPoint)_dataSocket!.LocalEndPoint!;
                if (ServerAddress.AddressFamily == AddressFamily.InterNetwork || ServerAddress.IsIPv4MappedToIPv6)
                {
                    return FormatAddress(localEP.Address, localEP.Port);
                }
                else if (ServerAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return FormatAddressV6(localEP.Address, localEP.Port);
                }
                else
                {
                    throw new InternalException();
                }
            }
            catch (Exception e)
            {
                throw GenerateException(SR.net_ftp_protocolerror, WebExceptionStatus.ProtocolError, e); // could not open data connection
            }
        }

        /// <summary>
        ///    <para>Formats a simple FTP command + parameter in correct pre-wire format</para>
        /// </summary>
        private static string FormatFtpCommand(string command, string? parameter)
        {
            return string.IsNullOrEmpty(parameter) ?
                command + "\r\n" :
                command + " " + parameter + "\r\n";
        }

        /// <summary>
        ///    <para>
        ///     This will handle either connecting to a port or listening for one
        ///    </para>
        /// </summary>
        private static Socket CreateFtpDataSocket(FtpWebRequest request, Socket templateSocket)
        {
            // Safe to be called under an Assert.
            Socket socket = new Socket(templateSocket.AddressFamily, templateSocket.SocketType, templateSocket.ProtocolType);
            if (templateSocket.AddressFamily == AddressFamily.InterNetworkV6 && templateSocket.DualMode)
            {
                socket.DualMode = true;
            }

            return socket;
        }

        protected override bool CheckValid(ResponseDescription response, ref int validThrough, ref int completeLength)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"CheckValid({response.StatusBuffer})");

            // If the response is less than 4 bytes long, it is too short to tell, so return true, valid so far.
            if (response.StatusBuffer.Length < 4)
            {
                return true;
            }
            string responseString = response.StatusBuffer.ToString();

            // Otherwise, if there is no status code for this response yet, get one.
            if (response.Status == ResponseDescription.NoStatus)
            {
                // If the response does not start with three digits, then it is not a valid response from an FTP server.
                if (!(char.IsDigit(responseString[0]) && char.IsDigit(responseString[1]) && char.IsDigit(responseString[2]) && (responseString[3] == ' ' || responseString[3] == '-')))
                {
                    return false;
                }
                else
                {
                    response.StatusCodeString = responseString.Substring(0, 3);
                    response.Status = Convert.ToInt16(response.StatusCodeString, NumberFormatInfo.InvariantInfo);
                }

                // IF a hyphen follows the status code on the first line of the response, then we have a multiline response coming.
                if (responseString[3] == '-')
                {
                    response.Multiline = true;
                }
            }

            // If a complete line of response has been received from the server, then see if the
            // overall response is complete.
            // If this was not a multiline response, then the response is complete at the end of the line.

            // If this was a multiline response (indicated by three digits followed by a '-' in the first line),
            // then we see if the last line received started with the same three digits followed by a space.
            // If it did, then this is the sign of a complete multiline response.
            // If the line contained three other digits followed by the response, then this is a violation of the
            // FTP protocol for multiline responses.
            // All other cases indicate that the response is not yet complete.
            int index;
            while ((index = responseString.IndexOf("\r\n", validThrough, StringComparison.Ordinal)) != -1)  // gets the end line.
            {
                int lineStart = validThrough;
                validThrough = index + 2;  // validThrough now marks the end of the line being examined.
                if (!response.Multiline)
                {
                    completeLength = validThrough;
                    return true;
                }

                if (responseString.Length > lineStart + 4)
                {
                    // If the first three characters of the response line currently being examined
                    // match the status code, then if they are followed by a space, then we
                    // have reached the end of the reply.
                    if (responseString.Substring(lineStart, 3) == response.StatusCodeString)
                    {
                        if (responseString[lineStart + 3] == ' ')
                        {
                            completeLength = validThrough;
                            return true;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///    <para>Determines whether the stream we return is Writeable or Readable</para>
        /// </summary>
        private TriState IsFtpDataStreamWriteable()
        {
            FtpWebRequest? request = _request as FtpWebRequest;
            if (request != null)
            {
                if (request.MethodInfo.IsUpload)
                {
                    return TriState.True;
                }
                else if (request.MethodInfo.IsDownload)
                {
                    return TriState.False;
                }
            }
            return TriState.Unspecified;
        }
    } // class FtpControlStream
} // namespace System.Net
