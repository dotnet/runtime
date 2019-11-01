// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
// TPL namespaces
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class TaskContinueWhenAllTests
    {
        #region Test Methods

        // Test functionality of "bare" ContinueWhenAll overloads
        [Fact]
        public static void TestContinueWhenAll_bare()
        {
            int smallSize = 2;
            int largeSize = 3;
            Task[] smallTaskArray = null;
            Task[] largeTaskArray = null;
            Task<int>[] smallFutureArray = null;
            Task<int>[] largeFutureArray = null;
            Task tSmall;
            Task tLarge;
            for (int i = 0; i < 2; i++)
            {
                bool antecedentsAreFutures = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool continuationsAreFutures = (j == 0);

                    for (int x = 0; x < 2; x++)
                    {
                        bool useFutureFactory = (x == 0);

                        // This would be a nonsensical combination
                        if (useFutureFactory && !continuationsAreFutures) continue;

                        Debug.WriteLine("    Testing {0} = {3}.Factory.CWAll({1}, {2})",
                            continuationsAreFutures ? "Future" : "Task",
                            antecedentsAreFutures ? "Future[]" : "Task[]",
                            continuationsAreFutures ? "func" : "action",
                            useFutureFactory ? "Task<int>" : "Task");

                        // Set up our antecedents
                        if (antecedentsAreFutures)
                        {
                            makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);
                            if (continuationsAreFutures)
                            {
                                if (useFutureFactory)
                                {
                                    // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = true
                                    tSmall = Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => 10);
                                    tLarge = Task<int>.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => 20);
                                }
                                else // useFutureFactory = false (use Task factory)
                                {
                                    // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Task<int>[] finishedArray) => 10);
                                    tLarge = Task.Factory.ContinueWhenAll<int, int>(largeFutureArray, (Task<int>[] finishedArray) => 20);
                                }
                            }
                            else // continuationsAreFutures = false (continuations are Tasks)
                            {
                                // antecedentsAreFutures=true, continuationsAreFutures=false, useFutureFactory = false
                                tSmall = Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => { });
                                tLarge = Task.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => { });
                            }

                            // Kick off the smallFutureArray
                            startTaskArray(smallFutureArray);
                        }
                        else // antecedentsAreFutures = false (antecedents are Tasks)
                        {
                            makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);
                            if (continuationsAreFutures)
                            {
                                if (useFutureFactory)
                                {
                                    // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = true
                                    tSmall = Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => 10);
                                    tLarge = Task<int>.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => 20);
                                }
                                else // useFutureFactory = false (use TaskFactory)
                                {
                                    // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Task[] finishedArray) => 10);
                                    tLarge = Task.Factory.ContinueWhenAll<int>(largeTaskArray, (Task[] finishedArray) => 20);
                                }
                            }
                            else // continuationsAreFutures = false (continuations are Tasks)
                            {
                                // antecedentsAreFutures=false, continuationsAreFutures=false, useFutureFactory = false
                                tSmall = Task.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => { });
                                tLarge = Task.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => { });
                            }

                            // Kick off the smallTaskArray
                            startTaskArray(smallTaskArray);
                        }

                        // Verify correct behavior for starting small array
                        int result = 0;
                        Exception ex = null;
                        try
                        {
                            if (continuationsAreFutures) result = ((Task<int>)tSmall).Result;
                            else tSmall.Wait();
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }

                        Assert.Null(ex); // , "Did not expect exception from tSmall.Wait()")
                        Assert.True((result == 10) || (!continuationsAreFutures), "Expected valid result from tSmall");
                        Assert.False(tLarge.IsCompleted, "tLarge completed before its time");

                        //
                        // Now start the large array
                        //
                        if (antecedentsAreFutures) startTaskArray(largeFutureArray);
                        else startTaskArray(largeTaskArray);

                        result = 0;
                        try
                        {
                            if (continuationsAreFutures) result = ((Task<int>)tLarge).Result;
                            else tLarge.Wait();
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }

                        Assert.Null(ex); // , "Did not expect exception from tLarge.Wait()")
                        Assert.True((result == 20) || (!continuationsAreFutures), "Expected valid result from tLarge");
                    } // end x-loop (FutureFactory or TaskFactory)
                } // end j-loop (continuations are futures or tasks)
            }// end i-loop (antecedents are futures or tasks)
        }

        // Test functionality of ContinueWhenAll overloads w/ CancellationToken
        [Fact]
        public static void TestContinueWhenAll_CancellationToken()
        {
            int smallSize = 2;
            int largeSize = 3;
            Task[] smallTaskArray = null;
            Task[] largeTaskArray = null;
            Task<int>[] smallFutureArray = null;
            Task<int>[] largeFutureArray = null;
            Task tSmall;
            Task tLarge;
            for (int i = 0; i < 2; i++)
            {
                bool antecedentsAreFutures = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool continuationsAreFutures = (j == 0);
                    for (int k = 0; k < 2; k++)
                    {
                        bool preCanceledToken = (k == 0);
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        if (preCanceledToken) cts.Cancel();

                        for (int x = 0; x < 2; x++)
                        {
                            bool useFutureFactory = (x == 0);

                            // This would be a nonsensical combination
                            if (useFutureFactory && !continuationsAreFutures) continue;

                            Debug.WriteLine("    Testing {0} = {4}.Factory.CWAll({1}, {3}, ct({2}))",
                                continuationsAreFutures ? "Future" : "Task",
                                antecedentsAreFutures ? "Future[]" : "Task[]",
                                preCanceledToken ? "signaled" : "unsignaled",
                                continuationsAreFutures ? "func" : "action",
                                useFutureFactory ? "Task<int>" : "Task");

                            // Set up our antecedents
                            if (antecedentsAreFutures)
                            {
                                makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);
                                if (continuationsAreFutures)
                                {
                                    if (useFutureFactory)
                                    {
                                        // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = true
                                        tSmall = Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => 10, ct);
                                        tLarge = Task<int>.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => 20, ct);
                                    }
                                    else // useFutureFactory = false (use Task factory)
                                    {
                                        // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = false
                                        tSmall = Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Task<int>[] finishedArray) => 10, ct);
                                        tLarge = Task.Factory.ContinueWhenAll<int, int>(largeFutureArray, (Task<int>[] finishedArray) => 20, ct);
                                    }
                                }
                                else // continuationsAreFutures = false (continuations are Tasks)
                                {
                                    // antecedentsAreFutures=true, continuationsAreFutures=false, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => { }, ct);
                                    tLarge = Task.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => { }, ct);
                                }

                                // Kick off the smallFutureArray
                                startTaskArray(smallFutureArray);
                            }
                            else // antecedentsAreFutures = false (antecedents are Tasks)
                            {
                                makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);
                                if (continuationsAreFutures)
                                {
                                    if (useFutureFactory)
                                    {
                                        // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = true
                                        tSmall = Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => 10, ct);
                                        tLarge = Task<int>.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => 20, ct);
                                    }
                                    else // useFutureFactory = false (use TaskFactory)
                                    {
                                        // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = false
                                        tSmall = Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Task[] finishedArray) => 10, ct);
                                        tLarge = Task.Factory.ContinueWhenAll<int>(largeTaskArray, (Task[] finishedArray) => 20, ct);
                                    }
                                }
                                else // continuationsAreFutures = false (continuations are Tasks)
                                {
                                    // antecedentsAreFutures=false, continuationsAreFutures=false, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => { }, ct);
                                    tLarge = Task.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => { }, ct);
                                }

                                // Kick off the smallTaskArray
                                startTaskArray(smallTaskArray);
                            }

                            // Verify correct behavior for starting small array
                            int result = 0;
                            Exception ex = null;
                            try
                            {
                                if (continuationsAreFutures) result = ((Task<int>)tSmall).Result;
                                else tSmall.Wait();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }

                            Assert.True(preCanceledToken == (ex != null), "Expected tSmall.Wait() exception only on preCanceledToken");
                            if (preCanceledToken)
                            {
                                if (ex != null)
                                {
                                    Assert.True(
                                       ((ex is AggregateException) &&
                                         (((AggregateException)ex).InnerExceptions[0].GetType() == typeof(TaskCanceledException))),
                                       "Expected AE(TCE) on tSmall Cancellation, got " + ex.ToString());
                                }
                                Assert.True(tLarge.IsCompleted, "Expected tLarge to complete immediately on pre-canceled token");
                                CheckForCorrectCT(tSmall, ct);
                            }
                            else // !preCanceledToken
                            {
                                Assert.True((result == 10) || (!continuationsAreFutures), "Expected valid result from tSmall");
                                Assert.False(tLarge.IsCompleted, "tLarge completed before its time");
                            }

                            //
                            // Now start the large array
                            //
                            if (antecedentsAreFutures) startTaskArray(largeFutureArray);
                            else startTaskArray(largeTaskArray);

                            result = 0;
                            try
                            {
                                if (continuationsAreFutures) result = ((Task<int>)tLarge).Result;
                                else tLarge.Wait();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }

                            Assert.True(preCanceledToken == (ex != null), "Expected tLarge.Wait() exception only on preCanceledToken");
                            if (preCanceledToken)
                            {
                                if (ex != null)
                                {
                                    Assert.True(
                                       ((ex is AggregateException) &&
                                         (((AggregateException)ex).InnerExceptions[0].GetType() == typeof(TaskCanceledException))),
                                       "Expected AE(TCE) on tLarge cancellation, got " + ex.ToString());
                                }

                                CheckForCorrectCT(tLarge, ct);
                            }
                            else // !preCanceledToken
                            {
                                Assert.True((result == 20) || (!continuationsAreFutures), "Expected valid result from tLarge");
                            }
                        } // end x-loop (FutureFactory or TaskFactory)
                    } // end k-loop (preCanceled or not)
                } // end j-loop (continuations are futures or tasks)
            }// end i-loop (antecedents are futures or tasks)
        }

        // Test functionality of ContinueWhenAll overloads w/ TaskContinuationOptions
        [Fact]
        public static void TestContinueWhenAll_TaskContinuationOptions()
        {
            int smallSize = 2;
            int largeSize = 3;
            Task[] smallTaskArray = null;
            Task[] largeTaskArray = null;
            Task<int>[] smallFutureArray = null;
            Task<int>[] largeFutureArray = null;
            Task tSmall;
            Task tLarge;
            for (int i = 0; i < 2; i++)
            {
                bool antecedentsAreFutures = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool continuationsAreFutures = (j == 0);
                    for (int k = 0; k < 2; k++)
                    {
                        bool longRunning = (k == 0);
                        TaskContinuationOptions tco = longRunning ? TaskContinuationOptions.LongRunning : TaskContinuationOptions.None;

                        for (int x = 0; x < 2; x++)
                        {
                            bool useFutureFactory = (x == 0);

                            // This would be a nonsensical combination
                            if (useFutureFactory && !continuationsAreFutures) continue;

                            Debug.WriteLine("    Testing {0} = {3}.Factory.CWAll({1}, {2}, {4})",
                                continuationsAreFutures ? "Future" : "Task",
                                antecedentsAreFutures ? "Future[]" : "Task[]",
                                continuationsAreFutures ? "func" : "action",
                                useFutureFactory ? "Task<int>" : "Task",
                                tco);

                            // Set up our antecedents
                            if (antecedentsAreFutures)
                            {
                                makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);
                                if (continuationsAreFutures)
                                {
                                    if (useFutureFactory)
                                    {
                                        // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = true
                                        tSmall = Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => 10, tco);
                                        tLarge = Task<int>.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => 20, tco);
                                    }
                                    else // useFutureFactory = false (use Task factory)
                                    {
                                        // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = false
                                        tSmall = Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Task<int>[] finishedArray) => 10, tco);
                                        tLarge = Task.Factory.ContinueWhenAll<int, int>(largeFutureArray, (Task<int>[] finishedArray) => 20, tco);
                                    }
                                }
                                else // continuationsAreFutures = false (continuations are Tasks)
                                {
                                    // antecedentsAreFutures=true, continuationsAreFutures=false, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => { }, tco);
                                    tLarge = Task.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => { }, tco);
                                }

                                // Kick off the smallFutureArray
                                startTaskArray(smallFutureArray);
                            }
                            else // antecedentsAreFutures = false (antecedents are Tasks)
                            {
                                makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);
                                if (continuationsAreFutures)
                                {
                                    if (useFutureFactory)
                                    {
                                        // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = true
                                        tSmall = Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => 10, tco);
                                        tLarge = Task<int>.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => 20, tco);
                                    }
                                    else // useFutureFactory = false (use TaskFactory)
                                    {
                                        // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = false
                                        tSmall = Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Task[] finishedArray) => 10, tco);
                                        tLarge = Task.Factory.ContinueWhenAll<int>(largeTaskArray, (Task[] finishedArray) => 20, tco);
                                    }
                                }
                                else // continuationsAreFutures = false (continuations are Tasks)
                                {
                                    // antecedentsAreFutures=false, continuationsAreFutures=false, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => { }, tco);
                                    tLarge = Task.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => { }, tco);
                                }

                                // Kick off the smallTaskArray
                                startTaskArray(smallTaskArray);
                            }

                            // Verify correct behavior for starting small array
                            int result = 0;
                            Exception ex = null;
                            try
                            {
                                if (continuationsAreFutures) result = ((Task<int>)tSmall).Result;
                                else tSmall.Wait();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }

                            Assert.Null(ex); // , "Did not expect exception from tSmall.Wait()")
                            Assert.True((result == 10) || (!continuationsAreFutures), "Expected valid result from tSmall");
                            Assert.False(tLarge.IsCompleted, "tLarge completed before its time");
                            Assert.Equal((tSmall.CreationOptions & TaskCreationOptions.LongRunning) != 0, longRunning);
                            Assert.True((tSmall.CreationOptions == TaskCreationOptions.None) || longRunning, "tSmall CreationOptions should be None unless longRunning is true");

                            //
                            // Now start the large array
                            //
                            if (antecedentsAreFutures) startTaskArray(largeFutureArray);
                            else startTaskArray(largeTaskArray);

                            result = 0;
                            try
                            {
                                if (continuationsAreFutures) result = ((Task<int>)tLarge).Result;
                                else tLarge.Wait();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }

                            Assert.Null(ex); // , "Did not expect exception from tLarge.Wait()")
                            Assert.True((result == 20) || (!continuationsAreFutures), "Expected valid result from tLarge");
                            Assert.Equal((tLarge.CreationOptions & TaskCreationOptions.LongRunning) != 0, longRunning);
                            Assert.True((tLarge.CreationOptions == TaskCreationOptions.None) || longRunning, "tLarge CreationOptions should be None unless longRunning is true");
                        } // end x-loop (FutureFactory or TaskFactory)
                    } // end k-loop (TaskContinuationOptions are LongRunning or None)
                } // end j-loop (continuations are futures or tasks)
            }// end i-loop (antecedents are futures or tasks)
        }

        // Test functionality of "full up" ContinueWhenAll overloads
        [Fact]
        public static void TestCWAll_CancellationToken_TaskContinuation_TaskScheduler()
        {
            int smallSize = 2;
            int largeSize = 3;
            Task[] smallTaskArray = null;
            Task[] largeTaskArray = null;
            Task<int>[] smallFutureArray = null;
            Task<int>[] largeFutureArray = null;
            Task tSmall;
            Task tLarge;
            for (int i = 0; i < 2; i++)
            {
                bool antecedentsAreFutures = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool continuationsAreFutures = (j == 0);
                    for (int k = 0; k < 2; k++)
                    {
                        bool preCanceledToken = (k == 0);
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        if (preCanceledToken) cts.Cancel();

                        for (int x = 0; x < 2; x++)
                        {
                            bool useFutureFactory = (x == 0);

                            // This would be a nonsensical combination
                            if (useFutureFactory && !continuationsAreFutures) continue;

                            TaskContinuationOptions tco = TaskContinuationOptions.None; // for now
                            TaskScheduler ts = TaskScheduler.Default;

                            Debug.WriteLine("    Testing {0} = {4}.Factory.CWAll({1}, {3}, ct({2}), tco.None, ts.Default)",
                                continuationsAreFutures ? "Future" : "Task",
                                antecedentsAreFutures ? "Future[]" : "Task[]",
                                preCanceledToken ? "signaled" : "unsignaled",
                                continuationsAreFutures ? "func" : "action",
                                useFutureFactory ? "Task<int>" : "Task");

                            // Set up our antecedents
                            if (antecedentsAreFutures)
                            {
                                makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);
                                if (continuationsAreFutures)
                                {
                                    if (useFutureFactory)
                                    {
                                        // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = true
                                        tSmall = Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => 10, ct, tco, ts);
                                        tLarge = Task<int>.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => 20, ct, tco, ts);
                                    }
                                    else // useFutureFactory = false (use Task factory)
                                    {
                                        // antecedentsAreFutures=true, continuationsAreFutures=true, useFutureFactory = false
                                        tSmall = Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Task<int>[] finishedArray) => 10, ct, tco, ts);
                                        tLarge = Task.Factory.ContinueWhenAll<int, int>(largeFutureArray, (Task<int>[] finishedArray) => 20, ct, tco, ts);
                                    }
                                }
                                else // continuationsAreFutures = false (continuations are Tasks)
                                {
                                    // antecedentsAreFutures=true, continuationsAreFutures=false, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Task<int>[] finishedArray) => { }, ct, tco, ts);
                                    tLarge = Task.Factory.ContinueWhenAll<int>(largeFutureArray, (Task<int>[] finishedArray) => { }, ct, tco, ts);
                                }

                                // Kick off the smallFutureArray
                                startTaskArray(smallFutureArray);
                            }
                            else // antecedentsAreFutures = false (antecedents are Tasks)
                            {
                                makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);
                                if (continuationsAreFutures)
                                {
                                    if (useFutureFactory)
                                    {
                                        // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = true
                                        tSmall = Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => 10, ct, tco, ts);
                                        tLarge = Task<int>.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => 20, ct, tco, ts);
                                    }
                                    else // useFutureFactory = false (use TaskFactory)
                                    {
                                        // antecedentsAreFutures=false, continuationsAreFutures=true, useFutureFactory = false
                                        tSmall = Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Task[] finishedArray) => 10, ct, tco, ts);
                                        tLarge = Task.Factory.ContinueWhenAll<int>(largeTaskArray, (Task[] finishedArray) => 20, ct, tco, ts);
                                    }
                                }
                                else // continuationsAreFutures = false (continuations are Tasks)
                                {
                                    // antecedentsAreFutures=false, continuationsAreFutures=false, useFutureFactory = false
                                    tSmall = Task.Factory.ContinueWhenAll(smallTaskArray, (Task[] finishedArray) => { }, ct, tco, ts);
                                    tLarge = Task.Factory.ContinueWhenAll(largeTaskArray, (Task[] finishedArray) => { }, ct, tco, ts);
                                }

                                // Kick off the smallTaskArray
                                startTaskArray(smallTaskArray);
                            }

                            // Verify correct behavior for starting small array
                            int result = 0;
                            Exception ex = null;
                            try
                            {
                                if (continuationsAreFutures) result = ((Task<int>)tSmall).Result;
                                else tSmall.Wait();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }

                            Assert.True(preCanceledToken == (ex != null), "Expected tSmall.Wait() exception only on preCanceledToken");
                            if (preCanceledToken)
                            {
                                if (ex != null)
                                {
                                    Assert.True(
                                       ((ex is AggregateException) &&
                                         (((AggregateException)ex).InnerExceptions[0].GetType() == typeof(TaskCanceledException))),
                                       "Expected AE(TCE) on tSmall Cancellation, got " + ex.ToString());
                                }
                                Assert.True(tLarge.IsCompleted, "Expected tLarge to complete immediately on pre-canceled token");
                                CheckForCorrectCT(tSmall, ct);
                            }
                            else // !preCanceledToken
                            {
                                Assert.True((result == 10) || (!continuationsAreFutures), "Expected valid result from tSmall");
                                Assert.False(tLarge.IsCompleted, "tLarge completed before its time");
                            }

                            //
                            // Now start the large array
                            //
                            if (antecedentsAreFutures) startTaskArray(largeFutureArray);
                            else startTaskArray(largeTaskArray);

                            result = 0;
                            try
                            {
                                if (continuationsAreFutures) result = ((Task<int>)tLarge).Result;
                                else tLarge.Wait();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }

                            Assert.True(preCanceledToken == (ex != null), "Expected tLarge.Wait() exception only on preCanceledToken");
                            if (preCanceledToken)
                            {
                                if (ex != null)
                                {
                                    Assert.True(
                                       ((ex is AggregateException) &&
                                         (((AggregateException)ex).InnerExceptions[0].GetType() == typeof(TaskCanceledException))),
                                       "Expected AE(TCE) on tLarge cancellation, got " + ex.ToString());
                                }

                                CheckForCorrectCT(tLarge, ct);
                            }
                            else // !preCanceledToken
                            {
                                Assert.True((result == 20) || (!continuationsAreFutures), "Expected valid result from tLarge");
                            }
                        } // end x-loop (FutureFactory or TaskFactory)
                    } // end k-loop (preCanceled or not)
                } // end j-loop (continuations are futures or tasks)
            }// end i-loop (antecedents are futures or tasks)
        }

        [Fact]
        public static void RunContinueWhenAllTests_Exceptions()
        {
            int smallSize = 2;
            int largeSize = 3;
            Task[] largeTaskArray = null;
            Task[] smallTaskArray;
            Task<int>[] largeFutureArray = null;
            Task<int>[] smallFutureArray;

            // The remainder of this method will verify exceptional conditions

            // Test that illegal LongRunning | ExecuteSynchronously combination results in an exception.
            Task dummy = Task.Factory.StartNew(delegate { });

            Assert.Throws<ArgumentOutOfRangeException>(
               () => { Task.Factory.ContinueWhenAll(new Task[] { dummy }, _ => { }, TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously); });

            dummy.Wait();

            //
            // Test exceptions when continuing from Task[] => Task
            //
            {
                makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll(smallTaskArray, delegate (Task[] finishedArray) { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                Assert.Throws<ArgumentOutOfRangeException>(
                   () => { Task.Factory.ContinueWhenAll(smallTaskArray, delegate (Task[] finishedArray) { }, TaskContinuationOptions.NotOnFaulted); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll(smallTaskArray, (Action<Task[]>)null); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll(smallTaskArray, (Action<Task[]>)null, CancellationToken.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll(smallTaskArray, (Action<Task[]>)null, TaskContinuationOptions.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll(smallTaskArray, (Action<Task[]>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll((Task[])null, delegate (Task[] finishedArray) { }); });

                smallTaskArray[0] = null;

                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll(smallTaskArray, delegate (Task[] finishedArray) { }));
                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll(new Task[0], delegate (Task[] finishedArray) { }));
            }

            //
            // Test exceptions from continuing from Task[] => Task<int> using TaskFactory
            //
            {
                makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallTaskArray, finishedArray => 10, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                Assert.Throws<ArgumentOutOfRangeException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallTaskArray, finishedArray => 10, TaskContinuationOptions.NotOnFaulted); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Func<Task[], int>)null); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Func<Task[], int>)null, CancellationToken.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Func<Task[], int>)null, TaskContinuationOptions.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallTaskArray, (Func<Task[], int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>((Task[])null, finishedArray => 10); });

                smallTaskArray[0] = null;

                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll(smallTaskArray, finishedArray => 10));
                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll<int>(new Task[] { }, finishedArray => 10));
            }


            //
            // Test exceptions from continuing from Task[] => Task<int> using FutureFactory
            //
            {
                makeCWAllTaskArrays(smallSize, largeSize, out smallTaskArray, out largeTaskArray);

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll(smallTaskArray, finishedArray => 10, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                Assert.Throws<ArgumentOutOfRangeException>(
                   () => { Task<int>.Factory.ContinueWhenAll(smallTaskArray, finishedArray => 10, TaskContinuationOptions.NotOnFaulted); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Func<Task[], int>)null); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Func<Task[], int>)null, CancellationToken.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Func<Task[], int>)null, TaskContinuationOptions.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll(smallTaskArray, (Func<Task[], int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll((Task[])null, finishedArray => 10); });

                smallTaskArray[0] = null;

                AssertExtensions.Throws<ArgumentException>("tasks", () => Task<int>.Factory.ContinueWhenAll(smallTaskArray, finishedArray => 10));
                AssertExtensions.Throws<ArgumentException>("tasks", () => Task<int>.Factory.ContinueWhenAll(new Task[0], finishedArray => 10));
            }

            //
            // Test exceptions from continuing from Task<int>[] => Task
            //
            {
                makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallFutureArray, finishedArray => { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                Assert.Throws<ArgumentOutOfRangeException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallFutureArray, finishedArray => { }, TaskContinuationOptions.NotOnFaulted); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Action<Task<int>[]>)null); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Action<Task<int>[]>)null, CancellationToken.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Action<Task<int>[]>)null, TaskContinuationOptions.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>(smallFutureArray, (Action<Task<int>[]>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int>((Task<int>[])null, finishedArray => { }); });

                smallFutureArray[0] = null;

                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll(smallFutureArray, finishedArray => { }));
                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll(new Task<int>[0], finishedArray => { }));
            }

            //
            // Test exceptions from continuing from Task<int>[] => Task<int> using TaskFactory
            //
            {
                makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, finishedArray => 10, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                Assert.Throws<ArgumentOutOfRangeException>(
                   () => { Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, finishedArray => 10, TaskContinuationOptions.NotOnFaulted); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Func<Task[], int>)null); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Func<Task[], int>)null, CancellationToken.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Func<Task[], int>)null, TaskContinuationOptions.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int, int>(smallFutureArray, (Func<Task[], int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task.Factory.ContinueWhenAll<int, int>((Task<int>[])null, finishedArray => 10); });

                smallFutureArray[0] = null;

                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll(smallFutureArray, finishedArray => 10));
                AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAll(new Task<int>[0], finishedArray => 10));
            }

            //
            // Test exceptions from continuing from Task<int>[] => Task<int> using FutureFactory
            //
            {
                makeCWAllFutureArrays(smallSize, largeSize, out smallFutureArray, out largeFutureArray);


                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, finishedArray => 10, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                Assert.Throws<ArgumentOutOfRangeException>(
                   () => { Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, finishedArray => 10, TaskContinuationOptions.NotOnFaulted); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Func<Task[], int>)null); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Func<Task[], int>)null, CancellationToken.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Func<Task[], int>)null, TaskContinuationOptions.None); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll<int>(smallFutureArray, (Func<Task[], int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });

                Assert.Throws<ArgumentNullException>(
                   () => { Task<int>.Factory.ContinueWhenAll<int>((Task<int>[])null, finishedArray => 10); });

                smallFutureArray[0] = null;

                AssertExtensions.Throws<ArgumentException>("tasks", () => Task<int>.Factory.ContinueWhenAll(smallFutureArray, finishedArray => 10));
                AssertExtensions.Throws<ArgumentException>("tasks", () => Task<int>.Factory.ContinueWhenAll(new Task<int>[0], finishedArray => 10));
            }
        }

        #endregion

        #region Helper Methods / Classes

        private static void CheckForCorrectCT(Task canceledTask, CancellationToken correctToken)
        {
            try
            {
                canceledTask.Wait();
                Assert.True(false, string.Format("    > FAILED!  Pre-canceled result did not throw from Wait()"));
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(e =>
                {
                    var tce = e as TaskCanceledException;
                    if (tce == null)
                    {
                        Assert.True(false, string.Format("    > FAILED!  Pre-canceled result threw non-TCE from Wait()"));
                    }
                    else if (tce.CancellationToken != correctToken)
                    {
                        Assert.True(false, string.Format("    > FAILED!  Pre-canceled result threw TCE w/ wrong token"));
                    }

                    return true;
                });
            }
        }

        private static void makeCWAllFutureArrays(int smallSize, int largeSize, out Task<int>[] aSmall, out Task<int>[] aLarge)
        {
            aLarge = new Task<int>[largeSize];
            aSmall = new Task<int>[smallSize];
            for (int i = 0; i < largeSize; i++) aLarge[i] = new Task<int>(delegate { return 30; });
            for (int i = 0; i < smallSize; i++) aSmall[i] = aLarge[i];
        }

        // used in ContinueWhenAll/ContinueWhenAny tests
        private static void startTaskArray(Task[] tasks)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].Status == TaskStatus.Created) tasks[i].Start();
            }
        }

        private static void makeCWAllTaskArrays(int smallSize, int largeSize, out Task[] aSmall, out Task[] aLarge)
        {
            aLarge = new Task[largeSize];
            aSmall = new Task[smallSize];
            for (int i = 0; i < largeSize; i++) aLarge[i] = new Task(delegate { });
            for (int i = 0; i < smallSize; i++) aSmall[i] = aLarge[i];
        }

        #endregion
    }
}
