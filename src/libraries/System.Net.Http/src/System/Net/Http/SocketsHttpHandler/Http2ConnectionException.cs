// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Net.Http
{
    [Serializable]
    internal sealed class Http2ConnectionException : Http2ProtocolException
    {
        public Http2ConnectionException(Http2ProtocolErrorCode protocolError)
            : base(SR.Format(SR.net_http_http2_connection_error, GetName(protocolError), ((int)protocolError).ToString("x")), protocolError)
        {
        }

        private Http2ConnectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
