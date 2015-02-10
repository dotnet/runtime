// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;

namespace System.Collections.SortedListTests
{
    public class SyncRootTests
    {
        private SortedList _arrDaughter;
        private SortedList _arrGrandDaughter;
        private Int32 _iNumberOfElements = 100;

        [Fact]
        [OuterLoop]
        public void TestGetSyncRootBasic()
        {
            SortedList arrSon;
            SortedList arrMother;

            Task[] workers;
            Action ts1;
            Action ts2;
            Int32 iNumberOfWorkers = 4;

            SortedList hshPossibleValues;
            IDictionaryEnumerator idic;

            // Vanila test case - testing SyncRoot is not as simple as its implementation looks like. This is the working
            //scenrio we have in mind.
            //1) Create your Down to earth mother SortedList
            //2) Get a synchronized wrapper from it
            //3) Get a Synchronized wrapper from 2)
            //4) Get a synchronized wrapper of the mother from 1)
            //5) all of these should SyncRoot to the mother earth

            arrMother = new SortedList();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                arrMother.Add("Key_" + i, "Value_" + i);
            }

            Assert.Equal(arrMother.SyncRoot.GetType(), typeof(Object));

            arrSon = SortedList.Synchronized(arrMother);
            _arrGrandDaughter = SortedList.Synchronized(arrSon);
            _arrDaughter = SortedList.Synchronized(arrMother);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(_arrGrandDaughter.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(_arrDaughter.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            //we are going to rumble with the SortedLists with some threads

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
            //Either there are some elements or none
            hshPossibleValues = new SortedList();
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                hshPossibleValues.Add("Key_" + i, "Value_" + i);
            }
            for (int i = 0; i < iNumberOfWorkers; i++)
            {
                hshPossibleValues.Add("Key_Thread_worker_" + i, "Thread_worker_" + i);
            }

            //lets check the values if
                idic = arrMother.GetEnumerator();
            while (idic.MoveNext())
            {
                Assert.True(hshPossibleValues.ContainsKey(idic.Key), "Error, Expected value not returned, " + idic.Key);
                Assert.True(hshPossibleValues.ContainsValue(idic.Value), "Error, Expected value not returned, " + idic.Value);
            }
        }

        void AddMoreElements(String threadName)
        {
                _arrGrandDaughter.Add("Key_" + threadName, threadName);
        }

        void RemoveElements()
        {
            _arrDaughter.Clear();
        }
    }
}
