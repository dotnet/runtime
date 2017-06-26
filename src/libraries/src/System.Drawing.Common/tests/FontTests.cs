// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Tests
{
    public class FontTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("NoSuchFont")]
        [InlineData("Serif")]
        public void Ctor_NoSuchFamilyName_SetsFamilyToGenericSansSerif(string familyName)
        {
            using (var font = new Font(familyName, 10))
            {
                Assert.Equal("Microsoft Sans Serif", font.FontFamily.Name);
            }
        }
    }
}
