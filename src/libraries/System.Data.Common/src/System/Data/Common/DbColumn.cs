// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    case nameof(AllowDBNull):
                        return AllowDBNull;
                    case nameof(BaseCatalogName):
                        return BaseCatalogName;
                    case nameof(BaseColumnName):
                        return BaseColumnName;
                    case nameof(BaseSchemaName):
                        return BaseSchemaName;
                    case nameof(BaseServerName):
                        return BaseServerName;
                    case nameof(BaseTableName):
                        return BaseTableName;
                    case nameof(ColumnName):
                        return ColumnName;
                    case nameof(ColumnOrdinal):
                        return ColumnOrdinal;
                    case nameof(ColumnSize):
                        return ColumnSize;
                    case nameof(IsAliased):
                        return IsAliased;
                    case nameof(IsAutoIncrement):
                        return IsAutoIncrement;
                    case nameof(IsExpression):
                        return IsExpression;
                    case nameof(IsHidden):
                        return IsHidden;
                    case nameof(IsIdentity):
                        return IsIdentity;
                    case nameof(IsKey):
                        return IsKey;
                    case nameof(IsLong):
                        return IsLong;
                    case nameof(IsReadOnly):
                        return IsReadOnly;
                    case nameof(IsUnique):
                        return IsUnique;
                    case nameof(NumericPrecision):
                        return NumericPrecision;
                    case nameof(NumericScale):
                        return NumericScale;
                    case nameof(UdtAssemblyQualifiedName):
                        return UdtAssemblyQualifiedName;
                    case nameof(DataType):
                        return DataType;
                    case nameof(DataTypeName):
                        return DataTypeName;
                    default:
                        return null;
                }
            }
        }
    }
}
