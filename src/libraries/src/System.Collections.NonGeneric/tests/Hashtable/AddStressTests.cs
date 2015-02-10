// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.HashtableTests
{
    public partial class AddStressTests
    {
        [Fact]
        public void TestAddRemoveLargeAmountNumbers()
        {
            Hashtable ht = new Hashtable();

            //[] Read all of the doubles from the file and store them into the hashtable
            int count = 0;
            foreach (var number in s_AddStressInputData)
            {
                ht.Add(number, count++);
            }

            //[] Read all of the doubles from the file and make sure they exist in the hashtable hashtable
            count = 0;
            foreach (var tempLong in s_AddStressInputData)
            {
                Assert.Equal((int)ht[tempLong], count);
                Assert.True(ht.ContainsKey(tempLong));

                ++count;
            }

            //[] Remove all of the entries
            foreach (var tempLong in s_AddStressInputData)
            {
                ht.Remove(tempLong);
            }

            Assert.Equal(0, ht.Count);
        }
    }
}
