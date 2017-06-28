// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
// TPL namespaces
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace System.Threading.Tasks.Tests
{
    public static class TaskRtTests_Core
    {
        [Fact]
        public static void RunTCSCompletionStateTests()
        {
            TaskCompletionSource<int> tcs = null;
            int failureCount = 0;
            int errorCount = 0;
            int concurrencyLevel = 50;
            Task[] threads = null;

            // Testing competing SetResults"

            ManualResetEvent mres = new ManualResetEvent(false);

            // May take a few runs to actually see a problem
            for (int repeats = 0; repeats < 20; repeats++)
            {
                //
                // Test transition to Faulted (exception)
                //
                failureCount = 0;
                errorCount = 0;
                tcs = new TaskCompletionSource<int>();
                threads = new Task[concurrencyLevel];
                mres.Reset();
                for (int i = 0; i < concurrencyLevel; i++)
                {
                    threads[i] = new Task(() =>
                    {
                        bool sawFailure = false;
                        bool sawError = false;
                        mres.WaitOne();
                        if (!tcs.TrySetResult(10)) sawFailure = true;
                        if (tcs.Task.Result != 10) sawError = true;

                        if (sawFailure) Interlocked.Increment(ref failureCount);
                        if (sawError) Interlocked.Increment(ref errorCount);
                    });
                }
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Start();
                mres.Set();
                //for (int i = 0; i < concurrencyLevel; i++) threads[i].Join();
                Task.WaitAll(threads);

                if (failureCount != concurrencyLevel - 1)
                {
                    Assert.True(false, string.Format("RunTCSCompletionStateTests:    > FAILED! Expected {0} failures on TrySetResult, got {1}",
                        concurrencyLevel - 1, failureCount));
                }

                if (errorCount > 0)
                {
                    Assert.True(false, string.Format("RunTCSCompletionStateTests:    > FAILED! Saw {0} instances of Result != 10", errorCount));
                }
            }
        }

        [Fact]
        public static void RunTCSCompletionStateTests_SetCancel()
        {
            // Testing competing SetCancels
            TaskCompletionSource<int> tcs = null;
            int failureCount = 0;
            int errorCount = 0;
            int concurrencyLevel = 50;
            Task[] threads = null;
            ManualResetEvent mres = new ManualResetEvent(false);

            // May take a few runs to actually see a problem
            for (int repeats = 0; repeats < 20; repeats++)
            {
                //
                // Test transition to Faulted (exception)
                //
                failureCount = 0;
                errorCount = 0;
                tcs = new TaskCompletionSource<int>();
                threads = new Task[concurrencyLevel];
                mres.Reset();
                for (int i = 0; i < concurrencyLevel; i++)
                {
                    threads[i] = new Task(() =>
                    {
                        bool sawFailure = false;
                        bool sawError = false;
                        mres.WaitOne();
                        if (!tcs.TrySetCanceled()) sawFailure = true;
                        if (!tcs.Task.IsCanceled) sawError = true;

                        if (sawFailure) Interlocked.Increment(ref failureCount);
                        if (sawError) Interlocked.Increment(ref errorCount);
                    });
                }
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Start();
                mres.Set();
                //for (int i = 0; i < concurrencyLevel; i++) threads[i].Join();
                Task.WaitAll(threads);

                if (failureCount != concurrencyLevel - 1)
                {
                    Assert.True(false, string.Format("RunTCSCompletionStateTests:    > FAILED! Expected {0} failures on TrySetCanceled, got {1}",
                        concurrencyLevel - 1, failureCount));
                }

                if (errorCount > 0)
                {
                    Assert.True(false, string.Format("RunTCSCompletionStateTests:    > FAILED! Saw {0} instances of !tcs.Task.IsCanceled", errorCount));
                }
            }
        }

        [Fact]
        public static void RunTCSCompletionStateTests_SetException()
        {
            TaskCompletionSource<int> tcs = null;
            int failureCount = 0;
            int errorCount = 0;
            int concurrencyLevel = 50;
            Task[] threads = null;

            // Testing competing SetExceptions 

            ManualResetEvent mres = new ManualResetEvent(false);

            // May take a few runs to actually see a problem
            for (int repeats = 0; repeats < 20; repeats++)
            {
                //
                // Test transition to Faulted (exception)
                //
                failureCount = 0;
                errorCount = 0;
                tcs = new TaskCompletionSource<int>();
                threads = new Task[concurrencyLevel];
                mres.Reset();
                for (int i = 0; i < concurrencyLevel; i++)
                {
                    threads[i] = new Task(() =>
                    {
                        mres.WaitOne();
                        bool sawFailure = !tcs.TrySetException(new Exception("some exception"));
                        bool sawError = (tcs.Task.Exception == null);

                        if (sawFailure) Interlocked.Increment(ref failureCount);
                        if (sawError) Interlocked.Increment(ref errorCount);
                    });
                }
                for (int i = 0; i < concurrencyLevel; i++) threads[i].Start();
                mres.Set();
                //for (int i = 0; i < concurrencyLevel; i++) threads[i].Join();
                Task.WaitAll(threads);

                if (failureCount != concurrencyLevel - 1)
                {
                    Assert.True(false, string.Format("RunTCSCompletionStateTests:    > FAILED! Expected {0} failures on TrySetException, got {1}",
                        concurrencyLevel - 1, failureCount));
                }

                if (errorCount > 0)
                {
                    Assert.True(false, string.Format("RunTCSCompletionStateTests:    > FAILED! saw {0} instances of post-call Exception == null", errorCount));
                }
            }
        }

        // Make sure that TaskCompletionSource/TaskCompletionSource.Task handle state changes correctly.
        [Fact]
        public static void RunTaskCompletionSourceTests()
        {
            // Test that recorded Result is persistent.
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(cts.Token);
                tcs.SetResult(5);
                Assert.Equal(tcs.Task.Status, TaskStatus.RanToCompletion);

                cts.Cancel();

                Assert.Equal(tcs.Task.Status, TaskStatus.RanToCompletion);

                Assert.False(tcs.TrySetException(new Exception("some exception")), "RunTaskCompletionSourceTests:    > Error!  Set result, Canceled, tcs.TrySetException succeeded");

                Assert.False(tcs.TrySetResult(10), "RunTaskCompletionSourceTests:    > Error!  Set result, Canceled, tcs.TrySetResult succeeded");
                Assert.False(tcs.TrySetCanceled(), "RunTaskCompletionSourceTests:    > Error!  Set result, Canceled, tcs.TrySetCanceled succeeded");

                Assert.Equal(tcs.Task.Result, 5);

                Exception fake = new Exception("blah!");
                try
                {
                    tcs.TrySetResult(10);
                    tcs.TrySetCanceled();
                    tcs.TrySetException(fake);
                }
                catch (Exception e)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySet*-after-Dispose should not have thrown an exception, but instead threw {0}", e.GetType()));
                }
            }
        }

        [Fact]
        public static void RunTaskCompletionSourceTests_Negative()
        {
            // Test that recorded Result is persistent.
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(cts.Token);
                tcs.SetResult(5);
                cts.Cancel();

                Assert.Throws<InvalidOperationException>(
                   () => tcs.SetResult(10));
                Assert.Throws<InvalidOperationException>(
                   () => tcs.SetCanceled());
                Assert.Throws<InvalidOperationException>(
                   () => tcs.SetCanceled());
                Assert.Throws<InvalidOperationException>(
                   () => tcs.SetException(new Exception("some other exception")));
                Assert.Throws<InvalidOperationException>(
                   () => tcs.SetException(new[] { new Exception("some other exception") }));
            }
        }

        [Fact]
        public static void RunTaskCompletionSourceTests_SetException()
        {
            // Test that recorded exception is persistent
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(cts.Token);
                tcs.SetException(new Exception("Some recorded exception"));
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, Status should be Faulted, is {0}", tcs.Task.Status));
                }
                cts.Cancel();
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, canceled, Status should be Faulted, is {0}", tcs.Task.Status));
                }
                if (tcs.TrySetResult(15))
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, canceled, TrySetResult succeeded"));
                }
                if (tcs.TrySetException(new Exception("blah")))
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, canceled, TrySetException succeeded"));
                }
                if (tcs.TrySetCanceled())
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, canceled, TrySetCanceled succeeded"));
                }
                try
                {
                    tcs.SetResult(10);
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, Canceled, no exception on setting result"));
                }
                catch { }
                try
                {
                    tcs.SetException(new Exception("bar"));
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, Canceled, no exception on re-setting exception"));
                }
                catch { }
                try
                {
                    tcs.SetCanceled();
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, Canceled, no exception on Cancel"));
                }
                catch { }
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, final Status should be Faulted, is {0}", tcs.Task.Status));
                }
                try
                {
                    tcs.Task.Wait();
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Set exception, Wait()-ed, expected exception, got none."));
                }
                catch { }
            }

            // Test that setting multiple exceptions works correctly
            {
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                bool succeeded =
                    tcs.TrySetException(new Exception[] { new Exception("Exception A"), new Exception("Exception B") });
                if (!succeeded)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() attempt did not succeed"));
                }
                if (tcs.Task.Status != TaskStatus.Faulted)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() attempt did not result in Faulted status (got {0})", tcs.Task.Status));
                }
                try
                {
                    tcs.Task.Wait();
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerExceptions.Count != 2)
                    {
                        Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! Expected TrySetException() to result in 2 inner exceptions, got {0}", ae.InnerExceptions.Count));
                    }
                }
                catch (Exception e)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() resulted in wrong exception type: {0}", e.GetType().ToString()));
                }

                tcs = new TaskCompletionSource<int>();
                try
                {
                    tcs.TrySetException(new Exception[] { new Exception("Exception A"), null });
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() with null array element should have thrown an exception"));
                }
                catch (ArgumentException) { }
                catch (Exception e)
                {
                    Assert.True(false, string.Format("    > Error! TrySetException() with null array element should have thrown an ArgumentException, got {0}",
                        e.GetType().ToString()));
                }

                try
                {
                    tcs.TrySetException((IEnumerable<Exception>)null);
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() with null IEnumerable should have thrown an exception"));
                }
                catch (ArgumentNullException) { }
                catch (Exception e)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() with null IEnumerable should have thrown an ArgumentNullException, got {0}",
                        e.GetType().ToString()));
                }

                try
                {
                    tcs.TrySetException(new Exception[0]);
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() with no elements should have thrown an exception"));
                }
                catch (ArgumentException) { }
                catch (Exception e)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() with no elements should have thrown an ArgumentException, got {0}",
                        e.GetType().ToString()));
                }

                // Technically, these last two aren't for multiple exceptions, but we'll test them as well.
                try
                {
                    tcs.TrySetException((Exception)null);
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() with null Exception should have thrown an exception"));
                }
                catch (ArgumentNullException) { }
                catch (Exception e)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! TrySetException() with null Exception should have thrown an ArgumentNullException, got {0}",
                        e.GetType().ToString()));
                }

                try
                {
                    tcs.SetException((Exception)null);
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! SetException() with null Exception should have thrown an exception"));
                }
                catch (ArgumentNullException) { }
                catch (Exception e)
                {
                    Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error! SetException() with null Exception should have thrown an ArgumentNullException, got {0}",
                        e.GetType().ToString()));
                }
            }
        }

        [Fact]
        public static void RunTaskCompletionSourceTests_CancellationTests()
        {
            // Test that cancellation is persistent
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(ct);
            cts.Cancel();

            if (tcs.Task.Status == TaskStatus.Canceled)
            {
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Task Canceled, Should not have seen status = Canceled, did"));
            }
            tcs.SetCanceled(); // cancel it for real
            if (tcs.Task.Status != TaskStatus.Canceled)
            {
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, Status should be Canceled, is {0}", tcs.Task.Status));
            }
            if (tcs.TrySetException(new Exception("spam")))
            {
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, TrySetException succeeded"));
            }
            if (tcs.TrySetResult(10))
            {
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, TrySetResult succeeded"));
            }
            if (tcs.TrySetCanceled())
            {
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, TrySetCanceled succeeded"));
            }
            try
            {
                tcs.SetResult(15);
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, no exception on setting Result"));
            }
            catch { }
            try
            {
                tcs.SetException(new Exception("yet another exception"));
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, no exception on setting Exception"));
            }
            catch { }
            try
            {
                tcs.SetCanceled();
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, no exception on re-Cancel"));
            }
            catch { }
            try
            {
                int i = tcs.Task.Result;
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, but get-Result threw no exception"));
            }
            catch { }


            if (tcs.Task.Status != TaskStatus.Canceled)
            {
                Assert.True(false, string.Format("RunTaskCompletionSourceTests:    > Error!  Canceled, final status should be Canceled, is {0}", tcs.Task.Status));
            }
        }

        [Fact]
        public static void RunTaskCreateTests()
        {
            try
            {
                Task t = new Task(delegate { }, (TaskCreationOptions)0x100);
                Assert.True(false, string.Format("RunTaskCreateTests:    > FAILED!  Failed to throw exception on use of internal TCO"));
            }
            catch { }
        }

        // Test "bare" overloads for Task<T> ctor, Task<T>.Factory.StartNew
        [Fact]
        public static void TestTaskTConstruction_bare()
        {
            for (int i = 0; i < 2; i++)
            {
                bool useCtor = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool useObj = (j == 0);
                    Task<int> f1;
                    object refObj = new object();
                    bool sideEffect = false;

                    if (useCtor)
                    {
                        if (useObj)
                        {
                            f1 = new Task<int>(obj => { sideEffect = true; return 42; }, refObj);
                        }
                        else
                        {
                            f1 = new Task<int>(() => { sideEffect = true; return 42; });
                        }
                        f1.Start();
                    }
                    else
                    {
                        if (useObj)
                        {
                            f1 = Task<int>.Factory.StartNew(obj => { sideEffect = true; return 42; }, refObj);
                        }
                        else
                        {
                            f1 = Task<int>.Factory.StartNew(() => { sideEffect = true; return 42; });
                        }
                    }

                    int result = 0;
                    Exception ex = null;
                    try
                    {
                        f1.Wait();
                        result = f1.Result;
                    }
                    catch (Exception e)
                    {
                        ex = e;
                    }

                    object asyncState = ((IAsyncResult)f1).AsyncState;

                    Assert.True(ex == null, "TestTaskTConstruction_bare: Did not expect an exception");
                    Assert.True(sideEffect, "TestTaskTConstruction_bare: The func delegate apparently did not run");
                    Assert.True(f1.Result == 42, "TestTaskTConstruction_bare: Expected future's result to be 42");
                    Assert.True(f1.Status == TaskStatus.RanToCompletion, "TestTaskTConstruction_bare: Expected future to be in RanToCompletion status");
                    Assert.True(useObj || (asyncState == null), "TestTaskTConstruction_bare: Expected non-null AsyncState only if object overload was used");
                    Assert.True((!useObj) || (asyncState == refObj), "TestTaskTConstruction_bare: Wrong AsyncState value returned");
                }
            }
        }

        // Test overloads for Task<T> ctor, Task<T>.Factory.StartNew that accept a CancellationToken
        [Fact]
        public static void TestTaskTConstruction_ct()
        {
            for (int i = 0; i < 2; i++)
            {
                bool useCtor = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool useObj = (j == 0);
                    object refObj = new object();
                    for (int k = 0; k < 2; k++)
                    {
                        bool preCanceledToken = (k == 0);
                        Task<int> f1;
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        if (preCanceledToken)
                        {
                            Debug.WriteLine("With cts cancelled");
                            cts.Cancel();
                            Debug.WriteLine("With cts cancelled worked.");
                        }
                        if (useCtor)
                        {
                            if (useObj)
                            {
                                //Debug.WriteLine(" - ctor(Func<obj, int>, obj, ct({0})); Start();", preCanceledToken ? "signaled" : "unsignaled");
                                f1 = new Task<int>(obj => 42, refObj, ct);
                            }
                            else
                            {
                                // Debug.WriteLine(" - ctor(Func<int>, ct({0})); Start();", preCanceledToken ? "signaled" : "unsignaled");
                                f1 = new Task<int>(() => 42, ct);
                            }
                            if (!preCanceledToken) f1.Start();
                        }
                        else
                        {
                            if (useObj)
                            {
                                // Debug.WriteLine(" - T<int>.F.StartNew(Func<obj, int>, obj, ct({0}))", preCanceledToken ? "signaled" : "unsignaled");
                                f1 = Task<int>.Factory.StartNew(obj => 42, refObj, ct);
                            }
                            else
                            {
                                // Debug.WriteLine(" - T<int>.F.StartNew(Func<int>, ct({0}))", preCanceledToken ? "signaled" : "unsignaled");
                                f1 = Task<int>.Factory.StartNew(() => 42, ct);
                            }
                        }

                        Exception ex = null;
                        int result = 0;
                        try
                        {
                            result = f1.Result;
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }

                        object asyncState = ((IAsyncResult)f1).AsyncState;

                        Assert.True((ex != null) || (result == 42), "TestTaskTConstruction_ct:  Expected either a valid result or an exception");
                        Assert.True(preCanceledToken == (f1.Status == TaskStatus.Canceled), "TestTaskTConstruction_ct:  Expected future cancellation only with pre-canceled token");
                        Assert.True(preCanceledToken != (f1.Status == TaskStatus.RanToCompletion), "TestTaskTConstruction_ct:  Expected future to complete only with un-canceled token");
                        Assert.True(preCanceledToken != (result == 42), "TestTaskTConstruction_ct:  Expected valid result only with non-canceled token");
                        Assert.True(preCanceledToken == (ex != null), "TestTaskTConstruction_ct:  Expected exception only with pre-canceled token");
                        Assert.True(
                           (!preCanceledToken) ||
                           ((ex != null) &&
                            (ex.GetType() == typeof(AggregateException)) &&
                            (((AggregateException)ex).InnerExceptions[0].GetType() == typeof(TaskCanceledException))),
                            "TestTaskTConstruction_ct:  Wrong exception type thrown on pre-canceled token");
                        Assert.True(useObj || (asyncState == null), "TestTaskTConstruction_ct:  Expected non-null AsyncState only if object overload was used");
                        Assert.True((!useObj) || (asyncState == refObj), "TestTaskTConstruction_ct:  Wrong AsyncState value returned");
                    }
                }
            }
        }

        // Test overloads for Task<T> ctor, Task<T>.Factory.StartNew that accept a TaskCreationOptions param
        [Fact]
        public static void TestTaskTConstruction_tco()
        {
            for (int i = 0; i < 2; i++)
            {
                bool useCtor = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool useObj = (j == 0);
                    object refObj = new object();
                    for (int k = 0; k < 2; k++)
                    {
                        bool useLongRunning = (k == 0);
                        Task<int> f1;
                        TaskCreationOptions tco = useLongRunning ? TaskCreationOptions.LongRunning : TaskCreationOptions.None;

                        if (useCtor)
                        {
                            if (useObj)
                            {
                                f1 = new Task<int>(obj => 42, refObj, tco);
                            }
                            else
                            {
                                f1 = new Task<int>(() => 42, tco);
                            }
                            f1.Start();
                        }
                        else
                        {
                            if (useObj)
                            {
                                f1 = Task<int>.Factory.StartNew(obj => 42, refObj, tco);
                            }
                            else
                            {
                                f1 = Task<int>.Factory.StartNew(() => 42, tco);
                            }
                        }

                        Exception ex = null;
                        int result = 0;
                        try
                        {
                            result = f1.Result;
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }

                        object asyncState = ((IAsyncResult)f1).AsyncState;

                        Assert.True((ex == null), "TestTaskTConstruction_tco:  Did not expect an exception");
                        Assert.True(f1.CreationOptions == tco, "TestTaskTConstruction_tco:  Mis-matched TaskCreationOptions");
                        Assert.True(result == 42, "TestTaskTConstruction_tco:  Expected valid result");
                        Assert.True(useObj || (asyncState == null), "TestTaskTConstruction_tco:  Expected non-null AsyncState only if object overload was used");
                        Assert.True((!useObj) || (asyncState == refObj), "TestTaskTConstruction_tco:  Wrong AsyncState value returned");
                    }
                }
            }
        }

        // Test overloads for Task<T> ctor, Task<T>.Factory.StartNew that accept a CancellationToken and TaskCreationOptions
        [Fact]
        public static void TestTaskTConstruction_ct_tco()
        {
            for (int i = 0; i < 2; i++)
            {
                bool useCtor = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool useObj = (j == 0);
                    object refObj = new object();
                    Task<int> f1;
                    TaskCreationOptions tco = TaskCreationOptions.None;
                    CancellationToken ct = CancellationToken.None;


                    if (useCtor)
                    {
                        if (useObj)
                        {
                            f1 = new Task<int>(obj => 42, refObj, ct, tco);
                        }
                        else
                        {
                            f1 = new Task<int>(() => 42, ct, tco);
                        }
                        f1.Start();
                    }
                    else
                    {
                        if (useObj)
                        {
                            f1 = Task<int>.Factory.StartNew(obj => 42, refObj, ct, tco, TaskScheduler.Default);
                        }
                        else
                        {
                            f1 = Task<int>.Factory.StartNew(() => 42, ct, tco, TaskScheduler.Default);
                        }
                    }

                    Exception ex = null;
                    int result = 0;
                    try
                    {
                        result = f1.Result;
                    }
                    catch (Exception e)
                    {
                        ex = e;
                    }

                    object asyncState = ((IAsyncResult)f1).AsyncState;

                    Assert.True((ex == null), "TestTaskTConstruction_ct_tco:  Did not expect an exception");
                    Assert.True(result == 42, "TestTaskTConstruction_ct_tco:  Expected valid result");
                    Assert.True(useObj || (asyncState == null), "TestTaskTConstruction_ct_tco:  Expected non-null AsyncState only if object overload was used");
                    Assert.True((!useObj) || (asyncState == refObj), "TestTaskTConstruction_ct_tco:  Wrong AsyncState value returned");
                }
            }
        }

        // Basic future functionality. This is also covered in scenario unit tests, here we focus on wait functionality, and promises 
        [Fact]
        public static void RunBasicFutureTest()
        {
            //
            // future basic functionality tests
            //

            //
            // Test some TaskCompletionSource functionality...
            //
            TaskCreationOptions testOptions = TaskCreationOptions.AttachedToParent;
            object testState = new object();
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            if (((IAsyncResult)tcs.Task).AsyncState != null)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:   > FAILED! non-null state when not spec'd in empty tcs ctor"));
            }
            if (tcs.Task.CreationOptions != TaskCreationOptions.None)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! non-None TCO in tcs ctor when not spec'd in empty ctor"));
            }
            tcs.SetResult(10);

            tcs = new TaskCompletionSource<int>(testOptions);
            if (tcs.Task.CreationOptions != testOptions)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! TCO in tcs ctor not persistent"));
            }
            if (((IAsyncResult)tcs.Task).AsyncState != null)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! non-null state when not spec'd in tcs ctor"));
            }
            tcs.SetResult(10);

            tcs = new TaskCompletionSource<int>(testState);
            if (((IAsyncResult)tcs.Task).AsyncState != testState)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! state in tcs ctor not persistent"));
            }
            if (tcs.Task.CreationOptions != TaskCreationOptions.None)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! non-None TCO in tcs ctor when not spec'd in ctor"));
            }
            tcs.SetResult(10);

            tcs = new TaskCompletionSource<int>(testState, testOptions);
            if (tcs.Task.CreationOptions != testOptions)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! TCO with state in tcs ctor not persistent"));
            }
            if (((IAsyncResult)tcs.Task).AsyncState != testState)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! state with options in tcs ctor not persistent"));
            }
            tcs.SetResult(10);
        }

        [Fact]
        public static void RunBasicFutureTest_Negative()
        {
            //
            // future basic functionality tests
            //

            // Test exceptional conditions
            Assert.Throws<ArgumentNullException>(
               () => { new Task<int>((Func<int>)null); });
            Assert.Throws<ArgumentNullException>(
               () => { new Task<int>((Func<object, int>)null, new object()); });
            Assert.Throws<ArgumentNullException>(
               () => { Task<int>.Factory.StartNew((Func<int>)null); });
            Assert.Throws<ArgumentNullException>(
               () => { Task<int>.Factory.StartNew((Func<int>)null, CancellationToken.None, TaskCreationOptions.None, (TaskScheduler)null); });
            Assert.Throws<ArgumentNullException>(
               () => { Task<int>.Factory.StartNew((Func<object, int>)null, new object()); });
            Assert.Throws<ArgumentNullException>(
               () => { Task<int>.Factory.StartNew((obj) => 42, new object(), CancellationToken.None, TaskCreationOptions.None, (TaskScheduler)null); });
        }

        [Fact]
        public static void RunBasicFutureTest_PromiseTestsAndCancellation()
        {
            //
            // promise tests
            //
            bool unexpectedStateObserved = false;
            TaskCompletionSource<int> promise1 = new TaskCompletionSource<int>(); // will be used for explicit wait testing
            TaskCompletionSource<int> promise2 = new TaskCompletionSource<int>(); // will be used for implicit wait testing
            TaskCompletionSource<int> promise3 = new TaskCompletionSource<int>(); // will be used for cancellation testing

            Task t2 = Task.Factory.StartNew(delegate
            {
                unexpectedStateObserved |= (promise1.Task.IsCompleted == true);

                promise1.SetResult(1234);

                unexpectedStateObserved |= (promise1.Task.IsCompleted == false);

                promise2.SetResult(5678);
                promise3.SetCanceled();
            }, TaskScheduler.Default);

            promise1.Task.Wait();

            int promiseValueObserved = 0;
            bool cancellationExceptionReceived = false;
            bool someotherExceptionReceived = false;

            Task t3 = Task.Factory.StartNew(delegate
            {
                promiseValueObserved = promise2.Task.Result;

                // the following should throw, because t2 will be calling Cancel on promise3 little after we block here
                try { int i = promise3.Task.Result; }
                catch (AggregateException) { cancellationExceptionReceived = true; }
                catch (Exception) { someotherExceptionReceived = true; }
            }, TaskScheduler.Default);

            t3.Wait();

            if (promise2.Task.Result != 5678)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - Promise Test:  > error: Promise value unblocked, but wrong value was read"));
            }

            if (cancellationExceptionReceived == false || someotherExceptionReceived == true)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - Promise Test:  > error: Cancel()ed promise didn't throw TaskCanceledException on value accessor"));
            }

            if (unexpectedStateObserved)
            {
                Assert.True(false, string.Format("RunBasicFutureTest - Promise Test:  > error: unexpected state observed in Promise test"));
            }

            // Creating a TCS with a promise-style constructor that only allows TaskCreationOptions.AttachedToParent
            try
            {
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.PreferFairness);
                Assert.True(false, string.Format("RunBasicFutureTest - TaskCompletionSource:    > FAILED! illegal tcs ctor TCO did not cause exception"));
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("Wrong exception thrown. " + e));
            }
        }

        // Test the Task.RunSynchronously() API on external and internal threads
        [Fact]
        public static void RunSynchronouslyTest()
        {
            Task.Factory.StartNew(delegate { CoreRunSynchronouslyTest(); }).Wait();

            // Executing RunSynchronously() on a task whose cancellationToken was previously signaled
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            Task t1 = new Task(delegate { }, ct);   // Notice we aren't throwing an OCE.
            cts.Cancel();
            try
            {
                t1.RunSynchronously();
                Assert.True(false, string.Format("RunSynchronouslyTest:    > error: Should have thrown an exception"));
            }
            catch (InvalidOperationException)
            {
                // Assert.True(false, string.Format("RunSynchronouslyTest:    > properly threw exception: {0}", e.Message));
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("RunSynchronouslyTest:    > error: threw wrong exception: {0}", e.Message));
            }


            // Executing RunSynchronously() on a continuation task
            t1 = new Task(delegate { });
            Task t2 = t1.ContinueWith((completedTask) => { });
            try
            {
                t2.RunSynchronously();
                Assert.True(false, string.Format("RunSynchronouslyTest - continuation task:    > error: Should have thrown an exception"));
            }
            catch (InvalidOperationException)
            {
                // Assert.True(false, string.Format("    > properly threw exception: {0}", e.Message));
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("RunSynchronouslyTest - continuation task:    > error: threw wrong exception: {0}", e.Message));
            }
            t1.Start();
            t1.Wait();

            // Executing RunSynchronously() on promise-style Task
            Task<int> f1 = new TaskCompletionSource<int>().Task;
            try
            {
                f1.RunSynchronously();
                Assert.True(false, string.Format("RunSynchronouslyTest - promise-style Task:    > error: Should have thrown an exception"));
            }
            catch (InvalidOperationException)
            {
                //Assert.True(false, string.Format("    > properly threw exception: {0}", e.Message));
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("RunSynchronouslyTest - promise-style Task:    > error: threw wrong exception: {0}", e.Message));
            }
        }

        // Verifications for the Task.RunSynchronously() API
        [Fact]
        public static void CoreRunSynchronouslyTest()
        {
            //Executing RunSynchronously() validations on external thread
            bool bExecuted = false;
            TaskScheduler observedTaskscheduler = null;

            // do RunSynchronously for a non-exceptional task            
            Task t = new Task(delegate
            {
                observedTaskscheduler = TaskScheduler.Current;
                bExecuted = true;
            });

            t.RunSynchronously();
            if (!bExecuted || t.Status != TaskStatus.RanToCompletion)
            {
                Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: task invoked through RunSynchronously() didn't execute or ended up in wrong state"));
            }

            if (observedTaskscheduler != TaskScheduler.Current)
            {
                Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: task invoked through RunSynchronously() didn't observe correct TaskScheduler.Current"));
            }

            // Wait() should work on a task that was RunSynchronously()
            try
            {
                if (!t.Wait(500))
                {
                    Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: Wait timed out on a task which was previously RunSynchronously()"));
                }
            }
            catch
            {
                Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: Wait threw on a task which was previously RunSynchronously()"));
            }
        }

        [Fact]
        public static void CoreRunSynchronouslyTest_NegativeTests()
        {
            //Executing RunSynchronously() validations on external thread

            // do RunSynchronously for a non-exceptional task            
            Task t = new Task(delegate
            {
            });

            t.RunSynchronously();

            // Start() should throw if the task was already RunSynchronously()
            try
            {
                t.Start();
                Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: Start() should have thrown on a task which was previously RunSynchronously()"));
            }
            catch { }

            // RunSynchronously() should throw on a task which is already started
            t = Task.Factory.StartNew(delegate { });
            try
            {
                t.RunSynchronously();
                Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: RunSynchronously() should have thrown on a task which was previously started"));
            }
            catch { }

            //
            // RunSynchronously() should not throw itself for exceptional tasks, and we should get the exception
            // regularly through Wait()
            // 
            t = new Task(delegate { throw new Exception(); });
            try
            {
                t.RunSynchronously();
            }
            catch
            {
                Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: RunSynchronously() should not have thrown itself on exceptional task"));
            }


            try
            {
                t.Wait();
                Assert.True(false, string.Format("CoreRunSynchronouslyTest:  > error: Wait() should not have thrown on exceptional task invoked through RunSynchronously()"));
            }
            catch
            { }
        }

        // Simply throws an exception from the task and ensures it is propagated.
        [Fact]
        public static void RunTaskExceptionTest()
        {
            Task t = Task.Factory.StartNew(delegate { });
            t.Wait();
            try
            {
                var e2 = t.Exception;
                if (e2 != null)
                {
                    Assert.True(false, string.Format("RunTaskExceptionTest:    > error: non-null Exception from cleanly completed task."));
                }
            }
            catch
            {
                Assert.True(false, string.Format("RunTaskExceptionTest:    > error: exception thrown when trying to retrieve Exception from cleanly completed task."));
            }
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);

            Task outer = Task.Factory.StartNew(delegate
            {
                Task inner = Task.Factory.StartNew(delegate { mre.WaitOne(); }, TaskCreationOptions.AttachedToParent);
                mre2.Set();
                throw new Exception("blah");
            });

            // wait until the outer task starts executing on a TPL thread and launches its child task
            mre2.WaitOne();

            // make sure the outer task finishes processing the exception 
            //while (outer.Status == TaskStatus.Running) Thread.Sleep(10);

            if (outer.Exception != null)
            {
                Assert.True(false, string.Format("RunTaskExceptionTest:    > FAILED.  Task.Exception seen before task completes"));
            }
            mre.Set(); // Allow inner to finish
            try { outer.Wait(); }
            catch { }

            Exception e = new Exception("foobomb");

            t = Task.Factory.StartNew(delegate { throw e; });
            try
            {
                t.Wait();
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Count == 1 && ae.InnerExceptions[0] == e)
                    return;
            }

            Assert.True(false, string.Format("RunTaskExceptionTest:  > error: expected an AggregateException w/ a single InnerException to be thrown"));
        }

        private static int NestedLevels(Exception e)
        {
            int levels = 0;
            while (e != null)
            {
                levels++;
                AggregateException ae = e as AggregateException;
                if (ae != null)
                {
                    e = ae.InnerExceptions[0];
                }
                else break;
            }

            return levels;
        }

        [Fact]
        public static void RunTaskWaitAnyTests()
        {
            int numCores = Environment.ProcessorCount;

            // Basic tests w/ <64 tasks
            CoreWaitAnyTest(0, new bool[] { }, -1);
            CoreWaitAnyTest(0, new bool[] { true }, 0);
            CoreWaitAnyTest(0, new bool[] { true, false, false, false }, 0);

            if (numCores > 1)
                CoreWaitAnyTest(0, new bool[] { false, true, false, false }, 1);

            // Tests w/ >64 tasks, w/ winning index >= 64
            CoreWaitAnyTest(100, new bool[] { true }, 100);
            CoreWaitAnyTest(100, new bool[] { true, false, false, false }, 100);
            if (numCores > 1)
                CoreWaitAnyTest(100, new bool[] { false, true, false, false }, 101);

            // Test w/ >64 tasks, w/ winning index < 64
            CoreWaitAnyTest(62, new bool[] { true, false, false, false }, 62);

            // Test w/ >64 tasks, w/ winning index = WaitHandle.WaitTimeout
            CoreWaitAnyTest(WaitHandle.WaitTimeout, new bool[] { true, false, false, false }, WaitHandle.WaitTimeout);

            // Test that already-completed task is returned
            Task t1 = Task.Factory.StartNew(delegate { });
            t1.Wait();
            int tonsOfIterations = 100000;
            // these are cold tasks... should not have started or run at all.
            Task t2 = new Task(delegate { for (int i = 0; i < tonsOfIterations; i++) { } });
            Task t3 = new Task(delegate { for (int i = 0; i < tonsOfIterations; i++) { } });
            Task t4 = new Task(delegate { for (int i = 0; i < tonsOfIterations; i++) { } });

            if (Task.WaitAny(t2, t1, t3, t4) != 1)
            {
                Assert.True(false, string.Format("RunTaskWaitAnyTests:    > FAILED pre-completed task test.  Wrong index returned."));
            }
        }

        [Fact]
        public static void RunTaskWaitAnyTests_Negative()
        {
            // test exceptions
            Assert.Throws<ArgumentNullException>(
               () => Task.WaitAny((Task[])null));
            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WaitAny(new Task[] { null }));
            Assert.Throws<ArgumentOutOfRangeException>(
               () => Task.WaitAny(new Task[] { Task.Factory.StartNew(() => { }) }, -2));
            Assert.Throws<ArgumentOutOfRangeException>(
               () => Task.WaitAny(new Task[] { Task.Factory.StartNew(() => { }) }, TimeSpan.FromMilliseconds(-2)));
        }

        public static void CoreWaitAnyTest(int fillerTasks, bool[] finishMeFirst, int nExpectedReturnCode)
        {
            // We need to do this test in a local TM with # or threads equal to or greater than
            // the number of tasks requested. Otherwise this test can undeservedly fail on dual proc machines

            Task[] tasks = new Task[fillerTasks + finishMeFirst.Length];

            // Create filler tasks
            for (int i = 0; i < fillerTasks; i++) tasks[i] = new Task(delegate { }); // don't start it -- that might make things complicated

            // Create a MRES to gate the finishers
            ManualResetEvent mres = new ManualResetEvent(false);

            // Create worker tasks
            for (int i = 0; i < finishMeFirst.Length; i++)
            {
                tasks[fillerTasks + i] = Task.Factory.StartNew(delegate (object obj)
                {
                    bool finishMe = (bool)obj;
                    if (!finishMe) mres.WaitOne();
                }, (object)finishMeFirst[i]);
            }

            int staRetCode = 0;
            int retCode = Task.WaitAny(tasks);

            Task t = new Task(delegate
            {
                staRetCode = Task.WaitAny(tasks);
            });
            t.Start();
            t.Wait();

            // Release the waiters.
            mres.Set();

            try
            {
                // get rid of the filler tasks by starting them and doing a WaitAll
                for (int i = 0; i < fillerTasks; i++) tasks[i].Start();
                Task.WaitAll(tasks);
            }
            catch (AggregateException)
            {
                // We expect some OCEs if we canceled some filler tasks.
                if (fillerTasks == 0) throw; // we shouldn't see an exception if we don't have filler tasks.
            }

            if (retCode != nExpectedReturnCode)
            {
                Debug.WriteLine("CoreWaitAnyTest:    Testing WaitAny with {0} tasks, expected winner = {1}",
                    fillerTasks + finishMeFirst.Length, nExpectedReturnCode);
                Assert.True(false, string.Format("CoreWaitAnyTest:   > error: WaitAny() return code not matching expected."));
            }

            if (staRetCode != nExpectedReturnCode)
            {
                Debug.WriteLine("CoreWaitAnyTest:    Testing WaitAny with {0} tasks, expected winner = {1}",
                    fillerTasks + finishMeFirst.Length, nExpectedReturnCode);
                Assert.True(false, string.Format("CoreWaitAnyTest:   > error: WaitAny() return code not matching expected for STA Thread."));
            }
        }

        // basic WaitAny validations with Cancellation token
        [Fact]
        public static void RunTaskWaitAnyTests_WithCancellationTokenTests()
        {
            //Test stuck tasks + a cancellation token
            var mre = new ManualResetEvent(false);
            var tokenSrc = new CancellationTokenSource();
            var task1 = Task.Factory.StartNew(() => mre.WaitOne());
            var task2 = Task.Factory.StartNew(() => mre.WaitOne());
            var waiterTask = Task.Factory.StartNew(() => Task.WaitAny(new Task[] { task1, task2 }, tokenSrc.Token));
            tokenSrc.Cancel();
            Assert.Throws<AggregateException>(() => waiterTask.Wait());
            mre.Set();

            Action<int, bool, bool> testWaitAnyWithCT = delegate (int nTasks, bool useSTA, bool preCancel)
            {
                Task[] tasks = new Task[nTasks];

                CancellationTokenSource ctsForTaskCancellation = new CancellationTokenSource();
                for (int i = 0; i < nTasks; i++) { tasks[i] = new Task(delegate { }, ctsForTaskCancellation.Token); }

                CancellationTokenSource ctsForWaitAny = new CancellationTokenSource();
                if (preCancel)
                    ctsForWaitAny.Cancel();
                CancellationToken ctForWaitAny = ctsForWaitAny.Token;
                Task cancelThread = null;
                Task thread = new Task(delegate
                {
                    try
                    {
                        Task.WaitAny(tasks, ctForWaitAny);
                        Debug.WriteLine("WaitAnyWithCancellationTokenTests:    --Testing {0} pending tasks, STA={1}, preCancel={2}", nTasks, useSTA, preCancel);
                        Assert.True(false, string.Format("WaitAnyWithCancellationTokenTests:    > error: WaitAny() w/ {0} tasks should have thrown OCE, threw no exception.", nTasks));
                    }
                    catch (OperationCanceledException) { }
                    catch
                    {
                        Debug.WriteLine("WaitAnyWithCancellationTokenTests:    --Testing {0} pending tasks, STA={1}, preCancel={2}", nTasks, useSTA, preCancel);
                        Assert.True(false, string.Format("    > error: WaitAny() w/ {0} tasks should have thrown OCE, threw different exception.", nTasks));
                    }
                });

                if (!preCancel)
                {
                    cancelThread = new Task(delegate
                    {
                        for (int i = 0; i < 200; i++) { }
                        ctsForWaitAny.Cancel();
                    });
                    cancelThread.Start();
                }
                thread.Start();
                //thread.Join();
                Task.WaitAll(thread);

                //if (!preCancel) cancelThread.Join();

                try
                {
                    for (int i = 0; i < nTasks; i++) tasks[i].Start(); // get rid of all tasks we created
                    Task.WaitAll(tasks);
                }
                catch
                {
                } // ignore any exceptions
            };

            // Test some small number of tasks
            testWaitAnyWithCT(2, false, true);
            testWaitAnyWithCT(2, false, false);
            testWaitAnyWithCT(2, true, true);
            testWaitAnyWithCT(2, true, false);

            // Now test for 63 tasks (max w/o overflowing w/ CT)
            testWaitAnyWithCT(63, false, true);
            testWaitAnyWithCT(63, false, false);
            testWaitAnyWithCT(63, true, true);
            testWaitAnyWithCT(63, true, false);

            // Now test for 100 tasks (overflows WaitAny())
            testWaitAnyWithCT(100, false, true);
            testWaitAnyWithCT(100, false, false);
            testWaitAnyWithCT(100, true, true);
            testWaitAnyWithCT(100, true, false);
        }

        // creates a large number of tasks and does WaitAll on them from a thread of the specified apartment state
        [Fact]
        [OuterLoop]
        public static void RunTaskWaitAllTests()
        {
            Assert.Throws<ArgumentNullException>(() => Task.WaitAll((Task[])null));
            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WaitAll(new Task[] { null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Task.WaitAll(new Task[] { Task.Factory.StartNew(() => { }) }, -2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Task.WaitAll(new Task[] { Task.Factory.StartNew(() => { }) }, TimeSpan.FromMilliseconds(-2)));

            ThreadPoolHelpers.EnsureMinThreadsAtLeast(10);
            RunTaskWaitAllTest(false, 1);
            RunTaskWaitAllTest(false, 10);
        }

        public static void RunTaskWaitAllTest(bool staThread, int nTaskCount)
        {
            string methodInput = string.Format("RunTaskWaitAllTest:  > WaitAll() Tests for aptState={0}, task count={1}", staThread ? "MTA" : "STA", nTaskCount);
            string excpMsg = "foo";

            int middleCeiling = (int)(nTaskCount / 2);
            if ((nTaskCount % 2) == 1)
                middleCeiling = middleCeiling + 1;
            int nFirstHalfCount = middleCeiling;
            int nSecondHalfCount = nTaskCount - nFirstHalfCount;

            //CancellationTokenSource ctsForSleepAndAckCancelAction = null; // this needs to be allocated every time sleepAndAckCancelAction is about to be used
            Action<object> emptyAction = delegate (Object o) { };
            Action<object> sleepAction = delegate (Object o) { for (int i = 0; i < 200; i++) { } };
            Action<object> longAction = delegate (Object o) { for (int i = 0; i < 400; i++) { } };

            Action<object> sleepAndAckCancelAction = delegate (Object o)
            {
                CancellationToken ct = (CancellationToken)o;
                if (!ct.IsCancellationRequested) ct.WaitHandle.WaitOne();
                throw new OperationCanceledException(ct);   // acknowledge
            };
            Action<object> exceptionThrowAction = delegate (Object o) { throw new Exception(excpMsg); };

            Exception e = null;

            // test case 1: trying: WaitAll() on a group of already completed tasks
            DoRunTaskWaitAllTest(staThread, nTaskCount, emptyAction, true, false, 0, null, 5000, ref e);

            if (e != null)
            {
                Assert.True(false, string.Format(methodInput + ":  RunTaskWaitAllTest:  > error: WaitAll() threw exception unexpectedly."));
            }

            // test case 2: WaitAll() on a group of tasks half of which is already completed, half of which is blocked when we start the wait
            //Debug.WriteLine("  > trying: WaitAll() on a group of tasks half of which is already ");
            //Debug.WriteLine("  >         completed, half of which is blocked when we start the wait");
            DoRunTaskWaitAllTest(staThread, nFirstHalfCount, emptyAction, true, false, nSecondHalfCount, sleepAction, 5000, ref e);

            if (e != null)
            {
                Assert.True(false, string.Format(methodInput + " : RunTaskWaitAllTest:  > error: WaitAll() threw exception unexpectedly."));
            }

            // test case 3: WaitAll() on a group of tasks half of which is Canceled, half of which is blocked when we start the wait
            //Debug.WriteLine("  > trying: WaitAll() on a group of tasks half of which is Canceled,");
            //Debug.WriteLine("  >         half of which is blocked when we start the wait");
            DoRunTaskWaitAllTest(staThread, nFirstHalfCount, sleepAndAckCancelAction, false, true, nSecondHalfCount, emptyAction, 5000, ref e);

            if (!(e is AggregateException) || !((e as AggregateException).InnerExceptions[0] is TaskCanceledException))
            {
                Assert.True(false, string.Format(methodInput + " : RunTaskWaitAllTest:  > error: WaitAll() didn't throw TaskCanceledException while waiting on a group of already canceled tasks.> {0}", e));
            }

            // test case 4: WaitAll() on a group of tasks some of which throws an exception
            //Debug.WriteLine("  > trying: WaitAll() on a group of tasks some of which throws an exception");
            DoRunTaskWaitAllTest(staThread, nFirstHalfCount, exceptionThrowAction, false, false, nSecondHalfCount, sleepAction, 5000, ref e);

            if (!(e is AggregateException) || ((e as AggregateException).InnerExceptions[0].Message != excpMsg))
            {
                Assert.True(false, string.Format(methodInput + "RunTaskWaitAllTest:  > error: WaitAll() didn't throw AggregateException while waiting on a group tasks that throw. > {0}", e));
            }

            //////////////////////////////////////////////////////
            //
            // WaitAll with CancellationToken tests
            //

            // test case 5: WaitAll() on a group of already completed tasks with an unsignaled token
            // this should complete cleanly with no exception
            DoRunTaskWaitAllTestWithCancellationToken(staThread, nTaskCount, true, false, 5000, -1, ref e);

            if (e != null)
            {
                Assert.True(false, string.Format(methodInput + ": RunTaskWaitAllTest:  > error: WaitAll() threw exception unexpectedly."));
            }

            // test case 6: WaitAll() on a group of already completed tasks with an already signaled token
            // this should throw OCE
            DoRunTaskWaitAllTestWithCancellationToken(staThread, nTaskCount, true, false, 5000, 0, ref e);

            if (!(e is OperationCanceledException))
            {
                Assert.True(false, string.Format(methodInput + "RunTaskWaitAllTest:  > error: WaitAll() should have thrown OperationCanceledException."));
            }

            // test case 7: WaitAll() on a group of long tasks with a token that gets canceled after a delay
            // this should throw OCE
            DoRunTaskWaitAllTestWithCancellationToken(staThread, nTaskCount, false, false, 5000, 25, ref e);

            if (!(e is OperationCanceledException))
            {
                Assert.True(false, string.Format(methodInput + "RunTaskWaitAllTest:  > error: WaitAll() should have thrown OperationCanceledException."));
            }
        }

        //
        // the core function for WaitAll tests. Takes 2 types of actions to create tasks, how many copies of each task type
        // to create, whether to wait for the completion of the first group, etc
        //
        public static void DoRunTaskWaitAllTest(bool staThread,
                                                    int numTasksType1,
                                                    Action<object> taskAction1,
                                                    bool bWaitOnAct1,
                                                    bool bCancelAct1,
                                                    int numTasksType2,
                                                    Action<object> taskAction2,
                                                    int timeoutForWaitThread,
                                                    ref Exception refWaitAllException)
        {
            int numTasks = numTasksType1 + numTasksType2;
            Task[] tasks = new Task[numTasks];

            //
            // Test case 1: WaitAll() on a mix of already completed tasks and yet blocked tasks
            //
            for (int i = 0; i < numTasks; i++)
            {
                if (i < numTasksType1)
                {
                    CancellationTokenSource taskCTS = new CancellationTokenSource();

                    //Both setting the cancellationtoken to the new task, and passing it in as the state object so that the delegate can acknowledge using it
                    tasks[i] = Task.Factory.StartNew(taskAction1, (object)taskCTS.Token, taskCTS.Token);
                    if (bCancelAct1) taskCTS.Cancel();

                    try
                    {
                        if (bWaitOnAct1) tasks[i].Wait();
                    }
                    catch { }
                }
                else
                {
                    tasks[i] = Task.Factory.StartNew(taskAction2, null);
                }
            }

            refWaitAllException = null;
            Exception waitAllException = null;
            Task t1 = new Task(delegate ()
            {
                try
                {
                    Task.WaitAll(tasks);
                }
                catch (Exception e)
                {
                    waitAllException = e;
                }
            });


            t1.Start();
            t1.Wait();

            refWaitAllException = waitAllException;
        }


        //
        // the core function for WaitAll tests. Takes 2 types of actions to create tasks, how many copies of each task type
        // to create, whether to wait for the completion of the first group, etc
        //
        public static void DoRunTaskWaitAllTestWithCancellationToken(bool staThread,
                                                    int numTasks,
                                                    bool bWaitOnAct1,
                                                    bool bCancelAct1,
                                                    int timeoutForWaitThread,
                                                    int timeToSignalCancellationToken, // -1 never, 0 beforehand, >0 for a delay
                                                    ref Exception refWaitAllException)
        {
            Task[] tasks = new Task[numTasks];

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            if (timeToSignalCancellationToken == 0)
                cts.Cancel();

            ManualResetEvent mres = new ManualResetEvent(false);

            // If timeToSignalCancellationToken is 0, it means that we pre-signal the cancellation token
            // If timeToSignalCancellationToken is -1, it means that we will never signal the cancellation token
            // Either way, it is OK for the tasks to complete ASAP.
            if (timeToSignalCancellationToken <= 0) mres.Set();

            //
            // Test case 1: WaitAll() on a mix of already completed tasks and yet blocked tasks
            //
            for (int i = 0; i < numTasks; i++)
            {
                CancellationTokenSource taskCTS = new CancellationTokenSource();

                //Both setting the cancellationtoken to the new task, and passing it in as the state object so that the delegate can acknowledge using it
                tasks[i] = Task.Factory.StartNew((obj) => { mres.WaitOne(); }, (object)taskCTS.Token, taskCTS.Token);
                if (bWaitOnAct1) tasks[i].Wait();
                if (bCancelAct1) taskCTS.Cancel();
            }

            if (timeToSignalCancellationToken > 0)
            {
                Task cancelthread = new Task(delegate ()
                {
                    //for (int i = 0; i < timeToSignalCancellationToken; i++) { }
                    Task.Delay(timeToSignalCancellationToken);
                    cts.Cancel();
                });
                cancelthread.Start();
            }

            refWaitAllException = null;
            Exception waitAllException = null;
            Task t1 = new Task(delegate ()
            {
                try
                {
                    Task.WaitAll(tasks, ct);
                }
                catch (Exception e)
                {
                    waitAllException = e;
                }
            });

            t1.Start();
            t1.Wait();

            refWaitAllException = waitAllException;

            // If we delay-signalled the cancellation token, then it is OK to let the tasks complete now.
            if (timeToSignalCancellationToken > 0)
            {
                mres.Set();
                Task.WaitAll(tasks);
            }
        }

        [Fact]
        public static void RunLongRunningTaskTests()
        {
            TaskScheduler tm = TaskScheduler.Default;
            // This is computed such that this number of long-running tasks will result in a back-up
            // without some assistance from TaskScheduler.RunBlocking() or TaskCreationOptions.LongRunning.

            int ntasks = Environment.ProcessorCount * 2;

            Task[] tasks = new Task[ntasks];
            ManualResetEvent mre = new ManualResetEvent(false); // could just use a bool?
            CountdownEvent cde = new CountdownEvent(ntasks); // to count the number of Tasks that successfully start
            for (int i = 0; i < ntasks; i++)
            {
                tasks[i] = Task.Factory.StartNew(delegate
                {
                    cde.Signal(); // indicate that task has begun execution
                    Debug.WriteLine("Signalled");
                    mre.WaitOne();
                }, CancellationToken.None, TaskCreationOptions.LongRunning, tm);
            }
            bool waitSucceeded = cde.Wait(5000);
            if (!waitSucceeded)
            {
                foreach (Task task in tasks)
                    Debug.WriteLine("Status: " + task.Status);
                int count = cde.CurrentCount;
                int initialCount = cde.InitialCount;
                Debug.WriteLine("Wait failed. CDE.CurrentCount: {0}, CDE.Initial Count: {1}", count, initialCount);
                Assert.True(false, string.Format("RunLongRunningTaskTests - TaskCreationOptions.LongRunning:    > FAILED.  Timed out waiting for tasks to start."));
            }

            mre.Set();
            Task.WaitAll(tasks);
        }


        // Various tests to exercise the refactored Task class.
        // Create()==>Factory.StartNew(), Task and Future ctors have been added,
        // and Task.Start() has been added.
        [Fact]
        public static void RunRefactoringTests()
        {
            int temp = 0;
            Task t = new Task(delegate { temp = 1; });
            Task<int> f;
            if (t.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action):   > FAILED.  Status after ctor != Created."));
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = new Task(delegate { temp = 1; }, TaskCreationOptions.None);
            if (t.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action, options):    > FAILED.  Status after ctor != Created."));
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = new Task(delegate (object i) { temp = (int)i; }, 1);
            if (t.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action<object>, object):    > FAILED.  Status after ctor != Created."));
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action<object>, object):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = new Task(delegate (object i) { temp = (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None);
            if (t.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action<object>, object, options):    > FAILED.  Status after ctor != Created."));
            }
            t.Start();
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task(action<object>, object, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = Task.Factory.StartNew(delegate { temp = 1; });
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew(action):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = Task.Factory.StartNew(delegate { temp = 1; }, TaskCreationOptions.None);
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew(action, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = Task.Factory.StartNew(delegate { temp = 1; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew(action, CT, options, TaskScheduler):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = Task.Factory.StartNew(delegate (object i) { temp = (int)i; }, 1);
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew(action<object>, object):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = Task.Factory.StartNew(delegate (object i) { temp = (int)i; }, 1, TaskCreationOptions.None);
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew(action<object>, object, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            t = Task.Factory.StartNew(delegate (object i) { temp = (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            t.Wait();
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew(action<object>, object, CT, options, TaskScheduler):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            TaskCompletionSource<int> tr = new TaskCompletionSource<int>();
            if (tr.Task.Status != TaskStatus.WaitingForActivation)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new TaskCompletionSource<int>():    > FAILED.  Status after ctor != WaitingForActivation."));
            }
            tr.SetResult(1);
            temp = tr.Task.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new TaskCompletionSource<int>():    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = new Task<int>(delegate () { return 1; });
            if (f.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task<int>(Func<int>):    > FAILED.  Status after ctor != Created."));
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task<int>(Func<int>):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = new Task<int>(delegate () { return 1; }, TaskCreationOptions.None);
            if (f.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task<int>(Func<int>, options):    > FAILED.  Status after ctor != Created."));
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task<int>(Func<int>, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = new Task<int>(delegate (object i) { return (int)i; }, 1);
            if (f.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task<int>(Func<object, int>, object):    > FAILED.  Status after ctor != Created."));
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - new Task<int>(Func<object, int>, object)    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = new Task<int>(delegate (object i) { return (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None);
            if (f.Status != TaskStatus.Created)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>(Func<object, int>, object, options):    > FAILED.  Status after ctor != Created."));
            }
            f.Start();
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>(Func<object, int>, object, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task<int>.Factory.StartNew(delegate () { return 1; });
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>.Factory.StartNew(Func<int>):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task<int>.Factory.StartNew(delegate () { return 1; }, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>.Factory.StartNew(Func<int>, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task<int>.Factory.StartNew(delegate () { return 1; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>.Factory.StartNew(Func<int>, CT, options, TaskScheduler):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task<int>.Factory.StartNew(delegate (object i) { return (int)i; }, 1);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>.Factory.StartNew(Func<object, int>, object):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task<int>.Factory.StartNew(delegate (object i) { return (int)i; }, 1, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>.Factory.StartNew(Func<object, int>, object, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task<int>.Factory.StartNew(delegate (object i) { return (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task<int>.Factory.StartNew(Func<object, int>, object, CT, options, TaskScheduler):     > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task.Factory.StartNew<int>(delegate () { return 1; });
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew<int>(Func<int>):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task.Factory.StartNew<int>(delegate () { return 1; }, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew<int>(Func<int>, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task.Factory.StartNew<int>(delegate () { return 1; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew<int>(Func<int>, CT, options, TaskScheduler, options):    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;

            f = Task.Factory.StartNew<int>((object i) => { return (int)i; }, 1);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew<int>(Func<object, int>, object)    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            f = Task.Factory.StartNew<int>((object i) => { return (int)i; }, 1, TaskCreationOptions.None);
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew<int>(Func<object, int>, object, options).    > FAILED.  Delegate failed to execute."));
            }

            temp = 0;
            tr = new TaskCompletionSource<int>();
            tr.SetResult(1);
            f = tr.Task;
            try
            {
                f.ContinueWith((tt) => { temp = 1; }).Wait();
                if (temp != 1)
                {
                    Assert.True(false, string.Format("RunRefactoringTests - Continuation off of TaskCompletionSource.Task:    > FAILED!  temp should be 1, is {0}", temp));
                }
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Continuation off of TaskCompletionSource.Task:    > FAILED! exception: {0}", e.Message));
            }
        }

        [Fact]
        public static void RunRefactoringTests_NegativeTests()
        {
            TaskCompletionSource<int> tr = new TaskCompletionSource<int>();
            int temp = 0;
            Task<int> f = Task.Factory.StartNew<int>((object i) => { return (int)i; }, 1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
            Task t;
            temp = f.Result;
            if (temp != 1)
            {
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew<int>(Func<object, int>, object, CT, options, TaskScheduler).    > FAILED.  Delegate failed to execute."));
            }

            f = new TaskCompletionSource<int>().Task;
            try
            {
                f.Start();
                Assert.True(false, string.Format("RunRefactoringTests - TaskCompletionSource<int>.Task (should throw exception):    > FAILED.  No exception thrown."));
            }
            catch (Exception)
            {
                //Assert.True(false, string.Format("    > caught exception: {0}", e.Message));
            }

            t = new Task(delegate { temp = 100; });
            t.Start();
            try
            {
                t.Start();
                Assert.True(false, string.Format("RunRefactoringTests - Restarting Task:    > FAILED.  No exception thrown, when there should be."));
            }
            catch (Exception)
            {
                //Assert.True(false, string.Format("    > caught exception: {0}", e.Message));
            }

            // If we don't do this, the asynchronous setting of temp=100 in the delegate could
            // screw up some tests below.
            t.Wait();

            try
            {
                t = new Task(delegate { temp = 100; }, (TaskCreationOptions)10000);
                Assert.True(false, string.Format("RunRefactoringTests - Illegal Options CTor Task:    > FAILED.  No exception thrown, when there should be."));
            }
            catch (Exception) { }

            try
            {
                t = new Task(null);
                Assert.True(false, string.Format("RunRefactoringTests - Task ctor w/ null action:    > FAILED.  No exception thrown."));
            }
            catch (Exception) { }

            try
            {
                t = Task.Factory.StartNew(null);
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew() w/ Null Action:    > FAILED.  No exception thrown."));
            }
            catch (Exception) { }

            t = new Task(delegate { });
            Task t2 = t.ContinueWith(delegate { });
            try
            {
                t2.Start();
                Assert.True(false, string.Format("RunRefactoringTests - Task.Start() on Continuation Task:    > FAILED.  No exception thrown."));
            }
            catch (Exception) { }

            t = new Task(delegate { });
            try
            {
                t.Start(null);
                Assert.True(false, string.Format("RunRefactoringTests - Task.Start() with null taskScheduler:    > FAILED.  No exception thrown."));
            }
            catch (Exception) { }

            t = Task.Factory.StartNew(delegate { });
            try
            {
                t = Task.Factory.StartNew(delegate { }, CancellationToken.None, TaskCreationOptions.None, (TaskScheduler)null);
                Assert.True(false, string.Format("RunRefactoringTests - Task.Factory.StartNew() with null taskScheduler:    > FAILED.  No exception thrown."));
            }
            catch (Exception) { }

            tr = new TaskCompletionSource<int>();
            tr.SetException(new Exception("some exception"));
            try
            {
                tr.SetResult(5);
                Assert.True(false, string.Format("RunRefactoringTests - TaskCompletionSource set Result after setting Exception:     > FAILED.  No exception thrown."));
            }
            catch (Exception)
            { }
            finally
            {
                //prevent finalize from crashing on exception
                Exception e2 = tr.Task.Exception;
            }

            tr = new TaskCompletionSource<int>();
            tr.SetResult(5);
            try
            {
                tr.SetException(new Exception("some exception"));
                Assert.True(false, string.Format("RunRefactoringTests - TaskCompletionSource set Exception after setting Result    > FAILED.  No exception thrown."));
            }
            catch (Exception)
            { }
            finally
            {
                // clean up.
                temp = tr.Task.Result;
            }
        }

        // Test that TaskStatus values returned from Task.Status are what they should be.
        // TODO: Test WaitingToRun, Blocked.
        [Fact]
        public static void RunTaskStatusTests()
        {
            Task t;
            TaskStatus ts;
            ManualResetEvent mre = new ManualResetEvent(false);

            //
            // Test for TaskStatus.Created
            //
            {
                t = new Task(delegate { });
                ts = t.Status;
                if (ts != TaskStatus.Created)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Create:    > FAILED.  Expected Created status, got {0}", ts));
                }
                if (t.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Create:    > FAILED.  Expected IsCompleted to be false."));
                }
            }

            //
            // Test for TaskStatus.WaitingForActivation
            //
            {
                Task ct = t.ContinueWith(delegate { });
                ts = ct.Status;
                if (ts != TaskStatus.WaitingForActivation)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForActivation:    > FAILED.  Expected WaitingForActivation status (continuation), got {0}", ts));
                }
                if (ct.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForActivation:    > FAILED.  Expected IsCompleted to be false."));
                }

                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                ts = tcs.Task.Status;
                if (ts != TaskStatus.WaitingForActivation)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForActivation:    > FAILED.  Expected WaitingForActivation status (TCS), got {0}", ts));
                }
                if (tcs.Task.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForActivation:    > FAILED.  Expected IsCompleted to be false."));
                }
                tcs.TrySetCanceled();
            }


            //
            // Test for TaskStatus.Canceled for unstarted task being created with an already signaled CTS (this became a case of interest with the TPL Cancellation DCR)
            //
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;
                cts.Cancel();
                t = new Task(delegate { }, token);  // should immediately transition into cancelled state

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Canceled (unstarted Task) (already signaled CTS):    > FAILED.  Expected Canceled status, got {0}", ts));
                }
            }


            //
            // Test for TaskStatus.Canceled for unstarted task being created with an already signaled CTS (this became a case of interest with the TPL Cancellation DCR)
            //
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;

                t = new Task(delegate { }, token);  // should immediately transition into cancelled state
                cts.Cancel();

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Canceled (unstarted Task) (CTS signaled after ctor):   > FAILED.  Expected Canceled status, got {0}", ts));
                }
            }

            //
            // Test that Task whose CT gets canceled while it's running but 
            // which doesn't throw an OCE to acknowledge cancellation will end up in RunToCompletion state
            //
            {
                CancellationTokenSource ctsource = new CancellationTokenSource();
                CancellationToken ctoken = ctsource.Token;

                t = Task.Factory.StartNew(delegate { ctsource.Cancel(); }, ctoken); // cancel but don't acknowledge
                try { t.Wait(); }
                catch { }

                ts = t.Status;
                if (ts != TaskStatus.RanToCompletion)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - Internal Cancellation:    > FAILED.  Expected RanToCompletion status, got {0}", ts));
                }
                if (!t.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - Internal Cancellation:    > FAILED.  Expected IsCompleted to be true."));
                }
            }

            mre.Reset();

            //
            // Test for TaskStatus.Running
            //
            ManualResetEvent mre2 = new ManualResetEvent(false);
            t = Task.Factory.StartNew(delegate { mre2.Set(); mre.WaitOne(); });
            mre2.WaitOne();
            mre2.Reset();
            ts = t.Status;
            if (ts != TaskStatus.Running)
            {
                Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Running:    > FAILED.  Expected Running status, got {0}", ts));
            }
            if (t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Running:    > FAILED.  Expected IsCompleted to be false."));
            }

            // Causes previously created task to finish
            mre.Set();

            //
            // Test for TaskStatus.WaitingForChildrenToComplete
            //
            mre.Reset();
            ManualResetEvent childCreatedMre = new ManualResetEvent(false);
            t = Task.Factory.StartNew(delegate
            {
                Task child = Task.Factory.StartNew(delegate { mre.WaitOne(); }, TaskCreationOptions.AttachedToParent);
                childCreatedMre.Set();
            });


            // This makes sure that task started running on a TP thread and created the child task
            childCreatedMre.WaitOne();
            // and this makes sure the delegate quit and the first stage of t.Finish() executed
            while (t.Status == TaskStatus.Running) { }

            ts = t.Status;
            if (ts != TaskStatus.WaitingForChildrenToComplete)
            {
                Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > FAILED.  Expected WaitingForChildrenToComplete status, got {0}", ts));
            }
            if (t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > FAILED.  Expected IsCompleted to be false."));
            }

            // Causes previously created Task(s) to finish
            mre.Set();



            //
            // Test for TaskStatus.RanToCompletion
            //
            {
                t = Task.Factory.StartNew(delegate { });
                t.Wait();
                ts = t.Status;
                if (ts != TaskStatus.RanToCompletion)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.RanToCompletion:    > FAILED.  Expected RanToCompletion status, got {0}", ts));
                }
                if (!t.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.RanToCompletion:    > FAILED.  Expected IsCompleted to be true."));
                }
            }
        }

        // Test that TaskStatus values returned from Task.Status are what they should be.
        [Fact]
        public static void RunTaskStatusTests_NegativeTests()
        {
            Task t;
            TaskStatus ts;

            //
            // Test for TaskStatus.Canceled for post-start cancellation
            //
            {
                ManualResetEvent taskStartMRE = new ManualResetEvent(false);
                CancellationTokenSource cts = new CancellationTokenSource();
                t = Task.Factory.StartNew(delegate
                {
                    taskStartMRE.Set();
                    while (!cts.Token.IsCancellationRequested) { }
                    throw new OperationCanceledException(cts.Token);
                }, cts.Token);

                taskStartMRE.WaitOne(); //make sure the task starts running before we cancel it
                cts.Cancel();

                // wait on the task to make sure the acknowledgement is fully processed
                try { t.Wait(); }
                catch { }

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Canceled:    > FAILED.  Expected Canceled status, got {0}", ts));
                }
                if (!t.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Canceled:    > FAILED.  Expected IsCompleted to be true."));
                }
            }

            //
            // Make sure that AcknowledgeCancellation() works correctly
            //
            {
                CancellationTokenSource ctsource = new CancellationTokenSource();
                CancellationToken ctoken = ctsource.Token;

                t = Task.Factory.StartNew(delegate
                {
                    while (!ctoken.IsCancellationRequested) { }
                    throw new OperationCanceledException(ctoken);
                }, ctoken);
                ctsource.Cancel();

                try { t.Wait(); }
                catch { }

                ts = t.Status;
                if (ts != TaskStatus.Canceled)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - AcknowledgeCancellation:     > FAILED.  Expected Canceled after AcknowledgeCancellation, got {0}", ts));
                }
            }

            // Test that cancellation acknowledgement does not slip past WFCTC improperly
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                bool innerStarted = false;
                SpinWait sw = new SpinWait();
                ManualResetEvent mreFaulted = new ManualResetEvent(false);
                mreFaulted.Reset();
                Task tCanceled = Task.Factory.StartNew(delegate
                {
                    Task tInner = Task.Factory.StartNew(delegate { mreFaulted.WaitOne(); }, TaskCreationOptions.AttachedToParent);
                    innerStarted = true;

                    cts.Cancel();
                    throw new OperationCanceledException(cts.Token);
                }, cts.Token);

                // and this makes sure the delegate quit and the first stage of t.Finish() executed
                while (!innerStarted || tCanceled.Status == TaskStatus.Running)
                    sw.SpinOnce();

                ts = tCanceled.Status;
                if (ts != TaskStatus.WaitingForChildrenToComplete)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > canceledTask FAILED.  Expected status = WaitingForChildrenToComplete, got {0}.", ts));
                }
                if (tCanceled.IsCanceled)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > canceledTask FAILED.  IsFaulted is true before children have completed."));
                }
                if (tCanceled.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > canceledTask FAILED.  IsCompleted is true before children have completed."));
                }

                mreFaulted.Set();
                try { tCanceled.Wait(); }
                catch { }
            }

            //
            // Test for TaskStatus.Faulted
            //
            {
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    t = Task.Factory.StartNew(delegate { throw new Exception("Some Unhandled Exception"); }, ct);
                    t.Wait();
                    cts.Cancel(); // Should have NO EFFECT on status, since task already completed/faulted.
                }
                catch { }
                ts = t.Status;
                if (ts != TaskStatus.Faulted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Faulted:    > FAILED.  Expected Faulted status, got {0}", ts));
                }
                if (!t.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Faulted:    > FAILED.  Expected IsCompleted to be true."));
                }
            }



            // Test that an exception does not skip past WFCTC improperly
            {
                ManualResetEvent mreFaulted = new ManualResetEvent(false);
                bool innerStarted = false;

                // I Think SpinWait has been implemented on all future platforms because
                // it is in the Contract.
                // So we can ignore this Thread.SpinWait(100);

                SpinWait sw = new SpinWait();
                Task tFaulted = Task.Factory.StartNew(delegate
                {
                    Task tInner = Task.Factory.StartNew(delegate { mreFaulted.WaitOne(); }, TaskCreationOptions.AttachedToParent);
                    innerStarted = true;
                    throw new Exception("oh no!");
                });

                // this makes sure the delegate quit and the first stage of t.Finish() executed
                while (!innerStarted || tFaulted.Status == TaskStatus.Running)
                    sw.SpinOnce();
                ts = tFaulted.Status;
                if (ts != TaskStatus.WaitingForChildrenToComplete)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > faultedTask FAILED.  Expected status = WaitingForChildrenToComplete, got {0}.", ts));
                }
                if (tFaulted.IsFaulted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > faultedTask FAILED.  IsFaulted is true before children have completed."));
                }
                if (tFaulted.IsCompleted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.WaitingForChildrenToComplete:    > faultedTask FAILED.  IsCompleted is true before children have completed."));
                }

                mreFaulted.Set();
                try { tFaulted.Wait(); }
                catch { }
            }

            // Make sure that Faulted trumps Canceled
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;

                t = Task.Factory.StartNew(delegate
                {
                    Task exceptionalChild = Task.Factory.StartNew(delegate { throw new Exception("some exception"); }, TaskCreationOptions.AttachedToParent); //this should push an exception in our list

                    cts.Cancel();
                    throw new OperationCanceledException(ct);
                }, ct);

                try { t.Wait(); }
                catch { }

                ts = t.Status;
                if (ts != TaskStatus.Faulted)
                {
                    Assert.True(false, string.Format("RunTaskStatusTests - TaskStatus.Faulted trumps Cancelled:    > FAILED.  Expected Faulted to trump Canceled"));
                }
            }
        }

        // Just runs a task and waits on it.
        [Fact]
        public static void RunTaskWaitTest()
        {
            // wait on non-exceptional task
            Task t = Task.Factory.StartNew(delegate { });
            t.Wait();

            if (!t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskWaitTest:  > error: task reported back !IsCompleted"));
            }

            // wait on non-exceptional delay started task
            t = new Task(delegate { });
            t.Start();
            //Timer tmr = new Timer((o) => t.Start(), null, 100, Timeout.Infinite);
            t.Wait();

            // This keeps a reference to the Timer so that it does not get GC'd
            // while we are waiting.
            //tmr.Dispose();

            if (!t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskWaitTest:  > error: constructed task reported back !IsCompleted"));
            }

            // This keeps a reference to the Timer so that it does not get GC'd
            // while we are waiting.
            //tmr.Dispose();

            // wait on a task that has children
            int numChildren = 10;
            CountdownEvent cntEv = new CountdownEvent(numChildren);
            t = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < numChildren; i++)
                    Task.Factory.StartNew(() => { cntEv.Signal(); }, TaskCreationOptions.AttachedToParent);
            });

            t.Wait();
            if (!cntEv.IsSet)
            {
                Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on a task with attached children returned before all children completed."));
            }
        }

        // Just runs a task and waits on it.
        [Fact]
        public static void RunTaskWaitTest_NegativeTests()
        {
            string exceptionMsg = "myexception";

            // test exceptions
            var task = Task.Factory.StartNew(() => { });
            Assert.Throws<ArgumentOutOfRangeException>(() => task.Wait(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => task.Wait(TimeSpan.FromMilliseconds(-2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => task.Wait(TimeSpan.FromMilliseconds(uint.MaxValue)));

            // wait on a task that gets canceled
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            ManualResetEvent taskStartedEvent = new ManualResetEvent(false);
            Task t = Task.Factory.StartNew(() =>
            {
                taskStartedEvent.Set();
                while (!ct.IsCancellationRequested) { }
                throw new OperationCanceledException(ct);   //acknowledge the request
            }, ct);

            taskStartedEvent.WaitOne(); // make sure the task starts running before we set the CTS
            cts.Cancel();
            //tmr = new Timer((o) => cts.Cancel(), null, 100, Timeout.Infinite);

            try
            {
                t.Wait();
                Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on exceptional task show have thrown."));
            }
            catch (Exception e)
            {
                if (!(e is AggregateException) ||
                    ((AggregateException)e).InnerExceptions.Count != 1 ||
                    !(((AggregateException)e).InnerExceptions[0] is TaskCanceledException))
                {
                    Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on exceptional task threw wrong exception."));
                }
            }

            // wait on a task that throws
            t = Task.Factory.StartNew(() => { throw new Exception(exceptionMsg); });
            try
            {
                t.Wait();
                Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on exceptional task show have thrown."));
            }
            catch (Exception e)
            {
                if (!(e is AggregateException) ||
                    ((AggregateException)e).InnerExceptions.Count != 1 ||
                    ((AggregateException)e).InnerExceptions[0].Message != exceptionMsg)
                {
                    Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on exceptional task threw wrong exception."));
                }
            }

            // wait on a task that has an exceptional child task 
            Task childTask = null;
            t = Task.Factory.StartNew(() =>
            {
                childTask = Task.Factory.StartNew(() => { throw new Exception(exceptionMsg); }, TaskCreationOptions.AttachedToParent);
            });

            try
            {
                t.Wait();
                Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on a task with an exceptional child should have thrown."));
            }
            catch (Exception e)
            {
                AggregateException outerAggExp = e as AggregateException;
                AggregateException innerAggExp = null;

                if (outerAggExp == null ||
                    outerAggExp.InnerExceptions.Count != 1 ||
                    !(outerAggExp.InnerExceptions[0] is AggregateException))
                {
                    Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on task with exceptional child threw an exception other than AggExp(AggExp(childsException))."));
                }

                innerAggExp = outerAggExp.InnerExceptions[0] as AggregateException;

                if (innerAggExp.InnerExceptions.Count != 1 ||
                    innerAggExp.InnerExceptions[0].Message != exceptionMsg)
                {
                    Assert.True(false, string.Format("RunTaskWaitTest:  > error: Wait on task with exceptional child threw AggExp(AggExp(childsException)), but contained wrong child exception."));
                }
            }
        }

        // Just runs a task and waits on it.
        [Fact]
        public static void RunTaskRecursiveWaitTest()
        {
            Task t2 = null;
            Task t = Task.Factory.StartNew(delegate
            {
                t2 = Task.Factory.StartNew(delegate { });
                t2.Wait();
            });
            t.Wait();

            if (!t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTest:  > error: task reported back !t.IsCompleted"));
            }

            if (!t2.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTest:  > error: task reported back !t2.IsCompleted"));
            }

            t2 = null;
            t = new Task(delegate
            {
                t2 = new Task(delegate { });
                t2.Start();
                t2.Wait();
            });
            t.Start();
            t.Wait();

            if (!t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTest:  > error: constructed task reported back !t.IsCompleted"));
            }

            if (!t2.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTest:  > error: constructed task reported back !t2.IsCompleted"));
            }
        }

        // Just runs a task and waits on it, using a timeout.
        [Fact]
        public static void RunTaskWaitTimeoutTest()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task t = Task.Factory.StartNew(delegate { mre.WaitOne(); });
            t.Wait(100);

            if (t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskWaitTimeoutTest:  > error: task reported back IsCompleted"));
            }

            mre.Set();
            t.Wait();

            if (!t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskWaitTimeoutTest:  > error: task reported back !IsCompleted"));
            }
        }

        // Just runs a task and waits on it, using a timeout.
        [Fact]
        public static void RunTaskRecursiveWaitTimeoutTest()
        {
            ManualResetEvent taskStartedMRE = new ManualResetEvent(false);
            ManualResetEvent mre = new ManualResetEvent(false);
            Task t2 = null;
            Task t = Task.Factory.StartNew(delegate
            {
                taskStartedMRE.Set();
                t2 = Task.Factory.StartNew(delegate
                {
                    mre.WaitOne();
                });
                t2.Wait();
            });

            taskStartedMRE.WaitOne();   //wait for the outer task to start executing
            t.Wait(100);

            if (t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: task reported back t.IsCompleted"));
            }

            if (t2.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: task reported back t2.IsCompleted"));
            }

            mre.Set();
            t.Wait();

            if (!t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: task reported back !t.IsCompleted"));
            }

            if (!t2.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: task reported back !t2.IsCompleted"));
            }

            mre.Reset();

            taskStartedMRE.Reset();
            t2 = null;
            t = new Task(delegate
            {
                taskStartedMRE.Set();
                t2 = new Task(delegate
                {
                    mre.WaitOne();
                });
                t2.Start();
                t2.Wait();
            });
            t.Start();
            taskStartedMRE.WaitOne();   //wait for the outer task to start executing
            t.Wait(100);

            if (t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: constructed task reported back t.IsCompleted"));
            }

            if (t2.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: constructed task reported back t2.IsCompleted"));
            }

            mre.Set();
            t.Wait();

            if (!t.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: constructed task reported back !t.IsCompleted"));
            }

            if (!t2.IsCompleted)
            {
                Assert.True(false, string.Format("RunTaskRecursiveWaitTimeoutTest:  > error: constructed task reported back !t2.IsCompleted"));
            }
        }

        [Fact]
        public static void RunTaskCanceledExceptionTests()
        {
            TaskCanceledException tce = null;

            // Test empty constructor
            tce = new TaskCanceledException();
            Assert.Null(tce.Task); // , "RunTaskCanceledExceptionTests:  Expected null Task prop after empty ctor")
            Assert.Null(tce.InnerException); // , "RunTaskCanceledExceptionTests:  Expected null InnerException prop after empty ctor")

            string message = "my exception message";
            tce = new TaskCanceledException(message);
            Assert.True(tce.Message.Equals(message), "RunTaskCanceledExceptionTests:  Message != string passed to ctor(string)");
            Assert.Null(tce.Task); // , "RunTaskCanceledExceptionTests:  Expected null Task prop after ctor(string)")
            Assert.Null(tce.InnerException); // , "RunTaskCanceledExceptionTests:  Expected null InnerException prop after ctor(string)")

            Task t1 = Task.Factory.StartNew(() => { });
            tce = new TaskCanceledException(t1);
            Assert.True(tce.Task == t1, "RunTaskCanceledExceptionTests:  Task != task passed to ctor(Task)");
            Assert.Null(tce.InnerException); // , "RunTaskCanceledExceptionTests:  Expected null InnerException prop after ctor(Task)")

            InvalidOperationException ioe = new InvalidOperationException();
            tce = new TaskCanceledException(message, ioe);
            Assert.True(tce.Message.Equals(message), "RunTaskCanceledExceptionTests:  Message != string passed to ctor(string, exception)");
            Assert.Null(tce.Task); // , "RunTaskCanceledExceptionTests:  Expected null Task prop after ctor(string, exception)")
            Assert.True(tce.InnerException == ioe, "RunTaskCanceledExceptionTests:  InnerException != exception passed to ctor(string, exception)");
        }

        [Fact]
        public static void RunTaskSchedulerExceptionTests()
        {
            TaskSchedulerException tse = null;

            tse = new TaskSchedulerException();
            Assert.Null(tse.InnerException); // , "RunTaskSchedulerExceptionTests:  Expected InnerException==null after empty ctor")

            InvalidOperationException ioe = new InvalidOperationException();
            tse = new TaskSchedulerException(ioe);
            Assert.True(tse.InnerException == ioe, "RunTaskSchedulerExceptionTests:  Expected InnerException == ioe passed to ctor(ex)");

            string message = "my exception message";
            tse = new TaskSchedulerException(message);
            Assert.Null(tse.InnerException); // , "RunTaskSchedulerExceptionTests:  Expected InnerException==null after ctor(string)")
            Assert.True(tse.Message.Equals(message), "RunTaskSchedulerExceptionTests:  Expected Message = message passed to ctor(string)");

            tse = new TaskSchedulerException(message, ioe);
            Assert.True(tse.InnerException == ioe, "RunTaskSchedulerExceptionTests:  Expected InnerException == ioe passed to ctor(string, ex)");
            Assert.True(tse.Message.Equals(message), "RunTaskSchedulerExceptionTests:  Expected Message = message passed to ctor(string, ex)");
        }

        [Fact]
        public static void RunAsyncWaitHandleTests()
        {
            // Start a task, but make sure that it does not complete
            ManualResetEvent mre = new ManualResetEvent(false);
            Task t1 = Task.Factory.StartNew(() => { mre.WaitOne(); });

            // Make sure that waiting on an uncompleted Task's AsyncWaitHandle does not succeed
            WaitHandle wh = ((IAsyncResult)t1).AsyncWaitHandle;
            Assert.False(wh.WaitOne(0), "RunAsyncWaitHandleTests:  Did not expect wait on uncompleted Task's AsyncWaitHandle to succeed");

            // Make sure that waiting on a completed Task's AsyncWaitHandle succeeds
            mre.Set();
            Assert.True(wh.WaitOne(), "RunAsyncWaitHandleTests:  Expected wait on completed Task's AsyncWaitHandle to succeed");

            // To complete coverage for CompletedEvent_get, we need to grab a fresh AsyncWaitHandle from
            // an already-completed Task.
            t1 = Task.Factory.StartNew(() => { });
            t1.Wait();
            wh = ((IAsyncResult)t1).AsyncWaitHandle;
            Assert.True(wh.WaitOne(0), "RunAsyncWaitHandleTests:  Expected wait on AsyncWaitHandle from completed Task to succeed");
        }
    }
}
