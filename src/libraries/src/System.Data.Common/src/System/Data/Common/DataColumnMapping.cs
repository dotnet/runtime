// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace System.Data.Common
{
    [TypeConverter(typeof(DataColumnMappingConverter))]
    public sealed class DataColumnMapping : MarshalByRefObject, IColumnMapping, ICloneable
    {
        private DataColumnMappingCollection _parent;
        private string _dataSetColumnName;
        private string _sourceColumnName;

        public DataColumnMapping()
        {
        }

        public DataColumnMapping(string sourceColumn, string dataSetColumn)
        {
            SourceColumn = sourceColumn;
            DataSetColumn = dataSetColumn;
        }

        [DefaultValue("")]
        public string DataSetColumn
        {
            get { return _dataSetColumnName ?? string.Empty; }
            set { _dataSetColumnName = value; }
        }

        internal DataColumnMappingCollection Parent
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
        public string SourceColumn
        {
            get { return _sourceColumnName ?? string.Empty; }
            set
            {
                if ((null != Parent) && (0 != ADP.SrcCompare(_sourceColumnName, value)))
                {
                    Parent.ValidateSourceColumn(-1, value);
                }
                _sourceColumnName = value;
            }
        }

        object ICloneable.Clone()
        {
            DataColumnMapping clone = new DataColumnMapping();
            clone._sourceColumnName = _sourceColumnName;
            clone._dataSetColumnName = _dataSetColumnName;
            return clone;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataColumn GetDataColumnBySchemaAction(DataTable dataTable, Type dataType, MissingSchemaAction schemaAction)
        {
            return GetDataColumnBySchemaAction(SourceColumn, DataSetColumn, dataTable, dataType, schemaAction);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static DataColumn GetDataColumnBySchemaAction(string sourceColumn, string dataSetColumn, DataTable dataTable, Type dataType, MissingSchemaAction schemaAction)
        {
            if (null == dataTable)
            {
                throw ADP.ArgumentNull(nameof(dataTable));
            }
            if (string.IsNullOrEmpty(dataSetColumn))
            {
#if DEBUG
                if (AdapterSwitches.DataSchema.TraceWarning)
                {
                    Debug.WriteLine("explicit filtering of SourceColumn \"" + sourceColumn + "\"");
                }
#endif
                return null;
            }
            DataColumnCollection columns = dataTable.Columns;
            Debug.Assert(null != columns, "GetDataColumnBySchemaAction: unexpected null DataColumnCollection");

            int index = columns.IndexOf(dataSetColumn);
            if ((0 <= index) && (index < columns.Count))
            {
                DataColumn dataColumn = columns[index];
                Debug.Assert(null != dataColumn, "GetDataColumnBySchemaAction: unexpected null dataColumn");

                if (!string.IsNullOrEmpty(dataColumn.Expression))
                {
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceError)
                    {
                        Debug.WriteLine("schema mismatch on DataColumn \"" + dataSetColumn + "\" which is a computed column");
                    }
#endif
                    throw ADP.ColumnSchemaExpression(sourceColumn, dataSetColumn);
                }
                if ((null == dataType) || (dataType.IsArray == dataColumn.DataType.IsArray))
                {
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceInfo)
                    {
                        Debug.WriteLine("schema match on DataColumn \"" + dataSetColumn + "\"");
                    }
#endif
                    return dataColumn;
                }
#if DEBUG
                if (AdapterSwitches.DataSchema.TraceWarning)
                {
                    Debug.WriteLine("schema mismatch on DataColumn \"" + dataSetColumn + "\" " + dataType.Name + " != " + dataColumn.DataType.Name);
                }
#endif
                throw ADP.ColumnSchemaMismatch(sourceColumn, dataType, dataColumn);
            }

            return CreateDataColumnBySchemaAction(sourceColumn, dataSetColumn, dataTable, dataType, schemaAction);
        }

        internal static DataColumn CreateDataColumnBySchemaAction(string sourceColumn, string dataSetColumn, DataTable dataTable, Type dataType, MissingSchemaAction schemaAction)
        {
            Debug.Assert(dataTable != null, "Should not call with a null DataTable");
            if (string.IsNullOrEmpty(dataSetColumn))
            {
                return null;
            }

            switch (schemaAction)
            {
                case MissingSchemaAction.Add:
                case MissingSchemaAction.AddWithKey:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceInfo)
                    {
                        Debug.WriteLine("schema add of DataColumn \"" + dataSetColumn + "\" <" + Convert.ToString(dataType, CultureInfo.InvariantCulture) + ">");
                    }
#endif
                    return new DataColumn(dataSetColumn, dataType);

                case MissingSchemaAction.Ignore:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceWarning)
                    {
                        Debug.WriteLine("schema filter of DataColumn \"" + dataSetColumn + "\" <" + Convert.ToString(dataType, CultureInfo.InvariantCulture) + ">");
                    }
#endif
                    return null;

                case MissingSchemaAction.Error:
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceError)
                    {
                        Debug.WriteLine("schema error on DataColumn \"" + dataSetColumn + "\" <" + Convert.ToString(dataType, CultureInfo.InvariantCulture) + ">");
                    }
#endif
                    throw ADP.ColumnSchemaMissing(dataSetColumn, dataTable.TableName, sourceColumn);
            }
            throw ADP.InvalidMissingSchemaAction(schemaAction);
        }

        public override string ToString()
        {
            return SourceColumn;
        }

        internal sealed class DataColumnMappingConverter : System.ComponentModel.ExpandableObjectConverter
        {
            // converter classes should have public ctor
            public DataColumnMappingConverter()
            {
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (typeof(InstanceDescriptor) == destinationType)
                {
                    return true;
                }
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (null == destinationType)
                {
                    throw ADP.ArgumentNull(nameof(destinationType));
                }

                if ((typeof(InstanceDescriptor) == destinationType) && (value is DataColumnMapping))
                {
                    DataColumnMapping mapping = (DataColumnMapping)value;

                    object[] values = new object[] { mapping.SourceColumn, mapping.DataSetColumn };
                    Type[] types = new Type[] { typeof(string), typeof(string) };

                    ConstructorInfo ctor = typeof(DataColumnMapping).GetConstructor(types);
                    return new InstanceDescriptor(ctor, values);
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
