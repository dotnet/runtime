// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public sealed class SocketsHttpConnectionContext
    {
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly HttpRequestMessage _requestMessage;

        internal SocketsHttpConnectionContext(DnsEndPoint dnsEndPoint, HttpRequestMessage requestMessage)
        {
            _dnsEndPoint = dnsEndPoint;
            _requestMessage = requestMessage;
        }

        public DnsEndPoint DnsEndPoint => _dnsEndPoint;

        public HttpRequestMessage RequestMessage => _requestMessage;
    }
}
