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
        private static NetworkCredential s_testCredentialWrong = new NetworkCredential("rightusername", "wrongpassword");
        private static readonly byte[] s_Hello = "Hello"u8.ToArray();

        [Fact]
        public void NtlmProtocolExampleTest()
        {
            // Mirrors the NTLMv2 example in the NTLM specification:
            NetworkCredential credential = new NetworkCredential("User", "Password", "Domain");
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(credential);
            fakeNtlmServer.SendTimestamp = false;
            fakeNtlmServer.TargetIsServer = true;
            fakeNtlmServer.PreferUnicode = false;

            // NEGOTIATE_MESSAGE
            // Flags:
            //   NTLMSSP_NEGOTIATE_KEY_EXCH
            //   NTLMSSP_NEGOTIATE_56
            //   NTLMSSP_NEGOTIATE_128
            //   NTLMSSP_NEGOTIATE_VERSION
            //   NTLMSSP_NEGOTIATE_TARGET_INFO
            //   NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY
            //   NTLMSSP_TARGET_TYPE_SERVER
            //   NTLMSSP_NEGOTIATE_ALWAYS_SIGN
            //   NTLMSSP_NEGOTIATE_NTLM
            //   NTLMSSP_NEGOTIATE_SEAL
            //   NTLMSSP_NEGOTIATE_SIGN
            //   NTLMSSP_NEGOTIATE_OEM
            //   NTLMSSP_NEGOTIATE_UNICODE
            // Domain: (empty) (should be "Domain" but the fake server doesn't check)
            // Workstation: (empty) (should be "COMPUTER" but the fake server doesn't check)
            // Version: 6.1.7600 / 15
            byte[] negotiateBlob = Convert.FromHexString("4e544c4d535350000100000033828ae2000000000000000000000000000000000601b01d0000000f");
            byte[]? challengeBlob = fakeNtlmServer.GetOutgoingBlob(negotiateBlob);

            // CHALLENGE_MESSAGE from 4.2.4.3 Messages
            byte[] expectedChallengeBlob = Convert.FromHexString(
                "4e544c4d53535000020000000c000c003800000033828ae20123456789abcdef" +
                "00000000000000002400240044000000060070170000000f5300650072007600" +
                "6500720002000c0044006f006d00610069006e0001000c005300650072007600" +
                "6500720000000000");
            Assert.Equal(expectedChallengeBlob, challengeBlob);

            // AUTHENTICATE_MESSAGE from 4.2.4.3 Messages
            byte[] authenticateBlob = Convert.FromHexString(
                "4e544c4d5353500003000000180018006c00000054005400840000000c000c00" +
                "480000000800080054000000100010005c00000010001000d8000000358288e2" +
                "0501280a0000000f44006f006d00610069006e00550073006500720043004f00" +
                "4d005000550054004500520086c35097ac9cec102554764a57cccc19aaaaaaaa" +
                "aaaaaaaa68cd0ab851e51c96aabc927bebef6a1c010100000000000000000000" +
                "00000000aaaaaaaaaaaaaaaa0000000002000c0044006f006d00610069006e00" +
                "01000c005300650072007600650072000000000000000000c5dad2544fc97990" +
                "94ce1ce90bc9d03e");
            byte[]? empty = fakeNtlmServer.GetOutgoingBlob(authenticateBlob);
            Assert.Null(empty);
            Assert.True(fakeNtlmServer.IsAuthenticated);
            Assert.False(fakeNtlmServer.IsMICPresent);
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public void NtlmCorrectExchangeTest()
        {
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NTAuthentication ntAuth = new NTAuthentication(
                isServer: false, "NTLM", s_testCredentialRight, "HTTP/foo",
                ContextFlagsPal.Connection | ContextFlagsPal.InitIntegrity, null);

            DoNtlmExchange(fakeNtlmServer, ntAuth);

            Assert.True(fakeNtlmServer.IsAuthenticated);
            // NTLMSSP on Linux doesn't send the MIC and sends incorrect SPN (drops the service prefix)
            if (!OperatingSystem.IsLinux())
            {
                Assert.True(fakeNtlmServer.IsMICPresent);
                Assert.Equal("HTTP/foo", fakeNtlmServer.ClientSpecifiedSpn);
            }
        }

        [ConditionalFact(nameof(IsNtlmInstalled))]
        public void NtlmIncorrectExchangeTest()
        {
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NTAuthentication ntAuth = new NTAuthentication(
                isServer: false, "NTLM", s_testCredentialWrong, "HTTP/foo",
                ContextFlagsPal.Connection | ContextFlagsPal.InitIntegrity, null);

            DoNtlmExchange(fakeNtlmServer, ntAuth);

            Assert.False(fakeNtlmServer.IsAuthenticated);
        }

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

        [ConditionalTheory(nameof(IsNtlmInstalled))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void NegotiateCorrectExchangeTest(bool requestMIC, bool requestConfidentiality)
        {
            // Older versions of gss-ntlmssp on Linux generate MIC at incorrect offset unless ForceNegotiateVersion is specified
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight) { ForceNegotiateVersion = true };
            FakeNegotiateServer fakeNegotiateServer = new FakeNegotiateServer(fakeNtlmServer) { RequestMIC = requestMIC };
            NTAuthentication ntAuth = new NTAuthentication(
                isServer: false, "Negotiate", s_testCredentialRight, "HTTP/foo",
                ContextFlagsPal.Connection | ContextFlagsPal.InitIntegrity |
                (requestConfidentiality ? ContextFlagsPal.Confidentiality : 0), null);

            byte[]? clientBlob = null;
            byte[]? serverBlob = null;
            do
            {
                clientBlob = ntAuth.GetOutgoingBlob(serverBlob, throwOnError: false, out SecurityStatusPal status);
                if (clientBlob != null)
                {
                    Assert.False(fakeNegotiateServer.IsAuthenticated);
                    // Send the client blob to the fake server
                    serverBlob = fakeNegotiateServer.GetOutgoingBlob(clientBlob);
                }

                if (status.ErrorCode == SecurityStatusPalErrorCode.OK)
                {
                    Assert.True(ntAuth.IsCompleted);
                    Assert.True(fakeNegotiateServer.IsAuthenticated);
                    Assert.True(fakeNtlmServer.IsAuthenticated);
                }
                else if (status.ErrorCode == SecurityStatusPalErrorCode.ContinueNeeded)
                {
                    Assert.NotNull(clientBlob);
                    Assert.NotNull(serverBlob);
                }
                else
                {
                    Assert.Fail(status.ErrorCode.ToString());
                }
            }
            while (!ntAuth.IsCompleted);
        }
    }
}
