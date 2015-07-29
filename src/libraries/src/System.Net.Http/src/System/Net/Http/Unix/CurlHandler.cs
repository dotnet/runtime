// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using SafeCurlHandle = Interop.libcurl.SafeCurlHandle;
using CURLoption = Interop.libcurl.CURLoption;
using CURLcode = Interop.libcurl.CURLcode;
using CURLINFO = Interop.libcurl.CURLINFO;
using CURLProxyType = Interop.libcurl.curl_proxytype;

namespace System.Net.Http
{
    internal class CurlHandler : HttpMessageHandler
    {
        #region Constants

        private const string UriSchemeHttps = "https";
        private readonly static string[] AuthenticationSchemes = { "Negotiate", "Digest", "Basic" }; // the order in which libcurl goes over authentication schemes
        #endregion

        #region Fields

        private volatile bool _anyOperationStarted;
        private volatile bool _disposed;
        private bool _automaticRedirection = true;
        private IWebProxy _proxy = null;
        private ICredentials _serverCredentials = null;
        private ProxyUsePolicy _proxyPolicy = ProxyUsePolicy.UseDefaultProxy;

        #endregion

        internal CurlHandler()
        {
        }

        #region Properties
        internal bool AutomaticRedirection
        {
            get
            {
                return _automaticRedirection;
            }

            set
            {
                CheckDisposedOrStarted();
                _automaticRedirection = value;
            }
        }

        internal bool SupportsProxy
        {
            get
            {
                return true;
            }
        }

        internal bool UseProxy
        {
            get
            {
                return _proxyPolicy != ProxyUsePolicy.DoNotUseProxy;
            }
            set
            {
                CheckDisposedOrStarted();
                if (value)
                {
                    _proxyPolicy = ProxyUsePolicy.UseCustomProxy;
                }
                else
                {
                    _proxyPolicy = ProxyUsePolicy.DoNotUseProxy;
                }
            }
        }

