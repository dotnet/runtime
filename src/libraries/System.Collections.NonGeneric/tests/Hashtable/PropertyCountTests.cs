// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;
using System.Collections;

namespace System.Collections.HashtableTests
{
    public class CountTests
    {
        [Fact]
        public void TestGetCountBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            Hashtable ht1 = null;

            string s1 = null;
            string s2 = null;

            int i = 0;

            // Set - null val, should pass

            ht1 = new Hashtable(); //default constructor
            ht1["first key"] = null;
            Assert.Equal(1, ht1.Count);

            //[]  vanila
            ht1 = new Hashtable(); //default constructor
            ht1["first key"] = "first value";
            Assert.Equal(1, ht1.Count);

            //   Set again with a diff value
            ht1["first key"] = "second value";

            // now, size should still be one
            Assert.Equal(1, ht1.Count);

            //[]  Testcase:  add 50 key-val pairs and check the size
            ht1 = new Hashtable();
            for (i = 0; i < 100; i++)
            {
                sblMsg = new StringBuilder(99);
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg = new StringBuilder(99);
                sblMsg.Append("old_val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                ht1.Add(s1, s2);

                Assert.Equal(i + 1, ht1.Count);
            }

            //
            //[]  Testcase:  now set 100 key-val pairs, this includes the first 50 pairs that
            // just added, their values must be changed to the new values set
            //
            for (i = 0; i < 100; i++)
            {
                sblMsg = new StringBuilder(99);
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg = new StringBuilder(99);
                sblMsg.Append("new_val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                ht1[s1] = s2;
            }

            Assert.Equal(100, ht1.Count);

            //
            //[] Remove a key and then check size
            //
            sblMsg = new StringBuilder(99);
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            ht1.Remove(s1); //removes "Key_50"
            Assert.Equal(99, ht1.Count);

            //
            //[] clear everything and check the size
            //
            ht1.Clear();
            Assert.Equal(0, ht1.Count);
        }
    }
}
