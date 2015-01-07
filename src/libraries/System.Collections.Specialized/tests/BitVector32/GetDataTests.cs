// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

//  
// test for read-only Data property
//

namespace System.Collections.Specialized.Tests
{
    public class GetDataTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            int data = 0;

            // [] BitVector is constructed as expected
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            if (bv32.Data != 0)
            {
                Assert.False(true, string.Format("Error, Data = {0} after default ctor", bv32.Data));
            }


            data = 0;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = 1;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = -1;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = 2;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = 10;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = 99;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = -9;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = System.Int32.MinValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = System.Int32.MaxValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = System.Int32.MinValue / 2;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            data = System.Int32.MaxValue / 2;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            // generate random data value
            data = -55;
            System.Random random = new System.Random(data);
            data = random.Next(System.Int32.MinValue, System.Int32.MaxValue);

            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            // [] Access data via mask and Section
            //-----------------------------------------------------------------

            data = 7;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            int mask = BitVector32.CreateMask();
            if ((bv32.Data & mask) != mask)
            {
                Assert.False(true, string.Format("Error, {0} & {1} != {1}", bv32.Data, mask));
            }

            BitVector32.Section sect = BitVector32.CreateSection(1);
            if ((bv32[sect]) != 1)
            {
                Assert.False(true, string.Format("Error, bv32[{0}] != {1}", sect, 1));
            }

            bv32[sect] = 0;
            if (bv32.Data != 6)
            {
                Assert.False(true, string.Format("Error, Data: {0} != {1}", bv32.Data, 6));
            }


            sect = BitVector32.CreateSection(5);
            if ((bv32[sect]) != bv32.Data)
            {
                Assert.False(true, string.Format("Error, bv32[{0}] != {1}", sect, bv32.Data));
            }

            bv32[sect] = 0;
            if (bv32.Data != 0)
            {
                Assert.False(true, string.Format("Error, Data: {0} != {1}", bv32.Data, 0));
            }

            data = -1;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bv32.Data, data));
            }

            sect = BitVector32.CreateSection(1);
            sect = BitVector32.CreateSection(Int16.MaxValue, sect);
            sect = BitVector32.CreateSection(Int16.MaxValue, sect);
            sect = BitVector32.CreateSection(1, sect);
            bv32[sect] = 0;
            if (bv32.Data != Int32.MaxValue)
            {
                Assert.False(true, string.Format("Error, Data: {0} != {1}", bv32.Data, Int32.MaxValue));
            }
        }
    }
}