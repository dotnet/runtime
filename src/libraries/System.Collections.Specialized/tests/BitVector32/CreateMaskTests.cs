// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CreateMaskTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            int data = 0;
            int mask = 0;

            // [] Mask is constructed as expected
            //-----------------------------------------------------------------

            mask = BitVector32.CreateMask();
            if (mask != 1)
            {
                Assert.False(true, string.Format("Error, Created Mask: {0} , expected: {1}", mask, 1));
            }

            mask = BitVector32.CreateMask();
            int cnt = 50;
            int mask1 = 0;
            for (int i = 0; i < cnt; i++)
            {
                mask1 = BitVector32.CreateMask();
                if (mask1 != mask)
                {
                    string err = "Error" + (i + 1).ToString();
                    Assert.False(true, string.Format(err + ", Created Mask: {0} , expected: {1}", mask1, mask));
                }
            }

            // [] apply first Mask 
            //-----------------------------------------------------------------

            data = 0;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, data));
            }
            mask = BitVector32.CreateMask();
            if (mask != 1)
            {
                Assert.False(true, string.Format("Error, Created Mask: {0} , expected: {1}", mask, 1));
            }

            if ((bv32.Data & mask) != 0)
            {
                Assert.False(true, string.Format("Error, {0} & {1} returned {2}, expected {3}", bv32.Data, mask, (bv32.Data & mask), 0));
            }
            if ((bv32.Data | mask) != 1)
            {
                Assert.False(true, string.Format("Error, {0} | {1} returned {2}, expected {3}", bv32.Data, mask, (bv32.Data | mask), 1));
            }

            data = 1;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, data));
            }

            if ((bv32.Data & mask) != 1)
            {
                Assert.False(true, string.Format("Error, {0} & {1} returned {2}, expected {3}", bv32.Data, mask, (bv32.Data & mask), 1));
            }
            if ((bv32.Data | mask) != 1)
            {
                Assert.False(true, string.Format("Error, {0} | {1} returned {2}, expected {3}", bv32.Data, mask, (bv32.Data | mask), 1));
            }
        }
    }
}