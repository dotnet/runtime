// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                MediaTypeHeaderValue contentType = _httpResponseMessage.Content.Headers.ContentType;
                return contentType != null ? contentType.ToString() : string.Empty;
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
            var buffer = new StringBuilder();

            // There is always at least one value even if it is an empty string.
            var enumerator = values.GetEnumerator();
            bool success = enumerator.MoveNext();
            Debug.Assert(success, "There should be at least one value");
            buffer.Append(enumerator.Current);

            // Handle more values if present.
            while (enumerator.MoveNext())
            {
                buffer.Append(", ");
                buffer.Append(enumerator.Current);
            }

            return buffer.ToString();
        }
    }
}
