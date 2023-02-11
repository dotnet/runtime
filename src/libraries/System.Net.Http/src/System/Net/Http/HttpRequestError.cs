// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public enum HttpRequestError
    {
        NameResolutionError,
        SocketError,
        SecureConnectionError,
        HttpProtocolError,

        ResponseEnded,
        InvalidResponse,
        InvalidResponseHeader,
        ContentBufferSizeExceeded,
        ResponseHeaderExceededLengthLimit
    }
}
