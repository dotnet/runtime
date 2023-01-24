// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class BlockSizeValueTests
    {
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void BlockSizeValueTest_HMACMD5()
        {
            int hmacBlockSizeValue = new HMACMD5Test().GetBlockSizeValue();
            const int ExpectedBlockSize = 64;
            Assert.Equal(ExpectedBlockSize, hmacBlockSizeValue);
        }

        [Fact]
        public static void BlockSizeValueTest_HMACSHA1()
        {
            int hmacBlockSizeValue = new HMACSHA1Test().GetBlockSizeValue();
            const int ExpectedBlockSize = 64;
            Assert.Equal(ExpectedBlockSize, hmacBlockSizeValue);
        }

        [Fact]
        public static void BlockSizeValueTest_HMACSHA256()
        {
            int hmacBlockSizeValue = new HMACSHA256Test().GetBlockSizeValue();
            const int ExpectedBlockSize = 64;
            Assert.Equal(ExpectedBlockSize, hmacBlockSizeValue);
        }

        [Fact]
        public static void BlockSizeValueTest_HMACSHA384()
        {
            int hmacBlockSizeValue = new HMACSHA384Test().GetBlockSizeValue();
            const int ExpectedBlockSize = 128;
            Assert.Equal(ExpectedBlockSize, hmacBlockSizeValue);
        }

        [Fact]
        public static void BlockSizeValueTest_HMACSHA512()
        {
            int hmacBlockSizeValue = new HMACSHA512Test().GetBlockSizeValue();
            const int ExpectedBlockSize = 128;
            Assert.Equal(ExpectedBlockSize, hmacBlockSizeValue);
        }
    }

    public class HMACMD5Test : HMACMD5 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA1Test : HMACSHA1 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA256Test : HMACSHA256 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA384Test : HMACSHA384 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA512Test : HMACSHA512 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
}
