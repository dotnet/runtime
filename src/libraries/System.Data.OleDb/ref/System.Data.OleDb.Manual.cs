// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Data.OleDb
{
    [System.ComponentModel.TypeConverterAttribute(typeof(OleDbParameter.OleDbParameterConverter))]
    public sealed partial class OleDbParameter : System.Data.Common.DbParameter, System.Data.IDataParameter, System.Data.IDbDataParameter, System.ICloneable
    {
        internal sealed class OleDbParameterConverter : System.ComponentModel.ExpandableObjectConverter
        {
        }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(OleDbConnectionStringBuilder.OleDbConnectionStringBuilderConverter))]
    public sealed partial class OleDbConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
    {
        [System.ComponentModel.DisplayNameAttribute("OLE DB Services")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.ComponentModel.TypeConverterAttribute(typeof(OleDbServicesConverter))]
        public int OleDbServices { get { throw null; } set { } }
        [System.ComponentModel.DisplayNameAttribute("Provider")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.ComponentModel.TypeConverterAttribute(typeof(OleDbProviderConverter))]
        public string Provider { get { throw null; } set { } }

        internal sealed class OleDbConnectionStringBuilderConverter { }
        internal sealed class OleDbServicesConverter { }
        internal sealed class OleDbProviderConverter { }
    }
    public sealed partial class OleDbException
    {
        [System.ComponentModel.TypeConverterAttribute(typeof(ErrorCodeConverter))]
        public override int ErrorCode { get { throw null; } }

        internal sealed class ErrorCodeConverter { }
    }
}
