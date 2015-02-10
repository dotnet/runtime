// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class ContainsTests
    {
        [Fact]
        public void TestContainsBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            Hashtable ht1 = null;

            string s1 = null;
            string s2 = null;

            int i = 0;

            ht1 = new Hashtable(); //defalult constructor
            Assert.False(ht1.Contains("No_Such_Key"));

            /// []  Testcase: add few key-val pairs
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

            for (i = 0; i < ht1.Count; i++)
            {
                sblMsg = new StringBuilder(99);
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                Assert.True(ht1.Contains(s1));
            }

            //
            // Remove a key and then check
            //
            sblMsg = new StringBuilder(99);
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            ht1.Remove(s1); //removes "Key_50"
            Assert.False(ht1.Contains(s1));
        }
    }
}
