// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class BlockingCollectionCancellationTests
    {
        [Fact]
        public static void InternalCancellation_CompleteAdding_Negative()
        {
            BlockingCollection<int> coll1 = new BlockingCollection<int>();

            Task.Run(() => coll1.CompleteAdding());
            //call Take.. it should wake up with an OCE. when CompleteAdding() is called.
            Assert.Throws<InvalidOperationException>(() => coll1.Take());
            // "InternalCancellation_WakingUpTake:  an IOE should be thrown if CompleteAdding occurs during blocking Take()");
            Assert.Throws<InvalidOperationException>(() => coll1.Add(1));
            // "InternalCancellation_WakingUpAdd:  an InvalidOpEx should be thrown if CompleteAdding occurs during blocking Add()");
            Assert.Throws<InvalidOperationException>(() => coll1.TryAdd(1, 1000000));  //an indefinite wait to add.. 1000 seconds.
            // "InternalCancellation_WakingUpTryAdd:  an InvalidOpEx should be thrown if CompleteAdding occurs during blocking Add()");
        }

        //This tests that Take/TryTake wake up correctly if CompleteAdding() is called while the taker is waiting.
        [Fact]
        public static void InternalCancellation_WakingUpTryTake()
        {
            BlockingCollection<int> coll1 = new BlockingCollection<int>();

            Task.Run(
                () =>
                {
                    coll1.CompleteAdding();
                });

            int item;
            bool tookItem = coll1.TryTake(out item, 1000000); // wait essentially indefinitely. 1000seconds.
            Assert.False(tookItem,
               "InternalCancellation_WakingUpTryTake:  TryTake should wake up with tookItem=false.");
        }

        //This tests that Take/TryTake wake up correctly if CompleteAdding() is called while the taker is waiting.
        [Fact]
        public static void InternalCancellation_WakingUpAdd()
        {
            BlockingCollection<int> coll1 = new BlockingCollection<int>(1);
            coll1.Add(1); //fills the collection.

            Task.Run(
                () =>
                {
                    coll1.CompleteAdding();
                });

            Assert.False(coll1.IsAddingCompleted, "InternalCancellation_WakingUpAdd:  (1) At this point CompleteAdding should not have occurred.");
        }

        //This tests that TryAdd wake up correctly if CompleteAdding() is called while the taker is waiting.
        [Fact]
        public static void InternalCancellation_WakingUpTryAdd()
        {
            BlockingCollection<int> coll1 = new BlockingCollection<int>(1);
            coll1.Add(1); //fills the collection.

            Task.Run(
                () =>
                {
                    coll1.CompleteAdding();
                });

            Assert.False(coll1.IsAddingCompleted,
               "InternalCancellation_WakingUpTryAdd:  At this point CompleteAdding should not have occurred.");
        }

        [Fact]
        public static void ExternalCancel_Negative()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>(); //empty collection.

            CancellationTokenSource cs = new CancellationTokenSource();
            Task.Run(() => cs.Cancel());

            int item;
            EnsureOperationCanceledExceptionThrown(
                () => bc.Take(cs.Token), cs.Token,
                "ExternalCancel_Take:  The operation should wake up via token cancellation.");
            EnsureOperationCanceledExceptionThrown(
               () => bc.TryTake(out item, 100000, cs.Token), cs.Token,
                "ExternalCancel_TryTake:  The operation should wake up via token cancellation.");
            EnsureOperationCanceledExceptionThrown(
                () => bc.Add(1, cs.Token), cs.Token,
                "ExternalCancel_Add:  The operation should wake up via token cancellation.");
            EnsureOperationCanceledExceptionThrown(
                () => bc.TryAdd(1, 100000, cs.Token), // a long timeout.
                cs.Token,
                "ExternalCancel_TryAdd:  The operation should wake up via token cancellation.");

            BlockingCollection<int> bc1 = new BlockingCollection<int>(1);
            BlockingCollection<int> bc2 = new BlockingCollection<int>(1);
            bc1.Add(1); //fill the bc.
            bc2.Add(1); //fill the bc.
            EnsureOperationCanceledExceptionThrown(
                () => BlockingCollection<int>.AddToAny(new[] { bc1, bc2 }, 1, cs.Token),
                cs.Token,
                "ExternalCancel_AddToAny:  The operation should wake up via token cancellation.");
            EnsureOperationCanceledExceptionThrown(
               () => BlockingCollection<int>.TryAddToAny(new[] { bc1, bc2 }, 1, 10000, cs.Token),
               cs.Token,
               "ExternalCancel_AddToAny:  The operation should wake up via token cancellation.");

            IEnumerable<int> enumerable = bc.GetConsumingEnumerable(cs.Token);
            EnsureOperationCanceledExceptionThrown(
               () => enumerable.GetEnumerator().MoveNext(),
               cs.Token, "ExternalCancel_GetConsumingEnumerable:  The operation should wake up via token cancellation.");
        }

        [Fact]
        public static void ExternalCancel_AddToAny()
        {
            BlockingCollection<int> bc1 = new BlockingCollection<int>(1);
            BlockingCollection<int> bc2 = new BlockingCollection<int>(1);
            bc1.Add(1); //fill the bc.
            bc2.Add(1); //fill the bc.

            CancellationTokenSource cs = new CancellationTokenSource();
            Task.Run(() =>
            {
                cs.Cancel();
            });

            Assert.False(cs.IsCancellationRequested,
               "ExternalCancel_AddToAny:  At this point the cancel should not have occurred.");
        }

        [Fact]
        public static void ExternalCancel_TryAddToAny()
        {
            BlockingCollection<int> bc1 = new BlockingCollection<int>(1);
            BlockingCollection<int> bc2 = new BlockingCollection<int>(1);
            bc1.Add(1); //fill the bc.
            bc2.Add(1); //fill the bc.

            CancellationTokenSource cs = new CancellationTokenSource();
            Task.Run(() =>
            {
                cs.Cancel();
            });

            Assert.False(cs.IsCancellationRequested,
               "ExternalCancel_AddToAny:  At this point the cancel should not have occurred.");
        }

        [Fact]
        public static void ExternalCancel_GetConsumingEnumerable()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>();
            CancellationTokenSource cs = new CancellationTokenSource();
            Task.Run(() =>
            {
                Task.WaitAll(Task.Delay(100));
                cs.Cancel();
            });

            IEnumerable<int> enumerable = bc.GetConsumingEnumerable(cs.Token);

            Assert.False(cs.IsCancellationRequested, "ExternalCancel_GetConsumingEnumerable:  At this point the cancel should not have occurred.");
        }

        #region Helper Methods

        public static void EnsureOperationCanceledExceptionThrown(Action action, CancellationToken token, string message)
        {
            OperationCanceledException operationCanceledEx =
                Assert.Throws<OperationCanceledException>(action); // "BlockingCollectionCancellationTests: OperationCanceledException not thrown.");

            if (operationCanceledEx.CancellationToken != token)
            {
                Assert.False(true, "BlockingCollectionCancellationTests: Failed.  " + message);
            }
        }
        #endregion
    }
}
