// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
using Test.Cryptography;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public partial class CoseMessageTests
    {
        [Fact]
        public void DecodeSign1_VerifyUntagged()
        {
            // https://github.com/cose-wg/Examples/blob/master/ecdsa-examples/ecdsa-sig-01.json minus first byte.
            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray("8445A201260300A10442313154546869732069732074686520636F6E74656E742E58406520BBAF2081D7E0ED0F95F76EB0733D667005F7467CEC4B87B9381A6BA1EDE8E00DF29F32A37230F39A842A54821FDD223092819D7728EFB9D3A0080B75380B"));
            Assert.True(msg.VerifyEmbedded(DefaultKey));
        }

        [Fact]
        public void DecodeSign1_VerifyDetachedContent()
        {
            // Content is replaced with CBOR null - https://github.com/cose-wg/Examples/blob/master/ecdsa-examples/ecdsa-sig-01.json.
            CoseSign1Message msg = CoseMessage.DecodeSign1(ByteUtils.HexToByteArray("D28445A201260300A104423131F658406520BBAF2081D7E0ED0F95F76EB0733D667005F7467CEC4B87B9381A6BA1EDE8E00DF29F32A37230F39A842A54821FDD223092819D7728EFB9D3A0080B75380B"));
            Assert.Null(msg.Content);
            Assert.True(msg.VerifyDetached(DefaultKey, s_sampleContent));
        }

        [Theory]
        // Protected bucket has duplicate header.
        [InlineData(true, "D28446A20126013822A10442313154546869732069732074686520636F6E74656E742E584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F")]
        // Unprotected bucket has duplicate header.
        [InlineData(true, "D28441A0A301260138220442313154546869732069732074686520636F6E74656E742E584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F")]
        // Duplicate header is in the union of protected and unprotected buckets.
        [InlineData(false, "D28443A10126A201260442313154546869732069732074686520636F6E74656E742E584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F")]
        public void DecodeSign1_ThrowsWithDuplicateHeaders(bool shouldContainInnerException, string hexCborMessage)
        {
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessage)));
            if (shouldContainInnerException) // if the duplicate headers were in one bucket the exception comes from CborReader because we use CborConformanceMode.Strict.
            {
                Assert.IsType<CborContentException>(ex.InnerException);
            }
            else
            {
                Assert.Null(ex.InnerException);
            }
        }

        [Theory]
        // Tag is 998 (Unknown) - https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-fail-01.json.
        [InlineData("D903E68443A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36")]
        // Tag is 98 (Sign) - https://github.com/cose-wg/Examples/blob/master/sign-tests/sign-pass-01.json.
        [InlineData("D8628441A0A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A")]
        public void DecodeSign1_IncorrectTag(string hexCborMessage)
        {
            Assert.Throws<CryptographicException>(() => CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(hexCborMessage)));
        }

        [Fact]
        public void DecodeSign1_IncorrectStructure()
        {
            var writer = new CborWriter();
            writer.WriteStartArray(4);
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteEndArray();
            Assert.Throws<CryptographicException>(() => CoseMessage.DecodeSign1(writer.Encode()));
        }

        [Fact]
        public void DecodeSign1_IndefiniteLengthArray()
        {
            byte[] cborPayload = ByteUtils.HexToByteArray("D29F43A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36FF");
            CoseSign1Message msg = CoseMessage.DecodeSign1(cborPayload);

            Assert.True(msg.VerifyEmbedded(DefaultKey));
        }

        [Fact]
        public void DecodeSign1_IndefiniteLengthArray_MissingBreak()
        {
            byte[] cborPayload = ByteUtils.HexToByteArray("D29F43A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36");
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeSign1(cborPayload));
            Assert.IsType<CborContentException>(ex.InnerException);
        }

        [Fact]
        public void DecodeSign1_IndefiniteLengthArray_LargerByOne()
        {
            byte[] cborPayload = ByteUtils.HexToByteArray("D29F43A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB3640FF");
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeSign1(cborPayload));
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void DecodeSign1_IndefiniteLengthArray_ShorterByOne()
        {
            byte[] cborPayload = ByteUtils.HexToByteArray("D29F43A10126A10442313154546869732069732074686520636F6E74656E742EFF");
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeSign1(cborPayload));
            Assert.Null(ex.InnerException);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void DecodeSign1ThrowsIfCriticalHeaderIsMissing(bool detached, bool useIndefiniteLength)
        {
            const string AttachedDefiniteHex =
                "D28447A201260281182AA054546869732069732074686520636F6E74656E742E" +
                "5840F78745BDFA8CDF90ED6EC130BC8D97F43C8A52899920221832A1E758A1E7" +
                "590827148F6D1A76673E7E9615F628730B19F07707B6FB1C9CD7B6D4E2B3C3F0" +
                "DEAD";

            const string AttachedIndefiniteHex =
                "D28448A20126029F182AFFA054546869732069732074686520636F6E74656E74" +
                "2E58408B07F60298F64453356EAF005C630A4576AF4C66E0327579BB81B5D726" +
                "3836AA9419B1312298DD47BC10BA22D6DEEE35F1526948BF098915816149B46A" +
                "3C9981";

            const string DetachedDefiniteHex =
                "D28447A201260281182AA0F6584089B093A038B0636940F9273EF11214B64CC1" +
                "BB862305EDEC9C772A3D5089A54A6CBBA00323FA59A593A828F157653DEE15B0" +
                "EBBDC070D02CDFD13E8A9F2ECA1B";

            const string DetachedIndefiniteHex =
                "D28448A20126029F182AFFA0F658409B35B9FD294BDF36EEF7494D0EC9E19F6A2" +
                "106638FD4A2A31B816FED80493772DCEA8B64F6618119E278379F83E1A62BA382" +
                "21B9F1AC705FAD8612DC6B0478A0";

            string inputHex = (detached, useIndefiniteLength) switch
            {
                (false, false) => AttachedDefiniteHex,
                (false, true) => AttachedIndefiniteHex,
                (true, false) => DetachedDefiniteHex,
                (true, true) => DetachedIndefiniteHex,
            };

            AssertExtensions.ThrowsContains<CryptographicException>(
                () => CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(inputHex)),
                "Critical Header '42' missing from protected map.");
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void DecodeSign1ThrowsIfCriticalHeadersIsEmpty(bool detached, bool useIndefiniteLength)
        {
            const string AttachedDefiniteHex =
                "D28445A201260280A054546869732069732074686520636F6E74656E742E5840" +
                "57C7EE86AF06B1ABB002480CE148DFDA06C2CA4AFE83E9C7AE3493EA13E06E9B" +
                "0A4C713F7FDCDD2F8731103CDA28B83313E411988B88AC7716E43307B5AF22FD";

            const string AttachedIndefiniteHex =
                "D28446A20126029FFFA054546869732069732074686520636F6E74656E742E58" +
                "401B941A9C799270827BE5139EC5F3DE4E072913F6473C7278E691D6C58D407A" +
                "23DB3176383E8429AA558418EE33CB7DFFD2CF251EEC93B6CFC300D0D9679CE5" +
                "42";

            const string DetachedDefiniteHex =
                "D28445A201260280A0F658409B0EBC937A969A7D4BB2AA0B1004091EDAA00AE2" +
                "BBCCBB994B7278C9E50C6C734B3A53CB5B87A99E75F63D16B73757CA23C99CF0" +
                "8F8F909A1332DAC05D9DB1C0";

            const string DetachedIndefiniteHex =
                "D28446A20126029FFFA0F65840CA96F1292FEE2B787DC75D91553024E70DD62B" +
                "EA0BFE284024385C6D9493EEF6F055825E79244B63E76F69A419C3A36B3B1F18" +
                "34789A23983D685B7CDA231E86";

            string inputHex = (detached, useIndefiniteLength) switch
            {
                (false, false) => AttachedDefiniteHex,
                (false, true) => AttachedIndefiniteHex,
                (true, false) => DetachedDefiniteHex,
                (true, true) => DetachedIndefiniteHex,
            };

            AssertExtensions.ThrowsContains<CryptographicException>(
                () => CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(inputHex)),
                "Critical Headers must be a CBOR array of at least one element.");
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void DecodeSign1ThrowsIfCriticalHeaderIsOfUnknownType(bool detached, bool useIndefiniteLength)
        {
            const string AttachedDefiniteHex =
                "D28447A201260281412AA054546869732069732074686520636F6E74656E742E" +
                "58403529AC69F69A80B4055CFFCA88F010390509E0A9D4D0083F23DF46841144" +
                "B7E9D7CC11E90D0D51103672083449B439B71EAF6B922C011CC471D8E1D577C6" +
                "B954";

            const string AttachedIndefiniteHex =
                "D28448A20126029F412AFFA054546869732069732074686520636F6E74656E74" +
                "2E5840FE8A2CBBBA2A154361BEF0892D11FF621A1DBDCBD1A955020DD7D85ED8" +
                "15C43B3AB39A32561AAEF679D08FD561339AC9A4E537B2E91DC120A32F406455" +
                "F3353F";

            const string DetachedDefiniteHex =
                "D28447A201260281412AA0F65840AB87DA5ABA5A470C7508F5F1724744458407" +
                "897746890428F877AD593F9D90E5503A6D1B3369AF77952223D5C474CBB8EC62" +
                "9726F967921A4AB91DC8F86DA1CF";

            const string DetachedIndefiniteHex =
                "D28448A20126029F412AFFA0F658409613065203B619BE9CEC1CC596F59C7395" +
                "5AEE8BD492F16B72D2C0F443AE70E5E5B1D615A06A90145078B41A1CA12D4067" +
                "D6C6CEEB2C19B3747A0926305EBA09";

            string inputHex = (detached, useIndefiniteLength) switch
            {
                (false, false) => AttachedDefiniteHex,
                (false, true) => AttachedIndefiniteHex,
                (true, false) => DetachedDefiniteHex,
                (true, true) => DetachedIndefiniteHex,
            };

            AssertExtensions.ThrowsContains<CryptographicException>(
               () => CoseMessage.DecodeSign1(ByteUtils.HexToByteArray(inputHex)),
               "Header '2' does not accept the specified value.");
        }
    }
}
