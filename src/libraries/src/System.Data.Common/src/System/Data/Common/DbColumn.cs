// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Data.Common
{
    public abstract class DbColumn
    {
        public bool? AllowDBNull { get; protected set; }
        public string BaseCatalogName { get; protected set; }
        public string BaseColumnName { get; protected set; }
        public string BaseSchemaName { get; protected set; }
        public string BaseServerName { get; protected set; }
        public string BaseTableName { get; protected set; }
        public string ColumnName { get; protected set; }
        public int? ColumnOrdinal { get; protected set; }
        public int? ColumnSize { get; protected set; }
        public bool? IsAliased { get; protected set; }
        public bool? IsAutoIncrement { get; protected set; }
        public bool? IsExpression { get; protected set; }
        public bool? IsHidden { get; protected set; }
        public bool? IsIdentity { get; protected set; }
        public bool? IsKey { get; protected set; }
        public bool? IsLong { get; protected set; }
        public bool? IsReadOnly { get; protected set; }
        public bool? IsUnique { get; protected set; }
        public int? NumericPrecision { get; protected set; }
        public int? NumericScale { get; protected set; }
        public string UdtAssemblyQualifiedName { get; protected set; }
        public Type DataType { get; protected set; }
        public string DataTypeName { get; protected set; }
        public virtual object this[string property]
        {
            get
            {
                switch (property)
                {
                    case "AllowDBNull":
                        return AllowDBNull;
                    case "BaseCatalogName":
                        return BaseCatalogName;
                    case "BaseColumnName":
                        return BaseColumnName;
                    case "BaseSchemaName":
                        return BaseSchemaName;
                    case "BaseServerName":
                        return BaseServerName;
                    case "BaseTableName":
                        return BaseTableName;
                    case "ColumnName":
                        return ColumnName;
                    case "ColumnOrdinal":
                        return ColumnOrdinal;
                    case "ColumnSize":
                        return ColumnSize;
                    case "IsAliased":
                        return IsAliased;
                    case "IsAutoIncrement":
                        return IsAutoIncrement;
                    case "IsExpression":
                        return IsExpression;
                    case "IsHidden":
                        return IsHidden;
                    case "IsIdentity":
                        return IsIdentity;
                    case "IsKey":
                        return IsKey;
                    case "IsLong":
                        return IsLong;
                    case "IsReadOnly":
                        return IsReadOnly;
                    case "IsUnique":
                        return IsUnique;
                    case "NumericPrecision":
                        return NumericPrecision;
                    case "NumericScale":
                        return NumericScale;
                    case "UdtAssemblyQualifiedName":
                        return UdtAssemblyQualifiedName;
                    case "DataType":
                        return DataType;
                    case "DataTypeName":
                        return DataTypeName;
                    default:
                        return null;
                }
            }
        }

    }
}
