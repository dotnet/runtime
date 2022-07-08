// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
