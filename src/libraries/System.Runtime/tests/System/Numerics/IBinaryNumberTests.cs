// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Numerics.Tests
{

    public class IBinaryNumberTests
    {

        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal(unchecked((int)0xFFFF_FFFF), BinaryNumberHelper<BinaryNumberDimHelper>.AllBitsSet.value);
            Assert.Equal(0, ~BinaryNumberHelper<BinaryNumberDimHelper>.AllBitsSet.value);
        }
    }
}
