// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Xunit;

namespace System.Data.Tests.Common
{
    public class DataColumnMappingTest
    {
        [Fact]
        public void DataSetColumn_Get_WhenNotSet()
        {
            DataColumnMapping dataColumnMapping = new DataColumnMapping();

            string dataSetColumn = dataColumnMapping.DataSetColumn;

            Assert.Equal(string.Empty, dataSetColumn);
        }

        [Fact]
        public void SourceColumn_Get_WhenNotSet()
        {
            DataColumnMapping dataColumnMapping = new DataColumnMapping();

            string sourceColumn = dataColumnMapping.SourceColumn;

            Assert.Equal(string.Empty, sourceColumn);
        }

        [Fact]
        public void GetDataColumnBySchemaAction_String_String_DataTable_Type_MissingSchemaAction_MissingDataTableThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("dataTable", () => DataColumnMapping.GetDataColumnBySchemaAction("", "", null, typeof(string), new MissingSchemaAction()));
        }

        [Fact]
        public void GetDataColumnBySchemaAction_String_String_DataTable_Type_MissingSchemaAction_MissingDataSetColumnReturnsNull()
        {
            DataColumn dataColumn = DataColumnMapping.GetDataColumnBySchemaAction("", null, new DataTable(), typeof(string), new MissingSchemaAction());

            Assert.Null(dataColumn);
        }

        [Fact]
        public void GetDataColumnBySchemaAction_String_String_DataTable_Type_MissingSchemaAction_DataColumnExpressionExistsThrowsException()
        {
            DataColumn priceColumn = new DataColumn();
            priceColumn.DataType = typeof(decimal);
            priceColumn.ColumnName = "price";
            priceColumn.DefaultValue = 50;

            DataColumn taxColumn = new DataColumn();
            taxColumn.DataType = typeof(decimal);
            taxColumn.ColumnName = "tax";
            taxColumn.Expression = "price * 0.0862";

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(priceColumn);
            dataTable.Columns.Add(taxColumn);

            Assert.Throws<InvalidOperationException>(() => DataColumnMapping.GetDataColumnBySchemaAction("", "tax", dataTable, typeof(string), new MissingSchemaAction()));
        }

        [Fact]
        public void ToString_OK()
        {
            DataColumnMapping dataColumnMapping = new DataColumnMapping("the source", "");

            Assert.Equal("the source", dataColumnMapping.ToString());
        }
    }
}
