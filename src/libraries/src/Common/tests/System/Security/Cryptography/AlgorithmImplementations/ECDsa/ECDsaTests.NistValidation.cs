// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

using Test.Cryptography;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public abstract partial class ECDsaTests : ECDsaTestsBase
    {
        // These test cases are from http://csrc.nist.gov/groups/STM/cavp/digital-signatures.html#test-vectors
        // FIPS 186-4 ECDSA test vectors
        // 186-3ecdsatestvectors.zip
        // SigGen.txt
#if netcoreapp
        [Fact]
        public static void ValidateNistP256Sha256()
        {
            byte[] msg = (
                "5905238877c77421f73e43ee3da6f2d9e2ccad5fc942dcec0cbd25482935faaf" +
                "416983fe165b1a045ee2bcd2e6dca3bdf46c4310a7461f9a37960ca672d3feb5" +
                "473e253605fb1ddfd28065b53cb5858a8ad28175bf9bd386a5e471ea7a65c17c" +
                "c934a9d791e91491eb3754d03799790fe2d308d16146d5c9b0d0debd97d79ce8"
                ).HexToByteArray();

            ECParameters parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = "1ccbe91c075fc7f4f033bfa248db8fccd3565de94bbfb12f3c59ff46c271bf83".HexToByteArray(),
                    Y = "ce4014c68811f9a21a1fdb2c0e6113e06db7ca93b7404e78dc7ccd5ca89a4ca9".HexToByteArray(),
                },
                D = "519b423d715f8b581f4fa8ee59f4771a5b44c8130b4e3eacca54a56dda72b464".HexToByteArray(),
            };

            byte[] signature = (
                // r
                "f3ac8061b514795b8843e3d6629527ed2afd6b1f6a555a7acabb5e6f79c8c2ac" +
                // s
                "8bf77819ca05a6b2786c76262bf7371cef97b218e96f175a3ccdda2acc058903"
                ).HexToByteArray();

            Validate(
                parameters,
                ECDsaTestData.GetNistP256ExplicitCurve(),
                msg,
                signature,
                HashAlgorithmName.SHA256);
        }

        [Fact]
        public static void ValidateNistP256Sha384()
        {
            byte[] msg = (
                "e0b8596b375f3306bbc6e77a0b42f7469d7e83635990e74aa6d713594a3a2449" +
                "8feff5006790742d9c2e9b47d714bee932435db747c6e733e3d8de41f2f91311" +
                "f2e9fd8e025651631ffd84f66732d3473fbd1627e63dc7194048ebec93c95c15" +
                "9b5039ab5e79e42c80b484a943f125de3da1e04e5bf9c16671ad55a1117d3306"
                ).HexToByteArray();

            ECParameters parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = "e0e7b99bc62d8dd67883e39ed9fa0657789c5ff556cc1fd8dd1e2a55e9e3f243".HexToByteArray(),
                    Y = "63fbfd0232b95578075c903a4dbf85ad58f8350516e1ec89b0ee1f5e1362da69".HexToByteArray(),
                },
                D = "b6faf2c8922235c589c27368a3b3e6e2f42eb6073bf9507f19eed0746c79dced".HexToByteArray(),
            };

            byte[] signature = (
                // r
                "f5087878e212b703578f5c66f434883f3ef414dc23e2e8d8ab6a8d159ed5ad83" +
                // s
                "306b4c6c20213707982dffbb30fba99b96e792163dd59dbe606e734328dd7c8a"
                ).HexToByteArray();

            Validate(
                parameters,
                ECDsaTestData.GetNistP256ExplicitCurve(),
                msg,
                signature,
                HashAlgorithmName.SHA384);
        }

        [Fact]
        public static void ValidateNistP384Sha256()
        {
            byte[] msg = (
                "663b12ebf44b7ed3872b385477381f4b11adeb0aec9e0e2478776313d536376d" +
                "c8fd5f3c715bb6ddf32c01ee1d6f8b731785732c0d8441df636d8145577e7b31" +
                "38e43c32a61bc1242e0e73d62d624cdc924856076bdbbf1ec04ad4420732ef0c" +
                "53d42479a08235fcfc4db4d869c4eb2828c73928cdc3e3758362d1b770809997"
                ).HexToByteArray();

            ECParameters parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP384,
                Q = new ECPoint
                {
                    X = ("0400193b21f07cd059826e9453d3e96dd145041c97d49ff6b7047f86bb0b0439" +
                        "e909274cb9c282bfab88674c0765bc75").HexToByteArray(),
                    Y = ("f70d89c52acbc70468d2c5ae75c76d7f69b76af62dcf95e99eba5dd11adf8f42" +
                        "ec9a425b0c5ec98e2f234a926b82a147").HexToByteArray(),
                },
                D = ("c602bc74a34592c311a6569661e0832c84f7207274676cc42a89f05816263018" +
                    "4b52f0d99b855a7783c987476d7f9e6b").HexToByteArray(),
            };

            byte[] signature = (
                // r
                "b11db00cdaf53286d4483f38cd02785948477ed7ebc2ad609054551da0ab0359978c61851788aa2ec3267946d440e878" +
                // s
                "16007873c5b0604ce68112a8fee973e8e2b6e3319c683a762ff5065a076512d7c98b27e74b7887671048ac027df8cbf2"
                ).HexToByteArray();

            Validate(
                parameters,
                ECDsaTestData.GetNistP384ExplicitCurve(),
                msg,
                signature,
                HashAlgorithmName.SHA256);
        }

        [Fact]
        public static void ValidateNistP384Sha512()
        {
            byte[] msg = (
                "67d9eb88f289454d61def4764d1573db49b875cfb11e139d7eacc4b7a79d3db3" +
                "bf7208191b2b2078cbbcc974ec0da1ed5e0c10ec37f6181bf81c0f32972a125d" +
                "f64e3b3e1d838ec7da8dfe0b7fcc911e43159a79c73df5fa252b98790be511d8" +
                "a732fcbf011aacc7d45d8027d50a347703d613ceda09f650c6104c9459537c8f"
                ).HexToByteArray();

            ECParameters parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP384,
                Q = new ECPoint
                {
                    X = ("fb937e4a303617b71b6c1a25f2ac786087328a3e26bdef55e52d46ab5e69e541" +
                        "1bf9fc55f5df9994d2bf82e8f39a153e").HexToByteArray(),
                    Y = ("a97d9075e92fa5bfe67e6ec18e21cc4d11fde59a68aef72c0e46a28f31a9d603" +
                        "85f41f39da468f4e6c3d3fbac9046765").HexToByteArray(),
                },
                D = ("217afba406d8ab32ee07b0f27eef789fc201d121ffab76c8fbe3c2d352c59490" +
                     "9abe591c6f86233992362c9d631baf7c").HexToByteArray(),
            };

            byte[] signature = (
                // r
                "c269d9c4619aafdf5f4b3100211dddb14693abe25551e04f9499c91152a296d7449c08b36f87d1e16e8e15fee4a7f5c8" +
                // s
                "77ffed5c61665152d52161dc13ac3fbae5786928a3d736f42d34a9e4d6d4a70a02d5af90fa37a23a318902ae2656c071"
                ).HexToByteArray();

            Validate(
                parameters,
                ECDsaTestData.GetNistP384ExplicitCurve(),
                msg,
                signature,
                HashAlgorithmName.SHA512);
        }

        [Fact]
        public static void ValidateNistP521Sha384()
        {
            byte[] msg = (
                "dbc094402c5b559d53168c6f0c550d827499c6fb2186ae2db15b89b4e6f46220" +
                "386d6f01bebde91b6ceb3ec7b4696e2cbfd14894dd0b7d656d23396ce920044f" +
                "9ca514bf115cf98ecaa55b950a9e49365c2f3a05be5020e93db92c3743751304" +
                "4973e792af814d0ffad2c8ecc89ae4b35ccb19318f0b988a7d33ec5a4fe85dfe"
                ).HexToByteArray();

            ECParameters parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,
                Q = new ECPoint
                {
                    X = ("013b4ab7bc1ddf7fd74ca6f75ac560c94169f435361e74eba1f8e759ac70ab3af1" +
                        "38d8807aca3d8e73b5c2eb787f6dcca2718122bd94f08943a686b115d869d3f406").HexToByteArray(),
                    Y = ("00f293c1d627b44e7954d0546270665888144a94d437679d074787959d0d944d82" +
                        "23b9d4b5d068b4fbbd1176a004b476810475cd2a200b83eccd226d08b444a71e71").HexToByteArray(),
                },
                D = ("0095976d387d814e68aeb09abecdbf4228db7232cd3229569ade537f33e07ed0da" +
                    "0abdee84ab057c9a00049f45250e2719d1ecaccf91c0e6fcdd4016b75bdd98a950").HexToByteArray(),
            };

            byte[] signature = (
                // r
                "002128f77df66d16a604ffcd1a515e039d49bf6b91a215b814b2a1c88d32039521" +
                "fbd142f717817b838450229025670d99c1fd5ab18bd965f093cae7accff0675aae" +
                // s
                "0008dc65a243700a84619dce14e44ea8557e36631db1a55de15865497dbfd66e76" +
                "a7471f78e510c04e613ced332aa563432a1017da8b81c146059ccc7930153103a6"
                ).HexToByteArray();

            Validate(
                parameters,
                ECDsaTestData.GetNistP521ExplicitCurve(),
                msg,
                signature,
                HashAlgorithmName.SHA384);
        }

        [Fact]
        public static void ValidateNistP521Sha512()
        {
            byte[] msg = (
                "9ecd500c60e701404922e58ab20cc002651fdee7cbc9336adda33e4c1088fab1" +
                "964ecb7904dc6856865d6c8e15041ccf2d5ac302e99d346ff2f686531d255216" +
                "78d4fd3f76bbf2c893d246cb4d7693792fe18172108146853103a51f824acc62" +
                "1cb7311d2463c3361ea707254f2b052bc22cb8012873dcbb95bf1a5cc53ab89f"
                ).HexToByteArray();

            ECParameters parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,
                Q = new ECPoint
                {
                    X = ("0061387fd6b95914e885f912edfbb5fb274655027f216c4091ca83e19336740fd8" +
                        "1aedfe047f51b42bdf68161121013e0d55b117a14e4303f926c8debb77a7fdaad1").HexToByteArray(),
                    Y = ("00e7d0c75c38626e895ca21526b9f9fdf84dcecb93f2b233390550d2b1463b7ee3" +
                        "f58df7346435ff0434199583c97c665a97f12f706f2357da4b40288def888e59e6").HexToByteArray(),
                },
                D = ("00f749d32704bc533ca82cef0acf103d8f4fba67f08d2678e515ed7db886267ffa" +
                    "f02fab0080dca2359b72f574ccc29a0f218c8655c0cccf9fee6c5e567aa14cb926").HexToByteArray(),
            };

            byte[] signature = (
                // r
                "004de826ea704ad10bc0f7538af8a3843f284f55c8b946af9235af5af74f2b76e0" +
                "99e4bc72fd79d28a380f8d4b4c919ac290d248c37983ba05aea42e2dd79fdd33e8" +
                // s
                "0087488c859a96fea266ea13bf6d114c429b163be97a57559086edb64aed4a1859" +
                "4b46fb9efc7fd25d8b2de8f09ca0587f54bd287299f47b2ff124aac566e8ee3b43"
                ).HexToByteArray();

            Validate(
                parameters,
                ECDsaTestData.GetNistP521ExplicitCurve(),
                msg,
                signature,
                HashAlgorithmName.SHA512);
        }

        private static void Validate(
            ECParameters parameters,
            ECCurve explicitCurve,
            byte[] msg,
            byte[] signature,
            HashAlgorithmName hashAlgorithm)
        {
            byte[] tamperedSignature = (byte[])signature.Clone();
            tamperedSignature[0] ^= 0xFF;

            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                ecdsa.ImportParameters(parameters);

                Assert.True(
                    ecdsa.VerifyData(msg, signature, hashAlgorithm),
                    "named verifies signature");

                Assert.False(
                    ecdsa.VerifyData(msg, tamperedSignature, hashAlgorithm),
                    "named verifies tampered");
            }

            if (ECDsaFactory.ExplicitCurvesSupported)
            {
                using (ECDsa ecdsa = ECDsaFactory.Create())
                {
                    parameters.Curve = explicitCurve;
                    ecdsa.ImportParameters(parameters);

                    Assert.True(
                        ecdsa.VerifyData(msg, signature, hashAlgorithm),
                        "explicit verifies signature");

                    Assert.False(
                        ecdsa.VerifyData(msg, tamperedSignature, hashAlgorithm),
                        "explicit verifies tampered");
                }
            }
        }
#endif // netcoreapp
    }
}
