// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    /// <summary>
    /// Input and helper methods for ECDsa
    /// </summary>
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class ECDsaTestsBase : EccTestBase
    {
#if NETCOREAPP
        internal static void Verify256(ECDsa e, bool expected)
        {
            byte[] sig = ("998791331eb2e1f4259297f5d9cb82fa20dec98e1cb0900e6b8f014a406c3d02cbdbf5238bde471c3155fc25565524301429"
                        + "d8713dad9a67eb0a5c355e9e23dc").HexToByteArray();
            bool verified = e.VerifyHash(EccTestData.s_hashSha512, sig);
            Assert.Equal(expected, verified);
        }

        // On CentOS, secp224r1 (also called nistP224) appears to be disabled. To prevent test failures on that platform,
        // probe for this capability before depending on it.
        internal static bool ECDsa224Available
        {
            get
            {
                return ECDsaFactory.IsCurveValid(new Oid(ECDSA_P224_OID_VALUE));
            }
        }

        internal static bool ECExplicitCurvesSupported
        {
            get
            {
                return ECDsaFactory.ExplicitCurvesSupported;
            }
        }
#endif
    }

    internal static class EcDsaTestExtensions
    {
        internal static void Exercise(this ECDsa e)
        {
            // Make a few calls on this to ensure we aren't broken due to bad/prematurely released handles.

            int keySize = e.KeySize;

            byte[] data = new byte[0x10];
            byte[] sig = e.SignData(data, 0, data.Length, HashAlgorithmName.SHA1);
            bool verified = e.VerifyData(data, sig, HashAlgorithmName.SHA1);
            Assert.True(verified);

            unchecked
            {
                sig[sig.Length - 1]++;
            }
            verified = e.VerifyData(data, sig, HashAlgorithmName.SHA1);
            Assert.False(verified);
        }
    }
}
