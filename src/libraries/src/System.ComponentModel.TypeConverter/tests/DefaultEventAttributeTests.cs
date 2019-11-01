// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DefaultEventAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void Ctor_Name(string name)
        {
            var attribute = new DefaultEventAttribute(name);
            Assert.Equal(name, attribute.Name);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DefaultEventAttribute("name");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DefaultEventAttribute("name"), true };
            yield return new object[] { attribute, new DefaultEventAttribute("name2"), false };
            yield return new object[] { attribute, new DefaultEventAttribute(null), false };

            yield return new object[] { new DefaultEventAttribute(null), new DefaultEventAttribute(null), true };
            yield return new object[] { new DefaultEventAttribute(null), new DefaultEventAttribute("name"), false };
            yield return new object[] { new DefaultEventAttribute(null), null, false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(DefaultEventAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsEqual()
        {
            var attribute = new DefaultEventAttribute("name");
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            DefaultEventAttribute attribute = DefaultEventAttribute.Default;
            Assert.Same(attribute, DefaultEventAttribute.Default);
            Assert.Null(attribute.Name);
        }
    }
}
