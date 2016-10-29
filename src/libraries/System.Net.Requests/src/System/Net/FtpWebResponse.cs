// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Net
{
    /// <summary>
    /// <para>The FtpWebResponse class contains the result of the FTP request.
    /// </summary>
    public class FtpWebResponse : WebResponse, IDisposable
    {
        internal Stream _responseStream;
        private long _contentLength;
        private Uri _responseUri;
        private FtpStatusCode _statusCode;
        private string _statusLine;
        private WebHeaderCollection _ftpRequestHeaders;
        private DateTime _lastModified;
        private string _bannerMessage;
        private string _welcomeMessage;
        private string _exitMessage;

        internal FtpWebResponse(Stream responseStream, long contentLength, Uri responseUri, FtpStatusCode statusCode, string statusLine, DateTime lastModified, string bannerMessage, string welcomeMessage, string exitMessage)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, contentLength, statusLine);

            _responseStream = responseStream;
            if (responseStream == null && contentLength < 0)
            {
                contentLength = 0;
            }
            _contentLength = contentLength;
            _responseUri = responseUri;
            _statusCode = statusCode;
            _statusLine = statusLine;
            _lastModified = lastModified;
            _bannerMessage = bannerMessage;
            _welcomeMessage = welcomeMessage;
            _exitMessage = exitMessage;
        }

        internal void UpdateStatus(FtpStatusCode statusCode, string statusLine, string exitMessage)
        {
            _statusCode = statusCode;
            _statusLine = statusLine;
            _exitMessage = exitMessage;
        }

        public override Stream GetResponseStream()
        {
            Stream responseStream = null;

            if (_responseStream != null)
            {
                responseStream = _responseStream;
            }
            else
            {
                responseStream = _responseStream = new EmptyStream();
            }
            return responseStream;
        }

        internal sealed class EmptyStream : MemoryStream
        {
            internal EmptyStream() : base(Array.Empty<byte>(), false)
            {
            }
        }

        internal void SetResponseStream(Stream stream)
        {
            if (stream == null || stream == Stream.Null || stream is EmptyStream)
                return;
            _responseStream = stream;
        }

        /// <summary>
        /// <para>Closes the underlying FTP response stream, but does not close control connection</para>
        /// </summary>
        public override void Close()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            _responseStream?.Close();
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        /// <summary>
        /// <para>Queries the length of the response</para>
        /// </summary>
        public override long ContentLength
        {
            get
            {
                return _contentLength;
            }
        }

        internal void SetContentLength(long value)
        {
            _contentLength = value;
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                if (_ftpRequestHeaders == null)
                {
                    lock (this)
                    {
                        if (_ftpRequestHeaders == null)
                        {
                            _ftpRequestHeaders = new WebHeaderCollection();
                        }
                    }
                }
                return _ftpRequestHeaders;
            }
        }

        public override bool SupportsHeaders
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// <para>Shows the final Uri that the FTP request ended up on</para>
        /// </summary>
        public override Uri ResponseUri
        {
            get
            {
                return _responseUri;
            }
        }

        /// <summary>
        /// <para>Last status code retrived</para>
        /// </summary>
        public FtpStatusCode StatusCode
        {
            get
            {
                return _statusCode;
            }
        }

        /// <summary>
        /// <para>Last status line retrived</para>
        /// </summary>
        public string StatusDescription
        {
            get
            {
                return _statusLine;
            }
        }

        /// <summary>
        /// <para>Returns last modified date time for given file (null if not relavant/avail)</para>
        /// </summary>
        public DateTime LastModified
        {
            get
            {
                return _lastModified;
            }
        }

        /// <summary>
        ///    <para>Returns the server message sent before user credentials are sent</para>
        /// </summary>
        public string BannerMessage
        {
            get
            {
                return _bannerMessage;
            }
        }

        /// <summary>
        ///    <para>Returns the server message sent after user credentials are sent</para>
        /// </summary>
        public string WelcomeMessage
        {
            get
            {
                return _welcomeMessage;
            }
        }

        /// <summary>
        ///    <para>Returns the exit sent message on shutdown</para>
        /// </summary>
        public string ExitMessage
        {
            get
            {
                return _exitMessage;
            }
        }
    }
}

