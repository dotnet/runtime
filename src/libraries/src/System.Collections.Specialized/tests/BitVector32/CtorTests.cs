// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            BitVector32 bv32Temp;       // extra BitVector32 - for comparison

            // [] BitVector is constructed as expected
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            if (bv32.Data != 0)
            {
                Assert.False(true, string.Format("Error, Data = {0} after default ctor", bv32.Data));
            }

            string result = bv32.ToString();
            if (result.IndexOf("BitVector32") == -1)
            {  // "BitVector32" is not a part of ToString()
                Assert.False(true, "Error: ToString() doesn't contain \"BitVector32\"");
            }

            bool item = bv32[1];
            if (item)
            {
                Assert.False(true, string.Format("Error: Item(0) returned {0} instead of {1}", item, false));
            }

            bv32Temp = new BitVector32();
            if (!bv32.Equals(bv32Temp))
            {
                Assert.False(true, string.Format("Error: two default vectors are not equal"));
            }
        }
    }
}