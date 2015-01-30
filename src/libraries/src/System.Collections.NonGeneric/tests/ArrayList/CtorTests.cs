// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class CtorTests
    {
        [Fact]
        public void CtorTest()
        {
            ArrayList arrList = null;
            //
            // []  Construct ArrayList.
            //
            arrList = new ArrayList();
            Assert.NotNull(arrList);

            //
            // []  Verify new ArrayList.
            //
            Assert.Equal(0, arrList.Count);
            Assert.Equal(0, arrList.Capacity);
        }

        [Fact]
        public void CtorIntTest()
        {
            //
            // []  Construct ArrayList with capacity of 16 entries.
            //
            int nCapacity = 16;
            ArrayList arrList = new ArrayList(nCapacity);
            Assert.NotNull(arrList);

            //
            // Verify new ArrayList.
            //
            Assert.Equal(nCapacity, arrList.Capacity);

            //
            // []  Bogus negative capacity.
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => arrList = new ArrayList(-1000));
        }

        [Fact]
        public void CtorCollectionTest()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList = null;
            ArrayList arrListColl = null;
            int nItems = 100;

            //
            // Construct ArrayList.
            //
            // Construct ArrayList.
            arrList = new ArrayList();

            // Add items to list.
            for (int ii = 0; ii < nItems; ++ii)
            {
                arrList.Add(ii.ToString());
            }

            // Verify items added to list.
            Assert.Equal(nItems, arrList.Count);

            //
            // []  Construct new ArrayList from current ArrayList (collection)
            //
            arrListColl = new ArrayList(arrList);

            // Verify the size of the new ArrayList.
            Assert.Equal(nItems, arrListColl.Count);

            //
            // []  Attempt invalid construction (parm)
            //
            Assert.Throws<ArgumentNullException>(() => arrListColl = new ArrayList(null));
        }
    }
}
