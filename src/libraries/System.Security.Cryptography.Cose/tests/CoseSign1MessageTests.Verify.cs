using Xunit;
using Test.Cryptography;
using System.Formats.Cbor;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseSign1MessageTests_Verify : CoseMessageTests_Verify
    {
        internal override CoseMessage Decode(byte[] cborPayload)
            => CoseMessage.DecodeSign1(cborPayload);

        [Theory]
        // https://github.com/cose-wg/Examples/blob/master/RFC8152/Appendix_C_2_1.json
        [InlineData((int)ECDsaAlgorithm.ES256, "D28443A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36")]
        // https://github.com/cose-wg/Examples/blob/master/ecdsa-examples/ecdsa-sig-01.json
        [InlineData((int)ECDsaAlgorithm.ES256, "D28445A201260300A10442313154546869732069732074686520636F6E74656E742E58406520BBAF2081D7E0ED0F95F76EB0733D667005F7467CEC4B87B9381A6BA1EDE8E00DF29F32A37230F39A842A54821FDD223092819D7728EFB9D3A0080B75380B")]
        // https://github.com/cose-wg/Examples/blob/master/ecdsa-examples/ecdsa-sig-02.json
        [InlineData((int)ECDsaAlgorithm.ES384, "D28444A1013822A104445033383454546869732069732074686520636F6E74656E742E58605F150ABD1C7D25B32065A14E05D6CB1F665D10769FF455EA9A2E0ADAB5DE63838DB257F0949C41E13330E110EBA7B912F34E1546FB1366A2568FAA91EC3E6C8D42F4A67A0EDF731D88C9AEAD52258B2E2C4740EF614F02E9D91E9B7B59622A3C")]
        // https://github.com/cose-wg/Examples/blob/master/ecdsa-examples/ecdsa-sig-03.json
        [InlineData((int)ECDsaAlgorithm.ES512, "D28444A1013823A104581E62696C626F2E62616767696E7340686F626269746F6E2E6578616D706C6554546869732069732074686520636F6E74656E742E588401664DD6962091B5100D6E1833D503539330EC2BC8FD3E8996950CE9F70259D9A30F73794F603B0D3E7C5E9C4C2A57E10211F76E79DF8FFD1B79D7EF5B9FA7DA109001965FA2D37E093BB13C040399C467B3B9908C09DB2B0F1F4996FE07BB02AAA121A8E1C671F3F997ADE7D651081017057BD3A8A5FBF394972EA71CFDC15E6F8FE2E1")]
        [InlineData((int)RSAAlgorithm.PS256, "D28444A1013824A054546869732069732074686520636F6E74656E742E59010055ECE8B56A00693173CCB750F85DF898AD1DEBC4F151BD9119B65517BC7A0A5C4B0D8B4B22C4A75C6BC72D0C39BE71630E10D637E68D1261CA2CE344EC1929E6BA74CDAE8B153EAF86476B06D85E8A0562BACFF1E8BA787964A9CB89E14F7C765BAB7C4A95C3D6AFA584A7B449FF6ED2C7CB3D6B9BE58393E9715B78BAA8BCF9105E39819111534A2E5A1A27802353DD5455C32E98E5904B451469453B2C507186EA87F57FE3711D6A6CDA947E1F1750488DDA62C4E9179D396E0A0646E14317E35B1244B8B1542314DB77414833850C8A214417D1647836CA27E4A37E7E2192496411EBBB38E44C69EC2443DE39C233BE0C0C0942E00CDA4ADB0DB3A3D0F674")]
        [InlineData((int)RSAAlgorithm.PS384, "D28444A1013825A054546869732069732074686520636F6E74656E742E5901002E9CBFCB8722E1B52D77228038DD27FAF0D8CF70F6E0E05C405C70534DF3C8EA419122E3007B3EB09FF6BD900E7DDC853BDA3D499F3779BD724D2B661CA9A2BB8C2AAE40298D13124B4E6C32AA311128D6553D7AB87F7771192DBC870635F5E355F89649946FC78B4647371EF3F2BB3175BA6CE1D1707851FEA8708A57BB26E206EACA1CB2F5661ED4C05D07975102009CE9309654AD022D3C79E7F043AC9C102C9DBF75D17DF52D269DBF6E87E5CE0185D888697D90E5AC371FC1BA7F1FBC9BA9D64CFE5EC1068157DBDB2A1475F31446058033E0CB0360A924AD917C0C363EACBBC5CA8C5B7CD19F67F52C01A6D9E213546D59EA5D8AE1806E9FA9C355A3D9")]
        [InlineData((int)RSAAlgorithm.PS512, "D28444A1013826A054546869732069732074686520636F6E74656E742E5901004B8B34077E4DB906C1A99A09E1569CCBB275A61AE077E5A62DD14DCDEB8F2D4071015CDFB5A6258F175CF3FAA6C11BF7667AAB6B69969A1B0A68E142C0E7B287E451CE4E889AB6EEF45CE9FF48DBEAEC246AD922D78C0811441C66FF31641F0E3D37852803C62832012F29933ADF4D3EFDB8D0C6397B4AA7AEA60D2E41E1DB68E2A0A28B28C01F39AD4ABA0F5FDD170E42F5CBD8A24695723C153A029DBB19C5D47FD9B77EC654CDE01353AA1049E80921EAF9968D56C7450CEBD0F4A8B847AF3DB8DD2A528CC9FDDC520C4797D42E8888800E0264838D21E5CF39CB912E0BADD24226F1A1C2BF0961D13EBE043375761B20CAA8E8A8B2449D2AAF7879426B9B")]
        // TODO: This test should be passing but is not https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-pass-01.json
        //[InlineData((int)ECDsaAlgorithm.ES256, "D28441A0A201260442313154546869732069732074686520636F6E74656E742E584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F")]
        // https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-pass-03.json
        [InlineData((int)ECDsaAlgorithm.ES256, "8443A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36")]
        public void TestVerify(int algorithm, string hexCborMessage)
        {
            ReplaceContentInHexCborMessage(ref hexCborMessage);

            foreach (bool useNonPrivateKey in new[] { false, true })
            {
                CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessage));
                AsymmetricAlgorithm key = GetKeyHashPaddingTriplet<AsymmetricAlgorithm>((CoseAlgorithm)algorithm, useNonPrivateKey).Key;

                Assert.True(Verify(msg, key, s_sampleContent), "Verification failed.");

                if (UseDetachedContent)
                {
                    Assert.Null(msg.Content);
                }
                else
                {
                    AssertExtensions.SequenceEqual(s_sampleContent, msg.Content.GetValueOrDefault().Span);
                }

                Assert.True(msg.ProtectedHeaders.TryGetValue(CoseHeaderLabel.Algorithm, out CoseHeaderValue value),
                    "Algorithm header must be protected");

                Assert.Equal(algorithm, new CborReader(value.EncodedValue).ReadInt32());
            }
        }

        public void TestVerifyWithAssociatedData()
        {
            // https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-pass-02.json
            string hexCborMessage = "D28443A10126A10442313154546869732069732074686520636F6E74656E742E584010729CD711CB3813D8D8E944A8DA7111E7B258C9BDCA6135F7AE1ADBEE9509891267837E1E33BD36C150326AE62755C6BD8E540C3E8F92D7D225E8DB72B8820B";
            ReplaceContentInHexCborMessage(ref hexCborMessage);

            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessage));

            Assert.False(Verify(msg, DefaultKey, s_sampleContent));

            byte[] associatedData = ByteUtils.HexToByteArray("11aa22bb33cc44dd55006699");
            Assert.True(Verify(msg, DefaultKey, s_sampleContent, associatedData));
        }

        [Theory]
        // https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-fail-02.json
        [InlineData("D28443A10126A10442313154546869732069732074686520636F6E74656E742F58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36", true)]
        // https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-fail-06.json
        [InlineData("D28445A201260300A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36", false)]
        // https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-fail-07.json
        [InlineData("D28443A10126A10442313154546869732069732074686520636F6E74656E742E58406520BBAF2081D7E0ED0F95F76EB0733D667005F7467CEC4B87B9381A6BA1EDE8E00DF29F32A37230F39A842A54821FDD223092819D7728EFB9D3A0080B75380B", false)]
        public void VerifyReturnsFalseWithWrongSignature(string hexCborMessage, bool corruptContent)
        {
            if (corruptContent && UseDetachedContent)
            {
                return;
            }

            ReplaceContentInHexCborMessage(ref hexCborMessage);

            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessage));
            Assert.False(Verify(msg, DefaultKey, s_sampleContent));
        }

        [Fact]
        public void VerifyReturnsFalseWithDataNotMatchingSignature()
        {
            string hexCborMessage = "D28445A201260300A10442313154546869732069732074686520636F6E74656E742E58406520BBAF2081D7E0ED0F95F76EB0733D667005F7467CEC4B87B9381A6BA1EDE8E00DF29F32A37230F39A842A54821FDD223092819D7728EFB9D3A0080B75380B";
            ReplaceContentInHexCborMessage(ref hexCborMessage);

            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessage));
            Assert.True(Verify(msg, DefaultKey, s_sampleContent), "Verification failed");

            string hexCborMessageCorrupt1 = ReplaceFirst(hexCborMessage, "45A201260300", "45A201260301");
            msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessageCorrupt1));
            Assert.False(Verify(msg, DefaultKey, s_sampleContent), "Verification passed when should've failed - Corrupt protected header");

            if (!UseDetachedContent)
            {
                string hexCborMessageCorrupt2 = ReplaceFirst(hexCborMessage, "546869732069732074686520636F6E74656E742E", "546869732069732074686520636F6E74656E743E");
                msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessageCorrupt2));
                Assert.False(Verify(msg, DefaultKey, s_sampleContent), "Verification passed when should've failed - Corrupt content");
            }
        }

        [Theory]
        // https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-fail-03.json
        [InlineData("D28445A1013903E6A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36")]
        // https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-fail-04.json
        [InlineData("D2844AA10167756E6B6E6F776EA10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36")]
        public void VerifyThrowsWithUnknownAlgorithm(string hexCborMessage)
        {
            ReplaceContentInHexCborMessage(ref hexCborMessage);
            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessage));
            Assert.Throws<CryptographicException>(() => Verify(msg, DefaultKey, s_sampleContent));
        }

        [Fact]
        public void VerifyThrowsIfMessageWasDetachedAndContentWasNotSupplied()
        {
            if (UseDetachedContent)
            {
                return;
            }

            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray("D28445A201260300A104423131F658406520BBAF2081D7E0ED0F95F76EB0733D667005F7467CEC4B87B9381A6BA1EDE8E00DF29F32A37230F39A842A54821FDD223092819D7728EFB9D3A0080B75380B"));
            Assert.Null(msg.Content);
            Assert.Throws<InvalidOperationException>(() => Verify(msg, DefaultKey, s_sampleContent));
        }

        [Fact]
        public void VerifyThrowsIfMessageWasEmbeddedAndContentWasSupplied()
        {
            if (!UseDetachedContent)
            {
                return;
            }

            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray("D28443A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36"));
            Assert.NotNull(msg.Content);
            Assert.Throws<InvalidOperationException>(() => Verify(msg, DefaultKey, s_sampleContent));
        }

        private void ReplaceContentInHexCborMessage(ref string hexCborMessage)
        {
            if (UseDetachedContent)
            {
                hexCborMessage = hexCborMessage.Replace(SampleContentByteStringCborHex, NullCborHex);
            }
        }
    }

    public class CoseSign1MessageTests_VerifyEmbedded: CoseSign1MessageTests_Verify
    {
        internal override bool UseDetachedContent => false;

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseSign1Message sign1Msg = Assert.IsType<CoseSign1Message>(msg);
            return sign1Msg.VerifyEmbedded(key, associatedData);
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
            => CoseSign1Message.SignEmbedded(content, signer);
    }

    public class CoseSign1MessageTests_VerifyDetached : CoseSign1MessageTests_Verify
    {
        internal override bool UseDetachedContent => true;

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseSign1Message sign1Msg = Assert.IsType<CoseSign1Message>(msg);
            return sign1Msg.VerifyDetached(key, content, associatedData);
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
            => CoseSign1Message.SignDetached(content, signer);
    }
}
