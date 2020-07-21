// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        public static SslProtocols SupportedSslProtocols { get; } = DetermineSupportedProtocols();

        private static SslProtocols DetermineSupportedProtocols()
        {
            SslProtocols supported = 0;

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
            // TLS 1.3 is new
            if (PlatformDetection.SupportsTls13)
            {
                supported |= SslProtocols.Tls13;
            }
#endif
            return supported;
        }

        public class SupportedSslProtocolsTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                foreach (SslProtocols protocol in Enum.GetValues(typeof(SslProtocols)))
                {
                    if (protocol != 0 && (protocol & SupportedSslProtocols) == protocol)
                    {
                        yield return new object[] { protocol };
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
