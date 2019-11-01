// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Common
{
    public class DataTableMappingTest
    {
        private DataTableMapping _dataTableMapping { get; }
        private DataColumnMapping[] _dataColumnMappings { get; }

        public DataTableMappingTest()
        {
            _dataColumnMappings = new DataColumnMapping[2];
            _dataColumnMappings[0] = new DataColumnMapping("firstSourceColumn", "firstDataSetColumn");
            _dataColumnMappings[1] = new DataColumnMapping("secondSourceColumn", "secondDataSetColumn");
            _dataTableMapping = new DataTableMapping("MyCustomSourceTable", "MyCustomDataSetTable", _dataColumnMappings);
        }

        [Fact]
        public void GetDataTableBySchemaAction()
        {
            // Test method with existing dataset table in dataset
            DataSet dataSet = new DataSet();
            DataTable dataTable = new DataTable("MyCustomDataSetTable");
            dataSet.Tables.Add(dataTable);

            DataTable result = _dataTableMapping.GetDataTableBySchemaAction(dataSet, MissingSchemaAction.Error);
            Assert.Equal(dataTable.TableName, result.TableName);

            // Test method with missing dataset table from dataset with MissingSchemaAction error.
            dataSet = new DataSet();
            dataTable = new DataTable("UnknownDataSet");
            dataSet.Tables.Add(dataTable);
            Assert.Throws<InvalidOperationException>(() => _dataTableMapping.GetDataTableBySchemaAction(dataSet, MissingSchemaAction.Error));

            // Test method with missing dataset table from dataset with MissingSchemaAction add.
            dataSet = new DataSet();
            dataTable = new DataTable("UnknownDataSet");
            dataSet.Tables.Add(dataTable);
            result = _dataTableMapping.GetDataTableBySchemaAction(dataSet, MissingSchemaAction.Add);
            Assert.Equal(_dataTableMapping.DataSetTable, result.TableName);

            // Test method with missing dataset table from dataset with MissingSchemaAction add with key.
            dataSet = new DataSet();
            dataTable = new DataTable("UnknownDataSet");
            dataSet.Tables.Add(dataTable);
            result = _dataTableMapping.GetDataTableBySchemaAction(dataSet, MissingSchemaAction.AddWithKey);
            Assert.Equal(_dataTableMapping.DataSetTable, result.TableName);

            // Test method with missing dataset table from dataset with invalid MissingSchemaAction.
            dataSet = new DataSet();
            dataTable = new DataTable("UnknownDataSet");
            dataSet.Tables.Add(dataTable);
            Assert.Throws<ArgumentOutOfRangeException>(() => _dataTableMapping.GetDataTableBySchemaAction(dataSet, 0));
        }

        [Fact]
        public void SourceTableParentValidation()
        {
            // Sets dataTableMappingCollection as the dataTableMapping Parent.
            DataTableMappingCollection dataTableMappingCollection = new DataTableMappingCollection();
            dataTableMappingCollection.Add(_dataTableMapping);

            // Adds a data table with another source table name.
            DataTableMapping secondDataTableMapping = new DataTableMapping("AnotherCustomSourceTable", "");
            dataTableMappingCollection.Add(secondDataTableMapping);

            // Attempts to change the second data table source table with a repeated value.
            Assert.Throws<ArgumentException>(() => secondDataTableMapping.SourceTable = "MyCustomSourceTable");
        }

        [Fact]
        public void Clone()
        {
            DataTableMapping clonedDataTableMapping = (DataTableMapping)((ICloneable)_dataTableMapping).Clone();

            Assert.Equal(_dataTableMapping.SourceTable, clonedDataTableMapping.SourceTable);
            Assert.Equal(_dataTableMapping.DataSetTable, clonedDataTableMapping.DataSetTable);
            Assert.Equal(_dataTableMapping.ColumnMappings.Count, clonedDataTableMapping.ColumnMappings.Count);
        }

        [Fact]
        public void SourceTableProperties()
        {
            Assert.Equal("MyCustomSourceTable", _dataTableMapping.SourceTable);

            DataTableMapping dataTableMapping = new DataTableMapping(null, "MyCustomDataSetTable");
            Assert.Equal(string.Empty, dataTableMapping.SourceTable);

            dataTableMapping = new DataTableMapping();
            Assert.Equal(string.Empty, dataTableMapping.SourceTable);
        }

        [Fact]
        public void DataSetTableProperties()
        {
            Assert.Equal("MyCustomDataSetTable", _dataTableMapping.DataSetTable);

            DataTableMapping dataTableMapping = new DataTableMapping("MyCustomSourceTable", null);
            Assert.Equal(string.Empty, dataTableMapping.DataSetTable);

            dataTableMapping = new DataTableMapping();
            Assert.Equal(string.Empty, dataTableMapping.DataSetTable);
        }

        [Fact]
        public void ColumnMappingsProperties()
        {
            Assert.Equal(_dataColumnMappings.Length, _dataTableMapping.ColumnMappings.Count);

            DataTableMapping dataTableMapping = new DataTableMapping("MyCustomSourceTable", "MyCustomDataSetTable");
            Assert.NotNull(dataTableMapping.ColumnMappings);
            Assert.Equal(0, dataTableMapping.ColumnMappings.Count);

            dataTableMapping = new DataTableMapping("MyCustomSourceTable", "MyCustomDataSetTable", null);
            Assert.NotNull(dataTableMapping.ColumnMappings);
            Assert.Equal(0, dataTableMapping.ColumnMappings.Count);

            dataTableMapping = new DataTableMapping();
            Assert.NotNull(dataTableMapping.ColumnMappings);
            Assert.Equal(0, dataTableMapping.ColumnMappings.Count);
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal("MyCustomSourceTable", _dataTableMapping.ToString());
        }
    }
}
