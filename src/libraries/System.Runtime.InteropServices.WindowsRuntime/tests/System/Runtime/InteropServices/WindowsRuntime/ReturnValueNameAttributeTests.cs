// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class ReturnValueNameAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("Name")]
        public void Ctor_Name(string name)
        {
            var attribute = new ReturnValueNameAttribute(name);
            Assert.Equal(name, attribute.Name);
        }
    }
}
