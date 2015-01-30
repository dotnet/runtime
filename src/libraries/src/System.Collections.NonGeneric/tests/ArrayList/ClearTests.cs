// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class ClearTests
    {
        [Fact]
        public void TestClearBasic()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            string[] strHeroes = new string[]
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

            //[]  Clear list with elements

            // Construct ArrayList.
            ArrayList arrList = new ArrayList(strHeroes);
            arrList.Clear();

            Assert.Equal(0, arrList.Count);

            //[]  Clear list with no elements
            // Construct ArrayList.
            arrList = new ArrayList();
            arrList.Clear();
            Assert.Equal(0, arrList.Count);
        }
    }
}
