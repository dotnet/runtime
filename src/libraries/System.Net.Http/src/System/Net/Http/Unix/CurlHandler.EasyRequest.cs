// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using CURLAUTH = Interop.Http.CURLAUTH;
using CURLcode = Interop.Http.CURLcode;
using CURLoption = Interop.Http.CURLoption;
using CurlProtocols = Interop.Http.CurlProtocols;
using CURLProxyType = Interop.Http.curl_proxytype;
using SafeCurlHandle = Interop.Http.SafeCurlHandle;
using SafeCurlSListHandle = Interop.Http.SafeCurlSListHandle;
using SafeCallbackHandle = Interop.Http.SafeCallbackHandle;
using SeekCallback = Interop.Http.SeekCallback;
using ReadWriteCallback = Interop.Http.ReadWriteCallback;
using ReadWriteFunction = Interop.Http.ReadWriteFunction;
using SslCtxCallback = Interop.Http.SslCtxCallback;
using DebugCallback = Interop.Http.DebugCallback;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        private sealed class EasyRequest : TaskCompletionSource<HttpResponseMessage>
        {
            internal readonly CurlHandler _handler;
            internal readonly HttpRequestMessage _requestMessage;
            internal readonly CurlResponseMessage _responseMessage;
            internal readonly CancellationToken _cancellationToken;
            internal readonly HttpContentAsyncStream _requestContentStream;

            internal SafeCurlHandle _easyHandle;
            private SafeCurlSListHandle _requestHeaders;

            internal MultiAgent _associatedMultiAgent;
            internal SendTransferState _sendTransferState;
            internal bool _isRedirect = false;
            internal Uri _targetUri;

            private SafeCallbackHandle _callbackHandle;

            public EasyRequest(CurlHandler handler, HttpRequestMessage requestMessage, CancellationToken cancellationToken) :
                base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _handler = handler;
                _requestMessage = requestMessage;
                _cancellationToken = cancellationToken;

                if (requestMessage.Content != null)
                {
                    _requestContentStream = new HttpContentAsyncStream(requestMessage.Content);
                }

                _responseMessage = new CurlResponseMessage(this);
                _targetUri = requestMessage.RequestUri;
            }

            /// <summary>
            /// Initialize the underlying libcurl support for this EasyRequest.
            /// This is separated out of the constructor so that we can take into account
            /// any additional configuration needed based on the request message
            /// after the EasyRequest is configured and so that error handling
            /// can be better handled in the caller.
            /// </summary>
            internal void InitializeCurl()
            {
                // Create the underlying easy handle
                SafeCurlHandle easyHandle = Interop.Http.EasyCreate();
                if (easyHandle.IsInvalid)
                {
                    throw new OutOfMemoryException();
                }
                _easyHandle = easyHandle;

                // Configure the handle
                SetUrl();
                SetMultithreading();
                SetRedirection();
                SetVerb();
                SetVersion();
                SetDecompressionOptions();
                SetProxyOptions(_requestMessage.RequestUri);
                SetCredentialsOptions(_handler.GetCredentials(_requestMessage.RequestUri));
                SetCookieOption(_requestMessage.RequestUri);
                SetRequestHeaders();
                SetSslOptions();
            }

            public void EnsureResponseMessagePublished()
            {
                // If this is the response hasn't been published yet, do any finaly processing of the response 
                // message before it's published.
                if (!Task.IsCompleted)
                {
                    // On Windows, if the response was automatically decompressed, Content-Encoding and Content-Length
                    // headers are removed from the response. Do the same thing here.
                    DecompressionMethods dm = _handler.AutomaticDecompression;
                    if (dm != DecompressionMethods.None)
                    {
                        HttpContentHeaders contentHeaders = _responseMessage.Content.Headers;
                        IEnumerable<string> encodings;
                        if (contentHeaders.TryGetValues(HttpKnownHeaderNames.ContentEncoding, out encodings))
                        {
                            foreach (string encoding in encodings)
                            {
                                if (((dm & DecompressionMethods.GZip) != 0 && string.Equals(encoding, EncodingNameGzip, StringComparison.OrdinalIgnoreCase)) ||
                                    ((dm & DecompressionMethods.Deflate) != 0 && string.Equals(encoding, EncodingNameDeflate, StringComparison.OrdinalIgnoreCase)))
                                {
                                    contentHeaders.Remove(HttpKnownHeaderNames.ContentEncoding);
                                    contentHeaders.Remove(HttpKnownHeaderNames.ContentLength);
                                    break;
                                }
                            }
                        }
                    }
                }

                // Now ensure it's published.
                bool result = TrySetResult(_responseMessage);
                Debug.Assert(result || Task.Status == TaskStatus.RanToCompletion,
                    "If the task was already completed, it should have been completed succesfully; " +
                    "we shouldn't be completing as successful after already completing as failed.");
            }

            public void FailRequest(Exception error)
            {
                Debug.Assert(error != null, "Expected non-null exception");

                var oce = error as OperationCanceledException;
                if (oce != null)
                {
                    TrySetCanceled(oce.CancellationToken);
                }
                else
                {
                    if (error is IOException || error is CurlException || error == null)
                    {
                        error = CreateHttpRequestException(error);
                    }
                    TrySetException(error);
                }
                // There's not much we can reasonably assert here about the result of TrySet*.
                // It's possible that the task wasn't yet completed (e.g. a failure while initiating the request),
                // it's possible that the task was already completed as success (e.g. a failure sending back the response),
                // and it's possible that the task was already completed as failure (e.g. we handled the exception and
                // faulted the task, but then tried to fault it again while finishing processing in the main loop).

                // Make sure the exception is available on the response stream so that it's propagated
                // from any attempts to read from the stream.
                _responseMessage.ResponseStream.SignalComplete(error);
            }

            public void Cleanup() // not called Dispose because the request may still be in use after it's cleaned up
            {
                _responseMessage.ResponseStream.SignalComplete(); // No more callbacks so no more data
                // Don't dispose of the ResponseMessage.ResponseStream as it may still be in use
                // by code reading data stored in the stream.

                // Dispose of the input content stream if there was one.  Nothing should be using it any more.
                if (_requestContentStream != null)
                    _requestContentStream.Dispose();

                // Dispose of the underlying easy handle.  We're no longer processing it.
                if (_easyHandle != null)
                    _easyHandle.Dispose();

                // Dispose of the request headers if we had any.  We had to keep this handle
                // alive as long as the easy handle was using it.  We didn't need to do any
                // ref counting on the safe handle, though, as the only processing happens
                // in Process, which ensures the handle will be rooted while libcurl is
                // doing any processing that assumes it's valid.
                if (_requestHeaders != null)
                    _requestHeaders.Dispose();

                if (_callbackHandle != null)
                    _callbackHandle.Dispose();
            }

            private void SetUrl()
            {
                EventSourceTrace("Url: {0}", _requestMessage.RequestUri);
                SetCurlOption(CURLoption.CURLOPT_URL, _requestMessage.RequestUri.AbsoluteUri);
                SetCurlOption(CURLoption.CURLOPT_PROTOCOLS, (long)(CurlProtocols.CURLPROTO_HTTP | CurlProtocols.CURLPROTO_HTTPS));
            }

            private void SetMultithreading()
            {
                SetCurlOption(CURLoption.CURLOPT_NOSIGNAL, 1L);
            }

            private void SetRedirection()
            {
                if (!_handler._automaticRedirection)
                {
                    return;
                }

                SetCurlOption(CURLoption.CURLOPT_FOLLOWLOCATION, 1L);

                CurlProtocols redirectProtocols = string.Equals(_requestMessage.RequestUri.Scheme, UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ?
                    CurlProtocols.CURLPROTO_HTTPS : // redirect only to another https
                    CurlProtocols.CURLPROTO_HTTP | CurlProtocols.CURLPROTO_HTTPS; // redirect to http or to https
                SetCurlOption(CURLoption.CURLOPT_REDIR_PROTOCOLS, (long)redirectProtocols);

                SetCurlOption(CURLoption.CURLOPT_MAXREDIRS, _handler._maxAutomaticRedirections);
                EventSourceTrace("Max automatic redirections: {0}", _handler._maxAutomaticRedirections);
            }

            private void SetVerb()
            {
                EventSourceTrace<string>("Verb: {0}", _requestMessage.Method.Method);

                if (_requestMessage.Method == HttpMethod.Put)
                {
                    SetCurlOption(CURLoption.CURLOPT_UPLOAD, 1L);
                    if (_requestMessage.Content == null)
                    {
                        SetCurlOption(CURLoption.CURLOPT_INFILESIZE, 0L);
                    }
                }
                else if (_requestMessage.Method == HttpMethod.Head)
                {
                    SetCurlOption(CURLoption.CURLOPT_NOBODY, 1L);
                }
                else if (_requestMessage.Method == HttpMethod.Post)
                {
                    SetCurlOption(CURLoption.CURLOPT_POST, 1L);
                    if (_requestMessage.Content == null)
                    {
                        SetCurlOption(CURLoption.CURLOPT_POSTFIELDSIZE, 0L);
                        SetCurlOption(CURLoption.CURLOPT_COPYPOSTFIELDS, string.Empty);
                    }
                }
                else if (_requestMessage.Method == HttpMethod.Trace)
                {
                    SetCurlOption(CURLoption.CURLOPT_CUSTOMREQUEST, _requestMessage.Method.Method);
                    SetCurlOption(CURLoption.CURLOPT_NOBODY, 1L);
                }
                else
                {
                    SetCurlOption(CURLoption.CURLOPT_CUSTOMREQUEST, _requestMessage.Method.Method);
                    if (_requestMessage.Content != null)
                    {
                        SetCurlOption(CURLoption.CURLOPT_UPLOAD, 1L);
                    }
                }
            }

            private void SetVersion()
            {
                Version v = _requestMessage.Version;
                if (v != null)
                {
                    // Try to use the requested version, if a known version was explicitly requested.
                    // If an unknown version was requested, we simply use libcurl's default.
                    var curlVersion =
                        (v.Major == 1 && v.Minor == 1) ? Interop.Http.CurlHttpVersion.CURL_HTTP_VERSION_1_1 :
                        (v.Major == 1 && v.Minor == 0) ? Interop.Http.CurlHttpVersion.CURL_HTTP_VERSION_1_0 :
                        (v.Major == 2 && v.Minor == 0) ? Interop.Http.CurlHttpVersion.CURL_HTTP_VERSION_2_0 :
                        Interop.Http.CurlHttpVersion.CURL_HTTP_VERSION_NONE;

                    if (curlVersion != Interop.Http.CurlHttpVersion.CURL_HTTP_VERSION_NONE)
                    {
                        // Ask libcurl to use the specified version if possible.
                        CURLcode c = Interop.Http.EasySetOptionLong(_easyHandle, CURLoption.CURLOPT_HTTP_VERSION, (long)curlVersion);
                        if (c == CURLcode.CURLE_OK)
                        {
                            // Success.  The requested version will be used.
                            EventSourceTrace("HTTP version: {0}", v);
                        }
                        else if (c == CURLcode.CURLE_UNSUPPORTED_PROTOCOL)
                        {
                            // The requested version is unsupported.  Fall back to using the default version chosen by libcurl.
                            EventSourceTrace("Unsupported protocol: {0}", v);
                        }
                        else
                        {
                            // Some other error. Fail.
                            ThrowIfCURLEError(c);
                        }
                    }
                }
            }

            private void SetDecompressionOptions()
            {
                if (!_handler.SupportsAutomaticDecompression)
                {
                    return;
                }

                DecompressionMethods autoDecompression = _handler.AutomaticDecompression;
                bool gzip = (autoDecompression & DecompressionMethods.GZip) != 0;
                bool deflate = (autoDecompression & DecompressionMethods.Deflate) != 0;
                if (gzip || deflate)
                {
                    string encoding = (gzip && deflate) ? EncodingNameGzip + "," + EncodingNameDeflate :
                                       gzip ? EncodingNameGzip :
                                       EncodingNameDeflate;
                    SetCurlOption(CURLoption.CURLOPT_ACCEPT_ENCODING, encoding);
                    EventSourceTrace<string>("Encoding: {0}", encoding);
                }
            }

            internal void SetProxyOptions(Uri requestUri)
            {
                if (!_handler._useProxy)
                {
                    // Explicitly disable the use of a proxy.  This will prevent libcurl from using
                    // any proxy, including ones set via environment variable.
                    SetCurlOption(CURLoption.CURLOPT_PROXY, string.Empty);
                    EventSourceTrace("UseProxy false, disabling proxy");
                    return;
                }

                if (_handler.Proxy == null)
                {
                    // UseProxy was true, but Proxy was null.  Let libcurl do its default handling, 
                    // which includes checking the http_proxy environment variable.
                    EventSourceTrace("UseProxy true, Proxy null, using default proxy");
                    return;
                }

                // Custom proxy specified.
                Uri proxyUri;
                try
                {
                    // Should we bypass a proxy for this URI?
                    if (_handler.Proxy.IsBypassed(requestUri))
                    {
                        SetCurlOption(CURLoption.CURLOPT_PROXY, string.Empty);
                        EventSourceTrace("Proxy's IsBypassed returned true, bypassing proxy");
                        return;
                    }

                    // Get the proxy Uri for this request.
                    proxyUri = _handler.Proxy.GetProxy(requestUri);
                    if (proxyUri == null)
                    {
                        EventSourceTrace("GetProxy returned null, using default.");
                        return;
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // WebRequest.DefaultWebProxy throws PlatformNotSupportedException,
                    // in which case we should use the default rather than the custom proxy.
                    EventSourceTrace("PlatformNotSupportedException from proxy, using default");
                    return;
                }

                // Configure libcurl with the gathered proxy information

                SetCurlOption(CURLoption.CURLOPT_PROXYTYPE, (long)CURLProxyType.CURLPROXY_HTTP);
                SetCurlOption(CURLoption.CURLOPT_PROXY, proxyUri.AbsoluteUri);
                SetCurlOption(CURLoption.CURLOPT_PROXYPORT, proxyUri.Port);
                EventSourceTrace("Proxy: {0}", proxyUri);

                KeyValuePair<NetworkCredential, CURLAUTH> credentialScheme = GetCredentials(_requestMessage.RequestUri, _handler.Proxy.Credentials, AuthTypesPermittedByCredentialKind(_handler.Proxy.Credentials));
                NetworkCredential credentials = credentialScheme.Key;
                if (credentials == CredentialCache.DefaultCredentials)
                {
                    // No "default credentials" on Unix; nop just like UseDefaultCredentials.
                    EventSourceTrace("Default proxy credentials. Skipping.");
                }
                else if (credentials != null)
                {
                    if (string.IsNullOrEmpty(credentials.UserName))
                    {
                        throw new ArgumentException(SR.net_http_argument_empty_string, "UserName");
                    }

                    string credentialText = string.IsNullOrEmpty(credentials.Domain) ?
                        string.Format("{0}:{1}", credentials.UserName, credentials.Password) :
                        string.Format("{2}\\{0}:{1}", credentials.UserName, credentials.Password, credentials.Domain);

                    EventSourceTrace("Proxy credentials set.");
                    SetCurlOption(CURLoption.CURLOPT_PROXYUSERPWD, credentialText);
                }
            }

            internal void SetCredentialsOptions(KeyValuePair<NetworkCredential, CURLAUTH> credentialSchemePair)
            {
                if (credentialSchemePair.Key == null)
                {
                    return;
                }

                NetworkCredential credentials = credentialSchemePair.Key;
                CURLAUTH authScheme = credentialSchemePair.Value;
                string userName = string.IsNullOrEmpty(credentials.Domain) ?
                    credentials.UserName :
                    string.Format("{0}\\{1}", credentials.Domain, credentials.UserName);

                SetCurlOption(CURLoption.CURLOPT_USERNAME, userName);
                SetCurlOption(CURLoption.CURLOPT_HTTPAUTH, (long)authScheme);
                if (credentials.Password != null)
                {
                    SetCurlOption(CURLoption.CURLOPT_PASSWORD, credentials.Password);
                }

                EventSourceTrace("Credentials set.");
            }

            internal void SetCookieOption(Uri uri)
            {
                if (!_handler._useCookie)
                {
                    return;
                }

                string cookieValues = _handler.CookieContainer.GetCookieHeader(uri);
                if (!string.IsNullOrEmpty(cookieValues))
                {
                    SetCurlOption(CURLoption.CURLOPT_COOKIE, cookieValues);
                    EventSourceTrace<string>("Cookies: {0}", cookieValues);
                }
            }

            internal void SetRequestHeaders()
            {
                HttpContentHeaders contentHeaders = null;
                if (_requestMessage.Content != null)
                {
                    SetChunkedModeForSend(_requestMessage);

                    // TODO: Content-Length header isn't getting correctly placed using ToString()
                    // This is a bug in HttpContentHeaders that needs to be fixed.
                    if (_requestMessage.Content.Headers.ContentLength.HasValue)
                    {
                        long contentLength = _requestMessage.Content.Headers.ContentLength.Value;
                        _requestMessage.Content.Headers.ContentLength = null;
                        _requestMessage.Content.Headers.ContentLength = contentLength;
                    }
                    contentHeaders = _requestMessage.Content.Headers;
                }

                var slist = new SafeCurlSListHandle();

                // Add request and content request headers
                if (_requestMessage.Headers != null)
                {
                    AddRequestHeaders(_requestMessage.Headers, slist);
                }
                if (contentHeaders != null)
                {
                    AddRequestHeaders(contentHeaders, slist);
                    if (contentHeaders.ContentType == null)
                    {
                        if (!Interop.Http.SListAppend(slist, NoContentType))
                        {
                            throw CreateHttpRequestException(new CurlException((int)CURLcode.CURLE_OUT_OF_MEMORY, isMulti: false));
                        }
                    }
                }

                // Since libcurl always adds a Transfer-Encoding header, we need to explicitly block
                // it if caller specifically does not want to set the header
                if (_requestMessage.Headers.TransferEncodingChunked.HasValue &&
                    !_requestMessage.Headers.TransferEncodingChunked.Value)
                {
                    if (!Interop.Http.SListAppend(slist, NoTransferEncoding))
                    {
                        throw CreateHttpRequestException(new CurlException((int)CURLcode.CURLE_OUT_OF_MEMORY, isMulti: false));
                    }
                }

                if (!slist.IsInvalid)
                {
                    _requestHeaders = slist;
                    SetCurlOption(CURLoption.CURLOPT_HTTPHEADER, slist);
                }
                else
                {
                    slist.Dispose();
                }
            }

            private void SetSslOptions()
            {
                // SSL Options should be set regardless of the type of the original request,
                // in case an http->https redirection occurs.
                //
                // While this does slow down the theoretical best path of the request the code
                // to decide that we need to register the callback is more complicated than, and
                // potentially more expensive than, just always setting the callback.
                SslProvider.SetSslOptions(this, _handler.ClientCertificateOptions);
            }

            internal void SetCurlCallbacks(
                IntPtr easyGCHandle,
                ReadWriteCallback receiveHeadersCallback,
                ReadWriteCallback sendCallback,
                SeekCallback seekCallback,
                ReadWriteCallback receiveBodyCallback,
                DebugCallback debugCallback)
            {
                if (_callbackHandle == null)
                {
                    _callbackHandle = new SafeCallbackHandle();
                }

                // Add callback for processing headers
                Interop.Http.RegisterReadWriteCallback(
                    _easyHandle,
                    ReadWriteFunction.Header,
                    receiveHeadersCallback,
                    easyGCHandle,
                    ref _callbackHandle);

                // If we're sending data as part of the request, add callbacks for sending request data
                if (_requestMessage.Content != null)
                {
                    Interop.Http.RegisterReadWriteCallback(
                        _easyHandle,
                        ReadWriteFunction.Read,
                        sendCallback,
                        easyGCHandle,
                        ref _callbackHandle);

                    Interop.Http.RegisterSeekCallback(
                        _easyHandle,
                        seekCallback,
                        easyGCHandle,
                        ref _callbackHandle);
                }

                // If we're expecting any data in response, add a callback for receiving body data
                if (_requestMessage.Method != HttpMethod.Head)
                {
                    Interop.Http.RegisterReadWriteCallback(
                        _easyHandle,
                        ReadWriteFunction.Write,
                        receiveBodyCallback,
                        easyGCHandle,
                        ref _callbackHandle);
                }

                if (EventSourceTracingEnabled)
                {
                    SetCurlOption(CURLoption.CURLOPT_VERBOSE, 1L);
                    CURLcode curlResult = Interop.Http.RegisterDebugCallback(
                        _easyHandle, 
                        debugCallback,
                        easyGCHandle,
                        ref _callbackHandle);
                    if (curlResult != CURLcode.CURLE_OK)
                    {
                        EventSourceTrace("Failed to register debug callback.");
                    }
                }
            }

            internal CURLcode SetSslCtxCallback(SslCtxCallback callback, IntPtr userPointer)
            {
                if (_callbackHandle == null)
                {
                    _callbackHandle = new SafeCallbackHandle();
                }

                CURLcode result = Interop.Http.RegisterSslCtxCallback(_easyHandle, callback, userPointer, ref _callbackHandle);
                return result;
            }

            private static void AddRequestHeaders(HttpHeaders headers, SafeCurlSListHandle handle)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
                {
                    string headerValue = headers.GetHeaderString(header.Key);
                    string headerKeyAndValue = string.IsNullOrEmpty(headerValue) ?
                        header.Key + ";" : // semicolon used by libcurl to denote empty value that should be sent
                        header.Key + ": " + headerValue;
                    if (!Interop.Http.SListAppend(handle, headerKeyAndValue))
                    {
                        throw CreateHttpRequestException(new CurlException((int)CURLcode.CURLE_OUT_OF_MEMORY, isMulti: false));
                    }
                }
            }

            internal void SetCurlOption(CURLoption option, string value)
            {
                ThrowIfCURLEError(Interop.Http.EasySetOptionString(_easyHandle, option, value));
            }

            internal void SetCurlOption(CURLoption option, long value)
            {
                ThrowIfCURLEError(Interop.Http.EasySetOptionLong(_easyHandle, option, value));
            }

            internal void SetCurlOption(CURLoption option, IntPtr value)
            {
                ThrowIfCURLEError(Interop.Http.EasySetOptionPointer(_easyHandle, option, value));
            }

            internal void SetCurlOption(CURLoption option, SafeHandle value)
            {
                ThrowIfCURLEError(Interop.Http.EasySetOptionPointer(_easyHandle, option, value));
            }

            internal sealed class SendTransferState
            {
                internal readonly byte[] _buffer = new byte[RequestBufferSize]; // PERF TODO: Determine if this should be optimized to start smaller and grow
                internal int _offset;
                internal int _count;
                internal Task<int> _task;

                internal void SetTaskOffsetCount(Task<int> task, int offset, int count)
                {
                    Debug.Assert(offset >= 0, "Offset should never be negative");
                    Debug.Assert(count >= 0, "Count should never be negative");
                    Debug.Assert(offset <= count, "Offset should never be greater than count");

                    _task = task;
                    _offset = offset;
                    _count = count;
                }
            }

            internal void  SetRedirectUri(Uri redirectUri)
            {
                _targetUri = _requestMessage.RequestUri;
                _requestMessage.RequestUri = redirectUri;
            }

            private void EventSourceTrace<TArg0>(string formatMessage, TArg0 arg0, [CallerMemberName] string memberName = null)
            {
                CurlHandler.EventSourceTrace(formatMessage, arg0, easy: this, memberName: memberName);
            }

            private void EventSourceTrace(string message, [CallerMemberName] string memberName = null)
            {
                CurlHandler.EventSourceTrace(message, easy: this, memberName: memberName);
            }
        }
    }
}
