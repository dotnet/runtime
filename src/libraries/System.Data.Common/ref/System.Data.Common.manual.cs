// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Data
{
    [System.ComponentModel.TypeConverter(typeof(ConstraintConverter))]
    public abstract partial class Constraint { }
    internal class ConstraintConverter { }

    [System.ComponentModel.TypeConverter(typeof(RelationshipConverter))]
    public partial class DataRelation { }
    internal class RelationshipConverter { }
}
namespace System.Data.Common
{
    [System.ComponentModel.TypeConverterAttribute(typeof(DataColumnMapping.DataColumnMappingConverter))]
    public sealed partial class DataColumnMapping 
    {
        internal class DataColumnMappingConverter { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(DataTableMapping.DataTableMappingConverter))]
    public sealed partial class DataTableMapping
    {
        internal class DataTableMappingConverter { }
    }
}
