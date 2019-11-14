// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyConfigurationAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("configuration")]
        public void Ctor_String(string configuration)
        {
            var attribute = new AssemblyConfigurationAttribute(configuration);
            Assert.Equal(configuration, attribute.Configuration);
        }
    }
}
