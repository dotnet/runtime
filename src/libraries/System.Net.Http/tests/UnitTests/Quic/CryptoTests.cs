using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Security.Cryptography;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class CryptoTests
    {
        private static readonly string dcidHex = "8394c8f03e515708";

        private static readonly string initialSecretHex =
            "524e374c6da8cf8b496f4bcb69678350" +
            "7aafee6198b202b4bc823ebf7514a423";

        [Theory]
        [InlineData("client in", 32, "00200f746c73313320636c69656e7420696e00")]
        [InlineData("server in", 32, "00200f746c7331332073657276657220696e00")]
        [InlineData("quic key", 16, "00100e746c7331332071756963206b657900")]
        [InlineData("quic iv", 12, "000c0d746c733133207175696320697600")]
        [InlineData("quic hp", 16, "00100d746c733133207175696320687000")]
        public void TestLabelGeneration(string label, ushort len, string expectedHex)
        {
            var actual = KeyDerivation.CreateHkdfLabel(label, len);
            var actualHex = HexHelpers.ToHexString(actual);

            Assert.Equal(expectedHex, actualHex);
        }

        [Fact]
        public void TestInitialSecret()
        {
            var actual = KeyDerivation.DeriveInitialSecret(HexHelpers.FromHexString(dcidHex));
            var actualHex = HexHelpers.ToHexString(actual);

            Assert.Equal(initialSecretHex, actualHex);
        }

        [Fact]
        public void TestInitialClientKeyingMaterial()
        {
            var initial = KeyDerivation.DeriveClientInitialSecret(HexHelpers.FromHexString(initialSecretHex));

            var clientInitial =
                "fda3953aecc040e48b34e27ef87de3a6" +
                "098ecf0e38b7e032c5c57bcbd5975b84";

            Assert.Equal(clientInitial, HexHelpers.ToHexString(initial));

            var key = KeyDerivation.DeriveKey(initial);
            Assert.Equal("af7fd7efebd21878ff66811248983694", HexHelpers.ToHexString(key));

            var iv = KeyDerivation.DeriveIv(initial);
            Assert.Equal("8681359410a70bb9c92f0420", HexHelpers.ToHexString(iv));

            var hp = KeyDerivation.DeriveHp(initial);
            Assert.Equal("a980b8b4fb7d9fbc13e814c23164253d", HexHelpers.ToHexString(hp));
        }

        [Fact]
        public void TestInitialServerKeyingMaterial()
        {
            var initial = KeyDerivation.DeriveServerInitialSecret(HexHelpers.FromHexString(initialSecretHex));

            var serverInitial =
                "554366b81912ff90be41f17e80222130" +
                "90ab17d8149179bcadf222f29ff2ddd5";

            Assert.Equal(serverInitial, HexHelpers.ToHexString(initial));

            var key = KeyDerivation.DeriveKey(initial);
            Assert.Equal("5d51da9ee897a21b2659ccc7e5bfa577", HexHelpers.ToHexString(key));

            var iv = KeyDerivation.DeriveIv(initial);
            Assert.Equal("5e5ae651fd1e8495af13508b", HexHelpers.ToHexString(iv));

            var hp = KeyDerivation.DeriveHp(initial);
            Assert.Equal("a8ed82e6664f865aedf6106943f95fb8", HexHelpers.ToHexString(hp));
        }

        [Fact]
        public void TestHeaderProtection()
        {
            var payloadSample = HexHelpers.FromHexString("535064a4268a0d9d7b1c9d250ae35516");
            var expectedMask = "833b343aaa";
            var headerKey = HexHelpers.FromHexString("a980b8b4fb7d9fbc13e814c23164253d");
            var header = HexHelpers.FromHexString("c3ff00001b088394c8f03e5157080000449e00000002");
            var expectedProtectedHeader = "c0ff00001b088394c8f03e5157080000449e3b343aa8";

            var protectionMask = Encryption.GetHeaderProtectionMask(Algorithm.AEAD_AES_128_GCM, headerKey, payloadSample);
            Assert.Equal(expectedMask, HexHelpers.ToHexString(protectionMask));

            var actual = (byte[])header.Clone();

            Encryption.ProtectHeader(actual, protectionMask);
            Assert.Equal(expectedProtectedHeader, HexHelpers.ToHexString(actual));

            // also try unprotecting
            Encryption.UnprotectHeader(actual, protectionMask, 18);
            Assert.Equal(
                HexHelpers.ToHexString(header),
                HexHelpers.ToHexString(actual));
        }

        private const string encryptedClientInitialHex = @"
   c0ff00001b088394c8f03e5157080000 449e3b343aa8535064a4268a0d9d7b1c
   9d250ae355162276e9b1e3011ef6bbc0 ab48ad5bcc2681e953857ca62becd752
   4daac473e68d7405fbba4e9ee616c870 38bdbe908c06d9605d9ac49030359eec
   b1d05a14e117db8cede2bb09d0dbbfee 271cb374d8f10abec82d0f59a1dee29f
   e95638ed8dd41da07487468791b719c5 5c46968eb3b54680037102a28e53dc1d
   12903db0af5821794b41c4a93357fa59 ce69cfe7f6bdfa629eef78616447e1d6
   11c4baf71bf33febcb03137c2c75d253 17d3e13b684370f668411c0f00304b50
   1c8fd422bd9b9ad81d643b20da89ca05 25d24d2b142041cae0af205092e43008
   0cd8559ea4c5c6e4fa3f66082b7d303e 52ce0162baa958532b0bbc2bc785681f
   cf37485dff6595e01e739c8ac9efba31 b985d5f656cc092432d781db95221724
   87641c4d3ab8ece01e39bc85b1543661 4775a98ba8fa12d46f9b35e2a55eb72d
   7f85181a366663387ddc20551807e007 673bd7e26bf9b29b5ab10a1ca87cbb7a
   d97e99eb66959c2a9bc3cbde4707ff77 20b110fa95354674e395812e47a0ae53
   b464dcb2d1f345df360dc227270c7506 76f6724eb479f0d2fbb6124429990457
   ac6c9167f40aab739998f38b9eccb24f d47c8410131bf65a52af841275d5b3d1
   880b197df2b5dea3e6de56ebce3ffb6e 9277a82082f8d9677a6767089b671ebd
   244c214f0bde95c2beb02cd1172d58bd f39dce56ff68eb35ab39b49b4eac7c81
   5ea60451d6e6ab82119118df02a58684 4a9ffe162ba006d0669ef57668cab38b
   62f71a2523a084852cd1d079b3658dc2 f3e87949b550bab3e177cfc49ed190df
   f0630e43077c30de8f6ae081537f1e83 da537da980afa668e7b7fb25301cf741
   524be3c49884b42821f17552fbd1931a 813017b6b6590a41ea18b6ba49cd48a4
   40bd9a3346a7623fb4ba34a3ee571e3c 731f35a7a3cf25b551a680fa68763507
   b7fde3aaf023c50b9d22da6876ba337e b5e9dd9ec3daf970242b6c5aab3aa4b2
   96ad8b9f6832f686ef70fa938b31b4e5 ddd7364442d3ea72e73d668fb0937796
   f462923a81a47e1cee7426ff6d922126 9b5a62ec03d6ec94d12606cb485560ba
   b574816009e96504249385bb61a819be 04f62c2066214d8360a2022beb316240
   b6c7d78bbe56c13082e0ca272661210a bf020bf3b5783f1426436cf9ff418405
   93a5d0638d32fc51c5c65ff291a3a7a5 2fd6775e623a4439cc08dd25582febc9
   44ef92d8dbd329c91de3e9c9582e41f1 7f3d186f104ad3f90995116c682a2a14
   a3b4b1f547c335f0be710fc9fc03e0e5 87b8cda31ce65b969878a4ad4283e6d5
   b0373f43da86e9e0ffe1ae0fddd35162 55bd74566f36a38703d5f34249ded1f6
   6b3d9b45b9af2ccfefe984e13376b1b2 c6404aa48c8026132343da3f3a33659e
   c1b3e95080540b28b7f3fcd35fa5d843 b579a84c089121a60d8c1754915c344e
   eaf45a9bf27dc0c1e784161691220913 13eb0e87555abd706626e557fc36a04f
   cd191a58829104d6075c5594f627ca50 6bf181daec940f4a4f3af0074eee89da
   acde6758312622d4fa675b39f728e062 d2bee680d8f41a597c262648bb18bcfc
   13c8b3d97b1a77b2ac3af745d61a34cc 4709865bac824a94bb19058015e4e42d
   38d3b779d72edc00c5cd088eff802b05
";

        private const string unprotectedCryptoFrameHex = @"
060040c4010000c003036660261ff947 cea49cce6cfad687f457cf1b14531ba1
4131a0e8f309a1d0b9c4000006130113 031302010000910000000b0009000006
736572766572ff01000100000a001400 12001d00170018001901000101010201
03010400230000003300260024001d00 204cfdfcd178b784bf328cae793b136f
2aedce005ff183d7bb14952072366470 37002b0003020304000d0020001e0403
05030603020308040805080604010501 060102010402050206020202002d0002
0101001c00024001";

        [Fact]
        public void EncryptClientInitial()
        {
            const string headerHex = "c3ff00001b088394c8f03e5157080000449e00000002";
            int headerLen = headerHex.Length / 2;

            var encryptedClientInitial = HexHelpers.FromHexString(encryptedClientInitialHex);

            byte[] buff = new byte[encryptedClientInitial.Length];

            HexHelpers.FromHexString(headerHex, buff);
            HexHelpers.FromHexString(unprotectedCryptoFrameHex, buff.AsSpan(headerLen));
            // rest of the packet is zeros (padding frames)

            const int payloadLength = 1162;

            // derive keying material
            CryptoSealAesGcm seal = DeriveClientCryptoSeal();
            seal.EncryptPacket(buff, headerLen, payloadLength, 2);

            Assert.Equal(encryptedClientInitial, buff);
        }

        private static CryptoSealAesGcm DeriveClientCryptoSeal()
        {
            var initial = KeyDerivation.DeriveClientInitialSecret(HexHelpers.FromHexString(initialSecretHex));
            var key = KeyDerivation.DeriveKey(initial);
            var iv = KeyDerivation.DeriveIv(initial);
            var hp = KeyDerivation.DeriveHp(initial);

            var seal = new CryptoSealAesGcm(iv, key, hp);
            return seal;
        }

        [Fact]
        public void DecryptClientInitial()
        {
            var packet = HexHelpers.FromHexString(encryptedClientInitialHex);

            // no error handling yet, test happy path only

            // derive keying material (we need client material to decrypt client's message)
            var seal = DeriveClientCryptoSeal();

            seal.DecryptPacket(packet);

            const string headerHex = "c3ff00001b088394c8f03e5157080000449e00000002";
            int headerLen = headerHex.Length / 2;

            Assert.Equal(
                headerHex,
                HexHelpers.ToHexString(packet.AsSpan(0, headerLen)));
        }
    }
}
