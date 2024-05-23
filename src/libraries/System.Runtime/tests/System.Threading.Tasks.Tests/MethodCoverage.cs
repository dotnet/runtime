// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Collections.Generic;
using System.Diagnostics;

namespace TaskCoverage
{
    public class Coverage
    {
        // Regression test: Validates that tasks can wait on int.MaxValue without assertion.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWait_MaxInt32()
        {
            Task t = Task.Delay(1);
            Debug.WriteLine("Wait with int.Maxvalue");
            Task.WaitAll(new Task[] { t }, int.MaxValue);
        }

        //EH
        [Fact]
        [OuterLoop]
        public static void TaskContinuation()
        {
            int taskCount = Environment.ProcessorCount;
            int maxDOP = int.MaxValue;
            int maxNumberExecutionsPerTask = 1;
            int data = 0;

            Task[] allTasks = new Task[taskCount + 1];

            CancellationTokenSource[] cts = new CancellationTokenSource[taskCount + 1];
            for (int i = 0; i <= taskCount; i++)
            {
                cts[i] = new CancellationTokenSource();
            }

            CancellationTokenSource cts2 = new CancellationTokenSource();
            ConcurrentExclusiveSchedulerPair scheduler = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, maxDOP, maxNumberExecutionsPerTask);
            for (int i = 0; i <= taskCount; i++)
            {
                int j = i;
                allTasks[i] = new Task(() =>
                {
                    new TaskFactory(TaskScheduler.Current).StartNew(() => { }).
                    ContinueWith((task, o) =>
                    {
                        int d = (int)o;
                        Interlocked.Add(ref data, d);
                    }, j).
                    ContinueWith((task, o) =>
                    {
                        int d = (int)o;
                        Interlocked.Add(ref data, d);
                        cts[d].Cancel();
                        if (d <= taskCount)
                        {
                            throw new OperationCanceledException(cts[d].Token);
                        }
                        return "Done";
                    }, j, cts[j].Token).
                    ContinueWith((task, o) =>
                        {
                            int d = (int)o;
                            Interlocked.Add(ref data, d);
                        }, j, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default).Wait(int.MaxValue - 1, cts2.Token);
                });

                allTasks[i].Start(scheduler.ConcurrentScheduler);
            }

            Task.WaitAll(allTasks, int.MaxValue - 1, CancellationToken.None);
            Debug.WriteLine("Tasks ended: result {0}", data);
            Task completion = scheduler.Completion;
            scheduler.Complete();
            completion.Wait();

            int expectedResult = 3 * taskCount * (taskCount + 1) / 2;
            Assert.Equal(expectedResult, data);

            Assert.NotEqual(TaskScheduler.Default.Id, scheduler.ConcurrentScheduler.Id);
            Assert.NotEqual(TaskScheduler.Default.Id, scheduler.ExclusiveScheduler.Id);
        }

        /// <summary>
        /// Test various Task.WhenAll and Wait overloads - EH
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitWithCTS()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEvent mreCont = new ManualResetEvent(false);
            CancellationTokenSource cts = new CancellationTokenSource();

            int? taskId1 = 0; int? taskId2 = 0;
            int? taskId12 = 0; int? taskId22 = 0;

            Task t1 = Task.Factory.StartNew(() => { mre.WaitOne(); taskId1 = Task.CurrentId; });
            Task t2 = Task.Factory.StartNew(() => { mre.WaitOne(); taskId2 = Task.CurrentId; cts.Cancel(); });

            List<Task<int?>> whenAllTaskResult = new List<Task<int?>>();
            List<Task> whenAllTask = new List<Task>();
            whenAllTask.Add(t1); whenAllTask.Add(t2);
            Task<int> contTask = Task.WhenAll(whenAllTask).ContinueWith<int>(
                (task) =>
                {
                    // when task1 ends, the token will be cancelled
                    // move the continuation task in cancellation state
                    if (cts.IsCancellationRequested) { throw new OperationCanceledException(cts.Token); }
                    return 0;
                }, cts.Token);
            contTask.ContinueWith((task) => { mreCont.Set(); });

