// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public partial class ConcurrentBagTests
    {
        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 20)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 20)]
        public static void Clear_AddItemsToThisAndOtherThreads_EmptyAfterClear(bool addToLocalThread, int otherThreads)
        {
            var bag = new ConcurrentBag<int>();

            const int ItemsPerThread = 100;

            for (int repeat = 0; repeat < 2; repeat++)
            {
                // If desired, add items on other threads
                if (addToLocalThread)
                {
                    for (int i = 0; i < ItemsPerThread; i++) bag.Add(i);
                }

                // If desired, add items on other threads
                int origThreadId = Environment.CurrentManagedThreadId;
                Task.WaitAll((from _ in Enumerable.Range(0, otherThreads)
                              select Task.Factory.StartNew(() =>
                              {
                                  Assert.NotEqual(origThreadId, Environment.CurrentManagedThreadId);
                                  for (int i = 0; i < ItemsPerThread; i++) bag.Add(i);
                              }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

                // Make sure we got the expected number of items, then clear, and make sure it's empty
                Assert.Equal((ItemsPerThread * otherThreads) + (addToLocalThread ? ItemsPerThread : 0), bag.Count);
                bag.Clear();
                Assert.Equal(0, bag.Count);
            }
        }

        [Fact]
        public static void Clear_DuringEnumeration_DoesntAffectEnumeration()
        {
            const int ExpectedCount = 100;
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, ExpectedCount));
            using (IEnumerator<int> e = bag.GetEnumerator())
            {
                bag.Clear();
                int count = 0;
                while (e.MoveNext()) count++;
                Assert.Equal(ExpectedCount, count);
            }
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(3, 100)]
        [InlineData(8, 1000)]
        public static void Clear_ConcurrentUsage_NoExceptions(int threadsCount, int itemsPerThread)
        {
            var bag = new ConcurrentBag<int>();
            Task.WaitAll((from i in Enumerable.Range(0, threadsCount) select Task.Run(() =>
            {
                var random = new Random();
                for (int j = 0; j < itemsPerThread; j++)
                {
                    int item;
                    switch (random.Next(5))
                    {
                        case 0: bag.Add(j); break;
                        case 1: bag.TryPeek(out item); break;
                        case 2: bag.TryTake(out item); break;
                        case 3: bag.Clear(); break;
                        case 4: bag.ToArray(); break;
                    }
                }
            })).ToArray());
        }
    }
}
