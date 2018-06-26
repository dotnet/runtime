// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class BindableAttributeTests
    {
        [Theory]
        [InlineData(BindableSupport.Default, true)]
        [InlineData(BindableSupport.No, false)]
        [InlineData(BindableSupport.Yes, true)]
        [InlineData(BindableSupport.No - 1, true)]
        [InlineData(BindableSupport.Default + 1, true)]
        public void Ctor_BindableSupport(BindableSupport support, bool expectedBindable)
        {
            var attribute = new BindableAttribute(support);
            Assert.Equal(expectedBindable, attribute.Bindable);
            Assert.Equal(BindingDirection.OneWay, attribute.Direction);

            Assert.Equal(!expectedBindable || support == BindableSupport.Default, attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(BindableSupport.Default, BindingDirection.OneWay, true)]
        [InlineData(BindableSupport.No, BindingDirection.OneWay, false)]
        [InlineData(BindableSupport.Yes, BindingDirection.OneWay, true)]
        [InlineData(BindableSupport.No - 1, BindingDirection.OneWay - 1, true)]
        [InlineData(BindableSupport.Default + 1, BindingDirection.TwoWay + 1, true)]
        public void Ctor_BindableSupport_BindingDirection(BindableSupport support, BindingDirection direction, bool expectedBindable)
        {
            var attribute = new BindableAttribute(support, direction);
            Assert.Equal(expectedBindable, attribute.Bindable);
            Assert.Equal(direction, attribute.Direction);

            Assert.Equal(!expectedBindable || support == BindableSupport.Default, attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bindable(bool bindable)
        {
            var attribute = new BindableAttribute(bindable);
            Assert.Equal(bindable, attribute.Bindable);
            Assert.Equal(BindingDirection.OneWay, attribute.Direction);

            Assert.Equal(!bindable, attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(true, BindingDirection.OneWay)]
        [InlineData(false, BindingDirection.TwoWay)]
        [InlineData(true, BindingDirection.OneWay - 1)]
        [InlineData(false, BindingDirection.TwoWay + 1)]
        public void Ctor_Bindable_BindingDirection(bool bindable, BindingDirection direction)
        {
            var attribute = new BindableAttribute(bindable, direction);
            Assert.Equal(bindable, attribute.Bindable);
            Assert.Equal(direction, attribute.Direction);

            Assert.Equal(!bindable, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new BindableAttribute(true, BindingDirection.OneWay);
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new BindableAttribute(true, BindingDirection.TwoWay), true };
            yield return new object[] { attribute, new BindableAttribute(false, BindingDirection.OneWay), false };
            
            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(BindableAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsBindableHashCode()
        {
            var attribute = new BindableAttribute(true);
            Assert.Equal(1, attribute.GetHashCode());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            BindableAttribute attribute = BindableAttribute.Default;
            Assert.Same(attribute, BindableAttribute.Default);
            Assert.Same(attribute, BindableAttribute.No);

            Assert.False(attribute.Bindable);
            Assert.Equal(BindingDirection.OneWay, attribute.Direction);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Yes_Get_ReturnsExpected()
        {
            BindableAttribute attribute = BindableAttribute.Yes;
            Assert.Same(attribute, BindableAttribute.Yes);

            Assert.True(attribute.Bindable);
            Assert.Equal(BindingDirection.OneWay, attribute.Direction);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void No_Get_ReturnsExpected()
        {
            BindableAttribute attribute = BindableAttribute.No;
            Assert.Same(attribute, BindableAttribute.No);

            Assert.False(attribute.Bindable);
            Assert.Equal(BindingDirection.OneWay, attribute.Direction);
            Assert.True(attribute.IsDefaultAttribute());
        }
    }
}
