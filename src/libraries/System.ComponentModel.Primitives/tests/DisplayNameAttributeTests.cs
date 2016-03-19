// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
