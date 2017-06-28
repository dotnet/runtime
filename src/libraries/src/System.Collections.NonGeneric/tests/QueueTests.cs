// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Tests
{
    public static class QueueTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            const int DefaultCapactiy = 32;
            var queue = new Queue();

            Assert.Equal(0, queue.Count);

            for (int i = 0; i <= DefaultCapactiy; i++)
            {
                queue.Enqueue(i);
            }

            Assert.Equal(DefaultCapactiy + 1, queue.Count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(32)]
        [InlineData(77)]
        public static void Ctor_Int(int capacity)
        {
            var queue = new Queue(capacity);

            for (int i = 0; i <= capacity; i++)
            {
                queue.Enqueue(i);
            }
            Assert.Equal(capacity + 1, queue.Count);
        }

        [Fact]
        public static void Ctor_Int_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Queue(-1)); // Capacity < 0
        }

        [Theory]
        [InlineData(1, 2.0)]
        [InlineData(32, 1.0)]
        [InlineData(77, 5.0)]
        public static void Ctor_Int_Int(int capacity, float growFactor)
        {
            var queue = new Queue(capacity, growFactor);

            for (int i = 0; i <= capacity; i++)
            {
                queue.Enqueue(i);
            }
            Assert.Equal(capacity + 1, queue.Count);
        }

        [Fact]
        public static void Ctor_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Queue(-1, 1)); // Capacity < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("growFactor", () => new Queue(1, (float)0.99)); // Grow factor < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>("growFactor", () => new Queue(1, (float)10.01)); // Grow factor > 10
        }

        [Fact]
        public static void Ctor_ICollection()
        {
            ArrayList arrList = Helpers.CreateIntArrayList(100);
            var queue = new Queue(arrList);

            Assert.Equal(arrList.Count, queue.Count);
            for (int i = 0; i < queue.Count; i++)
            {
                Assert.Equal(i, queue.Dequeue());
            }
        }

        [Fact]
        public static void Ctor_ICollection_NullCollection_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("col", () => new Queue(null)); // Collection is null
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new Queue());

            var testQueue = new Queue();
            testQueue.Enqueue("a");
            testQueue.Enqueue(1);
            testQueue.Enqueue("b");
            testQueue.Enqueue(2);

            DebuggerAttributeInfo debuggerAttribute = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(testQueue);
            PropertyInfo infoProperty = debuggerAttribute.Properties.Single(property => property.Name == "Items");
            object[] items = (object[])infoProperty.GetValue(debuggerAttribute.Instance);

            Assert.Equal(testQueue.ToArray(), items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute_NullQueue_ThrowsArgumentNullException()
        {
            bool threwNull = false;
            try
            {
                DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(Queue), null);
            }
            catch (TargetInvocationException ex)
            {
                threwNull = ex.InnerException is ArgumentNullException;
            }

            Assert.True(threwNull);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(32)]
        public static void Clear(int capacity)
        {
            var queue1 = new Queue(capacity);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                queue2.Enqueue(1);
                queue2.Clear();
                Assert.Equal(0, queue2.Count);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(32)]
        public static void Clear_Empty(int capacity)
        {
            var queue1 = new Queue(capacity);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                queue2.Clear();
                Assert.Equal(0, queue2.Count);

                queue2.Clear();
                Assert.Equal(0, queue2.Count);
            });
        }
        [Fact]
        public static void Clone()
        {
            Queue queue1 = Helpers.CreateIntQueue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Queue clone = (Queue)queue2.Clone();
                Assert.Equal(queue2.IsSynchronized, clone.IsSynchronized);

                Assert.Equal(queue2.Count, clone.Count);
                for (int i = 0; i < queue2.Count; i++)
                {
                    Assert.True(clone.Contains(i));
                }
            });
        }

        [Fact]
        public static void Clone_IsShallowCopy()
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                queue2.Enqueue(new Foo(10));
                Queue clone = (Queue)queue2.Clone();

                var foo = (Foo)queue2.Dequeue();
                foo.IntValue = 50;

                var fooClone = (Foo)clone.Dequeue();
                Assert.Equal(50, fooClone.IntValue);
            });
        }

        [Fact]
        public static void Clone_Empty()
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Queue clone = (Queue)queue2.Clone();
                Assert.Equal(0, clone.Count);

                // Can change the clone queue
                clone.Enqueue(500);
                Assert.Equal(500, clone.Dequeue());
            });
        }

        [Fact]
        public static void Clone_Clear()
        {
            Queue queue1 = Helpers.CreateIntQueue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                queue2.Clear();

                Queue clone = (Queue)queue2.Clone();
                Assert.Equal(0, clone.Count);

                // Can change clone queue
                clone.Enqueue(500);
                Assert.Equal(500, clone.Dequeue());
            });
        }

        [Fact]
        public static void Clone_DequeueUntilEmpty()
        {
            Queue queue1 = Helpers.CreateIntQueue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                for (int i = 0; i < 100; i++)
                {
                    queue2.Dequeue();
                }

                Queue clone = (Queue)queue2.Clone();
                Assert.Equal(0, queue2.Count);

                // Can change clone the queue
                clone.Enqueue(500);
                Assert.Equal(500, clone.Dequeue());
            });
        }

        [Fact]
        public static void Clone_DequeueThenEnqueue()
        {
            var queue1 = new Queue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                // Insert 50 items in the Queue
                for (int i = 0; i < 50; i++)
                {
                    queue2.Enqueue(i);
                }

                // Insert and Remove 75 items in the Queue. This should wrap the queue 
                // where there is 25 at the end of the array and 25 at the beginning
                for (int i = 0; i < 75; i++)
                {
                    queue2.Enqueue(i + 50);
                    queue2.Dequeue();
                }

                Queue queClone = (Queue)queue2.Clone();

                Assert.Equal(50, queClone.Count);
                Assert.Equal(75, queClone.Dequeue());

                // Add an item to the Queue
                queClone.Enqueue(100);
                Assert.Equal(50, queClone.Count);
                Assert.Equal(76, queClone.Dequeue());
            });
        }

        [Fact]
        public static void Contains()
        {
            Queue queue1 = Helpers.CreateIntQueue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                for (int i = 0; i < queue2.Count; i++)
                {
                    Assert.True(queue2.Contains(i));
                }

                queue2.Enqueue(null);
                Assert.True(queue2.Contains(null));
            });
        }

        [Fact]
        public static void Contains_NonExistentObject()
        {
            Queue queue1 = Helpers.CreateIntQueue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Assert.False(queue2.Contains(101));
                Assert.False(queue2.Contains("hello world"));
                Assert.False(queue2.Contains(null));

                queue2.Enqueue(null);
                Assert.False(queue2.Contains(-1)); // We have a null item in the list, so the algorithm may use a different branch 
            });
        }

        [Fact]
        public static void Contains_EmptyQueue()
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Assert.False(queue2.Contains(101));
                Assert.False(queue2.Contains("hello world"));
                Assert.False(queue2.Contains(null));
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(10, 0)]
        [InlineData(100, 0)]
        [InlineData(1000, 0)]
        [InlineData(1, 50)]
        [InlineData(10, 50)]
        [InlineData(100, 50)]
        [InlineData(1000, 50)]
        public static void CopyTo(int count, int index)
        {
            Queue queue1 = Helpers.CreateIntQueue(count);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                var array = new object[count + index];
                queue2.CopyTo(array, index);
                Assert.Equal(count + index, array.Length);
                for (int i = index; i < index + count; i++)
                {
                    Assert.Equal(queue2.Dequeue(), array[i]);
                }
            });
        }

        [Fact]
        public static void CopyTo_DequeueThenEnqueue()
        {
            var queue1 = new Queue(100);
            // Insert 50 items in the Queue
            for (int i = 0; i < 50; i++)
            {
                queue1.Enqueue(i);
            }

            // Insert and Remove 75 items in the Queue. This should wrap the queue 
            // where there is 25 at the end of the array and 25 at the beginning
            for (int i = 0; i < 75; i++)
            {
                queue1.Enqueue(i + 50);
                queue1.Dequeue();
            }

            var array = new object[queue1.Count];
            queue1.CopyTo(array, 0);
            Assert.Equal(queue1.Count, array.Length);
            for (int i = 0; i < queue1.Count; i++)
            {
                Assert.Equal(queue1.Dequeue(), array[i]);
            }
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            Queue queue1 = Helpers.CreateIntQueue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                AssertExtensions.Throws<ArgumentNullException>("array", () => queue2.CopyTo(null, 0)); // Array is null
                AssertExtensions.Throws<ArgumentException>("array", null, () => queue2.CopyTo(new object[150, 150], 0)); // Array is multidimensional

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => queue2.CopyTo(new object[150], -1)); // Index < 0

                AssertExtensions.Throws<ArgumentException>(null, () => queue2.CopyTo(new object[150], 51)); // Index + queue.Count > array.Length
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void Dequeue(int count)
        {
            Queue queue1 = Helpers.CreateIntQueue(count);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                for (int i = 1; i <= count; i++)
                {
                    int obj = (int)queue2.Dequeue();
                    Assert.Equal(i - 1, obj);
                    Assert.Equal(count - i, queue2.Count);
                }
            });
        }

        [Fact]
        public static void Dequeue_EmptyQueue_ThrowsInvalidOperationException()
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Assert.Throws<InvalidOperationException>(() => queue2.Dequeue()); // Queue is empty
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void Dequeue_UntilEmpty(int count)
        {
            Queue queue1 = Helpers.CreateIntQueue(count);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                for (int i = 0; i < count; i++)
                {
                    queue2.Dequeue();
                }
                Assert.Throws<InvalidOperationException>(() => queue2.Dequeue());
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(10000)]
        public static void Enqueue(int count)
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                for (int i = 1; i <= count; i++)
                {
                    queue2.Enqueue(i);
                    Assert.Equal(i, queue2.Count);
                }
                Assert.Equal(count, queue2.Count);
            });
        }

        [Fact]
        public static void Enqueue_Null()
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                queue2.Enqueue(null);
                Assert.Equal(1, queue2.Count);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void GetEnumerator(int count)
        {
            var queue1 = Helpers.CreateIntQueue(count);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Assert.NotSame(queue2.GetEnumerator(), queue2.GetEnumerator());
                IEnumerator enumerator = queue2.GetEnumerator();
                for (int i = 0; i < 2; i++)
                {
                    int counter = 0;
                    while (enumerator.MoveNext())
                    {
                        Assert.Equal(counter, enumerator.Current);
                        counter++;
                    }
                    Assert.Equal(count, counter);
                    enumerator.Reset();
                }
            });
        }

        [Fact]
        public static void GetEnumerator_StartOfEnumeration_Clone()
        {
            Queue queue = Helpers.CreateIntQueue(10);

            IEnumerator enumerator = queue.GetEnumerator();
            ICloneable cloneableEnumerator = (ICloneable)enumerator;

            IEnumerator clonedEnumerator = (IEnumerator)cloneableEnumerator.Clone();
            Assert.NotSame(enumerator, clonedEnumerator);

            // Cloned and original enumerators should enumerate separately.
            Assert.True(enumerator.MoveNext());
            Assert.Equal(0, enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => clonedEnumerator.Current);

            Assert.True(clonedEnumerator.MoveNext());
            Assert.Equal(0, enumerator.Current);
            Assert.Equal(0, clonedEnumerator.Current);

            // Cloned and original enumerators should enumerate in the same sequence.
            for (int i = 1; i < queue.Count; i++)
            {
                Assert.True(enumerator.MoveNext());
                Assert.NotEqual(enumerator.Current, clonedEnumerator.Current);

                Assert.True(clonedEnumerator.MoveNext());
                Assert.Equal(enumerator.Current, clonedEnumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Equal(queue.Count - 1, clonedEnumerator.Current);

            Assert.False(clonedEnumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => clonedEnumerator.Current);
        }

        [Fact]
        public static void GetEnumerator_InMiddleOfEnumeration_Clone()
        {
            Queue queue = Helpers.CreateIntQueue(10);

            IEnumerator enumerator = queue.GetEnumerator();
            enumerator.MoveNext();
            ICloneable cloneableEnumerator = (ICloneable)enumerator;

            // Cloned and original enumerators should start at the same spot, even
            // if the original is in the middle of enumeration.
            IEnumerator clonedEnumerator = (IEnumerator)cloneableEnumerator.Clone();
            Assert.Equal(enumerator.Current, clonedEnumerator.Current);

            for (int i = 0; i < queue.Count - 1; i++)
            {
                Assert.True(clonedEnumerator.MoveNext());
            }

            Assert.False(clonedEnumerator.MoveNext());
        }

        [Fact]
        public static void GetEnumerator_Invalid()
        {
            var queue1 = Helpers.CreateIntQueue(100);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                IEnumerator enumerator = queue2.GetEnumerator();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // If the underlying collection is modified, MoveNext and Reset throw, but Current doesn't
                enumerator.MoveNext();
                object dequeued = queue2.Dequeue();
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                Assert.Equal(dequeued, enumerator.Current);


                // Current throws if the current index is < 0 or >= count
                enumerator = queue2.GetEnumerator();
                while (enumerator.MoveNext()) ;

                Assert.False(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());

                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Current throws after resetting
                enumerator = queue2.GetEnumerator();
                Assert.True(enumerator.MoveNext());
                Assert.True(enumerator.MoveNext());

                enumerator.Reset();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            });
        }

        [Fact]
        public static void Peek()
        {
            string s1 = "hello";
            string s2 = "world";
            char c = '\0';
            bool b = false;
            byte i8 = 0;
            short i16 = 0;
            int i32 = 0;
            long i64 = 0L;
            float f = (float)0.0;
            double d = 0.0;

            var queue1 = new Queue();
            queue1.Enqueue(s1);
            queue1.Enqueue(s2);
            queue1.Enqueue(c);
            queue1.Enqueue(b);
            queue1.Enqueue(i8);
            queue1.Enqueue(i16);
            queue1.Enqueue(i32);
            queue1.Enqueue(i64);
            queue1.Enqueue(f);
            queue1.Enqueue(d);

            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Assert.Same(s1, queue2.Peek());
                queue2.Dequeue();

                Assert.Same(s2, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(c, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(b, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(i8, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(i16, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(i32, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(i64, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(f, queue2.Peek());
                queue2.Dequeue();

                Assert.Equal(d, queue2.Peek());
                queue2.Dequeue();
            });
        }

        [Fact]
        public static void Peek_EmptyQueue_ThrowsInvalidOperationException()
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                Assert.Throws<InvalidOperationException>(() => queue2.Peek()); // Queue is empty
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void ToArray(int count)
        {
            Queue queue1 = Helpers.CreateIntQueue(count);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                object[] arr = queue2.ToArray();
                Assert.Equal(count, arr.Length);
                for (int i = 0; i < count; i++)
                {
                    Assert.Equal(queue2.Dequeue(), arr[i]);
                }
            });
        }

        [Fact]
        public static void ToArray_Wrapped()
        {
            var queue1 = new Queue(1);
            queue1.Enqueue(1);

            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                object[] arr = queue2.ToArray();
                Assert.Equal(1, arr.Length);
                Assert.Equal(1, arr[0]);
            });
        }

        [Fact]
        public static void ToArrayDiffentObjectTypes()
        {
            string s1 = "hello";
            string s2 = "world";
            char c = '\0';
            bool b = false;
            byte i8 = 0;
            short i16 = 0;
            int i32 = 0;
            long i64 = 0L;
            float f = (float)0.0;
            double d = 0.0;

            var queue1 = new Queue();
            queue1.Enqueue(s1);
            queue1.Enqueue(s2);
            queue1.Enqueue(c);
            queue1.Enqueue(b);
            queue1.Enqueue(i8);
            queue1.Enqueue(i16);
            queue1.Enqueue(i32);
            queue1.Enqueue(i64);
            queue1.Enqueue(f);
            queue1.Enqueue(d);

            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                object[] arr = queue2.ToArray();
                Assert.Same(s1, arr[0]);
                Assert.Same(s2, arr[1]);
                Assert.Equal(c, arr[2]);
                Assert.Equal(b, arr[3]);
                Assert.Equal(i8, arr[4]);
                Assert.Equal(i16, arr[5]);
                Assert.Equal(i32, arr[6]);
                Assert.Equal(i64, arr[7]);
                Assert.Equal(f, arr[8]);
                Assert.Equal(d, arr[9]);
            });
        }

        [Fact]
        public static void ToArray_EmptyQueue()
        {
            var queue1 = new Queue();
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                object[] arr = queue2.ToArray();
                Assert.Equal(0, arr.Length);
            });
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TrimToSize(int count)
        {
            Queue queue1 = Helpers.CreateIntQueue(count);
            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                queue2.TrimToSize();
                Assert.Equal(count, queue2.Count);

                // Can change the queue after trimming
                queue2.Enqueue(100);
                Assert.Equal(count + 1, queue2.Count);
                if (count == 0)
                {
                    Assert.Equal(100, queue2.Dequeue());
                }
                else
                {
                    Assert.Equal(0, queue2.Dequeue());
                }
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TrimToSize_DequeueAll(int count)
        {
            Queue queue1 = Helpers.CreateIntQueue(count);
            for (int i = 0; i < count; i++)
            {
                queue1.Dequeue();
            }

            Helpers.PerformActionOnAllQueueWrappers(queue1, queue2 =>
            {
                queue2.TrimToSize();
                Assert.Equal(0, queue2.Count);

                // Can change the queue after trimming
                queue2.Enqueue(1);
                Assert.Equal(1, queue2.Dequeue());
            });
        }
        
        [Fact]
        public static void TrimToSize_Wrapped()
        {
            var queue = new Queue(100);

            // Insert 50 items in the Queue
            for (int i = 0; i < 50; i++)
            {
                queue.Enqueue(i);
            }

            // Insert and Remove 75 items in the Queue. This should wrap the queue 
            // where there is 25 at the end of the array and 25 at the beginning
            for (int i = 0; i < 75; i++)
            {
                queue.Enqueue(i + 50);
                queue.Dequeue();
            }

            queue.TrimToSize();
            Assert.Equal(50, queue.Count);
            Assert.Equal(75, queue.Dequeue());

            queue.Enqueue(100);
            Assert.Equal(50, queue.Count);
            Assert.Equal(76, queue.Dequeue());
        }

        private class Foo
        {
            public Foo(int intValue)
            {
                IntValue = intValue;
            }
            
            public int IntValue { get; set; }
        }
    }

    public class Queue_SyncRootTests
    {
        private const int NumberOfElements = 1000;
        private const int NumberOfWorkers = 1000;

        private Queue _queueDaughter;
        private Queue _queueGrandDaughter;

        [Fact]
        public void SyncRoot()
        {
            var queueMother = new Queue();
            for (int i = 0; i < NumberOfElements; i++)
            {
                queueMother.Enqueue(i);
            }
            Assert.Equal(queueMother.SyncRoot.GetType(), typeof(object));

            var queueSon = Queue.Synchronized(queueMother);
            _queueGrandDaughter = Queue.Synchronized(queueSon);
            _queueDaughter = Queue.Synchronized(queueMother);

            Assert.Equal(queueMother.SyncRoot, queueSon.SyncRoot);
            Assert.Equal(queueSon.SyncRoot, queueMother.SyncRoot);

            Assert.Equal(queueMother.SyncRoot, _queueGrandDaughter.SyncRoot);
            Assert.Equal(queueMother.SyncRoot, _queueDaughter.SyncRoot);

            Task[] workers = new Task[NumberOfWorkers];
            Action action1 = SortElements;
            Action action2 = ReverseElements;
            for (int i = 0; i < NumberOfWorkers; i += 2)
            {
                workers[i] = Task.Run(action1);
                workers[i + 1] = Task.Run(action2);
            }

            Task.WaitAll(workers);
        }

        private void SortElements()
        {
            _queueGrandDaughter.Clear();
            for (int i = 0; i < NumberOfElements; i++)
            {
                _queueGrandDaughter.Enqueue(i);
            }
        }

        private void ReverseElements()
        {
            _queueDaughter.Clear();
            for (int i = 0; i < NumberOfElements; i++)
            {
                _queueDaughter.Enqueue(i);
            }
        }
    }

    public class Queue_SynchronizedTests
    {
        public Queue _queue;
        public int _threadsToUse = 8;
        public int _threadAge = 5; // 5000;

        public int _threadCount;

        [Fact]
        public static void Synchronized()
        {
            Queue queue = Helpers.CreateIntQueue(100);
            Queue syncQueue = Queue.Synchronized(queue);

            Assert.True(syncQueue.IsSynchronized);
            Assert.Equal(queue.Count, syncQueue.Count);
            for (int i = 0; i < queue.Count; i++)
            {
                Assert.True(syncQueue.Contains(i));
            }
        }

        [Fact]
        public void SynchronizedEnqueue()
        {
            // Enqueue
            _queue = Queue.Synchronized(new Queue());
            PerformTest(StartEnqueueThread, 40);

            // Dequeue
            Queue queue = Helpers.CreateIntQueue(_threadAge);
            _queue = Queue.Synchronized(queue);
            PerformTest(StartDequeueThread, 0);

            // Enqueue, dequeue
            _queue = Queue.Synchronized(new Queue());
            PerformTest(StartEnqueueDequeueThread, 0);

            // Dequeue, enqueue
            queue = Helpers.CreateIntQueue(_threadsToUse);
            _queue = Queue.Synchronized(queue);
            PerformTest(StartDequeueEnqueueThread, _threadsToUse);
        }

        private void PerformTest(Action action, int expected)
        {
            var tasks = new Task[_threadsToUse];

            for (int i = 0; i < _threadsToUse; i++)
            {
                tasks[i] = Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            _threadCount = _threadsToUse;
            Task.WaitAll(tasks);

            Assert.Equal(expected, _queue.Count);
        }

        [Fact]
        public static void Synchronized_NullQueue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("queue", () => Queue.Synchronized(null)); // Queue is null
        }

        public void StartEnqueueThread()
        {
            int t_age = _threadAge;
            while (t_age > 0)
            {
                _queue.Enqueue(t_age);
                Interlocked.Decrement(ref t_age);
            }
            Interlocked.Decrement(ref _threadCount);
        }

        private void StartDequeueThread()
        {
            int t_age = _threadAge;
            while (t_age > 0)
            {
                try
                {
                    _queue.Dequeue();
                }
                catch { }
                Interlocked.Decrement(ref t_age);
            }
            Interlocked.Decrement(ref _threadCount);
        }

        private void StartEnqueueDequeueThread()
        {
            int t_age = _threadAge;
            while (t_age > 0)
            {
                _queue.Enqueue(2);
                _queue.Dequeue();
                Interlocked.Decrement(ref t_age);
            }
            Interlocked.Decrement(ref _threadCount);
        }

        private void StartDequeueEnqueueThread()
        {
            int t_age = _threadAge;
            while (t_age > 0)
            {
                _queue.Dequeue();
                _queue.Enqueue(2);
                Interlocked.Decrement(ref t_age);
            }
            Interlocked.Decrement(ref _threadCount);
        }
    }
}
