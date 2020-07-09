// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Text.Tests
{
    public class InstalledFontCollectionTests
    {
        [ConditionalFact(Helpers.GdiPlusIsAvailableNotRedhat73)]
        public void Ctor_Default()
        {
            using (var fontCollection = new InstalledFontCollection())
            {
                Assert.NotEmpty(fontCollection.Families);
            }
        }

        [ConditionalFact(Helpers.GdiPlusIsAvailableNotRedhat73)]
        public void Families_GetWhenDisposed_ReturnsNonEmpty()
        {
            var fontCollection = new InstalledFontCollection();
            fontCollection.Dispose();

            Assert.NotEmpty(fontCollection.Families);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Dispose_MultipleTimes_Nop()
        {
            var fontCollection = new InstalledFontCollection();
            fontCollection.Dispose();
            fontCollection.Dispose();
        }
    }
}
