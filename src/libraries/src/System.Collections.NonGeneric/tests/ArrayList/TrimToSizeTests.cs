// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class ArrayList_TrimToSizeTests
    {
        [Fact]
        public void TestTrimToSizeBasic()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;

            string[] strHeroes =
            {
                "Green Arrow",
                "Atom",
                "Batman",
                "Steel",
                "Superman",
                "Wonder Woman",
                "Hawkman",
                "Flash",
                "Aquaman",
                "Green Lantern",
                "Catwoman",
                "Huntress",
                "Robin",
                "Captain Atom",
                "Wildcat",
                "Nightwing",
                "Ironman",
                "SpiderMan",
                "Black Canary",
                "Thor",
                "Cyborg",
                "Captain America",
            };

            //
            // Construct array list.
            //
            arrList = new ArrayList((ICollection)strHeroes);

            //
            // []  Verify TrimToSize.
            //
            // Set capacity greater than the size of the ArrayList.
            arrList.Capacity = 2 * arrList.Count;
            Assert.True(arrList.Capacity > arrList.Count);

            // Verify TrimToSize
            arrList.TrimToSize();
            Assert.Equal(arrList.Count, arrList.Capacity);
        }
    }
}
