// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
    public class HttpResponseMessage : IDisposable
    {
        private const HttpStatusCode defaultStatusCode = HttpStatusCode.OK;

        private HttpStatusCode _statusCode;
        private HttpResponseHeaders _headers;
        private string _reasonPhrase;
        private HttpRequestMessage _requestMessage;
        private Version _version;
        private HttpContent _content;
        private bool _disposed;

        public Version Version
        {
            get { return _version; }
            set
            {
#if !PHONE
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
#endif
                CheckDisposed();

                _version = value;
            }
        }

        public HttpContent Content
        {
            get { return _content; }
            set
            {
                CheckDisposed();

                if (NetEventSource.IsEnabled)
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
                if (((int)value < 0) || ((int)value > 999))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                CheckDisposed();

                _statusCode = value;
            }
        }

        public string ReasonPhrase
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
                if ((value != null) && ContainsNewLineCharacter(value))
                {
                    throw new FormatException(SR.net_http_reasonphrase_format_error);
                }
                CheckDisposed();

                _reasonPhrase = value; // It's OK to have a 'null' reason phrase.
            }
        }

        public HttpResponseHeaders Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new HttpResponseHeaders();
                }
                return _headers;
            }
        }

        public HttpRequestMessage RequestMessage
        {
            get { return _requestMessage; }
            set
            {
                CheckDisposed();
                if (value != null) NetEventSource.Associate(this, value);
                _requestMessage = value;
            }
        }

        public bool IsSuccessStatusCode
        {
            get { return ((int)_statusCode >= 200) && ((int)_statusCode <= 299); }
        }

        public HttpResponseMessage()
            : this(defaultStatusCode)
        {
        }

        public HttpResponseMessage(HttpStatusCode statusCode)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, statusCode);

            if (((int)statusCode < 0) || ((int)statusCode > 999))
            {
                throw new ArgumentOutOfRangeException(nameof(statusCode));
            }

            _statusCode = statusCode;
            _version = HttpUtilities.DefaultResponseVersion;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public HttpResponseMessage EnsureSuccessStatusCode()
        {
            if (!IsSuccessStatusCode)
            {
                // Disposing the content should help users: If users call EnsureSuccessStatusCode(), an exception is
                // thrown if the response status code is != 2xx. I.e. the behavior is similar to a failed request (e.g.
                // connection failure). Users don't expect to dispose the content in this case: If an exception is 
                // thrown, the object is responsible fore cleaning up its state.
                if (_content != null)
                {
                    _content.Dispose();
                }

                throw new HttpRequestException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_message_not_success_statuscode, (int)_statusCode,
                    ReasonPhrase));
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

            sb.Append(", Headers:\r\n");
            sb.Append(HeaderUtilities.DumpHeaders(_headers, _content == null ? null : _content.Headers));

            return sb.ToString();
        }

        private bool ContainsNewLineCharacter(string value)
        {
            foreach (char character in value)
            {
                if ((character == HttpRuleParser.CR) || (character == HttpRuleParser.LF))
                {
                    return true;
                }
            }
            return false;
        }

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
