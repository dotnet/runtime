// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Xunit;

namespace System.Runtime.Tests
{
    public static class DefaultValueAttributeTests
    {
        [Fact]
        public static void TestCtor()
        {
            Assert.Equal(true, new DefaultValueAttribute(true).Value);
            Assert.Equal(false, new DefaultValueAttribute(false).Value);
            Assert.Equal((byte)1, new DefaultValueAttribute((byte)1).Value);
            Assert.Equal('c', new DefaultValueAttribute('c').Value);
            Assert.Equal(3.14, new DefaultValueAttribute(3.14).Value);
            Assert.Equal(3.14f, new DefaultValueAttribute(3.14f).Value);
            Assert.Equal(42, new DefaultValueAttribute(42).Value);
            Assert.Equal(42L, new DefaultValueAttribute(42L).Value);
            Assert.Equal("test", new DefaultValueAttribute((object)"test").Value);
            Assert.Equal((short)42, new DefaultValueAttribute((short)42).Value);
            Assert.Equal("test", new DefaultValueAttribute("test").Value);
            Assert.Equal(DayOfWeek.Monday, new DefaultValueAttribute(typeof(DayOfWeek), "Monday").Value);
            Assert.Equal(TimeSpan.FromHours(1), new DefaultValueAttribute(typeof(TimeSpan), "1:00:00").Value);
            Assert.Equal(42, new DefaultValueAttribute(typeof(int), "42").Value);
            Assert.Equal(null, new DefaultValueAttribute(typeof(int), "caughtException").Value);
        }

        [Fact]
        public static void TestEqual()
        {
            DefaultValueAttribute attr;

            attr = new DefaultValueAttribute(42);
            Assert.Equal(attr, attr);
            Assert.True(attr.Equals(attr));
            Assert.Equal(attr.GetHashCode(), attr.GetHashCode());
            Assert.Equal(attr, new DefaultValueAttribute(42));
            Assert.Equal(attr.GetHashCode(), new DefaultValueAttribute(42).GetHashCode());
            Assert.NotEqual(new DefaultValueAttribute(43), attr);
            Assert.NotEqual(new DefaultValueAttribute(null), attr);
            Assert.False(attr.Equals(null));

            attr = new DefaultValueAttribute(null);
            Assert.Equal(new DefaultValueAttribute(null), attr);
            Assert.Equal(attr.GetHashCode(), new DefaultValueAttribute(null).GetHashCode());
        }
    }
}
