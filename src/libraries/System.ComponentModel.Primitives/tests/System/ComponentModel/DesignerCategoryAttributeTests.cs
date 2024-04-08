// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DesignerCategoryAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DesignerCategoryAttribute();
            Assert.Empty(attribute.Category);
            Assert.True(attribute.IsDefaultAttribute());
            Assert.Equal("System.ComponentModel.DesignerCategoryAttribute", attribute.TypeId);
        }

        [Theory]
        [InlineData(null, false, "System.ComponentModel.DesignerCategoryAttribute")]
        [InlineData("", true, "System.ComponentModel.DesignerCategoryAttribute")]
        [InlineData("category", false, "System.ComponentModel.DesignerCategoryAttributecategory")]
        public void Ctor_String(string category, bool expectedIsDefaultAttribute, string expectedTypeId)
        {
            var attribute = new DesignerCategoryAttribute(category);
            Assert.Equal(category, attribute.Category);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
            Assert.Equal(expectedTypeId, attribute.TypeId);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DesignerCategoryAttribute("category");
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DesignerCategoryAttribute("category"), true };
            yield return new object[] { attribute, new DesignerCategoryAttribute("category2"), false };
            yield return new object[] { attribute, new DesignerCategoryAttribute(string.Empty), false };
            yield return new object[] { attribute, new DesignerCategoryAttribute(null), false };
            yield return new object[] { new DesignerCategoryAttribute(null), new DesignerCategoryAttribute(null), true };
            yield return new object[] { new DesignerCategoryAttribute(null), new DesignerCategoryAttribute("category"), false };
            yield return new object[] { new DesignerCategoryAttribute(null), new DesignerCategoryAttribute(string.Empty), false };
            yield return new object[] { new DesignerCategoryAttribute("category"), new object(), false };
            yield return new object[] { new DesignerCategoryAttribute("category"), null, false };
            yield return new object[] { new DesignerCategoryAttribute(null), new object(), false };
            yield return new object[] { new DesignerCategoryAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DesignerCategoryAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DesignerCategoryAttribute otherAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> Properties_TestData()
        {
            yield return new object[] { (Func<DesignerCategoryAttribute>)(() => DesignerCategoryAttribute.Component), "Component" };
            yield return new object[] { (Func<DesignerCategoryAttribute>)(() => DesignerCategoryAttribute.Default), string.Empty };
            yield return new object[] { (Func<DesignerCategoryAttribute>)(() => DesignerCategoryAttribute.Form), "Form" };
            yield return new object[] { (Func<DesignerCategoryAttribute>)(() => DesignerCategoryAttribute.Generic), "Designer" };
        }

        [Theory]
        [MemberData(nameof(Properties_TestData))]
        public void Properties_Get_ReturnsExpected(Func<DesignerCategoryAttribute> attributeThunk, string expectedCategory)
        {
            DesignerCategoryAttribute attribute = attributeThunk();
            Assert.Same(attribute, attributeThunk());
            Assert.Equal(expectedCategory, attribute.Category);
        }
    }
}
