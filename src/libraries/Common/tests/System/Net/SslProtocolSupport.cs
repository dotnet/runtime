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
            SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

        public const SslProtocols NonTls13Protocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

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
                if (PlatformDetection.SupportsTls10)
                {
                    supported |= SslProtocols.Tls;
                }

                if (PlatformDetection.SupportsTls11)
                {
                    supported |= SslProtocols.Tls11;
                }

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
                    if (protocol != SslProtocols.None && (protocol & SupportedSslProtocols) == protocol)
                    {
                        yield return new object[] { protocol };
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
