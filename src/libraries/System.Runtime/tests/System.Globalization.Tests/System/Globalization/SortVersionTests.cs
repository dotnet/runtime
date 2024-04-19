// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class SortVersionMiscTests
    {
        [Fact]
        public void ConstructionsTest()
        {
            string guidString =  "00000001-57ee-1e5c-00b4-d0000bb1e11e";
            SortVersion version = new SortVersion(393742, new Guid(guidString));
            Assert.Equal(393742, version.FullVersion);
            Assert.Equal(new Guid(guidString), version.SortId);
        }

        [Fact]
        public void EquaityTest()
        {
            SortVersion version1 = new SortVersion(393742, new Guid("00000001-57ee-1e5c-00b4-d0000bb1e11e"));
            SortVersion version2 = new SortVersion(393742, new Guid("00000001-57ee-1e5c-00b4-d0000bb1e11e"));

            Assert.Equal(version2, version1);
        }
    }
}
