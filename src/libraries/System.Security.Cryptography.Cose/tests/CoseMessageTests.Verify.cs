using Xunit;
using Test.Cryptography;
using System.Formats.Cbor;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseMessageTests_Verify
    {
        internal abstract bool UseDetachedContent { get; }
        internal abstract CoseMessage Decode(byte[] cborPayload);
        internal abstract byte[] Sign(byte[] content, CoseSigner signer);
        internal abstract bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null);

        [Fact]
        public void VerifyReturnsTrueAfterAttemptWithWrongContent()
        {
            if (!UseDetachedContent)
            {
                return;
            }

            byte[] correctContent = s_sampleContent;
            byte[] wrongContent = new byte[s_sampleContent.Length];
            wrongContent.AsSpan().Fill(42);

            byte[] encodedMsg = Sign(correctContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseMessage msg = Decode(encodedMsg);

            Assert.False(Verify(msg, DefaultKey, wrongContent), "Calling Verify with the wrong content");
            Assert.True(Verify(msg, DefaultKey, s_sampleContent), "Calling Verify with the correct content");
        }

        [Fact]
        public void VerifyReturnsFalseAfterAttemptWithCorrectContent()
        {
            if (!UseDetachedContent)
            {
                return;
            }

            byte[] correctContent = s_sampleContent;
            byte[] wrongContent = new byte[s_sampleContent.Length];
            wrongContent.AsSpan().Fill(42);

            byte[] encodedMsg = Sign(correctContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseMessage msg = Decode(encodedMsg);

            Assert.True(Verify(msg, DefaultKey, s_sampleContent), "Calling Verify with the correct content");
            Assert.False(Verify(msg, DefaultKey, wrongContent), "Calling Verify with the wrong content");
        }

        [Fact]
        public void VerifyThrowsIfContentIsNull()
        {
            if (!UseDetachedContent)
            {
                return;
            }

            byte[] encodedMsg = Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseMessage msg = Decode(encodedMsg);

            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => Verify(msg, DefaultKey, null!));
            Assert.True(ex.ParamName == "content" || ex.ParamName == "detachedContent");
        }

        [Fact]
        public void VerifyThrowsIfKeyIsNull()
        {
            byte[] encodedMsg = Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));

            CoseMessage msg = Decode(encodedMsg);

            Assert.Throws<ArgumentNullException>("key", () => Verify(msg, null!, s_sampleContent));
        }

        [Fact]
        public void VerifyThrowsIfKeyIsNotSupported()
        {
            byte[] encodedMsg = Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));

            CoseMessage msg = Decode(encodedMsg);

            AsymmetricAlgorithm key = ECDiffieHellman.Create();
            Assert.Throws<ArgumentException>("key", () => Verify(msg, key, s_sampleContent));
        }

        [Theory]
        [InlineData(-6)]
        [InlineData(-8)]
        [InlineData(-34)]
        [InlineData(-40)]
        [InlineData(-256)]
        [InlineData(-260)]
        public void VerifyThrowsIfIncorrectIntegerAlgorithm(int incorrectAlg)
        {
            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            // Template header
            signer.ProtectedHeaders.Add(new CoseHeaderLabel(42), 42);
            string hexTemplateHeaders = "47A20126182A182A";
            string hexCborMessage = Sign(s_sampleContent, signer).ByteArrayToHex();

            // Creaft a encoded protected map that replaces the "Template value" map.
            var writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(1);
            writer.WriteInt32(incorrectAlg);
            writer.WriteEndMap();
            byte[] newMap = writer.Encode();

            writer.Reset();
            writer.WriteByteString(newMap);
            string hexNewMap = writer.Encode().ByteArrayToHex();

            hexCborMessage = ReplaceFirst(hexCborMessage, hexTemplateHeaders, hexNewMap);

            CoseMessage msg = Decode(ByteUtils.HexToByteArray(hexCborMessage));
            Assert.Throws<CryptographicException>(() => Verify(msg, DefaultKey, s_sampleContent));
        }

        [Fact]
        public void VerifyThrowsIfIncorrectStringAlgorithm()
        {
            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            // Template header
            signer.ProtectedHeaders.Add(new CoseHeaderLabel(42), 42);
            string hexTemplateHeaders = "47A20126182A182A";
            string hexCborMessage = Sign(s_sampleContent, signer).ByteArrayToHex();

            // Algorithm header is "FOO".
            string hexNewMap = "49A10166343634463446";
            hexCborMessage = ReplaceFirst(hexCborMessage, hexTemplateHeaders, hexNewMap);

            CoseMessage msg = Decode(ByteUtils.HexToByteArray(hexCborMessage));
            Assert.Throws<CryptographicException>(() => Verify(msg, DefaultKey, s_sampleContent));
        }
    }
}
