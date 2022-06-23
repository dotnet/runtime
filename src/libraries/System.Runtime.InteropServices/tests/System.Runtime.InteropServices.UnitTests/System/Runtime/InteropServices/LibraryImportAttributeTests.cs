// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class LibraryImportAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("LibraryName")]
        public void Ctor(string libraryName)
        {
            var attribute = new LibraryImportAttribute(libraryName);
            Assert.Equal(libraryName, attribute.LibraryName);
        }
    }
}
