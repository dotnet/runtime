// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/123472", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot), nameof(PlatformDetection.IsLinux))]
    public class NegotiateAuthenticationTests
    {
        // Ubuntu 24 and 26 ship with broekn gss-ntlmssp 1.2
        // RHEL 8 ships gss-ntlmssp 1.2 built against OpenSSL 1.1 which produces broken NTLM responses
        private static bool UseManagedNtlm => PlatformDetection.IsUbuntu24 || PlatformDetection.IsUbuntu26 || PlatformDetection.IsOpenSUSE16 || (PlatformDetection.IsRedHatFamily && !PlatformDetection.IsOpenSsl3);
        private static bool IsNtlmAvailable => UseManagedNtlm || Capability.IsNtlmInstalled() || OperatingSystem.IsAndroid() || OperatingSystem.IsTvOS();
        private static bool IsNtlmUnavailable => !IsNtlmAvailable;

        private static NetworkCredential s_testCredentialRight = new NetworkCredential("rightusername", "rightpassword");
        private static NetworkCredential s_testCredentialWrong = new NetworkCredential("rightusername", "wrongpassword");
        private static readonly byte[] s_Hello = "Hello"u8.ToArray();

        static NegotiateAuthenticationTests()
        {
            if (UseManagedNtlm)
            {
                AppContext.SetSwitch("System.Net.Security.UseManagedNtlm", true);
            }
        }

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

        [ConditionalFact(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        public void RemoteIdentity_ThrowsOnDisposed()
        {
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
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

        [Theory]
        [InlineData("!!!!")]
        [InlineData("AAA")]
        [InlineData("AAAAA")]
        [InlineData("AA=A")]
        public void GetOutgoingBlob_InvalidBase64_ReturnsInvalidToken(string incomingBlob)
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Package = "Negotiate", Credential = s_testCredentialRight, TargetName = "HTTP/foo" };
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            string? outgoingBlob = negotiateAuthentication.GetOutgoingBlob(incomingBlob, out NegotiateAuthenticationStatusCode statusCode);
            Assert.Null(outgoingBlob);
            Assert.Equal(NegotiateAuthenticationStatusCode.InvalidToken, statusCode);
        }

        [ConditionalFact(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        public void Package_Supported_NTLM()
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Package = "NTLM", Credential = s_testCredentialRight, TargetName = "HTTP/foo" };
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            NegotiateAuthenticationStatusCode statusCode;
            negotiateAuthentication.GetOutgoingBlob((byte[]?)null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
        }

        [ConditionalFact(typeof(NegotiateAuthenticationTests), nameof(IsNtlmUnavailable))]
        public void Package_Unsupported_NTLM()
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Package = "NTLM", Credential = s_testCredentialRight, TargetName = "HTTP/foo" };
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            NegotiateAuthenticationStatusCode statusCode;
            negotiateAuthentication.GetOutgoingBlob((byte[]?)null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.Unsupported, statusCode);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "The test is specific to GSSAPI / Managed implementations of NegotiateAuthentication")]
        public void DefaultNetworkCredentials_NTLM_DoesNotThrow()
        {
            NegotiateAuthenticationClientOptions clientOptions = new NegotiateAuthenticationClientOptions { Package = "NTLM", Credential = CredentialCache.DefaultNetworkCredentials, TargetName = "HTTP/foo" };
            // Assert.DoesNotThrow
            NegotiateAuthentication negotiateAuthentication = new NegotiateAuthentication(clientOptions);
            NegotiateAuthenticationStatusCode statusCode;
            negotiateAuthentication.GetOutgoingBlob((byte[]?)null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.UnknownCredentials, statusCode);
        }

        [Fact]
        public void NtlmProtocolExampleTest()
        {
            // Mirrors the NTLMv2 example in the NTLM specification:
            NetworkCredential credential = new NetworkCredential("User", "Password", "Domain");
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(credential);
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

        public static IEnumerable<object[]> TestCredentials()
        {
            yield return new object[] { new NetworkCredential("rightusername", "rightpassword") };
            yield return new object[] { new NetworkCredential("rightusername", "rightpassword", "rightdomain") };
            yield return new object[] { new NetworkCredential("rightusername@rightdomain.com", "rightpassword") };
        }

        [ConditionalTheory(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        [MemberData(nameof(TestCredentials))]
        public void NtlmCorrectExchangeTest(NetworkCredential credential)
        {
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(credential);
            NegotiateAuthentication ntAuth = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = credential,
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

        [ConditionalFact(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        public void NtlmIncorrectExchangeTest()
        {
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
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

        [ConditionalFact(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        public void NtlmEncryptionTest()
        {
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);

            NegotiateAuthentication ntAuth = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = s_testCredentialRight,
                    TargetName = "HTTP/foo",
                    RequiredProtectionLevel = ProtectionLevel.EncryptAndSign
                });

            NegotiateAuthenticationStatusCode statusCode;
            byte[]? negotiateBlob = ntAuth.GetOutgoingBlob((byte[])null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
            Assert.NotNull(negotiateBlob);

            byte[]? challengeBlob = fakeNtlmServer.GetOutgoingBlob(negotiateBlob);
            Assert.NotNull(challengeBlob);
            // Validate that the client sent NegotiateSeal flag
            Assert.Equal(FakeNtlmServer.Flags.NegotiateSeal, (fakeNtlmServer.InitialClientFlags & FakeNtlmServer.Flags.NegotiateSeal));

            byte[]? authenticateBlob = ntAuth.GetOutgoingBlob(challengeBlob, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.Completed, statusCode);
            Assert.NotNull(authenticateBlob);

            byte[]? empty = fakeNtlmServer.GetOutgoingBlob(authenticateBlob);
            Assert.Null(empty);
            Assert.True(fakeNtlmServer.IsAuthenticated);

            // Validate that the NegotiateSeal flag survived the full exchange
            Assert.Equal(FakeNtlmServer.Flags.NegotiateSeal, (fakeNtlmServer.NegotiatedFlags & FakeNtlmServer.Flags.NegotiateSeal));
        }

        [ConditionalFact(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        public void NtlmSignatureTest()
        {
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
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

        [ConditionalFact(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        public void NtlmIntegrityCheckTest()
        {
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
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

            ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
            for (int i = 0; i < 3; i++)
            {
                // Test ComputeIntegrityCheck on client side and decoding it on server side
                ntAuth.ComputeIntegrityCheck(s_Hello, output);
                Assert.Equal(16, output.WrittenCount);
                // Verify the signature computation
                fakeNtlmServer.VerifyMIC(s_Hello, output.WrittenSpan);
                // Prepare buffer for reuse
                output.Clear();
            }

            Span<byte> signature = stackalloc byte[16];
            for (int i = 0; i < 3; i++)
            {
                fakeNtlmServer.GetMIC(s_Hello, signature);
                Assert.True(ntAuth.VerifyIntegrityCheck(s_Hello, signature));
            }
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

        [ConditionalTheory(typeof(NegotiateAuthenticationTests), nameof(UseManagedNtlm))]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void NtlmWithPreExistingTargetInfoEntriesTest(bool sendPreExistingTargetName, bool sendPreExistingChannelBindings)
        {
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight)
            {
                SendPreExistingTargetName = sendPreExistingTargetName,
                SendPreExistingChannelBindings = sendPreExistingChannelBindings,
            };
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
        }

        [ConditionalTheory(typeof(NegotiateAuthenticationTests), nameof(IsNtlmAvailable))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void NegotiateCorrectExchangeTest(bool requestMIC, bool requestConfidentiality)
        {
            // Older versions of gss-ntlmssp on Linux generate MIC at incorrect offset unless ForceNegotiateVersion is specified
            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight) { ForceNegotiateVersion = true };
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

        public static IEnumerable<object[]> MalformedChallengeBlobs()
        {
            // Truncated below sizeof(ChallengeMessage). The fixed header is 56 bytes.
            yield return new object[] { "TooShort", (Func<byte[], byte[]>)(blob => blob.AsSpan(0, 40).ToArray()) };

            // TargetInfo payload offset points past the end of the blob.
            yield return new object[] { "TargetInfoOffsetOutOfRange", (Func<byte[], byte[]>)(blob =>
            {
                byte[] copy = (byte[])blob.Clone();
                BinaryPrimitives.WriteUInt32LittleEndian(copy.AsSpan(44), (uint)(copy.Length + 100));
                return copy;
            }) };

            // TargetInfo length extends past the end of the blob.
            yield return new object[] { "TargetInfoLengthOutOfRange", (Func<byte[], byte[]>)(blob =>
            {
                byte[] copy = (byte[])blob.Clone();
                BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(40), ushort.MaxValue);
                BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(42), ushort.MaxValue);
                return copy;
            }) };

            // TargetInfo payload offset is negative (would underflow without the offset < 0 guard).
            yield return new object[] { "TargetInfoOffsetNegative", (Func<byte[], byte[]>)(blob =>
            {
                byte[] copy = (byte[])blob.Clone();
                BinaryPrimitives.WriteInt32LittleEndian(copy.AsSpan(44), -8);
                return copy;
            }) };

            // AV pair declares a length longer than the remaining TargetInfo bytes.
            yield return new object[] { "AvPairOverrun", (Func<byte[], byte[]>)(blob =>
            {
                byte[] copy = (byte[])blob.Clone();
                int targetInfoOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(copy.AsSpan(44));
                // Overwrite the first AV pair length so it extends past the end of TargetInfo.
                BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(targetInfoOffset + 2), ushort.MaxValue);
                return copy;
            }) };

            // Timestamp AV pair body is shorter than the required 8 bytes.
            yield return new object[] { "ShortTimestamp", (Func<byte[], byte[]>)(blob =>
            {
                byte[] copy = (byte[])blob.Clone();
                int targetInfoOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(copy.AsSpan(44));
                int targetInfoLength = BinaryPrimitives.ReadUInt16LittleEndian(copy.AsSpan(40));
                Span<byte> ti = copy.AsSpan(targetInfoOffset, targetInfoLength);
                // Walk to the Timestamp AV (AvId 7) emitted by FakeNtlmServer and shorten it.
                int pos = 0;
                while (pos + 4 <= ti.Length)
                {
                    ushort id = BinaryPrimitives.ReadUInt16LittleEndian(ti.Slice(pos));
                    ushort len = BinaryPrimitives.ReadUInt16LittleEndian(ti.Slice(pos + 2));
                    if (id == 7 /* Timestamp */)
                    {
                        BinaryPrimitives.WriteUInt16LittleEndian(ti.Slice(pos + 2), 4);
                        break;
                    }
                    pos += 4 + len;
                }
                return copy;
            }) };
        }

        [ConditionalTheory(typeof(NegotiateAuthenticationTests), nameof(UseManagedNtlm))]
        [MemberData(nameof(MalformedChallengeBlobs))]
        public void NtlmMalformedChallenge_ReturnsInvalidToken(string scenario, Func<byte[], byte[]> corruptor)
        {
            _ = scenario;

            using FakeNtlmServer fakeNtlmServer = new FakeNtlmServer(s_testCredentialRight);
            NegotiateAuthentication ntAuth = new NegotiateAuthentication(
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = s_testCredentialRight,
                    TargetName = "HTTP/foo",
                });

            NegotiateAuthenticationStatusCode statusCode;
            byte[]? negotiateBlob = ntAuth.GetOutgoingBlob((byte[])null, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.ContinueNeeded, statusCode);
            Assert.NotNull(negotiateBlob);

            byte[]? challengeBlob = fakeNtlmServer.GetOutgoingBlob(negotiateBlob);
            Assert.NotNull(challengeBlob);

            byte[] malformed = corruptor(challengeBlob);

            byte[]? response = ntAuth.GetOutgoingBlob(malformed, out statusCode);
            Assert.Equal(NegotiateAuthenticationStatusCode.InvalidToken, statusCode);
            Assert.Null(response);
        }
    }
}
