// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
#if NETCOREAPP
    // These test cases are from http://csrc.nist.gov/groups/STM/cavp/component-testing.html#test-vectors
    // SP 800-56A ECCCDH Primitive test vectors
    // ecccdhtestvectors.zip
    // KAS_ECC_CDH_PrimitiveTest.txt
    public partial class ECDiffieHellmanTests
    {
        [Fact]
        public static void ValidateNistP256_0()
        {
            Verify(
                ECCurve.NamedCurves.nistP256,
                EccTestData.GetNistP256ExplicitCurve(),
                "700c48f77f56584c5cc632ca65640db91b6bacce3a4df6b42ce7cc838833d287",
                "db71e509e3fd9b060ddb20ba5c51dcc5948d46fbf640dfe0441782cab85fa4ac",
                "7d7dc5f71eb29ddaf80d6214632eeae03d9058af1fb6d22ed80badb62bc1a534",
                "ead218590119e8876b29146ff89ca61770c4edbbf97d38ce385ed281d8a6b230",
                "28af61281fd35e2fa7002523acc85a429cb06ee6648325389f59edfce1405141",
                "46fc62106420ff012e54a434fbdd2d25ccc5852060561e68040dd7778997bd7b");
        }

        [Fact]
        public static void ValidateNistP256_1()
        {
            Verify(
                ECCurve.NamedCurves.nistP256,
                EccTestData.GetNistP256ExplicitCurve(),
                "809f04289c64348c01515eb03d5ce7ac1a8cb9498f5caa50197e58d43a86a7ae",
                "b29d84e811197f25eba8f5194092cb6ff440e26d4421011372461f579271cda3",
                "38f65d6dce47676044d58ce5139582d568f64bb16098d179dbab07741dd5caf5",
                "119f2f047902782ab0c9e27a54aff5eb9b964829ca99c06b02ddba95b0a3f6d0",
                "8f52b726664cac366fc98ac7a012b2682cbd962e5acb544671d41b9445704d1d",
                "057d636096cb80b67a8c038c890e887d1adfa4195e9b3ce241c8a778c59cda67");
        }

        [Fact]
        public static void ValidateNistP384_0()
        {
            Verify(
                ECCurve.NamedCurves.nistP384,
                EccTestData.GetNistP384ExplicitCurve(),
                "a7c76b970c3b5fe8b05d2838ae04ab47697b9eaf52e764592efda27fe7513272734466b400091adbf2d68c58e0c50066",
                "ac68f19f2e1cb879aed43a9969b91a0839c4c38a49749b661efedf243451915ed0905a32b060992b468c64766fc8437a",
                "3cc3122a68f0d95027ad38c067916ba0eb8c38894d22e1b15618b6818a661774ad463b205da88cf699ab4d43c9cf98a1",
                "9803807f2f6d2fd966cdd0290bd410c0190352fbec7ff6247de1302df86f25d34fe4a97bef60cff548355c015dbb3e5f",
                "ba26ca69ec2f5b5d9dad20cc9da711383a9dbe34ea3fa5a2af75b46502629ad54dd8b7d73a8abb06a3a3be47d650cc99",
                "5f9d29dc5e31a163060356213669c8ce132e22f57c9a04f40ba7fcead493b457e5621e766c40a2e3d4d6a04b25e533f1");
        }

        [Fact]
        public static void ValidateNistP384_1()
        {
            Verify(
                ECCurve.NamedCurves.nistP384,
                EccTestData.GetNistP384ExplicitCurve(),
                "30f43fcf2b6b00de53f624f1543090681839717d53c7c955d1d69efaf0349b7363acb447240101cbb3af6641ce4b88e0",
                "25e46c0c54f0162a77efcc27b6ea792002ae2ba82714299c860857a68153ab62e525ec0530d81b5aa15897981e858757",
                "92860c21bde06165f8e900c687f8ef0a05d14f290b3f07d8b3a8cc6404366e5d5119cd6d03fb12dc58e89f13df9cd783",
                "ea4018f5a307c379180bf6a62fd2ceceebeeb7d4df063a66fb838aa35243419791f7e2c9d4803c9319aa0eb03c416b66",
                "68835a91484f05ef028284df6436fb88ffebabcdd69ab0133e6735a1bcfb37203d10d340a8328a7b68770ca75878a1a6",
                "a23742a2c267d7425fda94b93f93bbcc24791ac51cd8fd501a238d40812f4cbfc59aac9520d758cf789c76300c69d2ff");
        }

        [Fact]
        public static void ValidateNistP521_0()
        {
            Verify(
                ECCurve.NamedCurves.nistP521,
                EccTestData.GetNistP521ExplicitCurve(),
                "00685a48e86c79f0f0875f7bc18d25eb5fc8c0b07e5da4f4370f3a9490340854334b1e1b87fa395464c60626124a4e70d0f785601d37c09870ebf176666877a2046d",
                "01ba52c56fc8776d9e8f5db4f0cc27636d0b741bbe05400697942e80b739884a83bde99e0f6716939e632bc8986fa18dccd443a348b6c3e522497955a4f3c302f676",
                "017eecc07ab4b329068fba65e56a1f8890aa935e57134ae0ffcce802735151f4eac6564f6ee9974c5e6887a1fefee5743ae2241bfeb95d5ce31ddcb6f9edb4d6fc47",
                "00602f9d0cf9e526b29e22381c203c48a886c2b0673033366314f1ffbcba240ba42f4ef38a76174635f91e6b4ed34275eb01c8467d05ca80315bf1a7bbd945f550a5",
                "01b7c85f26f5d4b2d7355cf6b02117659943762b6d1db5ab4f1dbc44ce7b2946eb6c7de342962893fd387d1b73d7a8672d1f236961170b7eb3579953ee5cdc88cd2d",
                "005fc70477c3e63bc3954bd0df3ea0d1f41ee21746ed95fc5e1fdf90930d5e136672d72cc770742d1711c3c3a4c334a0ad9759436a4d3c5bf6e74b9578fac148c831");
        }

        [Fact]
        public static void ValidateNistP521_1()
        {
            Verify(
                ECCurve.NamedCurves.nistP521,
                EccTestData.GetNistP521ExplicitCurve(),
                "01df277c152108349bc34d539ee0cf06b24f5d3500677b4445453ccc21409453aafb8a72a0be9ebe54d12270aa51b3ab7f316aa5e74a951c5e53f74cd95fc29aee7a",
                "013d52f33a9f3c14384d1587fa8abe7aed74bc33749ad9c570b471776422c7d4505d9b0a96b3bfac041e4c6a6990ae7f700e5b4a6640229112deafa0cd8bb0d089b0",
                "00816f19c1fb10ef94d4a1d81c156ec3d1de08b66761f03f06ee4bb9dcebbbfe1eaa1ed49a6a990838d8ed318c14d74cc872f95d05d07ad50f621ceb620cd905cfb8",
                "00d45615ed5d37fde699610a62cd43ba76bedd8f85ed31005fe00d6450fbbd101291abd96d4945a8b57bc73b3fe9f4671105309ec9b6879d0551d930dac8ba45d255",
                "01425332844e592b440c0027972ad1526431c06732df19cd46a242172d4dd67c2c8c99dfc22e49949a56cf90c6473635ce82f25b33682fb19bc33bd910ed8ce3a7fa",
                "000b3920ac830ade812c8f96805da2236e002acbbf13596a9ab254d44d0e91b6255ebf1229f366fb5a05c5884ef46032c26d42189273ca4efa4c3db6bd12a6853759");
        }

        private static void Verify(
            ECCurve namedCurve,
            ECCurve explicitCurve,
            string cavsQx,
            string cavsQy,
            string iutD,
            string iutQx,
            string iutQy,
            string iutZ)
        {
            Assert.True(namedCurve.IsNamed, "namedCurve.IsNamed");
            Assert.True(explicitCurve.IsExplicit, "explicitCurve.IsExplicit");

            ECParameters iutParameters = new ECParameters
            {
                Curve = namedCurve,
                Q =
                {
                    X = iutQx.HexToByteArray(),
                    Y = iutQy.HexToByteArray(),
                },
                D = iutD.HexToByteArray(),
            };

            ECParameters cavsParameters = new ECParameters
            {
                Curve = namedCurve,
                Q =
                {
                    X = cavsQx.HexToByteArray(),
                    Y = cavsQy.HexToByteArray(),
                },
            };

            Verify(ref iutParameters, ref cavsParameters, explicitCurve, iutZ.HexToByteArray());
        }

        private static void Verify(
            ref ECParameters iutParameters,
            ref ECParameters cavsParameters,
            ECCurve explicitCurve,
            byte[] iutZ)
        {
            using (ECDiffieHellman iut = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman cavs = ECDiffieHellmanFactory.Create())
            {
                iut.ImportParameters(iutParameters);
                cavs.ImportParameters(cavsParameters);

                using (ECDiffieHellmanPublicKey cavsPublic = cavs.PublicKey)
                using (HashAlgorithm sha256 = SHA256.Create())
                using (HashAlgorithm sha384 = SHA384.Create())
                {
                    Verify(iut, cavsPublic, sha256, HashAlgorithmName.SHA256, iutZ);
                    Verify(iut, cavsPublic, sha384, HashAlgorithmName.SHA384, iutZ);
                }

                if (ECDiffieHellmanFactory.ExplicitCurvesSupported)
                {
                    iutParameters.Curve = explicitCurve;
                    iut.ImportParameters(iutParameters);

                    // IUT is explicit, CAVS is named, but they're the same key.
                    // If this test ever starts throwing CryptographicException we can guard it.
                    // Support is entirely left up to the library.
                    // It was kind of surprising that it worked.
                    using (ECDiffieHellmanPublicKey cavsPublic = cavs.PublicKey)
                    using (HashAlgorithm sha256 = SHA256.Create())
                    using (HashAlgorithm sha512 = SHA512.Create())
                    {
                        bool trySecondCase = true;

                        try
                        {
                            Verify(iut, cavsPublic, sha256, HashAlgorithmName.SHA256, iutZ);
                        }
                        catch (CryptographicException)
                        {
                            // This is expected to work on Windows.
                            // On Linux (via OpenSSL) it is less predictable, since it fails with
                            // EVP_PKEY_derive_set_peer:different parameters if one key uses
                            // the GFp_simple routines and another uses GFp_nist or GFp_mont (or,
                            // one presumes, something custom from an HSM) even if they actually
                            // represent the same curve.
                            //
                            // secp256r1 and secp521r1 both succeed this block, secp384r1 fails.
                            if (OperatingSystem.IsWindows())
                            {
                                throw;
                            }

                            trySecondCase = false;
                        }

                        // If the first one failed, don't try the second.
                        // If the first one passed, the second should, too.
                        if (trySecondCase)
                        {
                            Verify(iut, cavsPublic, sha512, HashAlgorithmName.SHA512, iutZ);
                        }
                    }

                    cavsParameters.Curve = explicitCurve;
                    cavs.ImportParameters(cavsParameters);

                    // Explicit, explicit; over the same curve.
                    using (ECDiffieHellmanPublicKey cavsPublic = cavs.PublicKey)
                    using (HashAlgorithm sha384 = SHA384.Create())
                    using (HashAlgorithm sha512 = SHA512.Create())
                    {
                        Verify(iut, cavsPublic, sha384, HashAlgorithmName.SHA384, iutZ);
                        Verify(iut, cavsPublic, sha512, HashAlgorithmName.SHA512, iutZ);
                    }
                }
            }
        }

        private static void Verify(
            ECDiffieHellman iut,
            ECDiffieHellmanPublicKey cavsPublic,
            HashAlgorithm zHasher,
            HashAlgorithmName zHashAlgorithm,
            byte[] iutZ)
        {
            byte[] result = iut.DeriveKeyFromHash(cavsPublic, zHashAlgorithm);
            byte[] hashedZ = zHasher.ComputeHash(iutZ);
            Assert.Equal(hashedZ.ByteArrayToHex(), result.ByteArrayToHex());
        }
    }
#endif
}
