// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;

namespace System.Collections.HashtableTests
{
    public class IsSynchronizedTests
    {
        private Hashtable _hsh2;
        private int _iNumberOfElements = 20;

        [Fact]
        [OuterLoop]
        public void TestIsSynchronizedBasic()
        {
            Hashtable hsh1;
            Hashtable hsh3;
            string strValue;

            Task[] workers;
            Action ts1;
            int iNumberOfWorkers = 3;

            //[]Vanila - we will make sure that IsSynchronized is returned for the correct HT's
            hsh1 = new Hashtable();
            _hsh2 = Hashtable.Synchronized(hsh1);

            Assert.False(hsh1.IsSynchronized);
            Assert.True(_hsh2.IsSynchronized);

            hsh3 = Hashtable.Synchronized(_hsh2);
            Assert.True(hsh3.IsSynchronized);

            //[] Synchronized returns an HT that is thread safe
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
                    Assert.True(_hsh2.Contains(strValue), "Error, Expected value not returned, " + strValue);
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

            Assert.Equal(0, _hsh2.Count);
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
