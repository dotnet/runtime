// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class GetEnumeratorTests
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
        #endregion

        [Fact]
        public void TestArrayListWrappers01()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            int ii = 0;
            int start = 3;
            int count = 15;
            bool bGetNext = false;
            object[] tempArray1;

            //
            // Construct array lists.
            //
            arrList = new ArrayList((ICollection)strHeroes);
            Assert.NotNull(arrList);

            //Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of 
            //BinarySearch, Following variable cotains each one of these types of array lists

            ArrayList[] arrayListTypes = {
                                    (ArrayList)arrList.Clone(),
                                    (ArrayList)ArrayList.Adapter(arrList).Clone(),
                                    (ArrayList)ArrayList.FixedSize(arrList).Clone(),
                                    (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                                    (ArrayList)ArrayList.ReadOnly(arrList).Clone(),
                                    (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            IEnumerator enu;
            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;

                // Obtain the enumerator for the test range.
                enu = (IEnumerator)arrList.GetEnumerator(start, count);
                Assert.NotNull(enu);

                // Verify the enumerator.
                for (ii = start; ii < start + count; ++ii)
                {
                    bGetNext = enu.MoveNext();
                    if (bGetNext == false)
                        break;

                    Assert.Equal(0, strHeroes[ii].CompareTo((string)enu.Current));
                }

                ii -= start;
                Assert.Equal(count, ii);

                bGetNext = enu.MoveNext();
                Assert.False(bGetNext);
                Assert.False(enu.MoveNext());
                Assert.False(enu.MoveNext());
                Assert.False(enu.MoveNext());

                // Obtain and verify enumerator with 0 count");
                // Obtain the enumerator for the test range.
                enu = (IEnumerator)arrList.GetEnumerator(start, 0);
                Assert.False(enu.MoveNext());
                Assert.False(enu.MoveNext());
                Assert.False(enu.MoveNext());

                enu.Reset();
                Assert.Throws<InvalidOperationException>(() =>
                {
                    object test = enu.Current;
                });

                Assert.False(enu.MoveNext());
                Assert.False(enu.MoveNext());
                Assert.False(enu.MoveNext());

                //
                // []  Make Sure both MoveNext and Reset throw InvalidOperationException if underlying collection has been modified but Current should not throw
                //
                if (!arrList.IsReadOnly)
                {
                    object origValue = arrList[arrList.Count - 1];

                    // [] MoveNext and Reset throw if collection has been modified
                    try
                    {
                        IEnumerator enu1 = (IEnumerator)arrList.GetEnumerator(start, count);
                        enu1.MoveNext();

                        arrList[arrList.Count - 1] = "Underdog";
                        object myValue = enu1.Current;

                        Assert.Throws<InvalidOperationException>(() => enu1.MoveNext());
                        Assert.Throws<InvalidOperationException>(() => enu1.Reset());

                    }
                    finally
                    {
                        arrList[arrList.Count - 1] = origValue;
                    }
                }

                //
                // []  Verify Current throws InvalidOperationException when positioned before the first element or after the last
                //
                enu = (IEnumerator)arrList.GetEnumerator(start, count);
                Assert.Throws<InvalidOperationException>(() =>
                {
                    object myValue = enu.Current;
                });

                while (enu.MoveNext()) ;
                Assert.Throws<InvalidOperationException>(() =>
                {
                    object myValue = enu.Current;
                });

                //
                // []  Use invalid parameters.
                //
                Assert.Throws<ArgumentException>(() =>
                {
                    IEnumerator enu0 = (IEnumerator)arrList.GetEnumerator(0, 10000);
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    IEnumerator enu1 = (IEnumerator)arrList.GetEnumerator(-1, arrList.Count);
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    IEnumerator enu2 = (IEnumerator)arrList.GetEnumerator(0, -1);
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    IEnumerator enu3 = (IEnumerator)arrList.GetEnumerator(-1, arrList.Count);
                });

                //[] Verify the eneumerator works correctly when the ArrayList itself is in the ArrayList
                if (!arrList.IsFixedSize)
                {
                    arrList.Insert(0, arrList);
                    arrList.Insert(arrList.Count, arrList);
                    arrList.Insert(arrList.Count / 2, arrList);

                    tempArray1 = new object[strHeroes.Length + 3];
                    tempArray1[0] = (object)arrList;
                    tempArray1[tempArray1.Length / 2] = arrList;
                    tempArray1[tempArray1.Length - 1] = arrList;

                    Array.Copy(strHeroes, 0, tempArray1, 1, strHeroes.Length / 2);
                    Array.Copy(strHeroes, strHeroes.Length / 2, tempArray1, (tempArray1.Length / 2) + 1, strHeroes.Length - (strHeroes.Length / 2));

                    //[] Enumerate the entire collection
                    enu = arrList.GetEnumerator(0, tempArray1.Length);

                    for (int loop = 0; loop < 2; ++loop)
                    {
                        for (int i = 0; i < tempArray1.Length; ++i)
                        {
                            enu.MoveNext();

                            Assert.StrictEqual(tempArray1[i], enu.Current);
                        }

                        Assert.False(enu.MoveNext());

                        enu.Reset();
                    }

                    //[] Enumerate only part of the collection
                    enu = arrList.GetEnumerator(1, tempArray1.Length - 2);

                    for (int loop = 0; loop < 2; ++loop)
                    {
                        for (int i = 1; i < tempArray1.Length - 1; ++i)
                        {
                            enu.MoveNext();

                            Assert.StrictEqual(tempArray1[i], enu.Current);
                        }

                        Assert.False(enu.MoveNext());
                        enu.Reset();
                    }
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
            int ii = 0;
            bool bGetNext = false;
            object o1;

            IEnumerator ien1;

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
                                    (ArrayList)ArrayList.Synchronized(arrList).Clone(),
                                    (ArrayList)(new MyArrayList((ICollection) strHeroes))};

            IEnumerator enu = null;
            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;
                //
                // [] Add the ArrayList itself to the ArrayList
                //
                try
                {
                    arrList.Add(arrList);
                    enu = arrList.GetEnumerator();

                    int index;

                    index = 0;
                    while (enu.MoveNext())
                    {
                        Assert.StrictEqual(enu.Current, arrList[index]);
                        ++index;
                    }

                    enu.Reset();

                    index = 0;
                    while (enu.MoveNext())
                    {
                        Assert.StrictEqual(enu.Current, arrList[index]);
                        ++index;
                    }
                }
                finally
                {
                    arrList.RemoveAt(arrList.Count - 1);
                }

                //
                // []  Vanila - Obtain and verify enumerator.
                //
                // Obtain the enumerator for the test range.
                enu = arrList.GetEnumerator();
                IEnumerator enuClone = arrList.GetEnumerator();

                IEnumerator[] enuArray = { enu, enuClone };

                //Verify both this instance and the cloned copy
                foreach (IEnumerator enumerator in enuArray)
                {
                    enu = enumerator;

                    for (int i = 0; i < 2; i++)
                    {
                        Assert.NotNull(enu);

                        // Verify the enumerator.
                        for (ii = 0; ; ii++)
                        {
                            bGetNext = enu.MoveNext();
                            if (bGetNext == false)
                                break;

                            Assert.Equal(0, strHeroes[ii].CompareTo((string)enu.Current));
                        }

                        Assert.Equal(strHeroes.Length, ii);

                        bGetNext = enu.MoveNext();
                        Assert.False(bGetNext);
                        Assert.False(enu.MoveNext());
                        Assert.False(enu.MoveNext());
                        Assert.False(enu.MoveNext());

                        enu.Reset();
                    }
                }

                //[]we'll make sure that the enumerator throws if the underlying collection is changed with MoveNext() but not at Current

                arrList.Clear();
                for (int i = 0; i < 100; i++)
                    arrList.Add(i);

                ien1 = arrList.GetEnumerator();
                ien1.MoveNext();
                ien1.MoveNext();
                arrList[10] = 1000;

                Assert.Equal(1, (int)ien1.Current);
                Assert.Throws<InvalidOperationException>(() => ien1.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ien1.Reset());

                //[]we'll make sure that the enumerator throws before and after MoveNext returns false

                arrList.Clear();
                for (int i = 0; i < 100; i++)
                    arrList.Add(i);

                ien1 = arrList.GetEnumerator();

                Assert.Throws<InvalidOperationException>(() =>
                {
                    o1 = ien1.Current;
                });

                while (ien1.MoveNext()) ;
                Assert.Throws<InvalidOperationException>(() =>
                {
                    o1 = ien1.Current;
                });
            }
        }
    }

    public class MyArrayList : ArrayList
    {
        public MyArrayList(ICollection c) : base(c) { }
    }
}
