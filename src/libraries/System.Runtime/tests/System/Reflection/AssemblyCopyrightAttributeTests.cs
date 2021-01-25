// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyCopyrightAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("copyright")]
        public void Ctor_String(string copyright)
        {
            var attribute = new AssemblyCopyrightAttribute(copyright);
            Assert.Equal(copyright, attribute.Copyright);
        }
    }
}
