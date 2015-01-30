// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class LastIndexOfTests
    {
        #region "Test Data - Keep the data close to tests so it can vary independently from other tests"
        string[] strHeroes =
            {
                "Aquaman",
                "Atom",
                "Batman",
                "Black Canary",
                "Captain America",
                "Captain Atom",
                "Batman",
                "Catwoman",
                "Cyborg",
                "Flash",
                "Green Arrow",
                "Batman",
                "Green Lantern",
                "Hawkman",
                "Huntress",
                "Ironman",
                "Nightwing",
                "Batman",
                "Robin",
                "SpiderMan",
                "Steel",
                "Superman",
                "Thor",
                "Batman",
                "Wildcat",
                "Wonder Woman",
                "Batman",
            };

        #endregion

        [Fact]
        public void TestLastIndexOfBasic()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            int ndx = -1;
            //
            //  Construct array lists.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            //
            // []  Obtain last index of "Batman" items.
            //
            ndx = arrList.LastIndexOf("Batman");
            if (ndx != -1)
            {
                Assert.Equal(0, strHeroes[ndx].CompareTo((string)arrList[ndx]));
            }

            //
            // []  Attempt to find null object.
            //
            // Remove range of items.
            ndx = arrList.LastIndexOf(null);
            Assert.Equal(-1, ndx);

            // []  Call LastIndexOf on an empty list
            var myList = new ArrayList();
            var lastIndex = myList.LastIndexOf(6);

            Assert.Equal(-1, lastIndex);
        }

        [Fact]
        public void TestInvalidIndex()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            int ndx = -1;

            //
            // Construct array lists.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            //
            // []  Obtain last index of "Batman" items.
            //
            ndx = arrList.Count;
            while ((ndx = arrList.LastIndexOf("Batman", --ndx)) != -1)
            {
                Assert.Equal(0, strHeroes[ndx].CompareTo((string)arrList[ndx]));
            }

            //
            // []  Attempt to find null object.
            //
            // Remove range of items.
            ndx = arrList.LastIndexOf(null, arrList.Count - 1);
            Assert.Equal(-1, ndx);

            //
            //  []  Attempt invalid LastIndexOf using negative endindex
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => arrList.LastIndexOf("Batman", -1));

            //
            //  []  Attempt invalid LastIndexOf using negative startindex
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => arrList.LastIndexOf("Batman", -1000));
        }

        [Fact]
        public void TestArrayListWrappers()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            int ndx = -1;

            string[] strHeroes =
            {
                "Aquaman",
                "Atom",
                "Batman",
                "Black Canary",
                "Captain America",
                "Captain Atom",
                "Batman",
                "Catwoman",
                "Cyborg",
                "Flash",
                "Green Arrow",
                "Batman",
                "Green Lantern",
                "Hawkman",
                "Huntress",
                "Ironman",
                "Nightwing",
                "Batman",
                "Robin",
                "SpiderMan",
                "Steel",
                "Superman",
                "Thor",
                "Batman",
                "Wildcat",
                "Wonder Woman",
                "Batman",
                null
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
                                    (ArrayList)ArrayList.FixedSize(arrList).Clone(),
                                    (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                                    (ArrayList)ArrayList.ReadOnly(arrList).Clone(),
                                    (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;

                //
                // []  Obtain last index of "Batman" items.
                //
                int startIndex = arrList.Count - 1;
                int tmpNdx = 0;
                while (0 < startIndex && (ndx = arrList.LastIndexOf("Batman", startIndex, startIndex + 1)) != -1)
                {
                    Assert.True(ndx <= startIndex);

                    Assert.Equal(0, strHeroes[ndx].CompareTo((string)arrList[ndx]));

                    tmpNdx = arrList.LastIndexOf("Batman", startIndex, startIndex - ndx + 1);
                    Assert.Equal(ndx, tmpNdx);

                    tmpNdx = arrList.LastIndexOf("Batman", startIndex, startIndex - ndx);
                    Assert.Equal(-1, tmpNdx);

                    startIndex = ndx - 1;
                }

                //
                // []  Attempt to find null object.
                //
                // Remove range of items.
                ndx = arrList.LastIndexOf(null, arrList.Count - 1, arrList.Count);
                Assert.NotEqual(-1, ndx);
                Assert.Null(arrList[ndx]);

                //
                //  []  Attempt invalid LastIndexOf using negative endindex
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.LastIndexOf("Batman", arrList.Count - 1, -1000));

                //
                //  []  Attempt invalid LastIndexOf using negative startindex
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.LastIndexOf("Batman", -1000, 0));

                //
                //  []  Attempt invalid LastIndexOf using endIndex greater than the size.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.LastIndexOf("Batman", 3, arrList.Count + 10));

                //
                //  []  Attempt invalid LastIndexOf using startIndex greater than the size.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.LastIndexOf("Batman", arrList.Count + 1, arrList.Count));

                //
                //  []  Attempt invalid LastIndexOf using count > starIndex + 1 
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.LastIndexOf("Batman", 3, 5));

                //
                //  []  Attempt LastIndexOf on empty ArrayList 
                //
                if (!arrList.IsFixedSize)
                {
                    arrList.Clear();
                    int index = arrList.LastIndexOf("", 0, 0);
                    Assert.Equal(-1, index);
                }
            }
        }
    }
}
