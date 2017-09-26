// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

using Windows.Web.Http.Headers;

using RTHttpMethod = Windows.Web.Http.HttpMethod;
using RTHttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using RTHttpResponseMessage = Windows.Web.Http.HttpResponseMessage;
using RTHttpBufferContent = Windows.Web.Http.HttpBufferContent;
using RTHttpStreamContent = Windows.Web.Http.HttpStreamContent;
using RTHttpVersion = Windows.Web.Http.HttpVersion;
using RTIHttpContent = Windows.Web.Http.IHttpContent;
using RTIInputStream = Windows.Storage.Streams.IInputStream;
using RTHttpBaseProtocolFilter = Windows.Web.Http.Filters.HttpBaseProtocolFilter;
using RTChainValidationResult = Windows.Security.Cryptography.Certificates.ChainValidationResult;

namespace System.Net.Http
{
    internal class HttpHandlerToFilter : HttpMessageHandler
    {
        // We need two different WinRT filters because we need to remove credentials during redirection requests
        // and WinRT doesn't allow changing the filter properties after the first request.
        private readonly RTHttpBaseProtocolFilter _filter;
        private Lazy<RTHttpBaseProtocolFilter> _filterWithNoCredentials;
        private RTHttpBaseProtocolFilter FilterWithNoCredentials => _filterWithNoCredentials.Value;

        private int _filterMaxVersionSet;
        private HttpClientHandler _handler;

        internal HttpHandlerToFilter(
            RTHttpBaseProtocolFilter filter,
            HttpClientHandler handler)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            _filter = filter;
            _filterMaxVersionSet = 0;
            _handler = handler;
            
            _filterWithNoCredentials = new Lazy<RTHttpBaseProtocolFilter>(InitFilterWithNoCredentials);
        }

        internal string RequestMessageLookupKey { get; set; }
        internal string SavedExceptionDispatchInfoLookupKey { get; set; }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            int redirects = 0;
            HttpMethod requestHttpMethod;
            bool skipRequestContentIfPresent = false;
            HttpResponseMessage response = null;

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            requestHttpMethod = request.Method;

            while (true)
            {
                cancel.ThrowIfCancellationRequested();

                if (response != null)
                {
                    response.Dispose();
                    response = null;
                }

                RTHttpRequestMessage rtRequest = await ConvertRequestAsync(
                    request,
                    requestHttpMethod,
                    skipRequestContentIfPresent).ConfigureAwait(false);

                RTHttpResponseMessage rtResponse;
                try
                {
                    rtResponse = await (redirects > 0 ? FilterWithNoCredentials : _filter).SendRequestAsync(rtRequest).AsTask(cancel).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch
                {
                    if (rtRequest.Properties.TryGetValue(SavedExceptionDispatchInfoLookupKey, out object info))
                    {
                        ((ExceptionDispatchInfo)info).Throw();
                    }

                    throw;
                }

                response = ConvertResponse(rtResponse);

                ProcessResponseCookies(response, request.RequestUri);

                if (!_handler.AllowAutoRedirect)
                {
                    break;
                }

                if (response.StatusCode != HttpStatusCode.MultipleChoices &&
                    response.StatusCode != HttpStatusCode.MovedPermanently &&
                    response.StatusCode != HttpStatusCode.Redirect &&
                    response.StatusCode != HttpStatusCode.RedirectMethod &&
                    response.StatusCode != HttpStatusCode.RedirectKeepVerb)
                {
                    break;
                }

                redirects++;
                if (redirects > _handler.MaxAutomaticRedirections)
                {
                    break;
                }

                Uri redirectUri = response.Headers.Location;
                if (redirectUri == null)
                {
                    break;
                }

                if (!redirectUri.IsAbsoluteUri)
                {
                    redirectUri = new Uri(request.RequestUri, redirectUri.OriginalString);
                }

                if (redirectUri.Scheme != Uri.UriSchemeHttp &&
                    redirectUri.Scheme != Uri.UriSchemeHttps)
                {
                    break;
                }

                if (request.RequestUri.Scheme == Uri.UriSchemeHttps &&
                    redirectUri.Scheme == Uri.UriSchemeHttp)
                {
                    break;
                }

                // Follow HTTP RFC 7231 rules. In general, 3xx responses
                // except for 307 will keep verb except POST becomes GET.
                // 307 responses have all verbs stay the same.
                // https://tools.ietf.org/html/rfc7231#section-6.4
                if (response.StatusCode != HttpStatusCode.RedirectKeepVerb &&
                    requestHttpMethod == HttpMethod.Post)
                {
                    requestHttpMethod = HttpMethod.Get;
                    skipRequestContentIfPresent = true;
                }

                request.RequestUri = redirectUri;
            }

            response.RequestMessage = request;

            return response;
        }

