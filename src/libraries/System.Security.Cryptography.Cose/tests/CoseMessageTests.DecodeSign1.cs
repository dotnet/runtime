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
    }
}
