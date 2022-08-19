// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public sealed class IBinaryNumberTests
    {
        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal(unchecked((int)0xFFFF_FFFF), BinaryNumberHelper<BinaryNumberDimHelper>.AllBitsSet.Value);
            Assert.Equal(0, ~BinaryNumberHelper<BinaryNumberDimHelper>.AllBitsSet.Value);
        }
    }
}
