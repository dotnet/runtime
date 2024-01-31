// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DescriptionAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new SubDescriptionAttribute();
            Assert.Empty(attribute.Description);
            Assert.Empty(attribute.DescriptionValue);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("description", false)]
        public void Ctor_String(string description, bool expectedIsDefaultAttribute)
        {
            var attribute = new SubDescriptionAttribute(description);
            Assert.Equal(description, attribute.Description);
            Assert.Equal(description, attribute.DescriptionValue);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("descriptionValue")]
        public void DescriptionValue_Set_GetReturnsExpected(string value)
        {
            var attribute = new SubDescriptionAttribute("Name")
            {
                DescriptionValue = value
            };
            Assert.Same(value, attribute.Description);
            Assert.Same(value, attribute.DescriptionValue);

            // Set same.
            Assert.Same(value, attribute.Description);
            Assert.Same(value, attribute.DescriptionValue);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DescriptionAttribute("description");
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DescriptionAttribute("description"), true };
            yield return new object[] { attribute, new DescriptionAttribute("description2"), false };
            yield return new object[] { attribute, new DescriptionAttribute(""), false };
            yield return new object[] { attribute, new DescriptionAttribute(null), false };
            yield return new object[] { new DescriptionAttribute(null), new DescriptionAttribute(null), true };
            yield return new object[] { new DescriptionAttribute(null), new DescriptionAttribute("description"), false };
            yield return new object[] { new DescriptionAttribute(null), new DescriptionAttribute(""), false };
            yield return new object[] { new DescriptionAttribute("description"), new object(), false };
            yield return new object[] { new DescriptionAttribute("description"), null, false };
            yield return new object[] { new DescriptionAttribute(null), new object(), false };
            yield return new object[] { new DescriptionAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DescriptionAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DescriptionAttribute otherAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void IsDefaultAttribute_Get_CallsEquals()
        {
            var attribute = new AlwaysEqualAttribute("description");
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            DescriptionAttribute attribute = DescriptionAttribute.Default;
            Assert.Same(attribute, DescriptionAttribute.Default);
            Assert.Empty(attribute.Description);
        }

        private class SubDescriptionAttribute : DescriptionAttribute
        {
            public SubDescriptionAttribute() : base()
            {
            }

            public SubDescriptionAttribute(string description) : base(description)
            {
            }

            public new string DescriptionValue
            {
                get => base.DescriptionValue;
                set => base.DescriptionValue = value;
            }
        }

        private class AlwaysEqualAttribute : DescriptionAttribute
        {
            public AlwaysEqualAttribute() : base()
            {
            }

            public AlwaysEqualAttribute(string description) : base(description)
            {
            }

            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }
    }
}
