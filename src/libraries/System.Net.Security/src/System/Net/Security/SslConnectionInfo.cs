// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    internal partial struct SslConnectionInfo
    {
#pragma warning disable CA1823
        private static readonly byte[] s_http1 = SslApplicationProtocol.Http11.Protocol.ToArray();
        private static readonly byte[] s_http2 = SslApplicationProtocol.Http2.Protocol.ToArray();
        private static readonly byte[] s_http3 = SslApplicationProtocol.Http3.Protocol.ToArray();
#pragma warning restore CA1823

        public int Protocol { get; private set; }
        public TlsCipherSuite TlsCipherSuite { get; private set; }
        public int DataCipherAlg { get; private set; }
        public int DataKeySize { get; private set; }
        public int DataHashAlg { get; private set; }
        public int DataHashKeySize { get; private set; }
        public int KeyExchangeAlg { get; private set; }
        public int KeyExchKeySize { get; private set; }

        public byte[]? ApplicationProtocol { get; internal set; }
    }
}
