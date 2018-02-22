// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.SqlServer.Server;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlMetaDataTest
    {

        // Test UDT constrtuctor without tvp extended properties
        [Fact]
        public void UdtConstructorTest()
        {
            Address address = Address.Parse("123 baker st || Redmond");
            SqlMetaData metaData = new SqlMetaData("col1", SqlDbType.Udt, typeof(Address), "UdtTestDb.dbo.Address");
            Assert.Equal("col1", metaData.Name);
            Assert.Equal(SqlDbType.Udt, metaData.SqlDbType);
            Assert.Equal(address.GetType(), metaData.Type);
            Assert.Equal("UdtTestDb.dbo.Address", metaData.TypeName);
            Assert.False(metaData.UseServerDefault);
            Assert.False(metaData.IsUniqueKey);
            Assert.Equal(SortOrder.Unspecified, metaData.SortOrder);
            Assert.Equal(-1, metaData.SortOrdinal);
        }


        // Test UDT constrtuctor with tvp extended properties
        [Fact]
        public void UdtConstructorWithTvpTest()
        {
            Address address = Address.Parse("123 baker st || Redmond");
            SqlMetaData metaData = new SqlMetaData("col2", SqlDbType.Udt, typeof(Address), "UdtTestDb.dbo.Address", true, true, SortOrder.Ascending, 0);
            Assert.Equal("col2", metaData.Name);
            Assert.Equal(SqlDbType.Udt, metaData.SqlDbType);
            Assert.Equal(address.GetType(), metaData.Type);
            Assert.Equal("UdtTestDb.dbo.Address", metaData.TypeName);
            Assert.True(metaData.UseServerDefault);
            Assert.True(metaData.IsUniqueKey);
            Assert.Equal(SortOrder.Ascending, metaData.SortOrder);
            Assert.Equal(0, metaData.SortOrdinal);
        }
    }
}
