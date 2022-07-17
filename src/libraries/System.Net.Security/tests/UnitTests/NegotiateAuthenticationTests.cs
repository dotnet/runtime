// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public class NegotiateAuthenticationTests
    {
        private static bool IsNtlmAvailable => Capability.IsNtlmInstalled() || OperatingSystem.IsAndroid() || OperatingSystem.IsTvOS();
        private static bool IsNtlmUnavailable => !IsNtlmAvailable;

        private static NetworkCredential s_testCredentialRight = new NetworkCredential("rightusername", "rightpassword");
        private static NetworkCredential s_testCredentialWrong = new NetworkCredential("rightusername", "wrongpassword");
        private static readonly byte[] s_Hello = "Hello"u8.ToArray();

        [Fact]
        public void Constructor_Overloads_Validation()
        {
            AssertExtensions.Throws<ArgumentNullException>("clientOptions", () => { new NegotiateAuthentication((NegotiateAuthenticationClientOptions)null); });
            AssertExtensions.Throws<ArgumentNullException>("serverOptions", () => { new NegotiateAuthentication((NegotiateAuthenticationServerOptions)null); });
        }

        [Fact]
        public void RemoteIdentity_ThrowsOnUnauthenticated()
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Credential = s_testCredentialRight, TargetName = "HTTP/foo" };
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            Assert.Throws<InvalidOperationException>(() => negotiateAuthentication.RemoteIdentity);
        }

        [ConditionalFact(nameof(IsNtlmAvailable))]
        public void RemoteIdentity_ThrowsOnDisposed()
        {
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = s_testCredentialRight,
                    TargetName = "HTTP/foo",
                    RequiredProtectionLevel = ProtectionLevel.Sign
                });

            DoNtlmExchange(fakeNtlmServer, negotiateAuthentication);

            Assert.True(fakeNtlmServer.IsAuthenticated);
            Assert.True(negotiateAuthentication.IsAuthenticated);
            IIdentity remoteIdentity = negotiateAuthentication.RemoteIdentity;
            using (remoteIdentity as IDisposable)
            {
                negotiateAuthentication.Dispose();
                Assert.Throws<InvalidOperationException>(() => negotiateAuthentication.RemoteIdentity);
            }
        }

        [Fact]
        public void Package_Unsupported()
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Package = "INVALID", Credential = s_testCredentialRight, TargetName = "HTTP/foo" };
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            NegotiateAuthenticationStatusCode statusCode;
            negotiateAuthentication.GetOutgoingBlob((byte[]?)null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.Unsupported, statusCode);
        }

        [ConditionalFact(nameof(IsNtlmAvailable))]
        public void Package_Supported_NTLM()
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Package = "NTLM", Credential = s_testCredentialRight, TargetName = "HTTP/foo" };
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            NegotiateAuthenticationStatusCode statusCode;
            negotiateAuthentication.GetOutgoingBlob((byte[]?)null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
        }

        [ConditionalFact(nameof(IsNtlmUnavailable))]
        public void Package_Unsupported_NTLM()
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Package = "NTLM", Credential = s_testCredentialRight, TargetName = "HTTP/foo" };
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            NegotiateAuthenticationStatusCode statusCode;
            negotiateAuthentication.GetOutgoingBlob((byte[]?)null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.Unsupported, statusCode);
        }

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

        [ConditionalFact(nameof(IsNtlmAvailable))]
        public void NtlmCorrectExchangeTest()
        {
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NegotiateAuthentication ntAuth = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = s_testCredentialRight,
                    TargetName = "HTTP/foo",
                    RequiredProtectionLevel = ProtectionLevel.Sign
                });

            DoNtlmExchange(fakeNtlmServer, ntAuth);

            Assert.True(fakeNtlmServer.IsAuthenticated);
            // NTLMSSP on Linux doesn't send the MIC and sends incorrect SPN (drops the service prefix)
            if (!OperatingSystem.IsLinux())
            {
                Assert.True(fakeNtlmServer.IsMICPresent);
                Assert.Equal("HTTP/foo", fakeNtlmServer.ClientSpecifiedSpn);
            }
        }

        [ConditionalFact(nameof(IsNtlmAvailable))]
        public void NtlmIncorrectExchangeTest()
        {
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NegotiateAuthentication ntAuth = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = s_testCredentialWrong,
                    TargetName = "HTTP/foo",
                    RequiredProtectionLevel = ProtectionLevel.Sign
                });

            DoNtlmExchange(fakeNtlmServer, ntAuth);

            Assert.False(fakeNtlmServer.IsAuthenticated);
        }

        [ConditionalFact(nameof(IsNtlmAvailable))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65678", TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.MacCatalyst)]
        public void NtlmSignatureTest()
        {
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NegotiateAuthentication ntAuth = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = s_testCredentialRight,
                    TargetName = "HTTP/foo",
                    RequiredProtectionLevel = ProtectionLevel.EncryptAndSign
                });

            DoNtlmExchange(fakeNtlmServer, ntAuth);

            Assert.True(fakeNtlmServer.IsAuthenticated);

            // Test MakeSignature on client side and decoding it on server side
            ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
            NegotiateAuthenticationStatusCode statusCode;
            statusCode = ntAuth.Wrap(s_Hello, output, ntAuth.IsEncrypted, out bool isEncrypted);
            Assert.Equal(16 + s_Hello.Length, output.WrittenCount);
            // Unseal the content and check it
            byte[] temp = new byte[s_Hello.Length];
            fakeNtlmServer.Unwrap(output.WrittenSpan, temp);
            Assert.Equal(s_Hello, temp);

            // Test creating signature on server side and decoding it with VerifySignature on client side 
            byte[] serverSignedMessage = new byte[16 + s_Hello.Length];
            fakeNtlmServer.Wrap(s_Hello, serverSignedMessage);
            output.Clear();
            statusCode = ntAuth.Unwrap(serverSignedMessage, output, out isEncrypted);
            Assert.Equal(NegotiateAuthenticationStatusCode.Completed, statusCode);
            Assert.Equal(s_Hello.Length, output.WrittenCount);
            Assert.Equal(s_Hello, output.WrittenSpan.ToArray());
        }

        private void DoNtlmExchange(FakeNtlmServer fakeNtlmServer, NegotiateAuthentication ntAuth)
        {
            NegotiateAuthenticationStatusCode statusCode;
            byte[]? negotiateBlob = ntAuth.GetOutgoingBlob((byte[])null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
            Assert.NotNull(negotiateBlob);
            byte[]? challengeBlob = fakeNtlmServer.GetOutgoingBlob(negotiateBlob);
            Assert.NotNull(challengeBlob);
            byte[]? authenticateBlob = ntAuth.GetOutgoingBlob(challengeBlob, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.Completed, statusCode);
            Assert.NotNull(authenticateBlob);
            byte[]? empty = fakeNtlmServer.GetOutgoingBlob(authenticateBlob);
            Assert.Null(empty);
        }

        [ConditionalTheory(nameof(IsNtlmAvailable))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void NegotiateCorrectExchangeTest(bool requestMIC, bool requestConfidentiality)
        {
            // Older versions of gss-ntlmssp on Linux generate MIC at incorrect offset unless ForceNegotiateVersion is specified
            FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight) { ForceNegotiateVersion = true };
            FakeNegotiateServer fakeNegotiateServer = new FakeNegotiateServer(fakeNtlmServer) { RequestMIC = requestMIC };
            NegotiateAuthentication ntAuth = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "Negotiate",
                    Credential = s_testCredentialRight,
                    TargetName = "HTTP/foo",
                    RequiredProtectionLevel = requestConfidentiality ? ProtectionLevel.EncryptAndSign : ProtectionLevel.Sign
                });

            byte[]? clientBlob = null;
            byte[]? serverBlob = null;
            NegotiateAuthenticationStatusCode statusCode;
            do
            {
                clientBlob = ntAuth.GetOutgoingBlob(serverBlob, out statusCode);
                if (clientBlob != null)
                {
                    Assert.False(fakeNegotiateServer.IsAuthenticated);
                    // Send the client blob to the fake server
                    serverBlob = fakeNegotiateServer.GetOutgoingBlob(clientBlob);
                }

                if (statusCode == NegotiateAuthenticationStatusCode.Completed)
                {
                    Assert.True(ntAuth.IsAuthenticated);
                    Assert.True(fakeNegotiateServer.IsAuthenticated);
                    Assert.True(fakeNtlmServer.IsAuthenticated);
                }
                else if (statusCode == NegotiateAuthenticationStatusCode.ContinueNeeded)
                {
                    Assert.NotNull(clientBlob);
                    Assert.NotNull(serverBlob);
                }
                else
                {
                    Assert.Fail(statusCode.ToString());
                }
            }
            while (!ntAuth.IsAuthenticated);
        }
    }
}
