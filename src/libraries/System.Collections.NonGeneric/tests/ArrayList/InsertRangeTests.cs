// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class InsertRangeTests
    {
        #region "Test data - Keep the data close to tests so it can vary independently from other tests"
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
        #endregion

        [Fact]
        public void TestInsertRangeBasic()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            ArrayList arrInsert = null;
            int ii = 0;
            int start = 3;

            //
            // Construct array lists.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            //
            // Construct insert array list.
            //
            arrInsert = new ArrayList((ICollection)strInsert);

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
                // []  Insert collection into array list.
                //
                // InsertRange values.
                arrList.InsertRange(start, arrInsert);

                // Verify InsertRange.
                for (ii = 0; ii < strResult.Length; ++ii)
                {
                    Assert.Equal(0, strResult[ii].CompareTo((String)arrList[ii]));
                }

                //
                //  []  Attempt invalid InsertRange using negative index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.InsertRange(-1000, arrInsert));

                //
                //  []  Attempt invalid InsertRange using out of range index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.InsertRange(1000, arrInsert));

                //
                //  []  Attempt insertion of null collection.
                arrInsert = new ArrayList((ICollection)strInsert);
                Assert.Throws<ArgumentNullException>(() => arrList.InsertRange(start, null));

                // []Insert an empty ICollection
                arrList = new ArrayList();
                Queue que = new Queue();
                arrList.InsertRange(0, que);

                Assert.Equal(0, arrList.Count);
            }
        }

        [Fact]
        public void TestLargeCapacity()
        {
            //
            //  []  Add a range large enough to increase the capacity of the arrayList by more than a factor of two
            //
            ArrayList arrInsert = new ArrayList();

            for (int i = 0; i < 128; i++)
            {
                arrInsert.Add(-i);
            }

            ArrayList arrList = new ArrayList();

            ArrayList[] arrayListTypes1 = {
                                    (ArrayList)arrList.Clone(),
                                    (ArrayList)ArrayList.Adapter(arrList).Clone(),
                                    (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                                    (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            foreach (ArrayList arrayListType in arrayListTypes1)
            {
                arrList = arrayListType;

                arrList.InsertRange(0, arrInsert);

                for (int i = 0; i < arrInsert.Count; i++)
                {
                    Assert.Equal(-i, (int)arrList[i]);
                }
            }
        }

        [Fact]
        public void TestInsertItself()
        {
            //
            // []  Insert itself into array list.
            //
            ArrayList arrList = new ArrayList((ICollection)strHeroes);

            ArrayList[] arrayListTypes = new ArrayList[] {
                            (ArrayList)arrList.Clone(),
                            (ArrayList)ArrayList.Adapter(arrList).Clone(),
                            (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                            (ArrayList)ArrayList.Synchronized(arrList).Clone()};

            int start = 3;
            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;
                // InsertRange values.
                arrList.InsertRange(start, arrList);

                // Verify InsertRange.
                for (int ii = 0; ii < arrList.Count; ++ii)
                {
                    string expectedItem;

                    if (ii < start)
                    {
                        expectedItem = strHeroes[ii];
                    }
                    else if (start <= ii && ii - start < strHeroes.Length)
                    {
                        expectedItem = strHeroes[ii - start];
                    }
                    else
                    {
                        expectedItem = strHeroes[(ii - strHeroes.Length)];
                    }

                    Assert.Equal(0, expectedItem.CompareTo((string)arrList[ii]));
                }

                //[] Verify that ArrayList does not pass the internal array to CopyTo
                arrList.Clear();
                for (int i = 0; i < 64; ++i)
                {
                    arrList.Add(i);
                }

                ArrayList arrInsert = new ArrayList();

                for (int i = 0; i < 4; ++i)
                {
                    arrInsert.Add(i);
                }

                MyCollection myCollection = new MyCollection(arrInsert);
                arrList.InsertRange(4, myCollection);

                Assert.Equal(0, myCollection.StartIndex);

                Assert.Equal(4, myCollection.Array.Length);
            }
        }

        [Fact]
        public void TestInsertItselfWithRange()
        {
            //
            // []  Insert itself into array list. with range
            //
            ArrayList arrList = new ArrayList((ICollection)strHeroes);

            ArrayList[] arrayListTypes = new ArrayList[] {
                            (ArrayList)arrList.Clone(),
                            (ArrayList)ArrayList.Adapter(arrList).Clone(),
                            (ArrayList)ArrayList.Synchronized(arrList).Clone()
            };

            int start = 3;
            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;

                // InsertRange values.
                arrList.InsertRange(start, arrList.GetRange(0, arrList.Count));

                // Verify InsertRange.
                for (int ii = 0; ii < arrList.Count; ++ii)
                {
                    string expectedItem;

                    if (ii < start)
                    {
                        expectedItem = strHeroes[ii];
                    }
                    else if (start <= ii && ii - start < strHeroes.Length)
                    {
                        expectedItem = strHeroes[ii - start];
                    }
                    else
                    {
                        expectedItem = strHeroes[(ii - strHeroes.Length)];
                    }

                    Assert.Equal(0, expectedItem.CompareTo((string)arrList[ii]));
                }
            }

        }
    }

    public class MyCollection : ICollection
    {
        private ICollection _collection;
        private Array _array;
        private int _startIndex;

        public MyCollection(ICollection collection)
        {
            _collection = collection;
        }

        public Array Array
        {
            get
            {
                return _array;
            }
        }

        public int StartIndex
        {
            get
            {
                return _startIndex;
            }
        }

        public int Count
        {
            get
            {
                return _collection.Count;
            }
        }

        public Object SyncRoot
        {
            get
            {
                return _collection.SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return _collection.IsSynchronized;
            }
        }

        public void CopyTo(Array array, int startIndex)
        {
            _array = array;
            _startIndex = startIndex;
            _collection.CopyTo(array, startIndex);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}
