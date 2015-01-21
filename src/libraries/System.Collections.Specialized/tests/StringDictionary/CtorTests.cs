// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorStringDictionaryTests
    {
        [Fact]
        public void Test01()
        {
            StringDictionary sd;
            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] Compare to null
            //
            if (sd == null)
            {
                Assert.False(true, string.Format("Error, collection is null after default ctor"));
            }

            // [] check Count
            //
            if (sd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", sd.Count));
            }

            // [] check other properties
            //
            if (sd.ContainsValue("string"))
            {
                Assert.False(true, string.Format("Error, ContainsValue() returned true after default ctor"));
            }

            if (sd.ContainsKey("string"))
            {
                Assert.False(true, string.Format("Error, ContainsKey() returned true after default ctor"));
            }

            //
            // IsSynchronized = false by default
            //
            if (sd.IsSynchronized)
            {
                Assert.False(true, string.Format("Error, IsSynchronized returned {0}", sd.IsSynchronized));
            }

            //
            // [] Add item and verify
            //
            sd.Add("key", "value");
            if (sd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0}", sd.Count));
            }

            if (!sd.ContainsKey("key"))
            {
                Assert.False(true, string.Format("Error, ContainsKey() returned false"));
            }

            if (!sd.ContainsValue("value"))
            {
                Assert.False(true, string.Format("Error, ContainsValue() returned false"));
            }
        }
    }
}
