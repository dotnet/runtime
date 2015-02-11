// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;

namespace System.Collections.HashtableTests
{
    public class SyncRootTests
    {
        private Hashtable _arrDaughter;
        private Hashtable _arrGrandDaughter;
        private Int32 _iNumberOfElements = 100;

        [Fact]
        public void TestGetSyncRootBasic()
        {
            Hashtable arrSon;
            Hashtable arrMother;

            Task[] workers;
            Action ts1;
            Action ts2;
            Int32 iNumberOfWorkers = 30;
            Hashtable hshPossibleValues;
            IDictionaryEnumerator idic;

            Hashtable hsh1;
            Hashtable hsh2;
            Hashtable hsh3;

            //[] we will create different HT and make sure that they return different SyncRoot values
            hsh1 = new Hashtable();
            hsh2 = new Hashtable();

            Assert.NotEqual(hsh1.SyncRoot, hsh2.SyncRoot);
            Assert.Equal(hsh1.SyncRoot.GetType(), typeof(object));

            //[] a clone of a Syncronized HT should not be pointing to the same SyncRoot
            hsh1 = new Hashtable();
            hsh2 = Hashtable.Synchronized(hsh1);
            hsh3 = (Hashtable)hsh2.Clone();

            Assert.NotEqual(hsh2.SyncRoot, hsh3.SyncRoot);

            Assert.NotEqual(hsh1.SyncRoot, hsh3.SyncRoot);

            //[] testing SyncRoot is not as simple as its implementation looks like. This is the working
            //scenrio we have in mind.
            //1) Create your Down to earth mother Hashtable
            //2) Get a synchronized wrapper from it
            //3) Get a Synchronized wrapper from 2)
            //4) Get a synchronized wrapper of the mother from 1)
            //5) all of these should SyncRoot to the mother earth

            arrMother = new Hashtable();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                arrMother.Add("Key_" + i, "Value_" + i);
            }

            arrSon = Hashtable.Synchronized(arrMother);
            _arrGrandDaughter = Hashtable.Synchronized(arrSon);
            _arrDaughter = Hashtable.Synchronized(arrMother);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);
            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);
            Assert.Equal(_arrGrandDaughter.SyncRoot, arrMother.SyncRoot);
            Assert.Equal(_arrDaughter.SyncRoot, arrMother.SyncRoot);
            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            //we are going to rumble with the Hashtables with some threads
            workers = new Task[iNumberOfWorkers];
            ts2 = new Action(RemoveElements);
            for (int iThreads = 0; iThreads < iNumberOfWorkers; iThreads += 2)
            {
                var name = "Thread_worker_" + iThreads;
                ts1 = new Action(() => AddMoreElements(name));

                workers[iThreads] = Task.Run(ts1);
                workers[iThreads + 1] = Task.Run(ts2);
            }

            Task.WaitAll(workers);

            //checking time
            //Now lets see how this is done.
            //Either there should be some elements (the new ones we added and/or the original ones) or none
            hshPossibleValues = new Hashtable();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                hshPossibleValues.Add("Key_" + i, "Value_" + i);
            }

            for (int i = 0; i < iNumberOfWorkers; i++)
            {
                hshPossibleValues.Add("Key_Thread_worker_" + i, "Thread_worker_" + i);
            }

            idic = arrMother.GetEnumerator();

            while (idic.MoveNext())
            {
                Assert.True(hshPossibleValues.ContainsKey(idic.Key), "Error, Expected value not returned, " + idic.Key);
                Assert.True(hshPossibleValues.ContainsValue(idic.Value), "Error, Expected value not returned, " + idic.Value);
            }
        }

        void AddMoreElements(string threadName)
        {
            _arrGrandDaughter.Add("Key_" + threadName, threadName);
        }

        void RemoveElements()
        {
            _arrDaughter.Clear();
        }
    }
}
