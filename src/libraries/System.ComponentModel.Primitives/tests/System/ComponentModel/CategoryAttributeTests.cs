// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CategoryAttributeTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50881", TestPlatforms.Android)]
        public void Ctor_Default()
        {
            var attribute = new CategoryAttribute();
            Assert.Equal("Misc", attribute.Category);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("category", false)]
        [InlineData("Misc", true)]
        [InlineData("misc", false)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50881", TestPlatforms.Android)]
        public void Ctor_String(string category, bool expectedIsDefaultAttribute)
        {
            var attribute = new CategoryAttribute(category);
            Assert.Equal(category, attribute.Category);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new CategoryAttribute("category");
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new CategoryAttribute("category"), true };
            yield return new object[] { attribute, new CategoryAttribute("category2"), false };
            yield return new object[] { attribute, new CategoryAttribute(string.Empty), false };
            // .NET Framework throws a NullReferenceException.
            if (!PlatformDetection.IsNetFramework)
            {
                yield return new object[] { attribute, new CategoryAttribute(null), false };
            }

            // .NET Framework throws a NullReferenceException.
            if (!PlatformDetection.IsNetFramework)
            {
                yield return new object[] { new CategoryAttribute(null), new CategoryAttribute(null), true };
                yield return new object[] { new CategoryAttribute(null), new CategoryAttribute("category"), false };
                yield return new object[] { new CategoryAttribute(null), new CategoryAttribute(string.Empty), false };
            }

            yield return new object[] { new CategoryAttribute("category"), new object(), false };
            yield return new object[] { new CategoryAttribute("category"), null, false };
            yield return new object[] { new CategoryAttribute(null), new object(), false };
            yield return new object[] { new CategoryAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50881", TestPlatforms.Android)]
        public void Equals_Object_ReturnsExpected(CategoryAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is CategoryAttribute otherAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> Properties_TestData()
        {
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Appearance), "Appearance" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Asynchronous), "Asynchronous" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Behavior), "Behavior" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Data), "Data" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Default), "Misc" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Design), "Design" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.DragDrop), "Drag Drop" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Focus), "Focus" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Format), "Format" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Key), "Key" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Layout), "Layout" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.Mouse), "Mouse" };
            yield return new object[] { (Func<CategoryAttribute>)(() => CategoryAttribute.WindowStyle), "Window Style" };
        }

        [Theory]
        [MemberData(nameof(Properties_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50881", TestPlatforms.Android)]
        public void Properties_Get_ReturnsExpected(Func<CategoryAttribute> attributeThunk, string expectedCategory)
        {
            CategoryAttribute attribute = attributeThunk();
            Assert.Same(attribute, attributeThunk());
            Assert.Equal(expectedCategory, attribute.Category);
        }

        [Theory]
        [InlineData("Action")]
        [InlineData("Appearance")]
        [InlineData("Behavior")]
        [InlineData("Config")]
        [InlineData("Data")]
        [InlineData("DDE")]
        [InlineData("Default")]
        [InlineData("Design")]
        [InlineData("DragDrop")]
        [InlineData("Focus")]
        [InlineData("Font")]
        [InlineData("Format")]
        [InlineData("Key")]
        [InlineData("Layout")]
        [InlineData("List")]
        [InlineData("Mouse")]
        [InlineData("Position")]
        [InlineData("Scale")]
        [InlineData("Text")]
        [InlineData("WindowStyle")]
        public void GetLocalizedString_InvokeValueExists_ReturnsNonEmpty(string value)
        {
            var attribute = new SubCategoryAttribute();
            Assert.NotEmpty(attribute.GetLocalizedString(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50881", TestPlatforms.Android)]
        public void GetLocalizedString_InvokeNoSuchValue_ReturnsNull(string value)
        {
            var attribute = new SubCategoryAttribute();
            Assert.Null(attribute.GetLocalizedString(value));
        }

        [Fact]
        public void IsDefaultAttribute_Get_DoesNotCallEquals()
        {
            var attribute = new AlwaysEqualAttribute("category");
            Assert.False(attribute.IsDefaultAttribute());
        }

        private class SubCategoryAttribute : CategoryAttribute
        {
            public new string GetLocalizedString(string value) => base.GetLocalizedString(value);
        }

        private class AlwaysEqualAttribute : CategoryAttribute
        {
            public AlwaysEqualAttribute() : base()
            {
            }

            public AlwaysEqualAttribute(string category) : base(category)
            {
            }

            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }
    }
}
