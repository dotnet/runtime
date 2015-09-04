// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CURLAUTH = Interop.libcurl.CURLAUTH;
using CURLProxyType = Interop.libcurl.curl_proxytype;
using CURLoption = Interop.libcurl.CURLoption;
using SafeCurlHandle = Interop.libcurl.SafeCurlHandle;
using SafeCurlSlistHandle = Interop.libcurl.SafeCurlSlistHandle;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        private sealed class EasyRequest : TaskCompletionSource<HttpResponseMessage>
        {
            private readonly CurlHandler _handler;
            private readonly SafeCurlHandle _easyHandle;

            internal MultiAgent _associatedMultiAgent;

            private readonly HttpRequestMessage _requestMessage;
            private readonly CurlResponseMessage _responseMessage;
            private readonly CancellationToken _cancellationToken;
            private SafeCurlSlistHandle _requestHeaders;

            public EasyRequest(CurlHandler handler, HttpRequestMessage requestMessage, CancellationToken cancellationToken) :
                base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                // Store supplied arguments
                _handler = handler;
                _requestMessage = requestMessage;
                _responseMessage = new CurlResponseMessage(requestMessage);
                _cancellationToken = cancellationToken;

                // Create the underlying easy handle
                _easyHandle = Interop.libcurl.curl_easy_init();
                if (_easyHandle.IsInvalid)
                {
                    throw new OutOfMemoryException();
                }

                // Configure the easy handle
                SetUrl();
                SetDebugging();
                SetMultithreading();
                SetRedirection();
                SetVerb();
                SetDecompressionOptions();
                SetProxyOptions();
                SetCredentialsOptions();
                SetCookieOption();
                SetRequestHeaders();
            }

            public CurlHandler Handler { get { return _handler; } }
            public SafeCurlHandle EasyHandle { get { return _easyHandle; } }
            public CancellationToken CancellationToken { get { return _cancellationToken; } }
            public HttpRequestMessage RequestMessage { get { return _requestMessage; } }
            public CurlResponseMessage ResponseMessage { get { return _responseMessage; } }

            public Stream RequestContentStream { get; set; }
            public NetworkCredential NetworkCredential { get; set; }

            public void EnsureResponseMessagePublished()
            {
                bool result = TrySetResult(ResponseMessage);
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
                    TrySetException(error as HttpRequestException ?? CreateHttpRequestException(error));
                }
                // There's not much we can reasonably assert here about the result of TrySet*.
                // It's possible that the task wasn't yet completed (e.g. a failure while initiating the request),
                // it's possible that the task was already completed as success (e.g. a failure sending back the response),
                // and it's possible that the task was already completed as failure (e.g. we handled the exception and
                // faulted the task, but then tried to fault it again while finishing processing in the main loop).

                // Make sure the exception is available on the response stream so that it's propagated
                // from any attempts to read from the stream.
                ResponseMessage.ContentStream.SignalFailure(ExceptionDispatchInfo.Capture(error));
            }

            public void Cleanup() // not called Dispose because the request may still be in use after it's cleaned up
            {
                ResponseMessage.ContentStream.SignalComplete(); // No more callbacks so no more data
                // Don't dispose of the ResponseMessage.ContentStream as it may still be in use
                // by code reading data stored in the stream.

                // Dispose of the input content stream if there was one.  Nothing should be using it any more.
                if (RequestContentStream != null)
                    RequestContentStream.Dispose();

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
            }

            private void SetUrl()
            {
                SetCurlOption(CURLoption.CURLOPT_URL, _requestMessage.RequestUri.AbsoluteUri);
            }

            [Conditional("CURLHANDLER_VERBOSE")]
            private void SetDebugging()
            {
                SetCurlOption(CURLoption.CURLOPT_VERBOSE, 1L);

                // In addition to CURLOPT_VERBOSE, CURLOPT_DEBUGFUNCTION could be used here in the future to:
                // - Route the verbose output to somewhere other than stderr
                // - Dump additional data related to CURLINFO_DATA_* and CURLINFO_SSL_DATA_*
            }

            private void SetMultithreading()
            {
                SetCurlOption(CURLoption.CURLOPT_NOSIGNAL, 1L);
            }

            private void SetRedirection()
            {
                if (!Handler._automaticRedirection)
                {
                    return;
                }

                SetCurlOption(CURLoption.CURLOPT_FOLLOWLOCATION, 1L);
                SetCurlOption(CURLoption.CURLOPT_MAXREDIRS, Handler._maxAutomaticRedirections);
            }

            private void SetVerb()
            {
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
            }

            private void SetDecompressionOptions()
            {
                if (!Handler.SupportsAutomaticDecompression)
                {
                    return;
                }

                DecompressionMethods autoDecompression = Handler.AutomaticDecompression;
                bool gzip = (autoDecompression & DecompressionMethods.GZip) != 0;
                bool deflate = (autoDecompression & DecompressionMethods.Deflate) != 0;
                if (gzip || deflate)
                {
                    string encoding = (gzip && deflate) ? EncodingNameGzip + "," + EncodingNameDeflate :
                                       gzip ? EncodingNameGzip :
                                       EncodingNameDeflate;
                    SetCurlOption(CURLoption.CURLOPT_ACCEPTENCODING, encoding);
                }
            }

            private void SetProxyOptions()
            {
                if (Handler._proxyPolicy == ProxyUsePolicy.DoNotUseProxy)
                {
                    SetCurlOption(CURLoption.CURLOPT_PROXY, string.Empty);
                    return;
                }

                if ((Handler._proxyPolicy == ProxyUsePolicy.UseDefaultProxy) || 
                    (Handler.Proxy == null))
                {
                    return;
                }

                Debug.Assert(Handler.Proxy != null, "proxy is null");
                Debug.Assert(Handler._proxyPolicy == ProxyUsePolicy.UseCustomProxy, "_proxyPolicy is not UseCustomProxy");
                if (Handler.Proxy.IsBypassed(RequestMessage.RequestUri))
                {
                    SetCurlOption(CURLoption.CURLOPT_PROXY, string.Empty);
                    return;
                }

                var proxyUri = Handler.Proxy.GetProxy(RequestMessage.RequestUri);
                if (proxyUri == null)
                {
                    return;
                }

                SetCurlOption(CURLoption.CURLOPT_PROXYTYPE, CURLProxyType.CURLPROXY_HTTP);
                SetCurlOption(CURLoption.CURLOPT_PROXY, proxyUri.AbsoluteUri);
                SetCurlOption(CURLoption.CURLOPT_PROXYPORT, proxyUri.Port);
                NetworkCredential credentials = GetCredentials(Handler.Proxy.Credentials, RequestMessage.RequestUri);
                if (credentials != null)
                {
                    if (string.IsNullOrEmpty(credentials.UserName))
                    {
                        throw new ArgumentException(SR.net_http_argument_empty_string, "UserName");
                    }

                    string credentialText = string.IsNullOrEmpty(credentials.Domain) ?
                        string.Format("{0}:{1}", credentials.UserName, credentials.Password) :
                        string.Format("{2}\\{0}:{1}", credentials.UserName, credentials.Password, credentials.Domain);
                    SetCurlOption(CURLoption.CURLOPT_PROXYUSERPWD, credentialText);
                }
            }

            private void SetCredentialsOptions()
            {
                NetworkCredential credentials = Handler.GetNetworkCredentials(Handler._serverCredentials, RequestMessage.RequestUri);
                if (credentials == null)
                {
                    return;
                }

                string userName = string.IsNullOrEmpty(credentials.Domain) ?
                    credentials.UserName :
                    string.Format("{0}\\{1}", credentials.Domain, credentials.UserName);

                SetCurlOption(CURLoption.CURLOPT_USERNAME, userName);
                SetCurlOption(CURLoption.CURLOPT_HTTPAUTH, CURLAUTH.AuthAny);
                if (credentials.Password != null)
                {
                    SetCurlOption(CURLoption.CURLOPT_PASSWORD, credentials.Password);
                }

                NetworkCredential = credentials;
            }

            private void SetCookieOption()
            {
                if (!Handler._useCookie)
                {
                    return;
                }

                string cookieValues = Handler.CookieContainer.GetCookieHeader(RequestMessage.RequestUri);
                if (cookieValues != null)
                {
                    SetCurlOption(CURLoption.CURLOPT_COOKIE, cookieValues);
                }
            }

            private void SetRequestHeaders()
            {
                HttpHeaders contentHeaders = null;
                if (RequestMessage.Content != null)
                {
                    SetChunkedModeForSend(RequestMessage);

                    // TODO: Content-Length header isn't getting correctly placed using ToString()
                    // This is a bug in HttpContentHeaders that needs to be fixed.
                    if (RequestMessage.Content.Headers.ContentLength.HasValue)
                    {
                        long contentLength = RequestMessage.Content.Headers.ContentLength.Value;
                        RequestMessage.Content.Headers.ContentLength = null;
                        RequestMessage.Content.Headers.ContentLength = contentLength;
                    }
                    contentHeaders = RequestMessage.Content.Headers;
                }

                var slist = new SafeCurlSlistHandle();

                // Add request and content request headers
                if (RequestMessage.Headers != null)
                {
                    AddRequestHeaders(RequestMessage.Headers, slist);
                }
                if (contentHeaders != null)
                {
                    AddRequestHeaders(contentHeaders, slist);
                }

                // Since libcurl always adds a Transfer-Encoding header, we need to explicitly block
                // it if caller specifically does not want to set the header
                if (RequestMessage.Headers.TransferEncodingChunked.HasValue && 
                    !RequestMessage.Headers.TransferEncodingChunked.Value)
                {
                    if (!Interop.libcurl.curl_slist_append(slist, NoTransferEncoding))
                    {
                        throw CreateHttpRequestException();
                    }
                }

                if (!slist.IsInvalid)
                {
                    SetCurlOption(CURLoption.CURLOPT_HTTPHEADER, slist);
                    _requestHeaders = slist;
                }
                else
                {
                    slist.Dispose();
                }
            }

            private static void AddRequestHeaders(HttpHeaders headers, SafeCurlSlistHandle handle)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
                {
                    string headerStr = header.Key + ": " + headers.GetHeaderString(header.Key);
                    if (!Interop.libcurl.curl_slist_append(handle, headerStr))
                    {
                        throw CreateHttpRequestException();
                    }
                }
            }

            internal void SetCurlOption(int option, string value)
            {
                ThrowIfCURLEError(Interop.libcurl.curl_easy_setopt(_easyHandle, option, value));
            }

            internal void SetCurlOption(int option, long value)
            {
                ThrowIfCURLEError(Interop.libcurl.curl_easy_setopt(_easyHandle, option, value));
            }

            internal void SetCurlOption(int option, ulong value)
            {
                ThrowIfCURLEError(Interop.libcurl.curl_easy_setopt(_easyHandle, option, value));
            }

            internal void SetCurlOption(int option, IntPtr value)
            {
                ThrowIfCURLEError(Interop.libcurl.curl_easy_setopt(_easyHandle, option, value));
            }

            internal void SetCurlOption(int option, Delegate value)
            {
                ThrowIfCURLEError(Interop.libcurl.curl_easy_setopt(_easyHandle, option, value));
            }

            internal void SetCurlOption(int option, SafeHandle value)
            {
                ThrowIfCURLEError(Interop.libcurl.curl_easy_setopt(_easyHandle, option, value));
            }

        }
    }
}
