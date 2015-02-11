// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Collections;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class KeysTests
    {
        [Fact]
        public void TestGetKeys()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            Hashtable ht1 = null;

            string s1 = null;
            string s2 = null;
            string s3 = null;

            object[] objArr;

            ICollection icol1;
            IEnumerator ienm1;

            int i = 0;
            int iCount = 0;

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
            // test Get to make sure the key-vals are accessible
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
            // []  now GetKeys and make sure every key is obtained
            //  NOTE: the keys obtained may not necessarily be in any order
            //
            int temp = ht1.Count;
            objArr = new object[temp];

            ht1.Keys.CopyTo(objArr, 0);
            Assert.Equal(objArr.Length, ht1.Count);

            for (i = 0; i < objArr.Length; i++)
            {
                Assert.True(ht1.ContainsKey(objArr[i].ToString()));
            }

            // [] We test other methods in the Collection to make sure that they work!!
            icol1 = ht1.Keys;
            ienm1 = icol1.GetEnumerator();
            iCount = 0;
            while (ienm1.MoveNext())
            {
                iCount++;
                Assert.True(ht1.ContainsKey(ienm1.Current));
            }

            Assert.Equal(iCount, ht1.Count);
            Assert.Equal(icol1.Count, ht1.Count);
            Assert.False(icol1.IsSynchronized);
            Assert.Equal(icol1.SyncRoot, ht1.SyncRoot);

            Assert.Throws<ArgumentException>( () => 
            {
                object[,] mdObjArray = new object[ht1.Keys.Count, ht1.Keys.Count];
                ht1.Keys.CopyTo(mdObjArray, 0);
            });

            //[] The ICollection is alive - i.e. whatever changes we do to the Underlying HT is reflected in the collection
            sblMsg = new StringBuilder();
            sblMsg.Append("key_");
            sblMsg.Append(0);
            s1 = sblMsg.ToString();

            ht1.Remove(s1);
            ienm1 = icol1.GetEnumerator();
            iCount = 0;
            while (ienm1.MoveNext())
            {
                s3 = (string)ienm1.Current;
                Assert.NotEqual(s1, s3);
            }

            Assert.Equal(icol1.Count, ht1.Count);
            //[] we will clear the HT and make sure that the Keys are kaput
            ht1.Clear();
            Assert.Equal(0, icol1.Count);

            iCount = 0;
            ienm1 = icol1.GetEnumerator();
            while (ienm1.MoveNext())
            {
                iCount++;
            }

            Assert.Equal(0, iCount);
        }
    }
}