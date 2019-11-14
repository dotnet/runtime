// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ToolboxItemFilterAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("filterString")]
        public void Ctor_String(string filterString)
        {
            var attribute = new ToolboxItemFilterAttribute(filterString);
            Assert.Equal(filterString ?? string.Empty, attribute.FilterString);
            Assert.Equal(ToolboxItemFilterType.Allow, attribute.FilterType);
        }

        [Theory]
        [InlineData(null, ToolboxItemFilterType.Allow)]
        [InlineData("", ToolboxItemFilterType.Custom)]
        [InlineData("filterString", ToolboxItemFilterType.Prevent)]
        [InlineData("filterString", ToolboxItemFilterType.Require)]
        [InlineData("filterString", ToolboxItemFilterType.Allow - 1)]
        [InlineData("filterString", ToolboxItemFilterType.Require + 1)]
        public void Ctor_String_ToolboxItemFilterType(string filterString, ToolboxItemFilterType filterType)
        {
            var attribute = new ToolboxItemFilterAttribute(filterString, filterType);
            Assert.Equal(filterString ?? string.Empty, attribute.FilterString);
            Assert.Equal(filterType, attribute.FilterType);
        }

        [Theory]
        [InlineData(null, "System.ComponentModel.ToolboxItemFilterAttribute")]
        [InlineData("", "System.ComponentModel.ToolboxItemFilterAttribute")]
        [InlineData("filterString", "System.ComponentModel.ToolboxItemFilterAttributefilterString")]
        public void TypeId_ValidEditorBaseTypeName_ReturnsExcepted(string filterType, object expected)
        {
            var attribute = new ToolboxItemFilterAttribute(filterType);
            Assert.Equal(expected, attribute.TypeId);
            Assert.Same(attribute.TypeId, attribute.TypeId);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new ToolboxItemFilterAttribute("filterString", ToolboxItemFilterType.Allow);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new ToolboxItemFilterAttribute("filterString", ToolboxItemFilterType.Allow), true };
            yield return new object[] { attribute, new ToolboxItemFilterAttribute("filterstring", ToolboxItemFilterType.Allow), false };
            yield return new object[] { attribute, new ToolboxItemFilterAttribute("filterString", ToolboxItemFilterType.Custom), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ToolboxItemFilterAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsConsistentValue()
        {
            var attribute = new ToolboxItemFilterAttribute("filterString");
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }

        public static IEnumerable<object[]> Match_TestData()
        {
            var attribute = new ToolboxItemFilterAttribute("filterString", ToolboxItemFilterType.Allow);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new ToolboxItemFilterAttribute("filterString", ToolboxItemFilterType.Allow), true };
            yield return new object[] { attribute, new ToolboxItemFilterAttribute("filterstring", ToolboxItemFilterType.Allow), false };
            yield return new object[] { attribute, new ToolboxItemFilterAttribute("filterString", ToolboxItemFilterType.Custom), true };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Match_TestData))]
        public void Match_Object_ReturnsExpected(ToolboxItemFilterAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Match(other));
        }

        [Theory]
        [InlineData(null, ToolboxItemFilterType.Allow, ",Allow")]
        [InlineData("", ToolboxItemFilterType.Custom, ",Custom")]
        [InlineData("filterString", ToolboxItemFilterType.Prevent, "filterString,Prevent")]
        [InlineData("filterString", ToolboxItemFilterType.Require, "filterString,Require")]
        [InlineData("filterString", ToolboxItemFilterType.Allow - 1, "filterString,")]
        [InlineData("filterString", ToolboxItemFilterType.Require + 1, "filterString,")]
        public void ToString_Invoke_ReturnsExpected(string filterString, ToolboxItemFilterType filterType, string expected)
        {
            var attribute = new ToolboxItemFilterAttribute(filterString, filterType);
            Assert.Equal(expected, attribute.ToString());
        }
    }
}
