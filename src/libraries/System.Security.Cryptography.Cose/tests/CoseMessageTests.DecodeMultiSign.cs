// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Formats.Cbor;
using Test.Cryptography;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public partial class CoseMessageTests
    {
        [Fact]
        public void DecodeMultiSign_VerifyUntagged()
        {
            // https://github.com/cose-wg/Examples/blob/master/ecdsa-examples/ecdsa-01.json minus first 2 bytes.
            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(ByteUtils.HexToByteArray("8443A10300A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258"));

            ReadOnlyCollection<CoseSignature> signatures = msg.Signatures;
            Assert.Equal(1, signatures.Count);
            Assert.True(signatures[0].VerifyEmbedded(DefaultKey));
        }

        [Fact]
        public void DecodeMultiSign_VerifyDetachedContent()
        {
            // Content is replaced with CBOR null - https://github.com/cose-wg/Examples/blob/master/ecdsa-examples/ecdsa-01.json.
            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(ByteUtils.HexToByteArray("D8628443A10300A0F6818343A10126A1044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258"));
            Assert.Null(msg.Content);

            ReadOnlyCollection<CoseSignature> signatures = msg.Signatures;
            Assert.Equal(1, signatures.Count);
            Assert.True(signatures[0].VerifyDetached(DefaultKey, s_sampleContent));
        }

        [Theory]
        // Body protected has duplicate header.
        [InlineData(true, "D8628445A203000301A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258")]
        // Body unprotected has duplicate header.
        [InlineData(true, "D8628443A10300A2012601382254546869732069732074686520636F6E74656E742E818343A10126A1044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258")]
        // Duplicate header is in the union of body protected and body unprotected.
        [InlineData(false, "D8628443A10300A1030054546869732069732074686520636F6E74656E742E818343A10126A1044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258")]
        // Sign protected has duplicate header.
        [InlineData(true, "D8628443A10300A054546869732069732074686520636F6E74656E742E818345A201260138A1044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258")]
        // Sign unprotected has duplicate header.
        [InlineData(true, "D8628443A10300A054546869732069732074686520636F6E74656E742E818340A30126013822044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258")]
        // Duplicate header is in the union of sign protected and sign unprotected.
        [InlineData(false, "D8628443A10300A054546869732069732074686520636F6E74656E742E818343A10126A20126044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258")]
        public void DecodeMultiSign_ThrowsWithDuplicateHeaders(bool shouldContainInnerException, string hexCborMessage)
        {
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeMultiSign(ByteUtils.HexToByteArray(hexCborMessage)));
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
        // Tag is 998 (Unknown) - https://github.com/cose-wg/Examples/blob/master/sign-tests/sign-fail-01.json.
        [InlineData("D903E68440A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A")]
        // Tag is 18 (Sign1) - https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-pass-01.json.
        [InlineData("D28441A0A201260442313154546869732069732074686520636F6E74656E742E584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F")]
        public void DecodeMultiSign_IncorrectTag(string hexCborMessage)
        {
            Assert.Throws<CryptographicException>(() => CoseMessage.DecodeMultiSign(ByteUtils.HexToByteArray(hexCborMessage)));
        }

        [Fact]
        public void DecodeMultiSign_IncorrectStructure()
        {
            var writer = new CborWriter();
            writer.WriteStartArray(4);
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteEndArray();
            Assert.Throws<CryptographicException>(() => CoseMessage.DecodeMultiSign(writer.Encode()));
        }

        [Theory]
        // COSE_Sign is an indefinite-length array
        [InlineData("D8629F40A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30AFF")]
        // [+COSE_Signature]
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E9F8343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30AFF")]
        // COSE_Signature
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E819F43A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30AFF")]
        // All of them
        [InlineData("D8629F40A054546869732069732074686520636F6E74656E742E9F9F43A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30AFFFFFF")]
        public void DecodeMultiSign_IndefiniteLengthArray(string hexCborPayload)
        {
            byte[] cborPayload = ByteUtils.HexToByteArray(hexCborPayload);
            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(cborPayload);

            ReadOnlyCollection<CoseSignature> signatures = msg.Signatures;
            Assert.Equal(1, signatures.Count);
            Assert.True(signatures[0].VerifyEmbedded(DefaultKey));
        }

        [Theory]
        // COSE_Sign
        [InlineData("D8629F40A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A")]
        // [+COSE_Signature]
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E9F8343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A")]
        // COSE_Signature
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E819F43A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A")]
        public void DecodeMultiSign_IndefiniteLengthArray_MissingBreak(string hexCborPayload)
        {
            byte[] cborPayload = ByteUtils.HexToByteArray(hexCborPayload);
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeMultiSign(cborPayload));
            Assert.IsType<CborContentException>(ex.InnerException);
        }

        // All these payloads contain one extra element of type byte string.
        [Theory]
        // COSE_Sign
        [InlineData("D8629F40A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A40FF")]
        // [+COSE_Signature] - this structure does not have a fixed length required, but the byte string is unexpected.
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E9F8343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A40FF")]
        // COSE_Signature
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E819F43A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A40FF")]
        public void DecodeMultiSign_IndefiniteLengthArray_LargerByOne(string hexCborPayload)
        {
            byte[] cborPayload = ByteUtils.HexToByteArray(hexCborPayload);
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeMultiSign(cborPayload));
        }

        [Theory]
        // COSE_Sign
        [InlineData("D8629F40A054546869732069732074686520636F6E74656E742EFF")]
        // [+COSE_Signature]
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E9FFF")]
        // COSE_Signature
        [InlineData("D8628440A054546869732069732074686520636F6E74656E742E819F43A10126A104423131FF")]
        public void DecodeMultiSign_IndefiniteLengthArray_ShorterByOne(string hexCborPayload)
        {
            byte[] cborPayload = ByteUtils.HexToByteArray(hexCborPayload);
            CryptographicException ex = Assert.Throws<CryptographicException>(() => CoseMessage.DecodeMultiSign(cborPayload));
            Assert.Null(ex.InnerException);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void DecodeMultiSignThrowsIfCriticalHeaderIsMissing(bool detached, bool useIndefiniteLength)
        {
            const string AttachedDefiniteHex =
                "D8628440A054546869732069732074686520636F6E74656E742E818347A20126" +
                "0281182AA05840ECB8C39BE15156FB6567C33634C75396D7FE1042C84FE54B9C" +
                "EFA51E674C0CB227A8C08E558B6047668BBE3311749776670D1583A14B3A2DD8" +
                "7F63F0FA298452";

            const string AttachedIndefiniteHex =
                "D8628440A054546869732069732074686520636F6E74656E742E818348A20126" +
                "029F182AFFA05840F62CB760AC27D393D88ED392D5D4D55A02B0BB75261E75FE" +
                "9B346C280DA6B93BE7F5B1B66B74561513EA52CAA2C66FE7474010035C678DA6" +
                "B3549D3E671166EB";

            const string DetachedDefiniteHex =
                "D8628440A0F6818347A201260281182AA05840F96CE3D0999F34BE0E3FC62AE2" +
                "AB25DD8D88F7154E6FADD5FFFEAF78F89DB97AC3E599ADB555C8442BD520F3F4" +
                "8CB6A320B864677E26D1FA79FEDD79C3BCA927";

            const string DetachedIndefiniteHex =
                "D8628440A0F6818348A20126029F182AFFA0584028E95F7F9267CED0061339A7" +
                "6602D823774EDA3E8D53B0A4FA436B71B0DBCA6F03F561A67355374AF494648C" +
                "941558146F9C22B17542EBAF23497D27635A1829";

            string inputHex = (detached, useIndefiniteLength) switch
            {
                (false, false) => AttachedDefiniteHex,
                (false, true) => AttachedIndefiniteHex,
                (true, false) => DetachedDefiniteHex,
                (true, true) => DetachedIndefiniteHex,
            };

            AssertExtensions.ThrowsContains<CryptographicException>(
                () => CoseMessage.DecodeMultiSign(ByteUtils.HexToByteArray(inputHex)),
                "Critical Header '42' missing from protected map.");
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void DecodeMultiSignThrowsIfCriticalHeadersIsEmpty(bool detached, bool useIndefiniteLength)
        {
            const string AttachedDefiniteHex =
                "D8628440A054546869732069732074686520636F6E74656E742E818345A20126" +
                "0280A05840B5F9E21078643A74B181ED294AC72C71F20AC5CA7AD037F559C68E" +
                "06148429396A4194133763AB6918D747ACEE820CC430C2E891E3E2D5EECF6126" +
                "1CEA33C6D4";

            const string AttachedIndefiniteHex =
                "D8628440A054546869732069732074686520636F6E74656E742E818346A20126" +
                "029FFFA05840DDF3C0B85415AD1628C0B50C0F3FEDE675C1003484687CDFA3FA" +
                "09285D5A31D48ADF11744BE0AE87F0189408A9CF38F0572537E8A786D505B6A6" +
                "EE2008B91C74";

            const string DetachedDefiniteHex =
                "D8628440A0F6818345A201260280A05840EB66EE9E064CAB2E2F50244661734D" +
                "9AEBD959BD21278E8D4827870DFE10C27B52E3E21D29185FC64526DC3B80C108" +
                "548E956E9DBDDC7B23D100C17715AEE163";

            const string DetachedIndefiniteHex =
                "D8628440A0F6818346A20126029FFFA05840FC954ABD1611F7C6EEDD7FE71C3F" +
                "62821AD46ED1988500F3309D0C607F0F151A69D0FC7BC968B2C36AEE68AC2B9A" +
                "9580DFE1244F6E5F834183497F21EA5900C1";

            string inputHex = (detached, useIndefiniteLength) switch
            {
                (false, false) => AttachedDefiniteHex,
                (false, true) => AttachedIndefiniteHex,
                (true, false) => DetachedDefiniteHex,
                (true, true) => DetachedIndefiniteHex,
            };

            AssertExtensions.ThrowsContains<CryptographicException>(
                () => CoseMessage.DecodeMultiSign(ByteUtils.HexToByteArray(inputHex)),
                "Critical Headers must be a CBOR array of at least one element.");
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void DecodeMultiSignThrowsIfCriticalHeaderIsOfUnknownType(bool detached, bool useIndefiniteLength)
        {
            const string AttachedDefiniteHex =
                "D8628440A054546869732069732074686520636F6E74656E742E818347A20126" +
                "0281412AA05840FCAFEDBE41693C7BA43FB58E2CF06182BE1BF340122CC5AFD4" +
                "F59172C7E95166FF8E98FE9A0C2BEFEA135FD800DE6CA9A281D49B141CB93B17" +
                "D992E693540F8A";

            const string AttachedIndefiniteHex =
                "D8628440A054546869732069732074686520636F6E74656E742E818348A20126" +
                "029F412AFFA058400D3F4426B26007D731677D99B542E524847FF3927BCA74E4" +
                "1823B09D6CA57A0E107F93DFE5DB851F4CEE8C0E4AF83E3540848F026FCD761F" +
                "91CA2ED8D5F98134";

            const string DetachedDefiniteHex =
                "D8628440A0F6818347A201260281412AA0584008E0EEF66622FEC926CB651E90" +
                "13D8628AB72581533761EDE52972FE6DFBF2C4BADB6C218E8AD1E28F8192DFB2" +
                "8A82A4444A74C370AEA6C63AC982EABCD52874";

            const string DetachedIndefiniteHex =
                "D8628440A0F6818348A20126029F412AFFA05840C6DDCA2F35B7B285AB594963" +
                "E9DB43CBDC77842256A7D1D31704749C7446AD5A67BBC02F9DBAF8F394ECCCA7" +
                "8E8B63E5BB746F0205EE5732DFB2E00EBA3D5F48";

            string inputHex = (detached, useIndefiniteLength) switch
            {
                (false, false) => AttachedDefiniteHex,
                (false, true) => AttachedIndefiniteHex,
                (true, false) => DetachedDefiniteHex,
                (true, true) => DetachedIndefiniteHex,
            };

            AssertExtensions.ThrowsContains<CryptographicException>(
                () => CoseMessage.DecodeMultiSign(ByteUtils.HexToByteArray(inputHex)),
                "Header '2' does not accept the specified value.");
        }
    }
}
