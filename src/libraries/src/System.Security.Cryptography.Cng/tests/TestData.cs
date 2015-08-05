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
    }
}
