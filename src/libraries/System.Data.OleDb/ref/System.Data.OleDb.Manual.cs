// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Data.OleDb
{
    [System.ComponentModel.TypeConverterAttribute(typeof(OleDbParameter.OleDbParameterConverter))]
    public sealed partial class OleDbParameter : System.Data.Common.DbParameter, System.Data.IDataParameter, System.Data.IDbDataParameter, System.ICloneable
    {
        internal class OleDbParameterConverter : System.ComponentModel.ExpandableObjectConverter
        {
        }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(OleDbConnectionStringBuilder.OleDbConnectionStringBuilderConverter))]
    public sealed partial class OleDbConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
    {
        internal class OleDbConnectionStringBuilderConverter : System.ComponentModel.ExpandableObjectConverter
        {
        }
    }
}
