// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    internal sealed partial class SslConnectionInfo
    {
        public SslConnectionInfo(SecPkgContext_ConnectionInfo interopConnectionInfo, TlsCipherSuite cipherSuite)
        {
            Protocol = interopConnectionInfo.Protocol;
            DataCipherAlg = interopConnectionInfo.DataCipherAlg;
            DataKeySize = interopConnectionInfo.DataKeySize;
            DataHashAlg = interopConnectionInfo.DataHashAlg;
            DataHashKeySize = interopConnectionInfo.DataHashKeySize;
            KeyExchangeAlg = interopConnectionInfo.KeyExchangeAlg;
            KeyExchKeySize = interopConnectionInfo.KeyExchKeySize;

            TlsCipherSuite = cipherSuite;
        }
    }
}
