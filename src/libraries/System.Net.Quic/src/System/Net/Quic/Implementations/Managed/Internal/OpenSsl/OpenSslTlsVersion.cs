// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    internal enum OpenSslTlsVersion : short
    {
        Tls1 = 0x0301,
        Tls11 = 0x0302,
        Tls12 = 0x0303,
        Tls13 = 0x0304
    }
}
