// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class ArrayList_ClearTests
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
