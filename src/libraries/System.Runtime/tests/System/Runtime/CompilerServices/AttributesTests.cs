// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class AttributesTests
    {
        [Fact]
        public static void TypeForwardedToAttributeTests()
        {
            string assemblyFullName = "MyAssembly";
            var attr = new TypeForwardedFromAttribute(assemblyFullName);
            Assert.Equal(assemblyFullName, attr.AssemblyFullName);

            AssertExtensions.Throws<ArgumentNullException>("assemblyFullName", () => new TypeForwardedFromAttribute(null));
            AssertExtensions.Throws<ArgumentNullException>("assemblyFullName", () => new TypeForwardedFromAttribute(""));
        }
    }
}
