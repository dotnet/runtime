// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class CountTests
    {
        [Fact]
        public void TestGetCountBasic()
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
            arrList = new ArrayList((ICollection)strHeroes);
            Assert.NotNull(arrList);

            //
            // []  Verify array list size.
            //
            Assert.Equal(strHeroes.Length, arrList.Count);

            //
            // []  Verify size of empty array list.
            //
            arrList = new ArrayList();
            // Verify size.
            Assert.Equal(0, arrList.Count);
        }
    }
}
