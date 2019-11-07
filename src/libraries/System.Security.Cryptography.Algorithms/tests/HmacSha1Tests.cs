// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public class HmacSha1Tests : Rfc2202HmacTests
    {
        private static readonly byte[][] s_testKeys2202 =
        {
            null,
            ByteUtils.RepeatByte(0x0b, 20),
            ByteUtils.AsciiBytes("Jefe"),
            ByteUtils.RepeatByte(0xaa, 20),
            ByteUtils.HexToByteArray("0102030405060708090a0b0c0d0e0f10111213141516171819"),
            ByteUtils.RepeatByte(0x0c, 20),
            ByteUtils.RepeatByte(0xaa, 80),
            ByteUtils.RepeatByte(0xaa, 80),
        };

        public HmacSha1Tests()
            : base(s_testKeys2202)
        {
        }

        protected override HMAC Create()
        {
            return new HMACSHA1();
        }

        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return SHA1.Create();
        }

        protected override int BlockSize { get { return 64; } }

        [Fact]
        public void HmacSha1_Byte_Constructors()
        {
            byte[] key = (byte[])s_testKeys2202[1].Clone();
            string digest = "b617318655057264e28bc0b6fb378c8ef146be00";

            using (HMACSHA1 h1 = new HMACSHA1(key))
            {
                VerifyHmac_KeyAlreadySet(h1, 1, digest);
                using (HMACSHA1 h2 = new HMACSHA1(key, true))
                {
                    VerifyHmac_KeyAlreadySet(h2, 1, digest);
                    Assert.Equal(h1.Key, h2.Key);
                }
                using (HMACSHA1 h2 = new HMACSHA1(key, false))
                {
                    VerifyHmac_KeyAlreadySet(h1, 1, digest);
                    Assert.Equal(h1.Key, h2.Key);
                }
            }
        }

        [Fact]
        public void HmacSha1_Rfc2202_1()
        {
            VerifyHmac(1, "b617318655057264e28bc0b6fb378c8ef146be00");
        }

        [Fact]
        public void HmacSha1_Rfc2202_2()
        {
            VerifyHmac(2, "effcdf6ae5eb2fa2d27416d5f184df9c259a7c79");
        }

        [Fact]
        public void HmacSha1_Rfc2202_3()
        {
            VerifyHmac(3, "125d7342b9ac11cd91a39af48aa17b4f63f175d3");
        }

        [Fact]
        public void HmacSha1_Rfc2202_4()
        {
            VerifyHmac(4, "4c9007f4026250c6bc8414f9bf50c86c2d7235da");
        }

        [Fact]
        public void HmacSha1_Rfc2202_5()
        {
            VerifyHmac(5, "4c1a03424b55e07fe7f27be1d58bb9324a9a5a04");
        }

        [Fact]
        public void HmacSha1_Rfc2202_6()
        {
            VerifyHmac(6, "aa4ae5e15272d00e95705637ce8a3b55ed402112");
        }

        [Fact]
        public void HmacSha1_Rfc2202_7()
        {
            VerifyHmac(7, "e8e99d0f45237d786d6bbaa7965c7808bbff1a91");
        }

        [Fact]
        public void HMacSha1_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }
    }
}
