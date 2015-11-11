// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public static class CertificateChainValidation
    {
        public static void Validate(X509Certificate2Collection expected, X509Chain actual)
        {
            ITestOutputHelper log = TestLogging.GetInstance();
            log.WriteLine("CertificateChainValidation()");

            var expectedIndexed = new Dictionary<string, X509Certificate>();
            var actualIndexed = new Dictionary<string, X509Certificate>();

            Assert.Equal(expected.Count, actual.ChainElements.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                expectedIndexed.Add(expected[i].Thumbprint, expected[i]);
                actualIndexed.Add(actual.ChainElements[i].Certificate.Thumbprint, actual.ChainElements[i].Certificate);
            }

            foreach (string thumbprint in expectedIndexed.Keys)
            {
                Assert.Equal(expectedIndexed[thumbprint], actualIndexed[thumbprint]);
            }
        }
    }
}
