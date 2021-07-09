// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Net.Http
{
    [Serializable]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    internal sealed class Http3ConnectionException : Http3ProtocolException
    {
        public Http3ConnectionException(Http3ErrorCode errorCode)
            : base(SR.Format(SR.net_http_http3_connection_error, GetName(errorCode), ((long)errorCode).ToString("x")), errorCode)
        {
        }

        private Http3ConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
