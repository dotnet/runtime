// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    public sealed class CThreads
    {
        //Data
        private IList<CThread> _rgThreads = new List<CThread>();

        //Static
        public static int MaxThreads = 20;

        private ITestOutputHelper _output;
        public CThreads(ITestOutputHelper output)
        {
            _output = output;
        }

        //Accessors
        private IList<CThread> Internal
        {
            get { return _rgThreads; }
        }

        public CThread this[int index]
        {
            get { return _rgThreads[index]; }
        }

        public CThread Add(CThread rThread)
        {
            _rgThreads.Add(rThread);
            return rThread;
        }

        public void Add(ThreadFunc func, object oParam)
        {
            Add(1, func, oParam);
        }

        public void Add(int cThreads, ThreadFunc func, object oParam)
        {
            //Default to one iteration
            Add(cThreads, 1, func, oParam);
        }

        public void Add(int cThreads, int iterations, ThreadFunc func, object oParam)
        {
            for (int i = 0; i < cThreads; i++)
                Add(new CThread(iterations, func, oParam, _output));
        }

        public void Start()
        {
            for (int i = 0; i < _rgThreads.Count; i++)
                this[i].Start();
        }

        public void Abort()
        {
            for (int i = 0; i < _rgThreads.Count; i++)
                this[i].Abort();
        }

        public void Wait()
        {
            Exception eReturn = null;

            //Wait for all the threads to complete...
            for (int i = 0; i < _rgThreads.Count; i++)
            {
                //Even if a thread ends up throwing, we still having to wait for all the
                //other threads to complete first.  Then throw the first exception received.
                try
                {
                    this[i].Wait();
                }
                catch (Exception e)
                {
                    //Only need to save off the first exception
                    if (eReturn == null)
                        eReturn = e;
                }
            }

            //If any of the threads failed, throw an exception...
            if (eReturn != null)
                throw eReturn;
        }

        public void StartSingleThreaded()
        {
            //This is mainly a debugging tool to ensure your threading scenario works
            //if it was run actually sequentially.  (ie: non-multlthreaded).
            for (int i = 0; i < _rgThreads.Count; i++)
            {
                this[i].Start();
                this[i].Wait();
            }
        }

        public int ReturnCodes(int iReturnCode)
        {
            int cMatches = 0;
            for (int i = 0; i < _rgThreads.Count; i++)
            {
                if (this[i].ReturnCode == iReturnCode)
                    cMatches++;
            }

            return cMatches;
        }

        public void Verify(int cThreads, int iReturnCode)
        {
            //Make sure the number of threads expected had the correct return code...
            Assert.Equal(ReturnCodes(iReturnCode), cThreads);
        }
    }
}