// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Security.Cryptography.Rsa.Tests
{
    // Note to contributors:
    //   Keys contained in this file should be randomly generated for the purpose of inclusion here,
    //   or obtained from some fixed set of test data. (Please) DO NOT use any key that has ever been
    //   used for any real purpose.
    //
    // Note to readers:
    //   The keys contained in this file should all be treated as compromised. That means that you
    //   absolutely SHOULD NOT use these keys on anything that you actually want to be protected.
    internal class TestData
    {
        public static readonly RSAParameters CspTestKey = new RSAParameters
        {
            Modulus = ByteUtils.HexToByteArray("e06aac9ec3e98bae9ebaf921eb7898f34a58a6a4f6370a9d767cd1f0492e7969b4defdb11b1795a63fefb3359b55c392ecb22f5791e1d925ea5cf74bc5094ddc0164ebb021028423151c7641181940112b0d46e9562d3cec8364d58be8d9c84910e196fa458f633cf6431354df98b773c32c0cf6d18147222a96824b64019ae1"),
            Exponent = ByteUtils.HexToByteArray("010001"),
            D = ByteUtils.HexToByteArray("143d3aa534e8feaa7871475faa435d93ef7c104767572e736608bacc4f654c18def18f72a60d59f73ce3eac72663b5382e75a17465d93702c6e0ac82de59c8f627b01b1bc02b0aa98925b4a010d2c5c563544daeabf148997d8d016b63fa3ce05b3788c5ae9ba0d5ea9b990804f40ac7ebbe62f8b9b884c154a0f8628b00ac43"),
            P = ByteUtils.HexToByteArray("e1aab100887245692770c5059cf3b6f2dabb83b015c61a229806e298a79bd360609d4b5894a1c231c9b47fd7b7a4f1a44b3870acf80373b484e5296e9f3ab47b"),
            DP = ByteUtils.HexToByteArray("4966e0fe0063d2e9fa37370eb5579ca96fb6508644fed3df6ebdc694cae7e7a050acb9264dea33a5482b9aedcac12f0c369f5c1f16e8e088d63547fdc07332e3"),
            Q = ByteUtils.HexToByteArray("fe94f7aa687940d862b0f6f44165656bf81acc5790a9d065624dd0f9d239d39e77d5c3038a317593ce7b24f31e76ce2654ca3cccf878a12088ae8d87b5111553"),
            DQ = ByteUtils.HexToByteArray("6c501eeb1e95f013e03160705d5e717f3548d985abe3c3e94ea0c2f7770ce94f33b6fbc886c4323d178d671414f3011467e0bf6b898f71263160ea9041662a47"),
            InverseQ = ByteUtils.HexToByteArray("97d9b81076a4b08a1427168b3deacfb3d65d2a5ce23e098671cd1150882161f3911b60e02f6ebbc9a5009d06ef50f2c51ed2da8f787c5b7d63bc7bc0fe1cf75a"),
       };
    }
}
