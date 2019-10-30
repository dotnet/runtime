// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyTitleAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("title")]
        public void Ctor_String(string title)
        {
            var attribute = new AssemblyTitleAttribute(title);
            Assert.Equal(title, attribute.Title);
        }
    }
}
