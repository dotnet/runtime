// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Text.Tests
{
    public class InstalledFontCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            using (var fontCollection = new InstalledFontCollection())
            {
                Assert.NotEmpty(fontCollection.Families);
            }
        }

        [Fact]
        public void Families_GetWhenDisposed_ReturnsNonEmpty()
        {
            var fontCollection = new InstalledFontCollection();
            fontCollection.Dispose();

            Assert.NotEmpty(fontCollection.Families);
        }

        [Fact]
        public void Dispose_MultipleTimes_Nop()
        {
            var fontCollection = new InstalledFontCollection();
            fontCollection.Dispose();
            fontCollection.Dispose();
        }
    }
}
