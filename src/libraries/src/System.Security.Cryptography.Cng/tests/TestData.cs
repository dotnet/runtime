// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
