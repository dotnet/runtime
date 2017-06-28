// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Tests
{
    public static class StackTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var stack = new Stack();
            Assert.Equal(0, stack.Count);
            Assert.False(stack.IsSynchronized);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void Ctor_Int(int initialCapacity)
        {
            var stack = new Stack(initialCapacity);
            Assert.Equal(0, stack.Count);
            Assert.False(stack.IsSynchronized);
        }

        [Fact]
        public static void Ctor_Int_NegativeInitialCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCapacity", () => new Stack(-1)); // InitialCapacity < 0
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public static void Ctor_ICollection(int count)
        {
            var array = new object[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = i;
            }

            var stack = new Stack(array);
            Assert.Equal(count, stack.Count);
            Assert.False(stack.IsSynchronized);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(count - i - 1, stack.Pop());
            }
        }

        [Fact]
        public static void Ctor_ICollection_NullCollection_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("col", () => new Stack(null)); // Collection is null
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new Stack());

            var stack = new Stack();
            stack.Push("a");
            stack.Push(1);
            stack.Push("b");
            stack.Push(2);

            DebuggerAttributeInfo debuggerAttribute = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(stack);
            PropertyInfo infoProperty = debuggerAttribute.Properties.Single(property => property.Name == "Items");
            object[] items = (object[])infoProperty.GetValue(debuggerAttribute.Instance);

            Assert.Equal(stack.ToArray(), items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute_NullStack_ThrowsArgumentNullException()
        {
            bool threwNull = false;
            try
            {
                DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(Stack), null);
            }
            catch (TargetInvocationException ex)
            {
                threwNull = ex.InnerException is ArgumentNullException;
            }

            Assert.True(threwNull);
        }

        [Fact]
        public static void Clear()
        {
            Stack stack1 = Helpers.CreateIntStack(100);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                stack2.Clear();
                Assert.Equal(0, stack2.Count);

                stack2.Clear();
                Assert.Equal(0, stack2.Count);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void Clone(int count)
        {
            Stack stack1 = Helpers.CreateIntStack(count);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                Stack stackClone = (Stack)stack2.Clone();

                Assert.Equal(stack2.Count, stackClone.Count);
                Assert.Equal(stack2.IsSynchronized, stackClone.IsSynchronized);

                for (int i = 0; i < stackClone.Count; i++)
                {
                    Assert.Equal(stack2.Pop(), stackClone.Pop());
                }
            });
        }

        [Fact]
        public static void Clone_IsShallowCopy()
        {
            var stack1 = new Stack();
            stack1.Push(new Foo(10));
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                Stack stackClone = (Stack)stack2.Clone();

                Foo a1 = (Foo)stack2.Pop();
                a1.IntValue = 50;

                Foo a2 = (Foo)stackClone.Pop();
                Assert.Equal(50, a1.IntValue);
            });
        }

        [Fact]
        public static void Contains()
        {
            Stack stack1 = Helpers.CreateIntStack(100);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                for (int i = 0; i < stack2.Count; i++)
                {
                    Assert.True(stack2.Contains(i));
                }

                Assert.False(stack2.Contains(101));
                Assert.False(stack2.Contains("hello"));
                Assert.False(stack2.Contains(null));

                stack2.Push(null);
                Assert.True(stack2.Contains(null));

                Assert.False(stack2.Contains(-1)); // We have a null item in the list, so the algorithm may use a different branch 
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(10, 0)]
        [InlineData(100, 0)]
        [InlineData(1000, 0)]
        [InlineData(10, 5)]
        [InlineData(100, 50)]
        [InlineData(1000, 500)]
        public static void CopyTo_ObjectArray(int count, int index)
        {
            Stack stack1 = Helpers.CreateIntStack(count);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                object[] oArray = new object[index + count];
                stack2.CopyTo(oArray, index);

                Assert.Equal(index + count, oArray.Length);
                for (int i = index; i < count; i++)
                {
                    Assert.Equal(stack2.Pop(), oArray[i]);
                }
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(10, 0)]
        [InlineData(100, 0)]
        [InlineData(1000, 0)]
        [InlineData(10, 5)]
        [InlineData(100, 50)]
        [InlineData(1000, 500)]
        public static void CopyTo_IntArray(int count, int index)
        {
            Stack stack1 = Helpers.CreateIntStack(count);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                int[] iArray = new int[index + count];
                stack2.CopyTo(iArray, index);
                Assert.Equal(index + count, iArray.Length);
                for (int i = index; i < count; i++)
                {
                    Assert.Equal(stack2.Pop(), iArray[i]);
                }
            });
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            Stack stack1 = Helpers.CreateIntStack(100);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                AssertExtensions.Throws<ArgumentNullException>("array", () => stack2.CopyTo(null, 0)); // Array is null
                AssertExtensions.Throws<ArgumentException>("array", null, () => stack2.CopyTo(new object[10, 10], 0)); // Array is multidimensional

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => stack2.CopyTo(new object[100], -1)); // Index < 0
                AssertExtensions.Throws<ArgumentException>(null, () => stack2.CopyTo(new object[0], 0)); // Index >= array.Count
                AssertExtensions.Throws<ArgumentException>(null, () => stack2.CopyTo(new object[100], 1)); // Index + array.Count > stack.Count
                AssertExtensions.Throws<ArgumentException>(null, () => stack2.CopyTo(new object[150], 51)); // Index + array.Count > stack.Count
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void GetEnumerator(int count)
        {
            Stack stack1 = Helpers.CreateIntStack(count);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                Assert.NotSame(stack2.GetEnumerator(), stack2.GetEnumerator());
                IEnumerator enumerator = stack2.GetEnumerator();
                for (int i = 0; i < 2; i++)
                {
                    int counter = 0;
                    while (enumerator.MoveNext())
                    {
                        counter++;
                        Assert.NotNull(enumerator.Current);
                    }
                    Assert.Equal(count, counter);
                    enumerator.Reset();
                }
            });
        }

        [Fact]
        public static void GetEnumerator_StartOfEnumeration_Clone()
        {
            Stack stack = Helpers.CreateIntStack(10);

            IEnumerator enumerator = stack.GetEnumerator();
            ICloneable cloneableEnumerator = (ICloneable)enumerator;

            IEnumerator clonedEnumerator = (IEnumerator)cloneableEnumerator.Clone();
            Assert.NotSame(enumerator, clonedEnumerator);

            // Cloned and original enumerators should enumerate separately.
            Assert.True(enumerator.MoveNext());
            Assert.Equal(stack.Count - 1, enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => clonedEnumerator.Current);

            Assert.True(clonedEnumerator.MoveNext());
            Assert.Equal(stack.Count - 1, enumerator.Current);
            Assert.Equal(stack.Count - 1, clonedEnumerator.Current);

            // Cloned and original enumerators should enumerate in the same sequence.
            for (int i = 1; i < stack.Count; i++)
            {
                Assert.True(enumerator.MoveNext());
                Assert.NotEqual(enumerator.Current, clonedEnumerator.Current);

                Assert.True(clonedEnumerator.MoveNext());
                Assert.Equal(enumerator.Current, clonedEnumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Equal(0, clonedEnumerator.Current);

            Assert.False(clonedEnumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => clonedEnumerator.Current);
        }

        [Fact]
        public static void GetEnumerator_InMiddleOfEnumeration_Clone()
        {
            Stack stack = Helpers.CreateIntStack(10);

            IEnumerator enumerator = stack.GetEnumerator();
            enumerator.MoveNext();
            ICloneable cloneableEnumerator = (ICloneable)enumerator;

            // Cloned and original enumerators should start at the same spot, even
            // if the original is in the middle of enumeration.
            IEnumerator clonedEnumerator = (IEnumerator)cloneableEnumerator.Clone();
            Assert.Equal(enumerator.Current, clonedEnumerator.Current);

            for (int i = 0; i < stack.Count - 1; i++)
            {
                Assert.True(clonedEnumerator.MoveNext());
            }

            Assert.False(clonedEnumerator.MoveNext());
        }

        [Fact]
        public static void GetEnumerator_Invalid()
        {
            Stack stack1 = Helpers.CreateIntStack(100);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                IEnumerator enumerator = stack2.GetEnumerator();

                // Index < 0
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Index > dictionary.Count
                while (enumerator.MoveNext()) ;
                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Current throws after resetting
                enumerator.Reset();
                Assert.True(enumerator.MoveNext());

                enumerator.Reset();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // MoveNext and Reset throws after stack is modified, but current doesn't
                enumerator = stack2.GetEnumerator();
                enumerator.MoveNext();
                stack2.Push("hi");
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                Assert.NotNull(enumerator.Current);

                enumerator = stack2.GetEnumerator();
                enumerator.MoveNext();
                stack2.Pop();
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                Assert.NotNull(enumerator.Current);
            });
        }

        [Fact]
        public static void Peek()
        {
            int count = 100;
            Stack stack1 = Helpers.CreateIntStack(count);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                for (int i = 0; i < count; i++)
                {
                    int peek1 = (int)stack2.Peek();
                    int peek2 = (int)stack2.Peek();

                    Assert.Equal(peek1, peek2);
                    Assert.Equal(stack2.Pop(), peek1);
                    Assert.Equal(count - i - 1, peek1);
                }
            });
        }

        [Fact]
        public static void Peek_EmptyStack_ThrowsInvalidOperationException()
        {
            var stack1 = new Stack();
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                Assert.Throws<InvalidOperationException>(() => stack2.Peek()); // Empty stack

                for (int i = 0; i < 1000; i++)
                {
                    stack2.Push(i);
                }

                for (int i = 0; i < 1000; i++)
                {
                    stack2.Pop();
                }

                Assert.Throws<InvalidOperationException>(() => stack2.Peek()); // Empty stack
            });
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 100)]
        [InlineData(1000, 1000)]
        [InlineData(10, 5)]
        [InlineData(100, 50)]
        [InlineData(1000, 500)]
        public static void Pop(int pushCount, int popCount)
        {
            Stack stack1 = Helpers.CreateIntStack(pushCount);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                for (int i = 0; i < popCount; i++)
                {
                    Assert.Equal(pushCount - i - 1, stack2.Pop());
                    Assert.Equal(pushCount - i - 1, stack2.Count);
                }
                Assert.Equal(pushCount - popCount, stack2.Count);
            });
        }

        [Fact]
        public static void Pop_Null()
        {
            var stack1 = new Stack();
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                stack2.Push(null);
                stack2.Push(-1);
                stack2.Push(null);

                Assert.Equal(null, stack2.Pop());
                Assert.Equal(-1, stack2.Pop());
                Assert.Equal(null, stack2.Pop());
            });
        }

        [Fact]
        public static void Pop_EmptyStack_ThrowsInvalidOperationException()
        {
            var stack1 = new Stack();
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                Assert.Throws<InvalidOperationException>(() => stack2.Pop()); // Empty stack
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void Push(int count)
        {
            var stack1 = new Stack();
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                for (int i = 0; i < count; i++)
                {
                    stack2.Push(i);
                    Assert.Equal(i + 1, stack2.Count);
                }
                Assert.Equal(count, stack2.Count);
            });
        }

        [Fact]
        public static void Push_Null()
        {
            var stack1 = new Stack();
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                stack2.Push(null);
                stack2.Push(-1);
                stack2.Push(null);

                Assert.True(stack2.Contains(null));
                Assert.True(stack2.Contains(-1));
            });
        }

        [Fact]
        public static void Synchronized_IsSynchronized()
        {
            Stack stack = Stack.Synchronized(new Stack());
            Assert.True(stack.IsSynchronized);
        }

        [Fact]
        public static void Synchronized_NullStack_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("stack", () => Stack.Synchronized(null)); // Stack is null
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void ToArray(int count)
        {
            Stack stack1 = Helpers.CreateIntStack(count);
            Helpers.PerformActionOnAllStackWrappers(stack1, stack2 =>
            {
                object[] array = stack2.ToArray();

                Assert.Equal(stack2.Count, array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    Assert.Equal(stack2.Pop(), array[i]);
                }
            });
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

    public class Stack_SyncRootTests
    {
        private Stack _stackDaughter;
        private Stack _stackGrandDaughter;

        private const int NumberOfElements = 100;

        [Fact]
        public void GetSyncRootBasic()
        {
            // Testing SyncRoot is not as simple as its implementation looks like. This is the working
            // scenario we have in mind.
            // 1) Create your Down to earth mother Stack
            // 2) Get a Fixed wrapper from it
            // 3) Get a Synchronized wrapper from 2)
            // 4) Get a synchronized wrapper of the mother from 1)
            // 5) all of these should SyncRoot to the mother earth
            var stackMother = new Stack();
            for (int i = 0; i < NumberOfElements; i++)
            {
                stackMother.Push(i);
            }

            Assert.Equal(typeof(object), stackMother.SyncRoot.GetType());

            Stack stackSon = Stack.Synchronized(stackMother);
            _stackGrandDaughter = Stack.Synchronized(stackSon);
            _stackDaughter = Stack.Synchronized(stackMother);

            Assert.Equal(stackSon.SyncRoot, stackMother.SyncRoot);
            Assert.Equal(_stackGrandDaughter.SyncRoot, stackMother.SyncRoot);
            Assert.Equal(_stackDaughter.SyncRoot, stackMother.SyncRoot);
            Assert.Equal(stackSon.SyncRoot, stackMother.SyncRoot);

            // We are going to rumble with the Stacks with 2 threads
            Action ts1 = SortElements;
            Action ts2 = ReverseElements;
            var tasks = new Task[4];
            for (int iThreads = 0; iThreads < tasks.Length; iThreads += 2)
            {
                tasks[iThreads] = Task.Run(ts1);
                tasks[iThreads + 1] = Task.Run(ts2);
            }

            Task.WaitAll(tasks);
        }

        private void SortElements()
        {
            _stackGrandDaughter.Clear();
            for (int i = 0; i < NumberOfElements; i++)
            {
                _stackGrandDaughter.Push(i);
            }
        }

        private void ReverseElements()
        {
            _stackDaughter.Clear();
            for (int i = 0; i < NumberOfElements; i++)
            {
                _stackDaughter.Push(i);
            }
        }
    }
}
