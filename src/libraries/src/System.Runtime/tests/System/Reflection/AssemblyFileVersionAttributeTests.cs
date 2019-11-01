// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyFileVersionAttributeTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("version")]
        [InlineData("1.2.3.4.5")]
        public void Ctor_String(string version)
        {
            var attribute = new AssemblyFileVersionAttribute(version);
            Assert.Equal(version, attribute.Version);
        }

        [Fact]
        public void Ctor_NullVersion_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("version", () => new AssemblyFileVersionAttribute(null));
        }
    }
}
