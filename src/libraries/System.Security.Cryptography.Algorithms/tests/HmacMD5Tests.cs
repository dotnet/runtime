// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public class HmacMD5Tests : Rfc2202HmacTests
    {
        private static readonly byte[][] s_testKeys2202 =
        {
            null,
            ByteUtils.RepeatByte(0x0b, 16),
            ByteUtils.AsciiBytes("Jefe"),
            ByteUtils.RepeatByte(0xaa, 16),
            ByteUtils.HexToByteArray("0102030405060708090a0b0c0d0e0f10111213141516171819"),
            ByteUtils.RepeatByte(0x0c, 16),
            ByteUtils.RepeatByte(0xaa, 80),
            ByteUtils.RepeatByte(0xaa, 80),
        };

        public HmacMD5Tests()
            : base(s_testKeys2202)
        {
        }

        protected override HMAC Create()
        {
            return new HMACMD5();
        }

        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return MD5.Create();
        }

        protected override int BlockSize { get { return 64; } }

        [Fact]
        public void HmacMD5_Rfc2202_1()
        {
            VerifyHmac(1, "9294727a3638bb1c13f48ef8158bfc9d");
        }

        [Fact]
        public void HmacMD5_Rfc2202_2()
        {
            VerifyHmac(2, "750c783e6ab0b503eaa86e310a5db738");
        }

        [Fact]
        public void HmacMD5_Rfc2202_3()
        {
            VerifyHmac(3, "56be34521d144c88dbb8c733f0e8b3f6");
        }

        [Fact]
        public void HmacMD5_Rfc2202_4()
        {
            VerifyHmac(4, "697eaf0aca3a3aea3a75164746ffaa79");
        }

        [Fact]
        public void HmacMD5_Rfc2202_5()
        {
            VerifyHmac(5, "56461ef2342edc00f9bab995690efd4c");
        }

        [Fact]
        public void HmacMD5_Rfc2202_6()
        {
            VerifyHmac(6, "6b1ab7fe4bd7bf8f0b62e6ce61b9d0cd");
        }

        [Fact]
        public void HmacMD5_Rfc2202_7()
        {
            VerifyHmac(7, "6f630fad67cda0ee1fb1f562db3aa53e");
        }

        [Fact]
        public void HMacMD5_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }
    }
}
