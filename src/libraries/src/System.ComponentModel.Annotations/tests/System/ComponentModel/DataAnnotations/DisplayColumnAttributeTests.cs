// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class DisplayColumnAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("DisplayColumn")]
        public void Ctor_DisplayColumn(string displayColumn)
        {
            var attribute = new DisplayColumnAttribute(displayColumn);
            Assert.Equal(displayColumn, attribute.DisplayColumn);
            Assert.Null(attribute.SortColumn);
            Assert.False(attribute.SortDescending);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("DisplayColumn", "SortColumn")]
        public void Ctor_DisplayColumn_SortColumn(string displayColumn, string sortColumn)
        {
            var attribute = new DisplayColumnAttribute(displayColumn, sortColumn);
            Assert.Equal(displayColumn, attribute.DisplayColumn);
            Assert.Equal(sortColumn, attribute.SortColumn);
            Assert.False(attribute.SortDescending);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData("", "", false)]
        [InlineData("DisplayColumn", "SortColumn", true)]
        public void Ctor_DisplayColumn_SortColumn_SortDescending(string displayColumn, string sortColumn, bool sortDescending)
        {
            var attribute = new DisplayColumnAttribute(displayColumn, sortColumn, sortDescending);
            Assert.Equal(displayColumn, attribute.DisplayColumn);
            Assert.Equal(sortColumn, attribute.SortColumn);
            Assert.Equal(sortDescending, attribute.SortDescending);
        }
    }
}
