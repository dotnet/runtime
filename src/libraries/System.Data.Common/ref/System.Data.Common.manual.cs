// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Data
{
    [System.ComponentModel.TypeConverter(typeof(ConstraintConverter))]
    public abstract partial class Constraint { }
    internal sealed class ConstraintConverter { }

    [System.ComponentModel.TypeConverter(typeof(RelationshipConverter))]
    public partial class DataRelation { }
    internal sealed class RelationshipConverter { }

    public partial class DataColumn
    {
        [System.ComponentModel.DefaultValueAttribute(typeof(string))]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.ComponentModel.TypeConverter(typeof(ColumnTypeConverter))]
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Type DataType { get { throw null; } set { } }

        [System.ComponentModel.TypeConverter(typeof(DefaultValueTypeConverter))]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object DefaultValue { get { throw null; } set { } }
    }
    internal sealed class ColumnTypeConverter { }
    internal sealed class DefaultValueTypeConverter { }

    public partial class DataTable
    {
        [System.ComponentModel.EditorAttribute("Microsoft.VSDesigner.Data.Design.PrimaryKeyEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [System.ComponentModel.TypeConverter(typeof(PrimaryKeyTypeConverter))]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Data.DataColumn[] PrimaryKey { get { throw null; } set { } }
    }
    internal sealed class PrimaryKeyTypeConverter { }

    public partial class DataView
    {
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.ComponentModel.TypeConverter(typeof(DataTableTypeConverter))]
        public System.Data.DataTable? Table { get { throw null; } set { } }
    }
    internal sealed class DataTableTypeConverter { }
}
namespace System.Data.Common
{
    [System.ComponentModel.TypeConverterAttribute(typeof(DataColumnMapping.DataColumnMappingConverter))]
    public sealed partial class DataColumnMapping
    {
        internal sealed class DataColumnMappingConverter { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(DataTableMapping.DataTableMappingConverter))]
    public sealed partial class DataTableMapping
    {
        internal sealed class DataTableMappingConverter { }
    }
}
