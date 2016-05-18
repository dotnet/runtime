// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class DisplayNameAttributeTests
    {
        [Fact]
        public void GetDisplayName()
        {
            var name = "test name";
            var attribute = new DisplayNameAttribute(name);

            Assert.Equal(name, attribute.DisplayName);
        }

        [Theory]
        [MemberData(nameof(NameData))]
        public void NameTests(DisplayNameAttribute attribute, string name)
        {
            Assert.Equal(name, attribute.DisplayName);
        }

        private static IEnumerable<object[]> NameData()
        {
            yield return new object[] { DisplayNameAttribute.Default, "" };
            yield return new object[] { new DisplayNameAttribute(""), "" };
            yield return new object[] { new DisplayNameAttribute("other"), "other" };
        }
    }
}
