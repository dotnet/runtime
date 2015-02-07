// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class CopyToTests
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
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null
        };

        #endregion

        [Fact]
        public void TestCopyToBasic()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            String[] arrCopy = null;

            //
            // []  CopyTo an array normal
            //
            // []  Normal Copy Test 1

            arrList = new ArrayList(strHeroes);
            arrCopy = new String[strHeroes.Length];
            arrList.CopyTo(arrCopy);

            for (int i = 0; i < arrCopy.Length; i++)
            {
                Assert.Equal(strHeroes[i], arrCopy[i]);
            }

            //[]  Normal Copy Test 2 - copy 0 elements
            // Construct ArrayList.
            arrList = new ArrayList();
            arrList.Add(null);
            arrList.Add(arrList);
            arrList.Add(null);
            arrList.Remove(null);
            arrList.Remove(null);
            arrList.Remove(arrList);

            Assert.Equal(0, arrList.Count);

            arrCopy = new String[strHeroes.Length];

            // put some elements in arrCopy that should not be overriden
            for (int i = 0; i < strHeroes.Length; i++)
            {
                arrCopy[i] = strHeroes[i];
            }

            //copying 0 elements into arrCopy
            arrList.CopyTo(arrCopy);

            // check to make sure sentinals stay the same
            for (int i = 0; i < arrCopy.Length; i++)
            {
                Assert.Equal(strHeroes[i], arrCopy[i]);
            }

            //we'll make sure by copying only 0
            arrList = new ArrayList();
            arrCopy = new String[0];

            //copying 0 elements into arrCopy
            arrList.CopyTo(arrCopy);
            Assert.Equal(0, arrCopy.Length);

            //[]  Copy so that exception should be thrown
            Assert.Throws<ArgumentNullException>(() =>
            {
                // Construct ArrayList.
                arrList = new ArrayList();
                arrCopy = null;

                //copying 0 elements into arrCopy, into INVALID index of arrCopy
                arrList.CopyTo(arrCopy);
            });
        }

        [Fact]
        public void TestArrayListWrappers()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            String[] arrCopy = null;

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
                // []  CopyTo an array normal
                //
                arrCopy = new String[strHeroes.Length];
                arrList.CopyTo(arrCopy, 0);

                for (int i = 0; i < arrCopy.Length; i++)
                {
                    Assert.Equal<string>(strHeroes[i], arrCopy[i]);
                }

                //[]  Normal Copy Test 2 - copy 0 elements
                arrList.Clear();
                arrList.Add(null);
                arrList.Add(arrList);
                arrList.Add(null);
                arrList.Remove(null);
                arrList.Remove(null);
                arrList.Remove(arrList);

                Assert.Equal(0, arrList.Count);

                arrCopy = new String[strHeroes.Length];
                // put some elements in arrCopy that should not be overriden
                for (int i = 0; i < strHeroes.Length; i++)
                {
                    arrCopy[i] = strHeroes[i];
                }

                //copying 0 elements into arrCopy
                arrList.CopyTo(arrCopy, 1);

                // check to make sure sentinals stay the same
                for (int i = 0; i < arrCopy.Length; i++)
                {
                    Assert.Equal<string>(strHeroes[i], arrCopy[i]);
                }

                //[]  Normal Copy Test 3 - copy 0 elements from the end
                arrList.Clear();
                Assert.Equal(0, arrList.Count);

                arrCopy = new String[strHeroes.Length];

                // put some elements in arrCopy that should not be overriden
                for (int i = 0; i < strHeroes.Length; i++)
                {
                    arrCopy[i] = strHeroes[i];
                }

                //copying 0 elements into arrCopy, into last valid index of arrCopy
                arrList.CopyTo(arrCopy, arrCopy.Length - 1);

                // check to make sure sentinals stay the same
                for (int i = 0; i < arrCopy.Length; i++)
                {
                    Assert.Equal<string>(strHeroes[i], arrCopy[i]);
                }

                //[]  Copy so that exception should be thrown
                arrList.Clear();
                arrCopy = new String[2];

                //copying 0 elements into arrCopy
                arrList.CopyTo(arrCopy, arrCopy.Length);

                // []  Copy so that exception should be thrown 2
                arrList.Clear();
                Assert.Equal(0, arrList.Count);

                arrCopy = new String[0];
                //copying 0 elements into arrCopy
                arrList.CopyTo(arrCopy, 0);

                // []  CopyTo with negative index
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.CopyTo(arrCopy, -1));

                // []  CopyTo with array with index is not large enough
                Assert.Throws<ArgumentException>(() =>
                    {
                        arrList.Clear();
                        for (int i = 0; i < 10; i++)
                            arrList.Add(i);

                        arrList.CopyTo(new Object[11], 2);
                    });

                // []  CopyTo with null array
                Assert.Throws<ArgumentNullException>(() => arrList.CopyTo(null, 0));

                // []  CopyTo with multidimentional array
                Assert.Throws<ArgumentException>(() => arrList.CopyTo(new Object[10, 10], 1));
            }
        }

        [Fact]
        public void TestCopyToWithCount()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            string[] arrCopy = null;

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
            arrList = new ArrayList();
            Assert.NotNull(arrList);

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

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                arrList = arrayListType;
                //
                // []  Use CopyTo copy range of items to array.
                //
                int start = 3;
                int count = 15;

                // Allocate sting array.
                arrCopy = new String[100];

                // Obtain string from ArrayList.
                arrList.CopyTo(start, arrCopy, start, count);

                // Verify the items in the array.
                for (int ii = start; ii < start + count; ++ii)
                {
                    Assert.Equal(0, ((String)arrList[ii]).CompareTo(arrCopy[ii]));
                }

                //
                // []  Invalid Arguments
                //

                // 2nd throw ArgumentOutOfRangeException
                // rest throw ArgumentException 
                Assert.ThrowsAny<ArgumentException>( () => arrList.CopyTo(0, arrCopy, -100, 1000) );

                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.CopyTo(-1, arrCopy, 0, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.CopyTo(0, arrCopy, 0, -1));

                // this is valid now
                arrCopy = new String[100];
                arrList.CopyTo(arrList.Count, arrCopy, 0, 0);

                Assert.Throws<ArgumentException>(() =>
                {
                    arrCopy = new String[100];
                    arrList.CopyTo(arrList.Count - 1, arrCopy, 0, 24);
                });

                Assert.Throws<ArgumentNullException>(() => arrList.CopyTo(0, null, 3, 15));

                Assert.Throws<ArgumentException>(() =>
                {
                    arrCopy = new String[1];
                    arrList.CopyTo(0, arrCopy, 3, 15);
                });

                Assert.Throws<ArgumentException>(() => arrList.CopyTo(0, new Object[arrList.Count, arrList.Count], 0, arrList.Count));
                // same as above, some iteration throws different exceptions: ArgumentOutOfRangeException
                Assert.ThrowsAny<ArgumentException>(() => arrList.CopyTo(0, new Object[arrList.Count, arrList.Count], 0, -1));
            }
        }
    }
}