        internal IWebProxy Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                CheckDisposedOrStarted();
                _proxy = value;
            }
        }
        
        internal ICredentials Credentials
        {
            get
            {
                return _serverCredentials;
            }
            set
            {
                CheckDisposedOrStarted();
                _serverCredentials = value;
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
            }

            base.Dispose(disposing);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", SR.net_http_handler_norequest);
            }
            if (request.RequestUri.Scheme == UriSchemeHttps)
            {
                throw NotImplemented.ByDesignWithMessage("HTTPS stack is not yet implemented");
            }
            if (request.Content != null)
            {
                throw NotImplemented.ByDesignWithMessage("HTTP requests with a body are not yet supported");
            }

            CheckDisposed();

            SetOperationStarted();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            // Create RequestCompletionSource object and save current values of handler settings.
            RequestCompletionSource state = new RequestCompletionSource();
            state.CancellationToken = cancellationToken;
            state.RequestMessage = request;
            state.Handler = this;

            Task.Factory.StartNew(
                s =>
                {
                    var rcs = (RequestCompletionSource)s;
                    rcs.Handler.StartRequest(rcs);
                },
                state,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            return state.Task;
        }

        #region Private methods

        private void StartRequest(RequestCompletionSource state)
        {
            HttpResponseMessage responseMessage = null;
            SafeCurlHandle requestHandle = null;

            if (state.CancellationToken.IsCancellationRequested)
            {
                state.TrySetCanceled();
                return;
            }

            var cancellationTokenRegistration = state.CancellationToken.Register(x => ((RequestCompletionSource) x).TrySetCanceled(), state);

            try
            {
                requestHandle = CreateRequestHandle(state);

                state.CancellationToken.ThrowIfCancellationRequested();
                int result = Interop.libcurl.curl_easy_perform(requestHandle);
                if (CURLcode.CURLE_OK != result)
                {
                    throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
                }

                // TODO: Handle requests with body

                state.CancellationToken.ThrowIfCancellationRequested();
                responseMessage = CreateResponseMessage(requestHandle, state.RequestMessage);
                state.TrySetResult(responseMessage);
            }
            catch (Exception ex)
            {
                HandleAsyncException(state, ex);
            }
            finally
            {
                SafeCurlHandle.DisposeAndClearHandle(ref requestHandle);
                cancellationTokenRegistration.Dispose();
            }
        }

        private void SetOperationStarted()
        {
            if (!_anyOperationStarted)
            {
                _anyOperationStarted = true;
            }
        }

        private SafeCurlHandle CreateRequestHandle(RequestCompletionSource state)
        {
            // TODO: If this impacts perf, optimize using a handle pool
            SafeCurlHandle requestHandle = Interop.libcurl.curl_easy_init();
            if (requestHandle.IsInvalid)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error);
            }

            Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_URL,
                state.RequestMessage.RequestUri.OriginalString);
            if (_automaticRedirection)
            {
                Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_FOLLOWLOCATION, 1);
            }

            SetProxyOptions(state, requestHandle);
            // TODO: Handle headers and other options

            return requestHandle;
        }

        private static void SetProxyOptions(RequestCompletionSource state, SafeCurlHandle requestHandle)
        {
            var requestUri = state.RequestMessage.RequestUri;
            Debug.Assert(state.Handler != null);
            if (state.Handler._proxyPolicy == ProxyUsePolicy.DoNotUseProxy)
            {
                Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_PROXY, string.Empty);
                return;
            }

            if ((state.Handler._proxyPolicy == ProxyUsePolicy.UseDefaultProxy) || (state.Handler.Proxy == null))
            {
                return;
            }

            Debug.Assert( (state.Handler.Proxy != null) && (state.Handler._proxyPolicy == ProxyUsePolicy.UseCustomProxy));
            if (state.Handler.Proxy.IsBypassed(requestUri))
            {
                Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_PROXY, string.Empty);
                return;
            }

            var proxyUri = state.Handler.Proxy.GetProxy(requestUri);
            if (proxyUri == null)
            {
                return;
            }

            Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_PROXYTYPE, CURLProxyType.CURLPROXY_HTTP);
            Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_PROXY, proxyUri.AbsoluteUri);
            Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_PROXYPORT, proxyUri.Port);
            NetworkCredential credentials = GetCredentials(state.Handler.Proxy.Credentials, requestUri);
            if (credentials != null)
            {
                if (string.IsNullOrEmpty(credentials.UserName))
                {
                    throw new ArgumentException(SR.net_http_argument_empty_string, "UserName");
                }

                string credentialText;
                if (string.IsNullOrEmpty(credentials.Domain))
                {
                    credentialText = string.Format("{0}:{1}", credentials.UserName, credentials.Password);
                }
                else
                {
                    credentialText = string.Format("{2}\\{0}:{1}", credentials.UserName, credentials.Password, credentials.Domain);
                }
                Interop.libcurl.curl_easy_setopt(requestHandle, CURLoption.CURLOPT_PROXYUSERPWD, credentialText);
            }
        }

        private static NetworkCredential GetCredentials(ICredentials proxyCredentials, Uri requestUri)
        {
            if (proxyCredentials == null)
            {
                return null;
            }

            foreach (var authScheme in AuthenticationSchemes)
            {
                NetworkCredential proxyCreds = proxyCredentials.GetCredential(requestUri, authScheme);
                if (proxyCreds != null)
                {
                    return proxyCreds;
                }
            }

            return null;
        }

        private HttpResponseMessage CreateResponseMessage(SafeCurlHandle requestHandle, HttpRequestMessage request)
        {
            var response = new HttpResponseMessage();
            long httpStatusCode = 0;
            int result = Interop.libcurl.curl_easy_getinfo(requestHandle, CURLINFO.CURLINFO_RESPONSE_CODE,
                ref httpStatusCode);
            if (CURLcode.CURLE_OK != result)
            {
                throw new HttpRequestException(SR.net_http_client_execution_error, GetCurlException(result));
            }
            response.StatusCode = (HttpStatusCode)httpStatusCode;

            // TODO: Do error processing if needed and return actual response
            response.Content =
                new StringContent("SendAsync to " + request.RequestUri.OriginalString + " returned: " +
                                  response.StatusCode);
            return response;
        }

        private void HandleAsyncException(RequestCompletionSource state, Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                // If the exception was due to the cancellation token being canceled, throw cancellation exception.
                Debug.Assert(state.CancellationToken.IsCancellationRequested);
                state.TrySetCanceled();
            }
            else if (ex is HttpRequestException)
            {
                state.TrySetException(ex);
            }
            else
            {
                state.TrySetException(new HttpRequestException(SR.net_http_client_execution_error, ex));
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void CheckDisposedOrStarted()
        {
            CheckDisposed();
            if (_anyOperationStarted)
            {
                throw new InvalidOperationException(SR.net_http_operation_started);
            }
        }

        private string GetCurlErrorString(int code)
        {
            IntPtr ptr = Interop.libcurl.curl_easy_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        private Exception GetCurlException(int code)
        {
            return new Exception(GetCurlErrorString(code));
        }

        #endregion

        private sealed class RequestCompletionSource : TaskCompletionSource<HttpResponseMessage>
        {
            public CancellationToken CancellationToken { get; set; }

            public HttpRequestMessage RequestMessage { get; set; }

            public CurlHandler Handler { get; set; }
        }

        private enum ProxyUsePolicy
        {
            DoNotUseProxy = 0, // Do not use proxy. Ignores the value set in the environment.
            UseDefaultProxy = 1, // Do not set the proxy parameter. Use the value of environment variable, if any.
            UseCustomProxy = 2  // Use The proxy specified by the user.
        }
    }
}

