// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DisplayNameAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new SubDisplayNameAttribute();
            Assert.Empty(attribute.DisplayName);
            Assert.Empty(attribute.DisplayNameValue);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("displayName", false)]
        public void Ctor_String(string displayName, bool expectedIsDefaultAttribute)
        {
            var attribute = new SubDisplayNameAttribute(displayName);
            Assert.Equal(displayName, attribute.DisplayName);
            Assert.Equal(displayName, attribute.DisplayNameValue);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("displayName")]
        public void DisplayNameValue_Set_GetReturnsExpected(string value)
        {
            var attribute = new SubDisplayNameAttribute("Name")
            {
                DisplayNameValue = value
            };
            Assert.Same(value, attribute.DisplayName);
            Assert.Same(value, attribute.DisplayNameValue);

            // Set same.
            Assert.Same(value, attribute.DisplayName);
            Assert.Same(value, attribute.DisplayNameValue);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DisplayNameAttribute("name");
            yield return new object[] { attribute, new DisplayNameAttribute("name"), true };
            yield return new object[] { attribute, new DisplayNameAttribute("name2"), false };
            yield return new object[] { attribute, new DisplayNameAttribute(""), false };
            yield return new object[] { attribute, new DisplayNameAttribute(null), false };
            yield return new object[] { new DisplayNameAttribute(null), new DisplayNameAttribute(null), true };
            yield return new object[] { new DisplayNameAttribute(null), new DisplayNameAttribute(""), false };
            yield return new object[] { new DisplayNameAttribute(null), new DisplayNameAttribute("name"), false };
            yield return new object[] { new DisplayNameAttribute("name"), new object(), false };
            yield return new object[] { new DisplayNameAttribute("name"), null, false };
            yield return new object[] { new DisplayNameAttribute(null), new object(), false };
            yield return new object[] { new DisplayNameAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DisplayNameAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DisplayNameAttribute otherAttribute && otherAttribute.DisplayName != null && attribute.DisplayName != null)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void IsDefaultAttribute_Get_CallsEquals()
        {
            var attribute = new AlwaysEqualAttribute("displayName");
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            DisplayNameAttribute attribute = DisplayNameAttribute.Default;
            Assert.Same(attribute, DisplayNameAttribute.Default);
            Assert.Empty(attribute.DisplayName);
        }

        private class SubDisplayNameAttribute : DisplayNameAttribute
        {
            public SubDisplayNameAttribute() : base()
            {
            }

            public SubDisplayNameAttribute(string displayName) : base(displayName)
            {
            }

            public new string DisplayNameValue
            {
                get => base.DisplayNameValue;
                set => base.DisplayNameValue = value;
            }
        }

        private class AlwaysEqualAttribute : DisplayNameAttribute
        {
            public AlwaysEqualAttribute() : base()
            {
            }

            public AlwaysEqualAttribute(string displayName) : base(displayName)
            {
            }

            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }
    }
}
