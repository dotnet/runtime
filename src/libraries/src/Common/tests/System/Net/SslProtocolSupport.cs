// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Security.Authentication;

namespace System.Net.Test.Common
{
    public class SslProtocolSupport
    {
        public const SslProtocols DefaultSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

        public const SslProtocols SupportedSslProtocols =
            SslProtocols.Tls
            | SslProtocols.Tls11
            | SslProtocols.Tls12;

        public const SslProtocols UnsupportedSslProtocols =
            SslProtocols.Ssl2
            | SslProtocols.Ssl3;

        public class SupportedSslProtocolsTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                foreach (SslProtocols protocol in Enum.GetValues(typeof(SslProtocols)))
                {
                    if ((protocol & SupportedSslProtocols) != 0)
                    {
                        yield return new object[] { protocol };
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class UnsupportedSslProtocolsTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                foreach (SslProtocols protocol in Enum.GetValues(typeof(SslProtocols)))
                {
                    if ((protocol & UnsupportedSslProtocols) != 0)
                    {
                        yield return new object[] { protocol };
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
