// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public class BlockSizeValueTests
    {
        public static IEnumerable<object[]> GetBlockSizeValue()
        {
            return new[]
            {
                new object[] { new HMACMD5Test().GetBlockSizeValue(),    64  },
                new object[] { new HMACSHA1Test().GetBlockSizeValue(),   64  },
                new object[] { new HMACSHA256Test().GetBlockSizeValue(), 64  },
                new object[] { new HMACSHA384Test().GetBlockSizeValue(), 128 },
                new object[] { new HMACSHA512Test().GetBlockSizeValue(), 128 },
            };
        }
        
        [Theory]
        [MemberData(nameof(GetBlockSizeValue))]
        public static void BlockSizeValueTest(int hmacBlockSizeValue, int expectedBlockSizeValue)
        {
            Assert.Equal(expectedBlockSizeValue, hmacBlockSizeValue);
        }
    }

    public class HMACMD5Test : HMACMD5 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA1Test : HMACSHA1 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA256Test : HMACSHA256 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA384Test : HMACSHA384 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
    public class HMACSHA512Test : HMACSHA512 { public int GetBlockSizeValue() { Dispose(); return BlockSizeValue; } }
}
