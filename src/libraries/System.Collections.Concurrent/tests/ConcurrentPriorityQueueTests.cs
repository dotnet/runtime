using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class ConcurrentPriorityQueueTests : ProducerConsumerCollectionTests
    {
        protected override IProducerConsumerCollection<T> CreateProducerConsumerCollection<T>() => 
            new ConcurrentPriorityQueue<T, int>(new[] { 1 });
        
        protected override IProducerConsumerCollection<int> CreateProducerConsumerCollection(IEnumerable<int> collection) => 
            new ConcurrentPriorityQueue<int, int>(new[] { 1 });
        
        protected override bool IsEmpty(IProducerConsumerCollection<int> pcc) => 
            ((ConcurrentPriorityQueue<int, int>)pcc).IsEmpty;
        
        protected override bool TryPeek<T>(IProducerConsumerCollection<T> pcc, out T result)
        {
            var queue = (ConcurrentPriorityQueue<T, int>)pcc;
            return queue.TryPeek(out result);
        }

        [Fact]
        public void Ctor_NullPriorities_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ConcurrentPriorityQueue<string, int>(null));
        }

        [Fact]
        public void Ctor_EmptyPriorities_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ConcurrentPriorityQueue<string, int>(Array.Empty<int>()));
        }

        [Fact]
        public void Ctor_DuplicatePriorities_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ConcurrentPriorityQueue<string, int>(new[] { 1, 2, 1 }));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        public void StrictPriorityOrdering_SingleThread(int count)
        {
            var queue = new ConcurrentPriorityQueue<int, int>(new[] { 1, 2, 3 });
            var random = new Random(42);

            // Add items with random priorities
            for (int i = 0; i < count; i++)
            {
                queue.Enqueue(i, random.Next(1, 4));
            }

            // Verify items are dequeued in strict priority order
            int lastPriority = 0;
            int itemsDequeued = 0;
            while (queue.TryDequeueStrict(out var item))
            {
                itemsDequeued++;
                Assert.True(lastPriority <= item);
            }
            Assert.Equal(count, itemsDequeued);
        }

        [Theory]
        [InlineData(4, 1000)]
        [InlineData(8, 10000)]
        public async Task ConcurrentEnqueueDequeue_StrictPriority(int threadCount, int itemsPerThread)
        {
            var priorities = new[] { 1, 2, 3 };
            var queue = new ConcurrentPriorityQueue<(int Value, int Priority), int>(priorities);
            var tasks = new List<Task>();
            var results = new ConcurrentQueue<(int Value, int Priority)>();

            // Producer tasks
            for (int t = 0; t < threadCount; t++)
            {
                var threadId = t;
                tasks.Add(Task.Run(() =>
                {
                    var random = new Random(threadId);
                    for (int i = 0; i < itemsPerThread; i++)
                    {
                        var priority = random.Next(1, 4);
                        queue.Enqueue((threadId * itemsPerThread + i, priority), priority);
                    }
                }));
            }

            // Consumer tasks
            for (int t = 0; t < threadCount; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (results.Count < itemsPerThread * threadCount)
                    {
                        if (queue.TryDequeueStrict(out var item))
                        {
                            results.Enqueue(item);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Verify results
            var resultsList = results.ToList();
            Assert.Equal(itemsPerThread * threadCount, resultsList.Count);
            
            // Verify priority ordering
            int lastPriority = 0;
            foreach (var batch in resultsList.GroupBy(x => x.Priority).OrderBy(g => g.Key))
            {
                Assert.True(batch.Key > lastPriority);
                lastPriority = batch.Key;
            }
        }

        [Theory]
        [InlineData(4, 1000)]
        [InlineData(8, 10000)]
        public async Task ConcurrentEnqueueDequeue_FastPriority(int threadCount, int itemsPerThread)
        {
            var priorities = new[] { 1, 2, 3 };
            var queue = new ConcurrentPriorityQueue<int, int>(priorities);
            var tasks = new List<Task>();
            var processedItems = new ConcurrentDictionary<int, byte>();
            var expectedItemCount = itemsPerThread * threadCount;

            // Producer tasks
            for (int t = 0; t < threadCount; t++)
            {
                var threadId = t;
                tasks.Add(Task.Run(() =>
                {
                    var random = new Random(threadId);
                    for (int i = 0; i < itemsPerThread; i++)
                    {
                        var item = threadId * itemsPerThread + i;
                        queue.Enqueue(item, random.Next(1, 4));
                    }
                }));
            }

            // Consumer tasks
            for (int t = 0; t < threadCount; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (processedItems.Count < expectedItemCount)
                    {
                        if (queue.TryDequeueFast(out var item))
                        {
                            Assert.True(processedItems.TryAdd(item, 0));
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(expectedItemCount, processedItems.Count);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public async Task ConcurrentOperations_MixedPriorityAccess()
        {
            var priorities = new[] { 1, 2, 3 };
            var queue = new ConcurrentPriorityQueue<string, int>(priorities);
            var tasks = new List<Task>();
            var results = new ConcurrentDictionary<int, int>();
            const int OperationsPerThread = 1000;

            // Mixed operations across multiple threads
            for (int i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var random = new Random();
                    for (int j = 0; j < OperationsPerThread; j++)
                    {
                        var operation = random.Next(3);
                        var priority = random.Next(1, 4);

                        switch (operation)
                        {
                            case 0: // Enqueue
                                queue.TryAdd($"Item_{j}", priority);
                                results.AddOrUpdate(priority, 1, (_, count) => count + 1);
                                break;
                            case 1: // Strict Dequeue
                                if (queue.TryDequeueStrict(out _))
                                    results.AddOrUpdate(priority, -1, (_, count) => count - 1);
                                break;
                            case 2: // Fast Dequeue
                                if (queue.TryDequeueFast(out _))
                                    results.AddOrUpdate(priority, -1, (_, count) => count - 1);
                                break;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            
            // Verify final state
            var remainingItems = queue.Count;
            var totalOperations = results.Values.Sum();
            Assert.Equal(remainingItems, totalOperations);
        }

        [Fact]
        public void Clear_DuringEnumeration_DoesntAffectEnumeration()
        {
            const int ExpectedCount = 100;
            var queue = new ConcurrentPriorityQueue<int, int>(new[] { 1, 2, 3 });
            
            // Add items with different priorities
            for (int i = 0; i < ExpectedCount; i++)
            {
                queue.Enqueue(i, (i % 3) + 1);
            }

            using (var enumerator = queue.GetEnumerator())
            {
                queue.Clear();
                int count = 0;
                while (enumerator.MoveNext()) count++;
                Assert.Equal(ExpectedCount, count);
            }

            Assert.Equal(0, queue.Count);
            Assert.True(queue.IsEmpty);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void CopyTo_ValidatesArguments(int count)
        {
            var queue = new ConcurrentPriorityQueue<int, int>(new[] { 1 });
            for (int i = 0; i < count; i++)
            {
                queue.Enqueue(i, 1);
            }

            // Null array
            Assert.Throws<ArgumentNullException>(() => queue.CopyTo(null, 0));

            // Negative index
            var array = new int[count];
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.CopyTo(array, -1));

            // Array too small
            if (count > 0)
            {
                array = new int[count - 1];
                Assert.Throws<ArgumentException>(() => queue.CopyTo(array, 0));
            }

            // Valid copy
            array = new int[count + 1];
            queue.CopyTo(array, 1);
            Assert.Equal(0, array[0]); // Untouched
            Assert.Equal(queue, array.Skip(1).Take(count));
        }

        [Fact]
        public async Task ConcurrentClear_WithActiveOperations()
        {
            var priorities = new[] { 1, 2, 3 };
            var queue = new ConcurrentPriorityQueue<int, int>(priorities);
            var operations = new List<Task>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Continuously enqueue items
            operations.Add(Task.Run(() =>
            {
                var i = 0;
                while (!cts.Token.IsCancellationRequested)
                {
                    queue.Enqueue(i++, (i % 3) + 1);
                }
            }));

            // Continuously dequeue items
            operations.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    queue.TryDequeueStrict(out _);
                }
            }));

            // Periodically clear the queue
            operations.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    queue.Clear();
                    Thread.Sleep(100);
                }
            }));

            try
            {
                await Task.WhenAll(operations);
            }
            catch (OperationCanceledException)
            {
                // Expected when duration is reached
            }

            queue.Clear();
            Assert.True(queue.IsEmpty);
        }
    }
}
