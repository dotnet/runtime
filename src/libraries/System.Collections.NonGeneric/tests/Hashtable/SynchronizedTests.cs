// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

namespace System.Collections.HashtableTests
{
    public class SynchronizedTests
    {
        private Hashtable _hsh2;
        private int _iNumberOfElements = 20;

        [Fact]
        [OuterLoop]
        public void TestSynchronizedBasic()
        {
            Hashtable hsh1;

            string strValue;

            Task[] workers;
            Action ts1;
            int iNumberOfWorkers = 3;
            DictionaryEntry[] strValueArr;
            string[] strKeyArr;
            Hashtable hsh3;
            Hashtable hsh4;
            IDictionaryEnumerator idic;

            object oValue;

            //[]Vanila - Syncronized returns a wrapped HT. We must make sure that all the methods
            //are accounted for here for the wrapper
            hsh1 = new Hashtable();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                hsh1.Add("Key_" + i, "Value_" + i);
            }

            _hsh2 = Hashtable.Synchronized(hsh1);
            //Count
            Assert.Equal(_hsh2.Count, hsh1.Count);

            //get/set item
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                Assert.True(((string)_hsh2["Key_" + i]).Equals("Value_" + i));
            }

            Assert.Throws<ArgumentNullException>(() =>
                {
                    oValue = _hsh2[null];
                });

