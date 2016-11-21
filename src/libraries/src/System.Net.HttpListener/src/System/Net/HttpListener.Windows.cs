// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed unsafe partial class HttpListener
    {
        public static bool IsSupported
        {
            get
            {
                return Interop.HttpApi.s_supported;
            }
        }

        private static readonly Type s_channelBindingStatusType = typeof(Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS);
        private static readonly int s_requestChannelBindStatusSize =
            Marshal.SizeOf(typeof(Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS));

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

        private static readonly byte[] s_WwwAuthenticateBytes = new byte[]
        {
            (byte) 'W', (byte) 'W', (byte) 'W', (byte) '-', (byte) 'A', (byte) 'u', (byte) 't', (byte) 'h',
            (byte) 'e', (byte) 'n', (byte) 't', (byte) 'i', (byte) 'c', (byte) 'a', (byte) 't', (byte) 'e'
        };

        private AuthenticationSchemeSelector _authenticationDelegate;
        private AuthenticationSchemes _authenticationScheme = AuthenticationSchemes.Anonymous;
        private string _realm;
        private SafeHandle _requestQueueHandle;
        private ThreadPoolBoundHandle _requestQueueBoundHandle;
        private volatile State _state; // _state is set only within lock blocks, but often read outside locks. 
        private HttpListenerPrefixCollection _prefixes;
        private bool _ignoreWriteExceptions;
        private bool _unsafeConnectionNtlmAuthentication;
        private ExtendedProtectionSelector _extendedProtectionSelectorDelegate;
        private ExtendedProtectionPolicy _extendedProtectionPolicy;
        private ServiceNameStore _defaultServiceNames;
        private HttpServerSessionHandle _serverSessionHandle;
        private ulong _urlGroupId;
        private HttpListenerTimeoutManager _timeoutManager;
        private bool _V2Initialized;

        private Dictionary<ulong, DisconnectAsyncResult> _disconnectResults;         // ulong -> DisconnectAsyncResult
        private readonly object _internalLock;

        internal Hashtable _uriPrefixes = new Hashtable();

        public HttpListener()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _state = State.Stopped;
            _internalLock = new object();
            _defaultServiceNames = new ServiceNameStore();

            _timeoutManager = new HttpListenerTimeoutManager(this);

            // default: no CBT checks on any platform (appcompat reasons); applies also to PolicyEnforcement 
            // config element
            _extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal ICollection PrefixCollection => _uriPrefixes.Keys;

        internal SafeHandle RequestQueueHandle
        {
            get
            {
                return _requestQueueHandle;
            }
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get
            {
                return _authenticationDelegate;
            }
            set
            {
                CheckDisposed();
                _authenticationDelegate = value;
            }
        }

        public ExtendedProtectionSelector ExtendedProtectionSelectorDelegate
        {
            get
            {
                return _extendedProtectionSelectorDelegate;
            }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _extendedProtectionSelectorDelegate = value;
            }
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                return _authenticationScheme;
            }
            set
            {
                CheckDisposed();
                _authenticationScheme = value;
            }
        }

        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get
            {
                return _extendedProtectionPolicy;
            }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.CustomChannelBinding != null)
                {
                    throw new ArgumentException(SR.net_listener_cannot_set_custom_cbt, nameof(value));
                }

                _extendedProtectionPolicy = value;
            }
        }

        public ServiceNameCollection DefaultServiceNames
        {
            get
            {
                return _defaultServiceNames.ServiceNames;
            }
        }

        public string Realm
        {
            get
            {
                return _realm;
            }
            set
            {
                CheckDisposed();
                _realm = value;
            }
        }

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

        private void SetUrlGroupProperty(Interop.HttpApi.HTTP_SERVER_PROPERTY property, IntPtr info, uint infosize)
        {
            uint statusCode = Interop.HttpApi.ERROR_SUCCESS;

            Debug.Assert(_urlGroupId != 0, "SetUrlGroupProperty called with invalid url group id");
            Debug.Assert(info != IntPtr.Zero, "SetUrlGroupProperty called with invalid pointer");

            //
            // Set the url group property using Http Api.
            //
            statusCode = Interop.HttpApi.HttpSetUrlGroupProperty(
                _urlGroupId, property, info, infosize);

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                HttpListenerException exception = new HttpListenerException((int)statusCode);
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"HttpSetUrlGroupProperty:: Property: {property} {exception}");
                throw exception;
            }
        }

        internal void SetServerTimeout(int[] timeouts, uint minSendBytesPerSecond)
        {
            ValidateV2Property(); // CheckDispose and initilize HttpListener in the case of app.config timeouts

            Interop.HttpApi.HTTP_TIMEOUT_LIMIT_INFO timeoutinfo =
                new Interop.HttpApi.HTTP_TIMEOUT_LIMIT_INFO();

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

        public bool IsListening
        {
            get
            {
                return _state == State.Started;
            }
        }

        public bool IgnoreWriteExceptions
        {
            get
            {
                return _ignoreWriteExceptions;
            }
            set
            {
                CheckDisposed();
                _ignoreWriteExceptions = value;
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get
            {
                return _unsafeConnectionNtlmAuthentication;
            }

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

        private Dictionary<ulong, DisconnectAsyncResult> DisconnectResults
        {
            get
            {
                if (_disconnectResults == null)
                {
                    lock (_internalLock)
                    {
                        if (_disconnectResults == null)
                        {
                            _disconnectResults = new Dictionary<ulong, DisconnectAsyncResult>();
                        }
                    }
                }
                return _disconnectResults;
            }
        }

        internal void AddPrefix(string uriPrefix)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, $"uriPrefix:{uriPrefix}");
            string registeredPrefix = null;
            try
            {
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException(nameof(uriPrefix));
                }
                CheckDisposed();
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "uriPrefix:" + uriPrefix);
                int i;
                if (string.Compare(uriPrefix, 0, "http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    i = 7;
                }
                else if (string.Compare(uriPrefix, 0, "https://", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    i = 8;
                }
                else
                {
                    throw new ArgumentException(SR.net_listener_scheme, nameof(uriPrefix));
                }
                bool inSquareBrakets = false;
                int j = i;
                while (j < uriPrefix.Length && uriPrefix[j] != '/' && (uriPrefix[j] != ':' || inSquareBrakets))
                {
                    if (uriPrefix[j] == '[')
                    {
                        if (inSquareBrakets)
                        {
                            j = i;
                            break;
                        }
                        inSquareBrakets = true;
                    }
                    if (inSquareBrakets && uriPrefix[j] == ']')
                    {
                        inSquareBrakets = false;
                    }
                    j++;
                }
                if (i == j)
                {
                    throw new ArgumentException(SR.net_listener_host, nameof(uriPrefix));
                }
                if (uriPrefix[uriPrefix.Length - 1] != '/')
                {
                    throw new ArgumentException(SR.net_listener_slash, nameof(uriPrefix));
                }
                registeredPrefix = uriPrefix[j] == ':' ? String.Copy(uriPrefix) : uriPrefix.Substring(0, j) + (i == 7 ? ":80" : ":443") + uriPrefix.Substring(j);
                fixed (char* pChar = registeredPrefix)
                {
                    i = 0;
                    while (pChar[i] != ':')
                    {
                        pChar[i] = (char)CaseInsensitiveAscii.AsciiToLower[(byte)pChar[i]];
                        i++;
                    }
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "mapped uriPrefix:" + uriPrefix + " to registeredPrefix:" + registeredPrefix);
                if (_state == State.Started)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpAddUrl[ToUrlGroup]");
                    uint statusCode = InternalAddPrefix(registeredPrefix);
                    if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                    {
                        if (statusCode == Interop.HttpApi.ERROR_ALREADY_EXISTS)
                            throw new HttpListenerException((int)statusCode, SR.Format(SR.net_listener_already, registeredPrefix));
                        else
                            throw new HttpListenerException((int)statusCode);
                    }
                }
                _uriPrefixes[uriPrefix] = registeredPrefix;
                _defaultServiceNames.Add(uriPrefix);
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"AddPrefix {exception}");
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this, $"prefix: {registeredPrefix}");
            }
        }

        internal bool ContainsPrefix(string uriPrefix) => _uriPrefixes.Contains(uriPrefix);

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
                CheckDisposed();
                if (_prefixes == null)
                {
                    _prefixes = new HttpListenerPrefixCollection(this);
                }
                if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
                return _prefixes;
            }
        }

        internal bool RemovePrefix(string uriPrefix)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, $"uriPrefix: {uriPrefix}");
            try
            {
                CheckDisposed();
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"uriPrefix: {uriPrefix}");
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException(nameof(uriPrefix));
                }

                if (!_uriPrefixes.Contains(uriPrefix))
                {
                    return false;
                }

                if (_state == State.Started)
                {
                    InternalRemovePrefix((string)_uriPrefixes[uriPrefix]);
                }

                _uriPrefixes.Remove(uriPrefix);
                _defaultServiceNames.Remove(uriPrefix);
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"RemovePrefix {exception}");
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this, $"uriPrefix: {uriPrefix}");
            }
            return true;
        }

        internal void RemoveAll(bool clear)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                CheckDisposed();
                // go through the uri list and unregister for each one of them
                if (_uriPrefixes.Count > 0)
                {
                    if (_state == State.Started)
                    {
                        foreach (string registeredPrefix in _uriPrefixes.Values)
                        {
                            // ignore possible failures
                            InternalRemovePrefix(registeredPrefix);
                        }
                    }

                    if (clear)
                    {
                        _uriPrefixes.Clear();
                        _defaultServiceNames.Clear();
                    }
                }
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        private IntPtr DangerousGetHandle()
        {
            return ((HttpRequestQueueV2Handle)_requestQueueHandle).DangerousGetHandle();
        }

        internal ThreadPoolBoundHandle RequestQueueBoundHandle
        {
            get
            {
                if (_requestQueueBoundHandle == null)
                {
                    lock (_internalLock)
                    {
                        if (_requestQueueBoundHandle == null)
                        {
                            _requestQueueBoundHandle = ThreadPoolBoundHandle.BindHandle(_requestQueueHandle);
                        }
                    }
                }
                return _requestQueueBoundHandle;
            }
        }

        private void SetupV2Config()
        {
            uint statusCode = Interop.HttpApi.ERROR_SUCCESS;
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
                statusCode = Interop.HttpApi.HttpCreateServerSession(
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

                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"SetupV2Config {exception}");
                throw;
            }
        }

        public void Start()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

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
                        // If an error occured while adding prefixes, free all resources allocated by previous steps.
                        DetachRequestQueueFromUrlGroup();
                        ClearDigestCache();
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
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"Start {exception}");
                    throw;
                }
                finally
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
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
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"CloseV2Config {SR.Format(SR.net_listener_close_urlgroup_error, statusCode)}");
            }
            _urlGroupId = 0;

            Debug.Assert(_serverSessionHandle != null, "ServerSessionHandle is null in CloseV2Config");
            Debug.Assert(!_serverSessionHandle.IsInvalid, "ServerSessionHandle is invalid in CloseV2Config");

            _serverSessionHandle.Dispose();
        }

        private void AttachRequestQueueToUrlGroup()
        {
            //
            // Set the association between request queue and url group. After this, requests for registered urls will 
            // get delivered to this request queue.
            //
            Interop.HttpApi.HTTP_BINDING_INFO info = new Interop.HttpApi.HTTP_BINDING_INFO();
            info.Flags = Interop.HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            info.RequestQueueHandle = DangerousGetHandle();

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
            Interop.HttpApi.HTTP_BINDING_INFO info = new Interop.HttpApi.HTTP_BINDING_INFO();
            info.Flags = Interop.HttpApi.HTTP_FLAGS.NONE;
            info.RequestQueueHandle = IntPtr.Zero;

            IntPtr infoptr = new IntPtr(&info);

            uint statusCode = Interop.HttpApi.HttpSetUrlGroupProperty(_urlGroupId,
                Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
                infoptr, (uint)Marshal.SizeOf(typeof(Interop.HttpApi.HTTP_BINDING_INFO)));

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"DetachRequestQueueFromUrlGroup {SR.Format(SR.net_listener_detach_error, statusCode)}");
            }
        }

        public void Stop()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
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

                ClearDigestCache();
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"Stop {exception}");
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        private unsafe void CreateRequestQueueHandle()
        {
            uint statusCode = Interop.HttpApi.ERROR_SUCCESS;

            HttpRequestQueueV2Handle requestQueueHandle = null;
            statusCode =
                Interop.HttpApi.HttpCreateRequestQueue(
                    Interop.HttpApi.s_version, null, null, 0, out requestQueueHandle);

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                throw new HttpListenerException((int)statusCode);
            }

            // Disabling callbacks when IO operation completes synchronously (returns ErrorCodes.ERROR_SUCCESS)
            if (SkipIOCPCallbackOnSuccess &&
                !Interop.HttpApi.SetFileCompletionNotificationModes(
                    requestQueueHandle,
                    Interop.HttpApi.FileCompletionNotificationModes.SkipCompletionPortOnSuccess |
                    Interop.HttpApi.FileCompletionNotificationModes.SkipSetEventOnHandle))
            {
                throw new HttpListenerException(Marshal.GetLastWin32Error());
            }

            _requestQueueHandle = requestQueueHandle;
        }

        private unsafe void CloseRequestQueueHandle()
        {
            if ((_requestQueueHandle != null) && (!_requestQueueHandle.IsInvalid))
            {
                _requestQueueBoundHandle?.Dispose();
                _requestQueueHandle.Dispose();
            }
        }

        public void Abort()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

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
                    ClearDigestCache();
                }
                catch (Exception exception)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"Abort {exception}");
                    throw;
                }
                finally
                {
                    _state = State.Closed;
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
                }
            }
        }

        private void Dispose()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

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
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"Dispose {exception}");
                    throw;
                }
                finally
                {
                    _state = State.Closed;
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
                }
            }
        }

        private uint InternalAddPrefix(string uriPrefix)
        {
            uint statusCode = 0;

            statusCode =
                Interop.HttpApi.HttpAddUrlToUrlGroup(
                    _urlGroupId,
                    uriPrefix,
                    0,
                    0);

            return statusCode;
        }

        private bool InternalRemovePrefix(string uriPrefix)
        {
            uint statusCode = Interop.HttpApi.HttpRemoveUrlFromUrlGroup(_urlGroupId, uriPrefix, 0);
            return statusCode != Interop.HttpApi.ERROR_NOT_FOUND;
        }

        private void AddAllPrefixes()
        {
            // go through the uri list and register for each one of them
            if (_uriPrefixes.Count > 0)
            {
                foreach (string registeredPrefix in _uriPrefixes.Values)
                {
                    uint statusCode = InternalAddPrefix(registeredPrefix);
                    if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                    {
                        if (statusCode == Interop.HttpApi.ERROR_ALREADY_EXISTS)
                            throw new HttpListenerException((int)statusCode, SR.Format(SR.net_listener_already, registeredPrefix));
                        else
                            throw new HttpListenerException((int)statusCode);
                    }
                }
            }
        }

        public HttpListenerContext GetContext()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            SyncRequestContext memoryBlob = null;
            HttpListenerContext httpContext = null;
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
                for (;;)
                {
                    for (;;)
                    {
                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Calling Interop.HttpApi.HttpReceiveHttpRequest RequestId: {requestId}");
                        uint bytesTransferred = 0;
                        statusCode =
                            Interop.HttpApi.HttpReceiveHttpRequest(
                                _requestQueueHandle,
                                requestId,
                                (uint)Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                                memoryBlob.RequestBlob,
                                size,
                                &bytesTransferred,
                                null);

                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveHttpRequest returned:" + statusCode);

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

                    if (ValidateRequest(memoryBlob))
                    {
                        // We need to hook up our authentication handling code here.
                        httpContext = HandleAuthentication(memoryBlob, out stoleBlob);
                    }

                    if (stoleBlob)
                    {
                        // The request has been handed to the user, which means this code can't reuse the blob.  Reset it here.
                        memoryBlob = null;
                        stoleBlob = false;
                    }
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, ":HandleAuthentication() returned httpContext" + httpContext);
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
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"{exception}");
                throw;
            }
            finally
            {
                if (memoryBlob != null && !stoleBlob)
                {
                    memoryBlob.ReleasePins();
                    memoryBlob.Close();
                }
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this, "RequestTraceIdentifier: " + (httpContext != null ? httpContext.Request.RequestTraceIdentifier.ToString() : "<null>"));
            }
        }

        internal unsafe bool ValidateRequest(RequestContextBase requestMemory)
        {
            // Block potential DOS attacks
            if (requestMemory.RequestBlob->Headers.UnknownHeaderCount > UnknownHeaderLimit)
            {
                SendError(requestMemory.RequestBlob->RequestId, HttpStatusCode.BadRequest, null);
                return false;
            }
            return true;
        }

        public IAsyncResult BeginGetContext(AsyncCallback callback, object state)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            ListenerAsyncResult asyncResult = null;
            try
            {
                CheckDisposed();
                if (_state == State.Stopped)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "Start()"));
                }
                // prepare the ListenerAsyncResult object (this will have it's own
                // event that the user can wait on for IO completion - which means we
                // need to signal it when IO completes)
                asyncResult = new ListenerAsyncResult(this, state, callback);
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
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"BeginGetContext {exception}");
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }

            return asyncResult;
        }

        public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            HttpListenerContext httpContext = null;
            try
            {
                CheckDisposed();
                if (asyncResult == null)
                {
                    throw new ArgumentNullException(nameof(asyncResult));
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"asyncResult: {asyncResult}");
                ListenerAsyncResult castedAsyncResult = asyncResult as ListenerAsyncResult;
                if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this)
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
                    throw castedAsyncResult.Result as Exception;
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"EndGetContext {exception}");
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this, "EndGetContext " + httpContext == null ? "<no context>" : "HttpListenerContext" + httpContext.ToString() + " RequestTraceIdentifier#" + httpContext.Request.RequestTraceIdentifier);
            }
            return httpContext;
        }

        public Task<HttpListenerContext> GetContextAsync()
        {
            return Task.Factory.FromAsync(
                (callback, state) => ((HttpListener)state).BeginGetContext(callback, state),
                iar => ((HttpListener)iar.AsyncState).EndGetContext(iar),
                this);
        }

        internal HttpListenerContext HandleAuthentication(RequestContextBase memoryBlob, out bool stoleBlob)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "HandleAuthentication() memoryBlob:0x" + ((IntPtr)memoryBlob.RequestBlob).ToString("x"));

            string challenge = null;
            stoleBlob = false;

            // Some things we need right away.  Lift them out now while it's convenient.
            string verb = Interop.HttpApi.GetVerb(memoryBlob.RequestBlob);
            string authorizationHeader = Interop.HttpApi.GetKnownHeader(memoryBlob.RequestBlob, (int)HttpRequestHeader.Authorization);
            ulong connectionId = memoryBlob.RequestBlob->ConnectionId;
            ulong requestId = memoryBlob.RequestBlob->RequestId;
            bool isSecureConnection = memoryBlob.RequestBlob->pSslInfo != null;

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"HandleAuthentication() authorizationHeader: ({authorizationHeader})");

            // if the app has turned on AuthPersistence, an anonymous request might
            // be authenticated by virtue of it coming on a connection that was
            // previously authenticated.
            // assurance that we do this only for NTLM/Negotiate is not here, but in the
            // code that caches WindowsIdentity instances in the Dictionary.
            DisconnectAsyncResult disconnectResult;
            DisconnectResults.TryGetValue(connectionId, out disconnectResult);
            if (UnsafeConnectionNtlmAuthentication)
            {
                if (authorizationHeader == null)
                {
                    WindowsPrincipal principal = disconnectResult?.AuthenticatedConnection;
                    if (principal != null)
                    {
                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Principal: {principal} principal.Identity.Name: {principal.Identity.Name} creating request");
                        stoleBlob = true;
                        HttpListenerContext ntlmContext = new HttpListenerContext(this, memoryBlob);
                        ntlmContext.SetIdentity(principal, null);
                        ntlmContext.Request.ReleasePins();
                        return ntlmContext;
                    }
                }
                else
                {
                    // They sent an authorization - destroy their previous credentials.
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Clearing principal cache");
                    if (disconnectResult != null)
                    {
                        disconnectResult.AuthenticatedConnection = null;
                    }
                }
            }

            // Figure out what schemes we're allowing, what context we have.
            stoleBlob = true;
            HttpListenerContext httpContext = null;
            NTAuthentication oldContext = null;
            NTAuthentication newContext = null;
            NTAuthentication context = null;
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

                httpContext = new HttpListenerContext(this, memoryBlob);

                AuthenticationSchemeSelector authenticationSelector = _authenticationDelegate;
                if (authenticationSelector != null)
                {
                    try
                    {
                        httpContext.Request.ReleasePins();
                        authenticationScheme = authenticationSelector(httpContext.Request);
                        // Cache the results of authenticationSelector (if any)
                        httpContext.AuthenticationSchemes = authenticationScheme;
                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"AuthenticationScheme: {authenticationScheme}");
                    }
                    catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
                    {
                        if (NetEventSource.IsEnabled)
                        {
                            NetEventSource.Error(this, SR.Format(SR.net_log_listener_delegate_exception, exception));
                            NetEventSource.Info(this, $"authenticationScheme: {authenticationScheme}");
                        }
                        SendError(requestId, HttpStatusCode.InternalServerError, null);
                        httpContext.Close();
                        return null;
                    }
                }
                else
                {
                    // We didn't give the request to the user yet, so we haven't lost control of the unmanaged blob and can
                    // continue to reuse the buffer.
                    stoleBlob = false;
                }

                ExtendedProtectionSelector extendedProtectionSelector = _extendedProtectionSelectorDelegate;
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
                            string.Compare(authorizationHeader, 0, AuthConstants.Negotiate, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Negotiate;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Ntlm) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, AuthConstants.NTLM, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Ntlm;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Digest) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, AuthConstants.Digest, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Digest;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Basic) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, AuthConstants.Basic, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Basic;
                        }
                        else
                        {
                            if (NetEventSource.IsEnabled) NetEventSource.Error(this, SR.Format(SR.net_log_listener_unsupported_authentication_scheme, authorizationHeader, authenticationScheme));
                        }
                    }
                }

                // httpError holds the error we will return if an Authorization header is present but can't be authenticated
                HttpStatusCode httpError = HttpStatusCode.InternalServerError;

                // See if we found an acceptable auth header
                if (headerScheme == AuthenticationSchemes.None)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, SR.Format(SR.net_log_listener_unmatched_authentication_scheme, authenticationScheme.ToString(), (authorizationHeader == null ? "<null>" : authorizationHeader)));

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
                    httpContext.Request.DetachBlob(memoryBlob);
                    httpContext.Close();
                    httpContext = null;
                }
                else
                {
                    throw new NotImplementedException();
                }

                // if we're not giving a request to the application, we need to send an error
                ArrayList challenges = null;
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
                            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "ConnectionId:" + connectionId + " because of error:" + httpError.ToString());
                            SendError(requestId, httpError, null);
                            return null;
                        }

                        challenges = BuildChallenge(authenticationScheme, connectionId, out newContext,
                            extendedProtectionPolicy, isSecureConnection);
                    }
                }

                // Check if we need to call WaitForDisconnect, because if we do and it fails, we want to send a 500 instead.
                if (disconnectResult == null && newContext != null)
                {
                    RegisterForDisconnectNotification(connectionId, ref disconnectResult);

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

                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, "connectionId:" + connectionId + " because of failed HttpWaitForDisconnect");
                        SendError(requestId, HttpStatusCode.InternalServerError, null);
                        httpContext.Request.DetachBlob(memoryBlob);
                        httpContext.Close();
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

                    NTAuthentication toClose = oldContext;
                    oldContext = newContext;
                    disconnectResult.Session = newContext;

                    if (toClose != null)
                    {
                        // Save digest context in digest cache, we may need it later because of
                        // subsequest responses to the same req on the same/diff connection
                        if ((authenticationScheme & AuthenticationSchemes.Digest) != 0)
                        {
                            SaveDigestContext(toClose);
                        }
                        else
                        {
                            toClose.CloseContext();
                        }
                    }
                }

                // Send the 401 here.
                if (httpContext == null)
                {
                    SendError(requestId, challenges != null && challenges.Count > 0 ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, challenges);
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Scheme:" + authenticationScheme);
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
                if (httpContext != null)
                {
                    httpContext.Request.DetachBlob(memoryBlob);
                    httpContext.Close();
                }
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

                        // Save digest context in digest cache, we may need it later because of
                        // subsequest responses to the same req on the same/diff connection

                        if ((authenticationScheme & AuthenticationSchemes.Digest) != 0)
                        {
                            SaveDigestContext(oldContext);
                        }
                        else
                        {
                            oldContext.CloseContext();
                        }
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

        // Using the configured Auth schemes, populate the auth challenge headers. This is for scenarios where 
        // Anonymous access is allowed for some resources, but the server later determines that authorization 
        // is required for this request.
        internal void SetAuthenticationHeaders(HttpListenerContext context)
        {
            Debug.Assert(context != null, "Null Context");

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // We use the cached results from the delegates so that we don't have to call them again here.
            NTAuthentication newContext;
            ArrayList challenges = BuildChallenge(context.AuthenticationSchemes, request._connectionId,
                out newContext, context.ExtendedProtectionPolicy, request.IsSecureConnection);

            // Setting 401 without setting WWW-Authenticate is a protocol violation
            // but throwing from HttpListener would be a breaking change.
            if (challenges != null) // null == Anonymous
            {
                if (newContext != null) // Digest challenge, keep it alive for 10s - 5min.
                {
                    SaveDigestContext(newContext);
                }

                // Add the new WWW-Authenticate headers
                foreach (string challenge in challenges)
                {
                    response.Headers.Add(HttpKnownHeaderNames.WWWAuthenticate, challenge);
                }
            }
        }

        private static void AddChallenge(ref ArrayList challenges, string challenge)
        {
            if (challenge != null)
            {
                challenge = challenge.Trim();
                if (challenge.Length > 0)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Info(null, "challenge:" + challenge);
                    if (challenges == null)
                    {
                        challenges = new ArrayList(4);
                    }
                    challenges.Add(challenge);
                }
            }
        }

        private ArrayList BuildChallenge(AuthenticationSchemes authenticationScheme, ulong connectionId,
            out NTAuthentication newContext, ExtendedProtectionPolicy policy, bool isSecureConnection)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "AuthenticationScheme:" + authenticationScheme.ToString());
            ArrayList challenges = null;
            newContext = null;

            if ((authenticationScheme & AuthenticationSchemes.Negotiate) != 0)
            {
                AddChallenge(ref challenges, AuthConstants.Negotiate);
            }

            if ((authenticationScheme & AuthenticationSchemes.Ntlm) != 0)
            {
                AddChallenge(ref challenges, AuthConstants.NTLM);
            }

            if ((authenticationScheme & AuthenticationSchemes.Digest) != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "WDigest");
                throw new NotImplementedException();
            }

            if ((authenticationScheme & AuthenticationSchemes.Basic) != 0)
            {
                AddChallenge(ref challenges, "Basic realm =\"" + Realm + "\"");
            }

            return challenges;
        }

        private void RegisterForDisconnectNotification(ulong connectionId, ref DisconnectAsyncResult disconnectResult)
        {
            Debug.Assert(disconnectResult == null);

            try
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpWaitForDisconnect");

                DisconnectAsyncResult result = new DisconnectAsyncResult(this, connectionId);

                uint statusCode = Interop.HttpApi.HttpWaitForDisconnect(
                    _requestQueueHandle,
                    connectionId,
                    result.NativeOverlapped);

                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Call to Interop.HttpApi.HttpWaitForDisconnect returned:" + statusCode);

                if (statusCode == Interop.HttpApi.ERROR_SUCCESS ||
                    statusCode == Interop.HttpApi.ERROR_IO_PENDING)
                {
                    // Need to make sure it's going to get returned before adding it to the hash.  That way it'll be handled
                    // correctly in HandleAuthentication's finally.
                    disconnectResult = result;
                    DisconnectResults[connectionId] = disconnectResult;
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
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Call to Interop.HttpApi.HttpWaitForDisconnect threw, statusCode:" + statusCode);
            }
        }

        private void SendError(ulong requestId, HttpStatusCode httpStatusCode, ArrayList challenges)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"RequestId: {requestId}");
            Interop.HttpApi.HTTP_RESPONSE httpResponse = new Interop.HttpApi.HTTP_RESPONSE();
            httpResponse.Version = new Interop.HttpApi.HTTP_VERSION();
            httpResponse.Version.MajorVersion = (ushort)1;
            httpResponse.Version.MinorVersion = (ushort)1;
            httpResponse.StatusCode = (ushort)httpStatusCode;
            string statusDescription = HttpStatusDescription.Get(httpStatusCode);
            uint DataWritten = 0;
            uint statusCode;
            byte[] byteReason = Encoding.Default.GetBytes(statusDescription);
            fixed (byte* pReason = byteReason)
            {
                httpResponse.pReason = (sbyte*)pReason;
                httpResponse.ReasonLength = (ushort)byteReason.Length;

                byte[] byteContentLength = Encoding.Default.GetBytes("0");
                fixed (byte* pContentLength = byteContentLength)
                {
                    (&httpResponse.Headers.KnownHeaders)[(int)HttpResponseHeader.ContentLength].pRawValue = (sbyte*)pContentLength;
                    (&httpResponse.Headers.KnownHeaders)[(int)HttpResponseHeader.ContentLength].RawValueLength = (ushort)byteContentLength.Length;

                    httpResponse.Headers.UnknownHeaderCount = checked((ushort)(challenges == null ? 0 : challenges.Count));
                    GCHandle[] challengeHandles = null;
                    Interop.HttpApi.HTTP_UNKNOWN_HEADER[] headersArray = null;
                    GCHandle headersArrayHandle = new GCHandle();
                    GCHandle wwwAuthenticateHandle = new GCHandle();
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
                            httpResponse.Headers.pUnknownHeaders = (Interop.HttpApi.HTTP_UNKNOWN_HEADER*)Marshal.UnsafeAddrOfPinnedArrayElement(headersArray, 0);
                            wwwAuthenticateHandle = GCHandle.Alloc(s_WwwAuthenticateBytes, GCHandleType.Pinned);
                            sbyte* wwwAuthenticate = (sbyte*)Marshal.UnsafeAddrOfPinnedArrayElement(s_WwwAuthenticateBytes, 0);

                            for (int i = 0; i < challengeHandles.Length; i++)
                            {
                                byte[] byteChallenge = Encoding.Default.GetBytes((string)challenges[i]);
                                challengeHandles[i] = GCHandle.Alloc(byteChallenge, GCHandleType.Pinned);
                                headersArray[i].pName = wwwAuthenticate;
                                headersArray[i].NameLength = (ushort)s_WwwAuthenticateBytes.Length;
                                headersArray[i].pRawValue = (sbyte*)Marshal.UnsafeAddrOfPinnedArrayElement(byteChallenge, 0);
                                headersArray[i].RawValueLength = checked((ushort)byteChallenge.Length);
                            }
                        }

                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpSendHtthttpResponse");
                        statusCode =
                            Interop.HttpApi.HttpSendHttpResponse(
                                _requestQueueHandle,
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
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Call to Interop.HttpApi.HttpSendHttpResponse returned:" + statusCode);
            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                // if we fail to send a 401 something's seriously wrong, abort the request
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "SendUnauthorized returned:" + statusCode);
                HttpListenerContext.CancelRequest(_requestQueueHandle, requestId);
            }
        }

        private unsafe static int GetTokenOffsetFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            IntPtr tokenPointer = Marshal.ReadIntPtr((IntPtr)blob, (int)Marshal.OffsetOf(s_channelBindingStatusType, "ChannelToken"));

            Debug.Assert(tokenPointer != IntPtr.Zero);
            return (int)((long)tokenPointer - (long)blob);
        }

        private unsafe static int GetTokenSizeFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            return Marshal.ReadInt32(blob, (int)Marshal.OffsetOf(s_channelBindingStatusType, "ChannelTokenSize"));
        }

        internal ChannelBinding GetChannelBindingFromTls(ulong connectionId)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, $"connectionId: {connectionId}");

            // +128 since a CBT is usually <128 thus we need to call HRCC just once. If the CBT
            // is >128 we will get ERROR_MORE_DATA and call again
            int size = s_requestChannelBindStatusSize + 128;

            Debug.Assert(size >= 0);

            byte[] blob = null;
            Interop.HttpApi.SafeLocalFreeChannelBinding token = null;

            uint bytesReceived = 0;
            uint statusCode;

            do
            {
                blob = new byte[size];
                fixed (byte* blobPtr = blob)
                {
                    // Http.sys team: ServiceName will always be null if 
                    // HTTP_RECEIVE_SECURE_CHANNEL_TOKEN flag is set.
                    statusCode = Interop.HttpApi.HttpReceiveClientCertificate(
                        RequestQueueHandle,
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
                        Debug.Assert(tokenSize < Int32.MaxValue);

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
                        Debug.Assert(tokenSize < Int32.MaxValue);

                        size = s_requestChannelBindStatusSize + tokenSize;
                    }
                    else if (statusCode == Interop.HttpApi.ERROR_INVALID_PARAMETER)
                    {
                        if (NetEventSource.IsEnabled)
                        {
                            NetEventSource.Error(this, SR.net_ssp_dont_support_cbt);
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

        internal void CheckDisposed()
        {
            if (_state == State.Closed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private enum State
        {
            Stopped,
            Started,
            Closed,
        }

        private const int DigestLifetimeSeconds = 300;
        private const int MaximumDigests = 1024;  // Must be a power of two.
        private const int MinimumDigestLifetimeSeconds = 10;

        private struct DigestContext
        {
            internal NTAuthentication context;
            internal int timestamp;
        }

        private DigestContext[] _savedDigests;
        private ArrayList _extraSavedDigests;
        private ArrayList _extraSavedDigestsBaking;
        private int _extraSavedDigestsTimestamp;
        private int _newestContext;
        private int _oldestContext;

        private void SaveDigestContext(NTAuthentication digestContext)
        {
            if (_savedDigests == null)
            {
                Interlocked.CompareExchange<DigestContext[]>(ref _savedDigests, new DigestContext[MaximumDigests], null);
            }

            // We want to actually close the contexts outside the lock.
            NTAuthentication oldContext = null;
            ArrayList digestsToClose = null;
            lock (_savedDigests)
            {
                // If we're stopped, just throw it away.
                if (!IsListening)
                {
                    digestContext.CloseContext();
                    return;
                }

                int now = ((now = Environment.TickCount) == 0 ? 1 : now);

                _newestContext = (_newestContext + 1) & (MaximumDigests - 1);

                int oldTimestamp = _savedDigests[_newestContext].timestamp;
                oldContext = _savedDigests[_newestContext].context;
                _savedDigests[_newestContext].timestamp = now;
                _savedDigests[_newestContext].context = digestContext;

                // May need to move this up.
                if (_oldestContext == _newestContext)
                {
                    _oldestContext = (_newestContext + 1) & (MaximumDigests - 1);
                }

                // Delete additional contexts older than five minutes.
                while (unchecked(now - _savedDigests[_oldestContext].timestamp) >= DigestLifetimeSeconds && _savedDigests[_oldestContext].context != null)
                {
                    if (digestsToClose == null)
                    {
                        digestsToClose = new ArrayList();
                    }
                    digestsToClose.Add(_savedDigests[_oldestContext].context);
                    _savedDigests[_oldestContext].context = null;
                    _oldestContext = (_oldestContext + 1) & (MaximumDigests - 1);
                }

                // If the old context is younger than 10 seconds, put it in the backup pile.
                if (oldContext != null && unchecked(now - oldTimestamp) <= MinimumDigestLifetimeSeconds * 1000)
                {
                    // Use a two-tier ArrayList system to guarantee each entry lives at least 10 seconds.
                    if (_extraSavedDigests == null ||
                        unchecked(now - _extraSavedDigestsTimestamp) > MinimumDigestLifetimeSeconds * 1000)
                    {
                        digestsToClose = _extraSavedDigestsBaking;
                        _extraSavedDigestsBaking = _extraSavedDigests;
                        _extraSavedDigestsTimestamp = now;
                        _extraSavedDigests = new ArrayList();
                    }
                    _extraSavedDigests.Add(oldContext);
                    oldContext = null;
                }
            }

            if (oldContext != null)
            {
                oldContext.CloseContext();
            }
            if (digestsToClose != null)
            {
                for (int i = 0; i < digestsToClose.Count; i++)
                {
                    ((NTAuthentication)digestsToClose[i]).CloseContext();
                }
            }
        }

        private void ClearDigestCache()
        {
            if (_savedDigests == null)
            {
                return;
            }

            ArrayList[] toClose = new ArrayList[3];
            lock (_savedDigests)
            {
                toClose[0] = _extraSavedDigestsBaking;
                _extraSavedDigestsBaking = null;
                toClose[1] = _extraSavedDigests;
                _extraSavedDigests = null;

                _newestContext = 0;
                _oldestContext = 0;

                toClose[2] = new ArrayList();
                for (int i = 0; i < MaximumDigests; i++)
                {
                    if (_savedDigests[i].context != null)
                    {
                        toClose[2].Add(_savedDigests[i].context);
                        _savedDigests[i].context = null;
                    }
                    _savedDigests[i].timestamp = 0;
                }
            }

            for (int j = 0; j < toClose.Length; j++)
            {
                if (toClose[j] != null)
                {
                    for (int k = 0; k < toClose[j].Count; k++)
                    {
                        ((NTAuthentication)toClose[j][k]).CloseContext();
                    }
                }
            }
        }

        private class DisconnectAsyncResult : IAsyncResult
        {
            private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(WaitCallback);

            private ulong _connectionId;
            private HttpListener _httpListener;
            private NativeOverlapped* _nativeOverlapped;
            private int _ownershipState;   // 0 = normal, 1 = in HandleAuthentication(), 2 = disconnected, 3 = cleaned up

            private WindowsPrincipal _authenticatedConnection;
            private NTAuthentication _session;

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

            internal unsafe DisconnectAsyncResult(HttpListener httpListener, ulong connectionId)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"HttpListener: {httpListener}, ConnectionId: {connectionId}");
                _ownershipState = 1;
                _httpListener = httpListener;
                _connectionId = connectionId;
                // we can call the Unsafe API here, we won't ever call user code
                _nativeOverlapped = httpListener._requestQueueBoundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: null);
            }

            internal bool StartOwningDisconnectHandling()
            {
                int oldValue;

                SpinWait spin = new SpinWait();
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
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, "_connectionId:" + asyncResult._connectionId);
                asyncResult._httpListener._requestQueueBoundHandle.FreeNativeOverlapped(nativeOverlapped);
                if (Interlocked.Exchange(ref asyncResult._ownershipState, 2) == 0)
                {
                    asyncResult.HandleDisconnect();
                }
            }

            private static unsafe void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"errorCode: {errorCode}, numBytes: {numBytes}, nativeOverlapped: {((IntPtr)nativeOverlapped).ToString("x")}");
                // take the DisconnectAsyncResult object from the state
                DisconnectAsyncResult asyncResult = (DisconnectAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
                IOCompleted(asyncResult, errorCode, numBytes, nativeOverlapped);
            }

            private void HandleDisconnect()
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"DisconnectResults {_httpListener.DisconnectResults} removing for _connectionId: {_connectionId}");
                _httpListener.DisconnectResults.Remove(_connectionId);
                if (_session != null)
                {
                    throw new NotImplementedException();
                }

                // Clean up the identity. This is for scenarios where identity was not cleaned up before due to
                // identity caching for unsafe ntlm authentication

                IDisposable identity = _authenticatedConnection == null ? null : _authenticatedConnection.Identity as IDisposable;
                if ((identity != null) &&
                    (_authenticatedConnection.Identity.AuthenticationType == AuthConstants.NTLM) &&
                    (_httpListener.UnsafeConnectionNtlmAuthentication))
                {
                    identity.Dispose();
                }

                int oldValue = Interlocked.Exchange(ref _ownershipState, 3);
                Debug.Assert(oldValue == 2, $"Expected OwnershipState of 2, saw {oldValue}.");
            }

            internal WindowsPrincipal AuthenticatedConnection
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

            internal NTAuthentication Session
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
