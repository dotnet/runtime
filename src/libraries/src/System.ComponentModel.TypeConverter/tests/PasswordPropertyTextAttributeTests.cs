// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class PasswordPropertyTextAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new PasswordPropertyTextAttribute();
            Assert.False(attribute.Password);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool password)
        {
            var attribute = new PasswordPropertyTextAttribute(password);
            Assert.Equal(password, attribute.Password);
            Assert.Equal(!password, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            PasswordPropertyTextAttribute attribute = PasswordPropertyTextAttribute.Default;
            Assert.Same(attribute, PasswordPropertyTextAttribute.Default);
            Assert.Same(attribute, PasswordPropertyTextAttribute.No);
            Assert.False(attribute.Password);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Yes_Get_ReturnsExpected()
        {
            PasswordPropertyTextAttribute attribute = PasswordPropertyTextAttribute.Yes;
            Assert.Same(attribute, PasswordPropertyTextAttribute.Yes);
            Assert.True(attribute.Password);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void No_Get_ReturnsExpected()
        {
            PasswordPropertyTextAttribute attribute = PasswordPropertyTextAttribute.No;
            Assert.Same(attribute, PasswordPropertyTextAttribute.No);
            Assert.False(attribute.Password);
            Assert.True(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new PasswordPropertyTextAttribute(true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new PasswordPropertyTextAttribute(true), true };
            yield return new object[] { attribute, new PasswordPropertyTextAttribute(false), false };
            yield return new object[] { new PasswordPropertyTextAttribute(false), new PasswordPropertyTextAttribute(false), true };
            yield return new object[] { new PasswordPropertyTextAttribute(false), new PasswordPropertyTextAttribute(true), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(PasswordPropertyTextAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is PasswordPropertyTextAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
