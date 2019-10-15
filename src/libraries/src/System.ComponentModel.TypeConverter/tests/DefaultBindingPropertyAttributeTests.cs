// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DefaultBindingPropertyAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DefaultBindingPropertyAttribute();
            Assert.Null(attribute.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void Ctor_Name(string name)
        {
            var attribute = new DefaultBindingPropertyAttribute(name);
            Assert.Equal(name, attribute.Name);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DefaultBindingPropertyAttribute("name");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DefaultBindingPropertyAttribute("name"), true };
            yield return new object[] { attribute, new DefaultBindingPropertyAttribute("name2"), false };
            yield return new object[] { attribute, new DefaultBindingPropertyAttribute(null), false };

            yield return new object[] { new DefaultBindingPropertyAttribute(null), new DefaultBindingPropertyAttribute(null), true };
            yield return new object[] { new DefaultBindingPropertyAttribute(null), new DefaultBindingPropertyAttribute("name"), false };
            yield return new object[] { new DefaultBindingPropertyAttribute(null), null, false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(DefaultBindingPropertyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsEqual()
        {
            var attribute = new DefaultBindingPropertyAttribute("name");
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            DefaultBindingPropertyAttribute attribute = DefaultBindingPropertyAttribute.Default;
            Assert.Same(attribute, DefaultBindingPropertyAttribute.Default);
            Assert.Null(attribute.Name);
        }
    }
}
