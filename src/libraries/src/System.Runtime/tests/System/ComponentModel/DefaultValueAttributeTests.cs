// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public static partial class DefaultValueAttributeTests
    {
        [Fact]
        public static void Ctor()
        {
            Assert.Equal(true, new DefaultValueAttribute(true).Value);
            Assert.Equal(false, new DefaultValueAttribute(false).Value);

            Assert.Equal(3.14, new DefaultValueAttribute(3.14).Value);
            Assert.Equal(3.14f, new DefaultValueAttribute(3.14f).Value);

            Assert.Equal((byte)1, new DefaultValueAttribute((byte)1).Value);
            Assert.Equal(42, new DefaultValueAttribute(42).Value);
            Assert.Equal(42L, new DefaultValueAttribute(42L).Value);
            Assert.Equal((short)42, new DefaultValueAttribute((short)42).Value);

            Assert.Equal('c', new DefaultValueAttribute('c').Value);
            Assert.Equal("test", new DefaultValueAttribute("test").Value);

            Assert.Equal("test", new DefaultValueAttribute((object)"test").Value);

            Assert.Equal(DayOfWeek.Monday, new DefaultValueAttribute(typeof(DayOfWeek), "Monday").Value);
            Assert.Equal(TimeSpan.FromHours(1), new DefaultValueAttribute(typeof(TimeSpan), "1:00:00").Value);

            Assert.Equal(42, new DefaultValueAttribute(typeof(int), "42").Value);
            Assert.Null(new DefaultValueAttribute(typeof(int), "caughtException").Value);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attr = new DefaultValueAttribute(42);
            yield return new object[] { attr, attr, true };
            yield return new object[] { attr, new DefaultValueAttribute(42), true };
            yield return new object[] { attr, new DefaultValueAttribute(43), false };
            yield return new object[] { attr, new DefaultValueAttribute(null), false };
            yield return new object[] { attr, null, false };
            yield return new object[] { new DefaultValueAttribute(null), new DefaultValueAttribute(null), true };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(DefaultValueAttribute attr1, object obj, bool expected)
        {
            Assert.Equal(expected, attr1.Equals(obj));

            DefaultValueAttribute attr2 = obj as DefaultValueAttribute;
            if (attr2 != null)
            {
                Assert.Equal(expected, attr1.GetHashCode() == attr2.GetHashCode());
            }
        }
    }

    public sealed class CustomDefaultValueAttribute : DefaultValueAttribute
    {
        public CustomDefaultValueAttribute(object value) : base(value) { }

        public new void SetValue(object value) => base.SetValue(value);
    }

    public static class DefaultValueAttributeTestsNetStandard17
    {
        [Fact]
        public static void SetValue()
        {
            var attr = new CustomDefaultValueAttribute(null);

            attr.SetValue(true);
            Assert.Equal(true, attr.Value);

            attr.SetValue(false);
            Assert.Equal(false, attr.Value);

            attr.SetValue(12.8f);
            Assert.Equal(12.8f, attr.Value);

            attr.SetValue(12.8);
            Assert.Equal(12.8, attr.Value);

            attr.SetValue((byte)1);
            Assert.Equal((byte)1, attr.Value);

            attr.SetValue(28);
            Assert.Equal(28, attr.Value);

            attr.SetValue(TimeSpan.FromHours(1));
            Assert.Equal(TimeSpan.FromHours(1), attr.Value);

            attr.SetValue(null);
            Assert.Null(attr.Value);
        }
    }
}
