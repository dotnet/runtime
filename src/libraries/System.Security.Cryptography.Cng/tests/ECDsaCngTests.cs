// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class EcDSACngTests
    {
        [Fact]
        public static void TestPositiveVerify256()
        {
            CngKey key = TestData.s_ECDsa256Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] sig = ("998791331eb2e1f4259297f5d9cb82fa20dec98e1cb0900e6b8f014a406c3d02cbdbf5238bde471c3155fc25565524301429"
                        + "d8713dad9a67eb0a5c355e9e23dc").HexToByteArray();
            bool verified = e.VerifyHash(TestData.s_hashSha512, sig);
            Assert.True(verified);
        }

        [Fact]
        public static void TestNegativeVerify256()
        {
            CngKey key = TestData.s_ECDsa256Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] tamperedSig = ("998791331eb2e1f4259297f5d9cb82fa20dec98e1cb0900e6b8f014a406c3d02cbdbf5238bde471c3155fc25565524301429"
                                + "e8713dad9a67eb0a5c355e9e23dc").HexToByteArray();
            bool verified = e.VerifyHash(TestData.s_hashSha512, tamperedSig);
            Assert.False(verified);
        }

        [Fact]
        public static void TestPositiveVerify384()
        {
            CngKey key = TestData.s_ECDsa384Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] sig = ("7805c494b17bba8cba09d3e5cdd16d69ce785e56c4f2d9d9061d549fce0a6860cca1cb9326bd534da21ad4ff326a1e0810d8"
                        + "2366eb6afc66ede0d1ffe345f6b37ac622ed77838b42825ceb96cd3996d3d77fd6a248357ae1ae6cb85f048b1b04").HexToByteArray();
            bool verified = e.VerifyHash(TestData.s_hashSha512, sig);
            Assert.True(verified);
        }

        [Fact]
        public static void TestNegativeVerify384()
        {
            CngKey key = TestData.s_ECDsa384Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] tamperedSig = ("7805c494b17bba8cba09d3e5cdd16d69ce785e56c4f2d9d9061d549fce0a6860cca1cb9326bd534da21ad4ff326a1e0810d8"
                                + "f366eb6afc66ede0d1ffe345f6b37ac622ed77838b42825ceb96cd3996d3d77fd6a248357ae1ae6cb85f048b1b04").HexToByteArray();
            bool verified = e.VerifyHash(TestData.s_hashSha512, tamperedSig);
            Assert.False(verified);
        }

        [Fact]
        public static void TestPositiveVerify521()
        {
            CngKey key = TestData.s_ECDsa521Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] sig = ("0084461450745672df85735fbf89f2dccef804d6b56e86ca45ea5c366a05a5de96327eddb75582821c6315c8bb823c875845"
                        + "b6f25963ddab70461b786261507f971401fdc300697824129e0a84e0ba1ab4820ac7b29e7f8248bc2e29d152a9190eb3fcb7"
                        + "6e8ebf1aa5dd28ffd582a24cbfebb3426a5f933ce1d995b31c951103d24f6256").HexToByteArray();
            bool verified = e.VerifyHash(TestData.s_hashSha512, sig);
            Assert.True(verified);
        }

        [Fact]
        public static void TestNegativeVerify521()
        {
            CngKey key = TestData.s_ECDsa521Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] tamperedSig = ("0084461450745672df85735fbf89f2dccef804d6b56e86ca45ea5c366a05a5de96327eddb75582821c6315c8bb823c875845"
                                + "a6f25963ddab70461b786261507f971401fdc300697824129e0a84e0ba1ab4820ac7b29e7f8248bc2e29d152a9190eb3fcb7"
                                + "6e8ebf1aa5dd28ffd582a24cbfebb3426a5f933ce1d995b31c951103d24f6256").HexToByteArray();
            bool verified = e.VerifyHash(TestData.s_hashSha512, tamperedSig);
            Assert.False(verified);
        }
    }
}
