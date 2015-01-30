// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class BinarySearchTests : IComparer
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

        static string[] strFindHeroes =
            {
                "Batman",
                "Superman",
                "SpiderMan",
                "Wonder Woman",
                "Green Lantern",
                "Flash",
                "Steel"
            };

        #endregion

        [Fact]
        public void TestStandardArrayList()
        {
            //
            // Construct array list.
            //
            ArrayList list = new ArrayList();
            // Add items to the lists.
            for (int ii = 0; ii < strHeroes.Length; ++ii)
                list.Add(strHeroes[ii]);

            // Verify items added to list.
            Assert.Equal(strHeroes.Length, list.Count);

            //
            // []  Use BinarySearch to find selected items.
            //
            // Search and verify selected items.
            for (int ii = 0; ii < strFindHeroes.Length; ++ii)
            {
                // Locate item.
                int ndx = list.BinarySearch(strFindHeroes[ii]);
                Assert.True(ndx >= 0);

                // Verify item.
                Assert.Equal(0, strHeroes[ndx].CompareTo(strFindHeroes[ii]));
            }

            // Return Value;
            // The zero-based index of the value in the sorted ArrayList, if value is found; otherwise, a negative number,
            // which is the bitwise complement of the index of the next element.

            list = new ArrayList();
            for (int i = 0; i < 100; i++)
                list.Add(i);
            list.Sort();

            Assert.Equal(100, ~list.BinarySearch(150));

            //[]null - should return -1
            Assert.Equal(-1, list.BinarySearch(null));

            //[]we can add null as a value and then search it!!!
            list.Add(null);
            list.Sort();
            Assert.Equal(0, list.BinarySearch(null));

            //[]duplicate values, always return the first one
            list = new ArrayList();
            for (int i = 0; i < 100; i++)
                list.Add(5);

            list.Sort();
            //remember this is BinarySeearch
            Assert.Equal(49, list.BinarySearch(5));
        }

        [Fact]
        public void TestArrayListWrappers()
        {
            //
            // Construct array list.
            //
            ArrayList arrList = new ArrayList();

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
                                    arrList,
                                    ArrayList.Adapter(arrList),
                                    ArrayList.FixedSize(arrList),
                                    arrList.GetRange(0, arrList.Count),
                                    ArrayList.ReadOnly(arrList),
                                    ArrayList.Synchronized(arrList)};

            int ndx = 0;
            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;

                //
                // []  Use BinarySearch to find selected items.
                //
                // Search and verify selected items.
                for (int ii = 0; ii < strFindHeroes.Length; ++ii)
                {
                    // Locate item.
                    ndx = arrList.BinarySearch(0, arrList.Count, strFindHeroes[ii], this);
                    Assert.True(ndx >= 0);

                    // Verify item.
                    Assert.Equal(0, strHeroes[ndx].CompareTo(strFindHeroes[ii]));
                }

                //
                // []  Locate item in list using null comparer.
                //
                ndx = arrList.BinarySearch(0, arrList.Count, "Batman", null);
                Assert.Equal(2, ndx);

                //
                // []  Locate insertion index of new list item.
                //
                // Append the list.
                ndx = arrList.BinarySearch(0, arrList.Count, "Batgirl", this);
                Assert.Equal(2, ~ndx);

                //
                // []  Bogus Arguments
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.BinarySearch(-100, 1000, arrList.Count, this));
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.BinarySearch(-100, 1000, "Batman", this));
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.BinarySearch(-1, arrList.Count, "Batman", this));
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.BinarySearch(0, -1, "Batman", this));

                Assert.Throws<ArgumentException>(() => arrList.BinarySearch(1, arrList.Count, "Batman", this));
                Assert.Throws<ArgumentException>(() => arrList.BinarySearch(3, arrList.Count - 2, "Batman", this));
            }
        }

        [Fact]
        public void TestCustomComparer()
        {
            ArrayList list = null;
            list = new ArrayList();

            // Add items to the lists.
            for (int ii = 0; ii < strHeroes.Length; ++ii)
                list.Add(strHeroes[ii]);

            // Verify items added to list.
            Assert.Equal(strHeroes.Length, list.Count);

            //
            // []  Use BinarySearch to find selected items.
            //
            // Search and verify selected items.
            for (int ii = 0; ii < strFindHeroes.Length; ++ii)
            {
                // Locate item.
                int ndx = list.BinarySearch(strFindHeroes[ii], new BinarySearchTests());
                Assert.True(ndx < strHeroes.Length);

                // Verify item.
                Assert.Equal(0, strHeroes[ndx].CompareTo(strFindHeroes[ii]));
            }

            //Return Value;
            //The zero-based index of the value in the sorted ArrayList, if value is found; otherwise, a negative number, which is the 
            //bitwise complement of the index of the next element.

            list = new ArrayList();
            for (int i = 0; i < 100; i++)
                list.Add(i);
            list.Sort();

            Assert.Equal(100, ~list.BinarySearch(150, new BinarySearchTests()));

            //[]null - should return -1
            Assert.Equal(-1, list.BinarySearch(null, new BinarySearchTests()));

            //[]we can add null as a value and then search it!!!

            list.Add(null);
            list.Sort();
            Assert.Equal(0, list.BinarySearch(null, new CompareWithNullEnabled()));

            //[]duplicate values, always return the first one

            list = new ArrayList();
            for (int i = 0; i < 100; i++)
                list.Add(5);
            list.Sort();

            //remember this is BinarySeearch
            Assert.Equal(49, list.BinarySearch(5, new BinarySearchTests()));

            //[]IC as null
            list = new ArrayList();
            for (int i = 0; i < 100; i++)
                list.Add(5);
            list.Sort();
            //remember this is BinarySeearch
            Assert.Equal(49, list.BinarySearch(5, null));
        }

        public virtual int Compare(object x, object y)
        {
            if (x is string)
            {
                return ((string)x).CompareTo((string)y);
            }

            var comparer = new Comparer(System.Globalization.CultureInfo.InvariantCulture);
            if (x is int || y is string)
            {
                return comparer.Compare(x, y);
            }

            return -1;
        }
    }

    class CompareWithNullEnabled : IComparer
    {
        public int Compare(Object a, Object b)
        {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            IComparable ia = a as IComparable;
            if (ia != null)
                return ia.CompareTo(b);

            IComparable ib = b as IComparable;
            if (ib != null)
                return -ib.CompareTo(a);

            throw new ArgumentException("Wrong stuff");
        }
    }
}
