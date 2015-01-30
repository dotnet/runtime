// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class SynchronizedTests
    {
        private IList _ilst1;
        private Int32 _iNumberOfElements = 10;
        private const string _prefix = "String_";

        private void AddElements()
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _ilst1.Add(_prefix + i);
            }
        }

        private void RemoveElements()
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _ilst1.Remove(_prefix + i);
            }
        }

        [Fact]
        public void TestIListParameterMultiThreads()
        {
            ArrayList alst1;
            int iNumberOfTimes;
            string strValue;

            Task[] workers;
            Action ts1;
            int iNumberOfWorkers = 10;

            //[] Vanila test case - Synchronized returns an IList that is thread safe
            // We will try to test this by getting a number of threads to write some items
            // to a synchronized IList

            alst1 = new ArrayList();
            _ilst1 = ArrayList.Synchronized((IList)alst1);

            workers = new Task[iNumberOfWorkers];
            ts1 = new Action(AddElements);
            // LongRunning will have problem when running under xUint
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Factory.StartNew(ts1, TaskCreationOptions.LongRunning);
            }

            Task.WaitAll(workers);

            //checking time
            Assert.Equal(_iNumberOfElements * iNumberOfWorkers, _ilst1.Count);

            for (int i = 0; i < _iNumberOfElements; i++)
            {
                iNumberOfTimes = 0;
                strValue = _prefix + i;
                for (int j = 0; j < _ilst1.Count; j++)
                {
                    if (((string)_ilst1[j]).Equals(strValue))
                        iNumberOfTimes++;
                }

                Assert.Equal(iNumberOfTimes, iNumberOfWorkers);
            }

            workers = new Task[iNumberOfWorkers];
            ts1 = new Action(RemoveElements);
            // LongRunning will have problem with xUnit run
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Factory.StartNew(ts1, TaskCreationOptions.LongRunning);
            }

            Task.WaitAll(workers);

            Assert.Equal(0, _ilst1.Count);
            Assert.False(alst1.IsSynchronized);
            Assert.True(_ilst1.IsSynchronized);

            // [] Check null value
            Assert.Throws<ArgumentNullException>(() =>
                {
                    IList myIList = null;
                    ArrayList.Synchronized(myIList);
                });
        }


        public ArrayList arrList = null;
        public Hashtable hash = null;    // this will verify that threads will only add elements the num of times
        // they are specified to
        public class MyArrrayList
        {
        }

        [Fact]
        public void TestArrayListParameterMultiThreads()
        {
            // [] make 40 threads which add strHeroes to an ArrayList
            // the outcome is that the length of the ArrayList should be the same size as the strHeroes array
            arrList = new ArrayList();
            arrList = ArrayList.Synchronized(arrList);

            hash = new Hashtable();                             // Synchronized Hashtable
            hash = Hashtable.Synchronized(hash);

            Task[] workers = new Task[7];

            // initialize the threads
            for (int i = 0; i < workers.Length; i++)
            {
                var name = "ThreadID " + i.ToString();
                Action delegStartMethod = () => AddElems(name);
                workers[i] = Task.Factory.StartNew(delegStartMethod, TaskCreationOptions.LongRunning);
            }

            Task.WaitAll(workers);

            Assert.Equal(workers.Length * strHeroes.Length, arrList.Count);

            // [] Check null value
            Assert.Throws<ArgumentNullException>(() =>
            {
                ArrayList myArrayList = null;
                ArrayList.Synchronized(myArrayList);
            });
        }

        public void AddElems(string currThreadName)
        {
            int iNumTimesThreadUsed = 0;

            for (int i = 0; i < strHeroes.Length; i++)
            {
                // to test that we only use the right threads the right number of times  keep track with the hashtable
                // how many times we use this thread
                try
                {
                    hash.Add(currThreadName, null);
                    // this test assumes ADD will throw for dup elements
                }
                catch (ArgumentException)
                {
                    iNumTimesThreadUsed++;
                }

                Assert.NotNull(arrList);
                Assert.True(arrList.IsSynchronized);

                arrList.Add(strHeroes[i]);
            }

            Assert.Equal(strHeroes.Length - 1, iNumTimesThreadUsed);
        }

        #region "Test Data - Keep the data close to tests so it can vary independently from other tests"

        public static string[] strHeroes = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
        };

        #endregion
    }
}
