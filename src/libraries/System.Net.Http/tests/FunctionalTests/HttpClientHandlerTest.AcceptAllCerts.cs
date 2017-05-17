// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        [Fact]
        public void SingletonReturnsTrue()
        {
            Assert.NotNull(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
            Assert.Same(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator, HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
            Assert.True(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator(null, null, null, SslPolicyErrors.None));
        }
    }
}
