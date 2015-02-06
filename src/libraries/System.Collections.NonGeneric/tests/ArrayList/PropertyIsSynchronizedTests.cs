// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class GetIsSynchronizedTests
    {
        private ArrayList _alst2;
        private Int32 _iNumberOfElements = 10;

        [Fact]
        public void TestGetIsSynchronized()
        {
            ArrayList alst1;
            Int32 iNumberOfTimes;
            String strValue;

            Task[] workers;
            Action ts1;
            Int32 iNumberOfWorkers = 10;

            //[] Vanila test case - Synchronized returns an IList that is thread safe
            // We will try to test this by getting a number of threads to write some items
            // to a synchronized IList

            alst1 = new ArrayList();
            _alst2 = ArrayList.Synchronized(alst1);

            workers = new Task[iNumberOfWorkers];
            ts1 = new Action(AddElements);

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(ts1);
            }

            Task.WaitAll(workers);

            //checking time
            Assert.Equal(_iNumberOfElements * iNumberOfWorkers, _alst2.Count);

            for (int i = 0; i < _iNumberOfElements; i++)
            {
                iNumberOfTimes = 0;
                strValue = "String_" + i;
                for (int j = 0; j < _alst2.Count; j++)
                {
                    if (((String)_alst2[j]).Equals(strValue))
                        iNumberOfTimes++;
                }

                Assert.Equal(iNumberOfTimes, iNumberOfWorkers);
            }

            //I dont think that we can make an assumption on the order of these items though
            //now we are going to remove all of these
            workers = new Task[iNumberOfWorkers];
            ts1 = new Action(RemoveElements);
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(ts1);
            }

            Task.WaitAll(workers);

            Assert.Equal(0, _alst2.Count);
            Assert.False(alst1.IsSynchronized);
            Assert.True(_alst2.IsSynchronized);
        }

        void AddElements()
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _alst2.Add("String_" + i);
            }
        }

        void RemoveElements()
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _alst2.Remove("String_" + i);
            }
        }

    }
}
