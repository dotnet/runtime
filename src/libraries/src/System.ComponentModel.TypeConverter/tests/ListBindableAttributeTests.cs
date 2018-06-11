// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ListBindableAttributeTests
    {
        [Theory]
        [InlineData(BindableSupport.Default, true)]
        [InlineData(BindableSupport.No, false)]
        [InlineData(BindableSupport.Yes, true)]
        [InlineData(BindableSupport.No - 1, true)]
        [InlineData(BindableSupport.Default + 1, true)]
        public void Ctor_BindableSupport(BindableSupport support, bool expectedBindable)
        {
            var attribute = new ListBindableAttribute(support);
            Assert.Equal(expectedBindable, attribute.ListBindable);
            Assert.Equal(expectedBindable || support == BindableSupport.Default, attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool listBindable)
        {
            var attribute = new ListBindableAttribute(listBindable);
            Assert.Equal(listBindable, attribute.ListBindable);
            Assert.Equal(listBindable, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new ListBindableAttribute(BindableSupport.Yes);
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new ListBindableAttribute(BindableSupport.Yes), true };
            yield return new object[] { attribute, new ListBindableAttribute(BindableSupport.No), false };
            
            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ListBindableAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsSame()
        {
            var attribute = new ListBindableAttribute(false);
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            ListBindableAttribute attribute = ListBindableAttribute.Default;
            Assert.Same(attribute, ListBindableAttribute.Default);
            Assert.Same(attribute, ListBindableAttribute.Yes);

            Assert.True(attribute.ListBindable);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Yes_Get_ReturnsExpected()
        {
            ListBindableAttribute attribute = ListBindableAttribute.Yes;
            Assert.Same(attribute, ListBindableAttribute.Yes);

            Assert.True(attribute.ListBindable);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void No_Get_ReturnsExpected()
        {
            ListBindableAttribute attribute = ListBindableAttribute.No;
            Assert.Same(attribute, ListBindableAttribute.No);

            Assert.False(attribute.ListBindable);
            Assert.False(attribute.IsDefaultAttribute());
        }
    }
}
