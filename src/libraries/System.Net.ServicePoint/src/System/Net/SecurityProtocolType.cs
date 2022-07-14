// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;

namespace System.Net
{
    [Flags]
    public enum SecurityProtocolType
    {
        SystemDefault = 0,
#pragma warning disable CS0618
        [Obsolete("SecurityProtocolType.Ssl3 has been deprecated and is not supported.")]
        Ssl3 = SslProtocols.Ssl3,
#pragma warning restore CS0618
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        Tls = SslProtocols.Tls,
        Tls11 = SslProtocols.Tls11,
#pragma warning restore SYSLIB0039
        Tls12 = SslProtocols.Tls12,
        Tls13 = SslProtocols.Tls13,
    }
}
