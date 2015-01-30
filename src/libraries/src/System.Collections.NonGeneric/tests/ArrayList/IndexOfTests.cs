// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class IndexOfTests
    {
        [Fact]
        public void TestArrayListWrappers01()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            // no null
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

            ArrayList arrList = null;
            int ndx = -1;

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
                // Construct array lists.
                //
                Assert.NotNull(arrList);

                //
                // []  Obtain index of "Batman" items.
                //
                int startIndex = 0;
                while (startIndex < arrList.Count && (ndx = arrList.IndexOf("Batman", startIndex)) != -1)
                {
                    Assert.True(startIndex <= ndx);

                    Assert.Equal(0, strHeroes[ndx].CompareTo((string)arrList[ndx]));

                    //
                    // []  Attempt to find null object.
                    //
                    // Remove range of items.
                    ndx = arrList.IndexOf(null, 0);
                    Assert.Equal(-1, ndx);

                    //
                    //  []  Attempt invalid IndexOf using negative index
                    //
                    Assert.Throws<ArgumentOutOfRangeException>(() => arrList.IndexOf("Batman", -1000));

                    //
                    //  []  Attempt invalid IndexOf using out of range index
                    //
                    Assert.Throws<ArgumentOutOfRangeException>(() => arrList.IndexOf("Batman", 1000));

                    // []Team review feedback - query for an existing object after the index. expects -1
                    arrList.Clear();
                    for (int i = 0; i < 10; i++)
                        arrList.Add(i);

                    Assert.Equal(-1, arrList.IndexOf(0, 1));
                }
            }
        }

        [Fact]
        public void TestArrayListWrappers02()
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
            Assert.NotNull(arrList);

            //Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of 
            //BinarySearch, Following variable cotains each one of these types of array lists

            ArrayList[] arrayListTypes = {
                                    arrList,
                                    ArrayList.Adapter(arrList),
                                    ArrayList.FixedSize(arrList),
                                    arrList.GetRange(0, arrList.Count),
                                    ArrayList.ReadOnly(arrList),
                                    ArrayList.Synchronized(arrList)};

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;
                //
                // []  Obtain index of "Batman" items.
                //
                ndx = 0;

                int startIndex = 0;
                int tmpNdx = 0;
                while (startIndex < arrList.Count && (ndx = arrList.IndexOf("Batman", startIndex, (arrList.Count - startIndex))) != -1)
                {
                    Assert.True(ndx >= startIndex);

                    Assert.Equal(0, strHeroes[ndx].CompareTo((string)arrList[ndx]));

                    tmpNdx = arrList.IndexOf("Batman", startIndex, ndx - startIndex + 1);
                    Assert.Equal(ndx, tmpNdx);

                    tmpNdx = arrList.IndexOf("Batman", startIndex, ndx - startIndex);
                    Assert.Equal(-1, tmpNdx);

                    startIndex = ndx + 1;
                }

                //
                // []  Attempt to find null object when a null element exists in the collections
                //
                ndx = arrList.IndexOf(null, 0, arrList.Count);
                Assert.Null(arrList[ndx]);

                //
                //  []  Attempt invalid IndexOf using negative index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.IndexOf("Batman", -1000, arrList.Count));

                //
                //  []  Attempt invalid IndexOf using out of range index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.IndexOf("Batman", 1000, arrList.Count));

                //
                //  []  Attempt invalid IndexOf using index=Count
                //
                Assert.Equal(-1, arrList.IndexOf("Batman", arrList.Count, 0));


                //
                //  []  Attempt invalid IndexOf using endIndex greater than the size.
                //
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.IndexOf("Batman", 3, arrList.Count + 10));


                //[]Team Review feedback - attempt to find non-existent object and confirm that -1 returned
                arrList = new ArrayList();
                for (int i = 0; i < 10; i++)
                    arrList.Add(i);

                Assert.Equal(-1, arrList.IndexOf(50, 0, arrList.Count));
                Assert.Equal(-1, arrList.IndexOf(0, 1, arrList.Count - 1));
            }
        }

        [Fact]
        public void TestDuplicatedItems()
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
                "Daniel Takacs",
                "Ironman",
                "Nightwing",
                "Robin",
                "SpiderMan",
                "Steel",
                "Gene",
                "Thor",
                "Wildcat",
                null
            };

            // Construct ArrayList.
            arrList = new ArrayList(strHeroes);

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
                // []  IndexOf an array normal
                //
                for (int i = 0; i < strHeroes.Length; i++)
                {
                    Assert.Equal(i, arrList.IndexOf(strHeroes[i]));
                }


                //[]  Check IndexOf when element is in list twice
                arrList.Clear();
                arrList.Add(null);
                arrList.Add(arrList);
                arrList.Add(null);

                Assert.Equal(0, arrList.IndexOf(null));

                //[]  check for something which does not exist in a list
                arrList.Clear();
                Assert.Equal(-1, arrList.IndexOf(null));
            }
        }
    }
}
