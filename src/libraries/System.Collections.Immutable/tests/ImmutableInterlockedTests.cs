// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableInterlockedTests
    {
        private delegate bool UpdateDelegate<T>(ref T location, Func<T, T> transformer);
        private delegate bool UpdateArrayDelegate<T>(ref ImmutableArray<T> location, Func<ImmutableArray<T>, ImmutableArray<T>> transformer);

        [Fact]
        public void Update_StartWithNull()
        {
            UpdateHelper<ImmutableList<int>>(func =>
            {
                ImmutableList<int> list = null;
                Assert.True(func(ref list, l => { Assert.Null(l); return ImmutableList.Create(1); }));
                Assert.Equal(1, list.Count);
                Assert.Equal(1, list[0]);
            });
        }

        [Fact]
        public void UpdateArray_StartWithDefault()
        {
            UpdateArrayHelper<int>(func =>
            {
                ImmutableArray<int> array = default;
                Assert.True(func(ref array, l => { Assert.True(l.IsDefault); return ImmutableArray.Create(1); }));
                Assert.Equal(1, array.Length);
                Assert.Equal(1, array[0]);
            });
        }

        [Fact]
        public void UpdateArray_StartWithEmpty()
        {
            UpdateArrayHelper<int>(func =>
            {
                ImmutableArray<int> array = ImmutableArray<int>.Empty;
                Assert.True(func(ref array, l => { Assert.Equal(0, l.Length); return ImmutableArray.Create(1); }));
                Assert.Equal(1, array.Length);
                Assert.Equal(1, array[0]);
            });
        }

        [Fact]
        public void Update_IncrementalUpdate()
        {
            UpdateHelper<ImmutableList<int>>(func =>
            {
                ImmutableList<int> list = ImmutableList.Create(1);
                Assert.True(func(ref list, l => l.Add(2)));
                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0]);
                Assert.Equal(2, list[1]);
            });
        }

        [Fact]
        public void UpdateArray_IncrementalUpdate()
        {
            UpdateArrayHelper<int>(func =>
            {
                ImmutableArray<int> array = ImmutableArray.Create(1);
                Assert.True(func(ref array, l => l.Add(2)));
                Assert.Equal(2, array.Length);
                Assert.Equal(1, array[0]);
                Assert.Equal(2, array[1]);
            });
        }

        [Fact]
        public void Update_FuncThrowsThrough()
        {
            UpdateHelper<ImmutableList<int>>(func =>
            {
                ImmutableList<int> list = ImmutableList.Create(1);
                Assert.Throws<InvalidOperationException>(() => func(ref list, l => { throw new InvalidOperationException(); }));
            });
        }

        [Fact]
        public void UpdateArray_FuncThrowsThrough()
        {
            UpdateArrayHelper<int>(func =>
            {
                ImmutableArray<int> array = ImmutableArray.Create(42);
                Assert.Throws<InvalidOperationException>(() => func(ref array, l => throw new InvalidOperationException()));
            });
        }

        [Fact]
        public void Update_NoEffectualChange()
        {
            UpdateHelper<ImmutableList<int>>(func =>
            {
                ImmutableList<int> list = ImmutableList.Create<int>(1);
                Assert.False(func(ref list, l => l));
            });
        }

        [Fact]
        public void UpdateArray_NoEffectualChange()
        {
            UpdateArrayHelper<int>(func =>
            {
                ImmutableArray<int> array = ImmutableArray.Create(42);
                Assert.False(func(ref array, l => l));
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Update_HighConcurrency()
        {
            UpdateHelper<ImmutableList<int>>(func =>
            {
                ImmutableList<int> list = ImmutableList.Create<int>();
                int concurrencyLevel = Environment.ProcessorCount;
                int iterations = 500;
                Task[] tasks = new Task[concurrencyLevel];
                var barrier = new Barrier(tasks.Length);
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Factory.StartNew(delegate
                    {
                        // Maximize concurrency by blocking this thread until all the other threads are ready to go as well.
                        barrier.SignalAndWait();

                        for (int j = 0; j < iterations; j++)
                        {
                            Assert.True(func(ref list, l => l.Add(l.Count)));
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }

                Task.WaitAll(tasks);
                Assert.Equal(concurrencyLevel * iterations, list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    Assert.Equal(i, list[i]);
                }
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void UpdateArray_HighConcurrency()
        {
            UpdateArrayHelper<int>(func =>
            {
                ImmutableArray<int> array = ImmutableArray.Create<int>();
                int concurrencyLevel = Environment.ProcessorCount;
                int iterations = 500;
                Task[] tasks = new Task[concurrencyLevel];
                var barrier = new Barrier(tasks.Length);
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Factory.StartNew(delegate
                    {
                        // Maximize concurrency by blocking this thread until all the other threads are ready to go as well.
                        barrier.SignalAndWait();

                        for (int j = 0; j < iterations; j++)
                        {
                            Assert.True(func(ref array, l => l.Add(l.Length)));
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }

                Task.WaitAll(tasks);
                Assert.Equal(concurrencyLevel * iterations, array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    Assert.Equal(i, array[i]);
                }
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Update_CarefullyScheduled()
        {
            UpdateHelper<ImmutableHashSet<int>>(func =>
            {
                ImmutableHashSet<int> set = ImmutableHashSet.Create<int>();
                var task2TransformEntered = new AutoResetEvent(false);
                var task1TransformExited = new AutoResetEvent(false);

                Task task1 = Task.Run(delegate
                {
                    int transform1ExecutionCounter = 0;
                    func(
                        ref set,
                        s =>
                        {
                            Assert.Equal(1, ++transform1ExecutionCounter);
                            task2TransformEntered.WaitOne();
                            return s.Add(1);
                        });
                    task1TransformExited.Set();
                    Assert.Equal(1, transform1ExecutionCounter);
                });

                Task task2 = Task.Run(delegate
                {
                    int transform2ExecutionCounter = 0;
                    func(
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
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void UpdateArray_CarefullyScheduled()
        {
            UpdateArrayHelper<int>(func =>
            {
                ImmutableArray<int> array = ImmutableArray.Create<int>();
                var task2TransformEntered = new AutoResetEvent(false);
                var task1TransformExited = new AutoResetEvent(false);

                Task task1 = Task.Run(delegate
                {
                    int transform1ExecutionCounter = 0;
                    func(
                        ref array,
                        s =>
                        {
                            Assert.Equal(1, ++transform1ExecutionCounter);
                            task2TransformEntered.WaitOne();
                            return s.Add(1);
                        });
                    task1TransformExited.Set();
                    Assert.Equal(1, transform1ExecutionCounter);
                });

                Task task2 = Task.Run(delegate
                {
                    int transform2ExecutionCounter = 0;
                    func(
                        ref array,
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
                                    Assert.Equal(1, s.Length);
                                    break;
                            }

                            return s.Add(2);
                        });

                    // Verify that this transform had to execute twice.
                    Assert.Equal(2, transform2ExecutionCounter);
                });

                // Wait for all tasks and rethrow any exceptions.
                Task.WaitAll(task1, task2);
                Assert.Equal(2, array.Length);
                Assert.True(array.Contains(1));
                Assert.True(array.Contains(2));
            });
        }

        [Fact]
        public void InterlockedExchangeArrayDefault()
        {
            ImmutableArray<int> array = default(ImmutableArray<int>);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedExchange(ref array, ImmutableArray.Create<int>());
            Assert.True(oldValue.IsDefault);
            Assert.False(array.IsDefault);
        }

        [Fact]
        public void InterlockedExchangeArrayNonDefault()
        {
            ImmutableArray<int> array = ImmutableArray.Create(1, 2, 3);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedExchange(ref array, ImmutableArray.Create<int>(4, 5, 6));
            Assert.Equal(new[] { 1, 2, 3 }, oldValue);
            Assert.Equal(new[] { 4, 5, 6 }, array);
        }

        [Fact]
        public void InterlockedCompareExchangeArray_LocationDefault_EqualToComparand()
        {
            ImmutableArray<int> array = default(ImmutableArray<int>);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), array);
            Assert.True(oldValue.IsDefault);
            Assert.Equal(new[] { 4, 5, 6 }, array);
        }

        [Fact]
        public void InterlockedCompareExchangeArray_LocationDefault_NotEqualToComparand()
        {
            ImmutableArray<int> array = default(ImmutableArray<int>);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), ImmutableArray.Create<int>(1, 2, 3));
            Assert.True(oldValue.IsDefault);
            Assert.True(array.IsDefault);
        }

        [Fact]
        public void InterlockedCompareExchangeArray_LocationNotDefault_EqualToComparand()
        {
            ImmutableArray<int> array = ImmutableArray.Create<int>(1, 2, 3);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), array);
            Assert.Equal(new[] { 1, 2, 3 }, oldValue);
            Assert.Equal(new[] { 4, 5, 6 }, array);
        }

        [Fact]
        public void InterlockedCompareExchangeArray_LocationNotDefault_EqualToComparandDifferentInstance()
        {
            ImmutableArray<int> array = ImmutableArray.Create<int>(1, 2, 3);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), ImmutableArray.Create<int>(1, 2, 3));
            Assert.Equal(new[] { 1, 2, 3 }, oldValue);
            Assert.Equal(new[] { 1, 2, 3 }, array);
        }

        [Fact]
        public void InterlockedCompareExchangeArray_LocationNotDefault_NotEqualToDefaultComparand()
        {
            ImmutableArray<int> array = ImmutableArray.Create(1, 2, 3);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), default(ImmutableArray<int>));
            Assert.Equal(new[] { 1, 2, 3 }, oldValue);
            Assert.Equal(new[] { 1, 2, 3 }, array);
        }

        [Fact]
        public void InterlockedCompareExchangeArray_LocationNotDefault_NotEqualToNonDefaultComparand()
        {
            ImmutableArray<int> array = ImmutableArray.Create(1, 2, 3);
            ImmutableArray<int> oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref array, ImmutableArray.Create<int>(4, 5, 6), array);
            Assert.Equal(new[] { 1, 2, 3 }, oldValue);
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
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.GetOrAdd(ref dictionary, 1, "a");
            Assert.Equal("a", value);
            value = ImmutableInterlocked.GetOrAdd(ref dictionary, 1, "b");
            Assert.Equal("a", value);
        }

        [Fact]
        public void GetOrAddDictionaryValueFactory()
        {
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();
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
                key => { throw new ShouldNotBeInvokedException(); }
                );
            Assert.Equal("a", value);
        }

        [Fact]
        public void GetOrAddDictionaryValueFactoryWithArg()
        {
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();
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
                (key, arg) => { throw new ShouldNotBeInvokedException(); },
                true);
            Assert.Equal("a", value);
        }

        [Fact]
        public void AddOrUpdateDictionaryAddValue()
        {
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, "a", (k, v) => { throw new ShouldNotBeInvokedException(); });
            Assert.Equal("a", value);
            Assert.Equal("a", dictionary[1]);

            value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, "c", (k, v) => { Assert.Equal("a", v); return "b"; });
            Assert.Equal("b", value);
            Assert.Equal("b", dictionary[1]);
        }

        [Fact]
        public void AddOrUpdateDictionaryAddValueFactory()
        {
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();
            string value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, k => "a", (k, v) => { throw new ShouldNotBeInvokedException(); });
            Assert.Equal("a", value);
            Assert.Equal("a", dictionary[1]);

            value = ImmutableInterlocked.AddOrUpdate(ref dictionary, 1, k => { throw new ShouldNotBeInvokedException(); }, (k, v) => { Assert.Equal("a", v); return "b"; });
            Assert.Equal("b", value);
            Assert.Equal("b", dictionary[1]);
        }

        [Fact]
        public void TryAddDictionary()
        {
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();

            Assert.True(ImmutableInterlocked.TryAdd(ref dictionary, 1, "a"));
            Assert.Equal("a", dictionary[1]);

            Assert.False(ImmutableInterlocked.TryAdd(ref dictionary, 1, "a"));
            Assert.False(ImmutableInterlocked.TryAdd(ref dictionary, 1, "b"));
            Assert.Equal("a", dictionary[1]);
        }

        [Fact]
        public void TryUpdateDictionary()
        {
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();

            // missing
            ImmutableDictionary<int, string> before = dictionary;
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
            ImmutableDictionary<int, string> dictionary = ImmutableDictionary.Create<int, string>();

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
            ImmutableStack<int> stack = ImmutableStack.Create<int>();
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
            ImmutableStack<int> stack = ImmutableStack.Create<int>();

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
            ImmutableQueue<int> queue = ImmutableQueue.Create<int>();
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
            ImmutableQueue<int> queue = ImmutableQueue.Create<int>();
            ImmutableInterlocked.Enqueue(ref queue, 1);
            Assert.Equal(1, queue.Peek());
            Assert.True(queue.Dequeue().IsEmpty);

            ImmutableInterlocked.Enqueue(ref queue, 2);
            Assert.Equal(1, queue.Peek());
            Assert.Equal(2, queue.Dequeue().Peek());
            Assert.True(queue.Dequeue().Dequeue().IsEmpty);
        }

        /// <summary>
        /// Executes a test against both <see cref="ImmutableInterlocked.Update{T}(ref T, Func{T, T})"/>
        /// and <see cref="ImmutableInterlocked.Update{T, TArg}(ref T, Func{T, TArg, T}, TArg)"/>.
        /// </summary>
        /// <typeparam name="T">The type of value under test.</typeparam>
        /// <param name="test">
        /// The test to execute. Invoke the parameter instead of calling
        /// the ImmutableInterlocked method so that the delegate can test both overloads
        /// by being executed twice.
        /// </param>
        private static void UpdateHelper<T>(Action<UpdateDelegate<T>> test)
            where T : class
        {
            test(ImmutableInterlocked.Update<T>);
            test(UpdateWrapper<T>);
        }

        /// <summary>
        /// Executes a test against both <see cref="ImmutableInterlocked.Update{T}(ref ImmutableArray{T}, Func{ImmutableArray{T}, ImmutableArray{T}})"/>
        /// and <see cref="ImmutableInterlocked.Update{ImmutableArray{T}, TArg}(ref ImmutableArray{T}, Func{ImmutableArray{T}, TArg, ImmutableArray{T}}, TArg)"/>.
        /// </summary>
        /// <typeparam name="T">The type of value under test.</typeparam>
        /// <param name="test">
        /// The test to execute. Invoke the parameter instead of calling
        /// the ImmutableInterlocked method so that the delegate can test both overloads
        /// by being executed twice.
        /// </param>
        private static void UpdateArrayHelper<T>(Action<UpdateArrayDelegate<T>> test)
        {
            test(ImmutableInterlocked.Update<T>);
            test(UpdateArrayWrapper<T>);
        }

        /// <summary>
        /// A wrapper that makes one overload look like another so the same test delegate can execute against both.
        /// </summary>
        /// <typeparam name="T">The type of value being changed.</typeparam>
        /// <param name="location">The variable or field to be changed.</param>
        /// <param name="transformer">The function that transforms the value.</param>
        /// <returns>The result of the replacement function.</returns>
        private static bool UpdateWrapper<T>(ref T location, Func<T, T> transformer)
            where T : class
        {
            return ImmutableInterlocked.Update<T, int>(
                ref location,
                (t, arg) =>
                {
                    Assert.Equal(1, arg);
                    return transformer(t);
                },
                1);
        }

        /// <summary>
        /// A wrapper that makes one overload look like another so the same test delegate can execute against both.
        /// </summary>
        /// <typeparam name="T">The type of value being changed.</typeparam>
        /// <param name="location">The variable or field to be changed.</param>
        /// <param name="transformer">The function that transforms the value.</param>
        /// <returns>The result of the replacement function.</returns>
        private static bool UpdateArrayWrapper<T>(ref ImmutableArray<T> location, Func<ImmutableArray<T>, ImmutableArray<T>> transformer)
        {
            return ImmutableInterlocked.Update<T, int>(
                ref location,
                (t, arg) =>
                {
                    Assert.Equal(1, arg);
                    return transformer(t);
                },
                1);
        }
    }
}
