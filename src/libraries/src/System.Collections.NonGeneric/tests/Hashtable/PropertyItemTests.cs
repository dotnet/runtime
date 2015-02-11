// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class ItemTests
    {
        public void TestGetItem()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            Hashtable ht1 = null;

            string s1 = null;
            string s2 = null;
            string s3 = null;

            int i = 0;

            // []  No_Such_Get
            ht1 = new Hashtable(); //default constructor

            s3 = (string)ht1["No_Such_Get"];
            Assert.Null(s3);

            /// add few key-val pairs
            ht1 = new Hashtable();
            for (i = 0; i < 100; i++)
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

            //
            // []  test Get
            //
            for (i = 0; i < ht1.Count; i++)
            {
                sblMsg = new StringBuilder(99);
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg = new StringBuilder(99);
                sblMsg.Append("val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                s3 = (string)ht1[s1];
                Assert.Equal(s3, s2);
            }

            //
            // Remove a key and then check
            //
            sblMsg = new StringBuilder(99);
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            ht1.Remove(s1); //removes "Key_50"
            s3 = (string)ht1[s1];
            Assert.Null(s3);
        }

        public void TestSetItem()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            Hashtable ht1 = null;

            string s1 = null;
            string s2 = null;
            string s3 = null;

            int i = 0;

            // []  Testcase: Set - null key, ArgExc expected
            ht1 = new Hashtable(); //default constructor
            Assert.NotNull(ht1[0]);
            Assert.Equal(ht1.Count, 0);

            // []  Testcase: Set - null val, should pass
            ht1 = new Hashtable(); //default constructor
            ht1["first key"] = null;
            Assert.Equal(ht1.Count, 1);

            // []  vanila Set
            ht1 = new Hashtable(); //default constructor
            ht1["first key"] = "first value";
            Assert.Equal(ht1.Count, 1);

            // check to see whether the key is there
            Assert.True(ht1.ContainsKey("first key"));

            // Get and check the value
            s2 = (string)ht1["first key"];
            Assert.Equal("first value", s2);

            // []  Testcase: Set again with a diff value
            ht1["first key"] = "second value";
            Assert.Equal(ht1.Count, 1);

            // now, Get again and check the value set
            s2 = (string)ht1["first key"];
            Assert.Equal("second value", s2);

            // add 50 key-val pairs
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
            }

            //
            // [] now set 100 key-val pairs, this includes the first 50 pairs that
            // we just added, their values must be changed to the new values set
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

            Assert.Equal(ht1.Count, 100);

            // Testcase:  check the values
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

                s3 = (string)ht1[s1];

                Assert.Equal(s3, s2);
            }
        }
    }
}
