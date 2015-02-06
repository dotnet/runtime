// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class GetSetRangeTests
    {
        [Fact]
        public void TestGetRange()
        {
            string strValue = string.Empty;

            ArrayList list;
            ArrayList range;

            //[]vanila
            list = new ArrayList();

            for (int i = 0; i < 100; i++)
                list.Add(i);

            //Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of 
            //BinarySearch, Following variable cotains each one of these types of array lists

            ArrayList[] arrayListTypes = {
                                    (ArrayList)list.Clone(),
                                    (ArrayList)ArrayList.Adapter(list).Clone(),
                                    (ArrayList)ArrayList.FixedSize(list).Clone(),
                                    (ArrayList)list.GetRange(0, list.Count).Clone(),
                                    (ArrayList)ArrayList.ReadOnly(list).Clone(),
                                    (ArrayList)ArrayList.Synchronized(list).Clone()
                                  };

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                list = arrayListType;

                range = list.GetRange(10, 50);
                Assert.Equal(50, range.Count);

                for (int i = 0; i < range.Count; i++)
                {
                    Assert.Equal(i + 10, (int)range[i]);
                }

                Assert.Equal(list.IsFixedSize, range.IsFixedSize);

                Assert.Equal(list.IsReadOnly, range.IsReadOnly);

                //[]we can change the underlying collection through the range and this[int index]
                if (!range.IsReadOnly)
                {

                    for (int i = 0; i < 50; i++)
                        range[i] = ((int)range[i]) + 1;

                    for (int i = 0; i < 50; i++)
                    {
                        Assert.Equal((i + 10) + 1, (int)range[i]);
                    }

                    for (int i = 0; i < 50; i++)
                        range[i] = (int)range[i] - 1;
                }

                //[]we can change the underlying collection through the range and Add
                if (!range.IsFixedSize)
                {

                    for (int i = 0; i < 100; i++)
                        range.Add(i + 1000);

                    Assert.Equal(150, range.Count);
                    Assert.Equal(200, list.Count);

                    for (int i = 0; i < 50; i++)
                    {
                        Assert.Equal(i + 10, (int)range[i]);
                    }

                    for (int i = 0; i < 100; i++)
                    {
                        Assert.Equal(i + 1000, (int)range[50 + i]);
                    }
                }

                ////[]if we change the underlying collection through set this[int index] range will start to throw
                if (list.IsReadOnly)
                {
                    Assert.Throws<NotSupportedException>(() =>
                        {
                            list[list.Count - 1] = -1;
                        });

                    Int32 iTemp = range.Count;
                }
                else
                {
                    list[list.Count - 1] = -1;

                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        Int32 iTemp = range.Count;
                    });
                }

                //[]if we change the underlying collection through add range will start to throw
                range = list.GetRange(10, 50);
                if (list.IsFixedSize)
                {
                    Assert.Throws<NotSupportedException>(() =>
                    {
                        list.Add(list.Count + 1000);
                    });

                    Int32 iTemp = range.Count;
                }
                else
                {
                    list.Add(list.Count + 1000);
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        Int32 iTemp = range.Count;
                    });
                }

                //[]parm tests
                Assert.Throws<ArgumentException>(() =>
                {
                    range = list.GetRange(0, 500);
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    range = list.GetRange(0, -1);
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    range = list.GetRange(-1, 50);
                });

                Assert.Throws<ArgumentException>(() =>
                {
                    range = list.GetRange(list.Count, 1);
                });

                //[]we should be able to get a range of 0!
                list = new ArrayList();
                range = list.GetRange(0, 0);
                Assert.Equal(0, range.Count);
            }
        }

        [Fact]
        public void TestSetRange()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            ArrayList arrSetRange = null;
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
            };//22

            string[] strSetRange =
            {
                "Hardware",
                "Icon",
                "Johnny Quest",
                "Captain Sisko",
                "Captain Picard",
                "Captain Kirk",
                "Agent J",
                "Agent K",
                "Space Ghost",
                "Wolverine",
                "Cyclops",
                "Storm",
                "Lone Ranger",
                "Tonto",
                "Supergirl",
            };//15

            // Construct ArrayList.
            arrList = new ArrayList((ICollection)strHeroes);
            arrSetRange = new ArrayList((ICollection)strSetRange);

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

                // SetRange entire array list.
                arrList.SetRange(start, arrSetRange);

                // Verify set.
                for (int ii = 0; ii < arrSetRange.Count; ++ii)
                {
                    Assert.Equal(0, ((string)arrList[start + ii]).CompareTo((String)arrSetRange[ii]));
                }

                //
                // []  Attempt invalid SetRange using collection that exceed valid range.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.SetRange(start, arrList));

                //
                // []  Attempt invalid SetRange using negative index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.SetRange(-100, arrSetRange));

                //
                //  []  Attempt SetRange using out of range index
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList.SetRange(1000, arrSetRange));

                //
                //  []  Attempt SetRange using null collection.
                //
                Assert.Throws<ArgumentNullException>(() => arrList.SetRange(0, null));
            }
        }
    }
}
