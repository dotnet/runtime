// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Test class that verifies the integration with APM (Task => APM) section 2.5.11 in the TPL spec
// 
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    /// <summary>
    /// A class that implements the APM pattern to ensure that TPL can support APM patttern
    /// </summary>
    public sealed class TaskAPMTest
    {
        /// <summary>
        /// Used to indicate whether to test TPL's Task or Future functionality for the APM pattern
        /// </summary>
        private readonly bool _hasReturnType;

        /// <summary>
        /// Used to synchornize between Main thread and async thread, by blocking the main thread until
        /// the thread that invokes the TaskCompleted method via AsyncCallback finishes
        /// </summary>
        private ManualResetEvent _mre;

        private const int INTINPUT = 1000;     // the input to the LongTask<int>.DoTask/BeginDoTask
        /// <summary>
        /// The constant that defines the amount time to spinwait (to simulate work) in the LongTask class
        /// </summary>
        private const int LongTaskSeconds = 5;

        /// <summary>
        /// Ctor that reads the XML testcase and sets the boolean to indicate whether to test Task or Future
        /// functionality
        /// </summary>
        /// <param name="hasReturnType"></param>
        public TaskAPMTest(bool hasReturnType)
        {
            _hasReturnType = hasReturnType;
            _mre = new ManualResetEvent(false);
        }

        /// <summary>
        /// Method that tests that the four APM patterns works
        /// </summary>
        /// <returns></returns>
        internal void RealRun()
        {
            IAsyncResult ar;

            LongTask lt = null;
            if (_hasReturnType)
                lt = new LongTask<int>(LongTaskSeconds);
            else
                lt = new LongTask(LongTaskSeconds);

            //1. Prove that the Wait-until-done technique works
            ar = lt.BeginDoTask(null, null);
            lt.EndDoTask(ar);
            // verify task completed
            if (!VerifyTaskCompleted(ar))
                Assert.True(false, string.Format("Wait-until-done: Task is not completed"));

            Debug.WriteLine("Wait-until-Done -- Task completed");

            //2. Prove that the Polling technique works
            ar = lt.BeginDoTask(null, null);
            while (!ar.IsCompleted)
            {
                Task delay = Task.Delay(1000);
                delay.Wait();
                //Thread.Sleep(1000);
            }
            // verify task completed
            if (!VerifyTaskCompleted(ar))
                Assert.True(false, string.Format("Polling: Task is not completed"));

            Debug.WriteLine("Polling -- Task completed");

            //3. Prove the AsyncWaitHandle works
            ar = lt.BeginDoTask(null, null);
            ar.AsyncWaitHandle.WaitOne();
            // verify task completed
            if (!VerifyTaskCompleted(ar))
                Assert.True(false, string.Format("wait via AsyncWaitHandle: Task is not completed"));

            Debug.WriteLine("Wait on AsyncWaitHandle -- Task completed");

            //4. Prove that the Callback technique works
            if (_hasReturnType)
                ar = ((LongTask<int>)lt).BeginDoTask(INTINPUT, TaskCompleted, lt);
            else
                ar = lt.BeginDoTask(TaskCompleted, lt);

            _mre.WaitOne(); //Block the main thread until async thread finishes executing the call back
            // verify task completed
            if (!VerifyTaskCompleted(ar))
                Assert.True(false, string.Format("Callback: Task is not completed"));

            Debug.WriteLine("Callback -- Task completed");
            //reaching this point means that the test didnt encounter any crashes or hangs.
            //So set the test as passed by returning true
            Assert.False(ar.CompletedSynchronously, "Should not have completed synchronously.");


            // Cleanup
            _mre.Dispose();
        }

        /// <summary>
        /// Method used by the callback implementation by the APM
        /// </summary>
        /// <param name="ar"></param>
        private void TaskCompleted(IAsyncResult ar)
        {
            if (_hasReturnType)
            {
                LongTask<int> lt = (LongTask<int>)ar.AsyncState;
                int retValue = lt.EndDoTask(ar);
                if (retValue != INTINPUT)
                    Assert.True(false, string.Format("Mismatch: Return = {0} vs Expect = {1}", retValue, INTINPUT));
            }
            else
            {
                LongTask lt = (LongTask)ar.AsyncState;
                lt.EndDoTask(ar);
            }

            _mre.Set();
        }

        /// <summary>
        /// Verify the IAsyncResult represent a completed Task
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        private bool VerifyTaskCompleted(IAsyncResult ar)
        {
            return ar.IsCompleted &&
                ((Task)ar).Status == TaskStatus.RanToCompletion; // assume no exception thrown           
        }
    }

    public static class TaskAPMTestCases
    {
        [Fact]
        [OuterLoop]
        public static void TaskAPMTest0()
        {
            TaskAPMTest test = new TaskAPMTest(false);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void TaskAPMTest1()
        {
            TaskAPMTest test = new TaskAPMTest(true);
            test.RealRun();
        }
    }

    /// <summary>
    /// A dummy class that simulates a long running task that implements IAsyncResult methods
    /// </summary>
    public class LongTask
    {
        //Used to store the amount time to perform spinWait
        protected readonly Int32 _ms;  // Milliseconds;

        public LongTask(Int32 seconds)
        {
            _ms = seconds * 1000;
        }

        // Synchronous version of time-consuming method
        public void DoTask()
        {
            // Simulate time-consuming task
            SpinWait.SpinUntil(() => false, _ms);
            //Thread.SpinWait(_ms); 
        }

        // Asynchronous version of time-consuming method (Begin part)
        public IAsyncResult BeginDoTask(AsyncCallback callback, Object state)
        {
            // Create IAsyncResult object identifying the asynchronous operation
            Task task = Task.Factory.StartNew(
                delegate
                {
                    DoTask(); //simulates workload
                },
                state);

            if (callback != null)
            {
                task.ContinueWith(delegate
                {
                    callback(task);
                });
            }

            return task;  // Return the IAsyncResult to the caller
        }

        // Asynchronous version of time-consuming method (End part)
        public void EndDoTask(IAsyncResult asyncResult)
        {
            // We know that the IAsyncResult is really a Task object
            Task task = (Task)asyncResult;
            // Wait for operation to complete
            task.Wait();
        }
    }

    /// <summary>
    /// A dummy class that simulates a long running Future that implements IAsyncResult methods
    /// </summary>
    public sealed class LongTask<T> : LongTask
    {
        public LongTask(Int32 seconds)
            : base(seconds)
        {
        }

        // Synchronous version of time-consuming method
        public T DoTask(T input)
        {
            SpinWait.SpinUntil(() => false, _ms); // Simulate time-consuming task
            return input;           // Return some result, for now, just return the input
        }

        public IAsyncResult BeginDoTask(T input, AsyncCallback callback, Object state)
        {
            // Create IAsyncResult object identifying the asynchronous operation
            Task<T> task = Task<T>.Factory.StartNew(
                delegate
                {
                    return DoTask(input);
                },
                state);

            task.ContinueWith(delegate
            {
                callback(task);
            });

            return task;  // Return the IAsyncResult to the caller
        }

        // Asynchronous version of time-consuming method (End part)
        new public T EndDoTask(IAsyncResult asyncResult)
        {
            // We know that the IAsyncResult is really a Task object
            Task<T> task = (Task<T>)asyncResult;

            // Return the result
            return task.Result;
        }
    }
}
