// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public class HttpWebRequest : WebRequest
    {
        private const int DefaultContinueTimeout = 350; // Current default value from .NET Desktop.

        private WebHeaderCollection _webHeaderCollection = new WebHeaderCollection();

        private Uri _requestUri;
        private string _originVerb = HttpMethod.Get.Method;

        // We allow getting and setting this (to preserve app-compat). But we don't do anything with it 
        // as the underlying System.Net.Http API doesn't support it.
        private int _continueTimeout = DefaultContinueTimeout;

        private bool _allowReadStreamBuffering = false;
        private CookieContainer _cookieContainer = null;
        private ICredentials _credentials = null;
        private IWebProxy _proxy = WebRequest.DefaultWebProxy;

        private Task<HttpResponseMessage> _sendRequestTask;

        private int _beginGetRequestStreamCalled = 0;
        private int _beginGetResponseCalled = 0;
        private int _endGetRequestStreamCalled = 0;
        private int _endGetResponseCalled = 0;

        private RequestStream _requestStream;

        private TaskCompletionSource<Stream> _requestStreamOperation = null;
        private TaskCompletionSource<WebResponse> _responseOperation = null;
        private AsyncCallback _requestStreamCallback = null;
        private AsyncCallback _responseCallback = null;
        private int _abortCalled = 0;
        private CancellationTokenSource _sendRequestCts;

        internal HttpWebRequest(Uri uri)
        {
            _requestUri = uri;
        }

        private void SetSpecialHeaders(string HeaderName, string value)
        {
            _webHeaderCollection.Remove(HeaderName);
            if (!string.IsNullOrEmpty(value))
            {
                _webHeaderCollection[HeaderName] = value;
            }
        }

        public string Accept
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.Accept];
            }
            set
            {
                SetSpecialHeaders(HttpKnownHeaderNames.Accept, value);
            }
        }

        public virtual bool AllowReadStreamBuffering
        {
            get
            {
                return _allowReadStreamBuffering;
            }
            set
            {
                _allowReadStreamBuffering = value;
            }
        }

        public override String ContentType
        {
            get
            {
                return _webHeaderCollection[HttpKnownHeaderNames.ContentType];
            }
            set
            {
                SetSpecialHeaders(HttpKnownHeaderNames.ContentType, value);
            }
        }

        public int ContinueTimeout
        {
            get
            {
                return _continueTimeout;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }
                if ((value < 0) && (value != System.Threading.Timeout.Infinite))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_ge_zero);
                }
                _continueTimeout = value;
            }
        }

        public virtual CookieContainer CookieContainer
        {
            get
            {
                return _cookieContainer;
            }
            set
            {
                _cookieContainer = value;
            }
        }

        public override ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }

        public virtual bool HaveResponse
        {
            get
            {
                return (_sendRequestTask != null) && (_sendRequestTask.Status == TaskStatus.RanToCompletion);
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                return _webHeaderCollection;
            }
            set
            {
                // We can't change headers after they've already been sent.
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                WebHeaderCollection webHeaders = value;
                WebHeaderCollection newWebHeaders = new WebHeaderCollection();

                // Copy And Validate -
                // Handle the case where their object tries to change
                //  name, value pairs after they call set, so therefore,
                //  we need to clone their headers.
                foreach (String headerName in webHeaders.AllKeys)
                {
                    newWebHeaders[headerName] = webHeaders[headerName];
                }

                _webHeaderCollection = newWebHeaders;
            }
        }

        public override string Method
        {
            get
            {
                return _originVerb;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(SR.net_badmethod, nameof(value));
                }

                if (HttpValidationHelpers.IsInvalidMethodOrHeaderString(value))
                {
                    throw new ArgumentException(SR.net_badmethod, nameof(value));
                }
                _originVerb = value;
            }
        }

        public override Uri RequestUri
        {
            get
            {
                return _requestUri;
            }
        }

        public virtual bool SupportsCookieContainer
        {
            get
            {
                return true;
            }
        }

        public override bool UseDefaultCredentials
        {
            get
            {
                return (_credentials == CredentialCache.DefaultCredentials);
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_writestarted);
                }

                // Match Desktop behavior.  Changing this property will also
                // change the .Credentials property as well.
                _credentials = value ? CredentialCache.DefaultCredentials : null;
            }
        }

        public override IWebProxy Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                // We can't change the proxy while the request is already fired.
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException(SR.net_reqsubmitted);
                }

                _proxy = value;
            }
        }

        public override void Abort()
        {
            if (Interlocked.Exchange(ref _abortCalled, 1) != 0)
            {
                return;
            }

            // .NET Desktop behavior requires us to invoke outstanding callbacks
            // before returning if used in either the BeginGetRequestStream or
            // BeginGetResponse methods.
            //
            // If we can transition the task to the canceled state, then we invoke
            // the callback. If we can't transition the task, it is because it is
            // already in the terminal state and the callback has already been invoked
            // via the async task continuation.

            if (_responseOperation != null)
            {
                if (_responseOperation.TrySetCanceled() && _responseCallback != null)
                {
                    _responseCallback(_responseOperation.Task);
                }

                // Cancel the underlying send operation.
                Debug.Assert(_sendRequestCts != null);
                _sendRequestCts.Cancel();
            }
            else if (_requestStreamOperation != null)
            {
                if (_requestStreamOperation.TrySetCanceled() && _requestStreamCallback != null)
                {
                    _requestStreamCallback(_requestStreamOperation.Task);
                }
            }
        }

        private Task<Stream> GetRequestStream()
        {
            CheckAbort();

            if (RequestSubmitted)
            {
                throw new InvalidOperationException(SR.net_reqsubmitted);
            }

            // Match Desktop behavior: prevent someone from getting a request stream
            // if the protocol verb/method doesn't support it. Note that this is not
            // entirely compliant RFC2616 for the aforementioned compatbility reasons.
            if (string.Equals(HttpMethod.Get.Method, _originVerb, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(HttpMethod.Head.Method, _originVerb, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("CONNECT", _originVerb, StringComparison.OrdinalIgnoreCase))
            {
                throw new ProtocolViolationException(SR.net_nouploadonget);
            }

            _requestStream = new RequestStream();

            return Task.FromResult((Stream)_requestStream);
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, Object state)
        {
            CheckAbort();

            if (Interlocked.Exchange(ref _beginGetRequestStreamCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.net_repcall);
            }

            _requestStreamCallback = callback;
            _requestStreamOperation = GetRequestStream().ToApm(callback, state);

            return _requestStreamOperation.Task;
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            CheckAbort();

            if (asyncResult == null || !(asyncResult is Task<Stream>))
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            if (Interlocked.Exchange(ref _endGetRequestStreamCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetRequestStream"));
            }

            Stream stream;
            try
            {
                stream = ((Task<Stream>)asyncResult).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw WebException.CreateCompatibleException(ex);
            }

            return stream;
        }

        private async Task<WebResponse> SendRequest()
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException(SR.net_reqsubmitted);
            }

            var handler = new HttpClientHandler();
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(new HttpMethod(_originVerb), _requestUri);

            if (_requestStream != null)
            {
                ArraySegment<byte> bytes = _requestStream.GetBuffer();
                request.Content = new ByteArrayContent(bytes.Array, bytes.Offset, bytes.Count);
            }

            // Set to match original defaults of HttpWebRequest.
            // HttpClientHandler.AutomaticDecompression defaults to true; set it to false to match the desktop behavior 
            handler.AutomaticDecompression = DecompressionMethods.None;
            handler.Credentials = _credentials;

            if (_cookieContainer != null)
            {
                handler.CookieContainer = _cookieContainer;
                Debug.Assert(handler.UseCookies); // Default of handler.UseCookies is true.
            }
            else
            {
                handler.UseCookies = false;
            }

            Debug.Assert(handler.UseProxy); // Default of handler.UseProxy is true.
            Debug.Assert(handler.Proxy == null); // Default of handler.Proxy is null.
            if (_proxy == null)
            {
                handler.UseProxy = false;
            }
            else
            {
                handler.Proxy = _proxy;
            }

            // Copy the HttpWebRequest request headers from the WebHeaderCollection into HttpRequestMessage.Headers and
            // HttpRequestMessage.Content.Headers.
            foreach (string headerName in _webHeaderCollection)
            {
                // The System.Net.Http APIs require HttpRequestMessage headers to be properly divided between the request headers
                // collection and the request content headers colllection for all well-known header names.  And custom headers
                // are only allowed in the request headers collection and not in the request content headers collection.
                if (IsWellKnownContentHeader(headerName))
                {
                    if (request.Content == null)
                    {
                        // Create empty content so that we can send the entity-body header.
                        request.Content = new ByteArrayContent(Array.Empty<byte>());
                    }

                    request.Content.Headers.TryAddWithoutValidation(headerName, _webHeaderCollection[headerName]);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(headerName, _webHeaderCollection[headerName]);
                }
            }

            _sendRequestTask = client.SendAsync(
                request,
                _allowReadStreamBuffering ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead,
                _sendRequestCts.Token);
            HttpResponseMessage responseMessage = await _sendRequestTask.ConfigureAwait(false);

            HttpWebResponse response = new HttpWebResponse(responseMessage, _requestUri, _cookieContainer);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new WebException(
                    SR.Format(SR.net_servererror, (int)response.StatusCode, response.StatusDescription),
                    null,
                    WebExceptionStatus.ProtocolError,
                    response);
            }

            return response;
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            CheckAbort();

            if (Interlocked.Exchange(ref _beginGetResponseCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.net_repcall);
            }

            _sendRequestCts = new CancellationTokenSource();
            _responseCallback = callback;
            _responseOperation = SendRequest().ToApm(callback, state);

            return _responseOperation.Task;
        }

        public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            CheckAbort();

            if (asyncResult == null || !(asyncResult is Task<WebResponse>))
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            if (Interlocked.Exchange(ref _endGetResponseCalled, 1) != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetResponse"));
            }

            WebResponse response;
            try
            {
                response = ((Task<WebResponse>)asyncResult).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw WebException.CreateCompatibleException(ex);
            }

            return response;
        }

        private bool RequestSubmitted
        {
            get
            {
                return _sendRequestTask != null;
            }
        }

        private void CheckAbort()
        {
            if (Volatile.Read(ref _abortCalled) == 1)
            {
                throw new WebException(SR.net_reqaborted, WebExceptionStatus.RequestCanceled);
            }
        }

        private readonly static string[] s_wellKnownContentHeaders = {
            HttpKnownHeaderNames.ContentDisposition,
            HttpKnownHeaderNames.ContentEncoding,
            HttpKnownHeaderNames.ContentLanguage,
            HttpKnownHeaderNames.ContentLength,
            HttpKnownHeaderNames.ContentLocation,
            HttpKnownHeaderNames.ContentMD5,
            HttpKnownHeaderNames.ContentRange,
            HttpKnownHeaderNames.ContentType,
            HttpKnownHeaderNames.Expires,
            HttpKnownHeaderNames.LastModified
        };

        private bool IsWellKnownContentHeader(string header)
        {
            foreach (string contentHeaderName in s_wellKnownContentHeaders)
            {
                if (string.Equals(header, contentHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
