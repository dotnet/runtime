// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class ItemTests
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
        public void TestGetItems()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            //
            // Construct array list.
            //
            arrList = new ArrayList();

            // Add items to the lists.
            for (int ii = 0; ii < strHeroes.Length; ++ii)
            {
                arrList.Add(strHeroes[ii]);
            }

            // Verify items added to list.
            Assert.Equal(strHeroes.Length, arrList.Count);

            //Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of 
            //BinarySearch, Following variable cotains each one of these types of array lists

            ArrayList[] arrayListTypes = {
                                        (ArrayList)arrList.Clone(),
                                        (ArrayList)ArrayList.Adapter(arrList).Clone(),
                                        (ArrayList)ArrayList.FixedSize(arrList).Clone(),
                                        (ArrayList)ArrayList.ReadOnly(arrList).Clone(),
                                        (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                                        (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;
                //
                // []  Verify get method.
                //
                // Search and verify selected items.
                for (int ii = 0; ii < strHeroes.Length; ++ii)
                {
                    // Verify get.
                    Assert.Equal(0, ((string)arrList[ii]).CompareTo(strHeroes[ii]));
                }

                //
                // []  Invalid Index.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    string str = (string)arrList[(int)arrList.Count];
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    string str = (string)arrList[-1];
                });
            }
        }

        [Fact]
        public void TestSetItems()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;

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
                // []  Set item in the array list.
                //
                arrList[0] = "Lone Ranger";

                // Verify set.
                Assert.Equal(0, ((string)arrList[0]).CompareTo("Lone Ranger"));

                //
                // []  Attempt invalid Set using negative index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => { arrList[-100] = "Lone Ranger"; });

                //
                //  []  Attempt Set using out of range index=1000
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => { arrList[1000] = "Lone Ranger"; });

                //
                //  []  Attempt Set using out of range index=-1
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => { arrList[-1] = "Lone Ranger"; });

                //
                //  []  Attempt Set using null value.
                //
                arrList[0] = null;
                // Verify set.
                Assert.Null(arrList[0]);
            }
        }

        //public virtual int Compare(object x, object y)
        //{
        //    return ((string)x).CompareTo((string)y);
        //}
    }
}
