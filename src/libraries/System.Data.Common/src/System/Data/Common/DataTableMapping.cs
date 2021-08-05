// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Data.Common
{
    [TypeConverter(typeof(DataTableMappingConverter))]
    public sealed class DataTableMapping : MarshalByRefObject, ITableMapping, ICloneable
    {
        private DataTableMappingCollection? _parent;
        private DataColumnMappingCollection? _columnMappings;
        private string? _dataSetTableName;
        private string? _sourceTableName;

        public DataTableMapping()
        {
        }

        public DataTableMapping(string? sourceTable, string? dataSetTable)
        {
            SourceTable = sourceTable;
            DataSetTable = dataSetTable;
        }

        public DataTableMapping(string? sourceTable, string? dataSetTable, DataColumnMapping[]? columnMappings)
        {
            SourceTable = sourceTable;
            DataSetTable = dataSetTable;
            if ((null != columnMappings) && (0 < columnMappings.Length))
            {
                ColumnMappings.AddRange(columnMappings);
            }
        }

        // explicit ITableMapping implementation
        IColumnMappingCollection ITableMapping.ColumnMappings
        {
            get { return ColumnMappings; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public DataColumnMappingCollection ColumnMappings
        {
            get
            {
                DataColumnMappingCollection? columnMappings = _columnMappings;
                if (null == columnMappings)
                {
                    columnMappings = new DataColumnMappingCollection();
                    _columnMappings = columnMappings;
                }
                return columnMappings;
            }
        }

        [DefaultValue("")]
        [AllowNull]
        public string DataSetTable
        {
            get { return _dataSetTableName ?? string.Empty; }
            set { _dataSetTableName = value; }
        }

        internal DataTableMappingCollection? Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
            }
        }

        [DefaultValue("")]
        [AllowNull]
        public string SourceTable
        {
            get { return _sourceTableName ?? string.Empty; }
            set
            {
                if ((null != Parent) && (0 != ADP.SrcCompare(_sourceTableName, value)))
                {
                    Parent.ValidateSourceTable(-1, value);
                }
                _sourceTableName = value;
            }
        }

        object ICloneable.Clone()
        {
            DataTableMapping clone = new DataTableMapping();
            clone._dataSetTableName = _dataSetTableName;
            clone._sourceTableName = _sourceTableName;

            if ((null != _columnMappings) && (0 < ColumnMappings.Count))
            {
                DataColumnMappingCollection parameters = clone.ColumnMappings;
                foreach (ICloneable parameter in ColumnMappings)
                {
                    parameters.Add(parameter.Clone());
                }
            }
            return clone;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataColumn? GetDataColumn(string sourceColumn, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type? dataType, DataTable dataTable, MissingMappingAction mappingAction, MissingSchemaAction schemaAction)
        {
            return DataColumnMappingCollection.GetDataColumn(_columnMappings, sourceColumn, dataType, dataTable, mappingAction, schemaAction);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataColumnMapping? GetColumnMappingBySchemaAction(string sourceColumn, MissingMappingAction mappingAction)
        {
            return DataColumnMappingCollection.GetColumnMappingBySchemaAction(_columnMappings, sourceColumn, mappingAction);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataTable? GetDataTableBySchemaAction(DataSet dataSet, MissingSchemaAction schemaAction)
        {
            if (null == dataSet)
            {
                throw ADP.ArgumentNull(nameof(dataSet));
            }
            string dataSetTable = DataSetTable;

            if (string.IsNullOrEmpty(dataSetTable))
            {
                return null;
            }
            DataTableCollection tables = dataSet.Tables;
            int index = tables.IndexOf(dataSetTable);
            if ((0 <= index) && (index < tables.Count))
            {
                return tables[index];
            }
            switch (schemaAction)
            {
                case MissingSchemaAction.Add:
                case MissingSchemaAction.AddWithKey:
                    return new DataTable(dataSetTable);

                case MissingSchemaAction.Ignore:
                    return null;

                case MissingSchemaAction.Error:
                    throw ADP.MissingTableSchema(dataSetTable, SourceTable);
            }
            throw ADP.InvalidMissingSchemaAction(schemaAction);
        }

        public override string ToString()
        {
            return SourceTable;
        }

        internal sealed class DataTableMappingConverter : System.ComponentModel.ExpandableObjectConverter
        {
            // converter classes should have public ctor
            public DataTableMappingConverter()
            {
            }

            public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            {
                if (typeof(InstanceDescriptor) == destinationType)
                {
                    return true;
                }
                return base.CanConvertTo(context, destinationType);
            }

            public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
            {
                if (null == destinationType)
                {
                    throw ADP.ArgumentNull(nameof(destinationType));
                }

                if ((typeof(InstanceDescriptor) == destinationType) && (value is DataTableMapping))
                {
                    DataTableMapping mapping = (DataTableMapping)value;

                    DataColumnMapping[] columnMappings = new DataColumnMapping[mapping.ColumnMappings.Count];
                    mapping.ColumnMappings.CopyTo(columnMappings, 0);
                    object[] values = new object[] { mapping.SourceTable, mapping.DataSetTable, columnMappings };
                    Type[] types = new Type[] { typeof(string), typeof(string), typeof(DataColumnMapping[]) };

                    ConstructorInfo ctor = typeof(DataTableMapping).GetConstructor(types)!;
                    return new InstanceDescriptor(ctor, values);
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
