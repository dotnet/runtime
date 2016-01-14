// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Data.Common
{
    public class DbColumn
    {
        private readonly Dictionary<string, object> _customValues = new Dictionary<string, object>();
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
                object value;
                _customValues.TryGetValue(property, out value);
                return value;
            }
            set
            {
                _customValues[property] = value;
            }
        }

    }
}
