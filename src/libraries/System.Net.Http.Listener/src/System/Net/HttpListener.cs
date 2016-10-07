// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections;
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
    public sealed unsafe class HttpListener : IDisposable
    {
        private static readonly Type ChannelBindingStatusType = typeof(Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS);
        private static readonly int RequestChannelBindStatusSize =
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

        private static byte[] s_WwwAuthenticateBytes = new byte[]
        {
            (byte) 'W', (byte) 'W', (byte) 'W', (byte) '-', (byte) 'A', (byte) 'u', (byte) 't', (byte) 'h',
            (byte) 'e', (byte) 'n', (byte) 't', (byte) 'i', (byte) 'c', (byte) 'a', (byte) 't', (byte) 'e'
        };

        private AuthenticationSchemeSelector m_AuthenticationDelegate;
        private AuthenticationSchemes m_AuthenticationScheme = AuthenticationSchemes.Anonymous;
        private string m_Realm;
        private SafeHandle m_RequestQueueHandle;
        private ThreadPoolBoundHandle m_RequestQueueBoundHandle;
        private volatile State m_State; // m_State is set only within lock blocks, but often read outside locks. 
        private HttpListenerPrefixCollection m_Prefixes;
        private bool m_IgnoreWriteExceptions;
        private bool m_UnsafeConnectionNtlmAuthentication;
        private ExtendedProtectionSelector m_ExtendedProtectionSelectorDelegate;
        private ExtendedProtectionPolicy m_ExtendedProtectionPolicy;
        private ServiceNameStore m_DefaultServiceNames;
        private HttpServerSessionHandle m_ServerSessionHandle;
        private ulong m_UrlGroupId;
        private HttpListenerTimeoutManager m_TimeoutManager;
        private bool m_V2Initialized;

        private Hashtable m_DisconnectResults;         // ulong -> DisconnectAsyncResult
        private object m_InternalLock;

        internal Hashtable m_UriPrefixes = new Hashtable();

        public delegate ExtendedProtectionPolicy ExtendedProtectionSelector(HttpListenerRequest request);

        public HttpListener()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "HttpListener", "");

            m_State = State.Stopped;
            m_InternalLock = new object();
            m_DefaultServiceNames = new ServiceNameStore();

            m_TimeoutManager = new HttpListenerTimeoutManager(this);

            // default: no CBT checks on any platform (appcompat reasons); applies also to PolicyEnforcement 
            // config element
            m_ExtendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "HttpListener", "");
        }

        internal SafeHandle RequestQueueHandle
        {
            get
            {
                return m_RequestQueueHandle;
            }
        }

        internal ThreadPoolBoundHandle RequestQueueBoundHandle
        {
            get
            {
                return m_RequestQueueBoundHandle;
            }
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get
            {
                return m_AuthenticationDelegate;
            }
            set
            {
                CheckDisposed();
                m_AuthenticationDelegate = value;
            }
        }

        public ExtendedProtectionSelector ExtendedProtectionSelectorDelegate
        {
            get
            {
                return m_ExtendedProtectionSelectorDelegate;
            }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                m_ExtendedProtectionSelectorDelegate = value;
            }
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                return m_AuthenticationScheme;
            }
            set
            {
                CheckDisposed();
                m_AuthenticationScheme = value;
            }
        }

        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get
            {
                return m_ExtendedProtectionPolicy;
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

                m_ExtendedProtectionPolicy = value;
            }
        }

        public ServiceNameCollection DefaultServiceNames
        {
            get
            {
                return m_DefaultServiceNames.ServiceNames;
            }
        }

        public string Realm
        {
            get
            {
                return m_Realm;
            }
            set
            {
                CheckDisposed();
                m_Realm = value;
            }
        }

        private void ValidateV2Property()
        {
            // Make sure that calling CheckDisposed and SetupV2Config is an atomic operation. This 
            // avoids race conditions if the listener is aborted/closed after CheckDisposed(), but 
            // before SetupV2Config().
            lock (m_InternalLock)
            {
                CheckDisposed();
                SetupV2Config();
            }
        }

        private void SetUrlGroupProperty(Interop.HttpApi.HTTP_SERVER_PROPERTY property, IntPtr info, uint infosize)
        {
            uint statusCode = Interop.HttpApi.ERROR_SUCCESS;

            Debug.Assert(m_UrlGroupId != 0, "SetUrlGroupProperty called with invalid url group id");
            Debug.Assert(info != IntPtr.Zero, "SetUrlGroupProperty called with invalid pointer");

            //
            // Set the url group property using Http Api.
            //
            statusCode = Interop.HttpApi.HttpSetUrlGroupProperty(
                m_UrlGroupId, property, info, infosize);

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                HttpListenerException exception = new HttpListenerException((int)statusCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "HttpSetUrlGroupProperty:: Property: " +
                    property, exception);
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
                Debug.Assert(m_TimeoutManager != null, "Timeout manager is not assigned");
                return m_TimeoutManager;
            }
        }

        public static bool IsSupported
        {
            get
            {
                return Interop.HttpApi.Supported;
            }
        }

        public bool IsListening
        {
            get
            {
                return m_State == State.Started;
            }
        }

        public bool IgnoreWriteExceptions
        {
            get
            {
                return m_IgnoreWriteExceptions;
            }
            set
            {
                CheckDisposed();
                m_IgnoreWriteExceptions = value;
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get
            {
                return m_UnsafeConnectionNtlmAuthentication;
            }

            set
            {
                CheckDisposed();
                if (m_UnsafeConnectionNtlmAuthentication == value)
                {
                    return;
                }
                lock (DisconnectResults.SyncRoot)
                {
                    if (m_UnsafeConnectionNtlmAuthentication == value)
                    {
                        return;
                    }
                    m_UnsafeConnectionNtlmAuthentication = value;
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

        private Hashtable DisconnectResults
        {
            get
            {
                if (m_DisconnectResults == null)
                {
                    lock (m_InternalLock)
                    {
                        if (m_DisconnectResults == null)
                        {
                            m_DisconnectResults = Hashtable.Synchronized(new Hashtable());
                        }
                    }
                }
                return m_DisconnectResults;
            }
        }

        internal void AddPrefix(string uriPrefix)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "AddPrefix", "uriPrefix:" + uriPrefix);
            string registeredPrefix = null;
            try
            {
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException("uriPrefix");
                }
                CheckDisposed();
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::AddPrefix() uriPrefix:" + uriPrefix);
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
                        pChar[i] = (char)CaseInsensitiveAsciiComparer.AsciiToLower[(byte)pChar[i]];
                        i++;
                    }
                }
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::AddPrefix() mapped uriPrefix:" + uriPrefix + " to registeredPrefix:" + registeredPrefix);
                if (m_State == State.Started)
                {
                    GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::AddPrefix() calling Interop.HttpApi.HttpAddUrl[ToUrlGroup]");
                    uint statusCode = InternalAddPrefix(registeredPrefix);
                    if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                    {
                        if (statusCode == Interop.HttpApi.ERROR_ALREADY_EXISTS)
                            throw new HttpListenerException((int)statusCode, SR.Format(SR.net_listener_already, registeredPrefix));
                        else
                            throw new HttpListenerException((int)statusCode);
                    }
                }
                m_UriPrefixes[uriPrefix] = registeredPrefix;
                m_DefaultServiceNames.Add(uriPrefix);
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "AddPrefix", exception);
                throw;
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "AddPrefix", "prefix:" + registeredPrefix);
            }
        }

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Prefixes_get", "");
                CheckDisposed();
                if (m_Prefixes == null)
                {
                    m_Prefixes = new HttpListenerPrefixCollection(this);
                }
                return m_Prefixes;
            }
        }

        internal bool RemovePrefix(string uriPrefix)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "RemovePrefix", "uriPrefix:" + uriPrefix);
            try
            {
                CheckDisposed();
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::RemovePrefix() uriPrefix:" + uriPrefix);
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException("uriPrefix");
                }

                if (!m_UriPrefixes.Contains(uriPrefix))
                {
                    return false;
                }

                if (m_State == State.Started)
                {
                    InternalRemovePrefix((string)m_UriPrefixes[uriPrefix]);
                }

                m_UriPrefixes.Remove(uriPrefix);
                m_DefaultServiceNames.Remove(uriPrefix);
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "RemovePrefix", exception);
                throw;
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "RemovePrefix", "uriPrefix:" + uriPrefix);
            }
            return true;
        }

        internal void RemoveAll(bool clear)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "RemoveAll", "");
            try
            {
                CheckDisposed();
                // go through the uri list and unregister for each one of them
                if (m_UriPrefixes.Count > 0)
                {
                    if (m_State == State.Started)
                    {
                        foreach (string registeredPrefix in m_UriPrefixes.Values)
                        {
                            // ignore possible failures
                            InternalRemovePrefix(registeredPrefix);
                        }
                    }

                    if (clear)
                    {
                        m_UriPrefixes.Clear();
                        m_DefaultServiceNames.Clear();
                    }
                }
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "RemoveAll", "");
            }
        }

        private IntPtr DangerousGetHandle()
        {
            return ((HttpRequestQueueV2Handle)m_RequestQueueHandle).DangerousGetHandle();
        }

        internal void EnsureBoundHandle()
        {
            if (m_RequestQueueBoundHandle == null)
            {
                lock (m_InternalLock)
                {
                    if (m_RequestQueueBoundHandle == null)
                    {
                        m_RequestQueueBoundHandle = ThreadPoolBoundHandle.BindHandle(m_RequestQueueHandle);
                    }
                }
            }
        }

        private void SetupV2Config()
        {
            uint statusCode = Interop.HttpApi.ERROR_SUCCESS;
            ulong id = 0;

            //
            // If we have already initialized V2 config, then nothing to do.
            //
            if (m_V2Initialized)
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
                    Interop.HttpApi.Version, &id, 0);

                if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                {
                    throw new HttpListenerException((int)statusCode);
                }

                Debug.Assert(id != 0, "Invalid id returned by HttpCreateServerSession");

                m_ServerSessionHandle = new HttpServerSessionHandle(id);

                id = 0;
                statusCode = Interop.HttpApi.HttpCreateUrlGroup(
                    m_ServerSessionHandle.DangerousGetServerSessionId(), &id, 0);

                if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
                {
                    throw new HttpListenerException((int)statusCode);
                }

                Debug.Assert(id != 0, "Invalid id returned by HttpCreateUrlGroup");
                m_UrlGroupId = id;

                m_V2Initialized = true;
            }
            catch (Exception exception)
            {
                //
                // If V2 initialization fails, we mark object as unusable.
                //
                m_State = State.Closed;

                //
                // If Url group or request queue creation failed, close server session before throwing.
                //
                if (m_ServerSessionHandle != null)
                {
                    m_ServerSessionHandle.Dispose();
                }
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "SetupV2Config", exception);
                throw;
            }
        }

        public void Start()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Start", "");

            // Make sure there are no race conditions between Start/Stop/Abort/Close/Dispose and
            // calls to SetupV2Config: Start needs to setup all resources (esp. in V2 where besides
            // the request handle, there is also a server session and a Url group. Abort/Stop must
            // not interfere while Start is allocating those resources. The lock also makes sure
            // all methods changing state can read and change the state in an atomic way.
            lock (m_InternalLock)
            {
                try
                {
                    CheckDisposed();
                    if (m_State == State.Started)
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

                    m_State = State.Started;
                }
                catch (Exception exception)
                {
                    // Make sure the HttpListener instance can't be used if Start() failed.
                    m_State = State.Closed;
                    CloseRequestQueueHandle();
                    CleanupV2Config();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Start", exception);
                    throw;
                }
                finally
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Start", "");
                }
            }
        }

        private void CleanupV2Config()
        {

            //
            // If we never setup V2, just return.
            //
            if (!m_V2Initialized)
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

            Debug.Assert(m_UrlGroupId != 0, "HttpCloseUrlGroup called with invalid url group id");

            uint statusCode = Interop.HttpApi.HttpCloseUrlGroup(m_UrlGroupId);

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.HttpListener, this, "CloseV2Config", SR.Format(SR.net_listener_close_urlgroup_error, statusCode));
            }
            m_UrlGroupId = 0;

            Debug.Assert(m_ServerSessionHandle != null, "ServerSessionHandle is null in CloseV2Config");
            Debug.Assert(!m_ServerSessionHandle.IsInvalid, "ServerSessionHandle is invalid in CloseV2Config");

            m_ServerSessionHandle.Dispose();
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
            Debug.Assert(m_UrlGroupId != 0, "DetachRequestQueueFromUrlGroup can't detach using Url group id 0.");

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

            uint statusCode = Interop.HttpApi.HttpSetUrlGroupProperty(m_UrlGroupId,
                Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
                infoptr, (uint)Marshal.SizeOf(typeof(Interop.HttpApi.HTTP_BINDING_INFO)));

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.HttpListener, this, "DetachRequestQueueFromUrlGroup", SR.Format(SR.net_listener_detach_error, statusCode));
            }
        }

        public void Stop()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Stop", "");
            try
            {
                lock (m_InternalLock)
                {
                    CheckDisposed();
                    if (m_State == State.Stopped)
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

                    m_State = State.Stopped;
                }

                ClearDigestCache();
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Stop", exception);
                throw;
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Stop", "");
            }
        }

        private unsafe void CreateRequestQueueHandle()
        {
            uint statusCode = Interop.HttpApi.ERROR_SUCCESS;

            HttpRequestQueueV2Handle requestQueueHandle = null;
            statusCode =
                Interop.HttpApi.HttpCreateRequestQueue(
                    Interop.HttpApi.Version, null, null, 0, out requestQueueHandle);

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

            m_RequestQueueHandle = requestQueueHandle;
        }

        private unsafe void CloseRequestQueueHandle()
        {

            if ((m_RequestQueueHandle != null) && (!m_RequestQueueHandle.IsInvalid))
            {
                m_RequestQueueBoundHandle.Dispose();
                m_RequestQueueHandle.Dispose();
            }
        }

        public void Abort()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Abort", "");
            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::Abort()");

            lock (m_InternalLock)
            {
                try
                {
                    if (m_State == State.Closed)
                    {
                        return;
                    }

                    // Just detach and free resources. Don't call Stop (which may throw). Behave like v1: just 
                    // clean up resources.   
                    if (m_State == State.Started)
                    {
                        DetachRequestQueueFromUrlGroup();
                        CloseRequestQueueHandle();
                    }
                    CleanupV2Config();
                    ClearDigestCache();
                }
                catch (Exception exception)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Abort", exception);
                    throw;
                }
                finally
                {
                    m_State = State.Closed;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Abort", "");
                }
            }
        }

        public void Close()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Close", "");
            try
            {
                GlobalLog.Print("HttpListenerRequest::Close()");
                ((IDisposable)this).Dispose();
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Close", exception);
                throw;
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Close", "");
            }
        }

        // old API, now private, and helper methods
        private void Dispose(bool disposing)
        {
            Debug.Assert(disposing, "Dispose(bool) does nothing if called from the finalizer.");

            if (!disposing)
            {
                return;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Dispose", "");
            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::Dispose()");

            lock (m_InternalLock)
            {
                try
                {
                    if (m_State == State.Closed)
                    {
                        return;
                    }

                    Stop();
                    CleanupV2Config();
                }
                catch (Exception exception)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Dispose", exception);
                    throw;
                }
                finally
                {
                    m_State = State.Closed;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Dispose", "");
                }
            }
        }

        /// <internalonly/>
        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        private uint InternalAddPrefix(string uriPrefix)
        {
            uint statusCode = 0;

            statusCode =
                Interop.HttpApi.HttpAddUrlToUrlGroup(
                    m_UrlGroupId,
                    uriPrefix,
                    0,
                    0);

            return statusCode;
        }

        private bool InternalRemovePrefix(string uriPrefix)
        {
            uint statusCode = 0;

            statusCode =
                Interop.HttpApi.HttpRemoveUrlFromUrlGroup(
                    m_UrlGroupId,
                    uriPrefix,
                    0);

            if (statusCode == Interop.HttpApi.ERROR_NOT_FOUND)
            {
                return false;
            }
            return true;
        }

        private void AddAllPrefixes()
        {
            // go through the uri list and register for each one of them
            if (m_UriPrefixes.Count > 0)
            {
                foreach (string registeredPrefix in m_UriPrefixes.Values)
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
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "GetContext", "");

            SyncRequestContext memoryBlob = null;
            HttpListenerContext httpContext = null;
            bool stoleBlob = false;

            try
            {
                CheckDisposed();
                if (m_State == State.Stopped)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "Start()"));
                }
                if (m_UriPrefixes.Count == 0)
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
                        GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::GetContext() calling Interop.HttpApi.HttpReceiveHttpRequest RequestId:" + requestId);
                        uint bytesTransferred = 0;
                        statusCode =
                            Interop.HttpApi.HttpReceiveHttpRequest(
                                m_RequestQueueHandle,
                                requestId,
                                (uint)Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                                memoryBlob.RequestBlob,
                                size,
                                &bytesTransferred,
                                null);

                        GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::GetContext() call to Interop.HttpApi.HttpReceiveHttpRequest returned:" + statusCode);

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
                        // someother bad error, possible(?) return values are:
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
                    GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::GetContext() HandleAuthentication() returned httpContext#" + LoggingHash.HashString(httpContext));
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "GetContext", exception);
                throw;
            }
            finally
            {
                if (memoryBlob != null && !stoleBlob)
                {
                    memoryBlob.ReleasePins();
                    memoryBlob.Close();
                }
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "GetContext", "HttpListenerContext#" + LoggingHash.HashString(httpContext) + " RequestTraceIdentifier#" + (httpContext != null ? httpContext.Request.RequestTraceIdentifier.ToString() : "<null>"));
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
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "BeginGetContext", "");
            ListenerAsyncResult asyncResult = null;
            try
            {
                CheckDisposed();
                if (m_State == State.Stopped)
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
                    // someother bad error, possible(?) return values are:
                    // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                    throw new HttpListenerException((int)statusCode);
                }
            }
            catch (Exception exception)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "BeginGetContext", exception);
                throw;
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "BeginGetContext", "IAsyncResult#" + LoggingHash.HashString(asyncResult));
            }

            return asyncResult;
        }

        public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "EndGetContext", "IAsyncResult#" + LoggingHash.HashString(asyncResult));
            HttpListenerContext httpContext = null;
            try
            {
                CheckDisposed();
                if (asyncResult == null)
                {
                    throw new ArgumentNullException("asyncResult");
                }
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::EndGetContext() asyncResult:" + LoggingHash.ObjectToString(asyncResult));
                ListenerAsyncResult castedAsyncResult = asyncResult as ListenerAsyncResult;
                if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this)
                {
                    throw new ArgumentException(SR.net_io_invalidasyncresult, "asyncResult");
                }
                if (castedAsyncResult.EndCalled)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetContext"));
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "EndGetContext", exception);
                throw;
            }
            finally
            {
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::EndGetContext() returning HttpListenerContext#" + LoggingHash.ObjectToString(httpContext));
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "EndGetContext", httpContext == null ? "<no context>" : "HttpListenerContext#" + LoggingHash.HashString(httpContext) + " RequestTraceIdentifier#" + httpContext.Request.RequestTraceIdentifier);
            }
            return httpContext;
        }

        //************* Task-based async public methods *************************
        public Task<HttpListenerContext> GetContextAsync()
        {
            return Task<HttpListenerContext>.Factory.FromAsync(BeginGetContext, EndGetContext, null);
        }

        internal HttpListenerContext HandleAuthentication(RequestContextBase memoryBlob, out bool stoleBlob)
        {
            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() memoryBlob:0x" + ((IntPtr)memoryBlob.RequestBlob).ToString("x"));

            string challenge = null;
            stoleBlob = false;

            // Some things we need right away.  Lift them out now while it's convenient.
            string verb = Interop.HttpApi.GetVerb(memoryBlob.RequestBlob);
            string authorizationHeader = Interop.HttpApi.GetKnownHeader(memoryBlob.RequestBlob, (int)HttpRequestHeader.Authorization);
            ulong connectionId = memoryBlob.RequestBlob->ConnectionId;
            ulong requestId = memoryBlob.RequestBlob->RequestId;
            bool isSecureConnection = memoryBlob.RequestBlob->pSslInfo != null;

            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() authorizationHeader:" + LoggingHash.ObjectToString(authorizationHeader));

            // if the app has turned on AuthPersistence, an anonymous request might
            // be authenticated by virtue of it coming on a connection that was
            // previously authenticated.
            // assurance that we do this only for NTLM/Negotiate is not here, but in the
            // code that caches WindowsIdentity instances in the Dictionary.
            DisconnectAsyncResult disconnectResult = (DisconnectAsyncResult)DisconnectResults[connectionId];
            if (UnsafeConnectionNtlmAuthentication)
            {
                if (authorizationHeader == null)
                {
                    WindowsPrincipal principal = disconnectResult == null ? null : disconnectResult.AuthenticatedConnection;
                    if (principal != null)
                    {
                        GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() got principal:" + LoggingHash.ObjectToString(principal) + " principal.Identity.Name:" + LoggingHash.ObjectToString(principal.Identity.Name) + " creating request");
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
                    GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() clearing principal cache");
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
            ExtendedProtectionPolicy extendedProtectionPolicy = m_ExtendedProtectionPolicy;
            try
            {
                // Take over handling disconnects for now.
                if (disconnectResult != null && !disconnectResult.StartOwningDisconnectHandling())
                {
                    // Oops!  Just disconnected just then.  Pretend we didn't see the disconnectResult.
                    disconnectResult = null;
                }

                // Pick out the old context now.  By default, it'll be removed in the finally, unless context is set somewhere. 
                if (disconnectResult != null)
                {
                    oldContext = disconnectResult.Session;
                }

                httpContext = new HttpListenerContext(this, memoryBlob);

                AuthenticationSchemeSelector authenticationSelector = m_AuthenticationDelegate;
                if (authenticationSelector != null)
                {
                    try
                    {
                        httpContext.Request.ReleasePins();
                        authenticationScheme = authenticationSelector(httpContext.Request);
                        // Cache the results of authenticationSelector (if any)
                        httpContext.AuthenticationSchemes = authenticationScheme;
                        GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() AuthenticationSchemeSelectorDelegate() returned authenticationScheme:" + authenticationScheme);
                    }
                    catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.HttpListener, this, "HandleAuthentication", SR.Format(SR.net_log_listener_delegate_exception, exception));
                        GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() AuthenticationSchemeSelectorDelegate() returned authenticationScheme:" + authenticationScheme);
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

                ExtendedProtectionSelector extendedProtectionSelector = m_ExtendedProtectionSelectorDelegate;
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
                            string.Compare(authorizationHeader, 0, "Negotiate", 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Negotiate;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Ntlm) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, "NTLM", 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Ntlm;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Digest) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, "Digest", 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Digest;
                        }
                        else if ((authenticationScheme & AuthenticationSchemes.Basic) != AuthenticationSchemes.None &&
                            string.Compare(authorizationHeader, 0, "Basic", 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            headerScheme = AuthenticationSchemes.Basic;
                        }
                        else
                        {
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.HttpListener, this, "HandleAuthentication", SR.Format(SR.net_log_listener_unsupported_authentication_scheme, authorizationHeader, authenticationScheme));
                        }
                    }
                }

                // httpError holds the error we will return if an Authorization header is present but can't be authenticated
                HttpStatusCode httpError = HttpStatusCode.InternalServerError;

                // See if we found an acceptable auth header
                if (headerScheme == AuthenticationSchemes.None)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintError(NetEventSource.ComponentType.HttpListener, this, "HandleAuthentication", SR.Format(SR.net_log_listener_unmatched_authentication_scheme, LoggingHash.ObjectToString(authenticationScheme), (authorizationHeader == null ? "<null>" : authorizationHeader)));

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
                            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() failed context#" + LoggingHash.HashString(context) + " for connectionId:" + connectionId + " because of error:" + httpError.ToString());
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

                        GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() failed context#" + LoggingHash.HashString(context) + " for connectionId:" + connectionId + " because of failed HttpWaitForDisconnect");
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
                    GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() SendUnauthorized(Scheme:" + authenticationScheme + ")");
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
            ArrayList challenges = BuildChallenge(context.AuthenticationSchemes, request.m_ConnectionId,
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
                foreach (String challenge in challenges)
                {
                    response.Headers[HttpKnownHeaderNames.WWWAuthenticate] = challenge;
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
                    GlobalLog.Print("HttpListener:AddChallenge() challenge:" + challenge);
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
            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::BuildChallenge()  authenticationScheme:" + authenticationScheme.ToString());
            ArrayList challenges = null;
            newContext = null;

            if ((authenticationScheme & AuthenticationSchemes.Negotiate) != 0)
            {
                AddChallenge(ref challenges, "Negotiate");
            }

            if ((authenticationScheme & AuthenticationSchemes.Ntlm) != 0)
            {
                AddChallenge(ref challenges, "NTLM");
            }

            if ((authenticationScheme & AuthenticationSchemes.Digest) != 0)
            {
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::BuildChallenge() package:WDigest");

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
            Debug.Assert(disconnectResult == null, "HttpListener#{0}::RegisterForDisconnectNotification()|Called with a disconnectResult.", LoggingHash.HashString(this));

            try
            {
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::RegisterForDisconnectNotification() calling Interop.HttpApi.HttpWaitForDisconnect");

                DisconnectAsyncResult result = new DisconnectAsyncResult(this, connectionId);

                EnsureBoundHandle();
                uint statusCode = Interop.HttpApi.HttpWaitForDisconnect(
                    m_RequestQueueHandle,
                    connectionId,
                    result.NativeOverlapped);

                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::RegisterForDisconnectNotification() call to Interop.HttpApi.HttpWaitForDisconnect returned:" + statusCode);

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
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::RegisterForDisconnectNotification() call to Interop.HttpApi.HttpWaitForDisconnect threw.  statusCode:" + statusCode);
            }
        }

        private void SendError(ulong requestId, HttpStatusCode httpStatusCode, ArrayList challenges)
        {
            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::SendInternalError() requestId:" + LoggingHash.ObjectToString(requestId));
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

                        GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::SendInternalError() calling Interop.HttpApi.HttpSendHtthttpResponse");
                        statusCode =
                            Interop.HttpApi.HttpSendHttpResponse(
                                m_RequestQueueHandle,
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
            GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::SendInternalError() call to Interop.HttpApi.HttpSendHttpResponse returned:" + statusCode);
            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                // if we fail to send a 401 something's seriously wrong, abort the request
                GlobalLog.Print("HttpListener#" + LoggingHash.HashString(this) + "::HandleAuthentication() SendUnauthorized() returned:" + statusCode);
                HttpListenerContext.CancelRequest(m_RequestQueueHandle, requestId);
            }
        }

        private unsafe static int GetTokenOffsetFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            IntPtr tokenPointer = Marshal.ReadIntPtr((IntPtr)blob, (int)Marshal.OffsetOf(ChannelBindingStatusType, "ChannelToken"));

            Debug.Assert(tokenPointer != IntPtr.Zero);
            return (int)((long)tokenPointer - (long)blob);
        }

        private unsafe static int GetTokenSizeFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            return Marshal.ReadInt32(blob, (int)Marshal.OffsetOf(ChannelBindingStatusType, "ChannelTokenSize"));
        }

        internal ChannelBinding GetChannelBindingFromTls(ulong connectionId)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, "HttpListener#" + LoggingHash.HashString(this) +
                    "::GetChannelBindingFromTls() connectionId: " + connectionId.ToString(), nameof(GetChannelBindingFromTls), null);
            }

            // +128 since a CBT is usually <128 thus we need to call HRCC just once. If the CBT
            // is >128 we will get ERROR_MORE_DATA and call again
            int size = RequestChannelBindStatusSize + 128;

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

                        size = RequestChannelBindStatusSize + tokenSize;
                    }
                    else if (statusCode == Interop.HttpApi.ERROR_INVALID_PARAMETER)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.PrintError(NetEventSource.ComponentType.HttpListener, "HttpListener#" +
                                LoggingHash.HashString(this) + "::GetChannelBindingFromTls() " +
                                SR.net_ssp_dont_support_cbt);
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
            if (m_State == State.Closed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        enum State
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

        private DigestContext[] m_SavedDigests;
        private ArrayList m_ExtraSavedDigests;
        private ArrayList m_ExtraSavedDigestsBaking;
        private int m_ExtraSavedDigestsTimestamp;
        private int m_NewestContext;
        private int m_OldestContext;

        private void SaveDigestContext(NTAuthentication digestContext)
        {
            if (m_SavedDigests == null)
            {
                Interlocked.CompareExchange<DigestContext[]>(ref m_SavedDigests, new DigestContext[MaximumDigests], null);
            }

            // We want to actually close the contexts outside the lock.
            NTAuthentication oldContext = null;
            ArrayList digestsToClose = null;
            lock (m_SavedDigests)
            {
                // If we're stopped, just throw it away.
                if (!IsListening)
                {
                    digestContext.CloseContext();
                    return;
                }

                int now = ((now = Environment.TickCount) == 0 ? 1 : now);

                m_NewestContext = (m_NewestContext + 1) & (MaximumDigests - 1);

                int oldTimestamp = m_SavedDigests[m_NewestContext].timestamp;
                oldContext = m_SavedDigests[m_NewestContext].context;
                m_SavedDigests[m_NewestContext].timestamp = now;
                m_SavedDigests[m_NewestContext].context = digestContext;

                // May need to move this up.
                if (m_OldestContext == m_NewestContext)
                {
                    m_OldestContext = (m_NewestContext + 1) & (MaximumDigests - 1);
                }

                // Delete additional contexts older than five minutes.
                while (unchecked(now - m_SavedDigests[m_OldestContext].timestamp) >= DigestLifetimeSeconds && m_SavedDigests[m_OldestContext].context != null)
                {
                    if (digestsToClose == null)
                    {
                        digestsToClose = new ArrayList();
                    }
                    digestsToClose.Add(m_SavedDigests[m_OldestContext].context);
                    m_SavedDigests[m_OldestContext].context = null;
                    m_OldestContext = (m_OldestContext + 1) & (MaximumDigests - 1);
                }

                // If the old context is younger than 10 seconds, put it in the backup pile.
                if (oldContext != null && unchecked(now - oldTimestamp) <= MinimumDigestLifetimeSeconds * 1000)
                {
                    // Use a two-tier ArrayList system to guarantee each entry lives at least 10 seconds.
                    if (m_ExtraSavedDigests == null ||
                        unchecked(now - m_ExtraSavedDigestsTimestamp) > MinimumDigestLifetimeSeconds * 1000)
                    {
                        digestsToClose = m_ExtraSavedDigestsBaking;
                        m_ExtraSavedDigestsBaking = m_ExtraSavedDigests;
                        m_ExtraSavedDigestsTimestamp = now;
                        m_ExtraSavedDigests = new ArrayList();
                    }
                    m_ExtraSavedDigests.Add(oldContext);
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
            if (m_SavedDigests == null)
            {
                return;
            }

            ArrayList[] toClose = new ArrayList[3];
            lock (m_SavedDigests)
            {
                toClose[0] = m_ExtraSavedDigestsBaking;
                m_ExtraSavedDigestsBaking = null;
                toClose[1] = m_ExtraSavedDigests;
                m_ExtraSavedDigests = null;

                m_NewestContext = 0;
                m_OldestContext = 0;

                toClose[2] = new ArrayList();
                for (int i = 0; i < MaximumDigests; i++)
                {
                    if (m_SavedDigests[i].context != null)
                    {
                        toClose[2].Add(m_SavedDigests[i].context);
                        m_SavedDigests[i].context = null;
                    }
                    m_SavedDigests[i].timestamp = 0;
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

        class DisconnectAsyncResult : IAsyncResult
        {
            private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(WaitCallback);

            private ulong m_ConnectionId;
            private HttpListener m_HttpListener;
            NativeOverlapped* m_NativeOverlapped;
            private int m_OwnershipState;   // 0 = normal, 1 = in HandleAuthentication(), 2 = disconnected, 3 = cleaned up

            private WindowsPrincipal m_AuthenticatedConnection;
            private NTAuthentication m_Session;

            internal const string NTLM = "NTLM";

            internal NativeOverlapped* NativeOverlapped
            {
                get
                {
                    return m_NativeOverlapped;
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
                GlobalLog.Print("DisconnectAsyncResult#" + LoggingHash.HashString(this) + "::.ctor() httpListener#" + LoggingHash.HashString(httpListener) + " connectionId:" + connectionId);
                m_OwnershipState = 1;
                m_HttpListener = httpListener;
                m_ConnectionId = connectionId;
                // we can call the Unsafe API here, we won't ever call user code
                m_NativeOverlapped = httpListener.m_RequestQueueBoundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: null);
            }

            internal bool StartOwningDisconnectHandling()
            {
                int oldValue;

                SpinWait spin = new SpinWait();
                while ((oldValue = Interlocked.CompareExchange(ref m_OwnershipState, 1, 0)) == 2)
                {
                    // Must block until it equals 3 - we must be in the callback right now.
                    spin.SpinOnce();
                }

                Debug.Assert(oldValue != 1, "DisconnectAsyncResult#{0}::HandleDisconnect()|StartOwningDisconnectHandling() called twice.", LoggingHash.HashString(this));
                return oldValue < 2;
            }

            internal void FinishOwningDisconnectHandling()
            {
                // If it got disconnected, run the disconnect code.
                if (Interlocked.CompareExchange(ref m_OwnershipState, 0, 1) == 2)
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
                GlobalLog.Print("DisconnectAsyncResult#" + LoggingHash.HashString(asyncResult) + "::WaitCallback() m_ConnectionId:" + asyncResult.m_ConnectionId);
                asyncResult.m_HttpListener.m_RequestQueueBoundHandle.FreeNativeOverlapped(nativeOverlapped);
                if (Interlocked.Exchange(ref asyncResult.m_OwnershipState, 2) == 0)
                {
                    asyncResult.HandleDisconnect();
                }
            }

            private static unsafe void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                GlobalLog.Print("DisconnectAsyncResult::WaitCallback() errorCode:" + errorCode + " numBytes:" + numBytes + " nativeOverlapped:" + ((IntPtr)nativeOverlapped).ToString("x"));
                // take the DisconnectAsyncResult object from the state
                DisconnectAsyncResult asyncResult = (DisconnectAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
                IOCompleted(asyncResult, errorCode, numBytes, nativeOverlapped);
            }

            private void HandleDisconnect()
            {
                GlobalLog.Print("DisconnectAsyncResult#" + LoggingHash.HashString(this) + "::HandleDisconnect() DisconnectResults#" + LoggingHash.HashString(m_HttpListener.DisconnectResults) + " removing for m_ConnectionId:" + m_ConnectionId);
                m_HttpListener.DisconnectResults.Remove(m_ConnectionId);
                if (m_Session != null)
                {
                    throw new NotImplementedException();
                }

                // Clean up the identity. This is for scenarios where identity was not cleaned up before due to
                // identity caching for unsafe ntlm authentication

                IDisposable identity = m_AuthenticatedConnection == null ? null : m_AuthenticatedConnection.Identity as IDisposable;
                if ((identity != null) &&
                    (m_AuthenticatedConnection.Identity.AuthenticationType == NTLM) &&
                    (m_HttpListener.UnsafeConnectionNtlmAuthentication))
                {
                    identity.Dispose();
                }

                int oldValue = Interlocked.Exchange(ref m_OwnershipState, 3);
                Debug.Assert(oldValue == 2, "DisconnectAsyncResult#{0}::HandleDisconnect()|Expected OwnershipState of 2, saw {1}.", LoggingHash.HashString(this), oldValue);
            }

            internal WindowsPrincipal AuthenticatedConnection
            {
                get
                {
                    return m_AuthenticatedConnection;
                }

                set
                {
                    // The previous value can't be disposed because it may be in use by the app.
                    m_AuthenticatedConnection = value;
                }
            }

            internal NTAuthentication Session
            {
                get
                {
                    return m_Session;
                }

                set
                {
                    m_Session = value;
                }
            }
        }
    }
}
