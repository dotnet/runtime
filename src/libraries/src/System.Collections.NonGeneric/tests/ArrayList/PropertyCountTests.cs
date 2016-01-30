// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class ArrayList_CountTests
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
