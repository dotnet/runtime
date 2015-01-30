// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class ContainsTests
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

            //[]  Vanila Contains

            // Construct ArrayList.
            arrList = new ArrayList(strHeroes);

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
                for (int i = 0; i < strHeroes.Length; i++)
                {
                    Assert.True(arrList.Contains(strHeroes[i]), "Error, Contains returns false but shour return true at position " + i.ToString());
                }

                if (!arrList.IsFixedSize)
                {
                    //[]  Normal Contains which expects false
                    for (int i = 0; i < strHeroes.Length; i++)
                    {
                        // remove element, if element is in 2 times make sure we remove it completely.
                        for (int j = 0; j < strHeroes.Length; j++)
                        {
                            arrList.Remove(strHeroes[i]);
                        }

                        Assert.False(arrList.Contains(strHeroes[i]), "Error, Contains returns true but should return false at position " + i.ToString());
                    }
                }

                if (!arrList.IsFixedSize)
                {
                    //[]  Normal Contains on empty list
                    arrList.Clear();

                    for (int i = 0; i < strHeroes.Length; i++)
                    {
                        Assert.False(arrList.Contains(strHeroes[i]), "Error, Contains returns true but should return false at position " + i.ToString());
                    }
                }
            }
        }
    }
}