        private RTHttpBaseProtocolFilter InitFilterWithNoCredentials()
        {
            RTHttpBaseProtocolFilter filter = new RTHttpBaseProtocolFilter();

            filter.AllowAutoRedirect = _filter.AllowAutoRedirect;
            filter.AllowUI = _filter.AllowUI;
            filter.AutomaticDecompression = _filter.AutomaticDecompression;
            filter.CacheControl.ReadBehavior = _filter.CacheControl.ReadBehavior;
            filter.CacheControl.WriteBehavior = _filter.CacheControl.WriteBehavior;

            if (HttpClientHandler.RTCookieUsageBehaviorSupported)
            {
                filter.CookieUsageBehavior = _filter.CookieUsageBehavior;
            }

            filter.MaxConnectionsPerServer = _filter.MaxConnectionsPerServer;
            filter.MaxVersion = _filter.MaxVersion;
            filter.UseProxy = _filter.UseProxy;

            if (_handler.ServerCertificateCustomValidationCallback != null)
            {
                foreach (RTChainValidationResult error in _filter.IgnorableServerCertificateErrors)
                {
                    filter.IgnorableServerCertificateErrors.Add(error);
                }

                filter.ServerCustomValidationRequested += _handler.RTServerCertificateCallback;
            }

            return filter;
        }

