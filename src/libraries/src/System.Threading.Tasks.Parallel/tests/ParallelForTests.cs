// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using CoreFXTestLibrary;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Test
{

    public class ParallelForTests
    {
        [Fact]
        public static void RunParallelExceptionTests()
        {
            // ParallelOptions tests
            ParallelOptions options = new ParallelOptions();
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { options.MaxDegreeOfParallelism = 0; });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { options.MaxDegreeOfParallelism = -2; });

            // 
            // Parallel.Invoke tests
            //
            Action[] smallActionArray = new Action[] { () => { } };
            Action[] largeActionArray = new Action[15];
            for (int i = 0; i < 15; i++) largeActionArray[i] = () => { };

            Assert.Throws<ArgumentNullException>(
               () => { Parallel.Invoke((Action[])null); } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.Invoke((ParallelOptions)null, () => { }); });
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.Invoke(options, (Action[])null); } );
            Assert.Throws<ArgumentException>(
               () => { Parallel.Invoke(options, (Action)null); } );
            CancellationTokenSource cts = new CancellationTokenSource();
            options.CancellationToken = cts.Token;
            cts.Cancel();
            EnsureOperationCanceledExceptionThrown(
               () => { Parallel.Invoke(options, smallActionArray); },
               options.CancellationToken,
               "RunParallelExceptionTests:  FAILED.  Expected CT on Parallel.Invoke(optionsWithPreCanceledToken, smallArray)");
            EnsureOperationCanceledExceptionThrown(
               () => { Parallel.Invoke(options, largeActionArray); },
               options.CancellationToken,
               "RunParallelExceptionTests:  FAILED.  Expected CT on Parallel.Invoke(optionsWithPreCanceledToken, largeArray)");

            // 
            // Parallel.For(32) tests
            //
            options = new ParallelOptions(); // Reset to get rid of CT

            // Test P.For(from, to, action<int>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0, 10, (Action<int>)null); } );

            // Test P.For(from, to, options, action<int>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0, 10, (ParallelOptions)null, _ => { }); } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0, 10, options, (Action<int>)null); } );

            // Test P.For(from, to, Action<int, ParallelLoopState>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0, 10, (Action<int, ParallelLoopState>)null); } );

            // Test P.For(from, to, options, Action<int, ParallelLoopState>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0, 10, options, (Action<int, ParallelLoopState>)null); });
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0, 10, (ParallelOptions)null, _ => { }); });

            // Test P.For<TLocal>(from, to, Func<TLocal>, Func<int, PLS, TLocal, TLocal>, Action<TLocal>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0, 10, (Func<string>)null, (a, b, c) => "", _ => { }); });
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0, 10, () => "", (Func<int, ParallelLoopState, string, string>)null, _ => { }); });
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0, 10, () => "", (a, b, c) => "", (Action<string>)null); });

            // Test P.For<TLocal>(from, to, options, Func<TLocal>, Func<int, PLS, TLocal, TLocal>, Action<TLocal>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0, 10, options, (Func<string>)null, (a, b, c) => "", _ => { }); });
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0, 10, options, () => "", (Func<int, ParallelLoopState, string, string>)null, _ => { }); });
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0, 10, options, () => "", (a, b, c) => "", (Action<string>)null); });
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0, 10, (ParallelOptions)null, () => "", (a, b, c) => "", _ => { }); });

            // 
            // Parallel.For(64) tests
            //

            // Test P.For(from, to, Action<long>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0L, 10L, (Action<long>)null);  } );

            // Test P.For(from, to, options, Action<long>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0L, 10L, (ParallelOptions)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0L, 10L, options, (Action<long>)null);  } );

            // Test P.For(from, to, Action<long, PLS>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0L, 10L, (Action<long, ParallelLoopState>)null);  } );

            // Test P.For(from, to, options, Action<long, PLS>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0L, 10L, options, (Action<long, ParallelLoopState>)null);  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For(0L, 10L, (ParallelOptions)null, _ => { });  } );

            // Test P.For<TLocal>(from, to, Func<TLocal>, Func<long, PLS, TLocal, TLocal>, Action<TLocal>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0L, 10L, (Func<string>)null, (a, b, c) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0L, 10L, () => "", (Func<long, ParallelLoopState, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0L, 10L, () => "", (a, b, c) => "", (Action<string>)null);  } );

            // Test P.For<TLocal>(from, to, options, Func<TLocal>, Func<long, PLS, TLocal, TLocal>, Action<TLocal>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0L, 10L, options, (Func<string>)null, (a, b, c) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0L, 10L, options, () => "", (Func<long, ParallelLoopState, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0L, 10L, options, () => "", (a, b, c) => "", (Action<string>)null);  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.For<string>(0L, 10L, (ParallelOptions)null, () => "", (a, b, c) => "", _ => { });  } );

            // Check that we properly handle pre-canceled requests
            options.CancellationToken = cts.Token;
            EnsureOperationCanceledExceptionThrown(
               () => { Parallel.For(0, 10, options, _ => { }); },
               options.CancellationToken,
               "RunParallelExceptionTests:  FAILED.  Expected OCE on pre-canceled P.For(32)");
            EnsureOperationCanceledExceptionThrown(
               () => { Parallel.For(0L, 10L, options, _ => { }); },
               options.CancellationToken,
               "RunParallelExceptionTests:  FAILED.  Expected OCE on pre-canceled P.For(64)");

            //
            // Parallel.ForEach(IEnumerable) tests
            //
            options.CancellationToken = CancellationToken.None; // reset

            // Test P.FE<T>(IE<T>, Action<T>)
            string[] sArray = new string[] { "one", "two", "three" };
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((IEnumerable<string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, (Action<string>)null);  } );

            // Test P.FE<T>(IE<T>, options, Action<T>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((IEnumerable<string>)null, options, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, (ParallelOptions)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, options, (Action<string>)null);  } );

            // Test P.FE<T>(IE<T>, Action<T,ParallelLoopState>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((IEnumerable<string>)null, (_, state) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, (Action<string, ParallelLoopState>)null);  } );

            // Test P.FE<T>(IE<T>, options, Action<T,ParallelLoopState>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((IEnumerable<string>)null, options, (_, state) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, (ParallelOptions)null, (_, state) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, options, (Action<string, ParallelLoopState>)null);  } );

            // Test P.FE<T>(IE<T>, Action<T,ParallelLoopState,idx>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((IEnumerable<string>)null, (_, state, idx) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, (Action<string, ParallelLoopState, long>)null);  } );

            // Test P.FE<T>(IE<T>, options, Action<T,ParallelLoopState,idx>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((IEnumerable<string>)null, options, (_, state, idx) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, (ParallelOptions)null, (_, state, idx) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(sArray, options, (Action<string, ParallelLoopState, long>)null);  } );

            //Test P.FE<T,L>(IE<T>, Func<L>, Func<T,PLS,L,L>, Action<L>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach((IEnumerable<string>)null, () => "", (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, (Func<string>)null, (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, () => "", (Func<string, ParallelLoopState, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, () => "", (_, state, local) => "", (Action<string>)null);  } );

            //Test P.FE<T,L>(IE<T>, options, Func<L>, Func<T,PLS,L,L>, Action<L>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach((IEnumerable<string>)null, options, () => "", (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, (ParallelOptions)null, () => "", (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, options, (Func<string>)null, (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, options, () => "", (Func<string, ParallelLoopState, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, options, () => "", (_, state, local) => "", (Action<string>)null);  } );

            //Test P.FE<T,L>(IE<T>, Func<L>, Func<T,PLS,long,L,L>, Action<L>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach((IEnumerable<string>)null, () => "", (_, state, idx, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, (Func<string>)null, (_, state, idx, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, () => "", (Func<string, ParallelLoopState, long, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, () => "", (_, state, idx, local) => "", (Action<string>)null);  } );

            //Test P.FE<T,L>(IE<T>, options, Func<L>, Func<T,PLS,idx,L,L>, Action<L>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach((IEnumerable<string>)null, options, () => "", (_, state, idx, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, (ParallelOptions)null, () => "", (_, state, idx, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, options, (Func<string>)null, (_, state, idx, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, options, () => "", (Func<string, ParallelLoopState, long, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(sArray, options, () => "", (_, state, idx, local) => "", (Action<string>)null);  } );

            //
            // Parallel.ForEach(Partitioner) tests
            //
            options.CancellationToken = CancellationToken.None; // reset
            var partitioner = Partitioner.Create(sArray);

            // Test P.FE<T>(Partitioner<T>, Action<T>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((Partitioner<string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(Partitioner.Create(sArray), (Action<string>)null);  } );

            // Test P.FE<T>(Partitioner<T>, options, Action<T>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((Partitioner<string>)null, options, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(Partitioner.Create(sArray), (ParallelOptions)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(Partitioner.Create(sArray), options, (Action<string>)null);  } );

            // Test P.FE<T>(Partitioner<T>, Action<T,ParallelLoopState>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((Partitioner<string>)null, (_, state) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(Partitioner.Create(sArray), (Action<string, ParallelLoopState>)null);  } );

            // Test P.FE<T>(Partitioner<T>, options, Action<T,ParallelLoopState>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>((Partitioner<string>)null, options, (_, state) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(Partitioner.Create(sArray), (ParallelOptions)null, (_, state) => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach<string>(Partitioner.Create(sArray), options, (Action<string, ParallelLoopState>)null);  } );

            //Test P.FE<T,L>(Partitioner<T>, Func<L>, Func<T,PLS,L,L>, Action<L>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach((Partitioner<string>)null, () => "", (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(Partitioner.Create(sArray), (Func<string>)null, (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(Partitioner.Create(sArray), () => "", (Func<string, ParallelLoopState, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(Partitioner.Create(sArray), () => "", (_, state, local) => "", (Action<string>)null);  } );

            //Test P.FE<T,L>(Partitioner<T>, options, Func<L>, Func<T,PLS,L,L>, Action<L>)
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach((Partitioner<string>)null, options, () => "", (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(Partitioner.Create(sArray), (ParallelOptions)null, () => "", (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(Partitioner.Create(sArray), options, (Func<string>)null, (_, state, local) => "", _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(Partitioner.Create(sArray), options, () => "", (Func<string, ParallelLoopState, string, string>)null, _ => { });  } );
            Assert.Throws<ArgumentNullException>(
               () => { Parallel.ForEach(Partitioner.Create(sArray), options, () => "", (_, state, local) => "", (Action<string>)null);  } );
        }

        // Cover converting P.ForEaches of arrays, lists to P.Fors
        [Fact]
        public static void TestParallelForEachConversions()
        {
            Logger.LogInformation("* TestParallelForEachConversions()");

            ParallelOptions options = new ParallelOptions();
            int[] intArray = new int[] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 0 };
            int targetSum = 0;
            foreach (int item in intArray) targetSum += item;

            // Test Parallel.ForEach(array) => Parallel.For
            int sum = 0;
            Parallel.ForEach(intArray, item => { Interlocked.Add(ref sum, item); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intArray, (item, state) => { Interlocked.Add(ref sum, item); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intArray, (item, state, index) => { Interlocked.Add(ref sum, item); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intArray, () => 0, (item, state, local) => { return local + item; }, subSum => { Interlocked.Add(ref sum, subSum); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intArray, () => 0, (item, state, index, local) => { return local + item; }, subSum => { Interlocked.Add(ref sum, subSum); });
            Assert.Equal(sum, targetSum);

            // Test Parallel.ForEach(list) => Parallel.For
            List<int> intList = new List<int>(intArray);

            sum = 0;
            Parallel.ForEach(intList, item => { Interlocked.Add(ref sum, item); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intList, (item, state) => { Interlocked.Add(ref sum, item); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intList, (item, state, index) => { Interlocked.Add(ref sum, item); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intList, () => 0, (item, state, local) => { return local + item; }, subSum => { Interlocked.Add(ref sum, subSum); });
            Assert.Equal(sum, targetSum);

            sum = 0;
            Parallel.ForEach(intList, () => 0, (item, state, index, local) => { return local + item; }, subSum => { Interlocked.Add(ref sum, subSum); });
            Assert.Equal(sum, targetSum);
        }

        [Fact]
        public static void RunSimpleParallelDoTest()
        {
            RunSimpleParallelDoTest(0);
            RunSimpleParallelDoTest(1);
            RunSimpleParallelDoTest(1024);
            RunSimpleParallelDoTest(1024 * 256);
        }

        [Fact]
        public static void RunSimpleParallelForIncrementTest()
        {
            RunSimpleParallelForIncrementTest(-1);
            RunSimpleParallelForIncrementTest(0);
            RunSimpleParallelForIncrementTest(1);
            RunSimpleParallelForIncrementTest(1024);
            RunSimpleParallelForIncrementTest(1024 * 1024);
            RunSimpleParallelForIncrementTest(1024 * 1024 * 16);
        }

        [Fact]
        public static void RunSimpleParallelFor64IncrementTest()
        {
            RunSimpleParallelFor64IncrementTest(-1);
            RunSimpleParallelFor64IncrementTest(0);
            RunSimpleParallelFor64IncrementTest(1);
            RunSimpleParallelFor64IncrementTest(1024);
            RunSimpleParallelFor64IncrementTest(1024 * 1024);
            RunSimpleParallelFor64IncrementTest(1024 * 1024 * 16);
        }

        [Fact]
        public static void RunSimpleParallelForAddTest()
        {
            RunSimpleParallelForAddTest(0);
            RunSimpleParallelForAddTest(1);
            RunSimpleParallelForAddTest(1024);
            RunSimpleParallelForAddTest(1024 * 1024);
        }

        [Fact]
        public static void RunSimpleParallelFor64AddTest()
        {
            RunSimpleParallelFor64AddTest(0);
            RunSimpleParallelFor64AddTest(1);
            RunSimpleParallelFor64AddTest(1024);
            RunSimpleParallelFor64AddTest(1024 * 1024);
        }

        [Fact]
        public static void SequentialForParityTest()
        {
            SequentialForParityTest(0, 100);
            SequentialForParityTest(-100, 100);
            SequentialForParityTest(-100, 100);
            SequentialForParityTest(-100, 100);
            SequentialForParityTest(Int32.MaxValue - 1, Int32.MaxValue);
            SequentialForParityTest(Int32.MaxValue - 100, Int32.MaxValue);
            SequentialForParityTest(Int32.MaxValue - 100, Int32.MaxValue);
            SequentialForParityTest(Int32.MaxValue - 100, Int32.MaxValue);
        }

        [Fact]
        public static void SequentialFor64ParityTest()
        {
            SequentialFor64ParityTest(0, 100);
            SequentialFor64ParityTest(-100, 100);
            SequentialFor64ParityTest(-100, 100);
            SequentialFor64ParityTest(-100, 100);
            SequentialFor64ParityTest((long)Int32.MaxValue - 100, (long)Int32.MaxValue + 100);
            SequentialFor64ParityTest((long)Int32.MaxValue - 100, (long)Int32.MaxValue + 100);
            SequentialFor64ParityTest((long)Int32.MaxValue - 100, (long)Int32.MaxValue + 100);
            SequentialFor64ParityTest(Int64.MaxValue - 1, Int64.MaxValue);
            // These fail for now.  Should be fixed when Huseyin implements new range-splitting logic.
            // SequentialFor64ParityTest(Int64.MaxValue - 100, Int64.MaxValue, 1);
            // SequentialFor64ParityTest(Int64.MaxValue - 100, Int64.MaxValue, 2);
            // SequentialFor64ParityTest(Int64.MaxValue - 100, Int64.MaxValue, 10);
        }

        [Fact]
        public static void RunSimpleParallelForeachAddTest_Enumerable()
        {
            RunSimpleParallelForeachAddTest_Enumerable(0);
            RunSimpleParallelForeachAddTest_Enumerable(1);
            RunSimpleParallelForeachAddTest_Enumerable(1024);
            RunSimpleParallelForeachAddTest_Enumerable(1024 * 1024);
            // This one just stopped working around 07/08/08 -- Can cause a horrible, indecipherable crash.
            // RunSimpleParallelForeachAddTest_Enumerable(1024 * 1024 * 16);
        }

        [Fact]
        public static void RunSimpleParallelForeachAddTest_List()
        {
            RunSimpleParallelForeachAddTest_List(0);
            RunSimpleParallelForeachAddTest_List(1);
            RunSimpleParallelForeachAddTest_List(1024);
            RunSimpleParallelForeachAddTest_List(1024 * 1024);
            RunSimpleParallelForeachAddTest_List(1024 * 1024 * 16);
        }

        [Fact]
        public static void RunSimpleParallelForeachAddTest_Array()
        {
            RunSimpleParallelForeachAddTest_Array(0);
            RunSimpleParallelForeachAddTest_Array(1);
            RunSimpleParallelForeachAddTest_Array(1024);
            RunSimpleParallelForeachAddTest_Array(1024 * 1024);
            RunSimpleParallelForeachAddTest_Array(1024 * 1024 * 16);
        }

        [Fact]
        public static void RunSimpleParallelForAverageAggregation()
        {
            RunSimpleParallelForAverageAggregation(1);
            RunSimpleParallelForAverageAggregation(1024);
            RunSimpleParallelForAverageAggregation(1024 * 1024);
            RunSimpleParallelForAverageAggregation(1024 * 1024 * 16);
        }

        [Fact]
        public static void RunSimpleParallelFor64AverageAggregation()
        {
            RunSimpleParallelFor64AverageAggregation(1);
            RunSimpleParallelFor64AverageAggregation(1024);
            RunSimpleParallelFor64AverageAggregation(1024 * 1024);
            RunSimpleParallelFor64AverageAggregation(1024 * 1024 * 16);
        }

        [Fact]
        public static void TestParallelForDOP()
        {
            int counter = 0;
            int maxDOP = 0;

            ParallelOptions parallelOptions = new ParallelOptions();
            int desiredDOP = 1;
            bool exceededDOP = false;

            Action<int> init = delegate (int DOP)
            {
                parallelOptions.MaxDegreeOfParallelism = DOP;
                counter = 0;
                maxDOP = 0;
                exceededDOP = false;
            };

            //
            // Test For32 loops
            //
            init(desiredDOP);
            Parallel.For(0, 100000, parallelOptions, delegate (int i)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            Assert.False(exceededDOP, String.Format("TestParallelForDOP:  FAILED!  For32-loop exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP));

            //
            // Test For64 loops
            //
            init(desiredDOP);
            Parallel.For(0L, 100000L, parallelOptions, delegate (long i)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            Assert.False(exceededDOP, String.Format("TestParallelForDOP:  FAILED!  For64-loop exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP));

            //
            // Test ForEach loops
            //
            Dictionary<int, int> dict = new Dictionary<int, int>();
            for (int i = 0; i < 100000; i++) dict.Add(i, i);

            init(desiredDOP);
            Parallel.ForEach(dict, parallelOptions, delegate (KeyValuePair<int, int> kvp)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            Assert.False(exceededDOP, String.Format("TestParallelForDOP:  FAILED!  ForEach-loop exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP));

            //
            // Test ForEach loops w/ Partitioner
            //
            List<int> baselist = new List<int>();
            for (int i = 0; i < 100000; i++) baselist.Add(i);
            MyPartitioner<int> mp = new MyPartitioner<int>(baselist);

            init(desiredDOP);
            Parallel.ForEach(mp, parallelOptions, delegate (int item)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            Assert.False(exceededDOP, String.Format("TestParallelForDOP:  FAILED!  ForEach-loop w/ Partitioner exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP));

            //
            // Test ForEach loops w/ OrderablePartitioner
            //
            OrderablePartitioner<int> mop = Partitioner.Create(baselist, true);

            init(desiredDOP);
            Parallel.ForEach(mop, parallelOptions, delegate (int item, ParallelLoopState state, long index)
            {
                int newval = Interlocked.Increment(ref counter);
                if (newval > desiredDOP)
                {
                    exceededDOP = true;
                    if (newval > maxDOP) maxDOP = newval;
                }
                Interlocked.Decrement(ref counter);
            });
            Assert.False(exceededDOP, String.Format("TestParallelForDOP:  FAILED!  ForEach-loop w/ OrderablePartitioner exceeded desired DOP ({0} > {1}).", maxDOP, desiredDOP));
        }

        [Fact]
        public static void TestParallelForPaths()
        {
            int loopsize = 1000;
            int expSum = 0;
            int intSum = 0;
            long longSum = 0;

            for (int i = 0; i < loopsize; i++) expSum += i;

            Action<int, string> intSumCheck = delegate (int observed, string label)
            {
                Assert.False(observed != expSum, String.Format("TestParallelForPaths:    > FAILED!  {0} gave wrong result", label));
            };

            Action<long, string> longSumCheck = delegate (long observed, string label)
            {
                Assert.False(observed != expSum, String.Format("TestParallelForPaths:    > FAILED!  {0} gave wrong result", label));
            };

            //
            // Test For w/ 32-bit indices
            //
            intSum = 0;
            Parallel.For(0, loopsize, delegate (int i)
            {
                Interlocked.Add(ref intSum, i);
            });
            intSumCheck(intSum, "For32(from, to, body(int))");

            intSum = 0;
            Parallel.For(0, loopsize, delegate (int i, ParallelLoopState state)
            {
                Interlocked.Add(ref intSum, i);
            });
            intSumCheck(intSum, "For32(from, to, body(int, state))");

            intSum = 0;
            Parallel.For(0, loopsize,
                delegate { return 0; },
                delegate (int i, ParallelLoopState state, int local)
                {
                    return local + i;
                },
                delegate (int local) { Interlocked.Add(ref intSum, local); });
            intSumCheck(intSum, "For32(from, to, localInit, body(int, state, local), localFinally)");


            //
            // Test For w/ 64-bit indices
            //
            longSum = 0;
            Parallel.For(0L, loopsize, delegate (long i)
            {
                Interlocked.Add(ref longSum, i);
            });
            longSumCheck(longSum, "For64(from, to, body(long))");

            longSum = 0;
            Parallel.For(0, loopsize, delegate (long i, ParallelLoopState state)
            {
                Interlocked.Add(ref longSum, i);
            });
            longSumCheck(longSum, "For64(from, to, body(long, state))");

            longSum = 0;
            Parallel.For(0, loopsize,
                delegate { return 0L; },
                delegate (long i, ParallelLoopState state, long local)
                {
                    return local + i;
                },
                delegate (long local) { Interlocked.Add(ref longSum, local); });
            longSumCheck(longSum, "For64(from, to, localInit, body(long, state, local), localFinally)");

            //
            // Test ForEach
            //
            Dictionary<long, long> dict = new Dictionary<long, long>(loopsize);
            for (int i = 0; i < loopsize; i++) dict[(long)i] = (long)i;

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>>(dict, delegate (KeyValuePair<long, long> kvp)
            {
                Interlocked.Add(ref longSum, kvp.Value);
            });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>>(dict, delegate (KeyValuePair<long, long> kvp, ParallelLoopState state)
            {
                Interlocked.Add(ref longSum, kvp.Value);
            });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>>(dict, delegate (KeyValuePair<long, long> kvp, ParallelLoopState state, long index)
            {
                Interlocked.Add(ref longSum, index);
            });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state, index))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>, long>(dict,
                delegate { return 0L; },
                delegate (KeyValuePair<long, long> kvp, ParallelLoopState state, long local)
                {
                    return local + kvp.Value;
                },
                delegate (long local) { Interlocked.Add(ref longSum, local); });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state, TLocal))");

            longSum = 0;
            Parallel.ForEach<KeyValuePair<long, long>, long>(dict,
                delegate { return 0L; },
                delegate (KeyValuePair<long, long> kvp, ParallelLoopState state, long index, long local)
                {
                    return local + index;
                },
                delegate (long local) { Interlocked.Add(ref longSum, local); });
            longSumCheck(longSum, "ForEach(enumerable, body(TSource, state, index, TLocal))");

            //
            // Test ForEach w/ Partitioner
            //
            List<int> baselist = new List<int>();
            for (int i = 0; i < loopsize; i++) baselist.Add(i);
            MyPartitioner<int> mp = new MyPartitioner<int>(baselist);
            OrderablePartitioner<int> mop = Partitioner.Create(baselist, true);

            intSum = 0;
            Parallel.ForEach(mp, delegate (int item) { Interlocked.Add(ref intSum, item); });
            intSumCheck(intSum, "ForEachP(enumerable, body(TSource))");

            intSum = 0;
            Parallel.ForEach(mp, delegate (int item, ParallelLoopState state) { Interlocked.Add(ref intSum, item); });
            intSumCheck(intSum, "ForEachP(enumerable, body(TSource, state))");

            intSum = 0;
            Parallel.ForEach(mop, delegate (int item, ParallelLoopState state, long index) { Interlocked.Add(ref intSum, item); });
            intSumCheck(intSum, "ForEachOP(enumerable, body(TSource, state, index))");

            intSum = 0;
            Parallel.ForEach(
                mp,
                delegate { return 0; },
                delegate (int item, ParallelLoopState state, int local) { return local + item; },
                delegate (int local) { Interlocked.Add(ref intSum, local); }
            );
            intSumCheck(intSum, "ForEachP(enumerable, localInit, body(TSource, state, local), localFinally)");

            intSum = 0;
            Parallel.ForEach(
                mop,
                delegate { return 0; },
                delegate (int item, ParallelLoopState state, long index, int local) { return local + item; },
                delegate (int local) { Interlocked.Add(ref intSum, local); }
            );
            intSumCheck(intSum, "ForEachOP(enumerable, localInit, body(TSource, state, index, local), localFinally)");
        }

        [Fact]
        public static void TestParallelForPaths_Exceptions()
        {
            int loopsize = 1000;
            List<int> baselist = new List<int>();
            for (int i = 0; i < loopsize; i++)
                baselist.Add(i);

            // And check that the use of OrderablePartitioner w/o dynamic support is rejected
            var mop = Partitioner.Create(baselist, false);
            try
            {
                Parallel.ForEach(mop, delegate (int item, ParallelLoopState state, long index) { });
                Assert.False(true, "TestParallelForPaths:    > FAILED.  Expected use of OrderablePartitioner w/o dynamic support to throw.");
            }
            catch { }
        }

        [Fact]
        public static void TestParallelScheduler()
        {
            ParallelOptions parallelOptions = new ParallelOptions();

            TaskScheduler myTaskScheduler = new ParallelTestsScheduler();

            Task t1 = Task.Factory.StartNew(delegate ()
            {
                TaskScheduler usedScheduler = null;

                do
                {
                    //
                    // Parallel.For() testing.
                    // Not, for now, testing all flavors (For(int), For(long), ForEach(), Partioner ForEach()).
                    // Assuming that all use ParallelOptions in the same fashion.
                    //

                    // Make sure that TaskScheduler is used by default (no ParallelOptions)
                    Parallel.For(0, 1, delegate (int i)
                    {
                        usedScheduler = TaskScheduler.Current;
                    });
                    Assert.True(usedScheduler == TaskScheduler.Default, "TestParallelScheduler:    > FAILED.  PFor: TaskScheduler.Default not used when no ParallelOptions are specified.");

                    // Make sure that TaskScheduler is used by default (with ParallelOptions)
                    Parallel.For(0, 1, parallelOptions, delegate (int i)
                    {
                        usedScheduler = TaskScheduler.Current;
                    });
                    Assert.True(usedScheduler == TaskScheduler.Default, "TestParallelScheduler:    > FAILED.  PFor: TaskScheduler.Default not used when none specified in ParallelOptions.");

                    // Make sure that specified scheduler is actually used
                    parallelOptions.TaskScheduler = myTaskScheduler;
                    Parallel.For(0, 1, parallelOptions, delegate (int i)
                    {
                        usedScheduler = TaskScheduler.Current;
                    });
                    Assert.True(usedScheduler == myTaskScheduler, "TestParallelScheduler:    > FAILED.  PFor: Failed to run with specified scheduler.");

                    // Make sure that current scheduler is used when null is specified
                    parallelOptions.TaskScheduler = null;
                    Parallel.For(0, 1, parallelOptions, delegate (int i)
                    {
                        usedScheduler = TaskScheduler.Current;
                    });
                    Assert.True(usedScheduler == myTaskScheduler, "TestParallelScheduler:    > FAILED.  PFor: Failed to run with TS.Current when null was specified.");

                    //
                    // Parallel.Invoke testing.
                    //
                    parallelOptions = new ParallelOptions();

                    // Make sure that TaskScheduler is used by default (w/o ParallelOptions)
                    Parallel.Invoke(
                        delegate { usedScheduler = TaskScheduler.Current; }
                    );
                    Assert.True(usedScheduler == TaskScheduler.Default, "TestParallelScheduler:    > FAILED.  PInvoke: TaskScheduler.Default not used when no ParallelOptions are specified.");

                    // Make sure that TaskScheduler is used by default (with ParallelOptions)
                    Parallel.Invoke(
                        parallelOptions,
                        delegate { usedScheduler = TaskScheduler.Current; }
                    );
                    Assert.True(usedScheduler == TaskScheduler.Default, "TestParallelScheduler:    > FAILED.  PInvoke: TaskScheduler.Default not used when none specified in ParallelOptions.");

                    // Make sure that specified scheduler is actually used
                    parallelOptions.TaskScheduler = myTaskScheduler;
                    Parallel.Invoke(
                        parallelOptions,
                        delegate { usedScheduler = TaskScheduler.Current; }
                    );
                    Assert.True(usedScheduler == myTaskScheduler, "TestParallelScheduler:    > FAILED.  PInvoke: Failed to run with specified scheduler.");

                    // Make sure that current scheduler is used when null is specified
                    parallelOptions.TaskScheduler = null;
                    Parallel.Invoke(
                        parallelOptions,
                        delegate { usedScheduler = TaskScheduler.Current; }
                    );
                    Assert.True(usedScheduler == myTaskScheduler, "TestParallelScheduler:    > FAILED.  PInvoke: Failed to run with TS.Current when null was specified.");
                    
                    // Some tests for wonky behavior seen before fixes
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    bool timeExpired = false;
                    Task continuation = tcs.Task.ContinueWith(delegate
                    {
                        Assert.True(timeExpired, "TestParallelScheduler:    > FAILED.  WaitAll() started/inlined a continuation task!");
                    });

                    // Arrange for another task to complete the tcs.
                    Task delayedOperation = Task.Factory.StartNew(delegate
                    {
                        timeExpired = true;
                        tcs.SetResult(null);
                    });

                    Task.WaitAll(tcs.Task, continuation);
                    Assert.True(timeExpired, 
                            String.Format("TestParallelScheduler:    > FAILED.  WaitAll() completed for unstarted continuation task or TCS.task! -- continuation status: {0}", continuation.Status));
                } while (false);
            }, CancellationToken.None, TaskCreationOptions.None, myTaskScheduler);

            t1.Wait();
        }

        [Fact]
        public static void TestInvokeDOPAndCancel()
        {
            ParallelOptions parallelOptions = null;
            //
            // Test DOP functionality
            //
            int desiredDOP = 1;
            bool exceededDOP = false;
            parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = desiredDOP;
            int counter = 0;
            Action a1 = delegate
            {
                if (Interlocked.Increment(ref counter) > desiredDOP)
                    exceededDOP = true;
                //Thread.Sleep(10); // make sure we have some time in this state
                Interlocked.Decrement(ref counter);
            };

            int numActions = 100;
            Action[] actions = new Action[numActions];
            for (int i = 0; i < numActions; i++) actions[i] = a1;
            exceededDOP = false;

            Parallel.Invoke(parallelOptions, actions);
            Assert.False( exceededDOP, "TestInvokeDOPAndCancel:    > FAILED!  DOP not respected.");


            //
            // Test cancellation
            //
            CancellationTokenSource cts = new CancellationTokenSource();
            parallelOptions.MaxDegreeOfParallelism = 2;
            parallelOptions.CancellationToken = cts.Token;
            counter = 0;
            Action a2 = delegate
            {
                // Return value isn't guaranteed to be lock'ed... so store it off to prevent other thread 
                // from incrementing it while we check the value.  Otherwise it's entirely possible to skip this entirely
                // since with 2 DOP it's entirely possible for the returned value to be 1 or 2.
                // the expected value leaving should always be 2 with 2 DOP though.
                // May make more sense just to lock(counter) for this whole delegate but I'm trying to maintain the spirit of the test
                // in case there is some intrinsic value in using Interlocked.Increment() here.
                int incrementedValue = Interlocked.Increment(ref counter);
                if (incrementedValue >= 1) cts.Cancel();
            };
            for (int i = 0; i < numActions; i++) actions[i] = a2;

            Assert.Throws<OperationCanceledException>(() =>
            {
                Parallel.Invoke(parallelOptions, actions);
            });

            Logger.LogInformation("Saw counter get incremented to " + counter + " with 2 degrees of parallelism");
            
            Assert.False((counter == numActions) || (counter > 2), 
                    String.Format("TestInvokeDOPAndCancel:    > FAILED!  Cancellation was not correctly effected.  Saw {0} calls to the Action delegate", counter));


            //
            // Make sure that cancellation + "regular" exception results in AggregateException
            //
            cts = new CancellationTokenSource();
            parallelOptions.CancellationToken = cts.Token;
            counter = 0;
            Action a3 = delegate
            {
                int newVal = Interlocked.Increment(ref counter);
                if (newVal == 1) throw new Exception("some non-cancellation-related exception");
                if (newVal == 2) cts.Cancel();
            };
            for (int i = 0; i < numActions; i++) actions[i] = a3;

            Assert.Throws<AggregateException>(() =>
            {
                Parallel.Invoke(parallelOptions, actions);
            });

            Assert.False(counter == numActions, "TestInvokeDOPAndCancel:    > FAILED!  Cancellation+exception not effected.");

            // Test that exceptions do not prevent other actions from running
            counter = 0;
            Action a4 = delegate
            {
                int newVal = Interlocked.Increment(ref counter);
                if (newVal == 1) throw new Exception("a singleton exception");
            };
            for (int i = 0; i < numActions; i++) actions[i] = a4;
            
            Assert.Throws<AggregateException>(()=>
            {
                Parallel.Invoke(actions);
            });
            
            Assert.True(counter == numActions, 
                    String.Format("TestInvokeDOPAndCancel:    > FAILED!  exception prevented actions from executing ({0}/{1} executed).", counter, numActions));
            
            // Test that simple example doesn't deadlock
            ManualResetEvent mres = new ManualResetEvent(false);
            Logger.LogInformation("TestInvokeDOPAndCancel:    About to call a potentially deadlocking Parallel.Invoke()...");
            Parallel.Invoke(
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { },
                () => { mres.WaitOne(); },
                () => { mres.Set(); }
            );
            Logger.LogInformation("TestInvokeDOPAndCancel:    (Done.)");

        }

        // Just runs a simple parallel invoke block that increments a counter.
        private static void RunSimpleParallelDoTest(int increms)
        {
            Logger.LogInformation("* RunSimpleParallelDoTest(increms={0})", increms);

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            var t = Task.Run(
                delegate
                {
                    Action[] actions = new Action[increms];
                    Action a = () => Interlocked.Increment(ref counter);
                    for (int i = 0; i < actions.Length; i++) actions[i] = a;

                    Parallel.Invoke(actions);
                });
            t.Wait();

            Assert.True(counter == increms, String.Format("  > failed: counter = {0}, expected {1}", counter, increms));
        }

        // Just increments a shared counter in a loop.
        private static void RunSimpleParallelForIncrementTest(int increms)
        {
            Logger.LogInformation("* RunSimpleParallelForIncrementTest(increms={0})", increms);

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.For(0, increms, (x) => Interlocked.Increment(ref counter));
                });
            t.Wait();

            var expected = increms > 0 ? increms : 0;

            Assert.True(counter == expected, String.Format("  > failed: counter = {0}, expected {1}", counter, increms));

            counter = 0;
            t = Task.Run(
                delegate
                {
                    Parallel.For(0, increms, (x) => Interlocked.Increment(ref counter));
                }
            );
            t.Wait();

            var expectedValue = increms > 0 ? increms : 0;

            Assert.True(counter == expectedValue, String.Format("  > failed2: counter = {0}, expected {1}", counter, increms));
        }

        // ... and a 64-bit version.
        private static void RunSimpleParallelFor64IncrementTest(long increms)
        {
            Logger.LogInformation("* RunSimpleParallelFor64IncrementTest(increms={0})", increms);

            long counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.For(0L, increms, (x) => Interlocked.Increment(ref counter));
                });
            t.Wait();

            var expected = increms > 0 ? increms : 0;

            Assert.True(counter == expected, String.Format("  > failed: counter = {0}, expected {1}", counter, increms));
        }

        // Just adds the indices of a loop (with a stride) in a parallel for loop.
        private static void RunSimpleParallelForAddTest(int count)
        {
            Logger.LogInformation("* RunSimpleParallelForAddTest(count={0})", count);

            int expectCounter = 0;
            for (int i = 0; i < count; i++)
            {
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.For(0, count, (x) => Interlocked.Add(ref counter, x));
                });
            t.Wait();

            Assert.True(counter == expectCounter, String.Format("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter));
        }

        // ... and a 64-bit version
        private static void RunSimpleParallelFor64AddTest(long count)
        {
            Logger.LogInformation("* RunSimpleParallelFor64AddTest(count={0})", count);

            long expectCounter = 0;
            for (long i = 0; i < count; i++)
            {
                expectCounter += i;
            }

            long counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.For(0L, count, (x) => Interlocked.Add(ref counter, x));
                });
            t.Wait();

            Assert.True(counter == expectCounter, String.Format("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter));
        }

        // tests whether parallel.for is on par with sequential loops. This is mostly interesting for testing the boundaries
        private static void SequentialForParityTest(int nInclusiveFrom, int nExclusiveTo)
        {
            Logger.LogInformation("* Parallel.For and sequential for equivalancy test /w ({0},{1})", nInclusiveFrom, nExclusiveTo);

            List<int> seqForIndices = new List<int>();
            List<int> parForIndices = new List<int>();

            for (int i = nInclusiveFrom; i < nExclusiveTo; i++)
            {
                seqForIndices.Add(i);
            }


            Parallel.For(nInclusiveFrom, nExclusiveTo, i =>
            {
                lock (parForIndices)
                {
                    parForIndices.Add(i);
                }
            });

            parForIndices.Sort();

            Assert.True(seqForIndices.Count == parForIndices.Count,"  > failed: Different iteration counts in parallel and sequential for loops.");

            for (int i = 0; i < seqForIndices.Count; i++)
            {
                Assert.True(seqForIndices[i] == parForIndices[i],
                        String.Format("  > failed: Iteration #{0} hit different values in sequential and parallel loops ({1},{2})", i, seqForIndices[i], parForIndices[i]));
            }
        }

        // ... and a 64-bit version
        private static void SequentialFor64ParityTest(long nInclusiveFrom, long nExclusiveTo)
        {
            Logger.LogInformation("* Parallel.For64 and sequential for equivalancy test /w ({0},{1})", nInclusiveFrom, nExclusiveTo);

            List<long> seqForIndices = new List<long>();
            List<long> parForIndices = new List<long>();

            for (long i = nInclusiveFrom; i < nExclusiveTo; i++)
            {
                seqForIndices.Add(i);
            }


            Parallel.For(nInclusiveFrom, nExclusiveTo, i =>
            {
                lock (parForIndices)
                {
                    parForIndices.Add(i);
                }
            });

            parForIndices.Sort();

            Assert.True(seqForIndices.Count == parForIndices.Count, "  > failed: Different iteration counts in parallel and sequential for loops.");

            for (int i = 0; i < seqForIndices.Count; i++)
            {
                Assert.True(seqForIndices[i] == parForIndices[i],
                        String.Format("  > failed: Iteration #{0} hit different values in sequential and parallel loops ({1},{2})", i, seqForIndices[i], parForIndices[i]));
            }
        }

        private static void RunSimpleParallelForeachAddTest_Enumerable(int count)
        {
            Logger.LogInformation("* RunSimpleParallelForeachAddTest_Enumerable(count={0})", count);

            int[] data = new int[count];
            int expectCounter = 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.ForEach(
                        new SimpleParallelForeachAddTest_Enumerable<int>(data), (x) => Interlocked.Add(ref counter, x));
                });
            t.Wait();

            Assert.True(counter == expectCounter, String.Format("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter));
        }

        // Just adds the contents of an auto-generated list inside a foreach loop. Hits the IList code-path.
        private static void RunSimpleParallelForeachAddTest_List(int count)
        {
            Logger.LogInformation("* RunSimpleParallelForeachAddTest_List(count={0})", count);

            List<int> data = new List<int>(count);
            int expectCounter = 0;
            for (int i = 0; i < data.Count; i++)
            {
                data.Add(i);
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.ForEach(data, (x) => Interlocked.Add(ref counter, x));
                });
            t.Wait();

            Assert.True(counter == expectCounter, String.Format("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter));
        }

        // Just adds the contents of an auto-generated list inside a foreach loop. Hits the array code-path.
        private static void RunSimpleParallelForeachAddTest_Array(int count)
        {
            Logger.LogInformation("* RunSimpleParallelForeachAddTest_Array(count={0})", count);

            int[] data = new int[count];
            int expectCounter = 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
                expectCounter += i;
            }

            int counter = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.ForEach(data, (x) => Interlocked.Add(ref counter, x));
                });
            t.Wait();

            Assert.True(counter == expectCounter, String.Format("  > failed: counter = {0}, expectCounter = {1}", counter, expectCounter));
        }

        // Does an average aggregation using for.
        private static void RunSimpleParallelForAverageAggregation(int count)
        {
            Logger.LogInformation("* RunSimpleParallelForAverageAggregation(count={0})", count);

            int sum = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.For(
                        0,
                        count,
                        () => 0,
                        (i, state, local) => local += i,
                        (local) => Interlocked.Add(ref sum, local)
                    );
                });
            t.Wait();

            // check that the average is correct.  (if the totals are correct, the avgs will also be correct.)
            int expectTotal = 0;
            for (int i = 0; i < count; i++)
                expectTotal += i;
            Assert.True(sum == expectTotal, String.Format("  > total was not accurate: got {0}, expected {1}", sum, expectTotal));
        }

        // ... and a 64-bit version
        private static void RunSimpleParallelFor64AverageAggregation(long count)
        {
            Logger.LogInformation("* RunSimpleParallelFor64AverageAggregation(count={0})", count);

            long sum = 0;

            // run inside of a separate task mgr to isolate impacts to other tests.
            Task t = Task.Run(
                delegate
                {
                    Parallel.For<long>(
                        0L,
                        count,
                        delegate () { return 0L; },
                        delegate (long i, ParallelLoopState state, long local) { return local + i; },
                        delegate (long local) { Interlocked.Add(ref sum, local); }
                    );
                });
            t.Wait();

            // check that the average is correct.  (if the totals are correct, the avgs will also be correct.)
            long expectTotal = 0;
            for (long i = 0; i < count; i++)
                expectTotal += i;
            Assert.True(sum == expectTotal, String.Format("  > total was not accurate: got {0}, expected {1}", sum, expectTotal));
        }

        [Fact]
        public static void RunParallelLoopCancellationTests()
        {
            int counter = 0; // Counts the actual number of iterations
            int iterations = 0; // Target number of iterations
            CancellationTokenSource cts = null;
            ParallelOptions parallelOptions = null;

            // Action to take when initializing a test
            Action init = delegate
            {
                counter = 0;
                cts = new CancellationTokenSource();
                parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = cts.Token;

                // This is the only way to protect
                // against false failures.  The false failures
                // occur when the root For/ForEach task is "held up" for
                // a significant amount of time, and thus fails to
                // see/act on the cancellation.  By setting DOP = 1,
                // no other tasks do work while the root task is held up.
                parallelOptions.MaxDegreeOfParallelism = 1;
            };

            // Common logic for running a test
            Action<Action> runtest = delegate (Action body)
            {
                Assert.Throws<OperationCanceledException>(() =>
                {
                    body();
                });

                Assert.False(counter == iterations, "RunParallelLoopCancellationTests:      > FAILED! Loop does not appear to have been canceled.");
            };

            iterations = 100000000; // Lots of iterations when we want to verify that something got canceled.

            init();
            runtest(delegate
            {
                Parallel.For(0, iterations, parallelOptions, delegate (int i)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });

            init();
            runtest(delegate
            {
                Parallel.For(0L, iterations, parallelOptions, delegate (long i)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });

            // Something smaller for the ForEach() test, because we need to construct an equivalent-sized
            // data structure.
            iterations = 10000;

            Dictionary<int, int> dict = new Dictionary<int, int>(iterations);
            for (int i = 0; i < iterations; i++) dict[i] = i;
            init();
            runtest(delegate
            {
                Parallel.ForEach(dict, parallelOptions, delegate (KeyValuePair<int, int> kvp)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });

            List<int> baselist = new List<int>();
            for (int i = 0; i < iterations; i++) baselist.Add(i);
            MyPartitioner<int> mp = new MyPartitioner<int>(baselist);
            init();
            runtest(delegate
            {
                Parallel.ForEach(mp, parallelOptions, delegate (int i)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });

            OrderablePartitioner<int> mop = Partitioner.Create(baselist, true);
            init();
            runtest(delegate
            {
                Parallel.ForEach(mop, parallelOptions, delegate (int i, ParallelLoopState state, long index)
                {
                    int myOrder = Interlocked.Increment(ref counter) - 1;
                    if (myOrder == 10) cts.Cancel();
                });
            });
        }

        /// <summary>
        /// Test to ensure that the task ID can be accessed from inside the task 
        /// </summary>
        [Fact]
        public static void TaskIDFromExternalContextTest()
        {
            int? withinTaskId = int.MinValue;
            Task t1 = Task.Run(() => withinTaskId = Task.CurrentId);

            Parallel.For(0, 10, (i) =>
            {
                Assert.False(Task.CurrentId == null || Task.CurrentId < 0, "Task within Parallel.For must have non-negative Id");
            });

            t1.Wait();

            // Verification
            Assert.False(withinTaskId == null || withinTaskId < 0, "Task.CurrentId called from within a Task must be non-negative");
        }

        #region Helper Classes and Methods

        // Just adds the contents of an auto-generated list inside a foreach loop.
        // Hits the IEnumerator code-path, since IList just forwards to Parallel.For internally.
        private class SimpleParallelForeachAddTest_Enumerable<T> : IEnumerable<T>
        {
            private IEnumerable<T> _source;
            internal SimpleParallelForeachAddTest_Enumerable(IEnumerable<T> source)
            {
                _source = source;
            }
            public IEnumerator<T> GetEnumerator()
            {
                return _source.GetEnumerator();
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<T>)this).GetEnumerator();
            }
        }

        // Used for parallel scheduler tests
        public class ParallelTestsScheduler : TaskScheduler
        {
            [SecurityCritical]
            protected override void QueueTask(Task task)
            {
                Task.Run(() => TryExecuteTaskWrapper(task));
            }

            [SecuritySafeCritical]
            private void TryExecuteTaskWrapper(Task task)
            {
                TryExecuteTask(task);
            }

            [SecurityCritical]
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }

            [SecurityCritical]
            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return null;
            }
        }

        //
        // Utility class for use w/ Partitioner-style ForEach testing.
        // Created by Cindy Song.
        //
        public class MyPartitioner<TSource> : Partitioner<TSource>
        {
            private IList<TSource> _data;

            public MyPartitioner(IList<TSource> data)
            {
                _data = data;
            }

            override public IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }
                IEnumerator<TSource>[] partitions
                    = new IEnumerator<TSource>[partitionCount];
                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = Partitioner.Create(_data, true).GetOrderableDynamicPartitions();
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = DropIndices(partitionEnumerable.GetEnumerator());
                }
                return partitions;
            }

            override public IEnumerable<TSource> GetDynamicPartitions()
            {
                return DropIndices(Partitioner.Create(_data, true).GetOrderableDynamicPartitions());
            }

            private static IEnumerable<TSource> DropIndices(IEnumerable<KeyValuePair<long, TSource>> source)
            {
                foreach (KeyValuePair<long, TSource> pair in source)
                {
                    yield return pair.Value;
                }
            }

            private static IEnumerator<TSource> DropIndices(IEnumerator<KeyValuePair<long, TSource>> source)
            {
                while (source.MoveNext())
                {
                    yield return source.Current.Value;
                }
            }

            public override bool SupportsDynamicPartitions
            {
                get { return true; }
            }
        }

        private static void EnsureOperationCanceledExceptionThrown(Action action, CancellationToken token, string message)
        {
            OperationCanceledException operationCanceledEx =
                Assert.Throws<OperationCanceledException>(action);

            Assert.True(operationCanceledEx.CancellationToken == token, String.Format("BarrierCancellationTests: Failed.  " + message));
        }

        #endregion
    }
}
