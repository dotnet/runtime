// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class RemoveRangeTests
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
            // Construct array lists.
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
                // []  Remove range of items from array list.
                //
                // Remove range of items.
                arrList.RemoveRange(start, count);

                // Verify remove.
                for (int ii = 0; ii < strResult.Length; ++ii)
                {
                    Assert.Equal(0, strResult[ii].CompareTo((string)arrList[ii]));
                }

                //
                // []  Attempt remove range using zero count.
                //
                // Remove range of items.
                arrList.RemoveRange(start, 0);

                // Verify size.
                Assert.Equal(strResult.Length, arrList.Count);

                // Verify remove.
                for (int ii = 0; ii < strResult.Length; ++ii)
                {
                    Assert.Equal(0, strResult[ii].CompareTo((string)arrList[ii]));
                }

                //
                //  []  Attempt invalid RemoveRange using very large count.
                //
                Assert.Throws<ArgumentException>(() => arrList.RemoveRange(start, 10005));

                //
                //  []  Attempt invalid RemoveRange using negative index
                //
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.RemoveRange(-1000, 5));

                //
                //  []  Attempt invalid RemoveRange using negative count
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.RemoveRange(start, -1));

                //
                //  []  Attempt invalid RemoveRange using out of range index
                //
                Assert.Throws<ArgumentException>(() => arrList.RemoveRange(1000, 5));
            }
        }
    }
}
