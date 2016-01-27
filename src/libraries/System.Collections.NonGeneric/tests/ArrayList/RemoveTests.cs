// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class ArrayList_RemoveTests
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
