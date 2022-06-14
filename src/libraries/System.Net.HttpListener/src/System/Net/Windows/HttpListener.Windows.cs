// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace System.Net
{
    public sealed unsafe partial class HttpListener
    {
        public static bool IsSupported => Interop.HttpApi.s_supported;

        // Windows 8 fixed a bug in Http.sys's HttpReceiveClientCertificate method.
        // Without this fix IOCP callbacks were not being called although ERROR_IO_PENDING was
        // returned from HttpReceiveClientCertificate when using the
        // FileCompletionNotificationModes.SkipCompletionPortOnSuccess flag.
        // This bug was only hit when the buffer passed into HttpReceiveClientCertificate
        // (1500 bytes initially) is tool small for the certificate.
        // Due to this bug in downlevel operating systems the FileCompletionNotificationModes.SkipCompletionPortOnSuccess
        // flag is only used on Win8 and later.
        internal static readonly bool SkipIOCPCallbackOnSuccess = Environment.OSVersion.Version >= new Version(6, 2);

        // Mitigate potential DOS attacks by limiting the number of unknown headers we accept.  Numerous header names
        // with hash collisions will cause the server to consume excess CPU.  1000 headers limits CPU time to under
        // 0.5 seconds per request.  Respond with a 400 Bad Request.
        private const int UnknownHeaderLimit = 1000;

        private static readonly byte[] s_wwwAuthenticateBytes = new byte[]
        {
            (byte) 'W', (byte) 'W', (byte) 'W', (byte) '-', (byte) 'A', (byte) 'u', (byte) 't', (byte) 'h',
            (byte) 'e', (byte) 'n', (byte) 't', (byte) 'i', (byte) 'c', (byte) 'a', (byte) 't', (byte) 'e'
        };

        private HttpListenerSession? _currentSession;

        private bool _unsafeConnectionNtlmAuthentication;

        private HttpServerSessionHandle? _serverSessionHandle;
        private ulong _urlGroupId;

        private bool _V2Initialized;
        private Dictionary<ulong, DisconnectAsyncResult>? _disconnectResults;

        private void ValidateV2Property()
        {
            // Make sure that calling CheckDisposed and SetupV2Config is an atomic operation. This
            // avoids race conditions if the listener is aborted/closed after CheckDisposed(), but
            // before SetupV2Config().
            lock (_internalLock)
            {
                CheckDisposed();
                SetupV2Config();
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get => _unsafeConnectionNtlmAuthentication;
            set
            {
                CheckDisposed();
                if (_unsafeConnectionNtlmAuthentication == value)
                {
                    return;
                }
                lock ((DisconnectResults as ICollection).SyncRoot)
                {
                    if (_unsafeConnectionNtlmAuthentication == value)
                    {
                        return;
                    }
                    _unsafeConnectionNtlmAuthentication = value;
                    if (!value)
                    {
                        foreach (DisconnectAsyncResult result in DisconnectResults.Values)
                        {
                            result.AuthenticatedConnection = null;
                        }
                    }
                }
            }
        }

        private Dictionary<ulong, DisconnectAsyncResult> DisconnectResults =>
            LazyInitializer.EnsureInitialized(ref _disconnectResults, () => new Dictionary<ulong, DisconnectAsyncResult>());

        private void SetUrlGroupProperty(Interop.HttpApi.HTTP_SERVER_PROPERTY property, IntPtr info, uint infosize)
        {
            Debug.Assert(_urlGroupId != 0, "SetUrlGroupProperty called with invalid url group id");
            Debug.Assert(info != IntPtr.Zero, "SetUrlGroupProperty called with invalid pointer");

            //
            // Set the url group property using Http Api.
            //
            uint statusCode = Interop.HttpApi.HttpSetUrlGroupProperty(
                _urlGroupId, property, info, infosize);

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                HttpListenerException exception = new HttpListenerException((int)statusCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"HttpSetUrlGroupProperty:: Property: {property} {exception}");
                throw exception;
            }
        }

        internal void SetServerTimeout(int[] timeouts, uint minSendBytesPerSecond)
        {
            ValidateV2Property(); // CheckDispose and initilize HttpListener in the case of app.config timeouts

            Interop.HttpApi.HTTP_TIMEOUT_LIMIT_INFO timeoutinfo = default;

            timeoutinfo.Flags = Interop.HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            timeoutinfo.DrainEntityBody =
                (ushort)timeouts[(int)Interop.HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody];
            timeoutinfo.EntityBody =
                (ushort)timeouts[(int)Interop.HttpApi.HTTP_TIMEOUT_TYPE.EntityBody];
            timeoutinfo.RequestQueue =
                (ushort)timeouts[(int)Interop.HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue];
            timeoutinfo.IdleConnection =
                (ushort)timeouts[(int)Interop.HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection];
            timeoutinfo.HeaderWait =
                (ushort)timeouts[(int)Interop.HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait];
            timeoutinfo.MinSendRate = minSendBytesPerSecond;

            IntPtr infoptr = new IntPtr(&timeoutinfo);

            SetUrlGroupProperty(
                Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerTimeoutsProperty,
                infoptr, (uint)Marshal.SizeOf(typeof(Interop.HttpApi.HTTP_TIMEOUT_LIMIT_INFO)));
        }

        public HttpListenerTimeoutManager TimeoutManager
        {
            get
            {
                ValidateV2Property();
                Debug.Assert(_timeoutManager != null, "Timeout manager is not assigned");
                return _timeoutManager;
            }
        }

        private void SetupV2Config()
        {
            ulong id = 0;

            //
            // If we have already initialized V2 config, then nothing to do.
            //
            if (_V2Initialized)
            {
                return;
            }

            //
            // V2 initialization sequence:
            // 1. Create server session
            // 2. Create url group
            // 3. Create request queue - Done in Start()
            // 4. Add urls to url group - Done in Start()
            // 5. Attach request queue to url group - Done in Start()
            //

            try
            {
                uint statusCode = Interop.HttpApi.HttpCreateServerSession(
                    Interop.HttpApi.s_version, &id, 0);

                if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                {
                    throw new HttpListenerException((int)statusCode);
                }

                Debug.Assert(id != 0, "Invalid id returned by HttpCreateServerSession");

                _serverSessionHandle = new HttpServerSessionHandle(id);

                id = 0;
                statusCode = Interop.HttpApi.HttpCreateUrlGroup(
                    _serverSessionHandle.DangerousGetServerSessionId(), &id, 0);

                if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                {
                    throw new HttpListenerException((int)statusCode);
                }

                Debug.Assert(id != 0, "Invalid id returned by HttpCreateUrlGroup");
                _urlGroupId = id;

                _V2Initialized = true;
            }
            catch (Exception exception)
            {
                //
                // If V2 initialization fails, we mark object as unusable.
                //
                _state = State.Closed;

                //
                // If Url group or request queue creation failed, close server session before throwing.
                //
                _serverSessionHandle?.Dispose();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"SetupV2Config {exception}");
                throw;
            }
        }

        public void Start()
        {
            // Make sure there are no race conditions between Start/Stop/Abort/Close/Dispose and
            // calls to SetupV2Config: Start needs to setup all resources (esp. in V2 where besides
            // the request handle, there is also a server session and a Url group. Abort/Stop must
            // not interfere while Start is allocating those resources. The lock also makes sure
            // all methods changing state can read and change the state in an atomic way.
            lock (_internalLock)
            {
                try
                {
                    CheckDisposed();
                    if (_state == State.Started)
                    {
                        return;
                    }

                    Debug.Assert(_currentSession is null);

                    // SetupV2Config() is not called in the ctor, because it may throw. This would
                    // be a regression since in v1 the ctor never threw. Besides, ctors should do
                    // minimal work according to the framework design guidelines.
                    SetupV2Config();
                    CreateRequestQueueHandle();
                    AttachRequestQueueToUrlGroup();

                    // All resources are set up correctly. Now add all prefixes.
                    try
                    {
                        AddAllPrefixes();
                    }
                    catch (HttpListenerException)
                    {
                        // If an error occurred while adding prefixes, free all resources allocated by previous steps.
                        DetachRequestQueueFromUrlGroup();
                        throw;
                    }

                    _state = State.Started;
                }
                catch (Exception exception)
                {
                    // Make sure the HttpListener instance can't be used if Start() failed.
                    _state = State.Closed;
                    CloseRequestQueueHandle();
                    CleanupV2Config();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Start {exception}");
                    throw;
                }
            }
        }

        private void CleanupV2Config()
        {
            //
            // If we never setup V2, just return.
            //
            if (!_V2Initialized)
            {
                return;
            }

            //
            // V2 stopping sequence:
            // 1. Detach request queue from url group - Done in Stop()/Abort()
            // 2. Remove urls from url group - Done in Stop()
            // 3. Close request queue - Done in Stop()/Abort()
            // 4. Close Url group.
            // 5. Close server session.

            Debug.Assert(_urlGroupId != 0, "HttpCloseUrlGroup called with invalid url group id");

            uint statusCode = Interop.HttpApi.HttpCloseUrlGroup(_urlGroupId);

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"CloseV2Config {SR.Format(SR.net_listener_close_urlgroup_error, statusCode)}");
            }
            _urlGroupId = 0;

            Debug.Assert(_serverSessionHandle != null, "ServerSessionHandle is null in CloseV2Config");
            Debug.Assert(!_serverSessionHandle.IsInvalid, "ServerSessionHandle is invalid in CloseV2Config");

            _serverSessionHandle.Dispose();
        }

        private void AttachRequestQueueToUrlGroup()
        {
            Debug.Assert(Monitor.IsEntered(_internalLock));

            // Set the association between request queue and url group. After this, requests for registered urls will
            // get delivered to this request queue.
            Interop.HttpApi.HTTP_BINDING_INFO info = default;
            info.Flags = Interop.HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            info.RequestQueueHandle = _currentSession!.RequestQueueHandle.DangerousGetHandle();

            IntPtr infoptr = new IntPtr(&info);

            SetUrlGroupProperty(Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
                infoptr, (uint)Marshal.SizeOf(typeof(Interop.HttpApi.HTTP_BINDING_INFO)));
        }

        private void DetachRequestQueueFromUrlGroup()
        {
            Debug.Assert(_urlGroupId != 0, "DetachRequestQueueFromUrlGroup can't detach using Url group id 0.");

            //
            // Break the association between request queue and url group. After this, requests for registered urls
            // will get 503s.
            // Note that this method may be called multiple times (Stop() and then Abort()). This
            // is fine since http.sys allows to set HttpServerBindingProperty multiple times for valid
            // Url groups.
            //
            Interop.HttpApi.HTTP_BINDING_INFO info = default;
            info.Flags = Interop.HttpApi.HTTP_FLAGS.NONE;
            info.RequestQueueHandle = IntPtr.Zero;

            IntPtr infoptr = new IntPtr(&info);

            uint statusCode = Interop.HttpApi.HttpSetUrlGroupProperty(_urlGroupId,
                Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
                infoptr, (uint)Marshal.SizeOf(typeof(Interop.HttpApi.HTTP_BINDING_INFO)));

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"DetachRequestQueueFromUrlGroup {SR.Format(SR.net_listener_detach_error, statusCode)}");
            }
        }

        public void Stop()
        {
            try
            {
                lock (_internalLock)
                {
                    CheckDisposed();
                    if (_state == State.Stopped)
                    {
                        return;
                    }

                    RemoveAll(false);
                    DetachRequestQueueFromUrlGroup();

                    // Even though it would be enough to just detach the request queue in v2, in order to
                    // keep app compat with earlier versions of the framework, we need to close the request queue.
                    // This will make sure that pending GetContext() calls will complete and throw an exception. Just
                    // detaching the url group from the request queue would not cause GetContext() to return.
                    CloseRequestQueueHandle();

                    _state = State.Stopped;
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Stop {exception}");
                throw;
            }
        }

        private unsafe void CreateRequestQueueHandle()
        {
            Debug.Assert(Monitor.IsEntered(_internalLock));
            Debug.Assert(_currentSession is null);

            _currentSession = new HttpListenerSession(this);
        }

        private unsafe void CloseRequestQueueHandle()
        {
            Debug.Assert(Monitor.IsEntered(_internalLock));

            _currentSession?.CloseRequestQueueHandle();
            _currentSession = null;
        }

        public void Abort()
        {
            lock (_internalLock)
            {
                try
                {
                    if (_state == State.Closed)
                    {
                        return;
                    }

                    // Just detach and free resources. Don't call Stop (which may throw). Behave like v1: just
                    // clean up resources.
                    if (_state == State.Started)
                    {
                        DetachRequestQueueFromUrlGroup();
                        CloseRequestQueueHandle();
                    }
                    CleanupV2Config();
                }
                catch (Exception exception)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Abort {exception}");
                    throw;
                }
                finally
                {
                    _state = State.Closed;
                }
            }
        }

        private void Dispose()
        {
            lock (_internalLock)
            {
                try
                {
                    if (_state == State.Closed)
                    {
                        return;
                    }

                    Stop();
                    CleanupV2Config();
                }
                catch (Exception exception)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Dispose {exception}");
                    throw;
                }
                finally
                {
                    _state = State.Closed;
                }
            }
        }

        private void RemovePrefixCore(string uriPrefix)
        {
            Interop.HttpApi.HttpRemoveUrlFromUrlGroup(_urlGroupId, uriPrefix, 0);
        }

        private void AddAllPrefixes()
        {
            // go through the uri list and register for each one of them
            if (_uriPrefixes.Count > 0)
            {
                foreach (string registeredPrefix in _uriPrefixes.Values)
                {
                    AddPrefixCore(registeredPrefix);
                }
            }
        }

        private void AddPrefixCore(string registeredPrefix)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpAddUrl[ToUrlGroup]");

            uint statusCode = Interop.HttpApi.HttpAddUrlToUrlGroup(
                                  _urlGroupId,
                                  registeredPrefix,
                                  0,
                                  0);
            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                if (statusCode == Interop.HttpApi.ERROR_ALREADY_EXISTS)
                    throw new HttpListenerException((int)statusCode, SR.Format(SR.net_listener_already, registeredPrefix));
                else
                    throw new HttpListenerException((int)statusCode);
            }
        }

        public HttpListenerContext GetContext()
        {
            SyncRequestContext? memoryBlob = null;
            HttpListenerContext? httpContext = null;
            bool stoleBlob = false;

            try
            {
                CheckDisposed();
                if (_state == State.Stopped)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "Start()"));
                }
                if (_uriPrefixes.Count == 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "AddPrefix()"));
                }
                uint statusCode = Interop.HttpApi.ERROR_SUCCESS;
                uint size = 4096;
                ulong requestId = 0;
                memoryBlob = new SyncRequestContext((int)size);
                HttpListenerSession? session = _currentSession;

                // Because there is no synchronization, the listener can be stopped or closed while the method is executing,
                // resulting in a null session
                if (session == null)
                {
                    throw new HttpListenerException((int)Interop.HttpApi.ERROR_INVALID_PARAMETER);
                }

                while (true)
                {
                    while (true)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Calling Interop.HttpApi.HttpReceiveHttpRequest RequestId: {requestId}");
                        uint bytesTransferred = 0;
                        statusCode =
                            Interop.HttpApi.HttpReceiveHttpRequest(
                                session.RequestQueueHandle,
                                requestId,
                                (uint)Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                                memoryBlob.RequestBlob,
                                size,
                                &bytesTransferred,
                                null);

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveHttpRequest returned:" + statusCode);

                        if (statusCode == Interop.HttpApi.ERROR_INVALID_PARAMETER && requestId != 0)
                        {
                            // we might get this if somebody stole our RequestId,
                            // we need to start all over again but we can reuse the buffer we just allocated
                            requestId = 0;
                            continue;
                        }
                        else if (statusCode == Interop.HttpApi.ERROR_MORE_DATA)
                        {
                            // the buffer was not big enough to fit the headers, we need
                            // to read the RequestId returned, allocate a new buffer of the required size
                            size = bytesTransferred;
                            requestId = memoryBlob.RequestBlob->RequestId;
                            memoryBlob.Reset(checked((int)size));
                            continue;
                        }
                        break;
                    }
                    if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                    {
                        // someother bad error, return values are:
                        // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                        throw new HttpListenerException((int)statusCode);
                    }

                    if (ValidateRequest(session, memoryBlob))
                    {
                        // We need to hook up our authentication handling code here.
                        httpContext = HandleAuthentication(session, memoryBlob, out stoleBlob);
                    }

                    if (stoleBlob)
                    {
                        // The request has been handed to the user, which means this code can't reuse the blob.  Reset it here.
                        memoryBlob = null;
                        stoleBlob = false;
                    }
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, ":HandleAuthentication() returned httpContext" + httpContext);
                    // if the request survived authentication, return it to the user
                    if (httpContext != null)
                    {
                        return httpContext;
                    }

                    // HandleAuthentication may have cleaned this up.
                    if (memoryBlob == null)
                    {
                        memoryBlob = new SyncRequestContext(checked((int)size));
                    }

                    requestId = 0;
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"{exception}");
                throw;
            }
            finally
            {
                if (memoryBlob != null && !stoleBlob)
                {
                    memoryBlob.ReleasePins();
                    memoryBlob.Close();
                }
            }
        }

        internal static unsafe bool ValidateRequest(HttpListenerSession session, RequestContextBase requestMemory)
        {
            // Block potential DOS attacks
            if (requestMemory.RequestBlob->Headers.UnknownHeaderCount > UnknownHeaderLimit)
            {
                SendError(session, requestMemory.RequestBlob->RequestId, HttpStatusCode.BadRequest, null);
                return false;
            }
            return true;
        }

        public IAsyncResult BeginGetContext(AsyncCallback? callback, object? state)
        {
            ListenerAsyncResult? asyncResult = null;
            try
            {
                CheckDisposed();
                if (_state == State.Stopped)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "Start()"));
                }

                HttpListenerSession? session = _currentSession;

                // Because there is no synchronization, the listener can be stopped or closed while the method is executing,
                // resulting in a null session
                if (session == null)
                {
                    throw new HttpListenerException((int)Interop.HttpApi.ERROR_INVALID_PARAMETER);
                }

                // prepare the ListenerAsyncResult object (this will have it's own
                // event that the user can wait on for IO completion - which means we
                // need to signal it when IO completes)
                asyncResult = new ListenerAsyncResult(session, state, callback);
                uint statusCode = asyncResult.QueueBeginGetContext();
                if (statusCode != Interop.HttpApi.ERROR_SUCCESS &&
                    statusCode != Interop.HttpApi.ERROR_IO_PENDING)
                {
                    // someother bad error, return values are:
                    // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                    throw new HttpListenerException((int)statusCode);
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"BeginGetContext {exception}");
                throw;
            }

            return asyncResult;
        }

        public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
        {
            HttpListenerContext? httpContext = null;
            try
            {
                CheckDisposed();
                ArgumentNullException.ThrowIfNull(asyncResult);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"asyncResult: {asyncResult}");
                if (!(asyncResult is ListenerAsyncResult castedAsyncResult) || !(castedAsyncResult.AsyncObject is HttpListenerSession session) || session.Listener != this)
                {
                    throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
                }
                if (castedAsyncResult.EndCalled)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndGetContext)));
                }
                castedAsyncResult.EndCalled = true;
                httpContext = castedAsyncResult.InternalWaitForCompletion() as HttpListenerContext;
                if (httpContext == null)
                {
                    Debug.Assert(castedAsyncResult.Result is Exception, "EndGetContext|The result is neither a HttpListenerContext nor an Exception.");
                    ExceptionDispatchInfo.Throw((castedAsyncResult.Result as Exception)!);
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"EndGetContext {exception}");
                throw;
            }
            return httpContext;
        }

        internal HttpListenerContext? HandleAuthentication(HttpListenerSession session, RequestContextBase memoryBlob, out bool stoleBlob)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "HandleAuthentication() memoryBlob:0x" + ((IntPtr)memoryBlob.RequestBlob).ToString("x"));

            string? challenge = null;
            stoleBlob = false;

            // Some things we need right away.  Lift them out now while it's convenient.
            string? authorizationHeader = Interop.HttpApi.GetKnownHeader(memoryBlob.RequestBlob, (int)HttpRequestHeader.Authorization);
            ulong connectionId = memoryBlob.RequestBlob->ConnectionId;
            ulong requestId = memoryBlob.RequestBlob->RequestId;
            bool isSecureConnection = memoryBlob.RequestBlob->pSslInfo != null;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"HandleAuthentication() authorizationHeader: ({authorizationHeader})");

            // if the app has turned on AuthPersistence, an anonymous request might
            // be authenticated by virtue of it coming on a connection that was
            // previously authenticated.
            // assurance that we do this only for NTLM/Negotiate is not here, but in the
            // code that caches WindowsIdentity instances in the Dictionary.
            DisconnectAsyncResult? disconnectResult;
            DisconnectResults.TryGetValue(connectionId, out disconnectResult);
            if (UnsafeConnectionNtlmAuthentication)
            {
                if (authorizationHeader == null)
                {
                    WindowsPrincipal? principal = disconnectResult?.AuthenticatedConnection;
                    if (principal != null)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Principal: {principal} principal.Identity.Name: {principal.Identity.Name} creating request");
                        stoleBlob = true;
                        HttpListenerContext ntlmContext = new HttpListenerContext(session, memoryBlob);
                        ntlmContext.SetIdentity(principal, null);
                        ntlmContext.Request.ReleasePins();
                        return ntlmContext;
                    }
                }
                else
                {
                    // They sent an authorization - destroy their previous credentials.
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Clearing principal cache");
                    if (disconnectResult != null)
                    {
                        disconnectResult.AuthenticatedConnection = null;
                    }
                }
            }

            // Figure out what schemes we're allowing, what context we have.
            stoleBlob = true;
            HttpListenerContext? httpContext = null;
            NTAuthentication? oldContext = null;
            NTAuthentication? newContext = null;
            NTAuthentication? context = null;
            AuthenticationSchemes headerScheme = AuthenticationSchemes.None;
            AuthenticationSchemes authenticationScheme = AuthenticationSchemes;
            ExtendedProtectionPolicy extendedProtectionPolicy = _extendedProtectionPolicy;
            try
            {
                // Take over handling disconnects for now.
                if (disconnectResult != null && !disconnectResult.StartOwningDisconnectHandling())
                {
                    // Just disconnected just then.  Pretend we didn't see the disconnectResult.
                    disconnectResult = null;
                }

                // Pick out the old context now.  By default, it'll be removed in the finally, unless context is set somewhere.
                if (disconnectResult != null)
                {
                    oldContext = disconnectResult.Session;
                }

                httpContext = new HttpListenerContext(session, memoryBlob);

                AuthenticationSchemeSelector? authenticationSelector = _authenticationDelegate;
                if (authenticationSelector != null)
                {
                    try
                    {
                        httpContext.Request.ReleasePins();
                        authenticationScheme = authenticationSelector(httpContext.Request);
                        // Cache the results of authenticationSelector (if any)
                        httpContext.AuthenticationSchemes = authenticationScheme;
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"AuthenticationScheme: {authenticationScheme}");
                    }
                    catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Error(this, SR.Format(SR.net_log_listener_delegate_exception, exception));
                            NetEventSource.Info(this, $"authenticationScheme: {authenticationScheme}");
                        }
                        SendError(session, requestId, HttpStatusCode.InternalServerError, null);
                        FreeContext(ref httpContext, memoryBlob);
                        return null;
                    }
                }
                else
                {
                    // We didn't give the request to the user yet, so we haven't lost control of the unmanaged blob and can
                    // continue to reuse the buffer.
                    stoleBlob = false;
                }

                ExtendedProtectionSelector? extendedProtectionSelector = _extendedProtectionSelectorDelegate;
                if (extendedProtectionSelector != null)
                {
                    extendedProtectionPolicy = extendedProtectionSelector(httpContext.Request);

                    if (extendedProtectionPolicy == null)
                    {
                        extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
                    }
                    // Cache the results of extendedProtectionSelector (if any)
                    httpContext.ExtendedProtectionPolicy = extendedProtectionPolicy;
                }

                // Then figure out what scheme they're trying (if any are allowed)
                int index = -1;
                if (authorizationHeader != null && (authenticationScheme & ~AuthenticationSchemes.Anonymous) != AuthenticationSchemes.None)
                {
                    // Find the end of the scheme name.  Trust that HTTP.SYS parsed out just our header ok.
                    for (index = 0; index < authorizationHeader.Length; index++)
                    {
                        if (authorizationHeader[index] == ' ' || authorizationHeader[index] == '\t' ||
                            authorizationHeader[index] == '\r' || authorizationHeader[index] == '\n')
                        {
                            break;
                        }
                    }

                    // Currently only allow one Authorization scheme/header per request.
                    if (index < authorizationHeader.Length)
                    {
                        if ((authenticationScheme & AuthenticationSchemes.Negotiate) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, AuthenticationTypes.Negotiate, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Negotiate;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Ntlm) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, AuthenticationTypes.NTLM, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Ntlm;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Basic) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, AuthenticationTypes.Basic, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Basic;
                        }
                        else
                        {
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, SR.Format(SR.net_log_listener_unsupported_authentication_scheme, authorizationHeader, authenticationScheme));
                        }
                    }
                }

                // httpError holds the error we will return if an Authorization header is present but can't be authenticated
                HttpStatusCode httpError = HttpStatusCode.InternalServerError;
                bool error = false;

                // See if we found an acceptable auth header
                if (headerScheme == AuthenticationSchemes.None)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, SR.Format(SR.net_log_listener_unmatched_authentication_scheme, authenticationScheme.ToString(), authorizationHeader ?? "<null>"));

                    // If anonymous is allowed, just return the context.  Otherwise go for the 401.
                    if ((authenticationScheme & AuthenticationSchemes.Anonymous) != AuthenticationSchemes.None)
                    {
                        if (!stoleBlob)
                        {
                            stoleBlob = true;
                            httpContext.Request.ReleasePins();
                        }
                        return httpContext;
                    }

                    httpError = HttpStatusCode.Unauthorized;
                    FreeContext(ref httpContext, memoryBlob);
                }
                else
                {
                    // Perform Authentication
                    byte[]? bytes = null;
                    byte[]? decodedOutgoingBlob = null;
                    string? outBlob = null;

                    // Find the beginning of the blob.  Trust that HTTP.SYS parsed out just our header ok.
                    Debug.Assert(authorizationHeader != null);
                    for (index++; index < authorizationHeader!.Length; index++)
                    {
                        if (authorizationHeader[index] != ' ' && authorizationHeader[index] != '\t' &&
                            authorizationHeader[index] != '\r' && authorizationHeader[index] != '\n')
                        {
                            break;
                        }
                    }
                    string inBlob = index < authorizationHeader.Length ? authorizationHeader.Substring(index) : "";

                    IPrincipal? principal = null;
                    SecurityStatusPal statusCodeNew;
                    ChannelBinding? binding;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Performing Authentication headerScheme: {headerScheme}");
                    switch (headerScheme)
                    {
                        case AuthenticationSchemes.Negotiate:
                        case AuthenticationSchemes.Ntlm:
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"context: {oldContext} for connectionId: {connectionId}");

                            string package = headerScheme == AuthenticationSchemes.Ntlm ? NegotiationInfoClass.NTLM : NegotiationInfoClass.Negotiate;
                            if (oldContext != null && oldContext.Package == package)
                            {
                                context = oldContext;
                            }
                            else
                            {
                                binding = GetChannelBinding(session, connectionId, isSecureConnection, extendedProtectionPolicy);
                                ContextFlagsPal contextFlags = GetContextFlags(extendedProtectionPolicy, isSecureConnection);
                                context = new NTAuthentication(true, package, CredentialCache.DefaultNetworkCredentials, null, contextFlags, binding);
                            }

                            try
                            {
                                bytes = Convert.FromBase64String(inBlob);
                            }
                            catch (FormatException)
                            {
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"FormatException from FormBase64String");
                                httpError = HttpStatusCode.BadRequest;
                                error = true;
                            }
                            if (!error)
                            {
                                decodedOutgoingBlob = context.GetOutgoingBlob(bytes, false, out statusCodeNew);
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetOutgoingBlob returned IsCompleted: {context.IsCompleted} and statusCodeNew: {statusCodeNew}");
                                error = !context.IsValidContext;
                                if (error)
                                {
                                    // SSPI Workaround
                                    // If a client sends up a blob on the initial request, Negotiate returns SEC_E_INVALID_HANDLE
                                    // when it should return SEC_E_INVALID_TOKEN.
                                    if (statusCodeNew.ErrorCode == SecurityStatusPalErrorCode.InvalidHandle && oldContext == null && bytes != null && bytes.Length > 0)
                                    {
                                        statusCodeNew = new SecurityStatusPal(SecurityStatusPalErrorCode.InvalidToken);
                                    }

                                    httpError = HttpStatusFromSecurityStatus(statusCodeNew.ErrorCode);
                                }
                            }

                            if (decodedOutgoingBlob != null)
                            {
                                // Prefix SPNEGO token/NTLM challenge with scheme per RFC 4559, MS-NTHT
                                outBlob = $"{(headerScheme == AuthenticationSchemes.Ntlm ? NegotiationInfoClass.NTLM : NegotiationInfoClass.Negotiate)} {Convert.ToBase64String(decodedOutgoingBlob)}";
                            }

                            if (!error)
                            {
                                if (context.IsCompleted)
                                {
                                    SecurityContextTokenHandle? userContext = null;
                                    try
                                    {
                                        if (!CheckSpn(context, isSecureConnection, extendedProtectionPolicy))
                                        {
                                            httpError = HttpStatusCode.Unauthorized;
                                        }
                                        else
                                        {
                                            httpContext.Request.ServiceName = context.ClientSpecifiedSpn;

                                            SafeDeleteContext securityContext = context.GetContext(out statusCodeNew)!;
                                            if (statusCodeNew.ErrorCode != SecurityStatusPalErrorCode.OK)
                                            {
                                                if (NetEventSource.Log.IsEnabled())
                                                {
                                                    NetEventSource.Info(this,
                                                        $"HandleAuthentication GetContextToken failed with statusCodeNew: {statusCodeNew}");
                                                }

                                                httpError = HttpStatusFromSecurityStatus(statusCodeNew.ErrorCode);
                                            }
                                            else
                                            {
                                                SSPIWrapper.QuerySecurityContextToken(GlobalSSPI.SSPIAuth, securityContext, out userContext);

                                                if (NetEventSource.Log.IsEnabled())
                                                {
                                                    NetEventSource.Info(this,
                                                        $"HandleAuthentication creating new WindowsIdentity from user context: {userContext.DangerousGetHandle():x8}");
                                                }

                                                WindowsPrincipal windowsPrincipal = new WindowsPrincipal(
                                                    new WindowsIdentity(userContext.DangerousGetHandle(), context.ProtocolName));

                                                principal = windowsPrincipal;
                                                // if appropriate, cache this credential on this connection
                                                if (UnsafeConnectionNtlmAuthentication && context.ProtocolName == NegotiationInfoClass.NTLM)
                                                {
                                                    if (NetEventSource.Log.IsEnabled())
                                                    {
                                                        NetEventSource.Info(this,
                                                            $"HandleAuthentication inserting principal: {principal} for connectionId: {connectionId}");
                                                    }

                                                    // We may need to call WaitForDisconnect.
                                                    if (disconnectResult == null)
                                                    {
                                                        RegisterForDisconnectNotification(session, connectionId, ref disconnectResult);
                                                    }
                                                    if (disconnectResult != null)
                                                    {
                                                        lock ((DisconnectResults as ICollection).SyncRoot)
                                                        {
                                                            if (UnsafeConnectionNtlmAuthentication)
                                                            {
                                                                disconnectResult.AuthenticatedConnection = windowsPrincipal;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // Registration failed - UnsafeConnectionNtlmAuthentication ignored.
                                                        if (NetEventSource.Log.IsEnabled())
                                                        {
                                                            NetEventSource.Info(this, $"HandleAuthentication RegisterForDisconnectNotification failed.");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (userContext != null)
                                        {
                                            userContext.Dispose();
                                        }
                                    }
                                }
                                else
                                {
                                    // auth incomplete
                                    newContext = context;
                                    challenge = string.IsNullOrEmpty(outBlob)
                                        ? headerScheme == AuthenticationSchemes.Ntlm ? NegotiationInfoClass.NTLM : NegotiationInfoClass.Negotiate
                                        : outBlob;
                                }
                            }
                            break;

                        case AuthenticationSchemes.Basic:
                            try
                            {
                                bytes = Convert.FromBase64String(inBlob);

                                inBlob = WebHeaderEncoding.GetString(bytes, 0, bytes.Length);
                                index = inBlob.IndexOf(':');

                                if (index != -1)
                                {
                                    string userName = inBlob.Substring(0, index);
                                    string password = inBlob.Substring(index + 1);
                                    if (NetEventSource.Log.IsEnabled())
                                    {
                                        NetEventSource.Info(this, $"Basic Identity found, userName: {userName}");
                                    }

                                    principal = new GenericPrincipal(new HttpListenerBasicIdentity(userName, password), null);
                                }
                                else
                                {
                                    httpError = HttpStatusCode.BadRequest;
                                }
                            }
                            catch (FormatException)
                            {
                                if (NetEventSource.Log.IsEnabled())
                                {
                                    NetEventSource.Info(this, $"FromBase64String threw a FormatException.");
                                }
                            }
                            break;
                    }

                    if (principal != null)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(this, $"Got principal: {principal}, IdentityName: {principal!.Identity!.Name} for creating request.");
                        }

                        httpContext.SetIdentity(principal, outBlob);
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(this, "Handshake has failed.");
                        }

                        FreeContext(ref httpContext, memoryBlob);
                    }
                }

                // if we're not giving a request to the application, we need to send an error
                ArrayList? challenges = null;
                if (httpContext == null)
                {
                    // If we already have a challenge, just use it.  Otherwise put a challenge for each acceptable scheme.
                    if (challenge != null)
                    {
                        AddChallenge(ref challenges, challenge);
                    }
                    else
                    {
                        // We're starting over.  Any context SSPI might have wanted us to keep is useless.
                        if (newContext != null)
                        {
                            if (newContext == context)
                            {
                                context = null;
                            }

                            if (newContext != oldContext)
                            {
                                NTAuthentication toClose = newContext;
                                newContext = null;
                                toClose.CloseContext();
                            }
                            else
                            {
                                newContext = null;
                            }
                        }

                        // If we're sending something besides 401, do it here.
                        if (httpError != HttpStatusCode.Unauthorized)
                        {
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "ConnectionId:" + connectionId + " because of error:" + httpError.ToString());
                            SendError(session, requestId, httpError, null);
                            return null;
                        }

                        challenges = BuildChallenge(authenticationScheme, connectionId, out newContext,
                            extendedProtectionPolicy, isSecureConnection);
                    }
                }

                // Check if we need to call WaitForDisconnect, because if we do and it fails, we want to send a 500 instead.
                if (disconnectResult == null && newContext != null)
                {
                    RegisterForDisconnectNotification(session, connectionId, ref disconnectResult);

                    // Failed - send 500.
                    if (disconnectResult == null)
                    {
                        if (newContext != null)
                        {
                            if (newContext == context)
                            {
                                context = null;
                            }

                            if (newContext != oldContext)
                            {
                                NTAuthentication toClose = newContext;
                                newContext = null;
                                toClose.CloseContext();
                            }
                            else
                            {
                                newContext = null;
                            }
                        }

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "connectionId:" + connectionId + " because of failed HttpWaitForDisconnect");
                        SendError(session, requestId, HttpStatusCode.InternalServerError, null);
                        FreeContext(ref httpContext, memoryBlob);
                        return null;
                    }
                }

                // Update Session if necessary.
                if (oldContext != newContext)
                {
                    if (oldContext == context)
                    {
                        // Prevent the finally from closing this twice.
                        context = null;
                    }

                    NTAuthentication? toClose = oldContext;
                    oldContext = newContext;
                    // TODO: Can disconnectResult be null here?
                    Debug.Assert(disconnectResult != null);
                    disconnectResult!.Session = newContext;

                    if (toClose != null)
                    {
                        toClose.CloseContext();
                    }
                }

                // Send the 401 here.
                if (httpContext == null)
                {
                    SendError(session, requestId, challenges != null && challenges.Count > 0 ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, challenges);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Scheme:" + authenticationScheme);
                    return null;
                }

                if (!stoleBlob)
                {
                    stoleBlob = true;
                    httpContext.Request.ReleasePins();
                }
                return httpContext;
            }
            catch
            {
                FreeContext(ref httpContext, memoryBlob);
                if (newContext != null)
                {
                    if (newContext == context)
                    {
                        // Prevent the finally from closing this twice.
                        context = null;
                    }

                    if (newContext != oldContext)
                    {
                        NTAuthentication toClose = newContext;
                        newContext = null;
                        toClose.CloseContext();
                    }
                    else
                    {
                        newContext = null;
                    }
                }
                throw;
            }
            finally
            {
                try
                {
                    // Clean up the previous context if necessary.
                    if (oldContext != null && oldContext != newContext)
                    {
                        // Clear out Session if it wasn't already.
                        if (newContext == null && disconnectResult != null)
                        {
                            disconnectResult.Session = null;
                        }

                        oldContext.CloseContext();
                    }

                    // Delete any context created but not stored.
                    if (context != null && oldContext != context && newContext != context)
                    {
                        context.CloseContext();
                    }
                }
                finally
                {
                    // Check if the connection got deleted while in this method, and clear out the hashtables if it did.
                    // In a nested finally because if this doesn't happen, we leak.
                    if (disconnectResult != null)
                    {
                        disconnectResult.FinishOwningDisconnectHandling();
                    }
                }
            }
        }

        private static void FreeContext(ref HttpListenerContext? httpContext, RequestContextBase memoryBlob)
        {
            if (httpContext != null)
            {
                httpContext.Request.DetachBlob(memoryBlob);
                httpContext.Close();
                httpContext = null;
            }
        }

        // Using the configured Auth schemes, populate the auth challenge headers. This is for scenarios where
        // Anonymous access is allowed for some resources, but the server later determines that authorization
        // is required for this request.
        internal void SetAuthenticationHeaders(HttpListenerContext context)
        {
            Debug.Assert(context != null, "Null Context");

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // We use the cached results from the delegates so that we don't have to call them again here.
            ArrayList? challenges = BuildChallenge(context.AuthenticationSchemes, request._connectionId,
                out _, context.ExtendedProtectionPolicy, request.IsSecureConnection);

            // Setting 401 without setting WWW-Authenticate is a protocol violation
            // but throwing from HttpListener would be a breaking change.
            if (challenges != null) // null == Anonymous
            {
                // Add the new WWW-Authenticate headers
                foreach (string challenge in challenges)
                {
                    response.Headers.Add(HttpKnownHeaderNames.WWWAuthenticate, challenge);
                }
            }
        }

        private ChannelBinding? GetChannelBinding(HttpListenerSession session, ulong connectionId, bool isSecureConnection, ExtendedProtectionPolicy policy)
        {
            if (policy.PolicyEnforcement == PolicyEnforcement.Never)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_cbt_disabled);
                return null;
            }

            if (!isSecureConnection)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_cbt_http);
                return null;
            }

            if (policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_no_cbt_trustedproxy);
                return null;
            }

            ChannelBinding? result = GetChannelBindingFromTls(session, connectionId);

            if (NetEventSource.Log.IsEnabled() && result != null)
                NetEventSource.Info(this, "GetChannelBindingFromTls returned null even though OS supposedly supports Extended Protection");
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_log_listener_cbt);
            return result;
        }

        private bool CheckSpn(NTAuthentication context, bool isSecureConnection, ExtendedProtectionPolicy policy)
        {
            // Kerberos does SPN check already in ASC
            if (context.IsKerberos)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, SR.net_log_listener_no_spn_kerberos);
                }
                return true;
            }

            // Don't check the SPN if Extended Protection is off or we already checked the CBT
            if (policy.PolicyEnforcement == PolicyEnforcement.Never)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, SR.net_log_listener_no_spn_disabled);
                }
                return true;
            }

            if (ScenarioChecksChannelBinding(isSecureConnection, policy.ProtectionScenario))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, SR.net_log_listener_no_spn_cbt);
                }
                return true;
            }

            string? clientSpn = context.ClientSpecifiedSpn;

            // An empty SPN is only allowed in the WhenSupported case
            if (string.IsNullOrEmpty(clientSpn))
            {
                if (policy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(this,
                            SR.net_log_listener_no_spn_whensupported);
                    }
                    return true;
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(this,
                            SR.net_log_listener_spn_failed_always);
                    }
                    return false;
                }
            }
            else if (string.Equals(clientSpn, "http/localhost", StringComparison.OrdinalIgnoreCase))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, SR.net_log_listener_no_spn_loopback);
                }

                return true;
            }
            else
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, SR.net_log_listener_spn, clientSpn);
                }

                ServiceNameCollection serviceNames = GetServiceNames(policy);

                bool found = serviceNames.Contains(clientSpn);

                if (NetEventSource.Log.IsEnabled())
                {
                    if (found)
                    {
                        NetEventSource.Info(this, SR.net_log_listener_spn_passed);
                    }
                    else
                    {
                        NetEventSource.Info(this, SR.net_log_listener_spn_failed);

                        if (serviceNames.Count == 0)
                        {
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.Info(this, SR.net_log_listener_spn_failed_empty);
                            }
                        }
                        else
                        {
                            NetEventSource.Info(this, SR.net_log_listener_spn_failed_dump);

                            foreach (string serviceName in serviceNames)
                            {
                                NetEventSource.Info(this, "\t" + serviceName);
                            }
                        }
                    }
                }

                return found;
            }
        }

        private ServiceNameCollection GetServiceNames(ExtendedProtectionPolicy policy)
        {
            ServiceNameCollection serviceNames;

            if (policy.CustomServiceNames == null)
            {
                if (_defaultServiceNames.ServiceNames.Count == 0)
                {
                    throw new InvalidOperationException(SR.net_listener_no_spns);
                }
                serviceNames = _defaultServiceNames.ServiceNames;
            }
            else
            {
                serviceNames = policy.CustomServiceNames;
            }
            return serviceNames;
        }

        private static bool ScenarioChecksChannelBinding(bool isSecureConnection, ProtectionScenario scenario)
        {
            return (isSecureConnection && scenario == ProtectionScenario.TransportSelected);
        }

        private static ContextFlagsPal GetContextFlags(ExtendedProtectionPolicy policy, bool isSecureConnection)
        {
            ContextFlagsPal result = ContextFlagsPal.Connection;
            if (policy.PolicyEnforcement != PolicyEnforcement.Never)
            {
                if (policy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    result |= ContextFlagsPal.AllowMissingBindings;
                }

                if (policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
                {
                    result |= ContextFlagsPal.ProxyBindings;
                }
            }

            return result;
        }

        // This only works for context-destroying errors.
        private static HttpStatusCode HttpStatusFromSecurityStatus(SecurityStatusPalErrorCode statusErrorCode)
        {
            if (IsCredentialFailure(statusErrorCode))
            {
                return HttpStatusCode.Unauthorized;
            }
            if (IsClientFault(statusErrorCode))
            {
                return HttpStatusCode.BadRequest;
            }
            return HttpStatusCode.InternalServerError;
        }

        // This only works for context-destroying errors.
        internal static bool IsCredentialFailure(SecurityStatusPalErrorCode error)
        {
            return error == SecurityStatusPalErrorCode.LogonDenied ||
                error == SecurityStatusPalErrorCode.UnknownCredentials ||
                error == SecurityStatusPalErrorCode.NoImpersonation ||
                error == SecurityStatusPalErrorCode.NoAuthenticatingAuthority ||
                error == SecurityStatusPalErrorCode.UntrustedRoot ||
                error == SecurityStatusPalErrorCode.CertExpired ||
                error == SecurityStatusPalErrorCode.SmartcardLogonRequired ||
                error == SecurityStatusPalErrorCode.BadBinding;
        }

        // This only works for context-destroying errors.
        internal static bool IsClientFault(SecurityStatusPalErrorCode error)
        {
            return error == SecurityStatusPalErrorCode.InvalidToken ||
                error == SecurityStatusPalErrorCode.CannotPack ||
                error == SecurityStatusPalErrorCode.QopNotSupported ||
                error == SecurityStatusPalErrorCode.NoCredentials ||
                error == SecurityStatusPalErrorCode.MessageAltered ||
                error == SecurityStatusPalErrorCode.OutOfSequence ||
                error == SecurityStatusPalErrorCode.IncompleteMessage ||
                error == SecurityStatusPalErrorCode.IncompleteCredentials ||
                error == SecurityStatusPalErrorCode.WrongPrincipal ||
                error == SecurityStatusPalErrorCode.TimeSkew ||
                error == SecurityStatusPalErrorCode.IllegalMessage ||
                error == SecurityStatusPalErrorCode.CertUnknown ||
                error == SecurityStatusPalErrorCode.AlgorithmMismatch ||
                error == SecurityStatusPalErrorCode.SecurityQosFailed ||
                error == SecurityStatusPalErrorCode.UnsupportedPreauth;
        }

        private static void AddChallenge(ref ArrayList? challenges, string challenge)
        {
            if (challenge != null)
            {
                challenge = challenge.Trim();
                if (challenge.Length > 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "challenge:" + challenge);
                    if (challenges == null)
                    {
                        challenges = new ArrayList(4);
                    }
                    challenges.Add(challenge);
                }
            }
        }

        private ArrayList? BuildChallenge(AuthenticationSchemes authenticationScheme, ulong connectionId,
            out NTAuthentication? newContext, ExtendedProtectionPolicy policy, bool isSecureConnection)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "AuthenticationScheme:" + authenticationScheme.ToString());
            ArrayList? challenges = null;
            newContext = null;

            if ((authenticationScheme & AuthenticationSchemes.Negotiate) != 0)
            {
                AddChallenge(ref challenges, AuthenticationTypes.Negotiate);
            }

            if ((authenticationScheme & AuthenticationSchemes.Ntlm) != 0)
            {
                AddChallenge(ref challenges, AuthenticationTypes.NTLM);
            }

            if ((authenticationScheme & AuthenticationSchemes.Basic) != 0)
            {
                AddChallenge(ref challenges, "Basic realm=\"" + Realm + "\"");
            }

            return challenges;
        }

        private static void RegisterForDisconnectNotification(HttpListenerSession session, ulong connectionId, ref DisconnectAsyncResult? disconnectResult)
        {
            Debug.Assert(disconnectResult == null);

            try
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(session.Listener, "Calling Interop.HttpApi.HttpWaitForDisconnect");

                DisconnectAsyncResult result = new DisconnectAsyncResult(session, connectionId);

                uint statusCode = Interop.HttpApi.HttpWaitForDisconnect(
                    session.RequestQueueHandle,
                    connectionId,
                    result.NativeOverlapped);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(session.Listener, "Call to Interop.HttpApi.HttpWaitForDisconnect returned:" + statusCode);

                if (statusCode == Interop.HttpApi.ERROR_SUCCESS ||
                    statusCode == Interop.HttpApi.ERROR_IO_PENDING)
                {
                    // Need to make sure it's going to get returned before adding it to the hash.  That way it'll be handled
                    // correctly in HandleAuthentication's finally.
                    disconnectResult = result;
                    session.Listener.DisconnectResults[connectionId] = disconnectResult;
                }

                if (statusCode == Interop.HttpApi.ERROR_SUCCESS && HttpListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    result.IOCompleted(statusCode, 0, result.NativeOverlapped);
                }
            }
            catch (Win32Exception exception)
            {
                uint statusCode = (uint)exception.NativeErrorCode;
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(session.Listener, "Call to Interop.HttpApi.HttpWaitForDisconnect threw, statusCode:" + statusCode);
            }
        }

        private static void SendError(HttpListenerSession session, ulong requestId, HttpStatusCode httpStatusCode, ArrayList? challenges)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(session.Listener, $"RequestId: {requestId}");
            Interop.HttpApi.HTTP_RESPONSE httpResponse = default;
            httpResponse.Version = default;
            httpResponse.Version.MajorVersion = (ushort)1;
            httpResponse.Version.MinorVersion = (ushort)1;
            httpResponse.StatusCode = (ushort)httpStatusCode;
            string? statusDescription = HttpStatusDescription.Get(httpStatusCode);
            uint DataWritten = 0;
            uint statusCode;
            byte[] byteReason = Encoding.Default.GetBytes(statusDescription!);
            fixed (byte* pReason = byteReason)
            {
                httpResponse.pReason = (sbyte*)pReason;
                httpResponse.ReasonLength = (ushort)byteReason.Length;

                byte[] byteContentLength = Encoding.Default.GetBytes("0");
                fixed (byte* pContentLength = &byteContentLength[0])
                {
                    (&httpResponse.Headers.KnownHeaders)[(int)HttpResponseHeader.ContentLength].pRawValue = (sbyte*)pContentLength;
                    (&httpResponse.Headers.KnownHeaders)[(int)HttpResponseHeader.ContentLength].RawValueLength = (ushort)byteContentLength.Length;

                    httpResponse.Headers.UnknownHeaderCount = checked((ushort)(challenges == null ? 0 : challenges.Count));
                    GCHandle[]? challengeHandles = null;
                    Interop.HttpApi.HTTP_UNKNOWN_HEADER[]? headersArray = null;
                    GCHandle headersArrayHandle = default;
                    GCHandle wwwAuthenticateHandle = default;
                    if (httpResponse.Headers.UnknownHeaderCount > 0)
                    {
                        challengeHandles = new GCHandle[httpResponse.Headers.UnknownHeaderCount];
                        headersArray = new Interop.HttpApi.HTTP_UNKNOWN_HEADER[httpResponse.Headers.UnknownHeaderCount];
                    }

                    try
                    {
                        if (httpResponse.Headers.UnknownHeaderCount > 0)
                        {
                            headersArrayHandle = GCHandle.Alloc(headersArray, GCHandleType.Pinned);
                            httpResponse.Headers.pUnknownHeaders = (Interop.HttpApi.HTTP_UNKNOWN_HEADER*)Marshal.UnsafeAddrOfPinnedArrayElement(headersArray!, 0);
                            wwwAuthenticateHandle = GCHandle.Alloc(s_wwwAuthenticateBytes, GCHandleType.Pinned);
                            sbyte* wwwAuthenticate = (sbyte*)Marshal.UnsafeAddrOfPinnedArrayElement(s_wwwAuthenticateBytes, 0);

                            for (int i = 0; i < challengeHandles!.Length; i++)
                            {
                                byte[] byteChallenge = Encoding.Default.GetBytes((string)challenges![i]!);
                                challengeHandles[i] = GCHandle.Alloc(byteChallenge, GCHandleType.Pinned);
                                headersArray![i].pName = wwwAuthenticate;
                                headersArray[i].NameLength = (ushort)s_wwwAuthenticateBytes.Length;
                                headersArray[i].pRawValue = (sbyte*)Marshal.UnsafeAddrOfPinnedArrayElement(byteChallenge, 0);
                                headersArray[i].RawValueLength = checked((ushort)byteChallenge.Length);
                            }
                        }

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(session.Listener, "Calling Interop.HttpApi.HttpSendHtthttpResponse");
                        statusCode =
                            Interop.HttpApi.HttpSendHttpResponse(
                                session.RequestQueueHandle,
                                requestId,
                                0,
                                &httpResponse,
                                null,
                                &DataWritten,
                                SafeLocalAllocHandle.Zero,
                                0,
                                null,
                                null);
                    }
                    finally
                    {
                        if (headersArrayHandle.IsAllocated)
                        {
                            headersArrayHandle.Free();
                        }
                        if (wwwAuthenticateHandle.IsAllocated)
                        {
                            wwwAuthenticateHandle.Free();
                        }
                        if (challengeHandles != null)
                        {
                            for (int i = 0; i < challengeHandles.Length; i++)
                            {
                                if (challengeHandles[i].IsAllocated)
                                {
                                    challengeHandles[i].Free();
                                }
                            }
                        }
                    }
                }
            }
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(session.Listener, "Call to Interop.HttpApi.HttpSendHttpResponse returned:" + statusCode);
            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                // if we fail to send a 401 something's seriously wrong, abort the request
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(session.Listener, "SendUnauthorized returned:" + statusCode);
                HttpListenerContext.CancelRequest(session.RequestQueueHandle, requestId);
            }
        }

        private static unsafe int GetTokenOffsetFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            IntPtr tokenPointer = ((Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS*)blob)->ChannelToken;

            Debug.Assert(tokenPointer != IntPtr.Zero);
            return (int)((byte*)tokenPointer - (byte*)blob);
        }

        private static unsafe int GetTokenSizeFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            return (int)((Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS*)blob)->ChannelTokenSize;
        }

        internal static ChannelBinding? GetChannelBindingFromTls(HttpListenerSession session, ulong connectionId)
        {
            // +128 since a CBT is usually <128 thus we need to call HRCC just once. If the CBT
            // is >128 we will get ERROR_MORE_DATA and call again
            int size = sizeof(Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS) + 128;

            Debug.Assert(size > 0);

            byte[]? blob = null;
            Interop.HttpApi.SafeLocalFreeChannelBinding? token = null;

            uint bytesReceived = 0;
            uint statusCode;

            do
            {
                blob = new byte[size];
                fixed (byte* blobPtr = &blob[0])
                {
                    // Http.sys team: ServiceName will always be null if
                    // HTTP_RECEIVE_SECURE_CHANNEL_TOKEN flag is set.
                    statusCode = Interop.HttpApi.HttpReceiveClientCertificate(
                        session.RequestQueueHandle,
                        connectionId,
                        (uint)Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_SECURE_CHANNEL_TOKEN,
                        blobPtr,
                        (uint)size,
                        &bytesReceived,
                        null);

                    if (statusCode == Interop.HttpApi.ERROR_SUCCESS)
                    {
                        int tokenOffset = GetTokenOffsetFromBlob((IntPtr)blobPtr);
                        int tokenSize = GetTokenSizeFromBlob((IntPtr)blobPtr);
                        Debug.Assert(tokenSize < int.MaxValue);

                        token = Interop.HttpApi.SafeLocalFreeChannelBinding.LocalAlloc(tokenSize);
                        if (token.IsInvalid)
                        {
                            throw new OutOfMemoryException();
                        }
                        Marshal.Copy(blob, tokenOffset, token.DangerousGetHandle(), tokenSize);
                    }
                    else if (statusCode == Interop.HttpApi.ERROR_MORE_DATA)
                    {
                        int tokenSize = GetTokenSizeFromBlob((IntPtr)blobPtr);
                        Debug.Assert(tokenSize < int.MaxValue);

                        size = sizeof(Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS) + tokenSize;
                    }
                    else if (statusCode == Interop.HttpApi.ERROR_INVALID_PARAMETER)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Error(session.Listener, SR.net_ssp_dont_support_cbt);
                        }
                        return null; // old schannel library which doesn't support CBT
                    }
                    else
                    {
                        throw new HttpListenerException((int)statusCode);
                    }
                }
            } while (statusCode != Interop.HttpApi.ERROR_SUCCESS);

            return token;
        }


        private sealed class DisconnectAsyncResult : IAsyncResult
        {
            private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(WaitCallback);

            private readonly ulong _connectionId;
            private readonly HttpListenerSession _listenerSession;
            private readonly NativeOverlapped* _nativeOverlapped;
            private int _ownershipState;   // 0 = normal, 1 = in HandleAuthentication(), 2 = disconnected, 3 = cleaned up

            private WindowsPrincipal? _authenticatedConnection;
            private NTAuthentication? _session;

            internal NativeOverlapped* NativeOverlapped
            {
                get
                {
                    return _nativeOverlapped;
                }
            }

            public object AsyncState
            {
                get
                {
                    throw new NotImplementedException(SR.net_PropertyNotImplementedException);
                }
            }
            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    throw new NotImplementedException(SR.net_PropertyNotImplementedException);
                }
            }
            public bool CompletedSynchronously
            {
                get
                {
                    throw new NotImplementedException(SR.net_PropertyNotImplementedException);
                }
            }
            public bool IsCompleted
            {
                get
                {
                    throw new NotImplementedException(SR.net_PropertyNotImplementedException);
                }
            }

            internal unsafe DisconnectAsyncResult(HttpListenerSession session, ulong connectionId)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"HttpListener: {session.Listener}, ConnectionId: {connectionId}");
                _ownershipState = 1;
                _listenerSession = session;
                _connectionId = connectionId;

                // we can call the Unsafe API here, we won't ever call user code
                _nativeOverlapped = session.RequestQueueBoundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: null);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info($"DisconnectAsyncResult: ThreadPoolBoundHandle.AllocateNativeOverlapped({session.RequestQueueBoundHandle}) -> {_nativeOverlapped->GetHashCode()}");
            }

            internal bool StartOwningDisconnectHandling()
            {
                int oldValue;

                SpinWait spin = default;
                while ((oldValue = Interlocked.CompareExchange(ref _ownershipState, 1, 0)) == 2)
                {
                    // Must block until it equals 3 - we must be in the callback right now.
                    spin.SpinOnce();
                }

                Debug.Assert(oldValue != 1, "StartOwningDisconnectHandling called twice.");
                return oldValue < 2;
            }

            internal void FinishOwningDisconnectHandling()
            {
                // If it got disconnected, run the disconnect code.
                if (Interlocked.CompareExchange(ref _ownershipState, 0, 1) == 2)
                {
                    HandleDisconnect();
                }
            }

            internal unsafe void IOCompleted(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                IOCompleted(this, errorCode, numBytes, nativeOverlapped);
            }

            private static unsafe void IOCompleted(DisconnectAsyncResult asyncResult, uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "_connectionId:" + asyncResult._connectionId);

                asyncResult._listenerSession.RequestQueueBoundHandle.FreeNativeOverlapped(nativeOverlapped);
                if (Interlocked.Exchange(ref asyncResult._ownershipState, 2) == 0)
                {
                    asyncResult.HandleDisconnect();
                }
            }

            private static unsafe void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"errorCode: {errorCode}, numBytes: {numBytes}, nativeOverlapped: {(IntPtr)nativeOverlapped:x}");
                // take the DisconnectAsyncResult object from the state
                DisconnectAsyncResult asyncResult = (DisconnectAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!;
                IOCompleted(asyncResult, errorCode, numBytes, nativeOverlapped);
            }

            private void HandleDisconnect()
            {
                HttpListener listener = _listenerSession.Listener;

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"DisconnectResults {listener.DisconnectResults} removing for _connectionId: {_connectionId}");
                listener.DisconnectResults.Remove(_connectionId);
                if (_session != null)
                {
                    _session.CloseContext();
                }

                // Clean up the identity. This is for scenarios where identity was not cleaned up before due to
                // identity caching for unsafe ntlm authentication

                IDisposable? identity = _authenticatedConnection == null ? null : _authenticatedConnection.Identity as IDisposable;
                if ((identity != null) &&
                    (_authenticatedConnection!.Identity.AuthenticationType == AuthenticationTypes.NTLM) &&
                    (listener.UnsafeConnectionNtlmAuthentication))
                {
                    identity.Dispose();
                }

                int oldValue = Interlocked.Exchange(ref _ownershipState, 3);
                Debug.Assert(oldValue == 2, $"Expected OwnershipState of 2, saw {oldValue}.");
            }

            internal WindowsPrincipal? AuthenticatedConnection
            {
                get
                {
                    return _authenticatedConnection;
                }

                set
                {
                    // The previous value can't be disposed because it may be in use by the app.
                    _authenticatedConnection = value;
                }
            }

            internal NTAuthentication? Session
            {
                get
                {
                    return _session;
                }

                set
                {
                    _session = value;
                }
            }
        }
    }
}
