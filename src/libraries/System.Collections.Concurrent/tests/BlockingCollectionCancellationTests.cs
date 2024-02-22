// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class BlockingCollectionCancellationTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
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

        //This tests that Take/TryTake wake up correctly if CompleteAdding() is called while waiting
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void InternalCancellation_WakingUp()
        {
            for (int test = 0; test < 2; test++)
            {
                BlockingCollection<int> coll1 = new BlockingCollection<int>(1);
                coll1.Add(1); //fills the collection.
                Assert.False(coll1.IsAddingCompleted,
                   "InternalCancellation_WakingUp:  At this point CompleteAdding should not have occurred.");

                // This is racy on what we want to test, in that it's possible this queued work could execute
                // so quickly that CompleteAdding happens before the tested method gets invoked, but the test
                // should still pass in such cases, we're just testing something other than we'd planned.
                Task t = Task.Run(() => coll1.CompleteAdding());

                // Try different methods that should wake up once CompleteAdding has been called
                int item = coll1.Take(); // remove the existing item in the collection
                switch (test)
                {
                    case 0:
                        Assert.Throws<InvalidOperationException>(() => coll1.Take());
                        break;
                    case 1:
                        Assert.False(coll1.TryTake(out item));
                        break;
                }

                t.Wait();

                Assert.True(coll1.IsAddingCompleted,
                   "InternalCancellation_WakingUp:  At this point CompleteAdding should have occurred.");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void ExternalCancel_Negative()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>(); //empty collection.

            CancellationTokenSource cs = new CancellationTokenSource();
            Task.Run(() => cs.Cancel());

            int item;
            EnsureOperationCanceledExceptionThrown(
                () => bc.Take(cs.Token), cs.Token);
            EnsureOperationCanceledExceptionThrown(
                () => bc.TryTake(out item, 100000, cs.Token), cs.Token);
            EnsureOperationCanceledExceptionThrown(
                () => bc.Add(1, cs.Token), cs.Token);
            EnsureOperationCanceledExceptionThrown(
                () => bc.TryAdd(1, 100000, cs.Token), // a long timeout.
                cs.Token);

            BlockingCollection<int> bc1 = new BlockingCollection<int>(1);
            BlockingCollection<int> bc2 = new BlockingCollection<int>(1);
            bc1.Add(1); //fill the bc.
            bc2.Add(1); //fill the bc.
            EnsureOperationCanceledExceptionThrown(
                () => BlockingCollection<int>.AddToAny(new[] { bc1, bc2 }, 1, cs.Token),
                cs.Token);
            EnsureOperationCanceledExceptionThrown(
               () => BlockingCollection<int>.TryAddToAny(new[] { bc1, bc2 }, 1, 10000, cs.Token),
               cs.Token);

            IEnumerable<int> enumerable = bc.GetConsumingEnumerable(cs.Token);
            EnsureOperationCanceledExceptionThrown(
               () => enumerable.GetEnumerator().MoveNext(),
               cs.Token);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void ExternalCancel_AddToAny()
        {
            for (int test = 0; test < 3; test++)
            {
                BlockingCollection<int> bc1 = new BlockingCollection<int>(1);
                BlockingCollection<int> bc2 = new BlockingCollection<int>(1);
                bc1.Add(1); //fill the bc.
                bc2.Add(1); //fill the bc.

                // This may or may not cancel before {Try}AddToAny executes, but either way the test should pass.
                // A delay could be used to attempt to force the right timing, but not for an inner loop test.
                CancellationTokenSource cs = new CancellationTokenSource();
                Task.Run(() => cs.Cancel());
                Assert.Throws<OperationCanceledException>(() =>
                {
                    switch (test)
                    {
                        case 0:
                            BlockingCollection<int>.AddToAny(new[] { bc1, bc2 }, 42, cs.Token);
                            break;
                        case 1:
                            BlockingCollection<int>.TryAddToAny(new[] { bc1, bc2 }, 42, Timeout.Infinite, cs.Token);
                            break;
                        case 2:
                            BlockingCollection<int>.TryAddToAny(new[] { bc1, bc2 }, 42, (int)TimeSpan.FromDays(1).TotalMilliseconds, cs.Token);
                            break;
                    }
                });
                Assert.True(cs.IsCancellationRequested);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void ExternalCancel_GetConsumingEnumerable()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>();
            bc.Add(1);
            bc.Add(2);

            var cs = new CancellationTokenSource();
            int total = 0;
            Assert.Throws<OperationCanceledException>(() =>
            {
                foreach (int item in bc.GetConsumingEnumerable(cs.Token))
                {
                    total += item;
                    cs.Cancel();
                }
            });
            Assert.True(cs.IsCancellationRequested);
            Assert.Equal(expected: 1, actual: total);
        }

        #region Helper Methods

        private static void EnsureOperationCanceledExceptionThrown(Action action, CancellationToken token)
        {
            OperationCanceledException operationCanceledEx =
                Assert.Throws<OperationCanceledException>(action); // "BlockingCollectionCancellationTests: OperationCanceledException not thrown.");
            Assert.Equal(token, operationCanceledEx.CancellationToken);
        }
        #endregion
    }
}
