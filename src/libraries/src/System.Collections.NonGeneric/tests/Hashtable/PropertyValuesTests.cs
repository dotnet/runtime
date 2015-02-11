// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;
using System.Collections;

namespace System.Collections.HashtableTests
{
    public class ValueTests
    {
        public void TestGetValues()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            Hashtable ht1 = null;

            string s1 = null;
            string s2 = null;
            string s3 = null;

            object[] objArrKeys;
            object[] objArrValues;

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

                Assert.Equal(s3 , s2);
            }

            //
            // []  now GetValues and make sure every value is obtained
            //  NOTE: the keys/values obtained may not necessarily be in any order
            //
            int temp = ht1.Count;
            objArrValues = new object[temp];
                ht1.Values.CopyTo(objArrValues, 0);

                Assert.Equal(objArrValues.Length, ht1.Count);

                for (i = 0; i < objArrValues.Length; i++)
                {
                    Assert.True(ht1.ContainsValue(objArrValues[i].ToString()));
                }

            //[] Returns a collection representing the keys of this hashtable. The order in which the returned collection represents the keys is unspecified, but it is guaranteed to be the same order in which a collection returned by GetValues represents the values of the hashtable.

            int temp1 = ht1.Count;
            objArrKeys = new object[temp1];
            ht1.Keys.CopyTo(objArrKeys, 0);

            Assert.Equal(objArrKeys.Length, ht1.Count);
            for (i = 0; i < objArrKeys.Length; i++)
            {
                Assert.True(ht1.ContainsKey(objArrKeys[i].ToString()));

                // check the order of GetKeys and GetValues
                Assert.True(((string)ht1[objArrKeys[i].ToString()]).Equals(objArrValues[i].ToString()));
            }

            //[]We test other methods in the Collection to make sure that they work!!
            icol1 = ht1.Values;
            ienm1 = icol1.GetEnumerator();
            iCount = 0;
            while (ienm1.MoveNext())
            {
                iCount++;
                Assert.True(ht1.ContainsValue(ienm1.Current));
            }

            Assert.Equal(iCount, ht1.Count);
            Assert.Equal(icol1.Count, ht1.Count);
            Assert.False(icol1.IsSynchronized);
            Assert.Equal(icol1.SyncRoot, ht1.SyncRoot);

            //[] The ICollection is alive - i.e. whatever changes we do to the Underlying HT is reflected in the collection
            sblMsg = new StringBuilder();
            sblMsg.Append("key_");
            sblMsg.Append(0);
            s1 = "key_0";
            s2 = "val_0";

            ht1.Remove(s1);
            ienm1 = icol1.GetEnumerator();
            iCount = 0;
            while (ienm1.MoveNext())
            {
                s3 = (string)ienm1.Current;
                Assert.False(s3.Equals(s2));
            }

            Assert.Equal(icol1.Count, ht1.Count);

            //[] we will clear the HT and make sure that the Values are kaput
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