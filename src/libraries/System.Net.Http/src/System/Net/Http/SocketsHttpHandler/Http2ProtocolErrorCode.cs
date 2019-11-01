// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    // NOTE: If any additional error codes are added here, they should also be added to Http2ProtocolException's mapping.

    /// <summary>
    /// Error codes defined by the HTTP/2 protocol, used in RST_STREAM and GOAWAY frames to convey the reasons for the stream or connection error.
    /// https://http2.github.io/http2-spec/#PROTOCOL_ERROR
    /// </summary>
    internal enum Http2ProtocolErrorCode
    {
        /// <summary>The associated condition is not a result of an error.</summary>
        NoError = 0x0,
        /// <summary>The endpoint detected an unspecific protocol error. This error is for use when a more specific error code is not available.</summary>
        ProtocolError = 0x1,
        /// <summary>The endpoint encountered an unexpected internal error.</summary>
        InternalError = 0x2,
        /// <summary>The endpoint detected that its peer violated the flow-control protocol.</summary>
        FlowControlError = 0x3,
        /// <summary>The endpoint sent a SETTINGS frame but did not receive a response in a timely manner.</summary>
        SettingsTimeout = 0x4,
        /// <summary>The endpoint received a frame after a stream was half-closed.</summary>
        StreamClosed = 0x5,
        /// <summary>The endpoint received a frame with an invalid size.</summary>
        FrameSizeError = 0x6,
        /// <summary>The endpoint refused the stream prior to performing any application processing.</summary>
        RefusedStream = 0x7,
        /// <summary>Used by the endpoint to indicate that the stream is no longer needed.</summary>
        Cancel = 0x8,
        /// <summary>The endpoint is unable to maintain the header compression context for the connection.</summary>
        CompressionError = 0x9,
        /// <summary>The connection established in response to a CONNECT request was reset or abnormally closed.</summary>
        ConnectError = 0xa,
        /// <summary>The endpoint detected that its peer is exhibiting a behavior that might be generating excessive load.</summary>
        EnhanceYourCalm = 0xb,
        /// <summary>The underlying transport has properties that do not meet minimum security requirements.</summary>
        InadequateSecurity = 0xc,
        /// <summary>The endpoint requires that HTTP/1.1 be used instead of HTTP/2.</summary>
        Http11Required = 0xd
    }
}
