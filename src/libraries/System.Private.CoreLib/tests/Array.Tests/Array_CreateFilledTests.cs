// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Array.Tests
{
    public class Array_CreateFilledTests
    {
        private class Foo
        {
            public Foo(int value)
            {
                Value = value;
            }

            public int Value { get; set; }
        }

        [Fact]
        public void CreateFilled_Factory_ValueType()
        {
            var array = System.Array.CreateFilled<int>(10, index => index);
            Assert.Equal(10, array.Length);
            Assert.Equal(array, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        }

        [Fact]
        public void CreateFilled_Empty_ValueType()
        {
            var array = System.Array.CreateFilled<int>(0, index => index);
            Assert.Empty(array);
        }

        [Fact]
        public void CreateFilled_Factory_ReferenceType()
        {
            var array = System.Array.CreateFilled<Foo>(10, index => new Foo(index));
            var expected = new Foo[10];
            for (int i = 0; i < 10; i++)
                expected[i] = new Foo(i);

            Assert.Equal(10, array.Length);
            Assert.Equal(expected, array);
        }

        [Fact]
        public void CreateFilled_Empty_ReferenceType()
        {
            var array = System.Array.CreateFilled<Foo>(0, index => new Foo(index));
            Assert.Empty(array);
        }


        public void CreateFilled_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => System.Array.CreateFilled<Foo>(7, null));
        }

        [Fact]
        public void CreateFilled_NegativeLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => System.Array.CreateFilled<int>(-1, index => index));
            Assert.Throws<ArgumentOutOfRangeException>(() => System.Array.CreateFilled<object>(-1, index => new object()));
        }

    }
}