            _hsh2.Clear();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _hsh2["Key_" + i] = "Value_" + i;
            }

            strValueArr = new DictionaryEntry[_hsh2.Count];
            _hsh2.CopyTo(strValueArr, 0);
            //ContainsXXX
            hsh3 = new Hashtable();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                Assert.True(_hsh2.Contains("Key_" + i));
                Assert.True(_hsh2.ContainsKey("Key_" + i));
                Assert.True(_hsh2.ContainsValue("Value_" + i));

                //we still need a way to make sure that there are all these unique values here -see below code for that
                Assert.True(hsh1.ContainsValue(((DictionaryEntry)strValueArr[i]).Value));

                hsh3.Add(strValueArr[i], null);
            }

            hsh4 = (Hashtable)_hsh2.Clone();

            Assert.Equal(hsh4.Count, hsh1.Count);
            strValueArr = new DictionaryEntry[hsh4.Count];
            hsh4.CopyTo(strValueArr, 0);
            //ContainsXXX
            hsh3 = new Hashtable();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                Assert.True(hsh4.Contains("Key_" + i));
                Assert.True(hsh4.ContainsKey("Key_" + i));
                Assert.True(hsh4.ContainsValue("Value_" + i));

                //we still need a way to make sure that there are all these unique values here -see below code for that
                Assert.True(hsh1.ContainsValue(((DictionaryEntry)strValueArr[i]).Value));

                hsh3.Add(((DictionaryEntry)strValueArr[i]).Value, null);
            }

            Assert.False(hsh4.IsReadOnly);
            Assert.True(hsh4.IsSynchronized);

            //Phew, back to other methods
            idic = _hsh2.GetEnumerator();
            hsh3 = new Hashtable();
            hsh4 = new Hashtable();
            while (idic.MoveNext())
            {
                Assert.True(_hsh2.ContainsKey(idic.Key));
                Assert.True(_hsh2.ContainsValue(idic.Value));
                hsh3.Add(idic.Key, null);
                hsh4.Add(idic.Value, null);
            }

            hsh4 = (Hashtable)_hsh2.Clone();
            strValueArr = new DictionaryEntry[hsh4.Count];
            hsh4.CopyTo(strValueArr, 0);
            hsh3 = new Hashtable();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                Assert.True(hsh4.Contains("Key_" + i));
                Assert.True(hsh4.ContainsKey("Key_" + i));
                Assert.True(hsh4.ContainsValue("Value_" + i));

                //we still need a way to make sure that there are all these unique values here -see below code for that
                Assert.True(hsh1.ContainsValue(((DictionaryEntry)strValueArr[i]).Value));

                hsh3.Add(((DictionaryEntry)strValueArr[i]).Value, null);
            }

            Assert.False(hsh4.IsReadOnly);
            Assert.True(hsh4.IsSynchronized);

            //Properties
            Assert.False(_hsh2.IsReadOnly);
            Assert.True(_hsh2.IsSynchronized);
            Assert.Equal(_hsh2.SyncRoot, hsh1.SyncRoot);

            //we will test the Keys & Values
            string[] strValueArr11 = new string[hsh1.Count];
            strKeyArr = new string[hsh1.Count];
            _hsh2.Keys.CopyTo(strKeyArr, 0);
            _hsh2.Values.CopyTo(strValueArr11, 0);

            hsh3 = new Hashtable();
            hsh4 = new Hashtable();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                Assert.True(_hsh2.ContainsKey(strKeyArr[i]));
                Assert.True(_hsh2.ContainsValue(strValueArr11[i]));

                hsh3.Add(strKeyArr[i], null);
                hsh4.Add(strValueArr11[i], null);
            }

            //now we test the modifying methods
            _hsh2.Remove("Key_1");
            Assert.False(_hsh2.ContainsKey("Key_1"));
            Assert.False(_hsh2.ContainsValue("Value_1"));

            _hsh2.Add("Key_1", "Value_1");
            Assert.True(_hsh2.ContainsKey("Key_1"));
            Assert.True(_hsh2.ContainsValue("Value_1"));

            _hsh2["Key_1"] = "Value_Modified_1";
            Assert.True(_hsh2.ContainsKey("Key_1"));
            Assert.False(_hsh2.ContainsValue("Value_1"));
            ///////////////////////////

            Assert.True(_hsh2.ContainsValue("Value_Modified_1"));
            hsh3 = Hashtable.Synchronized(_hsh2);

            //we are not going through all of the above again:) we will just make sure that this syncrnized and that 
            //values are there
            Assert.Equal(hsh3.Count, hsh1.Count);
            Assert.True(hsh3.IsSynchronized);

            _hsh2.Clear();
            Assert.Equal(_hsh2.Count, 0);

            //[] Synchronized returns a HT that is thread safe
            // We will try to test this by getting a number of threads to write some items
            // to a synchronized IList
            hsh1 = new Hashtable();
            _hsh2 = Hashtable.Synchronized(hsh1);

            workers = new Task[iNumberOfWorkers];
            for (int i = 0; i < workers.Length; i++)
            {
                var name = "Thread worker " + i;
                ts1 = new Action(() => AddElements(name));
                workers[i] = Task.Run(ts1);
            }

            Task.WaitAll(workers);

            //checking time
            Assert.Equal(_hsh2.Count, _iNumberOfElements * iNumberOfWorkers);

            for (int i = 0; i < iNumberOfWorkers; i++)
            {
                for (int j = 0; j < _iNumberOfElements; j++)
                {
                    strValue = "Thread worker " + i + "_" + j;
                    Assert.True(_hsh2.Contains(strValue));
                }
            }

            //I dont think that we can make an assumption on the order of these items though
            //now we are going to remove all of these
            workers = new Task[iNumberOfWorkers];
            for (int i = 0; i < workers.Length; i++)
            {
                var name = "Thread worker " + i;
                ts1 = new Action(() => RemoveElements(name));
                workers[i] = Task.Run(ts1);
            }

            Task.WaitAll(workers);

            Assert.Equal(_hsh2.Count, 0);
            Assert.False(hsh1.IsSynchronized);
            Assert.True(_hsh2.IsSynchronized);

            //[] Tyr calling Synchronized with null
            Assert.Throws<ArgumentNullException>(() =>
                             {
                                 Hashtable.Synchronized(null);
                             }
            );
        }


        void AddElements(string strName)
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _hsh2.Add(strName + "_" + i, "string_" + i);
            }
        }

        void RemoveElements(string strName)
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _hsh2.Remove(strName + "_" + i);
            }
        }
    }
}
