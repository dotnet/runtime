// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;
using System.Collections;

namespace System.Collections.HashtableTests
{
    public class ContainsValueTests
    {
        [Fact]
        public void TestContainsValueBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            Hashtable ht1 = null;

            string s1 = null;
            string s2 = null;

            ht1 = new Hashtable(); //default constructor
            Assert.False(ht1.ContainsValue("No_Such_Val"));

            /// add few key-val pairs
            ht1 = new Hashtable();
            for (int i = 0; i < 100; i++)
            {
                sblMsg = new StringBuilder(99);
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg = new StringBuilder(99);
                sblMsg.Append("val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                ht1.Add(s1, s2);
            }

            for (int i = 0; i < ht1.Count; i++)
            {
                sblMsg = new StringBuilder(99);
                sblMsg.Append("val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();


                Assert.True(ht1.ContainsValue(s2));
            }

            //
            // Remove a key and then check
            //
            sblMsg = new StringBuilder(99);
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            ht1.Remove(s1); //removes "Key_50"

            sblMsg = new StringBuilder(99);
            sblMsg.Append("val_50");
            s2 = sblMsg.ToString();

            Assert.False(ht1.ContainsValue(s2));

            //
            // Look for a null element after two element have been added with the same hashcode then one removed
            //
            sblMsg = new StringBuilder(99);
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            ht1 = new Hashtable();
            int i1 = 0x10;
            int i2 = 0x100;
            long l1 = (((long)i1) << 32) + i2;// create two longs with same hashcode
            long l2 = (((long)i2) << 32) + i1;
            string str1 = "string 1";

            object o1 = l1;
            object o2 = l2;
            ht1.Add(o1, str1);
            ht1.Add(o2, str1);      // this will cause collision bit of the first entry to be set
            ht1.Remove(o1);         // remove the first item, the hashtable should not

            // contain null value
            Assert.False(ht1.ContainsValue(0));

            //
            // Look for a null element when the collection contains a null element
            //
            ht1 = new Hashtable();
            for (int i = 0; i < 256; i++)
            {
                ht1.Add(i.ToString(), i);
            }

            ht1.Add(ht1.Count.ToString(), 0);
            Assert.True(ht1.ContainsValue(0));
        }
    }
}
