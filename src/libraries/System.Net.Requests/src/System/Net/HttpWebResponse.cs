// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///    An HTTP-specific implementation of the
    ///    <see cref='System.Net.WebResponse'/> class.
    /// </para>
    /// </devdoc>
    public class HttpWebResponse : WebResponse
    {
        private HttpResponseMessage _httpResponseMessage;
        private Uri _requestUri;
        private CookieCollection _cookies;
        private WebHeaderCollection _webHeaderCollection = null;

        internal HttpWebResponse(HttpResponseMessage _message, Uri requestUri, CookieContainer cookieContainer)
        {
            _httpResponseMessage = _message;
            _requestUri = requestUri;

            // Match Desktop behavior. If the request didn't set a CookieContainer, we don't populate the response's CookieCollection.
            if (cookieContainer != null)
            {
                _cookies = cookieContainer.GetCookies(requestUri);
            }
            else
            {
                _cookies = new CookieCollection();
            }
        }

        public override long ContentLength
        {
            get
            {
                CheckDisposed();
                long? length = _httpResponseMessage.Content.Headers.ContentLength;
                return length.HasValue ? length.Value : -1;
            }
        }

        public override string ContentType
        {
            get
            {
                CheckDisposed();

                // We use TryGetValues() instead of the strongly type Headers.ContentType property so that
                // we return a string regardless of it being fully RFC conformant. This matches current
                // .NET Framework behavior.
                IEnumerable<string> values;
                if (_httpResponseMessage.Content.Headers.TryGetValues("Content-Type", out values))
                {
                    // In most cases, there is only one media type value as per RFC. But for completeness, we
                    // return all values in cases of overly malformed strings.
                    var builder = new StringBuilder();
                    int ndx = 0;
                    foreach (string value in values)
                    {
                        if (ndx > 0)
                        {
                            builder.Append(',');
                        }
                        
                        builder.Append(value);
                        ndx++;
                    }

                    return builder.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public virtual CookieCollection Cookies
        {
            get
            {
                CheckDisposed();
                return _cookies;
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                CheckDisposed();
                if (_webHeaderCollection == null)
                {
                    _webHeaderCollection = new WebHeaderCollection();

                    foreach (var header in _httpResponseMessage.Headers)
                    {
                        _webHeaderCollection[header.Key] = GetHeaderValueAsString(header.Value);
                    }

                    if (_httpResponseMessage.Content != null)
                    {
                        foreach (var header in _httpResponseMessage.Content.Headers)
                        {
                            _webHeaderCollection[header.Key] = GetHeaderValueAsString(header.Value);
                        }
                    }
                }
                return _webHeaderCollection;
            }
        }

        public virtual string Method
        {
            get
            {
                CheckDisposed();
                return _httpResponseMessage.RequestMessage.Method.Method;
            }
        }

        public override Uri ResponseUri
        {
            get
            {
                CheckDisposed();

                // The underlying System.Net.Http API will automatically update
                // the .RequestUri property to be the final URI of the response.
                return _httpResponseMessage.RequestMessage.RequestUri;
            }
        }

        public virtual HttpStatusCode StatusCode
        {
            get
            {
                CheckDisposed();
                return _httpResponseMessage.StatusCode;
            }
        }

        public virtual string StatusDescription
        {
            get
            {
                CheckDisposed();
                return _httpResponseMessage.ReasonPhrase;
            }
        }

        public override bool SupportsHeaders
        {
            get
            {
                return true;
            }
        }

        public override Stream GetResponseStream()
        {
            CheckDisposed();
            return _httpResponseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }

        protected override void Dispose(bool disposing)
        {
            var httpResponseMessage = _httpResponseMessage;
            if (httpResponseMessage != null)
            {
                httpResponseMessage.Dispose();
                _httpResponseMessage = null;
            }
        }

        private void CheckDisposed()
        {
            if (_httpResponseMessage == null)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }

        private string GetHeaderValueAsString(IEnumerable<string> values)
        {
            // There is always at least one value even if it is an empty string.
            var enumerator = values.GetEnumerator();
            bool success = enumerator.MoveNext();
            Debug.Assert(success, "There should be at least one value");

            string headerValue = enumerator.Current;

            if (enumerator.MoveNext())
            {
                // Multi-valued header
                var buffer = new StringBuilder(headerValue);

                do
                {
                    buffer.Append(", ");
                    buffer.Append(enumerator.Current);
                } while (enumerator.MoveNext());

                return buffer.ToString();
            }

            return headerValue;
        }
    }
}
