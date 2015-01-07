// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class Co8782ctor
    {
        [Fact]
        public void Test01()
        {
            ListDictionary ld;

            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------

            ld = new ListDictionary();


            if (ld == null)
            {
                Assert.False(true, string.Format("Error, dictionary is null after default ctor"));
            }

            if (ld.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", ld.Count));
            }

            if (ld["key"] != null)
            {
                Assert.False(true, string.Format("Error, Item(some_key) returned non-null after default ctor"));
            }

            System.Collections.ICollection keys = ld.Keys;
            if (keys.Count != 0)
            {
                Assert.False(true, string.Format("Error, Keys contains {0} keys after default ctor", keys.Count));
            }

            System.Collections.ICollection values = ld.Values;
            if (values.Count != 0)
            {
                Assert.False(true, string.Format("Error, Values contains {0} items after default ctor", values.Count));
            }

            //
            // [] Add(string, string) 
            //
            ld.Add("Name", "Value");
            if (ld.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", ld.Count));
            }
            if (String.Compare(ld["Name"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }

            //
            // [] Clear() 
            //
            ld.Clear();
            if (ld.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", ld.Count));
            }
            if (ld["Name"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value after Clear()"));
            }

            //
            // [] elements not overriding Equals() 
            //
            ld.Clear();
            Hashtable lbl = new Hashtable();
            Hashtable lbl1 = new Hashtable();
            ArrayList b = new ArrayList();
            ArrayList b1 = new ArrayList();
            ld.Add(lbl, b);
            ld.Add(lbl1, b1);
            if (ld.Count != 2)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 2", ld.Count));
            }
            if (!ld.Contains(lbl))
            {
                Assert.False(true, string.Format("Error, doesn't contain 1st special item"));
            }
            if (!ld.Contains(lbl1))
            {
                Assert.False(true, string.Format("Error, doesn't contain 2nd special item"));
            }
            if (ld.Values.Count != 2)
            {
                Assert.False(true, string.Format("Error, Values.Count returned {0} instead of 2", ld.Values.Count));
            }

            ld.Remove(lbl1);
            if (ld.Count != 1)
            {
                Assert.False(true, string.Format("Error, failed to remove item"));
            }
            if (ld.Contains(lbl1))
            {
                Assert.False(true, string.Format("Error, failed to remove special item"));
            }
        }
    }
}
