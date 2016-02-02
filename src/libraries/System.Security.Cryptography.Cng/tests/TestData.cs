// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Test.Cryptography;

namespace System.Security.Cryptography.Cng.Tests
{
    // Note to contributors:
    //   Keys contained in this file should be randomly generated for the purpose of inclusion here,
    //   or obtained from some fixed set of test data. (Please) DO NOT use any key that has ever been
    //   used for any real purpose.
    //
    // Note to readers:
    //   The keys contained in this file should all be treated as compromised. That means that you
    //   absolutely SHOULD NOT use these keys on anything that you actually want to be protected.
    internal static class TestData
    {
        // AllowExport|AllowPlainTextExport,  CngKeyCreationOptions.NOne, UIPolicy(CngUIProtectionLevels.None), CngKeyUsages.Decryption
        public static byte[] Key_ECDiffieHellmanP256 =
           ("45434b3120000000d679ed064a01dacd012d24495795d4a3272fb6f6bd3d9baf8b40c0db26a81dfb8b4919d5477a07ae5c4b"
          + "4b577f2221be085963abc7515bbbf6998919a34baefe").HexToByteArray();

        public static RSA CreateRsaCng(this RSAParameters rsaParameters)
        {
            RSA rsa = new RSACng();
            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        public static RSAParameters TestRsaKeyPair;


        public static readonly CngKey s_ECDsa256Key =
            CngKey.Import(
                ("454353322000000096e476f7473cb17c5b38684daae437277ae1efadceb380fad3d7072be2ffe5f0b54a94c2d6951f073bfc"
              + "25e7b81ac2a4c41317904929d167c3dfc99122175a9438e5fb3e7625493138d4149c9438f91a2fecc7f48f804a92b6363776"
              + "892ee134").HexToByteArray(), CngKeyBlobFormat.GenericPrivateBlob);

        public static readonly CngKey s_ECDsa384Key =
            CngKey.Import(
                ("45435334300000009dc6bb9cdc8dac31e3db6e6b5f58f8e3a304e5c08e632705ca9a236f1134646dca526b89f7ea98653962"
               + "f4a781f2fc9bf479a2d627561b1269548050e6d2c388018b837f4ceba8ee7fe2eefea67c8418ad1e84f60c1309385e573ea5"
               + "183e9ae8b6d5308a78da207c6e556af2053983321a5f8ac057b787089ee783c99093b9f2afb2f9a1e9a560ad3095b9667aa6"
               + "99fa").HexToByteArray(), CngKeyBlobFormat.GenericPrivateBlob);

        internal static readonly byte[] s_ecdsa521KeyBlob =
             ("454353364200000001f9f06ea4e00fd3fecc1753af7983b43cb9b692941ee6364616c9c4168845fce804beca7aa23d0a5049"
            + "910db45dfb61112f4cb02e93ff62af1be203ad248dd70952015ddc31d1ad7411ca5996b8b76a40ea65f286c665225114bec8"
            + "557365aa4bc79358f8c68b873cb76a1c86a5a394185d8eeb9602b8b968db1e4ac49b7cc51f83c7170055ad9b0b2d0d5d2306"
            + "a66bf87a256a3739696121eb131e64ae61991ea23db99b397c32df95efb0cb284147a929c65e9f671073ca3c7a084cb9211d"
            + "ceb06c987277").HexToByteArray();

        public static readonly CngKey s_ECDsa521Key =
            CngKey.Import(
                s_ecdsa521KeyBlob, CngKeyBlobFormat.GenericPrivateBlob);

        public static readonly byte[] s_hashSha1 = ("77dbd8b3ef3084e1cc475c8bf955a972214b5f36").HexToByteArray();
        public static readonly byte[] s_hashSha512 = 
            ("a232cec7be26319e53db0d48470232d37793b06b99e8ed82fac1996b3d1596449087769927d64af657cce62d853c4cf7ff4c"
           + "d069eda230d1c524d225756ffbaf").HexToByteArray();

        static TestData()
        {
            RSAParameters rp = new RSAParameters();
            rp.D = ("2806880f41dfba6ea9cb91f141c07e09cc0def786030162e1947c50d427d21dc5c0779ded52c50e570665884ba0ba32977c6"
                  + "3019da0d255de458c9f421f0a17cd70bc21ea1e97152d3ded5ef1f17927bf2c03f83a72534033baacc670443d4e9c80e2d87"
                  + "e206a3c3094ee5b20c3a1edf99c275f8f63cd4de7cdea326050cb151").HexToByteArray();

            rp.DP = ("0aa6fc0436a24aa03c7a4d0b4cb84b75b9475eb0410ffaaa2a2c6d4dd8d4c3a5ac815bdeb93245babef613f983e4770d63d0"
                   + "d931e33f0509019a1e431e6b5911").HexToByteArray();

            rp.DQ = ("b7944d4d4846708c33adb0ad964623ad0e55d7c5bbd6475d25b12fbb39ab8c75794fdc977d67f54833ba59acbec8f3d91ddb"
                   + "f29d0e780d52f8c656cad787fad5").HexToByteArray();

            rp.Exponent = ("010001").HexToByteArray();

            rp.InverseQ = ("8fdd8821b7fcc6e907436bc33d7311f9344ee18a3af36429c550f34f83c4c93fd0429f63bdc502db9cc03d3d857a6354e98b"
                         + "db7c76b3ab54c32cdae75c539f2c").HexToByteArray();

            rp.Modulus = ("c7b5012552672f812a015bf3356abdfe4964cfe2ae35b8aba819120c58ffa2f1fc0f512e76fd22e6d32646ceea78829a9cbb"
                        + "2dbe5c66d14390e1bcef05afbababfe1f5ca07983b1f688a01b2beef8886b05df9e9420e65a1c0dc605ccfa2e27d84b39433"
                        + "ffcd07441ef5be8ab80497bc553fce022c7620922d1d624b6e3babe1").HexToByteArray();

            rp.P = ("c7eb601fdd49b22eda5b9a5ccb2fcfc35a660bb3bd2872857c864432e32916c2231e3b3da8afddc3efa38d04f9b1a08a08ab"
                  + "08b4603ff28345ba32d24de3cfa5").HexToByteArray();

            rp.Q = ("ffba608710355472b48b41e57eadd19a3f1a5d2fc1baa3d6210520c95694f11a065a16354827abdb06a59c3616f5ff2c5ca3"
                  + "be835f1278e9a9e9f0373027b68d").HexToByteArray();

            TestRsaKeyPair = rp;
        }
    }
}
