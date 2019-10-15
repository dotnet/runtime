// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class InheritanceAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new InheritanceAttribute();
            Assert.Equal(InheritanceLevel.NotInherited, attribute.InheritanceLevel);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(InheritanceLevel.NotInherited)]
        [InlineData(InheritanceLevel.Inherited)]
        [InlineData(InheritanceLevel.InheritedReadOnly)]
        [InlineData(InheritanceLevel.Inherited - 1)]
        [InlineData(InheritanceLevel.NotInherited + 1)]
        public void Ctor_InheritanceLevel(InheritanceLevel inheritanceLevel)
        {
            var attribute = new InheritanceAttribute(inheritanceLevel);
            Assert.Equal(inheritanceLevel, attribute.InheritanceLevel);
            Assert.Equal(inheritanceLevel == InheritanceLevel.NotInherited, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Inherited_Get_ReturnsExpected()
        {
            InheritanceAttribute attribute = InheritanceAttribute.Inherited;
            Assert.Same(attribute, InheritanceAttribute.Inherited);

            Assert.Equal(InheritanceLevel.Inherited, attribute.InheritanceLevel);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void InheritedReadOnly_Get_ReturnsExpected()
        {
            InheritanceAttribute attribute = InheritanceAttribute.InheritedReadOnly;
            Assert.Same(attribute, InheritanceAttribute.InheritedReadOnly);

            Assert.Equal(InheritanceLevel.InheritedReadOnly, attribute.InheritanceLevel);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void NotInherited_Get_ReturnsExpected()
        {
            InheritanceAttribute attribute = InheritanceAttribute.NotInherited;
            Assert.Same(attribute, InheritanceAttribute.NotInherited);

            Assert.Equal(InheritanceLevel.NotInherited, attribute.InheritanceLevel);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            InheritanceAttribute attribute = InheritanceAttribute.Default;
            Assert.Same(attribute, InheritanceAttribute.Default);
            Assert.Same(attribute, InheritanceAttribute.NotInherited);

            Assert.Equal(InheritanceLevel.NotInherited, attribute.InheritanceLevel);
            Assert.True(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new InheritanceAttribute(InheritanceLevel.Inherited);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new InheritanceAttribute(InheritanceLevel.Inherited), true };
            yield return new object[] { attribute, new InheritanceAttribute(InheritanceLevel.InheritedReadOnly), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(InheritanceAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is InheritanceAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Theory]
        [InlineData(InheritanceLevel.NotInherited)]
        [InlineData(InheritanceLevel.Inherited)]
        [InlineData(InheritanceLevel.InheritedReadOnly)]
        public void ToString_ValidInheritanceLevel_ReturnsExpected(InheritanceLevel inheritanceLevel)
        {
            var attribute = new InheritanceAttribute(inheritanceLevel);
            Assert.Equal(inheritanceLevel.ToString(), attribute.ToString());
        }

        [Theory]
        [InlineData(InheritanceLevel.Inherited - 1)]
        [InlineData(InheritanceLevel.NotInherited + 1)]
        public void ToString_InvalidInheritanceLevel_ThrowsArgumentException(InheritanceLevel inheritanceLevel)
        {
            var attribute = new InheritanceAttribute(inheritanceLevel);
            AssertExtensions.Throws<ArgumentException>(null, () => attribute.ToString());
        }
    }
}