        // Taken from System.Net.CookieModule.OnReceivedHeaders
        private void ProcessResponseCookies(HttpResponseMessage response, Uri uri)
        {
            if (_handler.UseCookies)
            {
                IEnumerable<string> values;
                if (response.Headers.TryGetValues(HttpKnownHeaderNames.SetCookie, out values))
                {
                    foreach (string cookieString in values)
                    {
                        if (!string.IsNullOrWhiteSpace(cookieString))
                        {
                            try
                            {
                                // Parse the cookies so that we can filter some of them out
                                CookieContainer helper = new CookieContainer();
                                helper.SetCookies(uri, cookieString);
                                CookieCollection cookies = helper.GetCookies(uri);
                                foreach (Cookie cookie in cookies)
                                {
                                    // We don't want to put HttpOnly cookies in the CookieContainer if the system
                                    // doesn't support the RTHttpBaseProtocolFilter CookieUsageBehavior property.
                                    // Prior to supporting that, the WinRT HttpClient could not turn off cookie
                                    // processing. So, it would always be storing all cookies in its internal container.
                                    // Putting HttpOnly cookies in the .NET CookieContainer would cause problems later
                                    // when the .NET layer tried to add them on outgoing requests and conflicted with
                                    // the WinRT internal cookie processing.
                                    //
                                    // With support for WinRT CookieUsageBehavior, cookie processing is turned off
                                    // within the WinRT layer. This allows us to process cookies using only the .NET
                                    // layer. So, we need to add all applicable cookies that are received to the
                                    // CookieContainer.
                                    if (HttpClientHandler.RTCookieUsageBehaviorSupported || !cookie.HttpOnly)
                                    {
                                        _handler.CookieContainer.Add(uri, cookie);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
        }

        private async Task<RTHttpRequestMessage> ConvertRequestAsync(
            HttpRequestMessage request,
            HttpMethod httpMethod,
            bool skipRequestContentIfPresent)
        {
            RTHttpRequestMessage rtRequest = new RTHttpRequestMessage(
                new RTHttpMethod(httpMethod.Method),
                request.RequestUri);

            // Add a reference from the WinRT object back to the .NET object.
            rtRequest.Properties.Add(RequestMessageLookupKey, request);

            // We can only control the Version on the first request message since the WinRT API
            // has this property designed as a filter/handler property. In addition the overall design
            // of HTTP/2.0 is such that once the first request is using it, all the other requests
            // to the same endpoint will use it as well.
            if (Interlocked.Exchange(ref _filterMaxVersionSet, 1) == 0)
            {
                RTHttpVersion maxVersion;
                if (request.Version == HttpVersionInternal.Version20)
                {
                    maxVersion = RTHttpVersion.Http20;
                }
                else if (request.Version == HttpVersionInternal.Version11)
                {
                    maxVersion = RTHttpVersion.Http11;
                }
                else if (request.Version == HttpVersionInternal.Version10)
                {
                    maxVersion = RTHttpVersion.Http10;
                }
                else
                {
                    maxVersion = RTHttpVersion.Http11;
                }
                
                // The default for WinRT HttpBaseProtocolFilter.MaxVersion is HttpVersion.Http20.
                // So, we only have to change it if we don't want HTTP/2.0.
                if (maxVersion !=  RTHttpVersion.Http20)
                {
                    _filter.MaxVersion = maxVersion;
                }
            }
            
            // Headers
            foreach (KeyValuePair<string, IEnumerable<string>> headerPair in request.Headers)
            {
                foreach (string value in headerPair.Value)
                {
                    bool success = rtRequest.Headers.TryAppendWithoutValidation(headerPair.Key, value);
                    Debug.Assert(success);
                }
            }

            // Cookies
            if (_handler.UseCookies)
            {
                string cookieHeader = _handler.CookieContainer.GetCookieHeader(request.RequestUri);
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                {
                    bool success = rtRequest.Headers.TryAppendWithoutValidation(HttpKnownHeaderNames.Cookie, cookieHeader);
                    Debug.Assert(success);
                }
            }

            // Properties
            foreach (KeyValuePair<string, object> propertyPair in request.Properties)
            {
                rtRequest.Properties.Add(propertyPair.Key, propertyPair.Value);
            }

            // Content
            if (!skipRequestContentIfPresent && request.Content != null)
            {
                rtRequest.Content = await CreateRequestContentAsync(request, rtRequest.Headers).ConfigureAwait(false);
            }

            return rtRequest;
        }

        private static async Task<RTIHttpContent> CreateRequestContentAsync(HttpRequestMessage request, HttpRequestHeaderCollection rtHeaderCollection)
        {
            HttpContent content = request.Content;

            RTIHttpContent rtContent;
            ArraySegment<byte> buffer;

            // If we are buffered already, it is more efficient to send the data directly using the buffer with the
            // WinRT HttpBufferContent class than using HttpStreamContent. This also avoids issues caused by
            // a design limitation in the System.Runtime.WindowsRuntime System.IO.NetFxToWinRtStreamAdapter.
            if (content.TryGetBuffer(out buffer))
            {
                rtContent = new RTHttpBufferContent(buffer.Array.AsBuffer(), (uint)buffer.Offset, (uint)buffer.Count);
            }
            else
            {
                Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
                
                if (contentStream is RTIInputStream)
                {
                    rtContent = new RTHttpStreamContent((RTIInputStream)contentStream);
                }
                else if (contentStream is MemoryStream)
                {
                    var memStream = contentStream as MemoryStream;
                    if (memStream.TryGetBuffer(out buffer))
                    {
                        rtContent = new RTHttpBufferContent(buffer.Array.AsBuffer(), (uint)buffer.Offset, (uint)buffer.Count);
                    }
                    else
                    {
                        byte[] byteArray = memStream.ToArray();
                        rtContent = new RTHttpBufferContent(byteArray.AsBuffer(), 0, (uint) byteArray.Length);
                    }
                }
                else
                {
                    rtContent = new RTHttpStreamContent(contentStream.AsInputStream());
                }
            }

            // RTHttpBufferContent constructor automatically adds a Content-Length header. RTHttpStreamContent does not.
            // Clear any 'Content-Length' header added by the RTHttp*Content objects. We need to clear that now
            // and decide later whether we need 'Content-Length' or 'Transfer-Encoding: chunked' headers based on the
            // .NET HttpRequestMessage and Content header collections.
            rtContent.Headers.ContentLength = null;

            // Deal with conflict between 'Content-Length' vs. 'Transfer-Encoding: chunked' semantics.
            // Desktop System.Net allows both headers to be specified but ends up stripping out
            // 'Content-Length' and using chunked semantics.  The WinRT APIs throw an exception so
            // we need to manually strip out the conflicting header to maintain app compatibility.
            if (request.Headers.TransferEncodingChunked.HasValue && request.Headers.TransferEncodingChunked.Value)
            {
                content.Headers.ContentLength = null;
            }
            else
            {
                // Trigger delayed header generation via TryComputeLength. This code is needed due to an outstanding
                // bug in HttpContentHeaders.ContentLength. See GitHub Issue #5523.
                content.Headers.ContentLength = content.Headers.ContentLength;
            }

            foreach (KeyValuePair<string, IEnumerable<string>> headerPair in content.Headers)
            {
                foreach (string value in headerPair.Value)
                {
                    if (!rtContent.Headers.TryAppendWithoutValidation(headerPair.Key, value))
                    {
                        // rtContent headers are restricted to a white-list of allowed headers, while System.Net.HttpClient's content headers 
                        // will allow custom headers.  If something is not successfully added to the content headers, try adding them to the standard headers.
                        bool success = rtHeaderCollection.TryAppendWithoutValidation(headerPair.Key, value);
                        Debug.Assert(success);
                    }
                }
            }
            return rtContent;
        }

        private static HttpResponseMessage ConvertResponse(RTHttpResponseMessage rtResponse)
        {
            HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)rtResponse.StatusCode);
            response.ReasonPhrase = rtResponse.ReasonPhrase;

            // Version
            if (rtResponse.Version == RTHttpVersion.Http11)
            {
                response.Version = HttpVersionInternal.Version11;
            }
            else if (rtResponse.Version == RTHttpVersion.Http10)
            {
                response.Version = HttpVersionInternal.Version10;
            }
            else if (rtResponse.Version == RTHttpVersion.Http20)
            {
                response.Version = HttpVersionInternal.Version20;
            }
            else
            {
                response.Version = new Version(0,0);
            }

            bool success;

            // Headers
            foreach (KeyValuePair<string, string> headerPair in rtResponse.Headers)
            {
                if (headerPair.Key.Equals(HttpKnownHeaderNames.SetCookie, StringComparison.OrdinalIgnoreCase))
                {
                    // The Set-Cookie header always comes back with all of the cookies concatenated together. 
                    // For example if the response contains the following:
                    //     Set-Cookie A=1
                    //     Set-Cookie B=2
                    // Then we will have a single header KeyValuePair of Key=Set-Cookie, Value=A=1, B=2. 
                    // However clients expect these headers to be separated(i.e. 
                    // httpResponseMessage.Headers.GetValues("Set-Cookie") should return two cookies not one 
                    // concatenated together).
                    success = response.Headers.TryAddWithoutValidation(headerPair.Key, CookieHelper.GetCookiesFromHeader(headerPair.Value));
                }
                else
                {
                    success = response.Headers.TryAddWithoutValidation(headerPair.Key, headerPair.Value);
                }

                Debug.Assert(success);
            }

            // Content
            if (rtResponse.Content != null)
            {
                var rtResponseStream = rtResponse.Content.ReadAsInputStreamAsync().AsTask().Result;
                response.Content = new StreamContent(rtResponseStream.AsStreamForRead());

                foreach (KeyValuePair<string, string> headerPair in rtResponse.Content.Headers)
                {
                    success = response.Content.Headers.TryAddWithoutValidation(headerPair.Key, headerPair.Value);
                    Debug.Assert(success);
                }
            }

            return response;
        }
    }
}
