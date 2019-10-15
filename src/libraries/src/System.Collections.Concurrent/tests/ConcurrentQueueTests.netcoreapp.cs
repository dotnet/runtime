// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public partial class ConcurrentQueueTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1000)]
        public void Clear_CountMatch(int count)
        {
            var q = new ConcurrentQueue<int>(Enumerable.Range(1, count));
            Assert.Equal(count, q.Count);

            // Clear initial items
            q.Clear();
            Assert.Equal(0, q.Count);
            Assert.True(q.IsEmpty);
            Assert.Equal(Enumerable.Empty<int>(), q);

            // Clear again has no effect
            q.Clear();
            Assert.True(q.IsEmpty);

            // Add more items then clear and verify
            for (int i = 0; i < count; i++)
            {
                q.Enqueue(i);
            }
            Assert.Equal(Enumerable.Range(0, count), q);
            q.Clear();
            Assert.Equal(0, q.Count);
            Assert.True(q.IsEmpty);
            Assert.Equal(Enumerable.Empty<int>(), q);

            // Add items and clear after each item
            for (int i = 0; i < count; i++)
            {
                q.Enqueue(i);
                Assert.Equal(1, q.Count);
                q.Clear();
                Assert.Equal(0, q.Count);
            }
        }

        [Fact]
        public static void Clear_DuringEnumeration_DoesntAffectEnumeration()
        {
            const int ExpectedCount = 100;
            var q = new ConcurrentQueue<int>(Enumerable.Range(0, ExpectedCount));
            using (IEnumerator<int> beforeClear = q.GetEnumerator())
            {
                q.Clear();
                using (IEnumerator<int> afterClear = q.GetEnumerator())
                {
                    int count = 0;
                    while (beforeClear.MoveNext()) count++;
                    Assert.Equal(ExpectedCount, count);

                    count = 0;
                    while (afterClear.MoveNext()) count++;
                    Assert.Equal(0, count);
                }
            }
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(3, 100)]
        [InlineData(8, 1000)]
        public void Concurrent_Clear_NoExceptions(int threadsCount, int itemsPerThread)
        {
            var q = new ConcurrentQueue<int>();
            Task.WaitAll((from i in Enumerable.Range(0, threadsCount) select Task.Run(() =>
            {
                var random = new Random();
                for (int j = 0; j < itemsPerThread; j++)
                {
                    switch (random.Next(7))
                    {
                        case 0:
                            int c = q.Count;
                            break;
                        case 1:
                            bool e = q.IsEmpty;
                            break;
                        case 2:
                            q.Enqueue(random.Next(int.MaxValue));
                            break;
                        case 3:
                            q.ToArray();
                            break;
                        case 4:
                            int d;
                            q.TryDequeue(out d);
                            break;
                        case 5:
                            int p;
                            q.TryPeek(out p);
                            break;
                        case 6:
                            q.Clear();
                            break;
                    }
                }
            })).ToArray());
        }
    }
}
