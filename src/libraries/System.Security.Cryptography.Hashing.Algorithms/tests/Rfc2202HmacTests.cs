// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public abstract class Rfc2202HmacTests : HmacTests
    {
        private static readonly byte[][] s_testKeys2202 =
        {
            null,
            ByteUtils.RepeatByte(0x0b, 20),
            ByteUtils.AsciiBytes("Jefe"),
            ByteUtils.RepeatByte(0xaa, 20),
            ByteUtils.HexToByteArray("0102030405060708090a0b0c0d0e0f10111213141516171819"),
            ByteUtils.HexToByteArray("0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c"),
            ByteUtils.RepeatByte(0xaa, 80),
            ByteUtils.RepeatByte(0xaa, 80),
        };

        private static readonly byte[][] s_testData2202 =
        {
            null,
            ByteUtils.AsciiBytes("Hi There"),
            ByteUtils.AsciiBytes("what do ya want for nothing?"),
            ByteUtils.RepeatByte(0xdd, 50),
            ByteUtils.RepeatByte(0xcd, 50),
            ByteUtils.AsciiBytes("Test With Truncation"),
            ByteUtils.AsciiBytes("Test Using Larger Than Block-Size Key - Hash Key First"),
            ByteUtils.AsciiBytes("Test Using Larger Than Block-Size Key and Larger Than One Block-Size Data"),
        };

        protected Rfc2202HmacTests() :
            base(s_testKeys2202, s_testData2202)
        {
        }
    }
}