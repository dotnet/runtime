// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    /// <summary>The class that contains the unit tests of the ThreadLocal.</summary>
    public static class ThreadLocalTests
    {
        /// <summary>Tests for the Ctor.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        [Fact]
        public static void RunThreadLocalTest1_Ctor()
        {
            ThreadLocal<object> testObject;
            testObject = new ThreadLocal<object>();
            testObject = new ThreadLocal<object>(true);
            testObject = new ThreadLocal<object>(() => new object());
            testObject = new ThreadLocal<object>(() => new object(), true);
        }

        [Fact]
        public static void RunThreadLocalTest1_Ctor_Negative()
        {
            try
            {
                new ThreadLocal<object>(null);
            }
            catch (ArgumentNullException)
            {
                // No other exception should be thrown.
                // With a previous issue, if the constructor threw an exception, the finalizer would throw an exception as well.
            }
        }

        /// <summary>Tests for the ToString.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        [Fact]
        public static void RunThreadLocalTest2_ToString()
        {
            ThreadLocal<object> tlocal = new ThreadLocal<object>(() => (object)1);
            Assert.Equal(1.ToString(), tlocal.ToString());
        }

        /// <summary>Tests for the Initialized property.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        [Fact]
        public static void RunThreadLocalTest3_IsValueCreated()
        {
            ThreadLocal<string> tlocal = new ThreadLocal<string>(() => "Test");
            Assert.False(tlocal.IsValueCreated);
            string temp = tlocal.Value;
            Assert.True(tlocal.IsValueCreated);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunThreadLocalTest4_Value()
        {
            ThreadLocal<string> tlocal = null;

            // different threads call Value
            int numOfThreads = 10;
            Task[] threads = new Task[numOfThreads];
            object alock = new object();
            List<string> seenValuesFromAllThreads = new List<string>();
            int counter = 0;
            tlocal = new ThreadLocal<string>(() => (++counter).ToString());
            //CancellationToken ct = new CancellationToken();
            for (int i = 0; i < threads.Length; ++i)
            {
                // We are creating the task using TaskCreationOptions.LongRunning because...
                // there is no guarantee that the Task will be created on another thread.
                // There is also no guarantee that using this TaskCreationOption will force
                // it to be run on another thread.
                threads[i] = new Task(() =>
                {
                    string value = tlocal.Value;
                    Debug.WriteLine("Val: " + value);
                    seenValuesFromAllThreads.Add(value);
                }, TaskCreationOptions.LongRunning);
                threads[i].Start(TaskScheduler.Default);
                threads[i].Wait();
            }
            Assert.Equal(Enumerable.Range(1, threads.Length).Select(x => x.ToString()), seenValuesFromAllThreads);
        }

        [Fact]
        public static void RunThreadLocalTest4_Value_NegativeCases()
        {
            ThreadLocal<string> tlocal = null;
            Assert.Throws<InvalidOperationException>(() =>
            {
                int x = 0;
                tlocal = new ThreadLocal<string>(delegate
                {
                    if (x++ < 5)
                        return tlocal.Value;
                    else
                        return "Test";
                });
                string str = tlocal.Value;
            });
        }

        [ConditionalFact(nameof(IsThreadingAndPreciseGcSupported))]
        public static void RunThreadLocalTest5_Dispose()
        {
            // test recycling the combination index;
            ThreadLocal<string> tl = new ThreadLocal<string>(() => null);
            Assert.False(tl.IsValueCreated);
            Assert.Null(tl.Value);

            // Test that a value is not kept alive by a departed thread
            var threadLocal = new ThreadLocal<SetMreOnFinalize>();
            var mres = new ManualResetEventSlim(false);

            // (Careful when editing this test: saving the created thread into a local variable would likely break the
            // test in Debug build.)
            // We are creating the task using TaskCreationOptions.LongRunning because...
            // there is no guarantee that the Task will be created on another thread.
            // There is also no guarantee that using this TaskCreationOption will force
            // it to be run on another thread.
            new Task(() => { threadLocal.Value = new SetMreOnFinalize(mres); }, TaskCreationOptions.LongRunning).Start(TaskScheduler.Default);

            ThreadTestHelpers.WaitForConditionWithCustomDelay(
                () => mres.IsSet,
                () =>
                {
                    Thread.Sleep(1);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                });
        }

        [Fact]
        public static void RunThreadLocalTest5_Dispose_Negative()
        {
            ThreadLocal<string> tl = new ThreadLocal<string>(() => "dispose test");
            string value = tl.Value;

            tl.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { string tmp = tl.Value; });
            // Failure Case: The Value property of the disposed ThreadLocal object should throw ODE

            Assert.Throws<ObjectDisposedException>(() => { bool tmp = tl.IsValueCreated; });
            // Failure Case: The IsValueCreated property of the disposed ThreadLocal object should throw ODE

            Assert.Throws<ObjectDisposedException>(() => { string tmp = tl.ToString(); });
            // Failure Case: The ToString method of the disposed ThreadLocal object should throw ODE

        }

        [Fact]
        public static void RunThreadLocalTest6_SlowPath()
        {
            // the maximum fast path instances for each type is 16 ^ 3 = 4096, when this number changes in the product code, it should be changed here as well
            int MaximumFastPathPerInstance = 4096;
            ThreadLocal<int>[] locals_int = new ThreadLocal<int>[MaximumFastPathPerInstance + 10];
            for (int i = 0; i < locals_int.Length; i++)
            {
                locals_int[i] = new ThreadLocal<int>(() => i);
                var val = locals_int[i].Value;
            }
            Assert.Equal(Enumerable.Range(0, locals_int.Length), locals_int.Select(x => x.Value));

            // The maximum slowpath for all Ts is MaximumFastPathPerInstance * 4;
            locals_int = new ThreadLocal<int>[4096];
            ThreadLocal<long>[] locals_long = new ThreadLocal<long>[4096];
            ThreadLocal<float>[] locals_float = new ThreadLocal<float>[4096];
            ThreadLocal<double>[] locals_double = new ThreadLocal<double>[4096];
            for (int i = 0; i < locals_int.Length; i++)
            {
                locals_int[i] = new ThreadLocal<int>(() => i);
                locals_long[i] = new ThreadLocal<long>(() => i);
                locals_float[i] = new ThreadLocal<float>(() => i);
                locals_double[i] = new ThreadLocal<double>(() => i);
            }

            ThreadLocal<string> local = new ThreadLocal<string>(() => "slow path");
            Assert.Equal("slow path", local.Value);
        }

        private class ThreadLocalWeakReferenceTest
        {
            private object _foo;
            private WeakReference _wFoo;
            [MethodImplAttribute(MethodImplOptions.NoInlining)]

            private void Method()
            {
                _foo = new object();
                _wFoo = new WeakReference(_foo);

                new ThreadLocal<object>() { Value = _foo }.Dispose();
            }

            public void Run()
            {
                Method();
                _foo = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // s_foo should have been garbage collected
                Assert.False(_wFoo.IsAlive);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public static void RunThreadLocalTest7_WeakReference()
        {
            var threadLocalWeakReferenceTest = new ThreadLocalWeakReferenceTest();
            threadLocalWeakReferenceTest.Run();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/43981", TestRuntimes.Mono)]
        public static void RunThreadLocalTest8_Values()
        {
            // Test adding values and updating values
            {
                var threadLocal = new ThreadLocal<int>(true);
                Assert.True(threadLocal.Values.Count == 0, "RunThreadLocalTest8_Values: Expected thread local to initially have 0 values");
                Assert.True(threadLocal.Value == 0, "RunThreadLocalTest8_Values: Expected initial value of 0");
                Assert.True(threadLocal.Values.Count == 1, "RunThreadLocalTest8_Values: Expected values count to now be 1 from initialized value");
                Assert.True(threadLocal.Values[0] == 0, "RunThreadLocalTest8_Values: Expected values to contain initialized value");

                threadLocal.Value = 1000;
                Assert.True(threadLocal.Values.Count == 1, "RunThreadLocalTest8_Values: Expected values count to still be 1 after updating existing value");
                Assert.True(threadLocal.Values[0] == 1000, "RunThreadLocalTest8_Values: Expected values to contain updated value");

                ((IAsyncResult)Task.Run(() => threadLocal.Value = 1001)).AsyncWaitHandle.WaitOne();
                Assert.True(threadLocal.Values.Count == 2, "RunThreadLocalTest8_Values: Expected values count to be 2 now that another thread stored a value");
                Assert.True(threadLocal.Values.Contains(1000) && threadLocal.Values.Contains(1001), "RunThreadLocalTest8_Values: Expected values to contain both thread's values");

                int numTasks = 1000;
                Task[] allTasks = new Task[numTasks];
                for (int i = 0; i < numTasks; i++)
                {
                    // We are creating the task using TaskCreationOptions.LongRunning because...
                    // there is no guarantee that the Task will be created on another thread.
                    // There is also no guarantee that using this TaskCreationOption will force
                    // it to be run on another thread.
                    var task = Task.Factory.StartNew(() => threadLocal.Value = i, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    task.Wait();
                }

                var values = threadLocal.Values;
                if (values.Count != 1002)
                {
                    string message =
                        "RunThreadLocalTest8_Values: Expected values to contain both previous values and 1000 new values. Actual count: " +
                        values.Count +
                        '.';
                    if (values.Count != 0)
                    {
                        message += " Missing items:";
                        for (int i = 0; i < 1002; i++)
                        {
                            if (!values.Contains(i))
                            {
                                message += " " + i;
                            }
                        }
                    }
                    Assert.Fail(message);
                }
                for (int i = 0; i < 1000; i++)
                {
                    Assert.True(values.Contains(i), "RunThreadLocalTest8_Values: Expected values to contain value for thread #: " + i);
                }

                threadLocal.Dispose();
            }

            // Test that thread values remain after threads depart
            {
                var tl = new ThreadLocal<string>(true);
                var t = Task.Run(() => tl.Value = "Parallel");
                t.Wait();
                t = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.True(tl.Values.Count == 1, "RunThreadLocalTest8_Values: Expected values count to be 1 from other thread's initialization");
                Assert.True(tl.Values.Contains("Parallel"), "RunThreadLocalTest8_Values: Expected values to contain 'Parallel'");
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void RunThreadLocalTest8Helper(ManualResetEventSlim mres)
        {
            var tl = new ThreadLocal<object>(true);
            var t = Task.Run(() => tl.Value = new SetMreOnFinalize(mres));
            t.Wait();
            t = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.True(tl.Values.Count == 1, "RunThreadLocalTest8_Values: Expected other thread to have set value");
            Assert.True(tl.Values[0] is SetMreOnFinalize, "RunThreadLocalTest8_Values: Expected other thread's value to be of the right type");
            tl.Dispose();

            object values;
            Assert.Throws<ObjectDisposedException>(() => values = tl.Values);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public static void RunThreadLocalTest8_Values_NegativeCases()
        {
            // Test that Dispose works and that objects are released on dispose
            {
                var mres = new ManualResetEventSlim();
                RunThreadLocalTest8Helper(mres);

                SpinWait.SpinUntil(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return mres.IsSet;
                }, ThreadTestHelpers.UnexpectedTimeoutMilliseconds);

                Assert.True(mres.IsSet, "RunThreadLocalTest8_Values: Expected thread local to release the object and for it to be finalized");
            }

            // Test that Values property throws an exception unless true was passed into the constructor
            {
                ThreadLocal<int> t = new ThreadLocal<int>();
                t.Value = 5;
                Exception exceptionCaught = null;
                try
                {
                    var randomValue = t.Values.Count;
                }
                catch (Exception ex)
                {
                    exceptionCaught = ex;
                }

                Assert.True(exceptionCaught != null, "RunThreadLocalTest8_Values: Expected Values to throw an InvalidOperationException. No exception was thrown.");
                Assert.True(
                   exceptionCaught != null && exceptionCaught is InvalidOperationException,
                   "RunThreadLocalTest8_Values: Expected Values to throw an InvalidOperationException. Wrong exception was thrown: " + exceptionCaught.GetType().ToString());
            }
        }

        [Fact]
        public static void RunThreadLocalTest9_Uninitialized()
        {
            for (int iter = 0; iter < 10; iter++)
            {
                ThreadLocal<int> t1 = new ThreadLocal<int>();
                t1.Value = 177;
                ThreadLocal<int> t2 = new ThreadLocal<int>();
                Assert.True(!t2.IsValueCreated, "RunThreadLocalTest9_Uninitialized: The ThreadLocal instance should have been uninitialized.");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void ValuesGetterDoesNotThrowUnexpectedExceptionWhenDisposed()
        {
            var startTest = new ManualResetEvent(false);
            var gotUnexpectedException = new ManualResetEvent(false);
            ThreadLocal<int> threadLocal = null;
            bool stop = false;

            Action waitForCreatorDisposer;
            Thread creatorDisposer = ThreadTestHelpers.CreateGuardedThread(out waitForCreatorDisposer, () =>
            {
                startTest.CheckedWait();
                do
                {
                    var tl = new ThreadLocal<int>(trackAllValues: true);
                    Volatile.Write(ref threadLocal, tl);
                    tl.Value = 1;
                    tl.Dispose();
                } while (!Volatile.Read(ref stop));
            });
            creatorDisposer.IsBackground = true;
            creatorDisposer.Start();

            int readerCount = Math.Max(1, Environment.ProcessorCount - 1);
            var waitsForReader = new Action[readerCount];
            for (int i = 0; i < readerCount; ++i)
            {
                Thread reader = ThreadTestHelpers.CreateGuardedThread(out waitsForReader[i], () =>
                {
                    startTest.CheckedWait();
                    do
                    {
                        var tl = Volatile.Read(ref threadLocal);
                        if (tl == null)
                        {
                            continue;
                        }

                        try
                        {
                            IList<int> values = tl.Values;
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                        catch
                        {
                            gotUnexpectedException.Set();
                            throw;
                        }
                    } while (!Volatile.Read(ref stop));
                });
                reader.IsBackground = true;
                reader.Start();
            }

            startTest.Set();
            bool failed = gotUnexpectedException.WaitOne(500);
            Volatile.Write(ref stop, true);
            foreach (Action waitForReader in waitsForReader)
            {
                waitForReader();
            }
            waitForCreatorDisposer();
            Assert.False(failed);
        }

        private enum UniqueEnumUsedOnlyWithNonInterferenceTest { True, False }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TestUnrelatedThreadLocalDoesNotInterfereWithTrackAllValues()
        {
            ThreadLocal<UniqueEnumUsedOnlyWithNonInterferenceTest> localThatDoesNotTrackValues = new ThreadLocal<UniqueEnumUsedOnlyWithNonInterferenceTest>(false);
            ThreadLocal<UniqueEnumUsedOnlyWithNonInterferenceTest> localThatDoesTrackValues = new ThreadLocal<UniqueEnumUsedOnlyWithNonInterferenceTest>(true);

            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(Work);
                t.Start();
                t.Join();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            int count = 0;
            foreach (var x in localThatDoesTrackValues.Values)
            {
                if (x == UniqueEnumUsedOnlyWithNonInterferenceTest.True)
                    count++;
            }

            Assert.Equal(10, count);
            void Work()
            {
                localThatDoesNotTrackValues.Value = UniqueEnumUsedOnlyWithNonInterferenceTest.True;
                localThatDoesTrackValues.Value = UniqueEnumUsedOnlyWithNonInterferenceTest.True;
            }
        }

        private class SetMreOnFinalize
        {
            private ManualResetEventSlim _mres;
            public SetMreOnFinalize(ManualResetEventSlim mres)
            {
                _mres = mres;
            }
            ~SetMreOnFinalize()
            {
                _mres.Set();
            }
        }

        public static bool IsThreadingAndPreciseGcSupported =>
            PlatformDetection.IsThreadingSupported && PlatformDetection.IsPreciseGcSupported;
    }
}
