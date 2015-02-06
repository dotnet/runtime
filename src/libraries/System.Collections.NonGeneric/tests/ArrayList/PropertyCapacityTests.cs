// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class CapacityTests
    {
        #region "Test Data - Keep the data close to tests so it can vary independently from other tests"

        static string[] strHeroes =
            {
                "Aquaman",
                "Atom",
                "Batman",
                "Black Canary",
                "Captain America",
                "Captain Atom",
                "Catwoman",
                "Cyborg",
                "Flash",
                "Green Arrow",
                "Green Lantern",
                "Hawkman",
                "Huntress",
                "Ironman",
                "Nightwing",
                "Robin",
                "SpiderMan",
                "Steel",
                "Superman",
                "Thor",
                "Wildcat",
                "Wonder Woman",
            };

        #endregion

        [Fact]
        public void TestGetBasic()
        {
            // Construct ArrayList.
            ArrayList arrList = new ArrayList((ICollection)strHeroes);
            Assert.NotNull(arrList);

            // []  Obtain list capacity.
            Assert.True(arrList.Capacity >= arrList.Count);
        }

        [Fact]
        public void TestSetBasic()
        {
            ArrayList arrList = null;

            //
            // Construct array list.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            //
            // []  Set and verify list capacity.
            //
            int nCapacity = 2 * arrList.Capacity;
            arrList.Capacity = nCapacity;
            Assert.Equal(nCapacity, arrList.Capacity);

            //
            // []  Bogus negative argument.
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => { arrList.Capacity = -1000; });

            //
            // []  Bogus super large capacity.
            //
            Assert.Throws<OutOfMemoryException>(() => { arrList.Capacity = Int32.MaxValue; });

            //[] Team Review feedback - set capacity to a value less than the count
            arrList = new ArrayList();
            for (int i = 0; i < 10; i++)
                arrList.Add(i);

            Assert.Throws<ArgumentOutOfRangeException>(() => { arrList.Capacity = arrList.Count - 1; });

            //
            // []  Set Capacity equal to 0
            //
            arrList = new ArrayList(1);

            arrList.Capacity = 0;
            Assert.Equal(4, arrList.Capacity);

            for (int i = 0; i < 32; i++)
                arrList.Add(-i);

            for (int i = 0; i < 32; i++)
                Assert.Equal(-i, (int)arrList[i]);
        }
    }
}
