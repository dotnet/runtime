// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Net.Http
{
    [Serializable]
    internal sealed class Http2StreamException : Http2ProtocolException
    {
        public Http2StreamException(Http2ProtocolErrorCode protocolError)
            : base(SR.Format(SR.net_http_http2_stream_error, GetName(protocolError), ((int)protocolError).ToString("x")), protocolError)
        {
        }

        private Http2StreamException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
