// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace System.Net.Http
{
    public class HttpRequestMessage : IDisposable
    {
        internal static Version DefaultRequestVersion => HttpVersion.Version11;
        internal static HttpVersionPolicy DefaultVersionPolicy => HttpVersionPolicy.RequestVersionOrLower;

        private const int MessageNotYetSent = 0;
        private const int MessageAlreadySent = 1;

        // Track whether the message has been sent.
        // The message shouldn't be sent again if this field is equal to MessageAlreadySent.
        private int _sendStatus = MessageNotYetSent;

        private HttpMethod _method;
        private Uri? _requestUri;
        private HttpRequestHeaders? _headers;
        private Version _version;
        private HttpVersionPolicy _versionPolicy;
        private HttpContent? _content;
        private bool _disposed;
        private HttpRequestOptions? _options;

        public Version Version
        {
            get { return _version; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                CheckDisposed();

                _version = value;
            }
        }

        /// <summary>
        /// Gets or sets the policy determining how <see cref="Version" /> is interpreted and how is the final HTTP version negotiated with the server.
        /// </summary>
        public HttpVersionPolicy VersionPolicy
        {
            get { return _versionPolicy; }
            set
            {
                CheckDisposed();

                _versionPolicy = value;
            }
        }

        public HttpContent? Content
        {
            get { return _content; }
            set
            {
                CheckDisposed();

                if (NetEventSource.Log.IsEnabled())
                {
                    if (value == null)
                    {
                        NetEventSource.ContentNull(this);
                    }
                    else
                    {
                        NetEventSource.Associate(this, value);
                    }
                }

                // It's OK to set a 'null' content, even if the method is POST/PUT.
                _content = value;
            }
        }

        public HttpMethod Method
        {
            get { return _method; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                CheckDisposed();

                _method = value;
            }
        }

        public Uri? RequestUri
        {
            get { return _requestUri; }
            set
            {
                CheckDisposed();
                _requestUri = value;
            }
        }

        public HttpRequestHeaders Headers => _headers ??= new HttpRequestHeaders();

        internal bool HasHeaders => _headers != null;

        [Obsolete("Use Options instead.")]
        public IDictionary<string, object?> Properties => Options;

        public HttpRequestOptions Options => _options ??= new HttpRequestOptions();

        public HttpRequestMessage()
            : this(HttpMethod.Get, (Uri?)null)
        {
        }

        public HttpRequestMessage(HttpMethod method, Uri? requestUri)
        {
            // It's OK to have a 'null' request Uri. If HttpClient is used, the 'BaseAddress' will be added.
            // If there is no 'BaseAddress', sending this request message will throw.
            // Note that we also allow the string to be empty: null and empty are considered equivalent.
            _method = method ?? throw new ArgumentNullException(nameof(method));
            _requestUri = requestUri;
            _version = DefaultRequestVersion;
            _versionPolicy = DefaultVersionPolicy;
        }

        public HttpRequestMessage(HttpMethod method, string? requestUri)
            : this(method, string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute))
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Method: ");
            sb.Append(_method);

            sb.Append(", RequestUri: '");
            sb.Append(_requestUri == null ? "<null>" : _requestUri.ToString());

            sb.Append("', Version: ");
            sb.Append(_version);

            sb.Append(", Content: ");
            sb.Append(_content == null ? "<null>" : _content.GetType().ToString());

            sb.AppendLine(", Headers:");
            HeaderUtilities.DumpHeaders(sb, _headers, _content?.Headers);

            return sb.ToString();
        }

        internal bool MarkAsSent()
        {
            return Interlocked.Exchange(ref _sendStatus, MessageAlreadySent) == MessageNotYetSent;
        }

        internal bool WasSentByHttpClient() => _sendStatus == MessageAlreadySent;

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            // The reason for this type to implement IDisposable is that it contains instances of types that implement
            // IDisposable (content).
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_content != null)
                {
                    _content.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }
    }
}
