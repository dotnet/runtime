// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Http
{
    /// <summary>
    /// The exception thrown when an HTTP/2 or an HTTP/3 protocol error occurs.
    /// </summary>
    /// <remarks>
    /// When calling <see cref="HttpClient"/> or <see cref="SocketsHttpHandler"/> methods, <see cref="HttpProtocolException"/> will be the inner exception of
    /// <see cref="HttpRequestException"/> if a protocol error occurs.
    /// When calling <see cref="Stream"/> methods on the stream returned by <see cref="HttpContent.ReadAsStream()"/> or
    /// <see cref="HttpContent.ReadAsStreamAsync(Threading.CancellationToken)"/>, <see cref="HttpProtocolException"/> can be thrown directly.
    /// </remarks>
    public sealed class HttpProtocolException : IOException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpProtocolException"/> class with the specified error code,
        /// message, and inner exception.
        /// </summary>
        /// <param name="errorCode">The HTTP/2 or HTTP/3 error code.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public HttpProtocolException(long errorCode, string message, Exception? innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Gets the HTTP/2 or HTTP/3 error code associated with this exception.
        /// </summary>
        public long ErrorCode { get; }

        internal static HttpProtocolException CreateHttp2StreamException(Http2ProtocolErrorCode protocolError)
        {
            string message = SR.Format(SR.net_http_http2_stream_error, GetName(protocolError), ((int)protocolError).ToString("x"));
            return new HttpProtocolException((long)protocolError, message, null);
        }

        internal static HttpProtocolException CreateHttp2ConnectionException(Http2ProtocolErrorCode protocolError)
        {
            string message = SR.Format(SR.net_http_http2_connection_error, GetName(protocolError), ((int)protocolError).ToString("x"));
            return new HttpProtocolException((long)protocolError, message, null);
        }

        private static string GetName(Http2ProtocolErrorCode code) =>
            // These strings are the names used in the HTTP2 spec and should not be localized.
            code switch
            {
                Http2ProtocolErrorCode.NoError => "NO_ERROR",
                Http2ProtocolErrorCode.ProtocolError => "PROTOCOL_ERROR",
                Http2ProtocolErrorCode.InternalError => "INTERNAL_ERROR",
                Http2ProtocolErrorCode.FlowControlError => "FLOW_CONTROL_ERROR",
                Http2ProtocolErrorCode.SettingsTimeout => "SETTINGS_TIMEOUT",
                Http2ProtocolErrorCode.StreamClosed => "STREAM_CLOSED",
                Http2ProtocolErrorCode.FrameSizeError => "FRAME_SIZE_ERROR",
                Http2ProtocolErrorCode.RefusedStream => "REFUSED_STREAM",
                Http2ProtocolErrorCode.Cancel => "CANCEL",
                Http2ProtocolErrorCode.CompressionError => "COMPRESSION_ERROR",
                Http2ProtocolErrorCode.ConnectError => "CONNECT_ERROR",
                Http2ProtocolErrorCode.EnhanceYourCalm => "ENHANCE_YOUR_CALM",
                Http2ProtocolErrorCode.InadequateSecurity => "INADEQUATE_SECURITY",
                Http2ProtocolErrorCode.Http11Required => "HTTP_1_1_REQUIRED",
                _ => "(unknown error)",
            };
    }
}
