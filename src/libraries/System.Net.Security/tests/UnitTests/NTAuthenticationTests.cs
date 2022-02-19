// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public class NTAuthenticationTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/62264", TestPlatforms.Android)]
        public void BasicNtlmTest()
        {
            NetworkCredential credential = new NetworkCredential("rightusername", "rightpassword");

            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(credential);
            NTAuthentication ntAuth = new NTAuthentication(
                isServer: false, "NTLM", credential, "HTTP/foo",
                ContextFlagsPal.Connection | ContextFlagsPal.InitIntegrity, null);

            byte[]? negotiateBlob = ntAuth.GetOutgoingBlob(null, throwOnError: false);
            Assert.NotNull(negotiateBlob);
            byte[]? challengeBlob = fakeNtlmServer.GetOutgoingBlob(negotiateBlob);
            Assert.NotNull(challengeBlob);
            byte[]? authenticateBlob = ntAuth.GetOutgoingBlob(challengeBlob, throwOnError: false);
            Assert.NotNull(authenticateBlob);
            byte[]? empty = fakeNtlmServer.GetOutgoingBlob(authenticateBlob);
            Assert.Null(empty);
        }
    }
}
