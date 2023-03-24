// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
    public class HttpResponseMessage : IDisposable
    {
        private const HttpStatusCode DefaultStatusCode = HttpStatusCode.OK;
        private static Version DefaultResponseVersion => HttpVersion.Version11;

        private HttpStatusCode _statusCode;
        private HttpResponseHeaders? _headers;
        private HttpResponseHeaders? _trailingHeaders;
        private string? _reasonPhrase;
        private HttpRequestMessage? _requestMessage;
        private Version _version;
        private HttpContent? _content;
        private bool _disposed;

        public Version Version
        {
            get { return _version; }
            set
            {
#if !PHONE
                ArgumentNullException.ThrowIfNull(value);
#endif
                CheckDisposed();

                _version = value;
            }
        }

        internal void SetVersionWithoutValidation(Version value) => _version = value;

        [AllowNull]
        public HttpContent Content
        {
            get { return _content ??= new EmptyContent(); }
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

                _content = value;
            }
        }

        public HttpStatusCode StatusCode
        {
            get { return _statusCode; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative((int)value, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan((int)value, 999, nameof(value));
                CheckDisposed();

                _statusCode = value;
            }
        }

        internal void SetStatusCodeWithoutValidation(HttpStatusCode value) => _statusCode = value;

        public string? ReasonPhrase
        {
            get
            {
                if (_reasonPhrase != null)
                {
                    return _reasonPhrase;
                }
                // Provide a default if one was not set.
                return HttpStatusDescription.Get(StatusCode);
            }
            set
            {
                if ((value != null) && HttpRuleParser.ContainsNewLine(value))
                {
                    throw new FormatException(SR.net_http_reasonphrase_format_error);
                }
                CheckDisposed();

                _reasonPhrase = value; // It's OK to have a 'null' reason phrase.
            }
        }

        internal void SetReasonPhraseWithoutValidation(string value) => _reasonPhrase = value;

        public HttpResponseHeaders Headers => _headers ??= new HttpResponseHeaders();

        public HttpResponseHeaders TrailingHeaders => _trailingHeaders ??= new HttpResponseHeaders(containsTrailingHeaders: true);

        /// <summary>Stores the supplied trailing headers into this instance.</summary>
        /// <remarks>
        /// In the common/desired case where response.TrailingHeaders isn't accessed until after the whole payload has been
        /// received, <see cref="_trailingHeaders" /> will still be null, and we can simply store the supplied instance into
        /// <see cref="_trailingHeaders" /> and assume ownership of the instance.  In the uncommon case where it was accessed,
        /// we add all of the headers to the existing instance.
        /// </remarks>
        internal void StoreReceivedTrailingHeaders(HttpResponseHeaders headers)
        {
            Debug.Assert(headers.ContainsTrailingHeaders);

            if (_trailingHeaders is null)
            {
                _trailingHeaders = headers;
            }
            else
            {
                _trailingHeaders.AddHeaders(headers);
            }
        }

        public HttpRequestMessage? RequestMessage
        {
            get { return _requestMessage; }
            set
            {
                CheckDisposed();
                if (value is not null && NetEventSource.Log.IsEnabled())
                    NetEventSource.Associate(this, value);
                _requestMessage = value;
            }
        }

        public bool IsSuccessStatusCode
        {
            get { return ((int)_statusCode >= 200) && ((int)_statusCode <= 299); }
        }

        public HttpResponseMessage()
            : this(DefaultStatusCode)
        {
        }

        public HttpResponseMessage(HttpStatusCode statusCode)
        {
            ArgumentOutOfRangeException.ThrowIfNegative((int)statusCode, nameof(statusCode));
            ArgumentOutOfRangeException.ThrowIfGreaterThan((int)statusCode, 999, nameof(statusCode));

            _statusCode = statusCode;
            _version = DefaultResponseVersion;
        }

        public HttpResponseMessage EnsureSuccessStatusCode()
        {
            if (!IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    SR.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        string.IsNullOrWhiteSpace(ReasonPhrase) ? SR.net_http_message_not_success_statuscode : SR.net_http_message_not_success_statuscode_reason,
                        (int)_statusCode,
                        ReasonPhrase),
                    inner: null,
                    _statusCode);
            }

            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("StatusCode: ");
            sb.Append((int)_statusCode);

            sb.Append(", ReasonPhrase: '");
            sb.Append(ReasonPhrase ?? "<null>");

            sb.Append("', Version: ");
            sb.Append(_version);

            sb.Append(", Content: ");
            sb.Append(_content == null ? "<null>" : _content.GetType().ToString());

            sb.AppendLine(", Headers:");
            HeaderUtilities.DumpHeaders(sb, _headers, _content?.Headers);

            if (_trailingHeaders != null)
            {
                sb.AppendLine(", Trailing Headers:");
                HeaderUtilities.DumpHeaders(sb, _trailingHeaders);
            }

            return sb.ToString();
        }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            // The reason for this type to implement IDisposable is that it contains instances of types that implement
            // IDisposable (content).
            if (disposing && !_disposed)
            {
                _disposed = true;
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
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
