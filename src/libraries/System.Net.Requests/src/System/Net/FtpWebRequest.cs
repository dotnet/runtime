// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Net.Sockets;
using System.Security;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace System.Net
{
    internal enum FtpOperation
    {
        DownloadFile = 0,
        ListDirectory = 1,
        ListDirectoryDetails = 2,
        UploadFile = 3,
        UploadFileUnique = 4,
        AppendFile = 5,
        DeleteFile = 6,
        GetDateTimestamp = 7,
        GetFileSize = 8,
        Rename = 9,
        MakeDirectory = 10,
        RemoveDirectory = 11,
        PrintWorkingDirectory = 12,
        Other = 13,
    }

    [Flags]
    internal enum FtpMethodFlags
    {
        None = 0x0,
        IsDownload = 0x1,
        IsUpload = 0x2,
        TakesParameter = 0x4,
        MayTakeParameter = 0x8,
        DoesNotTakeParameter = 0x10,
        ParameterIsDirectory = 0x20,
        ShouldParseForResponseUri = 0x40,
        HasHttpCommand = 0x80,
        MustChangeWorkingDirectoryToPath = 0x100
    }

    internal sealed class FtpMethodInfo
    {
        internal string Method;
        internal FtpOperation Operation;
        internal FtpMethodFlags Flags;
        internal string? HttpCommand;

        internal FtpMethodInfo(string method,
                               FtpOperation operation,
                               FtpMethodFlags flags,
                               string? httpCommand)
        {
            Method = method;
            Operation = operation;
            Flags = flags;
            HttpCommand = httpCommand;
        }

        internal bool HasFlag(FtpMethodFlags flags)
        {
            return (Flags & flags) != 0;
        }

        internal bool IsCommandOnly
        {
            get { return (Flags & (FtpMethodFlags.IsDownload | FtpMethodFlags.IsUpload)) == 0; }
        }

        internal bool IsUpload
        {
            get { return (Flags & FtpMethodFlags.IsUpload) != 0; }
        }

        internal bool IsDownload
        {
            get { return (Flags & FtpMethodFlags.IsDownload) != 0; }
        }

        /// <summary>
        ///    <para>True if we should attempt to get a response uri
        ///    out of a server response</para>
        /// </summary>
        internal bool ShouldParseForResponseUri
        {
            get { return (Flags & FtpMethodFlags.ShouldParseForResponseUri) != 0; }
        }

        internal static FtpMethodInfo GetMethodInfo(string method)
        {
            method = method.ToUpperInvariant();
            foreach (FtpMethodInfo methodInfo in s_knownMethodInfo)
                if (method == methodInfo.Method)
                    return methodInfo;
            // We don't support generic methods
            throw new ArgumentException(SR.net_ftp_unsupported_method, nameof(method));
        }

        private static readonly FtpMethodInfo[] s_knownMethodInfo =
        {
            new FtpMethodInfo(WebRequestMethods.Ftp.DownloadFile,
                              FtpOperation.DownloadFile,
                              FtpMethodFlags.IsDownload
                              | FtpMethodFlags.HasHttpCommand
                              | FtpMethodFlags.TakesParameter,
                              "GET"),
            new FtpMethodInfo(WebRequestMethods.Ftp.ListDirectory,
                              FtpOperation.ListDirectory,
                              FtpMethodFlags.IsDownload
                              | FtpMethodFlags.MustChangeWorkingDirectoryToPath
                              | FtpMethodFlags.HasHttpCommand
                              | FtpMethodFlags.MayTakeParameter,
                              "GET"),
            new FtpMethodInfo(WebRequestMethods.Ftp.ListDirectoryDetails,
                              FtpOperation.ListDirectoryDetails,
                              FtpMethodFlags.IsDownload
                              | FtpMethodFlags.MustChangeWorkingDirectoryToPath
                              | FtpMethodFlags.HasHttpCommand
                              | FtpMethodFlags.MayTakeParameter,
                              "GET"),
            new FtpMethodInfo(WebRequestMethods.Ftp.UploadFile,
                              FtpOperation.UploadFile,
                              FtpMethodFlags.IsUpload
                              | FtpMethodFlags.TakesParameter,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.UploadFileWithUniqueName,
                              FtpOperation.UploadFileUnique,
                              FtpMethodFlags.IsUpload
                              | FtpMethodFlags.MustChangeWorkingDirectoryToPath
                              | FtpMethodFlags.DoesNotTakeParameter
                              | FtpMethodFlags.ShouldParseForResponseUri,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.AppendFile,
                              FtpOperation.AppendFile,
                              FtpMethodFlags.IsUpload
                              | FtpMethodFlags.TakesParameter,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.DeleteFile,
                              FtpOperation.DeleteFile,
                              FtpMethodFlags.TakesParameter,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.GetDateTimestamp,
                              FtpOperation.GetDateTimestamp,
                              FtpMethodFlags.TakesParameter,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.GetFileSize,
                              FtpOperation.GetFileSize,
                              FtpMethodFlags.TakesParameter,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.Rename,
                              FtpOperation.Rename,
                              FtpMethodFlags.TakesParameter,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.MakeDirectory,
                              FtpOperation.MakeDirectory,
                              FtpMethodFlags.TakesParameter
                              | FtpMethodFlags.ParameterIsDirectory,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.RemoveDirectory,
                              FtpOperation.RemoveDirectory,
                              FtpMethodFlags.TakesParameter
                              | FtpMethodFlags.ParameterIsDirectory,
                              null),
            new FtpMethodInfo(WebRequestMethods.Ftp.PrintWorkingDirectory,
                              FtpOperation.PrintWorkingDirectory,
                              FtpMethodFlags.DoesNotTakeParameter,
                              null)
        };
    }

    /// <summary>
    /// The FtpWebRequest class implements a basic FTP client interface.
    /// </summary>
    public sealed class FtpWebRequest : WebRequest
    {
        private object _syncObject;
        private ICredentials _authInfo;
        private readonly Uri _uri;
        private FtpMethodInfo _methodInfo;
        private string? _renameTo;
        private bool _getRequestStreamStarted;
        private bool _getResponseStarted;
        private DateTime _startTime;
        private int _timeout = s_DefaultTimeout;
        private int _remainingTimeout;
        private long _contentLength;
        private long _contentOffset;
        private X509CertificateCollection? _clientCertificates;
        private bool _passive = true;
        private bool _binary = true;
        private string? _connectionGroupName;
        private ServicePoint? _servicePoint;

        private bool _async;
        private bool _aborted;
        private bool _timedOut;

        private Exception? _exception;

        private TimerThread.Queue? _timerQueue = s_DefaultTimerQueue;
        private readonly TimerThread.Callback _timerCallback;

        private bool _enableSsl;
        private FtpControlStream? _connection;
        private Stream? _stream;
        private RequestStage _requestStage;
        private bool _onceFailed;
        private WebHeaderCollection? _ftpRequestHeaders;
        private FtpWebResponse? _ftpWebResponse;
        private int _readWriteTimeout = 5 * 60 * 1000;  // 5 minutes.

        private ContextAwareResult? _writeAsyncResult;
        private LazyAsyncResult? _readAsyncResult;
        private LazyAsyncResult? _requestCompleteAsyncResult;

        // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Anonymous FTP credential in production code.")]
        private static readonly NetworkCredential s_defaultFtpNetworkCredential = new NetworkCredential("anonymous", "anonymous@", string.Empty);
        private const int s_DefaultTimeout = 100000;  // 100 seconds
        private static readonly TimerThread.Queue s_DefaultTimerQueue = TimerThread.GetOrCreateQueue(s_DefaultTimeout);

        // Used by FtpControlStream
        internal FtpMethodInfo MethodInfo
        {
            get
            {
                return _methodInfo;
            }
        }

        public static new RequestCachePolicy? DefaultCachePolicy
        {
            get
            {
                return WebRequest.DefaultCachePolicy;
            }
            set
            {
                // We don't support caching, so ignore attempts to set this property.
            }
        }

        /// <summary>
        /// <para>
        /// Selects FTP command to use. WebRequestMethods.Ftp.DownloadFile is default.
        /// Not allowed to be changed once request is started.
        /// </para>
        /// </summary>
        public override string Method
        {
            get
            {
                return _methodInfo.Method;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(SR.net_ftp_invalid_method_name, nameof(value));
                }
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                try
                {
                    _methodInfo = FtpMethodInfo.GetMethodInfo(value);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(SR.net_ftp_unsupported_method, nameof(value));
                }
            }
        }

        /// <summary>
        /// <para>
        /// Sets the target name for the WebRequestMethods.Ftp.Rename command.
        /// Not allowed to be changed once request is started.
        /// </para>
        /// </summary>
        [DisallowNull]
        public string? RenameTo
        {
            get
            {
                return _renameTo;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(SR.net_ftp_invalid_renameto, nameof(value));
                }

                _renameTo = value;
            }
        }

        /// <summary>
        /// <para>Used for clear text authentication with FTP server</para>
        /// </summary>
        [DisallowNull]
        public override ICredentials? Credentials
        {
            get
            {
                return _authInfo;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                ArgumentNullException.ThrowIfNull(value);
                if (value == CredentialCache.DefaultNetworkCredentials)
                {
                    throw new ArgumentException(SR.net_ftp_no_defaultcreds, nameof(value));
                }
                _authInfo = value;
            }
        }

        /// <summary>
        /// <para>Gets the Uri used to make the request</para>
        /// </summary>
        public override Uri RequestUri
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// <para>Timeout of the blocking calls such as GetResponse and GetRequestStream (default 100 secs)</para>
        /// </summary>
        public override int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_ge_zero);
                }
                if (_timeout != value)
                {
                    _timeout = value;
                    _timerQueue = null;
                }
            }
        }

        internal int RemainingTimeout
        {
            get
            {
                return _remainingTimeout;
            }
        }

        /// <summary>
        ///    <para>Used to control the Timeout when calling Stream.Read and Stream.Write.
        ///         Applies to Streams returned from GetResponse().GetResponseStream() and GetRequestStream().
        ///         Default is 5 mins.
        ///    </para>
        /// </summary>
        public int ReadWriteTimeout
        {
            get
            {
                return _readWriteTimeout;
            }
            set
            {
                if (_getResponseStarted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                if (value <= 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_gt_zero);
                }
                _readWriteTimeout = value;
            }
        }

        /// <summary>
        /// <para>Used to specify what offset we will read at</para>
        /// </summary>
        public long ContentOffset
        {
            get
            {
                return _contentOffset;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _contentOffset = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the data size of to-be uploaded data</para>
        /// </summary>
        public override long ContentLength
        {
            get
            {
                return _contentLength;
            }
            set
            {
                _contentLength = value;
            }
        }

        public override IWebProxy? Proxy
        {
            get
            {
                return null;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                // Ignore, since we do not support proxied requests.
            }
        }

        public override string? ConnectionGroupName
        {
            get
            {
                return _connectionGroupName;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                _connectionGroupName = value;
            }
        }

        public ServicePoint ServicePoint => _servicePoint ??= ServicePointManager.FindServicePoint(_uri);

        internal bool Aborted
        {
            get
            {
                return _aborted;
            }
        }

        internal FtpWebRequest(Uri uri)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, uri);

            if ((object)uri.Scheme != (object)Uri.UriSchemeFtp)
                throw new ArgumentOutOfRangeException(nameof(uri));

            _timerCallback = new TimerThread.Callback(TimerCallback);
            _syncObject = new object();

            NetworkCredential? networkCredential = null;
            _uri = uri;
            _methodInfo = FtpMethodInfo.GetMethodInfo(WebRequestMethods.Ftp.DownloadFile);
            if (_uri.UserInfo != null && _uri.UserInfo.Length != 0)
            {
                string userInfo = _uri.UserInfo;
                string username = userInfo;
                string password = "";
                int index = userInfo.IndexOf(':');
                if (index != -1)
                {
                    username = Uri.UnescapeDataString(userInfo.Substring(0, index));
                    index++; // skip ':'
                    password = Uri.UnescapeDataString(userInfo.Substring(index, userInfo.Length - index));
                }
                networkCredential = new NetworkCredential(username, password);
            }
            if (networkCredential == null)
            {
                networkCredential = s_defaultFtpNetworkCredential;
            }
            _authInfo = networkCredential;
        }

        //
        // Used to query for the Response of an FTP request
        //
        public override WebResponse GetResponse()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Method: {_methodInfo.Method}");

            try
            {
                CheckError();

                if (_ftpWebResponse != null)
                {
                    return _ftpWebResponse;
                }

                if (_getResponseStarted)
                {
                    throw new InvalidOperationException(SR.net_repcall);
                }

                _getResponseStarted = true;

                _startTime = DateTime.UtcNow;
                _remainingTimeout = Timeout;

                if (Timeout != System.Threading.Timeout.Infinite)
                {
                    _remainingTimeout = Timeout - (int)((DateTime.UtcNow - _startTime).TotalMilliseconds);

                    if (_remainingTimeout <= 0)
                    {
                        throw ExceptionHelper.TimeoutException;
                    }
                }

                RequestStage prev = FinishRequestStage(RequestStage.RequestStarted);
                if (prev >= RequestStage.RequestStarted)
                {
                    if (prev < RequestStage.ReadReady)
                    {
                        lock (_syncObject)
                        {
                            if (_requestStage < RequestStage.ReadReady)
                                _readAsyncResult = new LazyAsyncResult(null, null, null);
                        }

                        // GetRequeststream or BeginGetRequestStream has not finished yet
                        if (_readAsyncResult != null)
                            _readAsyncResult.InternalWaitForCompletion();

                        CheckError();
                    }
                }
                else
                {
                    SubmitRequest(false);
                    if (_methodInfo.IsUpload)
                        FinishRequestStage(RequestStage.WriteReady);
                    else
                        FinishRequestStage(RequestStage.ReadReady);
                    CheckError();

                    EnsureFtpWebResponse(null);
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);

                // if _exception == null, we are about to throw an exception to the user
                // and we haven't saved the exception, which also means we haven't dealt
                // with it. So just release the connection and log this for investigation.
                if (_exception == null)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                    SetException(exception);
                    FinishRequestStage(RequestStage.CheckForError);
                }
                throw;
            }
            return _ftpWebResponse!;
        }

        /// <summary>
        /// <para>Used to query for the Response of an FTP request [async version]</para>
        /// </summary>
        public override IAsyncResult BeginGetResponse(AsyncCallback? callback, object? state)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Method: {_methodInfo.Method}");

            ContextAwareResult? asyncResult;

            try
            {
                if (_ftpWebResponse != null)
                {
                    asyncResult = new ContextAwareResult(this, state, callback);
                    asyncResult.InvokeCallback(_ftpWebResponse);
                    return asyncResult;
                }

                if (_getResponseStarted)
                {
                    throw new InvalidOperationException(SR.net_repcall);
                }

                _getResponseStarted = true;
                CheckError();

                RequestStage prev = FinishRequestStage(RequestStage.RequestStarted);
                asyncResult = new ContextAwareResult(true, true, this, state, callback);
                _readAsyncResult = asyncResult;

                if (prev >= RequestStage.RequestStarted)
                {
                    // To make sure the context is flowed
                    asyncResult.StartPostingAsyncOp();
                    asyncResult.FinishPostingAsyncOp();

                    if (prev >= RequestStage.ReadReady)
                        asyncResult = null;
                    else
                    {
                        lock (_syncObject)
                        {
                            if (_requestStage >= RequestStage.ReadReady)
                                asyncResult = null;
                        }
                    }

                    if (asyncResult == null)
                    {
                        // need to complete it now
                        asyncResult = (ContextAwareResult)_readAsyncResult;
                        if (!asyncResult.InternalPeekCompleted)
                            asyncResult.InvokeCallback();
                    }
                }
                else
                {
                    // Do internal processing in this handler to optimize context flowing.
                    lock (asyncResult.StartPostingAsyncOp())
                    {
                        SubmitRequest(true);
                        asyncResult.FinishPostingAsyncOp();
                    }
                    FinishRequestStage(RequestStage.CheckForError);
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                throw;
            }

            return asyncResult;
        }

        /// <summary>
        /// <para>Returns result of query for the Response of an FTP request [async version]</para>
        /// </summary>
        public override WebResponse EndGetResponse(IAsyncResult asyncResult!!)
        {
            try
            {
                // parameter validation
                LazyAsyncResult? castedAsyncResult = asyncResult as LazyAsyncResult;
                if (castedAsyncResult == null)
                {
                    throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
                }
                if (castedAsyncResult.EndCalled)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetResponse"));
                }

                castedAsyncResult.InternalWaitForCompletion();
                castedAsyncResult.EndCalled = true;
                CheckError();
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                throw;
            }

            return _ftpWebResponse!;
        }

        /// <summary>
        /// <para>Used to query for the Request stream of an FTP Request</para>
        /// </summary>
        public override Stream GetRequestStream()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Method: {_methodInfo.Method}");

            try
            {
                if (_getRequestStreamStarted)
                {
                    throw new InvalidOperationException(SR.net_repcall);
                }
                _getRequestStreamStarted = true;
                if (!_methodInfo.IsUpload)
                {
                    throw new ProtocolViolationException(SR.net_nouploadonget);
                }
                CheckError();

                _startTime = DateTime.UtcNow;
                _remainingTimeout = Timeout;

                if (Timeout != System.Threading.Timeout.Infinite)
                {
                    _remainingTimeout = Timeout - (int)((DateTime.UtcNow - _startTime).TotalMilliseconds);

                    if (_remainingTimeout <= 0)
                    {
                        throw ExceptionHelper.TimeoutException;
                    }
                }

                FinishRequestStage(RequestStage.RequestStarted);
                SubmitRequest(false);
                FinishRequestStage(RequestStage.WriteReady);
                CheckError();

                if (_stream!.CanTimeout)
                {
                    _stream.WriteTimeout = ReadWriteTimeout;
                    _stream.ReadTimeout = ReadWriteTimeout;
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                throw;
            }
            return _stream;
        }

        /// <summary>
        /// <para>Used to query for the Request stream of an FTP Request [async version]</para>
        /// </summary>
        public override IAsyncResult BeginGetRequestStream(AsyncCallback? callback, object? state)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Method: {_methodInfo.Method}");

            ContextAwareResult? asyncResult = null;
            try
            {
                if (_getRequestStreamStarted)
                {
                    throw new InvalidOperationException(SR.net_repcall);
                }
                _getRequestStreamStarted = true;
                if (!_methodInfo.IsUpload)
                {
                    throw new ProtocolViolationException(SR.net_nouploadonget);
                }
                CheckError();

                FinishRequestStage(RequestStage.RequestStarted);
                asyncResult = new ContextAwareResult(true, true, this, state, callback);
                lock (asyncResult.StartPostingAsyncOp())
                {
                    _writeAsyncResult = asyncResult;
                    SubmitRequest(true);
                    asyncResult.FinishPostingAsyncOp();
                    FinishRequestStage(RequestStage.CheckForError);
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                throw;
            }

            return asyncResult;
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult!!)
        {
            Stream? requestStream;
            try
            {
                LazyAsyncResult? castedAsyncResult = asyncResult as LazyAsyncResult;

                if (castedAsyncResult == null)
                {
                    throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
                }

                if (castedAsyncResult.EndCalled)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetResponse"));
                }

                castedAsyncResult.InternalWaitForCompletion();
                castedAsyncResult.EndCalled = true;
                CheckError();
                requestStream = _stream;
                castedAsyncResult.EndCalled = true;

                if (requestStream!.CanTimeout)
                {
                    requestStream.WriteTimeout = ReadWriteTimeout;
                    requestStream.ReadTimeout = ReadWriteTimeout;
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                throw;
            }
            return requestStream;
        }

        //
        // NOTE1: The caller must synchronize access to SubmitRequest(), only one call is allowed for a particular request.
        // NOTE2: This method eats all exceptions so the caller must rethrow them.
        //
        private void SubmitRequest(bool isAsync)
        {
            try
            {
                _async = isAsync;

                //
                // FYI: Will do 2 attempts max as per AttemptedRecovery
                //
                Stream? stream;

                while (true)
                {
                    FtpControlStream? connection = _connection;

                    if (connection == null)
                    {
                        if (isAsync)
                        {
                            CreateConnectionAsync();
                            return;
                        }

                        connection = CreateConnection();
                        _connection = connection;
                    }

                    if (!isAsync)
                    {
                        if (Timeout != System.Threading.Timeout.Infinite)
                        {
                            _remainingTimeout = Timeout - (int)((DateTime.UtcNow - _startTime).TotalMilliseconds);

                            if (_remainingTimeout <= 0)
                            {
                                throw ExceptionHelper.TimeoutException;
                            }
                        }
                    }

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Request being submitted");

                    connection.SetSocketTimeoutOption(RemainingTimeout);

                    try
                    {
                        stream = TimedSubmitRequestHelper(isAsync);
                    }
                    catch (Exception e)
                    {
                        if (AttemptedRecovery(e))
                        {
                            if (!isAsync)
                            {
                                if (Timeout != System.Threading.Timeout.Infinite)
                                {
                                    _remainingTimeout = Timeout - (int)((DateTime.UtcNow - _startTime).TotalMilliseconds);
                                    if (_remainingTimeout <= 0)
                                    {
                                        throw;
                                    }
                                }
                            }
                            continue;
                        }
                        throw;
                    }
                    // no retry needed
                    break;
                }
            }
            catch (WebException webException)
            {
                // If this was a timeout, throw a timeout exception
                if (webException.InnerException is IOException ioEx &&
                    ioEx.InnerException is SocketException sEx &&
                    sEx.SocketErrorCode == SocketError.TimedOut)
                {
                    SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new WebException(SR.net_timeout, WebExceptionStatus.Timeout)));
                }
                else
                {
                    SetException(webException);
                }
            }
            catch (Exception exception)
            {
                SetException(exception);
            }
        }

        private static Exception TranslateConnectException(Exception e)
        {
            if (e is SocketException se)
            {
                if (se.SocketErrorCode == SocketError.HostNotFound)
                {
                    return new WebException(SR.net_webstatus_NameResolutionFailure, WebExceptionStatus.NameResolutionFailure);
                }
                else
                {
                    return new WebException(SR.net_webstatus_ConnectFailure, WebExceptionStatus.ConnectFailure);
                }
            }

            // Wasn't a socket error, so leave as is
            return e;
        }

        private async void CreateConnectionAsync()
        {
            object result;
            try
            {
                var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await client.ConnectAsync(_uri.Host, _uri.Port).ConfigureAwait(false);
                result = new FtpControlStream(new NetworkStream(client, ownsSocket: true));
            }
            catch (Exception e)
            {
                result = TranslateConnectException(e);
            }

            AsyncRequestCallback(result);
        }

        private FtpControlStream CreateConnection()
        {
            string hostname = _uri.Host;
            int port = _uri.Port;

            var client = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                client.Connect(hostname, port);
            }
            catch (Exception e)
            {
                throw TranslateConnectException(e);
            }

            return new FtpControlStream(new NetworkStream(client, ownsSocket: true));
        }

        private Stream? TimedSubmitRequestHelper(bool isAsync)
        {
            if (isAsync)
            {
                // non-null in the case of re-submit (recovery)
                if (_requestCompleteAsyncResult == null)
                    _requestCompleteAsyncResult = new LazyAsyncResult(null, null, null);
                return _connection!.SubmitRequest(this, true, true)!;
            }

            Stream? stream = null;
            bool timedOut = false;
            TimerThread.Timer timer = TimerQueue.CreateTimer(_timerCallback, null);
            try
            {
                stream = _connection!.SubmitRequest(this, false, true);
            }
            catch (Exception exception)
            {
                if (!(exception is SocketException || exception is ObjectDisposedException) || !timer.HasExpired)
                {
                    timer.Cancel();
                    throw;
                }

                timedOut = true;
            }

            if (timedOut || !timer.Cancel())
            {
                _timedOut = true;
                throw ExceptionHelper.TimeoutException;
            }

            if (stream != null)
            {
                lock (_syncObject)
                {
                    if (_aborted)
                    {
                        ((ICloseEx)stream).CloseEx(CloseExState.Abort | CloseExState.Silent);
                        CheckError(); //must throw
                        throw new InternalException(); //consider replacing this on Assert
                    }
                    _stream = stream;
                }
            }

            return stream;
        }

        /// <summary>
        ///    <para>Because this is called from the timer thread, neither it nor any methods it calls can call user code.</para>
        /// </summary>
        private void TimerCallback(TimerThread.Timer timer, int timeNoticed, object? context)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);

            FtpControlStream? connection = _connection;
            if (connection != null)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "aborting connection");
                connection.AbortConnect();
            }
        }

        private TimerThread.Queue TimerQueue
        {
            get
            {
                if (_timerQueue == null)
                {
                    _timerQueue = TimerThread.GetOrCreateQueue(RemainingTimeout);
                }

                return _timerQueue;
            }
        }

        /// <summary>
        ///    <para>Returns true if we should restart the request after an error</para>
        /// </summary>
        private bool AttemptedRecovery(Exception e)
        {
            if (e is OutOfMemoryException
                || _onceFailed
                || _aborted
                || _timedOut
                || _connection == null
                || !_connection.RecoverableFailure)
            {
                return false;
            }
            _onceFailed = true;

            lock (_syncObject)
            {
                if (_connection != null)
                {
                    _connection.CloseSocket();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Releasing connection: {_connection}");
                    _connection = null;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///    <para>Updates and sets our exception to be thrown</para>
        /// </summary>
        private void SetException(Exception exception)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);

            if (exception is OutOfMemoryException)
            {
                _exception = exception;
                throw exception;
            }

            FtpControlStream? connection = _connection;
            if (_exception == null)
            {
                if (exception is WebException)
                {
                    EnsureFtpWebResponse(exception);
                    _exception = new WebException(exception.Message, null, ((WebException)exception).Status, _ftpWebResponse);
                }
                else if (exception is AuthenticationException || exception is SecurityException)
                {
                    _exception = exception;
                }
                else if (connection != null && connection.StatusCode != FtpStatusCode.Undefined)
                {
                    EnsureFtpWebResponse(exception);
                    _exception = new WebException(SR.Format(SR.net_ftp_servererror, connection.StatusLine), exception, WebExceptionStatus.ProtocolError, _ftpWebResponse);
                }
                else
                {
                    _exception = new WebException(exception.Message, exception);
                }

                if (connection != null && _ftpWebResponse != null)
                    _ftpWebResponse.UpdateStatus(connection.StatusCode, connection.StatusLine, connection.ExitMessage);
            }
        }

        /// <summary>
        ///    <para>Opposite of SetException, rethrows the exception</para>
        /// </summary>
        private void CheckError()
        {
            if (_exception != null)
            {
                ExceptionDispatchInfo.Throw(_exception);
            }
        }

        internal void RequestCallback(object? obj)
        {
            if (_async)
                AsyncRequestCallback(obj);
            else
                SyncRequestCallback(obj);
        }

        //
        // Only executed for Sync requests when the pipline is completed
        //
        private void SyncRequestCallback(object? obj)
        {

            RequestStage stageMode = RequestStage.CheckForError;
            try
            {
                bool completedRequest = obj == null;
                Exception? exception = obj as Exception;

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"exp:{exception} completedRequest:{completedRequest}");

                if (exception != null)
                {
                    SetException(exception);
                }
                else if (!completedRequest)
                {
                    throw new InternalException();
                }
                else
                {
                    FtpControlStream? connection = _connection;

                    if (connection != null)
                    {
                        EnsureFtpWebResponse(null);

                        // This to update response status and exit message if any.
                        // Note that status 221 "Service closing control connection" is always suppressed.
                        _ftpWebResponse!.UpdateStatus(connection.StatusCode, connection.StatusLine, connection.ExitMessage);
                    }

                    stageMode = RequestStage.ReleaseConnection;
                }
            }
            catch (Exception exception)
            {
                SetException(exception);
            }
            finally
            {
                FinishRequestStage(stageMode);
                CheckError(); //will throw on error
            }
        }

        //
        // Only executed for Async requests
        //
        private void AsyncRequestCallback(object? obj)
        {
            RequestStage stageMode = RequestStage.CheckForError;

            try
            {
                FtpControlStream? connection = obj as FtpControlStream;
                FtpDataStream? stream = obj as FtpDataStream;
                Exception? exception = obj as Exception;

                bool completedRequest = (obj == null);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"stream:{stream} conn:{connection} exp:{exception} completedRequest:{completedRequest}");
                while (true)
                {
                    if (exception != null)
                    {
                        if (AttemptedRecovery(exception))
                        {
                            connection = CreateConnection();
                            if (connection == null)
                                return;

                            exception = null;
                        }
                        if (exception != null)
                        {
                            SetException(exception);
                            break;
                        }
                    }

                    if (connection != null)
                    {
                        lock (_syncObject)
                        {
                            if (_aborted)
                            {
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Releasing connect:{connection}");
                                connection.CloseSocket();
                                break;
                            }
                            _connection = connection;
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, _connection);
                        }

                        try
                        {
                            stream = (FtpDataStream?)TimedSubmitRequestHelper(true);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                            continue;
                        }
                        return;
                    }
                    else if (stream != null)
                    {
                        lock (_syncObject)
                        {
                            if (_aborted)
                            {
                                ((ICloseEx)stream).CloseEx(CloseExState.Abort | CloseExState.Silent);
                                break;
                            }
                            _stream = stream;
                        }

                        stream.SetSocketTimeoutOption(Timeout);
                        EnsureFtpWebResponse(null);

                        stageMode = stream.CanRead ? RequestStage.ReadReady : RequestStage.WriteReady;
                    }
                    else if (completedRequest)
                    {
                        connection = _connection;

                        if (connection != null)
                        {
                            EnsureFtpWebResponse(null);

                            // This to update response status and exit message if any.
                            // Note that the status 221 "Service closing control connection" is always suppressed.
                            _ftpWebResponse!.UpdateStatus(connection.StatusCode, connection.StatusLine, connection.ExitMessage);
                        }

                        stageMode = RequestStage.ReleaseConnection;
                    }
                    else
                    {
                        throw new InternalException();
                    }
                    break;
                }
            }
            catch (Exception exception)
            {
                SetException(exception);
            }
            finally
            {
                FinishRequestStage(stageMode);
            }
        }

        private enum RequestStage
        {
            CheckForError = 0,  // Do nothing except if there is an error then auto promote to ReleaseConnection
            RequestStarted,     // Mark this request as started
            WriteReady,         // First half is done, i.e. either writer or response stream. This is always assumed unless Started or CheckForError
            ReadReady,          // Second half is done, i.e. the read stream can be accesses.
            ReleaseConnection   // Release the control connection (request is read i.e. done-done)
        }

        //
        // Returns a previous stage
        //
        private RequestStage FinishRequestStage(RequestStage stage)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"state:{stage}");

            if (_exception != null)
                stage = RequestStage.ReleaseConnection;

            RequestStage prev;
            LazyAsyncResult? writeResult;
            LazyAsyncResult? readResult;
            FtpControlStream? connection;

            lock (_syncObject)
            {
                prev = _requestStage;

                if (stage == RequestStage.CheckForError)
                    return prev;

                if (prev == RequestStage.ReleaseConnection &&
                    stage == RequestStage.ReleaseConnection)
                {
                    return RequestStage.ReleaseConnection;
                }

                if (stage > prev)
                    _requestStage = stage;

                if (stage <= RequestStage.RequestStarted)
                    return prev;

                writeResult = _writeAsyncResult;
                readResult = _readAsyncResult;
                connection = _connection;

                if (stage == RequestStage.ReleaseConnection)
                {
                    if (_exception == null &&
                        !_aborted &&
                        prev != RequestStage.ReadReady &&
                        _methodInfo.IsDownload &&
                        !_ftpWebResponse!.IsFromCache)
                    {
                        return prev;
                    }

                    _connection = null;
                }
            }

            try
            {
                // First check to see on releasing the connection
                if ((stage == RequestStage.ReleaseConnection ||
                     prev == RequestStage.ReleaseConnection)
                    && connection != null)
                {
                    try
                    {
                        if (_exception != null)
                        {
                            connection.Abort(_exception);
                        }
                    }
                    finally
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Releasing connection: {connection}");
                        connection.CloseSocket();
                        if (_async)
                            if (_requestCompleteAsyncResult != null)
                                _requestCompleteAsyncResult.InvokeCallback();
                    }
                }
                return prev;
            }
            finally
            {
                try
                {
                    // In any case we want to signal the writer if came here
                    if (stage >= RequestStage.WriteReady)
                    {
                        // If writeResult == null and this is an upload request, it means
                        // that the user has called GetResponse() without calling
                        // GetRequestStream() first. So they are not interested in a
                        // stream. Therefore we close the stream so that the
                        // request/pipeline can continue
                        if (_methodInfo.IsUpload && !_getRequestStreamStarted)
                        {
                            if (_stream != null)
                                _stream.Close();
                        }
                        else if (writeResult != null && !writeResult.InternalPeekCompleted)
                            writeResult.InvokeCallback();
                    }
                }
                finally
                {
                    // The response is ready either with or without a stream
                    if (stage >= RequestStage.ReadReady && readResult != null && !readResult.InternalPeekCompleted)
                        readResult.InvokeCallback();
                }
            }
        }

        /// <summary>
        /// <para>Aborts underlying connection to FTP server (command &amp; data)</para>
        /// </summary>
        public override void Abort()
        {
            if (_aborted)
                return;


            try
            {
                Stream? stream;
                FtpControlStream? connection;
                lock (_syncObject)
                {
                    if (_requestStage >= RequestStage.ReleaseConnection)
                        return;
                    _aborted = true;
                    stream = _stream;
                    connection = _connection;
                    _exception = ExceptionHelper.RequestAbortedException;
                }

                if (stream != null)
                {
                    Debug.Assert(stream is ICloseEx, "The _stream member is not CloseEx hence the risk of connection been orphaned.");
                    ((ICloseEx)stream).CloseEx(CloseExState.Abort | CloseExState.Silent);
                }

                connection?.Abort(ExceptionHelper.RequestAbortedException);
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, exception);
                throw;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return true;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                // We don't support connection pooling, so just silently ignore this.
            }
        }

        public override RequestCachePolicy? CachePolicy
        {
            get
            {
                return FtpWebRequest.DefaultCachePolicy;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                // We don't support caching, so just silently ignore this.
            }
        }

        /// <summary>
        /// <para>True by default, false allows transmission using text mode</para>
        /// </summary>
        public bool UseBinary
        {
            get
            {
                return _binary;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                _binary = value;
            }
        }

        public bool UsePassive
        {
            get
            {
                return _passive;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                _passive = value;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _clientCertificates, ref _syncObject, () => new X509CertificateCollection());
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _clientCertificates = value;
            }
        }

        /// <summary>
        ///    <para>Set to true if we need SSL</para>
        /// </summary>
        public bool EnableSsl
        {
            get
            {
                return _enableSsl;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                _enableSsl = value;
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                if (_ftpRequestHeaders == null)
                {
                    _ftpRequestHeaders = new WebHeaderCollection();
                }
                return _ftpRequestHeaders;
            }
            set
            {
                _ftpRequestHeaders = value;
            }
        }

        // NOT SUPPORTED method
        public override string? ContentType
        {
            get
            {
                throw ExceptionHelper.PropertyNotSupportedException;
            }
            set
            {
                throw ExceptionHelper.PropertyNotSupportedException;
            }
        }

        // NOT SUPPORTED method
        public override bool UseDefaultCredentials
        {
            get
            {
                throw ExceptionHelper.PropertyNotSupportedException;
            }
            set
            {
                throw ExceptionHelper.PropertyNotSupportedException;
            }
        }

        // NOT SUPPORTED method
        public override bool PreAuthenticate
        {
            get
            {
                throw ExceptionHelper.PropertyNotSupportedException;
            }
            set
            {
                throw ExceptionHelper.PropertyNotSupportedException;
            }
        }

        /// <summary>
        ///    <para>True if a request has been submitted (ie already active)</para>
        /// </summary>
        private bool InUse
        {
            get
            {
                if (_getRequestStreamStarted || _getResponseStarted)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///    <para>Creates an FTP WebResponse based off the responseStream and our active Connection</para>
        /// </summary>
        private void EnsureFtpWebResponse(Exception? exception)
        {
            if (_ftpWebResponse == null || (_ftpWebResponse.GetResponseStream() is FtpWebResponse.EmptyStream && _stream != null))
            {
                lock (_syncObject)
                {
                    if (_ftpWebResponse == null || (_ftpWebResponse.GetResponseStream() is FtpWebResponse.EmptyStream && _stream != null))
                    {
                        Stream? responseStream = _stream;

                        if (_methodInfo.IsUpload)
                        {
                            responseStream = null;
                        }

                        if (_stream != null && _stream.CanRead && _stream.CanTimeout)
                        {
                            _stream.ReadTimeout = ReadWriteTimeout;
                            _stream.WriteTimeout = ReadWriteTimeout;
                        }

                        FtpControlStream? connection = _connection;
                        long contentLength = connection != null ? connection.ContentLength : -1;

                        if (responseStream == null)
                        {
                            // If the last command was SIZE, we set the ContentLength on
                            // the FtpControlStream to be the size of the file returned in the
                            // response. We should propagate that file size to the response so
                            // users can access it. This also maintains the compatibility with
                            // HTTP when returning size instead of content.
                            if (contentLength < 0)
                                contentLength = 0;
                        }

                        if (_ftpWebResponse != null)
                        {
                            _ftpWebResponse.SetResponseStream(responseStream);
                        }
                        else
                        {
                            if (connection != null)
                                _ftpWebResponse = new FtpWebResponse(responseStream, contentLength, connection.ResponseUri, connection.StatusCode, connection.StatusLine, connection.LastModified, connection.BannerMessage, connection.WelcomeMessage, connection.ExitMessage);
                            else
                                _ftpWebResponse = new FtpWebResponse(responseStream, -1, _uri, FtpStatusCode.Undefined, null, DateTime.Now, null, null, null);
                        }
                    }
                }
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Returns {_ftpWebResponse} with stream {_ftpWebResponse._responseStream}");

            return;
        }

        internal void DataStreamClosed(CloseExState closeState)
        {
            if ((closeState & CloseExState.Abort) == 0)
            {
                if (!_async)
                {
                    if (_connection != null)
                        _connection.CheckContinuePipeline();
                }
                else
                {
                    _requestCompleteAsyncResult!.InternalWaitForCompletion();
                    CheckError();
                }
            }
            else
            {
                FtpControlStream? connection = _connection;
                if (connection != null)
                    connection.Abort(ExceptionHelper.RequestAbortedException);
            }
        }
    }  // class FtpWebRequest

    //
    // Class used by the WebRequest.Create factory to create FTP requests
    //
    internal sealed class FtpWebRequestCreator : IWebRequestCreate
    {
        internal FtpWebRequestCreator()
        {
        }
        public WebRequest Create(Uri uri)
        {
            return new FtpWebRequest(uri);
        }
    } // class FtpWebRequestCreator
} // namespace System.Net
