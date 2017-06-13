// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Text;
// TPL namespaces
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace System.Threading.Tasks.Tests
{
    public static class TaskRtTests
    {
        [Fact]
        [OuterLoop]
        public static void RunRunTests()
        {
            //
            // Test that AttachedToParent is ignored in Task.Run delegate
            //
            {
                Task tInner = null;

                // Test Run(Action)
                Task t1 = Task.Run(() =>
                {
                    tInner = new Task(() => { }, TaskCreationOptions.AttachedToParent);
                });
                Debug.WriteLine("RunRunTests - AttachToParentIgnored:      -- Waiting on outer Task.  If we hang, that's a failure");
                t1.Wait();
                tInner.Start();
                tInner.Wait();

                // Test Run(Func<int>)
                Task<int> f1 = Task.Run(() =>
                {
                    tInner = new Task(() => { }, TaskCreationOptions.AttachedToParent);
                    return 42;
                });
                Debug.WriteLine("RunRunTests - AttachToParentIgnored:      -- Waiting on outer Task<int>.  If we hang, that's a failure");
                f1.Wait();
                tInner.Start();
                tInner.Wait();

                // Test Run(Func<Task>)
                Task t2 = Task.Run(() =>
                {
                    tInner = new Task(() => { }, TaskCreationOptions.AttachedToParent);
                    Task returnTask = Task.Factory.StartNew(() => { });
                    return returnTask;
                });
                Debug.WriteLine("RunRunTests - AttachToParentIgnored:      -- Waiting on outer Task (unwrap-style).  If we hang, that's a failure");
                t2.Wait();
                tInner.Start();
                tInner.Wait();

                Task<int> fInner = null;
                // Test Run(Func<Task<int>>)
                Task<int> f2 = Task.Run(() =>
                {
                    // Make sure AttachedToParent is ignored for futures as well as tasks
                    fInner = new Task<int>(() => { return 42; }, TaskCreationOptions.AttachedToParent);
                    Task<int> returnTask = Task<int>.Factory.StartNew(() => 11);
                    return returnTask;
                });
                Debug.WriteLine("RunRunTests - AttachToParentIgnored: Waiting on outer Task<int> (unwrap-style).  If we hang, that's a failure");
                f2.Wait();
                fInner.Start();
                fInner.Wait();
            }

            //
            // Test basic functionality w/o cancellation token
            //
            int count = 0;
            Task task1 = Task.Run(() => { count = 1; });
            Debug.WriteLine("RunRunTests: waiting for a task.  If we hang, something went wrong.");
            task1.Wait();
            Assert.True(count == 1, "    > FAILED.  Task completed but did not run.");
            Assert.True(task1.Status == TaskStatus.RanToCompletion, "    > FAILED.  Task did not end in RanToCompletion state.");

            Task<int> future1 = Task.Run(() => { return 7; });
            Debug.WriteLine("RunRunTests - Basic w/o CT: waiting for a future.  If we hang, something went wrong.");
            future1.Wait();
            Assert.True(future1.Result == 7, "    > FAILED.  Future completed but did not run.");
            Assert.True(future1.Status == TaskStatus.RanToCompletion, "    > FAILED.  Future did not end in RanToCompletion state.");

            task1 = Task.Run(() => { return Task.Run(() => { count = 11; }); });
            Debug.WriteLine("RunRunTests - Basic w/o CT: waiting for a task(unwrapped).  If we hang, something went wrong.");
            task1.Wait();
            Assert.True(count == 11, "    > FAILED.  Task(unwrapped) completed but did not run.");
            Assert.True(task1.Status == TaskStatus.RanToCompletion, "    > FAILED.  Task(unwrapped) did not end in RanToCompletion state.");

            future1 = Task.Run(() => { return Task.Run(() => 17); });
            Debug.WriteLine("RunRunTests - Basic w/o CT: waiting for a future(unwrapped).  If we hang, something went wrong.");
            future1.Wait();
            Assert.True(future1.Result == 17, "    > FAILED.  Future(unwrapped) completed but did not run.");
            Assert.True(future1.Status == TaskStatus.RanToCompletion, "    > FAILED.  Future(unwrapped) did not end in RanToCompletion state.");

            //
            // Test basic functionality w/ uncancelled cancellation token
            //
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            Task task2 = Task.Run(() => { count = 21; }, token);
            Debug.WriteLine("RunRunTests: waiting for a task w/ uncanceled token.  If we hang, something went wrong.");
            task2.Wait();
            Assert.True(count == 21, "    > FAILED.  Task w/ uncanceled token completed but did not run.");
            Assert.True(task2.Status == TaskStatus.RanToCompletion, "    > FAILED.  Task w/ uncanceled token did not end in RanToCompletion state.");

            Task<int> future2 = Task.Run(() => 27, token);
            Debug.WriteLine("RunRunTests: waiting for a future w/ uncanceled token.  If we hang, something went wrong.");
            future2.Wait();
            Assert.True(future2.Result == 27, "    > FAILED.  Future w/ uncanceled token completed but did not run.");
            Assert.True(future2.Status == TaskStatus.RanToCompletion, "    > FAILED.  Future w/ uncanceled token did not end in RanToCompletion state.");

            task2 = Task.Run(() => { return Task.Run(() => { count = 31; }); }, token);
            Debug.WriteLine("RunRunTests: waiting for a task(unwrapped) w/ uncanceled token.  If we hang, something went wrong.");
            task2.Wait();
            Assert.True(count == 31, "    > FAILED.  Task(unwrapped) w/ uncanceled token completed but did not run.");
            Assert.True(task2.Status == TaskStatus.RanToCompletion, "    > FAILED.  Task(unwrapped) w/ uncanceled token did not end in RanToCompletion state.");

            future2 = Task.Run(() => Task.Run(() => 37), token);
            Debug.WriteLine("RunRunTests: waiting for a future(unwrapped) w/ uncanceled token.  If we hang, something went wrong.");
            future2.Wait();
            Assert.True(future2.Result == 37, "    > FAILED.  Future(unwrapped) w/ uncanceled token completed but did not run.");
            Assert.True(future2.Status == TaskStatus.RanToCompletion, "    > FAILED.  Future(unwrapped) w/ uncanceled token did not end in RanToCompletion state.");
        }

        [Fact]
        [OuterLoop]
        public static void RunRunTests_Cancellation_Negative()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            int count = 0;
            //
            // Test that the right thing is done with a canceled cancellation token
            //
            cts.Cancel();
            Task task3 = Task.Run(() => { count = 41; }, token);
            Debug.WriteLine("RunRunTests: waiting for a task w/ canceled token.  If we hang, something went wrong.");
            Assert.Throws<AggregateException>(
               () => { task3.Wait(); });
            Assert.False(count == 41, "    > FAILED.  Task w/ canceled token ran when it should not have.");
            Assert.True(task3.IsCanceled, "    > FAILED.  Task w/ canceled token should have ended in Canceled state");

            Task future3 = Task.Run(() => { count = 47; return count; }, token);
            Debug.WriteLine("RunRunTests: waiting for a future w/ canceled token.  If we hang, something went wrong.");
            Assert.Throws<AggregateException>(
               () => { future3.Wait(); });
            Assert.False(count == 47, "    > FAILED.  Future w/ canceled token ran when it should not have.");
            Assert.True(future3.IsCanceled, "    > FAILED.  Future w/ canceled token should have ended in Canceled state");

            task3 = Task.Run(() => { return Task.Run(() => { count = 51; }); }, token);
            Debug.WriteLine("RunRunTests: waiting for a task(unwrapped) w/ canceled token.  If we hang, something went wrong.");
            Assert.Throws<AggregateException>(
               () => { task3.Wait(); });
            Assert.False(count == 51, "    > FAILED.  Task(unwrapped) w/ canceled token ran when it should not have.");
            Assert.True(task3.IsCanceled, "    > FAILED.  Task(unwrapped) w/ canceled token should have ended in Canceled state");

            future3 = Task.Run(() => { return Task.Run(() => { count = 57; return count; }); }, token);
            Debug.WriteLine("RunRunTests: waiting for a future(unwrapped) w/ canceled token.  If we hang, something went wrong.");
            Assert.Throws<AggregateException>(
               () => { future3.Wait(); });
            Assert.False(count == 57, "    > FAILED.  Future(unwrapped) w/ canceled token ran when it should not have.");
            Assert.True(future3.IsCanceled, "    > FAILED.  Future(unwrapped) w/ canceled token should have ended in Canceled state");
        }

        [Fact]
        public static void RunRunTests_FastPathTests()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken token = cts.Token;

            //
            // Test that "fast paths" operate correctly
            //
            {
                // Create some pre-completed Tasks
                Task alreadyCompletedTask = Task.Factory.StartNew(() => { });
                alreadyCompletedTask.Wait();

                Task alreadyFaultedTask = Task.Factory.StartNew(() => { throw new Exception("FAULTED!"); });
                try { alreadyFaultedTask.Wait(); }
                catch { }

                Task alreadyCanceledTask = new Task(() => { }, cts.Token); // should result in cancellation
                try { alreadyCanceledTask.Wait(); }
                catch { }

                // Now run them through Task.Run
                Task fastPath1 = Task.Run(() => alreadyCompletedTask);
                fastPath1.Wait();
                Assert.True(fastPath1.Status == TaskStatus.RanToCompletion,
                   "RunRunTests: Expected proxy for already-ran-to-completion task to be in RanToCompletion status");

                fastPath1 = Task.Run(() => alreadyFaultedTask);
                try
                {
                    fastPath1.Wait();
                    Assert.True(false, string.Format("RunRunTests:    > FAILURE: Expected proxy for already-faulted Task to throw on Wait()"));
                }
                catch { }
                Assert.True(fastPath1.Status == TaskStatus.Faulted, "Expected proxy for already-faulted task to be in Faulted status");

                fastPath1 = Task.Run(() => alreadyCanceledTask);
                try
                {
                    fastPath1.Wait();
                    Assert.True(false, string.Format("RunRunTests:    > FAILURE: Expected proxy for already-canceled Task to throw on Wait()"));
                }
                catch { }
                Assert.True(fastPath1.Status == TaskStatus.Canceled, "RunRunTests: Expected proxy for already-canceled task to be in Canceled status");
            }
            {
                // Create some pre-completed Task<int>s
                Task<int> alreadyCompletedTask = Task<int>.Factory.StartNew(() => 42);
                alreadyCompletedTask.Wait();
                bool doIt = true;

                Task<int> alreadyFaultedTask = Task<int>.Factory.StartNew(() => { if (doIt) throw new Exception("FAULTED!"); return 42; });
                try { alreadyFaultedTask.Wait(); }
                catch { }

                Task<int> alreadyCanceledTask = new Task<int>(() => 42, cts.Token); // should result in cancellation
                try { alreadyCanceledTask.Wait(); }
                catch { }

                // Now run them through Task.Run
                Task<int> fastPath1 = Task.Run(() => alreadyCompletedTask);
                fastPath1.Wait();
                Assert.True(fastPath1.Status == TaskStatus.RanToCompletion, "RunRunTests: Expected proxy for already-ran-to-completion future to be in RanToCompletion status");

                fastPath1 = Task.Run(() => alreadyFaultedTask);
                try
                {
                    fastPath1.Wait();
                    Assert.True(false, string.Format("RunRunTests:    > FAILURE: Expected proxy for already-faulted future to throw on Wait()"));
                }
                catch { }
                Assert.True(fastPath1.Status == TaskStatus.Faulted, "Expected proxy for already-faulted future to be in Faulted status");

                fastPath1 = Task.Run(() => alreadyCanceledTask);
                try
                {
                    fastPath1.Wait();
                    Assert.True(false, string.Format("RunRunTests:    > FAILURE: Expected proxy for already-canceled future to throw on Wait()"));
                }
                catch { }
                Assert.True(fastPath1.Status == TaskStatus.Canceled, "RunRunTests: Expected proxy for already-canceled future to be in Canceled status");
            }
        }

        [Fact]
        public static void RunRunTests_Unwrap_NegativeCases()
        {
            //
            // Test cancellation/exceptions behavior in the unwrap overloads
            //
            Action<UnwrappedScenario> TestUnwrapped =
                delegate (UnwrappedScenario scenario)
                {
                    Debug.WriteLine("RunRunTests: testing Task unwrap (scenario={0})", scenario);

                    CancellationTokenSource cts1 = new CancellationTokenSource();
                    CancellationToken token1 = cts1.Token;

                    int something = 0;
                    Task t1 = Task.Run(() =>
                    {
                        if (scenario == UnwrappedScenario.ThrowExceptionInDelegate) throw new Exception("thrownInDelegate");
                        if (scenario == UnwrappedScenario.ThrowOceInDelegate) throw new OperationCanceledException("thrownInDelegate");
                        return Task.Run(() =>
                        {
                            if (scenario == UnwrappedScenario.ThrowExceptionInTask) throw new Exception("thrownInTask");
                            if (scenario == UnwrappedScenario.ThrowTargetOceInTask) { cts1.Cancel(); throw new OperationCanceledException(token1); }
                            if (scenario == UnwrappedScenario.ThrowOtherOceInTask) throw new OperationCanceledException(CancellationToken.None);
                            something = 1;
                        }, token1);
                    });

                    bool cancellationExpected = (scenario == UnwrappedScenario.ThrowOceInDelegate) ||
                                                (scenario == UnwrappedScenario.ThrowTargetOceInTask);
                    bool exceptionExpected = (scenario == UnwrappedScenario.ThrowExceptionInDelegate) ||
                                             (scenario == UnwrappedScenario.ThrowExceptionInTask) ||
                                             (scenario == UnwrappedScenario.ThrowOtherOceInTask);
                    try
                    {
                        t1.Wait();
                        Assert.False(cancellationExpected || exceptionExpected, "TaskRtTests.RunRunTests: Expected exception or cancellation");
                        Assert.True(something == 1, "TaskRtTests.RunRunTests: Task completed but apparently did not run");
                    }
                    catch (AggregateException ae)
                    {
                        Assert.True(cancellationExpected || exceptionExpected, "TaskRtTests.RunRunTests: Didn't expect exception, got " + ae);
                    }

                    if (cancellationExpected)
                    {
                        Assert.True(t1.IsCanceled, "TaskRtTests.RunRunTests: Expected t1 to be Canceled, was " + t1.Status);
                    }
                    else if (exceptionExpected)
                    {
                        Assert.True(t1.IsFaulted, "TaskRtTests.RunRunTests: Expected t1 to be Faulted, was " + t1.Status);
                    }
                    else
                    {
                        Assert.True(t1.Status == TaskStatus.RanToCompletion, "TaskRtTests.RunRunTests: Expected t1 to be RanToCompletion, was " + t1.Status);
                    }

                    Debug.WriteLine("RunRunTests: -- testing Task<int> unwrap (scenario={0})", scenario);

                    CancellationTokenSource cts2 = new CancellationTokenSource();
                    CancellationToken token2 = cts2.Token;

                    Task<int> f1 = Task.Run(() =>
                    {
                        if (scenario == UnwrappedScenario.ThrowExceptionInDelegate) throw new Exception("thrownInDelegate");
                        if (scenario == UnwrappedScenario.ThrowOceInDelegate) throw new OperationCanceledException("thrownInDelegate");
                        return Task.Run(() =>
                        {
                            if (scenario == UnwrappedScenario.ThrowExceptionInTask) throw new Exception("thrownInTask");
                            if (scenario == UnwrappedScenario.ThrowTargetOceInTask) { cts2.Cancel(); throw new OperationCanceledException(token2); }
                            if (scenario == UnwrappedScenario.ThrowOtherOceInTask) throw new OperationCanceledException(CancellationToken.None);
                            return 10;
                        }, token2);
                    });

                    try
                    {
                        f1.Wait();
                        Assert.False(cancellationExpected || exceptionExpected, "RunRunTests: Expected exception or cancellation");
                        Assert.True(f1.Result == 10, "RunRunTests: Expected f1.Result to be 10, and it was " + f1.Result);
                    }
                    catch (AggregateException ae)
                    {
                        Assert.True(cancellationExpected || exceptionExpected, "RunRunTests: Didn't expect exception, got " + ae);
                    }

                    if (cancellationExpected)
                    {
                        Assert.True(f1.IsCanceled, "RunRunTests: Expected f1 to be Canceled, was " + f1.Status);
                    }
                    else if (exceptionExpected)
                    {
                        Assert.True(f1.IsFaulted, "RunRunTests: Expected f1 to be Faulted, was " + f1.Status);
                    }
                    else
                    {
                        Assert.True(f1.Status == TaskStatus.RanToCompletion, "RunRunTests: Expected f1 to be RanToCompletion, was " + f1.Status);
                    }
                };

            TestUnwrapped(UnwrappedScenario.CleanRun); // no exceptions or cancellation
            TestUnwrapped(UnwrappedScenario.ThrowExceptionInDelegate); // exception in delegate
            TestUnwrapped(UnwrappedScenario.ThrowOceInDelegate); // delegate throws OCE
            TestUnwrapped(UnwrappedScenario.ThrowExceptionInTask); // user-produced Task throws exception
            TestUnwrapped(UnwrappedScenario.ThrowTargetOceInTask); // user-produced Task throws OCE(target)
            TestUnwrapped(UnwrappedScenario.ThrowOtherOceInTask); // user-produced Task throws OCE(random)
        }

        [Fact]
        public static void RunFromResult()
        {
            // Test FromResult with value type
            {
                var results = new[] { -1, 0, 1, 1, 42, Int32.MaxValue, Int32.MinValue, 42, -42 }; // includes duplicate values to ensure that tasks from these aren't the same object
                Task<int>[] tasks = new Task<int>[results.Length];
                for (int i = 0; i < results.Length; i++)
                    tasks[i] = Task.FromResult(results[i]);

                // Make sure they've all completed
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].IsCompleted, "TaskRtTests.RunFromResult:    > FAILED: Task " + i + " should have already completed (value)");

                // Make sure they all completed successfully
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].Status == TaskStatus.RanToCompletion, "TaskRtTests.RunFromResult:    > FAILED: Task " + i + " should have already completed successfully (value)");

                // Make sure no two are the same instance
                for (int i = 0; i < tasks.Length; i++)
                {
                    for (int j = i + 1; j < tasks.Length; j++)
                    {
                        Assert.False(tasks[i] == tasks[j], "TaskRtTests.RunFromResult:    > FAILED: " + i + " and " + j + " created tasks should not be equal (value)");
                    }
                }

                // Make sure they all have the correct results
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].Result == results[i], "TaskRtTests.RunFromResult:    > FAILED: Task " + i + " had the result " + tasks[i].Result + " but should have had " + results[i] + " (value)");
            }

            // Test FromResult with reference type
            {
                var results = new[] { new object(), null, new object(), null, new object() }; // includes duplicate values to ensure that tasks from these aren't the same object
                Task<Object>[] tasks = new Task<Object>[results.Length];
                for (int i = 0; i < results.Length; i++)
                    tasks[i] = Task.FromResult(results[i]);

                // Make sure they've all completed
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].IsCompleted, "TaskRtTests.RunFromResult:    > FAILED: Task " + i + " should have already completed  (ref)");

                // Make sure they all completed successfully
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].Status == TaskStatus.RanToCompletion, "TaskRtTests.RunFromResult:    > FAILED: Task " + i + " should have already completed successfully (ref)");

                // Make sure no two are the same instance
                for (int i = 0; i < tasks.Length; i++)
                {
                    for (int j = i + 1; j < tasks.Length; j++)
                    {
                        Assert.False(tasks[i] == tasks[j], "TaskRtTests.RunFromResult:    > FAILED: " + i + " and " + j + " created tasks should not be equal (ref)");
                    }
                }

                // Make sure they all have the correct results
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].Result == results[i], "TaskRtTests.RunFromResult:    > FAILED: Task " + i + " had the wrong result (ref)");
            }

            // Test FromException
            {
                var exceptions = new Exception[] { new InvalidOperationException(), new OperationCanceledException(), new Exception(), new Exception() }; // includes duplicate values to ensure that tasks from these aren't the same object
                var tasks = exceptions.Select(e => Task.FromException<int>(e)).ToArray();

                // Make sure they've all completed
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].IsCompleted, "Task " + i + " should have already completed");

                // Make sure they all completed with an error
                for (int i = 0; i < tasks.Length; i++)
                    Assert.True(tasks[i].Status == TaskStatus.Faulted, "    > FAILED: Task " + i + " should have already faulted");

                // Make sure no two are the same instance
                for (int i = 0; i < tasks.Length; i++)
                {
                    for (int j = i + 1; j < tasks.Length; j++)
                    {
                        Assert.True(tasks[i] != tasks[j], "    > FAILED: " + i + " and " + j + " created tasks should not be equal");
                    }
                }

                // Make sure they all have the correct exceptions
                for (int i = 0; i < tasks.Length; i++)
                {
                    Assert.NotNull(tasks[i].Exception);
                    Assert.Equal(1, tasks[i].Exception.InnerExceptions.Count);
                    Assert.Equal(exceptions[i], tasks[i].Exception.InnerException);
                }

                // Make sure we handle invalid exceptions correctly
                Assert.Throws<ArgumentNullException>(() => { Task.FromException<int>(null); });

                // Make sure we throw from waiting on a faulted task
                Assert.Throws<AggregateException>(() => { var result = Task.FromException<object>(new InvalidOperationException()).Result; });
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Uses reflection to access internal fields of the Task class.")]
        public static void RunFromResult_FaultedTask()
        {
            // Make sure faulted tasks are actually faulted.  We have little choice for this test but to use reflection,
            // as the harness will crash by throwing from the unobserved event if a task goes unhandled (everywhere
            // other than here it's a bad thing for an exception to go unobserved)
            var faultedTask = Task.FromException<object>(new InvalidOperationException("uh oh"));
            object holderObject = null;
            FieldInfo isHandledField = null;
            var contingentPropertiesField = typeof(Task).GetField("m_contingentProperties", BindingFlags.NonPublic | BindingFlags.Instance);
            if (contingentPropertiesField != null)
            {
                var contingentProperties = contingentPropertiesField.GetValue(faultedTask);
                if (contingentProperties != null)
                {
                    var exceptionsHolderField = contingentProperties.GetType().GetField("m_exceptionsHolder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (exceptionsHolderField != null)
                    {
                        holderObject = exceptionsHolderField.GetValue(contingentProperties);
                        if (holderObject != null)
                        {
                            isHandledField = holderObject.GetType().GetField("m_isHandled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        }
                    }
                }
            }
            Assert.NotNull(holderObject);
            Assert.NotNull(isHandledField);

            Assert.False((bool)isHandledField.GetValue(holderObject), "Expected FromException task to be unobserved before accessing Exception");
            var ignored = faultedTask.Exception;
            Assert.True((bool)isHandledField.GetValue(holderObject), "Expected FromException task to be observed after accessing Exception");
        }

        [Fact]
        public static void RunDelayTests()
        {
            //
            // Test basic functionality
            //
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // These should all complete quickly, with RAN_TO_COMPLETION status.
            Task task1 = Task.Delay(0);
            Task task2 = Task.Delay(new TimeSpan(0));
            Task task3 = Task.Delay(0, token);
            Task task4 = Task.Delay(new TimeSpan(0), token);

            Debug.WriteLine("RunDelayTests:    > Waiting for 0-delayed uncanceled tasks to complete.  If we hang, something went wrong.");
            try
            {
                Task.WaitAll(task1, task2, task3, task4);
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("RunDelayTests:    > FAILED.  Unexpected exception on WaitAll(simple tasks): {0}", e));
            }

            Assert.True(task1.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(0) to run to completion");
            Assert.True(task2.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(TimeSpan(0)) to run to completion");
            Assert.True(task3.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(0,uncanceledToken) to run to completion");
            Assert.True(task4.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(TimeSpan(0),uncanceledToken) to run to completion");

            // This should take some time
            Task task7 = Task.Delay(10000);
            Assert.False(task7.IsCompleted, "RunDelayTests:    > FAILED.  Delay(10000) appears to have completed too soon(1).");
            Task t2 = Task.Delay(10);
            Assert.False(task7.IsCompleted, "RunDelayTests:    > FAILED.  Delay(10000) appears to have completed too soon(2).");
        }

        [Fact]
        public static void RunDelayTests_NegativeCases()
        {
            CancellationTokenSource disposedCTS = new CancellationTokenSource();
            CancellationToken disposedToken = disposedCTS.Token;
            disposedCTS.Dispose();

            //
            // Test for exceptions
            //
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { Task.Delay(-2); });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { Task.Delay(new TimeSpan(1000, 0, 0, 0)); });

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            cts.Cancel();

            // These should complete quickly, in Canceled status
            Task task5 = Task.Delay(0, token);
            Task task6 = Task.Delay(new TimeSpan(0), token);

            Debug.WriteLine("RunDelayTests:    > Waiting for 0-delayed canceled tasks to complete.  If we hang, something went wrong.");
            try
            {
                Task.WaitAll(task5, task6);
            }
            catch { }

            Assert.True(task5.Status == TaskStatus.Canceled, "RunDelayTests:    > FAILED.  Expected Delay(0,canceledToken) to end up Canceled");
            Assert.True(task6.Status == TaskStatus.Canceled, "RunDelayTests:    > FAILED.  Expected Delay(TimeSpan(0),canceledToken) to end up Canceled");

            // Cancellation token on two tasks and waiting on a task a second time.
            CancellationTokenSource cts2 = new CancellationTokenSource();

            Task task8 = Task.Delay(-1, cts2.Token);
            Task task9 = Task.Delay(new TimeSpan(1, 0, 0, 0), cts2.Token);
            Task.Factory.StartNew(() =>
            {
                cts2.Cancel();
            });

            Debug.WriteLine("RunDelayTests:    > Waiting for infinite-delayed, eventually-canceled tasks to complete.  If we hang, something went wrong.");
            try
            {
                Task.WaitAll(task8, task9);
            }
            catch { }

            Assert.True(task8.IsCanceled, "RunDelayTests:    > FAILED.  Expected Delay(-1, token) to end up Canceled.");
            Assert.True(task9.IsCanceled, "RunDelayTests:    > FAILED.  Expected Delay(TimeSpan(1,0,0,0), token) to end up Canceled.");

            try
            {
                task8.Wait();
            }
            catch (AggregateException ae)
            {
                Assert.True(
                   ae.InnerException is OperationCanceledException && ((OperationCanceledException)ae.InnerException).CancellationToken == cts2.Token,
                   "RunDelayTests:    > FAILED.  Expected resulting OCE to contain canceled token.");
            }
        }

        // Test that exceptions are properly wrapped when thrown in various scenarios.
        // Make sure that "indirect" logic does not add superfluous exception wrapping.
        [Fact]
        public static void RunExceptionWrappingTest()
        {
            Action throwException = delegate { throw new InvalidOperationException(); };

            //
            //
            // Test Monadic ContinueWith()
            //
            //
            Action<Task, string> mcwExceptionChecker = delegate (Task mcwTask, string scenario)
            {
                try
                {
                    mcwTask.Wait();
                    Assert.True(false, string.Format("RunExceptionWrappingTest:    > FAILED.  Wait-on-continuation did not throw for {0}", scenario));
                }
                catch (Exception e)
                {
                    int levels = NestedLevels(e);
                    if (levels != 2)
                    {
                        Assert.True(false, string.Format("RunExceptionWrappingTest:    > FAILED.  Exception had {0} levels instead of 2 for {1}.", levels, scenario));
                    }
                }
            };

            // Test mcw off of Task
            Task t = Task.Factory.StartNew(delegate { });

            // Throw in the returned future
            Task<int> mcw1 = t.ContinueWith(delegate (Task antecedent)
            {
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    throw new InvalidOperationException();
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw1, "Task antecedent, throw in ContinuationFunction");

            // Throw in the continuationFunction
            Task<int> mcw2 = t.ContinueWith(delegate (Task antecedent)
            {
                throwException();
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    return 0;
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw2, "Task antecedent, throw in returned Future");

            // Test mcw off of future
            Task<int> f = Task<int>.Factory.StartNew(delegate { return 0; });

            // Throw in the returned future
            mcw1 = f.ContinueWith(delegate (Task<int> antecedent)
            {
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    throw new InvalidOperationException();
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw1, "Future antecedent, throw in ContinuationFunction");

            // Throw in the continuationFunction
            mcw2 = f.ContinueWith(delegate (Task<int> antecedent)
            {
                throwException();
                Task<int> inner = Task<int>.Factory.StartNew(delegate
                {
                    return 0;
                });

                return inner;
            }).Unwrap();

            mcwExceptionChecker(mcw2, "Future antecedent, throw in returned Future");

            //
            //
            // Test FromAsync()
            //
            //

            // Used to test APM-related functionality
            FakeAsyncClass fac = new FakeAsyncClass();

            // Common logic for checking exception nesting
            Action<Task, string> AsyncExceptionChecker = delegate (Task _asyncTask, string msg)
            {
                try
                {
                    _asyncTask.Wait();
                    Assert.True(false, string.Format("RunExceptionWrappingTest APM-Related Funct:    > FAILED. {0} did not throw exception.", msg));
                }
                catch (Exception e)
                {
                    int levels = NestedLevels(e);
                    if (levels != 2)
                    {
                        Assert.True(false, string.Format("RunExceptionWrappingTest APM-Related Funct:    > FAILED.  {0} exception had {1} levels instead of 2", msg, levels));
                    }
                }
            };

            // Try Task.FromAsync(iar,...)
            Task asyncTask = Task.Factory.FromAsync(fac.StartWrite("1234567890", null, null), delegate (IAsyncResult iar)
            {
                throw new InvalidOperationException();
            });

            AsyncExceptionChecker(asyncTask, "Task-based FromAsync(iar, ...)");

            // Try Task.FromAsync(beginMethod, endMethod, ...)
            asyncTask = Task.Factory.FromAsync(fac.StartWrite, delegate (IAsyncResult iar)
            {
                throw new InvalidOperationException();
            }, "1234567890", null);

            AsyncExceptionChecker(asyncTask, "Task-based FromAsync(beginMethod, ...)");

            // Try Task<string>.Factory.FromAsync(iar,...)
            Task<string> asyncFuture = Task<string>.Factory.FromAsync(fac.StartRead(10, null, null), delegate (IAsyncResult iar)
            {
                throwException();
                return fac.EndRead(iar);
            });

            AsyncExceptionChecker(asyncFuture, "Future-based FromAsync(iar, ...)");

            asyncFuture = Task<string>.Factory.FromAsync(fac.StartRead, delegate (IAsyncResult iar)
            {
                throwException();
                return fac.EndRead(iar);
            }, 10, null);

            AsyncExceptionChecker(asyncFuture, "Future-based FromAsync(beginMethod, ...)");
        }

        [Fact]
        public static void RunHideSchedulerTests()
        {
            TaskScheduler[] schedules = new TaskScheduler[2];
            schedules[0] = TaskScheduler.Default;

            for (int i = 0; i < schedules.Length; i++)
            {
                bool useCustomTs = (i == 1);
                TaskScheduler outerTs = schedules[i]; // useCustomTs ? customTs : TaskScheduler.Default;
                // If we are running CoreCLR, then schedules[1] = null, and we should continue in this case.
                if (i == 1 && outerTs == null)
                    continue;

                for (int j = 0; j < 2; j++)
                {
                    bool hideScheduler = (j == 0);
                    TaskCreationOptions creationOptions = hideScheduler ? TaskCreationOptions.HideScheduler : TaskCreationOptions.None;
                    TaskContinuationOptions continuationOptions = hideScheduler ? TaskContinuationOptions.HideScheduler : TaskContinuationOptions.None;
                    TaskScheduler expectedInnerTs = hideScheduler ? TaskScheduler.Default : outerTs;

                    Action<string> commonAction = delegate (string setting)
                    {
                        Assert.Equal(TaskScheduler.Current, expectedInnerTs);

                        // And just for completeness, make sure that inner tasks are started on the correct scheduler
                        TaskScheduler tsInner1 = null, tsInner2 = null;

                        Task tInner = Task.Factory.StartNew(() =>
                        {
                            tsInner1 = TaskScheduler.Current;
                        });
                        Task continuation = tInner.ContinueWith(_ =>
                        {
                            tsInner2 = TaskScheduler.Current;
                        });

                        Task.WaitAll(tInner, continuation);

                        Assert.Equal(tsInner1, expectedInnerTs);
                        Assert.Equal(tsInner2, expectedInnerTs);
                    };

                    Task outerTask = Task.Factory.StartNew(() =>
                    {
                        commonAction("task");
                    }, CancellationToken.None, creationOptions, outerTs);
                    Task outerContinuation = outerTask.ContinueWith(_ =>
                    {
                        commonAction("continuation");
                    }, CancellationToken.None, continuationOptions, outerTs);

                    Task.WaitAll(outerTask, outerContinuation);

                    // Check that the option was internalized by the task/continuation
                    Assert.True(hideScheduler == ((outerTask.CreationOptions & TaskCreationOptions.HideScheduler) != 0), "RunHideSchedulerTests:  FAILED.  CreationOptions mismatch on outerTask");
                    Assert.True(hideScheduler == ((outerContinuation.CreationOptions & TaskCreationOptions.HideScheduler) != 0), "RunHideSchedulerTests:  FAILED.  CreationOptions mismatch on outerContinuation");
                } // end j-loop, for hideScheduler setting
            } // ending i-loop, for customTs setting
        }


        [Fact]
        public static void RunHideSchedulerTests_Negative()
        {
            // Test that HideScheduler is flagged as an illegal option when creating a TCS
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { new TaskCompletionSource<int>(TaskCreationOptions.HideScheduler); });
        }

        [Fact]
        public static void RunDenyChildAttachTests()
        {
            // StartNew, Task and Future
            Task i1 = null;
            Task t1 = Task.Factory.StartNew(() =>
            {
                i1 = new Task(() => { }, TaskCreationOptions.AttachedToParent);
            }, TaskCreationOptions.DenyChildAttach);

            Task i2 = null;
            Task t2 = Task<int>.Factory.StartNew(() =>
            {
                i2 = new Task(() => { }, TaskCreationOptions.AttachedToParent);
                return 42;
            }, TaskCreationOptions.DenyChildAttach);

            // ctor/Start, Task and Future
            Task i3 = null;
            Task t3 = new Task(() =>
            {
                i3 = new Task(() => { }, TaskCreationOptions.AttachedToParent);
            }, TaskCreationOptions.DenyChildAttach);
            t3.Start();

            Task i4 = null;
            Task t4 = new Task<int>(() =>
            {
                i4 = new Task(() => { }, TaskCreationOptions.AttachedToParent);
                return 42;
            }, TaskCreationOptions.DenyChildAttach);
            t4.Start();

            // continuations, Task and Future
            Task i5 = null;
            Task t5 = t3.ContinueWith(_ =>
            {
                i5 = new Task(() => { }, TaskCreationOptions.AttachedToParent);
            }, TaskContinuationOptions.DenyChildAttach);

            Task i6 = null;
            Task t6 = t4.ContinueWith<int>(_ =>
            {
                i6 = new Task(() => { }, TaskCreationOptions.AttachedToParent);
                return 42;
            }, TaskContinuationOptions.DenyChildAttach);

            // If DenyChildAttach doesn't work in any of the cases, then the associated "parent"
            // task will hang waiting for its child.
            Debug.WriteLine("RunDenyChildAttachTests: Waiting on 'parents' ... if we hang, something went wrong.");
            Task.WaitAll(t1, t2, t3, t4, t5, t6);

            // And clean up.
            i1.Start(); i1.Wait();
            i2.Start(); i2.Wait();
            i3.Start(); i3.Wait();
            i4.Start(); i4.Wait();
            i5.Start(); i5.Wait();
            i6.Start(); i6.Wait();
        }

        [Fact]
        public static void RunBasicFutureTest_Negative()
        {
            Task<int> future = new Task<int>(() => 1);
            Assert.ThrowsAsync<ArgumentNullException>(
               () => future.ContinueWith((Action<Task<int>, Object>)null, null, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentNullException>(
              () => future.ContinueWith((Action<Task<int>, Object>)null, null, TaskContinuationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
              () => future.ContinueWith((Action<Task<int>, Object>)null, null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default));
            Assert.ThrowsAsync<ArgumentNullException>(
              () => future.ContinueWith((t, s) => { }, null, CancellationToken.None, TaskContinuationOptions.None, null));

            Assert.ThrowsAsync<ArgumentNullException>(
               () => future.ContinueWith<int>((Func<Task<int>, Object, int>)null, null, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentNullException>(
              () => future.ContinueWith<int>((Func<Task<int>, Object, int>)null, null, TaskContinuationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
              () => future.ContinueWith<int>((Func<Task<int>, Object, int>)null, null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default));
            Assert.ThrowsAsync<ArgumentNullException>(
              () => future.ContinueWith<int>((t, s) => 2, null, CancellationToken.None, TaskContinuationOptions.None, null));
        }

        #region Helper Methods / Classes

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

        internal enum UnwrappedScenario
        {
            CleanRun = 0,
            ThrowExceptionInDelegate = 1,
            ThrowOceInDelegate = 2,
            ThrowExceptionInTask = 3,
            ThrowTargetOceInTask = 4,
            ThrowOtherOceInTask = 5
        };

        // This class is used in testing APM Factory tests.
        private class FakeAsyncClass
        {
            private List<char> _list = new List<char>();

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                lock (_list)
                {
                    for (int i = 0; i < _list.Count; i++) sb.Append(_list[i]);
                }
                return sb.ToString();
            }

            // Silly use of Write, but I wanted to test no-argument StartXXX handling.
            public IAsyncResult StartWrite(AsyncCallback cb, object o)
            {
                return StartWrite("", 0, 0, cb, o);
            }

            public IAsyncResult StartWrite(string s, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, s.Length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int length, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int offset, int length, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (s == null) throw new ArgumentNullException(nameof(s));

                Task t = Task.Factory.StartNew(delegate
                {
                    //Thread.Sleep(100);
                    try
                    {
                        lock (_list)
                        {
                            for (int i = 0; i < length; i++) _list.Add(s[i + offset]);
                        }
                        mar.Signal();
                    }
                    catch (Exception e) { mar.Signal(e); }
                });


                return mar;
            }

            public void EndWrite(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                mar.Wait();
                if (mar.IsFaulted) throw (mar.Exception);
            }

            public IAsyncResult StartRead(AsyncCallback cb, object o)
            {
                return StartRead(128 /*=maxbytes*/, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, buf, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, int offset, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (maxBytes == -1) throw new ArgumentException("Value was not valid", nameof(maxBytes));

                Task t = Task.Factory.StartNew(delegate
                {
                    //Thread.Sleep(100);
                    StringBuilder sb = new StringBuilder();
                    int bytesRead = 0;
                    try
                    {
                        lock (_list)
                        {
                            while ((_list.Count > 0) && (bytesRead < maxBytes))
                            {
                                sb.Append(_list[0]);
                                if (buf != null) { buf[offset] = _list[0]; offset++; }
                                _list.RemoveAt(0);
                                bytesRead++;
                            }
                        }

                        mar.SignalState(sb.ToString());
                    }
                    catch (Exception e) { mar.Signal(e); }
                });

                return mar;
            }


            public string EndRead(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                if (mar.IsFaulted) throw (mar.Exception);
                return (string)mar.AsyncState;
            }

            public void ResetStateTo(string s)
            {
                _list.Clear();
                for (int i = 0; i < s.Length; i++) _list.Add(s[i]);
            }
        }

        // This is an internal class used for a concrete IAsyncResult in the APM Factory tests.
        private class myAsyncResult : IAsyncResult
        {
            private volatile int _isCompleted;
            private ManualResetEvent _asyncWaitHandle;
            private AsyncCallback _callback;
            private object _asyncState;
            private Exception _exception;

            public myAsyncResult(AsyncCallback cb, object o)
            {
                _isCompleted = 0;
                _asyncWaitHandle = new ManualResetEvent(false);
                _callback = cb;
                _asyncState = o;
                _exception = null;
            }

            public bool IsCompleted
            {
                get { return (_isCompleted == 1); }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _asyncWaitHandle; }
            }

            public object AsyncState
            {
                get { return _asyncState; }
            }

            public void Signal()
            {
                _isCompleted = 1;
                _asyncWaitHandle.Set();
                if (_callback != null) _callback(this);
            }

            public void Signal(Exception e)
            {
                _exception = e;
                Signal();
            }

            public void SignalState(object o)
            {
                _asyncState = o;
                Signal();
            }

            public void Wait()
            {
                _asyncWaitHandle.WaitOne();
                if (_exception != null) throw (_exception);
            }

            public bool IsFaulted
            {
                get { return ((_isCompleted == 1) && (_exception != null)); }
            }

            public Exception Exception
            {
                get { return _exception; }
            }
        }

        #endregion
    }
}
