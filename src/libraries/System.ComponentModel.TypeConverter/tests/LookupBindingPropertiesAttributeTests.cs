// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class LookupBindingPropertiesAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new LookupBindingPropertiesAttribute();
            Assert.Null(attribute.DataSource);
            Assert.Null(attribute.DisplayMember);
            Assert.Null(attribute.LookupMember);
            Assert.Null(attribute.ValueMember);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "", "", "")]
        [InlineData("dataSource", "displayMember", "valueMember", "lookupMember")]
        public void Ctor_String_String_String_String(string dataSource, string displayMember, string valueMember, string lookupMember)
        {
            var attribute = new LookupBindingPropertiesAttribute(dataSource, displayMember, valueMember, lookupMember);
            Assert.Equal(dataSource, attribute.DataSource);
            Assert.Equal(displayMember, attribute.DisplayMember);
            Assert.Equal(lookupMember, attribute.LookupMember);
            Assert.Equal(valueMember, attribute.ValueMember);
            Assert.False(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new LookupBindingPropertiesAttribute("dataSource", "displayMember", "lookupMember", "valueMember");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("dataSource", "displayMember", "lookupMember", "valueMember"), true };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("datasource", "displayMember", "lookupMember", "valueMember"), false };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("dataSource", "displaymember", "lookupMember", "valueMember"), false };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("dataSource", "displayMember", "lookupmember", "valueMember"), false };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("dataSource", "displayMember", "lookupMember", "valuemember"), false };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute(null, "displayMember", "lookupMember", "valueMember"), false };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("dataSource", null, "lookupMember", "valueMember"), false };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("dataSource", "displayMember", null, "valueMember"), false };
            yield return new object[] { attribute, new LookupBindingPropertiesAttribute("dataSource", "displayMember", "lookupMember", null), false };

            yield return new object[] { new LookupBindingPropertiesAttribute(), new LookupBindingPropertiesAttribute(), true };
            yield return new object[] { new LookupBindingPropertiesAttribute(), new LookupBindingPropertiesAttribute("dataSource", null, null, null), false };
            yield return new object[] { new LookupBindingPropertiesAttribute(), new LookupBindingPropertiesAttribute(null, "displayMember", null, null), false };
            yield return new object[] { new LookupBindingPropertiesAttribute(), new LookupBindingPropertiesAttribute(null, null, "valueMember", null), false };
            yield return new object[] { new LookupBindingPropertiesAttribute(), new LookupBindingPropertiesAttribute(null, null, null, "lookupMember"), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(LookupBindingPropertiesAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsSame()
        {
            var attribute = new LookupBindingPropertiesAttribute();
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            LookupBindingPropertiesAttribute attribute = LookupBindingPropertiesAttribute.Default;
            Assert.Null(attribute.DataSource);
            Assert.Null(attribute.DisplayMember);
            Assert.Null(attribute.LookupMember);
            Assert.Null(attribute.ValueMember);
            Assert.False(attribute.IsDefaultAttribute());
        }
    }
}
