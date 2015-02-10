// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class ClearTests
    {
        [Fact]
        public void TestClearBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);
            Hashtable ht1 = null;
            string s1 = null;
            string s2 = null;

            int i = 0;
            ht1 = new Hashtable(); //defalult constructor
            ht1.Clear();

            Assert.Equal(0, ht1.Count);

            // add 100 key-val pairs
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

            Assert.Equal(100, ht1.Count);
            ht1.Clear();

            Assert.Equal(0, ht1.Count);

            //[]we will make a token call for some important methods to make sure that this is indeed clear
            s1 = "key_0";
            Assert.False(ht1.ContainsKey(s1));

            s1 = "val_0";
            Assert.False(ht1.ContainsValue(s1));

            //[]repeated clears of the HT. Nothing should happen		

            for (i = 0; i < 100; i++)
            {
                ht1.Clear();

                Assert.Equal(0, ht1.Count);
            }
        }
    }
}
