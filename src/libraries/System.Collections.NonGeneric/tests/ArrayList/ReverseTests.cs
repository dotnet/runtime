// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class ReverseTests
    {
        [Fact]
        public void TestArrayListWrappers()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;

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

            //
            // Construct array list.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            //Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of 
            //BinarySearch, Following variable cotains each one of these types of array lists

            ArrayList[] arrayListTypes = {
                                    (ArrayList)arrList.Clone(),
                                    (ArrayList)ArrayList.Adapter(arrList).Clone(),
                                    (ArrayList)ArrayList.FixedSize(arrList).Clone(),
                                    (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                                    (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;

                //
                // []  Reverse entire array list.
                //
                arrList.Reverse(0, arrList.Count);

                // Verify items have been reversed.
                for (int ii = 0; ii < arrList.Count; ++ii)
                {
                    Assert.Equal(0, strHeroes[ii].CompareTo((String)arrList[arrList.Count - ii - 1]));
                }

                //
                // []  Attempt invalid Reverse using negative index
                //
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.Reverse(-100, arrList.Count));

                //
                //  []  Attempt Reverse using out of range index
                //
                Assert.Throws<ArgumentException>(() => arrList.Reverse(1000, arrList.Count));

                //
                //  []  Attempt Reverse using negative count.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.Reverse(0, -arrList.Count));

                //
                //  []  Attempt Reverse using zero count.
                //
                arrList.Reverse(0, 0);

                // Verify no reversal (List should still be reveresed of the original.)
                for (int ii = 0; ii < arrList.Count; ++ii)
                {
                    Assert.Equal(0, strHeroes[ii].CompareTo((string)arrList[arrList.Count - ii - 1]));
                }
            }
        }

        [Fact]
        public void Test02()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;

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

            //
            // Construct array list.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            //Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of 
            //BinarySearch, Following variable cotains each one of these types of array lists

            ArrayList[] arrayListTypes = {
                                        (ArrayList)arrList.Clone(),
                                        (ArrayList)ArrayList.Adapter(arrList).Clone(),
                                        (ArrayList)ArrayList.FixedSize(arrList).Clone(),
                                        (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                                        (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;

                //
                // []  Reverse entire array list.
                //
                // Reverse entire array list.
                arrList.Reverse();

                // Verify items have been reversed.
                for (int ii = 0; ii < arrList.Count; ++ii)
                {
                    Assert.Equal(0, strHeroes[ii].CompareTo((String)arrList[arrList.Count - ii - 1]));
                }

                //[]Team review feedback - Reversing lists of varying sizes inclusing 0
                arrList = new ArrayList();
                arrList.Reverse();

                arrList = new ArrayList();
                for (int i = 0; i < 1; i++)
                    arrList.Add(i);

                arrList.Reverse();
            }
        }
    }
}
