// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DefaultPropertyAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void Ctor_Name(string name)
        {
            var attribute = new DefaultPropertyAttribute(name);
            Assert.Equal(name, attribute.Name);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DefaultPropertyAttribute("name");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DefaultPropertyAttribute("name"), true };
            yield return new object[] { attribute, new DefaultPropertyAttribute("name2"), false };
            yield return new object[] { attribute, new DefaultPropertyAttribute(null), false };

            yield return new object[] { new DefaultPropertyAttribute(null), new DefaultPropertyAttribute(null), true };
            yield return new object[] { new DefaultPropertyAttribute(null), new DefaultPropertyAttribute("name"), false };
            yield return new object[] { new DefaultPropertyAttribute(null), null, false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(DefaultPropertyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsEqual()
        {
            var attribute = new DefaultPropertyAttribute("name");
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            DefaultPropertyAttribute attribute = DefaultPropertyAttribute.Default;
            Assert.Same(attribute, DefaultPropertyAttribute.Default);
            Assert.Null(attribute.Name);
        }
    }
}
