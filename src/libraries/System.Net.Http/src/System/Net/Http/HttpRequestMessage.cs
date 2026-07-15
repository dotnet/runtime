// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
    public class HttpRequestMessage : IDisposable
    {
        internal static Version DefaultRequestVersion => HttpVersion.Version11;
        internal static HttpVersionPolicy DefaultVersionPolicy => HttpVersionPolicy.RequestVersionOrLower;

        [Flags]
        private enum MessageFlags
        {
            AlreadySent = 1,
            PropagatorStateInjectedByDiagnosticsHandler = 2,
            Disposed = 4,
            AuthDisabled = 8,
            ConnectionIdSet = 16,
        }

        private MessageFlags _flags;

        private long _connectionId;

        private HttpMethod _method;
        private Uri? _requestUri;
        private HttpRequestHeaders? _headers;
        private Version _version;
        private HttpVersionPolicy _versionPolicy;
        private HttpContent? _content;
        internal HttpRequestOptions? _options;

        public Version Version
        {
            get { return _version; }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
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
                if ((uint)value > (uint)HttpVersionPolicy.RequestVersionExact)
                {
                    throw new ArgumentException(SR.Format(SR.net_invalid_enum, nameof(HttpVersionPolicy)), nameof(value));
                }

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
                ArgumentNullException.ThrowIfNull(value);
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

        [Obsolete("HttpRequestMessage.Properties has been deprecated. Use Options instead.")]
        public IDictionary<string, object?> Properties => Options;

        /// <summary>
        /// Gets the collection of options to configure the HTTP request.
        /// </summary>
        public HttpRequestOptions Options => _options ??= new HttpRequestOptions();

        /// <summary>
        /// Gets or sets the identifier of the connection that this request was most recently sent on. The value is not
        /// guaranteed to be set: it remains <see langword="null"/> when the request was not handled by a connection, for
        /// example because it timed out before a connection could be obtained.
        /// </summary>
        /// <remarks>
        /// When the request is sent through a <see cref="SocketsHttpHandler"/>, the value matches the connection id
        /// reported through EventSource telemetry and the id passed to
        /// <see cref="SocketsHttpHandler.ShouldEvictConnection"/> for the connection that served the request, allowing
        /// a caller to correlate a request with that connection. It also matches the id surfaced to a custom
        /// <see cref="SocketsHttpHandler.ConnectCallback"/>, except when the request is served over an HTTP CONNECT
        /// proxy tunnel: there the callback observes the tunnel's underlying transport connection to the proxy, whose
        /// id differs from this one (which identifies the tunneled connection that carried the request). Both ids remain
        /// observable through a <see cref="SocketsHttpHandler.PlaintextStreamFilter"/>, which runs once per hop and
        /// reports the transport connection's id for the CONNECT hop and this id for the tunneled hop. When a request
        /// is sent over multiple connections (for example after a redirect or a retry), the value reflects the most
        /// recent attempt.
        /// <para>
        /// These correlations apply only when the request is handled by <see cref="SocketsHttpHandler"/>. Another
        /// <see cref="HttpMessageHandler"/> may never set this value, or may assign it a different meaning.
        /// </para>
        /// <para>
        /// This property is intended to be read after the request has been sent. Assigning a value before the request
        /// is sent has no effect on how the request is handled: it does not request or influence the use of a particular
        /// connection, and any value set by the caller is overwritten with the id of the connection that actually serves
        /// the request.
        /// </para>
        /// </remarks>
        [Experimental(Experimentals.SocketsHttpHandlerExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public long? ConnectionId
        {
            // ConnectionIdSet is stored separately to avoid the extra bytes needed for a nullable 'long?' field.
            get => _flags.HasFlag(MessageFlags.ConnectionIdSet) ? _connectionId : null;
            set
            {
                if (value is null)
                {
                    _flags &= ~MessageFlags.ConnectionIdSet;
                }
                else
                {
                    _connectionId = value.Value;
                    _flags |= MessageFlags.ConnectionIdSet;
                }
            }
        }

        public HttpRequestMessage()
            : this(HttpMethod.Get, (Uri?)null)
        {
        }

        public HttpRequestMessage(HttpMethod method, Uri? requestUri)
        {
            ArgumentNullException.ThrowIfNull(method);

            // It's OK to have a 'null' request Uri. If HttpClient is used, the 'BaseAddress' will be added.
            // If there is no 'BaseAddress', sending this request message will throw.
            // Note that we also allow the string to be empty: null and empty are considered equivalent.
            _method = method;
            _requestUri = requestUri;
            _version = DefaultRequestVersion;
            _versionPolicy = DefaultVersionPolicy;
        }

        public HttpRequestMessage(HttpMethod method, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
            : this(method, string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute))
        {
        }

        public override string ToString()
        {
            ValueStringBuilder sb = new ValueStringBuilder(stackalloc char[512]);

            sb.Append("Method: ");
            sb.Append(_method.ToString());

            sb.Append(", RequestUri: '");
            if (_requestUri is null)
            {
                sb.Append("<null>");
            }
            else
            {
                sb.AppendSpanFormattable(_requestUri);
            }

            sb.Append("', Version: ");
            sb.AppendSpanFormattable(_version);

            sb.Append(", Content: ");
            sb.Append(_content == null ? "<null>" : _content.GetType().ToString());

            sb.Append(", Headers:");
            sb.Append(Environment.NewLine);
            HeaderUtilities.DumpHeaders(ref sb, _headers, _content?.Headers);

            return sb.ToString();
        }

        internal bool MarkAsSent()
        {
            MessageFlags previousFlags = _flags;
            _flags = previousFlags | MessageFlags.AlreadySent;
            return !previousFlags.HasFlag(MessageFlags.AlreadySent);
        }

        internal bool WasSentByHttpClient() => _flags.HasFlag(MessageFlags.AlreadySent);

        internal void MarkPropagatorStateInjectedByDiagnosticsHandler() => _flags |= MessageFlags.PropagatorStateInjectedByDiagnosticsHandler;

        internal bool WasPropagatorStateInjectedByDiagnosticsHandler() => _flags.HasFlag(MessageFlags.PropagatorStateInjectedByDiagnosticsHandler);

        internal void DisableAuth() => _flags |= MessageFlags.AuthDisabled;

        internal bool IsAuthDisabled() => _flags.HasFlag(MessageFlags.AuthDisabled);

        private bool Disposed
        {
            get => _flags.HasFlag(MessageFlags.Disposed);
            set
            {
                Debug.Assert(value);
                _flags |= MessageFlags.Disposed;
            }
        }

        internal bool IsExtendedConnectRequest => Method == HttpMethod.Connect && _headers?.Protocol != null;

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            // The reason for this type to implement IDisposable is that it contains instances of types that implement
            // IDisposable (content).
            if (disposing && !Disposed)
            {
                Disposed = true;
                _content?.Dispose();
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
            ObjectDisposedException.ThrowIf(Disposed, this);
        }
    }
}
