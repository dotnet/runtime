// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    public sealed unsafe class HttpListenerResponse : IDisposable
    {
        enum ResponseState
        {
            Created,
            ComputedHeaders,
            SentHeaders,
            Closed,
        }

        private Encoding m_ContentEncoding;
        private CookieCollection m_Cookies;

        private string m_StatusDescription;
        private bool m_KeepAlive;
        private ResponseState m_ResponseState;
        private WebHeaderCollection m_WebHeaders;
        private HttpResponseStream m_ResponseStream;
        private long m_ContentLength;
        private BoundaryType m_BoundaryType;
        private Interop.HttpApi.HTTP_RESPONSE m_NativeResponse;

        private HttpListenerContext m_HttpContext;

        internal HttpListenerResponse()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, ".ctor", "");
            m_NativeResponse = new Interop.HttpApi.HTTP_RESPONSE();
            m_WebHeaders = new WebHeaderCollection();
            m_BoundaryType = BoundaryType.None;
            m_NativeResponse.StatusCode = (ushort)HttpStatusCode.OK;
            m_NativeResponse.Version.MajorVersion = 1;
            m_NativeResponse.Version.MinorVersion = 1;
            m_KeepAlive = true;
            m_ResponseState = ResponseState.Created;
        }

        internal HttpListenerResponse(HttpListenerContext httpContext) : this()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(NetEventSource.ComponentType.HttpListener, this, httpContext);
            m_HttpContext = httpContext;
        }

        private HttpListenerContext HttpListenerContext
        {
            get
            {
                return m_HttpContext;
            }
        }

        private HttpListenerRequest HttpListenerRequest
        {
            get
            {
                return HttpListenerContext.Request;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                return m_ContentEncoding;
            }
            set
            {
                m_ContentEncoding = value;
            }
        }

        public string ContentType
        {
            get
            {
                return Headers[HttpKnownHeaderNames.ContentType];
            }
            set
            {
                CheckDisposed();
                if (string.IsNullOrEmpty(value))
                {
                    Headers.Remove(HttpKnownHeaderNames.ContentType);
                }
                else
                {
                    Headers[HttpKnownHeaderNames.ContentType] = value;
                }
            }
        }

        public Stream OutputStream
        {
            get
            {
                CheckDisposed();
                EnsureResponseStream();
                return m_ResponseStream;
            }
        }

        public string RedirectLocation
        {
            get
            {
                return Headers[HttpResponseHeader.Location];
            }
            set
            {
                // note that this doesn't set the status code to a redirect one
                CheckDisposed();
                if (string.IsNullOrEmpty(value))
                {
                    Headers.Remove(HttpKnownHeaderNames.Location);
                }
                else
                {
                    Headers[HttpKnownHeaderNames.Location] = value;
                }
            }
        }

        public int StatusCode
        {
            get
            {
                return (int)m_NativeResponse.StatusCode;
            }
            set
            {
                CheckDisposed();
                if (value < 100 || value > 999)
                {
                    throw new ProtocolViolationException(SR.net_invalidstatus);
                }
                m_NativeResponse.StatusCode = (ushort)value;
            }
        }

        public string StatusDescription
        {
            get
            {
                if (m_StatusDescription == null)
                {
                    // if the user hasn't set this, generated on the fly, if possible.
                    // We know this one is safe, no need to verify it as in the setter.
                    m_StatusDescription = HttpStatusDescription.Get(StatusCode);
                }
                if (m_StatusDescription == null)
                {
                    m_StatusDescription = string.Empty;
                }
                return m_StatusDescription;
            }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                // Need to verify the status description doesn't contain any control characters except HT.  We mask off the high
                // byte since that's how it's encoded.
                for (int i = 0; i < value.Length; i++)
                {
                    char c = (char)(0x000000ff & (uint)value[i]);
                    if ((c <= 31 && c != (byte)'\t') || c == 127)
                    {
                        throw new ArgumentException(SR.net_WebHeaderInvalidControlChars, "name");
                    }
                }

                m_StatusDescription = value;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                if (m_Cookies == null)
                {
                    m_Cookies = new CookieCollection();
                }
                return m_Cookies;
            }
            set
            {
                m_Cookies = value;
            }
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, "CopyFrom", "templateResponse#" + LoggingHash.HashString(templateResponse));
            m_NativeResponse = new Interop.HttpApi.HTTP_RESPONSE();
            m_ResponseState = ResponseState.Created;
            m_WebHeaders = templateResponse.m_WebHeaders;
            m_BoundaryType = templateResponse.m_BoundaryType;
            m_ContentLength = templateResponse.m_ContentLength;
            m_NativeResponse.StatusCode = templateResponse.m_NativeResponse.StatusCode;
            m_NativeResponse.Version.MajorVersion = templateResponse.m_NativeResponse.Version.MajorVersion;
            m_NativeResponse.Version.MinorVersion = templateResponse.m_NativeResponse.Version.MinorVersion;
            m_StatusDescription = templateResponse.m_StatusDescription;
            m_KeepAlive = templateResponse.m_KeepAlive;
        }

        public bool SendChunked
        {
            get
            {
                return (EntitySendFormat == EntitySendFormat.Chunked);
            }
            set
            {
                if (value)
                {
                    EntitySendFormat = EntitySendFormat.Chunked;
                }
                else
                {
                    EntitySendFormat = EntitySendFormat.ContentLength;
                }
            }
        }

        // We MUST NOT send message-body when we send responses with these Status codes
        private static readonly int[] s_NoResponseBody = { 100, 101, 204, 205, 304 };

        private bool CanSendResponseBody(int responseCode)
        {
            for (int i = 0; i < s_NoResponseBody.Length; i++)
            {
                if (responseCode == s_NoResponseBody[i])
                {
                    return false;
                }
            }
            return true;
        }

        internal EntitySendFormat EntitySendFormat
        {
            get
            {
                return (EntitySendFormat)m_BoundaryType;
            }
            set
            {
                CheckDisposed();
                if (m_ResponseState >= ResponseState.SentHeaders)
                {
                    throw new InvalidOperationException(SR.net_rspsubmitted);
                }
                if (value == EntitySendFormat.Chunked && HttpListenerRequest.ProtocolVersion.Minor == 0)
                {
                    throw new ProtocolViolationException(SR.net_nochunkuploadonhttp10);
                }
                m_BoundaryType = (BoundaryType)value;
                if (value != EntitySendFormat.ContentLength)
                {
                    m_ContentLength = -1;
                }
            }
        }

        public bool KeepAlive
        {
            get
            {
                return m_KeepAlive;
            }
            set
            {
                CheckDisposed();
                m_KeepAlive = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return m_WebHeaders;
            }
            set
            {
                m_WebHeaders = new WebHeaderCollection();
                foreach (string headerName in value.AllKeys)
                {
                    m_WebHeaders[headerName] = value[headerName];
                }
            }
        }

        public void AddHeader(string name, string value)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, "AddHeader", " name=" + name + " value=" + value);
            Headers[name] = value;
        }

        public void AppendHeader(string name, string value)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, "AppendHeader", " name=" + name + " value=" + value);
            Headers[name] = value;
        }

        public void Redirect(string url)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, "Redirect", " url=" + url);
            Headers[HttpResponseHeader.Location] = url;
            StatusCode = (int)HttpStatusCode.Redirect;
            StatusDescription = HttpStatusDescription.Get(StatusCode);
        }

        public void AppendCookie(Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException("cookie");
            }
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, "AppendCookie", " cookie#" + LoggingHash.HashString(cookie));
            Cookies.Add(cookie);
        }

        public void SetCookie(Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException("cookie");
            }
            bool added = false;
            try
            {
                Cookies.Add(cookie);
                added = true;
            }
            catch (CookieException)
            {
                Debug.Assert(!added);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, "SetCookie", " cookie#" + LoggingHash.HashString(cookie));

            if (!added)
            {
                // cookie already existed and couldn't be replaced
                throw new ArgumentException(SR.net_cookie_exists, "cookie");
            }
        }

        public long ContentLength64
        {
            get
            {
                return m_ContentLength;
            }
            set
            {
                CheckDisposed();
                if (m_ResponseState >= ResponseState.SentHeaders)
                {
                    throw new InvalidOperationException(SR.net_rspsubmitted);
                }
                if (value >= 0)
                {
                    m_ContentLength = value;
                    m_BoundaryType = BoundaryType.ContentLength;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", SR.net_clsmall);
                }
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return new Version(m_NativeResponse.Version.MajorVersion, m_NativeResponse.Version.MinorVersion);
            }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                {
                    throw new ArgumentException(SR.net_wrongversion, "value");
                }
                m_NativeResponse.Version.MajorVersion = (ushort)value.Major;
                m_NativeResponse.Version.MinorVersion = (ushort)value.Minor;
            }
        }

        public void Abort()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "abort", "");
            try
            {
                if (m_ResponseState >= ResponseState.Closed)
                {
                    return;
                }

                m_ResponseState = ResponseState.Closed;
                HttpListenerContext.Abort();
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "abort", "");
            }
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Close", " responseEntity=" + LoggingHash.HashString(responseEntity) + " willBlock=" + willBlock);
            try
            {
                CheckDisposed();
                if (responseEntity == null)
                {
                    throw new ArgumentNullException("responseEntity");
                }
                GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::Close() ResponseState:" + m_ResponseState + " BoundaryType:" + m_BoundaryType + " ContentLength:" + m_ContentLength);
                if (m_ResponseState < ResponseState.SentHeaders && m_BoundaryType != BoundaryType.Chunked)
                {
                    ContentLength64 = responseEntity.Length;
                }
                EnsureResponseStream();
                if (willBlock)
                {
                    try
                    {
                        m_ResponseStream.Write(responseEntity, 0, responseEntity.Length);
                    }
                    catch (Win32Exception)
                    {
                    }
                    finally
                    {
                        m_ResponseStream.Close();
                        m_ResponseState = ResponseState.Closed;
                        HttpListenerContext.Close();
                    }
                }
                else
                {
                    // <CONSIDER>
                    // make this call unsafe, since we don't call user's code in the callback
                    // </CONSIDER>
                    m_ResponseStream.BeginWrite(responseEntity, 0, responseEntity.Length, new AsyncCallback(NonBlockingCloseCallback), null);
                }
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Close", "");
            }
        }

        public void Close()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Close", "");
            try
            {
                GlobalLog.Print("HttpListenerResponse::Close()");
                ((IDisposable)this).Dispose();
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Close", "");
            }
        }

        private void Dispose(bool disposing)
        {
            if (m_ResponseState >= ResponseState.Closed)
            {
                return;
            }
            EnsureResponseStream();
            m_ResponseStream.Close();
            m_ResponseState = ResponseState.Closed;

            HttpListenerContext.Close();
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // old API, now private, and helper methods

        internal BoundaryType BoundaryType
        {
            get
            {
                return m_BoundaryType;
            }
        }

        internal bool SentHeaders
        {
            get
            {
                return m_ResponseState >= ResponseState.SentHeaders;
            }
        }

        internal bool ComputedHeaders
        {
            get
            {
                return m_ResponseState >= ResponseState.ComputedHeaders;
            }
        }

        private void EnsureResponseStream()
        {
            if (m_ResponseStream == null)
            {
                m_ResponseStream = new HttpResponseStream(HttpListenerContext);
            }
        }

        private void NonBlockingCloseCallback(IAsyncResult asyncResult)
        {
            try
            {
                m_ResponseStream.EndWrite(asyncResult);
            }
            catch (Win32Exception)
            {
            }
            finally
            {
                m_ResponseStream.Close();
                HttpListenerContext.Close();
                m_ResponseState = ResponseState.Closed;
            }
        }

        /*
        12.3
        HttpSendHttpResponse() and HttpSendResponseEntityBody() Flag Values.
        The following flags can be used on calls to HttpSendHttpResponse() and HttpSendResponseEntityBody() API calls:

        #define HTTP_SEND_RESPONSE_FLAG_DISCONNECT          0x00000001
        #define HTTP_SEND_RESPONSE_FLAG_MORE_DATA           0x00000002
        #define HTTP_SEND_RESPONSE_FLAG_RAW_HEADER          0x00000004
        #define HTTP_SEND_RESPONSE_FLAG_VALID               0x00000007

        HTTP_SEND_RESPONSE_FLAG_DISCONNECT:
            specifies that the network connection should be disconnected immediately after
            sending the response, overriding the HTTP protocol's persistent connection features.
        HTTP_SEND_RESPONSE_FLAG_MORE_DATA:
            specifies that additional entity body data will be sent by the caller. Thus,
            the last call HttpSendResponseEntityBody for a RequestId, will have this flag reset.
        HTTP_SEND_RESPONSE_RAW_HEADER:
            specifies that a caller of HttpSendResponseEntityBody() is intentionally omitting
            a call to HttpSendHttpResponse() in order to bypass normal header processing. The
            actual HTTP header will be generated by the application and sent as entity body.
            This flag should be passed on the first call to HttpSendResponseEntityBody, and
            not after. Thus, flag is not applicable to HttpSendHttpResponse.
        */
        internal unsafe uint SendHeaders(Interop.HttpApi.HTTP_DATA_CHUNK* pDataChunk,
            HttpResponseStreamAsyncResult asyncResult,
            Interop.HttpApi.HTTP_FLAGS flags,
            bool isWebSocketHandshake)
        {
            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SendHeaders() pDataChunk:" + LoggingHash.ObjectToString((IntPtr)pDataChunk) + " asyncResult:" + LoggingHash.ObjectToString(asyncResult));
            Debug.Assert(!SentHeaders, "HttpListenerResponse#{0}::SendHeaders()|SentHeaders is true.", LoggingHash.HashString(this));

            if (StatusCode == (int)HttpStatusCode.Unauthorized)
            { // User set 401
                // Using the configured Auth schemes, populate the auth challenge headers. This is for scenarios where 
                // Anonymous access is allowed for some resources, but the server later determines that authorization 
                // is required for this request.
                HttpListenerContext.SetAuthenticationHeaders();
            }

            // Log headers
            if (NetEventSource.Log.IsEnabled())
            {
                StringBuilder sb = new StringBuilder("HttpListenerResponse Headers:\n");
                for (int i = 0; i < Headers.Count; i++)
                {
                    sb.Append("\t");
                    sb.Append(Headers.GetKey(i));
                    sb.Append(" : ");
                    sb.Append(Headers.Get(i));
                    sb.Append("\n");
                }
                NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, ".ctor", sb.ToString());
            }
            m_ResponseState = ResponseState.SentHeaders;
            /*
            if (m_BoundaryType==BoundaryType.Raw) {
                use HTTP_SEND_RESPONSE_FLAG_RAW_HEADER;
            }
            */
            uint statusCode;
            uint bytesSent;
            List<GCHandle> pinnedHeaders = SerializeHeaders(ref m_NativeResponse.Headers, isWebSocketHandshake);
            try
            {
                if (pDataChunk != null)
                {
                    m_NativeResponse.EntityChunkCount = 1;
                    m_NativeResponse.pEntityChunks = pDataChunk;
                }
                else if (asyncResult != null && asyncResult.pDataChunks != null)
                {
                    m_NativeResponse.EntityChunkCount = asyncResult.dataChunkCount;
                    m_NativeResponse.pEntityChunks = asyncResult.pDataChunks;
                }
                else
                {
                    m_NativeResponse.EntityChunkCount = 0;
                    m_NativeResponse.pEntityChunks = null;
                }
                GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SendHeaders() calling Interop.HttpApi.HttpSendHttpResponse flags:" + flags);
                if (StatusDescription.Length > 0)
                {
                    byte[] statusDescriptionBytes = new byte[WebHeaderEncoding.GetByteCount(StatusDescription)];
                    fixed (byte* pStatusDescription = statusDescriptionBytes)
                    {
                        m_NativeResponse.ReasonLength = (ushort)statusDescriptionBytes.Length;
                        WebHeaderEncoding.GetBytes(StatusDescription, 0, statusDescriptionBytes.Length, statusDescriptionBytes, 0);
                        m_NativeResponse.pReason = (sbyte*)pStatusDescription;
                        fixed (Interop.HttpApi.HTTP_RESPONSE* pResponse = &m_NativeResponse)
                        {
                            if (asyncResult != null)
                            {
                                HttpListenerContext.EnsureBoundHandle();
                            }
                            statusCode =
                                Interop.HttpApi.HttpSendHttpResponse(
                                    HttpListenerContext.RequestQueueHandle,
                                    HttpListenerRequest.RequestId,
                                    (uint)flags,
                                    pResponse,
                                    null,
                                    &bytesSent,
                                    SafeLocalAllocHandle.Zero,
                                    0,
                                    asyncResult == null ? null : asyncResult.m_pOverlapped,
                                    null);

                            if (asyncResult != null &&
                                statusCode == Interop.HttpApi.ERROR_SUCCESS &&
                                HttpListener.SkipIOCPCallbackOnSuccess)
                            {
                                asyncResult.IOCompleted(statusCode, bytesSent);
                                // IO operation completed synchronously - callback won't be called to signal completion.
                            }
                        }
                    }
                }
                else
                {
                    fixed (Interop.HttpApi.HTTP_RESPONSE* pResponse = &m_NativeResponse)
                    {
                        if (asyncResult != null)
                        {
                            HttpListenerContext.EnsureBoundHandle();
                        }
                        statusCode =
                            Interop.HttpApi.HttpSendHttpResponse(
                                HttpListenerContext.RequestQueueHandle,
                                HttpListenerRequest.RequestId,
                                (uint)flags,
                                pResponse,
                                null,
                                &bytesSent,
                                SafeLocalAllocHandle.Zero,
                                0,
                                asyncResult == null ? null : asyncResult.m_pOverlapped,
                                null);

                        if (asyncResult != null &&
                            statusCode == Interop.HttpApi.ERROR_SUCCESS &&
                            HttpListener.SkipIOCPCallbackOnSuccess)
                        {
                            asyncResult.IOCompleted(statusCode, bytesSent);
                            // IO operation completed synchronously - callback won't be called to signal completion.
                        }
                    }
                }
                GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SendHeaders() call to Interop.HttpApi.HttpSendHttpResponse returned:" + statusCode);
            }
            finally
            {
                FreePinnedHeaders(pinnedHeaders);
            }
            return statusCode;
        }

        internal void ComputeCookies()
        {
            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::ComputeCookies() entering Set-Cookie: " + LoggingHash.ObjectToString(Headers[HttpResponseHeader.SetCookie]) + " Set-Cookie2: " + LoggingHash.ObjectToString(Headers[HttpKnownHeaderNames.SetCookie2]));
            if (m_Cookies != null)
            {
                // now go through the collection, and concatenate all the cookies in per-variant strings
                string setCookie2 = null;
                string setCookie = null;
                for (int index = 0; index < m_Cookies.Count; index++)
                {
                    Cookie cookie = m_Cookies[index];
                    string cookieString = cookie.ToServerString();
                    if (cookieString == null || cookieString.Length == 0)
                    {
                        continue;
                    }
                    GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::ComputeCookies() now looking at index:" + index + " cookie.Variant:" + cookie.Variant + " cookie:" + cookie.ToString());
                    if (cookie.Variant == CookieVariant.Rfc2965)
                    {
                        setCookie2 = setCookie2 == null ? cookieString : setCookie2 + ", " + cookieString;
                    }
                    else
                    {
                        setCookie = setCookie == null ? cookieString : setCookie + ", " + cookieString;
                    }
                }
                if (!string.IsNullOrEmpty(setCookie))
                {
                    Headers[HttpResponseHeader.SetCookie] = setCookie;
                    if (string.IsNullOrEmpty(setCookie2))
                    {
                        Headers.Remove(HttpKnownHeaderNames.SetCookie2);
                    }
                }
                if (!string.IsNullOrEmpty(setCookie2))
                {
                    Headers[HttpKnownHeaderNames.SetCookie2] = setCookie2;
                    if (string.IsNullOrEmpty(setCookie))
                    {
                        Headers.Remove(HttpKnownHeaderNames.SetCookie);
                    }
                }
            }
            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::ComputeCookies() exiting Set-Cookie: " + LoggingHash.ObjectToString(Headers[HttpResponseHeader.SetCookie]) + " Set-Cookie2: " + LoggingHash.ObjectToString(Headers[HttpKnownHeaderNames.SetCookie2]));
        }

        internal Interop.HttpApi.HTTP_FLAGS ComputeHeaders()
        {
            Interop.HttpApi.HTTP_FLAGS flags = Interop.HttpApi.HTTP_FLAGS.NONE;
            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::ComputeHeaders()");
            Debug.Assert(!ComputedHeaders, "HttpListenerResponse#{0}::ComputeHeaders()|ComputedHeaders is true.", LoggingHash.HashString(this));
            m_ResponseState = ResponseState.ComputedHeaders;
            /*
            // here we would check for BoundaryType.Raw, in this case we wouldn't need to do anything
            if (m_BoundaryType==BoundaryType.Raw) {
                return flags;
            }
            */

            ComputeCoreHeaders();

            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::ComputeHeaders() flags:" + flags + " m_BoundaryType:" + m_BoundaryType + " m_ContentLength:" + m_ContentLength + " m_KeepAlive:" + m_KeepAlive);
            if (m_BoundaryType == BoundaryType.None)
            {
                if (HttpListenerRequest.ProtocolVersion.Minor == 0)
                {
                    // CONSIDER: here we could also buffer.
                    m_KeepAlive = false;
                }
                else
                {
                    m_BoundaryType = BoundaryType.Chunked;
                }
                if (CanSendResponseBody(m_HttpContext.Response.StatusCode))
                {
                    m_ContentLength = -1;
                }
                else
                {
                    ContentLength64 = 0;
                }
            }

            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::ComputeHeaders() flags:" + flags + " m_BoundaryType:" + m_BoundaryType + " m_ContentLength:" + m_ContentLength + " m_KeepAlive:" + m_KeepAlive);
            if (m_BoundaryType == BoundaryType.ContentLength)
            {
                Headers[HttpResponseHeader.ContentLength] = m_ContentLength.ToString("D", NumberFormatInfo.InvariantInfo);
                if (m_ContentLength == 0)
                {
                    flags = Interop.HttpApi.HTTP_FLAGS.NONE;
                }
            }
            else if (m_BoundaryType == BoundaryType.Chunked)
            {
                Headers[HttpResponseHeader.TransferEncoding] = "chunked";
            }
            else if (m_BoundaryType == BoundaryType.None)
            {
                flags = Interop.HttpApi.HTTP_FLAGS.NONE; // seems like HTTP_SEND_RESPONSE_FLAG_MORE_DATA but this hangs the app;
            }
            else
            {
                m_KeepAlive = false;
            }
            if (!m_KeepAlive)
            {
                Headers.Add(HttpResponseHeader.Connection, "close");
                if (flags == Interop.HttpApi.HTTP_FLAGS.NONE)
                {
                    flags = Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                }
            }
            else
            {
                if (HttpListenerRequest.ProtocolVersion.Minor == 0)
                {
                    Headers[HttpResponseHeader.KeepAlive] = "true";
                }
            }
            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::ComputeHeaders() flags:" + flags + " m_BoundaryType:" + m_BoundaryType + " m_ContentLength:" + m_ContentLength + " m_KeepAlive:" + m_KeepAlive);
            return flags;
        }

        // This method handles the shared response header processing between normal HTTP responses and WebSocket responses.
        internal void ComputeCoreHeaders()
        {
            if (HttpListenerContext.MutualAuthentication != null && HttpListenerContext.MutualAuthentication.Length > 0)
            {
                Headers[HttpResponseHeader.WwwAuthenticate] = HttpListenerContext.MutualAuthentication;
            }
            ComputeCookies();
        }

        private List<GCHandle> SerializeHeaders(ref Interop.HttpApi.HTTP_RESPONSE_HEADERS headers,
            bool isWebSocketHandshake)
        {
            Interop.HttpApi.HTTP_UNKNOWN_HEADER[] unknownHeaders = null;
            List<GCHandle> pinnedHeaders;
            GCHandle gcHandle;
            /*
            // here we would check for BoundaryType.Raw, in this case we wouldn't need to do anything
            if (m_BoundaryType==BoundaryType.Raw) {
                return null;
            }
            */
            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SerializeHeaders(HTTP_RESPONSE_HEADERS)");
            if (Headers.Count == 0)
            {
                return null;
            }
            string headerName;
            string headerValue;
            int lookup;
            byte[] bytes = null;
            pinnedHeaders = new List<GCHandle>();

            //---------------------------------------------------
            // DTS Issue: 609383:
            // The Set-Cookie headers are being merged into one. 
            // There are two issues here. 
            // 1. When Set-Cookie headers are set through SetCookie method on the ListenerResponse,
            // there is code in the SetCookie method and the methods it calls to flatten the Set-Cookie
            // values. This blindly concatenates the cookies with a comma delimiter. There could be 
            // a cookie value that contains comma, but we don't escape it with %XX value
            //  
            // As an alternative users can add the Set-Cookie header through the AddHeader method
            // like ListenerResponse.Headers.Add("name", "value")
            // That way they can add multiple headers - AND They can format the value like they want it.
            //
            // 2. Now that the header collection contains multiple Set-Cookie name, value pairs
            // you would think the problem would go away. However here is an interesting thing.
            // For NameValueCollection, when you add 
            // "Set-Cookie", "value1"
            // "Set-Cookie", "value2"
            //  The NameValueCollection.Count == 1. Because there is only one key
            //  NameValueCollection.Get("Set-Cookie") would conviniently take these two valuess
            //  concatenate them with a comma like 
            //  value1,value2. 
            //  In order to get individual values, you need to use 
            //  string[] values = NameValueCollection.GetValues("Set-Cookie");
            //
            //  -------------------------------------------------------------
            //  So here is the proposed fix here.
            //  We must first to loop through all the NameValueCollection keys
            //  and if the name is a unknown header, we must compute the number of 
            //  values it has. Then, we should allocate that many unknown header array 
            //  elements.
            //  
            //  Note that a part of the fix here is to treat Set-Cookie as an unknown header
            //
            //
            //-----------------------------------------------------------
            int numUnknownHeaders = 0;
            for (int index = 0; index < Headers.Count; index++)
            {
                headerName = Headers.GetKey(index) as string;

                //See if this is an unknown header
                lookup = Interop.HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerName);

                //Treat Set-Cookie as well as Connection header in Websocket mode as unknown
                if (lookup == (int)HttpResponseHeader.SetCookie ||
                    isWebSocketHandshake && lookup == (int)HttpResponseHeader.Connection)
                {
                    lookup = -1;
                }

                if (lookup == -1)
                {
                    string[] headerValues = Headers.GetValues(index);
                    numUnknownHeaders += headerValues.Length;
                }
            }

            try
            {
                fixed (Interop.HttpApi.HTTP_KNOWN_HEADER* pKnownHeaders = &headers.KnownHeaders)
                {
                    for (int index = 0; index < Headers.Count; index++)
                    {
                        headerName = Headers.GetKey(index) as string;
                        headerValue = Headers.Get(index) as string;
                        lookup = Interop.HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerName);
                        if (lookup == (int)HttpResponseHeader.SetCookie ||
                            isWebSocketHandshake && lookup == (int)HttpResponseHeader.Connection)
                        {
                            lookup = -1;
                        }
                        GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SerializeHeaders(" + index + "/" + Headers.Count + ") headerName:" + LoggingHash.ObjectToString(headerName) + " lookup:" + lookup + " headerValue:" + LoggingHash.ObjectToString(headerValue));
                        if (lookup == -1)
                        {

                            if (unknownHeaders == null)
                            {
                                //----------------------------------------
                                //*** This following comment is no longer true ***
                                // we waste some memory here (up to 32*41=1312 bytes) but we gain speed
                                //unknownHeaders = new Interop.HttpApi.HTTP_UNKNOWN_HEADER[Headers.Count-index];
                                //--------------------------------------------
                                unknownHeaders = new Interop.HttpApi.HTTP_UNKNOWN_HEADER[numUnknownHeaders];
                                gcHandle = GCHandle.Alloc(unknownHeaders, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                headers.pUnknownHeaders = (Interop.HttpApi.HTTP_UNKNOWN_HEADER*)gcHandle.AddrOfPinnedObject();
                            }

                            //----------------------------------------
                            //FOR UNKNOWN HEADERS
                            //ALLOW MULTIPLE HEADERS to be added 
                            //---------------------------------------
                            string[] headerValues = Headers.GetValues(index);
                            for (int headerValueIndex = 0; headerValueIndex < headerValues.Length; headerValueIndex++)
                            {
                                //Add Name
                                bytes = new byte[WebHeaderEncoding.GetByteCount(headerName)];
                                unknownHeaders[headers.UnknownHeaderCount].NameLength = (ushort)bytes.Length;
                                WebHeaderEncoding.GetBytes(headerName, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[headers.UnknownHeaderCount].pName = (sbyte*)gcHandle.AddrOfPinnedObject();

                                //Add Value
                                headerValue = headerValues[headerValueIndex];
                                bytes = new byte[WebHeaderEncoding.GetByteCount(headerValue)];
                                unknownHeaders[headers.UnknownHeaderCount].RawValueLength = (ushort)bytes.Length;
                                WebHeaderEncoding.GetBytes(headerValue, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[headers.UnknownHeaderCount].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                                headers.UnknownHeaderCount++;
                                GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SerializeHeaders(Unknown) UnknownHeaderCount:" + headers.UnknownHeaderCount);
                            }

                        }
                        else
                        {
                            GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SerializeHeaders(Known) HttpResponseHeader[" + lookup + "]:" + ((HttpResponseHeader)lookup) + " headerValue:" + LoggingHash.ObjectToString(headerValue));
                            if (headerValue != null)
                            {
                                bytes = new byte[WebHeaderEncoding.GetByteCount(headerValue)];
                                pKnownHeaders[lookup].RawValueLength = (ushort)bytes.Length;
                                WebHeaderEncoding.GetBytes(headerValue, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                pKnownHeaders[lookup].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                                GlobalLog.Print("HttpListenerResponse#" + LoggingHash.HashString(this) + "::SerializeHeaders(Known) pRawValue:" + LoggingHash.ObjectToString((IntPtr)(pKnownHeaders[lookup].pRawValue)) + " RawValueLength:" + pKnownHeaders[lookup].RawValueLength + " lookup:" + lookup);
                                GlobalLog.Dump((IntPtr)pKnownHeaders[lookup].pRawValue, 0, pKnownHeaders[lookup].RawValueLength);
                            }
                        }
                    }
                }
            }
            catch
            {
                FreePinnedHeaders(pinnedHeaders);
                throw;
            }
            return pinnedHeaders;
        }

        private void FreePinnedHeaders(List<GCHandle> pinnedHeaders)
        {
            if (pinnedHeaders != null)
            {
                foreach (GCHandle gcHandle in pinnedHeaders)
                {
                    if (gcHandle.IsAllocated)
                    {
                        gcHandle.Free();
                    }
                }
            }
        }

        private void CheckDisposed()
        {
            if (m_ResponseState >= ResponseState.Closed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        internal void CancelLastWrite(SafeHandle requestQueueHandle)
        {
            if (m_ResponseStream != null)
            {
                m_ResponseStream.CancelLastWrite(requestQueueHandle);
            }
        }
    }
}
