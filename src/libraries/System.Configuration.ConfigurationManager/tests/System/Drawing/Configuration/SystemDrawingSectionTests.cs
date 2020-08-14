// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using Xunit;

namespace System.Drawing.Configuration.Tests
{
    public class SystemDrawingSectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var section = new SystemDrawingSection();
            Assert.Null(section.BitmapSuffix);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("BitmapSuffix")]
        public void BitmapSuffix_Set_GetReturnsExpected(string bitmapSuffix)
        {
            var section = new SystemDrawingSection { BitmapSuffix = bitmapSuffix };
            Assert.Equal(bitmapSuffix, section.BitmapSuffix);

            PropertyInformation propertyInformation = section.ElementInformation.Properties["bitmapSuffix"];
            Assert.Equal(bitmapSuffix, propertyInformation.Value);
        }
    }
}
