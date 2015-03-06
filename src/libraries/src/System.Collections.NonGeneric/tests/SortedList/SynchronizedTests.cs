// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class SynchronizedTests
    {
        private SortedList _slst2;
        private Int32 _iNumberOfElements = 20;

        [Fact]
        [OuterLoop]
        public void TestSynchronizedBasic()
        {
            SortedList slst1;
            String strValue;

            Task[] workers;
            Action ts1;
            Int32 iNumberOfWorkers = 3;

            slst1 = new SortedList();
            _slst2 = SortedList.Synchronized(slst1);

            workers = new Task[iNumberOfWorkers];
            for (int i = 0; i < workers.Length; i++)
            {
                var name = "Thread worker " + i;
                ts1 = new Action(() => AddElements(name));
                workers[i] = Task.Run(ts1);
            }

            Task.WaitAll(workers);

            //checking time

            Assert.Equal(_slst2.Count, _iNumberOfElements * iNumberOfWorkers);


            for (int i = 0; i < iNumberOfWorkers; i++)
            {
                for (int j = 0; j < _iNumberOfElements; j++)
                {
                    strValue = "Thread worker " + i + "_" + j;

                    Assert.True(_slst2.Contains(strValue), "Error, Expected value not returned, " + strValue);
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

            Assert.Equal(0, _slst2.Count);
            Assert.False(slst1.IsSynchronized);
            Assert.True(_slst2.IsSynchronized);
        }

        private void AddElements(String strName)
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _slst2.Add(strName + "_" + i, "String_" + i);
            }
        }

        private void RemoveElements(String strName)
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _slst2.Remove(strName + "_" + i);
            }
        }
    }
}
