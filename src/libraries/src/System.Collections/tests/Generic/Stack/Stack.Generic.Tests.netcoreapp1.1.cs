// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class Stack_Generic_Tests<T> : IGenericSharedAPI_Tests<T>
    {       
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Stack_Generic_TryPop_AllElements(int count)
        {
            Stack<T> stack = GenericStackFactory(count);
            List<T> elements = stack.ToList();
            foreach (T element in elements)
            {
                T result;
                Assert.True(stack.TryPop(out result));
                Assert.Equal(element, result);
            }
        }

        [Fact]
        public void Stack_Generic_TryPop_EmptyStack_ReturnsFalse()
        {
            T result;
            Assert.False(new Stack<T>().TryPop(out result));
            Assert.Equal(default(T), result);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Stack_Generic_TryPeek_AllElements(int count)
        {
            Stack<T> stack = GenericStackFactory(count);
            List<T> elements = stack.ToList();
            foreach (T element in elements)
            {
                T result;
                Assert.True(stack.TryPeek(out result));
                Assert.Equal(element, result);

                stack.Pop();
            }
        }

        [Fact]
        public void Stack_Generic_TryPeek_EmptyStack_ReturnsFalse()
        {
            T result;
            Assert.False(new Stack<T>().TryPeek(out result));
            Assert.Equal(default(T), result);
        }
    }
}