            whenAllTaskResult.Add(Task<int?>.Factory.StartNew((o) => { mre.WaitOne((int)o); return Task.CurrentId; }, 10));
            whenAllTaskResult.Add(Task<int?>.Factory.StartNew((o) => { mre.WaitOne((int)o); return Task.CurrentId; }, 10));

            t1.Wait(5, cts.Token);
            Task.WhenAll(whenAllTaskResult).ContinueWith((task) => { taskId12 = task.Result[0]; taskId22 = task.Result[1]; mre.Set(); });
            // Task 2 calls CancellationTokenSource.Cancel. Thus, expect and not fail for System.OperationCanceledException being thrown.
            try
            {
                t2.Wait(cts.Token);
            }
            catch (System.OperationCanceledException) { } // expected, do nothing

            Assert.NotEqual<int?>(taskId1, taskId2);
            Assert.NotEqual<int?>(taskId12, taskId22);

            Debug.WriteLine("Waiting on continuation task that should move into the cancelled state.");
            mreCont.WaitOne();
            Assert.True(contTask.Status == TaskStatus.Canceled, "Task status is not correct");
        }

        /// <summary>
        /// test WaitAny and when Any overloads
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskWaitAny_WhenAny()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);

            CancellationTokenSource cts = new CancellationTokenSource();

            Task t1 = Task.Factory.StartNew(() => { mre.WaitOne(); });
            Task t2 = Task.Factory.StartNew(() => { mre.WaitOne(); });

            Task<int?> t11 = Task.Factory.StartNew(() => { mre2.WaitOne(); return Task.CurrentId; });
            Task<int?> t21 = Task.Factory.StartNew(() => { mre2.WaitOne(); return Task.CurrentId; });


            //waitAny with token and timeout
            Task[] waitAny = new Task[] { t1, t2 };
            int timeout = Task.WaitAny(waitAny, 1, cts.Token);

            //task whenany
            Task.Factory.StartNew(() => { Task.Delay(20); mre.Set(); });
            List<Task> whenAnyTask = new List<Task>(); whenAnyTask.Add(t1); whenAnyTask.Add(t2);
            List<Task<int?>> whenAnyTaskResult = new List<Task<int?>>(); whenAnyTaskResult.Add(t11); whenAnyTaskResult.Add(t21);

            //task<tresult> whenany
            int? taskId = 0; //this will be set to the first task<int?> ID that ends
            Task waitOnIt = Task.WhenAny(whenAnyTaskResult).ContinueWith((task) => { taskId = task.Result.Result; });
            Task.WhenAny(whenAnyTask).ContinueWith((task) => { mre2.Set(); });

            Debug.WriteLine("Wait on the scenario to finish");
            waitOnIt.Wait();
            Assert.Equal<int>(-1, timeout);
            Assert.Equal<int>(t11.Id, t11.Result.Value);
            Assert.Equal<int>(t21.Id, t21.Result.Value);

            bool whenAnyVerification = taskId == t11.Id || taskId == t21.Id;

            Assert.True(whenAnyVerification, string.Format("The id for whenAny is not correct expected to be {0} or {1} and it is {2}", t11.Id, t21.Id, taskId));
        }

        [Fact]
        public static void Task_WhenAny_TwoTasks_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("task1", () => Task.WhenAny(null, Task.CompletedTask));
            AssertExtensions.Throws<ArgumentNullException>("task2", () => Task.WhenAny(Task.CompletedTask, null));

            AssertExtensions.Throws<ArgumentNullException>("task1", () => Task.WhenAny(null, Task.FromResult(1)));
            AssertExtensions.Throws<ArgumentNullException>("task2", () => Task.WhenAny(Task.FromResult(2), null));
        }

        [Fact]
        public static void Task_WhenAny_NullTaskElement_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(new Task[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny((ReadOnlySpan<Task>)new Task[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(NullElementIterator<Task>()); });

            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(new Task<int>[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny((ReadOnlySpan<Task<int>>)new Task<int>[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(NullElementIterator<Task<int>>()); });

            static IEnumerable<T> NullElementIterator<T>() where T : Task { yield return null; }
        }

        [Fact]
        public static void Task_WhenAny_NoTasks_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(new Task[0]); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(ReadOnlySpan<Task>.Empty); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(new List<Task>()); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(EmptyIterator<Task>()); });

            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(new Task<int>[0]); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(ReadOnlySpan<Task<int>>.Empty); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(new List<Task<int>>()); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAny(EmptyIterator<Task<int>>()); });

            static IEnumerable<T> EmptyIterator<T>() { yield break; }
        }

        [Fact]
        public static async Task Task_WhenAny_TwoTasks_OnePreCompleted()
        {
            Task<int> t1 = Task.FromResult(1);
            Task<int> t2 = new TaskCompletionSource<int>().Task;

            Assert.Same(t1, await Task.WhenAny((Task)t1, (Task)t2));
            Assert.Same(t1, await Task.WhenAny((Task)t2, (Task)t1));

            Assert.Same(t1, await Task.WhenAny(t1, t2));
            Assert.Same(t1, await Task.WhenAny(t2, t1));
        }

        [Fact]
        public static async Task Task_WhenAny_TwoTasks_BothPreCompleted()
        {
            Task<int> t1 = Task.FromResult(1);
            Task<int> t2 = Task.FromResult(2);

            Assert.Same(t1, await Task.WhenAny((Task)t1, (Task)t2));
            Assert.Same(t1, await Task.WhenAny((Task)t1, (Task)t1));
            Assert.Same(t2, await Task.WhenAny((Task)t2, (Task)t1));

            Assert.Same(t1, await Task.WhenAny(t1, t2));
            Assert.Same(t1, await Task.WhenAny(t1, t1));
            Assert.Same(t2, await Task.WhenAny(t2, t1));
        }

        [Fact]
        public static async Task Task_WhenAny_TwoTasks_WakesOnFirstCompletion()
        {
            // Non-generic, first completes
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<Task> twa = Task.WhenAny((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(42);
                Assert.Same(t1.Task, await twa);
            }

            // Generic, first completes
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<Task<int>> twa = Task.WhenAny(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(42);
                Assert.Same(t1.Task, await twa);
            }

            // Non-generic, second completes
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<Task> twa = Task.WhenAny((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetResult(42);
                Assert.Same(t2.Task, await twa);
            }

            // Generic, second completes
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<Task<int>> twa = Task.WhenAny(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetResult(42);
                Assert.Same(t2.Task, await twa);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CancellationTokenRegitration()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Token.Register((o) => { mre.Set(); }, 1, true);

            cts.CancelAfter(5);
            Debug.WriteLine("Wait on the scenario to finish");
            mre.WaitOne();
        }

        /// <summary>
        /// verify that the taskawaiter.UnsafeOnCompleted is invoked
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskAwaiter()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);
            ManualResetEvent mre3 = new ManualResetEvent(false);

            Task t1 = Task.Factory.StartNew(() => { mre.WaitOne(); });
            Task<int> t11 = Task.Factory.StartNew(() => { mre.WaitOne(); return 1; });
            t1.GetAwaiter().UnsafeOnCompleted(() => { mre2.Set(); });
            t11.GetAwaiter().UnsafeOnCompleted(() => { mre3.Set(); });
            mre.Set();

            Debug.WriteLine("Wait on the scenario to finish");
            mre2.WaitOne(); mre3.WaitOne();
        }

        /// <summary>
        /// verify that the taskawaiter.UnsafeOnCompleted is invoked
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TaskConfigurableAwaiter()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);
            ManualResetEvent mre3 = new ManualResetEvent(false);

            Task t1 = Task.Factory.StartNew(() => { mre.WaitOne(); });
            Task<int> t11 = Task.Factory.StartNew(() => { mre.WaitOne(); return 1; });
            t1.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(() => { mre2.Set(); });
            t11.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(() => { mre3.Set(); });
            mre.Set();

            Debug.WriteLine("Wait on the scenario to finish");
            mre2.WaitOne(); mre3.WaitOne();
        }

        /// <summary>
        /// FromAsync testing: Not supported in .NET Native
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void FromAsync()
        {
            Task emptyTask = new Task(() => { });
            ManualResetEvent mre1 = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);

            Task.Factory.FromAsync(emptyTask, (iar) => { mre1.Set(); }, TaskCreationOptions.None, TaskScheduler.Current);
            Task<int>.Factory.FromAsync(emptyTask, (iar) => { mre2.Set(); return 1; }, TaskCreationOptions.None, TaskScheduler.Current);
            emptyTask.Start();

            Debug.WriteLine("Wait on the scenario to finish");
            mre1.WaitOne();
            mre2.WaitOne();
        }

        [Fact]
        public static void Task_WaitAll_NullArgument_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WaitAll((Task[])null); });
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WaitAll((Task[])null, CancellationToken.None); });
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WaitAll((Task[])null, 30_000); });
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WaitAll((Task[])null, TimeSpan.FromSeconds(30)); });
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WaitAll((Task[])null, 30_000, CancellationToken.None); });
        }

        [Fact]
        public static void Task_WaitAll_NullTaskElement_Throws()
        {
            Task[] nullElement = [null];
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WaitAll(nullElement); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WaitAll((ReadOnlySpan<Task>)nullElement); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WaitAll(nullElement, CancellationToken.None); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WaitAll(nullElement, 30_000); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WaitAll(nullElement, TimeSpan.FromSeconds(30)); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WaitAll(nullElement, 30_000, CancellationToken.None); });
        }

        [Fact]
        public static void Task_WaitAll_InvalidArgument_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Task.WaitAll([Task.Factory.StartNew(() => { })], -2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Task.WaitAll([Task.Factory.StartNew(() => { })], -2, CancellationToken.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => Task.WaitAll([Task.Factory.StartNew(() => { })], TimeSpan.FromMilliseconds(-2)));
        }

        [Fact]
        public static void Task_WhenAll_NullArgument_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WhenAll((Task[])null); });
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WhenAll((IEnumerable<Task>)null); });

            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WhenAll((Task<int>[])null); });
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => { Task.WhenAll((IEnumerable<Task<int>>)null); });
        }

        [Fact]
        public static void Task_WhenAll_NullTaskElement_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAll(new Task[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAll((ReadOnlySpan<Task>)new Task[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAll(NullElementIterator<Task>()); });

            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAll(new Task<int>[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAll((ReadOnlySpan<Task<int>>)new Task<int>[] { null }); });
            AssertExtensions.Throws<ArgumentException>("tasks", () => { Task.WhenAll(NullElementIterator<Task<int>>()); });

            static IEnumerable<T> NullElementIterator<T>() where T : Task { yield return null; }
        }

        [Fact]
        public static void Task_WhenAll_NoTasks_IsCompletedSuccessfully()
        {
            Assert.True(Task.WhenAll(new Task[0]).IsCompletedSuccessfully);
            Assert.True(Task.WhenAll(ReadOnlySpan<Task>.Empty).IsCompletedSuccessfully);
            Assert.True(Task.WhenAll(new List<Task>()).IsCompletedSuccessfully);
            Assert.True(Task.WhenAll(EmptyIterator<Task>()).IsCompletedSuccessfully);

            AssertIsCompletedWithEmptyResult(Task.WhenAll(new Task<int>[0]));
            AssertIsCompletedWithEmptyResult(Task.WhenAll(ReadOnlySpan<Task<int>>.Empty));
            AssertIsCompletedWithEmptyResult(Task.WhenAll(new List<Task<int>>()));
            AssertIsCompletedWithEmptyResult(Task.WhenAll(EmptyIterator<Task<int>>()));

            static IEnumerable<T> EmptyIterator<T>() { yield break; }

            static void AssertIsCompletedWithEmptyResult(Task<int[]> task)
            {
                Assert.True(task.IsCompletedSuccessfully);
                Assert.Empty(task.Result);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_BothPreCompleted()
        {
            Task<int> t1 = Task.FromResult(1);
            Task<int> t2 = Task.FromResult(2);

            Assert.True(Task.WhenAll((Task)t1, (Task)t2).IsCompletedSuccessfully);
            Assert.True(Task.WhenAll((Task)t1, (Task)t1).IsCompletedSuccessfully);
            Assert.True(Task.WhenAll((Task)t2, (Task)t1).IsCompletedSuccessfully);

            AssertIsCompletedSuccessfullyWithResult([1, 2], Task.WhenAll(t1, t2));
            AssertIsCompletedSuccessfullyWithResult([1, 1], Task.WhenAll(t1, t1));
            AssertIsCompletedSuccessfullyWithResult([2, 1], Task.WhenAll(t2, t1));

            static void AssertIsCompletedSuccessfullyWithResult(int[] expected, Task<int[]> task)
            {
                Assert.True(task.IsCompletedSuccessfully);
                Assert.Equal(expected, task.Result);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_OnePreCompleted()
        {
            Task<int> t1 = new TaskCompletionSource<int>().Task;
            Task<int> t2 = Task.FromResult(2);

            Assert.False(Task.WhenAll((Task)t1, (Task)t2).IsCompletedSuccessfully);
            Assert.False(Task.WhenAll((Task)t1, (Task)t1).IsCompletedSuccessfully);
            Assert.False(Task.WhenAll((Task)t2, (Task)t1).IsCompletedSuccessfully);

            Assert.False(Task.WhenAll(t1, t2).IsCompletedSuccessfully);
            Assert.False(Task.WhenAll(t1, t1).IsCompletedSuccessfully);
            Assert.False(Task.WhenAll(t2, t1).IsCompletedSuccessfully);
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WakesOnBothCompletion()
        {
            // Non-generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.False(twa.IsCompleted);
                t2.SetResult(2);
                Assert.True(twa.IsCompletedSuccessfully);
            }

            // Generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.False(twa.IsCompleted);
                t2.SetResult(2);
                Assert.True(twa.IsCompletedSuccessfully);
                Assert.Equal(new int[] { 1, 2 }, twa.Result);
            }

            // Non-generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetResult(2);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.True(twa.IsCompletedSuccessfully);
            }

            // Generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetResult(2);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.True(twa.IsCompletedSuccessfully);
                Assert.Equal(new int[] { 1, 2 }, twa.Result);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WhenPreCompletedFromException()
        {
            Exception exception = new Exception();
            Task<int> t1 = Task.FromException<int>(exception);
            Task<int> t2 = Task.FromResult(2);

            AssertIsCompletedWithException(Task.WhenAll((Task)t1, (Task)t2));
            AssertIsCompletedWithException(Task.WhenAll((Task)t1, (Task)t1));
            AssertIsCompletedWithException(Task.WhenAll((Task)t2, (Task)t1));

            AssertIsCompletedWithException(Task.WhenAll(t1, t2));
            AssertIsCompletedWithException(Task.WhenAll(t1, t1));
            AssertIsCompletedWithException(Task.WhenAll(t2, t1));

            void AssertIsCompletedWithException(Task task)
            {
                Assert.True(task.IsCompleted);
                Assert.True(task.IsFaulted);
                Assert.Same(exception, task.Exception?.InnerException);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WhenBothPreCompletedFromException()
        {
            Exception e1 = new Exception();
            Exception e2 = new Exception();
            Task<int> t1 = Task.FromException<int>(e1);
            Task<int> t2 = Task.FromException<int>(e2);

            AssertIsCompletedWithException([e1, e2], Task.WhenAll((Task)t1, (Task)t2));
            AssertIsCompletedWithException([e1, e1], Task.WhenAll((Task)t1, (Task)t1));
            AssertIsCompletedWithException([e2, e1], Task.WhenAll((Task)t2, (Task)t1));

            AssertIsCompletedWithException([e1, e2], Task.WhenAll(t1, t2));
            AssertIsCompletedWithException([e1, e1], Task.WhenAll(t1, t1));
            AssertIsCompletedWithException([e2, e1], Task.WhenAll(t2, t1));

            static void AssertIsCompletedWithException(Exception[] exceptions, Task task)
            {
                Assert.True(task.IsCompleted);
                Assert.True(task.IsFaulted);
                Assert.Equal(exceptions, task.Exception?.InnerExceptions);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WakesOnBothCompletionWithException()
        {
            Exception e2 = new Exception();

            // Non-generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e2, twa.Exception?.InnerException);
            }

            // Generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e2, twa.Exception?.InnerException);
            }

            // Non-generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e2, twa.Exception?.InnerException);
            }

            // Generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e2, twa.Exception?.InnerException);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WakesOnBothCompletionWithExceptionForBoth()
        {
            Exception e1 = new Exception();
            Exception e2 = new Exception();

            // Non-generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                // Exceptions order is not guaranteed
                Assert.Contains(e1, twa.Exception?.InnerExceptions);
                Assert.Contains(e2, twa.Exception?.InnerExceptions);
            }

            // Generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                // Exceptions order is not guaranteed
                Assert.Contains(e1, twa.Exception?.InnerExceptions);
                Assert.Contains(e2, twa.Exception?.InnerExceptions);
            }

            // Non-generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                // Exceptions order is not guaranteed
                Assert.Contains(e1, twa.Exception?.InnerExceptions);
                Assert.Contains(e2, twa.Exception?.InnerExceptions);
            }

            // Generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetException(e2);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                // Exceptions order is not guaranteed
                Assert.Contains(e1, twa.Exception?.InnerExceptions);
                Assert.Contains(e2, twa.Exception?.InnerExceptions);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WakesOnBothCompletionWithCancellation()
        {
            CancellationToken ct2 = new CancellationToken(true);

            // Non-generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Non-generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.False(twa.IsCompleted);
                t1.SetResult(1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WakesOnBothCompletionWithCancellationForBoth()
        {
            CancellationToken ct1 = new CancellationToken(true);
            CancellationToken ct2 = new CancellationToken(true);

            // Non-generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetCanceled(ct1);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetCanceled(ct1);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Non-generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.False(twa.IsCompleted);
                t1.SetCanceled(ct1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.False(twa.IsCompleted);
                t1.SetCanceled(ct1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void Task_WhenAll_TwoTasks_WakesOnBothCompletionWithSameCancellationForBoth()
        {
            // Non-generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                var cts = new CancellationTokenSource();
                cts.Token.Register(() => t1.TrySetCanceled());
                cts.Token.Register(() => t2.TrySetCanceled());

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                cts.Cancel();
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                var cts = new CancellationTokenSource();
                cts.Token.Register(() => t1.TrySetCanceled());
                cts.Token.Register(() => t2.TrySetCanceled());

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                cts.Cancel();
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Non-generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                var cts = new CancellationTokenSource();
                cts.Token.Register(() => t2.TrySetCanceled());
                cts.Token.Register(() => t1.TrySetCanceled());

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                cts.Cancel();
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }

            // Generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                var cts = new CancellationTokenSource();
                cts.Token.Register(() => t2.TrySetCanceled());
                cts.Token.Register(() => t1.TrySetCanceled());

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                cts.Cancel();
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsCanceled);
            }
        }

        [Fact]
        public static void Task_WhenAll_TwoTasks_WakesOnBothCompletionWithExceptionAndCancellation()
        {
            Exception e1 = new Exception();
            CancellationToken ct2 = new CancellationToken(true);

            // Non-generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e1, twa.Exception?.InnerException);
            }

            // Generic, first completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e1, twa.Exception?.InnerException);
            }

            // Non-generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task twa = Task.WhenAll((Task)t1.Task, (Task)t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e1, twa.Exception?.InnerException);
            }

            // Generic, second completes first
            {
                var t1 = new TaskCompletionSource<int>();
                var t2 = new TaskCompletionSource<int>();

                Task<int[]> twa = Task.WhenAll(t1.Task, t2.Task);
                Assert.False(twa.IsCompleted);
                t2.SetCanceled(ct2);
                Assert.False(twa.IsCompleted);
                t1.SetException(e1);
                Assert.True(twa.IsCompleted);
                Assert.True(twa.IsFaulted);
                Assert.Equal(e1, twa.Exception?.InnerException);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Task_WhenEach_NullsTriggerExceptions()
        {
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => Task.WhenEach((Task[])null));
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => Task.WhenEach((Task<int>[])null));
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => Task.WhenEach((IEnumerable<Task>)null));
            AssertExtensions.Throws<ArgumentNullException>("tasks", () => Task.WhenEach((IEnumerable<Task<int>>)null));

            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WhenEach((Task[])[null]));
            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WhenEach((ReadOnlySpan<Task>)[null]));
            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WhenEach((IEnumerable<Task>)[null]));
            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WhenEach((Task<int>[])[null]));
            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WhenEach((ReadOnlySpan<Task<int>>)[null]));
            AssertExtensions.Throws<ArgumentException>("tasks", () => Task.WhenEach((IEnumerable<Task<int>>)[null]));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Task_WhenEach_EmptyInputsCompleteImmediately()
        {
            Assert.False(await Task.WhenEach((Task[])[]).GetAsyncEnumerator().MoveNextAsync());
            Assert.False(await Task.WhenEach((ReadOnlySpan<Task>)[]).GetAsyncEnumerator().MoveNextAsync());
            Assert.False(await Task.WhenEach((IEnumerable<Task>)[]).GetAsyncEnumerator().MoveNextAsync());
            Assert.False(await Task.WhenEach((Task<int>[])[]).GetAsyncEnumerator().MoveNextAsync());
            Assert.False(await Task.WhenEach((ReadOnlySpan<Task<int>>)[]).GetAsyncEnumerator().MoveNextAsync());
            Assert.False(await Task.WhenEach((IEnumerable<Task<int>>)[]).GetAsyncEnumerator().MoveNextAsync());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Task_WhenEach_TasksOnlyEnumerableOnce()
        {
            IAsyncEnumerable<Task>[] enumerables =
            [
                Task.WhenEach((Task[])[Task.CompletedTask, Task.CompletedTask]),
                Task.WhenEach((ReadOnlySpan<Task>)[Task.CompletedTask, Task.CompletedTask]),
                Task.WhenEach((IEnumerable<Task>)[Task.CompletedTask, Task.CompletedTask]),
                Task.WhenEach((Task<int>[])[Task.FromResult(0), Task.FromResult(0)]),
                Task.WhenEach((ReadOnlySpan<Task<int>>)[Task.FromResult(0), Task.FromResult(0)]),
                Task.WhenEach((IEnumerable<Task<int>>)[Task.FromResult(0), Task.FromResult(0)]),
            ];

            foreach (IAsyncEnumerable<Task> e in enumerables)
            {
                IAsyncEnumerator<Task> e1 = e.GetAsyncEnumerator();
                IAsyncEnumerator<Task> e2 = e.GetAsyncEnumerator();
                IAsyncEnumerator<Task> e3 = e.GetAsyncEnumerator();

                Assert.True(await e1.MoveNextAsync());
                Assert.False(await e2.MoveNextAsync());
                Assert.False(await e3.MoveNextAsync());

                int count = 0;
                do
                {
                    count++;
                }
                while (await e1.MoveNextAsync());
                Assert.Equal(2, count);

                Assert.False(await e.GetAsyncEnumerator().MoveNextAsync());
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task Task_WhenEach_IteratesThroughCompleteAndIncompleteTasks(int mode)
        {
            TaskCompletionSource<int> tcs1 = new(), tcs2 = new(), tcs3 = new();
            Task<int>[] array = [Task.FromResult(1), tcs1.Task, Task.FromResult(2), tcs2.Task, Task.FromResult(3), tcs3.Task];

            IAsyncEnumerable<Task> tasks = mode switch
            {
                0 => Task.WhenEach((ReadOnlySpan<Task>)array),
                1 => Task.WhenEach((Task[])array),
                2 => Task.WhenEach((IEnumerable<Task>)array),
                3 => Task.WhenEach((ReadOnlySpan<Task<int>>)array),
                4 => Task.WhenEach((Task<int>[])array),
                _ => Task.WhenEach((IEnumerable<Task<int>>)array),
            };

            Assert.NotNull(tasks);

            IAsyncEnumerator<Task> e = tasks.GetAsyncEnumerator();
            Assert.NotNull(tasks);

            ValueTask<bool> moveNext;

            for (int i = 1; i <= 3; i++)
            {
                moveNext = e.MoveNextAsync();
                Assert.True(moveNext.IsCompletedSuccessfully);
                Assert.True(moveNext.Result);
                Assert.Same(Task.FromResult(i), e.Current);
            }

            foreach (TaskCompletionSource<int> tcs in new[] { tcs2, tcs1, tcs3 })
            {
                moveNext = e.MoveNextAsync();
                Assert.False(moveNext.IsCompleted);
                tcs.SetResult(42);
                Assert.True(await moveNext);
                Assert.Same(tcs.Task, e.Current);
            }

            moveNext = e.MoveNextAsync();
            Assert.True(moveNext.IsCompletedSuccessfully);
            Assert.False(moveNext.Result);
        }
    }
}
