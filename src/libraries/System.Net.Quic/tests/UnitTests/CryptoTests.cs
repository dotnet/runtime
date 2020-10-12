// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Security;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class CryptoTests
    {
        private static CryptoSeal DeriveClientCryptoSeal()
        {
            var secret =
                KeyDerivation.DeriveClientInitialSecret(HexHelpers.FromHexString(ReferenceData.DcidHex));

            return CryptoSeal.Create(QuicConstants.InitialCipherSuite, secret);
        }

        [Fact]
        public void DecryptClientInitial()
        {
            var packet = HexHelpers.FromHexString(ReferenceData.EncryptedClientInitialPacket);

            // no error handling yet, test happy path only

            // derive keying material (we need client material to decrypt client's message)
            var seal = DeriveClientCryptoSeal();

            // These would normally be calculated
            int pnOffset = 18;
            int payloadLength = 1162 + 4 + 16;
            seal.UnprotectHeader(packet, pnOffset);
            Assert.True(seal.UnprotectPacket(packet, pnOffset, payloadLength, 0));

            const string headerHex = ReferenceData.ClientInitialPacketHeaderHex;
            int headerLen = headerHex.Length / 2;

            Assert.Equal(
                headerHex,
                HexHelpers.ToHexString(packet.AsSpan(0, headerLen)));
        }

        [Fact]
        public void EncryptClientInitial()
        {
            const string headerHex = ReferenceData.ClientInitialPacketHeaderHex;
            int headerLen = headerHex.Length / 2;

            var encryptedClientInitial = HexHelpers.FromHexString(ReferenceData.EncryptedClientInitialPacket);

            byte[] buff = new byte[encryptedClientInitial.Length];

            HexHelpers.FromHexString(headerHex, buff);
            HexHelpers.FromHexString(ReferenceData.ClientInitialPacketCryptoFrameHex, buff.AsSpan(headerLen));
            // rest of the packet is zeros (padding frames)

            // derive keying material
            CryptoSeal seal = DeriveClientCryptoSeal();

            int pnOffset = headerLen - 4 /*pnLength*/;
            seal.ProtectPacket(buff, pnOffset, ReferenceData.ClientInitialPayloadLength + 4 + 16, 2);
            seal.ProtectHeader(buff, pnOffset);

            Assert.Equal(encryptedClientInitial, buff);
        }

        [Fact]
        public void TestHeaderProtection()
        {
            var payloadSample = HexHelpers.FromHexString(ReferenceData.ClientInitialPacketPayloadSampleHex);
            string expectedMask = ReferenceData.ClientInitialPacketProtectionMaskHex;
            var headerKey = HexHelpers.FromHexString(ReferenceData.ClientInitialHpHex);
            var header = HexHelpers.FromHexString(ReferenceData.ClientInitialPacketHeaderHex);
            string expectedProtectedHeader = ReferenceData.ClientInitialPacketProtectedHeaderHex;

            var alg = CryptoSealAlgorithm.Create(QuicConstants.InitialCipherSuite, new byte[16], headerKey);

            Span<byte> protectionMask = stackalloc byte[5];
            alg.CreateHeaderProtectionMask(payloadSample, protectionMask);
            Assert.Equal(expectedMask, HexHelpers.ToHexString(protectionMask));

            var actual = (byte[])header.Clone();

            CryptoSeal.ProtectHeader(actual, protectionMask, 4);
            Assert.Equal(expectedProtectedHeader, HexHelpers.ToHexString(actual));

            // also try unprotecting
            CryptoSeal.UnprotectHeader(actual, protectionMask);
            Assert.Equal(
                HexHelpers.ToHexString(header),
                HexHelpers.ToHexString(actual));
        }

        [Fact]
        public void TestInitialClientKeyingMaterial()
        {
            var initial =
                KeyDerivation.DeriveClientInitialSecret(HexHelpers.FromHexString(ReferenceData.DcidHex));

            Assert.Equal(ReferenceData.ClientInitialHex, HexHelpers.ToHexString(initial));

            var key = KeyDerivation.DeriveKey(initial);
            Assert.Equal(ReferenceData.ClientInitialKeyHex, HexHelpers.ToHexString(key));

            var iv = KeyDerivation.DeriveIv(initial);
            Assert.Equal(ReferenceData.ClientInitialIvHex, HexHelpers.ToHexString(iv));

            var hp = KeyDerivation.DeriveHp(initial);
            Assert.Equal(ReferenceData.ClientInitialHpHex, HexHelpers.ToHexString(hp));
        }

        [Fact]
        public void TestInitialServerKeyingMaterial()
        {
            var initial =
                KeyDerivation.DeriveServerInitialSecret(HexHelpers.FromHexString(ReferenceData.DcidHex));

            Assert.Equal(ReferenceData.ServerInitialHex, HexHelpers.ToHexString(initial));

            var key = KeyDerivation.DeriveKey(initial);
            Assert.Equal(ReferenceData.ServerInitialKeyHex, HexHelpers.ToHexString(key));

            var iv = KeyDerivation.DeriveIv(initial);
            Assert.Equal(ReferenceData.ServerInitialIvHex, HexHelpers.ToHexString(iv));

            var hp = KeyDerivation.DeriveHp(initial);
            Assert.Equal(ReferenceData.ServerInitialHpHex, HexHelpers.ToHexString(hp));
        }
    }
}
