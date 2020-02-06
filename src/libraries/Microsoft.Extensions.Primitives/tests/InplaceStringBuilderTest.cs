// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;
using Xunit;

// InplaceStringBuilder is obsolete
#pragma warning disable CS0618

namespace Microsoft.Extensions.Primitives
{
    public class InplaceStringBuilderTest
    {
        [Fact]
        public void Ctor_ThrowsIfCapacityIsNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InplaceStringBuilder(-1));
        }

        [Fact]
        public void ToString_ReturnsStringWithAllAppendedValues()
        {
            var s1 = "123";
            var c1 = '4';
            var s2 = "56789";
            var seg = new StringSegment("890123", 2, 2);

            var formatter = new InplaceStringBuilder();
            formatter.Capacity += s1.Length + 1 + s2.Length + seg.Length;
            formatter.Append(s1);
            formatter.Append(c1);
            formatter.Append(s2, 0, 2);
            formatter.Append(s2, 2, 2);
            formatter.Append(s2, 4, 1);
            formatter.Append(seg);
            Assert.Equal("12345678901", formatter.ToString());
        }

        [Fact]
        public void ToString_ThrowsIfCapacityNotUsed()
        {
            var formatter = new InplaceStringBuilder(10);

            formatter.Append("abc");

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.ToString());
            Assert.Equal("Entire reserved capacity was not used. Capacity: '10', written '3'.", exception.Message);
        }

        [Fact]
        public void Build_ThrowsIfNotEnoughWritten()
        {
            var formatter = new InplaceStringBuilder(5);
            formatter.Append("123");
            var exception = Assert.Throws<InvalidOperationException>(() => formatter.ToString());
            Assert.Equal("Entire reserved capacity was not used. Capacity: '5', written '3'.", exception.Message);
        }

        [Fact]
        public void Capacity_ThrowsIfAppendWasCalled()
        {
            var formatter = new InplaceStringBuilder(3);
            formatter.Append("123");

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Capacity = 5);
            Assert.Equal("Cannot change capacity after write started.", exception.Message);
        }

        [Fact]
        public void Capacity_ThrowsIfNegativeValueSet()
        {
            var formatter = new InplaceStringBuilder(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => formatter.Capacity = -1);
        }

        [Fact]
        public void Capacity_GetReturnsCorrectValue()
        {
            var formatter = new InplaceStringBuilder(3);
            Assert.Equal(3, formatter.Capacity);

            formatter.Capacity = 10;
            Assert.Equal(10, formatter.Capacity);

            formatter.Append("abc");
            Assert.Equal(10, formatter.Capacity);
        }

        [Fact]
        public void Append_ThrowsIfValueIsNull()
        {
            var formatter = new InplaceStringBuilder(3);

            Assert.Throws<ArgumentNullException>(() => formatter.Append(null as string));
        }

        [Fact]
        public void Append_ThrowsIfValueIsNullInOverloadWithIndex()
        {
            var formatter = new InplaceStringBuilder(3);

            Assert.Throws<ArgumentNullException>(() => formatter.Append(null as string, 0, 3));
        }

        [Fact]
        public void Append_ThrowsIfOffsetIsNegative()
        {
            var formatter = new InplaceStringBuilder(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => formatter.Append("abc", -1, 3));
        }

        [Fact]
        public void Append_ThrowIfValueLenghtMinusOffsetSmallerThanCount()
        {
            var formatter = new InplaceStringBuilder(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => formatter.Append("abc", 1, 3));
        }

        [Fact]
        public void Append_ThrowsIfNotEnoughCapacity()
        {
            var formatter = new InplaceStringBuilder(1);

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Append("123"));
            Assert.Equal("Not enough capacity to write '3' characters, only '1' left.", exception.Message);
        }

        [Fact]
        public void Append_ThrowsWhenNoCapacityIsSet()
        {
            var formatter = new InplaceStringBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Append("123"));
            Assert.Equal("Not enough capacity to write '3' characters, only '0' left.", exception.Message);
        }
    }
}

#pragma warning restore CS0618