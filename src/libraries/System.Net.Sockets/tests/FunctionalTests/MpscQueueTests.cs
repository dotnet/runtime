// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Sockets.Tests
{
    [PlatformSpecific(TestPlatforms.Linux)] // MPSC queue is used by Linux io_uring paths.
    public class MpscQueueTests
    {
        [Fact]
        public void MpscQueue_SingleProducerSingleConsumer_PreservesOrder()
        {
            const int count = 1024;
            var queue = new MpscQueue<int>(segmentSize: 16);

            for (int i = 0; i < count; i++)
            {
                queue.Enqueue(i);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.True(queue.TryDequeue(out int value));
                Assert.Equal(i, value);
            }

            Assert.True(queue.IsEmpty);
            Assert.False(queue.TryDequeue(out _));
        }

        [Fact]
        public async Task MpscQueue_MultiProducerSingleConsumer_ReceivesAllItems()
        {
            const int producerCount = 4;
            const int itemsPerProducer = 2000;
            const int totalItems = producerCount * itemsPerProducer;
            var queue = new MpscQueue<int>(segmentSize: 32);

            Task[] producers = new Task[producerCount];
            for (int p = 0; p < producerCount; p++)
            {
                int producerIndex = p;
                producers[p] = Task.Run(() =>
                {
                    int baseValue = producerIndex * itemsPerProducer;
                    for (int i = 0; i < itemsPerProducer; i++)
                    {
                        queue.Enqueue(baseValue + i);
                    }
                });
            }

            var seen = new bool[totalItems];
            int received = 0;
            var spin = new SpinWait();
            while (received < totalItems)
            {
                if (queue.TryDequeue(out int value))
                {
                    Assert.InRange(value, 0, totalItems - 1);
                    Assert.False(seen[value], $"duplicate dequeue value: {value}");
                    seen[value] = true;
                    received++;
                }
                else
                {
                    spin.SpinOnce();
                }
            }

            await Task.WhenAll(producers);
            Assert.All(seen, Assert.True);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void MpscQueue_EmptyQueue_ReportsEmptyAndTryDequeueFalse()
        {
            var queue = new MpscQueue<int>(segmentSize: 8);

            Assert.True(queue.IsEmpty);
            Assert.False(queue.TryDequeue(out _));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void MpscQueue_Ctor_InvalidSegmentSize_Throws(int segmentSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MpscQueue<int>(segmentSize));
        }

        [Fact]
        public void MpscQueue_SegmentCrossing_WorksAcrossMultipleSegments()
        {
            const int count = 37;
            var queue = new MpscQueue<int>(segmentSize: 2);

            for (int i = 0; i < count; i++)
            {
                queue.Enqueue(i);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.True(queue.TryDequeue(out int value));
                Assert.Equal(i, value);
            }

            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public async Task MpscQueue_SegmentSizeOne_MultiProducerSingleConsumer_ReceivesAllItems()
        {
            const int producerCount = 3;
            const int itemsPerProducer = 1000;
            const int totalItems = producerCount * itemsPerProducer;
            var queue = new MpscQueue<int>(segmentSize: 1);

            Task[] producers = new Task[producerCount];
            for (int p = 0; p < producerCount; p++)
            {
                int producerIndex = p;
                producers[p] = Task.Run(() =>
                {
                    int baseValue = producerIndex * itemsPerProducer;
                    for (int i = 0; i < itemsPerProducer; i++)
                    {
                        queue.Enqueue(baseValue + i);
                    }
                });
            }

            var seen = new bool[totalItems];
            int received = 0;
            var spin = new SpinWait();
            while (received < totalItems)
            {
                if (queue.TryDequeue(out int value))
                {
                    Assert.InRange(value, 0, totalItems - 1);
                    Assert.False(seen[value], $"duplicate dequeue value: {value}");
                    seen[value] = true;
                    received++;
                }
                else
                {
                    spin.SpinOnce();
                }
            }

            await Task.WhenAll(producers);
            Assert.All(seen, Assert.True);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public async Task MpscQueue_Stress_NoLossAndNoDeadlock()
        {
            const int producerCount = 6;
            const int itemsPerProducer = 4000;
            const int totalItems = producerCount * itemsPerProducer;
            var queue = new MpscQueue<int>(segmentSize: 32);

            Task[] producers = new Task[producerCount];
            for (int p = 0; p < producerCount; p++)
            {
                int producerIndex = p;
                producers[p] = Task.Run(() =>
                {
                    int baseValue = producerIndex * itemsPerProducer;
                    for (int i = 0; i < itemsPerProducer; i++)
                    {
                        queue.Enqueue(baseValue + i);
                    }
                });
            }

            var seen = new HashSet<int>();
            int received = 0;
            var timeout = Stopwatch.StartNew();
            while (received < totalItems)
            {
                if (timeout.Elapsed > TimeSpan.FromSeconds(30))
                {
                    throw new TimeoutException($"Timed out draining MPSC queue. received={received}, expected={totalItems}");
                }

                if (queue.TryDequeue(out int value))
                {
                    Assert.True(seen.Add(value), $"duplicate dequeue value: {value}");
                    received++;
                }
                else
                {
                    await Task.Yield();
                }
            }

            await Task.WhenAll(producers);
            Assert.Equal(totalItems, seen.Count);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public async Task MpscQueue_Arm64_ConcurrentStress_NoLossOrDeadlock()
        {
            if (!PlatformDetection.IsArm64Process)
            {
                return;
            }

            const int producerCount = 8;
            const int itemsPerProducer = 20000;
            const int totalItems = producerCount * itemsPerProducer;
            var queue = new MpscQueue<int>(segmentSize: 4);

            Task[] producers = new Task[producerCount];
            for (int p = 0; p < producerCount; p++)
            {
                int producerIndex = p;
                producers[p] = Task.Run(() =>
                {
                    int baseValue = producerIndex * itemsPerProducer;
                    for (int i = 0; i < itemsPerProducer; i++)
                    {
                        queue.Enqueue(baseValue + i);
                    }
                });
            }

            var seen = new bool[totalItems];
            int received = 0;
            var timeout = Stopwatch.StartNew();
            while (received < totalItems)
            {
                if (timeout.Elapsed > TimeSpan.FromSeconds(90))
                {
                    throw new TimeoutException($"Timed out draining ARM64 MPSC stress queue. received={received}, expected={totalItems}");
                }

                if (queue.TryDequeue(out int value))
                {
                    Assert.InRange(value, 0, totalItems - 1);
                    Assert.False(seen[value], $"duplicate dequeue value: {value}");
                    seen[value] = true;
                    received++;
                    continue;
                }

                await Task.Yield();
            }

            await Task.WhenAll(producers);
            Assert.All(seen, Assert.True);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void MpscQueue_TryEnqueue_RecoversAfterSegmentAllocationOom()
        {
#if DEBUG
            var queue = new MpscQueue<int>(segmentSize: 1);
            queue.Enqueue(1);
            MpscQueue<int>.SetSegmentAllocationFailuresForTest(1);
            try
            {
                Assert.False(queue.TryEnqueue(2));

                Assert.True(queue.TryDequeue(out int first));
                Assert.Equal(1, first);

                Assert.True(queue.TryEnqueue(2));
                Assert.True(queue.TryDequeue(out int second));
                Assert.Equal(2, second);
                Assert.True(queue.IsEmpty);
            }
            finally
            {
                MpscQueue<int>.SetSegmentAllocationFailuresForTest(0);
            }
#else
            Assert.True(true);
#endif
        }
    }
}
