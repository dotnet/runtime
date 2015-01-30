// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;

namespace System.Collections.ArrayListTests
{
    public class RemoveTests
    {
        [Fact]
        public void TestNullItems()
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

            //[] try removing each element make sure count goes down by one also
            // Construct ArrayList.
            arrList = new ArrayList(strHeroes);

            for (int i = 0; i < strHeroes.Length; i++)
            {
                arrList.Remove(strHeroes[i]);
                Assert.Equal(strHeroes.Length - i - 1, arrList.Count);
            }

            //[]  make sure count goes back to 0
            // Construct ArrayList.
            arrList = new ArrayList();
            arrList.Add(null);
            arrList.Add(arrList);
            arrList.Add(null);
            arrList.Remove(arrList);
            arrList.Remove(null);
            arrList.Remove(null);

            Assert.Equal(0, arrList.Count);

            //[]  remove from empty list
            // No Exception
            arrList = new ArrayList();
            arrList.Remove(null);

            //[]  remove elemnt which does not exist should throw
            arrList.Add(arrList);
        }
    }
}
