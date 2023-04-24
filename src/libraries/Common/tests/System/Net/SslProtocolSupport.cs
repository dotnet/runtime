// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;

namespace System.Net.Test.Common
{
    public class SslProtocolSupport
    {
        public const SslProtocols DefaultSslProtocols =
#if !NETSTANDARD2_0
            SslProtocols.Tls13 |
#endif
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

        public const SslProtocols NonTls13Protocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
#pragma warning restore SYSLIB0039

        public static SslProtocols SupportedSslProtocols
        {
            get
            {
                SslProtocols supported = SslProtocols.None;
#pragma warning disable 0618 // SSL2/3 are deprecated
                if (PlatformDetection.SupportsSsl3)
                {
                    supported |= SslProtocols.Ssl3;
                }
#pragma warning restore 0618
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                if (PlatformDetection.SupportsTls10)
                {
                    supported |= SslProtocols.Tls;
                }

                if (PlatformDetection.SupportsTls11)
                {
                    supported |= SslProtocols.Tls11;
                }
#pragma warning restore SYSLIB0039

                if (PlatformDetection.SupportsTls12)
                {
                    supported |= SslProtocols.Tls12;
                }
#if !NETSTANDARD2_0
                if (PlatformDetection.SupportsTls13)
                {
                    supported |= SslProtocols.Tls13;
                }
#endif
                Debug.Assert(SslProtocols.None != supported);

                return supported;
            }
        }

        public class SupportedSslProtocolsTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                foreach (SslProtocols protocol in Enum.GetValues(typeof(SslProtocols)))
                {
#pragma warning disable 0618 // SSL2/3 are deprecated
                    if (protocol != SslProtocols.None && protocol != SslProtocols.Default && (protocol & SupportedSslProtocols) == protocol)
                    {
                        yield return new object[] { protocol };
                    }
#pragma warning restore 0618
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
