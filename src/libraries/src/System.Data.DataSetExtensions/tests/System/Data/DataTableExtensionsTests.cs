// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Tests
{
    public class DataTableExtensionsTests
    {
        [Fact]
        public void AsEnumerable_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => DataTableExtensions.AsEnumerable(null));
        }

        [Fact]
        public void CopyToDataTable_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => DataTableExtensions.CopyToDataTable<DataRow>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => DataTableExtensions.CopyToDataTable<DataRow>(null, new DataTable(), LoadOption.OverwriteChanges));
            AssertExtensions.Throws<ArgumentNullException>("source", () => DataTableExtensions.CopyToDataTable<DataRow>(null, new DataTable(), LoadOption.OverwriteChanges, null));
        }

        [Fact]
        public void CopyToDataTable_NullTable_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("table", () => DataTableExtensions.CopyToDataTable(new DataRow[0], null, LoadOption.OverwriteChanges));
            AssertExtensions.Throws<ArgumentNullException>("table", () => DataTableExtensions.CopyToDataTable(new DataRow[0], null, LoadOption.OverwriteChanges, null));
        }
    }
}