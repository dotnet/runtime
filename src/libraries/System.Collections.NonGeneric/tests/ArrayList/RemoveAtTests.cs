// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class RemoveAtTests
    {
        [Fact]
        public void TestArrayListWrappers()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            int start = 3;
            int count = 15;

            string[] strHeroes =
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

            string[] strResult =
            {
                "Aquaman",
                "Atom",
                "Batman",
                "Superman",
                "Thor",
                "Wildcat",
                "Wonder Woman",
            };

            //
            // Construct array list.
            //
            // Construct ArrayList.
            arrList = new ArrayList((ICollection)strHeroes);

            //Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of 
            //BinarySearch, Following variable cotains each one of these types of array lists

            ArrayList[] arrayListTypes = {
                                    (ArrayList)arrList.Clone(),
                                    (ArrayList)ArrayList.Adapter(arrList).Clone(),
                                    (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                                    (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;

                //
                // []  Remove objects from the ArrayList.
                //
                for (int ii = 0; ii < count; ++ii)
                {
                    arrList.RemoveAt(start);
                }

                // Verify the items in the array.
                for (int ii = 0; ii < strResult.Length; ++ii)
                {
                    Assert.Equal(0, strResult[ii].CompareTo((string)arrList[ii]));
                }

                //
                // []  Attempt invalid remove using negative index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.RemoveAt(-100));

                //
                //  []  Attempt remove using out of range index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.RemoveAt(1000));
            }
        }
    }
}
