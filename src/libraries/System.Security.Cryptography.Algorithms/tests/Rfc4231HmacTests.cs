// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class Rfc4231HmacTests : HmacTests
    {
        private static readonly byte[][] s_testKeys4231 =
        {
            null,
            ByteUtils.RepeatByte(0x0b, 20),
            ByteUtils.AsciiBytes("Jefe"),
            ByteUtils.RepeatByte(0xaa, 20),
            ByteUtils.HexToByteArray("0102030405060708090a0b0c0d0e0f10111213141516171819"),
            ByteUtils.HexToByteArray("0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c"),
            ByteUtils.RepeatByte(0xaa, 131),
            ByteUtils.RepeatByte(0xaa, 131),
        };

        private static readonly byte[][] s_testData4231 =
        {
            null,
            ByteUtils.AsciiBytes("Hi There"),
            ByteUtils.AsciiBytes("what do ya want for nothing?"),
            ByteUtils.RepeatByte(0xdd, 50),
            ByteUtils.RepeatByte(0xcd, 50),
            ByteUtils.AsciiBytes("Test With Truncation"),
            ByteUtils.AsciiBytes("Test Using Larger Than Block-Size Key - Hash Key First"),
            ByteUtils.AsciiBytes("This is a test using a larger than block-size key and a larger than block-size data. The key needs to be hashed before being used by the HMAC algorithm."),
        };

        protected Rfc4231HmacTests() :
            base(s_testKeys4231, s_testData4231)
        {
        }
    }
}
