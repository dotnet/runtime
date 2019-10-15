// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class DllImportAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("DllName")]
        public void Ctor_SourceInterfaces(string dllName)
        {
            var attribute = new DllImportAttribute(dllName);
            Assert.Equal(dllName, attribute.Value);
        }
    }
}
