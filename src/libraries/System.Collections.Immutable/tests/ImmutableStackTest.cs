// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableStackTest : SimpleElementImmutablesTestBase
    {
        /// <summary>
        /// A test for Empty
        /// </summary>
        /// <typeparam name="T">The type of elements held in the stack.</typeparam>
        private void EmptyTestHelper<T>() where T : new()
        {
            IImmutableStack<T> actual = ImmutableStack<T>.Empty;
            Assert.NotNull(actual);
            Assert.True(actual.IsEmpty);
            AssertAreSame(ImmutableStack<T>.Empty, actual.Clear());
            AssertAreSame(ImmutableStack<T>.Empty, actual.Push(new T()).Clear());
        }

        private ImmutableStack<T> InitStackHelper<T>(params T[] values)
        {
            Assert.NotNull(values);

            ImmutableStack<T> result = ImmutableStack<T>.Empty;
            foreach (T value in values)
            {
                result = result.Push(value);
            }

            return result;
        }

        private void PushAndCountTestHelper<T>() where T : new()
        {
            ImmutableStack<T> actual0 = ImmutableStack<T>.Empty;
            Assert.Equal(0, actual0.Count());
            ImmutableStack<T> actual1 = actual0.Push(new T());
            Assert.Equal(1, actual1.Count());
            Assert.Equal(0, actual0.Count());
            ImmutableStack<T> actual2 = actual1.Push(new T());
            Assert.Equal(2, actual2.Count());
            Assert.Equal(0, actual0.Count());
        }

        private void PopTestHelper<T>(params T[] values)
        {
            Assert.NotNull(values);
            Assert.InRange(values.Length, 1, int.MaxValue);

            ImmutableStack<T> full = this.InitStackHelper(values);
            ImmutableStack<T> currentStack = full;

            // This loop tests the immutable properties of Pop.
            for (int expectedCount = values.Length; expectedCount > 0; expectedCount--)
            {
                Assert.Equal(expectedCount, currentStack.Count());
                currentStack.Pop();
                Assert.Equal(expectedCount, currentStack.Count());
                ImmutableStack<T> nextStack = currentStack.Pop();
                Assert.Equal(expectedCount, currentStack.Count());
                Assert.NotSame(currentStack, nextStack);
                AssertAreSame(currentStack.Pop(), currentStack.Pop());
                currentStack = nextStack;
            }
        }

        private void PeekTestHelper<T>(params T[] values)
        {
            Assert.NotNull(values);
            Assert.InRange(values.Length, 1, int.MaxValue);

            ImmutableStack<T> current = this.InitStackHelper(values);
            for (int i = values.Length - 1; i >= 0; i--)
            {
                AssertAreSame(values[i], current.Peek());
                T element;
                current.Pop(out element);
                AssertAreSame(current.Peek(), element);
                ImmutableStack<T> next = current.Pop();
                AssertAreSame(values[i], current.Peek());
                current = next;
            }
        }

        private void EnumeratorTestHelper<T>(params T[] values)
        {
            ImmutableStack<T> full = this.InitStackHelper(values);

            int i = values.Length - 1;
            foreach (T element in full)
            {
                AssertAreSame(values[i--], element);
            }

            Assert.Equal(-1, i);

            i = values.Length - 1;
            foreach (T element in (System.Collections.IEnumerable)full)
            {
                AssertAreSame(values[i--], element);
            }

            Assert.Equal(-1, i);
        }

        [Fact]
        public void EmptyTest()
        {
            this.EmptyTestHelper<GenericParameterHelper>();
            this.EmptyTestHelper<int>();
        }

        [Fact]
        public void PushAndCountTest()
        {
            this.PushAndCountTestHelper<GenericParameterHelper>();
            this.PushAndCountTestHelper<int>();
        }

        [Fact]
        public void PopTest()
        {
            this.PopTestHelper(
                new GenericParameterHelper(1),
                new GenericParameterHelper(2),
                new GenericParameterHelper(3));
            this.PopTestHelper(1, 2, 3);
        }

        [Fact]
        public void PopOutValue()
        {
            ImmutableStack<int> stack = ImmutableStack<int>.Empty.Push(5).Push(6);
            int top;
            stack = stack.Pop(out top);
            Assert.Equal(6, top);
            ImmutableStack<int> empty = stack.Pop(out top);
            Assert.Equal(5, top);
            Assert.True(empty.IsEmpty);

            // Try again with the interface to verify extension method behavior.
            IImmutableStack<int> stackInterface = stack;
            Assert.Same(empty, stackInterface.Pop(out top));
            Assert.Equal(5, top);
        }

        [Fact]
        public void PeekTest()
        {
            this.PeekTestHelper(
                new GenericParameterHelper(1),
                new GenericParameterHelper(2),
                new GenericParameterHelper(3));
            this.PeekTestHelper(1, 2, 3);
        }

        [Fact]
        public void EnumeratorTest()
        {
            this.EnumeratorTestHelper(new GenericParameterHelper(1), new GenericParameterHelper(2));
            this.EnumeratorTestHelper<GenericParameterHelper>();

            this.EnumeratorTestHelper(1, 2);
            this.EnumeratorTestHelper<int>();

            ImmutableStack<int> stack = ImmutableStack.Create(5);
            ImmutableStack<int>.Enumerator enumeratorStruct = stack.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumeratorStruct.Current);
            Assert.True(enumeratorStruct.MoveNext());
            Assert.Equal(5, enumeratorStruct.Current);
            Assert.False(enumeratorStruct.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumeratorStruct.Current);
            Assert.False(enumeratorStruct.MoveNext());

            IEnumerator<int> enumerator = ((IEnumerable<int>)stack).GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(5, enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.False(enumerator.MoveNext());

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(5, enumerator.Current);
            Assert.False(enumerator.MoveNext());
            enumerator.Dispose();

            Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        }

        [Fact]
        public void EqualityTest()
        {
            Assert.False(ImmutableStack<int>.Empty.Equals(null));
            Assert.False(ImmutableStack<int>.Empty.Equals("hi"));
            Assert.Equal(ImmutableStack<int>.Empty, ImmutableStack<int>.Empty);
            Assert.Equal(ImmutableStack<int>.Empty.Push(3), ImmutableStack<int>.Empty.Push(3));
            Assert.NotEqual(ImmutableStack<int>.Empty.Push(5), ImmutableStack<int>.Empty.Push(3));
            Assert.NotEqual(ImmutableStack<int>.Empty.Push(3).Push(5), ImmutableStack<int>.Empty.Push(3));
            Assert.NotEqual(ImmutableStack<int>.Empty.Push(3), ImmutableStack<int>.Empty.Push(3).Push(5));
        }

        [Fact]
        public void GetEnumerator_EmptyStackMoveNext_ReturnsFalse()
        {
            ImmutableStack<int> stack = ImmutableStack<int>.Empty;
            Assert.False(stack.GetEnumerator().MoveNext());
        }

        [Fact]
        public void EmptyPeekThrows()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableStack<GenericParameterHelper>.Empty.Peek());
        }

        [Fact]
        public void EmptyPopThrows()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableStack<GenericParameterHelper>.Empty.Pop());
        }

        [Fact]
        public void Create()
        {
            ImmutableStack<int> stack = ImmutableStack.Create<int>();
            Assert.True(stack.IsEmpty);

            stack = ImmutableStack.Create(1);
            Assert.False(stack.IsEmpty);
            Assert.Equal(new[] { 1 }, stack);

            stack = ImmutableStack.Create(new[] { 1, 2 });
            Assert.False(stack.IsEmpty);
            Assert.Equal(new[] { 2, 1 }, stack);

            stack = ImmutableStack.Create((ReadOnlySpan<int>)new[] { 1, 2 });
            Assert.False(stack.IsEmpty);
            Assert.Equal(new[] { 2, 1 }, stack);

            stack = ImmutableStack.CreateRange((IEnumerable<int>)new[] { 1, 2 });
            Assert.False(stack.IsEmpty);
            Assert.Equal(new[] { 2, 1 }, stack);

            AssertExtensions.Throws<ArgumentNullException>("items", () => ImmutableStack.CreateRange((IEnumerable<int>)null));
            AssertExtensions.Throws<ArgumentNullException>("items", () => ImmutableStack.Create((int[])null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableStack.Create<int>());
            ImmutableStack<string> stack = ImmutableStack.Create<string>("1", "2", "3");
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(stack);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            string[] items = itemProperty.GetValue(info.Instance) as string[];
            Assert.Equal(stack, items);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableStack.Create<string>("1", "2", "3"));
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void PeekRef()
        {
            ImmutableStack<int> stack = ImmutableStack<int>.Empty
                .Push(1)
                .Push(2)
                .Push(3);

            ref readonly int safeRef = ref stack.PeekRef();
            ref int unsafeRef = ref Unsafe.AsRef(in safeRef);

            Assert.Equal(3, stack.PeekRef());

            unsafeRef = 4;

            Assert.Equal(4, stack.PeekRef());
        }

        [Fact]
        public void PeekRef_Empty()
        {
            ImmutableStack<int> stack = ImmutableStack<int>.Empty;

            Assert.Throws<InvalidOperationException>(() => stack.PeekRef());
        }

        protected override IEnumerable<T> GetEnumerableOf<T>(params T[] contents)
        {
            ImmutableStack<T> stack = ImmutableStack<T>.Empty;
            foreach (T value in contents.Reverse())
            {
                stack = stack.Push(value);
            }

            return stack;
        }
    }
}
