// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//ArrayList

using System.Reflection;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // Delegate
    //
    ////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////
    // CThreads
    //
    ////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////
    // CThread
    //
    ////////////////////////////////////////////////////////////////
    public class CThread
    {
        //Data
        private ThreadFunc _threadfunc;

        private int _nIterations;
        private object _oParam;
        private int _iReturn;
        private Exception _eReturn;
        private Thread _rThread;

        private ITestOutputHelper _output;

        //Constructor
        public CThread(ThreadFunc func, object oParam, ITestOutputHelper output)
            : this(1, func, oParam, output)
        {
            //Default to 1 iteration
        }

        public CThread(int iterations, ThreadFunc func, object oParam, ITestOutputHelper output)
        {
            //Note: notice there are no "setters" on this class, so you can't reuse this class
            //for other thread functions, you need one class per thread function.  This is exaclty
            //how the System.Thread class works, you can't reuse it, once the thread is complete, its done
            //so all the state set is for that thread...

            _rThread = new Thread(new ThreadStart(InternalThreadStart));
            _threadfunc = func;
            _nIterations = iterations;
            _oParam = oParam;
            _output = output;
        }

        //Static
        public static int MaxIterations = 30;

        //Accessors
        public virtual Thread Internal
        {
            get { return _rThread; }
        }

        public virtual int Iterations
        {
            get { return _nIterations; }
        }

        public virtual ThreadFunc Func
        {
            get { return _threadfunc; }
        }

        public virtual int ReturnCode
        {
            get { return _iReturn; }
        }

        public virtual object Param
        {
            get { return _oParam; }
        }

        private void InternalThreadStart()
        {
            //Note: We have a "wrapper" thread function thats always called.
            //This allows us much greater control than the normal System.Thread class.
            //  1.  It allows us to call the function repeatedly (iterations)
            //  2.  It allows parameters to be passed into the thread function
            //  3.  It allows a return code from the thread function
            //  4.  etc...

            //Iterate the specified number of times
            for (int i = 0; i < Iterations; i++)
            {
                //call the user thread function
                try
                {
                    _iReturn = Func(Param);
                }
                catch (Exception e)
                {
                    //Note: If we don't handle this exception it doesn't get handled by the
                    //main thread try-catch since its on a separate thread.  Instead of crashing the
                    //URT - or requiring every thread function to catch any exception (there not expecting)
                    //we will catch it and store the exception for later throw from the calling function
                    _iReturn = HandleException(e);
                    _eReturn = e;

                    //We should break out of this iteration
                    break;
                }
            }
        }

        public virtual void Start()
        {
            Internal.Start();
        }

        public virtual void Abort()
        {
            throw new NotImplementedException();
        }

        public virtual void Wait()
        {
            //Wait for this thread to complete...
            Internal.Join();

            //Now throw any exceptions that occurred from within the thread to the caller
            if (_eReturn != null)
                throw _eReturn;
        }

        public virtual void Verify(int iReturnCode)
        {
            //Make sure the number of threads expected had the correct return code...
            Assert.Equal(ReturnCode, iReturnCode);
        }

        public int HandleException(Exception e)
        {
            //TargetInvocationException is almost always the outer
            //since we call the variation through late binding
            if (e is TargetInvocationException && e.InnerException != null)
                e = e.InnerException;

            int eResult = 0; //TEST_FAIL
            object actual = e.GetType();
            object expected = null;
            string message = e.Message;

            //Log the exception back to LTM (or whatever has the console)
            var eTest = e as OLEDB.Test.ModuleCore.CTestException;
            if (eTest != null)
            {
                //Setup more meaningful info
                actual = eTest.Actual;
                expected = eTest.Expected;
                eResult = eTest.Result;
                switch (eResult)
                {
                    case 1: //TEST_PASS
                            //case TEST_SKIPPED:
                        _output.WriteLine(eTest.Message);
                        return eResult; //were done
                }
            }

            _output.WriteLine("Actual  : {0}", actual);
            _output.WriteLine("Expected: {0}", expected);
            _output.WriteLine(e.Message);

            return eResult;
        }
    }
}
