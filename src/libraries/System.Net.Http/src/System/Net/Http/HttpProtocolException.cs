// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Http
{
    public partial class HttpProtocolException : IOException
    {
        public HttpProtocolException(string? message, long errorCode, Exception? innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        private HttpProtocolException(string message, long errorCode)
            : this(message, errorCode, null)
        {
        }

        public long ErrorCode { get; }

        internal static HttpProtocolException CreateHttp2StreamException(Http2ProtocolErrorCode protocolError)
        {
            string message = SR.Format(SR.net_http_http2_stream_error, GetName(protocolError), ((int)protocolError).ToString("x"));
            return new HttpProtocolException(message, (long)protocolError);
        }

        internal static HttpProtocolException CreateHttp2ConnectionException(Http2ProtocolErrorCode protocolError)
        {
            string message = SR.Format(SR.net_http_http2_connection_error, GetName(protocolError), ((int)protocolError).ToString("x"));
            return new HttpProtocolException(message, (long)protocolError);
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
