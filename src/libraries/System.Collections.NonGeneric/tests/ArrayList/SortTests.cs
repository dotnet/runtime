// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class SortTests
    {
        #region "Test Data - Keep the data close to tests so it can vary independently from other tests"

        string[] strHeroes =
            {
                "Green Arrow",
                "Atom",
                "Batman",
                "Steel",
                "Superman",
                "Wonder Woman",
                "Hawkman",
                "Flash",
                "Aquaman",
                "Green Lantern",
                "Catwoman",
                "Huntress",
                "Robin",
                "Captain Atom",
                "Wildcat",
                "Nightwing",
                "Ironman",
                "SpiderMan",
                "Black Canary",
                "Thor",
                "Cyborg",
                "Captain America",
            };

        string[] strHeroesSorted =
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
        public void TestAscendingAndDecendingSort()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            string[] strHeroesUnsorted = null;

            //
            // Test ascending sort.
            //
            // Construct unsorted array.
            strHeroesUnsorted = new String[strHeroes.Length];
            System.Array.Copy(strHeroes, 0, strHeroesUnsorted, 0, strHeroes.Length);

            // Sort ascending the array list.
            System.Array.Sort(strHeroesUnsorted, 0, strHeroesUnsorted.Length, new SortTests_Assending());

            // Verify ascending sort.
            for (int ii = 0; ii < strHeroesUnsorted.Length; ++ii)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo(strHeroesUnsorted[ii]));
            }

            //
            // Test decending sort.
            //
            // Construct unsorted array.
            strHeroesUnsorted = new String[strHeroes.Length];
            System.Array.Copy(strHeroes, 0, strHeroesUnsorted, 0, strHeroes.Length);

            // Sort decending the array list.
            System.Array.Sort(strHeroesUnsorted, 0, strHeroesUnsorted.Length, new SortTests_Decending());

            // Verify descending sort.
            for (int ii = 0; ii < strHeroesUnsorted.Length; ++ii)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo(strHeroesUnsorted[strHeroesUnsorted.Length - ii - 1]));
            }

            //
            // []  Sort array list using default comparer.
            //
            arrList = new ArrayList((ICollection)strHeroesUnsorted);
            Assert.NotNull(arrList);

            // Sort decending the array list.
            arrList.Sort(null);

            // Verify sort.
            for (int ii = 0; ii < arrList.Count; ++ii)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo((string)arrList[ii]));
            }

            //
            // []  Sort array list our ascending comparer.
            //
            arrList = new ArrayList((ICollection)strHeroesUnsorted);
            Assert.NotNull(arrList);

            // Sort decending the array list.
            arrList.Sort(new SortTests_Assending());

            // Verify sort.
            for (int ii = 0; ii < arrList.Count; ++ii)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo((String)arrList[ii]));
            }
            //
            // []  Sort array list our ascending comparer.
            //
            arrList = new ArrayList((ICollection)strHeroesUnsorted);
            Assert.NotNull(arrList);

            // Sort decending the array list.
            arrList.Sort(new SortTests_Decending());

            // Verify sort.
            for (int ii = 0, jj = arrList.Count - 1; ii < arrList.Count; ++ii, jj--)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo((string)arrList[jj]));
            }
        }

        [Fact]
        public void TestInvalidIndexOrCount()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            string[] strHeroesUnsorted = null;

            //
            // Test ascending sort.
            //
            // Construct unsorted array.
            strHeroesUnsorted = new String[strHeroes.Length];
            System.Array.Copy(strHeroes, 0, strHeroesUnsorted, 0, strHeroes.Length);

            // Sort ascending the array list.
            System.Array.Sort(strHeroesUnsorted, 0, strHeroesUnsorted.Length, new SortTests_Assending());

            // Verify ascending sort.
            for (int ii = 0; ii < strHeroesUnsorted.Length; ++ii)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo(strHeroesUnsorted[ii]));
            }

            //
            // Test decending sort.
            //
            // Construct unsorted array.
            strHeroesUnsorted = new String[strHeroes.Length];
            System.Array.Copy(strHeroes, 0, strHeroesUnsorted, 0, strHeroes.Length);

            // Sort decending the array list.
            System.Array.Sort(strHeroesUnsorted, 0, strHeroesUnsorted.Length, new SortTests_Decending());

            // Verify descending sort.
            for (int ii = 0; ii < strHeroesUnsorted.Length; ++ii)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo(strHeroesUnsorted[strHeroesUnsorted.Length - ii - 1]));
            }

            arrList = new ArrayList((ICollection)strHeroesUnsorted);

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
                // []  Sort array list using default comparer.
                //
                Assert.NotNull(arrList);

                // Sort decending the array list.
                arrList.Sort(0, arrList.Count, null);

                // Verify sort.
                for (int ii = 0; ii < arrList.Count; ++ii)
                {
                    Assert.Equal(0, strHeroesSorted[ii].CompareTo((string)arrList[ii]));
                }

                //
                // []  Bogus negative index.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.Sort(-1000, arrList.Count, null));

                //
                // []  Bogus out of bounds index.
                //
                Assert.Throws<ArgumentException>(() => arrList.Sort(1000, arrList.Count, null));

                //
                // []  Bogus negative size parmeter.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.Sort(0, -1000, null));

                //
                // []  Bogus out of bounds size parmeter.
                //
                Assert.Throws<ArgumentException>(() => arrList.Sort(0, 1000, null));
            }
        }

        internal class SortTests_Assending : IComparer
        {
            public virtual int Compare(Object x, Object y)
            {
                return ((String)x).CompareTo((String)y);
            }
        }
        internal class SortTests_Decending : IComparer
        {
            public virtual int Compare(Object x, Object y)
            {
                return -((String)x).CompareTo((String)y);
            }
        }

        [Fact]
        public void TestMultipleDataTypes()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            //
            // []  Sort array list using default comparer.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            // Sort decending the array list.
            arrList.Sort();

            // Verify sort.
            for (int ii = 0; ii < arrList.Count; ++ii)
            {
                Assert.Equal(0, strHeroesSorted[ii].CompareTo((String)arrList[ii]));
            }

            //[]Team review feedback - Sort an empty ArrayList
            arrList = new ArrayList();
            arrList.Sort();

            Assert.Equal(0, arrList.Count);

            //[] Sort an ArrayList with multiple data types. This will throw
            short i16;
            int i32;
            long i64;
            ushort ui16;
            uint ui32;
            ulong ui64;
            ArrayList alst;

            i16 = 1;
            i32 = 2;
            i64 = 3;
            ui16 = 4;
            ui32 = 5;
            ui64 = 6;
            alst = new ArrayList();

            alst.Add(i16);
            alst.Add(i32);
            alst.Add(i64);
            alst.Add(ui16);
            alst.Add(ui32);
            alst.Add(ui64);

            Assert.Throws<InvalidOperationException>(() => alst.Sort());
        }
    }
}
