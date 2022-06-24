// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Net.Test.Common;
using Xunit;

namespace System.Net.Security.Tests
{
    public class NTAuthenticationTests
    {
        private static bool IsNtlmInstalled => Capability.IsNtlmInstalled();

        private static NetworkCredential s_testCredentialRight = new NetworkCredential("rightusername", "rightpassword");
        private static readonly byte[] s_Hello = "Hello"u8.ToArray();

        [ConditionalFact(nameof(IsNtlmInstalled))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65678", TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.MacCatalyst)]
        public void NtlmSignatureTest()
        {
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NTAuthentication ntAuth = new NTAuthentication(
                isServer: false, "NTLM", s_testCredentialRight, "HTTP/foo",
                ContextFlagsPal.Connection | ContextFlagsPal.InitIntegrity | ContextFlagsPal.Confidentiality, null);

            DoNtlmExchange(fakeNtlmServer, ntAuth);

            Assert.True(fakeNtlmServer.IsAuthenticated);

            // Test MakeSignature on client side and decoding it on server side
            byte[]? output = null;
            int len = ntAuth.MakeSignature(s_Hello, 0, s_Hello.Length, ref output);
            Assert.NotNull(output);
            Assert.Equal(16 + s_Hello.Length, len);
            // Unseal the content and check it
            byte[] temp = new byte[s_Hello.Length];
            fakeNtlmServer.Unseal(output.AsSpan(16), temp);
            Assert.Equal(s_Hello, temp);
            // Check the signature
            fakeNtlmServer.VerifyMIC(temp, output.AsSpan(0, 16), sequenceNumber: 0);

            // Test creating signature on server side and decoding it with VerifySignature on client side 
            byte[] serverSignedMessage = new byte[16 + s_Hello.Length];
            fakeNtlmServer.Seal(s_Hello, serverSignedMessage.AsSpan(16, s_Hello.Length));
            fakeNtlmServer.GetMIC(s_Hello, serverSignedMessage.AsSpan(0, 16), sequenceNumber: 0);
            len = ntAuth.VerifySignature(serverSignedMessage, 0, serverSignedMessage.Length);
            Assert.Equal(s_Hello.Length, len);
            // NOTE: VerifySignature doesn't return the content on Windows
            // Assert.Equal(s_Hello, serverSignedMessage.AsSpan(0, len).ToArray());
        }

        private void DoNtlmExchange(FakeNtlmServer fakeNtlmServer, NTAuthentication ntAuth)
        {
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
