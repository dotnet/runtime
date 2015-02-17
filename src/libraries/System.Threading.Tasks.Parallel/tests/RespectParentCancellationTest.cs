// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// RespectParentCancellation.cs
//
//
// This file contains functional tests for Parallel loops with cancellation
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=--=-=-=-=-=-=-=-=-=-=-=-

using Xunit;
using CoreFXTestLibrary;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Test.PCP
{

    public sealed class RespectParentCancellationTest
    {
        private API _api;                                     // the API to be tested
        private const int Max_Cancellation_Delay = 2000;      // max delay time before a cancel request is issued

        public RespectParentCancellationTest(API api)
        {
            _api = api;
        }

        /// <summary>
        /// This test cancels a Parallel.* loop in flight and checks that we get a OperationCanceledException / TaskCanceledException
        /// </summary>
        /// <returns></returns>
        internal void RealRun()
        {
            ParallelLoopResult result = new ParallelLoopResult();
            CancellationTokenSource cts = new CancellationTokenSource();

            Task wrappedTask = Task.Factory.StartNew(delegate
            {
                CancellationToken ct = cts.Token;
                switch (_api)
                {
                    case API.For:
                        result = Parallel.For(0, Int32.MaxValue, new ParallelOptions() { CancellationToken = ct }, (i) => { });
                        break;

                    case API.For64:
                        result = Parallel.For(0, Int64.MaxValue, new ParallelOptions() { CancellationToken = ct }, (i) => { });
                        break;

                    case API.Foreach:
                        result = Parallel.ForEach<int>(GetIEnumerable(), new ParallelOptions() { CancellationToken = ct }, (i) => { });
                        break;
                }
            }, cts.Token, TaskCreationOptions.None, TaskScheduler.Default);

            var task = Task.Delay(Max_Cancellation_Delay);
            task.Wait();
            cts.Cancel();

            try
            {
                wrappedTask.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle((e) => { return e is OperationCanceledException || e is TaskCanceledException; });
            }

            // verify result : Stopped <==> if Completed is false and LowestBreakIteration == null
            Assert.False(result.IsCompleted, "Should not be completed.");
            Assert.Null(result.LowestBreakIteration);
        }

        private IEnumerable<int> GetIEnumerable()
        {
            while (true)
            {
                yield return 0;
            }
        }
       
    }

    public enum API
    {
        For,      // Parallel.For
        For64,    // Parallel.For64
        Foreach,  // Parallel.Foreach
    }

    public static class TestMethods
    {
        [Fact]
        public static void RespectParentCancellation1()
        {
            RespectParentCancellationTest test = new RespectParentCancellationTest(API.For);
            test.RealRun();
        }
        [Fact]
        public static void RespectParentCancellation2()
        {
            RespectParentCancellationTest test = new RespectParentCancellationTest(API.For64);
            test.RealRun();
        }
        [Fact]
        public static void RespectParentCancellation3()
        {
            RespectParentCancellationTest test = new RespectParentCancellationTest(API.Foreach);
            test.RealRun();
        }
    }
}
