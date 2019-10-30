// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class DisplayAttributeTests
    {
        [Fact]
        public void Ctor()
        {
            DisplayAttribute attribute = new DisplayAttribute();
            Assert.Null(attribute.ShortName);
            Assert.Null(attribute.GetShortName());

            Assert.Null(attribute.Name);
            Assert.Null(attribute.GetName());

            Assert.Null(attribute.Description);
            Assert.Null(attribute.GetDescription());

            Assert.Null(attribute.Prompt);
            Assert.Null(attribute.GetPrompt());

            Assert.Null(attribute.GroupName);
            Assert.Null(attribute.GetGroupName());

            Assert.Null(attribute.ResourceType);
        }

        [Fact]
        public void AutoGenerateField_Get_NotSet_ThrowsInvalidOperationException()
        {
            DisplayAttribute attribute = new DisplayAttribute();
            Assert.Throws<InvalidOperationException>(() => attribute.AutoGenerateField);
            Assert.Null(attribute.GetAutoGenerateField());
        }

        [Fact]
        public void AutoGenerateFilter_Get_NotSet_ThrowsInvalidOperationException()
        {
            DisplayAttribute attribute = new DisplayAttribute();
            Assert.Throws<InvalidOperationException>(() => attribute.AutoGenerateFilter);
            Assert.Null(attribute.GetAutoGenerateFilter());
        }

        [Fact]
        public void Order_Get_NotSet_ThrowsInvalidOperationException()
        {
            DisplayAttribute attribute = new DisplayAttribute();
            Assert.Throws<InvalidOperationException>(() => attribute.Order);
            Assert.Null(attribute.GetOrder());
        }

        public static IEnumerable<object[]> Strings_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { " \r \t \n " };
            yield return new object[] { "abc" };
        }

        [Theory]
        [MemberData(nameof(Strings_TestData))]
        [InlineData("ShortName")]
        public void ShortName_Get_Set(string value)
        {
            DisplayAttribute attribute = new DisplayAttribute();
            attribute.ShortName = value;

            Assert.Equal(value, attribute.ShortName);
            Assert.Equal(value == null, attribute.GetShortName() == null);

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.ShortName = value;
            Assert.Equal(value, attribute.ShortName);
        }

        [Theory]
        [MemberData(nameof(Strings_TestData))]
        [InlineData("Name")]
        public void Name_Get_Set(string value)
        {
            DisplayAttribute attribute = new DisplayAttribute();
            attribute.Name = value;

            Assert.Equal(value, attribute.Name);
            Assert.Equal(value == null, attribute.GetName() == null);

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.Name = value;
            Assert.Equal(value, attribute.Name);
        }

        [Theory]
        [MemberData(nameof(Strings_TestData))]
        [InlineData("Description")]
        public void Description_Get_Set(string value)
        {
            DisplayAttribute attribute = new DisplayAttribute();
            attribute.Description = value;

            Assert.Equal(value, attribute.Description);
            Assert.Equal(value == null, attribute.GetDescription() == null);

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.Description = value;
            Assert.Equal(value, attribute.Description);
        }

        [Theory]
        [MemberData(nameof(Strings_TestData))]
        [InlineData("Prompt")]
        public void Prompt_Get_Set(string value)
        {
            DisplayAttribute attribute = new DisplayAttribute();
            attribute.Prompt = value;

            Assert.Equal(value, attribute.Prompt);
            Assert.Equal(value == null, attribute.GetPrompt() == null);

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.Prompt = value;
            Assert.Equal(value, attribute.Prompt);
        }

        [Theory]
        [MemberData(nameof(Strings_TestData))]
        [InlineData("GroupName")]
        public void GroupName_Get_Set(string value)
        {
            DisplayAttribute attribute = new DisplayAttribute();
            attribute.GroupName = value;

            Assert.Equal(value, attribute.GroupName);
            Assert.Equal(value == null, attribute.GetGroupName() == null);

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.GroupName = value;
            Assert.Equal(value, attribute.GroupName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(string))]
        public void ResourceType_Get_Set(Type value)
        {
            DisplayAttribute attribute = new DisplayAttribute();
            attribute.ResourceType = value;
            Assert.Equal(value, attribute.ResourceType);

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.ResourceType = value;
            Assert.Equal(value, attribute.ResourceType);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AutoGenerateField_Get_Set(bool value)
        {
            DisplayAttribute attribute = new DisplayAttribute();

            attribute.AutoGenerateField = value;
            Assert.Equal(value, attribute.AutoGenerateField);
            Assert.Equal(value, attribute.GetAutoGenerateField());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AutoGenerateFilter_Get_Set(bool value)
        {
            DisplayAttribute attribute = new DisplayAttribute();

            attribute.AutoGenerateFilter = value;
            Assert.Equal(value, attribute.AutoGenerateFilter);
            Assert.Equal(value, attribute.GetAutoGenerateFilter());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1)]
        public void Order_Get_Set(int value)
        {
            DisplayAttribute attribute = new DisplayAttribute();

            attribute.Order = value;
            Assert.Equal(value, attribute.Order);
            Assert.Equal(value, attribute.GetOrder());
        }
    }
}
