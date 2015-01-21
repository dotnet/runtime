// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorStringCollectionTests
    {
        [Fact]
        public void Test01()
        {
            StringCollection sc;

            // [] StringCollection is constructed as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] compare to null
            //
            if (sc == null)
            {
                Assert.False(true, string.Format("Error, collection is null after default ctor"));
            }

            // [] check Count
            //
            if (sc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", sc.Count));
            }

            // [] check more properties
            //
            if (sc.Contains("string"))
            {
                Assert.False(true, string.Format("Error, Contains() returned true after default ctor"));
            }

            if (sc.IsReadOnly)
            {
                Assert.False(true, string.Format("Error, IsReadOnly returned {0}", sc.IsReadOnly));
            }

            //
            // IsSynchronized = false by default
            //
            if (sc.IsSynchronized)
            {
                Assert.False(true, string.Format("Error, IsSynchronized returned {0}", sc.IsSynchronized));
            }
        }
    }
}
