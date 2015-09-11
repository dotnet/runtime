// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using CURLAUTH = Interop.libcurl.CURLAUTH;
using CURLoption = Interop.libcurl.CURLoption;
using CURLProxyType = Interop.libcurl.curl_proxytype;
using SafeCurlHandle = Interop.libcurl.SafeCurlHandle;
using SafeCurlSlistHandle = Interop.libcurl.SafeCurlSlistHandle;

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

            internal SafeCurlHandle _easyHandle;
            private SafeCurlSlistHandle _requestHeaders;

            internal Stream _requestContentStream;
            internal NetworkCredential _networkCredential;

            internal MultiAgent _associatedMultiAgent;
            internal SendTransferState _sendTransferState;
            internal bool _isRedirect = false;

            public EasyRequest(CurlHandler handler, HttpRequestMessage requestMessage, CancellationToken cancellationToken) :
                base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _handler = handler;
                _requestMessage = requestMessage;
                _responseMessage = new CurlResponseMessage(this);
                _cancellationToken = cancellationToken;
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
                SafeCurlHandle easyHandle = Interop.libcurl.curl_easy_init();
                if (easyHandle.IsInvalid)
                {
                    throw new OutOfMemoryException();
                }
                _easyHandle = easyHandle;

                // Configure the handle
                SetUrl();
                SetDebugging();
                SetMultithreading();
                SetRedirection();
                SetVerb();
                SetDecompressionOptions();
                SetProxyOptions(_requestMessage.RequestUri);
                SetCredentialsOptions(_handler.GetNetworkCredentials(_handler._serverCredentials,_requestMessage.RequestUri));
                SetCookieOption(_requestMessage.RequestUri);
                SetRequestHeaders();
            }

            public void EnsureResponseMessagePublished()
            {
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
                    TrySetException(error as HttpRequestException ?? CreateHttpRequestException(error));
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
            }

            private void SetUrl()
            {
                VerboseTrace(_requestMessage.RequestUri.AbsoluteUri);
                SetCurlOption(CURLoption.CURLOPT_URL, _requestMessage.RequestUri.AbsoluteUri);
            }

            [Conditional(VerboseDebuggingConditional)]
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
                if (!_handler._automaticRedirection)
                {
                    return;
                }

                VerboseTrace(_handler._maxAutomaticRedirections.ToString());
                SetCurlOption(CURLoption.CURLOPT_FOLLOWLOCATION, 1L);
                SetCurlOption(CURLoption.CURLOPT_MAXREDIRS, _handler._maxAutomaticRedirections);
            }

            private void SetVerb()
            {
                VerboseTrace(_requestMessage.Method.Method);
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
                    SetCurlOption(CURLoption.CURLOPT_ACCEPTENCODING, encoding);
                    VerboseTrace(encoding);
                }
            }

            internal void SetProxyOptions(Uri requestUri)
            {
                if (_handler._proxyPolicy == ProxyUsePolicy.DoNotUseProxy)
                {
                    SetCurlOption(CURLoption.CURLOPT_PROXY, string.Empty);
                    VerboseTrace("No proxy");
                    return;
                }

                if ((_handler._proxyPolicy == ProxyUsePolicy.UseDefaultProxy) || 
                    (_handler.Proxy == null))
                {
                    VerboseTrace("Default proxy");
                    return;
                }

                Debug.Assert(_handler.Proxy != null, "proxy is null");
                Debug.Assert(_handler._proxyPolicy == ProxyUsePolicy.UseCustomProxy, "_proxyPolicy is not UseCustomProxy");
                if (_handler.Proxy.IsBypassed(requestUri))
                {
                    SetCurlOption(CURLoption.CURLOPT_PROXY, string.Empty);
                    VerboseTrace("Bypassed proxy");
                    return;
                }

                var proxyUri = _handler.Proxy.GetProxy(requestUri);
                if (proxyUri == null)
                {
                    VerboseTrace("No proxy URI");
                    return;
                }

                SetCurlOption(CURLoption.CURLOPT_PROXYTYPE, CURLProxyType.CURLPROXY_HTTP);
                SetCurlOption(CURLoption.CURLOPT_PROXY, proxyUri.AbsoluteUri);
                SetCurlOption(CURLoption.CURLOPT_PROXYPORT, proxyUri.Port);
                VerboseTrace("Set proxy: " + proxyUri.ToString());

                NetworkCredential credentials = GetCredentials(_handler.Proxy.Credentials, _requestMessage.RequestUri);
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
                    VerboseTrace("Set proxy credentials");
                }
            }

            internal void SetCredentialsOptions(NetworkCredential credentials)
            {
                if (credentials == null)
                {
                    SetCurlOption(CURLoption.CURLOPT_HTTPAUTH, CURLAUTH.None);
                    SetCurlOption(CURLoption.CURLOPT_USERNAME, IntPtr.Zero);
                    SetCurlOption(CURLoption.CURLOPT_PASSWORD, IntPtr.Zero);
                    _networkCredential = null;
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

                _networkCredential = credentials;
                VerboseTrace("Set credentials options");
            }

            internal void SetCookieOption(Uri uri)
            {
                if (!_handler._useCookie)
                {
                    return;
                }

                string cookieValues = _handler.CookieContainer.GetCookieHeader(uri);
                if (cookieValues != null)
                {
                    SetCurlOption(CURLoption.CURLOPT_COOKIE, cookieValues);
                    VerboseTrace("Set cookies");
                }
            }

            private void SetRequestHeaders()
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

                var slist = new SafeCurlSlistHandle();

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
                        if (!Interop.libcurl.curl_slist_append(slist, NoContentType))
                        {
                            throw CreateHttpRequestException();
                        }
                    }
                }

                // Since libcurl always adds a Transfer-Encoding header, we need to explicitly block
                // it if caller specifically does not want to set the header
                if (_requestMessage.Headers.TransferEncodingChunked.HasValue && 
                    !_requestMessage.Headers.TransferEncodingChunked.Value)
                {
                    if (!Interop.libcurl.curl_slist_append(slist, NoTransferEncoding))
                    {
                        throw CreateHttpRequestException();
                    }
                }

                if (!slist.IsInvalid)
                {
                    _requestHeaders = slist;
                    SetCurlOption(CURLoption.CURLOPT_HTTPHEADER, slist);
                    VerboseTrace("Set headers");
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

            [Conditional(VerboseDebuggingConditional)]
            private void VerboseTrace(string text = null, [CallerMemberName] string memberName = null)
            {
                CurlHandler.VerboseTrace(text, memberName, easy: this, agent: null);
            }
        }
    }
}
