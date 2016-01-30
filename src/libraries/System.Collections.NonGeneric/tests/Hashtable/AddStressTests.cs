// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public partial class HashTable_AddStressTests
    {
        [Fact]
        [OuterLoop]
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
