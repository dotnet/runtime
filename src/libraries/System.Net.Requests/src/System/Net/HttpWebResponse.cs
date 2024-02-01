// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///    An HTTP-specific implementation of the
    ///    <see cref='System.Net.WebResponse'/> class.
    /// </para>
    /// </devdoc>
    public class HttpWebResponse : WebResponse, ISerializable
    {
        private HttpResponseMessage _httpResponseMessage = null!;
        private readonly Uri _requestUri;
        private CookieCollection _cookies;
        private WebHeaderCollection? _webHeaderCollection;
        private string? _characterSet;
        private readonly bool _isVersionHttp11 = true;
        private readonly int _maxErrorResponseLength = -1;

        [Obsolete("This API supports the .NET infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HttpWebResponse()
        {
            _requestUri = null!;
            _cookies = null!;
        }

        [Obsolete("Serialization has been deprecated for HttpWebResponse.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected HttpWebResponse(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        [Obsolete("Serialization has been deprecated for HttpWebResponse.")]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        [Obsolete("Serialization has been deprecated for HttpWebResponse.")]
        protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        internal HttpWebResponse(HttpResponseMessage _message, Uri requestUri, CookieContainer? cookieContainer, int maxErrorResponseLength)
        {
            _httpResponseMessage = _message;
            _requestUri = requestUri;
            _maxErrorResponseLength = maxErrorResponseLength;

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
        public override bool IsMutuallyAuthenticated
        {
            get
            {
                return base.IsMutuallyAuthenticated;
            }
        }

        public override long ContentLength
        {
            get
            {
                CheckDisposed();
                return _httpResponseMessage.Content?.Headers.ContentLength ?? -1;
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
                if (_httpResponseMessage.Content != null && _httpResponseMessage.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string>? values))
                {
                    // In most cases, there is only one media type value as per RFC. But for completeness, we
                    // return all values in cases of overly malformed strings.
                    return string.Join(',', values);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string ContentEncoding
        {
            get
            {
                CheckDisposed();
                if (_httpResponseMessage.Content != null)
                {
                    return GetHeaderValueAsString(_httpResponseMessage.Content.Headers.ContentEncoding);
                }

                return string.Empty;
            }
        }

        public virtual CookieCollection Cookies
        {
            get
            {
                CheckDisposed();
                return _cookies;
            }

            set
            {
                CheckDisposed();
                _cookies = value;
            }
        }

        public DateTime LastModified
        {
            get
            {
                CheckDisposed();
                string? lastmodHeaderValue = Headers["Last-Modified"];
                if (string.IsNullOrEmpty(lastmodHeaderValue))
                {
                    return DateTime.Now;
                }

                if (HttpDateParser.TryParse(lastmodHeaderValue, out DateTimeOffset dateTimeOffset))
                {
                    return dateTimeOffset.LocalDateTime;
                }
                else
                {
                    throw new ProtocolViolationException(SR.net_baddate);
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name of the server that sent the response.
        ///    </para>
        /// </devdoc>
        public string Server
        {
            get
            {
                CheckDisposed();
                string? server = Headers["Server"];
                return string.IsNullOrEmpty(server) ? string.Empty : server;
            }
        }

        // HTTP Version
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the version of the HTTP protocol used in the response.
        ///    </para>
        /// </devdoc>
        public Version ProtocolVersion
        {
            get
            {
                CheckDisposed();
                return _isVersionHttp11 ? HttpVersion.Version11 : HttpVersion.Version10;
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
                return _httpResponseMessage.RequestMessage!.Method.Method;
            }
        }

        public override Uri ResponseUri
        {
            get
            {
                CheckDisposed();

                // The underlying System.Net.Http API will automatically update
                // the .RequestUri property to be the final URI of the response.
                return _httpResponseMessage.RequestMessage!.RequestUri!;
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
                return _httpResponseMessage.ReasonPhrase ?? string.Empty;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? CharacterSet
        {
            get
            {
                CheckDisposed();
                string? contentType = Headers["Content-Type"];

                if (_characterSet == null && !string.IsNullOrWhiteSpace(contentType))
                {
                    //sets characterset so the branch is never executed again.
                    _characterSet = string.Empty;

                    //first string is the media type
                    string srchString = contentType.ToLowerInvariant();

                    //media subtypes of text type has a default as specified by rfc 2616
                    if (srchString.AsSpan().Trim().StartsWith("text/", StringComparison.Ordinal))
                    {
                        _characterSet = "ISO-8859-1";
                    }

                    //one of the parameters may be the character set
                    //there must be at least a mediatype for this to be valid
                    int i = srchString.IndexOf(';');
                    if (i > 0)
                    {
                        //search the parameters
                        while ((i = srchString.IndexOf("charset", i, StringComparison.Ordinal)) >= 0)
                        {
                            i += 7;

                            //make sure the word starts with charset
                            if (srchString[i - 8] == ';' || srchString[i - 8] == ' ')
                            {
                                //skip whitespace
                                while (i < srchString.Length && srchString[i] == ' ')
                                    i++;

                                //only process if next character is '='
                                //and there is a character after that
                                if (i < srchString.Length - 1 && srchString[i] == '=')
                                {
                                    i++;

                                    //get and trim character set substring
                                    int j = srchString.IndexOf(';', i);
                                    //In the past we used
                                    //Substring(i, j). J is the offset not the length
                                    //the qfe is to fix the second parameter so that this it is the
                                    //length. since j points to the next ; the operation j -i
                                    //gives the length of the charset
                                    if (j > i)
                                        _characterSet = contentType.AsSpan(i, j - i).Trim().ToString();
                                    else
                                        _characterSet = contentType.AsSpan(i).Trim().ToString();

                                    //done
                                    break;
                                }
                            }
                        }
                    }
                }
                return _characterSet;
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
            if (_httpResponseMessage.Content != null)
            {
                Stream contentStream = _httpResponseMessage.Content.ReadAsStream();
                if (_maxErrorResponseLength == -1 || StatusCode <= (HttpStatusCode)399)
                {
                    return contentStream;
                }

                MemoryStream memoryStream = new MemoryStream();
                byte[] buffer = new byte[1024];
                int readLength = 0;

                while (readLength < _maxErrorResponseLength)
                {
                    int len = contentStream.Read(buffer, 0, Math.Min(_maxErrorResponseLength - readLength, buffer.Length));
                    if (len == 0)
                    {
                        break;
                    }
                    memoryStream.Write(buffer, 0, len);
                    readLength += len;
                }

                return memoryStream;
            }

            return Stream.Null;
        }

        public string GetResponseHeader(string headerName)
        {
            CheckDisposed();
            string? headerValue = Headers[headerName];
            return headerValue ?? string.Empty;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            var httpResponseMessage = _httpResponseMessage;
            if (httpResponseMessage != null)
            {
                httpResponseMessage.Dispose();
                _httpResponseMessage = null!;
            }
        }

        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_httpResponseMessage == null, this);
        }

        private static string GetHeaderValueAsString(IEnumerable<string> values) => string.Join(", ", values);
    }
}
