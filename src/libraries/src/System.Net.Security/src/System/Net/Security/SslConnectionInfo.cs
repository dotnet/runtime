// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Security
{
    internal partial class SslConnectionInfo 
    {
        public int Protocol { get; }
        public TlsCipherSuite TlsCipherSuite { get; private set; }
        public int DataCipherAlg { get; private set; }
        public int DataKeySize { get; private set; }
        public int DataHashAlg { get; private set; }
        public int DataHashKeySize { get; private set; }
        public int KeyExchangeAlg { get; private set; }
        public int KeyExchKeySize { get; private set; }
    }
}
