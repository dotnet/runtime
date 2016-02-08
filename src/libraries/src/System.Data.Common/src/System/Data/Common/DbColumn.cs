// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Data.Common
{
    public class DbColumn
    {
        public virtual bool AllowDBNull { get; set; }
        public virtual string BaseCatalogName { get; set; }
        public virtual string BaseColumnName { get; set; }
        public virtual string BaseSchemaName { get; set; }
        public virtual string BaseServerName { get; set; }
        public virtual string BaseTableName { get; set; }
        public virtual string ColumnName { get; set; }
        public virtual int ColumnOrdinal { get; set; }
        public virtual int ColumnSize { get; set; }
        public virtual bool IsAliased { get; set; }
        public virtual bool IsAutoIncrement { get; set; }
        public virtual bool IsExpression { get; set; }
        public virtual bool IsHidden { get; set; }
        public virtual bool IsIdentity { get; set; }
        public virtual bool IsKey { get; set; }
        public virtual bool IsLong { get; set; }
        public virtual bool IsReadOnly { get; set; }
        public virtual bool IsUnique { get; set; }
        public virtual int NumericPrecision { get; set; }
        public virtual int NumericScale { get; set; }
        public virtual string UdtAssemblyQualifiedName { get; set; }
        public virtual Type DataType { get; set; }
        public virtual string DataTypeName { get; set; }
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
