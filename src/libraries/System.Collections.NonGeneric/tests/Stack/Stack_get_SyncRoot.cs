// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.StackTests
{
    public class StackGetSyncRootTests
    {
        private Stack _arrDaughter;
        private Stack _arrGrandDaughter;

        private const Int32 iNumberOfElements = 100;

        [Fact]
        public void TestGetSyncRootBasic()
        {

            Stack arrSon;
            Stack arrMother;

            Int32 iNumberOfWorkers = 4;

            //[] Vanila test case - testing SyncRoot is not as simple as its implementation looks like. This is the working
            //scenrio we have in mind.
            //1) Create your Down to earth mother Stack
            //2) Get a Fixed wrapper from it
            //3) Get a Synchronized wrapper from 2)
            //4) Get a synchronized wrapper of the mother from 1)
            //5) all of these should SyncRoot to the mother earth

            arrMother = new Stack();
            for (int i = 0; i < iNumberOfElements; i++)
            {
                arrMother.Push(i);
            }

            
            Assert.Equal(arrMother.SyncRoot.GetType(), typeof(Object));

            arrSon = Stack.Synchronized(arrMother);
            _arrGrandDaughter = Stack.Synchronized(arrSon);
            _arrDaughter = Stack.Synchronized(arrMother);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(_arrGrandDaughter.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(_arrDaughter.SyncRoot, arrMother.SyncRoot);

            Assert.Equal(arrSon.SyncRoot, arrMother.SyncRoot);

            //we are going to rumble with the Stacks with 2 threads
            var ts1 = new Action(SortElements);
            var ts2 = new Action(ReverseElements);
            Task[] tasks = new Task[iNumberOfWorkers];
            for (int iThreads = 0; iThreads < iNumberOfWorkers; iThreads += 2)
            {
                tasks[iThreads] = Task.Run(ts1);
                tasks[iThreads + 1] = Task.Run(ts2);
            }

            Task.WaitAll(tasks);
        }

        void SortElements()
        {
            _arrGrandDaughter.Clear();
            for (int i = 0; i < iNumberOfElements; i++)
            {
                _arrGrandDaughter.Push(i);
            }
        }

        void ReverseElements()
        {
            _arrDaughter.Clear();
            for (int i = 0; i < iNumberOfElements; i++)
            {
                _arrDaughter.Push(i);
            }
        }
    }
}
