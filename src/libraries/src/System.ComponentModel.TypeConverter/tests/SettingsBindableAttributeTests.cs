// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class SettingsBindableAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool bindable)
        {
            var attribute = new SettingsBindableAttribute(bindable);
            Assert.Equal(bindable, attribute.Bindable);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Yes_Get_ReturnsExpected()
        {
            SettingsBindableAttribute attribute = SettingsBindableAttribute.Yes;
            Assert.Same(attribute, SettingsBindableAttribute.Yes);
            Assert.True(attribute.Bindable);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void No_Get_ReturnsExpected()
        {
            SettingsBindableAttribute attribute = SettingsBindableAttribute.No;
            Assert.Same(attribute, SettingsBindableAttribute.No);
            Assert.False(attribute.Bindable);
            Assert.False(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new SettingsBindableAttribute(true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new SettingsBindableAttribute(true), true };
            yield return new object[] { attribute, new SettingsBindableAttribute(false), false };
            yield return new object[] { new SettingsBindableAttribute(false), new SettingsBindableAttribute(false), true };
            yield return new object[] { new SettingsBindableAttribute(false), new SettingsBindableAttribute(true), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(SettingsBindableAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is SettingsBindableAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
