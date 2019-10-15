// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class AmbientValueAttributeTests
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData(typeof(int*), "1", null)]
        [InlineData(typeof(string), "1", "1")]
        [InlineData(typeof(int), "1", 1)]
        public void Ctor_Type_Value(Type type, string value, object expectedValue)
        {
            var attribute = new AmbientValueAttribute(type, value);
            Assert.Equal(expectedValue, attribute.Value);
        }

        [Fact]
        public void Ctor_Char()
        {
            char value = 'a';
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Fact]
        public void Ctor_Byte()
        {
            byte value = 123;
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Fact]
        public void Ctor_Short()
        {
            short value = 123;
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Fact]
        public void Ctor_Int()
        {
            int value = 123;
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Fact]
        public void Ctor_Long()
        {
            long value = 123;
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Fact]
        public void Ctor_Float()
        {
            float value = 123;
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Fact]
        public void Ctor_Double()
        {
            double value = 123;
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Fact]
        public void Ctor_Bool()
        {
            bool value = true;
            var args = new AmbientValueAttribute(value);
            Assert.Equal(value, args.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Value")]
        public void Ctor_String(string value)
        {
            var args = new AmbientValueAttribute(value);
            Assert.Same(value, args.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Value")]
        public void Ctor_Object(object value)
        {
            var args = new AmbientValueAttribute(value);
            Assert.Same(value, args.Value);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new AmbientValueAttribute(true);
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { new AmbientValueAttribute(true), new AmbientValueAttribute(true), true };
            yield return new object[] { new AmbientValueAttribute(true), new AmbientValueAttribute(false), false };
            yield return new object[] { new AmbientValueAttribute(true), new AmbientValueAttribute(null), false };
            yield return new object[] { new AmbientValueAttribute(null), new AmbientValueAttribute(false), false };
            yield return new object[] { new AmbientValueAttribute(null), new AmbientValueAttribute(null), true };

            yield return new object[] { new AmbientValueAttribute(true), new object(), false };
            yield return new object[] { new AmbientValueAttribute(true), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(AmbientValueAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsConsistentValue()
        {
            var attribute = new AmbientValueAttribute(null);
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }
    }
}
