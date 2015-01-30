// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;

namespace System.Collections.ArrayListTests
{
    public class SyncRootTests
    {
        private ArrayList _arrDaughter;
        private ArrayList _arrGrandDaughter;

        [Fact]
        public void TestGetSyncRoot()
        {
            ArrayList arrSon, arrSon2;
            ArrayList arrMother;

            int iNumberOfElements = 100;
            int iValue;
            bool fDescending, fWrongResult;

            Task[] workers;
            Action ts1;
            Action ts2;
            int iNumberOfWorkers = 100;

            //[] Vanila test case - testing SyncRoot is not as simple as its implementation looks like. This is the working
            //scenrio we have in mind.
            //1) Create your Down to earth mother ArrayList
            //2) Get a Fixed wrapper from it
            //3) Get a Synchronized wrapper from 2)
            //4) Get a synchronized wrapper of the mother from 1)
            //5) all of these should SyncRoot to the mother earth

            arrMother = new ArrayList();
            for (int i = 0; i < iNumberOfElements; i++)
            {
                arrMother.Add(i);
            }

            arrSon = ArrayList.FixedSize(arrMother);
            arrSon2 = ArrayList.ReadOnly(arrMother);
            _arrGrandDaughter = ArrayList.Synchronized(arrSon);
            _arrDaughter = ArrayList.Synchronized(arrMother);

            Assert.False(arrMother.SyncRoot is ArrayList);

            Assert.False(arrSon.SyncRoot is ArrayList);

            Assert.False(arrSon2.SyncRoot is ArrayList);

            Assert.False(_arrDaughter.SyncRoot is ArrayList);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            Assert.False(_arrGrandDaughter.SyncRoot is ArrayList);

            //we are going to rumble with the ArrayLists with 2 threads

            workers = new Task[iNumberOfWorkers];
            ts1 = new Action(SortElements);
            ts2 = new Action(ReverseElements);
            for (int iThreads = 0; iThreads < iNumberOfWorkers; iThreads += 2)
            {

                workers[iThreads] = Task.Factory.StartNew(ts1, TaskCreationOptions.LongRunning);
                workers[iThreads + 1] = Task.Factory.StartNew(ts2, TaskCreationOptions.LongRunning);
            }

            Task.WaitAll(workers);

            //checking time
            //Now lets see how this is done.
            //Reverse and sort - ascending more likely
            //Sort followed up Reverse - descending
            fDescending = false;
            if (((int)arrMother[0]).CompareTo((int)arrMother[1]) > 0)
                fDescending = true;

            fWrongResult = false;
            iValue = (int)arrMother[0];
            for (int i = 1; i < iNumberOfElements; i++)
            {
                if (fDescending)
                {
                    if (iValue.CompareTo((int)arrMother[i]) <= 0)
                    {
                        fWrongResult = true;
                        break;
                    }
                }
                else
                {
                    if (iValue.CompareTo((int)arrMother[i]) >= 0)
                    {
                        fWrongResult = true;
                        break;
                    }
                }
                iValue = (int)arrMother[i];
            }

            Assert.False(fWrongResult);
        }

        void SortElements()
        {
            _arrGrandDaughter.Sort();
        }

        void ReverseElements()
        {
            _arrDaughter.Reverse();
        }
    }
}
