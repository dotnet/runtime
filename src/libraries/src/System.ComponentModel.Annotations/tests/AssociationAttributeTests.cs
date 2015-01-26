// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class AssociationAttributeTests
    {
#pragma warning disable 618
        [Fact]
        public static void Can_construct_attribute_and_get_values()
        {
            var attribute =
                new AssociationAttribute("TestName", "TestThisKey", "TestOtherKey");
            Assert.Equal("TestName", attribute.Name);
            Assert.Equal("TestThisKey", attribute.ThisKey);
            Assert.Equal("TestOtherKey", attribute.OtherKey);
        }

        [Fact]
        public static void Can_construct_attribute_and_get_whitespace_values()
        {
            var attribute =
                new AssociationAttribute(null, string.Empty, " \t\r\n");
            Assert.Equal(null, attribute.Name);
            Assert.Equal(string.Empty, attribute.ThisKey);
            Assert.Equal(" \t\r\n", attribute.OtherKey);
        }

        [Fact]
        public static void Can_get_and_set_IsForeignKey()
        {
            var attribute = new AssociationAttribute("Name", "ThisKey", "OtherKey");
            Assert.Equal(false, attribute.IsForeignKey);
            attribute.IsForeignKey = true;
            Assert.Equal(true, attribute.IsForeignKey);
            attribute.IsForeignKey = false;
            Assert.Equal(false, attribute.IsForeignKey);
        }

        [Fact]
        public static void Can_get_ThisKeyMembers_and_OtherKeyMembers()
        {
            var listOfThisKeys = new List<string>() { "ThisKey1", "ThisKey2", "ThisKey3" };
            var listOfOtherKeys = new List<string>() { "OtherKey1", "OtherKey2" };
            // doesn't matter how many spaces are between keys, but they must be separated by a comma
            var attribute = new AssociationAttribute("Name", "ThisKey1,  ThisKey2, ThisKey3", "OtherKey1,  OtherKey2");
            Assert.True(listOfThisKeys.SequenceEqual(attribute.ThisKeyMembers));
            Assert.True(listOfOtherKeys.SequenceEqual(attribute.OtherKeyMembers));
        }
#pragma warning restore 618
    }
}
