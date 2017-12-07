// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.Tests
{
    public class SortOptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var sortOption = new SortOption();
            Assert.Null(sortOption.PropertyName);
            Assert.Equal(SortDirection.Ascending, sortOption.Direction);
        }

        [Theory]
        [InlineData("", SortDirection.Descending)]
        [InlineData("propertyName", SortDirection.Ascending)]
        public void Ctor_PropertyName_SortDirection(string propertyName, SortDirection direction)
        {
            var sortOption = new SortOption(propertyName, direction);
            Assert.Equal(propertyName, sortOption.PropertyName);

            if (PlatformDetection.TargetsNetFx452OrLower)
                Assert.Equal(SortDirection.Ascending, sortOption.Direction);
            else
                Assert.Equal(direction, sortOption.Direction);
        }

        [Fact]
        public void Ctor_NullPropertyName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new SortOption(null, SortDirection.Ascending));
        }

        [Theory]
        [InlineData(SortDirection.Ascending - 1)]
        [InlineData(SortDirection.Descending + 1)]
        public void Ctor_InvalidDirection_ThrowsInvalidEnumArgumentException(SortDirection direction)
        {
            if (PlatformDetection.TargetsNetFx452OrLower)
            {
                SortOption so = new SortOption("propertyName", direction);
                Assert.Equal("propertyName", so.PropertyName);
                Assert.Equal(SortDirection.Ascending, so.Direction);
            }
            else
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => new SortOption("propertyName", direction));
        }
    }
}
