// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class Rfc2202HmacTests : HmacTests
    {
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

        // The keys for test cases 1, 3, and 5 for RFC2202 are sized to match the
        // algorithm (16 bytes for MD5, 20 for SHA-1), so they need to be provided by
        // the more derived type.
        protected Rfc2202HmacTests(byte[][] testKeys, byte[][] testMacs) :
            base(testKeys, s_testData2202, testMacs)
        {
        }
    }
}
