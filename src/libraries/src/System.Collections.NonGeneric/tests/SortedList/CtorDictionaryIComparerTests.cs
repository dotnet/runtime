// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class CtorDictionaryIComparerTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }

        [Fact]
        public void TestCtorDictionaryIComparerBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            SortedList sl2 = null;
            Hashtable ht = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            //
            //  Construct a hashtable with 3 elements in an unsorted order
            //
            ht = new Hashtable();
            ht.Add("key_2", "val_2"); //add elements in randomn order
            ht.Add("key_0", "val_0"); //add elements in randomn order
            ht.Add("key_1", "val_1"); //add elements in randomn order

            //
            // 	Constructor: Create SortedList using this as IComparer and default settings.
            //
            sl2 = new SortedList(ht, this);
            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList is not empty.
            Assert.Equal(3, sl2.Count);

            //  Verify that the SortedList is actually sorted.
            Assert.True(((string)sl2["key_0"]).Equals("val_0"));

            Assert.True(((string)sl2["key_1"]).Equals("val_1"));

            Assert.True(((string)sl2["key_2"]).Equals("val_2"));

            sl2 = new SortedList(ht, null);

            //
            // 	We should not be able to construct using null dictionary
            //
            Assert.Throws<ArgumentNullException>(() =>
                             {
                                 sl2 = new SortedList(null, this);
                             }
            );
        }
    }
}
