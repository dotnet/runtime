// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class GetEnumeratorTests
    {
        [Fact]
        public void TestGetEnumeratorBasic()
        {
            Hashtable hash1;
            IDictionaryEnumerator dicEnum1;
            int ii;
            string str1;

            string strRetKey;
            int iExpValue;

            Queue que1;
            Queue que2;

            //[] 1) Empty Hash table
            //Get enumerator for an empty table
            hash1 = new Hashtable();
            dicEnum1 = hash1.GetEnumerator();

            Assert.Equal(dicEnum1.MoveNext(), false);

            //[] 2) Full hash table
            hash1 = new Hashtable();
            for (ii = 0; ii < 100; ii++)
            {
                str1 = string.Concat("key_", ii.ToString());
                hash1.Add(str1, ii);
            }

            dicEnum1 = hash1.GetEnumerator();

            ii = 0;
            //Enumerator (well, Hashtable really) does not hold values in the order we put them in!!
            //so we will use 2 ques to confirm that all the keys and values we put inside the hashtable was there
            que1 = new Queue();
            que2 = new Queue();

            while (dicEnum1.MoveNext() != false)
            {
                strRetKey = (string)dicEnum1.Key;
                iExpValue = Convert.ToInt32(dicEnum1.Value);

                //we make sure that the values are there
                Assert.True(hash1.ContainsKey(strRetKey));
                Assert.True(hash1.ContainsValue(iExpValue));

                //we will make sure that enumerator get all the objects
                que1.Enqueue(strRetKey);
                que2.Enqueue(iExpValue);
            }

            //making sure the que size is right
            Assert.Equal(100, que1.Count);
            Assert.Equal(100, que2.Count);

            // 3) Full hash table, allow remove true

            hash1 = new Hashtable();
            for (ii = 0; ii < 100; ii++)
            {
                str1 = string.Concat("key_", ii.ToString());
                hash1.Add(str1, ii);
            }

            dicEnum1 = hash1.GetEnumerator();

            //Enumerator (well, Hashtable really) does not hold values in the order we put them in!!
            //so we will use 2 ques to confirm that all the keys and values we put inside the hashtable was there
            que1 = new Queue();
            que2 = new Queue();

            while (dicEnum1.MoveNext() != false)
            {
                strRetKey = (string)dicEnum1.Key;
                iExpValue = Convert.ToInt32(dicEnum1.Value);

                //we make sure that the values are there

                Assert.True(hash1.ContainsKey(strRetKey));
                Assert.True(hash1.ContainsValue(iExpValue));

                //we will make sure that enumerator get all the objects
                que1.Enqueue(strRetKey);
                que2.Enqueue(iExpValue);
            }

            //making sure the que size is right
            Assert.Equal(100, que1.Count);
            Assert.Equal(100, que2.Count);

            //[] 3) change hash table externally whilst in the middle of enumerating

            hash1 = new Hashtable();
            for (ii = 0; ii < 100; ii++)
            {
                str1 = string.Concat("key_", ii.ToString());
                hash1.Add(str1, ii);
            }

            dicEnum1 = hash1.GetEnumerator();

            //this is the first object. we have a true
            Assert.True(dicEnum1.MoveNext());
            strRetKey = (string)dicEnum1.Key;
            iExpValue = Convert.ToInt32(dicEnum1.Value);

            //we make sure that the values are there
            Assert.True(hash1.ContainsKey(strRetKey));
            Assert.True(hash1.ContainsValue(iExpValue));

            // we will change the underlying hashtable
            ii = 87;

            str1 = string.Concat("key_", ii.ToString());
            hash1[str1] = ii;

            // MoveNext should throw
            Assert.Throws<InvalidOperationException>(() =>
                             {
                                 dicEnum1.MoveNext();
                             }
            );

            // Value should NOT throw
            object foo = dicEnum1.Value;
            object foo1 = dicEnum1.Key;

            // Current should NOT throw
            object foo2 = dicEnum1.Current;

            Assert.Throws<InvalidOperationException>(() =>
                             {
                                 dicEnum1.Reset();
                             }
            );

            //[] 5) try getting object
            //			a) before moving
            //			b) twice before moving to next
            //			c) after next returns false

            hash1 = new Hashtable();
            for (ii = 0; ii < 100; ii++)
            {
                str1 = string.Concat("key_", ii.ToString());
                hash1.Add(str1, ii);
            }

            dicEnum1 = hash1.GetEnumerator();
            // a) before moving
            Assert.Throws<InvalidOperationException>(() =>
                             {
                                 strRetKey = (string)dicEnum1.Key;
                             }
            );

            //Entry dicEnum1 = (IDictionaryEnumerator *)hash1.GetEnumerator();
            dicEnum1 = hash1.GetEnumerator();
            while (dicEnum1.MoveNext() != false)
            {
                ;//dicEntr1 = (dicEnum1.Entry);
            }

            Assert.Throws<InvalidOperationException>(() =>
                             {
                                 strRetKey = (string)dicEnum1.Key;
                             }
            );

            //[]We will get a valid enumerator, move to a valid position, then change the underlying HT. Current shouold not throw.
            //Only calling MoveNext should throw.

            //create the HT
            hash1 = new Hashtable();
            for (ii = 0; ii < 100; ii++)
            {
                str1 = string.Concat("key_", ii.ToString());
                hash1.Add(str1, ii);
            }

            //get the enumerator and move to a valid location
            dicEnum1 = hash1.GetEnumerator();

            dicEnum1.MoveNext();
            dicEnum1.MoveNext();

            ii = 87;
            str1 = string.Concat("key_", ii.ToString());

            //if we are currently pointer at the item that we are going to change then move to the next one
            if (0 == string.Compare((string)dicEnum1.Key, str1))
            {
                dicEnum1.MoveNext();
            }

            //change the underlying HT
            hash1[str1] = ii + 50;

            //calling current on the Enumerator should not throw
            strRetKey = (string)dicEnum1.Key;
            iExpValue = Convert.ToInt32(dicEnum1.Value);

            Assert.True(hash1.ContainsKey(strRetKey));
            Assert.True(hash1.ContainsValue(iExpValue));

            strRetKey = (string)dicEnum1.Entry.Key;
            iExpValue = Convert.ToInt32(dicEnum1.Entry.Value);

            Assert.True(hash1.ContainsKey(strRetKey));
            Assert.True(hash1.ContainsValue(iExpValue));

            strRetKey = (string)(((DictionaryEntry)(dicEnum1.Current)).Key);
            iExpValue = Convert.ToInt32(((DictionaryEntry)dicEnum1.Current).Value);

            Assert.True(hash1.ContainsKey(strRetKey));
            Assert.True(hash1.ContainsValue(iExpValue));

            // calling MoveNExt should throw

            Assert.Throws<InvalidOperationException>(() =>
                             {
                                 dicEnum1.MoveNext();
                             }
            );

            //[] Try calling clone on the enumerator
            dicEnum1 = hash1.GetEnumerator();
            var anotherEnumerator = hash1.GetEnumerator();
            Assert.False(object.ReferenceEquals(dicEnum1, anotherEnumerator));
        }
    }
}
