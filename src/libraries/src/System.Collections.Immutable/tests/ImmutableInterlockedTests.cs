// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Immutable.Test
{
    public class ImmutableInterlockedTests
    {
        [Fact]
        public void ApplyChange_StartWithNull()
        {
            ImmutableList<int> list = null;
            Assert.True(ImmutableInterlocked.ApplyChange(ref list, l => { Assert.Null(l); return ImmutableList.Create(1); }));
            Assert.Equal(1, list.Count);
            Assert.Equal(1, list[0]);
        }

        [Fact]
        public void ApplyChange_IncrementalUpdate()
        {
            ImmutableList<int> list = ImmutableList.Create(1);
            Assert.True(ImmutableInterlocked.ApplyChange(ref list, l => l.Add(2)));
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
        }

        [Fact]
        public void ApplyChange_FuncThrowsThrough()
        {
            ImmutableList<int> list = ImmutableList.Create(1);
            Assert.Throws<InvalidOperationException>(() => ImmutableInterlocked.ApplyChange(ref list, l => { throw new InvalidOperationException(); }));
        }

        [Fact]
        public void ApplyChange_NoEffectualChange()
        {
            ImmutableList<int> list = ImmutableList.Create<int>(1);
            Assert.False(ImmutableInterlocked.ApplyChange(ref list, l => l));
        }

        [Fact]
        public void ApplyChange_HighConcurrency()
        {
            ImmutableList<int> list = ImmutableList.Create<int>();
            int concurrencyLevel = Environment.ProcessorCount;
            int iterations = 500;
            Task[] tasks = new Task[concurrencyLevel];
            var barrier = new Barrier(tasks.Length);
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(delegate
                {
                    // Maximize concurrency by blocking this thread until all the other threads are ready to go as well.
                    barrier.SignalAndWait();

                    for (int j = 0; j < iterations; j++)
                    {
                        Assert.True(ImmutableInterlocked.ApplyChange(ref list, l => l.Add(l.Count)));
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.Equal(concurrencyLevel * iterations, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(i, list[i]);
            }
        }

        [Fact]
        public void ApplyChange_CarefullyScheduled()
        {
            var set = ImmutableHashSet.Create<int>();
            var task1TransformEntered = new AutoResetEvent(false);
            var task2TransformEntered = new AutoResetEvent(false);
            var task1TransformExited = new AutoResetEvent(false);

            var task1 = Task.Run(delegate
            {
                int transform1ExecutionCounter = 0;
                ImmutableInterlocked.ApplyChange(
                    ref set,
                    s =>
                    {
                        Assert.Equal(1, ++transform1ExecutionCounter);
                        task1TransformEntered.Set();
                        task2TransformEntered.WaitOne();
                        return s.Add(1);
                    });
                task1TransformExited.Set();
                Assert.Equal(1, transform1ExecutionCounter);
            });

            var task2 = Task.Run(delegate
            {
                int transform2ExecutionCounter = 0;
                ImmutableInterlocked.ApplyChange(
                    ref set,
                    s =>
                    {
                        switch (++transform2ExecutionCounter)
                        {
                            case 1:
                                task2TransformEntered.Set();
                                task1TransformExited.WaitOne();
                                Assert.True(s.IsEmpty);
                                break;
                            case 2:
                                Assert.True(s.Contains(1));
                                Assert.Equal(1, s.Count);
                                break;
                        }

                        return s.Add(2);
                    });

                // Verify that this transform had to execute twice.
                Assert.Equal(2, transform2ExecutionCounter);
            });

            // Wait for all tasks and rethrow any exceptions.
            Task.WaitAll(task1, task2);
            Assert.Equal(2, set.Count);
            Assert.True(set.Contains(1));
            Assert.True(set.Contains(2));
        }

        [Fact]
        public void InterlockedExchangeArrayDefault()
        {
            ImmutableArray<int> array = default(ImmutableArray<int>);
            var oldValue = ImmutableInterlocked.InterlockedExchange(ref array, ImmutableArray.Create<int>());
            Assert.True(oldValue.IsDefault);
            Assert.False(array.IsDefault);
        }

        [Fact]
        public void InterlockedExchangeArrayNonDefault()
        {
            ImmutableArray<int> array = ImmutableArray.Create(1, 2, 3);
            var oldValue = ImmutableInterlocked.InterlockedExchange(ref array, ImmutableArray.Create<int>(4, 5, 6));
            Assert.Equal(new[] { 1, 2, 3 }, oldValue);
            Assert.Equal(new[] { 4, 5, 6 }, array);
        }

        [Fact]
        public void InterlockedCompareExchangeArrayDefault()
        {
            ImmutableArray<int> array = default(ImmutableArray<int>);
            var oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), default(ImmutableArray<int>));
        }

        [Fact]
        public void InterlockedCompareExchangeArray()
        {
            ImmutableArray<int> array = ImmutableArray.Create(1, 2, 3);
            var oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), default(ImmutableArray<int>));
            Assert.Equal(array.AsEnumerable(), oldValue);

            var arrayBefore = array;
            oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), array);
            Assert.Equal(oldValue.AsEnumerable(), arrayBefore);
            Assert.Equal(new[] { 4, 5, 6 }, array);
        }

        [Fact]
        public void InterlockedInitializeArray()
        {
            ImmutableArray<int> array = default(ImmutableArray<int>);
            Assert.True(ImmutableInterlocked.InterlockedInitialize(ref array, ImmutableArray.Create<int>()));
            Assert.False(array.IsDefault);
            Assert.False(ImmutableInterlocked.InterlockedInitialize(ref array, ImmutableArray.Create(1, 2, 3)));
            Assert.True(array.IsEmpty);
        }

        [Fact]
        public void GetOrAddDictionaryValue()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.GetOrAdd(ref dictionary, 1, "a");
            Assert.Equal("a", value);
            value = ImmutableInterlocked.GetOrAdd(ref dictionary, 1, "b");
            Assert.Equal("a", value);
        }

        [Fact]
        public void GetOrAddDictionaryValueFactory()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.GetOrAdd(
                ref dictionary,
                1,
                key =>
                {
                    Assert.Equal(1, key);
                    return "a";
                });
            Assert.Equal("a", value);
            value = ImmutableInterlocked.GetOrAdd(
                ref dictionary,
                1,
                key =>
                {
                    Assert.True(false); // should never be invoked
                    return "b";
                });
            Assert.Equal("a", value);
        }

        [Fact]
        public void GetOrAddDictionaryValueFactoryWithArg()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.GetOrAdd(
                ref dictionary,
                1,
                (key, arg) =>
                {
                    Assert.True(arg);
                    Assert.Equal(1, key);
                    return "a";
                },
                true);
            Assert.Equal("a", value);
            value = ImmutableInterlocked.GetOrAdd(
                ref dictionary,
                1,
                (key, arg) =>
                {
                    Assert.True(false); // should never be invoked
                    return "b";
                },
                true);
            Assert.Equal("a", value);
        }

        [Fact]
        public void AddOrUpdateDictionaryAddValue()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, "a", (k, v) => { Assert.True(false); return "b"; });
            Assert.Equal("a", value);
            Assert.Equal("a", dictionary[1]);

            value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, "c", (k, v) => { Assert.Equal("a", v); return "b"; });
            Assert.Equal("b", value);
            Assert.Equal("b", dictionary[1]);
        }

        [Fact]
        public void AddOrUpdateDictionaryAddValueFactory()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, k => "a", (k, v) => { Assert.True(false); return "b"; });
            Assert.Equal("a", value);
            Assert.Equal("a", dictionary[1]);

            value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, k => { Assert.True(false); return "c"; }, (k, v) => { Assert.Equal("a", v); return "b"; });
            Assert.Equal("b", value);
            Assert.Equal("b", dictionary[1]);
        }

        [Fact]
        public void TryAddDictionary()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();

            Assert.True(ImmutableInterlocked.TryAdd(ref dictionary, 1, "a"));
            Assert.Equal("a", dictionary[1]);

            Assert.False(ImmutableInterlocked.TryAdd(ref dictionary, 1, "a"));
            Assert.False(ImmutableInterlocked.TryAdd(ref dictionary, 1, "b"));
            Assert.Equal("a", dictionary[1]);
        }

        [Fact]
        public void TryUpdateDictionary()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();

            // missing
            var before = dictionary;
            Assert.False(ImmutableInterlocked.TryUpdate(ref dictionary, 1, "a", "b"));
            Assert.Same(before, dictionary);

            // mismatched existing value
            before = dictionary = dictionary.SetItem(1, "b");
            Assert.False(ImmutableInterlocked.TryUpdate(ref dictionary, 1, "a", "c"));
            Assert.Same(before, dictionary);

            // match
            Assert.True(ImmutableInterlocked.TryUpdate(ref dictionary, 1, "c", "b"));
            Assert.NotSame(before, dictionary);
            Assert.Equal("c", dictionary[1]);
        }

        [Fact]
        public void TryRemoveDictionary()
        {
            var dictionary = ImmutableDictionary.Create<int, string>();

            string value;
            Assert.False(ImmutableInterlocked.TryRemove(ref dictionary, 1, out value));
            Assert.Null(value);

            dictionary = dictionary.Add(1, "a");
            Assert.True(ImmutableInterlocked.TryRemove(ref dictionary, 1, out value));
            Assert.Equal("a", value);
            Assert.True(dictionary.IsEmpty);
        }

        [Fact]
        public void PushStack()
        {
            var stack = ImmutableStack.Create<int>();
            ImmutableInterlocked.Push(ref stack, 5);
            Assert.False(stack.IsEmpty);
            Assert.Equal(5, stack.Peek());
            Assert.True(stack.Pop().IsEmpty);

            ImmutableInterlocked.Push(ref stack, 8);
            Assert.Equal(8, stack.Peek());
            Assert.Equal(5, stack.Pop().Peek());
        }

        [Fact]
        public void TryPopStack()
        {
            var stack = ImmutableStack.Create<int>();

            int value;
            Assert.False(ImmutableInterlocked.TryPop(ref stack, out value));
            Assert.Equal(0, value);
            stack = stack.Push(2).Push(3);
            Assert.True(ImmutableInterlocked.TryPop(ref stack, out value));
            Assert.Equal(3, value);
            Assert.Equal(2, stack.Peek());
            Assert.True(stack.Pop().IsEmpty);
        }

        [Fact]
        public void TryDequeueQueue()
        {
            var queue = ImmutableQueue.Create<int>();
            int value;
            Assert.False(ImmutableInterlocked.TryDequeue(ref queue, out value));
            Assert.Equal(0, value);

            queue = queue.Enqueue(1).Enqueue(2);
            Assert.True(ImmutableInterlocked.TryDequeue(ref queue, out value));
            Assert.Equal(1, value);
            Assert.True(ImmutableInterlocked.TryDequeue(ref queue, out value));
            Assert.Equal(2, value);
            Assert.False(ImmutableInterlocked.TryDequeue(ref queue, out value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void EnqueueQueue()
        {
            var queue = ImmutableQueue.Create<int>();
            ImmutableInterlocked.Enqueue(ref queue, 1);
            Assert.Equal(1, queue.Peek());
            Assert.True(queue.Dequeue().IsEmpty);

            ImmutableInterlocked.Enqueue(ref queue, 2);
            Assert.Equal(1, queue.Peek());
            Assert.Equal(2, queue.Dequeue().Peek());
            Assert.True(queue.Dequeue().Dequeue().IsEmpty);
        }
    }
}
