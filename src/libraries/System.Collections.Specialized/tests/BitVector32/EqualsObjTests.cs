// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class EqualsObjTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            BitVector32 bv32_1;       // extra BitVector32 - for comparison  
            int data = 0;

            // [] two BitVectors that are the same - expected true
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            bv32_1 = new BitVector32();
            if (!bv32.Equals(bv32_1))
            {
                Assert.False(true, string.Format("Error, two default structs are not equal"));
            }


            // generate random data value
            data = -55;
            System.Random random = new System.Random(data);
            data = random.Next(System.Int32.MinValue, System.Int32.MaxValue);

            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            if (!bv32.Equals(bv32_1))
            {
                Assert.False(true, string.Format("Error, two vectores with random data are not equal"));
            }

            if (bv32.Equals(null))
            {
                Assert.False(true, string.Format("Error, vector and null are equal"));
            }

            bv32 = new BitVector32(data);
            if (data < Int32.MaxValue)
                data++;
            else
                data--;
            bv32_1 = new BitVector32(data);
            if (bv32.Equals(bv32_1))
            {
                Assert.False(true, string.Format("Error, two different vectors are equal"));
            }

            bv32 = new BitVector32(data);
            if (bv32.Equals(data))
            {
                Assert.False(true, string.Format("Error, vector and non-vector-object are equal"));
            }
        }
    }
}