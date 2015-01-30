// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class InsertTests
    {
        [Fact]
        public void TestArrayListWrappers()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            int ii = 0;
            int start = 3;

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

            string[] strInsert =
            {
                "Dr. Fate",
                "Dr. Light",
                "Dr. Manhattan",
                "Hardware",
                "Hawkeye",
                "Icon",
                "Spawn",
                "Spectre",
                "Supergirl",
            };

            string[] strResult =
            {
                "Aquaman",
                "Atom",
                "Batman",
                "Dr. Fate",
                "Dr. Light",
                "Dr. Manhattan",
                "Hardware",
                "Hawkeye",
                "Icon",
                "Spawn",
                "Spectre",
                "Supergirl",
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

            //
            // Construct array lists.
            //
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
                // []  Insert values into array list.
                //
                // Insert values.
                for (ii = 0; ii < strInsert.Length; ++ii)
                {
                    arrList.Insert(start + ii, strInsert[ii]);
                }

                // Verify insert.
                for (ii = 0; ii < strResult.Length; ++ii)
                {
                    Assert.Equal(0, strResult[ii].CompareTo((String)arrList[ii]));
                }

                //
                //  []  Attempt invalid Insert using negative index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.Insert(-1000, "Batman"));

                //
                //  []  Attempt invalid Insert using out of range index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.Insert(1000, "Batman"));
            }
        }
    }
}
