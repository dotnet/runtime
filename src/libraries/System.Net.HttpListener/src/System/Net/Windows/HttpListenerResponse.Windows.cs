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
    public sealed unsafe partial class HttpListenerResponse
    {
        private enum ResponseState
        {
            Created,
            ComputedHeaders,
            SentHeaders,
            Closed,
        }

        private ResponseState _responseState;

        private long _contentLength;
        private Interop.HttpApi.HTTP_RESPONSE _nativeResponse;


        internal HttpListenerResponse()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);
            _nativeResponse = new Interop.HttpApi.HTTP_RESPONSE();
            _nativeResponse.StatusCode = (ushort)HttpStatusCode.OK;
            _nativeResponse.Version.MajorVersion = 1;
            _nativeResponse.Version.MinorVersion = 1;
            _responseState = ResponseState.Created;
        }

        internal HttpListenerResponse(HttpListenerContext httpContext) : this()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Associate(this, httpContext);
            _httpContext = httpContext;
        }



        public int StatusCode
        {
            get => _nativeResponse.StatusCode;
            set
            {
                CheckDisposed();
                if (value < 100 || value > 999)
                {
                    throw new ProtocolViolationException(SR.net_invalidstatus);
                }
                _nativeResponse.StatusCode = (ushort)value;
            }
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"templateResponse {templateResponse}");
            _nativeResponse = new Interop.HttpApi.HTTP_RESPONSE();
            _responseState = ResponseState.Created;
            _webHeaders = templateResponse._webHeaders;
            _boundaryType = templateResponse._boundaryType;
            _contentLength = templateResponse._contentLength;
            _nativeResponse.StatusCode = templateResponse._nativeResponse.StatusCode;
            _nativeResponse.Version.MajorVersion = templateResponse._nativeResponse.Version.MajorVersion;
            _nativeResponse.Version.MinorVersion = templateResponse._nativeResponse.Version.MinorVersion;
            _statusDescription = templateResponse._statusDescription;
            _keepAlive = templateResponse._keepAlive;
        }


        public Version ProtocolVersion
        {
            get => new Version(_nativeResponse.Version.MajorVersion, _nativeResponse.Version.MinorVersion);
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                {
                    throw new ArgumentException(SR.net_wrongversion, nameof(value));
                }
                _nativeResponse.Version.MajorVersion = (ushort)value.Major;
                _nativeResponse.Version.MinorVersion = (ushort)value.Minor;
            }
        }

        public void Abort()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                if (Disposed)
                {
                    return;
                }

                _responseState = ResponseState.Closed;
                HttpListenerContext.Abort();
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        public void Close()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this);
                ((IDisposable)this).Dispose();
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, $"responseEntity={responseEntity},willBlock={willBlock}");
            try
            {
                CheckDisposed();
                if (responseEntity == null)
                {
                    throw new ArgumentNullException(nameof(responseEntity));
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"ResponseState:{_responseState}, BoundaryType:{_boundaryType}, ContentLength:{_contentLength}");
                if (!SentHeaders && _boundaryType != BoundaryType.Chunked)
                {
                    ContentLength64 = responseEntity.Length;
                }
                EnsureResponseStream();
                if (willBlock)
                {
                    try
                    {
                        _responseStream.Write(responseEntity, 0, responseEntity.Length);
                    }
                    catch (Win32Exception)
                    {
                    }
                    finally
                    {
                        _responseStream.Close();
                        _responseState = ResponseState.Closed;
                        HttpListenerContext.Close();
                    }
                }
                else
                {
                    _responseStream.BeginWrite(responseEntity, 0, responseEntity.Length, new AsyncCallback(NonBlockingCloseCallback), null);
                }
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        private void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            EnsureResponseStream();
            _responseStream.Close();
            _responseState = ResponseState.Closed;

            HttpListenerContext.Close();
        }

        internal BoundaryType BoundaryType => _boundaryType;

        private void EnsureResponseStream()
        {
            if (_responseStream == null)
            {
                _responseStream = new HttpResponseStream(HttpListenerContext);
            }
        }

        private void NonBlockingCloseCallback(IAsyncResult asyncResult)
        {
            try
            {
                _responseStream.EndWrite(asyncResult);
            }
            catch (Win32Exception)
            {
            }
            finally
            {
                _responseStream.Close();
                HttpListenerContext.Close();
                _responseState = ResponseState.Closed;
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
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"pDataChunk: { ((IntPtr)pDataChunk)}, asyncResult: {asyncResult}");
            Debug.Assert(!SentHeaders, "SentHeaders is true.");

            if (StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                // User set 401
                // Using the configured Auth schemes, populate the auth challenge headers. This is for scenarios where 
                // Anonymous access is allowed for some resources, but the server later determines that authorization 
                // is required for this request.
                HttpListenerContext.SetAuthenticationHeaders();
            }

            // Log headers
            if (NetEventSource.IsEnabled)
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
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, sb.ToString());
            }
            _responseState = ResponseState.SentHeaders;

            uint statusCode;
            uint bytesSent;
            List<GCHandle> pinnedHeaders = SerializeHeaders(ref _nativeResponse.Headers, isWebSocketHandshake);
            try
            {
                if (pDataChunk != null)
                {
                    _nativeResponse.EntityChunkCount = 1;
                    _nativeResponse.pEntityChunks = pDataChunk;
                }
                else if (asyncResult != null && asyncResult.pDataChunks != null)
                {
                    _nativeResponse.EntityChunkCount = asyncResult.dataChunkCount;
                    _nativeResponse.pEntityChunks = asyncResult.pDataChunks;
                }
                else
                {
                    _nativeResponse.EntityChunkCount = 0;
                    _nativeResponse.pEntityChunks = null;
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Calling Interop.HttpApi.HttpSendHttpResponse flags:" + flags);
                if (StatusDescription.Length > 0)
                {
                    byte[] statusDescriptionBytes = new byte[WebHeaderEncoding.GetByteCount(StatusDescription)];
                    fixed (byte* pStatusDescription = &statusDescriptionBytes[0])
                    {
                        _nativeResponse.ReasonLength = (ushort)statusDescriptionBytes.Length;
                        WebHeaderEncoding.GetBytes(StatusDescription, 0, statusDescriptionBytes.Length, statusDescriptionBytes, 0);
                        _nativeResponse.pReason = (sbyte*)pStatusDescription;
                        fixed (Interop.HttpApi.HTTP_RESPONSE* pResponse = &_nativeResponse)
                        {
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
                                    asyncResult == null ? null : asyncResult._pOverlapped,
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
                    fixed (Interop.HttpApi.HTTP_RESPONSE* pResponse = &_nativeResponse)
                    {
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
                                asyncResult == null ? null : asyncResult._pOverlapped,
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
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Call to Interop.HttpApi.HttpSendHttpResponse returned:" + statusCode);
            }
            finally
            {
                FreePinnedHeaders(pinnedHeaders);
            }
            return statusCode;
        }

        internal Interop.HttpApi.HTTP_FLAGS ComputeHeaders()
        {
            Interop.HttpApi.HTTP_FLAGS flags = Interop.HttpApi.HTTP_FLAGS.NONE;
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);
            Debug.Assert(!ComputedHeaders, "ComputedHeaders is true.");
            _responseState = ResponseState.ComputedHeaders;

            ComputeCoreHeaders();

            if (NetEventSource.IsEnabled)
                NetEventSource.Info(this,
$"flags: {flags} _boundaryType: {_boundaryType} _contentLength: {_contentLength} _keepAlive: {_keepAlive}");
            if (_boundaryType == BoundaryType.None)
            {
                if (HttpListenerRequest.ProtocolVersion.Minor == 0)
                {
                    _keepAlive = false;
                }
                else
                {
                    _boundaryType = BoundaryType.Chunked;
                }
                if (CanSendResponseBody(_httpContext.Response.StatusCode))
                {
                    _contentLength = -1;
                }
                else
                {
                    ContentLength64 = 0;
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"flags:{flags} _BoundaryType:{_boundaryType} _contentLength:{_contentLength} _keepAlive: {_keepAlive}");
            if (_boundaryType == BoundaryType.ContentLength)
            {
                Headers[HttpResponseHeader.ContentLength] = _contentLength.ToString("D", NumberFormatInfo.InvariantInfo);
                if (_contentLength == 0)
                {
                    flags = Interop.HttpApi.HTTP_FLAGS.NONE;
                }
            }
            else if (_boundaryType == BoundaryType.Chunked)
            {
                Headers[HttpResponseHeader.TransferEncoding] = "chunked";
            }
            else if (_boundaryType == BoundaryType.None)
            {
                flags = Interop.HttpApi.HTTP_FLAGS.NONE; // seems like HTTP_SEND_RESPONSE_FLAG_MORE_DATA but this hangs the app;
            }
            else
            {
                _keepAlive = false;
            }
            if (!_keepAlive)
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
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"flags:{flags} _BoundaryType:{_boundaryType} _contentLength:{_contentLength} _keepAlive: {_keepAlive}");
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

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "SerializeHeaders(HTTP_RESPONSE_HEADERS)");
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
                        if (NetEventSource.IsEnabled)
                            NetEventSource.Info(this,
  $"index={index},headers.count={Headers.Count},headerName:{headerName},lookup:{lookup} headerValue:{headerValue}");
                        if (lookup == -1)
                        {
                            if (unknownHeaders == null)
                            {
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
                                if (NetEventSource.IsEnabled) NetEventSource.Info(this, "UnknownHeaderCount:" + headers.UnknownHeaderCount);
                            }
                        }
                        else
                        {
                            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"HttpResponseHeader[{lookup}]:{((HttpResponseHeader)lookup)} headerValue:{headerValue}");
                            if (headerValue != null)
                            {
                                bytes = new byte[WebHeaderEncoding.GetByteCount(headerValue)];
                                pKnownHeaders[lookup].RawValueLength = (ushort)bytes.Length;
                                WebHeaderEncoding.GetBytes(headerValue, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                pKnownHeaders[lookup].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                                if (NetEventSource.IsEnabled)
                                {
                                    NetEventSource.Info(this, $"pRawValue:{((IntPtr)(pKnownHeaders[lookup].pRawValue))} RawValueLength:{pKnownHeaders[lookup].RawValueLength} lookup: {lookup}");
                                }
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

        internal void CancelLastWrite(SafeHandle requestQueueHandle)
        {
            _responseStream?.CancelLastWrite(requestQueueHandle);
        }

        internal bool ComputedHeaders => _responseState >= ResponseState.ComputedHeaders;
        internal bool SentHeaders => _responseState >= ResponseState.SentHeaders;
        private bool Disposed => _responseState >= ResponseState.Closed;
    }
}
